// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: VERSION  +  tools/codegen/templates/unity_docs_base.cs.j2
// See: docs/dev/codegen.md

namespace Tryll.Client
{
    /// <summary>
    /// Docs site root pinned to the repo VERSION's <c>/vX.Y/</c> folder. The site serves no
    /// unversioned paths, and the in-tree package reports 0.0.0 until a release stamps it,
    /// so the pin is baked in here for <see cref="TryllDocs"/> and the node help links.
    /// </summary>
    internal static class TryllDocsBase
    {
        public const string Url = "https://docs.tryllengine.com/v0.10/";
    }
}
