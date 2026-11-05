// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Minimal JSON reader for the Dialog Lab.
    ///
    /// The package ships no general JSON parser: <c>UnityEngine.JsonUtility</c> cannot
    /// represent the arbitrary-key <c>parameters</c> / <c>engine</c> groups that
    /// <c>debug_info</c> (schema_version 2) carries, and adding a package dependency
    /// (Newtonsoft) for one editor-only feature would burden every consumer of the
    /// shipped client package. This reader is Editor-only by design (lives in
    /// <c>Tryll.Client.Editor</c>) and produces a small DOM (<see cref="TryllJsonValue"/>)
    /// good enough to drive the generic inspector blocks.
    ///
    /// Not a general-purpose JSON library: no writer, no streaming, no comments.
    /// <see cref="TryllJson.TryParse"/> never throws to callers — malformed input yields
    /// <c>false</c> plus a reason, because a turn's diagnostics being unparsable must not
    /// crash the inspector. That does not mean illegal JSON is accepted: unescaped control
    /// characters in strings are rejected.
    /// </summary>
    internal enum TryllJsonKind { Null, Bool, Number, String, Array, Object }

    internal sealed class TryllJsonValue
    {
        public TryllJsonKind Kind = TryllJsonKind.Null;

        public bool Bool;
        public double Number;
        public string String;
        public List<TryllJsonValue> Array;
        public Dictionary<string, TryllJsonValue> Object;

        public static readonly TryllJsonValue Null = new TryllJsonValue { Kind = TryllJsonKind.Null };

        public static TryllJsonValue OfBool(bool b) => new TryllJsonValue { Kind = TryllJsonKind.Bool, Bool = b };
        public static TryllJsonValue OfNumber(double n) => new TryllJsonValue { Kind = TryllJsonKind.Number, Number = n };
        public static TryllJsonValue OfString(string s) => new TryllJsonValue { Kind = TryllJsonKind.String, String = s };
        public static TryllJsonValue OfArray(List<TryllJsonValue> items) => new TryllJsonValue { Kind = TryllJsonKind.Array, Array = items };
        public static TryllJsonValue OfObject(Dictionary<string, TryllJsonValue> members) => new TryllJsonValue { Kind = TryllJsonKind.Object, Object = members };

        public bool IsNull => Kind == TryllJsonKind.Null;

        public bool AsBool(bool fallback = false) => Kind == TryllJsonKind.Bool ? Bool : fallback;

        public double AsDouble(double fallback = 0.0)
        {
            if (Kind == TryllJsonKind.Number) return Number;
            if (Kind == TryllJsonKind.String && double.TryParse(
                    String, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;
            return fallback;
        }

        public int AsInt(int fallback = 0) => Kind == TryllJsonKind.Number ? (int)Number : fallback;

        public string AsString(string fallback = null) => Kind == TryllJsonKind.String ? String : fallback;

        private static readonly List<TryllJsonValue> EmptyArray = new List<TryllJsonValue>();
        private static readonly Dictionary<string, TryllJsonValue> EmptyObject = new Dictionary<string, TryllJsonValue>();

        public IReadOnlyList<TryllJsonValue> AsArray() => Kind == TryllJsonKind.Array ? Array : EmptyArray;
        public IReadOnlyDictionary<string, TryllJsonValue> AsObject() => Kind == TryllJsonKind.Object ? Object : EmptyObject;

        public bool HasKey(string key) => Kind == TryllJsonKind.Object && Object.ContainsKey(key);

        /// <summary>Direct child lookup. Returns <see cref="Null"/> (never C# null) on a miss,
        /// so callers can chain <c>Get("a").Get("b").AsString()</c> without null checks.</summary>
        public TryllJsonValue Get(string key)
        {
            if (Kind == TryllJsonKind.Object && Object.TryGetValue(key, out var v)) return v;
            return Null;
        }

        /// <summary>Dotted/indexed path lookup, e.g. <c>Path("output.classification.topk[0].intent")</c>.
        /// Returns <see cref="Null"/> at the first missing segment.</summary>
        public TryllJsonValue Path(string path)
        {
            var cur = this;
            int i = 0;
            while (i < path.Length && !cur.IsNull)
            {
                int dot = path.IndexOf('.', i);
                int br = path.IndexOf('[', i);
                int segEnd = dot < 0 ? path.Length : dot;
                if (br >= 0 && br < segEnd) segEnd = br;

                if (segEnd > i) cur = cur.Get(path.Substring(i, segEnd - i));
                i = segEnd;

                while (i < path.Length && path[i] == '[')
                {
                    int close = path.IndexOf(']', i);
                    if (close < 0) return Null;
                    if (!int.TryParse(path.Substring(i + 1, close - i - 1), out int idx)) return Null;
                    var arr = cur.AsArray();
                    cur = idx >= 0 && idx < arr.Count ? arr[idx] : Null;
                    i = close + 1;
                }
                if (i < path.Length && path[i] == '.') i++;
            }
            return cur;
        }
    }

    internal static class TryllJson
    {
        /// <summary>Parse a JSON document. Never throws — returns <c>false</c> plus a
        /// human-readable <paramref name="error"/> on malformed input.</summary>
        public static bool TryParse(string json, out TryllJsonValue value, out string error)
        {
            value = TryllJsonValue.Null;
            error = null;
            if (string.IsNullOrEmpty(json)) { error = "empty input"; return false; }

            try
            {
                var reader = new Reader(json);
                reader.SkipWhitespace();
                value = reader.ParseValue();
                reader.SkipWhitespace();
                if (!reader.AtEnd)
                {
                    error = $"trailing content at offset {reader.Pos}";
                    value = TryllJsonValue.Null;
                    return false;
                }
                return true;
            }
            catch (JsonParseException ex)
            {
                error = ex.Message;
                value = TryllJsonValue.Null;
                return false;
            }
            catch (Exception ex)
            {
                // Belt-and-braces: a malformed payload must never crash the inspector,
                // even via an exception type this reader did not anticipate.
                error = $"unexpected parse failure: {ex.Message}";
                value = TryllJsonValue.Null;
                return false;
            }
        }

        private sealed class JsonParseException : Exception
        {
            public JsonParseException(string message) : base(message) { }
        }

        private sealed class Reader
        {
            private readonly string _s;
            private int _i;

            public Reader(string s) { _s = s; _i = 0; }
            public int Pos => _i;
            public bool AtEnd => _i >= _s.Length;

            public void SkipWhitespace()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
            }

            private char Cur => _i < _s.Length ? _s[_i] : '\0';

            private void Expect(char c)
            {
                if (Cur != c) throw new JsonParseException($"expected '{c}' at offset {_i}, got '{Cur}'");
                _i++;
            }

            public TryllJsonValue ParseValue()
            {
                SkipWhitespace();
                switch (Cur)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return TryllJsonValue.OfString(ParseString());
                    case 't': ParseLiteral("true");  return TryllJsonValue.OfBool(true);
                    case 'f': ParseLiteral("false"); return TryllJsonValue.OfBool(false);
                    case 'n': ParseLiteral("null");  return TryllJsonValue.Null;
                    default:  return ParseNumber();
                }
            }

            private void ParseLiteral(string lit)
            {
                if (_i + lit.Length > _s.Length || string.CompareOrdinal(_s, _i, lit, 0, lit.Length) != 0)
                    throw new JsonParseException($"expected literal '{lit}' at offset {_i}");
                _i += lit.Length;
            }

            private TryllJsonValue ParseObject()
            {
                Expect('{');
                var members = new Dictionary<string, TryllJsonValue>();
                SkipWhitespace();
                if (Cur == '}') { _i++; return TryllJsonValue.OfObject(members); }

                while (true)
                {
                    SkipWhitespace();
                    if (Cur != '"') throw new JsonParseException($"expected object key at offset {_i}");
                    string key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    var val = ParseValue();
                    members[key] = val;   // last-write-wins on a duplicate key; tolerant by design
                    SkipWhitespace();
                    if (Cur == ',') { _i++; continue; }
                    if (Cur == '}') { _i++; break; }
                    throw new JsonParseException($"expected ',' or '}}' at offset {_i}");
                }
                return TryllJsonValue.OfObject(members);
            }

            private TryllJsonValue ParseArray()
            {
                Expect('[');
                var items = new List<TryllJsonValue>();
                SkipWhitespace();
                if (Cur == ']') { _i++; return TryllJsonValue.OfArray(items); }

                while (true)
                {
                    items.Add(ParseValue());
                    SkipWhitespace();
                    if (Cur == ',') { _i++; continue; }
                    if (Cur == ']') { _i++; break; }
                    throw new JsonParseException($"expected ',' or ']' at offset {_i}");
                }
                return TryllJsonValue.OfArray(items);
            }

            private string ParseString()
            {
                Expect('"');
                var sb = new StringBuilder();
                while (true)
                {
                    if (_i >= _s.Length) throw new JsonParseException("unterminated string");
                    char c = _s[_i++];
                    if (c == '"') break;
                    if (c != '\\')
                    {
                        // RFC 8259: unescaped U+0000–U+001F inside strings is illegal.
                        // TryParse still never throws to callers — this becomes a parse error.
                        if (c < 0x20)
                            throw new JsonParseException($"unescaped control character U+{(int)c:X4} at offset {_i - 1}");
                        sb.Append(c);
                        continue;
                    }

                    if (_i >= _s.Length) throw new JsonParseException("unterminated escape");
                    char esc = _s[_i++];
                    switch (esc)
                    {
                        case '"':  sb.Append('"');  break;
                        case '\\': sb.Append('\\'); break;
                        case '/':  sb.Append('/');  break;
                        case 'b':  sb.Append('\b'); break;
                        case 'f':  sb.Append('\f'); break;
                        case 'n':  sb.Append('\n'); break;
                        case 'r':  sb.Append('\r'); break;
                        case 't':  sb.Append('\t'); break;
                        case 'u':
                            if (_i + 4 > _s.Length) throw new JsonParseException("truncated \\u escape");
                            string hex = _s.Substring(_i, 4);
                            if (!ushort.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code))
                                throw new JsonParseException($"invalid \\u escape '{hex}' at offset {_i}");
                            sb.Append((char)code);
                            _i += 4;
                            break;
                        default:
                            throw new JsonParseException($"invalid escape '\\{esc}' at offset {_i - 1}");
                    }
                }
                return sb.ToString();
            }

            private TryllJsonValue ParseNumber()
            {
                int start = _i;
                if (Cur == '-') _i++;
                while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
                if (Cur == '.') { _i++; while (_i < _s.Length && char.IsDigit(_s[_i])) _i++; }
                if (Cur == 'e' || Cur == 'E')
                {
                    _i++;
                    if (Cur == '+' || Cur == '-') _i++;
                    while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
                }
                if (_i == start) throw new JsonParseException($"expected value at offset {_i}, got '{Cur}'");

                string slice = _s.Substring(start, _i - start);
                if (!double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    throw new JsonParseException($"invalid number '{slice}' at offset {start}");
                return TryllJsonValue.OfNumber(d);
            }
        }
    }
}
