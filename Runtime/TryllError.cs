// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;

namespace Tryll.Client
{
    /// <summary>
    /// Error value returned by Tryll API calls and delivered to event delegates.
    /// Option A design: a value type — symmetric with the C++, Unreal, and Python clients.
    ///
    /// All <c>…Async</c> methods return a <c>(result, TryllError)</c> tuple;
    /// callers must check <see cref="IsOk"/> before using the result.
    /// Streaming / event surfaces deliver errors as a <c>TryllError</c> value directly.
    /// </summary>
    [Serializable]
    public struct TryllError
    {
        /// <summary>Numeric code. 0 = OK. See <see cref="TryllErrorCode"/> for named values.</summary>
        public int    Code;

        /// <summary>Human-readable description. Safe for display and logs.</summary>
        public string Message;

        /// <summary>True when <see cref="Code"/> is 0 (no error).</summary>
        public bool IsOk => Code == 0;

        public TryllError(TryllErrorCode code, string message)
        {
            Code    = (int)code;
            Message = message;
        }

        public TryllError(uint code, string message)
        {
            Code    = (int)code;
            Message = message;
        }

        public static TryllError Ok() => default;

        public override string ToString()
            => IsOk ? "TryllError(Ok)" : $"TryllError({Code}: {Message})";
    }
}
