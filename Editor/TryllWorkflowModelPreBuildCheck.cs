// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Fails a Windows Standalone player build when any <see cref="TryllWorkflowAsset"/>
    /// or <see cref="TryllVoiceProfile"/> references a model that is not marked <see cref="TryllModelRegistration.Production"/>
    /// (so it would not be staged by <see cref="TryllServerBuildPostprocessor"/>), or when
    /// an STT reference is present without a Production VAD companion, or when a
    /// Production model (or a shared asset it needs) is not downloaded.
    ///
    /// Covers every workflow asset and voice profile asset in the project.
    /// </summary>
    internal sealed class TryllWorkflowModelPreBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var platform = report.summary.platform;
            if (platform != BuildTarget.StandaloneWindows64 && platform != BuildTarget.StandaloneWindows)
                return;

            LogMultiManifestIfNeeded();

            FailIfStorageDataFolderUnderAssets();

            var manifest = TryllModelManifest.FindProjectManifest();
            FailIfManifestUnreachableAtRuntime(manifest);
            var violations = new List<TryllWorkflowModelRefs.Violation>();
            var allRefs = new List<TryllWorkflowModelRefs.ModelRef>();

            string[] guids = AssetDatabase.FindAssets("t:TryllWorkflowAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TryllWorkflowAsset>(path);
                if (asset?.Graph == null) continue;

                allRefs.AddRange(TryllWorkflowModelRefs.Collect(asset.Graph));
                violations.AddRange(
                    TryllWorkflowModelRefs.FindNonProductionRefs(asset.Graph, manifest, path));
            }

            // Voice profiles carry the TTS model since protocol v27.
            foreach (string guid in AssetDatabase.FindAssets("t:TryllVoiceProfile"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var voice = AssetDatabase.LoadAssetAtPath<TryllVoiceProfile>(path);
                if (voice?.Data == null) continue;

                allRefs.AddRange(TryllWorkflowModelRefs.Collect(voice.Data));
                violations.AddRange(
                    TryllWorkflowModelRefs.FindNonProductionRefs(voice.Data, manifest, path));
            }

            string vadReason = null;
            TryllWorkflowModelRefs.NeedsProductionVad(allRefs, manifest, out vadReason);

            if (violations.Count > 0 || !string.IsNullOrEmpty(vadReason))
            {
                throw new BuildFailedException(
                    TryllWorkflowModelRefs.FormatBuildFailure(violations, vadReason));
            }

            // Every Production model must be downloaded, or the postprocessor would fail
            // after Unity has already built and written the player.
            TryllServerBuildPostprocessor.FailIfProductionModelsUnavailable();
        }

        /// <summary>
        /// Fail when Storage Data Folder resolves under Assets/ — Unity would
        /// track model downloads and derived files there.
        /// </summary>
        internal static void FailIfStorageDataFolderUnderAssets()
        {
            var settings = TryllRuntimeSettings.Load();
            string configured = settings != null ? settings.StorageDataFolder : string.Empty;
            if (TryllRuntimeSettings.TryNormalizeStorageDataFolder(
                    TryllRuntimeSettings.ProjectRootDirectory(), configured, out _, out string error))
                return;

            throw new BuildFailedException(
                "[Tryll] " + (error ?? "Invalid Storage Data Folder.") +
                "\nOpen Project Settings → Tryll Client and use Move to TryllStorage.");
        }

        /// <summary>
        /// Fail the build when the manifest would not be reachable from the player *and*
        /// it has something to lose.
        ///
        /// A built player has no AssetDatabase, so <see cref="TryllModelManifest.LoadOrNull"/>
        /// can only reach the manifest through <c>Resources.Load</c>. A manifest outside a
        /// <c>Resources</c> folder is therefore absent at runtime, and the session ships
        /// without the RAM/VRAM placement the project authored or the engine pre-warm.
        ///
        /// **Why this fails the build rather than warning.** Nothing breaks — the engine
        /// hint is advisory, so models still load on the engines their catalog entries
        /// name. What happens is that the editor and the shipped game behave differently,
        /// silently, in a way a developer discovers as a performance mystery months later.
        /// That divergence is the exact thing this feature set out to remove, so it gets
        /// the same treatment as a non-Production model reference.
        ///
        /// **Why it is gated on content.** Failing a build over a lost *hint* is too harsh
        /// when there is no hint to lose. A manifest with nothing registered and no
        /// authored placement loses nothing by being unreachable, so it says nothing —
        /// which also keeps the message meaningful when it does fire.
        /// </summary>
        internal static void FailIfManifestUnreachableAtRuntime(TryllModelManifest manifest)
        {
            if (manifest == null) return;   // no manifest at all is a different (non-)problem

            string path = AssetDatabase.GetAssetPath(manifest);
            if (IsRuntimeReachable(path)) return;

            // What would actually be lost. Registration matters because an unregistered
            // model is never loaded, so its placement is moot either way.
            int registered = 0;
            int placed = 0;
            foreach (var e in manifest.Entries)
            {
                if (e.Registration == TryllModelRegistration.None) continue;
                registered++;
                if (e.Placement != TryllMemoryPlacement.Auto) placed++;
            }
            if (registered == 0 && placed == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("[Tryll] The model manifest is not reachable from a built player.");
            sb.AppendLine();
            sb.Append("  Asset:    ").AppendLine(path);
            sb.Append("  Expected: ").Append(TryllModelManifest.EditorAssetPath)
              .Append("  (or any Resources folder, named '")
              .Append(TryllModelManifest.RuntimeResourceName).AppendLine("')");
            sb.AppendLine();
            sb.Append("A player has no AssetDatabase, so the manifest can only be loaded ")
              .Append("from Resources. Building now would ship a game that ignores ")
              .Append(placed).Append(" authored RAM/VRAM placement setting(s) and pre-warms ")
              .Append("no engines, while the editor honours both — a difference you would ")
              .AppendLine("not see until someone profiled the shipped build.");
            sb.AppendLine();
            sb.Append("Fix: move the asset into a Resources folder (Assets/Resources/), ")
              .Append("keeping the file name. Nothing else references it by path.");
            throw new BuildFailedException(sb.ToString());
        }

        /// <summary>
        /// Whether <c>Resources.Load(RuntimeResourceName)</c> would find this asset.
        ///
        /// Requires the asset to sit *directly* in a <c>Resources</c> folder under the
        /// expected name: <c>Resources/Sub/TryllModelManifest.asset</c> loads as
        /// "Sub/TryllModelManifest", not "TryllModelManifest", so it would not be found.
        /// </summary>
        internal static bool IsRuntimeReachable(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;

            const string marker = "/Resources/";
            int at = assetPath.LastIndexOf(marker, System.StringComparison.Ordinal);
            if (at < 0) return false;

            string tail = assetPath.Substring(at + marker.Length);
            if (tail.Contains("/")) return false;   // nested under Resources — not by name

            return System.IO.Path.GetFileNameWithoutExtension(tail)
                == TryllModelManifest.RuntimeResourceName;
        }

        /// <summary>
        /// Non-blocking: log an error listing every manifest when more than one exists,
        /// then continue with the sorted-first asset.
        /// </summary>
        internal static void LogMultiManifestIfNeeded()
        {
            var all = TryllModelManifest.FindProjectManifests();
            if (all.Count <= 1) return;

            var sb = new StringBuilder();
            sb.Append("[Tryll] Multiple TryllModelManifest assets found (").Append(all.Count)
              .Append("). Using '").Append(all[0].path)
              .Append("' for packaging; remove the extras:\n");
            foreach (var (path, _) in all)
                sb.Append("  • ").Append(path).Append('\n');
            Debug.LogError(sb.ToString());
        }
    }
}
