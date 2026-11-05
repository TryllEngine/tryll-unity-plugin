// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Per-model build selection for this project. Ordinals match the Model Manager
    /// window's internal registration enum.
    /// </summary>
    public enum TryllModelRegistration : byte
    {
        /// <summary>Not part of this project's build (the default for unlisted models).</summary>
        None         = 0,
        /// <summary>Usable in the editor, but stripped from the packaged build.</summary>
        Experimental = 1,
        /// <summary>Shipped in the packaged build.</summary>
        Production   = 2,
    }

    /// <summary>
    /// Where a model's weights should live. Mirrors the wire <c>MemoryPlacement</c>.
    ///
    /// Always a <b>hint</b>: a machine that cannot honour it loads the model the
    /// other way and reports what it did. That matters here because this value is
    /// authored on a developer's machine and shipped to players' machines — a VRAM
    /// choice that suits a 24 GB card is not a promise an 8 GB card can keep.
    ///
    /// Which GPU API provides VRAM (CUDA / Vulkan) is deliberately not expressible:
    /// that is the host's decision, not the project's.
    /// </summary>
    /// <summary>
    /// Whether this project wants hotword biasing on an STT model. Mirrors the
    /// wire <c>HotwordBias</c>.
    ///
    /// Unlike <see cref="TryllMemoryPlacement"/> this is not a hint the server can
    /// quietly downgrade. Biasing needs beam-search decoding, which a recognizer
    /// fixes when it is built, so the answer decides *which* recognizer a session
    /// gets — and two sessions that disagree about one model hold two of them.
    /// </summary>
    public enum TryllHotwordBias : byte
    {
        /// <summary>Follow the server catalog: biased when that model's
        /// <c>stt_hotwords_score</c> is non-zero. What an unopinionated project
        /// means, and the value a manifest written before this field existed
        /// carries.</summary>
        Default = 0,
        /// <summary>Never bias. Supplying a hotwords lexicon on this model then
        /// fails with <c>HotwordBiasDisabled</c> (4106) instead of quietly
        /// biasing nothing.</summary>
        Off = 1,
        /// <summary>Always bias, lexicon or not. Beam-search decoding is then in
        /// force for every session on this model, costing a few percent of decode
        /// time and changing its transcripts — "on but unused" is not free.</summary>
        On = 2,
    }

    public enum TryllMemoryPlacement : byte
    {
        /// <summary>Let the server decide: device memory when the machine has a
        /// usable GPU, system memory otherwise.</summary>
        Auto = 0,
        /// <summary>System memory (RAM).</summary>
        Ram  = 1,
        /// <summary>Device memory (VRAM).</summary>
        Vram = 2,
    }

    /// <summary>
    /// Project-wide manifest of which models ship in the build. Stores a thin
    /// name -> { type, registration, languages } list; the rest of the catalog stays
    /// server-owned and is read via ListModels. One asset per project: the Model
    /// Manager window writes it and a future packaging step reads it to decide which
    /// model files to stage. Models not listed here are simply unregistered.
    /// </summary>
    public sealed class TryllModelManifest : ScriptableObject
    {
        /// <summary>
        /// Name the runtime loads by, via <c>Resources.Load</c>. The asset must sit
        /// directly in a <c>Resources</c> folder under this name — a nested path such as
        /// <c>Resources/Models/TryllModelManifest.asset</c> is *not* reachable as
        /// "TryllModelManifest".
        /// </summary>
        public const string RuntimeResourceName = "TryllModelManifest";

        /// <summary>
        /// Where the Model Manager creates the asset. In <c>Resources</c> because this is a
        /// runtime asset: a built player has no AssetDatabase, so an authored RAM/VRAM
        /// placement that is not reachable here is silently absent from the shipped game.
        /// </summary>
        public const string EditorAssetPath = "Assets/Resources/" + RuntimeResourceName + ".asset";

        [Serializable]
        public struct Entry
        {
            public string                 Name;
            /// <summary>"language" | "embedding" | "stt" | "tts" | "vad" — for type-filtered pickers.</summary>
            public string                 ModelType;
            public TryllModelRegistration Registration;

            /// <summary>
            /// Engine that runs this model, cached from the catalog when the model
            /// was registered. Cached rather than read live because the session's
            /// engine set is built from this manifest at CreateSession, before any
            /// ListModels round trip could answer — an editor that could only
            /// build a session while connected would be worse than none.
            /// </summary>
            public TryllInferenceEngine   Engine;

            /// <summary>Where this model should live. See <see cref="TryllMemoryPlacement"/>.</summary>
            public TryllMemoryPlacement   Placement;

            /// <summary>
            /// Whether this project wants hotword biasing on this STT model. See
            /// <see cref="TryllHotwordBias"/>. <c>Default</c> — the zero value, and
            /// therefore what a manifest authored before this field existed carries —
            /// defers to the server catalog, which is the pre-existing behaviour.
            /// </summary>
            public TryllHotwordBias       Hotwords;

            /// <summary>
            /// BCP-47 primary language subtags this model declares, cached from the catalog.
            ///
            /// Languages are the one piece of catalog metadata cached here rather than
    /// read live. Everything else can wait for a ListModels round trip, but the
    /// settings language dropdown and the model-picker warnings have to work with
    /// no server running — an editor that can only validate languages while
    /// connected would be worse than no validation at all. Written by the Model
    /// Manager from the live ModelInfo at the moment the model is registered.
            ///
            /// Empty means undeclared, not "supports nothing" — every consumer
            /// treats it as "no capability information".
            /// </summary>
            public string[]               Languages;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        /// <summary>All registered entries, read-only. The packaging step filters these by registration.</summary>
        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>Registration for a model, or <see cref="TryllModelRegistration.None"/> if unlisted.</summary>
        public TryllModelRegistration Get(string modelName)
        {
            foreach (var e in _entries)
                if (e.Name == modelName) return e.Registration;
            return TryllModelRegistration.None;
        }

        /// <summary>
        /// Set (or clear) a model's registration. Setting <see cref="TryllModelRegistration.None"/>
        /// removes the entry so the manifest only ever holds the models the project actually selects.
        /// </summary>
        /// <returns>
        /// True when this call actually changed the manifest, so a caller knows whether to
        /// mark the asset dirty and save.
        ///
        /// Reported from here rather than compared by the caller, and that is the point.
        /// Callers used to decide for themselves — the Model Manager's catalog refresh
        /// skipped the whole call when cached <em>languages</em> matched the server, which
        /// meant a stale cached <em>engine</em> was never corrected. Every manifest written
        /// before the engine field existed therefore kept <c>Mock</c> (the enum default) and
        /// silently pre-warmed nothing. Only this method knows what it caches, so only this
        /// method can say whether a write mattered; adding a field later cannot reintroduce
        /// the same bug.
        /// </returns>
        public bool Set(string modelName, string modelType, TryllModelRegistration registration,
                        string[] languages = null,
                        TryllInferenceEngine? engine = null,
                        TryllMemoryPlacement? placement = null,
                        TryllHotwordBias? hotwords = null)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Name != modelName) continue;
                if (registration == TryllModelRegistration.None)
                {
                    _entries.RemoveAt(i);
                    return true;
                }

                var e = _entries[i];
                bool changed = e.ModelType != modelType || e.Registration != registration;
                e.ModelType    = modelType;
                e.Registration = registration;
                // Null means "caller had no catalog data to hand" (an offline
                // re-registration): keep what we already cached rather than
                // erasing it and silently disabling the language warnings.
                if (languages != null)
                {
                    if (!LanguagesEqual(e.Languages, languages)) changed = true;
                    e.Languages = languages;
                }
                // Same rule as languages: null means "no new information",
                // so an offline re-registration keeps the cached engine
                // rather than silently resetting it to Mock (the zero value).
                if (engine.HasValue)
                {
                    if (e.Engine != engine.Value) changed = true;
                    e.Engine = engine.Value;
                }
                if (placement.HasValue)
                {
                    if (e.Placement != placement.Value) changed = true;
                    e.Placement = placement.Value;
                }
                if (hotwords.HasValue)
                {
                    if (e.Hotwords != hotwords.Value) changed = true;
                    e.Hotwords = hotwords.Value;
                }
                _entries[i] = e;
                return changed;
            }

            if (registration == TryllModelRegistration.None) return false;

            _entries.Add(new Entry
            {
                Name = modelName, ModelType = modelType,
                Registration = registration, Languages = languages ?? new string[0],
                Engine = engine ?? TryllInferenceEngine.LlamaCpp,
                Placement = placement ?? TryllMemoryPlacement.Auto,
                Hotwords = hotwords ?? TryllHotwordBias.Default,
            });
            return true;
        }

        /// <summary>Order-sensitive comparison; the catalog's order is stable.</summary>
        private static bool LanguagesEqual(string[] a, string[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return (a?.Length ?? 0) == (b?.Length ?? 0);
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        /// <summary>
        /// Cached languages for a registered model, or an empty array when the model
        /// is unlisted or declared none. Never null.
        /// </summary>
        public string[] LanguagesOf(string modelName)
        {
            foreach (var e in _entries)
                if (e.Name == modelName)
                    return e.Languages ?? new string[0];
            return new string[0];
        }

        /// <summary>
        /// Every language any registered model declares, sorted — the source for the
        /// settings language dropdown.
        ///
        /// A **union**, deliberately, not an intersection: a project may register an
        /// English-only model for one character and a 31-language voice for everyone
        /// else, and intersecting would collapse the menu to English. The per-model
        /// mismatches are surfaced where they are actionable instead — on the model
        /// picker (inspector warning) and at agent creation (server error).
        ///
        /// <paramref name="modelTypes"/> filters by catalog type; pass null for all.
        /// Language models declare nothing today, so in practice this reports what
        /// the voice stack can do.
        /// </summary>
        public List<string> DeclaredLanguages(params string[] modelTypes)
        {
            var seen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var e in _entries)
            {
                if (modelTypes != null && modelTypes.Length > 0 &&
                    Array.IndexOf(modelTypes, e.ModelType) < 0) continue;
                if (e.Languages == null) continue;
                foreach (var code in e.Languages)
                    if (!string.IsNullOrEmpty(code)) seen.Add(code);
            }
            return new List<string>(seen);
        }

        /// <summary>
        /// Registered models of the given types that do **not** declare
        /// <paramref name="language"/>, ignoring those that declared nothing.
        /// Drives the "3 of 5 voice models cannot do German" summary in settings.
        /// </summary>
        public List<string> ModelsMissingLanguage(string language, params string[] modelTypes)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(language)) return result;

            foreach (var e in _entries)
            {
                if (modelTypes != null && modelTypes.Length > 0 &&
                    Array.IndexOf(modelTypes, e.ModelType) < 0) continue;
                if (e.Languages == null || e.Languages.Length == 0) continue;  // undeclared = unknown

                bool found = false;
                foreach (var code in e.Languages)
                    if (string.Equals(code, language, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
                if (!found) result.Add(e.Name);
            }
            return result;
        }

        /// <summary>Names of registered models of a given type, for type-filtered pickers.</summary>
        public List<string> ModelsOfType(string modelType)
        {
            var result = new List<string>();
            foreach (var e in _entries)
                if (e.ModelType == modelType) result.Add(e.Name);
            return result;
        }

        /// <summary>
        /// The manifest a session builds its engine and placement hints from, or null
        /// when the project has none.
        ///
        /// In the editor this resolves the project asset directly. At runtime it can only
        /// use <c>Resources</c>, since a built player has no AssetDatabase — a manifest
        /// kept outside a <c>Resources</c> folder is invisible there, which is why the
        /// Model Manager creates it at <see cref="EditorAssetPath"/> and
        /// <c>TryllWorkflowModelPreBuildCheck</c> fails a build that would ship an
        /// unreachable one.
        ///
        /// Returning null is not a failure: the engine hint is advisory, so the session is
        /// created and every model still loads on the engine its catalog entry names. What
        /// is lost is the pre-warm and any authored RAM/VRAM placement — a silent
        /// performance difference between the editor and the shipped game, which is
        /// precisely why it is an error rather than a warning.
        /// </summary>
        public static TryllModelManifest LoadOrNull()
        {
#if UNITY_EDITOR
            var asset = FindProjectManifest();
            if (asset != null) return asset;
#endif
            var fromResources = Resources.Load<TryllModelManifest>(RuntimeResourceName);
            if (fromResources == null)
            {
                // A missing manifest no longer breaks the session — the engine hint
                // is advisory, so the session is created and every model still loads
                // on the engine its catalog entry names. What is lost is the
                // pre-warm, so the first load pays the backend init it could have
                // paid during a loading screen. Worth an error rather than a warning
                // because the manifest is also where placement is authored, and
                // silently losing that is a performance mystery later.
                Debug.LogError(
                    "Tryll: no TryllModelManifest found at runtime. A built player " +
                    "cannot use the editor's asset database, so the manifest must be " +
                    "in a Resources folder and named 'TryllModelManifest' " +
                    "(Assets/Resources/TryllModelManifest.asset). Without it this " +
                    "session sends no engine hint and no placement hints: models " +
                    "still load, but engine init is paid on first use and any " +
                    "authored RAM/VRAM placement is ignored.");
            }
            return fromResources;
        }

#if UNITY_EDITOR
        /// <summary>
        /// All <see cref="TryllModelManifest"/> assets in the project, sorted by asset path
        /// (deterministic). Empty when none exist. Prefer exactly one; packaging and the
        /// Model Manager use the first entry and surface a non-blocking error when there
        /// are more.
        /// </summary>
        public static List<(string path, TryllModelManifest manifest)> FindProjectManifests()
        {
            var result = new List<(string, TryllModelManifest)>();
            if (DebugOverrideForTests != null)
            {
                result.Add((DebugOverridePathForTests, DebugOverrideForTests));
                return result;
            }
            var guids = UnityEditor.AssetDatabase.FindAssets("t:TryllModelManifest");
            if (guids == null || guids.Length == 0) return result;

            var paths = new List<string>(guids.Length);
            foreach (var g in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            }
            paths.Sort(System.StringComparer.OrdinalIgnoreCase);

            foreach (var path in paths)
            {
                var m = UnityEditor.AssetDatabase.LoadAssetAtPath<TryllModelManifest>(path);
                if (m != null) result.Add((path, m));
            }
            return result;
        }

        /// <summary>
        /// Locates the project's primary manifest asset (sorted-first path), or null if none
        /// has been created yet. Shared by the Model Manager window and the editor
        /// model-readiness / packaging steps.
        /// </summary>
        public static TryllModelManifest FindProjectManifest()
        {
            var all = FindProjectManifests();
            return all.Count > 0 ? all[0].manifest : null;
        }

        /// <summary>
        /// Test-only in-memory manifest that every editor lookup returns instead of
        /// the project asset, so window tests never write
        /// <see cref="EditorAssetPath"/>. Callers that persist must check
        /// <c>AssetDatabase.Contains</c> before saving.
        /// </summary>
        internal static TryllModelManifest DebugOverrideForTests { get; set; }

        internal const string DebugOverridePathForTests = "(test override)";
#endif
    }
}
