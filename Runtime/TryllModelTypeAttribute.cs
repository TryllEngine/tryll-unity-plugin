// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Marks a serialized model-name string field as a picker over the project's
    /// registered models (the <see cref="TryllModelManifest"/>), filtered to a model
    /// type. Emitted by codegen (driven by the <c>(tryll_model)</c> attribute in
    /// <c>nodes.fbs</c>) on node-param model fields, and applied by hand to component
    /// model fields such as the Voice Input STT model.
    ///
    /// Editor: a custom <see cref="UnityEditor.PropertyDrawer"/> renders the field as a
    /// dropdown of the manifest's models of <see cref="ModelType"/>, each annotated with
    /// its registration tier (Production / Experimental / Unregistered).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TryllModelTypeAttribute : PropertyAttribute
    {
        /// <summary>Model type to filter the picker by: "language" | "embedding" | "stt" | "tts" | "vad".</summary>
        public string ModelType { get; }

        public TryllModelTypeAttribute(string modelType)
        {
            ModelType = modelType ?? string.Empty;
        }
    }
}
