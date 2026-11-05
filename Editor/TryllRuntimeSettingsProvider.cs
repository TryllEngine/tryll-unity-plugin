// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Registers a "Tryll Client" entry under Project Settings.
    /// Mirrors the UTryllRuntimeSettings UDeveloperSettings panel in Unreal.
    /// </summary>
    internal sealed class TryllRuntimeSettingsProvider : SettingsProvider
    {
        private const string SettingsPath    = "Project/Tryll Client";
        private const string ResourceFolder  = "Assets/Resources";

        private SerializedObject _serializedSettings;

        private TryllRuntimeSettingsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new TryllRuntimeSettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label    = "Tryll Client",
                keywords = new HashSet<string>(new[] { "Tryll", "AI", "inference", "server", "NPC", "cuda", "vulkan", "gpu" })
            };
        }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            var settings = LoadOrCreateSettings();
            _serializedSettings = new SerializedObject(settings);
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedSettings == null) return;
            _serializedSettings.Update();

            EditorGUILayout.LabelField("Server Process", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty(nameof(TryllRuntimeSettings.AutoLaunchServer)));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty(nameof(TryllRuntimeSettings.LlamaCppBackend)));

            // The build-variant selector is only meaningful when more than one variant folder
            // (e.g. Default + Release) is present under the package's .Server/ folder. Otherwise
            // Default is always used, so hide the option to avoid confusion.
            var variants = Internal.TryllServerProcess.EditorAvailableVariants();
            if (variants.Length > 1)
            {
                EditorGUILayout.PropertyField(
                    _serializedSettings.FindProperty(nameof(TryllRuntimeSettings.ServerBuildVariant)));
            }

            EditorGUILayout.HelpBox(
                Internal.TryllServerProcess.EditorHasBundledServer()
                    ? "The server is bundled with the package (.Server/) and launched automatically — " +
                      "no path configuration required."
                    : "The server is installed once per version from Window > Tryll > Welcome and " +
                      "launched automatically — no path configuration required.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Models", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Optional. Point at a models catalog JSON inside this project — any file name, " +
                "any folder. Leave the field empty if you are not adding models. The path is " +
                "stored relative to the project and cannot point outside it. The next " +
                "auto-launch loads it. From code, set it before BeforeSceneLoad " +
                "(see BeforeAutoLaunch), or call TrySetUserModelsCatalogPath to restart a " +
                "server that is already running. A player build copies the saved file to " +
                "Server/data/user-catalog/.",
                MessageType.None);
            DrawUserModelsCatalogPath();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Play Mode (Editor)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty(nameof(TryllRuntimeSettings.EnablePlayModeModelGuard)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Connection Defaults", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty(nameof(TryllRuntimeSettings.ServerHost)));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty(nameof(TryllRuntimeSettings.ServerPort)));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty(nameof(TryllRuntimeSettings.ConnectMaxAttempts)));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty(nameof(TryllRuntimeSettings.ConnectRetryDelaySeconds)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Inference Engines", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Derived from the models you register in Window > Tryll > Model Manager " +
                "— there is nothing to choose here. Each model's catalog entry already " +
                "states which engine runs it. The session sends the engines your registered " +
                "models need only as a warm-up hint, so they start at session creation " +
                "instead of at first use. With no model registered the hint is empty, " +
                "which is fine.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Telemetry", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty(nameof(TryllRuntimeSettings.GameName)));
            EditorGUILayout.HelpBox(
                "Game Name is sent as --project-name and as the session game_name when set. " +
                "When empty, Application.productName is used.",
                MessageType.None);
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty(nameof(TryllRuntimeSettings.TelemetryLevel)),
                new GUIContent("Telemetry Level"));
            EditorGUILayout.HelpBox(
                "Basic is the default: hardware, session, graph shape, and lifetime stats. No conversation text.\n" +
                "Turns adds per-turn timings and counts. No conversation text.\n" +
                "Full adds prompts, outputs, and error text.\n" +
                "The running server keeps its current level until the next launch.",
                MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Storage", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Process-wide storage root for KBs, voices, and Client Dev model downloads. " +
                "Must be a folder inside the project and outside Assets/ (default TryllStorage/). " +
                "The auto-launched server receives it as --storage-root. A player build copies " +
                "it to StreamingAssets/Tryll/ (Production models only). Packaged players currently " +
                "launch --mode client-dev until a Client Prod distribution exists.",
                MessageType.Info);
            DrawStorageDataFolder();
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty(nameof(TryllRuntimeSettings.SaveDataFolder)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Language", EditorStyles.boldLabel);
            DrawLocaleDropdown();
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty(nameof(TryllRuntimeSettings.TextProvider)));
            EditorGUILayout.HelpBox(
                "A Text Provider resolves Localized prompt fields (Text IDs) and, when set, " +
                "its current language is the session language. CreateSession(locale:) still wins. " +
                "Clear the provider to use the Locale field above.",
                MessageType.None);

            _serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private void DrawStorageDataFolder()
        {
            var prop = _serializedSettings.FindProperty(nameof(TryllRuntimeSettings.StorageDataFolder));
            string previous = prop.stringValue;
            EditorGUILayout.PropertyField(prop);

            string project = TryllRuntimeSettings.ProjectRootDirectory();
            string configured = prop.stringValue;
            bool ok = TryllRuntimeSettings.TryNormalizeStorageDataFolder(
                project, configured, out string relative, out string error);

            if (!ok)
            {
                EditorGUILayout.HelpBox(error ?? "Invalid storage data folder.", MessageType.Error);
                if (GUILayout.Button("Move to TryllStorage"))
                {
                    if (_serializedSettings.targetObject is TryllRuntimeSettings settings)
                    {
                        // Apply the typed value first so migration sees the current path.
                        _serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                        if (TryllStorageMigration.TryMoveToTryllStorage(settings, out string moveError))
                        {
                            prop.stringValue = TryllRuntimeSettings.DefaultStorageDataFolder;
                            Debug.Log("[Tryll] Moved storage folder to TryllStorage/.");
                        }
                        else
                        {
                            TryllEditorPrompts.Notify("Move to TryllStorage", moveError ?? "Move failed.", "OK");
                        }
                    }
                }
            }
            else if (!string.Equals(configured ?? "", relative ?? "", System.StringComparison.Ordinal))
            {
                // Normalize empty / absolute-inside-project to the canonical relative form.
                prop.stringValue = relative;
            }

            if (!string.Equals(previous ?? "", prop.stringValue ?? "", System.StringComparison.Ordinal)
                && Internal.TryllServerProcess.IsRunning
                && _serializedSettings.targetObject is TryllRuntimeSettings live)
            {
                _serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                if (!Internal.TryllServerProcess.TryLaunch(live, out string launchError)
                    && !string.IsNullOrEmpty(launchError))
                {
                    Debug.LogError("[Tryll] Storage data folder changed, but the server did not restart: " + launchError);
                }
            }
        }

        private void DrawUserModelsCatalogPath()
        {
            var prop = _serializedSettings.FindProperty(nameof(TryllRuntimeSettings.UserModelsCatalogPath));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop, new GUIContent("Catalog"));
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string project = TryllRuntimeSettings.ProjectRootDirectory();
                string picked = TryllEditorPrompts.OpenFilePanel("Custom models catalog", project, "json");
                if (!string.IsNullOrEmpty(picked))
                {
                    if (TryllRuntimeSettings.TryNormalizeUserModelsCatalogPath(
                            project, picked, out string relative, out string error))
                    {
                        prop.stringValue = relative;
                        if (_serializedSettings.targetObject is TryllRuntimeSettings settings)
                            settings.TrySetUserModelsCatalogPath(relative, out _);
                    }
                    else
                    {
                        TryllEditorPrompts.Notify("Custom models catalog", error, "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Language picker, built from the languages the project's *registered*
        /// models declare (the <see cref="TryllModelManifest"/>), never a hardcoded
        /// list — so it can only ever offer something the project can actually load.
        ///
        /// The offered set is a **union** across registered models, not an
        /// intersection: registering an English-only model for one character must
        /// not collapse the whole menu to English. Where a specific model cannot do
        /// the chosen language, that is reported here as a summary and on the model
        /// picker itself, which is where it is actionable.
        /// </summary>
        private void DrawLocaleDropdown()
        {
            var localeProp = _serializedSettings.FindProperty(nameof(TryllRuntimeSettings.Locale));
            var manifest   = TryllModelManifest.FindProjectManifest();

            if (manifest == null)
            {
                EditorGUILayout.HelpBox(
                    "No model manifest in this project yet. Open Window ▸ Tryll ▸ Model Manager and "
                    + "register the models you ship; the languages they declare become the choices here.",
                    MessageType.Info);
                DrawLocaleOptions(localeProp, new List<string>());
                return;
            }

            var codes = manifest.DeclaredLanguages();
            if (codes.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "None of the registered models declare their languages. Re-register them in the "
                    + "Model Manager while connected to a server so Tryll can cache what they support.",
                    MessageType.Info);
                DrawLocaleOptions(localeProp, codes);
                return;
            }

            DrawLocaleOptions(localeProp, codes);

            string selected = localeProp.stringValue;
            if (!string.IsNullOrEmpty(selected) && TryllModelCatalogFile.IsAvailable)
            {
                string code = TryllLanguages.LanguageOf(selected);
                var catalogCounts = TryllModelCatalogFile.CountsByLanguage();
                catalogCounts.TryGetValue(code, out int n);
                EditorGUILayout.LabelField(
                    " ",
                    $"{n} of {TryllModelCatalogFile.ModelCount} catalog models declare this language.",
                    EditorStyles.miniLabel);
            }

            if (string.IsNullOrEmpty(selected))
            {
                EditorGUILayout.HelpBox(
                    "No language selected. Voice output uses each model's own default, storage paths "
                    + "resolve unlocalized, and locale template keys are absent.",
                    MessageType.None);
                return;
            }

            // Union means some registered model may not cover the pick — say which.
            var missing = manifest.ModelsMissingLanguage(
                TryllLanguages.LanguageOf(selected), "tts", "stt");
            if (missing.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{missing.Count} registered voice model(s) cannot do this language: "
                    + string.Join(", ", missing)
                    + ".\n\nThis matters only when TTS or STT relies on the session locale. An explicit "
                    + "Lang on a voice profile exempts that voice; STT has no per-input override. Incompatible "
                    + "voices or voice inputs are rejected when they are created.",
                    MessageType.Warning);
            }
        }

        private static void DrawLocaleOptions(SerializedProperty localeProp, List<string> codes)
        {
            // "(none)" first: an unset locale is a valid, meaningful choice — it is
            // the pre-locale behaviour, not a missing value.
            //
            // Each entry is annotated with how many models in the shipped catalog
            // (models.json) can serve it — the catalog count, not the registered
            // one, because the question this answers is "is this language well
            // supported, or am I down to a single model?". The registered-model
            // side is covered by the mismatch summary below.
            var catalogCounts = TryllModelCatalogFile.CountsByLanguage();
            var options = new List<string> { NoneLabel };
            foreach (var c in codes)
            {
                string label = $"{TryllLanguages.DisplayName(c)} ({c})";
                if (catalogCounts.TryGetValue(c, out int n))
                    label += n == 1 ? "  —  1 model" : $"  —  {n} models";
                options.Add(label);
            }

            string current   = localeProp.stringValue ?? string.Empty;
            string currentLc = TryllLanguages.LanguageOf(current);

            int index = 0;
            if (!string.IsNullOrEmpty(currentLc))
            {
                int found = codes.IndexOf(currentLc);
                if (found >= 0 && string.Equals(
                        current, codes[found], System.StringComparison.OrdinalIgnoreCase))
                {
                    index = found + 1;
                }
                else
                {
                    // A value set by hand (or left over after unregistering a model)
                    // must never be silently rewritten — show it and say why.
                    options.Add(found >= 0
                        ? $"{current}  (uses {codes[found]} model capability)"
                        : $"{current}  (not offered by any registered model)");
                    index = options.Count - 1;
                }
            }

            int picked = EditorGUILayout.Popup("Locale", index, options.ToArray());
            if (picked != index)
            {
                localeProp.stringValue =
                    picked == 0 ? string.Empty
                                : (picked - 1 < codes.Count ? codes[picked - 1] : current);
            }
        }

        private const string NoneLabel = "(none — no session language)";

        /// <summary>
        /// The project's settings asset at <see cref="TryllRuntimeSettings.EditorAssetPath"/>,
        /// created with defaults when missing. Also used by the Welcome flow, so a new project
        /// works before anyone opens Project Settings.
        /// </summary>
        internal static TryllRuntimeSettings LoadOrCreateSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TryllRuntimeSettings>(TryllRuntimeSettings.EditorAssetPath);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder(ResourceFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var asset = ScriptableObject.CreateInstance<TryllRuntimeSettings>();
            asset.Locale = TryllRuntimeSettings.DefaultLocale;
            asset.StorageDataFolder = TryllRuntimeSettings.DefaultStorageDataFolder;
            AssetDatabase.CreateAsset(asset, TryllRuntimeSettings.EditorAssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
