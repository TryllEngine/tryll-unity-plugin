// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>Mic capture mode used by the editor Test Chat window.</summary>
    public enum VoiceMode
    {
        PushToTalk,
        HandsFree,
    }

    /// <summary>
    /// Which bundled server build to launch from the package's <c>.Server/</c> folder.
    /// Maps to the <c>Default</c> / <c>Release</c> subfolders. Only honored when more than
    /// one variant folder is present; otherwise <c>Default</c> is always used.
    /// </summary>
    public enum ServerBuild
    {
        Default,
        Release,
    }

    /// <summary>
    /// llama.cpp GPU plugin preference passed to an auto-launched server as
    /// <c>--llama-backend</c>. Vulkan is the portable default. Auto prefers CUDA
    /// on NVIDIA when the CUDA files were staged, otherwise Vulkan.
    /// Ignored when <see cref="TryllRuntimeSettings.AutoLaunchServer"/> is false.
    /// </summary>
    public enum TryllLlamaCppBackend
    {
        Vulkan,
        Auto,
    }

    /// <summary>
    /// Detail an editor server sends. <c>off</c> is command-line only and is not a project setting.
    /// </summary>
    public enum TryllTelemetryLevel
    {
        Basic,
        Turns,
        Full,
    }

    /// <summary>
    /// Project-level settings for the Tryll Client package.
    /// Edit in Project Settings → Tryll Client (registered by TryllRuntimeSettingsProvider).
    ///
    /// At runtime, load the asset with <see cref="TryllRuntimeSettings.Load"/>.
    /// Persisted to <c>Assets/Resources/TryllRuntimeSettings.asset</c> (created on first access).
    ///
    /// Mirrors UTryllRuntimeSettings (UDeveloperSettings) from the Unreal plugin.
    /// </summary>
    [CreateAssetMenu(menuName = "Tryll/Runtime Settings", fileName = "TryllRuntimeSettings")]
    [HelpURL(TryllDocs.TryllRuntimeSettings)]
    public sealed class TryllRuntimeSettings : ScriptableObject
    {
        /// <summary>
        /// Project-relative path where the settings asset is stored under
        /// <c>Assets/Resources/</c>. Used by editor tooling (Project Settings, file picker).
        /// </summary>
        public const string EditorAssetPath = "Assets/Resources/TryllRuntimeSettings.asset";

        private const string ResourcePath = "TryllRuntimeSettings";

        private static TryllRuntimeSettings s_Instance;

        /// <summary>
        /// Load (or return the cached) runtime settings asset. Falls back to defaults if the
        /// asset does not exist in Resources.
        /// </summary>
        public static TryllRuntimeSettings Load()
        {
            if (s_Instance != null) return s_Instance;
            s_Instance = Resources.Load<TryllRuntimeSettings>(ResourcePath);
            if (s_Instance == null)
            {
                s_Instance = CreateInstance<TryllRuntimeSettings>();
                s_Instance.Locale = DefaultLocale;
            }
            return s_Instance;
        }

        // ── Server process ─────────────────────────────────────────────────────

        /// <summary>
        /// When true, <see cref="TryllClient"/> spawns the bundled <c>tryll_server.exe</c>
        /// automatically on startup (Windows only). The server is always resolved from the
        /// package payload — <c>Packages/com.tryll.client/.Server/</c> in the Editor, or the
        /// <c>Server/</c> folder copied next to the player exe at build time — so no path
        /// configuration is required. Set to false when you manage the server process yourself.
        /// </summary>
        [Tooltip("Spawn the bundled tryll_server.exe automatically on startup (Windows only).")]
        public bool AutoLaunchServer = true;

        /// <summary>
        /// Which bundled server build to launch — the <c>Default</c> or <c>Release</c> subfolder
        /// of the package's <c>.Server/</c> folder. This only takes effect when more than one
        /// variant folder is present (a plugin-development setup where <c>Default</c> and
        /// <c>Release</c> are symlinks to the Debug and Release server builds). When only one
        /// folder is present (e.g. a shipped plugin with a single production build in
        /// <c>Default</c>), <c>Default</c> is always used regardless of this value.
        /// </summary>
        [Tooltip("Which bundled server build to launch (Default/Release). Only applies when both " +
                 "variant folders exist in .Server/; otherwise Default is always used.")]
        public ServerBuild ServerBuildVariant = ServerBuild.Default;

        /// <summary>
        /// llama.cpp GPU plugin for an auto-launched server. Vulkan (default) is
        /// portable and skips CUDA DLLs in a player build. Auto prefers CUDA on
        /// NVIDIA when those DLLs were staged. Passed as <c>--llama-backend</c>;
        /// ignored when <see cref="AutoLaunchServer"/> is false.
        /// </summary>
        [Tooltip("llama.cpp GPU plugin for the auto-launched server. Vulkan is portable. " +
                 "Auto uses CUDA on NVIDIA when the CUDA files were staged. " +
                 "Ignored when Auto Launch Server is off.")]
        public TryllLlamaCppBackend LlamaCppBackend = TryllLlamaCppBackend.Vulkan;

        /// <summary>
        /// CLI value for <c>--llama-backend</c> (<c>vulkan</c> or <c>auto</c>).
        /// </summary>
        public string LlamaCppBackendCliValue()
            => LlamaCppBackend == TryllLlamaCppBackend.Auto ? "auto" : "vulkan";

        // ── Play Mode (Editor) ───────────────────────────────────────────────────

        /// <summary>
        /// Editor-only. When true (default), the Play-mode model guard verifies every registered
        /// required model is downloaded before entering Play — it briefly cancels the Play
        /// transition, downloads anything missing (behind a cancelable progress bar), then
        /// re-enters Play automatically. Turn this off to enter Play immediately with no
        /// pre-flight model check. No effect in a built player (the guard is editor-only).
        ///
        /// Mirrors bEnablePlayModeModelGuard on UTryllRuntimeSettings in the Unreal plugin.
        /// </summary>
        [Tooltip("Verify required models are downloaded before entering Play Mode (Editor only).")]
        public bool EnablePlayModeModelGuard = true;

        // ── Connection defaults ────────────────────────────────────────────────

        /// <summary>Host name or IP address of the Tryll server. Default: 127.0.0.1.</summary>
        [Tooltip("Host name or IP address of the Tryll server.")]
        public string ServerHost = "127.0.0.1";

        /// <summary>TCP port the Tryll server is listening on. Default: 9100.</summary>
        [Tooltip("TCP port the Tryll server is listening on.")]
        public int ServerPort = 9100;

        /// <summary>
        /// Seconds to wait for the server's unsolicited <c>ConnectionReady</c> after
        /// TCP connect. A silent listener (HTTP, a squat on the editor port) must
        /// not hang the socket worker forever. Mirrors Unreal's
        /// <c>ConnectionReadyTimeoutSeconds</c>.
        /// </summary>
        [Tooltip("Seconds to wait for ConnectionReady after TCP connect. Timeout becomes ConnectFailed.")]
        public double ConnectionReadyTimeoutSeconds = 10.0;

        /// <summary>
        /// Max connect attempts for an editor-held shared connection (Chat, Dialog
        /// Lab, Model Manager). 1 disables retry. The first <see cref="TryllClient.Connect"/>
        /// counts as attempt 1. Mirrors Unreal <c>ConnectMaxAttempts</c>.
        /// Play Mode <see cref="TryllClient.StartSession"/> still retries until Disconnect.
        /// </summary>
        [Tooltip("Editor connect attempts while a managed server is still warming. 1 = no retry.")]
        public int ConnectMaxAttempts = 20;

        /// <summary>Delay between editor connect attempts. Mirrors Unreal <c>ConnectRetryDelaySeconds</c>.</summary>
        [Tooltip("Seconds to wait between editor connect retries.")]
        public float ConnectRetryDelaySeconds = 1.0f;

        // ── Telemetry ──────────────────────────────────────────────────────────

        /// <summary>
        /// Integration identifier sent to the server on every session for telemetry grouping.
        /// Use a short, stable, lowercase slug — e.g. <c>"my-game"</c> or <c>"tryll-roleplay-demo"</c>.
        /// Leave empty to omit (server defaults to <c>"unknown"</c> when telemetry is enabled).
        /// </summary>
        [Tooltip("Telemetry group identifier for this integration (e.g. \"my-game\"). " +
                 "Leave empty to omit.")]
        public string GameName = string.Empty;

        /// <summary>
        /// Detail the editor server sends. <see cref="TryllTelemetryLevel.Basic"/> is the default.
        /// Passed as <c>--telemetry-level</c> on the next client-dev launch. Packaged players
        /// do not send the flag.
        /// </summary>
        [Tooltip("Basic is the default. Turns and Full are for debugging. " +
                 "The running server keeps its current level until the next launch.")]
        public TryllTelemetryLevel TelemetryLevel = TryllTelemetryLevel.Basic;

        /// <summary>CLI token for <paramref name="level"/>. Unknown values become <c>basic</c>.</summary>
        public static string TelemetryLevelCliToken(TryllTelemetryLevel level)
        {
            switch (level)
            {
                case TryllTelemetryLevel.Turns: return "turns";
                case TryllTelemetryLevel.Full: return "full";
                default: return "basic";
            }
        }

        /// <summary>
        /// Hidden 32-hex project lineage id. Passed as <c>--project-id</c>.
        /// Not a user setting. A clone keeps the same value.
        /// </summary>
        [SerializeField, HideInInspector]
        public string ProjectId = string.Empty;

        public const int MaxWindowsLaunchCodeUnits = 30000;

        public static bool IsProjectId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                return false;
            for (int i = 0; i < value.Length; ++i)
            {
                char c = value[i];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex)
                    return false;
            }
            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Generate and save a project id on the committed settings asset when it is missing.
        /// </summary>
        public static string EnsureSavedProjectId()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TryllRuntimeSettings>(EditorAssetPath);
            if (asset == null)
            {
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                asset = CreateInstance<TryllRuntimeSettings>();
                asset.Locale = DefaultLocale;
                asset.StorageDataFolder = DefaultStorageDataFolder;
                UnityEditor.AssetDatabase.CreateAsset(asset, EditorAssetPath);
            }
            if (!IsProjectId(asset.ProjectId))
            {
                asset.ProjectId = System.Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(asset);
                UnityEditor.AssetDatabase.SaveAssets();
                s_Instance = null;
            }
            return asset.ProjectId.ToLowerInvariant();
        }
#endif

        // ── Storage ────────────────────────────────────────────────────────────

        /// <summary>
        /// Default process-wide storage root relative to the Unity project folder
        /// (parent of <c>Assets/</c>). Must stay outside <c>Assets/</c> so Unity's
        /// Asset Database never tracks model downloads or derived files.
        /// </summary>
        public const string DefaultStorageDataFolder = "TryllStorage";

        /// <summary>
        /// Project-relative folder that is the process-wide storage root for an
        /// auto-launched server (<c>--storage-root</c>). Node storage paths
        /// (string / embedded string storages, voice hotwords) and Client Dev
        /// model downloads resolve against it.
        ///
        /// Must be inside the project and <b>outside</b> <c>Assets/</c>. Default
        /// <see cref="DefaultStorageDataFolder"/>. A player build stages this
        /// folder to <c>StreamingAssets/Tryll/</c>; see
        /// <see cref="ResolveStorageRootForRuntime"/>.
        /// </summary>
        [Tooltip("Project-relative storage root outside Assets/ (default TryllStorage). " +
                 "Passed to the server as --storage-root. Paths under Assets/ are rejected.")]
        public string StorageDataFolder = DefaultStorageDataFolder;

        /// <summary>
        /// Folder an auto-launched sidecar passes as <c>--save-root</c> for
        /// <c>SaveGameMemory</c> / <c>SaveDialog</c> relative paths.
        ///
        /// Empty in the editor omits the flag (relative saves stay under the
        /// session storage root). Empty in a player build uses
        /// <see cref="Application.persistentDataPath"/>.
        /// </summary>
        [Tooltip("Save folder passed to an auto-launched server as --save-root. " +
                 "Empty in the Editor keeps relative saves under the storage root. " +
                 "Empty in a player build uses persistentDataPath.")]
        public string SaveDataFolder = string.Empty;

        /// <summary>
        /// Optional project-relative path to a custom models catalog JSON.
        /// Any file name. Empty means this project adds no extra models.
        ///
        /// Stored relative to the Unity project folder (parent of <c>Assets/</c>),
        /// for example <c>Assets/Tryll/my-models.json</c>. An auto-launched server
        /// receives the resolved file as <c>--user-models-path</c>. A path that
        /// is absolute and outside the project, or that escapes the project with
        /// <c>..</c>, is rejected. The file itself is optional: leave this empty
        /// when you do not add models.
        ///
        /// The first auto-launch runs at <c>BeforeSceneLoad</c>, before any
        /// scene <c>Awake</c>. Subscribe to <see cref="BeforeAutoLaunch"/> from
        /// <c>AfterAssembliesLoaded</c>, or call
        /// <see cref="TrySetUserModelsCatalogPath"/> on the cached
        /// <see cref="Load"/> instance before that, so the first process sees
        /// the file. Calling the setter while a server this package spawned is
        /// already running restarts it; an open client session is dropped.
        ///
        /// A player build copies the catalog saved here to
        /// <c>Server/data/user-catalog/</c> and rewrites relative
        /// <c>chat_template_file</c> values so they sit beside that copy.
        /// A different file chosen at runtime in a build must live under
        /// <c>Assets/StreamingAssets/</c>.
        /// </summary>
        [Tooltip("Optional project-relative JSON catalog of extra models. Any file name. Empty = none.")]
        public string UserModelsCatalogPath = string.Empty;

        /// <summary>
        /// Folder under the staged server directory where a player build places
        /// the custom catalog and its rewritten chat templates.
        /// </summary>
        public const string StagedUserCatalogDirectory = "data/user-catalog";

        /// <summary>
        /// Raised immediately before auto-launch. Subscribe from
        /// <c>[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]</c>
        /// when the catalog has to be chosen before the first spawn.
        /// </summary>
        public static event Action BeforeAutoLaunch;

        internal static void NotifyBeforeAutoLaunch()
        {
            Action handler = BeforeAutoLaunch;
            if (handler == null)
                return;
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // ── Language ───────────────────────────────────────────────────────────

        /// <summary>
        /// Session language as a BCP-47 tag ("de", "de-DE", "pt-BR"). One setting
        /// the whole voice + text stack derives from, so a language menu does not
        /// have to reach into every agent:
        ///
        /// <list type="bullet">
        /// <item>TTS — the default <c>lang</c> of every agent's voice. A voice
        ///       profile that authored its own <c>Lang</c> still wins.</item>
        /// <item>STT — language hint for multilingual recognizers (Whisper, Canary).</item>
        /// <item>Files — a linked file can resolve to a
        ///       <c>loc/&lt;locale&gt;/</c> sibling when one exists, so localized
        ///       knowledge bases and canned lines are a folder layout rather than code.</item>
        /// <item>Text — templates can read the <c>language</c> key to write a
        ///       "reply in X" instruction.</item>
        /// </list>
        ///
        /// Empty leaves every one of those at its pre-locale behaviour.
        /// This is the value used at session start; to change language later call
        /// <see cref="TryllClient.RequestSetSessionLocale"/>, which reaches
        /// existing agents' voice and template keys but not content already bound
        /// to a node (see the localization how-to).
        ///
        /// <c>Application.systemLanguage</c> or Unity Localization's selected locale
        /// are the usual sources — assign one before the session starts.
        /// </summary>
        [Tooltip("Session language as a BCP-47 tag (e.g. de, pt-BR). Drives TTS " +
                 "language, STT hints, per-locale storage paths, and the {language} " +
                 "template key. Empty = unset. When a Text Provider is assigned, " +
                 "its current language is used instead unless CreateSession is " +
                 "given a locale.")]
        public string Locale = "";

        /// <summary>
        /// Resolves Text IDs on Localized prompt fields, in the editor and at
        /// runtime. When set, <see cref="ITryllTextProvider.CurrentLocale"/> is
        /// the session language unless <see cref="TryllClient.CreateSession"/>
        /// is passed a locale. A provider assigned in code on
        /// <see cref="TryllClient.TextProvider"/> wins over this asset.
        /// </summary>
        [Tooltip("Looks up Text IDs for Localized prompt fields. Its current language " +
                 "becomes the session language unless CreateSession is given one.")]
        public TryllTextProviderAsset TextProvider;

        /// <summary>
        /// Language a new project starts in. English, because a game that has not
        /// thought about language yet is overwhelmingly an English one, and an
        /// explicit starting value is easier to reason about than a blank field.
        ///
        /// This is only the default for a *newly created* settings asset —
        /// existing projects keep whatever they saved, so adopting it never
        /// changes an established project's behaviour. Clear it to "(none)" to
        /// opt out of session languages entirely.
        /// </summary>
        public const string DefaultLocale = "en";

        private void Reset()
        {
            Locale = DefaultLocale;
            StorageDataFolder = DefaultStorageDataFolder;
        }

        /// <summary>
        /// Telemetry / project identity: configured <see cref="GameName"/> when set,
        /// otherwise <see cref="Application.productName"/>. Passed as
        /// <c>--project-name</c> and used as the empty <c>game_name</c> fallback.
        /// </summary>
        public string ResolveProjectName()
        {
            if (!string.IsNullOrWhiteSpace(GameName))
                return GameName.Trim();
            return Application.productName ?? string.Empty;
        }

        /// <summary>
        /// Absolute process storage root for the running game or editor.
        /// Editor: resolved <see cref="StorageDataFolder"/> (default
        /// <see cref="DefaultStorageDataFolder"/>). Player: always
        /// <c>Application.streamingAssetsPath/Tryll</c> (what the build
        /// postprocessor stages).
        /// </summary>
        public string ResolveStorageRootForRuntime()
            => ResolveStorageRootForRuntime(StorageDataFolder);

        /// <inheritdoc cref="ResolveStorageRootForRuntime()"/>
        public static string ResolveStorageRootForRuntime(string configured)
        {
#if !UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, "Tryll"));
#else
            if (!TryNormalizeStorageDataFolder(
                    ProjectRootDirectory(), configured, out string relative, out _))
            {
                relative = DefaultStorageDataFolder;
            }
            return Path.GetFullPath(Path.Combine(
                ProjectRootDirectory(),
                relative.Replace('/', Path.DirectorySeparatorChar)));
#endif
        }

        /// <summary>
        /// Absolute <c>--storage-root</c> for the next sidecar spawn.
        /// Same as <see cref="ResolveStorageRootForRuntime"/> — kept for call
        /// sites that historically named the CreateSession helper.
        /// </summary>
        public string ResolveStorageDataFolderForServer()
            => ResolveStorageRootForRuntime();

        /// <inheritdoc cref="ResolveStorageDataFolderForServer()"/>
        public static string ResolveStorageDataFolderForServer(string configured)
            => ResolveStorageRootForRuntime(configured);

        /// <summary>
        /// Absolute <c>--save-root</c> for the next sidecar spawn, or empty when
        /// the editor should omit the flag.
        /// </summary>
        public string ResolveSaveDataFolderForLaunch()
            => ResolveSaveDataFolderForLaunch(SaveDataFolder);

        /// <inheritdoc cref="ResolveSaveDataFolderForLaunch()"/>
        public static string ResolveSaveDataFolderForLaunch(string configured)
        {
            if (!string.IsNullOrWhiteSpace(configured))
            {
                if (Path.IsPathRooted(configured))
                    return Path.GetFullPath(configured);
                return Path.GetFullPath(Path.Combine(ProjectRootDirectory(), configured.Trim()));
            }
#if !UNITY_EDITOR
            return Path.GetFullPath(Application.persistentDataPath);
#else
            return string.Empty;
#endif
        }

        /// <summary>
        /// Normalize a storage-root setting against <paramref name="projectRoot"/>.
        /// Empty becomes <see cref="DefaultStorageDataFolder"/>. Paths under
        /// <c>Assets/</c> or outside the project are rejected.
        /// </summary>
        public static bool TryNormalizeStorageDataFolder(
            string projectRoot, string configured, out string relative, out string error)
        {
            relative = DefaultStorageDataFolder;
            error = null;

            string trimmed = string.IsNullOrWhiteSpace(configured)
                ? DefaultStorageDataFolder
                : configured.Trim();

            string root = Path.GetFullPath(projectRoot);
            string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string full = Path.IsPathRooted(trimmed)
                ? Path.GetFullPath(trimmed)
                : Path.GetFullPath(Path.Combine(root, trimmed));

            if (!full.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(
                    full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
            {
                error = "Storage data folder must be inside the Unity project.";
                relative = string.Empty;
                return false;
            }

            if (IsUnderAssetsFolder(root, full))
            {
                error = "Storage data folder must be outside Assets/. " +
                        "Use TryllStorage (project root) so Unity does not track model " +
                        "downloads or derived files. Use Move to TryllStorage to migrate.";
                relative = string.Empty;
                return false;
            }

            relative = full.Substring(rootPrefix.Length).Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(relative))
            {
                error = "Storage data folder cannot be the project root itself; use a subfolder " +
                        "(default TryllStorage).";
                relative = string.Empty;
                return false;
            }
            return true;
        }

        /// <summary>
        /// True when <paramref name="fullPath"/> is the project's <c>Assets</c>
        /// folder or anything under it.
        /// </summary>
        public static bool IsUnderAssetsFolder(string projectRoot, string fullPath)
        {
            if (string.IsNullOrEmpty(projectRoot) || string.IsNullOrEmpty(fullPath))
                return false;
            string assets = Path.GetFullPath(Path.Combine(projectRoot, "Assets"));
            string assetsPrefix = assets.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(fullPath);
            return full.StartsWith(assetsPrefix, StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    assets.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Unity project folder (parent of <c>Assets/</c>) in Editor and Standalone.</summary>
        internal static string ProjectRootDirectory()
            => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        /// <summary>
        /// Store <paramref name="path"/> as this project's custom models catalog.
        /// Empty clears it. An absolute path is kept only when it sits inside the
        /// project, and is stored relative. Returns false and leaves the setting
        /// unchanged when the path is outside the project.
        ///
        /// When a server this package spawned is already running and the path
        /// changes, that process is restarted and an open client session is dropped.
        /// </summary>
        public bool TrySetUserModelsCatalogPath(string path, out string error)
        {
            if (!TryNormalizeUserModelsCatalogPath(ProjectRootDirectory(), path, out string relative, out error))
                return false;
            if (string.Equals(UserModelsCatalogPath ?? string.Empty, relative ?? string.Empty, StringComparison.Ordinal))
                return true;
            UserModelsCatalogPath = relative;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (Internal.TryllServerProcess.IsRunning)
                RelaunchSpawnedServer("Custom models catalog was saved");
#endif
            return true;
        }

        /// <summary>
        /// Absolute catalog file for the next sidecar spawn, or empty when the
        /// setting is unset or the path leaves the project.
        ///
        /// Editor: the project file, so relative chat templates resolve in place.
        /// Player: a file under StreamingAssets when the setting points there;
        /// otherwise the copy staged at <c>Server/data/user-catalog/</c>.
        /// </summary>
        public string ResolveUserModelsCatalogForLaunch()
        {
            if (!TryNormalizeUserModelsCatalogPath(
                    ProjectRootDirectory(), UserModelsCatalogPath, out string relative, out string error))
            {
                Debug.LogError("[Tryll] " + error);
                return string.Empty;
            }
            if (string.IsNullOrEmpty(relative))
                return string.Empty;

#if !UNITY_EDITOR
            const string streamingPrefix = "Assets/StreamingAssets/";
            if (relative.StartsWith(streamingPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string tail = relative.Substring(streamingPrefix.Length);
                return Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, tail));
            }

            string serverDir = Internal.TryllServerProcess.ResolveServerDir(this);
            if (!string.IsNullOrEmpty(serverDir))
            {
                string staged = Path.GetFullPath(Path.Combine(
                    serverDir, StagedUserCatalogDirectory, Path.GetFileName(relative)));
                if (File.Exists(staged))
                    return staged;
            }
#endif
            return Path.GetFullPath(Path.Combine(
                ProjectRootDirectory(), relative.Replace('/', Path.DirectorySeparatorChar)));
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        /// <summary>
        /// Relaunch through <see cref="Internal.TryllServerProcess.TryLaunch"/> after a launch
        /// input changed, and move a connected <see cref="TryllClient"/> to the new server.
        /// <paramref name="reason"/> prefixes the error when the relaunch fails.
        /// </summary>
        internal void RelaunchSpawnedServer(string reason)
        {
            var client = TryllClient.Instance;
            bool wasConnected = client != null && client.IsConnected;
            if (!Internal.TryllServerProcess.TryLaunch(this, out string launchError))
            {
                if (!string.IsNullOrEmpty(launchError))
                    Debug.LogError("[Tryll] " + reason + ", but the server did not restart: " + launchError);
                return;
            }

            int port = Internal.TryllServerProcess.ActualPort;
            if (client == null || port <= 0)
                return;
            client.ServerPort = port;
            if (!wasConnected)
                return;
            client.Disconnect();
            client.Connect();
        }
#endif

        /// <summary>
        /// Normalize a custom-catalog path against <paramref name="projectRoot"/>.
        /// Empty is valid and yields an empty <paramref name="relative"/>. A path
        /// that resolves outside the project fails. Success stores forward-slash
        /// relative form, including when <paramref name="configured"/> was an
        /// absolute path inside the project.
        /// </summary>
        public static bool TryNormalizeUserModelsCatalogPath(
            string projectRoot, string configured, out string relative, out string error)
        {
            relative = string.Empty;
            error = null;
            if (string.IsNullOrWhiteSpace(configured))
                return true;

            string root = Path.GetFullPath(projectRoot);
            string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string full = Path.IsPathRooted(configured.Trim())
                ? Path.GetFullPath(configured.Trim())
                : Path.GetFullPath(Path.Combine(root, configured.Trim()));

            if (!full.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                error = "Custom models catalog must be inside the project. " +
                        "Use a path relative to the project folder, not a path outside it.";
                return false;
            }

            relative = full.Substring(rootPrefix.Length).Replace('\\', '/');
            if (string.IsNullOrEmpty(relative) || relative.EndsWith("/"))
            {
                error = "Custom models catalog must be a file inside the project, not the project folder itself.";
                relative = string.Empty;
                return false;
            }
            return true;
        }

        // ── Inference engines ──────────────────────────────────────────────────
        //
        // There are deliberately no engine dropdowns here any more. Through
        // protocol v12 this asset carried four (language / STT / TTS / embedding),
        // three of them defaulting to Mock — a question a game developer cannot
        // reasonably answer, whose wrong answer shipped mock engines to
        // production, and which the server can answer itself: the model catalog
        // already states which engine runs each model.
        //
        // The engine set is now derived from the Model Manager's manifest: for
        // every registered model, the engine of its catalog entry.

        /// <summary>
        /// The <c>(role, engine)</c> pairs this project expects to use, derived from
        /// the registered models in <see cref="TryllModelManifest"/>.
        ///
        /// Advisory. It lets the server initialise those engines' backends at
        /// CreateSession rather than at first use; a model's engine always comes
        /// from the server's catalog, so nothing here gates a load. Empty when no
        /// models are registered, which is legal and is exactly what a fresh
        /// project sends.
        ///
        /// Derived rather than configured, so it cannot drift from the models the
        /// project actually references — and there is deliberately no engine control
        /// anywhere in the editor, because which C++ library runs a model is not a
        /// question a game developer can answer.
        /// </summary>
        public (byte Role, byte Engine)[] BuildEngineDeclarations()
            => BuildEngineDeclarations(TryllModelManifest.LoadOrNull());

        /// <summary>
        /// Engine declarations derived from an already-loaded manifest.
        ///
        /// Takes the manifest rather than loading it so that a caller building
        /// several hint lists for one CreateSession resolves it once. Each
        /// <see cref="TryllModelManifest.LoadOrNull"/> that comes back empty logs an
        /// error, and a project with no manifest has one problem, not three.
        /// </summary>
        public (byte Role, byte Engine)[] BuildEngineDeclarations(TryllModelManifest manifest)
        {
            if (manifest == null)
                return System.Array.Empty<(byte, byte)>();

            var seen = new HashSet<(byte, byte)>();
            var result = new List<(byte Role, byte Engine)>();
            foreach (var entry in manifest.Entries)
            {
                if (entry.Registration == TryllModelRegistration.None)
                    continue;
                var pair = ((byte)RoleOrdinalFor(entry.ModelType), (byte)entry.Engine);
                // A duplicate pair is rejected server-side, and several models
                // legitimately share one (role, engine) — dedupe here.
                if (seen.Add(pair))
                    result.Add(pair);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Per-model hotword-bias hints from the manifest, omitting models left on
        /// <c>Default</c> — sparse for the same reason placement is, and because
        /// <c>Default</c> and "absent" mean the same thing to the server.
        ///
        /// Not filtered by model type: an STT model is the only kind the server
        /// reads this for, and a stale entry on another type costs one wire byte
        /// and is ignored. Filtering would need the manifest to be right about
        /// types it cached long ago.
        /// </summary>
        public (string Model, byte Hotwords)[] BuildHotwordHints()
            => BuildHotwordHints(TryllModelManifest.LoadOrNull());

        /// <summary>
        /// Hotword-bias hints derived from an already-loaded manifest.
        ///
        /// Takes the manifest rather than loading it so that a caller building
        /// several hint lists for one CreateSession resolves it once. Each
        /// <see cref="TryllModelManifest.LoadOrNull"/> that comes back empty logs an
        /// error, and a project with no manifest has one problem, not three.
        /// </summary>
        public (string Model, byte Hotwords)[] BuildHotwordHints(TryllModelManifest manifest)
        {
            if (manifest == null)
                return System.Array.Empty<(string, byte)>();

            var result = new List<(string Model, byte Hotwords)>();
            foreach (var entry in manifest.Entries)
            {
                if (entry.Registration == TryllModelRegistration.None)
                    continue;
                if (entry.Hotwords == TryllHotwordBias.Default)
                    continue;
                result.Add((entry.Name, (byte)entry.Hotwords));
            }
            return result.ToArray();
        }

        /// <summary>
        /// Per-model placement hints from the manifest, omitting models left on
        /// <c>Auto</c> — the hint list is sparse by design.
        /// </summary>
        public (string Model, byte Placement)[] BuildPlacementHints()
            => BuildPlacementHints(TryllModelManifest.LoadOrNull());

        /// <summary>
        /// Placement hints derived from an already-loaded manifest.
        ///
        /// Takes the manifest rather than loading it so that a caller building
        /// several hint lists for one CreateSession resolves it once. Each
        /// <see cref="TryllModelManifest.LoadOrNull"/> that comes back empty logs an
        /// error, and a project with no manifest has one problem, not three.
        /// </summary>
        public (string Model, byte Placement)[] BuildPlacementHints(TryllModelManifest manifest)
        {
            if (manifest == null)
                return System.Array.Empty<(string, byte)>();

            var result = new List<(string Model, byte Placement)>();
            foreach (var entry in manifest.Entries)
            {
                if (entry.Registration == TryllModelRegistration.None)
                    continue;
                if (entry.Placement == TryllMemoryPlacement.Auto)
                    continue;
                // VAD entries are download/config metadata, not models the server
                // caches — a VAD detector is built per STT session inside the
                // recognizer, and the server pins it to the CPU deliberately. Sending
                // a hint for one would be a value nothing reads, so the Model Manager
                // does not offer the control and this does not send it. Filtered here
                // as well as hidden there, because a manifest authored before the
                // control was hidden can still carry a stale VAD placement.
                if (string.Equals(entry.ModelType, "vad", StringComparison.OrdinalIgnoreCase))
                    continue;
                result.Add((entry.Name, (byte)entry.Placement));
            }
            return result.ToArray();
        }

        /// <summary>Wire <c>ModelRole</c> ordinal for a catalog model-type string.</summary>
        private static int RoleOrdinalFor(string modelType)
        {
            switch (modelType)
            {
                case "embedding": return 1;
                case "stt":       return 2;
                case "tts":       return 3;
                // A VAD model is loaded as an STT dependency and has no
                // declaration of its own; the server maps Vad onto Stt.
                case "vad":       return 2;
                default:          return 0;   // "language"
            }
        }
    }
}
