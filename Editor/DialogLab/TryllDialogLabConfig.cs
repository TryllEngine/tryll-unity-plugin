// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using UnityEditor;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// User-per-project Dialog Lab layout and experiment. EditorPrefs, same field
    /// set as Unreal <c>UTryllDialogLabConfig</c>.
    /// </summary>
    internal sealed class TryllDialogLabConfig
    {
        public int Stage;
        public int ViewMode;
        public int Density = 1;
        public float CellWidth = TryllEditorUiContract.CellWidthMedium;
        public float InspectorWidth = TryllEditorUiContract.InspectorDefaultWidth;
        public bool InspectorVisible = true;
        public string ScriptPath = "";
        public string DefaultAgentId = "";
        public int Repeats = 4;
        public int BaseSeed = 1;
        public List<string> Variants = new List<string>();

        const string KeyStage = "Stage";
        const string KeyViewMode = "ViewMode";
        const string KeyDensity = "Density";
        const string KeyCellWidth = "CellWidth";
        const string KeyInspectorWidth = "InspectorWidth";
        const string KeyInspectorVisible = "InspectorVisible";
        const string KeyScriptPath = "ScriptPath";
        const string KeyDefaultAgentId = "DefaultAgentId";
        const string KeyRepeats = "Repeats";
        const string KeyBaseSeed = "BaseSeed";
        const string KeyVariantCount = "VariantCount";

        public static string ProjectPrefix() => TryllEditorPrefs.ProjectPrefix("DialogLab");

        public static bool Exists() => Exists(ProjectPrefix());

        public static bool Exists(string prefix) =>
            EditorPrefs.HasKey(prefix + KeyInspectorWidth) || EditorPrefs.HasKey(prefix + KeyStage);

        public static TryllDialogLabConfig Load()
        {
            string dest = ProjectPrefix();
            string source = TryllEditorPrefs.SourcePrefixIfEmpty(
                dest, TryllEditorPrefs.LegacyHashPrefix("DialogLab"), KeyInspectorWidth, KeyStage);
            var cfg = Load(source);
            if (source != dest)
                cfg.Save(dest);
            return cfg;
        }

        public static TryllDialogLabConfig Load(string prefix)
        {
            int count = EditorPrefs.GetInt(prefix + KeyVariantCount, 0);
            var variants = new List<string>(count);
            for (int i = 0; i < count; i++)
                variants.Add(EditorPrefs.GetString(prefix + "Variant." + i, ""));

            return new TryllDialogLabConfig
            {
                Stage = EditorPrefs.GetInt(prefix + KeyStage, 0),
                // 1 = Grid. 0 (Summary, now Statistics) only when chosen.
                ViewMode = EditorPrefs.GetInt(prefix + KeyViewMode, 1),
                Density = EditorPrefs.GetInt(prefix + KeyDensity, 1),
                CellWidth = EditorPrefs.GetFloat(prefix + KeyCellWidth, TryllEditorUiContract.CellWidthMedium),
                InspectorWidth = EditorPrefs.GetFloat(prefix + KeyInspectorWidth, TryllEditorUiContract.InspectorDefaultWidth),
                InspectorVisible = EditorPrefs.GetBool(prefix + KeyInspectorVisible, true),
                ScriptPath = EditorPrefs.GetString(prefix + KeyScriptPath, ""),
                DefaultAgentId = EditorPrefs.GetString(prefix + KeyDefaultAgentId, ""),
                Repeats = EditorPrefs.GetInt(prefix + KeyRepeats, 4),
                BaseSeed = EditorPrefs.GetInt(prefix + KeyBaseSeed, 1),
                Variants = variants,
            };
        }

        public void Save() => Save(ProjectPrefix());

        public void Save(string prefix)
        {
            EditorPrefs.SetInt(prefix + KeyStage, Stage);
            EditorPrefs.SetInt(prefix + KeyViewMode, ViewMode);
            EditorPrefs.SetInt(prefix + KeyDensity, Density);
            EditorPrefs.SetFloat(prefix + KeyCellWidth, CellWidth);
            EditorPrefs.SetFloat(prefix + KeyInspectorWidth, InspectorWidth);
            EditorPrefs.SetBool(prefix + KeyInspectorVisible, InspectorVisible);
            EditorPrefs.SetString(prefix + KeyScriptPath, ScriptPath ?? "");
            EditorPrefs.SetString(prefix + KeyDefaultAgentId, DefaultAgentId ?? "");
            EditorPrefs.SetInt(prefix + KeyRepeats, Repeats);
            EditorPrefs.SetInt(prefix + KeyBaseSeed, BaseSeed);
            EditorPrefs.SetInt(prefix + KeyVariantCount, Variants?.Count ?? 0);
            if (Variants == null)
                return;
            for (int i = 0; i < Variants.Count; i++)
                EditorPrefs.SetString(prefix + "Variant." + i, Variants[i] ?? "");
        }
    }
}
