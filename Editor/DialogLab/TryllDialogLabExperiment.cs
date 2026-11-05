// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// A single parameter override addressed by node instance name + field path.
    /// <see cref="FieldPath"/> is either a top-level field name ("SystemPrompt") or a
    /// dotted path into a nested overridable group ("Sampling.Seed") — see
    /// <see cref="TryllNodeFieldReflection"/>. Value is stored as its round-trippable
    /// string form; applied to a cloned params object at run time
    /// (<c>TryllNodeFieldReflection.TryApply</c>), never to the workflow asset.
    /// </summary>
    [Serializable]
    public sealed class TryllParamOverride
    {
        public string NodeName;
        public string FieldPath;
        public string Value;
    }

    /// <summary>
    /// A user-labelled set of parameter overrides. Labels are always author-supplied —
    /// never derived from the override diff, which reads badly once a variant touches
    /// more than one field (see dialog-lab-requirements.md R2.2).
    /// </summary>
    [Serializable]
    public sealed class TryllDialogLabVariant
    {
        public string Label = "Variant";
        public Color Swatch = TryllEditorTheme.Accent;

        /// <summary>
        /// Optional per-variant agent. Null = use the window's default Agent Component.
        /// This is how structural differences are compared — a different model, context
        /// size, or graph wiring — since those params are `(structural)` and
        /// `ChangeParams` rejects them (`3006 ParamNotMutable`). Comparing two agents
        /// swaps the whole workflow rather than mutating one, which is why the override
        /// editor still refuses to offer structural fields.
        /// </summary>
        public TryllAgentComponent AgentOverride;

        /// <summary>
        /// Persisted <see cref="UnityEditor.GlobalObjectId"/> for <see cref="AgentOverride"/>.
        /// Kept independently so an unresolved scene object is not wiped on the next save.
        /// </summary>
        public string AgentOverrideId = "";

        /// <summary>
        /// Optional per-variant language model. Empty = the workflow's own
        /// <c>DefaultModelName</c>. This is the "same workflow, different model"
        /// comparison, and it needs no second agent or duplicated asset: on the wire
        /// the default model is a <c>CreateAgentRequest</c> field (a sibling of
        /// <c>graph</c>, see <c>messages.fbs</c>), and the Lab bakes variant values onto
        /// a throwaway graph clone before CreateAgent — so nothing structural is being
        /// mutated and the asset is never written.
        ///
        /// Applies only to nodes that leave their own <c>ModelName</c> empty; the server
        /// falls back to the agent default just for those. A node that pins a model keeps
        /// it, which the Variants tab warns about rather than silently ignoring.
        /// </summary>
        public string ModelOverride = "";

        /// <summary>
        /// When false (default), a tool call with no canned result for the current
        /// step is a CheckFail — the model must not call that tool here, even if the
        /// scene agent has a live <c>RegisterTool</c> handler. The turn still completes
        /// so later steps run. When true, an uncanned call may be answered by that
        /// handler instead.
        /// </summary>
        public bool UseLiveTools;

        public List<TryllParamOverride> Overrides = new List<TryllParamOverride>();

        public TryllDialogLabVariant Clone()
        {
            var c = new TryllDialogLabVariant
            {
                Label = Label,
                Swatch = Swatch,
                AgentOverride = AgentOverride,
                AgentOverrideId = AgentOverrideId,
                ModelOverride = ModelOverride,
                UseLiveTools = UseLiveTools,
            };
            foreach (var o in Overrides)
                c.Overrides.Add(new TryllParamOverride { NodeName = o.NodeName, FieldPath = o.FieldPath, Value = o.Value });
            return c;
        }
    }

    /// <summary>
    /// The Lab's window-local run configuration: which script, which variants, how many
    /// repeats. Deliberately window-authored rather than an asset by default (R2.3) —
    /// most variant sets are thirty-second experiments, and forcing an asset-creation
    /// dialog for that is exactly the friction that stops the feature being used.
    /// </summary>
    [Serializable]
    public sealed class TryllDialogLabExperiment
    {
        public TryllDialogScriptAsset Script;
        public List<TryllDialogLabVariant> Variants = new List<TryllDialogLabVariant>
        {
            new TryllDialogLabVariant { Label = "Baseline" },
        };

        /// <summary>
        /// Repeats per variant. MUST default &gt; 1 (R2.6): a single sample per variant
        /// reproduces the exact one-sample anecdote the Lab exists to replace — comparing
        /// prompt A to prompt B on one seed each is a coin flip presented as a result.
        /// </summary>
        [Min(1)]
        public int Repeats = 4;

        /// <summary>
        /// Seed base for derivation (<see cref="DeriveSeed"/>). MUST NOT be 0 — in
        /// <c>SamplingParams</c>, <c>seed == 0</c> means "random seed", which would
        /// silently defeat reproducibility for every sample.
        /// </summary>
        [Min(1)]
        public int BaseSeed = 1;

        /// <summary>
        /// Deterministic per-(variant, repeat) seed, reproducible and displayable
        /// (R2.7): <c>BaseSeed + variantIndex * 1000 + repeat</c>.
        /// </summary>
        public int DeriveSeed(int variantIndex, int repeat) => BaseSeed + variantIndex * 1000 + repeat;

        public int TotalSamples => Variants.Count * Mathf.Max(1, Repeats);
    }

    /// <summary>
    /// Durable experiment-asset persistence is deferred — save/load of promoted variant
    /// sets is not wired. This type is kept so existing serialized references do not
    /// break; it is intentionally not exposed via CreateAssetMenu.
    /// </summary>
    public sealed class TryllDialogLabExperimentAsset : ScriptableObject
    {
        public TryllDialogScriptAsset Script;
        public List<TryllDialogLabVariant> Variants = new List<TryllDialogLabVariant>();
        [Min(1)] public int Repeats = 4;
        [Min(1)] public int BaseSeed = 1;
    }
}
