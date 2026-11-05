// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Scroll follow-tail state machine. Selection follow stays on
    /// <c>TryllChatTurnLogic.IsFollowingLatest</c>. Host adapters live in
    /// <see cref="TryllTailFollow"/>.
    /// </summary>
    internal sealed class TryllTailFollowController
    {
        public enum FollowState
        {
            Following,
            Suspended,
        }

        public FollowState State { get; private set; } = FollowState.Following;

        /// <summary>
        /// Updates follow state from the user's current distance to the bottom.
        /// Never requests a programmatic scroll — the user is already moving the view.
        /// </summary>
        public bool OnUserScroll(float distanceFromBottom, float threshold)
        {
            State = distanceFromBottom <= threshold
                ? FollowState.Following
                : FollowState.Suspended;
            return false;
        }

        /// <summary>
        /// Requests scroll-to-end only while already following. An append while
        /// suspended must not yank the view back to the tail.
        /// </summary>
        public bool OnAppend() => State == FollowState.Following;

        /// <summary>
        /// User started a new stream (send, agent switch). Resume following.
        /// </summary>
        public void ResetFollowing() => State = FollowState.Following;
    }
}
