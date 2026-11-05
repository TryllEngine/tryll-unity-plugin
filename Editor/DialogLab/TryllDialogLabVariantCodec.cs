// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Tryll.Client.Internal;
using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Round-trips a variant through the one-line JSON form EditorPrefs stores.
    /// Keys match Unreal <c>FTryllDialogLabVariantCodec</c>; <c>agent</c> is a
    /// Unity <see cref="GlobalObjectId"/> string.
    /// </summary>
    internal static class TryllDialogLabVariantCodec
    {
        public static string Encode(TryllDialogLabVariant variant)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            AppendString(sb, "label", variant.Label ?? "");
            sb.Append(',');
            AppendNumber(sb, "r", variant.Swatch.r);
            sb.Append(',');
            AppendNumber(sb, "g", variant.Swatch.g);
            sb.Append(',');
            AppendNumber(sb, "b", variant.Swatch.b);
            sb.Append(',');
            AppendString(sb, "model", variant.ModelOverride ?? "");
            sb.Append(',');
            AppendString(sb, "agent", AgentId(variant));
            sb.Append(',');
            sb.Append("\"use_live_tools\":").Append(variant.UseLiveTools ? "true" : "false");
            sb.Append(",\"overrides\":[");
            if (variant.Overrides != null)
            {
                for (int i = 0; i < variant.Overrides.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    var o = variant.Overrides[i];
                    sb.Append('{');
                    AppendString(sb, "node", o.NodeName ?? "");
                    sb.Append(',');
                    AppendString(sb, "field", o.FieldPath ?? "");
                    sb.Append(',');
                    AppendString(sb, "value", o.Value ?? "");
                    sb.Append('}');
                }
            }
            sb.Append("]}");
            return sb.ToString();
        }

        public static bool Decode(string line, TryllDialogLabVariant dest)
        {
            if (dest == null || string.IsNullOrEmpty(line))
                return false;
            if (!TryllJson.TryParse(line, out var root, out _) || root.Kind != TryllJsonKind.Object)
                return false;

            dest.Label = root.Get("label").AsString("");
            dest.ModelOverride = root.Get("model").AsString("");
            dest.UseLiveTools = root.Get("use_live_tools").AsBool(false);
            dest.Swatch = new Color(
                (float)root.Get("r").AsDouble(),
                (float)root.Get("g").AsDouble(),
                (float)root.Get("b").AsDouble());
            dest.AgentOverrideId = root.Get("agent").AsString("");
            dest.AgentOverride = ResolveAgent(dest.AgentOverrideId);
            dest.Overrides = new List<TryllParamOverride>();
            foreach (var entry in root.Get("overrides").AsArray())
            {
                if (entry.Kind != TryllJsonKind.Object)
                    continue;
                dest.Overrides.Add(new TryllParamOverride
                {
                    NodeName = entry.Get("node").AsString(""),
                    FieldPath = entry.Get("field").AsString(""),
                    Value = entry.Get("value").AsString(""),
                });
            }
            return true;
        }

        static string AgentId(TryllDialogLabVariant variant)
        {
            if (variant?.AgentOverride != null)
                return GlobalObjectId.GetGlobalObjectIdSlow(variant.AgentOverride).ToString();
            return variant?.AgentOverrideId ?? "";
        }

        static TryllAgentComponent ResolveAgent(string idString)
        {
            if (string.IsNullOrEmpty(idString))
                return null;
            if (!GlobalObjectId.TryParse(idString, out var id))
                return null;
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as TryllAgentComponent;
        }

        static void AppendString(StringBuilder sb, string key, string value)
        {
            sb.Append('"').Append(key).Append("\":\"").Append(Escape(value)).Append('"');
        }

        static void AppendNumber(StringBuilder sb, string key, float value)
        {
            sb.Append('"').Append(key).Append("\":")
                .Append(value.ToString("G9", CultureInfo.InvariantCulture));
        }

        static string Escape(string s)
        {
            var quoted = TryllJsonWriter.Quote(s ?? "");
            return quoted.Substring(1, quoted.Length - 2);
        }
    }
}
