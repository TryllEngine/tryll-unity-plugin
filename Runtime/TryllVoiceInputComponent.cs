// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections;
using Tryll;
using UnityEngine;
using UnityEngine.Events;

namespace Tryll.Client
{
    /// <summary>
    /// Whether a <see cref="TryllVoiceInputComponent"/> has a microphone it can capture from.
    /// </summary>
    public enum TryllMicrophoneStatus
    {
        /// <summary>Not checked yet: no CreateVoiceInput or BeginUtterance has run.</summary>
        Unknown,
        /// <summary>A recording device is present (the configured one, when a name is set).</summary>
        Available,
        /// <summary>The system reports no recording device at all.</summary>
        NoDevice,
        /// <summary><see cref="TryllVoiceInputComponent.micDeviceName"/> names a device that is not connected.</summary>
        DeviceNotFound,
        /// <summary>The device is listed but <c>Microphone.Start</c> could not open it.</summary>
        CaptureFailed,
    }

    /// <summary>
    /// Component that owns one <see cref="TryllVoiceInput"/> handle and drives it from
    /// the Unity microphone. The Unity counterpart to the Unreal
    /// <c>UTryllVoiceInputComponent</c>.
    ///
    /// Capture model: on-demand. The microphone starts on <see cref="BeginUtterance"/>
    /// and stops when the final transcript arrives (or on End / Cancel). A coroutine
    /// pumps small PCM chunks at <see cref="pumpIntervalMs"/> cadence.
    ///
    /// Microphone check: <see cref="CreateVoiceInput"/> and every
    /// <see cref="BeginUtterance"/> first check that a recording device exists. Without
    /// one the voice input is not created (no STT model is loaded),
    /// <see cref="MicrophoneStatus"/> says why, one warning is logged, and each call
    /// raises <see cref="OnError"/> with <see cref="TryllErrorCode.MicrophoneUnavailable"/>
    /// (except the automatic create, which may run before listeners subscribe: read
    /// <see cref="MicrophoneStatus"/> at start-up). A later BeginUtterance that finds a
    /// microphone creates the voice input and raises
    /// <see cref="TryllErrorCode.VoiceInputStarting"/> for that call.
    ///
    /// Audio format: the device-native sample rate is declared to the server, which
    /// resamples to the STT model's expected rate.
    ///
    /// Threading: all <see cref="UnityEvent"/> callbacks fire on the Unity main thread.
    /// </summary>
    [AddComponentMenu("Tryll/Tryll Voice Input Component")]
    [DisallowMultipleComponent]
    [HelpURL(TryllDocs.TryllVoiceInputComponent)]
    public sealed class TryllVoiceInputComponent : MonoBehaviour
    {
        // ── Inspector fields ───────────────────────────────────────────────────

        [Tooltip("STT model catalog name.")]
        [TryllModelType("stt")]
        public string modelName = "Whisper Tiny EN (int8)";

        [Tooltip("Agent that receives the final transcript. Leave empty for transcribe-only mode.")]
        public TryllAgentComponent targetAgent;

        [Tooltip("Let server VAD close the utterance on silence (stop-on-silence mode).")]
        public bool autoFinishOnSilence = true;

        [Tooltip("Hard timeout (ms) per utterance.")]
        public uint maxUtteranceMs = 60000;

        [Tooltip("Silero VAD speech-probability threshold.")]
        [Range(0f, 1f)] public float vadThreshold = 0.5f;

        [Tooltip("Silence duration (ms) that closes a speech segment.")]
        public uint vadMinSilenceMs = 1000;

        [Tooltip("Padding (ms) added around detected speech.")]
        public uint vadSpeechPadMs = 250;

        [Tooltip("Relative path (resolved against Storage Data Folder) to a UTF-8 " +
                 "phrase list (one phrase per line). Empty disables hotword biasing.")]
        [TryllFilePicker]
        public string hotwordsStoragePath = "";

        [Tooltip("How strongly matched phrases are weighted against one another " +
                 "during beam search. Ignored when Hotwords Storage Path is empty.")]
        [Min(0f)]
        public float hotwordsScore = 1.5f;

        [Tooltip("Microphone device name. Empty = system default.")]
        public string micDeviceName = "";

        [Tooltip("PCM pump cadence in milliseconds.")]
        public float pumpIntervalMs = 50f;

        [Tooltip("Microphone ring-buffer length in seconds.")]
        public int ringBufferSeconds = 2;

        [Tooltip("Create the VoiceInput handle automatically once the TryllClient session is ready.")]
        public bool createOnEnable = true;

        // ── Events ─────────────────────────────────────────────────────────────

        [Serializable] public sealed class TranscriptEvent : UnityEvent<TranscriptUpdate> { }
        [Serializable] public sealed class ErrorEvent      : UnityEvent<TryllError>       { }

        /// <summary>Fired for each transcript update (partial and final).</summary>
        public TranscriptEvent OnTranscriptUpdate = new TranscriptEvent();

        /// <summary>Fired on protocol-level errors.</summary>
        public ErrorEvent OnError = new ErrorEvent();

        /// <summary>Fired when the VoiceInput handle is created.</summary>
        public UnityEvent OnVoiceInputCreated = new UnityEvent();

        // ── Runtime state ──────────────────────────────────────────────────────

        /// <summary>The live handle. Null until <see cref="CreateVoiceInput"/> completes.</summary>
        public TryllVoiceInput Voice { get; private set; }

        public bool HasVoiceInput   => Voice != null && Voice.IsValid;
        public bool IsUtteranceActive => Voice?.IsUtteranceActive ?? false;

        /// <summary>Result of the last microphone check. See <see cref="TryllMicrophoneStatus"/>.</summary>
        public TryllMicrophoneStatus MicrophoneStatus { get; private set; } = TryllMicrophoneStatus.Unknown;

        /// <summary>Why the microphone is unavailable, readable as-is. Empty when it is not.</summary>
        public string MicrophoneStatusMessage { get; private set; } = "";

        /// <summary>True when the last check found no usable microphone.</summary>
        public bool IsMicrophoneUnavailable =>
            MicrophoneStatus == TryllMicrophoneStatus.NoDevice
            || MicrophoneStatus == TryllMicrophoneStatus.DeviceNotFound
            || MicrophoneStatus == TryllMicrophoneStatus.CaptureFailed;

        /// <summary>Device list source. Tests replace it; everything else uses <c>Microphone.devices</c>.</summary>
        internal Func<string[]> MicrophoneDevices = () => Microphone.devices;

        private AudioClip _micClip;
        private int       _nativeRate;
        private int       _readPos;
        private Coroutine _pumpCoroutine;
        private bool      _creating;
        private bool      _createQueued;
        // CreateVoiceInput gave up for lack of a microphone; BeginUtterance retries it.
        private bool      _createSkippedNoMic;
        // Message of the problem already logged, so a repeated failure stays quiet.
        private string    _loggedMicProblem;

        private string Device => string.IsNullOrEmpty(micDeviceName) ? null : micDeviceName;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            var client = TryllClient.Instance;
            if (client == null) return;

            client.ConnectionChanged += OnConnectionChanged;
            client.CreateSessionComplete += OnCreateSessionComplete;

            // VoiceInput creation requires a session, not just a TCP connection
            // (protocol v4): create only once CreateSession has succeeded.
            if (createOnEnable && client.IsSessionReady && !HasVoiceInput)
                CreateVoiceInput(reportMicrophoneError: false);
        }

        private void OnDisable()
        {
            var client = TryllClient.Instance;
            if (client != null)
            {
                client.ConnectionChanged -= OnConnectionChanged;
                client.CreateSessionComplete -= OnCreateSessionComplete;
            }

            StopCapture();
            DestroyVoiceInput();
        }

        private void OnDestroy()
        {
            StopCapture();
            DestroyVoiceInput();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Create the server-side VoiceInput handle. Ignored when one already exists.
        /// Populates <see cref="Voice"/> and fires <see cref="OnVoiceInputCreated"/> on success.
        /// Without a microphone it raises <see cref="OnError"/> with
        /// <see cref="TryllErrorCode.MicrophoneUnavailable"/> instead.
        /// </summary>
        public void CreateVoiceInput() => CreateVoiceInput(reportMicrophoneError: true);

        /// <param name="reportMicrophoneError">
        /// False for the automatic create from OnEnable / session start. That can run
        /// before other scripts have subscribed to <see cref="OnError"/>, so it only sets
        /// <see cref="MicrophoneStatus"/> and logs; the next BeginUtterance raises the error.
        /// </param>
        private async void CreateVoiceInput(bool reportMicrophoneError)
        {
            if (HasVoiceInput || _creating) return;

            var client = TryllClient.Instance;
            if (client == null)
            {
                TryllLog.Error("TryllVoiceInputComponent.CreateVoiceInput: TryllClient.Instance is null");
                return;
            }

            // The session must exist first (protocol v4): CreateVoiceInput before
            // CreateSession is rejected by the server with SessionNotReady.  Remember
            // the intent and let OnCreateSessionComplete drive creation instead.
            if (!client.IsSessionReady)
            {
                _createQueued = true;
                TryllLog.Verbose("TryllVoiceInputComponent: session not ready; deferring CreateVoiceInput until CreateSession completes");
                return;
            }
            _createQueued = false;

            // Without a device, ResolveNativeRate would log Unity's own error and guess
            // a rate, and the server would load an STT model nothing can feed.
            _createSkippedNoMic = !CheckMicrophone();
            if (_createSkippedNoMic)
            {
                if (reportMicrophoneError)
                    OnError.Invoke(MicrophoneError());
                return;
            }

            _creating   = true;
            _nativeRate = ResolveNativeRate();

            var cfg = new VoiceInputConfig
            {
                ModelName       = modelName,
                InputFormat     = new AudioFormat
                {
                    SampleRate    = (uint)_nativeRate,
                    Channels      = 1,   // UnityEngine.Microphone always captures mono
                    BitsPerSample = 16,
                },
                VadThreshold         = vadThreshold,
                VadMinSilenceMs      = vadMinSilenceMs,
                VadSpeechPadMs       = vadSpeechPadMs,
                HotwordsStoragePath  = hotwordsStoragePath ?? "",
                HotwordsScore        = hotwordsScore,
            };

            var (voice, error) = await client.CreateVoiceInputAsync(cfg);
            _creating = false;

            if (!error.IsOk)
            {
                TryllLog.Error($"TryllVoiceInputComponent: CreateVoiceInput failed: {error}");
                OnError.Invoke(error);
                return;
            }

            Voice = voice;
            Voice.OnTranscriptUpdate = HandleTranscriptUpdate;
            Voice.OnError            = HandleError;
            OnVoiceInputCreated.Invoke();
        }

        /// <summary>Destroy the handle and release the server-side session.</summary>
        public void DestroyVoiceInput()
        {
            StopCapture();  // capture belongs to the handle's utterance
            if (Voice == null) return;
            Voice.OnTranscriptUpdate = null;
            Voice.OnError            = null;
            Voice.Dispose();
            Voice = null;
        }

        /// <summary>
        /// Open a new utterance and start microphone capture. A non-empty final
        /// transcript is sent through <paramref name="agentOverride"/> when given,
        /// otherwise through <see cref="targetAgent"/>, unless that agent already
        /// has a turn in flight (then the transcript is dropped). Transcribe-only
        /// when both are null.
        /// </summary>
        public void BeginUtterance(TryllAgentComponent agentOverride = null)
        {
            if (IsUtteranceActive)
            {
                // Refuse before touching capture: the handle's own UtteranceInProgress
                // would reach the error path below, which stops the live recording.
                OnError.Invoke(new TryllError(TryllErrorCode.UtteranceInProgress, "Utterance already in progress"));
                return;
            }

            if (!CheckMicrophone())
            {
                OnError.Invoke(MicrophoneError());
                return;
            }

            if (!HasVoiceInput && _createSkippedNoMic)
            {
                // A microphone appeared since creation was skipped.
                CreateVoiceInput();
                OnError.Invoke(new TryllError(TryllErrorCode.VoiceInputStarting,
                    "Microphone found; voice input is starting. Try again shortly."));
                return;
            }

            if (!HasVoiceInput)
            {
                TryllLog.Warning("TryllVoiceInputComponent.BeginUtterance: no VoiceInput handle ready");
                OnError.Invoke(new TryllError(TryllErrorCode.VoiceInputNotFound, "VoiceInput not ready"));
                return;
            }

            _utteranceOverride = agentOverride;

            var opts = new UtteranceOptions
            {
                AutoFinishOnSilence = autoFinishOnSilence,
                MaxUtteranceMs      = maxUtteranceMs,
            };

            // Begin sets IsUtteranceActive eagerly and the command queue is ordered,
            // so capture can start immediately. Starting only from the Begin ack
            // left a window where End stopped the utterance first, then the late
            // success callback opened an orphan microphone with no accepted PCM.
            Voice.BeginUtterance(opts, error =>
            {
                if (error.IsOk) return;
                StopCapture();
                OnError.Invoke(error);
            });
            if (!StartCapture())
            {
                CancelUtterance();
                OnError.Invoke(MicrophoneError());
            }
        }

        /// <summary>Commit the current utterance (push-to-talk release).</summary>
        public void EndUtterance()
        {
            StopCapture();
            if (HasVoiceInput)
                Voice.EndUtterance(error => { if (!error.IsOk) OnError.Invoke(error); });
        }

        /// <summary>Cancel the current utterance without producing a transcript.</summary>
        public void CancelUtterance()
        {
            StopCapture();
            if (HasVoiceInput)
                Voice.CancelUtterance(error => { if (!error.IsOk) OnError.Invoke(error); });
        }

        // ── Microphone capture ─────────────────────────────────────────────────

        private int ResolveNativeRate()
        {
            Microphone.GetDeviceCaps(Device, out int minFreq, out int maxFreq);
            // maxFreq == 0 means "any rate supported".
            if (maxFreq == 0) return 48000;
            return Mathf.Clamp(48000, minFreq <= 0 ? 8000 : minFreq, maxFreq);
        }

        /// <summary>Opens the microphone. False only when the device could not be opened.</summary>
        private bool StartCapture()
        {
            if (_micClip != null) return true;  // already capturing
            if (Voice == null || !Voice.IsUtteranceActive) return true;

            _micClip = Microphone.Start(Device, true, Mathf.Max(1, ringBufferSeconds), _nativeRate);
            if (_micClip == null)
            {
                string device = Device == null ? "The default microphone" : $"Microphone '{Device}'";
                SetMicrophoneStatus(TryllMicrophoneStatus.CaptureFailed,
                    $"{device} could not be opened. It may be in use by another app or blocked " +
                    "by Windows microphone privacy settings. Voice input is unavailable until it opens.");
                return false;
            }
            if (MicrophoneStatus != TryllMicrophoneStatus.Available)
                SetMicrophoneStatus(TryllMicrophoneStatus.Available, null);

            _readPos = 0;
            _pumpCoroutine = StartCoroutine(PumpLoop());
            return true;
        }

        private void StopCapture()
        {
            if (_pumpCoroutine != null)
            {
                StopCoroutine(_pumpCoroutine);
                _pumpCoroutine = null;
            }
            if (_micClip != null)
            {
                Microphone.End(Device);
                _micClip = null;
            }
            _readPos = 0;
        }

        private IEnumerator PumpLoop()
        {
            var wait = new WaitForSeconds(Mathf.Max(0.01f, pumpIntervalMs / 1000f));

            // Warm-up gate: wait until the mic actually produces samples.
            while (_micClip != null && Microphone.GetPosition(Device) <= 0)
                yield return null;

            if (_micClip == null) yield break;
            _readPos = Microphone.GetPosition(Device);

            int clipSamples = _micClip.samples;

            while (_micClip != null)
            {
                yield return wait;
                if (_micClip == null) break;

                int pos    = Microphone.GetPosition(Device);
                int avail  = pos - _readPos;
                if (avail < 0) avail += clipSamples;   // ring-buffer wrap
                if (avail <= 0) continue;
                if (avail > clipSamples) avail = clipSamples;  // writer lapped us — clamp

                var floats = new float[avail];
                _micClip.GetData(floats, _readPos);   // GetData wraps the ring buffer
                _readPos = (_readPos + avail) % clipSamples;

                var pcm = new short[avail];
                for (int i = 0; i < avail; i++)
                {
                    float s = Mathf.Clamp(floats[i], -1f, 1f);
                    pcm[i] = (short)Mathf.RoundToInt(s * 32767f);
                }

                Voice?.SendAudioBuffer(pcm);
            }
        }

        // ── Microphone check ───────────────────────────────────────────────────

        /// <summary>
        /// Re-reads the device list and updates <see cref="MicrophoneStatus"/>. True when
        /// capture can be attempted. A <see cref="TryllMicrophoneStatus.CaptureFailed"/>
        /// status is kept while the device is still listed: only a successful
        /// <c>Microphone.Start</c> clears it.
        /// </summary>
        internal bool CheckMicrophone()
        {
            string[] devices = MicrophoneDevices() ?? Array.Empty<string>();
            if (devices.Length == 0)
            {
                SetMicrophoneStatus(TryllMicrophoneStatus.NoDevice,
                    "No microphone found. Voice input is disabled; text chat is unaffected.");
                return false;
            }
            if (Device != null && Array.IndexOf(devices, Device) < 0)
            {
                SetMicrophoneStatus(TryllMicrophoneStatus.DeviceNotFound,
                    $"Microphone '{Device}' is not connected. Voice input is disabled; text chat is " +
                    $"unaffected. Available: \"{string.Join("\", \"", devices)}\". " +
                    "Leave Mic Device Name empty to use the system default.");
                return false;
            }
            if (MicrophoneStatus != TryllMicrophoneStatus.CaptureFailed)
                SetMicrophoneStatus(TryllMicrophoneStatus.Available, null);
            return true;
        }

        /// <summary>
        /// Sets the status; <paramref name="problem"/> is null when the microphone is usable.
        /// Logs a warning once per distinct problem, and once when it clears.
        /// </summary>
        private void SetMicrophoneStatus(TryllMicrophoneStatus status, string problem)
        {
            MicrophoneStatus        = status;
            MicrophoneStatusMessage = problem ?? "";

            if (problem != null)
            {
                if (problem == _loggedMicProblem) return;
                _loggedMicProblem = problem;
                TryllLog.Warning($"TryllVoiceInputComponent on '{OwnerPath()}': {problem}");
            }
            else if (_loggedMicProblem != null)
            {
                _loggedMicProblem = null;
                TryllLog.Info($"TryllVoiceInputComponent on '{OwnerPath()}': microphone available again.");
            }
        }

        private TryllError MicrophoneError()
            => new TryllError(TryllErrorCode.MicrophoneUnavailable, MicrophoneStatusMessage);

        private string OwnerPath()
        {
            string path = name;
            for (var t = transform.parent; t != null; t = t.parent)
                path = t.name + "/" + path;
            return path;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void OnCreateSessionComplete(TryllError error)
        {
            // Session is now live — safe to create the VoiceInput.  Honor a queued
            // manual CreateVoiceInput() first; otherwise auto-create when enabled.
            if (!error.IsOk || HasVoiceInput) return;
            if (_createQueued || createOnEnable)
                CreateVoiceInput(reportMicrophoneError: _createQueued);
        }

        private void OnConnectionChanged(bool isConnected)
        {
            // Do NOT create on raw connect: the session does not exist yet (protocol
            // v4).  Creation is driven by OnCreateSessionComplete.
            if (!isConnected)
            {
                // Server gone — drop any deferred-create intent and local state
                // without sending DestroyVoiceInput (the connection is gone).
                _createQueued = false;
                if (Voice != null)
                {
                    StopCapture();
                    Voice.OnTranscriptUpdate = null;
                    Voice.OnError            = null;
                    Voice = null;
                }
            }
        }

        private TryllAgentComponent _utteranceOverride;

        private void HandleTranscriptUpdate(TranscriptUpdate update)
        {
            OnTranscriptUpdate.Invoke(update);
            // Every UtteranceFinal closes the turn; stop capture so the next
            // turn starts from a fresh BeginUtterance. A non-empty final is sent
            // through the agent component unless that agent already has a turn
            // in flight.
            if (update.Kind != TranscriptUpdateKind.UtteranceFinal)
                return;

            StopCapture();
            var target = _utteranceOverride != null ? _utteranceOverride : targetAgent;
            _utteranceOverride = null;
            if (string.IsNullOrEmpty(update.Text) || target == null)
                return;
            if (target.IsTurnInFlight)
            {
                TryllLog.Warning("TryllVoiceInputComponent: dropping transcript; agent turn already in flight");
                return;
            }
            target.SendMessage(update.Text);
        }

        private void HandleError(TryllError error) => OnError.Invoke(error);
    }
}
