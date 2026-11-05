// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Runtime.CompilerServices;

// Test assemblies: shared support plus one per tier under Tests/<Tier>/.
[assembly: InternalsVisibleTo("Tryll.Tests.Support")]
[assembly: InternalsVisibleTo("Tryll.Tests.Support.FixtureServer")]
[assembly: InternalsVisibleTo("Tryll.Tests.Unit")]
[assembly: InternalsVisibleTo("Tryll.Tests.Editor")]
[assembly: InternalsVisibleTo("Tryll.Tests.Fixture")]
[assembly: InternalsVisibleTo("Tryll.Tests.Live")]
[assembly: InternalsVisibleTo("Tryll.Tests.Recovery")]
