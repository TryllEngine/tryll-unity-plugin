// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Section header above the first field of each <see cref="TryllGroupAttribute"/> group
    /// in the plain Inspector. The workflow graph window sets <see cref="Suppress"/> while it
    /// draws params, because it renders the sections itself (Advanced folded, no Exits).
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllGroupAttribute))]
    internal sealed class TryllGroupDecorator : DecoratorDrawer
    {
        internal static bool Suppress;

        private bool Visible => !Suppress && ((TryllGroupAttribute)attribute).First;

        public override float GetHeight()
        {
            return Visible ? EditorGUIUtility.singleLineHeight + 6f : 0f;
        }

        public override void OnGUI(Rect position)
        {
            if (!Visible) return;
            position.yMin += 6f;
            EditorGUI.LabelField(position, ((TryllGroupAttribute)attribute).Name, EditorStyles.boldLabel);
        }
    }
}
