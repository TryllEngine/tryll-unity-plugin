// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.IO;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// Per-user folder where the Editor installs the Tryll server when the package does
    /// not bundle one (a Git URL install). Layout:
    /// <c>%LOCALAPPDATA%\Tryll\server\&lt;version&gt;\&lt;variant&gt;\tryll_server.exe</c>,
    /// with variant <c>full</c> (CUDA + Vulkan) or <c>lite</c> (CPU + Vulkan).
    ///
    /// <para>
    /// The folder is outside the read-only package cache, so a server that writes next
    /// to its exe still works, and every project on the machine shares one copy per
    /// version. <c>TRYLL_SERVER_CACHE</c> overrides the root (tests, CI smoke runs).
    /// </para>
    /// </summary>
    internal static class TryllServerCache
    {
        internal const string RootEnvVar = "TRYLL_SERVER_CACHE";
        internal const string FullVariant = "full";
        internal const string LiteVariant = "lite";

        /// <summary>Variants in preference order: a full install wins over lite.</summary>
        internal static readonly string[] VariantsByPreference = { FullVariant, LiteVariant };

        /// <summary>Root of the cache. Never null; may not exist yet.</summary>
        internal static string Root
        {
            get
            {
                string overridden = Environment.GetEnvironmentVariable(RootEnvVar);
                if (!string.IsNullOrEmpty(overridden))
                    return Path.GetFullPath(overridden);
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(local, "Tryll", "server");
            }
        }

        internal static string VersionDir(string version) => Path.Combine(Root, Sanitize(version));

        internal static string VariantDir(string version, string variant)
            => Path.Combine(VersionDir(version), variant);

        /// <summary>
        /// Folder of an installed server for <paramref name="version"/> (full preferred),
        /// or null when none is installed.
        /// </summary>
        internal static string FindInstalled(string version) => FindInstalled(version, out _);

        internal static string FindInstalled(string version, out string variant)
        {
            variant = null;
            if (string.IsNullOrEmpty(version))
                return null;
            foreach (string candidate in VariantsByPreference)
            {
                string dir = VariantDir(version, candidate);
                if (File.Exists(Path.Combine(dir, TryllServerProcess.ExeName)))
                {
                    variant = candidate;
                    return dir;
                }
            }
            return null;
        }

        /// <summary>A version string is used as a folder name; refuse path tricks.</summary>
        internal static string Sanitize(string version)
        {
            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("version is empty");
            foreach (char c in version)
            {
                bool ok = char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '+' || c == '_';
                if (!ok)
                    throw new ArgumentException($"version '{version}' has an unsafe character '{c}'");
            }
            if (version == "." || version == "..")
                throw new ArgumentException($"version '{version}' is not a folder name");
            return version;
        }
    }
}
