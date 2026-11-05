// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Marks a serialized string field on a <see cref="TryllNodeParamsBase"/> subclass as
    /// a storage filename (relative to the process storage root configured in
    /// <see cref="TryllRuntimeSettings.StorageDataFolder"/> / passed as
    /// <c>--storage-root</c>).  Emitted by codegen (driven by the <c>(file_picker)</c>
    /// attribute in <c>nodes.fbs</c>) on every per-node params field that references a
    /// string storage or embedded string storage, and on storage paths inside nested
    /// tables (such as <c>TryllLoraAdapter.Path</c>).
    ///
    /// Editor: a custom <see cref="UnityEditor.PropertyDrawer"/> renders the field as a
    /// file-path input that validates the path is relative (no leading slash, no
    /// <c>..</c> segments) and shows a browse button for convenient selection.
    ///
    /// Wire: the string value is sent as-is in <c>CreateAgentRequest</c>; the server
    /// resolves it against the process storage root at agent-creation time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TryllFilePickerAttribute : PropertyAttribute
    {
    }
}
