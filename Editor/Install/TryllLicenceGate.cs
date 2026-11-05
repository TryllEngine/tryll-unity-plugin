// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using Tryll.Client.Internal;
using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Connects the licence to the server launch: the editor starts no Tryll server until the
    /// current licence is accepted. Runtime code cannot reference this assembly, so this class
    /// sets <c>TryllServerProcess.EditorLaunchBlockReason</c> at load. A blocked start opens
    /// the Welcome window once. Batch mode and tryll-dev are never blocked, and players have
    /// no gate (see <c>TryllServerProcess.EditorLaunchGateError</c>).
    /// </summary>
    [InitializeOnLoad]
    internal static class TryllLicenceGate
    {
        static bool s_openPending;

        /// <summary>Test seam: replaces opening the Welcome window after a blocked start.</summary>
        internal static Action DebugOpenWelcomeForTests;

        static TryllLicenceGate()
        {
#if UNITY_EDITOR_WIN
            TryllServerProcess.EditorLaunchBlockReason = BlockReason;
#endif
            TryllLicence.DeleteStaleKeys();
        }

        /// <summary>
        /// Null when the licence is accepted. Otherwise the message for the blocked start; the
        /// Welcome window then opens (or comes to the front) once, on the next editor tick.
        /// </summary>
        internal static string BlockReason()
        {
            if (TryllLicence.Accepted)
                return null;
            ScheduleOpenWelcome();
            return TryllEditorUiContract.ServerNeedsLicence;
        }

        static void ScheduleOpenWelcome()
        {
            if (DebugOpenWelcomeForTests != null)
            {
                DebugOpenWelcomeForTests();
                return;
            }
            // Several windows can fail to start in one tick; open the Welcome window once.
            if (s_openPending || Application.isBatchMode)
                return;
            s_openPending = true;
            EditorApplication.delayCall += () =>
            {
                s_openPending = false;
                TryllWelcomeWindow.Open();
            };
        }
    }
}
