// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Marks a serialized string field as a reference to a node name within the enclosing
    /// <see cref="TryllGraphDescription"/>.
    ///
    /// Editor: a custom <see cref="UnityEditor.PropertyDrawer"/> looks up the enclosing
    /// <see cref="TryllGraphDescription"/> and renders the field as a dropdown of valid
    /// node names.  Unlike <see cref="TryllExitAttribute"/>, there is no <c>&lt;End&gt;</c>
    /// option — the field must resolve to a real node.
    ///
    /// Currently applied to <see cref="TryllGraphDescription.StartNode"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TryllNodeNameAttribute : PropertyAttribute { }
}
