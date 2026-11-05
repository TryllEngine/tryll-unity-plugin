// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Marks a generated string field as localizable. The editor draws the field
    /// name with a Localize text checkbox inside it; sibling fields
    /// <c>&lt;Name&gt;Localized</c> and <c>&lt;Name&gt;TextId</c>
    /// hold the choice. <c>Pack()</c> still writes this string — the client fills it
    /// from the text provider before send.
    /// </summary>
    public sealed class TryllLocalizableAttribute : PropertyAttribute
    {
        /// <summary>
        /// The inline value is a multi-line prompt. Drawn by this drawer:
        /// a field can have only one property drawer, and <c>[TextArea]</c>
        /// would otherwise take the slot and hide the Localized checkbox.
        /// </summary>
        public readonly bool Multiline;

        public TryllLocalizableAttribute(bool multiline = false)
        {
            Multiline = multiline;
        }
    }
}
