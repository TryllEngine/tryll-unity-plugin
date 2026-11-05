// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using Tryll;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Applies a server-side Oculus 15-viseme track to ARKit blendshapes.
    ///
    /// Buffers <see cref="TryllAgent.OnTtsVisemes"/> events and, each
    /// LateUpdate, looks up the active viseme at the audible sample position
    /// and writes the serialized viseme→ARKit pose through a ~50 ms
    /// attack/decay envelope. Does not rely on mesh-native viseme morphs.
    ///
    /// Clock: do not use <see cref="TryllSpeaker.ConsumedSamples"/> raw — it is
    /// a sawtooth that leads audible position. This component recovers the
    /// audible position with <see cref="TryllTtsPlaybackClock"/>, then subtracts
    /// Unity's DSP-ring latency and the measured streaming-clip backlog.
    ///
    /// Blendshape scale is read per shape via <c>GetBlendShapeFrameWeight</c>
    /// (glTFast = 1, FBX importer = 100). Never hardcoded.
    ///
    /// Setup: add next to <see cref="TryllSpeaker"/> (required — it is the audio and
    /// the clock) and a <see cref="TryllAgentComponent"/> (the viseme source), and
    /// enable <c>emit_visemes</c> on the agent's voice profile
    /// (<see cref="TryllVoiceProfileData.EmitVisemes"/>). Kept separate from the
    /// speaker on purpose: many characters speak without a face, and the mapping
    /// settings here are per-rig.
    /// </summary>
    [AddComponentMenu("Tryll/Tryll Lip Sync")]
    [RequireComponent(typeof(TryllSpeaker))]
    public sealed class TryllLipSync : MonoBehaviour
    {
        public const int VisemeCount = 15;

        [Header("Source")]
        [SerializeField] private TryllAgentComponent _agent;
        [SerializeField] private TryllSpeaker _speaker;

        [Header("Face")]
        [SerializeField] private List<SkinnedMeshRenderer> _renderers = new List<SkinnedMeshRenderer>();
        [SerializeField] private Transform _faceRoot;

        [Header("Clock")]
        [SerializeField] private bool _smoothPlaybackClock = true;
        [SerializeField] private float _clockWindowSeconds = 1.5f;
        [Tooltip("How far TryllSpeaker's consumed-samples counter runs ahead of what is audible, " +
                 "beyond the DSP ring: it counts samples queued into a streaming clip that Unity " +
                 "refills before draining. Measured at 342 ms by the demo's automated sync test; " +
                 "re-measure (Tools/run-sync-test.ps1) if the audio device or buffer settings change.")]
        [SerializeField] private float _outputLeadTrimMs = 342f;
        [SerializeField] private bool _compensateOutputLatency = true;
        [SerializeField] private float _syncOffsetMs = 0f;

        [Header("Envelope")]
        [Tooltip("Attack/decay across viseme boundaries, in milliseconds.")]
        [SerializeField] private float _attackDecayMs = 50f;

        [Tooltip("Global scale on every viseme pose. The authored table is deliberately " +
                 "full-strength (1.0); real rigs usually want less. Live-editable in Play mode.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _intensity = 0.4f;

        [Tooltip("Exponential slew toward the target pose, in units per second. Lower = " +
                 "softer and more damped, higher = snappier. 0 disables smoothing (hard pops).")]
        [Range(0f, 120f)]
        [SerializeField] private float _smoothingPerSecond = 16f;

        [Tooltip("Slew rate used while a lip closure (p/b/m, f/v) is active. Closures are " +
                 "brief, so the regular rate never lets the lips actually shut.")]
        [Range(0f, 200f)]
        [SerializeField] private float _closureSmoothingPerSecond = 60f;

        [Tooltip("How long the final viseme of a turn may be held before the face returns " +
                 "to rest. A backstop for cancelled turns and dropped final frames; the " +
                 "server normally ends every turn with a rest event. 0 disables.")]
        [SerializeField] private float _maxHoldMs = 400f;

        [Tooltip("Sample the viseme timeline ahead by the delay the smoothing and attack blend " +
                 "introduce (about 1/smoothing s plus half the attack), so changing the dampening " +
                 "does not shift sync. Leave on unless measuring the raw lag.")]
        [SerializeField] private bool _compensateSmoothingLag = true;

        [Header("Mapping")]
        [Tooltip("Oculus viseme → ARKit channel weights. Empty at runtime fills the " +
                 "hand-authored Oculus/VRChat default. Inspector-editable.")]
        [SerializeField] private List<TryllVisemePose> _map = new List<TryllVisemePose>();

        [Header("Diagnostics")]
        [Tooltip("Log, per viseme frame, how far ahead of playback it arrived. Negative means the " +
                 "audio it describes was already playing, so the face missed that stretch.")]
        [SerializeField] private bool _logFrameArrival;

        [Header("Apply")]
        [Tooltip("When false the pose is still evaluated but not written to the mesh.")]
        [SerializeField] private bool _applyToMesh = true;

        private struct ShapeTarget
        {
            public SkinnedMeshRenderer Renderer;
            public int Index;
            public float FullWeight;
        }

        private readonly Dictionary<string, List<ShapeTarget>> _targets =
            new Dictionary<string, List<ShapeTarget>>(StringComparer.Ordinal);
        private readonly List<TryllVisemeEvent> _events = new List<TryllVisemeEvent>(64);
        private readonly TryllTtsPlaybackClock _clock = new TryllTtsPlaybackClock();
        private readonly Dictionary<string, float> _channelValues =
            new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _prevChannelValues =
            new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _curChannelValues =
            new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _smoothedValues =
            new Dictionary<string, float>(StringComparer.Ordinal);

        private Dictionary<string, float>[] _posesByViseme;

        private TryllAgent _boundAgent;
        private int _mappedShapes;
        private int _ttsSampleRate;
        private long _turnOrigin;
        private double _outputLatencySeconds;
        private WireViseme _activeViseme = WireViseme.sil;
        private float _appliedJawOpen;
        private long _lastTurnPosition;

        public bool ApplyToMesh { get => _applyToMesh; set => _applyToMesh = value; }
        /// <summary>Global scale on viseme poses (0 = no movement, 1 = the authored table).</summary>
        public float Intensity { get => _intensity; set => _intensity = value; }
        /// <summary>Exponential slew rate toward the target pose; 0 disables smoothing.</summary>
        public float SmoothingPerSecond { get => _smoothingPerSecond; set => _smoothingPerSecond = value; }
        public float AttackDecayMs { get => _attackDecayMs; set => _attackDecayMs = value; }

        /// <summary>
        /// How far ahead the timeline is sampled to cancel the smoothing delay, in ms.
        /// Exponential smoothing at rate k delays a slowly varying target by ~1/k s; the
        /// smoothstep attack delays each transition by ~half its length.
        /// </summary>
        public float SmoothingLagMs => !_compensateSmoothingLag ? 0f
            : (_smoothingPerSecond > 0f ? 1000f / _smoothingPerSecond : 0f) + 0.5f * _attackDecayMs;
        public bool FaceBound => _mappedShapes > 0;
        /// <summary>Unity DSP-ring latency subtracted from the playback clock, in ms.</summary>
        public double OutputLatencyMs => _outputLatencySeconds * 1000.0;
        public int MappedShapes => _mappedShapes;
        public WireViseme ActiveViseme => _activeViseme;
        public float AppliedJawOpen => _appliedJawOpen;
        public string StatusLine { get; private set; } = "starting…";

        public void BindFace(Transform faceRoot)
        {
            _faceRoot = faceRoot;
            _renderers.Clear();
            BindRenderers();
        }

        private void Reset()
        {
            _agent = GetComponent<TryllAgentComponent>();
            _speaker = GetComponent<TryllSpeaker>();
        }

        private void Awake()
        {
            if (_agent == null) _agent = GetComponent<TryllAgentComponent>();
            if (_speaker == null) _speaker = GetComponent<TryllSpeaker>();
            if (_agent == null)
                Debug.LogWarning("TryllLipSync: no TryllAgentComponent assigned or on this " +
                                 "GameObject — no viseme track will arrive.", this);
            EnsureDefaultMap();
            BuildPoseTable();
        }

        private void Start()
        {
            EstimateOutputLatency();
            if (_renderers.Count > 0 || (_faceRoot != null && _faceRoot.childCount > 0))
                BindRenderers();
            StatusLine = "ready";
        }

        private void OnEnable()
        {
            if (_agent != null)
            {
                _agent.OnAgentCreated.AddListener(BindAgent);
                _agent.OnAgentDestroyed.AddListener(UnbindAgent);
            }
            BindAgent();
        }

        private void OnDisable()
        {
            if (_agent != null)
            {
                _agent.OnAgentCreated.RemoveListener(BindAgent);
                _agent.OnAgentDestroyed.RemoveListener(UnbindAgent);
            }
            UnbindAgent();
            _clock.Reset();
            _smoothedValues.Clear();
        }

        private void BindAgent()
        {
            var agent = _agent != null ? _agent.Agent : null;
            if (ReferenceEquals(agent, _boundAgent)) return;
            UnbindAgent();
            _boundAgent = agent;
            if (_boundAgent == null) return;
            var problem = VisemeSetupProblem(_agent);
            if (problem != null)
                Debug.LogWarning("TryllLipSync: " + problem, this);
            _boundAgent.OnTtsVisemes += HandleVisemes;
            if (_speaker != null) _speaker.OnTtsFormat += HandleFormat;
        }

        /// <summary>
        /// Why <paramref name="agent"/> will send no viseme track, or null when its voice
        /// profile has <see cref="TryllVoiceProfileData.EmitVisemes"/> on. Shown by the
        /// inspector and logged when the agent is bound.
        /// </summary>
        public static string VisemeSetupProblem(TryllAgentComponent agent)
        {
            if (agent == null) return null;  // reported separately (no agent at all)
            var voice = agent.Voice;
            if (voice == null || voice.Data == null)
                return $"agent '{agent.name}' has no Voice profile, so no viseme track will arrive. " +
                       "Assign a TryllVoiceProfile with Emit Visemes on.";
            if (!voice.Data.EmitVisemes)
                return $"voice profile '{voice.name}' has Emit Visemes off, so no viseme track will " +
                       "arrive. Enable Emit Visemes on the agent's voice profile.";
            return null;
        }

        private void UnbindAgent()
        {
            if (_boundAgent != null)
                _boundAgent.OnTtsVisemes -= HandleVisemes;
            _boundAgent = null;
            if (_speaker != null) _speaker.OnTtsFormat -= HandleFormat;
        }

        private void HandleFormat(string nodeName, int sampleRate, int channels, int bitsPerSample)
        {
            _ = nodeName;
            _ = channels;
            _ = bitsPerSample;
            _ttsSampleRate = sampleRate;
            _events.Clear();
            if (_speaker != null)
                _turnOrigin = _speaker.ConsumedSamples + _speaker.QueuedSamples;
        }

        private void HandleVisemes(string nodeName, TryllVisemeEvent[] events)
        {
            _ = nodeName;
            if (events == null || events.Length == 0) return;
            for (int i = 0; i < events.Length; i++)
                _events.Add(events[i]);

            if (_logFrameArrival && _ttsSampleRate > 0)
            {
                double ms = 1000.0 / _ttsSampleRate;
                long lead = (long)events[0].SampleOffset - _lastTurnPosition;
                Debug.Log($"TryllLipSync: viseme frame ({events.Length} events) starting at " +
                          $"{events[0].SampleOffset * ms:F0} ms into the turn arrived " +
                          $"{lead * ms:+0;-0} ms " +
                          (lead < 0 ? "LATE (its audio was already playing)" : "ahead of playback"));
            }
        }

        private void LateUpdate()
        {
            _lastTurnPosition = CurrentTurnRelativeSamples();
            EvaluateAt(_lastTurnPosition, _ttsSampleRate);

            if (_applyToMesh)
                ApplyToRenderers();
        }

        private void EvaluateAt(long turnRelativeSamples, int sampleRate)
        {
            int count = EventCount;
            if (count == 0 || sampleRate <= 0)
            {
                _activeViseme = WireViseme.sil;
                _appliedJawOpen = 0f;
                FillRest(_channelValues);
                return;
            }

            if (turnRelativeSamples < 0) turnRelativeSamples = 0;

            int idx = FindActiveIndex(turnRelativeSamples);
            var cur = EventAt(idx);

            // Safety net for the end of a turn. The server appends a rest terminator
            // after the last viseme, but a cancelled turn or a dropped final frame
            // would otherwise leave the clock parked on the last mouth shape forever.
            // Past the hold limit, rest.
            if (idx == count - 1 && sampleRate > 0 && _maxHoldMs > 0f)
            {
                long past = turnRelativeSamples - (long)cur.SampleOffset;
                if (past > (long)(_maxHoldMs * sampleRate / 1000.0))
                {
                    _activeViseme = WireViseme.sil;
                    _channelValues.Clear();
                    _appliedJawOpen = 0f;
                    StatusLine = "rest (past end of turn)";
                    return;
                }
            }

            _activeViseme = cur.Viseme;

            FillPose(cur, _curChannelValues);

            // Blend's destination must not alias either input: it clears dest first,
            // so passing the current pose as both `b` and `dest` wipes it and leaves
            // only the previous pose fading to zero, then snapping. Keep them distinct.
            bool blended = false;
            long attack = AttackSamples(sampleRate);
            if (idx > 0 && attack > 0)
            {
                long into = turnRelativeSamples - (long)cur.SampleOffset;
                if (into < attack)
                {
                    FillPose(EventAt(idx - 1), _prevChannelValues);
                    float u = into / (float)attack;
                    u = u < 0f ? 0f : (u > 1f ? 1f : u);
                    Blend(_prevChannelValues, _curChannelValues, Smoothstep(u), _channelValues);
                    blended = true;
                }
            }

            if (!blended)
            {
                _channelValues.Clear();
                foreach (var kv in _curChannelValues) _channelValues[kv.Key] = kv.Value;
            }

            _channelValues.TryGetValue("jawOpen", out _appliedJawOpen);
            StatusLine = $"viseme {_activeViseme} @ {turnRelativeSamples}  jaw {_appliedJawOpen:F2}";
        }

        private long CurrentTurnRelativeSamples()
        {
            if (_speaker == null || _ttsSampleRate <= 0) return 0;
            _clock.Smooth = _smoothPlaybackClock;
            _clock.WindowSeconds = _clockWindowSeconds;
            _clock.OutputLeadTrimMs = _outputLeadTrimMs;
            _clock.SampleRate = _ttsSampleRate;
            long smoothed = _clock.Evaluate(_speaker.ConsumedSamples, Time.unscaledTimeAsDouble);
            long clock = smoothed - (long)(_outputLatencySeconds * _ttsSampleRate);
            float aheadMs = _syncOffsetMs + SmoothingLagMs;
            if (aheadMs != 0f)
                clock += (long)(aheadMs * _ttsSampleRate / 1000f);
            return clock - _turnOrigin;
        }

        private int EventCount => _events.Count;

        private TryllVisemeEvent EventAt(int i) => _events[i];

        private int FindActiveIndex(long sample)
        {
            int lo = 0, hi = EventCount - 1, ans = 0;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                if ((long)EventAt(mid).SampleOffset <= sample)
                {
                    ans = mid;
                    lo = mid + 1;
                }
                else hi = mid - 1;
            }
            return ans;
        }

        private long AttackSamples(int sampleRate)
            => sampleRate > 0 ? (long)(_attackDecayMs * sampleRate / 1000.0) : 0;

        private static float Smoothstep(float t) => t * t * (3f - 2f * t);

        private void FillPose(TryllVisemeEvent ev, Dictionary<string, float> dest)
        {
            dest.Clear();
            int i = (int)ev.Viseme;
            if (_posesByViseme == null || i < 0 || i >= _posesByViseme.Length) return;
            var src = _posesByViseme[i];
            if (src == null) return;
            // The server scales each shape by how loud that stretch of speech is, so
            // quieter syllables open less. Closures and rests always arrive at 1.
            float w = ev.Weight;
            if (!(w > 0f) || float.IsNaN(w)) w = 1f;
            if (w > 1f) w = 1f;
            foreach (var kv in src) dest[kv.Key] = kv.Value * w;
        }

        /// <summary>Lip contact shapes, which must visibly close even when brief.</summary>
        private static bool IsClosure(WireViseme v) => v == WireViseme.PP || v == WireViseme.FF;

        private static void FillRest(Dictionary<string, float> dest) => dest.Clear();

        private static void Blend(
            Dictionary<string, float> a, Dictionary<string, float> b, float u,
            Dictionary<string, float> dest)
        {
            dest.Clear();
            foreach (var kv in a)
            {
                b.TryGetValue(kv.Key, out float bv);
                dest[kv.Key] = kv.Value + (bv - kv.Value) * u;
            }
            foreach (var kv in b)
            {
                if (dest.ContainsKey(kv.Key)) continue;
                dest[kv.Key] = kv.Value * u;
            }
        }

        private void BindRenderers()
        {
            if (_renderers.Count == 0)
            {
                Transform root = _faceRoot != null ? _faceRoot : transform;
                _renderers.AddRange(root.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            }

            // Bind only the channels the map uses. Everything else on the mesh —
            // blinks, brows, expressions — belongs to whatever else animates this
            // face, and must not be written (let alone zeroed) by lip sync.
            var mapped = new HashSet<string>(StringComparer.Ordinal);
            if (_posesByViseme != null)
            {
                foreach (var pose in _posesByViseme)
                {
                    if (pose == null) continue;
                    foreach (var kv in pose) mapped.Add(kv.Key);
                }
            }

            _targets.Clear();
            _mappedShapes = 0;
            var scalesSeen = new HashSet<float>();
            foreach (var r in _renderers)
            {
                if (r == null || r.sharedMesh == null) continue;
                var mesh = r.sharedMesh;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string name = mesh.GetBlendShapeName(i);
                    if (!mapped.Contains(name)) continue;
                    int frames = mesh.GetBlendShapeFrameCount(i);
                    float full = frames > 0 ? mesh.GetBlendShapeFrameWeight(i, frames - 1) : 1f;
                    if (!(full > 0f) || float.IsNaN(full) || float.IsInfinity(full)) full = 1f;
                    scalesSeen.Add(full);
                    if (!_targets.TryGetValue(name, out var list))
                        _targets[name] = list = new List<ShapeTarget>();
                    list.Add(new ShapeTarget { Renderer = r, Index = i, FullWeight = full });
                    _mappedShapes++;
                }
            }

            if (scalesSeen.Count > 0)
                Debug.Log("TryllLipSync: blendshape full-deflection weight(s) on this mesh: " +
                          string.Join(", ", scalesSeen) +
                          "  (1 = glTF/glTFast convention, 100 = Unity FBX importer convention)");
        }

        private void ApplyToRenderers()
        {
            // Write every bound shape. Only mapped channels are bound, so a shape this
            // viseme does not use falls back to 0 — its own channel, not the rig's
            // blinks or brows, which this component never touches.
            //
            // Two dampers sit between the pose table and the mesh: _intensity scales
            // how far a viseme is allowed to push the face, and _smoothingPerSecond
            // slews toward that target so viseme boundaries outside the attack window
            // ease rather than pop.
            float dt = Time.unscaledDeltaTime;
            // A lip closure lasts ~70 ms; at the regular rate the jaw would still be on
            // its way down when the next shape arrives, so p/b/m never visibly shut.
            // While a closure is active, slew fast enough to land it.
            float rate = IsClosure(_activeViseme)
                ? Mathf.Max(_smoothingPerSecond, _closureSmoothingPerSecond)
                : _smoothingPerSecond;
            float k = (rate > 0f && dt > 0f)
                ? 1f - Mathf.Exp(-rate * dt)
                : 1f;

            foreach (var kv in _targets)
            {
                _channelValues.TryGetValue(kv.Key, out float target);
                if (float.IsNaN(target) || float.IsInfinity(target)) continue;
                target *= _intensity;
                target = target < 0f ? 0f : (target > 1f ? 1f : target);

                _smoothedValues.TryGetValue(kv.Key, out float prev);
                float v = prev + (target - prev) * k;
                if (float.IsNaN(v) || float.IsInfinity(v)) v = target;
                _smoothedValues[kv.Key] = v;

                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var t = list[i];
                    t.Renderer.SetBlendShapeWeight(t.Index, v * t.FullWeight);
                }
            }

            _smoothedValues.TryGetValue("jawOpen", out _appliedJawOpen);
        }

        private void EstimateOutputLatency()
        {
            if (!_compensateOutputLatency)
            {
                _outputLatencySeconds = 0.0;
                return;
            }
            AudioSettings.GetDSPBufferSize(out int bufferLength, out int numBuffers);
            int outRate = AudioSettings.outputSampleRate;
            if (bufferLength <= 0 || numBuffers <= 0 || outRate <= 0)
            {
                _outputLatencySeconds = 0.0;
                return;
            }
            _outputLatencySeconds = (double)bufferLength * numBuffers / outRate;
        }

        private void EnsureDefaultMap()
        {
            if (_map != null && _map.Count > 0) return;
            _map = BuildDefaultOculusArkitMap();
        }

        private void BuildPoseTable()
        {
            _posesByViseme = new Dictionary<string, float>[VisemeCount];
            for (int i = 0; i < VisemeCount; i++)
                _posesByViseme[i] = new Dictionary<string, float>(StringComparer.Ordinal);
            if (_map == null) return;
            for (int i = 0; i < _map.Count; i++)
            {
                var pose = _map[i];
                if (pose == null || pose.Channels == null) continue;
                int idx = (int)pose.Viseme;
                if (idx < 0 || idx >= VisemeCount) continue;
                var dest = _posesByViseme[idx];
                for (int c = 0; c < pose.Channels.Length; c++)
                {
                    var ch = pose.Channels[c];
                    if (ch == null || string.IsNullOrEmpty(ch.Channel)) continue;
                    dest[ch.Channel] = ch.Weight;
                }
            }
        }

        /// <summary>
        /// Hand-authored from published Oculus / VRChat viseme shape references.
        /// Not derived from corpus statistics.
        /// </summary>
        public static List<TryllVisemePose> BuildDefaultOculusArkitMap()
        {
            return new List<TryllVisemePose>
            {
                Pose(WireViseme.sil),
                Pose(WireViseme.PP,
                    ("mouthClose", 0.55f), ("mouthPressLeft", 0.45f), ("mouthPressRight", 0.45f),
                    ("mouthPucker", 0.15f)),
                Pose(WireViseme.FF,
                    ("jawOpen", 0.12f), ("mouthLowerDownLeft", 0.55f), ("mouthLowerDownRight", 0.55f),
                    ("mouthFunnel", 0.15f)),
                Pose(WireViseme.TH,
                    ("jawOpen", 0.18f), ("mouthStretchLeft", 0.25f), ("mouthStretchRight", 0.25f),
                    ("mouthUpperUpLeft", 0.20f), ("mouthUpperUpRight", 0.20f)),
                Pose(WireViseme.DD, ("jawOpen", 0.22f), ("mouthClose", 0.15f)),
                Pose(WireViseme.kk, ("jawOpen", 0.32f)),
                Pose(WireViseme.CH,
                    ("jawOpen", 0.18f), ("mouthFunnel", 0.45f), ("mouthPucker", 0.20f)),
                Pose(WireViseme.SS,
                    ("jawOpen", 0.08f), ("mouthStretchLeft", 0.55f), ("mouthStretchRight", 0.55f)),
                Pose(WireViseme.nn, ("jawOpen", 0.14f), ("mouthClose", 0.20f)),
                Pose(WireViseme.RR,
                    ("jawOpen", 0.16f), ("mouthPucker", 0.35f), ("mouthFunnel", 0.15f)),
                Pose(WireViseme.aa, ("jawOpen", 0.72f)),
                Pose(WireViseme.E,
                    ("jawOpen", 0.38f), ("mouthStretchLeft", 0.45f), ("mouthStretchRight", 0.45f)),
                Pose(WireViseme.ih,
                    ("jawOpen", 0.28f), ("mouthStretchLeft", 0.28f), ("mouthStretchRight", 0.28f)),
                Pose(WireViseme.oh,
                    ("jawOpen", 0.48f), ("mouthFunnel", 0.55f), ("mouthPucker", 0.25f)),
                Pose(WireViseme.ou,
                    ("jawOpen", 0.28f), ("mouthPucker", 0.72f), ("mouthFunnel", 0.35f)),
            };
        }

        private static TryllVisemePose Pose(WireViseme viseme, params (string ch, float w)[] channels)
        {
            var arr = new TryllVisemeChannelWeight[channels.Length];
            for (int i = 0; i < channels.Length; i++)
                arr[i] = new TryllVisemeChannelWeight { Channel = channels[i].ch, Weight = channels[i].w };
            return new TryllVisemePose { Viseme = viseme, Channels = arr };
        }
    }

    [Serializable]
    public sealed class TryllVisemePose
    {
        public WireViseme Viseme;
        public TryllVisemeChannelWeight[] Channels;
    }

    [Serializable]
    public sealed class TryllVisemeChannelWeight
    {
        public string Channel;
        public float Weight;
    }
}
