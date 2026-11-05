// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using Tryll.Client;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Every Text ID on a workflow against every language the provider lists.
    /// A missing cell is red. Language fallback is not applied here.
    /// </summary>
    sealed class TryllTextCheckWindow : EditorWindow
    {
        TryllWorkflowAsset _asset;
        Vector2 _scroll;

        public static void Open(TryllWorkflowAsset asset)
        {
            var window = GetWindow<TryllTextCheckWindow>("Check texts");
            window._asset = asset;
            window.Show();
        }

        void OnGUI()
        {
            _asset = (TryllWorkflowAsset)EditorGUILayout.ObjectField("Workflow", _asset, typeof(TryllWorkflowAsset), false);
            if (_asset == null)
            {
                EditorGUILayout.LabelField("Select a workflow asset.");
                return;
            }

            var provider = TryllRuntimeSettings.Load().TextProvider;
            if (provider is not ITryllTextProviderEditor editor)
            {
                string name = provider == null ? "none" : provider.GetType().Name;
                EditorGUILayout.HelpBox($"No preview — {name} has no editor support.", MessageType.None);
                return;
            }

            var locales = TryllTextPreviewCache.Locales(editor);
            var ids = TryllTextResolver.CollectTextIds(_asset.Graph);
            string table = _asset.TextTable ?? "";
            EditorGUILayout.LabelField("Table", string.IsNullOrEmpty(table) ? "(none)" : table);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (ids.Count == 0)
                EditorGUILayout.LabelField("This workflow has no Localized fields.");

            foreach (var entry in ids)
            {
                EditorGUILayout.LabelField($"{entry.NodeName} · {entry.FieldPath}", EditorStyles.boldLabel);
                if (string.IsNullOrEmpty(entry.Id))
                {
                    EditorGUILayout.HelpBox("Enter a Text ID", MessageType.Error);
                    continue;
                }
                EditorGUILayout.LabelField("Text ID", entry.Id);
                if (locales == null || locales.Count == 0)
                {
                    EditorGUILayout.HelpBox("The provider lists no languages.", MessageType.Warning);
                    continue;
                }
                foreach (var locale in locales)
                {
                    string text = TryllTextPreviewCache.Text(editor, table, entry.Id, locale);
                    if (text == null)
                    {
                        string problem = (provider as ITryllTextProviderDiagnostics)?.Diagnose(table, locale);
                        EditorGUILayout.HelpBox(
                            string.IsNullOrEmpty(problem) ? locale + ": missing" : locale + ": missing — " + problem,
                            MessageType.Error);
                    }
                    else
                        EditorGUILayout.LabelField(locale, FirstLine(text));
                }
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();
        }

        static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int nl = text.IndexOf('\n');
            string line = nl < 0 ? text : text.Substring(0, nl);
            return line.Length > 80 ? line.Substring(0, 80) + "…" : line;
        }
    }

    /// <summary>
    /// Drop cached previews and repaint when text data is imported: a JSON file,
    /// a Unity Localization table, or a Tryll text provider. Other assets are ignored.
    /// </summary>
    sealed class TryllTextAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!TouchesText(imported) && !TouchesText(deleted) && !TouchesText(moved))
                return;
            TryllTextEditorHooks.BumpRevision();
            InternalEditorUtility.RepaintAllViews();
        }

        static bool TouchesText(string[] paths)
        {
            if (paths == null) return false;
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                if (path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
                    return true;
                if (!path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                // Name check, so this compiles without com.unity.localization.
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == null) continue;
                if (typeof(TryllTextProviderAsset).IsAssignableFrom(type)
                    || type.Name == "StringTable"
                    || type.Name == "SharedTableData"
                    || type.Name == "StringTableCollection")
                    return true;
            }
            return false;
        }
    }

    [InitializeOnLoad]
    static class TryllTextEditorHooksInit
    {
        static TryllTextEditorHooksInit()
        {
            TryllTextEditorHooks.Open = Open;
            // An undone edit of a provider or table must show in the preview at once.
            Undo.undoRedoPerformed += TryllTextEditorHooks.BumpRevision;
        }

        static void Open(ITryllTextProviderEditor provider, string table, string id)
        {
            if (provider is TryllJsonTextProvider json)
            {
                var asset = json.AssetFor(id);
                if (asset == null) return;
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
                return;
            }

            if (string.IsNullOrEmpty(table)) return;
            foreach (var guid in AssetDatabase.FindAssets(table))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var loaded = AssetDatabase.LoadMainAssetAtPath(path);
                if (loaded != null && loaded.name == table)
                {
                    EditorGUIUtility.PingObject(loaded);
                    Selection.activeObject = loaded;
                    return;
                }
            }
        }
    }
}
