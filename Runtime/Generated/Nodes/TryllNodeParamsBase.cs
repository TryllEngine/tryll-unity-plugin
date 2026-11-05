// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// Hand-written once — do not delete.
// Generated per-node subclasses live alongside in this directory.
using System;
using Google.FlatBuffers;

namespace Tryll.Client
{
    /// <summary>
    /// Abstract base for all typed node-parameter objects.
    ///
    /// Subclasses are generated from nodes.fbs by tryll_codegen.py (nodes unity).
    /// Each carries a TryllNodeType discriminator and implements Pack() to serialise
    /// itself into a FlatBuffers NodeParams union slot for wire transmission.
    ///
    /// Unity Inspector usage:
    ///   - Add [SerializeReference] on the containing field to support polymorphic
    ///     subclass selection in the Inspector.
    ///
    /// C# usage (clone-set-send idiom):
    ///   var mutated = (TryllGenerateParams)agent.GetNodeParamsBaseline("gen");
    ///   mutated.SystemPrompt = "New prompt";
    ///   agent.ChangeParams("gen", mutated);
    /// </summary>
    [Serializable]
    public abstract class TryllNodeParamsBase
    {
        /// <summary>Wire-level discriminator for the NodeParams union.</summary>
        public abstract TryllNodeType NodeType { get; }

        /// <summary>
        /// True when this node speaks through the agent's voice (Speak,
        /// GenerateAndSpeak). A graph with such a node needs a voice profile.
        /// Generated from the <c>(tts_consumer)</c> schema attribute.
        /// </summary>
        public virtual bool IsTtsConsumer => false;

        /// <summary>
        /// Reference page of this node type on docs.tryllengine.com, opened by the
        /// editor's help button. Null for a type with no published page.
        /// </summary>
        public virtual string DocsUrl => null;

        /// <summary>
        /// Serialise this params object into the supplied FlatBufferBuilder as a
        /// NodeParams union slot.
        /// Returns a tuple of (NodeParams union byte discriminator, table offset).
        /// Not called directly by game code — invoked by the codec layer.
        /// </summary>
        public abstract (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb);

        /// <summary>
        /// Create a deep clone of this params object.
        /// Generated subclasses recursively clone nested sub-tables (e.g.
        /// TryllSamplingOverrides) and array fields, so mutating the returned
        /// clone never touches the source.
        /// </summary>
        public abstract TryllNodeParamsBase Clone();
    }

    /// <summary>
    /// Optional wrapper for nullable scalar fields in generated params classes.
    /// Mirrors the flatbuffers null-default pattern used by optional scalars.
    /// </summary>
    [Serializable]
    public struct TryllOptional<T> where T : struct
    {
        [UnityEngine.SerializeField]
        private bool _hasValue;

        [UnityEngine.SerializeField]
        private T _value;

        public bool HasValue => _hasValue;
        public T Value => _hasValue ? _value : throw new InvalidOperationException("TryllOptional has no value.");

        public static implicit operator TryllOptional<T>(T value) => new TryllOptional<T> { _hasValue = true, _value = value };
        public static readonly TryllOptional<T> None = default;

        public override string ToString() => _hasValue ? _value.ToString() : "<none>";
    }

    /// <summary>
    /// Range bounds for <see cref="TryllOptional{T}"/> fields.
    /// Do not use Unity's [Range] on TryllOptional — attribute drawers take precedence
    /// over the TryllOptional PropertyDrawer and break the Inspector.
    /// </summary>
    public sealed class TryllOptionalRangeAttribute : UnityEngine.PropertyAttribute
    {
        public float Min { get; }
        public float Max { get; }

        public TryllOptionalRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Minimum bound for <see cref="TryllOptional{T}"/> fields. See <see cref="TryllOptionalRangeAttribute"/>.
    /// </summary>
    public sealed class TryllOptionalMinAttribute : UnityEngine.PropertyAttribute
    {
        public float Min { get; }

        public TryllOptionalMinAttribute(float min) => Min = min;
    }
}
