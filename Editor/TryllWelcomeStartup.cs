// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Opens the Welcome window once per editor session when this project needs it:
    /// <list type="bullet">
    /// <item>the package version changed since the window last opened in this project
    /// (first import, or an update);</item>
    /// <item>setup is not finished (the current licence is not accepted, or a released
    /// package without its server);</item>
    /// <item>the person ticked "Show this window at startup".</item>
    /// </list>
    /// Never in batch mode, so CI and command-line builds are unaffected.
    /// </summary>
    [InitializeOnLoad]
    internal static class TryllWelcomeStartup
    {
        const string SessionKey = "Tryll.Welcome.CheckedThisSession";
        const string LastShownVersionField = "LastShownVersion";
        const string ShowAtStartupField = "ShowAtStartup";

        static TryllWelcomeStartup()
        {
            if (Application.isBatchMode)
                return;
            EditorApplication.delayCall += MaybeOpen;
        }

        /// <summary>Per-project choice ("Show this window at startup").</summary>
        internal static bool ShowAtStartup
        {
            get => EditorPrefs.GetBool(TryllEditorPrefs.ProjectPrefix("Welcome") + ShowAtStartupField, false);
            set => EditorPrefs.SetBool(TryllEditorPrefs.ProjectPrefix("Welcome") + ShowAtStartupField, value);
        }

        static string LastShownVersion
        {
            get => EditorPrefs.GetString(TryllEditorPrefs.ProjectPrefix("Welcome") + LastShownVersionField, "");
            set => EditorPrefs.SetString(TryllEditorPrefs.ProjectPrefix("Welcome") + LastShownVersionField, value);
        }

        static void MaybeOpen()
        {
            // InitializeOnLoad runs after every domain reload; decide once per session.
            if (SessionState.GetBool(SessionKey, false))
                return;
            SessionState.SetBool(SessionKey, true);

            EnsureProjectSettings();

            if (!ShouldOpen(
                    versionChanged: LastShownVersion != TryllVersion.Version,
                    licenceAccepted: TryllLicence.Accepted,
                    serverState: TryllServerInstaller.State(TryllServerPin.LoadForPackage(), out _),
                    showAtStartup: ShowAtStartup))
                return;

            LastShownVersion = TryllVersion.Version;
            TryllWelcomeWindow.Open();
        }

        /// <summary>
        /// Every Tryll window needs a <see cref="TryllRuntimeSettings"/> asset, and a new project
        /// has none. Create it where Project Settings &gt; Tryll Client would. Called at editor
        /// startup and when the Welcome window is opened from the menu — never from tests,
        /// so the test host project stays untouched.
        /// </summary>
        internal static void EnsureProjectSettings()
        {
            if (Application.isBatchMode || TryllEditorSettingsLookup.Find() != null)
                return;
            TryllRuntimeSettingsProvider.LoadOrCreateSettings();
            TryllLog.Info($"Created {TryllRuntimeSettings.EditorAssetPath} with default settings " +
                          "(Project Settings > Tryll Client).");
        }

        internal static bool ShouldOpen(bool versionChanged, bool licenceAccepted,
                                        TryllServerInstallState serverState, bool showAtStartup)
        {
            if (versionChanged || showAtStartup)
                return true;
            if (!licenceAccepted)
                return true;
            return serverState == TryllServerInstallState.NotInstalled;
        }
    }
}
