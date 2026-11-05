// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;

namespace Tryll.Client.Editor.AgentLog
{
    /// <summary>
    /// User-per-project Agent Log layout and retention. Legacy machine-global
    /// <c>Tryll.AgentLog.*</c> keys are copied once into the project prefix.
    /// </summary>
    internal sealed class AgentLogConfig
    {
        public const string LegacyPrefix = "Tryll.AgentLog.";

        static readonly string[] FieldKeys =
        {
            "MaxCompletedTurnsPerAgent",
            "MaxObservationsPerAgent",
            "MaxRawSessionObservations",
            "AgentListWidth",
            "InspectorWidth",
            "InspectorExpanded",
            "CoalesceAnswerText",
            "CollapseCompletedTurns",
        };

        public int MaxCompletedTurnsPerAgent = 200;
        public int MaxObservationsPerAgent = 4000;
        public int MaxRawSessionObservations = 4000;

        public float AgentListWidth = 180f;
        public float InspectorWidth = 320f;
        public bool InspectorExpanded = false;
        public bool CoalesceAnswerText = true;
        /// <summary>Collapse completed turns the user has not manually expanded.</summary>
        public bool CollapseCompletedTurns = true;

        public static string ProjectPrefix() => TryllEditorPrefs.ProjectPrefix("AgentLog");

        public static AgentLogConfig Load()
        {
            string dest = ProjectPrefix();
            string hashLegacy = TryllEditorPrefs.LegacyHashPrefix("AgentLog");
            if (!HasAny(dest) && dest != hashLegacy && HasAny(hashLegacy))
                return Load(dest, hashLegacy);
            return Load(dest, LegacyPrefix);
        }

        /// <summary>Test seam. Migrates from <paramref name="legacyPrefix"/> onto
        /// <paramref name="prefix"/> when the project keys are still empty.</summary>
        public static AgentLogConfig Load(string prefix, string legacyPrefix)
        {
            string source = prefix;
            bool migrated = false;
            if (!HasAny(prefix))
            {
                if (HasAny(legacyPrefix))
                {
                    source = legacyPrefix;
                    migrated = true;
                }
            }

            var cfg = Read(source);
            if (migrated)
                cfg.Save(prefix);
            return cfg;
        }

        public void Save() => Save(ProjectPrefix());

        public void Save(string prefix)
        {
            EditorPrefs.SetInt(prefix + "MaxCompletedTurnsPerAgent", MaxCompletedTurnsPerAgent);
            EditorPrefs.SetInt(prefix + "MaxObservationsPerAgent", MaxObservationsPerAgent);
            EditorPrefs.SetInt(prefix + "MaxRawSessionObservations", MaxRawSessionObservations);
            EditorPrefs.SetFloat(prefix + "AgentListWidth", AgentListWidth);
            EditorPrefs.SetFloat(prefix + "InspectorWidth", InspectorWidth);
            EditorPrefs.SetBool(prefix + "InspectorExpanded", InspectorExpanded);
            EditorPrefs.SetBool(prefix + "CoalesceAnswerText", CoalesceAnswerText);
            EditorPrefs.SetBool(prefix + "CollapseCompletedTurns", CollapseCompletedTurns);
        }

        static bool HasAny(string prefix)
        {
            foreach (var key in FieldKeys)
            {
                if (EditorPrefs.HasKey(prefix + key))
                    return true;
            }
            return false;
        }

        static AgentLogConfig Read(string prefix)
        {
            return new AgentLogConfig
            {
                MaxCompletedTurnsPerAgent = EditorPrefs.GetInt(prefix + "MaxCompletedTurnsPerAgent", 200),
                MaxObservationsPerAgent = EditorPrefs.GetInt(prefix + "MaxObservationsPerAgent", 4000),
                MaxRawSessionObservations = EditorPrefs.GetInt(prefix + "MaxRawSessionObservations", 4000),
                AgentListWidth = EditorPrefs.GetFloat(prefix + "AgentListWidth", 180f),
                InspectorWidth = EditorPrefs.GetFloat(prefix + "InspectorWidth", 320f),
                InspectorExpanded = EditorPrefs.GetBool(prefix + "InspectorExpanded", false),
                CoalesceAnswerText = EditorPrefs.GetBool(prefix + "CoalesceAnswerText", true),
                CollapseCompletedTurns = EditorPrefs.GetBool(prefix + "CollapseCompletedTurns", true),
            };
        }
    }
}
