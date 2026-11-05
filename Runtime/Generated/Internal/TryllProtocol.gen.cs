// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/common/include/tryll/Protocol.h
// See: docs/dev/codegen.md

namespace Tryll.Client
{
    public static partial class TryllProtocol
    {
        /// <summary>Current wire protocol version, sent in ConnectionReady.</summary>
        public const uint Version = 29;

        /// <summary>Max FlatBuffers payload bytes, excluding the 4-byte length prefix.</summary>
        public const uint MaxFramePayloadBytes = 1048576;
    }
}
