// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Static helper that validates a <see cref="TryllGraphDescription"/> for common
    /// authoring errors and returns a list of human-readable issue strings.
    ///
    /// Call <see cref="Validate"/> from:
    /// - A <c>[ContextMenu]</c> on <see cref="TryllWorkflowAsset"/> (see the extension class below).
    /// - The future visual-graph-editor canvas overlay.
    /// - CI asset validation scripts.
    ///
    /// Checks performed
    /// ----------------
    /// 1. StartNode is non-empty and present in Nodes.
    /// 2. No duplicate node names.
    /// 3. Each exit field (annotated <see cref="TryllExitAttribute"/>) references a node
    ///    that exists in the graph, or is empty (END).
    /// 4. Nodes with no incoming exit and not the start node (orphan detection).
    ///    Orphans are a warning, not an error — they may be intentional in draft graphs.
    /// 5. Storage file-picker fields: absolute paths and ".." traversal are errors;
    ///    kind mismatch (.kb.json records on a plain field, .txt on an embedded field)
    ///    and virtual names (no recognized extension) are warnings. A .json path is
    ///    accepted on both kinds (config for embedded, Map for plain).
    /// 9. Per-agent Variables references: every <c>{{var.&lt;name&gt;}}</c> in a node
    ///    template and every <c>{"var": "&lt;name&gt;"}</c> operand in a Retrieve/
    ///    ClassifyIntent <c>Filter</c> JSON must name a variable declared on the
    ///    workflow (the <c>declaredVariables</c> parameter of <see cref="Validate"/>).
    /// 10. Branch cross-field coherence: test/values/condition/input/variable shape rules
    ///     mirroring the server's <c>ValidateBranchParamShape</c>, plus a same-target
    ///     then_exit/else_exit warning and a hedged .NET-regex-compiles check for
    ///     test=Regex values. Condition *grammar* is never validated client-side.
    /// 11. Summarize cross-field coherence: a node with both triggers disabled
    ///     never folds, out-of-range hot window / summary cap, and a graph with
    ///     no Generate-class node for the summary to reach.
    ///
    /// Voice (protocol v27): <see cref="ValidateVoiceProfile(TryllVoiceProfile)"/> checks a
    /// <see cref="TryllVoiceProfile"/> (model required, ranges, mode names, clip paths,
    /// catalog capabilities) and <see cref="ValidateAgentComponent"/> flags a speaking
    /// workflow with no voice (the server's AgentVoiceRequired).
    /// </summary>
    public static class TryllWorkflowValidator
    {
        /// <summary>Represents a single validation finding.</summary>
        public sealed class Issue
        {
            public enum Severity { Warning, Error }
            public Severity Level;
            public string   Message;

            public Issue(Severity level, string message)
            {
                Level   = level;
                Message = message;
            }

            public override string ToString() => $"[{Level}] {Message}";
        }

        /// <summary>
        /// Validate <paramref name="graph"/> and return a (possibly empty) list of issues.
        /// </summary>
        /// <param name="graph">Graph to validate.</param>
        /// <param name="declaredVariables">
        /// This workflow's declared per-agent Variables (<see cref="TryllWorkflowAsset.Variables"/>),
        /// used to validate <c>{{var.x}}</c> template refs and <c>{"var":"x"}</c> filter operands
        /// (check 9). Omit/empty to skip that check (e.g. when validating a bare
        /// <see cref="TryllGraphDescription"/> with no asset).
        /// </param>
        public static List<Issue> Validate(TryllGraphDescription graph, List<TryllVariableDecl> declaredVariables = null)
        {
            var issues = new List<Issue>();

            if (graph == null)
            {
                issues.Add(new Issue(Issue.Severity.Error, "Graph is null."));
                return issues;
            }

            var nodes = graph.Nodes ?? new List<TryllNodeDescription>();

            // ── 1. StartNode ──────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(graph.StartNode))
                issues.Add(new Issue(Issue.Severity.Error, "StartNode is empty."));

            // ── 2. Build name → index map; catch duplicates ───────────────────
            var nameToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < nodes.Count; i++)
            {
                string name = nodes[i]?.Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name))
                {
                    issues.Add(new Issue(Issue.Severity.Error, $"Nodes[{i}] has an empty name."));
                    continue;
                }
                if (nameToIndex.ContainsKey(name))
                    issues.Add(new Issue(Issue.Severity.Error, $"Duplicate node name '{name}' at indices {nameToIndex[name]} and {i}."));
                else
                    nameToIndex[name] = i;
            }

            // ── 3. StartNode must resolve ─────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(graph.StartNode) && !nameToIndex.ContainsKey(graph.StartNode))
                issues.Add(new Issue(Issue.Severity.Error, $"StartNode '{graph.StartNode}' is not present in Nodes."));

            // ── 4. Exit fields: dangling references ───────────────────────────
            // Track which nodes are reachable (have at least one incoming exit).
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(graph.StartNode))
                reachable.Add(graph.StartNode);

            foreach (var node in nodes)
            {
                if (node?.Params == null) continue;
                string srcName = node.Name ?? string.Empty;

                foreach (var field in node.Params.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var exitAttr = field.GetCustomAttribute<TryllExitAttribute>();
                    if (exitAttr == null) continue;

                    string target = (field.GetValue(node.Params) as string) ?? string.Empty;
                    if (string.IsNullOrEmpty(target))
                        continue; // empty = END, always valid

                    if (!nameToIndex.ContainsKey(target))
                        issues.Add(new Issue(Issue.Severity.Error,
                            $"Node '{srcName}' exit '{exitAttr.ExitName}' references unknown node '{target}'."));
                    else
                        reachable.Add(target);
                }
            }

            // ── 5. Orphan detection ───────────────────────────────────────────
            foreach (var node in nodes)
            {
                string name = node?.Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (!reachable.Contains(name))
                    issues.Add(new Issue(Issue.Severity.Warning,
                        $"Node '{name}' is an orphan (no incoming exits and not the start node)."));
            }

            // ── 5b. Pausing graphs need game code ─────────────────────────────
            // A pause holds the turn open until Resume; nothing in the graph can
            // end it. The validator cannot see whether the game subscribes to
            // Paused, so this is an advisory — but an unresumed pause wedges the
            // agent and rejects every later SendMessage with AgentBusy.
            foreach (var node in nodes)
            {
                if (!(node?.Params is TryllPauseParams pause)) continue;
                if (pause.PauseTimeoutMs > 0) continue;  // has a deadline — cannot hang
                issues.Add(new Issue(Issue.Severity.Warning,
                    $"Node '{node.Name}' pauses the turn and has no Pause Timeout Ms: your game "
                    + "must call Resume (subscribe to TryllClient.Paused, or RegisterTool a "
                    + "handler for a pausing Tool Call). An unresumed pause holds the agent open "
                    + "and rejects further SendMessage calls with AgentBusy. Set a Pause Timeout "
                    + "Ms (with a Timeout Exit for a fallback line) if the game cannot guarantee "
                    + "a Resume."));
            }

            // ── 6. Grammar / reserved-name / duplicate-slot checks ─────────────
            var effectiveSlots = new Dictionary<string, List<string>>(StringComparer.Ordinal); // slot -> producing node names
            foreach (var node in nodes)
            {
                string name = node?.Name ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(name) && (!TryllRenamePropagation.IsValidGrammar(name) || TryllRenamePropagation.IsReserved(name)))
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node name '{name}' must match [A-Za-z_][A-Za-z0-9_]* and not be the reserved '{TryllRenamePropagation.ReservedUserMessage}'."));

                if (node?.Params == null) continue;
                var slotNameField = TryllNodeTypeRegistry.GetSlotNameField(node.Params.GetType());
                if (slotNameField != null)
                {
                    string overrideVal = TryllNodeTypeRegistry.ReadSlotName(node.Params, slotNameField);
                    if (!string.IsNullOrEmpty(overrideVal) &&
                        (!TryllRenamePropagation.IsValidGrammar(overrideVal) || TryllRenamePropagation.IsReserved(overrideVal)))
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            $"Node '{name}' slot override '{overrideVal}' must match [A-Za-z_][A-Za-z0-9_]* and not be the reserved '{TryllRenamePropagation.ReservedUserMessage}'."));
                    }

                    foreach (string effective in TryllSlotGraphAnalysis.GetEffectiveWriteSlots(node))
                    {
                        if (string.IsNullOrEmpty(effective)) continue;
                        if (!effectiveSlots.TryGetValue(effective, out var producers))
                            effectiveSlots[effective] = producers = new List<string>();
                        producers.Add(name);
                    }
                }
            }
            foreach (var kv in effectiveSlots)
            {
                if (kv.Value.Count > 1)
                    issues.Add(new Issue(Issue.Severity.Warning,
                        $"Slot '{kv.Key}' is produced by multiple nodes ({string.Join(", ", kv.Value)}); the effective value is whichever branch executes."));
            }

            // ── 7. May/must slot-availability checks ────────────────────────────
            foreach (var node in nodes)
            {
                if (node?.Params == null || string.IsNullOrEmpty(node.Name)) continue;
                var upstreamSlots = new HashSet<string>(TryllSlotGraphAnalysis.GetUpstreamSlots(graph, node.Name), StringComparer.Ordinal);

                var refs = new List<string>();
                foreach (var inputField in TryllNodeTypeRegistry.GetSlotInputFields(node.Params.GetType()))
                {
                    string val = TryllNodeTypeRegistry.ReadSlotInput(node.Params, inputField);
                    if (!string.IsNullOrEmpty(val)) refs.Add(val);
                }
                foreach (var templateField in TryllNodeTypeRegistry.GetTemplateFields(node.Params.GetType()))
                {
                    string tmpl = templateField.GetValue(node.Params) as string;
                    if (!string.IsNullOrEmpty(tmpl))
                        refs.AddRange(TryllMustacheRefs.ExtractRefs(tmpl));
                }
                foreach (var filterField in TryllNodeTypeRegistry.GetFilterJsonFields(node.Params.GetType()))
                {
                    string filterJson = filterField.GetValue(node.Params) as string;
                    if (!string.IsNullOrEmpty(filterJson))
                        refs.AddRange(TryllFilterRefs.ExtractSlotRefs(filterJson));
                }

                foreach (string slot in refs)
                {
                    if (!upstreamSlots.Contains(slot))
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            $"Node '{node.Name}' references slot '{slot}', which is not producible on any path reaching this node."));
                    }
                    else if (!TryllSlotGraphAnalysis.IsMustAvailable(graph, node.Name, slot))
                    {
                        issues.Add(new Issue(Issue.Severity.Warning,
                            $"Node '{node.Name}' references slot '{slot}', which is only produced on some branches reaching this node (may be empty at runtime)."));
                    }
                }
            }

            // ── 8. Storage file-picker field checks ───────────────────────────
            string storageRoot = ResolveStorageRoot();

            foreach (var node in nodes)
            {
                if (node?.Params == null) continue;
                string srcName = node.Name ?? string.Empty;

                foreach (var (fieldPath, field, value) in FilePickerValues(node.Params, string.Empty, 0))
                {
                    if (IsAbsoluteStoragePath(value))
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            $"Node '{srcName}' field '{fieldPath}' has an absolute path '{value}'; must be relative to storage_root."));
                        continue;
                    }

                    if (value.Contains(".."))
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            $"Node '{srcName}' field '{fieldPath}' path '{value}' contains '..'; directory traversal is not allowed."));
                        continue;
                    }

                    // Paths inside nested tables (LoRA adapters today) are model
                    // files, not string storages: check the file and the extension
                    // only.
                    if (field.DeclaringType != node.Params.GetType())
                    {
                        if (field.DeclaringType == typeof(TryllLoraAdapter)
                            && !value.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
                            issues.Add(new Issue(Issue.Severity.Warning,
                                $"Node '{srcName}' field '{fieldPath}': '{value}' is not a .gguf file; LoRA adapters are GGUF files."));
                        if (!string.IsNullOrEmpty(storageRoot))
                        {
                            string nestedFull = Path.Combine(storageRoot, value.Replace('/', Path.DirectorySeparatorChar));
                            if (!File.Exists(nestedFull))
                                issues.Add(new Issue(Issue.Severity.Warning,
                                    $"Node '{srcName}' field '{fieldPath}': file '{value}' not found under storage root '{storageRoot}'."));
                        }
                        continue;
                    }

                    bool isEmbeddedField = field.Name == "EmbeddedStringStorage";
                    // Embedded storages are referenced by a .json config (or the .kb.json
                    // records array). Plain storages are .txt (List), .json (Map), or
                    // .mm.json (Multimap). .kb.json is embedded-only.
                    bool isKbRecordsExt = value.EndsWith(".kb.json", StringComparison.OrdinalIgnoreCase);
                    bool isJsonExt      = value.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
                    bool isTxtExt       = value.EndsWith(".txt",  StringComparison.OrdinalIgnoreCase);
                    bool hasKnownExt    = isJsonExt || isTxtExt;

                    if (hasKnownExt)
                    {
                        // Only flag a genuine kind mismatch: a plain-only .txt on an embedded
                        // field, or the embedded-only .kb.json records file on a plain field.
                        // A .json path is accepted on both (config for embedded, Map for plain).
                        if (isEmbeddedField && isTxtExt)
                            issues.Add(new Issue(Issue.Severity.Warning,
                                $"Node '{srcName}' field '{fieldPath}': '{value}' looks like plain storage; expected a .json config for embedded fields."));
                        else if (!isEmbeddedField && isKbRecordsExt)
                            issues.Add(new Issue(Issue.Severity.Warning,
                                $"Node '{srcName}' field '{fieldPath}': '{value}' looks like embedded storage records; expected .txt or .json for plain fields."));

                        // File-existence check: warn (not error) on miss to tolerate
                        // per-session root ambiguity and server-only deployment layouts.
                        if (!string.IsNullOrEmpty(storageRoot))
                        {
                            string fullPath = Path.Combine(storageRoot, value.Replace('/', Path.DirectorySeparatorChar));
                            if (!File.Exists(fullPath))
                                issues.Add(new Issue(Issue.Severity.Warning,
                                    $"Node '{srcName}' field '{fieldPath}': file '{value}' not found under storage root '{storageRoot}'."));
                        }
                    }
                    else
                    {
                        issues.Add(new Issue(Issue.Severity.Warning,
                            $"Node '{srcName}' field '{fieldPath}' value '{value}' has no recognized extension; if this is a virtual name it must be registered via CreateStringStorageRequest before CreateAgent."));
                    }
                }
            }

            // ── 9. Per-agent Variables references ─────────────────────────────
            var declaredVarNames = new HashSet<string>(StringComparer.Ordinal);
            var enabledFoldNames = new Dictionary<string, string>(StringComparer.Ordinal);
            if (declaredVariables != null)
                foreach (var d in declaredVariables)
                {
                    if (d != null && !string.IsNullOrEmpty(d.Name))
                        declaredVarNames.Add(d.Name);
                    if (d == null || !d.AllowOutputSubstitution)
                        continue;

                    if (d.Type == TryllVariableType.StringSet)
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            $"Variable '{d.Name}' cannot enable output substitution on a StringSet."));
                        continue;
                    }
                    if (d.Name != null &&
                        (d.Name.Contains("__") || d.Name.EndsWith("_", StringComparison.Ordinal)))
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            $"Variable '{d.Name}' allows output substitution, so its name must not contain '__' or end with '_'."));
                    }
                    if (d.Type == TryllVariableType.String)
                    {
                        int utf8Bytes = Encoding.UTF8.GetByteCount(d.StringValue ?? "");
                        if (utf8Bytes > 2048)
                        {
                            issues.Add(new Issue(Issue.Severity.Error,
                                $"Variable '{d.Name}' allows output substitution, so its string value must be at most 2048 UTF-8 bytes (got {utf8Bytes})."));
                        }
                    }
                    if (!string.IsNullOrEmpty(d.Name))
                    {
                        string fold = d.Name.ToLowerInvariant();
                        if (enabledFoldNames.TryGetValue(fold, out string prior))
                        {
                            issues.Add(new Issue(Issue.Severity.Error,
                                $"Variables '{prior}' and '{d.Name}' both allow output substitution and collide under ASCII case-folding."));
                        }
                        else
                            enabledFoldNames[fold] = d.Name;
                    }
                }

            foreach (var node in nodes)
            {
                if (node?.Params == null || string.IsNullOrEmpty(node.Name)) continue;

                foreach (var templateField in TryllNodeTypeRegistry.GetTemplateFields(node.Params.GetType()))
                {
                    string tmpl = templateField.GetValue(node.Params) as string;
                    if (string.IsNullOrEmpty(tmpl)) continue;
                    foreach (string varName in TryllVarRefs.ExtractRefs(tmpl))
                    {
                        if (!declaredVarNames.Contains(varName))
                            issues.Add(new Issue(Issue.Severity.Error,
                                $"Node '{node.Name}' template references undeclared variable 'var.{varName}'."));
                    }
                }

                foreach (var filterField in TryllNodeTypeRegistry.GetFilterJsonFields(node.Params.GetType()))
                {
                    string filterJson = filterField.GetValue(node.Params) as string;
                    if (string.IsNullOrEmpty(filterJson)) continue;
                    foreach (string varName in TryllFilterRefs.ExtractVarRefs(filterJson))
                    {
                        if (!declaredVarNames.Contains(varName))
                            issues.Add(new Issue(Issue.Severity.Error,
                                $"Node '{node.Name}' filter references undeclared variable '{{\"var\": \"{varName}\"}}'."));
                    }
                }
            }

            // ── 10. Branch cross-field coherence checks ───────────────────────
            foreach (var node in nodes)
            {
                if (!(node?.Params is TryllBranchParams branch) || string.IsNullOrEmpty(node.Name))
                    continue;

                bool hasInput = !string.IsNullOrEmpty(branch.Input);
                bool hasVariable = !string.IsNullOrEmpty(branch.Variable);
                bool valuesEmpty = branch.Values == null || branch.Values.Length == 0;
                bool isTextTest = branch.Test == TryllBranchTest.Equals ||
                                   branch.Test == TryllBranchTest.EqualsIgnoreCase ||
                                   branch.Test == TryllBranchTest.Contains ||
                                   branch.Test == TryllBranchTest.Regex;
                bool isExpression = branch.Test == TryllBranchTest.Expression;

                if (isTextTest && valuesEmpty)
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node '{node.Name}': test={branch.Test} requires a non-empty 'values' list."));

                if (isExpression && string.IsNullOrEmpty(branch.Condition))
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node '{node.Name}': test=Expression requires a non-empty 'condition'."));

                if (!isExpression && hasInput && hasVariable)
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node '{node.Name}': set either 'input' or 'variable', not both."));

                if (hasVariable && !declaredVarNames.Contains(branch.Variable))
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node '{node.Name}' references undeclared variable '{branch.Variable}'."));

                if (!isExpression && !string.IsNullOrEmpty(branch.Condition))
                    issues.Add(new Issue(Issue.Severity.Warning,
                        $"Node '{node.Name}': 'condition' is ignored under test={branch.Test} (only used by Expression)."));

                bool valuesIgnored = branch.Test == TryllBranchTest.IsTrue ||
                                      branch.Test == TryllBranchTest.NonEmpty ||
                                      isExpression;
                if (valuesIgnored && !valuesEmpty)
                    issues.Add(new Issue(Issue.Severity.Warning,
                        $"Node '{node.Name}': 'values' is ignored under test={branch.Test}."));

                string thenExit = branch.ThenExit ?? string.Empty;
                string elseExit = branch.ElseExit ?? string.Empty;
                if (thenExit == elseExit)
                    issues.Add(new Issue(Issue.Severity.Warning,
                        $"Node '{node.Name}': then_exit and else_exit both target '{(string.IsNullOrEmpty(thenExit) ? "END" : thenExit)}'; the branch never changes routing."));

                if (branch.Test == TryllBranchTest.Regex && branch.Values != null)
                {
                    for (int i = 0; i < branch.Values.Length; i++)
                    {
                        try
                        {
                            _ = new System.Text.RegularExpressions.Regex(branch.Values[i] ?? string.Empty);
                        }
                        catch (System.ArgumentException ex)
                        {
                            // .NET's regex engine (used here) is not identical to the server's
                            // ECMAScript std::regex — this is a hint, not a guarantee either way.
                            issues.Add(new Issue(Issue.Severity.Warning,
                                $"Node '{node.Name}': values[{i}] '{branch.Values[i]}' does not compile as a .NET regex ({ex.Message}); the server uses ECMAScript std::regex, which is similar but not identical, so this is a hint, not a guarantee."));
                        }
                    }
                }
            }

            // ── 11. Summarize cross-field coherence checks ────────────────────
            //
            // A Summarize node with nothing to trigger it is not an error on the
            // server (an author may enable it later via ChangeAgentParam), but
            // in the editor it is almost always a mistake worth surfacing.
            foreach (var node in nodes)
            {
                if (!(node?.Params is TryllSummarizeParams summarize) || string.IsNullOrEmpty(node.Name))
                    continue;

                if (summarize.TriggerTokens < 0 || summarize.TriggerTurns < 0)
                {
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node '{node.Name}': trigger_tokens and trigger_turns must be >= 0."));
                }
                else if (summarize.TriggerTokens == 0 && summarize.TriggerTurns == 0)
                {
                    issues.Add(new Issue(Issue.Severity.Warning,
                        $"Node '{node.Name}': both trigger_tokens and trigger_turns are 0, so this node never folds and the dialogue is still trimmed."));
                }

                if (summarize.KeepRecentTurns < 0 || summarize.KeepRecentTurns > 512)
                {
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node '{node.Name}': keep_recent_turns must be between 0 and 512."));
                }

                if (summarize.MaxSummaryTokens < 16)
                {
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node '{node.Name}': max_summary_tokens must be >= 16."));
                }
                else if (summarize.ContextSize > 0
                         && (long)summarize.MaxSummaryTokens + 64 >= summarize.ContextSize)
                {
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node '{node.Name}': max_summary_tokens must leave at least 64 tokens in context_size for the summarizer prompt."));
                }

                if (!summarize.RunInline)
                {
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node '{node.Name}': run_inline=false is reserved but not implemented; enable run_inline."));
                }

                // The summary only reaches the model through a Generate-class
                // node's system message, so a graph with no such node folds
                // every few turns and throws the result away.
                bool hasConsumer = false;
                foreach (var other in nodes)
                {
                    if (other?.Params is TryllGenerateParams || other?.Params is TryllGenerateAndSpeakParams)
                    {
                        hasConsumer = true;
                        break;
                    }
                }
                if (!hasConsumer)
                {
                    issues.Add(new Issue(Issue.Severity.Warning,
                        $"Node '{node.Name}': the graph has no Generate or GenerateAndSpeak node, so nothing ever reads the summary this node produces."));
                }
            }

            // ── 12. ClassifyAttributesLLM shape ──────────────────────────────
            foreach (var node in nodes)
            {
                if (!(node?.Params is TryllClassifyAttributesLLMParams attrs) || string.IsNullOrEmpty(node.Name))
                    continue;
                var attributes = attrs.Attributes ?? System.Array.Empty<TryllClassifyAttributeDef>();
                if (attributes.Length < 1 || attributes.Length > 16)
                {
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node '{node.Name}': attributes must contain 1 to 16 entries."));
                }
                var names = new HashSet<string>(StringComparer.Ordinal);
                bool canTrigger = false;
                for (int i = 0; i < attributes.Length; i++)
                {
                    var attribute = attributes[i];
                    string prefix = $"Node '{node.Name}' attributes[{i}]";
                    if (attribute == null)
                    {
                        issues.Add(new Issue(Issue.Severity.Error, prefix + ": is missing."));
                        continue;
                    }
                    if (string.IsNullOrEmpty(attribute.Name) || !IsAttributeName(attribute.Name))
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            prefix + ": name must match [A-Za-z_][A-Za-z0-9_]*."));
                    }
                    else if (!names.Add(attribute.Name))
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            prefix + $": duplicate name '{attribute.Name}'."));
                    }
                    if (!string.IsNullOrEmpty(attribute.SystemPrompt)
                        && attribute.SystemPrompt.IndexOf("{{intents_block}}", StringComparison.Ordinal) < 0)
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            prefix + ": system_prompt must reference {{intents_block}}."));
                    }
                    if (attribute.Kind == TryllAttributeKind.Boolean)
                    {
                        if (attribute.Choices != null && attribute.Choices.Length > 0)
                        {
                            issues.Add(new Issue(Issue.Severity.Error,
                                prefix + ": Boolean cannot define choices."));
                        }
                        if (string.IsNullOrEmpty(attribute.Statement))
                        {
                            issues.Add(new Issue(Issue.Severity.Error,
                                prefix + ": statement is required for Boolean."));
                        }
                        if (!string.IsNullOrEmpty(attribute.SystemPrompt)
                            && attribute.SystemPrompt.IndexOf("{{statement}}", StringComparison.Ordinal) < 0)
                        {
                            issues.Add(new Issue(Issue.Severity.Error,
                                prefix + ": Boolean system_prompt must reference {{statement}}."));
                        }
                        if (attribute.BooleanTrigger != TryllBooleanTrigger.Never)
                            canTrigger = true;
                        if (!string.IsNullOrEmpty(attribute.UncertainChoice)
                            || attribute.ScoreTrigger != TryllScoreTrigger.Never
                            || Math.Abs(attribute.ScoreThreshold - 0.5f) > 0.0001f
                            || Math.Abs(attribute.Margin - 0.15f) > 0.0001f)
                        {
                            issues.Add(new Issue(Issue.Severity.Error,
                                prefix + ": only statement, threshold, and boolean_trigger apply."));
                        }
                        continue;
                    }
                    if (!string.IsNullOrEmpty(attribute.Statement))
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            prefix + ": statement is only valid for Boolean."));
                    }
                    var choices = attribute.Choices ?? System.Array.Empty<TryllClassifyAttributeChoice>();
                    if (choices.Length < 2 || choices.Length > 26)
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            prefix + ": needs 2 to 26 choices."));
                    }
                    var ids = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var choice in choices)
                    {
                        if (choice == null || string.IsNullOrEmpty(choice.Id) || string.IsNullOrEmpty(choice.Prompt))
                        {
                            issues.Add(new Issue(Issue.Severity.Error,
                                prefix + ": each choice needs an id and a prompt."));
                            continue;
                        }
                        if (choice.Id == TryllUncertainChoice.Id)
                        {
                            issues.Add(new Issue(Issue.Severity.Error,
                                prefix + $": choice id '{choice.Id}' is reserved."));
                        }
                        else if (!ids.Add(choice.Id))
                        {
                            issues.Add(new Issue(Issue.Severity.Error,
                                prefix + $": duplicate choice id '{choice.Id}'."));
                        }
                        if (attribute.Kind == TryllAttributeKind.Score && choice.Triggers)
                        {
                            issues.Add(new Issue(Issue.Severity.Error,
                                prefix + ": Score choice triggers are not used."));
                        }
                        if (attribute.Kind == TryllAttributeKind.Choice && choice.Triggers)
                            canTrigger = true;
                    }
                    if (attribute.Kind == TryllAttributeKind.Choice
                        && attribute.UncertainChoice != TryllUncertainChoice.Id
                        && (string.IsNullOrEmpty(attribute.UncertainChoice) || !ids.Contains(attribute.UncertainChoice)))
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            prefix + ": uncertain_choice must name one of its choices or '"
                            + TryllUncertainChoice.Id + "'."));
                    }
                    if (attribute.Kind == TryllAttributeKind.Score
                        && attribute.ScoreTrigger != TryllScoreTrigger.Never)
                    {
                        canTrigger = true;
                    }
                    if (attribute.Kind == TryllAttributeKind.Score)
                    {
                        if (!string.IsNullOrEmpty(attribute.UncertainChoice)
                            || attribute.BooleanTrigger != TryllBooleanTrigger.Never
                            || Math.Abs(attribute.Threshold - 0.5f) > 0.0001f
                            || Math.Abs(attribute.Margin - 0.15f) > 0.0001f)
                        {
                            issues.Add(new Issue(Issue.Severity.Error,
                                prefix + ": only the ladder, score_trigger, and score_threshold apply."));
                        }
                    }
                    else if (attribute.BooleanTrigger != TryllBooleanTrigger.Never
                        || attribute.ScoreTrigger != TryllScoreTrigger.Never
                        || Math.Abs(attribute.ScoreThreshold - 0.5f) > 0.0001f)
                    {
                        issues.Add(new Issue(Issue.Severity.Error,
                            prefix + ": boolean_trigger, score_trigger, and score_threshold do not apply."));
                    }
                }
                bool wired = !string.IsNullOrEmpty(attrs.TriggeredExit);
                if (wired != canTrigger)
                {
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Node '{node.Name}': triggered_exit must be set exactly when an attribute can trigger."));
                }
            }

            return issues;
        }

        static bool IsAttributeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            char first = name[0];
            if (!IsAsciiLetter(first) && first != '_') return false;
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (!IsAsciiLetter(c) && !IsAsciiDigit(c) && c != '_') return false;
            }
            return true;
        }

        static bool IsAsciiLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        static bool IsAsciiDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        // ── Voice profiles (protocol v27) ─────────────────────────────────────

        /// <summary>
        /// Validate a <see cref="TryllAgentComponent"/>'s workflow/voice pairing: a workflow
        /// with a speaking node (Speak, GenerateAndSpeak) and no voice profile is an error,
        /// matching the server's AgentVoiceRequired (3025). An assigned profile is validated
        /// with <see cref="ValidateVoiceProfile(TryllVoiceProfile)"/>.
        /// </summary>
        public static List<Issue> ValidateAgentComponent(TryllAgentComponent component)
        {
            var issues = new List<Issue>();
            if (component == null) return issues;
            var graph = component.WorkflowAsset != null ? component.WorkflowAsset.Graph : null;
            bool speaks = TryllAgentComponent.GraphUsesTts(graph);
            var voice = component.Voice;
            if (speaks && (voice == null || voice.Data == null))
            {
                issues.Add(new Issue(Issue.Severity.Error,
                    $"Workflow '{component.WorkflowAsset.name}' speaks (Speak / GenerateAndSpeak) but no " +
                    "Voice profile is assigned. Agent creation fails with AgentVoiceRequired. " +
                    "Create ▸ Tryll ▸ Voice Profile, then assign it as Voice."));
                return issues;
            }
            if (voice != null)
                issues.AddRange(ValidateVoiceProfile(voice));
            if (voice?.Data != null && component.OverrideVoiceDelivery)
            {
                string mode = TryllAgentComponent.NormalizeVoiceDelivery(component.VoiceDelivery);
                bool defined = string.IsNullOrEmpty(mode);
                if (!defined && voice.Data.DeliveryModes != null)
                {
                    foreach (var delivery in voice.Data.DeliveryModes)
                    {
                        if (delivery != null && delivery.Name == mode)
                        {
                            defined = true;
                            break;
                        }
                    }
                }
                if (!defined)
                {
                    issues.Add(new Issue(Issue.Severity.Error,
                        $"Voice Delivery override '{mode}' is not defined by Voice profile '{voice.name}'."));
                }
            }
            return issues;
        }

        /// <summary>
        /// Validate a voice profile asset against the catalog TTS capabilities of its model
        /// (when the bundled catalog is readable) and the storage root.
        /// </summary>
        public static List<Issue> ValidateVoiceProfile(TryllVoiceProfile profile)
        {
            if (profile == null) return new List<Issue>();
            TryllTtsCapabilities? caps = null;
            if (profile.Data != null &&
                TryllModelCatalogFile.TryGetTtsCapabilities(profile.Data.TtsModelName, out var c))
                caps = c;
            return ValidateVoiceProfile(profile.Data, profile.name, caps, ResolveStorageRoot());
        }

        /// <summary>
        /// Validate voice profile data. Mirrors the server's checks where they can be made
        /// client-side: required model, ranges, mode names, text limits, clip paths, and —
        /// when <paramref name="caps"/> is known — fields the model cannot use.
        /// </summary>
        /// <param name="label">Asset name used in messages.</param>
        /// <param name="caps">Catalog capabilities of the profile's model; null = unknown (skip those checks).</param>
        /// <param name="storageRoot">Storage root for clip existence checks; null/empty = skip.</param>
        public static List<Issue> ValidateVoiceProfile(
            TryllVoiceProfileData data, string label, TryllTtsCapabilities? caps = null, string storageRoot = null)
        {
            var issues = new List<Issue>();
            string who = $"Voice profile '{label}'";
            if (data == null)
            {
                issues.Add(new Issue(Issue.Severity.Error, $"{who} has no data."));
                return issues;
            }

            void Err(string m)  => issues.Add(new Issue(Issue.Severity.Error,   $"{who}: {m}"));
            void Warn(string m) => issues.Add(new Issue(Issue.Severity.Warning, $"{who}: {m}"));

            if (string.IsNullOrEmpty(data.TtsModelName))
                Err("Tts Model Name is required (there is no default TTS model).");
            if (data.SpeakerId < 0)
                Err($"Speaker Id {data.SpeakerId} must be 0 or more.");
            if (data.Speed < 0.1f || data.Speed > 3f)
                Err($"Speed {data.Speed} is outside 0.1-3.0.");
            if (data.Expressiveness < 0f || data.Expressiveness > 2f)
                Err($"Expressiveness {data.Expressiveness} is outside 0.0-2.0.");
            CheckVoiceText(data.VoiceDescription, "Voice Description", Err);

            // Delivery modes.
            var modeNames = new HashSet<string>(StringComparer.Ordinal);
            var modes = data.DeliveryModes ?? new List<TryllDeliveryMode>();
            for (int i = 0; i < modes.Count; i++)
            {
                var m = modes[i];
                if (m == null) continue;
                string mw = string.IsNullOrEmpty(m.Name) ? $"mode #{i}" : $"mode '{m.Name}'";
                if (!TryllVoiceProfileData.IsValidModeName(m.Name))
                    Err($"{mw}: name must be 1-32 characters of a-z, 0-9 and '_'.");
                else if (m.Name == TryllVoiceProfileData.NeutralMode)
                    Err("'neutral' is built in and cannot be defined as a delivery mode.");
                else if (!modeNames.Add(m.Name))
                    Err($"{mw} is defined more than once.");
                if (m.SpeedScale < 0.5f || m.SpeedScale > 2f)
                    Err($"{mw}: Speed Scale {m.SpeedScale} is outside 0.5-2.0.");
                if (m.Expressiveness > 2f)
                    Err($"{mw}: Expressiveness {m.Expressiveness} is above 2.0.");
                CheckVoiceText(m.Instruction, $"{mw} Instruction", Err);
            }
            if (!string.IsNullOrEmpty(data.DefaultDeliveryMode)
                && data.DefaultDeliveryMode != TryllVoiceProfileData.NeutralMode
                && !modeNames.Contains(data.DefaultDeliveryMode))
                Err($"Default Delivery Mode '{data.DefaultDeliveryMode}' is not one of the profile's delivery modes.");

            // Clip paths.
            foreach (var (owner, path) in data.ReferenceClips())
            {
                if (IsAbsoluteStoragePath(path))
                {
                    Err($"{owner} has an absolute path '{path}'; it must be relative to the storage root.");
                    continue;
                }
                if (path.Contains(".."))
                {
                    Err($"{owner} path '{path}' contains '..'; directory traversal is not allowed.");
                    continue;
                }
                if (!string.IsNullOrEmpty(storageRoot))
                {
                    string full = Path.Combine(storageRoot, path.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(full))
                        Warn($"{owner}: file '{path}' not found under storage root '{storageRoot}'.");
                }
            }

            // Capability checks (catalog), mirroring the server's InvalidVoiceProfile rules.
            if (caps.HasValue && !string.IsNullOrEmpty(data.TtsModelName))
            {
                var c = caps.Value;
                string model = $"'{data.TtsModelName}'";
                if (c.NumSpeakers > 0 && data.SpeakerId >= c.NumSpeakers)
                    Err($"Speaker Id {data.SpeakerId} is out of range; {model} has {c.NumSpeakers} speaker(s).");
                if (!c.Speed && !Mathf.Approximately(data.Speed, 1f))
                    Err($"{model} has no speed control; Speed must stay 1.0.");
                if (!c.ReferenceVoice && !string.IsNullOrEmpty(data.ReferenceVoice))
                    Err($"{model} cannot clone a voice; clear Reference Voice.");
                if (!c.VoiceDescription && !string.IsNullOrEmpty(data.VoiceDescription))
                    Err($"{model} does not take a voice description; clear Voice Description.");
                if (!c.LanguagePerCall && !string.IsNullOrEmpty(data.Lang))
                    Err($"{model} has a fixed language; clear Lang.");
                foreach (var m in modes)
                {
                    if (m == null) continue;
                    string mw = $"mode '{m.Name}'";
                    if (!c.ReferenceVoice && !string.IsNullOrEmpty(m.ReferenceVoice))
                        Err($"{mw}: {model} cannot clone a voice; clear Reference Voice.");
                    if (!c.DeliveryInstruction && !string.IsNullOrEmpty(m.Instruction))
                        Err($"{mw}: {model} does not take a delivery instruction; clear Instruction.");
                    if (!c.Speed && !Mathf.Approximately(m.SpeedScale, 1f))
                        Err($"{mw}: {model} has no speed control; Speed Scale must stay 1.0.");
                    if (!c.Expressiveness && m.Expressiveness >= 0f)
                        Err($"{mw}: {model} has no expressiveness control; set Expressiveness to -1.");
                }
            }
            return issues;
        }

        /// <summary>Server text rules for descriptions and instructions.</summary>
        private static void CheckVoiceText(string text, string field, Action<string> err)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (Encoding.UTF8.GetByteCount(text) > 1024)
                err($"{field} is longer than 1024 bytes.");
            foreach (char ch in text)
            {
                if (ch == '"' || ch == '<' || ch == '>')
                {
                    err($"{field} must not contain '\"', '<' or '>'.");
                    return;
                }
                if (char.IsControl(ch) && ch != '\t' && ch != '\r' && ch != '\n')
                {
                    err($"{field} must not contain control characters (tab and line breaks are allowed).");
                    return;
                }
            }
        }

        internal static string ResolveStorageRoot()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TryllRuntimeSettings>(TryllRuntimeSettings.EditorAssetPath)
                           ?? TryllRuntimeSettings.Load();
            string configured = settings != null ? settings.StorageDataFolder : string.Empty;
            return TryllRuntimeSettings.ResolveStorageRootForRuntime(configured) ?? string.Empty;
        }

        /// <summary>
        /// Every non-empty <see cref="TryllFilePickerAttribute"/> string under
        /// <paramref name="obj"/>: its own fields, nested tables and the elements of
        /// arrays and lists, with a readable path such as <c>Adaptation.Loras[0].Path</c>.
        /// </summary>
        internal static IEnumerable<(string Path, FieldInfo Field, string Value)> FilePickerValues(
            object obj, string prefix, int depth)
        {
            if (obj == null || depth > 6)
                yield break;

            foreach (var field in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object value = field.GetValue(obj);
                if (value == null)
                    continue;

                if (field.FieldType == typeof(string))
                {
                    if (field.GetCustomAttribute<TryllFilePickerAttribute>() != null
                        && !string.IsNullOrEmpty((string)value))
                        yield return (prefix + field.Name, field, (string)value);
                    continue;
                }

                if (value is System.Collections.IList list)
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (!IsNestedParamsType(list[i]?.GetType()))
                            continue;
                        foreach (var item in FilePickerValues(list[i], $"{prefix}{field.Name}[{i}].", depth + 1))
                            yield return item;
                    }
                    continue;
                }

                if (IsNestedParamsType(field.FieldType))
                {
                    foreach (var item in FilePickerValues(value, $"{prefix}{field.Name}.", depth + 1))
                        yield return item;
                }
            }
        }

        /// <summary>A generated nested params table (plain serializable class in Tryll.Client).</summary>
        private static bool IsNestedParamsType(Type type)
            => type != null
               && type.IsClass
               && type != typeof(string)
               && !typeof(UnityEngine.Object).IsAssignableFrom(type)
               && type.IsDefined(typeof(SerializableAttribute), false)
               && type.Namespace == typeof(TryllNodeParamsBase).Namespace;

        private static bool IsAbsoluteStoragePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path[0] == '/' || path[0] == '\\') return true;
            if (path.Length >= 2 && path[1] == ':') return true;
            return false;
        }
    }

    /// <summary>
    /// Context-menu extension for <see cref="TryllWorkflowAsset"/> in the Project window.
    /// Accessible via right-click → Validate Workflow on any workflow asset.
    /// </summary>
    internal static class TryllWorkflowAssetMenuExtension
    {
        [MenuItem("Assets/Tryll/Validate Workflow")]
        private static void ValidateSelected()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is TryllWorkflowAsset asset)
                    RunAndReport(asset.Graph, asset.Variables, asset.name);
                else if (obj is TryllVoiceProfile voice)
                    Report(TryllWorkflowValidator.ValidateVoiceProfile(voice), $"Voice profile '{voice.name}'");
            }
        }

        [MenuItem("Assets/Tryll/Validate Workflow", validate = true)]
        private static bool ValidateSelectedEnabled()
        {
            foreach (var obj in Selection.objects)
                if (obj is TryllWorkflowAsset || obj is TryllVoiceProfile) return true;
            return false;
        }

        private static void Report(List<TryllWorkflowValidator.Issue> issues, string what)
        {
            if (issues.Count == 0)
            {
                Debug.Log($"[Tryll] {what}: OK — no issues found.");
                return;
            }
            foreach (var issue in issues)
            {
                if (issue.Level == TryllWorkflowValidator.Issue.Severity.Error)
                    Debug.LogError("[Tryll] " + issue.Message);
                else
                    Debug.LogWarning("[Tryll] " + issue.Message);
            }
        }

        private static void RunAndReport(TryllGraphDescription graph, List<TryllVariableDecl> declaredVariables, string assetName)
        {
            var issues = TryllWorkflowValidator.Validate(graph, declaredVariables);
            if (issues.Count == 0)
            {
                Debug.Log($"[Tryll] Workflow '{assetName}': OK — no issues found.");
                return;
            }

            foreach (var issue in issues)
            {
                string msg = $"[Tryll] Workflow '{assetName}': {issue.Message}";
                if (issue.Level == TryllWorkflowValidator.Issue.Severity.Error)
                    Debug.LogError(msg);
                else
                    Debug.LogWarning(msg);
            }
        }
    }
}
