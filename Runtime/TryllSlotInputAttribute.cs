// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Marks a serialized string field on a <see cref="TryllNodeParamsBase"/> subclass as
    /// selecting an upstream inter-node slot (e.g. <c>GenerateParams.Input</c>). Emitted by
    /// codegen (driven by the <c>(tryll_slot_input)</c> attribute in <c>nodes.fbs</c>).
    ///
    /// Editor: a custom <see cref="UnityEditor.PropertyDrawer"/> renders a dropdown with
    /// exactly two kinds of entries — "User Message" and one "Slot &lt;X&gt;" per slot
    /// reachable upstream of the owning node on some path — no free-text mode.
    /// Empty value = "user_message" (every input-bearing node shares this default).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TryllSlotInputAttribute : PropertyAttribute
    {
    }
}
