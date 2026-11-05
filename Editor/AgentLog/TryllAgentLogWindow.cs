// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tryll.Client;
using Tryll.Client.Internal;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor.AgentLog
{
    /// <summary>
    /// Editor-only Agent Log window (Option 3 layout: agent list │ timeline │ inspector).
    /// </summary>
    public sealed class TryllAgentLogWindow : EditorWindow
    {
        // R11.4: spec calls for two row heights (1× / ~1.5×). Unity ListView exposes only one
        // fixedItemHeight, so we use the taller height for every row and draw a top separator on
        // first-of-turn rows instead of dual heights.
        const float TimelineRowHeight = 28f;
        const float AgentListMinWidth = TryllEditorUiContract.AgentListMinWidth;
        const float AgentListRowHeight = 38f;
        const float TimelineMinWidth = TryllEditorUiContract.TimelineMinWidth;
        const float ChevronColumnWidth = 14f;
        const float MarkerColumnWidth = 14f;
        const float TimestampColumnWidth = 84f; // HH:mm:ss.fff
        const float AgentGutterWidth = 108f;
        const float ReceivedIndentPx = 8f;

        static readonly Color SentRowBg = new Color(0.180f, 0.280f, 0.380f, 0.35f);
        static readonly Color ReceivedRowBg = new Color(0.160f, 0.160f, 0.180f, 0.55f);
        static readonly Color SystemRowBg = new Color(0.130f, 0.130f, 0.135f, 0.65f);
        static readonly Color SelectedRowBg = new Color(0.240f, 0.320f, 0.420f, 0.55f);
        static readonly Color TurnSeparatorColor = new Color(0.290f, 0.306f, 0.333f, 0.9f);

        AgentLogStore _store;
        AgentLogConfig _config;
        AgentLogFilter _filter = new AgentLogFilter { AllAgents = true };
        readonly AgentLogTurnCollapseState _turnCollapse = new AgentLogTurnCollapseState();
        readonly List<AgentLogRow> _projectedRows = new List<AgentLogRow>();
        readonly List<AgentListEntry> _agentEntries = new List<AgentListEntry>();

        TryllInspectorSplit _outerSplit;
        TryllInspectorSplit _innerSplit;
        ListView _agentList;
        ListView _timelineList;
        TryllTailFollow _timelineTail;
        AgentLogInspectorHost _inspectorHost;
        TryllBanner _statusBanner;
        TryllStatusBar _statusBar;
        VisualElement _segRaw;
        VisualElement _segCoalesce;
        VisualElement _segCollapse;
        Button _clearButton;
        Button _pauseButton;
        Button _exportButton;
        Button _inspectToggleButton;
        VisualElement _timelineHost;
        VisualElement _emptyOverlay;
        [NonSerialized] bool _pendingTimelinePopulate;
        // EditorWindow fields serialize. A saved layout can restore this as true
        // while ListViews are still null — OnEnable must not treat that as "GUI ready".
        [NonSerialized] bool _guiBuilt;
        ulong? _selectedTimelineEventId;
        bool _showAllAgents = true;
        ulong? _selectedAgentId;
        /// <summary>User Pause latch — blocks enable/focus/connection-changed auto-attach (Unreal bUserPaused).</summary>
        bool _userPaused;
        TryllClient _boundClient;
        string _lastClientError;

        struct AgentListEntry
        {
            public bool IsAllAgents;
            public ulong AgentId;
            public string Title;
            public string Status;
        }

        [MenuItem("Window/Tryll/Agent Log")]
        public static void Open()
        {
            var window = GetWindow<TryllAgentLogWindow>("Tryll Agent Log");
            window.minSize = new Vector2(720, 420);
        }

        void OnEnable()
        {
            _store = AgentLogStore.Instance;
            _config = _store.Config;
            _filter.CoalesceAnswerText = _config.CoalesceAnswerText;
            _store.Changed += OnStoreChanged;
            EditorApplication.update += OnEditorUpdate;
            TryllEditorTheme.HostColorsChanged += OnHostColorsChanged;
            AttachIfNeeded();
            if (_guiBuilt)
                RefreshAll();
        }

        /// <summary>Rebuild lists and chrome now, as the next store change would.</summary>
        internal void DebugRefreshForTests() => RefreshAll();

        /// <summary>
        /// Rows are agent-list titles without "All Agents"; LocalState is the store's
        /// capture state. Read after <see cref="DebugRefreshForTests"/>: the window
        /// refreshes on store changes, not on every event.
        /// </summary>
        internal TryllEditorWindowSnapshot DebugSnapshot()
        {
            var rows = new List<string>();
            foreach (var entry in _agentEntries)
                if (!entry.IsAllAgents) rows.Add(entry.Title);
            return new TryllEditorWindowSnapshot(
                TryllEditorOwnerPhase.FromClient(TryllClient.Instance, _lastClientError).Item1,
                _store?.State.ToString() ?? "None",
                _statusBar?.DebugCaption,
                _statusBar?.DebugActionText,
                _statusBanner?.DebugText,
                rows,
                leaseHeld: false,
                listGeneration: _store?.Journal.Count ?? 0);
        }

        // Unity can invoke OnFocus while restoring a docked layout
        // (WindowLayout.LoadDefaultWindowPreferences) before OnEnable assigns _store.
        void OnFocus() => AttachIfNeeded();

        void OnDisable()
        {
            TryllEditorTheme.HostColorsChanged -= OnHostColorsChanged;
            EditorApplication.update -= OnEditorUpdate;
            UnbindClientLifecycle();
            if (_store != null)
                _store.Changed -= OnStoreChanged;
            _store?.Detach();
            _config?.Save();
        }

        void OnHostColorsChanged()
        {
            if (_guiBuilt)
                RefreshAll();
        }

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            TryllEditorTheme.Attach(root);

            root.Add(BuildToolbar());
            root.Add(BuildStatusBanner());
            root.Add(BuildBody());

            _guiBuilt = true;
            _pendingTimelinePopulate = true;
            root.schedule.Execute(DeferInitialTimelinePopulate).ExecuteLater(0);
            RefreshAll();
        }

        VisualElement BuildToolbar()
        {
            // Mirror Dialog Lab: action buttons → segmented controls | spacer | inspector.
            var bar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexWrap = Wrap.Wrap,
                    flexShrink = 0,
                    backgroundColor = Panel2,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 5,
                    paddingBottom = 5,
                    borderBottomWidth = 1,
                    borderBottomColor = Line,
                },
            };

            _clearButton = new Button(OnClearClicked) { text = "Clear" };
            _clearButton.style.marginRight = 4;
            bar.Add(_clearButton);

            _exportButton = new Button(OnExportClicked) { text = "Export" };
            _exportButton.style.marginRight = 10;
            _exportButton.SetEnabled(false);
            bar.Add(_exportButton);

            _pauseButton = new Button(OnPauseResumeClicked) { text = "Pause" };
            _pauseButton.style.marginRight = 10;
            bar.Add(_pauseButton);

            _segRaw = Segmented(
                new[] { "Curated", "Raw" },
                () => _filter.RawMode ? 1 : 0,
                i =>
                {
                    bool raw = i == 1;
                    if (_filter.RawMode == raw) return;
                    _filter.RawMode = raw;
                    RefreshProjection();
                });
            bar.Add(_segRaw);

            _segCoalesce = Segmented(
                new[] { "Stream", "Coalesce" },
                () => _config.CoalesceAnswerText ? 1 : 0,
                i =>
                {
                    bool coalesce = i == 1;
                    if (_config.CoalesceAnswerText == coalesce) return;
                    _config.CoalesceAnswerText = coalesce;
                    _filter.CoalesceAnswerText = coalesce;
                    _config.Save();
                    RefreshProjection();
                });
            _segCoalesce.tooltip =
                "Merge consecutive AnswerText tokens on the same turn into one row. " +
                "TTS / other events still interrupt the stream.";
            bar.Add(_segCoalesce);

            _segCollapse = Segmented(
                new[] { "Expand", "Collapse Completed" },
                () => _config.CollapseCompletedTurns ? 1 : 0,
                i =>
                {
                    bool collapse = i == 1;
                    if (_config.CollapseCompletedTurns == collapse) return;
                    _config.CollapseCompletedTurns = collapse;
                    _config.Save();
                    RefreshProjection();
                });
            _segCollapse.tooltip =
                "Collapse completed turns you have not expanded. Manually expanded turns stay open.";
            bar.Add(_segCollapse);

            bar.Add(new VisualElement { style = { flexGrow = 1 } });

            bar.Add(new TryllServerModeBadge(() => TryllClient.Instance));

            _statusBar = new TryllStatusBar();
            _statusBar.style.marginRight = 10;
            bar.Add(_statusBar);

            // Same naming as Dialog Lab (visible → collapse cue, hidden → expand cue).
            _inspectToggleButton = new Button(ToggleInspector)
            {
                text = _config.InspectorExpanded ? "Inspector ▸" : "Inspector ◂",
            };
            bar.Add(_inspectToggleButton);

            return bar;
        }

        VisualElement Segmented(string[] labels, Func<int> get, Action<int> set)
        {
            var seg = new VisualElement();
            seg.style.flexDirection = FlexDirection.Row;
            seg.style.marginRight = 10;
            SetBorder(seg, Line2, 1);
            SetRadius(seg, 3);
            seg.style.overflow = Overflow.Hidden;

            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var b = new Button(() =>
                {
                    set(idx);
                    RestyleSegment(seg, get());
                }) { text = labels[i] };
                b.style.marginLeft = 0;
                b.style.marginRight = 0;
                b.style.marginTop = 0;
                b.style.marginBottom = 0;
                b.style.borderLeftWidth = 0;
                b.style.borderRightWidth = idx < labels.Length - 1 ? 1 : 0;
                b.style.borderTopWidth = 0;
                b.style.borderBottomWidth = 0;
                b.style.borderRightColor = Line2;
                SetRadius(b, 0);
                b.style.paddingLeft = 9;
                b.style.paddingRight = 9;
                bool on = get() == idx;
                b.style.backgroundColor = on ? SegmentOnBg : Panel2;
                b.style.color = on ? SegmentOnText : Txt;
                seg.Add(b);
            }
            return seg;
        }

        void RestyleSegment(VisualElement seg, int active)
        {
            if (seg == null) return;
            for (int i = 0; i < seg.childCount; i++)
            {
                var b = seg.ElementAt(i);
                bool on = i == active;
                b.style.backgroundColor = on ? SegmentOnBg : Panel2;
                b.style.color = on ? SegmentOnText : Txt;
            }
        }

        VisualElement BuildStatusBanner()
        {
            _statusBanner = new TryllBanner();
            return _statusBanner;
        }

        VisualElement BuildBody()
        {
            _innerSplit = new TryllInspectorSplit(
                BuildAgentListPane(),
                BuildTimelinePane(),
                TryllSplitFixedSide.First,
                () => AgentListMinWidth,
                () => TimelineMinWidth,
                Mathf.Max(AgentListMinWidth, _config.AgentListWidth),
                () => _config.AgentListWidth,
                w => _config.AgentListWidth = w,
                () => true);

            _outerSplit = new TryllInspectorSplit(
                _innerSplit,
                BuildInspectorPane(),
                TryllSplitFixedSide.Second,
                () => TimelineMinWidth + AgentListMinWidth
                    + TryllInspectorSplitMath.DefaultHandleWidth,
                () => TryllTurnInspectorView.MinWidth,
                Mathf.Max(TryllTurnInspectorView.MinWidth, _config.InspectorWidth),
                () => _config.InspectorWidth,
                w => _config.InspectorWidth = w,
                () => _config.InspectorExpanded);

            return _outerSplit;
        }

        VisualElement BuildAgentListPane()
        {
            var pane = new VisualElement { style = { flexGrow = 0, minWidth = AgentListMinWidth } };
            pane.style.backgroundColor = Panel;
            pane.style.borderRightWidth = 1;
            pane.style.borderRightColor = Line;

            var heading = new Label("Agents");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.color = Txt;
            heading.style.paddingLeft = 8;
            heading.style.paddingTop = 6;
            heading.style.paddingBottom = 4;
            pane.Add(heading);

            _agentList = new ListView
            {
                selectionType = SelectionType.Single,
                fixedItemHeight = AgentListRowHeight,
                makeItem = MakeAgentItem,
                bindItem = BindAgentItem,
            };
            _agentList.style.flexGrow = 1;
            _agentList.selectionChanged += OnAgentSelectionChanged;
            pane.Add(_agentList);
            return pane;
        }

        VisualElement BuildTimelinePane()
        {
            var pane = new VisualElement { style = { flexGrow = 1, minWidth = TimelineMinWidth } };
            pane.style.flexDirection = FlexDirection.Column;

            _timelineHost = new VisualElement { style = { flexGrow = 1 } };

            _timelineList = new ListView
            {
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                // Must be non-zero before population (R11 / §6.4).
                fixedItemHeight = TimelineRowHeight,
                makeItem = MakeTimelineItem,
                bindItem = BindTimelineItem,
            };
            _timelineList.style.flexGrow = 1;
            _timelineList.selectionChanged += OnTimelineSelectionChanged;
            _timelineTail = new TryllTailFollow(
                () => TryllTailFollow.DistanceFromBottom(_timelineList),
                ScrollTimelineToEnd);
            _timelineList.RegisterCallback<GeometryChangedEvent>(_ => HookTimelineScroll());
            _timelineHost.Add(_timelineList);

            _emptyOverlay = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    right = 0,
                    top = 0,
                    bottom = 0,
                    justifyContent = Justify.Center,
                    alignItems = Align.Center,
                },
            };
            _emptyOverlay.pickingMode = PickingMode.Ignore;
            _timelineHost.Add(_emptyOverlay);
            pane.Add(_timelineHost);
            return pane;
        }

        VisualElement BuildInspectorPane()
        {
            _inspectorHost = new AgentLogInspectorHost();
            var root = _inspectorHost.Root;
            root.style.flexGrow = 1;
            return root;
        }

        void ToggleInspector()
        {
            _config.InspectorExpanded = !_config.InspectorExpanded;
            if (_config.InspectorExpanded)
                _outerSplit.Restore();
            else
                _outerSplit.Collapse();
            if (_inspectToggleButton != null)
                _inspectToggleButton.text = _config.InspectorExpanded ? "Inspector ▸" : "Inspector ◂";
            _config.Save();
        }

        static VisualElement MakeAgentItem()
        {
            var col = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    justifyContent = Justify.Center,
                    paddingLeft = 8,
                    paddingRight = 6,
                    paddingTop = 3,
                    paddingBottom = 3,
                },
            };

            var title = new Label { name = "title" };
            title.style.color = Txt;
            title.style.overflow = Overflow.Hidden;
            title.style.textOverflow = TextOverflow.Ellipsis;
            title.style.whiteSpace = WhiteSpace.NoWrap;
            title.style.unityFontStyleAndWeight = FontStyle.Normal;
            col.Add(title);

            var status = new Label { name = "status" };
            status.style.color = Dim;
            status.style.fontSize = 10;
            status.style.overflow = Overflow.Hidden;
            status.style.textOverflow = TextOverflow.Ellipsis;
            status.style.whiteSpace = WhiteSpace.NoWrap;
            status.style.unityFontStyleAndWeight = FontStyle.Normal;
            col.Add(status);

            return col;
        }

        void BindAgentItem(VisualElement element, int index)
        {
            var title = element.Q<Label>("title");
            var status = element.Q<Label>("status");
            if (title == null || status == null) return;

            if (index < 0 || index >= _agentEntries.Count)
            {
                title.text = "";
                status.text = "";
                return;
            }

            var entry = _agentEntries[index];
            title.text = entry.Title ?? "";
            status.text = entry.Status ?? "";

            bool selected = entry.IsAllAgents ? _showAllAgents : _selectedAgentId == entry.AgentId;
            title.style.unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal;
            title.style.color = selected ? Accent : Txt;
            status.style.color = Dim;
            status.style.unityFontStyleAndWeight = FontStyle.Normal;
        }

        VisualElement MakeTimelineItem()
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 4,
                    paddingRight = 6,
                },
            };
            row.name = "agent-log-row";

            // Fixed rails so →/← and HH:mm:ss.fff stay vertically aligned.
            var chevron = new Button { name = "chevron", text = "" };
            chevron.style.width = ChevronColumnWidth;
            chevron.style.height = TimelineRowHeight - 4f;
            chevron.style.flexShrink = 0;
            chevron.style.marginLeft = 0;
            chevron.style.marginRight = 0;
            chevron.style.paddingLeft = 0;
            chevron.style.paddingRight = 0;
            chevron.style.backgroundColor = new Color(0, 0, 0, 0);
            chevron.style.borderLeftWidth = 0;
            chevron.style.borderRightWidth = 0;
            chevron.style.borderTopWidth = 0;
            chevron.style.borderBottomWidth = 0;
            chevron.style.unityTextAlign = TextAnchor.MiddleCenter;
            chevron.style.fontSize = 10;
            chevron.style.color = Dim;
            chevron.clicked += () => OnChevronClicked(chevron);
            row.Add(chevron);

            var marker = new Label { name = "marker" };
            marker.style.width = MarkerColumnWidth;
            marker.style.unityTextAlign = TextAnchor.MiddleCenter;
            marker.style.flexShrink = 0;
            row.Add(marker);

            var time = new Label { name = "time" };
            time.style.width = TimestampColumnWidth;
            time.style.flexShrink = 0;
            time.style.fontSize = 10;
            row.Add(time);

            var agent = new Label { name = "agent" };
            agent.style.width = AgentGutterWidth;
            agent.style.flexShrink = 0;
            agent.style.overflow = Overflow.Hidden;
            agent.style.textOverflow = TextOverflow.Ellipsis;
            agent.style.whiteSpace = WhiteSpace.NoWrap;
            agent.style.fontSize = 10;
            row.Add(agent);

            var summary = new Label { name = "summary" };
            summary.style.flexGrow = 1;
            summary.style.minWidth = 0;
            summary.style.overflow = Overflow.Hidden;
            summary.style.textOverflow = TextOverflow.Ellipsis;
            summary.style.whiteSpace = WhiteSpace.NoWrap;
            summary.style.fontSize = 11;
            row.Add(summary);
            return row;
        }

        void BindTimelineItem(VisualElement element, int index)
        {
            // Reset every conditional style on every bind — recycled-row safety (R12.10).
            element.style.paddingLeft = 4;
            element.style.backgroundColor = StyleKeyword.Null;
            element.style.borderTopWidth = 0;
            element.style.borderTopColor = StyleKeyword.Null;

            var chevron = element.Q<Button>("chevron");
            var marker = element.Q<Label>("marker");
            var time = element.Q<Label>("time");
            var agent = element.Q<Label>("agent");
            var summary = element.Q<Label>("summary");

            chevron.userData = null;
            chevron.text = "";
            chevron.SetEnabled(false);
            chevron.style.color = Dim;
            marker.style.color = Txt;
            time.style.color = Dim;
            agent.style.color = Dim;
            agent.style.display = DisplayStyle.None;
            summary.style.color = Txt;
            summary.style.unityFontStyleAndWeight = FontStyle.Normal;
            summary.style.marginLeft = 0;

            if (index < 0 || index >= _projectedRows.Count)
            {
                marker.text = time.text = agent.text = summary.text = "";
                return;
            }

            var row = _projectedRows[index];
            marker.text = row.GutterMarker ?? "";
            time.text = FormatRowTime(row.WallTimeUtc);
            summary.text = row.SummaryText ?? "";

            if (row.CanCollapseTurn)
            {
                chevron.SetEnabled(true);
                chevron.text = row.IsTurnCollapsed ? "▶" : "▼";
                chevron.userData = row;
                chevron.style.color = Dim;
            }

            if (row.ShowAgentIdentityInGutter && row.AgentId != 0)
            {
                agent.style.display = DisplayStyle.Flex;
                agent.text = AgentLogInspectionBuilder.FormatAgentGutterLabel(
                    row.AgentId, _store.Descriptors);
            }

            switch (row.Kind)
            {
            case AgentLogRowKind.Sent:
                element.style.backgroundColor = SentRowBg;
                break;
            case AgentLogRowKind.Received:
                element.style.backgroundColor = ReceivedRowBg;
                // R12.3a: indent summary only so timestamps stay on one rail.
                summary.style.marginLeft = ReceivedIndentPx;
                break;
            default:
                element.style.backgroundColor = SystemRowBg;
                break;
            }

            if (row.IsFirstRowOfTurn)
            {
                element.style.borderTopWidth = 1;
                element.style.borderTopColor = TurnSeparatorColor;
            }

            if (RowMatchesSelection(row, _selectedTimelineEventId))
                element.style.backgroundColor = SelectedRowBg;
        }

        void OnChevronClicked(Button chevron)
        {
            if (!(chevron?.userData is AgentLogRow row) || !row.CanCollapseTurn)
                return;
            if (!row.TurnKey.HasValue)
                return;

            _turnCollapse.Toggle(row.TurnKey, row.IsTurnCollapsed);
            // Keep the header selected when collapsing hides a body selection.
            _selectedTimelineEventId = row.EventId;
            RefreshProjection();
        }

        static bool RowMatchesSelection(AgentLogRow row, ulong? selectedEventId)
        {
            return row != null && selectedEventId.HasValue && row.EventId == selectedEventId.Value;
        }

        void DeferInitialTimelinePopulate()
        {
            if (!_pendingTimelinePopulate) return;
            _pendingTimelinePopulate = false;
            _timelineList.itemsSource = _projectedRows;
            _timelineList.RefreshItems();
            _timelineTail?.NotifyAppend();
        }

        void HookTimelineScroll()
        {
            var scroll = _timelineList?.Q<ScrollView>();
            if (scroll == null) return;
            scroll.verticalScroller.valueChanged -= OnTimelineUserScroll;
            scroll.verticalScroller.valueChanged += OnTimelineUserScroll;
        }

        void OnTimelineUserScroll(float _)
        {
            _timelineTail?.NotifyUserScroll();
        }

        void ScrollTimelineToEnd()
        {
            if (_timelineList == null || _projectedRows.Count == 0 || _timelineTail == null)
                return;
            _timelineTail.BeginProgrammaticScroll();
            _timelineList.schedule.Execute(() =>
            {
                if (_timelineList != null && _projectedRows.Count > 0)
                    _timelineList.ScrollToItem(_projectedRows.Count - 1);
                _timelineTail?.EndProgrammaticScroll();
            }).ExecuteLater(1);
        }

        void AttachIfNeeded()
        {
            // Unreal AttachIfNeeded also no-ops when Store is unset. OnFocus can
            // run during layout restore before OnEnable has assigned _store.
            if (_store == null) return;
            var client = AgentLogSession.ResolveClient();
            if (client == null) return;
            BindClientLifecycle(client);
            if (!AgentLogCaptureAttach.ShouldAttach(_userPaused, _store.IsSubscribedTo(client)))
                return;
            _store.Attach(client);
        }

        void OnEditorUpdate()
        {
            // Detect late Instance create / swap without EnsureEditorInstance every frame
            // (forcing Ensure here can create a shared client during unrelated EditMode suites).
            var live = TryllClient.Instance;
            if (!ReferenceEquals(_boundClient, live))
            {
                if (live == null)
                {
                    UnbindClientLifecycle();
                    return;
                }

                AttachIfNeeded();
            }

            // DisplayName is assigned after CreateAgentOk; pick Chat/Play names up
            // without waiting for the next journal event.
            if (_guiBuilt && RefreshLiveAgentLabels())
            {
                RefreshAgentList();
                _timelineList?.RefreshItems();
            }
        }

        void BindClientLifecycle(TryllClient client)
        {
            if (ReferenceEquals(_boundClient, client))
                return;

            UnbindClientLifecycle();
            _boundClient = client;
            if (_boundClient != null)
            {
                _boundClient.ConnectionChanged += OnConnectionChanged;
                _boundClient.Error += OnClientError;
            }
        }

        void UnbindClientLifecycle()
        {
            if (_boundClient != null)
            {
                _boundClient.ConnectionChanged -= OnConnectionChanged;
                _boundClient.Error -= OnClientError;
            }
            _boundClient = null;
        }

        void OnClientError(TryllError error)
        {
            if (error.IsOk) return;
            _lastClientError = error.Message;
            if (_guiBuilt) RefreshStatusChrome();
        }

        void OnConnectionChanged(bool connected)
        {
            if (connected)
                _lastClientError = null;
            AttachIfNeeded();
            if (_guiBuilt)
            {
                RefreshToolbar();
                RefreshBanner();
            }
        }

        void OnExportClicked()
        {
            if (_store == null || _store.Journal.Count == 0)
                return;
            var project = PlayerSettings.productName;
            var name = TryllAgentLogDocument.FileName(project, DateTime.Now);
            var path = TryllEditorPrompts.SaveFilePanel("Export Agent Log", "", name, "json");
            if (string.IsNullOrEmpty(path))
                return;
            var selected = _filter != null && _filter.SingleAgentId.HasValue
                ? _filter.SingleAgentId.Value
                : 0UL;
            var json = TryllAgentLogDocument.Write(
                TryllAgentLogExport.FromStore(_store, selected, TryllClient.Instance));
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        void OnClearClicked()
        {
            _userPaused = false;
            _store.Clear();
            _turnCollapse.Clear();
            _selectedTimelineEventId = null;
            AttachIfNeeded();
            RefreshAll();
        }

        void OnPauseResumeClicked()
        {
            if (!_userPaused)
            {
                _userPaused = true;
                _store.Detach();
            }
            else
            {
                _userPaused = false;
                AttachIfNeeded();
            }
            RefreshToolbar();
            RefreshBanner();
        }

        void OnStoreChanged() => RefreshAll();

        void OnAgentSelectionChanged(IEnumerable<object> selected)
        {
            // Prefer selectedIndex: null-conditional LINQ on a struct yields AgentListEntry?,
            // which does not expose fields without .Value.
            if (_agentList.selectedIndex < 0 || _agentList.selectedIndex >= _agentEntries.Count)
                return;

            var entry = _agentEntries[_agentList.selectedIndex];
            if (entry.IsAllAgents)
            {
                _showAllAgents = true;
                _selectedAgentId = null;
                _filter.AllAgents = true;
                _filter.SingleAgentId = null;
            }
            else
            {
                _showAllAgents = false;
                _selectedAgentId = entry.AgentId;
                _filter.AllAgents = false;
                _filter.SingleAgentId = entry.AgentId;
            }

            RefreshProjection();
            _timelineTail?.ResetFollowing();
            _agentList.RefreshItems();
        }

        void OnTimelineSelectionChanged(IEnumerable<object> selected)
        {
            AgentLogRow row = null;
            if (_timelineList.selectedIndex >= 0 && _timelineList.selectedIndex < _projectedRows.Count)
                row = _projectedRows[_timelineList.selectedIndex];
            _selectedTimelineEventId = row != null ? row.EventId : (ulong?)null;
            _inspectorHost?.Render(row, _store.Journal, _store.Descriptors);
            _timelineList.RefreshItems();
        }

        bool RefreshLiveAgentLabels()
        {
            var client = AgentLogSession.ResolveClient();
            if (client == null) return false;

            var changed = false;
            foreach (var snapshot in client.SnapshotRegisteredAgents())
            {
                if (!string.IsNullOrEmpty(snapshot.LabelHint))
                {
                    if (!_store.Descriptors.TryGet(snapshot.AgentId, out var descriptor)
                        || descriptor.Label != snapshot.LabelHint)
                    {
                        _store.Descriptors.ApplyLabelHint(snapshot.AgentId, snapshot.LabelHint);
                        changed = true;
                    }
                }
                else
                {
                    _store.Descriptors.PromotePendingLabel(snapshot.AgentId);
                }
            }
            return changed;
        }

        void RefreshAll()
        {
            RefreshAgentList();
            RefreshProjection();
            RefreshToolbar();
            RefreshBanner();
            RefreshEmptyOverlay();
        }

        void RefreshAgentList()
        {
            if (_agentList == null) return;

            _agentEntries.Clear();

            // Refresh labels from live agents (DisplayName set after CreateAgentOk).
            RefreshLiveAgentLabels();

            var ids = new List<ulong>(_store.Descriptors.All.Keys);
            ids.Sort();

            _agentEntries.Add(new AgentListEntry
            {
                IsAllAgents = true,
                Title = "All Agents",
                Status = AgentLogInspectionBuilder.FormatAllAgentsStatus(
                    ids.Count, _store.GetTotalCompletedTurnCount()),
            });

            foreach (var id in ids)
            {
                if (!_store.Descriptors.TryGet(id, out var descriptor)) continue;
                var title = string.IsNullOrEmpty(descriptor.Label)
                    ? $"Agent {id}"
                    : descriptor.Label;
                bool pending = title.StartsWith("Pending agent (req ", StringComparison.Ordinal);
                _agentEntries.Add(new AgentListEntry
                {
                    AgentId = id,
                    Title = title,
                    Status = AgentLogInspectionBuilder.FormatAgentListStatus(
                        _store.GetCompletedTurnCount(id),
                        descriptor.Destroyed,
                        descriptor.CreatedBeforeDiagnosticsCapture,
                        pending),
                });
            }

            _agentList.itemsSource = _agentEntries;
            _agentList.RefreshItems();

            if (_showAllAgents)
                _agentList.selectedIndex = 0;
            else if (_selectedAgentId.HasValue)
            {
                var idx = _agentEntries.FindIndex(e => e.AgentId == _selectedAgentId.Value);
                if (idx >= 0) _agentList.selectedIndex = idx;
            }
        }

        void RefreshProjection()
        {
            var full = AgentLogProjection.Project(_store.Journal, _filter);
            _projectedRows.Clear();
            _projectedRows.AddRange(AgentLogTurnCollapse.Apply(
                full, _turnCollapse, _config.CollapseCompletedTurns));

            if (_timelineList != null)
            {
                if (!_pendingTimelinePopulate)
                {
                    _timelineList.itemsSource = _projectedRows;
                    _timelineList.RefreshItems();
                    _timelineTail?.NotifyAppend();
                }

                if (_selectedTimelineEventId.HasValue)
                {
                    var selectedIndex = _projectedRows.FindIndex(r => r.EventId == _selectedTimelineEventId.Value);
                    if (selectedIndex < 0)
                    {
                        // Selection was on a collapsed body row — promote to that turn's header.
                        selectedIndex = FindHeaderIndexForHiddenEvent(full, _selectedTimelineEventId.Value);
                        if (selectedIndex >= 0)
                            _selectedTimelineEventId = _projectedRows[selectedIndex].EventId;
                    }

                    if (selectedIndex >= 0)
                    {
                        _timelineList.SetSelection(selectedIndex);
                        _inspectorHost?.Render(
                            _projectedRows[selectedIndex],
                            _store.Journal,
                            _store.Descriptors);
                    }
                    else
                    {
                        _selectedTimelineEventId = null;
                        _timelineList.ClearSelection();
                        _inspectorHost?.Render(null, _store.Journal, _store.Descriptors);
                    }
                }
                else
                {
                    _inspectorHost?.Render(null, _store.Journal, _store.Descriptors);
                }
            }

            RefreshEmptyOverlay();
        }

        int FindHeaderIndexForHiddenEvent(IReadOnlyList<AgentLogRow> fullRows, ulong eventId)
        {
            if (fullRows == null) return -1;
            TryllTurnKey key = default;
            for (int i = 0; i < fullRows.Count; ++i)
            {
                if (fullRows[i] != null && fullRows[i].EventId == eventId)
                {
                    key = fullRows[i].TurnKey;
                    break;
                }
            }
            if (!key.HasValue) return -1;
            return _projectedRows.FindIndex(r =>
                r != null && r.TurnKey.HasValue && r.TurnKey.Equals(key) && r.CanCollapseTurn);
        }

        void RefreshToolbar()
        {
            if (_pauseButton == null) return;
            _pauseButton.text = _userPaused ? "Resume" : "Pause";
            if (_exportButton != null)
                _exportButton.SetEnabled(_store != null && _store.Journal.Count > 0);
            RestyleSegment(_segRaw, _filter.RawMode ? 1 : 0);
            RestyleSegment(_segCoalesce, _config.CoalesceAnswerText ? 1 : 0);
            RestyleSegment(_segCollapse, _config.CollapseCompletedTurns ? 1 : 0);
            if (_inspectToggleButton != null)
                _inspectToggleButton.text = _config.InspectorExpanded ? "Inspector ▸" : "Inspector ◂";
            RefreshStatusChrome();
        }

        void RefreshStatusChrome()
        {
            if (_statusBar == null) return;

            var client = TryllClient.Instance;
            var (owner, detail) = TryllEditorOwnerPhase.FromClient(client, _lastClientError);

            TryllPanelOperation local = TryllPanelOperation.None;
            if (owner == TryllOwnerPhase.Ready)
            {
                if (_store != null && _store.State == AgentLogStore.CaptureState.Capturing && !_userPaused)
                    local = TryllPanelOperation.Capturing;
                else if (_store == null || _store.State == AgentLogStore.CaptureState.NeverAttached)
                    local = TryllPanelOperation.Waiting;
            }

            _statusBar.Bind(TryllStatusPresentation.Compose(
                owner, detail, local, TryllEditorPanel.AgentLog));
        }

        void RefreshBanner()
        {
            if (_statusBanner == null) return;

            string message = null;
            var severity = TryllStatusSeverity.Neutral;
            if (_userPaused)
            {
                message = "Capture paused — click Resume to continue recording observations.";
                severity = TryllStatusSeverity.Warning;
            }
            else
            {
                switch (_store.State)
                {
                case AgentLogStore.CaptureState.NeverAttached:
                    message = "Capture has not started. Connect a client or enter Play mode while this window is open.";
                    break;
                case AgentLogStore.CaptureState.PausedUnsubscribed:
                    message = "Capture paused — click Resume to continue recording observations.";
                    severity = TryllStatusSeverity.Warning;
                    break;
                case AgentLogStore.CaptureState.HistoryDiscardedByReload:
                    message = "History was discarded by an editor reload. New observations will appear from this point.";
                    severity = TryllStatusSeverity.Warning;
                    break;
                }
            }

            if (_store.HistoryWasDiscardedByReload && _store.State != AgentLogStore.CaptureState.HistoryDiscardedByReload)
            {
                message = CombineBanner(message,
                    "History was discarded by an editor reload. New observations will appear from this point.");
                severity = TryllStatusSeverity.Warning;
            }

            if (_store.Journal.Count == 0 && _store.State == AgentLogStore.CaptureState.Capturing && !_userPaused)
                message = CombineBanner(message, "Waiting for observations…");

            if (HasPreExistingAgents())
                message = CombineBanner(message,
                    "Agents marked * existed before capture began — per-turn diagnostics may be unavailable.");

            if (string.IsNullOrEmpty(message))
            {
                _statusBanner.Dismiss();
                return;
            }

            _statusBanner.Bind(message, severity);
        }

        void RefreshEmptyOverlay()
        {
            if (_emptyOverlay == null) return;
            _emptyOverlay.Clear();

            string emptyText = null;
            if (_projectedRows.Count == 0)
            {
                if (_userPaused)
                {
                    emptyText = "Capture paused — resume to record new observations.";
                }
                else
                {
                    switch (_store.State)
                    {
                    case AgentLogStore.CaptureState.NeverAttached:
                        emptyText = TryllEditorUiContract.AgentLogEmpty;
                        break;
                    case AgentLogStore.CaptureState.PausedUnsubscribed:
                        emptyText = "Capture paused — resume to record new observations.";
                        break;
                    case AgentLogStore.CaptureState.HistoryDiscardedByReload:
                        emptyText = "History discarded by reload.";
                        break;
                    default:
                        emptyText = _store.Journal.Count == 0
                            ? "Timeline empty — waiting for agent activity."
                            : "No rows match the current filter.";
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(emptyText))
            {
                _emptyOverlay.style.display = DisplayStyle.None;
                return;
            }

            _emptyOverlay.style.display = DisplayStyle.Flex;
            _emptyOverlay.Add(Placeholder(emptyText));
        }

        bool HasPreExistingAgents()
        {
            foreach (var pair in _store.Descriptors.All)
            {
                if (pair.Value.CreatedBeforeDiagnosticsCapture)
                    return true;
            }
            return false;
        }

        static string CombineBanner(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b;
            if (string.IsNullOrEmpty(b)) return a;
            return a + " " + b;
        }

        static string FormatRowTime(DateTime wallTimeUtc)
        {
            if (wallTimeUtc == default) return "—";
            return wallTimeUtc.ToLocalTime().ToString("HH:mm:ss.fff");
        }
    }
}
