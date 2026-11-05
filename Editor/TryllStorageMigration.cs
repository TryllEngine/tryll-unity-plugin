// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Tryll.Client;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Moves a project's storage folder from under <c>Assets/</c> (or any other
    /// configured path) to <see cref="TryllRuntimeSettings.DefaultStorageDataFolder"/>,
    /// dropping Unity <c>.meta</c> files so the Asset Database stops tracking them.
    /// </summary>
    internal static class TryllStorageMigration
    {
        /// <summary>
        /// Move the configured storage folder to <c>TryllStorage/</c> at the project
        /// root, update the setting, and restart a managed server if one is running.
        /// </summary>
        public static bool TryMoveToTryllStorage(TryllRuntimeSettings settings, out string error)
        {
            error = null;
            if (settings == null)
            {
                error = "No TryllRuntimeSettings asset.";
                return false;
            }

            string projectRoot = TryllRuntimeSettings.ProjectRootDirectory();
            string configured = string.IsNullOrWhiteSpace(settings.StorageDataFolder)
                ? string.Empty
                : settings.StorageDataFolder.Trim();

            string sourceFull;
            if (string.IsNullOrEmpty(configured))
            {
                error = "Storage data folder is empty; nothing to move.";
                return false;
            }

            sourceFull = Path.IsPathRooted(configured)
                ? Path.GetFullPath(configured)
                : Path.GetFullPath(Path.Combine(projectRoot, configured));

            string destRel = TryllRuntimeSettings.DefaultStorageDataFolder;
            string destFull = Path.GetFullPath(Path.Combine(projectRoot, destRel));

            if (string.Equals(
                    sourceFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    destFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
            {
                settings.StorageDataFolder = destRel;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                return true;
            }

            if (!Directory.Exists(sourceFull))
            {
                // Setting points under Assets/ but the folder was never created —
                // just retarget the setting.
                settings.StorageDataFolder = destRel;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                RelaunchIfNeeded();
                return true;
            }

            if (!IsSafeToMove(projectRoot, sourceFull, destFull, out error))
                return false;

            try
            {
                Directory.CreateDirectory(destFull);
                CopyTreeDroppingMeta(sourceFull, destFull);
                // Prefer deleting via AssetDatabase when the source is under Assets/
                // so .meta orphans are cleaned up with the folder.
                string sourceAssetPath = AbsoluteToAssetPath(projectRoot, sourceFull);
                if (!string.IsNullOrEmpty(sourceAssetPath) && AssetDatabase.IsValidFolder(sourceAssetPath))
                {
                    AssetDatabase.DeleteAsset(sourceAssetPath);
                }
                else
                {
                    Directory.Delete(sourceFull, recursive: true);
                }

                settings.StorageDataFolder = destRel;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                RelaunchIfNeeded();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// The move copies then deletes the source. Refuse the project root,
        /// <c>Assets</c>, <c>Assets/StreamingAssets</c>, anything outside the
        /// project, and a source that contains the destination.
        /// </summary>
        private static bool IsSafeToMove(
            string projectRoot, string sourceFull, string destFull, out string error)
        {
            error = null;
            string Norm(string path) => Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string root = Norm(projectRoot);
            string src = Norm(sourceFull);
            string dest = Norm(destFull);
            string sep = Path.DirectorySeparatorChar.ToString();

            bool underRoot = src.StartsWith(root + sep, StringComparison.OrdinalIgnoreCase);
            if (!underRoot)
            {
                error = "Refusing to move a storage folder outside the Unity project.";
                return false;
            }
            if (string.Equals(src, root, StringComparison.OrdinalIgnoreCase))
            {
                error = "Refusing to move the Unity project root.";
                return false;
            }

            string assets = Norm(Path.Combine(root, "Assets"));
            string streaming = Norm(Path.Combine(assets, "StreamingAssets"));
            if (string.Equals(src, assets, StringComparison.OrdinalIgnoreCase)
                || string.Equals(src, streaming, StringComparison.OrdinalIgnoreCase))
            {
                error = "Refusing to move Assets or StreamingAssets. Point storage at the Tryll folder inside them, then move again.";
                return false;
            }
            if (dest.StartsWith(src + sep, StringComparison.OrdinalIgnoreCase))
            {
                error = "Refusing to move a folder into itself.";
                return false;
            }
            return true;
        }

        private static void RelaunchIfNeeded()
        {
#if UNITY_EDITOR_WIN
            if (!TryllServerProcess.IsRunning)
                return;
            var settings = TryllRuntimeSettings.Load();
            if (!TryllServerProcess.TryLaunch(settings, out string launchError)
                && !string.IsNullOrEmpty(launchError))
            {
                Debug.LogError("[Tryll] Storage folder moved, but the server did not restart: " + launchError);
            }
#endif
        }

        private static void CopyTreeDroppingMeta(string source, string dest)
        {
            string prefix = Path.GetFullPath(source)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string rel = Path.GetFullPath(file).Substring(prefix.Length);
                string target = Path.Combine(dest, rel);
                string parent = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(parent))
                    Directory.CreateDirectory(parent);
                File.Copy(file, target, overwrite: true);
            }
        }

        private static string AbsoluteToAssetPath(string projectRoot, string absolute)
        {
            string rootPrefix = Path.GetFullPath(projectRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(absolute);
            if (!full.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                return null;
            string relative = full.Substring(rootPrefix.Length).Replace('\\', '/');
            if (!relative.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(relative, "Assets", StringComparison.OrdinalIgnoreCase))
                return null;
            return relative.TrimEnd('/');
        }
    }
}
