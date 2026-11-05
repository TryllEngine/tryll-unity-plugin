// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/nodes.fbs  +  tools/codegen/templates/unity_builder.cs.j2
// See: docs/dev/codegen.md
namespace Tryll.Generated.Internal {
    internal static class _FpCheck_TryllGraphBuilder_Nodes {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    public partial class TryllGraphBuilder
    {
        /// <summary>Add a Generate node to the graph.</summary>
        public TryllGraphBuilder AddGenerate(string name, TryllGenerateParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a RegexGuardrail node to the graph.</summary>
        public TryllGraphBuilder AddRegexGuardrail(string name, TryllRegexGuardrailParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a CannedResponse node to the graph.</summary>
        public TryllGraphBuilder AddCannedResponse(string name, TryllCannedResponseParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a ToolCall node to the graph.</summary>
        public TryllGraphBuilder AddToolCall(string name, TryllToolCallParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a Retrieve node to the graph.</summary>
        public TryllGraphBuilder AddRetrieve(string name, TryllRetrieveParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a Instruction node to the graph.</summary>
        public TryllGraphBuilder AddInstruction(string name, TryllInstructionParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a ClassifyIntent node to the graph.</summary>
        public TryllGraphBuilder AddClassifyIntent(string name, TryllClassifyIntentParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a ClassifyIntentLLM node to the graph.</summary>
        public TryllGraphBuilder AddClassifyIntentLLM(string name, TryllClassifyIntentLLMParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a IntentToInstruction node to the graph.</summary>
        public TryllGraphBuilder AddIntentToInstruction(string name, TryllIntentToInstructionParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a GenerateAndSpeak node to the graph.</summary>
        public TryllGraphBuilder AddGenerateAndSpeak(string name, TryllGenerateAndSpeakParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a Speak node to the graph.</summary>
        public TryllGraphBuilder AddSpeak(string name, TryllSpeakParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a Pause node to the graph.</summary>
        public TryllGraphBuilder AddPause(string name, TryllPauseParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a Transform node to the graph.</summary>
        public TryllGraphBuilder AddTransform(string name, TryllTransformParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a Branch node to the graph.</summary>
        public TryllGraphBuilder AddBranch(string name, TryllBranchParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a WriteToMemory node to the graph.</summary>
        public TryllGraphBuilder AddWriteToMemory(string name, TryllWriteToMemoryParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a Summarize node to the graph.</summary>
        public TryllGraphBuilder AddSummarize(string name, TryllSummarizeParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

        /// <summary>Add a ClassifyAttributesLLM node to the graph.</summary>
        public TryllGraphBuilder AddClassifyAttributesLLM(string name, TryllClassifyAttributesLLMParams p)
        {
            _desc.Nodes.Add(new TryllNodeDescription { Name = name, Params = p });
            return this;
        }

    }
}
