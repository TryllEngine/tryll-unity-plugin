// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Extracts <c>{{var.&lt;name&gt;}}</c> references (per-agent Variables) from a
    /// Mustache template source. Independent from <see cref="TryllMustacheRefs"/> —
    /// that scanner is contract-locked byte-for-byte with the C++ server and the
    /// Unreal editor port for <c>slot.</c>/<c>user_message</c> extraction
    /// (renaming propagation); this one is Unity-only tooling for the newer
    /// per-agent Variables feature and must NOT be merged into it.
    ///
    /// Scope mirrors the server's <c>ScanVarRefs</c>
    /// (<c>tryll/server/workflow/src/TemplateValidation.cpp</c>) — the same tag
    /// shapes <c>MustacheRefs::Scan</c> recognises for <c>slot.</c>, so anything
    /// kainjow would actually resolve at render time is caught here too:
    /// <list type="bullet">
    ///   <item><c>{{var.&lt;name&gt;}}</c> — reports <c>&lt;name&gt;</c>.</item>
    ///   <item>Padded with whitespace inside the braces (<c>{{ var.x }}</c>).</item>
    ///   <item>Triple (unescaped) braces (<c>{{{var.x}}}</c>), including combined
    ///     with padding (<c>{{{ var.x }}}</c>).</item>
    /// </list>
    /// Every other tag is left unreported.
    /// </summary>
    internal static class TryllVarRefs
    {
        private const string VarBody = "var.";

        private static bool IsNameChar(char c)
            => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';

        private static int SkipSpaces(string s, int pos)
        {
            int n = s.Length;
            while (pos < n && char.IsWhiteSpace(s[pos]))
                pos++;
            return pos;
        }

        private static bool MatchesAt(string s, int pos, string literal)
        {
            if (pos + literal.Length > s.Length)
                return false;
            for (int i = 0; i < literal.Length; i++)
                if (s[pos + i] != literal[i])
                    return false;
            return true;
        }

        /// <summary>Deduplicated, first-seen-order list of variable names referenced in <paramref name="tmpl"/>.</summary>
        public static List<string> ExtractRefs(string tmpl)
        {
            var outNames = new List<string>();
            if (string.IsNullOrEmpty(tmpl))
                return outNames;

            int pos = 0;
            int n = tmpl.Length;
            while (pos < n)
            {
                int braceCount = 0;
                if (MatchesAt(tmpl, pos, "{{{"))
                    braceCount = 3;
                else if (MatchesAt(tmpl, pos, "{{"))
                    braceCount = 2;

                if (braceCount > 0)
                {
                    int bodyStart = SkipSpaces(tmpl, pos + braceCount);
                    if (MatchesAt(tmpl, bodyStart, VarBody))
                    {
                        int identStart = bodyStart + VarBody.Length;
                        int identEnd = identStart;
                        while (identEnd < n && IsNameChar(tmpl[identEnd]))
                            identEnd++;

                        if (identEnd > identStart)
                        {
                            int closeStart = SkipSpaces(tmpl, identEnd);
                            string closeBraces = braceCount == 3 ? "}}}" : "}}";
                            if (MatchesAt(tmpl, closeStart, closeBraces))
                            {
                                string name = tmpl.Substring(identStart, identEnd - identStart);
                                if (!outNames.Contains(name))
                                    outNames.Add(name);
                                pos = closeStart + closeBraces.Length;
                                continue;
                            }
                        }
                    }
                }

                pos++;
            }
            return outNames;
        }
    }
}
