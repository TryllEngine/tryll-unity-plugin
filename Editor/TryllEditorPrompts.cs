// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Modal prompts and file pickers the editor plugin shows. Every
    /// <c>EditorUtility.DisplayDialog</c> / file-panel call under <c>Editor/</c>
    /// goes through here so window tests can record a prompt and script its
    /// answer instead of blocking (or auto-answering) the batchmode editor.
    /// </summary>
    internal interface ITryllEditorPrompts
    {
        bool Confirm(string title, string message, string ok, string cancel);
        void Notify(string title, string message, string ok);
        string SaveFilePanel(string title, string directory, string defaultName, string extension);
        string OpenFilePanel(string title, string directory, string extension);
        string OpenFilePanelWithFilters(string title, string directory, string[] filters);
    }

    internal static class TryllEditorPrompts
    {
        sealed class EditorUtilityPrompts : ITryllEditorPrompts
        {
            // Batch mode (headless Dialog Lab, CI) has nobody to answer a dialog: log instead,
            // and a confirmation is declined.
            public bool Confirm(string title, string message, string ok, string cancel)
            {
                if (Application.isBatchMode)
                {
                    Debug.LogWarning($"{title}: {message} (batch mode: declined)");
                    return false;
                }
                return EditorUtility.DisplayDialog(title, message, ok, cancel);
            }

            public void Notify(string title, string message, string ok)
            {
                if (Application.isBatchMode)
                {
                    Debug.LogError($"{title}: {message}");
                    return;
                }
                EditorUtility.DisplayDialog(title, message, ok);
            }

            public string SaveFilePanel(string title, string directory, string defaultName, string extension)
                => EditorUtility.SaveFilePanel(title, directory, defaultName, extension);

            public string OpenFilePanel(string title, string directory, string extension)
                => EditorUtility.OpenFilePanel(title, directory, extension);

            public string OpenFilePanelWithFilters(string title, string directory, string[] filters)
                => EditorUtility.OpenFilePanelWithFilters(title, directory, filters);
        }

        static readonly ITryllEditorPrompts Default = new EditorUtilityPrompts();

        /// <summary>Test-only replacement; null means the real editor dialogs.</summary>
        internal static ITryllEditorPrompts DebugOverrideForTests { get; set; }

        static ITryllEditorPrompts Active => DebugOverrideForTests ?? Default;

        public static bool Confirm(string title, string message, string ok, string cancel)
            => Active.Confirm(title, message, ok, cancel);

        public static void Notify(string title, string message, string ok = "OK")
            => Active.Notify(title, message, ok);

        public static string SaveFilePanel(string title, string directory, string defaultName, string extension)
            => Active.SaveFilePanel(title, directory, defaultName, extension);

        public static string OpenFilePanel(string title, string directory, string extension)
            => Active.OpenFilePanel(title, directory, extension);

        public static string OpenFilePanelWithFilters(string title, string directory, string[] filters)
            => Active.OpenFilePanelWithFilters(title, directory, filters);
    }
}
