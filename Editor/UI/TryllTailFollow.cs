// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Host adapter for <see cref="TryllTailFollowController"/>. Chat and Agent Log
    /// inject measure + scroll-to-end; this type owns suppress and the contract threshold.
    /// </summary>
    internal sealed class TryllTailFollow
    {
        readonly TryllTailFollowController _controller = new TryllTailFollowController();
        readonly Func<float> _distanceFromBottom;
        readonly Action _scrollToEnd;
        bool _suppressUserScroll;

        public TryllTailFollow(Func<float> distanceFromBottom, Action scrollToEnd)
        {
            _distanceFromBottom = distanceFromBottom
                ?? throw new ArgumentNullException(nameof(distanceFromBottom));
            _scrollToEnd = scrollToEnd ?? throw new ArgumentNullException(nameof(scrollToEnd));
        }

        public TryllTailFollowController.FollowState State => _controller.State;

        /// <summary>
        /// Measures remaining scroll for a <see cref="ScrollView"/>. Content shorter
        /// than the viewport is treated as already at the tail (distance 0).
        /// </summary>
        public static float DistanceFromBottom(ScrollView scroll)
        {
            if (scroll == null) return 0f;
            float content = scroll.contentContainer.layout.height;
            float viewport = scroll.contentViewport.layout.height;
            return DistanceFromBottom(content, viewport, scroll.scrollOffset.y);
        }

        public static float DistanceFromBottom(ListView list) =>
            DistanceFromBottom(list?.Q<ScrollView>());

        public static float DistanceFromBottom(float contentHeight, float viewportHeight, float offsetY)
        {
            if (contentHeight <= viewportHeight) return 0f;
            return Mathf.Max(0f, contentHeight - viewportHeight - offsetY);
        }

        public void NotifyUserScroll()
        {
            if (_suppressUserScroll) return;
            _controller.OnUserScroll(_distanceFromBottom(), TryllEditorUiContract.FollowTailThreshold);
        }

        public void NotifyAppend()
        {
            if (_controller.OnAppend())
                _scrollToEnd();
        }

        public void ResetFollowing()
        {
            _controller.ResetFollowing();
            _scrollToEnd();
        }

        public void BeginProgrammaticScroll() => _suppressUserScroll = true;

        public void EndProgrammaticScroll() => _suppressUserScroll = false;
    }
}
