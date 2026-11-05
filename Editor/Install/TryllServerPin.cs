// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// The server build a released package expects: <c>Editor/Install/TryllServerPin.json</c>,
    /// written by the release pipeline (<c>release.py --unity-export</c>) and never by hand.
    /// It names each server zip with its size and SHA-256, so the download is trusted
    /// because the Git tag the user installed says so, not because of the download host.
    /// A development package has no pin; it runs the server bundled in <c>.Server/</c>.
    /// </summary>
    [Serializable]
    internal sealed class TryllServerPin
    {
        internal const string FileName = "TryllServerPin.json";
        internal const string BaseUrlEnvVar = "TRYLL_SERVER_BASE_URL";

        public int schema;
        public string version;
        public int protocol_version;
        /// <summary>Folder URL the zips are downloaded from, ending with '/'. The release
        /// publish step sets it to the plugin repo's release; empty in a staged-only tree.</summary>
        public string base_url;
        /// <summary><c>public</c>, <c>test</c> or <c>dev</c>; tags the install event.</summary>
        public string channel;
        public Variant[] variants;

        [Serializable]
        internal sealed class Variant
        {
            public string name;
            public string file;
            public long size;
            public string sha256;
        }

        internal Variant Find(string name)
        {
            if (variants == null) return null;
            foreach (var v in variants)
                if (v != null && string.Equals(v.name, name, StringComparison.Ordinal))
                    return v;
            return null;
        }

        internal Variant FindBySha256(string sha256)
        {
            if (variants == null || string.IsNullOrEmpty(sha256)) return null;
            foreach (var v in variants)
                if (v != null && string.Equals(v.sha256, sha256, StringComparison.OrdinalIgnoreCase))
                    return v;
            return null;
        }

        /// <summary><c>TRYLL_SERVER_BASE_URL</c> wins over the pinned URL (CI smoke runs, tests).</summary>
        internal string EffectiveBaseUrl()
        {
            string overridden = Environment.GetEnvironmentVariable(BaseUrlEnvVar);
            string url = !string.IsNullOrEmpty(overridden) ? overridden : base_url;
            if (string.IsNullOrEmpty(url)) return "";
            return url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
        }

        static readonly Regex GitHubReleaseUrl =
            new Regex(@"^(?<repo>https://github\.com/[^/]+/[^/]+)/releases/", RegexOptions.IgnoreCase);

        /// <summary>
        /// The GitHub repo a release download URL belongs to
        /// (<c>https://github.com/org/repo/releases/download/v1.2.3/</c> gives
        /// <c>https://github.com/org/repo</c>), or "" for any other URL.
        /// </summary>
        internal static string RepoFromBaseUrl(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
                return "";
            var match = GitHubReleaseUrl.Match(baseUrl);
            return match.Success ? match.Groups["repo"].Value : "";
        }

        internal static TryllServerPin Parse(string json)
        {
            var pin = JsonUtility.FromJson<TryllServerPin>(json);
            if (pin == null || string.IsNullOrEmpty(pin.version) || pin.variants == null || pin.variants.Length == 0)
                throw new FormatException("server pin has no version or no variants");
            return pin;
        }

        /// <summary>The pin shipped in this package, or null (development package).</summary>
        internal static TryllServerPin LoadForPackage()
        {
            string path = PackagePath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            try
            {
                return Parse(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                TryllLog.Warning($"Ignoring unreadable server pin '{path}': {ex.Message}");
                return null;
            }
        }

        internal static string PackagePath()
        {
            string serverRoot = Internal.TryllServerProcess.EditorPackageServerRoot();
            string packageRoot = string.IsNullOrEmpty(serverRoot) ? null : Path.GetDirectoryName(serverRoot);
            return string.IsNullOrEmpty(packageRoot)
                ? null
                : Path.Combine(packageRoot, "Editor", "Install", FileName);
        }
    }
}
