// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Text;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Extracts and rewrites <c>{{slot.&lt;name&gt;}}</c> / <c>{{user_message}}</c>
    /// references in a Mustache template source. This is one of three
    /// independent implementations that must agree byte-for-byte on the shared test
    /// vectors in <c>tryll/schema/mustache-refs-vectors.json</c> — the other two are the
    /// C++ server (<c>tryll/server/workflow/src/MustacheRefs.cpp</c>) and the Unreal
    /// editor-module port.
    ///
    /// Scope: recognises the tag forms the kainjow Mustache renderer actually accepts:
    /// <list type="bullet">
    ///   <item><c>{{slot.&lt;name&gt;}}</c> — reports <c>&lt;name&gt;</c>.</item>
    ///   <item><c>{{user_message}}</c> — reports the literal "user_message".</item>
    ///   <item>Either form padded with whitespace inside the braces
    ///     (<c>{{ slot.x }}</c>, <c>{{ user_message }}</c>).</item>
    ///   <item>Either form using triple (unescaped) braces
    ///     (<c>{{{slot.x}}}</c>, <c>{{{ user_message }}}</c>).</item>
    /// </list>
    /// Every other tag (<c>{{knowledge_&lt;source&gt;}}</c>,
    /// <c>{{#instructions}}...{{/instructions}}</c>, section/inverted/partial tags) is
    /// left untouched and never reported or rewritten.
    /// </summary>
    internal static class TryllMustacheRefs
    {
        private const string UserMessageBody = "user_message";
        private const string SlotBody = "slot.";

        private readonly struct Ref
        {
            public readonly int Start;
            public readonly int End;
            public readonly string Name;

            public Ref(int start, int end, string name)
            {
                Start = start;
                End = end;
                Name = name;
            }
        }

        private static bool IsNameChar(char c)
            => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';

        private static int SkipSpaces(string s, int pos)
        {
            int n = s.Length;
            while (pos < n && char.IsWhiteSpace(s[pos]))
                pos++;
            return pos;
        }

        /// <summary>
        /// Scan <paramref name="tmpl"/> left to right for <c>{{slot.&lt;name&gt;}}</c> /
        /// <c>{{user_message}}</c> tags, also matching whitespace-padded
        /// (<c>{{ slot.name }}</c>) and triple-brace (<c>{{{slot.name}}}</c>) forms —
        /// including both at once (<c>{{{ slot.name }}}</c>). A candidate match must have
        /// a valid identifier (for <c>slot.</c>) and braces of matching count on both
        /// sides to qualify; anything else (e.g. a lone <c>{{#section}}</c>) is skipped.
        /// </summary>
        private static List<Ref> Scan(string tmpl)
        {
            var refs = new List<Ref>();
            if (string.IsNullOrEmpty(tmpl))
                return refs;

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
                    string name = null;
                    int nameEnd = bodyStart;

                    if (MatchesAt(tmpl, bodyStart, UserMessageBody))
                    {
                        nameEnd = bodyStart + UserMessageBody.Length;
                        name = "user_message";
                    }
                    else if (MatchesAt(tmpl, bodyStart, SlotBody))
                    {
                        int identStart = bodyStart + SlotBody.Length;
                        int identEnd = identStart;
                        while (identEnd < n && IsNameChar(tmpl[identEnd]))
                            identEnd++;
                        if (identEnd > identStart)
                        {
                            nameEnd = identEnd;
                            name = tmpl.Substring(identStart, identEnd - identStart);
                        }
                    }

                    if (name != null)
                    {
                        int closeStart = SkipSpaces(tmpl, nameEnd);
                        string closeBraces = braceCount == 3 ? "}}}" : "}}";
                        if (MatchesAt(tmpl, closeStart, closeBraces))
                        {
                            refs.Add(new Ref(pos, closeStart + closeBraces.Length, name));
                            pos = closeStart + closeBraces.Length;
                            continue;
                        }
                    }
                }

                pos++;
            }
            return refs;
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

        /// <summary>Deduplicated, first-seen-order list of slot names referenced in <paramref name="tmpl"/>.</summary>
        public static List<string> ExtractRefs(string tmpl)
        {
            var outNames = new List<string>();
            foreach (var r in Scan(tmpl))
                if (!outNames.Contains(r.Name))
                    outNames.Add(r.Name);
            return outNames;
        }

        /// <summary>
        /// Tag-aware rename: rewrites every <c>{{slot.&lt;from&gt;}}</c> /
        /// <c>{{user_message}}</c> tag whose reported name equals <paramref name="from"/>
        /// to reference <paramref name="to"/> instead. Prose outside matched tags (and
        /// every other tag form) is copied verbatim. A no-op when <paramref name="from"/>
        /// is never referenced. Rewritten tags are always normalized to the canonical
        /// bare, double-brace form, regardless of the original tag's padding/brace style.
        /// </summary>
        public static string Rename(string tmpl, string from, string to)
        {
            if (string.IsNullOrEmpty(from))
                return tmpl ?? string.Empty;
            return Rename(tmpl, new Dictionary<string, string> { { from, to } });
        }

        /// <summary>
        /// Rewrites every matched tag in one scan. A name produced by an earlier
        /// pair is not matched again, so overlapping slot renames cannot cascade.
        /// </summary>
        public static string Rename(string tmpl, IReadOnlyDictionary<string, string> renames)
        {
            if (string.IsNullOrEmpty(tmpl) || renames == null || renames.Count == 0)
                return tmpl ?? string.Empty;

            var refs = Scan(tmpl);
            if (refs.Count == 0)
                return tmpl;

            var sb = new StringBuilder(tmpl.Length);
            int cursor = 0;
            foreach (var r in refs)
            {
                sb.Append(tmpl, cursor, r.Start - cursor);
                if (renames.TryGetValue(r.Name, out string to))
                {
                    string newTag = (to == "user_message")
                        ? ("{{" + UserMessageBody + "}}")
                        : ("{{" + SlotBody + to + "}}");
                    sb.Append(newTag);
                }
                else
                {
                    sb.Append(tmpl, r.Start, r.End - r.Start);
                }
                cursor = r.End;
            }
            sb.Append(tmpl, cursor, tmpl.Length - cursor);
            return sb.ToString();
        }
    }
}
