// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client
{
    /// <summary>
    /// Version of the Tryll Client package, readable at runtime.
    ///
    /// <para>
    /// The literal below is a placeholder. The release pipeline overwrites it with the
    /// repo-root <c>VERSION</c> value when staging the package
    /// (<c>ci/scripts/release.py</c>, <c>_stamp_cs_version_const</c>) — the same source
    /// that feeds <c>package.json</c>'s <c>version</c> and the server's <c>kVersion</c>.
    /// An in-tree development build therefore reports <c>0.0.0</c>, and only a shipped
    /// package reports a real version.
    /// </para>
    ///
    /// <para>
    /// This constant exists because <c>package.json</c> is not present in a built
    /// player: <c>UnityEditor.PackageManager</c> is editor-only, so a compiled-in
    /// constant is the only way a runtime build can report which package it came from.
    /// </para>
    /// </summary>
    public static class TryllVersion
    {
        /// <summary>Package version, e.g. <c>"0.5.0"</c>. Stamped at release time.</summary>
        public const string Version = "0.10.0";
    }
}
