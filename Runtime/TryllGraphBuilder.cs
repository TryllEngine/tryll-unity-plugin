// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.
// Typed AddXxx partial methods are in TryllGraphBuilder.Nodes.cs (generated).

namespace Tryll.Client
{
    /// <summary>
    /// Fluent C# builder for <see cref="TryllGraphDescription"/>.
    ///
    /// Typed AddXxx methods are defined in the generated partial class
    /// <c>TryllGraphBuilder.Nodes.cs</c> produced by tryll_codegen.py (builder unity).
    ///
    /// Graph wiring lives on each node's typed params as
    /// <c>&lt;exit_name&gt;_exit</c> string fields (annotated with
    /// <see cref="TryllExitAttribute"/> on the generated <c>FooExit</c>
    /// properties).  Set them directly on the params object before adding
    /// the node; empty string = END.
    ///
    /// Example:
    /// <code>
    /// var graph = new TryllGraphBuilder()
    ///     .AddGenerate("gen", new TryllGenerateParams
    ///     {
    ///         ModelName    = "mymodel",
    ///         SystemPrompt = "You are helpful.",
    ///         // DefaultExit defaults to "" = END
    ///     })
    ///     .SetStartNode("gen")
    ///     .Build();
    /// </code>
    /// </summary>
    public sealed partial class TryllGraphBuilder
    {
        private readonly TryllGraphDescription _desc = new TryllGraphDescription();

        internal TryllGraphDescription Description => _desc;

        public TryllGraphBuilder SetStartNode(string name)
        {
            _desc.StartNode = name;
            return this;
        }

        public TryllGraphBuilder SetDefaultModelName(string name)
        {
            _desc.DefaultModelName = name;
            return this;
        }

        public TryllGraphDescription Build() => _desc;
    }
}
