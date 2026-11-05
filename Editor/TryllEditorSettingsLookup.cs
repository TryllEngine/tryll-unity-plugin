// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// How an editor window finds the project's <see cref="TryllRuntimeSettings"/>
    /// when none is assigned: <c>Resources</c> first, then the first asset in the
    /// project. Shared by Chat, Model Manager and Memory.
    /// </summary>
    internal static class TryllEditorSettingsLookup
    {
        /// <summary>
        /// Test-only settings returned instead of the project asset. Must be set
        /// before the window is created — Memory starts its session in OnEnable.
        /// </summary>
        internal static TryllRuntimeSettings DebugOverrideForTests { get; set; }

        public static TryllRuntimeSettings Find()
        {
            if (DebugOverrideForTests != null) return DebugOverrideForTests;

            var settings = Resources.Load<TryllRuntimeSettings>("TryllRuntimeSettings");
            if (settings != null) return settings;

            var guids = AssetDatabase.FindAssets("t:TryllRuntimeSettings");
            if (guids == null || guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<TryllRuntimeSettings>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
