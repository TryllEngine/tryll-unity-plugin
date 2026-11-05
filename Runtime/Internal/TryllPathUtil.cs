// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// Shared path-canonicalization helper used wherever a locally-resolved exe path
    /// must be compared against a server-reported <c>exe_path</c> (from
    /// <c>ConnectionReady</c>) — <see cref="TryllClient"/>'s post-connect identity
    /// check and <see cref="TryllServerProcess"/>'s editor known-port reuse probe.
    /// </summary>
    internal static class TryllPathUtil
    {
        /// <summary>
        /// Return the canonical absolute path with symlinks/junctions resolved,
        /// matching the server's <c>std::filesystem::weakly_canonical(exe_path)</c>.
        /// On Windows this uses <c>GetFinalPathNameByHandle</c>; elsewhere it falls
        /// back to <c>Path.GetFullPath</c> (the bundled server is Windows-only anyway).
        /// </summary>
        public static string Canonicalize(string p)
        {
            if (string.IsNullOrEmpty(p)) return p;
            try
            {
                string full = System.IO.Path.GetFullPath(p);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                string resolved = ResolveFinalWindowsPath(full);
                if (!string.IsNullOrEmpty(resolved))
                    return resolved;
#endif
                return full;
            }
            catch { return p; }
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // P/Invoke: resolve symlinks/junctions to the real on-disk path.
        // .NET Standard 2.1 (Unity's profile) lacks FileSystemInfo.ResolveLinkTarget,
        // so we call the Win32 API directly.
        private const uint _FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        private const uint _OPEN_EXISTING = 3;
        private const uint _FILE_SHARE_ALL = 0x1 | 0x2 | 0x4; // read | write | delete

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFileW(
            string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
            uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern uint GetFinalPathNameByHandleW(
            Microsoft.Win32.SafeHandles.SafeFileHandle hFile, System.Text.StringBuilder lpszFilePath,
            uint cchFilePath, uint dwFlags);

        private static string ResolveFinalWindowsPath(string path)
        {
            // dwDesiredAccess = 0 → query metadata only; works even on the running exe.
            using (var handle = CreateFileW(path, 0, _FILE_SHARE_ALL, IntPtr.Zero,
                _OPEN_EXISTING, _FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero))
            {
                if (handle == null || handle.IsInvalid)
                    return null;

                var sb = new System.Text.StringBuilder(512);
                uint len = GetFinalPathNameByHandleW(handle, sb, (uint)sb.Capacity, 0);
                if (len == 0) return null;
                if (len > sb.Capacity)
                {
                    sb = new System.Text.StringBuilder((int)len);
                    len = GetFinalPathNameByHandleW(handle, sb, (uint)sb.Capacity, 0);
                    if (len == 0) return null;
                }

                string result = sb.ToString();
                const string extPrefix = @"\\?\";
                if (result.StartsWith(extPrefix, StringComparison.Ordinal))
                    result = result.Substring(extPrefix.Length);
                return result;
            }
        }
#endif
    }
}
