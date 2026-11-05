// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.IO;
using System.Text.RegularExpressions;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Version numbers for the Welcome and Help &amp; Community windows, read live so the
    /// windows need no edit for a release. A released package carries the version the
    /// release pipeline stamps into <see cref="TryllVersion.Version"/>. A development
    /// checkout reports the <c>0.0.0</c> placeholder, so the number comes from the
    /// repo-root <c>VERSION</c> file the pipeline stamps from. The same repo root gives
    /// <see cref="TryllLicence"/> the licence the pipeline copies into a released package.
    /// </summary>
    internal static class TryllVersionLookup
    {
        internal const string Unstamped = "0.0.0";
        internal const string RepoVersionFileName = "VERSION";
        /// <summary>The licence source, relative to the repo root (ci/release.toml [paths].licence_file).</summary>
        internal const string RepoLicenceFile = "docs/legal/LICENSE.md";

        // A package inside the repo sits at <repo>/UnityPlugin/<Project>/Packages/com.tryll.client.
        const int MaxRepoDepth = 6;
        static readonly Regex VersionNumber = new Regex(@"^[0-9]+\.[0-9]+\.[0-9]+([\-+][0-9A-Za-z.\-+]*)?$");

        static string _repoRoot;
        static bool _repoRootRead;

        /// <summary>The Tryll client (this package) version number.</summary>
        internal static string Client()
            => TryllVersion.Version != Unstamped ? TryllVersion.Version : RepoVersion() ?? TryllVersion.Version;

        /// <summary>
        /// The Tryll server version number: what the connected server reports, else the
        /// server's own manifest, else (a server bundled in a development checkout) the
        /// repo <c>VERSION</c>. Empty when nothing is known.
        /// </summary>
        internal static string Server(TryllServerInfo info)
        {
            var client = TryllClient.Instance;
            if (client != null && client.IsConnected && !string.IsNullOrEmpty(client.ServerVersion))
                return client.ServerVersion;
            if (info == null)
                return "";
            if (!string.IsNullOrEmpty(info.Version))
                return info.Version;
            if (info.Source == TryllServerInfo.SourceKind.Bundled)
                return RepoVersion() ?? "";
            return "";
        }

        // Host only: the release stamper rewrites every "https://<host>/" prefix it finds.
        const string DocsHost = "docs.tryllengine.com";
        static readonly Regex VersionedDocsPath = new Regex(@"^/v[0-9]+\.[0-9]+/");
        static readonly Regex MajorMinor = new Regex(@"^(?<major>[0-9]+)\.(?<minor>[0-9]+)\.");

        /// <summary>
        /// The docs site keeps one folder per version (<c>/v0.9/</c>). The release pipeline
        /// pins every docs URL in a released package (<c>rewrite_docs_base_url</c> in
        /// ci/scripts/release.py). A development checkout is not stamped, so pin its docs
        /// URLs here the same way, from <see cref="Client"/>. Other URLs pass through.
        /// </summary>
        internal static string PinDocsUrl(string url)
        {
            if (TryllVersion.Version != Unstamped || string.IsNullOrEmpty(url))
                return url;   // a released package's URLs are pinned already
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || uri.Host != DocsHost
                || VersionedDocsPath.IsMatch(uri.AbsolutePath))
                return url;
            string client = Client();
            var match = MajorMinor.Match(client);
            if (client == Unstamped || !match.Success)
                return url;   // no repo VERSION either: there is no folder to name
            string prefix = uri.GetLeftPart(UriPartial.Authority);
            return prefix + "/v" + match.Groups["major"].Value + "." + match.Groups["minor"].Value
                + url.Substring(prefix.Length);
        }

        /// <summary>
        /// The repo root above this package, or null for a released (stamped) package or one
        /// outside a development checkout. Read once per domain.
        /// </summary>
        internal static string RepoRoot()
        {
            if (_repoRootRead)
                return _repoRoot;
            _repoRootRead = true;
            _repoRoot = TryllVersion.Version != Unstamped
                ? null
                : FindRepoRoot(Path.GetDirectoryName(TryllServerProcess.EditorPackageServerRoot()));
            return _repoRoot;
        }

        /// <summary>The repo-root <c>VERSION</c> above this package, or null outside a development checkout.</summary>
        internal static string RepoVersion()
        {
            string root = RepoRoot();
            return root == null ? null : ReadVersionFile(root);
        }

        /// <summary>The repo's <c>docs/legal/LICENSE.md</c> above this package, or null when there is none.</summary>
        internal static string RepoLicencePath()
        {
            string root = RepoRoot();
            if (root == null)
                return null;
            string path = Path.Combine(root, RepoLicenceFile);
            return File.Exists(path) ? path : null;
        }

        /// <summary>The version in the first readable VERSION file above <paramref name="start"/>.</summary>
        internal static string FindRepoVersion(string start)
        {
            string root = FindRepoRoot(start);
            return root == null ? null : ReadVersionFile(root);
        }

        /// <summary>Walks up from <paramref name="start"/> to the first folder whose VERSION holds a version number.</summary>
        internal static string FindRepoRoot(string start)
        {
            string dir = start;
            for (int i = 0; i <= MaxRepoDepth && !string.IsNullOrEmpty(dir); i++)
            {
                if (ReadVersionFile(dir) != null)
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        /// <summary>The version number in <paramref name="dir"/>/VERSION, or null.</summary>
        static string ReadVersionFile(string dir)
        {
            string path = Path.Combine(dir, RepoVersionFileName);
            if (!File.Exists(path))
                return null;
            try
            {
                string text = File.ReadAllText(path).Trim();
                return VersionNumber.IsMatch(text) ? text : null;
            }
            catch (Exception)
            {
                return null;   // unreadable: not a repo root
            }
        }
    }
}
