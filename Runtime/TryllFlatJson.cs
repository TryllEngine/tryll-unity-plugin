// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tryll.Client
{
    /// <summary>
    /// Parses a flat JSON object of strings, <c>{ "id": "text" }</c>.
    /// Unity's <c>JsonUtility</c> cannot read a dictionary.
    /// </summary>
    internal static class TryllFlatJson
    {
        public static bool TryParseObject(string json, out Dictionary<string, string> map, out string error)
        {
            map = new Dictionary<string, string>();
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "empty JSON";
                return false;
            }

            int i = 0;
            Skip(json, ref i);
            if (!Eat(json, ref i, '{'))
            {
                error = "expected '{'" + At(json, i);
                return false;
            }

            Skip(json, ref i);
            if (Eat(json, ref i, '}'))
                return true;

            while (i < json.Length)
            {
                Skip(json, ref i);
                if (!TryString(json, ref i, out var key))
                {
                    error = "expected a string key" + At(json, i);
                    return false;
                }
                Skip(json, ref i);
                if (!Eat(json, ref i, ':'))
                {
                    error = "expected ':'" + At(json, i);
                    return false;
                }
                Skip(json, ref i);
                if (!TryString(json, ref i, out var value))
                {
                    error = "expected a string value" + At(json, i);
                    return false;
                }
                map[key] = value;
                Skip(json, ref i);
                if (Eat(json, ref i, '}'))
                    return true;
                if (!Eat(json, ref i, ','))
                {
                    error = "expected ',' or '}'" + At(json, i);
                    return false;
                }
            }

            error = "unterminated object";
            return false;
        }

        // " at line L, column C" for an error at index i.
        static string At(string s, int i)
        {
            int line = 1, column = 1;
            for (int k = 0; k < i && k < s.Length; k++)
            {
                if (s[k] == '\n') { line++; column = 1; }
                else column++;
            }
            return $" at line {line}, column {column}";
        }

        static void Skip(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        static bool Eat(string s, ref int i, char c)
        {
            if (i < s.Length && s[i] == c)
            {
                i++;
                return true;
            }
            return false;
        }

        static bool TryString(string s, ref int i, out string value)
        {
            value = null;
            if (i >= s.Length || s[i] != '"') return false;
            i++;
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"')
                {
                    value = sb.ToString();
                    return true;
                }
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                if (i >= s.Length) return false;
                char e = s[i++];
                switch (e)
                {
                    case '"':
                    case '\\':
                    case '/':
                        sb.Append(e);
                        break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) return false;
                        if (!int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int cp))
                            return false;
                        sb.Append((char)cp);
                        i += 4;
                        break;
                    default:
                        return false;
                }
            }
            return false;
        }
    }
}
