// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Internal logging wrapper. Prepends [Tryll] to every message so log entries
    /// are easy to filter in the Unity Console.
    /// </summary>
    internal static class TryllLog
    {
        public static void Info(string message)
            => Debug.Log($"[Tryll] {message}");

        public static void Warning(string message)
            => Debug.LogWarning($"[Tryll] {message}");

        public static void Error(string message)
            => Debug.LogError($"[Tryll] {message}");

        public static void Verbose(string message)
        {
#if TRYLL_LOG_VERBOSE
            Debug.Log($"[Tryll][V] {message}");
#endif
        }
    }
}
