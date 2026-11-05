// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Linq;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// A headless Dialog Lab run's config file (<c>-tryllLabConfig</c>), format 1. A public
    /// surface: CI jobs script against it, so keys are never renamed, unknown keys are an
    /// error rather than ignored, and a breaking change bumps <see cref="Format"/>. Unreal's
    /// FTryllDialogLabBatchConfig reads the same file with the same messages.
    /// </summary>
    internal sealed class TryllDialogLabBatchConfig
    {
        public const int Format = 1;

        public string Workflow = "";
        /// <summary>Display name for the agent and the run's only variant; empty = the workflow asset's name.</summary>
        public string AgentName = "";
        /// <summary>Raw JSON values by variable name, in file order; typed against the workflow later.</summary>
        public List<KeyValuePair<string, TryllJsonValue>> Variables = new List<KeyValuePair<string, TryllJsonValue>>();
        public bool MaintainDialogueHistory = true;
        public string Model = "";

        /// <summary>A Dialog Script or Dialog Suite asset.</summary>
        public string Dialogues = "";
        public int Seeds;
        public int BaseSeed = 1;
        public string OutputDir = "Build/DialogLab";
        public double TurnTimeoutSeconds = 120;
        /// <summary>A TryllRuntimeSettings asset; empty = the project's only one.</summary>
        public string Settings = "";

        public double? TurnsPassed;
        public double? ChecksPassed;
        public double? CleanDialogues;
        public bool UseLowerBound;

        static readonly string[] TopKeys =
            { "format", "agent", "dialogues", "seeds", "base_seed", "output_dir", "turn_timeout_s", "settings", "thresholds" };
        static readonly string[] AgentKeys =
            { "workflow", "name", "variables", "maintain_dialogue_history", "model" };
        static readonly string[] ThresholdKeys =
            { "turns_passed", "checks_passed", "clean_dialogues", "use_lower_bound" };

        public static bool TryParse(string text, out TryllDialogLabBatchConfig config, out string error)
        {
            config = null;
            if (!TryllJson.TryParse(text ?? "", out var root, out var parseErr))
            {
                error = $"Config is not valid JSON: {parseErr}";
                return false;
            }
            if (root.Kind != TryllJsonKind.Object)
            {
                error = "Config must be a JSON object.";
                return false;
            }
            if (!OnlyKeys(root, TopKeys, "", out error)) return false;

            var format = root.Get("format");
            if (format.Kind != TryllJsonKind.Number || format.Number != Format)
            {
                error = $"format must be {Format}.";
                return false;
            }

            var c = new TryllDialogLabBatchConfig();

            var agent = root.Get("agent");
            if (agent.Kind != TryllJsonKind.Object)
            {
                error = "agent must be an object with a workflow.";
                return false;
            }
            if (!OnlyKeys(agent, AgentKeys, "agent.", out error)) return false;
            if (!TryString(agent, "workflow", "agent.workflow", true, ref c.Workflow, out error)) return false;
            if (!TryString(agent, "name", "agent.name", false, ref c.AgentName, out error)) return false;
            if (!TryString(agent, "model", "agent.model", false, ref c.Model, out error)) return false;
            if (!TryBool(agent, "maintain_dialogue_history", "agent.maintain_dialogue_history", ref c.MaintainDialogueHistory, out error)) return false;
            var vars = agent.Get("variables");
            if (!vars.IsNull)
            {
                if (vars.Kind != TryllJsonKind.Object)
                {
                    error = "agent.variables must be an object.";
                    return false;
                }
                foreach (var kv in vars.Object) c.Variables.Add(kv);
            }

            if (!TryString(root, "dialogues", "dialogues", true, ref c.Dialogues, out error)) return false;
            if (!TryInt(root, "seeds", "seeds", true, 1, ref c.Seeds, out error)) return false;
            if (!TryInt(root, "base_seed", "base_seed", false, 1, ref c.BaseSeed, out error)) return false;
            if (!TryString(root, "output_dir", "output_dir", false, ref c.OutputDir, out error)) return false;
            if (!TryString(root, "settings", "settings", false, ref c.Settings, out error)) return false;
            var timeout = root.Get("turn_timeout_s");
            if (!timeout.IsNull)
            {
                if (timeout.Kind != TryllJsonKind.Number || timeout.Number < 0)
                {
                    error = "turn_timeout_s must be a number of seconds, 0 or more (0 = no timeout).";
                    return false;
                }
                c.TurnTimeoutSeconds = timeout.Number;
            }

            var thresholds = root.Get("thresholds");
            if (!thresholds.IsNull)
            {
                if (thresholds.Kind != TryllJsonKind.Object)
                {
                    error = "thresholds must be an object.";
                    return false;
                }
                if (!OnlyKeys(thresholds, ThresholdKeys, "thresholds.", out error)) return false;
                if (!TryRate(thresholds, "turns_passed", ref c.TurnsPassed, out error)) return false;
                if (!TryRate(thresholds, "checks_passed", ref c.ChecksPassed, out error)) return false;
                if (!TryRate(thresholds, "clean_dialogues", ref c.CleanDialogues, out error)) return false;
                if (!TryBool(thresholds, "use_lower_bound", "thresholds.use_lower_bound", ref c.UseLowerBound, out error)) return false;
            }
            // Intervals start at MinSeedsForInterval seeds (the Lab and the stats block show none
            // below it). Gating on one there would fail on a bound the report leaves out, or,
            // at one seed, quietly gate on the plain rate.
            if (c.UseLowerBound && c.Seeds < TryllDialogLabStatistics.MinSeedsForInterval)
            {
                error = LowerBoundSeedsError(c.Seeds);
                return false;
            }

            config = c;
            error = null;
            return true;
        }

        /// <summary>
        /// The config's variables as component-style overrides, typed against the workflow's
        /// declarations. Unlike a component's stale override (a warning), a name the workflow
        /// does not declare or a value of the wrong type is an error: CI should not run a
        /// different NPC than the config describes.
        /// </summary>
        public bool TryBuildOverrides(
            IReadOnlyList<TryllVariableDecl> declared, out List<TryllVariableOverride> overrides, out string error)
        {
            overrides = new List<TryllVariableOverride>();
            foreach (var kv in Variables)
            {
                string where = $"agent.variables.{kv.Key}";
                var decl = declared?.FirstOrDefault(d => d != null && d.Name == kv.Key);
                if (decl == null)
                {
                    error = $"{where}: the workflow declares no variable '{kv.Key}'.";
                    return false;
                }
                var value = new TryllVariableDecl { Name = decl.Name, Type = decl.Type };
                var v = kv.Value;
                bool ok;
                switch (decl.Type)
                {
                    case TryllVariableType.Int:
                        ok = v.Kind == TryllJsonKind.Number && v.Number == System.Math.Floor(v.Number);
                        if (ok) value.IntValue = (long)v.Number;
                        break;
                    case TryllVariableType.Float:
                        ok = v.Kind == TryllJsonKind.Number;
                        if (ok) value.FloatValue = v.Number;
                        break;
                    case TryllVariableType.String:
                        ok = v.Kind == TryllJsonKind.String;
                        if (ok) value.StringValue = v.String;
                        break;
                    case TryllVariableType.Bool:
                        ok = v.Kind == TryllJsonKind.Bool;
                        if (ok) value.BoolValue = v.Bool;
                        break;
                    case TryllVariableType.StringSet:
                        ok = v.Kind == TryllJsonKind.Array && v.Array.All(e => e.Kind == TryllJsonKind.String);
                        if (ok) value.StringSetValue = v.Array.Select(e => e.String).ToList();
                        break;
                    default:
                        ok = false;
                        break;
                }
                if (!ok)
                {
                    error = $"{where}: expected {TypeWord(decl.Type)}.";
                    return false;
                }
                overrides.Add(new TryllVariableOverride { Name = decl.Name, Enabled = true, Value = value });
            }
            error = null;
            return true;
        }

        /// <param name="what">"config" before the run, "run" for the summary's guard.</param>
        internal static string LowerBoundSeedsError(int seeds, string what = "config") =>
            $"thresholds.use_lower_bound needs at least {TryllDialogLabStatistics.MinSeedsForInterval} seeds "
          + $"(intervals start there); this {what} has {seeds}.";

        static string TypeWord(TryllVariableType type)
        {
            switch (type)
            {
                case TryllVariableType.Int:       return "a whole number";
                case TryllVariableType.Float:     return "a number";
                case TryllVariableType.String:    return "a string";
                case TryllVariableType.Bool:      return "true or false";
                case TryllVariableType.StringSet: return "an array of strings";
                default:                          return type.ToString();
            }
        }

        static bool OnlyKeys(TryllJsonValue obj, string[] allowed, string prefix, out string error)
        {
            foreach (var key in obj.Object.Keys)
            {
                if (System.Array.IndexOf(allowed, key) < 0)
                {
                    error = $"Unknown key '{prefix}{key}'.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        static bool TryString(TryllJsonValue obj, string key, string where, bool required, ref string field, out string error)
        {
            var v = obj.Get(key);
            if (v.IsNull)
            {
                error = required ? $"{where} is required." : null;
                return !required;
            }
            if (v.Kind != TryllJsonKind.String || (required && string.IsNullOrWhiteSpace(v.String)))
            {
                error = required ? $"{where} must be a non-empty string." : $"{where} must be a string.";
                return false;
            }
            field = v.String;
            error = null;
            return true;
        }

        static bool TryInt(TryllJsonValue obj, string key, string where, bool required, int min, ref int field, out string error)
        {
            var v = obj.Get(key);
            if (v.IsNull)
            {
                error = required ? $"{where} is required." : null;
                return !required;
            }
            if (v.Kind != TryllJsonKind.Number || v.Number != System.Math.Floor(v.Number) || v.Number < min || v.Number > int.MaxValue)
            {
                error = $"{where} must be a whole number, {min} or more.";
                return false;
            }
            field = (int)v.Number;
            error = null;
            return true;
        }

        static bool TryBool(TryllJsonValue obj, string key, string where, ref bool field, out string error)
        {
            var v = obj.Get(key);
            error = null;
            if (v.IsNull) return true;
            if (v.Kind != TryllJsonKind.Bool)
            {
                error = $"{where} must be true or false.";
                return false;
            }
            field = v.Bool;
            return true;
        }

        static bool TryRate(TryllJsonValue obj, string key, ref double? field, out string error)
        {
            var v = obj.Get(key);
            error = null;
            if (v.IsNull) return true;
            if (v.Kind != TryllJsonKind.Number || v.Number < 0 || v.Number > 1)
            {
                error = $"thresholds.{key} must be a rate from 0 to 1, or null.";
                return false;
            }
            field = v.Number;
            return true;
        }
    }
}
