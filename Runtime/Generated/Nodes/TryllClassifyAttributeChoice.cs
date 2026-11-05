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
    internal static class _FpCheck_TryllClassifyAttributeChoice {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// One option of a Choice or Score attribute. Boolean does not use this table: its menu is fixed as A=True, B=False.
    /// </summary>
    /// <summary>Sub-table: One option of a Choice or Score attribute. Boolean does not use this table: its menu is fixed as A=True, B=False.</summary>
    [Serializable]
    public sealed class TryllClassifyAttributeChoice
    {
        public static readonly TryllClassifyAttributeChoice Default = new TryllClassifyAttributeChoice();

        [Tooltip("Value written to the output when this option wins. The model never sees it. Must be unique within the attribute.")]
        public string Id;

        [Tooltip("Description of the option the model reads, listed as 'A. <prompt>'.")]
        [TryllLocalizable(true)]
        public string Prompt;
        [HideInInspector] public bool PromptLocalized;
        [HideInInspector] public string PromptTextId;

        [Tooltip("Choice only. When this option is the answer, the node takes its Triggered exit. Leave off for Score attributes.")]
        public bool Triggers = false;


        internal int Pack(FlatBufferBuilder fbb)
        {
            // Pack nested objects and strings first (bottom-up). CreateString
            // after Start* throws "object serialization must not be nested".
            StringOffset idOff = default;
            if (Id != null)
                idOff = fbb.CreateString(Id);
            StringOffset promptOff = default;
            if (Prompt != null)
                promptOff = fbb.CreateString(Prompt);
            Tryll.NodeParams.ClassifyAttributeChoice.StartClassifyAttributeChoice(fbb);
            if (Id != null)
                Tryll.NodeParams.ClassifyAttributeChoice.AddId(fbb, idOff);
            if (Prompt != null)
                Tryll.NodeParams.ClassifyAttributeChoice.AddPrompt(fbb, promptOff);
            Tryll.NodeParams.ClassifyAttributeChoice.AddTriggers(fbb, Triggers);
            return Tryll.NodeParams.ClassifyAttributeChoice.EndClassifyAttributeChoice(fbb).Value;
        }

        public TryllClassifyAttributeChoice Clone()
        {
            var c = (TryllClassifyAttributeChoice)MemberwiseClone();
            return c;
        }
    }
}
