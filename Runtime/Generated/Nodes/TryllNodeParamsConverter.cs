// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/nodes.fbs  +  tools/codegen/templates/unity_converter.cs.j2
// See: docs/dev/codegen.md
using Google.FlatBuffers;

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_TryllNodeParamsConverter {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>Serialises a TryllNodeParamsBase to a FlatBuffers union offset.</summary>
    internal static class TryllNodeParamsConverter
    {
        public static int PackNodeParams(FlatBufferBuilder fbb, TryllNodeParamsBase p,
            out Tryll.NodeParams.NodeParams unionType)
        {
            switch (p)
            {
                case TryllGenerateParams typed:
                    unionType = Tryll.NodeParams.NodeParams.GenerateParams;
                    var (_, generateOff) = typed.Pack(fbb);
                    return generateOff;
                case TryllRegexGuardrailParams typed:
                    unionType = Tryll.NodeParams.NodeParams.RegexGuardrailParams;
                    var (_, regexguardrailOff) = typed.Pack(fbb);
                    return regexguardrailOff;
                case TryllCannedResponseParams typed:
                    unionType = Tryll.NodeParams.NodeParams.CannedResponseParams;
                    var (_, cannedresponseOff) = typed.Pack(fbb);
                    return cannedresponseOff;
                case TryllToolCallParams typed:
                    unionType = Tryll.NodeParams.NodeParams.ToolCallParams;
                    var (_, toolcallOff) = typed.Pack(fbb);
                    return toolcallOff;
                case TryllRetrieveParams typed:
                    unionType = Tryll.NodeParams.NodeParams.RetrieveParams;
                    var (_, retrieveOff) = typed.Pack(fbb);
                    return retrieveOff;
                case TryllInstructionParams typed:
                    unionType = Tryll.NodeParams.NodeParams.InstructionParams;
                    var (_, instructionOff) = typed.Pack(fbb);
                    return instructionOff;
                case TryllClassifyIntentParams typed:
                    unionType = Tryll.NodeParams.NodeParams.ClassifyIntentParams;
                    var (_, classifyintentOff) = typed.Pack(fbb);
                    return classifyintentOff;
                case TryllClassifyIntentLLMParams typed:
                    unionType = Tryll.NodeParams.NodeParams.ClassifyIntentLLMParams;
                    var (_, classifyintentllmOff) = typed.Pack(fbb);
                    return classifyintentllmOff;
                case TryllIntentToInstructionParams typed:
                    unionType = Tryll.NodeParams.NodeParams.IntentToInstructionParams;
                    var (_, intenttoinstructionOff) = typed.Pack(fbb);
                    return intenttoinstructionOff;
                case TryllGenerateAndSpeakParams typed:
                    unionType = Tryll.NodeParams.NodeParams.GenerateAndSpeakParams;
                    var (_, generateandspeakOff) = typed.Pack(fbb);
                    return generateandspeakOff;
                case TryllSpeakParams typed:
                    unionType = Tryll.NodeParams.NodeParams.SpeakParams;
                    var (_, speakOff) = typed.Pack(fbb);
                    return speakOff;
                case TryllPauseParams typed:
                    unionType = Tryll.NodeParams.NodeParams.PauseParams;
                    var (_, pauseOff) = typed.Pack(fbb);
                    return pauseOff;
                case TryllTransformParams typed:
                    unionType = Tryll.NodeParams.NodeParams.TransformParams;
                    var (_, transformOff) = typed.Pack(fbb);
                    return transformOff;
                case TryllBranchParams typed:
                    unionType = Tryll.NodeParams.NodeParams.BranchParams;
                    var (_, branchOff) = typed.Pack(fbb);
                    return branchOff;
                case TryllWriteToMemoryParams typed:
                    unionType = Tryll.NodeParams.NodeParams.WriteToMemoryParams;
                    var (_, writetomemoryOff) = typed.Pack(fbb);
                    return writetomemoryOff;
                case TryllSummarizeParams typed:
                    unionType = Tryll.NodeParams.NodeParams.SummarizeParams;
                    var (_, summarizeOff) = typed.Pack(fbb);
                    return summarizeOff;
                case TryllClassifyAttributesLLMParams typed:
                    unionType = Tryll.NodeParams.NodeParams.ClassifyAttributesLLMParams;
                    var (_, classifyattributesllmOff) = typed.Pack(fbb);
                    return classifyattributesllmOff;
                default:
                    unionType = Tryll.NodeParams.NodeParams.NONE;
                    return 0;
            }
        }
    }
}
