// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// ScriptableObject that stores a Tryll workflow graph description.
    /// Equivalent to UTryllWorkflowAsset (UPrimaryDataAsset) in the Unreal plugin.
    ///
    /// Usage:
    ///   1. In the Project window: right-click → Create → Tryll → Workflow Asset.
    ///   2. Edit <see cref="Graph"/> in the Inspector.
    ///   3. Assign this asset to a <see cref="TryllAgentComponent"/>'s WorkflowAsset field.
    ///
    /// Programmatic alternative: populate <see cref="TryllGraphDescription"/> directly in C#
    /// using <see cref="TryllGraphBuilder"/> and pass it to
    /// <see cref="TryllClient.RequestCreateAgentAsync"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Tryll/Workflow Asset", fileName = "NewTryllWorkflow")]
    [HelpURL(TryllDocs.TryllWorkflowAsset)]
    public sealed class TryllWorkflowAsset : ScriptableObject
    {
        /// <summary>Workflow graph stored by this asset.</summary>
        public TryllGraphDescription Graph;

        /// <summary>
        /// Name of the text table passed to the project's text provider with
        /// every Text ID in this workflow. Each Localized field stores only the
        /// key. A custom provider may ignore the table.
        /// </summary>
        [Tooltip("Table name passed to the text provider with every Text ID in this workflow.")]
        public string TextTable = "";

        /// <summary>
        /// Declared per-agent Variables for agents created from this asset — the
        /// complete declaration (name, type, initial value) sent once in
        /// CreateAgentRequest. Consumable from Mustache templates as
        /// <c>{{var.&lt;name&gt;}}</c> and the Retrieve/ClassifyIntent filter grammar as
        /// <c>{"var": "&lt;name&gt;"}</c>. A <see cref="TryllAgentComponent"/> may override
        /// initial values (not name/type) via its own VariableOverrides
        /// (Material-Instance pattern).
        /// </summary>
        public List<TryllVariableDecl> Variables = new List<TryllVariableDecl>();

        private void OnValidate()
        {
            if (Graph == null) return;
            string table = TextTable ?? "";
            if (Graph.TextTable != table)
                Graph.TextTable = table;
        }
    }
}
