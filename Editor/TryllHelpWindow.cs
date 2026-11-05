// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tryll.Client.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Window &gt; Tryll &gt; Help &amp; Community: what is installed, where to get help, and
    /// community links. Every value in "About this install" is read when the window builds —
    /// the client and server version numbers (<see cref="TryllVersionLookup"/>), the wire
    /// protocol, and the newest release of the repo the server pin names — so the window
    /// needs no edit for a release. The footer opens the Welcome window and the logs folder.
    /// Links come from <see cref="TryllCommunityLinks"/>; an empty URL hides its chip.
    /// </summary>
    internal sealed class TryllHelpWindow : EditorWindow
    {
        internal const string MenuPath = "Window/Tryll/Help & Community";
        internal const string ElementLogo = "tryll-help-logo";
        internal const string ElementAbout = "tryll-help-about";
        internal const string ElementGetHelp = "tryll-help-get-help";
        internal const string ElementCommunity = "tryll-help-community";
        internal const string ElementInstallServer = "tryll-help-install-server";
        internal const string ElementUpdateStatus = "tryll-help-update-status";
        internal const string ElementCheckUpdates = "tryll-help-check-updates";
        internal const string ElementViewRelease = "tryll-help-view-release";
        internal const string ElementOpenWelcome = "tryll-help-open-welcome";
        internal const string ElementOpenLogs = "tryll-help-open-logs";
        internal const string ElementReportBug = "tryll-help-report-bug";

        /// <summary>Newest release found this editor session; one network check per session.</summary>
        internal const string LatestVersionSessionKey = "Tryll.Help.LatestVersion";

        const float RowLabelWidth = 84f;

        /// <summary>Test seam: replaces <see cref="TryllUpdateCheck.LatestVersionAsync"/>.</summary>
        internal static Func<string, Task<string>> DebugLatestVersionForTests;

        [MenuItem(MenuPath, priority = 1)]
        public static void Open()
        {
            TryllCommunityView.OpenWindow<TryllHelpWindow>(TryllEditorUiContract.HelpTitle,
                new Vector2(460, 480), new Vector2(540, 700));
        }

        string _repo;
        string _serverText;
        string _latest;
        bool _checking;
        bool _checkFailed;
        Label _updateStatus;
        Button _checkUpdates, _viewRelease;
        readonly List<(string Label, string Value)> _rows = new List<(string, string)>();

        void OnEnable()
        {
            _repo = TryllCommunityLinks.ResolveReleasesRepo(TryllServerPin.LoadForPackage());
            _latest = SessionState.GetString(LatestVersionSessionKey, "");
            Build();
            TryllEditorTheme.HostColorsChanged += OnHostColorsChanged;
            if (string.IsNullOrEmpty(_latest) && !string.IsNullOrEmpty(_repo) && !Application.isBatchMode)
                _ = CheckForUpdatesAsync();
        }

        void OnDisable() => TryllEditorTheme.HostColorsChanged -= OnHostColorsChanged;

        void OnHostColorsChanged() => Build();

        // A server can be installed or connected while this window is open. Rebuild only
        // when the server row changed, so scrolling and the focusing click are kept.
        void OnFocus()
        {
            if (_updateStatus != null && ServerText(TryllServerInfo.ForEditor()) != _serverText)
                Build();
        }

        // ------------------------------------------------------------------
        // Layout
        // ------------------------------------------------------------------

        void Build()
        {
            rootVisualElement.Clear();
            TryllEditorTheme.Attach(rootVisualElement, paintWindowBackground: false);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            rootVisualElement.Add(scroll);
            var body = scroll.contentContainer;
            body.style.paddingLeft = body.style.paddingRight = 16;
            body.style.paddingTop = 14;
            body.style.paddingBottom = 12;

            body.Add(TryllCommunityView.BrandHeader(ElementLogo, TryllCommunityView.ClientVersionChip()));
            body.Add(TryllCommunityView.Heading(TryllEditorUiContract.HelpTitle, 18));
            body.Add(TryllCommunityView.Paragraph(TryllEditorUiContract.HelpIntro, dim: true));
            var hero = TryllCommunityView.Row();
            hero.style.marginTop = 4;
            hero.style.marginBottom = 6;
            var discord = TryllCommunityView.DiscordButton(primary: true);
            if (discord != null)
                hero.Add(discord);
            var reportBug = TryllCommunityView.SecondaryButton(TryllEditorUiContract.HelpReportBug,
                () => TryllCommunityView.OpenUrl(TryllCommunityLinks.ReportBug), ElementReportBug);
            reportBug.tooltip = TryllVersionLookup.PinDocsUrl(TryllCommunityLinks.ReportBug);
            hero.Add(reportBug);
            body.Add(hero);

            body.Add(BuildAbout());

            var help = TryllCommunityView.Card(TryllEditorUiContract.HelpGetHelpHeader);
            help.Add(TryllCommunityView.LinkRow(ElementGetHelp, TryllCommunityView.HelpLinks()));
            body.Add(help);

            var community = TryllCommunityView.Card(TryllEditorUiContract.WelcomeCommunityHeader);
            community.Add(TryllCommunityView.LinkRow(ElementCommunity, TryllCommunityView.CommunityLinks()));
            body.Add(community);

            var footer = TryllCommunityView.Row();
            footer.style.marginTop = 4;
            var openWelcome = TryllCommunityView.SecondaryButton(TryllEditorUiContract.HelpOpenWelcome,
                TryllWelcomeWindow.Open, ElementOpenWelcome);
            openWelcome.style.marginRight = 6;
            footer.Add(openWelcome);
            footer.Add(TryllCommunityView.SecondaryButton(TryllEditorUiContract.HelpOpenLogs,
                TryllLogsFolder.Open, ElementOpenLogs));
            body.Add(footer);

            ShowUpdateState();
        }

        VisualElement BuildAbout()
        {
            var about = TryllCommunityView.Card(TryllEditorUiContract.HelpAboutHeader);
            about.name = ElementAbout;
            _rows.Clear();

            // Technical values only (versions, variant and backend names); every sentence
            // comes from TryllEditorUiContract.
            AddRow(about, TryllEditorUiContract.HelpRowClient, TryllVersionLookup.Client());

            var server = TryllServerInfo.ForEditor();
            _serverText = ServerText(server);
            var serverRow = AddRow(about, TryllEditorUiContract.HelpRowServer, _serverText);
            if (server.Source == TryllServerInfo.SourceKind.Missing)
            {
                serverRow.value.style.color = ColWarn;
                serverRow.row.Add(TryllCommunityView.TextButton(TryllEditorUiContract.HelpInstallServer,
                    TryllWelcomeWindow.Open, ElementInstallServer));
            }

            var protocol = AddRow(about, TryllEditorUiContract.HelpRowProtocol, TryllProtocol.Version.ToString());
            if (server.ProtocolVersion != 0 && server.ProtocolVersion != TryllProtocol.Version)
            {
                var mismatch = new Label(TryllEditorUiContract.HelpProtocolMismatch
                    .Replace("{protocol}", server.ProtocolVersion.ToString()));
                mismatch.style.color = ColWarn;
                mismatch.style.marginLeft = 8;
                protocol.row.Add(mismatch);
            }

            var updates = AddRow(about, TryllEditorUiContract.HelpRowUpdates, "");
            _updateStatus = updates.value;
            _updateStatus.name = ElementUpdateStatus;
            _viewRelease = TryllCommunityView.TextButton(TryllEditorUiContract.HelpViewRelease,
                () => Application.OpenURL(_repo.TrimEnd('/') + "/releases/latest"), ElementViewRelease);
            updates.row.Add(_viewRelease);
            _checkUpdates = TryllCommunityView.TextButton(TryllEditorUiContract.HelpCheckUpdates,
                () => _ = CheckForUpdatesAsync(), ElementCheckUpdates);
            updates.row.Add(_checkUpdates);
            return about;
        }

        (VisualElement row, Label value) AddRow(VisualElement parent, string label, string value)
        {
            var row = TryllCommunityView.Row();
            row.style.alignItems = Align.Center;
            row.style.flexWrap = Wrap.Wrap;
            row.style.minHeight = 22;
            row.style.marginBottom = 2;
            var name = new Label(label);
            name.style.width = RowLabelWidth;
            name.style.color = Dim;
            row.Add(name);
            var text = new Label(value);
            text.style.color = Txt;
            text.style.whiteSpace = WhiteSpace.Normal;
            row.Add(text);
            parent.Add(row);
            _rows.Add((label, value));
            return (row, text);
        }

        /// <summary>The server's version number, then its variant and backends when known.</summary>
        static string ServerText(TryllServerInfo server)
        {
            if (server.Source == TryllServerInfo.SourceKind.Missing)
                return TryllEditorUiContract.HelpServerMissing;
            string version = TryllVersionLookup.Server(server);
            if (string.IsNullOrEmpty(version))
                return TryllEditorUiContract.HelpServerBundled;
            string details = server.Details();
            return string.IsNullOrEmpty(details) ? version : version + " · " + details;
        }

        // ------------------------------------------------------------------
        // Updates
        // ------------------------------------------------------------------

        void ShowUpdateState()
        {
            if (_updateStatus == null)
                return;
            bool known = !string.IsNullOrEmpty(_repo);
            bool newer = !string.IsNullOrEmpty(_latest) && TryllUpdateCheck.IsNewer(_latest, TryllVersion.Version);
            Show(_checkUpdates, known);
            Show(_viewRelease, known && newer && !_checking && !_checkFailed);
            TryllCommunityView.SetActionEnabled(_checkUpdates, !_checking);

            if (!known)
                SetUpdateStatus(TryllEditorUiContract.HelpUpdatesUnavailable, Dim);
            else if (_checking)
                SetUpdateStatus(TryllEditorUiContract.HelpChecking, Dim);
            else if (_checkFailed)
                SetUpdateStatus(TryllEditorUiContract.HelpUpdateFailed, ColWarn);
            else if (newer)
                SetUpdateStatus(TryllEditorUiContract.HelpUpdateAvailable.Replace("{version}", _latest), Accent);
            else if (!string.IsNullOrEmpty(_latest))
                SetUpdateStatus(TryllEditorUiContract.HelpUpToDate, ColOk);
            else
                SetUpdateStatus("", Dim);
        }

        void SetUpdateStatus(string text, Color color)
        {
            _updateStatus.text = text;
            _updateStatus.style.color = color;
            _updateStatus.style.marginRight = string.IsNullOrEmpty(text) ? 0 : 8;
        }

        static void Show(VisualElement element, bool visible)
            => element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        async Task CheckForUpdatesAsync()
        {
            if (_checking || string.IsNullOrEmpty(_repo))
                return;
            _checking = true;
            _checkFailed = false;
            ShowUpdateState();
            var lookup = DebugLatestVersionForTests ?? (repo => TryllUpdateCheck.LatestVersionAsync(repo));
            string latest = await lookup(_repo);
            if (this == null)
                return;   // window closed while waiting
            _checking = false;
            _checkFailed = latest == null;
            if (latest != null)
            {
                _latest = latest;
                SessionState.SetString(LatestVersionSessionKey, latest);
            }
            ShowUpdateState();
        }

        // ------------------------------------------------------------------
        // Debug surface for headless window tests
        // ------------------------------------------------------------------

        internal IReadOnlyList<(string Label, string Value)> DebugRows => _rows;
        internal int DebugCommunityButtonCount => rootVisualElement.Q(ElementCommunity)?.childCount ?? 0;
        internal int DebugGetHelpButtonCount => rootVisualElement.Q(ElementGetHelp)?.childCount ?? 0;
        internal bool DebugCheckUpdatesVisible => _checkUpdates?.style.display == DisplayStyle.Flex;
        internal bool DebugViewReleaseVisible => _viewRelease?.style.display == DisplayStyle.Flex;
        internal string DebugUpdateStatus => _updateStatus?.text ?? "";
        internal Texture DebugLogo => rootVisualElement.Q<Image>(ElementLogo)?.image;

        internal void DebugSetRepoForTests(string repo)
        {
            _repo = repo;
            _latest = "";
            _checkFailed = false;
            ShowUpdateState();
        }

        internal Task DebugCheckForUpdatesForTests() => CheckForUpdatesAsync();
    }
}
