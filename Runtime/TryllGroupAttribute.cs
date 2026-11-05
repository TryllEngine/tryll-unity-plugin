// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Editor section of a field on a <see cref="TryllNodeParamsBase"/> subclass. Emitted by
    /// codegen from the <c>(group)</c> attribute in <c>nodes.fbs</c>; fields are declared
    /// group by group (Input, Model, Prompt, Storage, Behavior, Output, Advanced, Exits).
    ///
    /// Editor: the Inspector draws a section header above the field marked
    /// <see cref="First"/>. The workflow graph window draws its own sections instead —
    /// Advanced folded, Exits left to the canvas ports.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TryllGroupAttribute : PropertyAttribute
    {
        public const string Advanced = "Advanced";
        public const string Exits    = "Exits";

        /// <summary>Section name, as written in <c>nodes.fbs</c>.</summary>
        public string Name { get; }

        /// <summary>True on the first field of the section, which carries its header.</summary>
        public bool First { get; set; }

        public TryllGroupAttribute(string name)
        {
            Name = name ?? string.Empty;
        }
    }
}
