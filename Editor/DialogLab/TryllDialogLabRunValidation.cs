// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// What a variant runs on, as the pre-run checks see it: the Lab window builds one from a
    /// scene <see cref="TryllAgentComponent"/>, headless mode from a workflow asset.
    /// </summary>
    internal sealed class TryllLabRunAgent
    {
        public string Name = "";
        public TryllGraphDescription Graph;
        public bool MaintainDialogueHistory = true;

        public static TryllLabRunAgent FromComponent(TryllAgentComponent agent) =>
            agent == null
                ? null
                : new TryllLabRunAgent
                {
                    Name = agent.name,
                    Graph = agent.WorkflowAsset != null ? agent.WorkflowAsset.Graph : null,
                    MaintainDialogueHistory = agent.MaintainDialogueHistory,
                };
    }

    /// <summary>
    /// The checks a run must pass before it starts, shared by the Lab window and headless
    /// mode so both refuse the same runs with the same messages.
    /// </summary>
    internal static class TryllDialogLabRunValidation
    {
        /// <summary>Every dialogue on its own; messages name the dialogue when there are several.</summary>
        public static bool TryValidateScripts(IReadOnlyList<TryllDialogScriptData> scripts, out string error)
        {
            for (int di = 0; di < scripts.Count; di++)
            {
                if (!TryllDialogScriptValidation.TryValidate(scripts[di], out var scriptErr))
                {
                    error = TryllDialogLabDialogues.Prefix(di, scripts[di].Name, scripts.Count) + scriptErr;
                    return false;
                }
            }
            error = null;
            return true;
        }

        /// <summary>
        /// One variant against its agent: a graph to run, overrides that still apply, a
        /// resolvable model, and no seeded history on a stateless agent. A null agent is
        /// refused with the window's wording.
        /// </summary>
        public static bool TryValidateVariant(
            TryllDialogLabVariant v, TryllLabRunAgent agent,
            IReadOnlyList<TryllDialogScriptData> scripts, out string error)
        {
            error = null;
            if (agent == null)
            {
                error = $"Variant '{v.Label}' has no agent: set one on the variant, "
                      + "or assign a default Agent Component.";
                return false;
            }
            var g = agent.Graph;
            if (g == null || g.Nodes == null || g.Nodes.Count == 0)
            {
                error = $"Variant '{v.Label}': agent '{agent.Name}' has no workflow graph.";
                return false;
            }
            var overrideProblems = TryllNodeFieldReflection.CollectOverrideProblems(g, v.Overrides);
            if (overrideProblems.Count > 0)
            {
                error = $"Variant '{v.Label}': " + string.Join("; ", overrideProblems);
                return false;
            }
            // A model must be resolvable from somewhere, or CreateAgent fails with
            // "no model_name param and no agent default_model_name" — a clearer
            // failure before the run than after it.
            if (string.IsNullOrEmpty(v.ModelOverride)
                && string.IsNullOrEmpty(g.DefaultModelName)
                && g.Nodes.Any(n => n?.Params != null && NodeNeedsModel(n.Params)))
            {
                error = $"Variant '{v.Label}': no model. The workflow sets no "
                      + "DefaultModelName and at least one node does not pin its own — "
                      + "pick a Model for this variant.";
                return false;
            }
            // Script validity also depends on the agent: seeded history is
            // meaningless against a stateless one.
            for (int di = 0; di < scripts.Count; di++)
            {
                if (!TryllDialogScriptValidation.TryValidateForAgent(
                        scripts[di], agent.MaintainDialogueHistory, agent.Name, out var agentErr))
                {
                    error = TryllDialogLabDialogues.Prefix(di, scripts[di].Name, scripts.Count)
                          + $"Variant '{v.Label}': {agentErr}";
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// True when this node resolves a language model and has not pinned one, so it
        /// depends on the agent default. Detected by the presence of a public
        /// <c>ModelName</c> string field rather than by enumerating node types, matching
        /// <see cref="TryllNodeFieldReflection"/>'s generic approach — a new model-using
        /// node type needs no change here.
        /// </summary>
        internal static bool NodeNeedsModel(TryllNodeParamsBase p)
        {
            var f = p.GetType().GetField("ModelName", BindingFlags.Public | BindingFlags.Instance);
            if (f == null || f.FieldType != typeof(string)) return false;
            return string.IsNullOrEmpty(f.GetValue(p) as string);
        }
    }
}
