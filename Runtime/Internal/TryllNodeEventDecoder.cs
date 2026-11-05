// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Globalization;

namespace Tryll.Client.Internal
{
    internal enum TryllNodeEventKind
    {
        ToolCall,
        IntentClassified,
        IntentLlmClassified,
        Paused,
        Unknown,
    }

    /// <summary>Owning decode of one NodeEvent. Strings outlive the inbound frame.</summary>
    internal sealed class TryllDecodedNodeEvent
    {
        public TryllNodeEventKind Kind = TryllNodeEventKind.Unknown;
        public string NodeName = string.Empty;
        public string EventType = string.Empty;
        public TryllToolCall ToolCall;
        public TryllIntentClassification Intent;
        public TryllIntentLlmClassification IntentLlm;
        public string PendingExit = string.Empty;
        public List<KeyValuePair<string, string>> KvPairs = new List<KeyValuePair<string, string>>();
    }

    /// <summary>
    /// Parse a NodeEvent payload once. <see cref="TryllClient"/> broadcasts its
    /// session events from the result, then <see cref="TryllAgent.HandleNodeEvent"/>
    /// runs the per-agent callbacks.
    /// </summary>
    internal static class TryllNodeEventDecoder
    {
        public static TryllDecodedNodeEvent Decode(
            string nodeName,
            string eventType,
            List<KeyValuePair<string, string>> kvPairs)
        {
            nodeName = nodeName ?? string.Empty;
            eventType = eventType ?? string.Empty;
            kvPairs = kvPairs ?? new List<KeyValuePair<string, string>>();

            var decoded = new TryllDecodedNodeEvent
            {
                NodeName = nodeName,
                EventType = eventType,
                KvPairs = kvPairs,
            };

            if (eventType == "tool_call")
            {
                decoded.Kind = TryllNodeEventKind.ToolCall;
                decoded.ToolCall = new TryllToolCall(
                    Value(kvPairs, "call_id"),
                    nodeName,
                    Value(kvPairs, "tool_name"),
                    Value(kvPairs, "arguments_json"),
                    Value(kvPairs, "disposition"));
            }
            else if (eventType == "intent_classified")
            {
                decoded.Kind = TryllNodeEventKind.IntentClassified;
                ulong recordIndex = 0;
                ulong.TryParse(Value(kvPairs, "record_index"), out recordIndex);
                float distance = 0f;
                ParseFloat(Value(kvPairs, "distance"), out distance);
                decoded.Intent = new TryllIntentClassification
                {
                    NodeName = nodeName,
                    Intent = Value(kvPairs, "intent"),
                    RecordId = Value(kvPairs, "record_id"),
                    RecordIndex = recordIndex,
                    Distance = distance,
                    NotFoundReason = Value(kvPairs, "not_found_reason"),
                };
            }
            else if (eventType == "intent_llm_classified")
            {
                decoded.Kind = TryllNodeEventKind.IntentLlmClassified;
                float topProb = 0f, secondProb = 0f;
                ParseFloat(Value(kvPairs, "top_prob"), out topProb);
                ParseFloat(Value(kvPairs, "second_prob"), out secondProb);
                var labelProbs = new List<TryllIntentLlmLabelProb>();
                for (int i = 0; i < kvPairs.Count; ++i)
                {
                    var key = kvPairs[i].Key;
                    if (key != null && key.EndsWith(".prob") && key != "top_prob" && key != "second_prob")
                    {
                        float prob = 0f;
                        ParseFloat(kvPairs[i].Value, out prob);
                        labelProbs.Add(new TryllIntentLlmLabelProb
                        {
                            Label = key.Substring(0, key.Length - 5),
                            Prob = prob,
                        });
                    }
                }
                decoded.IntentLlm = new TryllIntentLlmClassification
                {
                    NodeName = nodeName,
                    Intent = Value(kvPairs, "intent"),
                    TopProb = topProb,
                    SecondProb = secondProb,
                    NotFoundReason = Value(kvPairs, "not_found_reason"),
                    LabelProbs = labelProbs,
                };
            }
            else if (eventType == "paused")
            {
                decoded.Kind = TryllNodeEventKind.Paused;
                decoded.PendingExit = Value(kvPairs, "exit_route");
            }

            return decoded;
        }

        private static string Value(List<KeyValuePair<string, string>> kvPairs, string key)
        {
            for (int i = 0; i < kvPairs.Count; ++i)
            {
                if (kvPairs[i].Key == key)
                    return kvPairs[i].Value ?? string.Empty;
            }
            return string.Empty;
        }

        private static void ParseFloat(string raw, out float value)
            => float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
