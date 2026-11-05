// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tryll.Client.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// First-run window: licence acceptance (the licence includes telemetry, always on), the
    /// one-time server download, and community links. Opens on its own once per project when
    /// the package is new or updated (<see cref="TryllWelcomeStartup"/>), when the editor
    /// cannot start a server because the licence is not accepted (<see cref="TryllLicenceGate"/>),
    /// and from Window &gt; Tryll &gt; Welcome. Follows docs/dev/editor-window-checklist.md:
    /// theme roles only, copy from <see cref="TryllEditorUiContract"/>, prompts through
    /// <see cref="TryllEditorPrompts"/>, and a debug surface for headless tests.
    /// </summary>
    internal sealed class TryllWelcomeWindow : EditorWindow
    {
        internal const string MenuPath = "Window/Tryll/Welcome";

        internal const string ElementLogo = "tryll-welcome-logo";
        internal const string ElementSetupProgress = "tryll-welcome-setup-progress";
        internal const string ElementLicenceAccept = "tryll-welcome-licence-accept";
        internal const string ElementLicenceWithdraw = "tryll-welcome-licence-withdraw";
        internal const string ElementLicenceRead = "tryll-welcome-licence-read";
        internal const string ElementLicencePending = "tryll-welcome-licence-pending";
        internal const string ElementTelemetry = "tryll-welcome-telemetry";
        internal const string ElementDownloadLite = "tryll-welcome-download-lite";
        internal const string ElementDownloadFull = "tryll-welcome-download-full";
        internal const string ElementInstallZip = "tryll-welcome-install-zip";
        internal const string ElementCancel = "tryll-welcome-cancel";
        internal const string ElementServerStatus = "tryll-welcome-server-status";
        internal const string ElementCommunity = "tryll-welcome-community";
        internal const string ElementShowAtStartup = "tryll-welcome-show-at-startup";
        internal const string ElementOpenHelp = "tryll-welcome-open-help";

        const int SetupSteps = 2;

        [MenuItem(MenuPath, priority = 0)]
        public static void Open()
        {
            TryllWelcomeStartup.EnsureProjectSettings();
            TryllCommunityView.OpenWindow<TryllWelcomeWindow>(TryllEditorUiContract.WelcomeTitle,
                new Vector2(500, 560), new Vector2(560, 760));
        }

        /// <summary>
        /// Called after a failed server launch: a released package without its server
        /// opens this window so the fix is one click away. Never opens in batch mode.
        /// </summary>
        internal static void OpenIfServerMissing()
        {
            if (Application.isBatchMode)
                return;
            var state = TryllServerInstaller.State(TryllServerPin.LoadForPackage(), out _);
            if (state == TryllServerInstallState.NotInstalled)
                Open();
        }

        TryllServerPin _pin;
        TryllCommunityView.StepChrome _licenceStep, _serverStep;
        VisualElement _licenceTodo, _licenceDone;
        readonly List<Button> _readLicence = new List<Button>();
        Toggle _showAtStartup;
        Label _setupProgress, _serverStatus, _progressLabel, _missingStep, _fullHint, _licencePending;
        ProgressBar _progress;
        Button _downloadLite, _downloadFull, _installZip, _cancel;
        TryllBanner _banner;
        string _bannerText;
        TryllStatusSeverity _bannerSeverity;
        CancellationTokenSource _cts;
        IVisualElementScheduledItem _poll;
        bool _installing;
        bool _nvidia;

        void OnEnable()
        {
            _pin = TryllServerPin.LoadForPackage();
            _nvidia = DetectNvidia();
            Build();
            TryllEditorTheme.HostColorsChanged += OnHostColorsChanged;
            TryllLicence.Changed += Refresh;
            _poll = rootVisualElement.schedule.Execute(UpdateProgress).Every(150);
            Refresh();
        }

        void OnDisable()
        {
            TryllEditorTheme.HostColorsChanged -= OnHostColorsChanged;
            TryllLicence.Changed -= Refresh;
            _poll?.Pause();
            // Closing the window stops its download: the install holds the assembly-reload
            // lock, and a reopened window could not reach this one's Cancel.
            _cts?.Cancel();
        }

        void OnHostColorsChanged()
        {
            // Rebuild the static chrome so the new host colours apply; keep all state.
            Build();
            if (!string.IsNullOrEmpty(_bannerText))
                _banner.Bind(_bannerText, _bannerSeverity);
            Refresh();
        }

        void ShowBanner(string text, TryllStatusSeverity severity)
        {
            _bannerText = text;
            _bannerSeverity = severity;
            _banner.Bind(text, severity);
        }

        void HideBanner()
        {
            _bannerText = null;
            _banner.Dismiss();
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

            // Hero: logo, title, the two ways to start.
            body.Add(TryllCommunityView.BrandHeader(ElementLogo, TryllCommunityView.ClientVersionChip()));
            body.Add(TryllCommunityView.Heading(TryllEditorUiContract.WelcomeTitle, 18));
            body.Add(TryllCommunityView.Paragraph(TryllEditorUiContract.WelcomeIntro, dim: true));
            var hero = TryllCommunityView.Row();
            hero.style.flexWrap = Wrap.Wrap;
            hero.style.marginTop = 4;
            hero.style.marginBottom = 10;
            hero.Add(TryllCommunityView.PrimaryButton(TryllEditorUiContract.WelcomeGetStarted,
                () => TryllCommunityView.OpenUrl(TryllCommunityLinks.GettingStarted)));
            var discord = TryllCommunityView.DiscordButton(primary: false);
            if (discord != null)
                hero.Add(discord);
            body.Add(hero);

            _banner = new TryllBanner();
            body.Add(_banner);

            var setupHeader = TryllCommunityView.Row();
            setupHeader.style.justifyContent = Justify.SpaceBetween;
            setupHeader.style.marginTop = 4;
            setupHeader.Add(TryllCommunityView.SectionLabel(TryllEditorUiContract.WelcomeSetupHeader));
            _setupProgress = TryllCommunityView.SectionLabel("");
            _setupProgress.name = ElementSetupProgress;
            setupHeader.Add(_setupProgress);
            body.Add(setupHeader);

            body.Add(BuildLicenceStep());
            body.Add(BuildServerStep());

            var community = TryllCommunityView.SectionLabel(TryllEditorUiContract.WelcomeCommunityHeader);
            community.style.marginTop = 6;
            body.Add(community);
            body.Add(TryllCommunityView.LinkRow(ElementCommunity, TryllCommunityView.CommunityLinks()));

            var footer = TryllCommunityView.Row();
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.alignItems = Align.Center;
            footer.style.marginTop = 8;
            footer.style.paddingTop = 8;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = Line;
            // Toggle.text puts the label right of the box, next to it.
            _showAtStartup = new Toggle { name = ElementShowAtStartup, text = TryllEditorUiContract.WelcomeShowAtStartup };
            _showAtStartup.SetValueWithoutNotify(TryllWelcomeStartup.ShowAtStartup);
            _showAtStartup.RegisterValueChangedCallback(e => TryllWelcomeStartup.ShowAtStartup = e.newValue);
            footer.Add(_showAtStartup);
            var openHelp = TryllCommunityView.TextButton(TryllEditorUiContract.WelcomeOpenHelp, TryllHelpWindow.Open, ElementOpenHelp);
            openHelp.style.marginRight = 0;
            footer.Add(openHelp);
            body.Add(footer);
        }

        VisualElement BuildLicenceStep()
        {
            var card = TryllCommunityView.StepCard(1, TryllEditorUiContract.WelcomeLicenceHeader, out _licenceStep);
            card.Add(TryllCommunityView.Paragraph(TryllEditorUiContract.WelcomeLicenceBody));
            // Its own bold line: developers worry most about what the shipped game sends.
            var players = TryllCommunityView.Paragraph(TryllEditorUiContract.WelcomeLicencePlayers);
            players.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.Add(players);
            _licencePending = TryllCommunityView.Paragraph(TryllEditorUiContract.WelcomeLicencePending, dim: true);
            _licencePending.name = ElementLicencePending;
            card.Add(_licencePending);
            _readLicence.Clear();

            _licenceTodo = TryllCommunityView.Row();
            _licenceTodo.style.flexWrap = Wrap.Wrap;
            _licenceTodo.Add(TryllCommunityView.PrimaryButton(TryllEditorUiContract.WelcomeLicenceAccept,
                () => OnLicenceChanged(true), ElementLicenceAccept));
            AddLicenceLinks(_licenceTodo, TryllCommunityView.SecondaryButton);
            card.Add(_licenceTodo);

            _licenceDone = TryllCommunityView.Row();
            _licenceDone.style.alignItems = Align.Center;
            _licenceDone.style.flexWrap = Wrap.Wrap;
            _licenceDone.style.marginBottom = 4;
            var accepted = new Label("● " + TryllEditorUiContract.WelcomeLicenceAccepted);
            accepted.style.color = ColOk;
            accepted.style.marginRight = 8;
            _licenceDone.Add(accepted);
            _licenceDone.Add(TryllCommunityView.TextButton(TryllEditorUiContract.WelcomeLicenceWithdraw,
                () => OnLicenceChanged(false), ElementLicenceWithdraw));
            AddLicenceLinks(_licenceDone, TryllCommunityView.TextButton);
            card.Add(_licenceDone);
            return card;
        }

        /// <summary>"Read the licence" and "Telemetry" in <paramref name="row"/>, drawn by <paramref name="button"/>.</summary>
        void AddLicenceLinks(VisualElement row, Func<string, Action, string, Button> button)
        {
            var read = button(TryllEditorUiContract.WelcomeLicenceRead, TryllLicenceViewerWindow.Open, ElementLicenceRead);
            _readLicence.Add(read);
            row.Add(read);
            var telemetry = button(TryllEditorUiContract.WelcomeTelemetryLink, OpenTelemetryDocs, ElementTelemetry);
            telemetry.tooltip = TryllVersionLookup.PinDocsUrl(TryllCommunityLinks.Telemetry);
            row.Add(telemetry);
        }

        /// <summary>The online telemetry page: what the server sends and why.</summary>
        static void OpenTelemetryDocs() => TryllCommunityView.OpenUrl(TryllCommunityLinks.Telemetry);

        VisualElement BuildServerStep()
        {
            var card = TryllCommunityView.StepCard(2, TryllEditorUiContract.WelcomeServerHeader, out _serverStep);
            _serverStatus = TryllCommunityView.Paragraph("");
            _serverStatus.name = ElementServerStatus;
            card.Add(_serverStatus);

            var buttons = TryllCommunityView.Row();
            buttons.style.flexWrap = Wrap.Wrap;
            _downloadLite = TryllCommunityView.PrimaryButton("", () => StartDownload(TryllServerCache.LiteVariant), ElementDownloadLite);
            _downloadFull = TryllCommunityView.SecondaryButton("", () => StartDownload(TryllServerCache.FullVariant), ElementDownloadFull);
            _installZip = TryllCommunityView.SecondaryButton(TryllEditorUiContract.WelcomeInstallZip, StartInstallFromZip, ElementInstallZip);
            _cancel = TryllCommunityView.SecondaryButton(TryllEditorUiContract.WelcomeCancel, () => _cts?.Cancel(), ElementCancel);
            foreach (var b in new[] { _downloadLite, _downloadFull, _installZip, _cancel })
                buttons.Add(b);
            card.Add(buttons);

            _fullHint = TryllCommunityView.Paragraph(TryllEditorUiContract.WelcomeDownloadFullHint, dim: true);
            card.Add(_fullHint);
            _missingStep = TryllCommunityView.Paragraph(TryllEditorUiContract.WelcomeNeedsLicence, dim: true);
            _missingStep.style.color = ColWarn;
            card.Add(_missingStep);
            _progress = new ProgressBar { lowValue = 0, highValue = 100 };
            _progress.style.marginLeft = _progress.style.marginRight = 0;
            _progressLabel = TryllCommunityView.Paragraph("", dim: true);
            card.Add(_progress);
            card.Add(_progressLabel);
            return card;
        }

        /// <summary>
        /// Accept, or Withdraw, which also stops the server this editor started
        /// (<see cref="TryllLicence.SetAccepted"/>). <see cref="TryllLicence.Changed"/> refreshes.
        /// </summary>
        void OnLicenceChanged(bool accepted) => TryllLicence.SetAccepted(accepted);

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        void Refresh()
        {
            if (_serverStatus == null)
                return;
            var state = TryllServerInstaller.State(_pin, out string installedVariant);
            bool busy = _installing || TryllServerInstaller.Busy;
            bool accepted = TryllLicence.Accepted;
            bool hasLicenceFile = TryllLicence.HasFile;

            // 1. Licence. Without a LICENSE.md there is nothing to read; Accept still works.
            Show(_licenceTodo, !accepted);
            Show(_licenceDone, accepted);
            Show(_licencePending, !hasLicenceFile);
            foreach (var read in _readLicence)
                Show(read, hasLicenceFile);
            _licenceStep.Set(accepted,
                accepted ? TryllEditorUiContract.WelcomeStepDone : TryllEditorUiContract.WelcomeStepRequired,
                accepted ? TryllCommunityView.Tone.Ok : TryllCommunityView.Tone.Warn);

            // 2. Server. A server on disk starts only after the licence is accepted.
            bool present = state == TryllServerInstallState.Bundled || state == TryllServerInstallState.Installed;
            bool serverReady = accepted && present;
            if (present && !accepted)
                SetStatus("◌ " + TryllEditorUiContract.WelcomeServerAfterSetup, Dim);
            else
                SetServerStatus(state, installedVariant);
            if (state == TryllServerInstallState.NoPin)
                _serverStep.Set(false, TryllEditorUiContract.WelcomeStepUnavailable, TryllCommunityView.Tone.Neutral);
            else
                _serverStep.Set(serverReady,
                    serverReady ? TryllEditorUiContract.WelcomeStepDone : TryllEditorUiContract.WelcomeStepRequired,
                    serverReady ? TryllCommunityView.Tone.Ok : TryllCommunityView.Tone.Warn);

            bool canInstall = _pin != null && state != TryllServerInstallState.Bundled;
            var lite = _pin?.Find(TryllServerCache.LiteVariant);
            var full = _pin?.Find(TryllServerCache.FullVariant);
            bool liteInstalled = state == TryllServerInstallState.Installed && installedVariant == TryllServerCache.LiteVariant;
            bool fullInstalled = state == TryllServerInstallState.Installed && installedVariant == TryllServerCache.FullVariant;

            Show(_downloadLite, canInstall && lite != null && !liteInstalled && !fullInstalled);
            Show(_downloadFull, canInstall && full != null && !fullInstalled && _nvidia);
            Show(_fullHint, _downloadFull.style.display == DisplayStyle.Flex);
            Show(_installZip, canInstall && !fullInstalled);
            Show(_cancel, busy);
            Show(_missingStep, canInstall && !accepted && state != TryllServerInstallState.Installed);
            if (lite != null) _downloadLite.text = TryllEditorUiContract.WelcomeDownloadLite.Replace("{size}", TryllModelManagerPresentation.FmtDecimalBytes(lite.size));
            if (full != null) _downloadFull.text = TryllEditorUiContract.WelcomeDownloadFull.Replace("{size}", TryllModelManagerPresentation.FmtDecimalBytes(full.size));

            TryllCommunityView.SetActionEnabled(_downloadLite, accepted && !busy);
            TryllCommunityView.SetActionEnabled(_downloadFull, accepted && !busy);
            TryllCommunityView.SetActionEnabled(_installZip, accepted && !busy);
            Show(_progress, busy);
            Show(_progressLabel, busy);

            int done = (accepted ? 1 : 0) + (serverReady ? 1 : 0);
            _setupProgress.text = TryllEditorUiContract.WelcomeSetupProgress
                .Replace("{done}", done.ToString())
                .Replace("{total}", SetupSteps.ToString());
        }

        void SetServerStatus(TryllServerInstallState state, string installedVariant)
        {
            switch (state)
            {
                case TryllServerInstallState.Bundled:
                    SetStatus("● " + TryllEditorUiContract.WelcomeServerBundled, ColOk);
                    break;
                case TryllServerInstallState.Installed:
                    SetStatus("● " + TryllEditorUiContract.WelcomeServerInstalled
                        .Replace("{version}", _pin != null ? _pin.version : TryllVersion.Version)
                        .Replace("{variant}", installedVariant ?? ""), ColOk);
                    break;
                case TryllServerInstallState.NotInstalled:
                    SetStatus("◌ " + TryllEditorUiContract.WelcomeServerMissing, ColWarn);
                    break;
                default:
                    SetStatus("◌ " + TryllEditorUiContract.WelcomeServerNoPin, Dim);
                    break;
            }
        }

        void SetStatus(string text, Color color)
        {
            _serverStatus.text = text;
            _serverStatus.style.color = color;
        }

        static void Show(VisualElement element, bool visible)
            => element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        void UpdateProgress()
        {
            if (!_installing || _progress == null)
                return;
            float fraction = TryllServerInstaller.Progress;
            bool downloading = fraction > 0f && fraction < 1f;
            _progress.value = fraction * 100f;
            _progressLabel.text = downloading
                ? TryllEditorUiContract.WelcomeDownloading.Replace("{percent}", Mathf.RoundToInt(fraction * 100f).ToString())
                : TryllEditorUiContract.WelcomeInstalling;
        }

        // ------------------------------------------------------------------
        // Actions
        // ------------------------------------------------------------------

        async void StartDownload(string variant)
        {
            await RunInstall("download", variant, ct => TryllServerInstaller.DownloadAsync(_pin, variant, ct));
        }

        async void StartInstallFromZip()
        {
            string path = TryllEditorPrompts.OpenFilePanel(TryllEditorUiContract.WelcomeInstallZip, "", "zip");
            if (string.IsNullOrEmpty(path))
                return;
            await RunInstall("zip", null, ct => TryllServerInstaller.InstallZipAsync(_pin, path, null, ct));
        }

        /// <summary>
        /// Runs one install on the main thread's async context. Assembly reload is locked for
        /// its duration: a script recompile mid-download would kill the task and leave a
        /// half-written temp file.
        /// </summary>
        async Task RunInstall(string source, string variant, Func<CancellationToken, Task<TryllServerInstaller.Result>> work)
        {
            if (_installing || TryllServerInstaller.Busy)
                return;
            // The download needs only the licence.
            if (!TryllLicence.Accepted)
            {
                ShowBanner(TryllEditorUiContract.WelcomeNeedsLicence, TryllStatusSeverity.Warning);
                return;
            }

            _installing = true;
            _cts = new CancellationTokenSource();
            HideBanner();
            Refresh();
            UpdateProgress();
            EditorApplication.LockReloadAssemblies();
            TryllServerInstaller.Result result;
            try
            {
                result = await work(_cts.Token);
            }
            catch (Exception ex)
            {
                result = TryllServerInstaller.Result.Fail("extract_failed", ex.Message);
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
                _cts.Dispose();
                _cts = null;
                _installing = false;
            }

            TryllInstallTelemetry.TrySend(new TryllInstallTelemetry.Info
            {
                Result = result.Code,
                Version = _pin != null ? _pin.version : TryllVersion.Version,
                Variant = string.IsNullOrEmpty(result.Variant) ? (variant ?? "") : result.Variant,
                Source = source,
                Channel = string.IsNullOrEmpty(_pin?.channel) ? "dev" : _pin.channel,
                Bytes = result.Bytes,
                Seconds = result.Seconds,
            });

            if (this == null)
                return;   // window closed (and the install cancelled) while waiting
            if (result.Ok)
                ShowBanner(TryllEditorUiContract.WelcomeInstallDone, TryllStatusSeverity.Success);
            else if (result.Code != "cancelled")
                ShowBanner(TryllEditorUiContract.WelcomeInstallFailed.Replace("{reason}", result.Message), TryllStatusSeverity.Error);
            Refresh();
        }

        static bool DetectNvidia()
        {
            string vendor = SystemInfo.graphicsDeviceVendor ?? "";
            string name = SystemInfo.graphicsDeviceName ?? "";
            return vendor.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ------------------------------------------------------------------
        // Debug surface for headless window tests
        // ------------------------------------------------------------------

        internal TryllEditorWindowSnapshot DebugSnapshot()
        {
            var rows = new List<string> { _serverStatus?.text ?? "" };
            foreach (var (label, _) in TryllCommunityView.CommunityLinks())
                rows.Add(label);
            return new TryllEditorWindowSnapshot(
                TryllOwnerPhase.Idle,
                _installing ? "installing" : "idle",
                _serverStatus?.text ?? "",
                "",
                _banner?.DebugText ?? "",
                rows,
                false,
                0);
        }

        internal bool DebugDownloadLiteVisible => _downloadLite.style.display == DisplayStyle.Flex;
        internal bool DebugDownloadLiteEnabled => _downloadLite.enabledSelf;
        internal bool DebugInstallZipEnabled => _installZip.enabledSelf;
        internal bool DebugLicenceAcceptVisible => _licenceTodo.style.display == DisplayStyle.Flex;
        internal bool DebugReadLicenceVisible => _readLicence.Count > 0 && _readLicence[0].style.display == DisplayStyle.Flex;
        internal bool DebugLicencePendingVisible => _licencePending.style.display == DisplayStyle.Flex;
        internal Button DebugTelemetryButton => rootVisualElement.Q<Button>(ElementTelemetry);
        internal string DebugServerStatus => _serverStatus?.text ?? "";
        internal string DebugMissingStepText => _missingStep.style.display == DisplayStyle.Flex ? _missingStep.text : "";
        internal string DebugSetupProgress => _setupProgress?.text ?? "";
        internal string DebugBannerText => _banner?.DebugText ?? "";
        internal int DebugCommunityButtonCount => rootVisualElement.Q(ElementCommunity)?.childCount ?? 0;
        internal Texture DebugLogo => rootVisualElement.Q<Image>(ElementLogo)?.image;

        internal void DebugSetPinForTests(TryllServerPin pin)
        {
            _pin = pin;
            Refresh();
        }

        // A headless window has no panel to dispatch clicks, so run the handler a click runs.
        internal void DebugSetLicenceForTests(bool accepted) => OnLicenceChanged(accepted);

        internal void DebugOpenTelemetryForTests() => OpenTelemetryDocs();

        /// <summary>Re-read the licence file and state, after a test swapped the file.</summary>
        internal void DebugRefreshForTests() => Refresh();

        internal Task DebugInstallFromZipForTests()
        {
            string path = TryllEditorPrompts.OpenFilePanel(TryllEditorUiContract.WelcomeInstallZip, "", "zip");
            if (string.IsNullOrEmpty(path))
                return Task.CompletedTask;
            return RunInstall("zip", null, ct => TryllServerInstaller.InstallZipAsync(_pin, path, null, ct));
        }
    }
}
