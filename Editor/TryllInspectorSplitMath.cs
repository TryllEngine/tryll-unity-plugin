// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Pure splitter clamp / collapse / persist policy. Host widgets
    /// <see cref="TryllInspectorSplit"/> / STryllInspectorSplit apply this with
    /// <see cref="DefaultHandleWidth"/>.
    /// </summary>
    internal static class TryllInspectorSplitMath
    {
        public const float CollapseWidth = 0f;
        public const float DefaultHandleWidth = 5f;

        public static float ClampFixedPane(
            float outerWidth, float storedWidth, float fixedMin, float flexMin, float handleWidth)
        {
            if (outerWidth <= 0f)
                return storedWidth;
            float maxWidth = Mathf.Max(fixedMin, outerWidth - flexMin - handleWidth);
            return Mathf.Clamp(storedWidth, fixedMin, maxWidth);
        }

        public static bool ShouldPersistWidth(float width, float fixedMin) =>
            width >= fixedMin;
    }
}
