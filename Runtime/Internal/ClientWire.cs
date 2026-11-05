// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.
//
// Port of server/clients/unreal/Source/TryllClient/Private/ClientWire.cpp
// Wire framing: 4-byte little-endian length prefix + FlatBuffers payload.

using System;
using System.IO;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// Low-level framing helpers. Called only from <see cref="TryllSocketWorker"/>.
    ///
    /// Frame-size cap comes from Protocol.h via TryllProtocol.MaxFramePayloadBytes.
    /// LengthPrefixBytes is 4.
    /// </summary>
    internal static class ClientWire
    {
        public const uint MaxFramePayloadBytes         = Tryll.Client.TryllProtocol.MaxFramePayloadBytes;
        public const uint MaxDialogSnapshotBytes       = MaxFramePayloadBytes - 2048;
        public const uint MaxMutableKbSnapshotBytes    = MaxFramePayloadBytes - 2048;
        public const uint MaxGameMemorySnapshotBytes   = 64u * 1024u * 1024u;
        public const int  LengthPrefixBytes            = 4;

        /// <summary>
        /// Distinguishes a locally detected size violation from a dead socket.
        /// Only <see cref="IoError"/> should tear down the connection.
        /// </summary>
        public enum WriteFrameResult
        {
            Ok,
            TooLarge,
            IoError,
        }

        /// <summary>
        /// Read one framed message from <paramref name="stream"/> into a fresh byte array.
        /// Returns null on any I/O error or protocol violation.
        /// </summary>
        public static byte[] ReadFrame(Stream stream)
        {
            // Read the 4-byte LE length prefix.
            var lenBuf = new byte[LengthPrefixBytes];
            if (!ReadExact(stream, lenBuf, 0, LengthPrefixBytes))
                return null;

            uint len = ReadLE32(lenBuf, 0);
            if (len == 0 || len > MaxFramePayloadBytes)
                return null;

            var payload = new byte[(int)len];
            if (!ReadExact(stream, payload, 0, (int)len))
                return null;

            return payload;
        }

        /// <summary>
        /// Write one framed message to <paramref name="stream"/>.
        /// <see cref="WriteFrameResult.TooLarge"/> is a local input error — the
        /// caller must fail that request and keep the socket. <see cref="WriteFrameResult.IoError"/>
        /// means the socket is gone.
        /// </summary>
        public static WriteFrameResult WriteFrame(Stream stream, byte[] payload)
        {
            if (payload == null || payload.Length == 0 || (uint)payload.Length > MaxFramePayloadBytes)
                return WriteFrameResult.TooLarge;

            var lenBuf = new byte[LengthPrefixBytes];
            WriteLE32(lenBuf, 0, (uint)payload.Length);

            try
            {
                stream.Write(lenBuf, 0, LengthPrefixBytes);
                stream.Write(payload, 0, payload.Length);
                stream.Flush();
                return WriteFrameResult.Ok;
            }
            catch
            {
                return WriteFrameResult.IoError;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Blocking read of exactly <paramref name="count"/> bytes.
        /// Equivalent to the RecvExact helper in ClientWire.cpp.
        /// </summary>
        private static bool ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = stream.Read(buffer, offset + total, count - total);
                if (read <= 0) return false;
                total += read;
            }
            return true;
        }

        private static uint ReadLE32(byte[] buf, int offset)
            => (uint)buf[offset]
             | ((uint)buf[offset + 1] << 8)
             | ((uint)buf[offset + 2] << 16)
             | ((uint)buf[offset + 3] << 24);

        private static void WriteLE32(byte[] buf, int offset, uint value)
        {
            buf[offset]     = (byte)value;
            buf[offset + 1] = (byte)(value >> 8);
            buf[offset + 2] = (byte)(value >> 16);
            buf[offset + 3] = (byte)(value >> 24);
        }
    }
}
