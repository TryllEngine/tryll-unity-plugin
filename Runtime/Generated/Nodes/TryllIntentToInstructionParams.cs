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
    internal static class _FpCheck_TryllIntentToInstructionParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Maps an intent label to an instruction text from a Map-kind string storage. Resolution priority (factory): named string_storage → inline_keys + instructions (Map-kind in-memory storage built from the equal-length pair).
    /// </summary>
    [Serializable]
    [AddTypeMenu("IntentToInstruction")]
    public sealed class TryllIntentToInstructionParams : TryllNodeParamsBase, ISerializationCallbackReceiver    {
        public override TryllNodeType NodeType => TryllNodeType.IntentToInstruction;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/intent-to-instruction.html";

        [TryllGroup("Storage", First = true)]
        [Tooltip("Storage file that maps intent labels to instruction text. Used instead of Inline Keys and Instructions.")]
        [TryllFilePicker]
        // Optional string — null means "use server default".
        public string StringStorage;

        [TryllGroup("Storage")]
        [Tooltip("Intent labels, paired by position with Instructions. Used when String Storage is empty.")]
        [TryllStructural]
        public string[] InlineKeys;

        [TryllGroup("Storage")]
        [Tooltip("Instruction text for each label in Inline Keys, in the same order.")]
        [TryllStructural]
        public TryllLocalizableText[] Instructions;

        [TryllGroup("Output", First = true)]
        [Tooltip("Name later nodes use to read the chosen instruction (in their Input, or as the slot.<name> tag). Empty = this node's name.")]
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
            var string_storageOff = fbb.CreateString(StringStorage ?? "");
            var output_nameOff = fbb.CreateString(OutputName ?? "");
            var default_exitOff = fbb.CreateString(DefaultExit ?? "");
            VectorOffset inline_keysVecOff = default;
            if (InlineKeys != null && InlineKeys.Length > 0)
            {
                var inline_keysStrOffs = new StringOffset[InlineKeys.Length];
                for (int i = 0; i < InlineKeys.Length; i++)
                    inline_keysStrOffs[i] = fbb.CreateString(InlineKeys[i] ?? "");
                inline_keysVecOff = Tryll.NodeParams.IntentToInstructionParams.CreateInlineKeysVector(fbb, inline_keysStrOffs);
            }
            VectorOffset instructionsVecOff = default;
            if (Instructions != null && Instructions.Length > 0)
            {
                var instructionsOffs = new Offset<Tryll.NodeParams.LocalizableText>[Instructions.Length];
                for (int i = 0; i < Instructions.Length; i++)
                    instructionsOffs[i] = new Offset<Tryll.NodeParams.LocalizableText>(Instructions[i].Pack(fbb));
                instructionsVecOff = Tryll.NodeParams.IntentToInstructionParams.CreateInstructionsVector(fbb, instructionsOffs);
            }

            Tryll.NodeParams.IntentToInstructionParams.StartIntentToInstructionParams(fbb);
            Tryll.NodeParams.IntentToInstructionParams.AddStringStorage(fbb, string_storageOff);
            if (InlineKeys != null && InlineKeys.Length > 0)
                Tryll.NodeParams.IntentToInstructionParams.AddInlineKeys(fbb, inline_keysVecOff);
            Tryll.NodeParams.IntentToInstructionParams.AddOutputName(fbb, output_nameOff);
            Tryll.NodeParams.IntentToInstructionParams.AddDefaultExit(fbb, default_exitOff);
            if (Instructions != null && Instructions.Length > 0)
                Tryll.NodeParams.IntentToInstructionParams.AddInstructions(fbb, instructionsVecOff);
            var offset = Tryll.NodeParams.IntentToInstructionParams.EndIntentToInstructionParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.IntentToInstructionParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllIntentToInstructionParams)MemberwiseClone();
            if (InlineKeys != null)
                c.InlineKeys = (string[])InlineKeys.Clone();
            if (Instructions != null)
            {
                c.Instructions = new TryllLocalizableText[Instructions.Length];
                for (int i = 0; i < Instructions.Length; i++)
                    c.Instructions[i] = Instructions[i]?.Clone();
            }
            if (InlineStrings != null)
                c.InlineStrings = (string[])InlineStrings.Clone();
            return c;
        }

        // Older assets stored this list as a string array under this name.
        // OnAfterDeserialize copies it into Instructions when that list is
        // empty. OnBeforeSerialize drops it once Instructions has the text,
        // so the next save does not keep both.
        [HideInInspector] public string[] InlineStrings;

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (Instructions != null && Instructions.Length > 0)
                return;
            if (InlineStrings == null || InlineStrings.Length == 0)
                return;
            Instructions = new TryllLocalizableText[InlineStrings.Length];
            for (int i = 0; i < InlineStrings.Length; i++)
                Instructions[i] = new TryllLocalizableText { Text = InlineStrings[i] ?? "" };
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (Instructions != null && Instructions.Length > 0)
                InlineStrings = null;
        }
    }
}
