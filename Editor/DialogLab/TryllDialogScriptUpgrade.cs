// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Rewrites every Dialog Script in the project in the typed-check form. Loading an
    /// old script already migrates it in memory (<c>TryllDialogScriptAsset.OnAfterDeserialize</c>);
    /// this makes the change land on disk, so version control shows one reviewable diff
    /// instead of scripts converting whenever someone happens to edit them.
    /// Batch-mode entry point: <c>-executeMethod Tryll.Client.Editor.TryllDialogScriptUpgrade.UpgradeAll -quit</c>.
    /// </summary>
    internal static class TryllDialogScriptUpgrade
    {
        [MenuItem("Assets/Tryll/Upgrade Dialog Scripts")]
        public static void UpgradeAll()
        {
            var paths = AssetDatabase.FindAssets("t:" + nameof(TryllDialogScriptAsset))
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            AssetDatabase.ForceReserializeAssets(paths);
            Debug.Log($"[Tryll] Rewrote {paths.Length} Dialog Script(s) with typed checks.");
        }
    }
}
