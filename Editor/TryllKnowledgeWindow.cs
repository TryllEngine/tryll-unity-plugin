// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Tryll.Client;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Pull-only inspector for session mutable knowledge bases.
    /// Stores · records (filter or query) · write/export.
    /// </summary>
    public sealed class TryllKnowledgeWindow : EditorWindow
    {
        const float ContentPad = 12f;

        TryllRuntimeSettings _settings;
        TryllEditorSession _session;
        internal TryllKnowledgeWindowState State { get; } = new TryllKnowledgeWindowState();
        bool _refreshInFlight;
        bool _listing;

        TryllStatusBar _statusBar;
        TryllBanner _banner;
        Button _refreshBtn;
        ScrollView _storeScroll;
        ScrollView _recordScroll;
        Label _statsLabel;
        VisualElement _filterRow;
        VisualElement _queryRow;
        TextField _filterField;
        TextField _queryField;
        IntegerField _topKField;
        Button _filterModeBtn;
        Button _queryModeBtn;
        ScrollView _inspectorScroll;
        TextField _upsertIdField;
        TextField _upsertTextField;
        TextField _ensureNameField;
        DropdownField _ensureSchemaField;
        TextField _gamePathField;
        TextField _saveIdField;
        DropdownField _embedField;
        DropdownField _compatField;
        TextField _storeExportPathField;

        [SerializeField] string _persistStore;
        [SerializeField] string _persistRecord;

        string _ensureName = "npc.memory.nessa";
        string _ensureSchema = "npc_memory";
        string _queryText = "";
        uint _queryTopK = 4;
        string _upsertId = "";
        string _upsertText = "";
        string _storeExportPath = "memory/store.tmkb";
        string _gamePath = "memory/game.tmkb";
        string _saveId = "slot-1";
        TryllMutableKbEmbedTiming _embedTiming = TryllMutableKbEmbedTiming.Lazy;
        TryllMutableKbCompatibility _compatibility = TryllMutableKbCompatibility.Strict;

        float _storeWidth = TryllEditorUiContract.KnowledgeStoreDefaultWidth;
        float _inspectorWidth = TryllEditorUiContract.KnowledgeInspectorDefaultWidth;

        internal Func<string, string, bool> ConfirmDestructive = (title, message)
            => TryllEditorPrompts.Confirm(title, message, "Clear", "Cancel");

        [MenuItem("Window/Tryll/Knowledge")]
        public static void Open()
        {
            var w = GetWindow<TryllKnowledgeWindow>("Tryll Knowledge");
            w.minSize = new Vector2(
                TryllEditorUiContract.PanelMinWidth,
                TryllEditorUiContract.PanelMinHeight);
        }

        void OnEnable()
        {
            _settings = TryllEditorSettingsLookup.DebugOverrideForTests ?? TryllRuntimeSettings.Load();
            TryllEditorTheme.HostColorsChanged += OnHostColorsChanged;
        }

        internal TryllEditorSession DebugSession => _session;
        internal bool DebugBusy => _refreshInFlight;

        /// <summary>Press Refresh as a click would.</summary>
        internal Task DebugRefreshForTests() => RefreshAsync();

        /// <summary>Fill the Ensure fields and press Ensure.</summary>
        internal Task DebugEnsureForTests(string name, string schema)
        {
            _ensureName = name;
            _ensureSchema = schema;
            return EnsureAsync();
        }

        internal void DebugSelectStoreForTests(string name) => OnSelectStore(name);

        /// <summary>Fill the upsert fields and press Write on the selected store.</summary>
        internal Task DebugWriteForTests(string id, string text)
        {
            _upsertId = id;
            _upsertText = text;
            return WriteAsync();
        }

        /// <summary>Press "Clear store" as a click would; the confirm goes through <see cref="ConfirmDestructive"/>.</summary>
        internal Task DebugClearForTests() => ClearAsync();

        /// <summary>Rows are store names; the banner is the joined stale / warning text.</summary>
        internal TryllEditorWindowSnapshot DebugSnapshot()
        {
            var rows = new List<string>();
            foreach (var store in State.Stores) rows.Add(store.Name);
            return new TryllEditorWindowSnapshot(
                _session?.OwnerPhase ?? TryllOwnerPhase.Idle,
                _session?.CurrentState.ToString() ?? "None",
                _statusBar?.DebugCaption,
                _statusBar?.DebugActionText,
                _banner?.DebugText,
                rows,
                _session?.HoldsLease ?? false,
                State.RefreshGeneration);
        }

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            root.style.flexDirection = FlexDirection.Column;
            TryllEditorTheme.Attach(root, paintWindowBackground: false);

            LoadLayoutPrefs();
            RestorePersistedSelection();

            root.Add(BuildToolbar());
            _banner = new TryllBanner();
            root.Add(_banner);

            var inner = new TryllInspectorSplit(
                BuildCenter(),
                BuildRight(),
                TryllSplitFixedSide.Second,
                () => TryllEditorUiContract.KnowledgeRecordsMinWidth,
                () => TryllEditorUiContract.KnowledgeInspectorMinWidth,
                TryllEditorUiContract.KnowledgeInspectorDefaultWidth,
                () => _inspectorWidth,
                PersistInspectorWidth,
                () => true);

            var outer = new TryllInspectorSplit(
                BuildLeft(),
                inner,
                TryllSplitFixedSide.First,
                () => TryllEditorUiContract.KnowledgeStoreMinWidth,
                () => TryllEditorUiContract.KnowledgeRecordsMinWidth
                    + TryllInspectorSplit.HandleWidth
                    + TryllEditorUiContract.KnowledgeInspectorMinWidth,
                TryllEditorUiContract.KnowledgeStoreDefaultWidth,
                () => _storeWidth,
                PersistStoreWidth,
                () => true);
            root.Add(outer);

            EnsureStarted();
            RefreshChrome();
            RefreshLists();
        }

        void OnDisable()
        {
            TryllEditorTheme.HostColorsChanged -= OnHostColorsChanged;
            State.BeginRefresh();
            _refreshInFlight = false;
            _listing = false;
            if (_session != null)
            {
                _session.StateChanged -= OnSessionStateChanged;
                _session.SessionReady -= OnSessionReady;
                _session.Stop();
                _session = null;
            }
        }

        void OnHostColorsChanged()
        {
            if (_banner == null) return;
            RefreshChrome();
            RefreshLists();
        }

        static string LayoutPrefix() => TryllEditorPrefs.ProjectPrefix("Knowledge");

        void LoadLayoutPrefs()
        {
            string prefix = LayoutPrefix();
            _storeWidth = Mathf.Max(
                TryllEditorUiContract.KnowledgeStoreMinWidth,
                EditorPrefs.GetFloat(prefix + "StoreWidth",
                    TryllEditorUiContract.KnowledgeStoreDefaultWidth));
            _inspectorWidth = Mathf.Max(
                TryllEditorUiContract.KnowledgeInspectorMinWidth,
                EditorPrefs.GetFloat(prefix + "InspectorWidth",
                    TryllEditorUiContract.KnowledgeInspectorDefaultWidth));
        }

        void PersistStoreWidth(float width)
        {
            if (!TryllInspectorSplitMath.ShouldPersistWidth(
                    width, TryllEditorUiContract.KnowledgeStoreMinWidth))
                return;
            _storeWidth = width;
            EditorPrefs.SetFloat(LayoutPrefix() + "StoreWidth", width);
        }

        void PersistInspectorWidth(float width)
        {
            if (!TryllInspectorSplitMath.ShouldPersistWidth(
                    width, TryllEditorUiContract.KnowledgeInspectorMinWidth))
                return;
            _inspectorWidth = width;
            EditorPrefs.SetFloat(LayoutPrefix() + "InspectorWidth", width);
        }

        void RestorePersistedSelection()
        {
            if (!string.IsNullOrEmpty(_persistStore))
                State.SelectStore(_persistStore);
            if (!string.IsNullOrEmpty(_persistRecord))
                State.SelectedRecordId = _persistRecord;
        }

        void EnsureStarted()
        {
            if (_session != null && _session.CurrentState == TryllEditorSession.State.Ready)
                return;
            if (_session == null)
            {
                _session = new TryllEditorSession(_settings, requireRegisteredModels: false);
                _session.StateChanged += OnSessionStateChanged;
                _session.SessionReady += OnSessionReady;
            }
            if (_session.CurrentState == TryllEditorSession.State.Idle
                || _session.CurrentState == TryllEditorSession.State.Error)
                _session.Start();
        }

        void OnSessionStateChanged(TryllEditorSession.State state, string status)
        {
            RefreshChrome();
        }

        void OnSessionReady()
        {
            RefreshChrome();
        }

        VisualElement BuildToolbar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.paddingLeft = 8; bar.style.paddingRight = 8;
            bar.style.paddingTop = 5; bar.style.paddingBottom = 5;
            bar.style.borderBottomWidth = 1; bar.style.borderBottomColor = Hairline;
            bar.style.flexShrink = 0;

            var title = new Label("Tryll Knowledge");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            bar.Add(title);

            var note = new Label("live · session stores");
            note.style.color = Dim; note.style.marginLeft = 8; note.style.fontSize = 11;
            bar.Add(note);

            var spacer = new VisualElement(); spacer.style.flexGrow = 1; bar.Add(spacer);

            _statusBar = new TryllStatusBar();
            _statusBar.style.marginRight = 10;
            bar.Add(_statusBar);

            _refreshBtn = new Button(() => _ = RefreshAsync()) { text = "Refresh" };
            _refreshBtn.tooltip = "Reload the session store list and the selected store's records. Knowledge never polls.";
            bar.Add(_refreshBtn);
            return bar;
        }

        VisualElement BuildLeft()
        {
            var col = new VisualElement();
            col.style.flexGrow = 1;
            col.style.backgroundColor = PanelTint;
            col.style.borderRightWidth = 1;
            col.style.borderRightColor = Hairline;

            col.Add(PanelHeader("Stores"));

            _storeScroll = new ScrollView(ScrollViewMode.Vertical);
            _storeScroll.style.flexGrow = 1;
            col.Add(_storeScroll);

            var footer = new VisualElement();
            footer.style.flexShrink = 0;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = Hairline;

            var ensure = KnowledgeSection("Ensure store");
            ensure.Add(Hint("Create the named store if it is missing. Schema must exist in the bundled catalog."));
            _ensureNameField = LabeledText(ensure, "Name", _ensureName, v => _ensureName = v,
                "Store name, e.g. npc.memory.nessa");
            _ensureSchemaField = LabeledDropdown(ensure, "Schema", SchemaChoices(), _ensureSchema, v => _ensureSchema = v,
                "Catalog schema. Server validation stays authoritative.");
            var ensureBtn = new Button(() => _ = EnsureAsync()) { text = "Ensure" };
            ensureBtn.tooltip = "Get-or-create this store in the current session.";
            FlattenControl(ensureBtn);
            ensureBtn.style.marginTop = 6;
            ensure.Add(ensureBtn);
            footer.Add(ensure);

            var session = KnowledgeSection("Session file");
            session.Add(Hint("Save or load every store in this session. Paths are storage-root relative."));
            _gamePathField = LabeledText(session, "Path", _gamePath, v => _gamePath = v,
                "Storage-root-relative TMKB path, e.g. memory/game.tmkb");
            _saveIdField = LabeledText(session, "Save id", _saveId, v => _saveId = v,
                "Caller save_id stamped beside the snapshot.");
            _embedField = LabeledDropdown(session, "Embed timing",
                new List<string> { "Lazy", "Now" },
                _embedTiming == TryllMutableKbEmbedTiming.Now ? "Now" : "Lazy",
                v => _embedTiming = v == "Now" ? TryllMutableKbEmbedTiming.Now : TryllMutableKbEmbedTiming.Lazy,
                "Required on load. Lazy rebuilds vectors on first use; Now embeds immediately.");
            _compatField = LabeledDropdown(session, "Compatibility",
                new List<string> { "Strict", "Allow schema drift" },
                _compatibility == TryllMutableKbCompatibility.AllowSchemaDrift ? "Allow schema drift" : "Strict",
                v => _compatibility = v == "Allow schema drift"
                    ? TryllMutableKbCompatibility.AllowSchemaDrift
                    : TryllMutableKbCompatibility.Strict,
                "Strict rejects schema mismatches. Allow schema drift imports with a warning.");
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 6;
            var saveBtn = new Button(() => _ = SaveGameAsync()) { text = "Save game memory" };
            saveBtn.tooltip = "Write a session TMKB to the storage-root-relative path.";
            FlattenControl(saveBtn);
            saveBtn.style.flexGrow = 1;
            var loadBtn = new Button(() => _ = LoadGameAsync()) { text = "Load game memory" };
            loadBtn.tooltip = "Replace session stores from that path. Embed timing is required.";
            FlattenControl(loadBtn);
            loadBtn.style.flexGrow = 1;
            row.Add(saveBtn);
            row.Add(loadBtn);
            session.Add(row);
            footer.Add(session);

            col.Add(footer);
            return col;
        }

        VisualElement BuildCenter()
        {
            var col = new VisualElement();
            col.style.flexGrow = 1;

            var head = new VisualElement();
            head.style.flexShrink = 0;
            head.style.paddingLeft = ContentPad; head.style.paddingRight = ContentPad;
            head.style.paddingTop = 6; head.style.paddingBottom = 6;
            head.style.borderBottomWidth = 1; head.style.borderBottomColor = Hairline;

            _statsLabel = new Label();
            _statsLabel.style.fontSize = 11;
            _statsLabel.style.color = Dim;
            _statsLabel.style.whiteSpace = WhiteSpace.Normal;
            head.Add(_statsLabel);

            var modes = new VisualElement();
            modes.style.flexDirection = FlexDirection.Row;
            modes.style.marginTop = 6;
            _filterModeBtn = new Button(() => SetBrowseMode(TryllKnowledgeBrowseMode.Filter)) { text = "Filter" };
            _filterModeBtn.tooltip = TryllEditorUiContract.KnowledgeFilterHint;
            FlattenControl(_filterModeBtn);
            _filterModeBtn.style.flexGrow = 1;
            _queryModeBtn = new Button(() => SetBrowseMode(TryllKnowledgeBrowseMode.Query)) { text = "Query" };
            _queryModeBtn.tooltip = TryllEditorUiContract.KnowledgeQueryHint;
            FlattenControl(_queryModeBtn);
            _queryModeBtn.style.flexGrow = 1;
            modes.Add(_filterModeBtn);
            modes.Add(_queryModeBtn);
            head.Add(modes);

            _filterRow = new VisualElement();
            _filterRow.style.marginTop = 6;
            _filterField = LabeledText(_filterRow, "Filter", State.Filter, v =>
            {
                State.Filter = v;
                RefreshRecordList();
            }, TryllEditorUiContract.KnowledgeFilterHint);
            _filterRow.Add(Hint(TryllEditorUiContract.KnowledgeFilterHint));
            head.Add(_filterRow);

            _queryRow = new VisualElement();
            _queryRow.style.marginTop = 6;
            _queryField = LabeledText(_queryRow, "Query", _queryText, v => _queryText = v,
                TryllEditorUiContract.KnowledgeQueryHint);
            _topKField = new IntegerField("Top K") { value = (int)_queryTopK };
            _topKField.tooltip = "How many nearest records to return.";
            FlattenControl(_topKField);
            _topKField.RegisterValueChangedCallback(e =>
            {
                _queryTopK = (uint)Mathf.Max(1, e.newValue);
                if (e.newValue < 1) _topKField.SetValueWithoutNotify(1);
            });
            _queryRow.Add(_topKField);
            var run = new Button(() => _ = QueryAsync()) { text = "Run query" };
            run.tooltip = TryllEditorUiContract.KnowledgeQueryHint;
            FlattenControl(run);
            run.style.marginTop = 4;
            _queryRow.Add(run);
            _queryRow.Add(Hint(TryllEditorUiContract.KnowledgeQueryHint));
            head.Add(_queryRow);

            col.Add(head);

            _recordScroll = new ScrollView(ScrollViewMode.Vertical);
            _recordScroll.style.flexGrow = 1;
            col.Add(_recordScroll);
            return col;
        }

        VisualElement BuildRight()
        {
            var col = new VisualElement();
            col.style.flexGrow = 1;
            col.style.backgroundColor = PanelTint;
            col.style.borderLeftWidth = 1;
            col.style.borderLeftColor = Hairline;
            col.Add(PanelHeader("Modify"));

            _inspectorScroll = new ScrollView(ScrollViewMode.Vertical);
            _inspectorScroll.style.flexGrow = 1;
            col.Add(_inspectorScroll);
            var intro = Hint(TryllEditorUiContract.KnowledgeSelectRecord);
            intro.style.marginLeft = ContentPad;
            intro.style.marginRight = ContentPad;
            intro.style.marginTop = 12;
            _inspectorScroll.Add(intro);

            var add = KnowledgeSection("Add / update");
            add.Add(Hint("Empty Id lets the server generate one. Write upserts that text into the selected store."));
            _upsertIdField = LabeledText(add, "Id", _upsertId, v => _upsertId = v,
                "Record id. Leave empty to generate.");
            _upsertTextField = new TextField("Text") { value = _upsertText, multiline = true };
            _upsertTextField.tooltip = "Standalone memory sentence stored as the record text.";
            FlattenControl(_upsertTextField);
            _upsertTextField.style.minHeight = 72;
            _upsertTextField.style.whiteSpace = WhiteSpace.Normal;
            _upsertTextField.RegisterValueChangedCallback(e => _upsertText = e.newValue ?? "");
            add.Add(_upsertTextField);
            var write = new Button(() => _ = WriteAsync()) { text = "Write" };
            write.tooltip = "Upsert this id/text into the selected store.";
            FlattenControl(write);
            write.style.marginTop = 6;
            add.Add(write);
            _inspectorScroll.Add(add);

            var danger = KnowledgeSection("Remove");
            danger.Add(Hint("Delete removes the selected record. Clear wipes every live record in the store."));
            var del = new Button(() => _ = DeleteSelectedAsync()) { text = "Delete selected" };
            del.tooltip = "Delete the highlighted record. The store itself remains.";
            FlattenControl(del);
            var clear = new Button(() => _ = ClearAsync()) { text = "Clear store" };
            clear.tooltip = "Delete every live record. Asks for confirmation.";
            FlattenControl(clear);
            clear.style.marginTop = 4;
            danger.Add(del);
            danger.Add(clear);
            _inspectorScroll.Add(danger);

            var snap = KnowledgeSection("Store snapshot");
            snap.Add(Hint("Export or import one store as a TMKB file. This is not the whole-session save on the left."));
            _storeExportPathField = LabeledText(snap, "Hint name", _storeExportPath, v => _storeExportPath = v,
                "Suggested filename when the save dialog opens.");
            var io = new VisualElement();
            io.style.flexDirection = FlexDirection.Row;
            io.style.marginTop = 6;
            var exp = new Button(() => _ = ExportStoreAsync()) { text = "Export TMKB" };
            exp.tooltip = "Download this store's snapshot to a .tmkb file.";
            FlattenControl(exp);
            exp.style.flexGrow = 1;
            var imp = new Button(() => _ = ImportStoreAsync()) { text = "Import TMKB" };
            imp.tooltip = "Replace this store from a .tmkb file. Snapshot name must match.";
            FlattenControl(imp);
            imp.style.flexGrow = 1;
            io.Add(exp);
            io.Add(imp);
            snap.Add(io);
            _inspectorScroll.Add(snap);

            return col;
        }

        void SetBrowseMode(TryllKnowledgeBrowseMode mode)
        {
            State.SetMode(mode);
            RefreshRecordList();
            RefreshModeChrome();
        }

        void RefreshChrome()
        {
            bool ready = _session != null && _session.CurrentState == TryllEditorSession.State.Ready;
            _refreshBtn?.SetEnabled(ready && !_refreshInFlight);
            if (_statusBar == null) return;

            var owner = _session?.OwnerPhase ?? TryllOwnerPhase.Idle;
            var local = _refreshInFlight
                ? (_listing ? TryllPanelOperation.Listing : TryllPanelOperation.Refreshing)
                : TryllPanelOperation.None;
            var pres = TryllStatusPresentation.Compose(
                owner, _session?.OwnerDetail, local, TryllEditorPanel.Knowledge);
            _statusBar.Bind(pres, pres.Action == TryllStatusActionIntent.Retry
                ? () => EnsureStarted()
                : null);
            RefreshBanner();
        }

        void RefreshBanner()
        {
            if (_banner == null) return;
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(State.StaleBanner))
            {
                parts.Add(State.HasLastGoodView
                    ? "Showing the last good view.\n" + State.StaleBanner
                    : State.StaleBanner);
            }
            if (State.ListTruncated)
                parts.Add(TryllEditorUiContract.FormatKnowledgeTruncated((int)TryllKnowledgeWindowState.ListLimit));
            foreach (var w in State.Warnings)
            {
                if (!string.IsNullOrEmpty(w)) parts.Add(w);
            }
            if (parts.Count == 0)
            {
                _banner.Dismiss();
                return;
            }
            var severity = !string.IsNullOrEmpty(State.StaleBanner)
                ? TryllStatusSeverity.Warning
                : TryllStatusSeverity.Warning;
            _banner.Bind(string.Join("\n\n", parts), severity);
        }

        void RefreshLists()
        {
            RefreshStoreList();
            RefreshRecordList();
            RefreshModeChrome();
            RefreshBanner();
            SyncInspectorFields();
        }

        void RefreshModeChrome()
        {
            bool filter = State.Mode == TryllKnowledgeBrowseMode.Filter;
            if (_filterRow != null) _filterRow.style.display = filter ? DisplayStyle.Flex : DisplayStyle.None;
            if (_queryRow != null) _queryRow.style.display = filter ? DisplayStyle.None : DisplayStyle.Flex;
            StyleModeButton(_filterModeBtn, filter);
            StyleModeButton(_queryModeBtn, !filter);
        }

        static void StyleModeButton(Button btn, bool on)
        {
            if (btn == null) return;
            ApplySelected(btn, on);
            btn.style.unityFontStyleAndWeight = on ? FontStyle.Bold : FontStyle.Normal;
        }

        void RefreshStoreList()
        {
            if (_storeScroll == null) return;
            _storeScroll.Clear();
            if (State.Stores.Count == 0)
            {
                _storeScroll.Add(CenteredEmpty("No stores. Click Refresh or Ensure below."));
                return;
            }
            foreach (var store in State.Stores)
            {
                if (store == null) continue;
                bool selected = store.Name == State.SelectedName;
                var btn = new Button(() => OnSelectStore(store.Name));
                btn.style.unityTextAlign = TextAnchor.MiddleLeft;
                btn.style.whiteSpace = WhiteSpace.Normal;
                btn.style.marginLeft = 6; btn.style.marginRight = 6; btn.style.marginBottom = 4;
                btn.text = $"{store.Name}\n{store.Schema}  ·  {store.LiveCount}  ·  gen {store.WriteGeneration}"
                    + (store.Materialized ? "  ·  materialized" : "  ·  lazy");
                ApplySelected(btn, selected);
                _storeScroll.Add(btn);
            }
        }

        void RefreshRecordList()
        {
            if (_recordScroll == null) return;
            _recordScroll.Clear();

            if (State.Stats != null)
            {
                _statsLabel.text =
                    (string.IsNullOrEmpty(State.SelectedName) ? "Records" : $"Records — {State.SelectedName}")
                    + $"\nlive {State.Stats.LiveCount}  ·  tombstones {State.Stats.TombstonedCount}  ·  gen {State.Stats.WriteGeneration}  ·  "
                    + (State.Stats.Materialized ? "materialized" : "lazy")
                    + $"  ·  ~{State.Stats.BytesEstimate} bytes";
            }
            else if (!string.IsNullOrEmpty(State.SelectedName))
            {
                _statsLabel.text = $"Records — {State.SelectedName}\nLoading…";
            }
            else
            {
                _statsLabel.text = TryllEditorUiContract.KnowledgeSelectStore;
            }

            var rows = State.VisibleRows;
            if (rows.Count == 0)
            {
                string empty = string.IsNullOrEmpty(State.SelectedName)
                    ? TryllEditorUiContract.KnowledgeSelectStore
                    : State.Mode == TryllKnowledgeBrowseMode.Query
                        ? "No query results. Enter text and Run query."
                        : "No records in this store.";
                _recordScroll.Add(CenteredEmpty(empty));
                return;
            }

            foreach (var row in rows)
            {
                bool selected = row.Id == State.SelectedRecordId;
                var captured = row;
                var btn = new Button(() => OnSelectRecord(captured));
                btn.style.unityTextAlign = TextAnchor.MiddleLeft;
                btn.style.whiteSpace = WhiteSpace.Normal;
                btn.style.marginLeft = ContentPad; btn.style.marginRight = ContentPad; btn.style.marginBottom = 4;
                btn.text = captured.Distance.HasValue
                    ? $"{captured.Id}  d={captured.Distance.Value:0.000}\n{captured.Text}"
                    : $"{captured.Id}\n{captured.Text}";
                ApplySelected(btn, selected);
                _recordScroll.Add(btn);
            }
        }

        void OnSelectStore(string name)
        {
            State.SelectStore(name);
            _persistStore = name;
            _persistRecord = "";
            RefreshLists();
            _ = LoadSelectedStoreAsync();
        }

        async Task LoadSelectedStoreAsync()
        {
            int generation = State.BeginRefresh();
            _refreshInFlight = true;
            _listing = true;
            RefreshChrome();
            await RefreshSelectedListAsync(generation);
            if (!State.IsCurrent(generation)) return;
            _listing = false;
            _refreshInFlight = false;
            RefreshChrome();
            RefreshLists();
        }

        void OnSelectRecord(TryllKnowledgeVisibleRow row)
        {
            State.SelectedRecordId = row.Id;
            _persistRecord = row.Id;
            _upsertId = row.Id;
            _upsertText = row.Text;
            SyncInspectorFields();
            RefreshRecordList();
        }

        void SyncInspectorFields()
        {
            _upsertIdField?.SetValueWithoutNotify(_upsertId);
            _upsertTextField?.SetValueWithoutNotify(_upsertText);
            _ensureNameField?.SetValueWithoutNotify(_ensureName);
            _ensureSchemaField?.SetValueWithoutNotify(_ensureSchema);
            _gamePathField?.SetValueWithoutNotify(_gamePath);
            _saveIdField?.SetValueWithoutNotify(_saveId);
            _storeExportPathField?.SetValueWithoutNotify(_storeExportPath);
            _filterField?.SetValueWithoutNotify(State.Filter);
            _queryField?.SetValueWithoutNotify(_queryText);
        }

        TryllClient Client => _session?.Client;

        async Task RefreshAsync()
        {
            if (Client == null || !Client.IsSessionReady || _refreshInFlight) return;
            _refreshInFlight = true;
            _listing = false;
            int generation = State.BeginRefresh();
            State.ClearWarnings();
            RefreshChrome();
            var (stores, error) = await EditorCall(() => Client.EnumerateMutableKbsAsync());
            if (!State.IsCurrent(generation)) return;
            if (!error.IsOk)
            {
                _refreshInFlight = false;
                SurfaceError(error);
                RefreshChrome();
                RefreshLists();
                return;
            }
            State.ApplyEnumerate(stores);
            if (!string.IsNullOrEmpty(State.SelectedName))
                _persistStore = State.SelectedName;
            _listing = true;
            RefreshChrome();
            await RefreshSelectedListAsync(generation);
            if (!State.IsCurrent(generation)) return;
            _listing = false;
            _refreshInFlight = false;
            RefreshChrome();
            RefreshLists();
        }

        Task<T> EditorCall<T>(System.Func<Task<T>> start)
        {
            using (Client.AsEditorCall())
                return start();
        }

        async Task RefreshSelectedListAsync(int generation = 0)
        {
            if (Client == null || !Client.IsSessionReady) return;
            if (string.IsNullOrEmpty(State.SelectedName)) return;
            if (generation == 0) generation = State.RefreshGeneration;
            var store = await EditorCall(() => Client.EnsureMutableKbAsync(State.SelectedName, SchemaOf(State.SelectedName)));
            if (!State.IsCurrent(generation)) return;
            if (!store.error.IsOk)
            {
                SurfaceError(store.error);
                RefreshLists();
                return;
            }
            var (page, listErr) = await EditorCall(() => store.store.ListAsync(0, TryllKnowledgeWindowState.ListLimit));
            if (!State.IsCurrent(generation)) return;
            if (!listErr.IsOk)
            {
                SurfaceError(listErr);
                RefreshLists();
                return;
            }
            var (stats, statsErr) = await EditorCall(() => store.store.StatsAsync());
            if (!State.IsCurrent(generation)) return;
            if (!statsErr.IsOk)
            {
                SurfaceError(statsErr);
                RefreshLists();
                return;
            }
            State.ApplyList(page, stats);
        }

        async Task EnsureAsync()
        {
            if (Client == null || !Client.IsSessionReady) return;
            var (store, error) = await EditorCall(() => Client.EnsureMutableKbAsync(_ensureName, _ensureSchema));
            if (!error.IsOk) { SurfaceError(error); RefreshChrome(); RefreshLists(); return; }
            State.SelectStore(store.Name);
            _persistStore = store.Name;
            await RefreshAsync();
        }

        async Task QueryAsync()
        {
            var handle = await RequireSelected();
            if (handle == null) return;
            State.ClearWarnings();
            var (hits, error) = await EditorCall(() => handle.QueryAsync(_queryText, _queryTopK));
            if (!error.IsOk) { SurfaceError(error); RefreshChrome(); RefreshLists(); return; }
            State.ApplyQueryHits(hits);
            if (hits == null || hits.Count == 0)
                State.AddWarning("No matching records.");
            RefreshLists();
        }

        async Task WriteAsync()
        {
            var handle = await RequireSelected();
            if (handle == null) return;
            var (results, error) = await EditorCall(() => handle.WriteAsync(new[]
            {
                new TryllMutableKbUpsert(_upsertId, _upsertText),
            }));
            if (!error.IsOk) { SurfaceError(error); RefreshChrome(); RefreshLists(); return; }
            if (results != null)
            {
                foreach (var row in results)
                {
                    if (!row.Accepted)
                        State.AddWarning(string.IsNullOrEmpty(row.Error) ? "Write rejected" : row.Error);
                    else if (!string.IsNullOrEmpty(row.Id))
                        _upsertId = row.Id;
                }
            }
            await RefreshAsync();
        }

        async Task DeleteSelectedAsync()
        {
            if (string.IsNullOrEmpty(State.SelectedRecordId)) return;
            var handle = await RequireSelected();
            if (handle == null) return;
            var (_, error) = await EditorCall(() => handle.DeleteAsync(new[] { State.SelectedRecordId }));
            if (!error.IsOk) { SurfaceError(error); RefreshChrome(); RefreshLists(); return; }
            await RefreshAsync();
        }

        async Task ClearAsync()
        {
            if (!TryllKnowledgeWindowState.IsDestructiveClearConfirmed(
                    ConfirmDestructive("Clear store",
                        $"Delete every live record in '{State.SelectedName}'?")))
                return;
            var handle = await RequireSelected();
            if (handle == null) return;
            var (_, error) = await EditorCall(() => handle.ClearAsync());
            if (!error.IsOk) { SurfaceError(error); RefreshChrome(); RefreshLists(); return; }
            await RefreshAsync();
        }

        async Task ExportStoreAsync()
        {
            var handle = await RequireSelected();
            if (handle == null) return;
            var (snapshot, error) = await EditorCall(() => handle.ExportAsync());
            if (!error.IsOk) { SurfaceError(error); RefreshChrome(); RefreshLists(); return; }
            try
            {
                var bytes = snapshot.ToFlatBufferBytes();
                var dest = TryllEditorPrompts.SaveFilePanel("Export TMKB", "", Path.GetFileName(_storeExportPath), "tmkb");
                if (string.IsNullOrEmpty(dest)) return;
                File.WriteAllBytes(dest, bytes);
                _storeExportPath = Path.GetFileName(dest);
            }
            catch (Exception ex)
            {
                State.AddWarning(ex.Message);
            }
            RefreshLists();
        }

        async Task ImportStoreAsync()
        {
            var handle = await RequireSelected();
            if (handle == null) return;
            var src = TryllEditorPrompts.OpenFilePanel("Import TMKB", "", "tmkb");
            if (string.IsNullOrEmpty(src)) return;
            try
            {
                var snapshot = TryllMutableKbSnapshot.FromFlatBufferBytes(File.ReadAllBytes(src));
                var (result, error) = await EditorCall(() => handle.ImportAsync(snapshot, _compatibility));
                if (!error.IsOk) { SurfaceError(error, result?.Warnings); RefreshChrome(); RefreshLists(); return; }
                var formatted = TryllKnowledgeWindowState.FormatWarning(TryllError.Ok(), result?.Warnings);
                if (!string.IsNullOrEmpty(formatted))
                    State.AddWarning(formatted);
            }
            catch (Exception ex)
            {
                State.AddWarning(ex.Message);
            }
            await RefreshAsync();
        }

        async Task SaveGameAsync()
        {
            if (Client == null || !Client.IsSessionReady) return;
            var (result, error) = await EditorCall(() => Client.SaveGameMemoryAsync(_gamePath, _saveId));
            if (!error.IsOk) { SurfaceError(error); RefreshChrome(); RefreshLists(); return; }
            State.AddWarning($"Saved {result.StoreCount} stores at {result.SavedAtMs} (path {_gamePath}).");
            RefreshLists();
        }

        async Task LoadGameAsync()
        {
            if (Client == null || !Client.IsSessionReady) return;
            var (result, error) = await EditorCall(() => Client.LoadGameMemoryAsync(_gamePath, _embedTiming, _compatibility));
            if (!error.IsOk) { SurfaceError(error, result?.Warnings); RefreshChrome(); RefreshLists(); return; }
            var formatted = TryllKnowledgeWindowState.FormatWarning(TryllError.Ok(), result?.Warnings);
            if (!string.IsNullOrEmpty(formatted))
                State.AddWarning(formatted);
            await RefreshAsync();
        }

        async Task<TryllMutableKb> RequireSelected()
        {
            if (Client == null || !Client.IsSessionReady || string.IsNullOrEmpty(State.SelectedName))
                return null;
            var (store, error) = await EditorCall(() => Client.EnsureMutableKbAsync(State.SelectedName, SchemaOf(State.SelectedName)));
            if (!error.IsOk)
            {
                SurfaceError(error);
                RefreshChrome();
                RefreshLists();
                return null;
            }
            return store;
        }

        string SchemaOf(string name)
        {
            foreach (var store in State.Stores)
            {
                if (store != null && store.Name == name && !string.IsNullOrEmpty(store.Schema))
                    return store.Schema;
            }
            return _ensureSchema;
        }

        void SurfaceError(TryllError error, IReadOnlyList<string> extra = null)
        {
            var text = TryllKnowledgeWindowState.FormatWarning(error, extra);
            if (TryllKnowledgeWindowState.ShouldSurfaceBusy(error) || State.HasLastGoodView)
                State.MarkStale(text);
            else
                State.AddWarning(text);
        }

        static List<string> SchemaChoices()
        {
            var catalog = TryllSchemaCatalog.LoadBundled();
            var names = new List<string>();
            if (catalog?.Names != null)
                names.AddRange(catalog.Names);
            if (names.Count == 0)
                names.Add("npc_memory");
            return names;
        }

        static VisualElement PanelHeader(string text)
        {
            var h = new Label(text.ToUpperInvariant());
            h.style.fontSize = 10; h.style.color = Dim;
            h.style.paddingLeft = ContentPad; h.style.paddingRight = ContentPad;
            h.style.paddingTop = 7; h.style.paddingBottom = 7;
            h.style.borderBottomWidth = 1; h.style.borderBottomColor = Hairline;
            h.style.unityFontStyleAndWeight = FontStyle.Bold;
            return h;
        }

        static VisualElement KnowledgeSection(string title)
        {
            var s = new VisualElement();
            s.style.paddingLeft = ContentPad;
            s.style.paddingRight = ContentPad;
            s.style.paddingTop = 12;
            s.style.paddingBottom = 12;
            s.style.borderBottomWidth = 1;
            s.style.borderBottomColor = Hairline;
            if (title != null)
            {
                var h = new Label(title.ToUpperInvariant());
                h.style.fontSize = 10;
                h.style.color = Dim;
                h.style.marginBottom = 8;
                h.style.unityFontStyleAndWeight = FontStyle.Bold;
                s.Add(h);
            }
            return s;
        }

        static Label Hint(string text)
        {
            var l = Muted(text);
            FlattenControl(l);
            return l;
        }

        static VisualElement CenteredEmpty(string text)
        {
            var l = new Label(text);
            l.style.color = Faint;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = 24;
            l.style.marginLeft = ContentPad;
            l.style.marginRight = ContentPad;
            return l;
        }

        static void ApplySelected(VisualElement el, bool selected)
        {
            el.style.backgroundColor = selected ? SelBub : StyleKeyword.Null;
        }

        static void FlattenControl(VisualElement el)
        {
            el.style.marginLeft = 0;
            el.style.marginRight = 0;
            el.style.paddingLeft = 0;
            el.style.paddingRight = 0;
            if (el is BaseField<string> text)
                AlignFieldLabel(text);
            else if (el is BaseField<int> number)
                AlignFieldLabel(number);
        }

        static void AlignFieldLabel<T>(BaseField<T> field)
        {
            if (field.labelElement == null) return;
            field.labelElement.style.minWidth = 72;
            field.labelElement.style.width = 72;
        }

        static TextField LabeledText(VisualElement parent, string label, string value, Action<string> onChange, string tooltip)
        {
            var field = new TextField(label) { value = value };
            field.tooltip = tooltip;
            FlattenControl(field);
            field.RegisterValueChangedCallback(e => onChange(e.newValue ?? ""));
            parent.Add(field);
            return field;
        }

        static DropdownField LabeledDropdown(
            VisualElement parent, string label, List<string> choices, string value,
            Action<string> onChange, string tooltip)
        {
            if (!choices.Contains(value) && !string.IsNullOrEmpty(value))
                choices.Insert(0, value);
            var field = new DropdownField(label, choices, choices.IndexOf(value) >= 0 ? choices.IndexOf(value) : 0);
            field.tooltip = tooltip;
            FlattenControl(field);
            field.RegisterValueChangedCallback(e => onChange(e.newValue));
            parent.Add(field);
            return field;
        }
    }
}
