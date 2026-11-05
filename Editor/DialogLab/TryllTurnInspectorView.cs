// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
// Palette + pure chrome primitives. Imported statically so call sites read
// `Dim` / `Chip(...)` / `Section(...)` unchanged from when they lived on the window.
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Shared per-turn diagnostics panel used by Dialog Lab and Chat. Driven by one
    /// <see cref="Render"/> call carrying a neutral <see cref="TryllTurnInspection"/> —
    /// Lab pins/checks/diff arrive only via optional <see cref="TryllTurnLabContext"/>.
    /// </summary>
    internal sealed class TryllTurnInspectorView
    {
        /// <summary>Below this the dragged panel is an unreadable sliver. The window's
        /// split view needs the same number, so it lives here with the panel.</summary>
        public const float MinWidth = TryllEditorUiContract.InspectorMinWidth;

        private VisualElement _inspector;
        private VisualElement _inspTop;
        private ScrollView _inspScroll;
        private VisualElement _pinRow;
        private Label _inspTitle;
        private Label _inspHint;

        private TryllTurnInspection _turn;

        /// <summary>
        /// Index into the turn's <c>nodes[]</c> that every block below Route is narrowed to,
        /// or -1 for the whole turn. Set by clicking a Route chip.
        /// </summary>
        private int _nodeScope = -1;

        /// <summary>The turn key <see cref="_nodeScope"/> was chosen against.</summary>
        private string _scopeKey;

        /// <summary>The panel's root. Built once; the host parents it into the split view.</summary>
        public VisualElement Root => _inspector ?? BuildInspector();

        /// <summary>
        /// Draw one turn. Pass null for "no selection / no run yet" — the panel renders its
        /// own placeholder. Lab chrome (pins, checks, diff) appears only when
        /// <see cref="TryllTurnInspection.Lab"/> is non-null and populated.
        /// </summary>
        public void Render(TryllTurnInspection turn)
        {
            if (_inspector == null)
                BuildInspector();
            _turn = turn;
            string key = turn?.TurnKey ?? "";
            if (_scopeKey != key) { _nodeScope = -1; _scopeKey = key; }
            RebuildInspector();
        }


        private VisualElement BuildInspector()
        {
            _inspector = new VisualElement();
            // Width is owned by the window's split view (drag to resize) — do not set it
            // here, or the two fight. minWidth is still ours: it stops the drag collapsing
            // the panel to an unreadable sliver.
            _inspector.style.minWidth = MinWidth;
            _inspector.style.flexDirection = FlexDirection.Column;
            _inspector.AddToClassList(HostPanelClass);
            _inspector.style.borderLeftWidth = 1; _inspector.style.borderLeftColor = Line;

            _inspTop = new VisualElement();
            _inspTop.style.flexShrink = 0;
            _inspTop.AddToClassList(HostPanelClass);
            _inspTop.style.paddingLeft = 10; _inspTop.style.paddingRight = 10;
            _inspTop.style.paddingTop = 6; _inspTop.style.paddingBottom = 7;
            _inspTop.style.borderBottomWidth = 1; _inspTop.style.borderBottomColor = Line;

            var titleRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            _inspTitle = new Label("Turn diagnostics");
            _inspTitle.style.unityFontStyleAndWeight = FontStyle.Bold; _inspTitle.style.color = Txt;
            titleRow.Add(_inspTitle);
            _inspHint = new Label("click a cell");
            _inspHint.style.color = Faint; _inspHint.style.marginLeft = 8;
            titleRow.Add(_inspHint);
            _inspTop.Add(titleRow);

            _pinRow = new VisualElement();
            _pinRow.style.flexDirection = FlexDirection.Row; _pinRow.style.marginTop = 6;
            _inspTop.Add(_pinRow);

            _inspector.Add(_inspTop);

            _inspScroll = new ScrollView(ScrollViewMode.Vertical);
            _inspScroll.style.flexGrow = 1;
            // The inspector is a vertical panel by design: everything in it wraps. Pinning
            // the horizontal scroller off keeps one over-wide block (a long retrieval
            // chunk, a wide parameter value) from adding a horizontal scrollbar to the
            // whole panel. Blocks must still set minWidth:0 on any flexGrow child — this
            // is the backstop, not the fix.
            _inspScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _inspector.Add(_inspScroll);
            return _inspector;
        }

        void RebindHostColors()
        {
            if (_inspector != null)
                _inspector.style.borderLeftColor = Line;
            if (_inspTop != null)
                _inspTop.style.borderBottomColor = Line;
            if (_inspTitle != null)
                _inspTitle.style.color = Txt;
            if (_inspHint != null)
                _inspHint.style.color = Faint;
        }

        private void RebuildInspector()
        {
            if (_inspScroll == null) return;
            RebindHostColors();
            _inspScroll.Clear();
            _pinRow.Clear();

            var lab = _turn?.Lab;
            bool showPins = lab != null;
            if (showPins)
            {
                _pinRow.style.display = DisplayStyle.Flex;
                _pinRow.Add(PinSlot("A", lab.PinA, Accent));
                _pinRow.Add(PinSlot("B", lab.PinB, ColWarn));
            }
            else
            {
                _pinRow.style.display = DisplayStyle.None;
            }

            if (_turn == null || string.IsNullOrEmpty(_turn.TurnKey))
            {
                _inspHint.text = _turn?.Hint ?? "no run yet";
                if (lab != null && lab.ShowLabEmptyPlaceholder)
                {
                    _inspScroll.Add(Placeholder(TryllEditorUiContract.InspectorEmptyLab));
                }
                else
                {
                    _inspScroll.Add(Placeholder(TryllEditorUiContract.InspectorEmptyChat));
                }
                return;
            }

            _inspHint.text = string.IsNullOrEmpty(_turn.Hint) ? _turn.TurnKey : _turn.Hint;

            // Unsupported / unusable schema: explain and offer raw escape hatch only.
            if (_turn.AbsenceReason == TryllDiagnosticsAbsenceReason.UnsupportedSchema)
            {
                _inspScroll.Add(Section("Diagnostics", "unsupported schema", true, b =>
                {
                    b.Add(Muted(TryllLabJsonRender.NoDiagnosticsNote(_turn.AbsenceReason)));
                }));
                if (hasRawPayload(_turn))
                {
                    AddRawJsonSection(_turn);
                }
                return;
            }

            // Route (G1) — and the panel's node filter. Clicking a chip narrows every block
            // below to that one node execution; clicking it again widens back to the turn.
            // One selection mechanism, living on the breadcrumb that already shows the road.
            _inspScroll.Add(Section("Route",
                ScopedRouteNode() != null ? $"{ScopedRouteNode().Name} only" : (_turn.RouteTrailing ?? ""), true, b =>
            {
                if (_turn.Route == null || _turn.Route.Length == 0) { b.Add(Muted("Turn was not executed.")); return; }
                var r = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, alignItems = Align.Center } };
                for (int i = 0; i < _turn.Route.Length; i++)
                {
                    if (i > 0)
                    {
                        var arrow = new Label("→");
                        arrow.style.color = Faint; arrow.style.marginLeft = 3; arrow.style.marginRight = 3;
                        r.Add(arrow);
                    }
                    var n = _turn.Route[i];
                    bool scoped = _nodeScope == i;
                    var nd = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                    nd.style.backgroundColor = scoped ? Panel : Panel2;
                    SetBorder(nd, scoped ? Accent : Line2, 1); SetRadius(nd, 3);
                    nd.style.paddingLeft = 6; nd.style.paddingRight = 6;
                    nd.tooltip = scoped
                        ? $"{n.Type} — click to show the whole turn again"
                        : $"{n.Type} — click to narrow the panel to this node";
                    var nl = new Label(n.Name);
                    nl.style.color = scoped ? Accent : Txt;
                    if (scoped) nl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    nd.Add(nl);
                    var ex = new Label($"[{n.Exit}]");
                    ex.style.fontSize = 10; ex.style.marginLeft = 4;
                    ex.style.color = n.Exit == "not_found" ? ColWarn : ColOk;
                    nd.Add(ex);

                    int index = i;   // captured per chip, not the loop variable's final value
                    nd.RegisterCallback<PointerDownEvent>(e =>
                    {
                        e.StopPropagation();   // the section header toggle sits above this
                        _nodeScope = _nodeScope == index ? -1 : index;
                        RebuildInspector();
                    });
                    r.Add(nd);
                }
                b.Add(r);
                b.Add(Muted(_nodeScope >= 0
                    ? "Showing this node only. Click it again for the whole turn."
                    : "Click a node to narrow the blocks below to it."));
            }));

            // Timing
            _inspScroll.Add(Section("Timing", $"{_turn.DurationMs / 1000f:0.000}s", true, b =>
            {
                if (_turn.Route == null || _turn.Route.Length == 0) { b.Add(Muted("—")); return; }
                b.Add(TimingBar(_turn.Route, 6f));
                var lg = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginTop = 6 } };
                foreach (var n in _turn.Route)
                {
                    var item = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginRight = 10 } };
                    item.Add(Swatch(NodeColor(n.Type)));
                    var l = new Label($"{n.Name} {n.Ms}ms"); l.style.color = Dim; item.Add(l);
                    lg.Add(item);
                }
                if (_turn.TtftMs > 0)
                {
                    var l = new Label($"TTFT {_turn.TtftMs}ms"); l.style.color = Dim; lg.Add(l);
                }
                b.Add(lg);
            }));

            // Diagnostics-backed blocks below. False for seeded turns (injected with no
            // graph run) and skipped turns — both ordinary, neither a failure.
            bool hasDiag = TryllTurnInspectionFactory.HasUsableDiagnostics(_turn);
            bool hasRaw = hasRawPayload(_turn);

            // Parameters (G3). Generic: dumps every node's diagnostics.parameters object
            // as-is, no per-node-type code, so it covers node types the inspector has no
            // dedicated block for.
            if (hasDiag)
            {
                _inspScroll.Add(Section("Parameters", "diagnostics.parameters", false, b =>
                {
                    bool any = false;
                    foreach (var (index, n) in ScopedNodes(_turn.DebugInfo))
                    {
                        var parameters = n.Get("diagnostics").Get("parameters");
                        if (parameters.Kind != TryllJsonKind.Object || parameters.AsObject().Count == 0) continue;
                        any = true;
                        var nameLabel = new Label(NodeLabel(index, n));
                        nameLabel.style.color = Txt; nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                        nameLabel.style.marginTop = 4;
                        nameLabel.tooltip = n.Get("_type").AsString("");
                        b.Add(nameLabel);
                        foreach (var kv in parameters.AsObject())
                        {
                            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 1 } };
                            var kl = new Label(kv.Key); kl.style.color = Dim; kl.style.width = 140; kl.style.flexShrink = 0;
                            row.Add(kl);
                            // An array value would render as the bare count "[n]" — useless for
                            // a ToolCall node's `tools` signatures, which is the whole point of
                            // that key. Stack the elements down the value column instead; the
                            // label already wraps, so long entries behave like any other value.
                            string valueText = kv.Value.Kind == TryllJsonKind.Array
                                ? string.Join("\n", kv.Value.AsArray().Select(TryllLabJsonRender.JsonScalarToString))
                                : TryllLabJsonRender.JsonScalarToString(kv.Value);
                            var vl = new Label(valueText);
                            vl.style.color = Faint; vl.style.flexGrow = 1; vl.style.whiteSpace = WhiteSpace.Normal; vl.style.fontSize = 11;
                            vl.style.minWidth = 0;   // see the Retrieval note: min-width:auto would force the panel wider
                            row.Add(vl);
                            b.Add(row);
                        }
                    }
                    if (!any)
                        b.Add(Muted(_nodeScope >= 0
                            ? "This node reports no parameters."
                            : TryllEditorUiContract.InspectorNoParameters));
                }));
            }

            // Checks — Lab only (R1.2 / R3.1): omitted entirely in chat.
            if (lab != null && lab.AuthoredChecks != null && lab.AuthoredChecks.Count > 0)
            {
                int failCount = lab.Failed?.Count ?? 0;
                _inspScroll.Add(Section("Checks",
                    failCount > 0 ? $"{failCount} failed" : "all passed", true, b =>
                {
                    foreach (var ch in lab.AuthoredChecks)
                    {
                        var failure = lab.Failed == null
                            ? null
                            : System.Linq.Enumerable.FirstOrDefault(lab.Failed, f => TryllDialogLabChecks.Matches(f, ch));
                        bool bad = failure != null;
                        string label = bad && !string.IsNullOrEmpty(failure.Label) ? failure.Label : ch.Label;

                        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
                        var nm = new Label(label);
                        nm.style.color = bad ? ColWarn : Dim;
                        nm.style.flexGrow = 1;
                        nm.style.minWidth = 0;
                        nm.style.whiteSpace = WhiteSpace.Normal;
                        row.Add(nm);
                        row.Add(bad
                            ? Chip("✗", ColWarn, WarnBg, WarnBorder)
                            : Chip("✓", ColOk, OkBg, OkBorder));
                        b.Add(row);
                    }
                }));
            }

            // Prompt & output (S1) — one subsection per prompt-bearing node execution, and
            // the A/B diff target. `hasDiag` is false for legitimate reasons, not just
            // failure: seeded turns are injected without running the graph, and skipped
            // turns never ran, so neither carries diagnostics.
            //
            // Both the display and the comparison are per node. Flattening the turn into one
            // role-tagged blob made two LLM nodes indistinguishable to a reader, and let the
            // line diff align one variant's classifier prompt against the other's generate
            // prompt — so the headline feature could be quietly wrong, not merely dense.
            var peer = lab?.DiffPeer;
            bool diffing = peer != null
                && !string.IsNullOrEmpty(lab?.PinA)
                && !string.IsNullOrEmpty(lab?.PinB);
            var mySegments = hasDiag
                ? ScopedSegments(TryllLabJsonRender.PromptSegments(_turn.DebugInfo))
                : new List<TryllLabPromptSegment>();
            string mine = hasDiag
                ? TryllLabJsonRender.RenderSegments(mySegments)
                : TryllLabJsonRender.NoDiagnosticsNote(_turn.AbsenceReason);

            // A pinned peer with no usable diagnostics cannot be diffed against.
            bool otherHasDiag = peer != null && TryllTurnInspectionFactory.HasUsableDiagnostics(peer);

            string scopedName = ScopedRouteNode()?.Name;

            _inspScroll.Add(Section("Prompt & output",
                otherHasDiag ? "diff vs peer"
                             : scopedName != null ? scopedName
                             : $"{mine.Length} chars",
                true, b =>
            {
                if (peer != null && !otherHasDiag)
                    b.Add(Muted(TryllEditorUiContract.InspectorNoDiffPeer + " "
                              + TryllLabJsonRender.NoDiagnosticsNote(peer.AbsenceReason)));

                if (otherHasDiag)
                {
                    var theirSegments = MatchingSegments(
                        TryllLabJsonRender.PromptSegments(peer.DebugInfo), mySegments);

                    foreach (var sd in TryllLabJsonRender.DiffSegments(mySegments, theirSegments))
                    {
                        b.Add(SegmentHeading(sd, "selected", "peer"));
                        b.Add(DiffBody(sd.Lines));
                    }
                    b.Add(Muted("+ green = only in selected   ·   − red = only in peer"));
                }
                else if (mySegments.Count == 0)
                {
                    // Narrowing to a node that runs no inference (Retrieve, Transform,
                    // Branch) is an ordinary thing to do — say that rather than implying the
                    // turn had no prompt at all.
                    b.Add(scopedName != null
                        ? (VisualElement)Muted($"{scopedName} runs no inference, so it has no prompt or output.")
                        : TextBody(mine));
                }
                else
                {
                    // Headings only when there is more than one producer to tell apart —
                    // with a single node they restate the Route block. Mirrors RenderSegments.
                    bool label = mySegments.Count > 1;
                    foreach (var seg in mySegments)
                    {
                        if (label) b.Add(SegmentHeading(seg, null));
                        b.Add(TextBody(TryllLabJsonRender.RenderSegmentBody(seg)));
                    }
                }
            },
            // Only when there is something worth pasting: with no diagnostics `mine` is the
            // explanatory note, and offering to copy that is a broken promise. When two
            // cells are pinned this copies the SELECTED cell's transcript, not the diff —
            // the diff is a reading aid, the transcript is the artefact. Narrowed to the
            // scoped node when one is selected, matching what is on screen.
            hasDiag ? (System.Func<string>)(() => mine) : null));

            // Retrieval (S2) — RetrieveNode output.results[] in server order
            // (Hybrid RRF rank). Null distance renders as "—".
            //
            // One subsection per RetrieveNode execution, not just the first one found by
            // type: a graph with two retrievers (or one re-entered) used to render the first
            // and silently drop the rest, which reads as "these are the hits" while being
            // a subset of them.
            var retrieveNodes = hasDiag
                ? ScopedNodes(_turn.DebugInfo)
                    .Where(x => x.Node.Get("_type").AsString("") == "RetrieveNode").ToList()
                : new List<(int Index, TryllJsonValue Node)>();
            if (retrieveNodes.Count > 0)
            {
                var parsed = retrieveNodes.Select(x =>
                {
                    var diag = x.Node.Get("diagnostics");
                    var parameters = diag.Get("parameters");
                    return new
                    {
                        x.Index,
                        x.Node,
                        Diag = diag,
                        Mode = parameters.Get("retrieval_mode").AsString("Dense"),
                        Threshold = parameters.Get("threshold").AsDouble(),
                        RrfK = parameters.Get("rrf_k").AsInt(60),
                        Hits = TryllDialogLabRetrieval.ParseHits(diag.Get("output").Get("results")),
                    };
                }).ToList();

                string trailing = parsed.Count == 1
                    ? TryllDialogLabRetrieval.FormatHeader(
                        parsed[0].Hits.Count, parsed[0].Mode, parsed[0].Threshold, parsed[0].RrfK)
                    : $"{parsed.Count} nodes · {parsed.Sum(p => p.Hits.Count)} hits";

                _inspScroll.Add(Section("Retrieval", trailing, true, b =>
                {
                    bool label = parsed.Count > 1;
                    foreach (var p in parsed)
                    {
                        if (label)
                        {
                            b.Add(NodeHeading(NodeLabel(p.Index, p.Node),
                                TryllDialogLabRetrieval.FormatHeader(
                                    p.Hits.Count, p.Mode, p.Threshold, p.RrfK)));
                        }
                        var query = p.Diag.Get("input").Get("query").AsString("");
                        if (!string.IsNullOrEmpty(query)) b.Add(Muted($"query: {query}"));
                        if (p.Hits.Count == 0)
                        {
                            b.Add(Muted(TryllDialogLabRetrieval.FormatEmptyMessage(p.Mode)));
                            continue;
                        }
                        foreach (var h in p.Hits)
                        {
                            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
                            row.style.borderBottomWidth = 1; row.style.borderBottomColor = Line;
                            row.style.paddingBottom = 2;
                            var dl = new Label(h.FormatDistance());
                            dl.style.color = Txt; dl.style.width = 60; dl.style.flexShrink = 0;
                            row.Add(dl);
                            var il = new Label(h.Id);
                            il.style.color = Dim; il.style.width = 88; il.style.flexShrink = 0;
                            row.Add(il);
                            var meta = TryllDialogLabRetrieval.FormatHitMeta(h);
                            var body = string.IsNullOrEmpty(meta) ? h.Text : meta + "\n" + h.Text;
                            var tl = new Label(body);
                            tl.style.color = Dim; tl.style.flexGrow = 1; tl.style.whiteSpace = WhiteSpace.Normal;
                            tl.style.fontSize = 11;
                            // Retrieved chunks run to ~1.9 KB. A flex item defaults to
                            // min-width:auto, i.e. it refuses to shrink below its content's
                            // intrinsic width — which pushed the row (and the whole inspector)
                            // wider than the panel and produced a horizontal scrollbar.
                            tl.style.minWidth = 0;
                            row.Add(tl);
                            b.Add(row);
                        }
                    }
                }));
            }

            // Intent (S3) — ClassifyIntent / ClassifyIntentLLM output.classification.
            // Every in-scope classifier execution, not the first match by type.
            var intentExecs = hasDiag
                ? TryllDialogLabIntent.ParseExecutions(_turn.DebugInfo, ScopedNodes(_turn.DebugInfo))
                : new List<TryllIntentExecution>();
            if (intentExecs.Count > 0)
            {
                _inspScroll.Add(Section("Intent",
                    TryllDialogLabIntent.FormatHeader(intentExecs), true, b =>
                {
                    foreach (var e in intentExecs)
                        b.Add(BuildIntentExecution(e));

                    var attached = TryllDialogLabIntent.AttachedIntents(_turn.DebugInfo);
                    if (attached.Count > 0)
                        b.Add(Muted("attached to dialog: " + string.Join(", ", attached)));
                }));
            }

            // Tool calls (S4) — what the ToolCall node emitted this turn, paired with the
            // results that came back.
            //
            // The call↔result join key (`call_id`) exists *only* on the
            // interaction.components[] entries — neither `output.tool_calls[]` nor the
            // top-level `tool_calls[]` carries one — so ToolCallRecord/ToolResultComponent
            // are the primary source. Those components are gated by
            // `include_interaction_in_diagnostics` (C++ default false; the dev
            // server-config enables it), so when they are absent we fall back to the
            // un-correlated per-node list and say why rather than rendering nothing.
            // Every ToolCallNode execution in scope, not just the first found by type: the
            // raw `arguments_json` lookup read one node, so with two tool nodes the
            // un-flattened original was missed and the card silently fell back to the
            // flattened `arg.*` form.
            var toolNodes = hasDiag
                ? ScopedNodes(_turn.DebugInfo)
                    .Where(x => x.Node.Get("_type").AsString("") == "ToolCallNode").ToList()
                : new List<(int Index, TryllJsonValue Node)>();
            if (toolNodes.Count > 0)
            {
                var nodeCalls = toolNodes
                    .SelectMany(x => x.Node.Get("diagnostics").Get("output").Get("tool_calls").AsArray())
                    .ToList();
                bool parseFallback = toolNodes.Any(x =>
                    x.Node.Get("diagnostics").Get("output").Get("parse_fallback").AsBool());

                var components = _turn.DebugInfo.Get("interaction").Get("components").AsArray();
                var records = components.Where(x => x.Get("_type").AsString("") == "ToolCallRecord").ToList();
                var toolResults = components.Where(x => x.Get("_type").AsString("") == "ToolResultComponent").ToList();

                int callCount = records.Count > 0 ? records.Count : nodeCalls.Count;
                string exit = string.Join(", ", toolNodes
                    .Select(x => x.Node.Get("exit_route").AsString(""))
                    .Where(s => !string.IsNullOrEmpty(s)));
                int turnToolNodes = _turn.Route.Count(n => n.Type == "ToolCallNode");

                _inspScroll.Add(Section("Tool calls",
                    callCount > 0 ? $"{callCount} · {exit}" : exit, true, b =>
                {
                    // Only when true — a false value on every ordinary turn is noise.
                    if (parseFallback)
                    {
                        b.Add(Chip("parse fallback", ColWarn, WarnBg, WarnBorder));
                        b.Add(Muted("The output could not be parsed as a tool call, so no call was recorded. "
                                  + "The raw output is kept as text, and is sent as the answer under "
                                  + "call_or_answer."));
                        foreach (var x in toolNodes)
                        {
                            var output = x.Node.Get("diagnostics").Get("output");
                            if (!output.Get("parse_fallback").AsBool())
                                continue;
                            string failure = output.Get("parse_failure").AsString("");
                            string error = output.Get("parse_error").AsString("");
                            if (failure.Length > 0)
                                b.Add(Muted(error.Length > 0 ? $"{failure}: {error}" : failure));
                        }
                    }

                    // interaction.components[] carries no node linkage, so the records below
                    // cannot be attributed to one execution when the turn ran several.
                    if (turnToolNodes > 1)
                        b.Add(Muted($"This turn ran {turnToolNodes} ToolCall nodes. Results come from the "
                                  + "turn's interaction components, which carry no node linkage, so they "
                                  + "are listed for the whole turn."));

                    if (callCount == 0)
                    {
                        b.Add(Muted("No tool call parsed this turn."));
                        return;
                    }

                    if (records.Count == 0)
                    {
                        b.Add(Muted("Results unavailable — interaction diagnostics are off "
                                  + "(include_interaction_in_diagnostics in server-config.json), so "
                                  + "calls cannot be paired with their results."));
                        foreach (var call in nodeCalls)
                            b.Add(ToolCallCard(call.Get("name").AsString(""),
                                               call.Get("arguments_json").AsString(""), null));
                        return;
                    }

                    foreach (var rec in records)
                    {
                        string callId = rec.Get("call_id").AsString("");
                        string name = rec.Get("tool_name").AsString("");
                        // Prefer the node's raw arguments_json — it is the un-flattened
                        // original. The record's own args arrive as flattened `arg.*`
                        // string keys, which is the fallback.
                        string args = RawArgsForTool(nodeCalls, name) ?? FlattenedArgsOf(rec);
                        var result = toolResults.FirstOrDefault(r => r.Get("call_id").AsString("") == callId);
                        b.Add(ToolCallCard(name, args, result?.Get("text").AsString("")));
                    }
                }));
            }

            // Errors
            if (_turn.Status == TryllTurnTerminalStatus.Error
                || _turn.Status == TryllTurnTerminalStatus.Skipped)
            {
                _inspScroll.Add(Section("Errors", _turn.Status.ToString(), true, b =>
                {
                    var l = new Label(_turn.ErrorText ?? "");
                    l.style.whiteSpace = WhiteSpace.Normal;
                    l.style.color = _turn.Status == TryllTurnTerminalStatus.Error ? ColErr : Faint;
                    b.Add(l);
                }));
            }

            // Raw JSON (G4) — escape hatch only.
            if (hasRaw)
            {
                AddRawJsonSection(_turn);
            }
        }

        private static bool hasRawPayload(TryllTurnInspection turn) =>
            turn != null
            && (!string.IsNullOrEmpty(turn.RawDiagnosticsText)
                || (turn.DebugInfo != null && !turn.DebugInfo.IsNull));

        private void AddRawJsonSection(TryllTurnInspection turn)
        {
            var debugInfo = turn.DebugInfo ?? TryllJsonValue.Null;
            int keyCount = debugInfo.Kind == TryllJsonKind.Object ? debugInfo.AsObject().Count : 0;
            bool malformed = debugInfo.IsNull && !string.IsNullOrEmpty(turn.RawDiagnosticsText);
            string raw = !debugInfo.IsNull
                ? TryllLabJsonRender.PrettyPrintJson(debugInfo, 0)
                : (turn.RawDiagnosticsText ?? "");
            string trailing = malformed ? "malformed payload" : $"{keyCount} top-level keys";
            _inspScroll.Add(Section("Raw JSON", trailing, false, b =>
            {
                b.Add(Muted("Escape hatch. Everything above is a purpose-built view over this."));
                var host = new VisualElement();
                host.AddToClassList(HostInputClass);
                SetBorder(host, Line, 1); SetRadius(host, 3);
                host.style.paddingLeft = 7; host.style.paddingRight = 7;
                host.style.paddingTop = 5; host.style.paddingBottom = 5;
                // Label text is assigned directly: malformed payloads remain inert text and
                // cannot be interpreted as UXML/rich-text markup.
                var l = new Label(raw) { enableRichText = false };
                l.style.whiteSpace = WhiteSpace.Normal; l.style.fontSize = 11; l.style.color = Dim;
                host.Add(l);
                b.Add(host);
            },
            () => raw));
        }


        // ── Node scope ──────────────────────────────────────────────────────────

        /// <summary>The route entry the panel is narrowed to, or null for the whole turn.
        /// Bounds-checked: a stale scope index cannot survive a cell change (Render resets
        /// it), but a route shorter than the index must not throw either way.</summary>
        private TryllTurnRouteNode ScopedRouteNode() =>
            _nodeScope >= 0 && _turn?.Route != null && _nodeScope < _turn.Route.Length
                ? _turn.Route[_nodeScope]
                : null;

        /// <summary>
        /// The turn's node records in scope — every one, or just the selected execution —
        /// each paired with its index in <c>nodes[]</c>. That index is the identity the rest
        /// of the panel keys on, because a node re-entered within a turn appears once per
        /// execution and its name therefore is not unique.
        /// </summary>
        private List<(int Index, TryllJsonValue Node)> ScopedNodes(TryllJsonValue debugInfo)
        {
            var all = debugInfo.Get("nodes").AsArray();
            var result = new List<(int, TryllJsonValue)>();
            for (int i = 0; i < all.Count; i++)
                if (_nodeScope < 0 || _nodeScope == i) result.Add((i, all[i]));
            return result;
        }

        /// <summary>A node's display name, index-prefixed so two executions of the same node
        /// are told apart.</summary>
        private static string NodeLabel(int index, TryllJsonValue node) =>
            $"#{index} {node.Get("name").AsString("")}";

        private List<TryllLabPromptSegment> ScopedSegments(List<TryllLabPromptSegment> all) =>
            _nodeScope < 0 ? all : all.Where(s => s.NodeIndex == _nodeScope).ToList();

        /// <summary>
        /// The pinned cell's segments to compare against. Unscoped, that is all of them.
        /// Scoped, it is the ones sharing the selected node's identity — filtering the other
        /// cell by *index* would be wrong, since the whole point of a variant comparison is
        /// that the two routes may differ. A scoped node absent from the other side then
        /// reports as present on one side only, which is the true finding.
        /// </summary>
        private List<TryllLabPromptSegment> MatchingSegments(
            List<TryllLabPromptSegment> theirs, List<TryllLabPromptSegment> mine)
        {
            if (_nodeScope < 0) return theirs;
            var keys = new HashSet<string>(mine.Select(s => s.PairKey));
            return theirs.Where(s => keys.Contains(s.PairKey)).ToList();
        }

        // ── Segment chrome ──────────────────────────────────────────────────────

        /// <summary>A subsection heading naming the node that produced what follows.</summary>
        private VisualElement SegmentHeading(TryllLabPromptSegment seg, string badge)
        {
            var head = NodeHeading(
                $"#{seg.NodeIndex} {seg.NodeName}",
                string.IsNullOrEmpty(seg.ExitRoute) ? null : $"[{seg.ExitRoute}]");
            head.tooltip = seg.NodeType;
            if (!string.IsNullOrEmpty(badge))
                head.Add(Chip(badge, ColWarn, WarnBg, WarnBorder));
            return head;
        }

        /// <summary>A diff subsection's heading. An unpaired segment is labelled with the
        /// sample it is unique to — a node that ran in one variant and not the other is
        /// usually the answer to why their outputs differ.</summary>
        private VisualElement SegmentHeading(TryllLabSegmentDiff sd, string mineId, string theirsId)
        {
            string badge =
                sd.Pairing == TryllLabSegmentPairing.OnlyMine ? $"only in {mineId}"
                : sd.Pairing == TryllLabSegmentPairing.OnlyTheirs ? $"only in {theirsId}"
                : null;
            return SegmentHeading(sd.Either, badge);
        }

        private VisualElement NodeHeading(string label, string trailing)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            row.style.marginTop = 6; row.style.marginBottom = 2;
            var l = new Label(label);
            l.style.color = Txt; l.style.unityFontStyleAndWeight = FontStyle.Bold; l.style.fontSize = 11;
            row.Add(l);
            if (!string.IsNullOrEmpty(trailing))
            {
                var t = new Label(trailing);
                t.style.color = Faint; t.style.fontSize = 10; t.style.marginLeft = 5;
                row.Add(t);
            }
            return row;
        }

        private VisualElement BuildIntentExecution(TryllIntentExecution e)
        {
            var box = new VisualElement { style = { marginBottom = 6 } };

            var head = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            head.style.marginTop = 6; head.style.marginBottom = 2;
            var name = new Label($"#{e.NodeIndex} {e.NodeName}");
            name.style.color = Txt; name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.fontSize = 11; name.style.minWidth = 0;
            head.Add(name);
            bool found = e.Accepted;
            var chip = Chip(found ? "found" : "not_found",
                found ? ColOk : ColWarn,
                found ? OkBg : WarnBg,
                found ? OkBorder : WarnBorder);
            chip.style.marginLeft = 6;
            head.Add(chip);
            box.Add(head);

            var classification = /* diagnostics may be missing entirely */
                e.Kind == TryllIntentKind.Embedding
                    ? TryllDialogLabIntent.FormatEmbedSubHeader(e)
                    : TryllDialogLabIntent.FormatLlmSubHeader(e);
            box.Add(Muted(classification));

            if (e.Kind == TryllIntentKind.Embedding)
                BuildEmbedIntentBody(box, e);
            else
                BuildLlmIntentBody(box, e);
            return box;
        }

        private void BuildEmbedIntentBody(VisualElement box, TryllIntentExecution e)
        {
            if (e.Accepted)
            {
                var intent = new Label(string.IsNullOrEmpty(e.Intent) ? "—" : e.Intent);
                intent.style.color = Txt; intent.style.unityFontStyleAndWeight = FontStyle.Bold;
                intent.style.fontSize = 13; intent.style.minWidth = 0; intent.style.whiteSpace = WhiteSpace.Normal;
                box.Add(intent);

                var meta = new Label(
                    "distance " + TryllDialogLabIntent.FormatDistance(e.Distance)
                    + (string.IsNullOrEmpty(e.RecordId) ? "" : " · id " + e.RecordId)
                    + (e.RecordIndex.HasValue ? " · index " + e.RecordIndex.Value : ""));
                meta.style.color = Dim; meta.style.fontSize = 11; meta.style.minWidth = 0;
                box.Add(meta);

                if (!string.IsNullOrEmpty(e.RecordText))
                {
                    var text = new Label(e.RecordText);
                    text.style.color = Dim; text.style.fontSize = 11;
                    text.style.whiteSpace = WhiteSpace.Normal; text.style.minWidth = 0;
                    box.Add(text);
                }

                if (!string.IsNullOrEmpty(e.RunnerUpIntent))
                {
                    box.Add(Muted("runner-up " + e.RunnerUpIntent
                        + " @ " + TryllDialogLabIntent.FormatDistance(e.RunnerUpDistance)
                        + " (margin " + TryllDialogLabIntent.FormatDistance(e.Margin) + ")"));
                }
                else
                {
                    box.Add(Muted("every hit shared the winning label"));
                }
            }
            else
            {
                var reason = string.IsNullOrEmpty(e.NotFoundReason) ? "not_found" : e.NotFoundReason;
                var chipRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                chipRow.Add(Chip(reason, ColWarn, WarnBg, WarnBorder));
                box.Add(chipRow);

                var rejected = TryllDialogLabIntent.ResolveLabel(e);
                if (!string.IsNullOrEmpty(rejected))
                {
                    box.Add(Muted("rejected " + rejected
                        + " @ " + TryllDialogLabIntent.FormatDistance(e.Distance)
                        + (e.Threshold.HasValue
                            ? " (threshold " + e.Threshold.Value.ToString("0.00",
                                System.Globalization.CultureInfo.InvariantCulture) + ")"
                            : "")));
                }
            }

            if (e.Topk.Count == 0)
            {
                box.Add(Muted(TryllDialogLabIntent.FormatEmptyMessage(e)));
                return;
            }

            foreach (var hit in e.Topk)
            {
                bool beyond = e.Threshold.HasValue && hit.Distance.HasValue
                    && hit.Distance.Value > e.Threshold.Value;
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
                row.style.borderBottomWidth = 1; row.style.borderBottomColor = Line;
                row.style.paddingBottom = 2;
                var rank = new Label("#" + hit.Rank);
                rank.style.width = 28; rank.style.flexShrink = 0;
                rank.style.color = beyond ? Faint : Dim;
                row.Add(rank);
                var dl = new Label(TryllDialogLabIntent.FormatDistance(hit.Distance));
                dl.style.color = beyond ? Faint : Txt; dl.style.width = 60; dl.style.flexShrink = 0;
                row.Add(dl);
                var il = new Label(hit.Intent ?? "");
                il.style.color = beyond ? Faint : Dim; il.style.flexGrow = 1;
                il.style.minWidth = 0; il.style.whiteSpace = WhiteSpace.Normal;
                row.Add(il);
                box.Add(row);
            }
        }

        private void BuildLlmIntentBody(VisualElement box, TryllIntentExecution e)
        {
            var shown = e.Accepted ? e.Intent : TryllDialogLabIntent.ResolveLabel(e);
            if (!string.IsNullOrEmpty(shown))
            {
                var intent = new Label(shown);
                intent.style.color = Txt; intent.style.unityFontStyleAndWeight = FontStyle.Bold;
                intent.style.fontSize = 13; intent.style.minWidth = 0;
                box.Add(intent);
            }

            var strip = new Label(
                "letter " + (string.IsNullOrEmpty(e.PredictedLetter) ? "—" : e.PredictedLetter)
                + " · top " + TryllDialogLabIntent.FormatProb(e.TopProb)
                + " · second " + TryllDialogLabIntent.FormatProb(e.SecondProb)
                + " · margin " + TryllDialogLabIntent.FormatProb(e.MarginResult));
            strip.style.color = Dim; strip.style.fontSize = 11; strip.style.minWidth = 0;
            box.Add(strip);

            if (e.ProbsLengthMismatch)
                box.Add(ChipWarnRow(TryllDialogLabIntent.FormatMismatchWarning(e)));

            if (e.Probs.Count == 0)
            {
                if (!string.IsNullOrEmpty(e.NotFoundReason))
                    box.Add(ChipWarnRow(e.NotFoundReason));
                return;
            }

            double max = 0;
            foreach (var p in e.Probs)
                if (p.Prob > max) max = p.Prob;
            if (max <= 0) max = 1;

            for (int i = 0; i < e.Probs.Count; i++)
            {
                var p = e.Probs[i];
                box.Add(IntentProbBar(p.Label, p.Prob, max, e.Threshold));
                if (i == 0 && e.MarginResult.HasValue)
                    box.Add(Muted("margin " + TryllDialogLabIntent.FormatProb(e.MarginResult)));
            }
        }

        private static VisualElement ChipWarnRow(string text)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            row.Add(Chip(text, ColWarn, WarnBg, WarnBorder));
            return row;
        }

        private static VisualElement IntentProbBar(string label, double prob, double max, double? threshold)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
            var name = new Label(string.IsNullOrEmpty(label) ? "—" : label);
            name.style.width = 88; name.style.flexShrink = 0; name.style.color = Dim;
            name.style.overflow = Overflow.Hidden; name.style.minWidth = 0;
            row.Add(name);

            var track = new VisualElement();
            track.style.flexGrow = 1; track.style.height = 8; track.style.minWidth = 40;
            track.style.backgroundColor = TrackBg;
            SetRadius(track, 2); track.style.overflow = Overflow.Hidden;
            track.style.flexDirection = FlexDirection.Row;
            track.style.position = Position.Relative;

            float frac = max > 0 ? (float)(prob / max) : 0f;
            var fill = new VisualElement { style = { flexGrow = frac <= 0 ? 0.0001f : frac } };
            fill.style.backgroundColor = Accent;
            track.Add(fill);
            var rest = new VisualElement { style = { flexGrow = frac >= 1 ? 0.0001f : (1f - frac) } };
            track.Add(rest);

            if (threshold.HasValue && max > 0)
            {
                double t = threshold.Value / max;
                if (t < 0.0) t = 0.0;
                if (t > 1.0) t = 1.0;
                var marker = new VisualElement();
                marker.style.position = Position.Absolute;
                marker.style.left = Length.Percent((float)(t * 100.0));
                marker.style.width = 2; marker.style.top = 0; marker.style.bottom = 0;
                marker.style.marginLeft = -1;
                marker.style.backgroundColor = ColWarn;
                track.Add(marker);
                row.tooltip = "threshold " + threshold.Value.ToString("0.00",
                    System.Globalization.CultureInfo.InvariantCulture);
            }
            row.Add(track);

            var num = new Label(TryllDialogLabIntent.FormatProb(prob));
            num.style.width = 42; num.style.flexShrink = 0; num.style.marginLeft = 6;
            num.style.color = Txt; num.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(num);
            return row;
        }

        /// <summary>The bordered, height-capped host every transcript body sits in.</summary>
        private static ScrollView BodyHost()
        {
            var host = new ScrollView(ScrollViewMode.Vertical);
            host.style.maxHeight = 240;
            host.AddToClassList(HostInputClass);
            SetBorder(host, Line, 1); SetRadius(host, 3);
            host.style.paddingLeft = 7; host.style.paddingRight = 7;
            host.style.paddingTop = 5; host.style.paddingBottom = 5;
            return host;
        }

        private static VisualElement TextBody(string text)
        {
            var host = BodyHost();
            var l = new Label(text);
            l.style.whiteSpace = WhiteSpace.Normal; l.style.fontSize = 11;
            l.style.minWidth = 0;
            l.style.color = Txt;
            host.Add(l);
            return host;
        }

        private static VisualElement DiffBody(List<(TryllLabDiffOp Op, string Line)> lines)
        {
            var host = BodyHost();
            foreach (var (op, line) in lines)
            {
                string prefix = op == TryllLabDiffOp.Added ? "+ " : op == TryllLabDiffOp.Removed ? "− " : "  ";
                var l = new Label(prefix + (line.Length == 0 ? " " : line));
                l.style.whiteSpace = WhiteSpace.Normal; l.style.fontSize = 11;
                l.style.minWidth = 0;
                switch (op)
                {
                    case TryllLabDiffOp.Added:
                        l.style.color = DiffAdded;
                        l.style.backgroundColor = OkBg;
                        break;
                    case TryllLabDiffOp.Removed:
                        l.style.color = DiffRemoved;
                        l.style.backgroundColor = ErrBg;
                        break;
                    default:
                        l.style.color = Txt;
                        break;
                }
                host.Add(l);
            }
            return host;
        }

        // ── Tool-call rendering (S4) ────────────────────────────────────────────

        /// <summary>One call: tool name, arguments, and the result that came back.</summary>
        private VisualElement ToolCallCard(string toolName, string argsJson, string resultText)
        {
            var card = new VisualElement { style = { marginTop = 4, marginBottom = 2, paddingLeft = 8, paddingTop = 4, paddingBottom = 5 } };
            card.AddToClassList(HostInputClass);
            SetBorder(card, Line, 1); SetRadius(card, 3);

            var nameLabel = new Label(toolName);
            nameLabel.style.color = Txt; nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.Add(nameLabel);

            var args = new Label(TryllLabJsonRender.PrettyJsonOrRaw(argsJson));
            args.style.color = Dim; args.style.fontSize = 11;
            args.style.whiteSpace = WhiteSpace.Normal; args.style.minWidth = 0;
            card.Add(args);

            if (string.IsNullOrEmpty(resultText))
            {
                // Normal for RouteOnly / Notify / bare Pause — those dispositions write no
                // result component at all, so this is "nothing to show", not "missing".
                card.Add(Muted("no result recorded for this call"));
            }
            else
            {
                var res = new Label("→ " + resultText);
                res.style.color = ColOk; res.style.fontSize = 11; res.style.marginTop = 2;
                res.style.whiteSpace = WhiteSpace.Normal; res.style.minWidth = 0;
                card.Add(res);
            }
            return card;
        }

        /// <summary>The un-flattened <c>arguments_json</c> the model emitted, matched by
        /// tool name. Null when the node list has no entry for it.</summary>
        private static string RawArgsForTool(IReadOnlyList<TryllJsonValue> nodeCalls, string toolName)
        {
            foreach (var call in nodeCalls)
                if (call.Get("name").AsString("") == toolName)
                {
                    var raw = call.Get("arguments_json").AsString("");
                    if (!string.IsNullOrEmpty(raw)) return raw;
                }
            return null;
        }

        /// <summary>Re-groups a ToolCallRecord's flattened <c>arg.&lt;name&gt;</c> keys back
        /// into a JSON object — components are string→string bags, unlike
        /// <c>nodes[].diagnostics</c>.</summary>
        private static string FlattenedArgsOf(TryllJsonValue record)
        {
            var parts = record.AsObject()
                .Where(kv => kv.Key.StartsWith("arg."))
                .Select(kv => $"\"{kv.Key.Substring(4)}\": \"{kv.Value.AsString("")}\"")
                .ToList();
            return parts.Count == 0 ? "{}" : "{ " + string.Join(", ", parts) + " }";
        }

        private VisualElement PinSlot(string key, string value, Color accent)
        {
            bool filled = !string.IsNullOrEmpty(value);
            var s = new VisualElement();
            s.style.flexGrow = 1; s.style.flexBasis = 0; s.style.minWidth = 0;
            s.style.marginRight = key == "A" ? 6 : 0;
            SetBorder(s, filled ? accent : Line2, 1); SetRadius(s, 3);
            s.style.paddingLeft = 6; s.style.paddingRight = 4;
            s.style.paddingTop = 2; s.style.paddingBottom = 3;

            var head = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var k = new Label($"PIN {key}");
            k.style.fontSize = 10; k.style.unityFontStyleAndWeight = FontStyle.Bold;
            k.style.color = filled ? Txt : Faint;
            head.Add(k);
            head.Add(new VisualElement { style = { flexGrow = 1 } });
            if (filled)
            {
                var x = new Button(() => (_turn?.Lab?.OnClearPin)?.Invoke(key)) { text = "✕" };
                x.style.fontSize = 10;
                x.style.paddingLeft = 3; x.style.paddingRight = 3;
                x.style.marginLeft = 0; x.style.marginRight = 0;
                x.style.marginTop = 0; x.style.marginBottom = 0;
                head.Add(x);
            }
            s.Add(head);

            var v = new Label(filled ? $"column {value}" : "—");
            v.style.color = filled ? Txt : Faint;
            s.Add(v);
            return s;
        }

        /// <param name="copy">
        /// Optional header action: when non-null a small "copy" button sits at the right of
        /// the header and puts this function's result on the system clipboard. Deliberately
        /// on the *header* rather than in the body — the sections worth copying (Raw JSON,
        /// Prompt &amp; output) are the big ones, and requiring a reader to expand a
        /// 5,000-character blob just to reach a button is the friction this removes.
        /// A <c>Func</c> rather than a string so nothing is rendered until it is clicked.
        /// </param>
        private VisualElement Section(
            string title,
            string trailing,
            bool open,
            System.Action<VisualElement> build,
            System.Func<string> copy = null)
        {
            var wrap = new VisualElement();
            wrap.style.borderBottomWidth = 1; wrap.style.borderBottomColor = Line;

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
            head.style.backgroundColor = Panel2;
            head.style.paddingLeft = 10; head.style.paddingRight = 10;
            head.style.paddingTop = 4; head.style.paddingBottom = 4;

            var caret = new Label(open ? "▾" : "▸");
            caret.style.color = Faint; caret.style.width = 12; caret.style.flexShrink = 0;
            head.Add(caret);
            var t = new Label(title.ToUpperInvariant());
            t.style.fontSize = 11; t.style.unityFontStyleAndWeight = FontStyle.Bold; t.style.color = Dim;
            head.Add(t);
            head.Add(new VisualElement { style = { flexGrow = 1 } });
            if (!string.IsNullOrEmpty(trailing))
            {
                var tr = new Label(trailing);
                tr.style.color = Faint; tr.style.fontSize = 11;
                head.Add(tr);
            }
            if (copy != null) head.Add(CopyButton(copy));
            wrap.Add(head);

            var body = new VisualElement();
            body.style.paddingLeft = 10; body.style.paddingRight = 10;
            body.style.paddingTop = 6; body.style.paddingBottom = 8;
            body.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            build(body);
            wrap.Add(body);

            head.RegisterCallback<PointerDownEvent>(e =>
            {
                // The whole header toggles the section, so a header action would otherwise
                // collapse the panel out from under the click. Checking the target beats
                // StopPropagation on the button: it does not depend on whether our callback
                // runs before or after Button's own Clickable manipulator.
                if (e.target is Button) return;
                bool nowOpen = body.style.display == DisplayStyle.None;
                body.style.display = nowOpen ? DisplayStyle.Flex : DisplayStyle.None;
                caret.text = nowOpen ? "▾" : "▸";
            });
            return wrap;
        }

        private const string CopyLabel = "copy";
        private const string CopiedLabel = "copied";

        /// <summary>
        /// The header's copy action. A word rather than a glyph on purpose: the clipboard
        /// glyphs (⧉, ⎘) are not in Unity's default editor font on every platform, and a
        /// missing-glyph box in a header is worse than four lowercase letters. Fixed width
        /// so swapping to "copied" cannot reflow the header.
        /// </summary>
        private static Button CopyButton(System.Func<string> copy)
        {
            var btn = new Button { text = CopyLabel, tooltip = "Copy to clipboard" };
            btn.style.width = 48; btn.style.height = 15; btn.style.minHeight = 0;
            btn.style.flexShrink = 0; btn.style.fontSize = 10;
            btn.style.marginTop = 0; btn.style.marginBottom = 0;
            btn.style.marginLeft = 8; btn.style.marginRight = 0;
            btn.style.paddingLeft = 0; btn.style.paddingRight = 0;
            btn.style.paddingTop = 0; btn.style.paddingBottom = 0;
            btn.clicked += () =>
            {
                EditorGUIUtility.systemCopyBuffer = copy() ?? "";
                // A silent clipboard write is indistinguishable from a dead button.
                btn.text = CopiedLabel;
                btn.schedule.Execute(() => btn.text = CopyLabel).StartingIn(900);
            };
            return btn;
        }
    }
}
