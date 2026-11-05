// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Plays streaming TTS audio from a <see cref="TryllAgent"/> through an
    /// <see cref="AudioSource"/>. Wire it up by assigning this component to the
    /// <c>Speaker</c> slot on a <see cref="TryllAgentComponent"/>, or from a
    /// code-created agent:
    /// <c>agent.SetOnTtsAudioFormat(speaker.HandleFormat)</c> and
    /// <c>agent.SetOnTtsAudio(speaker.HandleAudio)</c>.
    ///
    /// Threading
    /// ---------
    /// <see cref="HandleFormat"/> and <see cref="HandleAudio"/> fire on the Unity
    /// main thread (drained by <see cref="TryllClient.Update"/>).
    /// <see cref="PcmReader"/> runs on the audio thread. Both share a chunk queue
    /// guarded by a short critical section — the lock is held only long enough
    /// to enqueue or dequeue one chunk reference, matching the cpp client's
    /// SpeakerPlayback design.
    ///
    /// End-of-stream
    /// -------------
    /// <c>TtsAudioFrame</c> carries no <c>is_final</c> flag — the turn ends with
    /// <c>TurnComplete</c>. The queue drains naturally; no explicit flush.
    /// </summary>
    [AddComponentMenu("Tryll/Tryll Speaker")]
    [RequireComponent(typeof(AudioSource))]
    public sealed class TryllSpeaker : MonoBehaviour
    {
        // Cap on the number of queued chunks. TTS synthesis runs well above
        // realtime, so for a long answer chunks pile up faster than playback
        // drains them. Dropping the *newest* chunk on overflow is safer than
        // truncating mid-sentence — matches cpp SpeakerPlayback (256).
        private const int kMaxQueuedChunks = 1024;

        [SerializeField] private AudioSource _src;
        private AudioClip          _clip;
        private readonly object    _gate    = new object();
        private readonly Queue<float[]> _chunks = new Queue<float[]>();
        private int                _frontOffset;     // audio thread only
        private long               _consumedSamples; // guarded by _gate
        private int                _sampleRate;
        private bool               _overflowLoggedThisTurn;
        private bool               _suppress;   // drop audio until the next turn's format announcement (StopPlayback)

        /// <summary>
        /// Fired on the Unity main thread for each TTS format announcement, after
        /// the mono-int16 check. Additive multicast — subscribe with <c>+=</c>.
        /// </summary>
        public event Action<string, int, int, int> OnTtsFormat;

        /// <summary>
        /// Fired on the Unity main thread for each TTS PCM chunk, with a
        /// read-only view valid for the duration of the callback. Additive
        /// multicast — subscribe with <c>+=</c>. Mirrors Unreal
        /// <c>UTryllSpeakerComponent::OnTtsPcm</c>.
        /// </summary>
        public event Action<string, ArraySegment<short>> OnTtsPcm;

        private void Awake()
        {
            // Auto-assign the AudioSource so the component works whether it was added
            // in the Editor or via AddComponent at runtime. RequireComponent normally
            // provisions one; add it explicitly as a fallback if it's still missing.
            if (_src == null) _src = GetComponent<AudioSource>();
            if (_src == null) _src = gameObject.AddComponent<AudioSource>();
        }

        /// <summary>
        /// Called when the server announces the audio format for the current turn.
        /// First call creates the streaming <see cref="AudioClip"/>; subsequent
        /// calls with the same sample rate are no-ops, a different sample rate
        /// rebuilds the clip and drops any queued audio.
        ///
        /// <paramref name="nodeName"/> (the producing node's name — multi-sender
        /// attribution) is accepted for signature compatibility with
        /// <see cref="TryllAgent.SetOnTtsAudioFormat"/> but otherwise unused: this
        /// speaker mixes all producers into one <see cref="AudioSource"/>.
        /// </summary>
        public void HandleFormat(string nodeName, int sampleRate, int channels, int bitsPerSample)
        {
            // A turn's audio is starting; lift any StopPlayback. Since protocol v27 the
            // server announces the format once per agent turn, from the first speaking node.
            _suppress = false;

            if (channels != 1 || bitsPerSample != 16)
            {
                TryllLog.Warning(
                    $"TryllSpeaker: unsupported TTS format ({channels}ch {bitsPerSample}bit). " +
                    "Only mono int16 PCM is supported.");
                return;
            }

            // A new rate restarts the sample clock below. Listeners mark their turn
            // origin from ConsumedSamples when this fires, so reset first and announce
            // after — announcing first hands them an origin from the old clock, and
            // every position in the turn then reads as negative.
            bool sameStream = _clip != null && _sampleRate == sampleRate;
            if (!sameStream && _src != null)
            {
                _sampleRate = sampleRate;
                lock (_gate)
                {
                    DiscardQueuedLocked();
                    // A different sample rate changes the clock's unit; restart it so
                    // callers never mix samples counted at two rates.
                    _consumedSamples = 0;
                }
            }

            OnTtsFormat?.Invoke(nodeName, sampleRate, channels, bitsPerSample);

            // _src is bound in Awake, which only runs in Play mode. When the agent is
            // driven outside Play mode (Window ▸ Tryll ▸ Chat) there is nothing to play
            // through — say so instead of throwing. Returning before _sampleRate is set
            // also makes HandleAudio a no-op.
            if (_src == null)
            {
                TryllLog.Error(
                    "TryllSpeaker: no AudioSource — cannot play TTS audio. Assemble the agent " +
                    "GameObject fully (a TryllSpeaker with its AudioSource, assigned to the " +
                    "Speaker slot on TryllAgentComponent) and run it in Play mode: components " +
                    "added outside Play mode never receive Awake, so the speaker is never " +
                    "initialised.");
                return;
            }

            _overflowLoggedThisTurn = false;
            if (sameStream)
            {
                // The clip is still bound, but OnDisable stops the source; restart it
                // so disabling the component between turns does not mute it for good.
                if (!_src.isPlaying) _src.Play();
                return;
            }

            if (_src.isPlaying) _src.Stop();
            // Small clip buffer to keep startup latency low. Unity drives playback
            // by repeatedly calling PcmReader regardless of clip length when
            // stream:true + loop:true, so this is just the per-tick wrap window.
            int lengthSamples = Mathf.Max(256, sampleRate / 10); // ~100 ms
            _clip = AudioClip.Create("TryllTTS", lengthSamples, 1, sampleRate, true, PcmReader);
            _src.clip = _clip;
            _src.loop = true;
            _src.Play();
        }

        /// <summary>
        /// Enqueue a PCM chunk. Converts int16 → float once on the producer.
        /// Drops the newest chunk if the queue is full (long answer overflow).
        /// <paramref name="nodeName"/> is accepted for signature compatibility
        /// (see <see cref="HandleFormat"/>) but unused.
        /// </summary>
        public void HandleAudio(string nodeName, ArraySegment<short> pcm)
        {
            OnTtsPcm?.Invoke(nodeName, pcm);

            if (_sampleRate == 0 || pcm.Count == 0 || _suppress) return;

            const float kInv = 1f / 32768f;
            var chunk  = new float[pcm.Count];
            var src    = pcm.Array;
            int offset = pcm.Offset;
            for (int i = 0; i < pcm.Count; i++)
                chunk[i] = src[offset + i] * kInv;

            lock (_gate)
            {
                if (_chunks.Count >= kMaxQueuedChunks)
                {
                    if (!_overflowLoggedThisTurn)
                    {
                        TryllLog.Warning(
                            $"TryllSpeaker: dropping chunk, queue full ({_chunks.Count} chunks)");
                        _overflowLoggedThisTurn = true;
                    }
                    return;
                }
                _chunks.Enqueue(chunk);
            }
        }

        // Audio thread.
        private void PcmReader(float[] data)
        {
            int written = 0;
            lock (_gate)
            {
                while (written < data.Length && _chunks.Count > 0)
                {
                    var front     = _chunks.Peek();
                    int available = front.Length - _frontOffset;
                    int needed    = data.Length - written;
                    int toCopy    = available < needed ? available : needed;
                    Array.Copy(front, _frontOffset, data, written, toCopy);
                    written          += toCopy;
                    _frontOffset     += toCopy;
                    _consumedSamples += toCopy;
                    if (_frontOffset >= front.Length)
                    {
                        _chunks.Dequeue();
                        _frontOffset = 0;
                    }
                }
            }
            // Zero-fill the tail on underrun.
            if (written < data.Length)
                Array.Clear(data, written, data.Length - written);
        }

        /// <summary>
        /// True while decoded audio is still waiting in the chunk queue (or the front chunk is
        /// only partially played). Combined with the turn's <c>TurnComplete</c> — after which no
        /// further audio arrives — a false value means playback has drained down to the small
        /// (~100 ms) streaming clip buffer. Safe to call from the Unity main thread.
        /// </summary>
        public bool HasPendingAudio
        {
            get
            {
                lock (_gate)
                {
                    return _chunks.Count > 0 || _frontOffset > 0;
                }
            }
        }

        /// <summary>
        /// Monotonic count of TTS samples this speaker has handed to the audio device —
        /// the playback clock. Units are samples at <see cref="HandleFormat"/>'s announced
        /// rate (mono, so samples == frames); underrun silence is NOT counted, so the clock
        /// tracks the audio stream rather than wall time.
        ///
        /// <para>Audio dropped without being played (a cancelled turn, a format change,
        /// disable) counts as played, so the clock never falls permanently behind the
        /// stream. This mirrors <c>FTryllTtsPlaybackClock::GetPlayedSamples()</c> in the
        /// Unreal client.</para>
        ///
        /// <para>Safe to read from any thread. Callers slaving animation to speech should
        /// compare this against sample offsets derived from the same PCM stream.</para>
        /// </summary>
        public long ConsumedSamples
        {
            get { lock (_gate) return _consumedSamples; }
        }

        /// <summary>
        /// Samples still waiting in the chunk queue (including the unconsumed tail of
        /// the front chunk). Safe to read from the Unity main thread. Used to place
        /// a new turn's sample origin after leftover audio from the previous turn.
        /// </summary>
        public long QueuedSamples
        {
            get
            {
                lock (_gate)
                {
                    long n = 0;
                    bool first = true;
                    foreach (var c in _chunks)
                    {
                        n += c.Length;
                        if (first)
                        {
                            n -= _frontOffset;
                            first = false;
                        }
                    }
                    return n;
                }
            }
        }

        /// <summary>
        /// Drops every queued sample and charges it to the playback clock. Call under <c>_gate</c>.
        /// </summary>
        private void DiscardQueuedLocked()
        {
            foreach (var c in _chunks) _consumedSamples += c.Length;
            _consumedSamples -= _frontOffset;   // the front chunk's head was already counted
            _chunks.Clear();
            _frontOffset = 0;
        }

        /// <summary>
        /// Cancels the current playback: drops all queued audio (silencing within the small
        /// clip buffer) and ignores any further audio frames until the next
        /// <see cref="HandleFormat"/>, which arrives once per agent turn (protocol v27), so the
        /// rest of the current turn stays silent even across several speaking nodes. The streaming <see cref="AudioSource"/> is left running so
        /// subsequent turns play normally. Safe to call repeatedly and when nothing is playing.
        /// Call from the Unity main thread.
        /// </summary>
        public void StopPlayback()
        {
            _suppress = true;
            lock (_gate)
            {
                DiscardQueuedLocked();
            }
        }

        private void OnDisable()
        {
            if (_src != null && _src.isPlaying) _src.Stop();
            lock (_gate)
            {
                DiscardQueuedLocked();
            }
        }

        private void OnDestroy()
        {
            if (_clip != null)
            {
                Destroy(_clip);
                _clip = null;
            }
        }
    }
}
