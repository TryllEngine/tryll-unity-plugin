// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Runtime.InteropServices;
using Tryll;
using Tryll.Client.Internal;

namespace Tryll.Client
{
    // ── Value types ───────────────────────────────────────────────────────────

    /// <summary>
    /// Declares the audio format of samples pushed via <see cref="TryllVoiceInput.SendAudioBuffer"/>.
    /// The server resamples to the STT model's expected rate as needed.
    /// </summary>
    [Serializable]
    public struct AudioFormat
    {
        public uint   SampleRate;     // Hz (e.g. 48000 for mic capture)
        public ushort Channels;       // mic capture is typically 1
        public ushort BitsPerSample;  // only 16-bit PCM is supported

        public static AudioFormat Default => new AudioFormat
        {
            SampleRate = 16000, Channels = 1, BitsPerSample = 16,
        };
    }

    /// <summary>Parameters for creating a <see cref="TryllVoiceInput"/> handle.</summary>
    [Serializable]
    public struct VoiceInputConfig
    {
        public string      ModelName;        // catalog name, e.g. "Whisper Tiny EN (int8)"
        public AudioFormat InputFormat;      // format of samples passed to SendAudioBuffer
        public float       VadThreshold;     // Silero VAD speech-probability threshold
        public uint        VadMinSilenceMs;  // silence (ms) that closes a segment
        public uint        VadSpeechPadMs;   // padding (ms) around detected speech
        /// <summary>Relative path (resolved against the session storage folder) to a <c>StringStorage</c>
        /// (kind=List) config whose phrases the STT decoder biases toward. Loaded on demand.
        /// Empty string (default) disables hotword biasing.</summary>
        public string      HotwordsStoragePath;
        /// <summary>Per-token bias strength applied to every phrase in the hotwords storage (default 1.5).
        /// Weights matched phrases against one another during beam search.
        /// Measured on Parakeet TDT 0.6B v3: 1.8 recovered 5 of 8 target
        /// terms, 2.5 recovered 6 of 8. A lexicon line may override this for
        /// itself by ending with <c>:&lt;number&gt;</c>.</summary>
        public float       HotwordsScore;
    }

    /// <summary>Per-utterance options passed to <see cref="TryllVoiceInput.BeginUtterance"/>.</summary>
    [Serializable]
    public struct UtteranceOptions
    {
        /// <summary>Let server VAD close the utterance on silence.</summary>
        public bool  AutoFinishOnSilence;
        /// <summary>Hard timeout (ms) per utterance.</summary>
        public uint  MaxUtteranceMs;

        public static UtteranceOptions Default => new UtteranceOptions
        {
            AutoFinishOnSilence = true, MaxUtteranceMs = 60000,
        };
    }

    /// <summary>
    /// What the server does after an empty or speech-ended UtteranceFinal.
    /// Mirrors <c>Tryll.SilencePolicy</c> (ordinal values must stay in sync).
    /// </summary>
    public enum SilencePolicy : byte
    {
        StopListening     = 0,
        ContinueListening = 1,
    }

    /// <summary>
    /// Classifies a transcript update. Mirrors the wire schema enum
    /// <c>Tryll.TranscriptUpdateKind</c> (ordinal values must stay in sync).
    /// </summary>
    public enum TranscriptUpdateKind : byte
    {
        SpeechStart    = 0,  // VAD rising edge; text is empty
        Partial        = 1,  // revisable in-progress hypothesis (online STT)
        SegmentFinal   = 2,  // engine committed a chunk; utterance still open
        UtteranceFinal = 3,  // last update for this Begin/End cycle
    }

    /// <summary>A single transcript update delivered via <see cref="TryllVoiceInput.OnTranscriptUpdate"/>.</summary>
    public struct TranscriptUpdate
    {
        public string               Text;
        public TranscriptUpdateKind Kind;
        public uint                 AudioMsConsumed;
    }

    // ── VoiceInput handle ─────────────────────────────────────────────────────

    /// <summary>
    /// Handle for a server-side VoiceInput session. Mirrors the C++ <c>Tryll::VoiceInput</c>
    /// and is the Unity counterpart to <see cref="TryllAgent"/>.
    ///
    /// Obtained via <see cref="TryllClient.CreateVoiceInputAsync"/>. The caller owns the
    /// handle and must call <see cref="Dispose"/> (the destructor of the server-side
    /// session is only released on Dispose or when the session closes).
    ///
    /// Threading: all callbacks fire on the Unity main thread (from
    /// <see cref="TryllClient.Update"/>). All public methods must be called from the
    /// main thread.
    /// </summary>
    public sealed class TryllVoiceInput : IDisposable
    {
        private readonly TryllClient _client;
        private ulong _voiceInputId;
        private bool  _disposed;

        // ── Callbacks (settable by TryllVoiceInputComponent or user code) ──────

        /// <summary>Fired for each transcript update. Invoked on the Unity main thread.</summary>
        public Action<TranscriptUpdate> OnTranscriptUpdate;

        /// <summary>Fired on protocol-level errors not tied to a Begin/End/Cancel callback.</summary>
        public Action<TryllError> OnError;

        // ── Constructor / Dispose ─────────────────────────────────────────────

        internal TryllVoiceInput(TryllClient client, ulong voiceInputId)
        {
            _client       = client;
            _voiceInputId = voiceInputId;
        }

        /// <summary>
        /// Cancel any active utterance and destroy the server-side handle.
        /// Safe to call multiple times — subsequent calls are no-ops.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            var cancel = IsUtteranceActive;
            _client?.Connection.FailOwnedBy(
                this, new TryllError(TryllErrorCode.VoiceInputNotFound, "VoiceInput destroyed"));

            if (_voiceInputId != 0)
            {
                if (cancel)
                    SendCancelUtterance(null);
                _client?.RequestDestroyVoiceInputInternal(this);
                _voiceInputId = 0;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public ulong VoiceInputId      => _voiceInputId;
        public bool  IsValid           => !_disposed && _voiceInputId != 0;
        public bool  IsUtteranceActive { get; private set; }

        /// <summary>
        /// Open a new utterance. Rejects locally with <see cref="TryllErrorCode.UtteranceInProgress"/>
        /// (via <paramref name="onComplete"/>) when one is already active.
        /// </summary>
        public void BeginUtterance(UtteranceOptions opts, Action<TryllError> onComplete = null)
        {
            if (!IsValid)
            {
                onComplete?.Invoke(new TryllError(TryllErrorCode.VoiceInputNotFound, "VoiceInput handle not valid"));
                return;
            }
            if (IsUtteranceActive)
            {
                onComplete?.Invoke(new TryllError(TryllErrorCode.UtteranceInProgress, "Utterance already in progress"));
                return;
            }

            IsUtteranceActive = true;  // eager — allows SendAudioBuffer before the ack
            var voiceId = _voiceInputId;
            var autoFinish = opts.AutoFinishOnSilence;
            var maxMs = opts.MaxUtteranceMs;
            ulong notedId = 0;
            TryllCalls.BeginUtterance(
                _client.Connection, this, voiceId, autoFinish, maxMs,
                err =>
                {
                    if (!err.IsOk)
                        IsUtteranceActive = false;
                    NoteAck(MessageBody.BeginUtteranceRequest, err.IsOk, err, voiceId, notedId);
                    onComplete?.Invoke(err);
                },
                rid =>
                {
                    notedId = rid;
                    _client.NoteOutbound(TryllObservationNote.Request(MessageBody.BeginUtteranceRequest, rid).Fill(n => { n.Flag = autoFinish; n.Code = maxMs; n.VoiceInputId = voiceId; }));
                });
        }

        /// <summary>
        /// Push a block of mono int16 PCM to the server. Fire-and-forget; dropped silently
        /// when no utterance is active.
        /// </summary>
        public void SendAudioBuffer(ReadOnlySpan<short> pcm)
        {
            if (!IsValid || !IsUtteranceActive || pcm.IsEmpty) return;
            var bytes = MemoryMarshal.AsBytes(pcm).ToArray();
            var voiceId = _voiceInputId;
            _client.NoteOutbound(TryllObservationNote.Request(MessageBody.AudioBuffer, 0).Fill(n => { n.BinaryByteCount = bytes != null ? (uint)bytes.Length : 0; n.BinaryKind = "pcm"; n.VoiceInputId = voiceId; }));
            _client.Connection.Send(ClientCodec.BuildAudioBuffer(voiceId, bytes));
        }

        /// <summary>Commit the current utterance (push-to-talk release).</summary>
        public void EndUtterance(Action<TryllError> onComplete = null)
        {
            if (!IsValid)
            {
                onComplete?.Invoke(new TryllError(TryllErrorCode.VoiceInputNotFound, "VoiceInput handle not valid"));
                return;
            }
            IsUtteranceActive = false;
            SendEndUtterance(onComplete);
        }

        /// <summary>Cancel the current utterance without producing a transcript.</summary>
        public void CancelUtterance(Action<TryllError> onComplete = null)
        {
            if (!IsValid)
            {
                onComplete?.Invoke(new TryllError(TryllErrorCode.VoiceInputNotFound, "VoiceInput handle not valid"));
                return;
            }
            IsUtteranceActive = false;
            SendCancelUtterance(onComplete);
        }

        // ── Internal dispatch (called by TryllClient.DispatchEvent, main thread) ─

        internal void HandleTranscriptUpdate(string text, TranscriptUpdateKind kind, uint audioMsConsumed)
        {
            // Every UtteranceFinal closes the turn (non-empty speech final,
            // EndUtterance, or the max-utterance timeout). Silence heartbeats
            // arrive as empty SegmentFinals and do not close.
            if (kind == TranscriptUpdateKind.UtteranceFinal)
                IsUtteranceActive = false;
            OnTranscriptUpdate?.Invoke(new TranscriptUpdate
            {
                Text            = text ?? "",
                Kind            = kind,
                AudioMsConsumed = audioMsConsumed,
            });
        }

        internal void HandleError(TryllError error) => OnError?.Invoke(error);

        /// <summary>
        /// Complete every outstanding Begin/End/Cancel callback so a drop or
        /// socket replacement cannot leave callers waiting.
        /// </summary>
        internal void DrainPendingAcks(TryllError terminalError)
        {
            _client?.Connection.FailOwnedBy(this, terminalError);
            IsUtteranceActive = false;
        }

        private void SendEndUtterance(Action<TryllError> onComplete)
        {
            var voiceId = _voiceInputId;
            ulong notedId = 0;
            TryllCalls.EndUtterance(
                _client.Connection, this, voiceId,
                err =>
                {
                    NoteAck(MessageBody.EndUtteranceRequest, err.IsOk, err, voiceId, notedId);
                    onComplete?.Invoke(err);
                },
                rid =>
                {
                    notedId = rid;
                    _client.NoteOutbound(TryllObservationNote.Request(MessageBody.EndUtteranceRequest, rid).Fill(n => { n.VoiceInputId = voiceId; }));
                });
        }

        private void SendCancelUtterance(Action<TryllError> onComplete)
        {
            var voiceId = _voiceInputId;
            ulong notedId = 0;
            TryllCalls.CancelUtterance(
                _client.Connection, this, voiceId,
                err =>
                {
                    NoteAck(MessageBody.CancelUtteranceRequest, err.IsOk, err, voiceId, notedId);
                    onComplete?.Invoke(err);
                },
                rid =>
                {
                    notedId = rid;
                    _client.NoteOutbound(TryllObservationNote.Request(MessageBody.CancelUtteranceRequest, rid).Fill(n => { n.VoiceInputId = voiceId; }));
                });
        }

        private void NoteAck(MessageBody request, bool ok, TryllError err, ulong voiceId, ulong requestId)
        {
            var note = TryllObservationNote.Reply(request, ok, requestId);
            note.VoiceInputId = voiceId;
            note.Code = (uint)err.Code;
            if (!ok)
                note.Message = err.Message;
            _client.NoteInbound(note);
        }

        internal void ClearAfterDestroyRequest()
        {
            _voiceInputId = 0;
            IsUtteranceActive = false;
        }
    }
}
