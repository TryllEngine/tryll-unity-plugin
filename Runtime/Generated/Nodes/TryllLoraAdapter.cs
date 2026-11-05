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
    internal static class _FpCheck_TryllLoraAdapter {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// One LoRA adapter applied to an LLM node's context. The GGUF adapter must have been trained for the node's base model; a file made for another base fails agent creation. Loaded once per model and shared by every context that names the same file.
    /// </summary>
    /// <summary>Sub-table: One LoRA adapter applied to an LLM node's context. The GGUF adapter must have been trained for the node's base model; a file made for another base fails agent creation. Loaded once per model and shared by every context that names the same file.</summary>
    [Serializable]
    public sealed class TryllLoraAdapter
    {
        public static readonly TryllLoraAdapter Default = new TryllLoraAdapter();

        [Tooltip("LoRA adapter file (.gguf), relative to the storage root. Must be trained for the same base model as the node's model.")]
        [TryllFilePicker]
        public string Path;

        [Tooltip("Adapter strength. 1 = as trained, 0 = off.")]
        [Range(0.0f, 4.0f)]
        public float Scale = 1.0f;


        internal int Pack(FlatBufferBuilder fbb)
        {
            // Pack nested objects and strings first (bottom-up). CreateString
            // after Start* throws "object serialization must not be nested".
            StringOffset pathOff = default;
            if (Path != null)
                pathOff = fbb.CreateString(Path);
            Tryll.NodeParams.LoraAdapter.StartLoraAdapter(fbb);
            if (Path != null)
                Tryll.NodeParams.LoraAdapter.AddPath(fbb, pathOff);
            Tryll.NodeParams.LoraAdapter.AddScale(fbb, Scale);
            return Tryll.NodeParams.LoraAdapter.EndLoraAdapter(fbb).Value;
        }

        public TryllLoraAdapter Clone()
        {
            var c = (TryllLoraAdapter)MemberwiseClone();
            return c;
        }
    }
}
