// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// A durable, version-controlled dialog test case for the Dialog Lab
    /// (<c>Window ▸ Tryll ▸ Dialog Lab</c>). Holds an ordered list of steps: the
    /// leading <see cref="SeedTurnCount"/> steps are injected via
    /// <see cref="TryllAgent.AppendInteractions"/> (no graph run), the rest are sent
    /// through <see cref="TryllAgent.SendMessage"/> and their answers checked.
    ///
    /// This is the "durable" half of the Lab's two-tier authoring model: variants
    /// (parameter sweeps) are usually throwaway and live in the Lab window itself
    /// (see <c>TryllDialogLabExperiment</c>); a script is the reusable test case that
    /// variants run against.
    /// </summary>
    [CreateAssetMenu(menuName = "Tryll/Dialog Script", fileName = "NewDialogScript")]
    public sealed class TryllDialogScriptAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [TextArea(1, 3)]
        public string Description = "";

        /// <summary>
        /// Number of leading <see cref="Steps"/> that are seeded (both sides supplied,
        /// injected with no graph run) rather than generated (user side only, sent
        /// through the workflow). Must be <c>&lt;= Steps.Count</c>.
        /// </summary>
        [Min(0)]
        public int SeedTurnCount = 0;

        /// <summary>
        /// Checks applied to <b>every generated</b> turn, on top of each step's own
        /// <see cref="Step.Checks"/>. Use them for constraints that hold for the whole
        /// dialog (a persona break, a leaked instruction marker, a length cap) instead
        /// of repeating the same check on every step. Seeded turns are author-written,
        /// so global checks skip them.
        /// </summary>
        [SerializeReference, SubclassSelector]
        public List<TryllDialogCheck> GlobalChecks = new List<TryllDialogCheck>();

        /// <summary>Legacy (pre-typed-check) global patterns. Migrated into
        /// <see cref="GlobalChecks"/> on load; kept only so old YAML still deserializes.</summary>
        [HideInInspector]
        public string[] GlobalRegexForbid = Array.Empty<string>();

        /// <summary>0 = written before typed checks; migrated in <c>OnAfterDeserialize</c>.</summary>
        [SerializeField, HideInInspector]
        private int _checksFormat;

        public List<Step> Steps = new List<Step>();

        [Serializable]
        public sealed class Step
        {
            /// <summary>Always required — the player line.</summary>
            [TextArea(1, 4)]
            public string User = "";

            /// <summary>
            /// Required when this step is seeded (its index &lt; SeedTurnCount);
            /// ignored for generated steps (the workflow produces the reply).
            /// </summary>
            [TextArea(1, 4)]
            public string Assistant = "";

            /// <summary>
            /// Deterministic checks on this step's answer — no model involved, ever. Add
            /// as many as needed, of any type, including several of the same type.
            /// </summary>
            [SerializeReference, SubclassSelector]
            public List<TryllDialogCheck> Checks = new List<TryllDialogCheck>();

            // ── Legacy check fields (before typed checks). Migrated into Checks on
            // load and then cleared; kept only so old YAML still deserializes. ──────
            [HideInInspector] public string[] MustContain = Array.Empty<string>();
            [HideInInspector] public string[] MustNotContain = Array.Empty<string>();
            [HideInInspector] public string[] RegexForbid = Array.Empty<string>();
            [HideInInspector] public int MaxWords = 0;
            [HideInInspector] public List<IntentCheck> IntentChecks = new List<IntentCheck>();
            [HideInInspector] public List<AttributeCheck> AttributeChecks = new List<AttributeCheck>();

            /// <summary>
            /// Canned results for a workflow that pauses on this step waiting for
            /// <c>AwaitResult</c> tool calls. Keyed by <b>tool name</b>, not call id:
            /// the server allocates each call's id at dispatch time
            /// (<c>ToolCallNode::MakeToolCallCorrelationId</c>), so it cannot be
            /// authored ahead of a run — matching by tool name mirrors how
            /// <c>TryllAgent.RegisterTool</c> already keys its handlers. If the step
            /// pauses and a call's tool name has no entry here, the runner resumes
            /// with an empty result and CheckFails the cell — unless the variant has
            /// Use Live Tools and the scene agent has a live handler for that name.
            /// A result here is fixture data only: to assert the tool was called, add
            /// a Calls tool check.
            /// </summary>
            public List<CannedToolResult> ToolResults = new List<CannedToolResult>();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Anything being written is in the typed form: either it was loaded (and
            // OnAfterDeserialize migrated it) or it was created after typed checks.
            _checksFormat = 1;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_checksFormat >= 1) return;
            MigrateLegacyChecks();
            _checksFormat = 1;
        }

        /// <summary>
        /// Moves the pre-typed-check fields into <see cref="Step.Checks"/> and
        /// <see cref="GlobalChecks"/>. Plain C# objects only, so it is safe inside the
        /// deserialization callback. Behaviour-preserving: the implicit "must call X"
        /// of a canned tool result on a generated step becomes an explicit Calls tool check.
        /// </summary>
        private void MigrateLegacyChecks()
        {
            Steps ??= new List<Step>();
            for (int i = 0; i < Steps.Count; i++)
            {
                var step = Steps[i];
                if (step == null) continue;
                step.Checks ??= new List<TryllDialogCheck>();
                step.Checks.AddRange(TryllDialogCheckMigration.FromLegacyStep(step, generated: i >= SeedTurnCount));
                TryllDialogCheckMigration.ClearLegacyFields(step);
            }
            GlobalChecks ??= new List<TryllDialogCheck>();
            GlobalChecks.AddRange(TryllDialogCheckMigration.FromLegacyGlobal(GlobalRegexForbid));
            GlobalRegexForbid = Array.Empty<string>();
        }

        /// <summary>Legacy (pre-typed-check) intent assertion; see <see cref="TryllIntentIsCheck"/>.</summary>
        [Serializable]
        public struct IntentCheck
        {
            /// <summary>Classifier node name. Empty = any classifier that ran this turn.</summary>
            public string Node;
            /// <summary>Expected intent label. Ignored when ExpectNotFound is true.</summary>
            public string Intent;
            /// <summary>Assert the classifier rejected the input instead of naming a label.</summary>
            public bool ExpectNotFound;
        }

        /// <summary>Legacy (pre-typed-check) attribute assertion; see <see cref="TryllAttributeEqualsCheck"/>.</summary>
        [Serializable]
        public struct AttributeCheck
        {
            /// <summary>Classifier node name. Empty = any ClassifyAttributesLLM node.</summary>
            public string Node;
            /// <summary>Attribute name.</summary>
            public string Name;
            /// <summary>Expected slot value. Ignored when ScoreRange is set.</summary>
            public string Value;
            /// <summary>When set, Value is ignored and the slot must be a score in range.</summary>
            public bool ScoreRange;
            public float ScoreMin;
            public float ScoreMax;
        }

        [Serializable]
        public struct CannedToolResult
        {
            public string ToolName;
            public string Result;
        }
    }
}
