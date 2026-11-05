// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.IO;
using Tryll.Client.Internal;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// The server the editor would start, read from its own files: a release zip carries
    /// <c>manifest.json</c> (version, protocol, variant, GPU backends), a local build carries
    /// <c>tryll-build.json</c> (version, build config). The Help &amp; Community window shows
    /// this, so it never needs editing for a release.
    /// </summary>
    internal sealed class TryllServerInfo
    {
        internal enum SourceKind
        {
            Missing,
            Bundled,    // the package's own .Server/ folder (development links, partner archives)
            Installed,  // the per-user download (TryllServerCache)
        }

        internal const string ReleaseManifestName = "manifest.json";
        internal const string BuildManifestName = "tryll-build.json";

        [Serializable]
        sealed class ManifestJson
        {
            public string version;
            public string commit;
            public int protocol_version;
            public string variant;
            public string[] gpu_backends;
            public string build_config;
        }

        internal SourceKind Source { get; private set; }
        internal string Version { get; private set; } = "";
        internal string Variant { get; private set; } = "";
        internal string BuildConfig { get; private set; } = "";
        internal int ProtocolVersion { get; private set; }
        internal string[] GpuBackends { get; private set; } = Array.Empty<string>();

        /// <summary>The server <see cref="TryllServerProcess.EditorServerDir"/> would start.</summary>
        internal static TryllServerInfo ForEditor()
        {
            if (TryllServerProcess.EditorHasBundledServer())
                return Read(TryllServerProcess.EditorServerDir(TryllEditorSettingsLookup.Find()), SourceKind.Bundled);
            string installed = TryllServerCache.FindInstalled(TryllVersion.Version, out string variant);
            if (installed == null)
                return new TryllServerInfo { Source = SourceKind.Missing };
            var info = Read(installed, SourceKind.Installed);
            if (string.IsNullOrEmpty(info.Variant))
                info.Variant = variant ?? "";
            return info;
        }

        /// <summary>Reads both manifests in <paramref name="dir"/>; a missing or broken file is skipped.</summary>
        internal static TryllServerInfo Read(string dir, SourceKind source)
        {
            var info = new TryllServerInfo { Source = source };
            if (string.IsNullOrEmpty(dir))
                return info;
            foreach (string name in new[] { ReleaseManifestName, BuildManifestName })
            {
                string path = Path.Combine(dir, name);
                if (!File.Exists(path))
                    continue;
                try
                {
                    info.Merge(JsonUtility.FromJson<ManifestJson>(File.ReadAllText(path)));
                }
                catch (Exception)
                {
                    // An unreadable manifest only hides detail; the server may still run.
                }
            }
            return info;
        }

        void Merge(ManifestJson m)
        {
            if (m == null)
                return;
            if (string.IsNullOrEmpty(Version) && !string.IsNullOrEmpty(m.version)) Version = m.version;
            if (string.IsNullOrEmpty(Variant) && !string.IsNullOrEmpty(m.variant)) Variant = m.variant;
            if (string.IsNullOrEmpty(BuildConfig) && !string.IsNullOrEmpty(m.build_config)) BuildConfig = m.build_config;
            if (ProtocolVersion == 0) ProtocolVersion = m.protocol_version;
            if (GpuBackends.Length == 0 && m.gpu_backends != null) GpuBackends = m.gpu_backends;
        }

        /// <summary>
        /// Technical values only, joined with " · " (<c>0.9.0 · lite · Vulkan</c>); empty when
        /// nothing is known. Sentences around it come from <see cref="TryllEditorUiContract"/>.
        /// </summary>
        internal string Describe()
        {
            string details = Details();
            if (string.IsNullOrEmpty(Version)) return details;
            return string.IsNullOrEmpty(details) ? Version : Version + " · " + details;
        }

        /// <summary>Like <see cref="Describe"/> without the version: <c>lite · Vulkan</c>.</summary>
        internal string Details()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Variant)) parts.Add(Variant);
            if (!string.IsNullOrEmpty(BuildConfig)) parts.Add(BuildConfig);
            foreach (string backend in GpuBackends)
                if (!string.IsNullOrEmpty(backend))
                    parts.Add(BackendName(backend));
            return string.Join(" · ", parts);
        }

        static string BackendName(string backend)
        {
            switch (backend.ToLowerInvariant())
            {
                case "cuda": return "CUDA";
                case "cpu": return "CPU";
                case "vulkan": return "Vulkan";
                default: return backend;
            }
        }
    }
}
