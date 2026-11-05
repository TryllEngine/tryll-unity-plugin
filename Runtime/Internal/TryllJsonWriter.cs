// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tryll.Client.Internal
{
    /// <summary>One stripped field. <see cref="Path"/> is dotted from the message body.</summary>
    internal readonly struct TryllOmittedField
    {
        public readonly string Path;
        public readonly long Count;
        public readonly long Bytes;

        public TryllOmittedField(string path, long count, long bytes)
        {
            Path = path ?? "";
            Count = count;
            Bytes = bytes;
        }
    }

    /// <summary>
    /// JSON writer shared by the generated Agent Log payload visitor and Dialog Lab.
    /// Strings are UTF-8 with U+FFFD for bytes the caller already decoded. NaN and
    /// infinities are JSON strings, because they are not numbers.
    /// </summary>
    internal sealed class TryllJsonWriter
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly bool _indent;
        private readonly List<Frame> _frames = new List<Frame>();

        private struct Frame
        {
            public bool Array;
            public int Count;
        }

        public TryllJsonWriter(bool indent = false)
        {
            _indent = indent;
        }

        public void BeginObject() => Begin('{');
        public void BeginArray() => Begin('[');

        public void EndObject() => End('}');
        public void EndArray() => End(']');

        public void Key(string name)
        {
            Separator();
            WriteString(name ?? "");
            _sb.Append(':');
            if (_indent) _sb.Append(' ');
        }

        public void Null()
        {
            ValueLead();
            _sb.Append("null");
        }

        public void Bool(bool value)
        {
            ValueLead();
            _sb.Append(value ? "true" : "false");
        }

        public void Int64(long value) => RawNumber(value.ToString(CultureInfo.InvariantCulture));
        public void UInt64(ulong value) => RawNumber(value.ToString(CultureInfo.InvariantCulture));

        public void Float(float value)
        {
            if (float.IsNaN(value)) { String("NaN"); return; }
            if (float.IsPositiveInfinity(value)) { String("Infinity"); return; }
            if (float.IsNegativeInfinity(value)) { String("-Infinity"); return; }
            if (value == 0f && BitConverter.ToInt32(BitConverter.GetBytes(value), 0) < 0) { RawNumber("-0.0"); return; }
            RawNumber(value.ToString("R", CultureInfo.InvariantCulture));
        }

        public void Double(double value)
        {
            if (double.IsNaN(value)) { String("NaN"); return; }
            if (double.IsPositiveInfinity(value)) { String("Infinity"); return; }
            if (double.IsNegativeInfinity(value)) { String("-Infinity"); return; }
            if (value == 0d && BitConverter.DoubleToInt64Bits(value) < 0) { RawNumber("-0.0"); return; }
            RawNumber(value.ToString("R", CultureInfo.InvariantCulture));
        }

        public void String(string value)
        {
            ValueLead();
            WriteString(value ?? "");
        }

        /// <summary>Splice one already-validated JSON value.</summary>
        public void Raw(string json)
        {
            ValueLead();
            _sb.Append(json);
        }

        public static bool TryParseJson(string text, out string reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(text))
            {
                reason = "empty";
                return false;
            }
            var i = 0;
            try
            {
                SkipWs(text, ref i);
                ParseValue(text, ref i);
                SkipWs(text, ref i);
                if (i != text.Length)
                {
                    reason = "trailing data";
                    return false;
                }
                return true;
            }
            catch (FormatException ex)
            {
                reason = ex.Message;
                return false;
            }
        }

        public static string Quote(string value)
        {
            var w = new TryllJsonWriter();
            w.String(value);
            return w.ToString();
        }

        public override string ToString() => _sb.ToString();

        private void Begin(char open)
        {
            Lead(value: true);
            _sb.Append(open);
            _frames.Add(new Frame { Array = open == '[', Count = 0 });
        }

        private void End(char close)
        {
            var empty = _frames.Count == 0 || _frames[_frames.Count - 1].Count == 0;
            if (_frames.Count > 0) _frames.RemoveAt(_frames.Count - 1);
            if (_indent && !empty)
            {
                _sb.Append('\n');
                _sb.Append(' ', Depth() * 2);
            }
            _sb.Append(close);
        }

        private void Separator() => Lead(value: false);

        private void ValueLead() => Lead(value: true);

        private void Lead(bool value)
        {
            if (_frames.Count == 0) return;
            var index = _frames.Count - 1;
            var frame = _frames[index];
            // Object values are separated by their key. Array values separate themselves.
            if (value && !frame.Array) return;
            if (frame.Count++ > 0)
            {
                _sb.Append(',');
                if (_indent)
                {
                    _sb.Append('\n');
                    _sb.Append(' ', Depth() * 2);
                }
            }
            else if (_indent)
            {
                _sb.Append('\n');
                _sb.Append(' ', Depth() * 2);
            }
            _frames[index] = frame;
        }

        private int Depth() => _frames.Count;

        private void RawNumber(string text)
        {
            ValueLead();
            _sb.Append(text);
        }

        private void WriteString(string value)
        {
            _sb.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '"': _sb.Append("\\\""); break;
                    case '\\': _sb.Append("\\\\"); break;
                    case '\n': _sb.Append("\\n"); break;
                    case '\r': _sb.Append("\\r"); break;
                    case '\t': _sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            _sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            _sb.Append(c);
                        break;
                }
            }
            _sb.Append('"');
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\n' || s[i] == '\r' || s[i] == '\t')) i++;
        }

        private static void ParseValue(string s, ref int i)
        {
            if (i >= s.Length) throw new FormatException("truncated");
            var c = s[i];
            if (c == '"') { ParseString(s, ref i); return; }
            if (c == '{') { ParseObject(s, ref i); return; }
            if (c == '[') { ParseArray(s, ref i); return; }
            if (c == 't' || c == 'f' || c == 'n') { ParseLiteral(s, ref i); return; }
            if (c == '-' || (c >= '0' && c <= '9')) { ParseNumber(s, ref i); return; }
            throw new FormatException("invalid value");
        }

        private static void ParseObject(string s, ref int i)
        {
            i++;
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return; }
            while (true)
            {
                if (i >= s.Length || s[i] != '"') throw new FormatException("object key");
                ParseString(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException("object colon");
                i++;
                SkipWs(s, ref i);
                ParseValue(s, ref i);
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; SkipWs(s, ref i); continue; }
                if (i < s.Length && s[i] == '}') { i++; return; }
                throw new FormatException("object end");
            }
        }

        private static void ParseArray(string s, ref int i)
        {
            i++;
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return; }
            while (true)
            {
                ParseValue(s, ref i);
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; SkipWs(s, ref i); continue; }
                if (i < s.Length && s[i] == ']') { i++; return; }
                throw new FormatException("array end");
            }
        }

        private static void ParseString(string s, ref int i)
        {
            i++;
            while (i < s.Length)
            {
                var c = s[i++];
                if (c == '"') return;
                if (c == '\\')
                {
                    if (i >= s.Length) throw new FormatException("bad escape");
                    var e = s[i++];
                    if (e == 'u')
                    {
                        if (i + 4 > s.Length) throw new FormatException("bad escape");
                        i += 4;
                    }
                    else if (e != '"' && e != '\\' && e != '/' && e != 'b' && e != 'f' && e != 'n' && e != 'r' && e != 't')
                        throw new FormatException("bad escape");
                }
                else if (c < 0x20)
                    throw new FormatException("control character");
            }
            throw new FormatException("unterminated string");
        }

        private static void ParseLiteral(string s, ref int i)
        {
            if (s.IndexOf("true", i, StringComparison.Ordinal) == i) { i += 4; return; }
            if (s.IndexOf("false", i, StringComparison.Ordinal) == i) { i += 5; return; }
            if (s.IndexOf("null", i, StringComparison.Ordinal) == i) { i += 4; return; }
            throw new FormatException("invalid literal");
        }

        private static void ParseNumber(string s, ref int i)
        {
            var start = i;
            if (s[i] == '-') i++;
            if (i >= s.Length || s[i] < '0' || s[i] > '9') throw new FormatException("invalid number");
            if (s[i] == '0') i++;
            else while (i < s.Length && s[i] >= '0' && s[i] <= '9') i++;
            if (i < s.Length && s[i] == '.')
            {
                i++;
                if (i >= s.Length || s[i] < '0' || s[i] > '9') throw new FormatException("invalid number");
                while (i < s.Length && s[i] >= '0' && s[i] <= '9') i++;
            }
            if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
            {
                i++;
                if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
                if (i >= s.Length || s[i] < '0' || s[i] > '9') throw new FormatException("invalid number");
                while (i < s.Length && s[i] >= '0' && s[i] <= '9') i++;
            }
            if (i == start) throw new FormatException("invalid number");
        }
    }

    internal static class TryllLogLabel
    {
        public static string Format(string template, params (string key, string value)[] refs)
        {
            var values = new Dictionary<string, string>(refs.Length);
            foreach (var (key, value) in refs)
                values[key] = value ?? "";
            return Format(template, values);
        }

        public static string Format(string template, IReadOnlyDictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(template)) return "";
            var parts = new List<(bool val, string text)>();
            var cursor = 0;
            while (cursor < template.Length)
            {
                var open = template.IndexOf('{', cursor);
                if (open < 0)
                {
                    parts.Add((false, template.Substring(cursor)));
                    break;
                }
                if (open > cursor) parts.Add((false, template.Substring(cursor, open - cursor)));
                var close = template.IndexOf('}', open + 1);
                if (close < 0)
                {
                    parts.Add((false, template.Substring(open)));
                    break;
                }
                var key = template.Substring(open + 1, close - open - 1);
                values.TryGetValue(key, out var found);
                parts.Add((true, found ?? ""));
                cursor = close + 1;
            }
            var sb = new StringBuilder();
            for (var i = 0; i < parts.Count; i++)
            {
                var (isVal, text) = parts[i];
                if (isVal)
                {
                    if (!string.IsNullOrEmpty(text)) sb.Append(text);
                    continue;
                }
                if (IsSeparator(text))
                {
                    if (HasContent(parts, i, -1) && HasContent(parts, i, 1)) sb.Append(text);
                    continue;
                }
                sb.Append(text);
            }
            return sb.ToString();
        }

        private static bool IsSeparator(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var ch in text)
            {
                if (ch != ' ' && ch != '\t' && ch != '·' && ch != '|' && ch != '-' && ch != ':' && ch != '/')
                    return false;
            }
            return true;
        }

        private static bool HasContent(List<(bool val, string text)> parts, int index, int step)
        {
            for (var i = index + step; i >= 0 && i < parts.Count; i += step)
            {
                var (isVal, text) = parts[i];
                if (isVal) return !string.IsNullOrEmpty(text);
                if (!IsSeparator(text)) return !string.IsNullOrEmpty(text);
            }
            return false;
        }
    }

    internal static class TryllFbNav
    {
        public static int Root(Google.FlatBuffers.ByteBuffer bb)
            => bb.GetInt(bb.Position) + bb.Position;

        public static int Field(Google.FlatBuffers.ByteBuffer bb, int tablePos, int vt)
        {
            var vtable = tablePos - bb.GetInt(tablePos);
            if (vt >= bb.GetShort(vtable)) return 0;
            return bb.GetShort(vtable + vt);
        }

        public static bool Present(Google.FlatBuffers.ByteBuffer bb, int tablePos, int vt)
            => Field(bb, tablePos, vt) != 0;

        public static int Indirect(Google.FlatBuffers.ByteBuffer bb, int pos)
            => pos + bb.GetInt(pos);

        public static int Child(Google.FlatBuffers.ByteBuffer bb, int tablePos, int vt)
        {
            var rel = Field(bb, tablePos, vt);
            return rel == 0 ? 0 : Indirect(bb, tablePos + rel);
        }
    }
}
