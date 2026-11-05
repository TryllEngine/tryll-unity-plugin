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
    internal static class _FpCheck_TryllInstructionParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Plain instruction node.  Attaches a kind=Instruction SlotComponent to the current interaction so downstream Mustache templates can reference it.
    /// </summary>
    [Serializable]
    [AddTypeMenu("Instruction")]
    public sealed class TryllInstructionParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.Instruction;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/instruction.html";

        [TryllGroup("Prompt", First = true)]
        [Tooltip("Text later nodes can insert into their prompts, as the slot.<Output Name> tag or through the #instructions list.")]
        [TryllLocalizable(true)]
        // Optional string — null means "use server default".
        public string Instruction;
        // Client-only. Pack() still writes Instruction; the resolve step fills it from InstructionTextId.
        [HideInInspector] public bool InstructionLocalized;
        [HideInInspector] public string InstructionTextId;

        [TryllGroup("Output", First = true)]
        [Tooltip("Name later nodes use to read the Instruction (in their Input, or as the slot.<name> tag). Empty = this node's name.")]
        [TryllStructural]
        [TryllSlotName]
        // Optional string — null means "use server default".
        public string OutputName;

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run next. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("default")]
        // Graph exit "default" — target node name; empty = END.
        public string DefaultExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var instructionOff = fbb.CreateString(Instruction ?? "");
            var output_nameOff = fbb.CreateString(OutputName ?? "");
            var default_exitOff = fbb.CreateString(DefaultExit ?? "");

            Tryll.NodeParams.InstructionParams.StartInstructionParams(fbb);
            Tryll.NodeParams.InstructionParams.AddInstruction(fbb, instructionOff);
            Tryll.NodeParams.InstructionParams.AddOutputName(fbb, output_nameOff);
            Tryll.NodeParams.InstructionParams.AddDefaultExit(fbb, default_exitOff);
            var offset = Tryll.NodeParams.InstructionParams.EndInstructionParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.InstructionParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllInstructionParams)MemberwiseClone();
            return c;
        }
    }
}
