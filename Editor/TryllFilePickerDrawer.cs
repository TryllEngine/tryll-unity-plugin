// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Inspector property drawer for fields annotated with <see cref="TryllFilePickerAttribute"/>.
    ///
    /// Renders the field as an editable text box (the relative path) plus a small
    /// <c>...</c> browse button that opens a native file-open dialog anchored at the
    /// project's configured storage root
    /// (<see cref="TryllRuntimeSettings.StorageDataFolder"/>; default
    /// <c>TryllStorage/</c> outside <c>Assets/</c>).
    ///
    /// The stored value is always the path <em>relative</em> to that root, using
    /// forward slashes — matching what the server's <c>storage_root</c>-relative
    /// resolution expects.
    ///
    /// <para><b>Link the default file, not a translation.</b> Translations live in a
    /// <c>loc/&lt;language&gt;/</c> folder beside the file you link, and the server
    /// looks there first. Picking <c>deathonset/loc/de/canned.txt</c> would pin the
    /// field to German in every language, so the drawer offers the default path
    /// (<c>deathonset/canned.txt</c>) instead.</para>
    ///
    /// A red-tinted background is drawn when the current value is obviously invalid
    /// (absolute path or contains <c>..</c>), giving instant visual feedback that the
    /// validator will flag the same issue.
    ///
    /// Extension filtering is inferred from the field name to match the server's
    /// extension-to-kind convention:
    /// <list type="bullet">
    ///   <item>Embedded storage field (<c>EmbeddedStringStorage</c>) → <c>.json</c>, <c>.kb.json</c>.</item>
    ///   <item>Voice clip field (<c>ReferenceVoice</c>) → <c>.wav</c>.</item>
    ///   <item>Plain storage fields → <c>.txt</c>, <c>.json</c>, <c>.mm.json</c>.</item>
    /// </list>
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllFilePickerAttribute))]
    internal sealed class TryllFilePickerDrawer : PropertyDrawer
    {
        private const float BrowseButtonWidth = 28f;
        private const float Spacing           = 2f;
        private static readonly Color s_InvalidColor = new Color(1f, 0.35f, 0.35f, 0.4f);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            string currentValue = property.stringValue ?? string.Empty;
            bool isInvalid = IsInvalidPath(currentValue);
            // Typed paths bypass the browse dialog, so catch a translation path here too.
            string pinnedLanguage = isInvalid ? null : LocalizedFolderLanguage(currentValue, out _);

            // ── Draw invalid-path tint ─────────────────────────────────────────
            if (isInvalid)
                EditorGUI.DrawRect(position, s_InvalidColor);

            // ── Layout: [label] [text field ......] [...]  ────────────────────
            // Match EditorGUI.Popup / TryllExitDrawer: PrefixLabel for the label,
            // then indentLevel = 0 for the value column (Unity's built-in pattern).
            Rect valueRect = EditorGUI.PrefixLabel(position, label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect textRect   = new Rect(valueRect.x,
                                       valueRect.y,
                                       valueRect.width - BrowseButtonWidth - Spacing,
                                       valueRect.height);
            Rect buttonRect = new Rect(valueRect.xMax - BrowseButtonWidth,
                                       valueRect.y,
                                       BrowseButtonWidth,
                                       valueRect.height);

            // ── Editable text field ────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            string typed = EditorGUI.DelayedTextField(textRect, currentValue);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = OfferDefaultPath(typed.Replace('\\', '/'));

            // ── Browse button ─────────────────────────────────────────────────
            var settings = AssetDatabase.LoadAssetAtPath<TryllRuntimeSettings>(TryllRuntimeSettings.EditorAssetPath)
                           ?? TryllRuntimeSettings.Load();
            bool storageRootOk = settings != null && TryllRuntimeSettings.TryNormalizeStorageDataFolder(
                TryllRuntimeSettings.ProjectRootDirectory(),
                settings.StorageDataFolder,
                out _,
                out _);

            GUIContent browseContent = new GUIContent("...",
                storageRootOk
                    ? "Browse for a storage file under the project storage root (TryllStorage by default)."
                    : "Fix Storage Data Folder in Project Settings → Tryll Client first " +
                      "(must be outside Assets/; use Move to TryllStorage).");

            using (new EditorGUI.DisabledGroupScope(!storageRootOk))
            {
                if (GUI.Button(buttonRect, browseContent))
                    ScheduleBrowse(property);
            }

            if (pinnedLanguage != null)
            {
                EditorGUI.LabelField(
                    new Rect(position.x, position.y, position.width, position.height),
                    GUIContent.none,
                    new GUIContent(string.Empty,
                        $"This links a '{pinnedLanguage}' translation directly, so it is used in " +
                        "every language. Link the default file instead — Tryll finds the " +
                        "'loc/<language>/' copy on its own."));
                EditorGUI.DrawRect(
                    new Rect(position.x, position.yMax - 2f, position.width, 2f),
                    s_LocalePinnedColor);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static readonly Color s_LocalePinnedColor = new Color(0.95f, 0.70f, 0.25f, 0.9f);

        // ── Browse ────────────────────────────────────────────────────────────

        // Opening a modal file dialog from inside OnGUI interrupts Unity's layout
        // group and causes "EndLayoutGroup: BeginLayoutGroup must be called first."
        // Deferring to EditorApplication.delayCall lets the current GUI frame finish
        // before the dialog is shown.
        private void ScheduleBrowse(SerializedProperty property)
        {
            // Capture everything we need; SerializedProperty is not safe to hold
            // across frames, so we snapshot the serialized object + property path.
            var serializedObject = property.serializedObject;
            string propertyPath  = property.propertyPath;
            string root          = ResolveStorageRoot();
            string[] filters     = BuildFilters();
            if (!Directory.Exists(root))
            {
                try { Directory.CreateDirectory(root); }
                catch { /* browse can still open the parent */ }
            }
            string startDir      = Directory.Exists(root)
                ? root
                : Directory.GetParent(Application.dataPath).FullName;

            EditorApplication.delayCall += () =>
                OpenAndApply(serializedObject, propertyPath, root, startDir, filters);
        }

        private static void OpenAndApply(
            SerializedObject serializedObject,
            string           propertyPath,
            string           root,
            string           startDir,
            string[]         filters)
        {
            string picked = TryllEditorPrompts.OpenFilePanelWithFilters(
                "Select storage file", startDir, filters);

            if (string.IsNullOrEmpty(picked))
                return;

            // ── Make relative to root ─────────────────────────────────────────
            string relative = MakeRelative(root, picked);

            // Guard: reject if the result is still an absolute path (e.g. different
            // drive on Windows) or otherwise invalid (starts with "..", contains
            // forbidden characters).
            if (Path.IsPathRooted(relative) || IsInvalidPath(relative))
            {
                Debug.LogWarning(
                    $"[Tryll] Selected file is outside the storage root '{root}'. " +
                    "Set Project Settings → Tryll Client → Storage Data Folder to the folder that " +
                    "contains your storage files (default TryllStorage). File not stored.");
                return;
            }

            relative = OfferDefaultPath(relative);

            serializedObject.Update();
            SerializedProperty prop = serializedObject.FindProperty(propertyPath);
            if (prop != null)
            {
                prop.stringValue = relative;
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// When <paramref name="relative"/> points inside a <c>loc/&lt;language&gt;/</c>
        /// folder, offer to store the default path instead — the file the
        /// translation belongs to.
        ///
        /// Asks rather than rewriting silently: pinning one field to one language
        /// is occasionally deliberate (a character who always speaks French), and
        /// a dialog that defaults to the right answer costs one keypress while a
        /// silent rewrite would be unexplainable.
        /// </summary>
        private static string OfferDefaultPath(string relative)
        {
            string language = LocalizedFolderLanguage(relative, out string defaultPath);
            if (language == null) return relative;

            bool useDefault = TryllEditorPrompts.Confirm(
                "Tryll — link the default file?",
                $"'{relative}' is a translation inside 'loc/{language}/'.\n\n" +
                $"Link the default file instead?\n\n    {defaultPath}\n\n" +
                "Tryll looks in 'loc/<language>/' beside the linked file automatically, " +
                $"so linking the default picks up this '{language}' translation when the " +
                "session runs in that language — and falls back to the default elsewhere.\n\n" +
                $"Linking the translation directly would use '{language}' in every language.",
                "Link default file", "Keep this translation");

            return useDefault ? defaultPath : relative;
        }

        /// <summary>
        /// Language code when <paramref name="relative"/> sits inside a
        /// <c>loc/&lt;language&gt;/</c> folder, else null. <paramref name="defaultPath"/>
        /// receives the path with the <c>loc/&lt;language&gt;</c> pair removed — the
        /// file this is a translation of.
        /// </summary>
        private static string LocalizedFolderLanguage(string relative, out string defaultPath)
        {
            defaultPath = null;
            if (string.IsNullOrEmpty(relative)) return null;

            string[] parts = relative.Split('/');
            // Need at least "loc/<lang>/<file>".
            for (int i = 0; i + 2 < parts.Length; i++)
            {
                if (!string.Equals(parts[i], LocFolder, System.StringComparison.Ordinal))
                    continue;

                string language = parts[i + 1];
                if (string.IsNullOrEmpty(language)) continue;

                var rebuilt = new List<string>(parts.Length - 2);
                for (int j = 0; j < parts.Length; j++)
                    if (j != i && j != i + 1) rebuilt.Add(parts[j]);

                defaultPath = string.Join("/", rebuilt);
                return language;
            }
            return null;
        }

        /// <summary>Translation folder name; mirrors kLocFolder in StoragePathResolver.h.</summary>
        private const string LocFolder = "loc";

        // ── Storage root resolution ───────────────────────────────────────────

        private static string ResolveStorageRoot()
        {
            // Load from the asset on disk so edits in Project Settings are picked up
            // immediately. TryllRuntimeSettings.Load() caches the first Resources.Load
            // result and can return stale values after the asset is saved.
            var settings = AssetDatabase.LoadAssetAtPath<TryllRuntimeSettings>(TryllRuntimeSettings.EditorAssetPath)
                           ?? TryllRuntimeSettings.Load();
            string configured = settings != null ? settings.StorageDataFolder : string.Empty;
            return TryllRuntimeSettings.ResolveStorageRootForRuntime(configured);
        }

        // ── Extension filter ─────────────────────────────────────────────────

        private string[] BuildFilters()
        {
            bool isEmbedded = string.Equals(
                fieldInfo?.Name, "EmbeddedStringStorage", StringComparison.OrdinalIgnoreCase);

            if (isEmbedded)
            {
                return new[]
                {
                    "Embedded storage config",  "json",
                    "All files",                "*",
                };
            }

            // LoRA adapters (TryllLoraAdapter.Path, inside a node's Adaptation).
            if (fieldInfo?.DeclaringType == typeof(TryllLoraAdapter))
            {
                return new[]
                {
                    "LoRA adapters",  "gguf",
                    "All files",      "*",
                };
            }

            // Voice profile clips (TryllVoiceProfileData / TryllDeliveryMode.ReferenceVoice).
            if (string.Equals(fieldInfo?.Name, "ReferenceVoice", StringComparison.Ordinal))
            {
                return new[]
                {
                    "Voice clips",  "wav",
                    "All files",    "*",
                };
            }

            return new[]
            {
                "Plain storage files",  "txt,json,mm.json",
                "All files",            "*",
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string MakeRelative(string root, string absolutePath)
        {
            // Path.GetRelativePath correctly handles Windows drive-letter mismatches
            // (returns the absolute path unchanged) and UNC paths, whereas the old
            // Uri-based approach could silently produce rooted results on different drives.
            string relative = Path.GetRelativePath(root, absolutePath);

            // Normalise to forward slashes for cross-platform server compatibility.
            return relative.Replace('\\', '/');
        }

        private static bool IsInvalidPath(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            // Absolute path.
            if (value[0] == '/' || value[0] == '\\') return true;
            if (value.Length >= 2 && value[1] == ':') return true;

            // Directory traversal.
            if (value.Contains("..")) return true;

            return false;
        }
    }
}
