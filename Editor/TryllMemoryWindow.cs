// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Editor-only Memory window. Pulls <see cref="TryllClient.RequestGetMemoryConsumptionAsync"/>
    /// on demand (Refresh) and renders the same snapshot the server monitor shows,
    /// without graphs or auto-refresh.
    /// </summary>
    public sealed class TryllMemoryWindow : EditorWindow
    {
        private TryllRuntimeSettings _settings;
        private TryllEditorSession _session;
        private int _fetchGeneration;
        private TryllMemoryPresentation.State _pres;
        private long _unityReservedBytes;

        private TryllStatusBar _statusBar;
        private Button _refreshBtn;
        private VisualElement _body;
        private TryllBanner _staleBanner;

        [MenuItem("Window/Tryll/Memory")]
        public static void Open()
        {
            var w = GetWindow<TryllMemoryWindow>("Tryll Memory");
            w.minSize = new Vector2(640, 460);
        }

        private void OnEnable()
        {
            AutofindSettings();
            rootVisualElement.Clear();
            TryllEditorTheme.Attach(rootVisualElement, paintWindowBackground: false);
            rootVisualElement.style.paddingLeft = 10;
            rootVisualElement.style.paddingRight = 10;
            rootVisualElement.style.paddingTop = 6;
            rootVisualElement.style.paddingBottom = 8;
            rootVisualElement.Add(BuildChrome());
            _staleBanner = new TryllBanner();
            rootVisualElement.Add(_staleBanner);
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            _body = new VisualElement();
            scroll.Add(_body);
            rootVisualElement.Add(scroll);
            RenderBody();

            _session = new TryllEditorSession(_settings, requireRegisteredModels: false);
            _session.StateChanged += OnSessionStateChanged;
            _session.SessionReady += OnSessionReady;
            TryllEditorTheme.HostColorsChanged += OnHostColorsChanged;
            _session.Start();
            RefreshChrome();
        }

        private void OnDisable()
        {
            TryllEditorTheme.HostColorsChanged -= OnHostColorsChanged;
            ++_fetchGeneration;
            _pres.RefreshInFlight = false;
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
            RefreshChrome();
            RenderBody();
        }

        private VisualElement BuildChrome()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingBottom = 8;
            row.style.marginBottom = 8;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = Hairline;

            _statusBar = new TryllStatusBar();
            _statusBar.style.flexGrow = 1;
            row.Add(_statusBar);

            _refreshBtn = new Button(OnRefreshClicked) { text = "Refresh", name = TryllEditorElementNames.Refresh };
            _refreshBtn.style.minWidth = 80;
            row.Add(_refreshBtn);
            return row;
        }

        private void OnSessionStateChanged(TryllEditorSession.State state, string status)
        {
            RefreshChrome();
            if (state == TryllEditorSession.State.Error || state == TryllEditorSession.State.Idle)
            {
                _pres = TryllMemoryPresentation.ApplyDisconnected(_pres, status);
                RenderBody();
            }
        }

        private void OnSessionReady()
        {
            RefreshChrome();
            RenderBody();
        }

        private void RefreshChrome()
        {
            bool ready = _session != null && _session.CurrentState == TryllEditorSession.State.Ready;
            _refreshBtn?.SetEnabled(ready && !_pres.RefreshInFlight);
            if (_statusBar == null) return;

            TryllOwnerPhase owner = _session?.OwnerPhase ?? TryllOwnerPhase.Idle;
            string detail = _session?.OwnerDetail;
            TryllPanelOperation local = _pres.RefreshInFlight
                ? TryllPanelOperation.Refreshing
                : TryllPanelOperation.None;
            var pres = TryllStatusPresentation.Compose(
                owner, detail, local, TryllEditorPanel.Memory);
            _statusBar.Bind(pres, pres.Action == TryllStatusActionIntent.Retry
                ? () => _session?.Start()
                : null);
        }

        private async void OnRefreshClicked()
        {
            if (_session?.Client == null || !_session.Client.IsSessionReady) return;
            if (_pres.RefreshInFlight) return;

            _pres = TryllMemoryPresentation.ApplyRefreshStarted(_pres);
            int generation = ++_fetchGeneration;
            RefreshChrome();

            System.Threading.Tasks.Task<(TryllMemorySnapshot, TryllError)> fetch;
            using (_session.Client.AsEditorCall())
                fetch = _session.Client.RequestGetMemoryConsumptionAsync();
            var (snapshot, error) = await fetch;
            if (generation != _fetchGeneration || _body == null) return;

            _pres = error.IsOk
                ? TryllMemoryPresentation.ApplyRefreshSucceeded(_pres, snapshot)
                : TryllMemoryPresentation.ApplyRefreshFailed(_pres, error.Message);
            RefreshChrome();
            RenderBody();
        }

        private void RenderBody()
        {
            if (_body == null) return;
            _body.Clear();
            _unityReservedBytes = Profiler.GetTotalReservedMemoryLong();

            bool connected = _session != null && _session.CurrentState == TryllEditorSession.State.Ready;
            string stale = TryllMemoryPresentation.StaleBannerText(_pres.Snapshot, _pres.StaleError);
            if (!string.IsNullOrEmpty(stale))
                _staleBanner.Bind(stale, TryllStatusSeverity.Warning);
            else
                _staleBanner.Dismiss();

            if (!connected && _pres.Snapshot == null)
            {
                _body.Add(Note("Not connected. The window will launch or reuse the shared Tryll session. Unity reserved memory is shown below."));
                _body.Add(SystemSection(null));
                return;
            }

            if (_pres.Snapshot == null)
            {
                _body.Add(Note("Click Refresh to take a snapshot. This is a pull — nothing updates until you ask."));
                _body.Add(SystemSection(null));
                return;
            }

            RenderSnapshot(_pres.Snapshot);
        }

        private void RenderSnapshot(TryllMemorySnapshot m)
        {
            var tables = TryllMemoryPresentation.Project(
                m, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            _body.Add(SystemSection(m, tables.SystemNotes));

            var dev = Heading("Devices");
            if (tables.DevicesKind == TryllMemoryPresentation.DeviceSection.Uninitialized)
            {
                dev.Add(Note(TryllMemoryPresentation.DevicesUninitializedNote));
            }
            else if (tables.DevicesKind == TryllMemoryPresentation.DeviceSection.Empty)
            {
                dev.Add(Note(TryllMemoryPresentation.DevicesEmptyNote));
            }
            else
            {
                var table = Table();
                var devCols = TryllMemoryTableColumns.Devices;
                table.Add(HeaderRow(new[]
                {
                    HeaderFrom(devCols[0]),
                    HeaderFrom(devCols[1]),
                    HeaderFrom(devCols[2]),
                    HeaderFrom(devCols[3]),
                    HeaderFrom(devCols[4]),
                    HeaderFrom(devCols[5]),
                }));
                foreach (var d in tables.Devices)
                {
                    table.Add(DataRow(new[]
                    {
                        Cell(d.Label, devCols[0].Weight),
                        PillCell(d.Kind, Dim, devCols[1].Weight),
                        NumCell(d.Self, devCols[2].Weight),
                        NumCell(d.Other, devCols[3].Weight),
                        NumCell(d.Free, devCols[4].Weight),
                        NumCell(d.Total, devCols[5].Weight),
                    }));
                }
                dev.Add(table);
                dev.Add(Note(TryllMemoryPresentation.DevicesFooterNote));
            }
            _body.Add(dev);

            var kvBlock = Heading($"KV cache by agent    {tables.KvTotalLabel}");
            kvBlock.Add(Note(TryllMemoryPresentation.ContextHeadroomNote));
            if (tables.KvRows.Count == 0)
            {
                kvBlock.Add(Note(TryllMemoryPresentation.EmptyKvNote));
            }
            else
            {
                var table = Table();
                var kvCols = TryllMemoryTableColumns.Kv;
                table.Add(HeaderRow(new[]
                {
                    HeaderFrom(kvCols[0]),
                    HeaderFrom(kvCols[1]),
                    HeaderFrom(kvCols[2]),
                    HeaderFrom(kvCols[3]),
                    HeaderFrom(kvCols[4]),
                }));
                foreach (var row in tables.KvRows)
                    table.Add(KvRow(row));
                kvBlock.Add(table);
                if (!string.IsNullOrEmpty(tables.BackgroundKvNote))
                    kvBlock.Add(Note(tables.BackgroundKvNote));
            }
            _body.Add(kvBlock);

            var comps = Heading("Components");
            var compTable = Table();
            var compCols = TryllMemoryTableColumns.Components;
            compTable.Add(HeaderRow(new[]
            {
                HeaderFrom(compCols[0]),
                HeaderFrom(compCols[1]),
                HeaderFrom(compCols[2]),
                HeaderFrom(compCols[3]),
                HeaderFrom(compCols[4]),
            }));
            foreach (var row in tables.ComponentRows)
                compTable.Add(CompRow(row));
            comps.Add(compTable);
            _body.Add(comps);
        }

        private VisualElement SystemSection(TryllMemorySnapshot m, List<string> notes = null)
        {
            var box = Heading("System memory");
            var table = Table();
            var sysCols = TryllMemoryTableColumns.System;
            table.Add(HeaderRow(new[]
            {
                HeaderFrom(sysCols[0]),
                HeaderFrom(sysCols[1]),
                HeaderFrom(sysCols[2]),
            }));
            table.Add(DataRow(new[]
            {
                Cell("Unity reserved", sysCols[0].Weight),
                NumCell(TryllMemoryPresentation.FmtBinaryBytes((ulong)_unityReservedBytes), sysCols[1].Weight),
                Cell("This editor process — RAM, not editor VRAM", sysCols[2].Weight, muted: true),
            }));
            if (m != null)
            {
                table.Add(DataRow(new[]
                {
                    Cell("Working set", sysCols[0].Weight),
                    NumCell(TryllMemoryPresentation.FmtBinaryBytes(m.ProcessWorkingSetBytes), sysCols[1].Weight),
                    Cell("Resident pages for tryll_server", sysCols[2].Weight, muted: true),
                }));
                table.Add(DataRow(new[]
                {
                    Cell("Private", sysCols[0].Weight),
                    NumCell(TryllMemoryPresentation.FmtBinaryBytes(m.ProcessPrivateBytes), sysCols[1].Weight),
                    Cell("Commit that is not shareable", sysCols[2].Weight, muted: true),
                }));
                table.Add(DataRow(new[]
                {
                    Cell("Unattributed", sysCols[0].Weight),
                    NumCell(TryllMemoryPresentation.FmtBinaryBytes(m.UnattributedRamBytes), sysCols[1].Weight),
                    Cell("Working set minus attributed RAM", sysCols[2].Weight, muted: true),
                }));
            }
            box.Add(table);
            if (notes != null)
            {
                foreach (var n in notes)
                    box.Add(Note(n));
            }
            return box;
        }

        static Color ToneColor(TryllMemoryPresentation.Tone tone) => tone switch
        {
            TryllMemoryPresentation.Tone.Ok => ColOk,
            TryllMemoryPresentation.Tone.Accent => Accent,
            TryllMemoryPresentation.Tone.Warn => ColWarn,
            TryllMemoryPresentation.Tone.Err => ColErr,
            _ => Dim,
        };

        private static VisualElement KvRow(TryllMemoryPresentation.KvRow c)
        {
            var cols = TryllMemoryTableColumns.Kv;
            var row = DataRow(new[]
            {
                Cell(c.Name, cols[0].Weight),
                PillCell(c.ClassLabel, ToneColor(c.ClassTone), cols[1].Weight),
                Cell(c.When, cols[2].Weight, muted: true),
                ContextUsedCell(c, cols[3].Weight),
                NumCell(c.Reserved, cols[4].Weight),
            });
            if (c.IsBackground)
            {
                row.style.borderLeftWidth = 3;
                row.style.borderLeftColor = ColWarn;
            }
            return row;
        }

        private static VisualElement CompRow(TryllMemoryPresentation.ComponentRow c)
        {
            var cols = TryllMemoryTableColumns.Components;
            var row = DataRow(new[]
            {
                Cell(c.Kind, cols[0].Weight, muted: true),
                Cell(c.Name, cols[1].Weight),
                PillCell(c.TierLabel, ToneColor(c.TierTone), cols[2].Weight),
                NumCell(c.Ram, cols[3].Weight, warn: c.RamWarn),
                NumCell(c.Vram, cols[4].Weight),
            });
            if (!string.IsNullOrEmpty(c.Tooltip))
                row.tooltip = c.Tooltip;
            return row;
        }

        private static VisualElement ContextUsedCell(TryllMemoryPresentation.KvRow c, float weight)
        {
            var cell = new VisualElement();
            ApplyWeight(cell, weight);
            cell.style.justifyContent = Justify.Center;
            cell.style.minWidth = 110;

            uint cap = c.CapacityTokens;
            if (cap == 0)
            {
                cell.Add(MutedLabel("—"));
                return cell;
            }

            float frac = Mathf.Clamp01(c.OccupancyFrac);
            var track = new VisualElement();
            track.style.height = 7;
            track.style.width = Length.Percent(100);
            track.style.backgroundColor = TrackBg;
            SetRadius(track, 3);
            track.style.overflow = Overflow.Hidden;
            track.style.marginBottom = 3;

            var fill = new VisualElement();
            fill.style.height = 7;
            fill.style.width = Length.Percent(frac * 100f);
            fill.style.backgroundColor = c.OccupancyStale ? Dim : Accent;
            track.Add(fill);
            cell.Add(track);

            string caption = c.ContextCaption;
            var capL = MutedLabel(caption);
            capL.style.fontSize = 10;
            cell.Add(capL);
            return cell;
        }

        private static VisualElement Table()
        {
            var t = new VisualElement();
            t.style.backgroundColor = PanelTint;
            t.style.borderTopWidth = 1;
            t.style.borderBottomWidth = 1;
            t.style.borderLeftWidth = 1;
            t.style.borderRightWidth = 1;
            t.style.borderTopColor = Hairline;
            t.style.borderBottomColor = Hairline;
            t.style.borderLeftColor = Hairline;
            t.style.borderRightColor = Hairline;
            SetRadius(t, 4);
            t.style.overflow = Overflow.Hidden;
            t.style.marginTop = 4;
            return t;
        }

        static VisualElement HeaderFrom(TryllMemoryColumn col) =>
            col.Numeric ? NumCell(col.Header, col.Weight, header: true) : Cell(col.Header, col.Weight);

        private static VisualElement HeaderRow(VisualElement[] cells)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = HeaderBg;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.paddingTop = 5;
            row.style.paddingBottom = 5;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = Hairline;
            foreach (var c in cells)
            {
                foreach (var child in c.Children())
                {
                    if (child is Label l)
                    {
                        l.style.unityFontStyleAndWeight = FontStyle.Bold;
                        l.style.fontSize = 10;
                        l.style.color = Dim;
                    }
                }
                row.Add(c);
            }
            return row;
        }

        private static VisualElement DataRow(VisualElement[] cells)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = Hairline;
            foreach (var c in cells)
                row.Add(c);
            return row;
        }

        private static VisualElement Cell(string text, float weight, bool muted = false)
        {
            var wrap = new VisualElement();
            ApplyWeight(wrap, weight);
            var l = new Label(text ?? "—");
            l.style.fontSize = 11;
            l.style.overflow = Overflow.Hidden;
            l.style.unityTextOverflowPosition = TextOverflowPosition.End;
            l.style.whiteSpace = WhiteSpace.NoWrap;
            if (muted) l.style.color = Dim;
            wrap.Add(l);
            return wrap;
        }

        private static VisualElement NumCell(string text, float weight, bool header = false, bool warn = false)
        {
            var wrap = Cell(text, weight);
            foreach (var child in wrap.Children())
            {
                if (child is Label l)
                {
                    l.style.unityTextAlign = TextAnchor.MiddleRight;
                    if (warn) l.style.color = ColWarn;
                    if (header) l.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
            }
            return wrap;
        }

        private static VisualElement PillCell(string text, Color fg, float weight)
        {
            var wrap = new VisualElement();
            ApplyWeight(wrap, weight);
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.alignItems = Align.Center;
            wrap.Add(Chip(text, fg));
            return wrap;
        }

        private static Label MutedLabel(string text)
        {
            var l = new Label(text);
            l.style.fontSize = 11;
            l.style.color = Dim;
            l.style.whiteSpace = WhiteSpace.NoWrap;
            return l;
        }

        private static Label Note(string text)
        {
            var l = new Label(text);
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.fontSize = 11;
            l.style.color = Dim;
            l.style.marginTop = 6;
            l.style.marginBottom = 2;
            return l;
        }

        private static void ApplyWeight(VisualElement e, float weight)
        {
            e.style.flexGrow = weight;
            e.style.flexShrink = 1;
            e.style.flexBasis = 0;
            e.style.overflow = Overflow.Hidden;
            e.style.paddingRight = 6;
        }

        private void AutofindSettings() => _settings = TryllEditorSettingsLookup.Find();

        internal TryllEditorSession DebugSession => _session;

        internal bool DebugRefreshEnabled => _refreshBtn != null && _refreshBtn.enabledSelf;

        internal bool DebugHasSnapshot => _pres.Snapshot != null;

        /// <summary>Press Refresh as a click would (no-op while disabled, like the button).</summary>
        internal void DebugRefreshForTests()
        {
            if (DebugRefreshEnabled) OnRefreshClicked();
        }

        /// <summary>The tables the window draws from the last snapshot; null before the first.</summary>
        internal TryllMemoryPresentation.Tables DebugTables()
            => _pres.Snapshot == null
                ? null
                : TryllMemoryPresentation.Project(_pres.Snapshot, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        internal TryllEditorWindowSnapshot DebugSnapshot()
        {
            var rows = new List<string>();
            if (_pres.Snapshot != null)
            {
                var tables = TryllMemoryPresentation.Project(
                    _pres.Snapshot, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                foreach (var c in tables.ComponentRows) rows.Add(c.Name);
            }
            return new TryllEditorWindowSnapshot(
                _session?.OwnerPhase ?? TryllOwnerPhase.Idle,
                _session?.CurrentState.ToString() ?? "None",
                _statusBar?.DebugCaption,
                _statusBar?.DebugActionText,
                _staleBanner?.DebugText,
                rows,
                _session?.HoldsLease ?? false,
                _fetchGeneration);
        }
    }
}
