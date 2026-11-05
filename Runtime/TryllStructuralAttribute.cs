// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Marks a field on a <see cref="TryllNodeParamsBase"/> subclass as structural —
    /// immutable after agent construction. Emitted by codegen (driven by the
    /// <c>(structural)</c> attribute in <c>nodes.fbs</c>).
    ///
    /// Server: mutations via <c>ChangeAgentParam</c> return ParamNotMutable (3006).
    /// Editor: Dialog Lab and similar override UIs exclude these fields so they never
    /// offer a runtime change the server will reject.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TryllStructuralAttribute : PropertyAttribute
    {
    }
}
