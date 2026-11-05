// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Testable reader for <c>data/schemas.json</c>. Production callers resolve the
    /// selected bundled server variant; tests pass JSON or a directory.
    /// </summary>
    public sealed class TryllSchemaCatalog
    {
        [Serializable] private sealed class CatalogFile { public int version; public CatalogSchema[] schemas; }
        [Serializable] public sealed class CatalogSchema { public string name; public string path; }

        public IReadOnlyList<string> Names { get; }
        public bool IsAvailable { get; }
        public string SourcePath { get; }

        public static readonly TryllSchemaCatalog Empty = new TryllSchemaCatalog(Array.Empty<string>(), false, null);

        TryllSchemaCatalog(IReadOnlyList<string> names, bool available, string sourcePath)
        {
            Names = names ?? Array.Empty<string>();
            IsAvailable = available;
            SourcePath = sourcePath;
        }

        public bool Contains(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            foreach (var n in Names)
            {
                if (string.Equals(n, name, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public static TryllSchemaCatalog Parse(string json, string sourcePath = null)
        {
            if (string.IsNullOrEmpty(json))
                return new TryllSchemaCatalog(Array.Empty<string>(), false, sourcePath);
            try
            {
                var parsed = JsonUtility.FromJson<CatalogFile>(json);
                if (parsed?.schemas == null)
                    return new TryllSchemaCatalog(Array.Empty<string>(), false, sourcePath);
                var names = new List<string>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var schema in parsed.schemas)
                {
                    if (schema == null || string.IsNullOrEmpty(schema.name)) continue;
                    if (seen.Add(schema.name))
                        names.Add(schema.name);
                }
                names.Sort(StringComparer.Ordinal);
                return new TryllSchemaCatalog(names, true, sourcePath);
            }
            catch (Exception)
            {
                return new TryllSchemaCatalog(Array.Empty<string>(), false, sourcePath);
            }
        }

        public static TryllSchemaCatalog LoadFromDirectory(string serverDir)
        {
            if (string.IsNullOrEmpty(serverDir))
                return Empty;
            string path = Path.Combine(serverDir, "data", "schemas.json");
            if (!File.Exists(path))
                return new TryllSchemaCatalog(Array.Empty<string>(), false, path);
            return Parse(File.ReadAllText(path), path);
        }

        public static TryllSchemaCatalog LoadBundled(TryllRuntimeSettings settings = null)
        {
            try
            {
                string serverDir = TryllServerProcess.ResolveServerDir(settings ?? TryllRuntimeSettings.Load());
                return LoadFromDirectory(serverDir);
            }
            catch
            {
                return Empty;
            }
        }
    }
}
