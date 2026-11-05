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
    /// Window &gt; Tryll &gt; Open Logs Folder: opens the folder where the editor's server
    /// writes its logs and crash dumps, <c>%LOCALAPPDATA%\Tryll\projects\&lt;key&gt;\logs</c>.
    ///
    /// The key is recomputed here rather than asked of the server, because the
    /// server is usually gone when someone needs its crash dump. It must match
    /// <c>tryll/server/runtime/src/ProjectKey.cpp</c>; both are tested against
    /// <c>tryll/schema/project-key-vectors.json</c>. When the folder does not exist
    /// (no server run yet, or a path the server canonicalised differently, such as a
    /// symlink), the parent <c>projects</c> folder opens instead.
    /// </summary>
    internal static class TryllLogsFolder
    {
        internal const string MenuPath = "Window/Tryll/Open Logs Folder";

        const int SlugMaxChars = 48;
        const int HashChars = 16;

        [MenuItem(MenuPath, priority = 2)]
        public static void Open()
        {
            string logs = LogsDirectory();
            if (Directory.Exists(logs))
            {
                EditorUtility.OpenWithDefaultApp(logs);
                return;
            }
            string projects = ProjectsDirectory();
            Directory.CreateDirectory(projects);
            EditorUtility.OpenWithDefaultApp(projects);
            TryllLog.Info(TryllEditorUiContract.HelpLogsNotCreated.Replace("{folder}", projects));
        }

        /// <summary>The Client Dev logs folder of this Unity project.</summary>
        internal static string LogsDirectory()
        {
            var settings = TryllRuntimeSettings.Load();
            string name = settings != null
                ? settings.ResolveProjectName()
                : (UnityEngine.Application.productName ?? string.Empty);
            string dir = CanonicalProjectDir(TryllRuntimeSettings.ProjectRootDirectory());
            return Path.Combine(ProjectsDirectory(), ProjectKey(name, dir), "logs");
        }

        internal static string ProjectsDirectory() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tryll", "projects");

        /// <summary>
        /// Absolute, '/' separators, no trailing separator, ASCII case folded: what the
        /// server's <c>CanonicalProjectDir</c> produces for an ordinary Windows path.
        /// </summary>
        internal static string CanonicalProjectDir(string dir)
        {
            string text = Path.GetFullPath(dir).Replace('\\', '/');
            while (text.Length > 3 && text.EndsWith("/", StringComparison.Ordinal))
                text = text.Substring(0, text.Length - 1);
            return AsciiLower(text);
        }

        /// <summary><c>&lt;slug&gt;-&lt;first 16 hex of SHA-256(dir)&gt;</c>; see ProjectKey.cpp.</summary>
        internal static string ProjectKey(string projectName, string canonicalDir)
        {
            var slug = new StringBuilder();
            bool pendingSeparator = false;
            foreach (char c in projectName ?? string.Empty)
            {
                if (IsSlugChar(c))
                {
                    bool needSeparator = pendingSeparator && slug.Length > 0;
                    if (slug.Length + (needSeparator ? 1 : 0) + 1 > SlugMaxChars)
                        break;
                    if (needSeparator)
                        slug.Append('-');
                    pendingSeparator = false;
                    slug.Append(c >= 'A' && c <= 'Z' ? (char)(c + 32) : c);
                }
                else
                {
                    pendingSeparator = true;
                }
            }
            string text = slug.ToString().TrimEnd('-', '.', '_');
            if (text.Length == 0)
                text = "project";

            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonicalDir ?? string.Empty));
            var hex = new StringBuilder(text.Length + 1 + HashChars).Append(text).Append('-');
            for (int i = 0; i < HashChars / 2; i++)
                hex.Append(hash[i].ToString("x2"));
            return hex.ToString();
        }

        static bool IsSlugChar(char c) =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
            || c == '.' || c == '_' || c == '-';

        static string AsciiLower(string s)
        {
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (chars[i] >= 'A' && chars[i] <= 'Z')
                    chars[i] = (char)(chars[i] + 32);
            return new string(chars);
        }
    }
}
