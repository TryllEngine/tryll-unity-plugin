// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Tryll.Client;
using Tryll.Client.Internal;
using Debug = UnityEngine.Debug;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Runs <c>tryll_server --bake</c> on the project storage before a Windows player
    /// build, so every knowledge base's <c>.usearch</c>, <c>.bm25</c> and
    /// <c>.bake.json</c>, and every speech model's <c>bpe.vocab</c>, exist when
    /// <see cref="TryllServerBuildPostprocessor"/> stages storage. A packaged game
    /// (Client Prod) only loads these files, so any bake failure fails the build.
    ///
    /// The server is launched with the editor's own arguments, so it sees the same
    /// storage root, catalogs and llama.cpp backend as the editor session.
    /// </summary>
    internal sealed class TryllDerivedFileBake : IPreprocessBuildWithReport
    {
        // After TryllWorkflowModelPreBuildCheck, which fails faster on a missing model.
        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            var platform = report.summary.platform;
            if (platform != BuildTarget.StandaloneWindows64 && platform != BuildTarget.StandaloneWindows)
                return;

#if !UNITY_EDITOR_WIN
            throw new BuildFailedException(
                "[Tryll] Knowledge base indexes are baked by the bundled Windows server, which " +
                "only runs in a Windows editor. Build the Windows player from a Windows editor.");
#else
            TryllRuntimeSettings.EnsureSavedProjectId();
            var settings = TryllRuntimeSettings.Load();
            string exe = TryllServerProcess.ResolveExePath(settings);
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
            {
                string msg = $"[Tryll] Cannot bake knowledge base indexes: no server at '{exe}'.";
                if (settings == null || settings.AutoLaunchServer)
                    throw new BuildFailedException(msg);
                Debug.LogWarning(msg);
                return;
            }

            string userCatalog = settings != null ? settings.ResolveUserModelsCatalogForLaunch() : string.Empty;
            if (!string.IsNullOrEmpty(userCatalog) && !File.Exists(userCatalog))
                userCatalog = string.Empty;
            string args = TryllServerProcess.BuildLaunchArguments(
                0,
                settings != null ? settings.LlamaCppBackendCliValue() : "vulkan",
                settings != null
                    ? settings.ResolveSaveDataFolderForLaunch()
                    : TryllRuntimeSettings.ResolveSaveDataFolderForLaunch(string.Empty),
                settings != null
                    ? settings.ResolveStorageRootForRuntime()
                    : TryllRuntimeSettings.ResolveStorageRootForRuntime(string.Empty),
                settings != null ? settings.ResolveProjectName() : (Application.productName ?? string.Empty),
                TryllRuntimeSettings.ProjectRootDirectory(),
                userCatalog,
                false,
                settings,
                out _) + " --bake";

            var output = new StringBuilder();
            var psi = new ProcessStartInfo
            {
                FileName               = exe,
                Arguments              = args,
                WorkingDirectory       = Path.GetDirectoryName(exe),
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };
            int exitCode;
            try
            {
                EditorUtility.DisplayProgressBar("Tryll", "Baking knowledge base indexes…", 0.5f);
                using (var process = new Process { StartInfo = psi })
                {
                    process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (output) output.AppendLine(e.Data); };
                    process.ErrorDataReceived  += (_, e) => { if (e.Data != null) lock (output) output.AppendLine(e.Data); };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (exitCode != 0)
            {
                throw new BuildFailedException(
                    $"[Tryll] Baking knowledge base indexes and speech vocabularies failed " +
                    $"(exit code {exitCode}). A packaged game cannot build them, so the build stops.\n{output}");
            }
            Debug.Log($"[Tryll] Baked derived files for the build:\n{output}");
#endif
        }
    }
}
