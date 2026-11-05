// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Marks a serialized schema-name string field as a picker over the bundled
    /// server catalog (<c>data/schemas.json</c>). Emitted by codegen from the
    /// <c>(tryll_schema)</c> attribute in <c>nodes.fbs</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TryllSchemaAttribute : PropertyAttribute
    {
    }
}
