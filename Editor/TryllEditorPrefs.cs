// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// User-per-project EditorPrefs prefix. SHA-256 of a separator-normalized
    /// data path — <see cref="string.GetHashCode"/> is not a cross-runtime contract.
    /// Legacy <c>Tryll.{Feature}.{GetHashCode:X8}.</c> keys are copied once by each
    /// feature Load, same pattern as Agent Log's global-key migration.
    /// </summary>
    internal static class TryllEditorPrefs
    {
        public static string ProjectPrefix(string feature) =>
            ProjectPrefix(feature, Application.dataPath ?? "");

        /// <summary>Test seam: same formula as <see cref="ProjectPrefix(string)"/>.</summary>
        public static string ProjectPrefix(string feature, string dataPath) =>
            "Tryll." + feature + "." + StablePathToken(dataPath) + ".";

        public static string LegacyHashPrefix(string feature) =>
            LegacyHashPrefix(feature, Application.dataPath ?? "");

        /// <summary>Pre-stable prefix: <c>string.GetHashCode</c> of the raw data path.</summary>
        public static string LegacyHashPrefix(string feature, string dataPath)
        {
            string project = dataPath ?? "";
            return "Tryll." + feature + "." + project.GetHashCode().ToString("X8") + ".";
        }

        public static string StablePathToken(string dataPath)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(NormalizePath(dataPath));
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(8);
            for (int i = 0; i < 4; i++)
                sb.Append(hash[i].ToString("X2"));
            return sb.ToString();
        }

        public static string NormalizePath(string dataPath)
        {
            if (string.IsNullOrEmpty(dataPath))
                return "";
            return dataPath.Replace('\\', '/').TrimEnd('/');
        }

        /// <summary>
        /// Prefer <paramref name="destPrefix"/> when it already has keys; otherwise
        /// read from <paramref name="legacyPrefix"/> so the caller can Save onto dest.
        /// </summary>
        public static string SourcePrefixIfEmpty(
            string destPrefix, string legacyPrefix, params string[] sentinelKeys)
        {
            if (HasAnyKey(destPrefix, sentinelKeys))
                return destPrefix;
            if (!string.IsNullOrEmpty(legacyPrefix)
                && destPrefix != legacyPrefix
                && HasAnyKey(legacyPrefix, sentinelKeys))
            {
                return legacyPrefix;
            }

            return destPrefix;
        }

        public static bool HasAnyKey(string prefix, params string[] fieldKeys)
        {
            if (string.IsNullOrEmpty(prefix) || fieldKeys == null)
                return false;
            foreach (var key in fieldKeys)
            {
                if (EditorPrefs.HasKey(prefix + key))
                    return true;
            }

            return false;
        }
    }
}
