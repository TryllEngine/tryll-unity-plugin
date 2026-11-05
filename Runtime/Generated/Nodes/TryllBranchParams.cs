// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/nodes.fbs  +  tools/codegen/templates/unity_node.cs.j2
// See: docs/dev/codegen.md
using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using UnityEngine;

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_TryllBranchParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Conditional routing node. Evaluates a test against a turn-local slot or an agent variable and takes then_exit or else_exit. Reader-only: no model, no slot write, no wire emission.
    /// </summary>
    [Serializable]
    [AddTypeMenu("Branch")]
    public sealed class TryllBranchParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.Branch;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/branch.html";

        [TryllGroup("Input", First = true)]
        [Tooltip("What to check: NonEmpty, IsTrue (bool Variable), Equals, EqualsIgnoreCase, Contains, Regex (against Values), or Expression (Condition).")]
        public TryllBranchTest Test = TryllBranchTest.NonEmpty;

        [TryllGroup("Input")]
        [Tooltip("Text to test: the player's message or an earlier node's output. Empty = the player's message. Leave empty when Variable is set.")]
        [TryllStructural]
        [TryllSlotInput]
        // Optional string — null means "use server default".
        public string Input;

        [TryllGroup("Input")]
        [Tooltip("Agent variable to test instead of Input. Leave empty to test Input.")]
        [TryllStructural]
        // Optional string — null means "use server default".
        public string Variable;

        [TryllGroup("Behavior", First = true)]
        [Tooltip("Values for Equals, EqualsIgnoreCase, Contains and Regex; the test passes if any one matches.")]
        public string[] Values;

        [TryllGroup("Behavior")]
        [Tooltip("JSON condition for the Expression test, over agent variables and {'slot':'<name>'} values. Unused by other tests.")]
        [TextArea(3, 10)]
        // Optional string — null means "use server default".
        public string Condition;

        [TryllGroup("Output", First = true)]
        [Tooltip("Send the game a branch_taken event with the exit taken.")]
        public bool NotifyClient = false;

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run when the test passes. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("then")]
        // Graph exit "then" — target node name; empty = END.
        public string ThenExit;

        [TryllGroup("Exits")]
        [Tooltip("Node to run when the test fails or the value to test is missing. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("else")]
        // Graph exit "else" — target node name; empty = END.
        public string ElseExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var inputOff = fbb.CreateString(Input ?? "");
            var variableOff = fbb.CreateString(Variable ?? "");
            var conditionOff = fbb.CreateString(Condition ?? "");
            var then_exitOff = fbb.CreateString(ThenExit ?? "");
            var else_exitOff = fbb.CreateString(ElseExit ?? "");
            VectorOffset valuesVecOff = default;
            if (Values != null && Values.Length > 0)
            {
                var valuesStrOffs = new StringOffset[Values.Length];
                for (int i = 0; i < Values.Length; i++)
                    valuesStrOffs[i] = fbb.CreateString(Values[i] ?? "");
                valuesVecOff = Tryll.NodeParams.BranchParams.CreateValuesVector(fbb, valuesStrOffs);
            }

            Tryll.NodeParams.BranchParams.StartBranchParams(fbb);
            Tryll.NodeParams.BranchParams.AddTest(fbb, (Tryll.BranchTest)(byte)Test);
            Tryll.NodeParams.BranchParams.AddInput(fbb, inputOff);
            Tryll.NodeParams.BranchParams.AddVariable(fbb, variableOff);
            if (Values != null && Values.Length > 0)
                Tryll.NodeParams.BranchParams.AddValues(fbb, valuesVecOff);
            Tryll.NodeParams.BranchParams.AddCondition(fbb, conditionOff);
            Tryll.NodeParams.BranchParams.AddNotifyClient(fbb, NotifyClient);
            Tryll.NodeParams.BranchParams.AddThenExit(fbb, then_exitOff);
            Tryll.NodeParams.BranchParams.AddElseExit(fbb, else_exitOff);
            var offset = Tryll.NodeParams.BranchParams.EndBranchParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.BranchParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllBranchParams)MemberwiseClone();
            if (Values != null)
                c.Values = (string[])Values.Clone();
            return c;
        }
    }
}
