// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tryll.Client.Editor
{
    internal enum TryllSplitFixedSide
    {
        First = 0,
        Second = 1,
    }

    /// <summary>
    /// Two-pane split that owns §4.2: fixed side, persist, collapse-guard,
    /// clamp against the flex min, deferred collapse, and CollapseChild (never hide).
    /// </summary>
    internal sealed class TryllInspectorSplit : VisualElement
    {
        public const float HandleWidth = TryllInspectorSplitMath.DefaultHandleWidth;

        readonly TwoPaneSplitView _split;
        readonly VisualElement _primaryPane;
        readonly VisualElement _secondaryPane;
        readonly TryllSplitFixedSide _fixedSide;
        readonly Func<float> _primaryMin;
        readonly Func<float> _secondaryMin;
        readonly Func<float> _getWidth;
        readonly Action<float> _setWidth;
        readonly Func<bool> _getVisible;

        float _lastGoodWidth;
        bool _suppressPersist;

        public TryllInspectorSplit(
            VisualElement primary,
            VisualElement secondary,
            TryllSplitFixedSide fixedSide,
            Func<float> primaryMin,
            Func<float> secondaryMin,
            float defaultFixedWidth,
            Func<float> getWidth,
            Action<float> setWidth,
            Func<bool> getVisible)
        {
            _fixedSide = fixedSide;
            _primaryMin = primaryMin ?? throw new ArgumentNullException(nameof(primaryMin));
            _secondaryMin = secondaryMin ?? throw new ArgumentNullException(nameof(secondaryMin));
            _getWidth = getWidth ?? throw new ArgumentNullException(nameof(getWidth));
            _setWidth = setWidth ?? throw new ArgumentNullException(nameof(setWidth));
            _getVisible = getVisible ?? throw new ArgumentNullException(nameof(getVisible));

            style.flexGrow = 1;

            float fixedMin = FixedMin();
            float stored = _getWidth();
            float initial = Mathf.Max(fixedMin, stored > 0f ? stored : defaultFixedWidth);
            _lastGoodWidth = initial;

            _split = new TwoPaneSplitView(
                (int)_fixedSide, initial, TwoPaneSplitViewOrientation.Horizontal);
            _split.style.flexGrow = 1;

            // Wrap so a child's minWidth (turn inspector = 260) cannot hold the
            // collapsed slot open. Unreal does the same with SBox WidthOverride 0.
            _primaryPane = WrapPane(primary, _primaryMin());
            _secondaryPane = WrapPane(secondary, _secondaryMin());
            _split.Add(_primaryPane);
            _split.Add(_secondaryPane);
            Add(_split);

            _split.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<AttachToPanelEvent>(_ => ApplyInitialCollapse());
        }

        public void Collapse()
        {
            // Snapshot before zeroing constraints — UnCollapse() re-Inits from
            // fixedPaneInitialDimension (constructor value), not the last drag.
            SnapshotVisibleWidth();
            _suppressPersist = true;
            ApplyConstraints(visible: false);
            _split.CollapseChild((int)_fixedSide);
        }

        public void Restore()
        {
            float stored = ClampedStoredWidth(_split.resolvedStyle.width);
            _suppressPersist = true;
            ApplyConstraints(visible: true);
            ApplyRestoredWidth(stored);
            _split.UnCollapse();
            ApplyRestoredWidth(stored);
            _split.schedule.Execute(() => { _suppressPersist = false; }).ExecuteLater(1);
        }

        float FixedMin() =>
            _fixedSide == TryllSplitFixedSide.First ? _primaryMin() : _secondaryMin();

        float FlexMin() =>
            _fixedSide == TryllSplitFixedSide.First ? _secondaryMin() : _primaryMin();

        VisualElement FixedPane =>
            _fixedSide == TryllSplitFixedSide.First ? _primaryPane : _secondaryPane;

        VisualElement FlexPane =>
            _fixedSide == TryllSplitFixedSide.First ? _secondaryPane : _primaryPane;

        static VisualElement WrapPane(VisualElement child, float minWidth)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));
            var box = new VisualElement();
            box.style.flexGrow = 1;
            box.style.minWidth = minWidth;
            box.style.overflow = Overflow.Hidden;
            child.style.flexGrow = 1;
            child.style.flexShrink = 1;
            box.Add(child);
            return box;
        }

        void ApplyInitialCollapse()
        {
            if (_getVisible())
                return;
            // Dragline exists only after the first geometry pass.
            _split.schedule.Execute(Collapse).ExecuteLater(1);
        }

        void OnGeometryChanged(GeometryChangedEvent _)
        {
            bool visible = _getVisible();
            ApplyConstraints(visible);

            float outer = _split.resolvedStyle.width;
            if (!visible || outer <= 0f)
                return;

            if (_suppressPersist)
            {
                ApplyRestoredWidth(ClampedStoredWidth(outer));
                return;
            }

            float w = ReadFixedPaneWidth();
            if (TryllInspectorSplitMath.ShouldPersistWidth(w, FixedMin()))
                RememberWidth(w);
            else
                ApplyRestoredWidth(ClampedStoredWidth(outer));
        }

        void ApplyConstraints(bool visible)
        {
            float outer = _split.resolvedStyle.width;
            float fixedMin = FixedMin();
            float flexMin = FlexMin();

            style.minWidth = visible
                ? _primaryMin() + _secondaryMin() + HandleWidth
                : FlexMin();

            FlexPane.style.minWidth = flexMin;

            if (!visible)
            {
                // Do not keep reserving the inspector minimum: that left the
                // timeline ending at MinWidth from the right after hide.
                FixedPane.style.minWidth = TryllInspectorSplitMath.CollapseWidth;
                FixedPane.style.maxWidth = TryllInspectorSplitMath.CollapseWidth;
                FlexPane.style.maxWidth = StyleKeyword.None;
                return;
            }

            FixedPane.style.minWidth = fixedMin;
            if (outer <= 0f)
                return;

            // TwoPaneSplitView ignores the flex pane's minWidth while dragging.
            FlexPane.style.maxWidth = TryllInspectorSplitMath.ClampFixedPane(
                outer, outer, flexMin, fixedMin, HandleWidth);
            FixedPane.style.maxWidth = TryllInspectorSplitMath.ClampFixedPane(
                outer, outer, fixedMin, flexMin, HandleWidth);
        }

        void SnapshotVisibleWidth()
        {
            float w = ReadFixedPaneWidth();
            if (TryllInspectorSplitMath.ShouldPersistWidth(w, FixedMin()))
                RememberWidth(w);
        }

        void RememberWidth(float width)
        {
            _lastGoodWidth = width;
            _setWidth(width);
        }

        void ApplyRestoredWidth(float stored)
        {
            if (!Mathf.Approximately(_split.fixedPaneInitialDimension, stored))
                _split.fixedPaneInitialDimension = stored;
            FixedPane.style.width = stored;
            if (_split.fixedPane != null)
                _split.fixedPane.style.width = stored;
        }

        float ReadFixedPaneWidth()
        {
            if (_split.fixedPane != null)
            {
                float fromSplit = _split.fixedPane.resolvedStyle.width;
                if (fromSplit > 0f)
                    return fromSplit;
            }
            return FixedPane.resolvedStyle.width;
        }

        float ClampedStoredWidth(float outer)
        {
            float stored = _lastGoodWidth;
            if (!TryllInspectorSplitMath.ShouldPersistWidth(stored, FixedMin()))
                stored = _getWidth();
            if (stored <= 0f)
                stored = FixedMin();
            return TryllInspectorSplitMath.ClampFixedPane(
                outer, stored, FixedMin(), FlexMin(), HandleWidth);
        }
    }
}
