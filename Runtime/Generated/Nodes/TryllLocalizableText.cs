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
    internal static class _FpCheck_TryllLocalizableText {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// One string a node can store inline or as a Text ID.
    /// </summary>
    /// <summary>Sub-table: One string a node can store inline or as a Text ID.</summary>
    [Serializable]
    public sealed class TryllLocalizableText
    {
        public static readonly TryllLocalizableText Default = new TryllLocalizableText();

        [Tooltip("The text. Tick Localized to store a Text ID from the workflow text table.")]
        [TryllLocalizable(true)]
        public string Text;
        [HideInInspector] public bool TextLocalized;
        [HideInInspector] public string TextTextId;


        internal int Pack(FlatBufferBuilder fbb)
        {
            // Pack nested objects and strings first (bottom-up). CreateString
            // after Start* throws "object serialization must not be nested".
            StringOffset textOff = default;
            if (Text != null)
                textOff = fbb.CreateString(Text);
            Tryll.NodeParams.LocalizableText.StartLocalizableText(fbb);
            if (Text != null)
                Tryll.NodeParams.LocalizableText.AddText(fbb, textOff);
            return Tryll.NodeParams.LocalizableText.EndLocalizableText(fbb).Value;
        }

        public TryllLocalizableText Clone()
        {
            var c = (TryllLocalizableText)MemberwiseClone();
            return c;
        }
    }
}
