// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Pure helpers that collect model-name references from a workflow graph and
    /// compare them against a <see cref="TryllModelManifest"/>. Used by the
    /// Windows Standalone pre-build check; EditMode tests cover the scan logic
    /// without going through <c>AssetDatabase</c>.
    ///
    /// Covers <see cref="TryllWorkflowAsset"/> graphs (and any in-memory
    /// <see cref="TryllGraphDescription"/>) and <see cref="TryllVoiceProfile"/> data, which
    /// holds the TTS model since protocol v27.
    /// </summary>
    internal static class TryllWorkflowModelRefs
    {
        /// <summary>
        /// <see cref="ModelRef.NodeName"/> of refs collected from a voice profile. Not a legal
        /// node name (contains '&lt;'), so it cannot collide with one.
        /// </summary>
        public const string VoiceOwner = "<voice>";

        /// <summary>One model-name field found on a graph (node params or DefaultModelName) or a voice profile.</summary>
        public readonly struct ModelRef
        {
            public readonly string NodeName;   // empty for graph-level DefaultModelName
            public readonly string FieldName;
            public readonly string ModelName;
            public readonly string ModelType;  // "language" | "embedding" | "stt" | "tts" | "vad"

            public ModelRef(string nodeName, string fieldName, string modelName, string modelType)
            {
                NodeName   = nodeName   ?? string.Empty;
                FieldName  = fieldName  ?? string.Empty;
                ModelName  = modelName  ?? string.Empty;
                ModelType  = modelType  ?? string.Empty;
            }
        }

        /// <summary>A referenced model that is not marked Production in the manifest.</summary>
        public readonly struct Violation
        {
            public readonly string AssetLabel; // caller-supplied (asset path or test label)
            public readonly ModelRef Ref;
            public readonly TryllModelRegistration Registration;

            public Violation(string assetLabel, ModelRef modelRef, TryllModelRegistration registration)
            {
                AssetLabel    = assetLabel ?? string.Empty;
                Ref           = modelRef;
                Registration  = registration;
            }

            public override string ToString()
            {
                string where = Ref.NodeName == VoiceOwner
                    ? $"{AssetLabel} · Voice.{Ref.FieldName}"
                    : string.IsNullOrEmpty(Ref.NodeName)
                    ? $"{AssetLabel} · Graph.{Ref.FieldName}"
                    : $"{AssetLabel} · node '{Ref.NodeName}' · {Ref.FieldName}";
                string tier = Registration == TryllModelRegistration.None
                    ? "Unregistered"
                    : Registration.ToString();
                return $"{where} → '{Ref.ModelName}' ({tier})";
            }
        }

        /// <summary>
        /// Walks <paramref name="graph"/> for every non-empty string field carrying
        /// <see cref="TryllModelTypeAttribute"/> (node params + graph DefaultModelName).
        /// </summary>
        public static List<ModelRef> Collect(TryllGraphDescription graph)
        {
            var result = new List<ModelRef>();
            if (graph == null) return result;

            CollectFromObject(graph, nodeName: string.Empty, result);

            if (graph.Nodes == null) return result;
            foreach (var node in graph.Nodes)
            {
                if (node?.Params == null) continue;
                CollectFromObject(node.Params, node.Name ?? string.Empty, result);
            }
            return result;
        }

        /// <summary>
        /// Model references of a voice profile (protocol v27: the TTS model lives on the
        /// voice, not on Speak / GenerateAndSpeak nodes). <see cref="ModelRef.NodeName"/> is
        /// <see cref="VoiceOwner"/>.
        /// </summary>
        public static List<ModelRef> Collect(TryllVoiceProfileData voice)
        {
            var result = new List<ModelRef>();
            CollectFromObject(voice, VoiceOwner, result);
            return result;
        }

        /// <summary>Voice-profile counterpart of <see cref="FindNonProductionRefs(TryllGraphDescription, TryllModelManifest, string)"/>.</summary>
        public static List<Violation> FindNonProductionRefs(
            TryllVoiceProfileData voice,
            TryllModelManifest manifest,
            string assetLabel)
            => NonProduction(Collect(voice), manifest, assetLabel);

        /// <summary>
        /// Returns a violation for every collected ref whose registration is not
        /// <see cref="TryllModelRegistration.Production"/>. Empty model names are ignored.
        /// </summary>
        public static List<Violation> FindNonProductionRefs(
            TryllGraphDescription graph,
            TryllModelManifest manifest,
            string assetLabel)
            => NonProduction(Collect(graph), manifest, assetLabel);

        private static List<Violation> NonProduction(
            List<ModelRef> refs, TryllModelManifest manifest, string assetLabel)
        {
            var violations = new List<Violation>();
            foreach (var r in refs)
            {
                if (string.IsNullOrEmpty(r.ModelName)) continue;
                var reg = manifest != null
                    ? manifest.Get(r.ModelName)
                    : TryllModelRegistration.None;
                if (reg != TryllModelRegistration.Production)
                    violations.Add(new Violation(assetLabel, r, reg));
            }
            return violations;
        }

        /// <summary>
        /// True when any collected/Production STT model is referenced (or present as a
        /// Production STT entry on the graph) but the manifest has no Production VAD.
        /// The server's <c>default_vad_model</c> is required for offline STT endpointing.
        /// </summary>
        public static bool NeedsProductionVad(
            IEnumerable<ModelRef> refs,
            TryllModelManifest manifest,
            out string reason)
        {
            reason = null;
            bool hasSttRef = false;
            foreach (var r in refs)
            {
                if (string.IsNullOrEmpty(r.ModelName)) continue;
                if (!string.Equals(r.ModelType, "stt", StringComparison.OrdinalIgnoreCase))
                    continue;
                // Any STT ref means the packaged game needs a VAD companion staged too.
                hasSttRef = true;
                break;
            }
            if (!hasSttRef) return false;

            if (manifest != null)
            {
                foreach (var e in manifest.Entries)
                {
                    if (e.Registration == TryllModelRegistration.Production
                        && string.Equals(e.ModelType, "vad", StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            reason =
                "A workflow references an STT model, but no VAD model is marked Production " +
                "in the Tryll Model Manifest. Offline STT needs the server's default_vad_model " +
                "(typically Silero VAD) staged into the build — mark a VAD model as Production " +
                "in the Model Manager, or remove the STT reference.";
            return true;
        }

        public static string FormatBuildFailure(
            IReadOnlyList<Violation> violations,
            string vadReason)
        {
            var sb = new StringBuilder();
            sb.Append("[Tryll] Build aborted: the player would ship without models it needs:\n");
            foreach (var v in violations)
                sb.Append("  - ").Append(v).Append('\n');
            if (!string.IsNullOrEmpty(vadReason))
                sb.Append("  - ").Append(vadReason).Append('\n');
            sb.Append("Mark each required model Production in the Tryll Model Manager, or remove ");
            sb.Append("the reference from the workflow or voice profile, then build again.");
            return sb.ToString();
        }

        private static void CollectFromObject(object obj, string nodeName, List<ModelRef> into)
        {
            if (obj == null) return;
            var type = obj.GetType();
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var field in type.GetFields(flags))
            {
                if (field.FieldType != typeof(string)) continue;
                var attr = field.GetCustomAttribute<TryllModelTypeAttribute>();
                if (attr == null) continue;
                string value = field.GetValue(obj) as string;
                if (string.IsNullOrEmpty(value)) continue;
                into.Add(new ModelRef(nodeName, field.Name, value, attr.ModelType));
            }
        }
    }
}
