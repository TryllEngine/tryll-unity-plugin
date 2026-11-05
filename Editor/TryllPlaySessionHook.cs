// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Domain reload off keeps the editor <see cref="TryllClient"/> alive across
    /// Play mode. Game-script calls are owned by the play-session token and fail
    /// when play ends. Editor-window calls use <see cref="TryllClient.AsEditorCall"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class TryllPlaySessionHook
    {
        static TryllPlaySessionHook()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                TryllClient.Instance?.EndPlaySession();
        }
    }
}
