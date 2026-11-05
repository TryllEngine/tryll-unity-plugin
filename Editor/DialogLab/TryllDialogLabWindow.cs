// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
// Palette + chrome primitives live on TryllEditorTheme. Imported
// statically so call sites read `Dim` / `Chip(...)` as before.
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Tryll Dialog Lab — runs one dialog script across N samples and shows the
    /// spread, so an author can see *variance* rather than one lucky answer.
    ///
    /// See docs/research/clients/dialog-lab-requirements.md for the full spec and
    /// docs/research/qa/workflow-iteration-and-scripted-dialogue-ux.md for why this
    /// exists at all.
    ///
    /// Deliberately a separate window from <see cref="TryllChatWindow"/>: that
    /// one is single-agent with a text/voice input as its centre of gravity;
    /// this one is N agents × N histories with no primary input at all. The
    /// connection lifecycle is the part worth sharing once this goes live.
    ///
    /// Two view modes:
    ///   Grid       — turn-banded, every sample side by side (default)
    ///   Statistics — per-variant pass rates with seed-based intervals, and where the
    ///                failures are (replaced the per-turn Summary)
    ///
    /// UI Toolkit has no `position: sticky`, so the "user band pinned to the
    /// viewport while columns scroll" behaviour is built by keeping bands
    /// *outside* the horizontally scrolling rows and syncing every row's
    /// horizontal scroll offset to a single shared value.
    /// </summary>
    public sealed class TryllDialogLabWindow : EditorWindow
    {
        /// <summary>Dropdown entry meaning "no override — use the workflow's own default".
        /// Parenthesised so it cannot collide with a real model name.</summary>
        internal const string ModelDefaultChoice = "(workflow default)";

        /// <summary>Values are persisted: 0 was Summary before Statistics replaced it.</summary>
        private enum ViewMode { Statistics = 0, Grid = 1 }

        /// <summary>What the main stage area shows. Variant authoring is a *workspace*,
        /// not a config field — with more than one or two variants the inline editor
        /// pushed the results off screen, so it gets the full stage and a toolbar switch
        /// of its own rather than living in the config box.</summary>
        private enum StageMode { Dialog, Variants }

        // ── Persisted view state ───────────────────────────────────────────────

        [SerializeField] private StageMode _stage   = StageMode.Dialog;
        [SerializeField] private ViewMode _mode    = ViewMode.Grid;
        [SerializeField] private bool _statsDelta;
        [SerializeField] private bool _statsIntervals = true;
        /// <summary>Statistics dialogue filter: -1 = every dialogue.</summary>
        private int _statsDialogue = -1;
        [SerializeField] private int      _density = 1;   // 0 compact · 1 comfortable · 2 full
        [SerializeField] private float    _cellW   = TryllEditorUiContract.CellWidthMedium;

        // ── Config ──────────────────────────────────────────────────────────────

        // Inspector pane geometry. Persisted so a dragged width survives domain reload.
        private const float MinStageWidth     = TryllEditorUiContract.StageMinWidth;
        [SerializeField] private float _inspectorWidth   = TryllEditorUiContract.InspectorDefaultWidth;
        [SerializeField] private bool  _inspectorVisible = true;

        [SerializeField] private TryllRuntimeSettings _labSettings;
        [SerializeField] private TryllAgentComponent  _sourceAgent;   // source of WorkflowAsset + VariableOverrides
        [SerializeField] private string _sourceAgentGlobalId;
        /// <summary>A <see cref="TryllDialogScriptAsset"/> or a <see cref="TryllDialogSuiteAsset"/>;
        /// <see cref="TryllDialogLabDialogues.TryResolve"/> turns it into the dialogues to run.</summary>
        [SerializeField, FormerlySerializedAs("_scriptAsset")] private ScriptableObject _dialogSource;

        // [SerializeField] and non-readonly so variants survive domain reload (R2.5) —
        // EditorWindow serialization cannot write back into a readonly field.
        [SerializeField] private TryllDialogLabExperiment _experiment = new TryllDialogLabExperiment();

        private TryllEditorSession _labSession;
        private TryllDialogLabRunner _runner;
        private string _runFailure;
        private string _runNotes;
        private int _runGeneration;
        private TryllDialogLabRunSnapshot _pendingSnapshot;
        /// <summary>
        /// Per-variant scene agent components, variant order. Deliberately NOT part of
        /// the snapshot: the snapshot is immutable, serialisable provenance, and a scene
        /// object is neither. The runner uses these only to reach live tool handlers.
        /// </summary>
        private List<TryllAgentComponent> _pendingLiveAgents;
        private Task _activeRunTask;
        private TryllDialogLabRunResult _completedRun;
        /// <summary>The run in flight, while its first dialogues are shown (Statistics reads it).</summary>
        private TryllDialogLabRunResult _partialRun;
        // Must rot with `_completedRun` / `_cells` / `_dataset` (td-069): Unity's
        // EditorWindow domain-reload backup restores plain bools, not those refs.
        [NonSerialized] private bool _resultsImported;

        // ── Run state ──────────────────────────────────────────────────────────

        [NonSerialized] private bool _ran;
        private bool _running;

        // Planned cost of the *next* run, derived from the current config (R4.3's
        // up-front estimate). Distinct from live progress, which comes from
        // TryllDialogLabRunner.Progress and is authoritative while a run is in flight.
        private int PlannedCells => _experiment.Variants.Count * Mathf.Max(1, _experiment.Repeats);
        private int PlannedTurns => PlannedCells * SourceScripts()
            .Sum(s => Mathf.Max(0, s.Steps.Count - s.SeedTurnCount));

        /// <summary>The dialogues the next run covers; empty when nothing usable is assigned.</summary>
        private List<TryllDialogScriptAsset> SourceScripts() =>
            TryllDialogLabDialogues.TryResolve(_dialogSource, out var scripts, out _)
                ? scripts
                : new List<TryllDialogScriptAsset>();

        private string PreRunMessage()
        {
            var missing = new List<string>();
            if (_labSettings == null) missing.Add("Tryll Settings");
            if (_sourceAgent == null) missing.Add("an Agent Component");
            if (_dialogSource == null) missing.Add("a Dialog Script or Suite");
            if (missing.Count > 0)
                return "Assign " + string.Join(", ", missing) + " to run.";
            int dialogues = SourceScripts().Count;
            string across = dialogues > 1 ? $" across {dialogues} dialogues" : "";
            return $"Press Run to execute {PlannedCells} cells ({PlannedTurns} generated turns{across}).";
        }

        // ── Selection ──────────────────────────────────────────────────────────

        private string _selectedKey;
        private string _pinA, _pinB;                       // sample ids, for the prompt diff
        /// <summary>Dialogue indices folded in the Grid. Window-local; a new run unfolds all.</summary>
        private readonly HashSet<int> _collapsedDialogues = new HashSet<int>();

        // ── Data ───────────────────────────────────────────────────────────────

        private Dictionary<string, LabCell> _cells;
        private TryllDialogLabDataset _dataset;

        // ── UI refs ────────────────────────────────────────────────────────────

        private VisualElement _headerHost, _progressRow;
        private ScrollView    _headerScroll, _contentScroll;
        private ProgressBar   _progress;
        private Label         _runStat;
        private TryllStatusBar _statusBar;
        private TryllBanner    _importBanner;

        /// <summary>The per-cell diagnostics panel. Owns its visual tree; the window
        /// owns only the split view, the collapse toggle and the remembered width.</summary>
        private TryllTurnInspectorView _inspectorView;
        private Button        _runButton, _inspToggle;
        private VisualElement _segStage, _segMode, _segDensity, _segWidth;
        private TryllInspectorSplit _bodySplit;

        private readonly List<ScrollView> _hScrollers = new List<ScrollView>();
        private bool    _syncing;
        private float   _hOffset;    // shared column offset, survives Rebuild()
        private Vector2 _vOffset;    // vertical scroll, survives Rebuild()

        // A Statistics → Grid jump: the turn and column JumpToTurn wants in view. The next
        // BuildGrid hands back the turn's band, which RestoreScroll scrolls to in place of
        // the saved offset. One-shot: consumed by that rebuild.
        private int           _jumpTurn = -1;
        private int           _jumpColumn = -1;
        private VisualElement _jumpTarget;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        [MenuItem("Window/Tryll/Dialog Lab")]
        public static void Open()
        {
            var w = GetWindow<TryllDialogLabWindow>("Tryll Dialog Lab");
            w.minSize = new Vector2(
                TryllEditorUiContract.PanelMinWidth,
                TryllEditorUiContract.PanelMinHeight);
        }

        /// <summary>
        /// Opens the Lab pre-seeded with <paramref name="sourceAgent"/> and the given
        /// chat history captured as a script's leading seeded steps (R10.2) — the
        /// "Compare in Lab…" entry point from <see cref="TryllChatWindow"/>. Appends one
        /// blank generated continuation step so Run has work to do; the user must fill
        /// its user line before running. The script is a transient, unsaved
        /// <see cref="TryllDialogScriptAsset"/> instance; nothing is written to disk
        /// unless the user explicitly saves it.
        /// </summary>
        public static void OpenFromChat(TryllAgentComponent sourceAgent, IReadOnlyList<(bool IsUser, string Text)> history)
        {
            var w = GetWindow<TryllDialogLabWindow>("Tryll Dialog Lab");
            w.minSize = new Vector2(
                TryllEditorUiContract.PanelMinWidth,
                TryllEditorUiContract.PanelMinHeight);

            var script = ScriptableObject.CreateInstance<TryllDialogScriptAsset>();
            TryllDialogLabCompareFromChat.ApplyToAsset(script, history);

            w._sourceAgent = sourceAgent;
            w._sourceAgentGlobalId = sourceAgent != null
                ? GlobalObjectId.GetGlobalObjectIdSlow(sourceAgent).ToString()
                : "";
            w._dialogSource = script;
            w.PersistLabConfig();
            w._ran = false;
            w._cells = null;
            w._dataset = null;
            w._completedRun = null;
            w._resultsImported = false;
            w._runFailure = null;
            w._runNotes = null;
            if (w._configFieldsHost != null) { w.RebuildConfigFields(); w.Rebuild(); }
        }

        /// <summary>
        /// Test-only: neither read nor write the Dialog Lab EditorPrefs, so a run
        /// is exactly what <see cref="DebugBindForTests"/> set and leaves no prefs.
        /// </summary>
        internal static bool DebugSkipPersistForTests { get; set; }

        /// <summary>Assign settings, default agent, script, variants and repeats as the fields would.</summary>
        internal void DebugBindForTests(
            TryllRuntimeSettings settings,
            TryllAgentComponent agent,
            ScriptableObject script,
            List<TryllDialogLabVariant> variants,
            int repeats)
        {
            _labSettings = settings;
            _sourceAgent = agent;
            _dialogSource = script;
            _experiment.Variants = variants;
            _experiment.Repeats = Mathf.Max(1, repeats);
            Rebuild();
        }

        /// <summary>Press Run as a click would.</summary>
        internal void DebugRunForTests() => OnRunClicked();

        internal bool DebugRunning => _running;
        internal TryllDialogLabRunResult DebugCompletedRun => _completedRun;

        /// <summary>
        /// Rows are "variant#repeat:dialogue:turn:status:answer" per completed turn; LocalState is
        /// Running / Completed / Failed / Idle; the banner is the run failure.
        /// </summary>
        internal TryllEditorWindowSnapshot DebugSnapshot()
        {
            var rows = new List<string>();
            if (_completedRun != null)
                foreach (var sample in _completedRun.Samples)
                    foreach (var turn in sample.Turns)
                        rows.Add($"{sample.VariantLabel}#{sample.Repeat}:{turn.DialogueIndex}:{turn.TurnIndex}:{turn.Status}:{turn.AnswerText}");
            string state = _running ? "Running"
                : _completedRun != null ? "Completed"
                : !string.IsNullOrEmpty(_runFailure) ? "Failed"
                : "Idle";
            return new TryllEditorWindowSnapshot(
                _labSession?.OwnerPhase ?? TryllOwnerPhase.Idle,
                state,
                _statusBar?.DebugCaption,
                _statusBar?.DebugActionText,
                _runFailure,
                rows,
                _labSession?.HoldsLease ?? false,
                _runGeneration);
        }

        private void OnEnable()
        {
            LoadLabConfig();
            TryllEditorTheme.HostColorsChanged += OnHostColorsChanged;
        }

        private void OnDisable()
        {
            TryllEditorTheme.HostColorsChanged -= OnHostColorsChanged;
            PersistLabConfig();
            _runGeneration++;
            _runner?.Stop();
            _running = false;
            _labSession?.Stop();
            _labSession = null;
            _pendingSnapshot = null;
            _activeRunTask = null;
        }

        void OnHostColorsChanged()
        {
            if (_contentScroll == null) return;
            Rebuild();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            TryllEditorTheme.Attach(root);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            root.Add(BuildConfig());
            root.Add(BuildToolbar());
            _importBanner = new TryllBanner();
            root.Add(_importBanner);
            root.Add(BuildProgressRow());

            var stage = new VisualElement();
            stage.style.flexGrow = 1; stage.style.flexDirection = FlexDirection.Column;

            _headerHost = new VisualElement();
            _headerHost.style.flexShrink = 0;
            stage.Add(_headerHost);

            _contentScroll = new ScrollView(ScrollViewMode.Vertical);
            _contentScroll.style.flexGrow = 1;
            stage.Add(_contentScroll);

            _bodySplit = new TryllInspectorSplit(
                stage,
                BuildInspectorPane(),
                TryllSplitFixedSide.Second,
                () => MinStageWidth,
                () => TryllTurnInspectorView.MinWidth,
                Mathf.Max(TryllTurnInspectorView.MinWidth, _inspectorWidth),
                () => _inspectorWidth,
                w => _inspectorWidth = w,
                () => _inspectorVisible);
            root.Add(_bodySplit);

            Rebuild();
        }

        // ── Config box ───────────────────────────────────────────────────────────

        private VisualElement BuildConfig()
        {
            var box = new VisualElement();
            box.style.flexShrink = 0;
            box.style.backgroundColor = Panel;
            box.style.paddingLeft = 10; box.style.paddingRight = 10;
            box.style.paddingTop = 7; box.style.paddingBottom = 5;
            box.style.borderBottomWidth = 1; box.style.borderBottomColor = Line;

            _configFieldsHost = new VisualElement();
            box.Add(_configFieldsHost);
            RebuildConfigFields();
            return box;
        }

        private VisualElement _configFieldsHost;
        /// <summary>Script-level warnings under the Dialog Script field; refilled by Rebuild.</summary>
        private VisualElement _scriptWarningsHost;

        private void RebuildConfigFields()
        {
            _configFieldsHost.Clear();

            var settingsField = new ObjectField("Tryll Settings") { objectType = typeof(TryllRuntimeSettings), value = _labSettings };
            settingsField.labelElement.style.minWidth = 118; settingsField.labelElement.style.color = Dim;
            settingsField.RegisterValueChangedCallback(e => { _labSettings = e.newValue as TryllRuntimeSettings; RefreshRunStat(); });
            _configFieldsHost.Add(settingsField);

            // "default": variants may override this per-variant (that is how structural
            // differences — model, context size, wiring — are compared).
            var agentField = new ObjectField("Agent (default)") { objectType = typeof(TryllAgentComponent), allowSceneObjects = true, value = _sourceAgent };
            agentField.labelElement.style.minWidth = 118; agentField.labelElement.style.color = Dim;
            // Rebuild the whole stage, not just the variant list: changing the agent
            // changes the graph every non-overriding variant's pickers are built against.
            agentField.RegisterValueChangedCallback(e =>
            {
                _sourceAgent = e.newValue as TryllAgentComponent;
                _sourceAgentGlobalId = _sourceAgent != null
                    ? GlobalObjectId.GetGlobalObjectIdSlow(_sourceAgent).ToString()
                    : "";
                PersistLabConfig();
                Rebuild();
            });
            _configFieldsHost.Add(agentField);

            // ObjectField takes one type, so it is typed to the common base and narrowed
            // here: a Dialog Script runs one dialogue, a Dialog Suite several.
            var scriptField = new ObjectField("Dialog Script / Suite") { objectType = typeof(ScriptableObject), value = _dialogSource };
            scriptField.tooltip = "A Dialog Script, or a Dialog Suite to run several dialogues in one run.";
            scriptField.labelElement.style.minWidth = 118; scriptField.labelElement.style.color = Dim;
            scriptField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != null && !TryllDialogLabDialogues.IsAcceptedSource(e.newValue))
                {
                    scriptField.SetValueWithoutNotify(_dialogSource);
                    _runFailure = $"'{e.newValue.name}' is a {e.newValue.GetType().Name}. Drop a Dialog Script or a Dialog Suite.";
                    Rebuild();
                    return;
                }
                _dialogSource = e.newValue as ScriptableObject;
                PersistLabConfig();
                Rebuild();
            });
            _configFieldsHost.Add(scriptField);
            _scriptWarningsHost = new VisualElement();
            _scriptWarningsHost.style.marginLeft = 122;
            _configFieldsHost.Add(_scriptWarningsHost);
            RefreshScriptWarnings();

            var repRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 3, marginBottom = 3 } };
            repRow.Add(FieldLabel("Repeats / base seed"));
            var repField = new IntegerField { value = _experiment.Repeats };
            repField.style.width = 60;
            repField.tooltip = "How many seeds per variant. Values below 1 are raised to 1.";
            repField.RegisterValueChangedCallback(e =>
            {
                var clamped = Mathf.Max(1, e.newValue);
                _experiment.Repeats = clamped;
                repField.SetValueWithoutNotify(clamped);
                PersistLabConfig();
            });
            repRow.Add(repField);
            var seedField = new IntegerField { value = _experiment.BaseSeed };
            seedField.style.width = 60; seedField.style.marginLeft = 8;
            // BaseSeed must never be 0 — SamplingParams.seed == 0 means "random seed",
            // which would silently defeat every reproducibility guarantee the Lab offers.
            seedField.tooltip =
                "Must be at least 1. Seed 0 means random on the wire; the Lab raises it to 1.";
            seedField.RegisterValueChangedCallback(e =>
            {
                var clamped = Mathf.Max(1, e.newValue);
                _experiment.BaseSeed = clamped;
                seedField.SetValueWithoutNotify(clamped);
                PersistLabConfig();
            });
            repRow.Add(seedField);
            _configFieldsHost.Add(repRow);

            // No count here — this box is only rebuilt on the Demo toggle, so a number
            // would go stale as variants are added. The toolbar tab carries the live one.
            var variantsHint = new Label("Variants — edit in the Variants tab.");
            variantsHint.style.color = Faint; variantsHint.style.fontSize = 10; variantsHint.style.marginTop = 2;
            _configFieldsHost.Add(variantsHint);
        }

        // ── Variants stage ───────────────────────────────────────────────────────

        /// <summary>The Variants tab. Full stage rather than a config field: at more
        /// than one or two variants the inline editor crowded out everything else.</summary>
        private void BuildVariantsStage()
        {
            var host = new VisualElement();
            host.style.paddingLeft = 10; host.style.paddingRight = 10;
            host.style.paddingTop = 8; host.style.paddingBottom = 24;

            var note = new Label(
                "A variant is one configuration to compare, run on its own fresh agent. Give it an "
                + "Agent to compare structural differences (a different model, context size, or "
                + "workflow); leave that empty to use the window's default agent and compare "
                + "parameter overrides instead. Structural fields are absent from the override "
                + "editor by design — ChangeParams rejects them (3006), so a different agent is how "
                + "you change them.");
            note.style.color = Faint; note.style.fontSize = 10; note.style.whiteSpace = WhiteSpace.Normal;
            note.style.marginBottom = 8;
            host.Add(note);

            foreach (var variant in _experiment.Variants.ToList())
            {
                var wrap = new VisualElement { style = { marginBottom = 8, paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6 } };
                wrap.style.backgroundColor = Panel;
                SetBorder(wrap, Line2, 1); SetRadius(wrap, 4);

                var head = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
                head.Add(Swatch(TryllEditorTheme.VariantSwatch(_experiment.Variants.IndexOf(variant))));
                var labelField = new TextField { value = variant.Label, style = { flexGrow = 1 } };
                labelField.RegisterValueChangedCallback(e =>
                {
                    variant.Label = e.newValue;
                    PersistLabConfig();
                });
                head.Add(labelField);
                if (_experiment.Variants.Count > 1)
                {
                    // Remove by reference, not by index: a `for` loop's variable is
                    // captured by reference in C#, so an index captured here would read
                    // its post-loop value (== Count) and throw on click.
                    var captured = variant;
                    var remove = new Button(() =>
                    {
                        _experiment.Variants.Remove(captured);
                        PersistLabConfig();
                        Rebuild();
                    })
                    { text = "✕" };
                    head.Add(remove);
                }
                wrap.Add(head);

                // Per-variant agent. Empty = the window default, which the placeholder
                // text names so the effective agent is always visible without guessing.
                var agentRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
                var agentField = new ObjectField("Agent")
                {
                    objectType = typeof(TryllAgentComponent),
                    allowSceneObjects = true,
                    value = variant.AgentOverride,
                };
                agentField.labelElement.style.minWidth = 46;
                agentField.labelElement.style.color = Dim;
                agentField.style.flexGrow = 1;
                agentField.style.minWidth = 0;
                var capturedForAgent = variant;
                agentField.RegisterValueChangedCallback(e =>
                {
                    capturedForAgent.AgentOverride = e.newValue as TryllAgentComponent;
                    capturedForAgent.AgentOverrideId = capturedForAgent.AgentOverride != null
                        ? GlobalObjectId.GetGlobalObjectIdSlow(capturedForAgent.AgentOverride).ToString()
                        : "";
                    PersistLabConfig();
                    // Rebuild rather than patch: the override pickers below are built
                    // against this variant's graph, which just changed.
                    Rebuild();
                });
                agentRow.Add(agentField);
                if (variant.AgentOverride == null)
                {
                    var usingDefault = new Label(_sourceAgent != null
                        ? $"→ default: {_sourceAgent.name}"
                        : "→ no default assigned");
                    usingDefault.style.color = _sourceAgent != null ? Faint : ColWarn;
                    usingDefault.style.fontSize = 10; usingDefault.style.marginLeft = 6;
                    usingDefault.style.flexShrink = 0;
                    agentRow.Add(usingDefault);
                }
                wrap.Add(agentRow);

                var liveToolsToggle = new Toggle("Use live tools")
                {
                    value = variant.UseLiveTools,
                    tooltip = "Off (default): a tool call with no canned result CheckFails the " +
                              "cell — the model must not call that tool here — and later steps " +
                              "still run. On: an uncanned call may be answered by the scene " +
                              "agent's registered handler.",
                };
                liveToolsToggle.labelElement.style.minWidth = 100;
                liveToolsToggle.labelElement.style.color = Dim;
                liveToolsToggle.style.marginBottom = 6;
                var capturedForLive = variant;
                liveToolsToggle.RegisterValueChangedCallback(e =>
                {
                    capturedForLive.UseLiveTools = e.newValue;
                    PersistLabConfig();
                });
                wrap.Add(liveToolsToggle);

                var variantGraph = GraphOf(EffectiveAgent(variant));
                if (variantGraph == null)
                {
                    var missing = new Label("Assign an agent with a workflow — on this variant or as the window default — to edit overrides.");
                    missing.style.color = Faint; missing.style.fontSize = 10; missing.style.whiteSpace = WhiteSpace.Normal;
                    wrap.Add(missing);
                }
                else
                {
                    // Overrides authored against another agent's workflow can name nodes
                    // this one lacks. The runner reports them too, but flagging here means
                    // it is visible before spending a run.
                    var known = new HashSet<string>(variantGraph.Nodes.Select(n => n.Name));
                    var stale = variant.Overrides.Where(o => !known.Contains(o.NodeName))
                                                 .Select(o => o.NodeName).Distinct().ToList();
                    if (stale.Count > 0)
                    {
                        var warn = new Label($"{TryllEditorUiContract.IconWarning} {string.Join(", ", stale)} — not in this agent's workflow; "
                                           + "these overrides will not be applied.");
                        warn.style.color = ColWarn; warn.style.fontSize = 10; warn.style.whiteSpace = WhiteSpace.Normal;
                        warn.style.marginBottom = 4;
                        wrap.Add(warn);
                    }
                    wrap.Add(BuildModelRow(variant, variantGraph));
                    var sourceScripts = SourceScripts();
                    for (int di = 0; di < sourceScripts.Count; di++)
                    {
                        var scriptData = TryllDialogScriptData.FromAsset(sourceScripts[di]);
                        string prefix = TryllDialogLabDialogues.Prefix(di, scriptData.Name, sourceScripts.Count);
                        foreach (var warn in TryllDialogScriptValidation.CollectIntentCheckWarnings(
                                     scriptData, variantGraph).Select(msg => prefix + msg))
                        {
                            var intentWarn = new Label(TryllEditorUiContract.IconWarning + " " + warn);
                            intentWarn.style.color = ColWarn; intentWarn.style.fontSize = 10;
                            intentWarn.style.whiteSpace = WhiteSpace.Normal;
                            intentWarn.style.marginBottom = 4;
                            wrap.Add(intentWarn);
                        }
                    }
                    wrap.Add(new TryllVariantPanel(variantGraph, variant, () =>
                    {
                        PersistLabConfig();
                        RefreshRunStat();
                    }));
                }
                host.Add(wrap);
            }

            var addVariant = new Button(() =>
            {
                _experiment.Variants.Add(new TryllDialogLabVariant
                {
                    Label = $"Variant {_experiment.Variants.Count + 1}",
                    Swatch = TryllEditorTheme.VariantSwatch(_experiment.Variants.Count),
                });
                PersistLabConfig();
                Rebuild();
            })
            { text = "+ Add variant" };
            addVariant.style.alignSelf = Align.FlexStart;
            host.Add(addVariant);

            _contentScroll.Add(host);
        }

        /// <summary>
        /// Choice list for the per-variant Model dropdown: "(workflow default)" first, then
        /// the manifest's registered language models.
        ///
        /// Two invariants exist because <c>PopupField</c> throws if its initial value is
        /// absent from its choices, and renders a confusing duplicate row otherwise:
        /// <paramref name="currentOverride"/> is always present (a value the manifest no
        /// longer lists — the model was unregistered, or the field was hand-edited — stays
        /// selectable instead of silently resetting to default), and the list never repeats
        /// an entry. Extracted from the UI so both are covered by tests.
        /// </summary>
        internal static List<string> BuildModelChoices(
            IEnumerable<string> languageModels, string currentOverride)
        {
            var choices = new List<string> { ModelDefaultChoice };
            foreach (var name in languageModels ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (!choices.Contains(name)) choices.Add(name);
            }
            if (!string.IsNullOrEmpty(currentOverride) && !choices.Contains(currentOverride))
                choices.Add(currentOverride);
            return choices;
        }

        /// <summary>
        /// Per-variant language model: a dropdown of the project manifest's registered
        /// language models, plus "(workflow default)". This is the cheap half of comparing
        /// models — the value is baked onto a throwaway graph clone at CreateAgent, so the
        /// workflow asset is never touched and no second agent is needed.
        /// </summary>
        private VisualElement BuildModelRow(TryllDialogLabVariant variant, TryllGraphDescription graph)
        {
            var box = new VisualElement { style = { marginBottom = 6 } };

            string assetDefault = string.IsNullOrEmpty(graph?.DefaultModelName)
                ? "" : graph.DefaultModelName;
            var manifest = TryllModelManifest.FindProjectManifest();
            var choices = BuildModelChoices(
                manifest != null ? manifest.ModelsOfType("language") : null,
                variant.ModelOverride);

            var row = FieldBox();
            row.Add(FieldLabel("Model"));
            var popup = new PopupField<string>(
                choices,
                string.IsNullOrEmpty(variant.ModelOverride) ? ModelDefaultChoice : variant.ModelOverride);
            popup.style.flexGrow = 1;
            popup.style.minWidth = 0;
            var capturedForModel = variant;
            popup.RegisterValueChangedCallback(e =>
            {
                capturedForModel.ModelOverride = e.newValue == ModelDefaultChoice ? "" : e.newValue;
                // Rebuild so the hint line and the pinned-node warning re-evaluate.
                Rebuild();
            });
            row.Add(popup);
            box.Add(row);

            if (string.IsNullOrEmpty(variant.ModelOverride))
            {
                var hint = new Label(assetDefault.Length > 0
                    ? $"→ {assetDefault} (from the workflow)"
                    : "→ the workflow sets no default; every model-using node must pin its own");
                hint.style.color = assetDefault.Length > 0 ? Faint : ColWarn;
                hint.style.fontSize = 10; hint.style.marginLeft = 122;
                hint.style.whiteSpace = WhiteSpace.Normal;
                box.Add(hint);
            }
            else
            {
                // The override only reaches nodes that inherit the agent default. Saying so
                // beats a run where the user changes the model and nothing moves.
                var pinned = (graph?.Nodes ?? new List<TryllNodeDescription>())
                    .Where(n => n?.Params != null)
                    .Select(n => new
                    {
                        n.Name,
                        Model = n.Params.GetType()
                            .GetField("ModelName", BindingFlags.Public | BindingFlags.Instance)
                            ?.GetValue(n.Params) as string,
                    })
                    .Where(x => !string.IsNullOrEmpty(x.Model))
                    .Select(x => $"{x.Name} ({x.Model})")
                    .ToList();
                if (pinned.Count > 0)
                {
                    var warn = new Label($"{TryllEditorUiContract.IconWarning} {string.Join(", ", pinned)} pin their own model and will "
                                       + "ignore this override.");
                    warn.style.color = ColWarn; warn.style.fontSize = 10;
                    warn.style.marginLeft = 122; warn.style.whiteSpace = WhiteSpace.Normal;
                    box.Add(warn);
                }
            }

            return box;
        }

        private static Label FieldLabel(string text)
        {
            var l = new Label(text);
            l.style.width = 118; l.style.flexShrink = 0; l.style.color = Dim;
            return l;
        }

        private static VisualElement FieldBox()
        {
            var f = new VisualElement();
            f.style.flexDirection = FlexDirection.Row; f.style.alignItems = Align.Center;
            f.style.backgroundColor = Panel2;
            SetBorder(f, Line2, 1); SetRadius(f, 2);
            f.style.paddingLeft = 7; f.style.paddingRight = 7;
            f.style.paddingTop = 2; f.style.paddingBottom = 2;
            f.style.minWidth = 0;
            return f;
        }

        // ── Toolbar ────────────────────────────────────────────────────────────

        private VisualElement BuildToolbar()
        {
            var bar = new VisualElement();
            bar.style.flexShrink = 0;
            bar.style.flexDirection = FlexDirection.Row; bar.style.alignItems = Align.Center;
            bar.style.flexWrap = Wrap.Wrap;
            bar.style.backgroundColor = Panel2;
            bar.style.paddingLeft = 10; bar.style.paddingRight = 10;
            bar.style.paddingTop = 5; bar.style.paddingBottom = 5;
            bar.style.borderBottomWidth = 1; bar.style.borderBottomColor = Line;

            _runButton = new Button(OnRunClicked) { text = TryllEditorUiContract.IconExpand + " Run" };
            _runButton.style.marginRight = 4;
            bar.Add(_runButton);

            var reset = new Button(OnResetClicked) { text = "↺ Reset" };
            reset.style.marginRight = 10;
            bar.Add(reset);

            _segStage = Segmented(new[] { "Dialog", VariantsTabLabel() },
                                  () => (int)_stage,
                                  i =>
                                  {
                                      if ((StageMode)i == _stage) return;
                                      _stage = (StageMode)i;
                                      // Each stage scrolls independently; carrying the other
                                      // one's offset across would land you mid-list on arrival.
                                      _vOffset = Vector2.zero;
                                      PersistLabConfig();
                                      Rebuild();
                                  });
            bar.Add(_segStage);

            _segMode = Segmented(new[] { "Statistics", "Grid" },
                                 () => (int)_mode,
                                 i => { _mode = (ViewMode)i; PersistLabConfig(); Rebuild(); });
            bar.Add(_segMode);

            _segDensity = Segmented(new[] { "Compact", "Comfortable", "Full" },
                                    () => _density,
                                    i => { _density = i; PersistLabConfig(); Rebuild(); });
            bar.Add(_segDensity);

            _segWidth = Segmented(new[] { "S", "M", "L" },
                                  () => _cellW <= TryllEditorUiContract.CellWidthSmall ? 0
                                      : _cellW <= TryllEditorUiContract.CellWidthMedium ? 1 : 2,
                                  i =>
                                  {
                                      _cellW = i == 0 ? TryllEditorUiContract.CellWidthSmall
                                          : i == 1 ? TryllEditorUiContract.CellWidthMedium
                                          : TryllEditorUiContract.CellWidthLarge;
                                      PersistLabConfig();
                                      Rebuild();
                                  });
            bar.Add(_segWidth);

            var spacer = new VisualElement { style = { flexGrow = 1 } };
            bar.Add(spacer);

            _statusBar = new TryllStatusBar();
            _statusBar.style.marginRight = 8;
            bar.Add(_statusBar);

            _runStat = new Label();
            _runStat.style.color = Dim; _runStat.style.marginRight = 8;
            bar.Add(_runStat);

            _importButton = new Button(OnImportClicked) { text = "Import…" };
            _importButton.tooltip =
                "Open a Dialog Lab JSONL v3 export as read-only results (does not recreate agents or config)";
            _importButton.style.marginRight = 4;
            bar.Add(_importButton);

            _exportButton = new Button(OnExportClicked) { text = "Export…" };
            _exportButton.tooltip =
                "Write the completed (or imported) run as JSONL v3 — header plus one line per cell";
            _exportButton.style.marginRight = 8;
            bar.Add(_exportButton);

            // Label reflects the persisted state — after a domain reload the pane may
            // already be collapsed.
            _inspToggle = new Button(ToggleInspector) { text = _inspectorVisible ? "Inspector ▸" : "Inspector ◂" };
            bar.Add(_inspToggle);

            return bar;
        }

        private Button _exportButton;
        private Button _importButton;

        private void OnExportClicked()
        {
            if (_completedRun == null) return;
            string path = TryllEditorPrompts.SaveFilePanel("Export Dialog Lab run", "", "dialog-lab-run.jsonl", "jsonl");
            if (string.IsNullOrEmpty(path)) return;
            var jsonl = TryllDialogLabExport.ExportToJsonl(_completedRun);
            System.IO.File.WriteAllText(path, jsonl);
            Debug.Log($"[Tryll] Dialog Lab: exported {_completedRun.Samples.Count} samples to {path}");
        }

        private void OnImportClicked()
        {
            if (_running) return;
            string path = TryllEditorPrompts.OpenFilePanel("Import Dialog Lab run", "", "jsonl");
            if (string.IsNullOrEmpty(path)) return;
            string text;
            try { text = System.IO.File.ReadAllText(path); }
            catch (System.Exception ex)
            {
                _importBanner?.Bind("Import failed — " + ex.Message, TryllStatusSeverity.Error);
                return;
            }
            if (!TryllDialogLabImport.TryLoad(text, out var run, out var error))
            {
                _importBanner?.Bind("Import failed — " + (error ?? "Unknown error"),
                    TryllStatusSeverity.Error);
                return;
            }
            _importBanner?.Dismiss();
            InstallRunResult(run, imported: true);
            Rebuild();
        }

        /// <summary>
        /// Show the dialogues finished so far while the run continues. Not a completed run:
        /// <c>_completedRun</c> stays null, so Export stays off, and selection survives so
        /// the reader is not thrown back to the top each time a dialogue lands.
        /// </summary>
        private void InstallPartialResult(TryllDialogLabRunResult run, int completedDialogues)
        {
            _partialRun = run;
            _dataset = TryllDialogLabDataset.FromRunResult(run, imported: false, visibleDialogues: completedDialogues);
            _cells = _dataset.BuildCells();
            _ran = true;
        }

        private void InstallRunResult(TryllDialogLabRunResult run, bool imported)
        {
            _partialRun = null;
            _completedRun = run;
            _resultsImported = imported;
            _dataset = TryllDialogLabDataset.FromRunResult(run, imported);
            _cells = _dataset.BuildCells();
            _ran = true;
            _running = false;
            _runFailure = null;
            _runNotes = run.Issues != null && run.Issues.Count > 0
                ? string.Join("; ", run.Issues)
                : null;
            _selectedKey = null;
            _pinA = null;
            _pinB = null;
            _statsDialogue = -1;
        }

        private VisualElement Segmented(string[] labels, System.Func<int> get, System.Action<int> set)
        {
            var seg = new VisualElement();
            seg.style.flexDirection = FlexDirection.Row;
            seg.style.marginRight = 10;
            SetBorder(seg, Line2, 1); SetRadius(seg, 3);
            seg.style.overflow = Overflow.Hidden;

            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var b = new Button(() => set(idx)) { text = labels[i] };
                b.style.marginLeft = 0; b.style.marginRight = 0;
                b.style.marginTop = 0; b.style.marginBottom = 0;
                b.style.borderLeftWidth = 0; b.style.borderRightWidth = idx < labels.Length - 1 ? 1 : 0;
                b.style.borderTopWidth = 0; b.style.borderBottomWidth = 0;
                b.style.borderRightColor = Line2;
                SetRadius(b, 0);
                b.style.paddingLeft = 9; b.style.paddingRight = 9;
                bool on = get() == idx;
                b.style.backgroundColor = on ? SegmentOnBg : Panel2;
                b.style.color = on ? SegmentOnText : Txt;
                seg.Add(b);
            }
            return seg;
        }

        private void RefreshSegments()
        {
            // Cheapest correct approach: segments are rebuilt with the toolbar's
            // owner on every Rebuild(), so just restyle in place.
            void Restyle(VisualElement seg, int active)
            {
                for (int i = 0; i < seg.childCount; i++)
                {
                    var b = seg.ElementAt(i);
                    bool on = i == active;
                    b.style.backgroundColor = on ? SegmentOnBg : Panel2;
                    b.style.color = on ? SegmentOnText : Txt;
                }
            }
            if (_segStage != null)
            {
                Restyle(_segStage, (int)_stage);
                // Live count in the tab label — the segment is built once in
                // BuildToolbar, so the text has to be refreshed here as variants are
                // added/removed.
                if (_segStage.ElementAt(1) is Button variantsTab) variantsTab.text = VariantsTabLabel();
            }
            if (_segMode    != null) Restyle(_segMode, (int)_mode);
            if (_segDensity != null) Restyle(_segDensity, _density);
            if (_segWidth   != null) Restyle(_segWidth, _cellW <= TryllEditorUiContract.CellWidthSmall ? 0
                : _cellW <= TryllEditorUiContract.CellWidthMedium ? 1 : 2);

            // Density / column width only mean anything for the results views.
            _segMode?.SetEnabled(_stage == StageMode.Dialog);
            _segDensity?.SetEnabled(_stage == StageMode.Dialog);
            _segWidth?.SetEnabled(_stage == StageMode.Dialog);
        }

        private string VariantsTabLabel() => $"Variants ({_experiment.Variants.Count})";

        // ── Progress ───────────────────────────────────────────────────────────

        private VisualElement BuildProgressRow()
        {
            _progressRow = new VisualElement();
            _progressRow.style.flexShrink = 0;
            _progressRow.style.paddingLeft = 10; _progressRow.style.paddingRight = 10;
            _progressRow.style.paddingTop = 5; _progressRow.style.paddingBottom = 5;
            _progressRow.style.backgroundColor = Panel;
            _progressRow.style.borderBottomWidth = 1; _progressRow.style.borderBottomColor = Line;
            _progressRow.style.display = DisplayStyle.None;

            _progress = new ProgressBar { value = 0f, title = "0%" };
            _progressRow.Add(_progress);
            return _progressRow;
        }

        private void OnRunClicked()
        {
            if (_running) return;
            _runFailure = null;
            _runNotes = null;

            if (_labSettings == null) { RefuseRun("Assign Tryll Settings."); return; }
            if (!TryllDialogLabDialogues.TryResolve(_dialogSource, out var scripts, out var sourceErr))
            {
                RefuseRun(sourceErr);
                return;
            }
            var scriptData = scripts.Select(TryllDialogScriptData.FromAsset).ToList();
            if (!TryllDialogLabRunValidation.TryValidateScripts(scriptData, out var scriptErr))
            {
                RefuseRun(scriptErr);
                return;
            }
            if (_experiment.Variants.Count == 0) _experiment.Variants.Add(new TryllDialogLabVariant());

            // The default agent is only required by variants that do not bring their own,
            // so a run with an agent set on every variant needs no default at all.
            foreach (var v in _experiment.Variants)
            {
                if (!TryllDialogLabRunValidation.TryValidateVariant(
                        v, TryllLabRunAgent.FromComponent(EffectiveAgent(v)), scriptData, out var variantErr))
                {
                    RefuseRun(variantErr);
                    return;
                }
            }

            // Capture immutable plan before connection/model awaits — live edits must not
            // affect the in-flight run or its export.
            _pendingSnapshot = TryllDialogLabRunSnapshot.Capture(
                _labSettings, _sourceAgent, scripts, _experiment, EffectiveAgent);
            _pendingLiveAgents = _experiment.Variants.Select(EffectiveAgent).ToList();

            _runGeneration++;
            int generation = _runGeneration;
            _resultsImported = false;
            _completedRun = null;
            _dataset = null;
            _cells = null;
            _ran = false;
            _running = true;
            _collapsedDialogues.Clear();
            Rebuild();

            // Dispose any leftover session from a prior failed Start before acquiring again.
            _labSession?.Stop();
            _labSession = null;

            var session = new TryllEditorSession(_labSettings);
            _labSession = session;
            session.SessionReady += () =>
            {
                if (generation != _runGeneration || _labSession != session) return;
                StartLiveRun(generation, session);
            };
            session.StateChanged += (state, status) =>
            {
                if (generation != _runGeneration || _labSession != session) return;
                if (state == TryllEditorSession.State.Error)
                {
                    _runFailure = status;
                    _running = false;
                    session.Stop();
                    if (_labSession == session) _labSession = null;
                    Rebuild();
                }
                else if (_progress != null)
                {
                    _progress.title = status;
                }
                RefreshOwnerChrome();
            };
            session.Start();
        }

        /// <summary>A run that never started. Kept in <see cref="_runFailure"/>, which
        /// <see cref="Rebuild"/> draws as a banner on every stage, and logged so a refusal
        /// is findable from the Console too.</summary>
        private void RefuseRun(string reason)
        {
            _runFailure = reason;
            Debug.LogWarning("[Tryll] Dialog Lab: run not started. " + reason);
            Rebuild();
        }

        // ── Per-variant agent resolution ────────────────────────────────────────

        /// <summary>The agent a variant actually runs on: its own override, else the
        /// window default.</summary>
        private TryllAgentComponent EffectiveAgent(TryllDialogLabVariant v) =>
            v.AgentOverride != null ? v.AgentOverride : _sourceAgent;

        private static TryllGraphDescription GraphOf(TryllAgentComponent a) =>
            a == null ? null : a.WorkflowAsset != null ? a.WorkflowAsset.Graph : null;

        private async void StartLiveRun(int generation, TryllEditorSession session)
        {
            if (generation != _runGeneration || _labSession != session) return;
            var snapshot = _pendingSnapshot;
            if (snapshot == null)
            {
                _runFailure = "Run snapshot missing.";
                _running = false;
                session.Stop();
                if (_labSession == session) _labSession = null;
                Rebuild();
                return;
            }

            _runner = new TryllDialogLabRunner();
            _runner.Progress += (done, total) =>
            {
                if (generation != _runGeneration) return;
                if (_progress == null) return;
                float pct = total > 0 ? 100f * done / total : 0f;
                _progress.value = pct;
                _progress.title = $"{done}/{total} turns · {(int)pct}%";
            };
            // A finished dialogue is complete in every column, so showing it before the
            // run ends shows no biased subset (R4.1 holds per dialogue). The last one
            // arrives through the normal completion path instead.
            _runner.DialogueCompleted += (partial, completed) =>
            {
                if (generation != _runGeneration) return;
                if (completed >= partial.Snapshot.Dialogues.Count) return;
                InstallPartialResult(partial, completed);
                Rebuild();
            };

            var runTask = _runner.RunAsync(session.Client, snapshot, _pendingLiveAgents);
            _activeRunTask = runTask;
            TryllDialogLabRunResult result;
            try { result = await runTask; }
            catch (System.Exception ex)
            {
                if (generation != _runGeneration) return;
                _runFailure = ex.Message;
                _running = false;
                session.Stop();
                if (_labSession == session) _labSession = null;
                Rebuild();
                return;
            }
            finally
            {
                if (_activeRunTask == runTask) _activeRunTask = null;
            }

            if (generation != _runGeneration) return;

            session.Stop();
            if (_labSession == session) _labSession = null;
            _pendingSnapshot = null;
            _running = false;

            // Reset/Stop: discard — do not publish partial rectangular results to the UI.
            if (result == null || result.Status == TryllDialogLabRunStatus.Stopped)
            {
                _partialRun = null;
                _dataset = null;
                _cells = null;
                _ran = false;
                Rebuild();
                return;
            }

            InstallRunResult(result, imported: false);
            Rebuild();
        }

        private async void OnResetClicked()
        {
            int generation = ++_runGeneration;
            _runner?.Stop();
            var task = _activeRunTask;
            if (task != null)
            {
                try { await task; }
                catch { /* runner surfaces status via result */ }
            }
            if (generation != _runGeneration) return;

            _labSession?.Stop();
            _labSession = null;
            _pendingSnapshot = null;
            _runner = null;
            _activeRunTask = null;
            _running = false;
            _ran = false;
            _runFailure = null;
            _runNotes = null;
            _completedRun = null;
            _resultsImported = false;
            _dataset = null;
            _cells = null;
            _selectedKey = null;
            _pinA = null;
            _pinB = null;
            Rebuild();
        }

        // ── Rebuild ────────────────────────────────────────────────────────────

        private void Rebuild()
        {
            if (_contentScroll == null) return;

            // Selection rebuilds the whole stage, so both scroll offsets have to
            // be carried across or every click would snap back to column 1, top.
            _vOffset = _contentScroll.scrollOffset;
            _jumpTarget = null;

            _hScrollers.Clear();
            _headerHost.Clear();
            _contentScroll.Clear();

            _runButton.SetEnabled(!_running);
            _runButton.text = $"{TryllEditorUiContract.IconExpand} Run {PlannedCells} cells";
            _progressRow.style.display = _running ? DisplayStyle.Flex : DisplayStyle.None;
            _importButton?.SetEnabled(!_running);
            _exportButton.SetEnabled(!_running && _completedRun != null);
            RefreshSegments();
            RefreshRunStat();
            RefreshScriptWarnings();

            // On every stage and over old results alike: the user may press Run from the
            // Variants tab, or with a previous run's grid still on screen.
            if (!string.IsNullOrEmpty(_runFailure))
                _contentScroll.Add(Banner(_runFailure));

            // Ahead of the run-state gates below: variant authoring is an editing
            // surface, usable before a first run and while one is in flight.
            if (_stage == StageMode.Variants)
            {
                BuildVariantsStage();
                RenderInspector();
                RestoreScroll();
                return;
            }

            if (_running && !HasResults) { _contentScroll.Add(Placeholder("Running…")); RenderInspector(); return; }
            // HasResults, not _ran: everything below dereferences _dataset/_cells
            // (BuildColumnHeader, BuildGrid, BuildStatistics), so falling through with the
            // flag set but the data missing would just move the crash one frame later.
            if (!HasResults)
            {
                _contentScroll.Add(Placeholder(PreRunMessage()));
                RenderInspector();
                return;
            }

            if (_resultsImported)
                _contentScroll.Add(Banner("Imported run (read-only results). Authoring fields above are for the next local run."));
            if (!string.IsNullOrEmpty(_runNotes))
                _contentScroll.Add(Banner(_runNotes));

            // Statistics is per variant, not per column, so it has no column header.
            if (_mode == ViewMode.Grid) { _headerHost.Add(BuildColumnHeader()); BuildGrid(); }
            else                        BuildStatistics();
            if (_running)
                _contentScroll.Add(Placeholder(
                    $"Running dialogue {_dataset.Dialogues.Length + 1} of {_dataset.TotalDialogues}…"));

            RenderInspector();
            RestoreScroll();
        }

        /// <summary>
        /// Seeded lines that break a global check: not counted (global checks skip seeded
        /// turns), so shown before Run. The runner repeats them in the run's issues.
        /// </summary>
        private void RefreshScriptWarnings()
        {
            if (_scriptWarningsHost == null) return;
            _scriptWarningsHost.Clear();
            var scripts = SourceScripts();
            for (int di = 0; di < scripts.Count; di++)
            {
                var data = TryllDialogScriptData.FromAsset(scripts[di]);
                string prefix = TryllDialogLabDialogues.Prefix(di, data.Name, scripts.Count);
                foreach (var msg in TryllDialogScriptValidation.CollectSeededGlobalCheckWarnings(data))
                {
                    var warn = new Label(TryllEditorUiContract.IconWarning + " " + prefix + msg);
                    warn.style.color = ColWarn; warn.style.fontSize = 10;
                    warn.style.whiteSpace = WhiteSpace.Normal;
                    warn.style.marginBottom = 2;
                    _scriptWarningsHost.Add(warn);
                }
            }
        }

        private VisualElement Banner(string text)
        {
            var l = new Label(text);
            l.style.color = ColWarn;
            l.style.backgroundColor = WarnBg;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginLeft = 10; l.style.marginRight = 10; l.style.marginTop = 8;
            l.style.paddingLeft = 8; l.style.paddingRight = 8;
            l.style.paddingTop = 6; l.style.paddingBottom = 6;
            SetBorder(l, new Color(0.420f, 0.325f, 0.125f), 1);
            SetRadius(l, 4);
            return l;
        }

        /// Offsets can only be applied once layout has run, hence the deferred
        /// execute — same delayed-offset pattern as TryllChatWindow.ScrollChatToEnd.
        private void RestoreScroll()
        {
            var jump = _jumpTarget;
            int jumpColumn = _jumpColumn;
            _jumpTarget = null;
            _jumpColumn = -1;
            if (jump != null) ScrollToJumpTarget(jump, jumpColumn);

            _contentScroll.schedule.Execute(() =>
            {
                if (jump == null) _contentScroll.scrollOffset = _vOffset;
                _syncing = true;
                foreach (var sv in _hScrollers) sv.horizontalScroller.value = _hOffset;
                _syncing = false;
            }).ExecuteLater(1);
        }

        /// <summary>
        /// Brings a jumped-to turn's band to the top of the Grid, and its column into view.
        /// Positions exist only after layout, so this waits for the content's geometry pass —
        /// on the content container, where the ScrollView's own handler (registered first)
        /// has already resized the scrollers — with a short timer as a fallback in case the
        /// new content happens to keep its old size. Applying twice is harmless.
        /// </summary>
        private void ScrollToJumpTarget(VisualElement band, int column)
        {
            var content = _contentScroll.contentContainer;
            void Apply()
            {
                if (band.panel == null || float.IsNaN(band.layout.height) || band.layout.height <= 0f) return;
                float y = Mathf.Max(0f, band.layout.y - band.resolvedStyle.marginTop);
                _contentScroll.scrollOffset = new Vector2(_contentScroll.scrollOffset.x, y);

                if (column < 0) return;
                float colX = column * (_cellW + Gap);
                float view = _contentScroll.contentViewport.layout.width - 20f; // row padding
                if (float.IsNaN(view) || view <= 0f) return;
                if (colX >= _hOffset && colX + _cellW <= _hOffset + view) return;
                _hOffset = colX;
                _syncing = true;
                foreach (var sv in _hScrollers) sv.horizontalScroller.value = _hOffset;
                _syncing = false;
            }
            EventCallback<GeometryChangedEvent> once = null;
            once = _ => { content.UnregisterCallback(once); Apply(); };
            content.RegisterCallback(once);
            _contentScroll.schedule.Execute(() => { content.UnregisterCallback(once); Apply(); }).ExecuteLater(50);
        }

        /// <summary>
        /// True only when there are results safe to read. `_ran` is *supposed* to imply
        /// both of these — they are assigned together in <see cref="InstallRunResult"/>
        /// and cleared together everywhere else — but throwing out of a UI callback
        /// aborts the whole <see cref="Rebuild"/> and leaves the window half-built, with
        /// a NullReferenceException instead of a usable panel. Checking the data rather
        /// than the flag makes every reader robust to that invariant being broken.
        /// </summary>
        private bool HasResults => _ran && _cells != null && _dataset != null;

        private bool _warnedInconsistentState;

        /// <summary>Surfaces the invariant break once per window instead of silently
        /// degrading, so it stays diagnosable if it recurs.</summary>
        private void WarnIfInconsistent(string where)
        {
            if (!_ran || HasResults || _warnedInconsistentState) return;
            _warnedInconsistentState = true;
            Debug.LogWarning(
                $"[Tryll] Dialog Lab: results marked present but missing in {where} "
                + $"(cells={( _cells == null ? "null" : "ok")}, dataset={(_dataset == null ? "null" : "ok")}). "
                + "Showing the pre-run state. Please report the steps that led here.");
        }

        private void RefreshOwnerChrome()
        {
            if (_statusBar == null) return;
            TryllOwnerPhase owner = _labSession?.OwnerPhase ?? TryllOwnerPhase.Idle;
            string detail = _labSession?.OwnerDetail;
            var pres = TryllStatusPresentation.Compose(
                owner, detail, TryllPanelOperation.None, TryllEditorPanel.DialogLab);
            _statusBar.Bind(pres);
        }

        private void RefreshRunStat()
        {
            RefreshOwnerChrome();
            if (_runStat == null) return;
            WarnIfInconsistent(nameof(RefreshRunStat));
            if (!HasResults) { _runStat.text = _running ? "running…" : "not run"; return; }

            // Generated turns only, as on the Statistics tab: a scripted line has no
            // checks to fail, so counting it would always score it clean.
            var all = _cells.Values.Where(c => !c.Seeded).Select(_dataset.StatusOf).ToList();
            int N(LabCellStatus s) => all.Count(x => x == s);
            var parts = new List<string>();
            if (_resultsImported) parts.Add("imported");
            parts.Add($"{_dataset.Samples.Length}/{_dataset.Samples.Length} cells");
            parts.Add($"{N(LabCellStatus.Ok)}/{all.Count} turns clean");
            if (N(LabCellStatus.CheckFail) > 0) parts.Add($"{N(LabCellStatus.CheckFail)} check fail");
            if (N(LabCellStatus.Error) > 0)     parts.Add($"{N(LabCellStatus.Error)} error");
            if (N(LabCellStatus.Skipped) > 0)   parts.Add($"{N(LabCellStatus.Skipped)} not run");
            _runStat.text = string.Join(" · ", parts);
        }

        // ── Column header (grouped: variant spans its repeat columns) ──────────

        private VisualElement BuildColumnHeader()
        {
            _headerScroll = MakeHScroll();
            _headerScroll.horizontalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            _headerScroll.style.paddingLeft = 10; _headerScroll.style.paddingRight = 10;
            _headerScroll.style.paddingTop = 4;

            var col = new VisualElement();
            col.style.flexDirection = FlexDirection.Column;

            // variant band
            var vr = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            foreach (var v in _dataset.Variants)
            {
                int n = _dataset.Samples.Count(s => s.VariantId == v.Id);
                var g = new VisualElement();
                g.style.flexDirection = FlexDirection.Row; g.style.alignItems = Align.Center;
                g.style.width = n * _cellW + (n - 1) * Gap;
                g.style.flexShrink = 0; g.style.marginRight = Gap;
                g.style.backgroundColor = Panel2;
                SetBorder(g, Line2, 1); SetRadius(g, 3);
                g.style.paddingLeft = 8; g.style.paddingRight = 8;
                g.style.paddingTop = 2; g.style.paddingBottom = 2;
                g.style.overflow = Overflow.Hidden;

                g.Add(Swatch(v.Color));
                var name = new Label(v.Label);
                name.style.unityFontStyleAndWeight = FontStyle.Bold; name.style.color = Txt;
                g.Add(name);
                // Agent first: with per-variant agents it is the coarsest difference
                // between columns, so it should read before the param overrides.
                var diff = new Label(string.IsNullOrEmpty(v.AgentName)
                    ? $"· {v.DiffLabel}"
                    : $"· {v.AgentName} · {v.DiffLabel}");
                diff.style.color = Faint; diff.style.marginLeft = 6;
                g.Add(diff);
                vr.Add(g);
            }
            col.Add(vr);

            // per-sample header
            var sr = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 3 } };
            foreach (var s in _dataset.Samples)
            {
                var h = new VisualElement();
                h.style.flexDirection = FlexDirection.Row; h.style.alignItems = Align.Center;
                h.style.width = _cellW; h.style.flexShrink = 0; h.style.marginRight = Gap;
                h.style.backgroundColor = Panel2;
                SetBorder(h, Line2, 1); SetRadius(h, 3);
                h.style.paddingLeft = 8; h.style.paddingRight = 6;
                h.style.paddingTop = 2; h.style.paddingBottom = 2;

                var id = new Label(s.Id);
                id.style.unityFontStyleAndWeight = FontStyle.Bold; id.style.color = Txt;
                h.Add(id);
                var seed = new Label($"seed {s.Seed}");
                seed.style.color = Faint; seed.style.marginLeft = 6;
                h.Add(seed);
                h.Add(new VisualElement { style = { flexGrow = 1 } });

                bool pinned = s.Id == _pinA || s.Id == _pinB;
                var pin = new Button(() => PinColumn(s.Id)) { text = "⊙" };
                pin.tooltip = "Pin this column for the A/B prompt diff";
                pin.style.color = pinned ? Accent : Faint;
                pin.style.paddingLeft = 4; pin.style.paddingRight = 4;
                pin.style.marginLeft = 0; pin.style.marginRight = 0;
                pin.style.marginTop = 0; pin.style.marginBottom = 0;
                h.Add(pin);

                sr.Add(h);
            }
            col.Add(sr);

            _headerScroll.Add(col);
            return _headerScroll;
        }

        // ── Grid mode ──────────────────────────────────────────────────────────

        private void BuildGrid()
        {
            bool grouped = _dataset.TotalDialogues > 1;
            for (int ti = 0; ti < _dataset.Turns.Length; ti++)
            {
                var t = _dataset.Turns[ti];
                if (grouped && t.StepIndex == 0)
                    _contentScroll.Add(BuildDialogueHeader(t.DialogueIndex));
                if (grouped && _collapsedDialogues.Contains(t.DialogueIndex)) continue;
                var band = BuildBand(ti);
                if (ti == _jumpTurn) _jumpTarget = band;
                _contentScroll.Add(band);
                _contentScroll.Add(BuildCellRow(ti));
            }
            _jumpTurn = -1;
            var pad = new VisualElement { style = { height = 24 } };
            _contentScroll.Add(pad);
        }

        /// <summary>
        /// A dialogue's separator in a multi-dialogue run: its name, size, and how many of
        /// its generated turns passed across all columns. Click to fold the group.
        /// </summary>
        private VisualElement BuildDialogueHeader(int d)
        {
            var dlg = _dataset.Dialogues[d];
            bool collapsed = _collapsedDialogues.Contains(d);
            int generated = 0, passed = 0;
            for (int ti = dlg.FirstTurn; ti < dlg.FirstTurn + dlg.TurnCount; ti++)
            {
                if (_dataset.Turns[ti].Seeded) continue;
                foreach (var s in _dataset.Samples)
                {
                    generated++;
                    if (_dataset.StatusOf(_cells[TryllDialogLabDataset.CellKey(s.Id, ti)]) == LabCellStatus.Ok)
                        passed++;
                }
            }

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
            head.style.marginLeft = 10; head.style.marginRight = 10; head.style.marginTop = 14;
            head.style.paddingLeft = 6; head.style.paddingRight = 10;
            head.style.paddingTop = 5; head.style.paddingBottom = 5;
            head.style.backgroundColor = Panel;
            SetBorder(head, Line, 1); SetRadius(head, 6);
            head.style.flexShrink = 0;

            var caret = new Label(collapsed ? "▸" : "▾");
            caret.style.color = Faint; caret.style.width = 14;
            caret.style.unityTextAlign = TextAnchor.MiddleCenter;
            head.Add(caret);

            var name = new Label($"DIALOGUE {d + 1} · {dlg.Name}");
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.flexGrow = 1; name.style.minWidth = 0;
            head.Add(name);

            var size = new Label($"{dlg.TurnCount} turns{(dlg.SeededTurns > 0 ? $" ({dlg.SeededTurns} seeded)" : "")}");
            size.style.color = Dim; size.style.marginRight = 8;
            head.Add(size);
            bool allOk = passed == generated;
            head.Add(Chip($"{passed}/{generated} turns passed",
                          allOk ? ColOk : ColWarn,
                          allOk ? OkBg : WarnBg,
                          allOk ? new Color(0.153f, 0.361f, 0.235f) : new Color(0.420f, 0.325f, 0.125f)));

            head.tooltip = collapsed ? "Expand this dialogue" : "Collapse this dialogue";
            head.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!_collapsedDialogues.Remove(d)) _collapsedDialogues.Add(d);
                Rebuild();
            });
            return head;
        }

        /// The user band lives OUTSIDE the horizontal scroller, so it always
        /// spans the visible width no matter how far the columns are scrolled.
        /// This is the UI Toolkit stand-in for `position: sticky; left: 0`.
        private VisualElement BuildBand(int ti)
        {
            var t = _dataset.Turns[ti];
            var b = new VisualElement();
            b.style.backgroundColor = UserBub;
            SetRadius(b, 8);
            b.style.paddingLeft = 11; b.style.paddingRight = 11;
            b.style.paddingTop = 5; b.style.paddingBottom = 6;
            b.style.marginLeft = 10; b.style.marginRight = 10;
            b.style.marginTop = 10; b.style.marginBottom = 6;
            b.style.flexShrink = 0;

            var who = new Label($"YOU · TURN {t.StepIndex + 1}{(t.Seeded ? " · SEEDED" : "")}");
            who.style.fontSize = 10; who.style.unityFontStyleAndWeight = FontStyle.Bold;
            who.style.color = new Color(1f, 1f, 1f, 0.7f);
            b.Add(who);

            var txt = new Label(t.User);
            txt.style.whiteSpace = WhiteSpace.Normal;
            txt.style.color = new Color(0.918f, 0.945f, 0.980f);
            b.Add(txt);

            var chips = new VisualElement();
            chips.style.flexDirection = FlexDirection.Row; chips.style.flexWrap = Wrap.Wrap;
            chips.style.marginTop = 3;
            foreach (var c in t.Checks) chips.Add(Chip(c.Label, Dim, new Color(0, 0, 0, 0.22f), Line2));
            b.Add(chips);
            return b;
        }

        private VisualElement BuildCellRow(int ti)
        {
            var sv = MakeHScroll();
            sv.style.paddingLeft = 10; sv.style.paddingRight = 10;
            // A row would otherwise spend a vertical wheel on its own columns until they hit
            // the end, so wheeling over the cells (most of the Grid) scrolled sideways first.
            // Columns scroll with the header's bar, Shift+wheel, or a sideways trackpad swipe.
            sv.RegisterCallback<WheelEvent>(e =>
            {
                if (e.shiftKey || Mathf.Abs(e.delta.y) <= Mathf.Abs(e.delta.x)) return;
                var offset = _contentScroll.scrollOffset;
                _contentScroll.scrollOffset = new Vector2(offset.x, offset.y + e.delta.y * _contentScroll.mouseWheelScrollSize);
                e.StopPropagation();
            }, TrickleDown.TrickleDown);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Stretch;

            foreach (var s in _dataset.Samples)
                row.Add(BuildCell(_cells[TryllDialogLabDataset.CellKey(s.Id, ti)]));

            sv.Add(row);
            return sv;
        }

        private VisualElement BuildCell(LabCell c)
        {
            var st = _dataset.StatusOf(c);
            bool selected = _selectedKey == c.Key;

            var d = new VisualElement();
            d.style.width = _cellW; d.style.flexShrink = 0; d.style.marginRight = Gap;
            d.style.flexDirection = FlexDirection.Column;
            d.style.backgroundColor = selected ? SelBub
                : st == LabCellStatus.Error     ? ErrCell
                : st == LabCellStatus.Skipped   ? SkipCell
                : st == LabCellStatus.CheckFail ? WarnBg
                : AsstBub;
            SetRadius(d, 10);
            // Cells have a fixed width by design: nothing may paint past the card.
            d.style.overflow = Overflow.Hidden;
            d.style.paddingLeft = 9; d.style.paddingRight = 9;
            d.style.paddingTop = 6; d.style.paddingBottom = 5;

            Color outline = c.SampleId == _pinA ? Accent
                          : c.SampleId == _pinB ? ColWarn
                          : Line2;
            SetBorder(d, outline, 1);

            // body text, clipped per density
            var wrap = new VisualElement();
            int maxLines = TryllEditorUiContract.DensityLines[Mathf.Clamp(_density, 0, 2)];
            if (maxLines > 0)
            {
                wrap.style.maxHeight = maxLines * LineH;
                wrap.style.overflow = Overflow.Hidden;
            }
            var body = new Label(c.Text);
            body.style.whiteSpace = WhiteSpace.Normal;
            body.style.color = st == LabCellStatus.Error || st == LabCellStatus.Skipped
                             ? Dim : new Color(0.902f, 0.910f, 0.925f);
            if (st == LabCellStatus.Error || st == LabCellStatus.Skipped)
                body.style.unityFontStyleAndWeight = FontStyle.Italic;
            wrap.Add(body);
            d.Add(wrap);

            // footer
            var foot = new VisualElement();
            foot.style.flexDirection = FlexDirection.Row; foot.style.flexWrap = Wrap.Wrap;
            foot.style.marginTop = 4;

            if (st == LabCellStatus.Error)
            {
                foot.Add(Chip("⨯ ERROR", ColErr, ErrBg, new Color(0.431f, 0.196f, 0.188f)));
            }
            else if (st == LabCellStatus.Skipped)
            {
                foot.Add(Chip("⊘ not run", Faint, new Color(0, 0, 0, 0.22f), Line2));
            }
            else
            {
                foot.Add(Chip($"{c.Ms / 1000f:0.00}s", Dim, new Color(0, 0, 0, 0.22f), Line2));
                foot.Add(Chip($"{c.Words}w", Dim, new Color(0, 0, 0, 0.22f), Line2));
                if (c.Route.Any(n => n.Type == "CannedResponseNode"))
                    foot.Add(Chip("canned", ColWarn, WarnBg, new Color(0.420f, 0.325f, 0.125f)));
                if (c.Failed.Count > 0)
                    foreach (var f in c.Failed)
                    {
                        // A check message can be a whole sentence: cut it at the card's
                        // width and keep the full text in the tooltip.
                        string label = f.Label ?? f.Kind;
                        var chip = Chip("✗ " + label, ColWarn, WarnBg, new Color(0.420f, 0.325f, 0.125f));
                        chip.style.flexShrink = 1;
                        chip.style.maxWidth = Length.Percent(100);
                        chip.style.overflow = Overflow.Hidden;
                        chip.style.textOverflow = TextOverflow.Ellipsis;
                        chip.tooltip = label;
                        foot.Add(chip);
                    }
                else
                    foot.Add(Chip("✓ checks", ColOk, OkBg, new Color(0.153f, 0.361f, 0.235f)));

                if (maxLines > 0 && EstimateLines(c.Text) > maxLines)
                    foot.Add(Chip("⋯ more", Faint, new Color(0, 0, 0, 0.22f), Line2));
            }
            d.Add(foot);

            if (st != LabCellStatus.Skipped) d.Add(TimingBar(c, 3f));

            d.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.shiftKey) PinColumn(c.SampleId);
                Select(c.Key);
            });
            return d;
        }

        private int EstimateLines(string text)
        {
            float charsPerLine = Mathf.Max(8f, (_cellW - 24f) / 6.2f);
            return Mathf.CeilToInt(text.Length / charsPerLine);
        }

        // ── Statistics mode ────────────────────────────────────────────────────

        /// <summary>Per-variant pass rates (layout A of dialog-lab-statistics-mockup.html),
        /// optionally as differences from the first variant (layout D), then where the
        /// failures are. Numbers come from <see cref="TryllDialogLabStatistics"/> — the same
        /// function the export header uses.</summary>
        private void BuildStatistics()
        {
            int? filter = _statsDialogue >= 0 && _statsDialogue < _dataset.Dialogues.Length ? _statsDialogue : (int?)null;
            var run = _completedRun ?? _partialRun;
            if (run == null) return;
            // While a run is still publishing, count only the dialogues already shown.
            if (_dataset.Dialogues.Length < _dataset.TotalDialogues) run = TrimToShown(run);
            var stats = TryllDialogLabStatistics.Compute(run, filter);
            bool intervals = _statsIntervals && stats.IntervalsAvailable;
            bool delta = _statsDelta && stats.Variants.Count > 1;

            var wrap = new VisualElement();
            wrap.style.marginLeft = 10; wrap.style.marginRight = 10; wrap.style.marginTop = 8;
            _contentScroll.Add(wrap);

            // Controls: dialogue filter, absolute vs baseline, intervals.
            var controls = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, alignItems = Align.Center } };
            if (_dataset.Dialogues.Length > 1)
            {
                var names = new[] { $"All ({_dataset.Dialogues.Length})" }
                    .Concat(_dataset.Dialogues.Select(d => d.Name)).ToArray();
                controls.Add(Segmented(names, () => _statsDialogue + 1, i => { _statsDialogue = i - 1; Rebuild(); }));
            }
            if (stats.Variants.Count > 1)
                controls.Add(Segmented(new[] { "Absolute", "vs " + stats.Variants[0].Label },
                    () => _statsDelta ? 1 : 0, i => { _statsDelta = i == 1; Rebuild(); }));
            var ci = Segmented(new[] { "95% interval" }, () => intervals ? 0 : -1,
                _ => { _statsIntervals = !_statsIntervals; Rebuild(); });
            ci.SetEnabled(stats.IntervalsAvailable);
            ci.tooltip = stats.IntervalsAvailable
                ? "Computed across seeds: each seed's rate is one sample."
                : $"Needs at least {TryllDialogLabStatistics.MinSeedsForInterval} seeds per variant (Repeats).";
            controls.Add(ci);
            if (!stats.IntervalsAvailable)
                controls.Add(Hint($"Intervals need {TryllDialogLabStatistics.MinSeedsForInterval}+ seeds per variant; this run has {stats.MinSeeds}."));
            wrap.Add(controls);

            Metric(wrap, stats, "Turns passed", "generated turns with every check passing", v => v.Turns, intervals, delta);
            Metric(wrap, stats, "Checks passed", "authored checks on generated turns", v => v.Checks, intervals, delta);
            Metric(wrap, stats, "Clean dialogues", "dialogue × seed runs with no failed turn", v => v.CleanDialogues, intervals, delta);

            var shape = string.Join("   ·   ", stats.Variants.Select(v =>
                $"{v.Label}: {v.Errors} error, {v.NotRun} not run, "
                + $"{Fmt(v.MedianWords, "0")} words median (p90 {Fmt(v.P90Words, "0")}), "
                + $"TTFT {Fmt(v.MedianTtftMs, "0")} ms (p90 {Fmt(v.P90TtftMs, "0")})"));
            var shapeLabel = Hint(shape);
            shapeLabel.style.marginTop = 6;
            wrap.Add(shapeLabel);

            wrap.Add(SectionTitle("Where turns fail", "failed seeds / seeds per generated turn · click a cell to open it in the Grid"));
            wrap.Add(WhereTurnsFailTable(stats));
            wrap.Add(SectionTitle("Failing checks by type", "failed / evaluated"));
            wrap.Add(CheckTypeTable(stats));
            _contentScroll.Add(new VisualElement { style = { height = 24 } });
        }

        /// <summary>A copy of a partial run keeping only the dialogues already shown, so the
        /// statistics never count turns of a dialogue that is still running.</summary>
        private TryllDialogLabRunResult TrimToShown(TryllDialogLabRunResult run)
        {
            int shown = _dataset.Dialogues.Length;
            return new TryllDialogLabRunResult
            {
                Snapshot = run.Snapshot,
                Samples = run.Samples.Select(s => new TryllLabSampleResult
                {
                    SampleId = s.SampleId,
                    VariantIndex = s.VariantIndex,
                    VariantLabel = s.VariantLabel,
                    VariantSwatch = s.VariantSwatch,
                    Repeat = s.Repeat,
                    Seed = s.Seed,
                    Turns = s.Turns.Where(t => t.DialogueIndex < shown).ToList(),
                }).ToList(),
            };
        }

        private const float StatsNameWidth = 170f;
        private const float StatsValueWidth = 210f;

        private void Metric(VisualElement parent, TryllDialogLabStatistics stats, string title, string hint,
                            System.Func<TryllLabVariantStats, TryllLabRate> pick, bool intervals, bool delta)
        {
            parent.Add(SectionTitle(title, delta ? $"points vs {stats.Variants[0].Label}" : hint));
            for (int vi = 0; vi < stats.Variants.Count; vi++)
            {
                var v = stats.Variants[vi];
                var rate = pick(v);
                if (delta && vi == 0) continue;

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 3 } };
                row.Add(VariantName(v));

                var track = new VisualElement();
                track.style.flexGrow = 1; track.style.height = 14; track.style.minWidth = 60;
                track.style.marginLeft = 6; track.style.marginRight = 6;
                row.Add(track);

                var value = new Label();
                value.style.width = StatsValueWidth; value.style.flexShrink = 0;
                value.style.unityTextAlign = TextAnchor.MiddleRight;
                row.Add(value);

                if (!delta)
                {
                    foreach (float g in new[] { 0.25f, 0.5f, 0.75f }) track.Add(Tick(g, Line));
                    track.Add(Bar(0f, (float)rate.Rate, 3, 8, v.Swatch));
                    if (intervals) track.Add(Whisker((float)rate.Low, (float)rate.High));
                    value.text = rate.Total == 0
                        ? "no data"
                        : $"{rate.Rate * 100:0.0}% · {rate.Passed}/{rate.Total}" + (intervals ? $" ± {rate.HalfWidth * 100:0.0}" : "");
                    row.tooltip = $"{v.Label} — {title}: {rate.Passed} of {rate.Total}"
                                + (intervals ? $", 95% interval {rate.Low * 100:0.0}–{rate.High * 100:0.0}%" : "")
                                + $" over {v.Seeds} seeds";
                }
                else
                {
                    const float range = 0.4f; // ± 40 points
                    var (diff, half) = stats.DifferenceFromBaseline(pick, vi);
                    float X(double d) => 0.5f + Mathf.Clamp((float)d, -range, range) / range * 0.5f;
                    track.Add(Tick(0.5f, Dim));
                    if (intervals) track.Add(Whisker(X(diff - half), X(diff + half), v.Swatch));
                    var dot = new VisualElement();
                    dot.style.position = Position.Absolute; dot.style.top = 2;
                    dot.style.width = 10; dot.style.height = 10; dot.style.marginLeft = -5;
                    dot.style.left = Length.Percent(X(diff) * 100f);
                    dot.style.backgroundColor = v.Swatch; SetRadius(dot, 5);
                    track.Add(dot);
                    bool clear = intervals && (diff - half > 0 || diff + half < 0);
                    string verdict = !intervals ? "" : clear ? (diff > 0 ? " · higher" : " · lower") : " · no clear difference";
                    value.text = $"{(diff >= 0 ? "+" : "")}{diff * 100:0.0} pts" + (intervals ? $" ± {half * 100:0.0}" : "") + verdict;
                    value.style.color = clear ? Txt : Dim;
                    row.tooltip = $"{v.Label} minus {stats.Variants[0].Label}: {diff * 100:0.0} points";
                }
                parent.Add(row);
            }
        }

        private VisualElement VariantName(TryllLabVariantStats v)
        {
            var name = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            name.style.width = StatsNameWidth; name.style.flexShrink = 0;
            var sw = new VisualElement();
            sw.style.width = 10; sw.style.height = 10; sw.style.marginRight = 6; sw.style.flexShrink = 0;
            sw.style.backgroundColor = v.Swatch; SetRadius(sw, 2);
            name.Add(sw);
            var l = new Label($"{v.Label}  ({v.Seeds} seeds)");
            l.style.overflow = Overflow.Hidden; l.style.textOverflow = TextOverflow.Ellipsis;
            name.Add(l);
            return name;
        }

        private static VisualElement Bar(float from, float to, float top, float height, Color c)
        {
            var b = new VisualElement();
            b.style.position = Position.Absolute;
            b.style.left = Length.Percent(from * 100f);
            b.style.width = Length.Percent(Mathf.Max(0f, to - from) * 100f);
            b.style.top = top; b.style.height = height;
            b.style.backgroundColor = c;
            b.style.borderTopRightRadius = 3; b.style.borderBottomRightRadius = 3;
            return b;
        }

        private static VisualElement Tick(float at, Color c)
        {
            var t = new VisualElement();
            t.style.position = Position.Absolute; t.style.left = Length.Percent(at * 100f);
            t.style.top = 0; t.style.bottom = 0; t.style.width = 1;
            t.style.backgroundColor = c;
            return t;
        }

        private static VisualElement Whisker(float low, float high, Color? c = null)
        {
            var w = Bar(low, high, 6, 2, c ?? Txt);
            w.style.borderTopRightRadius = 0; w.style.borderBottomRightRadius = 0;
            w.Add(EndCap(true, c ?? Txt));
            w.Add(EndCap(false, c ?? Txt));
            return w;
        }

        private static VisualElement EndCap(bool left, Color c)
        {
            var cap = new VisualElement();
            cap.style.position = Position.Absolute; cap.style.top = -4;
            cap.style.width = 2; cap.style.height = 10; cap.style.backgroundColor = c;
            if (left) cap.style.left = 0; else cap.style.right = 0;
            return cap;
        }

        private static Label Hint(string text)
        {
            var l = new Label(text);
            l.style.color = Faint; l.style.fontSize = 10;
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        private static string Fmt(double? x, string format) => x.HasValue ? x.Value.ToString(format) : "–";

        private static VisualElement SectionTitle(string title, string hint)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexEnd, flexWrap = Wrap.Wrap } };
            row.style.marginTop = 14; row.style.marginBottom = 2;
            var t = new Label(title);
            t.style.unityFontStyleAndWeight = FontStyle.Bold; t.style.marginRight = 8;
            row.Add(t);
            row.Add(Hint(hint));
            return row;
        }

        /// <summary>Rows are generated turns, columns are variants. A cell's tint grows with
        /// the share of seeds that failed; click opens that turn in the Grid.</summary>
        private VisualElement WhereTurnsFailTable(TryllDialogLabStatistics stats)
        {
            var table = new VisualElement();
            table.Add(TableHeader(stats));
            int lastDialogue = -1;
            foreach (var r in stats.WhereTurnsFail)
            {
                if (_dataset.Dialogues.Length > 1 && stats.Dialogue == null && r.DialogueIndex != lastDialogue)
                {
                    var group = new Label(_dataset.Dialogues[r.DialogueIndex].Name.ToUpperInvariant());
                    group.style.color = Faint; group.style.fontSize = 10; group.style.marginTop = 6;
                    table.Add(group);
                }
                lastDialogue = r.DialogueIndex;

                var row = TableRow();
                var user = new Label($"{r.StepIndex + 1}. {r.User}");
                user.style.flexGrow = 1; user.style.minWidth = 80;
                user.style.unityTextAlign = TextAnchor.MiddleLeft; user.style.paddingLeft = 4;
                user.style.overflow = Overflow.Hidden; user.style.textOverflow = TextOverflow.Ellipsis;
                user.style.color = SummaryUser;
                user.tooltip = r.User;
                row.Add(user);
                for (int vi = 0; vi < stats.Variants.Count; vi++)
                {
                    int failed = r.Failed[vi], seeds = r.Seeds[vi];
                    int variant = vi, flat = r.FlatIndex;
                    var cell = RatioCell(failed, seeds, $"{stats.Variants[vi].Label} · turn {r.StepIndex + 1}: {failed} of {seeds} seeds failed");
                    if (failed > 0)
                        cell.RegisterCallback<PointerDownEvent>(_ => JumpToTurn(flat, variant));
                    row.Add(cell);
                }
                table.Add(row);
            }
            if (stats.WhereTurnsFail.Count == 0) table.Add(Hint("No generated turns."));
            return table;
        }

        private VisualElement CheckTypeTable(TryllDialogLabStatistics stats)
        {
            var table = new VisualElement();
            table.Add(TableHeader(stats));
            foreach (var r in stats.ByCheckType)
            {
                var row = TableRow();
                string name = r.CheckType == TryllDialogLabChecks.GateUnexpectedToolCall
                    ? "unexpected tool call (built-in)"
                    : r.CheckType.Replace('_', ' ');
                var l = new Label(name);
                l.style.flexGrow = 1; l.style.minWidth = 80;
                l.style.unityTextAlign = TextAnchor.MiddleLeft; l.style.paddingLeft = 4;
                row.Add(l);
                for (int vi = 0; vi < stats.Variants.Count; vi++)
                    row.Add(RatioCell(r.Failed[vi], r.Evaluated[vi],
                        $"{stats.Variants[vi].Label} · {name}: {r.Failed[vi]} failed of {r.Evaluated[vi]}"));
                table.Add(row);
            }
            if (stats.ByCheckType.Count == 0) table.Add(Hint("No checks ran."));
            return table;
        }

        private VisualElement TableHeader(TryllDialogLabStatistics stats)
        {
            var head = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
            head.style.borderBottomWidth = 1; head.style.borderBottomColor = Line2;
            head.Add(new VisualElement { style = { flexGrow = 1, minWidth = 80 } });
            foreach (var v in stats.Variants)
            {
                var h = new Label(v.Label);
                h.style.width = 76; h.style.flexShrink = 0;
                h.style.unityTextAlign = TextAnchor.MiddleCenter;
                h.style.color = Dim; h.style.overflow = Overflow.Hidden; h.style.textOverflow = TextOverflow.Ellipsis;
                h.style.borderBottomWidth = 2; h.style.borderBottomColor = v.Swatch;
                head.Add(h);
            }
            return head;
        }

        /// <summary>A table body row: a hairline under it so the eye can follow a turn
        /// across the variant columns.</summary>
        private static VisualElement TableRow()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Stretch } };
            row.style.borderBottomWidth = 1; row.style.borderBottomColor = Line;
            row.style.minHeight = 22;
            return row;
        }

        private static Label RatioCell(int failed, int of, string tooltip)
        {
            var c = new Label(failed > 0 ? $"{failed}/{of}" : "·");
            c.style.width = 76; c.style.flexShrink = 0; c.style.minHeight = 21;
            c.style.borderLeftWidth = 1; c.style.borderLeftColor = Line;
            c.style.unityTextAlign = TextAnchor.MiddleCenter;
            float share = of > 0 ? (float)failed / of : 0f;
            c.style.backgroundColor = failed > 0 ? Wash(ColWarn, 0.15f + 0.6f * share) : new Color(0, 0, 0, 0);
            c.style.color = failed > 0 ? Txt : Faint;
            c.tooltip = tooltip;
            return c;
        }

        /// <summary>From a "where turns fail" cell to the Grid, on the first failing seed.</summary>
        private void JumpToTurn(int flatIndex, int variantIndex)
        {
            if (!HasResults || flatIndex >= _dataset.Turns.Length) return;
            _mode = ViewMode.Grid;
            _collapsedDialogues.Remove(_dataset.Turns[flatIndex].DialogueIndex);
            PersistLabConfig();
            string variantId = _dataset.Variants[variantIndex].Id;
            var target = _dataset.Samples
                .Where(s => s.VariantId == variantId)
                .Select(s => _cells[TryllDialogLabDataset.CellKey(s.Id, flatIndex)])
                .FirstOrDefault(c => _dataset.StatusOf(c) != LabCellStatus.Ok);
            _jumpTurn = flatIndex;
            if (target != null)
            {
                _jumpColumn = System.Array.FindIndex(_dataset.Samples, s => s.Id == target.SampleId);
                Select(target.Key);
            }
            else Rebuild();
        }

        /// <summary>Collapse/restore via the split view rather than
        /// <c>display: none</c>: the latter would leave the splitter behind and lose the
        /// dragged width. Collapsing restores to <see cref="_inspectorWidth"/>.</summary>
        private void ToggleInspector()
        {
            _inspectorVisible = !_inspectorVisible;
            if (_inspectorVisible) _bodySplit.Restore();
            else                   _bodySplit.Collapse();
            _inspToggle.text = _inspectorVisible ? "Inspector ▸" : "Inspector ◂";
            PersistLabConfig();
        }

        // ── Shared bits ────────────────────────────────────────────────────────

        /// One shared horizontal offset across the header and every turn row —
        /// this is what keeps columns aligned while bands stay full width.
        private ScrollView MakeHScroll()
        {
            var sv = new ScrollView(ScrollViewMode.Horizontal);
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.style.flexShrink = 0;
            sv.horizontalScroller.valueChanged += v => SyncH(sv, v);
            _hScrollers.Add(sv);
            return sv;
        }

        private void SyncH(ScrollView from, float value)
        {
            if (_syncing) return;
            _syncing = true;
            _hOffset = value;
            foreach (var sv in _hScrollers)
                if (sv != from) sv.horizontalScroller.value = value;
            _syncing = false;
        }

        // ── Inspector pane (shared TryllTurnInspectorView) ───

        private VisualElement BuildInspectorPane()
        {
            _inspectorView = new TryllTurnInspectorView();
            return _inspectorView.Root;
        }

        /// <summary>Hand the panel everything it draws from. Passing nulls when there are no
        /// results is deliberate — the panel owns its own empty state.</summary>
        private void RenderInspector() =>
            _inspectorView?.Render(TryllTurnInspectionFactory.FromLab(
                HasResults ? _dataset : null,
                HasResults ? _cells : null,
                _selectedKey, _pinA, _pinB,
                key =>
                {
                    if (key == "A") _pinA = null; else _pinB = null;
                    Rebuild();
                }));

        private void Select(string key)
        {
            _selectedKey = key;
            Rebuild();
        }

        private void PinColumn(string sampleId)
        {
            if (_pinA == sampleId)      _pinA = null;
            else if (_pinB == sampleId) _pinB = null;
            else if (string.IsNullOrEmpty(_pinA)) _pinA = sampleId;
            else if (string.IsNullOrEmpty(_pinB)) _pinB = sampleId;
            else { _pinA = _pinB; _pinB = sampleId; }
            Rebuild();
        }

        /// Arrow-key navigation across the grid — how you actually skim N samples.
        private void OnKeyDown(KeyDownEvent e)
        {
            if (!HasResults || string.IsNullOrEmpty(_selectedKey)) return;
            if (!_cells.TryGetValue(_selectedKey, out var c)) return;

            var samples = _dataset.Samples;
            int si = System.Array.FindIndex(samples, x => x.Id == c.SampleId);
            int ti = c.TurnIndex;

            switch (e.keyCode)
            {
                case KeyCode.RightArrow: si = Mathf.Min(samples.Length - 1, si + 1); break;
                case KeyCode.LeftArrow:  si = Mathf.Max(0, si - 1); break;
                case KeyCode.DownArrow:  ti = NextVisibleTurn(ti, +1); break;
                case KeyCode.UpArrow:    ti = NextVisibleTurn(ti, -1); break;
                default: return;
            }
            e.StopPropagation();
            Select(TryllDialogLabDataset.CellKey(samples[si].Id, ti));
        }

        /// <summary>The next turn row in <paramref name="step"/>'s direction that the Grid
        /// shows — rows of a folded dialogue are skipped. Stays put at either end.</summary>
        private int NextVisibleTurn(int from, int step)
        {
            for (int ti = from + step; ti >= 0 && ti < _dataset.Turns.Length; ti += step)
            {
                if (_mode == ViewMode.Grid && _collapsedDialogues.Contains(_dataset.Turns[ti].DialogueIndex))
                    continue;
                return ti;
            }
            return from;
        }

        void LoadLabConfig()
        {
            if (DebugSkipPersistForTests) return;
            if (!TryllDialogLabConfig.Exists())
            {
                PersistLabConfig();
                return;
            }

            var cfg = TryllDialogLabConfig.Load();
            _stage = (StageMode)Mathf.Clamp(cfg.Stage, 0, 1);
            _mode = (ViewMode)Mathf.Clamp(cfg.ViewMode, 0, 1);
            _density = Mathf.Clamp(cfg.Density, 0, 2);
            _cellW = cfg.CellWidth > 0f ? cfg.CellWidth : TryllEditorUiContract.CellWidthMedium;
            _inspectorWidth = Mathf.Max(cfg.InspectorWidth, TryllTurnInspectorView.MinWidth);
            _inspectorVisible = cfg.InspectorVisible;
            _experiment.Repeats = Mathf.Max(1, cfg.Repeats);
            _experiment.BaseSeed = Mathf.Max(1, cfg.BaseSeed);
            var source = string.IsNullOrEmpty(cfg.ScriptPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<ScriptableObject>(cfg.ScriptPath);
            _dialogSource = TryllDialogLabDialogues.IsAcceptedSource(source) ? source : null;
            _sourceAgentGlobalId = cfg.DefaultAgentId ?? "";
            _sourceAgent = ResolveAgentId(_sourceAgentGlobalId);

            var restored = new List<TryllDialogLabVariant>();
            foreach (var line in cfg.Variants)
            {
                var variant = new TryllDialogLabVariant();
                if (TryllDialogLabVariantCodec.Decode(line, variant))
                    restored.Add(variant);
            }
            if (restored.Count > 0)
                _experiment.Variants = restored;
        }

        void PersistLabConfig()
        {
            if (DebugSkipPersistForTests) return;
            if (_experiment.Variants == null || _experiment.Variants.Count == 0)
                _experiment.Variants = new List<TryllDialogLabVariant> { new TryllDialogLabVariant { Label = "Baseline" } };

            var cfg = new TryllDialogLabConfig
            {
                Stage = (int)_stage,
                ViewMode = (int)_mode,
                Density = _density,
                CellWidth = _cellW,
                InspectorWidth = _inspectorWidth,
                InspectorVisible = _inspectorVisible,
                ScriptPath = _dialogSource != null ? AssetDatabase.GetAssetPath(_dialogSource) ?? "" : "",
                DefaultAgentId = _sourceAgent != null
                    ? GlobalObjectId.GetGlobalObjectIdSlow(_sourceAgent).ToString()
                    : (_sourceAgentGlobalId ?? ""),
                Repeats = Mathf.Max(1, _experiment.Repeats),
                BaseSeed = Mathf.Max(1, _experiment.BaseSeed),
            };
            foreach (var variant in _experiment.Variants)
                cfg.Variants.Add(TryllDialogLabVariantCodec.Encode(variant));
            cfg.Save();
        }

        static TryllAgentComponent ResolveAgentId(string idString)
        {
            if (string.IsNullOrEmpty(idString))
                return null;
            if (!GlobalObjectId.TryParse(idString, out var id))
                return null;
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as TryllAgentComponent;
        }

    }
}
