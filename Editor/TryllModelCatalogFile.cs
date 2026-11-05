// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Reads the bundled server catalog (<c>.Server/&lt;variant&gt;/data/models.json</c>),
    /// the optional project catalog from Project Settings, and optional drop-ins
    /// under <c>data/models.d/*.json</c> and <c>&lt;ProjectRoot&gt;/Tryll/models.d/*.json</c>,
    /// so editor UI can answer
    /// catalog questions with no server running and nothing registered yet.
    ///
    /// Merge rules match the server: base/earlier wins on duplicate model names;
    /// a malformed user file is skipped and the rest still load.
    ///
    /// This is a *complement* to the two live sources, not a replacement:
    ///
    /// <list type="bullet">
    /// <item><c>ListModels</c> — authoritative, needs a connected server.</item>
    /// <item><see cref="TryllModelManifest"/> — what this project registered,
    ///       cached at registration time.</item>
    /// <item>This — everything the shipped + user catalogs *could* offer, whether
    ///       or not it is registered or downloaded.</item>
    /// </list>
    ///
    /// Used for the "how many models can serve this language" counts in the
    /// language picker, where the useful number is the catalog's, not the
    /// project's: it answers "could I register something for Japanese?" rather
    /// than "did I already?".
    ///
    /// Results are cached and refreshed only when any layer's timestamp changes —
    /// settings panels repaint every frame and must not re-parse JSON each time.
    /// </summary>
    internal static class TryllModelCatalogFile
    {
        // ── models.json shape (only the fields we need; JsonUtility ignores the rest) ──
        [Serializable] private sealed class CatalogFile    { public CatalogModel[] models; }
        [Serializable] private sealed class CatalogModel   { public string name; public string model_type; public CatalogVariant[] variants; }
        [Serializable] private sealed class CatalogVariant { public string engine; public string licence_url; public string[] languages; public CatalogTtsCaps tts_capabilities; }
        [Serializable] private sealed class CatalogPreset  { public string id; public string label; public string description; }
        [Serializable] private sealed class CatalogTtsCaps
        {
            public bool voice_description;
            public bool delivery_instruction;
            public bool reference_voice;
            public bool speed;
            public bool language_per_call;
            public bool expressiveness;
            // -1 = absent. JsonUtility cannot leave a nested object null, so presence of
            // tts_capabilities is inferred from num_speakers or voice_presets.
            public int num_speakers = -1;
            public CatalogPreset[] voice_presets;
        }

        private static Dictionary<string, TryllTtsCapabilities> s_TtsCapsByName;
        // Keyed by LicenceKey(name, engine), plus LicenceKey(name, "") for the model's first
        // variant that declares one — the fallback when the engine has no entry of its own.
        private static Dictionary<string, string> s_LicenceUrls;

        private static string[]                   s_CachedPaths;
        private static DateTime[]                 s_CachedStamps;
        private static Dictionary<string, int>    s_CountsByLanguage;
        private static int                        s_ModelCount;

        /// <summary>
        /// Number of catalog models declaring each language (counted per model, not
        /// per variant). Empty when the catalog cannot be found or parsed — callers
        /// must degrade to showing no counts rather than reporting zeros, which
        /// would read as "nothing supports this".
        /// </summary>
        public static IReadOnlyDictionary<string, int> CountsByLanguage()
        {
            Refresh();
            return s_CountsByLanguage ?? EmptyCounts;
        }

        /// <summary>
        /// Catalog TTS capabilities of <paramref name="modelName"/> (first variant that declares
        /// them). False when the catalog is unavailable or the model declares none — callers
        /// must then treat every field as possibly supported.
        /// </summary>
        public static bool TryGetTtsCapabilities(string modelName, out TryllTtsCapabilities caps)
        {
            Refresh();
            caps = default;
            if (string.IsNullOrEmpty(modelName) || s_TtsCapsByName == null) return false;
            return s_TtsCapsByName.TryGetValue(modelName, out caps);
        }

        /// <summary>
        /// Catalog <c>licence_url</c> of the <paramref name="engine"/> variant of
        /// <paramref name="modelName"/>, else of the model's first variant that declares one.
        /// False when the catalog is unavailable or the model declares none (typical for
        /// user-catalog entries).
        /// </summary>
        public static bool TryGetLicenceUrl(string modelName, string engine, out string url)
        {
            Refresh();
            url = null;
            if (string.IsNullOrEmpty(modelName) || s_LicenceUrls == null) return false;
            if (!string.IsNullOrEmpty(engine) && s_LicenceUrls.TryGetValue(LicenceKey(modelName, engine), out url))
                return true;
            return s_LicenceUrls.TryGetValue(LicenceKey(modelName, ""), out url);
        }

        private static string LicenceKey(string modelName, string engine) => modelName + "|" + engine;

        /// <summary>Total models in the merged catalog; 0 when it could not be read.</summary>
        public static int ModelCount { get { Refresh(); return s_ModelCount; } }

        /// <summary>True when a catalog was found and parsed.</summary>
        public static bool IsAvailable { get { Refresh(); return s_CountsByLanguage != null; } }

        private static readonly Dictionary<string, int> EmptyCounts = new Dictionary<string, int>();

        private static void Refresh()
        {
            string[] paths = ResolveLayerPaths();
            if (paths.Length == 0)
            {
                s_CachedPaths = null;
                s_CachedStamps = null;
                s_CountsByLanguage = null;
                s_TtsCapsByName = null;
                s_LicenceUrls = null;
                s_ModelCount = 0;
                return;
            }

            if (LayersUnchanged(paths) && s_CountsByLanguage != null)
                return;

            s_CachedPaths = paths;
            s_CachedStamps = new DateTime[paths.Length];
            for (int i = 0; i < paths.Length; ++i)
                s_CachedStamps[i] = File.GetLastWriteTimeUtc(paths[i]);

            s_CountsByLanguage = null;
            s_TtsCapsByName = null;
            s_LicenceUrls = null;
            s_ModelCount = 0;

            try
            {
                var modelsByName = new Dictionary<string, CatalogModel>(StringComparer.Ordinal);
                for (int i = 0; i < paths.Length; ++i)
                {
                    bool isBase = i == 0;
                    MergeFile(paths[i], modelsByName, isBase);
                }

                var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var model in modelsByName.Values)
                {
                    if (model?.variants == null) continue;

                    var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var v in model.variants)
                    {
                        if (v?.languages == null) continue;
                        foreach (var code in v.languages)
                            if (!string.IsNullOrEmpty(code)) declared.Add(code);
                    }
                    foreach (var code in declared)
                        counts[code] = counts.TryGetValue(code, out var n) ? n + 1 : 1;
                }

                var ttsCaps = new Dictionary<string, TryllTtsCapabilities>(StringComparer.Ordinal);
                foreach (var model in modelsByName.Values)
                {
                    if (model?.variants == null) continue;
                    foreach (var v in model.variants)
                    {
                        var c = v?.tts_capabilities;
                        if (c == null) continue;
                        int presets = c.voice_presets?.Length ?? 0;
                        if (c.num_speakers < 0 && presets == 0) continue;  // not declared
                        var presetArr = new TryllTtsVoicePreset[presets];
                        for (int i = 0; i < presets; ++i)
                        {
                            var pr = c.voice_presets[i];
                            presetArr[i] = new TryllTtsVoicePreset
                            {
                                Id = pr?.id, Label = pr?.label, Description = pr?.description,
                            };
                        }
                        ttsCaps[model.name] = new TryllTtsCapabilities
                        {
                            VoiceDescription    = c.voice_description,
                            DeliveryInstruction = c.delivery_instruction,
                            ReferenceVoice      = c.reference_voice,
                            Speed               = c.speed,
                            LanguagePerCall     = c.language_per_call,
                            Expressiveness      = c.expressiveness,
                            NumSpeakers         = c.num_speakers >= 0 ? c.num_speakers : presets,
                            VoicePresets        = presetArr,
                            InlineTags          = Array.Empty<TryllTtsInlineTag>(),
                        };
                        break;
                    }
                }

                var licences = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var model in modelsByName.Values)
                {
                    if (model?.variants == null) continue;
                    foreach (var v in model.variants)
                    {
                        if (string.IsNullOrEmpty(v?.licence_url)) continue;
                        string fallback = LicenceKey(model.name, "");
                        if (!licences.ContainsKey(fallback)) licences[fallback] = v.licence_url;
                        if (string.IsNullOrEmpty(v.engine)) continue;
                        string key = LicenceKey(model.name, v.engine);
                        if (!licences.ContainsKey(key)) licences[key] = v.licence_url;
                    }
                }

                s_CountsByLanguage = counts;
                s_TtsCapsByName    = ttsCaps;
                s_LicenceUrls      = licences;
                s_ModelCount       = modelsByName.Count;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Tryll] Could not read the model catalog: {ex.Message}");
                s_CountsByLanguage = null;
                s_TtsCapsByName = null;
                s_LicenceUrls = null;
            }
        }

        private static bool LayersUnchanged(string[] paths)
        {
            if (s_CachedPaths == null || s_CachedStamps == null || s_CachedPaths.Length != paths.Length)
                return false;
            for (int i = 0; i < paths.Length; ++i)
            {
                if (paths[i] != s_CachedPaths[i]) return false;
                if (File.GetLastWriteTimeUtc(paths[i]) != s_CachedStamps[i]) return false;
            }
            return true;
        }

        private static void MergeFile(
            string path,
            Dictionary<string, CatalogModel> modelsByName,
            bool isBase)
        {
            CatalogFile parsed;
            try
            {
                parsed = JsonUtility.FromJson<CatalogFile>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                if (isBase)
                    throw;
                Debug.LogWarning(
                    $"[Tryll] [user catalog] '{path}' could not be parsed and was skipped: {ex.Message}");
                return;
            }

            if (parsed?.models == null)
            {
                if (isBase) return;
                Debug.LogWarning(
                    $"[Tryll] [user catalog] '{path}' has no models array and was skipped.");
                return;
            }

            foreach (var model in parsed.models)
            {
                if (model == null || string.IsNullOrEmpty(model.name)) continue;
                if (modelsByName.ContainsKey(model.name))
                {
                    if (!isBase)
                    {
                        Debug.LogWarning(
                            $"[Tryll] [user catalog] '{path}' defines model '{model.name}', " +
                            "which an earlier catalog already defines. The earlier/base entry wins.");
                    }
                    continue;
                }
                modelsByName[model.name] = model;
            }
        }

        /// <summary>
        /// Ordered layer paths: base models.json first, then payload models.d,
        /// then project Tryll/models.d — each directory's *.json sorted by name.
        /// </summary>
        private static string[] ResolveLayerPaths()
        {
            var paths = new List<string>();
            try
            {
                string serverDir = TryllServerProcess.ResolveServerDir(TryllRuntimeSettings.Load());
                if (string.IsNullOrEmpty(serverDir)) return Array.Empty<string>();

                string basePath = Path.Combine(serverDir, "data", "models.json");
                if (!File.Exists(basePath)) return Array.Empty<string>();
                paths.Add(basePath);

                string userCatalog = TryllRuntimeSettings.Load().ResolveUserModelsCatalogForLaunch();
                if (!string.IsNullOrEmpty(userCatalog) && File.Exists(userCatalog))
                    paths.Add(userCatalog);

                AppendSortedJson(paths, Path.Combine(serverDir, "data", "models.d"));
                AppendSortedJson(paths,
                    Path.Combine(TryllRuntimeSettings.ProjectRootDirectory(), "Tryll", "models.d"));
            }
            catch
            {
                return Array.Empty<string>();
            }
            return paths.ToArray();
        }

        private static void AppendSortedJson(List<string> paths, string dir)
        {
            if (!Directory.Exists(dir)) return;
            var files = Directory.GetFiles(dir, "*.json");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (var f in files)
                paths.Add(f);
        }
    }
}
