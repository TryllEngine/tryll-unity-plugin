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
    internal static class _FpCheck_TryllClassifyIntentEntry {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// One intent label for ClassifyIntentLLM.  Order in the parent `intents` vector is letter order: entry 0 is A, entry 1 is B, …
    /// </summary>
    /// <summary>Sub-table: One intent label for ClassifyIntentLLM.  Order in the parent `intents` vector is letter order: entry 0 is A, entry 1 is B, …</summary>
    [Serializable]
    public sealed class TryllClassifyIntentEntry
    {
        public static readonly TryllClassifyIntentEntry Default = new TryllClassifyIntentEntry();

        [Tooltip("Label written to the output when this intent wins. The model never sees it. Must be unique among the intents.")]
        public string Id;

        [Tooltip("Description of the intent the model reads, listed as 'A. <prompt>', 'B. <prompt>', … in Intents order.")]
        [TryllLocalizable(true)]
        public string Prompt;
        [HideInInspector] public bool PromptLocalized;
        [HideInInspector] public string PromptTextId;


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
            Tryll.NodeParams.ClassifyIntentEntry.StartClassifyIntentEntry(fbb);
            if (Id != null)
                Tryll.NodeParams.ClassifyIntentEntry.AddId(fbb, idOff);
            if (Prompt != null)
                Tryll.NodeParams.ClassifyIntentEntry.AddPrompt(fbb, promptOff);
            return Tryll.NodeParams.ClassifyIntentEntry.EndClassifyIntentEntry(fbb).Value;
        }

        public TryllClassifyIntentEntry Clone()
        {
            var c = (TryllClassifyIntentEntry)MemberwiseClone();
            return c;
        }
    }
}
