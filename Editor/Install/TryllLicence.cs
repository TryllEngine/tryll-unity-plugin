// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Tryll.Client.Internal;
using UnityEditor;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// The Tryll Runtime Licence: the text of <c>LICENSE.md</c> in the package root (next to
    /// <c>package.json</c>) and the person's acceptance of it. An unstamped package in a
    /// development checkout has no such file; it reads the repo's <c>docs/legal/LICENSE.md</c>,
    /// which the release copies in (<see cref="TryllVersionLookup.RepoLicencePath"/>). The licence includes telemetry,
    /// which is always on, so there is no separate usage-data choice.
    ///
    /// The acceptance is user-global EditorPrefs (no project hash), so it follows the person
    /// across projects, per the persistence table in <c>docs/dev/editor-window-checklist.md</c>.
    /// It stores the licence version, a hash of the text: any change to the text asks again.
    /// The editor starts no server until the current version is accepted
    /// (<see cref="TryllLicenceGate"/>).
    /// </summary>
    internal static class TryllLicence
    {
        internal const string AcceptedVersionKey = "Tryll.Welcome.LicenceAcceptedVersion";
        internal const string AcceptedAtKey = "Tryll.Welcome.LicenceAcceptedAt";
        internal const string InstallIdKey = "Tryll.Welcome.InstallId";
        internal const string FileName = "LICENSE.md";

        /// <summary>Version of a package without <c>LICENSE.md</c> (evaluation only).</summary>
        internal const string EvaluationVersion = "evaluation-draft";

        /// <summary>Keys of the cancelled usage-data step; deleted once when found.</summary>
        internal static readonly string[] StaleKeys =
        {
            "Tryll.Welcome.TelemetryConsent",
            "Tryll.Welcome.TelemetryConsentAt",
        };

        /// <summary>Raised on the main thread after Accept or Withdraw.</summary>
        internal static event Action Changed;

        /// <summary>Test seam: path of a substitute <c>LICENSE.md</c>. Null uses the package's file.</summary>
        internal static string DebugFileForTests;

        /// <summary>Test seam: replaces stopping the editor's server on Withdraw.</summary>
        internal static Action DebugStopServerForTests;

        /// <summary>
        /// <c>LICENSE.md</c> in the package root, else the repo's licence in a development
        /// checkout, or null when the root is unknown.
        /// </summary>
        internal static string FilePath
        {
            get
            {
                if (DebugFileForTests != null)
                    return DebugFileForTests;
                string serverRoot = TryllServerProcess.EditorPackageServerRoot();
                string packageRoot = string.IsNullOrEmpty(serverRoot) ? null : Path.GetDirectoryName(serverRoot);
                string packageFile = string.IsNullOrEmpty(packageRoot) ? null : Path.Combine(packageRoot, FileName);
                if (packageFile != null && File.Exists(packageFile))
                    return packageFile;
                return TryllVersionLookup.RepoLicencePath() ?? packageFile;
            }
        }

        /// <summary>True when the package has a <c>LICENSE.md</c>.</summary>
        internal static bool HasFile
        {
            get
            {
                string path = FilePath;
                return !string.IsNullOrEmpty(path) && File.Exists(path);
            }
        }

        /// <summary>The licence text, or "" when the package has no readable <c>LICENSE.md</c>.</summary>
        internal static string LoadText() => ReadFile() ?? "";

        /// <summary>
        /// Version of the current licence text: <see cref="VersionOf"/> of <c>LICENSE.md</c>, or
        /// <see cref="EvaluationVersion"/> when the package has none.
        /// </summary>
        internal static string CurrentVersion
        {
            get
            {
                string text = ReadFile();
                return text == null ? EvaluationVersion : VersionOf(text);
            }
        }

        /// <summary>
        /// Stable version of a licence text: <c>sha256:</c> and the first 16 hex digits of the
        /// SHA-256 of its UTF-8 bytes, after dropping a BOM, turning CRLF and CR into LF and
        /// trimming trailing whitespace. A checkout's line endings never change the version.
        /// </summary>
        internal static string VersionOf(string text)
        {
            string normalized = (text ?? "").TrimStart('﻿')
                .Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd();
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                var sb = new StringBuilder("sha256:", 7 + 16);
                for (int i = 0; i < 8; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>True when the current licence version is accepted.</summary>
        internal static bool Accepted => EditorPrefs.GetString(AcceptedVersionKey, "") == CurrentVersion;

        /// <summary>
        /// Accept stores the current version. Withdraw removes it and stops the server this
        /// editor started; later starts are blocked until the person accepts again.
        /// </summary>
        internal static void SetAccepted(bool accepted)
        {
            if (accepted)
            {
                EditorPrefs.SetString(AcceptedVersionKey, CurrentVersion);
                EditorPrefs.SetString(AcceptedAtKey, DateTime.UtcNow.ToString("o"));
            }
            else
            {
                EditorPrefs.DeleteKey(AcceptedVersionKey);
                EditorPrefs.DeleteKey(AcceptedAtKey);
                (DebugStopServerForTests ?? StopEditorServer)();
            }
            Changed?.Invoke();
        }

        /// <summary>
        /// Drop the shared connection, so every Tryll window sees a disconnect, and stop the
        /// server this editor started. A connect that is still trying is stopped too, so it
        /// cannot reach the server again. Kill also finds a server this editor started before
        /// a domain reload. A server another editor started is left alone.
        /// </summary>
        static void StopEditorServer()
        {
            if (ShouldDisconnect(TryllClient.Instance))
                TryllClient.Instance.Disconnect();
            TryllServerProcess.Kill();
        }

        /// <summary>True when the client is connected or still trying to connect.</summary>
        internal static bool ShouldDisconnect(TryllClient client)
        {
            if (client == null || client.Connection == null)
                return false;
            return client.IsConnected
                || client.HasConnectRetriesRemaining
                || client.IsSessionStartPending
                || client.Connection.IsWorkerAlive;
        }

        /// <summary>
        /// Random id for the plugin's own events. Made on first use and never derived from
        /// the machine (no MAC address, no hardware hash).
        /// </summary>
        internal static string InstallId
        {
            get
            {
                string id = EditorPrefs.GetString(InstallIdKey, "");
                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString("N");
                    EditorPrefs.SetString(InstallIdKey, id);
                }
                return id;
            }
        }

        /// <summary>Delete the keys of the cancelled usage-data step, if this person has them.</summary>
        internal static void DeleteStaleKeys()
        {
            foreach (string key in StaleKeys)
                if (EditorPrefs.HasKey(key))
                    EditorPrefs.DeleteKey(key);
        }

        static string ReadFile()
        {
            string path = FilePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                TryllLog.Warning($"Could not read the licence '{path}': {ex.Message}");
                return null;
            }
        }
    }
}
