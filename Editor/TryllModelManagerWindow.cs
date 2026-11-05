// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Tryll.Client;
using Tryll.Client.Internal;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Editor-only Tryll Model Manager window (UI Toolkit).
    ///
    /// Three-panel layout — filters (left) · model list (middle) · properties (right) —
    /// matching <c>docs/research/clients/model-manager-mockup.html</c>.
    ///
    /// Rows are built from the live server: <see cref="TryllModelInfo"/> carries the full
    /// catalog view (status/size plus type/engine/audience/tool-format/context/files and
    /// default_sampling) via the enriched ListModels response. Build registration is the
    /// only client-owned field, persisted to <see cref="TryllModelManifest"/>.
    /// </summary>
    public sealed class TryllModelManagerWindow : EditorWindow
    {
        /// <summary>Published docs for offline STT + Silero VAD endpointing (plain text in dialogs).</summary>
        private const string VadCompanionDocsUrl = TryllDocs.UseVoiceInput;

        /// <summary>
        /// Catalog window must open without the play-mode registered-model guard.
        /// Listing and download prompts are panel-local.
        /// </summary>
        internal const bool SessionRequiresRegisteredModels = false;

        // ── Prototype data model ───────────────────────────────────────────────

        private enum ModelType { Language, Embedding, Stt, Tts, Vad }
        private enum Registration { None, Experimental, Production }
        private enum SortKey { Name, Type, Size, Status, Registration }

        /// <summary>Prototype-only enriched row. <see cref="Info"/> is the real thin server view.</summary>
        private sealed class ModelRow
        {
            public TryllModelInfo Info;        // Name / Status / HuggingFaceRepo / SizeBytes
            public ModelType      Type;
            public string         Engine;      // "llama-cpp" | "sherpa-onnx"
            public string         Audience;    // "public" | "experimental" | "internal" | "(unset)"
            public Registration   Reg;
            public string         ToolFormat;  // may be ""
            public string         ToolSupport; // "supported" | "unsupported"; "" for non-language
            public string[]       Files;
            public int            ContextSize; // 0 = unset
            public bool           Hidden;
            public string         Sampling;    // compact default_sampling summary; null for non-language
        }

        // Download job state lives on TryllModelManagerPresentation.

        // ── Filter / view state ────────────────────────────────────────────────

        private static readonly ModelType[]        AllTypes    = { ModelType.Language, ModelType.Embedding, ModelType.Stt, ModelType.Tts, ModelType.Vad };
        private static readonly string[]           AllEngines  = { "llama-cpp", "sherpa-onnx", "onnx-runtime" };
        private static readonly TryllModelStatus[] AllStatuses = { TryllModelStatus.Loaded, TryllModelStatus.Downloaded, TryllModelStatus.Local, TryllModelStatus.Incomplete, TryllModelStatus.Absent };
        private static readonly Registration[]     AllRegs     = { Registration.Production, Registration.Experimental, Registration.None };

        private readonly HashSet<ModelType>        _fType   = new HashSet<ModelType>(AllTypes);
        private readonly HashSet<string>           _fEngine = new HashSet<string>(AllEngines);
        private readonly HashSet<TryllModelStatus> _fStatus = new HashSet<TryllModelStatus>(AllStatuses);
        private readonly HashSet<Registration>     _fReg    = new HashSet<Registration>(AllRegs);
        private string  _search = "";
        private SortKey _sort   = SortKey.Name;

        private List<ModelRow>    _rows;
        private ModelRow          _selected;
        private TryllModelManifest _manifest;
        private bool               _missingModelsChecked;

        // ── Download state ─────────────────────────────────────────────────────

        private ProgressBar _downloadProgressBar;
        private readonly Dictionary<string, float> _progress = new Dictionary<string, float>();

        private readonly List<TryllModelManagerPresentation.DownloadJob> _downloads =
            new List<TryllModelManagerPresentation.DownloadJob>(); // newest first
        private VisualElement _dlOverlay;   // floating popover root (absolute-positioned)
        private VisualElement _dlList;      // scroll body inside the popover
        private Label         _dlBadge;     // active-count badge on the toolbar button

        // ── Connection state ───────────────────────────────────────────────────

        private TryllEditorSession   _session;
        private bool                 _hooked;
        private bool                 _listing;
        private int                  _listGeneration;
        private TryllRuntimeSettings _settings;
        private TryllStatusBar       _statusBar;
        private TryllBanner          _banner;
        private string               _operationalError;

        // ── UI references ──────────────────────────────────────────────────────

        private ScrollView _listScroll;
        private ScrollView _propsScroll;
        private Label      _countLabel;
        private readonly Dictionary<string, Label>  _countLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, Toggle> _toggles     = new Dictionary<string, Toggle>();
        private Label _stProd, _stExp, _stDisk, _stBytes, _stShip;

        private float _filterWidth = TryllEditorUiContract.ModelFilterDefaultWidth;
        private float _propsWidth  = TryllEditorUiContract.ModelPropsDefaultWidth;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        [MenuItem("Window/Tryll/Model Manager")]
        public static void Open()
        {
            var w = GetWindow<TryllModelManagerWindow>("Tryll Models");
            w.minSize = new Vector2(900, 480);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            if (_settings == null) AutofindSettings();
            TryllEditorTheme.HostColorsChanged += OnHostColorsChanged;
        }

        private void OnDisable()
        {
            TryllEditorTheme.HostColorsChanged -= OnHostColorsChanged;
            EditorApplication.update -= OnEditorUpdate;
            Stop();
        }

        void OnHostColorsChanged()
        {
            if (rootVisualElement.childCount == 0) return;
            RefreshAll();
        }

        static string LayoutPrefix() => TryllEditorPrefs.ProjectPrefix("ModelManager");

        void LoadLayoutPrefs()
        {
            string dest = LayoutPrefix();
            string prefix = TryllEditorPrefs.SourcePrefixIfEmpty(
                dest, TryllEditorPrefs.LegacyHashPrefix("ModelManager"),
                "FilterWidth", "PropsWidth");
            _filterWidth = Mathf.Max(
                TryllEditorUiContract.ModelFilterMinWidth,
                EditorPrefs.GetFloat(prefix + "FilterWidth",
                    TryllEditorUiContract.ModelFilterDefaultWidth));
            _propsWidth = Mathf.Max(
                TryllEditorUiContract.ModelPropsMinWidth,
                EditorPrefs.GetFloat(prefix + "PropsWidth",
                    TryllEditorUiContract.ModelPropsDefaultWidth));
            if (prefix != dest)
            {
                EditorPrefs.SetFloat(dest + "FilterWidth", _filterWidth);
                EditorPrefs.SetFloat(dest + "PropsWidth", _propsWidth);
            }
        }

        void PersistFilterWidth(float width)
        {
            if (!TryllInspectorSplitMath.ShouldPersistWidth(
                    width, TryllEditorUiContract.ModelFilterMinWidth))
            {
                return;
            }

            _filterWidth = width;
            EditorPrefs.SetFloat(LayoutPrefix() + "FilterWidth", width);
        }

        void PersistPropsWidth(float width)
        {
            if (!TryllInspectorSplitMath.ShouldPersistWidth(
                    width, TryllEditorUiContract.ModelPropsMinWidth))
            {
                return;
            }

            _propsWidth = width;
            EditorPrefs.SetFloat(LayoutPrefix() + "PropsWidth", width);
        }

        // Download progress is event-driven (OnDownloadProgress / OnDownloadComplete);
        // there is nothing to poll per frame.
        private void OnEditorUpdate() { }

        // ── Connection ─────────────────────────────────────────────────────────

        private void AutofindSettings() => _settings = TryllEditorSettingsLookup.Find();

        internal TryllEditorSession DebugSession => _session;

        /// <summary>Press toolbar Refresh as a click would.</summary>
        internal void DebugRefreshForTests() => ReloadModels();

        internal TryllEditorWindowSnapshot DebugSnapshot() => new TryllEditorWindowSnapshot(
            _session?.OwnerPhase ?? TryllOwnerPhase.Idle,
            _session?.CurrentState.ToString() ?? "None",
            _statusBar?.DebugCaption,
            _statusBar?.DebugActionText,
            _banner?.DebugText,
            (_rows ?? new List<ModelRow>()).Select(r => r.Info.Name).ToList(),
            _session?.HoldsLease ?? false,
            _listGeneration);

        private void Connect()
        {
            if (_settings == null) AutofindSettings();
            if (_settings == null)
            {
                _operationalError = "No TryllRuntimeSettings asset found";
                RefreshStatusChrome();
                return;
            }

            _missingModelsChecked = false;
            _operationalError = null;
            if (_session == null)
            {
                _session = new TryllEditorSession(_settings, requireRegisteredModels: SessionRequiresRegisteredModels);
                _session.StateChanged += OnSessionStateChanged;
                _session.SessionReady += OnSessionReady;
            }
            if (_session.Client != null) HookEvents(_session.Client);
            _session.Start();
            if (_session.Client != null) HookEvents(_session.Client);
            RefreshStatusChrome();
        }

        private void Stop()
        {
            if (_session?.Client != null) UnhookEvents(_session.Client);
            if (_session != null)
            {
                _session.StateChanged -= OnSessionStateChanged;
                _session.SessionReady -= OnSessionReady;
                _session.Stop();
                _session = null;
            }
            _listing = false;
            ++_listGeneration;
            RefreshStatusChrome();
        }

        private void ReloadModels()
        {
            if (_session != null && _session.CurrentState == TryllEditorSession.State.Ready)
                LoadModelsAsync();
            else if (_session == null
                || _session.CurrentState == TryllEditorSession.State.Idle
                || _session.CurrentState == TryllEditorSession.State.Error)
                Connect();
        }

        private void OnSessionStateChanged(TryllEditorSession.State state, string status)
        {
            if (state == TryllEditorSession.State.Error)
                _operationalError = status;
            RefreshStatusChrome();
        }

        private void OnSessionReady()
        {
            if (_session?.Client != null) HookEvents(_session.Client);
            LoadModelsAsync();
        }

        private void HookEvents(TryllClient client)
        {
            if (_hooked || client == null) return;
            client.Error               += OnClientError;
            client.DownloadProgress    += OnDownloadProgress;
            client.DownloadComplete    += OnDownloadComplete;
            client.LoadModelComplete   += OnLoadComplete;
            client.UnloadModelComplete += OnUnloadComplete;
            client.DeleteModelComplete += OnDeleteComplete;
            _hooked = true;
        }

        private void UnhookEvents(TryllClient client)
        {
            if (!_hooked || client == null) return;
            client.Error               -= OnClientError;
            client.DownloadProgress    -= OnDownloadProgress;
            client.DownloadComplete    -= OnDownloadComplete;
            client.LoadModelComplete   -= OnLoadComplete;
            client.UnloadModelComplete -= OnUnloadComplete;
            client.DeleteModelComplete -= OnDeleteComplete;
            _hooked = false;
        }

        private void OnClientError(TryllError error)
        {
            var client = _session?.Client ?? TryllClient.Instance;
            if (error.Code == (int)TryllErrorCode.ConnectionFailed
                && TryllEditorConnectionPolicy.ShouldWaitForOwnerRetry(
                    isStarting: _session != null
                        && (_session.CurrentState == TryllEditorSession.State.Connecting
                            || _session.CurrentState == TryllEditorSession.State.LaunchingServer),
                    retriesRemaining: client != null && client.HasConnectRetriesRemaining))
            {
                return;
            }

            if (_session != null && _session.CurrentState == TryllEditorSession.State.Ready)
            {
                _operationalError = $"Client error: {error.Message}";
                RefreshStatusChrome();
                return;
            }

            _operationalError = $"Client error: {error.Message}";
            RefreshStatusChrome();
        }

        private void OnDownloadProgress(string modelName, ulong bytes, ulong total, float percent)
        {
            _progress[modelName] = percent;
            var row = _rows?.FirstOrDefault(r => r.Info.Name == modelName);
            if (row != null)
                row.Info.Status = TryllModelManagerPresentation.StatusAfterDownloadProgress();

            NoteDownloadProgress(modelName, bytes, total, percent);

            if (_selected != null && _selected.Info.Name == modelName && _downloadProgressBar != null)
            {
                _downloadProgressBar.value = percent;
                _downloadProgressBar.title = $"{(int)percent}%";
            }
            Repaint();
        }

        private void OnDownloadComplete(string modelName, bool success, string error)
        {
            _progress.Remove(modelName);
            NoteDownloadComplete(modelName, success, error);
            var row = _rows?.FirstOrDefault(r => r.Info.Name == modelName);
            if (row != null)
                row.Info.Status = TryllModelManagerPresentation.StatusAfterDownloadComplete(success);

            // Re-list so the real on-disk size (and any status the server corrected) is reflected.
            if (success)
            {
                LoadModelsAsync();
                return;
            }

            _operationalError = string.IsNullOrEmpty(error)
                ? $"Download failed: {modelName}"
                : $"Download failed: {error}";
            RefreshStatusChrome();
            RefreshAll();
        }

        // ── Downloads popover (Chrome-style) ─────────────────────────────────────

        private int ActiveDownloadCount() =>
            TryllModelManagerPresentation.ActiveDownloadCount(_downloads);

        private void NoteDownloadProgress(string name, ulong bytes, ulong total, float percent)
        {
            bool isNew = TryllModelManagerPresentation.ApplyDownloadProgress(
                _downloads, name, bytes, total, percent, EditorApplication.timeSinceStartup);
            if (isNew) OpenDownloads();
            RefreshDownloads();
        }

        private void NoteDownloadComplete(string name, bool success, string error)
        {
            TryllModelManagerPresentation.ApplyDownloadComplete(_downloads, name, success, error);
            RefreshDownloads();
        }

        private VisualElement BuildDownloadsOverlay()
        {
            var ov = new VisualElement();
            ov.style.position = Position.Absolute;
            ov.style.top = 32; ov.style.right = 8; ov.style.width = 340; ov.style.maxHeight = 460;
            ov.style.backgroundColor = Panel;
            ov.style.borderTopWidth = 1; ov.style.borderBottomWidth = 1; ov.style.borderLeftWidth = 1; ov.style.borderRightWidth = 1;
            SetBorderColor(ov, Hairline); SetRadius(ov, 6);
            ov.style.display = DisplayStyle.None;

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
            head.style.paddingLeft = 12; head.style.paddingRight = 8; head.style.paddingTop = 8; head.style.paddingBottom = 6;
            head.style.borderBottomWidth = 1; head.style.borderBottomColor = Hairline;
            var title = new Label("Downloads"); title.style.unityFontStyleAndWeight = FontStyle.Bold; title.style.flexGrow = 1;
            head.Add(title);
            var clear = new Button(() =>
            {
                TryllModelManagerPresentation.ClearFinishedDownloads(_downloads);
                RefreshDownloads();
            }) { text = "Clear" };
            clear.style.fontSize = 11;
            head.Add(clear);
            ov.Add(head);

            _dlList = new ScrollView(ScrollViewMode.Vertical);
            _dlList.style.flexGrow = 1;
            ov.Add(_dlList);
            return ov;
        }

        private void OpenDownloads()
        {
            if (_dlOverlay != null) _dlOverlay.style.display = DisplayStyle.Flex;
            RefreshDownloads();
        }

        private void ToggleDownloads()
        {
            if (_dlOverlay == null) return;
            bool open = _dlOverlay.style.display == DisplayStyle.None;
            _dlOverlay.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open) RefreshDownloads();
        }

        private void RefreshDownloads()
        {
            if (_dlBadge != null)
            {
                int active = ActiveDownloadCount();
                _dlBadge.text = active.ToString();
                _dlBadge.style.display = active > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_dlList == null) return;

            _dlList.Clear();
            if (_downloads.Count == 0)
            {
                var empty = new Label("No recent downloads.");
                empty.style.color = Dim; empty.style.paddingLeft = 14; empty.style.paddingTop = 16; empty.style.paddingBottom = 16;
                _dlList.Add(empty);
            }
            else
            {
                foreach (var j in _downloads) _dlList.Add(DownloadCard(j));
            }
            Repaint();
        }

        private VisualElement DownloadCard(TryllModelManagerPresentation.DownloadJob j)
        {
            bool active = j.Phase == TryllModelManagerPresentation.DownloadPhase.Active;
            bool failed = j.Phase == TryllModelManagerPresentation.DownloadPhase.Failed;

            var card = new VisualElement();
            card.style.paddingLeft = 12; card.style.paddingRight = 12; card.style.paddingTop = 8; card.style.paddingBottom = 8;
            card.style.borderBottomWidth = 1; card.style.borderBottomColor = Hairline;

            var top = new VisualElement(); top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.Center;
            var dot = new Label(active ? "●" : failed ? "✗" : "✓");
            dot.style.color = active ? ColWarn : failed ? ColErr : ColOk; dot.style.marginRight = 8;
            top.Add(dot);
            var name = new Label(j.Name); name.style.flexGrow = 1;
            name.style.overflow = Overflow.Hidden; name.style.textOverflow = TextOverflow.Ellipsis;
            top.Add(name);
            card.Add(top);

            if (active)
            {
                float frac = j.Total > 0 ? (float)((double)j.Bytes / j.Total) : j.Percent / 100f;
                var bar = new ProgressBar { value = frac * 100f };
                bar.style.marginTop = 4; bar.style.marginBottom = 2;
                card.Add(bar);

                float remain = (j.Speed > 1f && j.Total > j.Bytes) ? (float)((j.Total - j.Bytes) / j.Speed) : 0f;
                var sub = new Label(TryllModelManagerPresentation.FormatDownloadProgress(
                    (long)j.Bytes, (long)j.Total, j.Speed, remain));
                sub.style.color = Dim; sub.style.fontSize = 11;
                card.Add(sub);
            }
            else
            {
                long size = (long)(j.Total > 0 ? j.Total : j.Bytes);
                var sub = new Label(failed
                    ? TryllModelManagerPresentation.FormatDownloadFailed(j.Error)
                    : TryllModelManagerPresentation.FormatDownloadDone(size));
                sub.style.color = failed ? ColErr : Dim; sub.style.fontSize = 11; sub.style.whiteSpace = WhiteSpace.Normal;
                card.Add(sub);
            }
            return card;
        }

        private void OnLoadComplete(string modelName, bool success)
        {
            if (success)
            {
                LoadModelsAsync();
                return;
            }

            _operationalError = $"Load failed: {modelName}";
            RefreshStatusChrome();
            RefreshAll();
        }

        private void OnUnloadComplete(string modelName, bool success)
        {
            if (success)
            {
                LoadModelsAsync();
                return;
            }

            _operationalError = $"Unload failed: {modelName}";
            RefreshStatusChrome();
            RefreshAll();
        }

        private async void LoadModelsAsync()
        {
            int generation = ++_listGeneration;
            _listing = true;
            RefreshStatusChrome();
            var client = _session?.Client ?? TryllClient.Instance;
            if (client == null)
            {
                if (!TryllModelManagerPresentation.ShouldApplyListGeneration(generation, _listGeneration)) return;
                _listing = false;
                _operationalError = "Not connected";
                RefreshStatusChrome();
                return;
            }

            System.Threading.Tasks.Task<(System.Collections.Generic.List<TryllModelInfo>, TryllError)> listTask;
            using (client.AsEditorCall())
                listTask = client.RequestListModelsAsync();
            var (models, error) = await listTask;
            if (!TryllModelManagerPresentation.ShouldApplyListGeneration(generation, _listGeneration)) return;
            _listing = false;
            if (!error.IsOk)
            {
                _operationalError = $"ListModels failed: {error.Message}";
                RefreshStatusChrome();
                return;
            }
            _operationalError = null;

            var prevSelected = _selected?.Info.Name;
            _rows = BuildRowsFromWire(models);
            ApplyManifest();
            _selected = (prevSelected != null ? _rows.FirstOrDefault(r => r.Info.Name == prevSelected) : null)
                        ?? _rows.FirstOrDefault();
            RefreshStatusChrome();
            RefreshAll();

            if (!_missingModelsChecked)
            {
                _missingModelsChecked = true;
                PromptDownloadMissingRegisteredModels();
            }
        }

        /// <summary>
        /// On connect, ask whether to download any Production/Experimental model that is
        /// registered but Absent on disk — feat-011's Model Manager window guard. Unlike the
        /// Play/Chat guards this only prompts once per connect; it never auto-downloads.
        /// </summary>
        private void PromptDownloadMissingRegisteredModels()
        {
            if (_manifest == null || _rows == null) return;

            var missing = _rows.Where(r =>
                TryllModelManagerPresentation.IsMissingRegistered((int)r.Reg, r.Info.Status)).ToList();
            if (missing.Count == 0) return;

            var downloadable    = missing.Where(r => !string.IsNullOrEmpty(r.Info.HuggingFaceRepo)).ToList();
            var nonDownloadable = missing.Where(r => string.IsNullOrEmpty(r.Info.HuggingFaceRepo)).ToList();

            if (nonDownloadable.Count > 0)
            {
                TryllEditorPrompts.Notify("Tryll — registered models missing",
                    "The following registered models are missing and cannot be downloaded automatically " +
                    "(no HuggingFace source configured):\n\n" +
                    string.Join("\n", nonDownloadable.Select(r => $"  • {r.Info.Name}")) +
                    "\n\nProvide them via a local path or update the catalog.",
                    "OK");
            }

            if (downloadable.Count == 0) return;

            bool download = TryllEditorPrompts.Confirm("Tryll — download missing registered models?",
                $"{downloadable.Count} registered (Production/Experimental) model(s) are not downloaded yet:\n\n" +
                string.Join("\n", downloadable.Select(r => $"  • {r.Info.Name}")) +
                "\n\nDownload them now?",
                "Download", "Not now");
            if (!download) return;

            var client = TryllClient.Instance;
            foreach (var row in downloadable)
            {
                row.Info.Status = TryllModelStatus.Downloading;
                _progress[row.Info.Name] = 0f;
                client.RequestDownloadModel(row.Info.Name);
            }
            RefreshAll();
        }

        /// <summary>Build the display rows straight from the enriched ListModels response.</summary>
        private List<ModelRow> BuildRowsFromWire(List<TryllModelInfo> live)
        {
            var rows = new List<ModelRow>();
            foreach (var m in live)
            {
                var projected = TryllModelManagerPresentation.ProjectCatalog(m);
                rows.Add(new ModelRow
                {
                    Info        = m,
                    Type        = (ModelType)(int)projected.Type,
                    Engine      = projected.Engine,
                    Audience    = projected.Audience,
                    Reg         = Registration.None,
                    ToolFormat  = projected.ToolFormat,
                    ToolSupport = projected.ToolSupport,
                    Files       = projected.Files,
                    ContextSize = projected.ContextSize,
                    Hidden      = false,
                    Sampling    = projected.Sampling,
                });
            }
            return rows;
        }

        private static ModelType MapModelType(string t) =>
            (ModelType)(int)TryllModelManagerPresentation.MapModelType(t);

        private void RefreshStatusChrome()
        {
            if (_statusBar == null) return;

            TryllOwnerPhase owner = _session?.OwnerPhase ?? TryllOwnerPhase.Idle;
            string detail = _session?.OwnerDetail;
            TryllPanelOperation local = _listing
                ? TryllPanelOperation.Listing
                : TryllPanelOperation.None;
            var pres = TryllStatusPresentation.Compose(
                owner, detail, local, TryllEditorPanel.ModelManager,
                _rows?.Count ?? 0);
            _statusBar.Bind(pres, pres.Action == TryllStatusActionIntent.Retry
                ? Connect
                : null);

            if (_banner != null)
            {
                if (!string.IsNullOrEmpty(_operationalError))
                    _banner.Bind(_operationalError, TryllStatusSeverity.Error);
                else
                    _banner.Dismiss();
            }
            Repaint();
        }

        // ── STT ↔ VAD companion helpers ─────────────────────────────────────────

        /// <summary>Catalog VAD rows (companions for offline STT endpointing).</summary>
        /// <summary>
        /// Whether a placement choice can do anything for this model type.
        ///
        /// False for VAD, and that is not a limitation of the editor. A VAD entry is
        /// download/config metadata, not a model the server caches: it is created per
        /// STT session inside the recognizer, so there is no load for a placement to
        /// apply to, and the server pins it to the CPU deliberately (a ~1.8 MB graph
        /// fed 32 ms windows is the wrong shape for a device). Offering the control
        /// anyway would be worse than not having it — an author would set VRAM, ship
        /// it, and never learn that nothing read it.
        /// </summary>
        private static bool SupportsPlacement(ModelType type) =>
            TryllModelManagerPresentation.SupportsPlacement(type.ToString());

        private List<ModelRow> VadCompanions() =>
            (_rows ?? new List<ModelRow>()).Where(r => r.Type == ModelType.Vad).ToList();

        // ── Build-registration manifest ────────────────────────────────────────

        /// <summary>
        /// Map the catalog's engine string (ModelInfo.engine, e.g. "llama-cpp") onto
        /// the client enum, so the manifest can declare it offline.
        ///
        /// Falls back to LlamaCpp for an unrecognised value rather than Mock: Mock is
        /// the enum's zero value and a production server refuses it, so defaulting
        /// there would turn "we did not recognise this engine name" into a refused
        /// session with a misleading reason.
        /// </summary>
        private static TryllInferenceEngine ParseEngine(string engine) =>
            TryllModelManagerPresentation.ParseEngine(engine);

        /// <summary>
        /// Persist a placement choice. Unlike registration this never adds or removes
        /// an entry — an unregistered model has no placement to store, and the UI
        /// only offers the control once a model is registered.
        /// </summary>
        private void SetPlacement(ModelRow m, TryllMemoryPlacement place)
        {
            EnsureManifest();
            if (_manifest.Get(m.Info.Name) == TryllModelRegistration.None)
                return;

            _manifest.Set(m.Info.Name, m.Info.ModelType,
                          _manifest.Get(m.Info.Name),
                          m.Info.Languages, ParseEngine(m.Info.Engine), place,
                          HotwordBiasOf(m));
            SaveManifest();
        }

        private void SetRegistration(ModelRow m, Registration reg)
        {
            m.Reg = reg;
            EnsureManifest();
            // Cache the catalog languages AND engine alongside the registration: the
            // settings dropdown, the model-picker warnings and the session's engine
            // declaration all read the manifest offline, long after this live
            // ModelInfo is gone. Getting the engine wrong here is not cosmetic — the
            // server refuses to load a model whose engine the session never declared.
            _manifest.Set(m.Info.Name, m.Info.ModelType, (TryllModelRegistration)(int)reg,
                          m.Info.Languages, ParseEngine(m.Info.Engine));

            if (m.Type == ModelType.Stt && (reg == Registration.Experimental || reg == Registration.Production))
            {
                string tierLabel = reg == Registration.Production ? "Production" : "Experimental";
                foreach (var vad in VadCompanions())
                {
                    if (!TryllModelManagerPresentation.ShouldPromptVadCompanion(
                            TryllModelManagerPresentation.CatalogType.Stt, (int)reg, (int)vad.Reg))
                    {
                        continue;
                    }

                    bool register = TryllEditorPrompts.Confirm("Tryll — register VAD companion?",
                        $"\"{m.Info.Name}\" uses \"{vad.Info.Name}\" for automatic end-of-speech detection.\n\n" +
                        $"Also register \"{vad.Info.Name}\" as {tierLabel}?\n\n" +
                        "Choose No if you control utterance boundaries yourself (push-to-talk / hands-free).\n\n" +
                        $"Learn more: {VadCompanionDocsUrl}",
                        "Yes", "No");
                    if (!register) continue;

                    vad.Reg = reg;
                    _manifest.Set(vad.Info.Name, vad.Info.ModelType, (TryllModelRegistration)(int)reg,
                                  vad.Info.Languages, ParseEngine(vad.Info.Engine));
                }
            }

            SaveManifest();

            // A newly-registered model may now be Absent-and-required; the Play-mode guard's
            // cached "verified ready" flag must not mask that on the next Play press.
            TryllPlayModeModelGuard.Invalidate();
        }

        // Apply the committed manifest's registrations onto the rows (unlisted = None).
        private void ApplyManifest()
        {
            if (_manifest == null) _manifest = TryllModelManifest.FindProjectManifest();
            bool changed = false;
            foreach (var row in _rows)
            {
                row.Reg = (Registration)TryllModelManagerPresentation.ResolveRegistration(
                    _manifest != null,
                    _manifest != null ? (int)_manifest.Get(row.Info.Name) : 0);
                if (_manifest == null || row.Reg == Registration.None) continue;

                // Backfill every cached catalog field on every refresh and let Set say
                // whether anything moved. This used to skip the call whenever cached
                // languages matched the server — which meant the cached *engine* was never
                // corrected, so any manifest written before that field existed kept Mock
                // and pre-warmed nothing. Comparing here at all was the mistake: the
                // manifest owns the list of what it caches.
                changed |= _manifest.Set(
                    row.Info.Name,
                    row.Info.ModelType,
                    (TryllModelRegistration)(int)row.Reg,
                    row.Info.Languages ?? new string[0],
                    ParseEngine(row.Info.Engine));
            }
            if (changed)
            {
                SaveManifest();
            }
        }

        // A manifest that is not a project asset is a test's in-memory override
        // (TryllModelManifest.DebugOverrideForTests); it has nowhere to be saved.
        private void SaveManifest()
        {
            if (!AssetDatabase.Contains(_manifest)) return;
            EditorUtility.SetDirty(_manifest);
            AssetDatabase.SaveAssets();
        }

        private void EnsureManifest()
        {
            if (_manifest == null) _manifest = TryllModelManifest.FindProjectManifest() ?? CreateManifest();
        }

        private static TryllModelManifest CreateManifest()
        {
            // Created in Resources, not Assets/Tryll, because the manifest is a *runtime*
            // asset: a built player has no AssetDatabase, so TryllModelManifest.LoadOrNull
            // reaches it through Resources.Load or not at all. It used to be created in
            // Assets/Tryll — which made every project the Model Manager set up silently
            // lose its authored RAM/VRAM placement (and the engine pre-warm) in a packaged
            // build. That was our default path, not a project's mistake.
            //
            // Resources is the right mechanism here rather than a compromise:
            // TryllRuntimeSettings already ships this way, and one small ScriptableObject
            // carries none of the build-size cost Resources is usually warned about.
            //
            // Projects created before this change keep their old path; the build-time check
            // in TryllWorkflowModelPreBuildCheck is what tells them.
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            var asset = ScriptableObject.CreateInstance<TryllModelManifest>();
            AssetDatabase.CreateAsset(asset, TryllModelManifest.EditorAssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private void CreateGUI()
        {
            // Rows arrive from the server on connect (LoadModelsAsync); start empty.
            if (_rows == null) _rows = new List<ModelRow>();

            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            TryllEditorTheme.Attach(root, paintWindowBackground: false);

            root.Add(BuildToolbar());
            _banner = new TryllBanner();
            root.Add(_banner);
            MaybeAddMultiManifestBanner(root);

            LoadLayoutPrefs();

            var inner = new TryllInspectorSplit(
                BuildMiddle(),
                BuildRight(),
                TryllSplitFixedSide.Second,
                () => TryllEditorUiContract.ModelListMinWidth,
                () => TryllEditorUiContract.ModelPropsMinWidth,
                TryllEditorUiContract.ModelPropsDefaultWidth,
                () => _propsWidth,
                PersistPropsWidth,
                () => true);

            var outer = new TryllInspectorSplit(
                BuildLeft(),
                inner,
                TryllSplitFixedSide.First,
                () => TryllEditorUiContract.ModelFilterMinWidth,
                () => TryllEditorUiContract.ModelListMinWidth
                    + TryllInspectorSplit.HandleWidth
                    + TryllEditorUiContract.ModelPropsMinWidth,
                TryllEditorUiContract.ModelFilterDefaultWidth,
                () => _filterWidth,
                PersistFilterWidth,
                () => true);
            root.Add(outer);

            _dlOverlay = BuildDownloadsOverlay();
            root.Add(_dlOverlay);

            RefreshAll();
            if (_session == null || _session.CurrentState == TryllEditorSession.State.Idle)
                Connect();
        }

        /// <summary>
        /// Non-blocking error banner when the project has more than one
        /// <see cref="TryllModelManifest"/> — packaging and this window both use the
        /// sorted-first asset; the rest are ignored until the duplicates are removed.
        /// </summary>
        private static void MaybeAddMultiManifestBanner(VisualElement root)
        {
            var all = TryllModelManifest.FindProjectManifests();
            if (all.Count <= 1) return;

            string active = all[0].path;
            var paths = string.Join("\n", all.ConvertAll(x => "  • " + x.path));
            var box = new HelpBox(
                $"Multiple TryllModelManifest assets found ({all.Count}). " +
                $"Using '{active}' for registration; remove the extras so packaging stays predictable:\n{paths}",
                HelpBoxMessageType.Error);
            box.style.marginLeft = 8; box.style.marginRight = 8;
            box.style.marginTop = 4; box.style.marginBottom = 4;
            box.style.flexShrink = 0;
            root.Add(box);
        }

        // ── Top toolbar ──────────────────────────────────────────────────────

        private VisualElement BuildToolbar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.paddingLeft = 8; bar.style.paddingRight = 8;
            bar.style.paddingTop = 5; bar.style.paddingBottom = 5;
            bar.style.borderBottomWidth = 1; bar.style.borderBottomColor = Hairline;
            bar.style.flexShrink = 0;

            var title = new Label("Tryll Model Manager");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            bar.Add(title);

            var note = new Label("live · server-backed");
            note.style.color = Dim; note.style.marginLeft = 8; note.style.fontSize = 11;
            bar.Add(note);

            var spacer = new VisualElement(); spacer.style.flexGrow = 1; bar.Add(spacer);

            _statusBar = new TryllStatusBar();
            _statusBar.style.marginRight = 4;
            bar.Add(_statusBar);

            bar.Add(new TryllServerModeBadge(() => _session?.Client ?? TryllClient.Instance));

            var dlBtn = new Button(ToggleDownloads) { text = TryllEditorUiContract.IconDownload };
            dlBtn.tooltip = "Show active and recent downloads";
            dlBtn.style.marginRight = 2;
            bar.Add(dlBtn);

            _dlBadge = new Label("0");
            _dlBadge.style.color = ColWarn; _dlBadge.style.fontSize = 11;
            _dlBadge.style.unityFontStyleAndWeight = FontStyle.Bold; _dlBadge.style.marginRight = 8;
            _dlBadge.style.display = DisplayStyle.None;
            bar.Add(_dlBadge);

            var downloadAll = new Button(OnDownloadAll) { text = "Download all" };
            downloadAll.style.marginRight = 4;
            downloadAll.tooltip = "Download every not-downloaded model in the current list";
            bar.Add(downloadAll);

            var refresh = new Button(ReloadModels) { text = "Refresh", name = TryllEditorElementNames.Refresh };
            bar.Add(refresh);

            return bar;
        }

        // ── Left: filters ────────────────────────────────────────────────────

        private VisualElement BuildLeft()
        {
            var col = new VisualElement();
            col.style.flexGrow = 1;
            col.style.borderRightWidth = 1; col.style.borderRightColor = Hairline;
            col.style.backgroundColor = PanelTint;

            col.Add(PanelHeader("Filters"));

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;

            // Search — always visible, no foldout
            var searchSection = new VisualElement();
            searchSection.style.paddingLeft = 8; searchSection.style.paddingRight = 8;
            searchSection.style.paddingTop = 8; searchSection.style.paddingBottom = 8;
            searchSection.style.borderBottomWidth = 1; searchSection.style.borderBottomColor = Hairline;
            var searchField = new TextField { value = _search };
            searchField.style.marginBottom = 0;
            searchField.RegisterValueChangedCallback(e => { _search = (e.newValue ?? "").ToLowerInvariant(); RefreshList(); });
            searchSection.Add(searchField);
            scroll.Add(searchSection);

            // Model type
            var typeF = new Foldout { text = "Model type", value = true };
            foreach (var t in AllTypes)
                typeF.Add(FilterToggle(TypeLabel(t), $"type:{t}", _fType.Contains(t), v => { if (v) _fType.Add(t); else _fType.Remove(t); }));
            scroll.Add(typeF);

            // Engine
            var engF = new Foldout { text = "Engine", value = true };
            foreach (var e in AllEngines)
                engF.Add(FilterToggle(e, $"engine:{e}", _fEngine.Contains(e), v => { if (v) _fEngine.Add(e); else _fEngine.Remove(e); }));
            scroll.Add(engF);

            // Download status
            var statF = new Foldout { text = "Download status", value = false };
            foreach (var s in AllStatuses)
                statF.Add(FilterToggle(StatusLabel(s), $"status:{s}", _fStatus.Contains(s), v => { if (v) _fStatus.Add(s); else _fStatus.Remove(s); }));
            scroll.Add(statF);

            // Project registration
            var regF = new Foldout { text = "Project registration", value = true };
            foreach (var r in AllRegs)
                regF.Add(FilterToggle(RegLabel(r), $"reg:{r}", _fReg.Contains(r), v => { if (v) _fReg.Add(r); else _fReg.Remove(r); }));
            scroll.Add(regF);

            // Project totals
            var totF = new Foldout { text = "Project totals", value = true };
            _stProd  = TotalLine(totF, "Production");
            _stExp   = TotalLine(totF, "Experimental");
            _stDisk  = TotalLine(totF, "Downloaded on disk");
            _stBytes = TotalLine(totF, "Disk usage");
            _stShip  = TotalLine(totF, "Ships in build");
            _stShip.style.color = ColOk;
            scroll.Add(totF);

            // Reset filters — flat, at the bottom
            var resetBtn = new Button(ResetFilters) { text = "Reset filters" };
            resetBtn.style.marginTop = 8; resetBtn.style.marginBottom = 8;
            resetBtn.style.marginLeft = 8; resetBtn.style.marginRight = 8;
            scroll.Add(resetBtn);

            col.Add(scroll);
            return col;
        }

        private VisualElement FilterToggle(string label, string key, bool initial, System.Action<bool> onChange)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            // Checkbox on the left (label-less Toggle), then the label, matching Unreal.
            var tog = new Toggle { value = initial };
            tog.RegisterValueChangedCallback(e => { onChange(e.newValue); RefreshList(); RefreshCounts(); });

            var lbl = new Label(label);
            lbl.style.flexGrow = 1; lbl.style.marginLeft = 2;
            lbl.RegisterCallback<MouseDownEvent>(_ => tog.value = !tog.value);

            var count = new Label("0");
            count.style.color = Dim; count.style.fontSize = 11;
            count.style.unityTextAlign = TextAnchor.MiddleRight;
            count.style.flexShrink = 0; count.style.minWidth = 24; count.style.marginLeft = 4;

            row.Add(tog); row.Add(lbl); row.Add(count);
            _countLabels[key] = count;
            _toggles[key] = tog;
            return row;
        }

        private Label TotalLine(VisualElement parent, string label)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            var k = new Label(label); k.style.color = Dim; k.style.fontSize = 11;
            var v = new Label("0"); v.style.unityFontStyleAndWeight = FontStyle.Bold; v.style.fontSize = 11;
            row.Add(k); row.Add(v);
            parent.Add(row);
            return v;
        }

        // ── Middle: model list ──────────────────────────────────────────────────

        private VisualElement BuildMiddle()
        {
            var col = new VisualElement();
            col.style.flexGrow = 1;

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.alignItems = Align.Center;
            head.style.flexShrink = 0; head.style.minHeight = 28;
            head.style.paddingLeft = 8; head.style.paddingRight = 8;
            head.style.paddingTop = 4; head.style.paddingBottom = 4;
            head.style.borderBottomWidth = 1; head.style.borderBottomColor = Hairline;

            _countLabel = new Label("0 models");
            _countLabel.style.color = Dim; _countLabel.style.fontSize = 11;
            head.Add(_countLabel);

            var sp = new VisualElement(); sp.style.flexGrow = 1; head.Add(sp);

            var sortLbl = new Label("Sort"); sortLbl.style.color = Dim; sortLbl.style.fontSize = 11; sortLbl.style.marginRight = 4;
            head.Add(sortLbl);
            var sort = new DropdownField(
                new List<string> { "Name", "Type", "Size", "Status", "Registration" },
                (int)_sort);
            sort.RegisterValueChangedCallback(e => { _sort = (SortKey)sort.index; RefreshList(); });
            sort.style.width = 130;
            sort.style.marginTop = 0; sort.style.marginBottom = 0;
            head.Add(sort);

            col.Add(head);

            _listScroll = new ScrollView(ScrollViewMode.Vertical);
            _listScroll.style.flexGrow = 1;
            col.Add(_listScroll);

            col.Add(BuildLegend());
            return col;
        }

        private VisualElement BuildLegend()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.flexShrink = 0; // never let flexbox compress this below its wrapped content height
            row.style.paddingLeft = 8; row.style.paddingRight = 8;
            row.style.paddingTop = 4; row.style.paddingBottom = 4;
            row.style.borderTopWidth = 1; row.style.borderTopColor = Hairline;
            row.Add(LegendItem(TryllModelStatus.Loaded,      "Loaded"));
            row.Add(LegendItem(TryllModelStatus.Downloaded,  "Downloaded"));
            row.Add(LegendItem(TryllModelStatus.Local,       "Local"));
            row.Add(LegendItem(TryllModelStatus.Incomplete,  "Incomplete"));
            row.Add(LegendItem(TryllModelStatus.Absent,      "Not downloaded"));
            return row;
        }

        private VisualElement LegendItem(TryllModelStatus s, string text)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row; item.style.alignItems = Align.Center;
            item.style.marginRight = 12; item.style.marginBottom = 2;
            item.Add(StatusDot(s));
            var l = new Label(text); l.style.color = Dim; l.style.fontSize = 11; l.style.marginLeft = 4;
            item.Add(l);
            return item;
        }

        private void RefreshList()
        {
            if (_listScroll == null) return;
            _listScroll.Clear();

            var filtered = Filtered().ToList();
            foreach (var row in filtered)
                _listScroll.Add(MakeRow(row));

            _countLabel.text = $"{filtered.Count} of {VisibleRows().Count()} models";
        }

        private IEnumerable<ModelRow> VisibleRows() => _rows.Where(r => !r.Hidden);

        private IEnumerable<ModelRow> Filtered()
        {
            var q = VisibleRows().Where(r =>
                TryllModelManagerPresentation.MatchesFilters(
                    TryllModelManagerPresentation.Included(_fType, r.Type, emptyMeansAll: false),
                    TryllModelManagerPresentation.Included(_fEngine, r.Engine, emptyMeansAll: false),
                    TryllModelManagerPresentation.Included(_fStatus, r.Info.Status, emptyMeansAll: false),
                    audienceOk: true,
                    TryllModelManagerPresentation.Included(_fReg, r.Reg, emptyMeansAll: false),
                    r.Info.Name,
                    r.Info.HuggingFaceRepo,
                    _search));

            switch (_sort)
            {
                case SortKey.Type:         q = q.OrderBy(r => r.Type).ThenBy(r => r.Info.Name); break;
                case SortKey.Size:         q = q.OrderByDescending(r => r.Info.SizeBytes); break;
                case SortKey.Status:       q = q.OrderBy(r => System.Array.IndexOf(AllStatuses, r.Info.Status)).ThenBy(r => r.Info.Name); break;
                case SortKey.Registration: q = q.OrderBy(r => System.Array.IndexOf(AllRegs, r.Reg)).ThenBy(r => r.Info.Name); break;
                default:                   q = q.OrderBy(r => r.Info.Name); break;
            }
            return q;
        }

        private VisualElement MakeRow(ModelRow row)
        {
            var r = new VisualElement { name = TryllEditorElementNames.ModelRow };
            r.style.flexDirection = FlexDirection.Row;
            r.style.alignItems = Align.Center;
            r.style.paddingLeft = 9; r.style.paddingRight = 10;
            r.style.paddingTop = 6; r.style.paddingBottom = 6;
            r.style.borderBottomWidth = 1; r.style.borderBottomColor = Hairline;

            if (row == _selected)
            {
                r.style.backgroundColor = SelBg;
                r.style.borderLeftWidth = 3; r.style.borderLeftColor = Accent;
            }
            r.RegisterCallback<MouseDownEvent>(_ => Select(row));

            r.Add(StatusDot(row.Info.Status));

            var nameCol = new VisualElement();
            nameCol.style.flexGrow = 1; nameCol.style.marginLeft = 9; nameCol.style.overflow = Overflow.Hidden;
            var name = new Label(row.Info.Name);
            name.style.unityTextOverflowPosition = TextOverflowPosition.End;
            name.style.overflow = Overflow.Hidden;
            nameCol.Add(name);
            var meta = new Label($"{TypeLabel(row.Type)}  ·  {row.Engine}");
            meta.style.color = Dim; meta.style.fontSize = 11;
            nameCol.Add(meta);
            r.Add(nameCol);

            // Placement sits to the left of the registration pill so PROD / EXP / —
            // stay in one column. The name flexes around a RAM/VRAM tag; an Auto
            // marker on every row would be noise, and Auto is what "no opinion"
            // looks like.
            if (row.Reg != Registration.None && SupportsPlacement(row.Type))
            {
                var place = PlacementOf(row);
                if (place != TryllMemoryPlacement.Auto)
                    r.Add(PlacementPill(place));
            }

            r.Add(RegPill(row.Reg));

            var size = new Label(
                row.Info.Status == TryllModelStatus.Absent ? "—"
                : row.Info.Status == TryllModelStatus.Downloading ? "…"
                : FmtBytes(row.Info.SizeBytes));
            size.style.color = Dim; size.style.fontSize = 11; size.style.minWidth = 56;
            size.style.marginLeft = 8; size.style.unityTextAlign = TextAnchor.MiddleRight;
            r.Add(size);

            return r;
        }

        private void Select(ModelRow row)
        {
            _selected = row;
            RefreshList();
            RefreshProps();
        }

        // ── Right: properties + controls ─────────────────────────────────────────

        private VisualElement BuildRight()
        {
            var col = new VisualElement();
            col.style.flexGrow = 1;
            col.style.borderLeftWidth = 1; col.style.borderLeftColor = Hairline;
            col.style.backgroundColor = PanelTint;

            col.Add(PanelHeader("Model properties"));

            _propsScroll = new ScrollView(ScrollViewMode.Vertical);
            _propsScroll.style.flexGrow = 1;
            col.Add(_propsScroll);
            return col;
        }

        private void RefreshProps()
        {
            if (_propsScroll == null) return;
            _downloadProgressBar = null;
            _propsScroll.Clear();

            var m = _selected;
            if (m == null)
            {
                var empty = new Label(TryllEditorUiContract.ModelManagerSelect);
                empty.style.color = Dim; empty.style.whiteSpace = WhiteSpace.Normal;
                empty.style.paddingLeft = 16; empty.style.paddingRight = 16; empty.style.paddingTop = 40;
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                _propsScroll.Add(empty);
                return;
            }

            // Header
            var header = SectionBox(null);
            var nm = new Label(m.Info.Name);
            nm.style.fontSize = 15; nm.style.unityFontStyleAndWeight = FontStyle.Bold;
            nm.style.whiteSpace = WhiteSpace.Normal;
            header.Add(nm);
            var tags = new VisualElement();
            tags.style.flexDirection = FlexDirection.Row; tags.style.flexWrap = Wrap.Wrap; tags.style.marginTop = 6;
            tags.Add(Badge(TypeLabel(m.Type)));
            tags.Add(Badge(m.Engine));
            tags.Add(Badge($"audience: {m.Audience}"));
            header.Add(tags);
            _propsScroll.Add(header);

            AddLanguageRow(m);

            // Status
            bool downloading = m.Info.Status == TryllModelStatus.Downloading;
            bool onDisk = m.Info.Status == TryllModelStatus.Downloaded
                       || m.Info.Status == TryllModelStatus.Local
                       || m.Info.Status == TryllModelStatus.Loaded;
            var st = SectionBox("Status");
            KV(st, "State", StatusLabel(m.Info.Status));
            KV(st, "On disk", onDisk ? FmtBytes(m.Info.SizeBytes) : "—");
            KV(st, "Location", string.IsNullOrEmpty(m.Info.LocalPath) ? "—" : m.Info.LocalPath);
            if (downloading)
            {
                float progress = _progress.TryGetValue(m.Info.Name, out var pct) ? pct : 0f;
                _downloadProgressBar = new ProgressBar { value = progress, title = $"{(int)progress}%" };
                _downloadProgressBar.style.marginTop = 8;
                st.Add(_downloadProgressBar);
            }
            _propsScroll.Add(st);

            // Build registration
            var reg = SectionBox("Build registration  (this project)");
            var seg = new VisualElement();
            seg.style.flexDirection = FlexDirection.Row;
            seg.Add(RegButton(m, Registration.None, "Not registered"));
            seg.Add(RegButton(m, Registration.Experimental, "Experimental"));
            seg.Add(RegButton(m, Registration.Production, "Production"));
            reg.Add(seg);
            var hint = new Label(
                m.Reg == Registration.Production
                    ? "Shipped: files are copied into the packaged build and survive the build filter."
                    : m.Reg == Registration.Experimental
                        ? "Dev-only: usable in the editor, but stripped from the final build by the packaging filter."
                        : "Not part of this project; ignored by the build.");
            hint.style.color = Dim; hint.style.fontSize = 11; hint.style.whiteSpace = WhiteSpace.Normal; hint.style.marginTop = 6;
            reg.Add(hint);
            _propsScroll.Add(reg);

            // Memory placement. Only meaningful for a registered model that this
            // control can actually affect — an unregistered model is never loaded, and
            // a VAD model is not placed at all (see SupportsPlacement).
            if (m.Reg != Registration.None && SupportsPlacement(m.Type))
            {
                var place = SectionBox("Memory placement  (this project)");
                var pseg = new VisualElement();
                pseg.style.flexDirection = FlexDirection.Row;
                pseg.Add(PlacementButton(m, TryllMemoryPlacement.Auto, "Auto"));
                pseg.Add(PlacementButton(m, TryllMemoryPlacement.Ram,  "RAM"));
                pseg.Add(PlacementButton(m, TryllMemoryPlacement.Vram, "VRAM"));
                place.Add(pseg);

                var ph = new Label(PlacementHintText(m));
                ph.style.color = Dim; ph.style.fontSize = 11;
                ph.style.whiteSpace = WhiteSpace.Normal; ph.style.marginTop = 6;
                place.Add(ph);
                _propsScroll.Add(place);
            }

            // Hotword biasing. STT only — nothing else has a decoder to bias — and
            // only once registered, for the same reason placement is: an
            // unregistered model is never loaded, so the choice would be stored and
            // never read.
            if (m.Reg != Registration.None && SupportsHotwordBias(m.Type))
            {
                var hw = SectionBox("Hotword biasing  (this project)");
                var hseg = new VisualElement();
                hseg.style.flexDirection = FlexDirection.Row;
                hseg.Add(HotwordButton(m, TryllHotwordBias.Default, "Default"));
                hseg.Add(HotwordButton(m, TryllHotwordBias.Off,     "Off"));
                hseg.Add(HotwordButton(m, TryllHotwordBias.On,      "On"));
                hw.Add(hseg);

                var hh = new Label(HotwordHintText(m));
                hh.style.color = Dim; hh.style.fontSize = 11;
                hh.style.whiteSpace = WhiteSpace.Normal; hh.style.marginTop = 6;
                hw.Add(hh);
                _propsScroll.Add(hw);
            }

            // Controls
            var ctl = SectionBox("Controls");
            var crow = new VisualElement(); crow.style.flexDirection = FlexDirection.Row;
            if (downloading)
            {
                var dlBtn = new Button { text = "Downloading…" };
                dlBtn.style.flexGrow = 1; dlBtn.style.marginRight = 4; dlBtn.SetEnabled(false);
                crow.Add(dlBtn);
                var ldBtn = new Button { text = "Load" };
                ldBtn.style.flexGrow = 1; ldBtn.SetEnabled(false);
                crow.Add(ldBtn);
            }
            else if (onDisk)
            {
                var del = new Button(() => OnDelete(m)) { text = "Delete files" };
                del.style.flexGrow = 1; del.style.marginRight = 4;
                crow.Add(del);
                if (m.Info.Status == TryllModelStatus.Loaded)
                {
                    var ul = new Button(() => OnLoadToggle(m, false)) { text = "Unload" };
                    ul.style.flexGrow = 1; crow.Add(ul);
                }
                else
                {
                    var ld = new Button(() => OnLoadToggle(m, true)) { text = "Load" };
                    ld.style.flexGrow = 1; crow.Add(ld);
                }
            }
            else
            {
                var dl = new Button(() => OnDownload(m)) { text = "Download" };
                dl.style.flexGrow = 1; dl.style.marginRight = 4;
                crow.Add(dl);
                var ldDis = new Button { text = "Load" };
                ldDis.style.flexGrow = 1; ldDis.SetEnabled(false);
                crow.Add(ldDis);
            }
            ctl.Add(crow);
            _propsScroll.Add(ctl);

            // Variant
            var va = SectionBox($"Variant — {m.Engine}");
            bool isLocal = m.Info.Status == TryllModelStatus.Local || string.IsNullOrEmpty(m.Info.HuggingFaceRepo);
            KV(va, "Source", isLocal ? "local_path" : "huggingface_repo");
            if (isLocal)
            {
                KV(va, "Repo", "(local path)");
            }
            else
            {
                string url = $"https://huggingface.co/{m.Info.HuggingFaceRepo}";
                var link = new Label($"<u>{m.Info.HuggingFaceRepo}</u>");
                link.style.color = Accent;
                link.style.whiteSpace = WhiteSpace.Normal; link.style.flexGrow = 1;
                link.tooltip = url;
                link.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL(url));
                KVRow(va, "Repo", link);
            }
            if (m.ContextSize > 0) KV(va, "context_size", m.ContextSize.ToString());
            if (!string.IsNullOrEmpty(m.ToolFormat)) KV(va, "tool_call", m.ToolFormat);
            // Advisory catalog verdict — detail pane only, never in the main list.
            if (!string.IsNullOrEmpty(m.ToolSupport)) KV(va, "tool_calls", m.ToolSupport);
            var filesLbl = new Label("files"); filesLbl.style.color = Dim; filesLbl.style.fontSize = 11; filesLbl.style.marginTop = 6;
            va.Add(filesLbl);
            foreach (var f in m.Files)
            {
                var fl = new Label(f); fl.style.color = Dim; fl.style.fontSize = 11; fl.style.whiteSpace = WhiteSpace.Normal;
                va.Add(fl);
            }
            _propsScroll.Add(va);

            // Licence — read from the local catalog; ListModels does not carry it.
            AddLicenceSection(m);

            // Default sampling (language only)
            if (!string.IsNullOrEmpty(m.Sampling))
            {
                var sa = SectionBox("Default sampling");
                var sl = new Label(m.Sampling); sl.style.whiteSpace = WhiteSpace.Normal;
                sa.Add(sl);
                _propsScroll.Add(sa);
            }
        }

        private Button RegButton(ModelRow m, Registration reg, string label)
        {
            var b = new Button(() => { SetRegistration(m, reg); RefreshAll(); }) { text = label };
            b.style.flexGrow = 1; b.style.fontSize = 11;
            if (m.Reg == reg)
            {
                Color c = reg == Registration.Production ? ColOk : reg == Registration.Experimental ? ColWarn : Dim;
                b.style.color = c;
                b.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            return b;
        }

        private Button PlacementButton(ModelRow m, TryllMemoryPlacement place, string label)
        {
            var b = new Button(() => { SetPlacement(m, place); RefreshAll(); }) { text = label };
            b.style.flexGrow = 1; b.style.fontSize = 11;
            if (PlacementOf(m) == place)
            {
                // Auto is the default, so it reads as neutral rather than a choice
                // the user made; RAM/VRAM are deliberate and highlighted as such.
                b.style.color = place == TryllMemoryPlacement.Auto ? Dim : ColOk;
                b.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            return b;
        }

        /// <summary>
        /// Whether hotword biasing means anything for this model type. STT only:
        /// biasing happens inside the speech recognizer's beam search, so there is
        /// nothing for a language, embedding, TTS or VAD entry to apply it to.
        ///
        /// Not narrowed further here. Whether a *particular* STT model can bias
        /// also depends on its decoder family and whether a sub-word vocabulary can
        /// be derived from its bundle, and neither is knowable offline — the server
        /// answers that at load, and refuses a lexicon with a reason when it cannot.
        /// Hiding the control on a guess would be worse: an author would conclude
        /// the feature does not exist.
        /// </summary>
        private static bool SupportsHotwordBias(ModelType type) => type == ModelType.Stt;

        private Button HotwordButton(ModelRow m, TryllHotwordBias bias, string label)
        {
            var b = new Button(() => { SetHotwordBias(m, bias); RefreshAll(); }) { text = label };
            b.style.flexGrow = 1; b.style.fontSize = 11;
            if (HotwordBiasOf(m) == bias)
            {
                // Default reads as neutral — it is the absence of an opinion, not a
                // decision — while Off and On are deliberate and highlighted.
                b.style.color = bias == TryllHotwordBias.Default ? Dim : ColOk;
                b.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            return b;
        }

        private TryllHotwordBias HotwordBiasOf(ModelRow m)
        {
            if (_manifest == null) return TryllHotwordBias.Default;
            foreach (var e in _manifest.Entries)
                if (e.Name == m.Info.Name) return e.Hotwords;
            return TryllHotwordBias.Default;
        }

        /// <summary>
        /// Persist a hotword-bias choice. Mirrors SetPlacement: never adds or
        /// removes an entry, and does nothing for an unregistered model.
        /// </summary>
        private void SetHotwordBias(ModelRow m, TryllHotwordBias bias)
        {
            EnsureManifest();
            if (_manifest.Get(m.Info.Name) == TryllModelRegistration.None)
                return;

            _manifest.Set(m.Info.Name, m.Info.ModelType,
                          _manifest.Get(m.Info.Name),
                          m.Info.Languages, ParseEngine(m.Info.Engine),
                          PlacementOf(m), bias);
            SaveManifest();
        }

        /// <summary>
        /// What this choice actually does, said plainly — including the part an
        /// author would otherwise discover from a profiler.
        /// </summary>
        private static string HotwordHintText(ModelRow m)
        {
            return "Hotwords bias the recognizer toward names and terms you supply "
                 + "per voice input. Default follows the server's own setting for "
                 + "this model. On and Off decide instead of hinting: biasing needs "
                 + "beam-search decoding, which is chosen when the model loads, so "
                 + "On costs a few percent of decode time and changes this model's "
                 + "transcripts even for voice inputs that supply no hotwords. Off "
                 + "makes supplying them an error rather than a silent no-op.";
        }

        private TryllMemoryPlacement PlacementOf(ModelRow m)
        {
            if (_manifest == null) return TryllMemoryPlacement.Auto;
            foreach (var e in _manifest.Entries)
                if (e.Name == m.Info.Name) return e.Placement;
            return TryllMemoryPlacement.Auto;
        }

        /// <summary>
        /// What this placement will actually mean, said plainly. Placement is a hint
        /// authored here and shipped to players' machines, so the text avoids
        /// promising anything about hardware we cannot see.
        /// </summary>
        private static string PlacementHintText(ModelRow m)
        {
            switch (m.Type)
            {
                case ModelType.Tts:
                    return "Auto keeps TTS on the CPU — measured faster for small "
                         + "voice graphs, where host/device copies cost more than they "
                         + "save. Override only if you have measured otherwise on your "
                         + "target hardware.";
                default:
                    return "Auto uses device memory when the player's machine has a "
                         + "usable GPU, system memory otherwise. RAM and VRAM are hints, "
                         + "not guarantees: a machine that cannot honour one loads the "
                         + "model the other way and reports what it did. Whether VRAM "
                         + "means CUDA or Vulkan is the host's decision, not yours.";
            }
        }

        // ── Actions ──────────────────────────────────────────────────────────────

        private void OnDownload(ModelRow m)
        {
            var client = TryllClient.Instance;
            if (client == null || !client.IsConnected)
            {
                _operationalError = "Not connected";
                RefreshStatusChrome();
                return;
            }
            m.Info.Status = TryllModelStatus.Downloading;
            _progress[m.Info.Name] = 0f;
            client.RequestDownloadModel(m.Info.Name);

            if (m.Type == ModelType.Stt)
            {
                foreach (var vad in VadCompanions())
                {
                    if (vad.Info.Status != TryllModelStatus.Absent) continue;
                    if (string.IsNullOrEmpty(vad.Info.HuggingFaceRepo)) continue;

                    string sizeHint = vad.Info.SizeBytes > 0 ? FmtBytes(vad.Info.SizeBytes) : "size unknown until downloaded";
                    bool download = TryllEditorPrompts.Confirm("Tryll — download VAD companion?",
                        $"Also download \"{vad.Info.Name}\" ({sizeHint}) for automatic end-of-speech detection?\n\n" +
                        "Choose No if you control utterance boundaries yourself (push-to-talk / hands-free).\n\n" +
                        $"Learn more: {VadCompanionDocsUrl}",
                        "Yes", "No");
                    if (!download) continue;

                    vad.Info.Status = TryllModelStatus.Downloading;
                    _progress[vad.Info.Name] = 0f;
                    client.RequestDownloadModel(vad.Info.Name);
                }
            }

            RefreshAll();
        }

        private void OnDownloadAll()
        {
            var client = TryllClient.Instance;
            if (client == null || !client.IsConnected)
            {
                _operationalError = "Not connected";
                RefreshStatusChrome();
                return;
            }
            // Download every not-downloaded model in the current (filtered) list.
            foreach (var row in Filtered())
            {
                if (row.Info.Status != TryllModelStatus.Absent) continue;
                row.Info.Status = TryllModelStatus.Downloading;
                _progress[row.Info.Name] = 0f;
                client.RequestDownloadModel(row.Info.Name);
            }
            RefreshAll();
        }

        private void OnDelete(ModelRow m)
        {
            var client = _session?.Client ?? TryllClient.Instance;
            if (client == null || !client.IsConnected)
            {
                _operationalError = "Not connected";
                RefreshStatusChrome();
                return;
            }

            if (!TryllEditorPrompts.Confirm("Delete model files",
                $"Delete the downloaded files for \"{m.Info.Name}\"?\n\nThis removes them from disk; the model can be re-downloaded later.",
                "Delete", "Cancel"))
                return;
            _progress.Remove(m.Info.Name);
            using (client.AsEditorCall())
                client.RequestDeleteModel(m.Info.Name);

            // Deleting files can turn a still-registered model back to Absent; don't let the
            // Play-mode guard's cache mask that.
            TryllPlayModeModelGuard.Invalidate();
        }

        private void OnDeleteComplete(string modelName, bool success)
        {
            // Re-list so the corrected on-disk status/size is reflected.
            if (success) LoadModelsAsync(); else RefreshAll();
        }

        private void OnLoadToggle(ModelRow m, bool load)
        {
            var client = TryllClient.Instance;
            if (client == null || !client.IsConnected)
            {
                _operationalError = "Not connected";
                RefreshStatusChrome();
                return;
            }
            // Send the selector's current value, not nothing. The session's hints are
            // a snapshot taken at CreateSession, so a placement chosen after that
            // would otherwise be ignored until the session was recreated — which is
            // exactly how this control appeared to do nothing.
            using (client.AsEditorCall())
            {
                if (load) client.RequestLoadModel(m.Info.Name, PlacementOf(m));
                else client.RequestUnloadModel(m.Info.Name);
            }
        }

        private void ResetFilters()
        {
            _search = ""; _sort = SortKey.Name;
            _fType.Clear();   foreach (var t in AllTypes)    _fType.Add(t);
            _fEngine.Clear(); foreach (var e in AllEngines)  _fEngine.Add(e);
            _fStatus.Clear(); foreach (var s in AllStatuses) _fStatus.Add(s);
            _fReg.Clear();    foreach (var r in AllRegs)     _fReg.Add(r);
            foreach (var kv in _toggles) kv.Value.SetValueWithoutNotify(true);
            RefreshAll();
        }

        // ── Refresh orchestration ────────────────────────────────────────────────

        private void RefreshAll() { RefreshList(); RefreshCounts(); RefreshProps(); }

        private void RefreshCounts()
        {
            var vis = VisibleRows().ToList();
            foreach (var t in AllTypes)    SetCount($"type:{t}",   vis.Count(r => r.Type == t));
            foreach (var e in AllEngines)  SetCount($"engine:{e}", vis.Count(r => r.Engine == e));
            foreach (var s in AllStatuses) SetCount($"status:{s}", vis.Count(r => r.Info.Status == s));
            foreach (var r in AllRegs)     SetCount($"reg:{r}",    vis.Count(x => x.Reg == r));

            var prod = vis.Where(r => r.Reg == Registration.Production).ToList();
            var exp  = vis.Where(r => r.Reg == Registration.Experimental).ToList();
            var disk = vis.Where(r => r.Info.Status != TryllModelStatus.Absent && r.Info.Status != TryllModelStatus.Downloading).ToList();
            if (_stProd  != null) _stProd.text  = prod.Count.ToString();
            if (_stExp   != null) _stExp.text   = exp.Count.ToString();
            if (_stDisk  != null) _stDisk.text  = disk.Count.ToString();
            if (_stBytes != null) _stBytes.text = FmtBytes(disk.Sum(r => r.Info.SizeBytes));
            if (_stShip  != null) _stShip.text  = $"{prod.Count} · {FmtBytes(prod.Sum(r => r.Info.SizeBytes))}";
        }

        private void SetCount(string key, int n) { if (_countLabels.TryGetValue(key, out var l)) l.text = n.ToString(); }

        // ── Small UI helpers ─────────────────────────────────────────────────────

        private static VisualElement PanelHeader(string text)
        {
            var h = new Label(text.ToUpperInvariant());
            h.style.fontSize = 10; h.style.color = Dim;
            h.style.paddingLeft = 12; h.style.paddingRight = 12;
            h.style.paddingTop = 7; h.style.paddingBottom = 7;
            h.style.borderBottomWidth = 1; h.style.borderBottomColor = Hairline;
            h.style.unityFontStyleAndWeight = FontStyle.Bold;
            return h;
        }

        private static void KV(VisualElement parent, string k, string v)
        {
            var val = new Label(v); val.style.whiteSpace = WhiteSpace.Normal; val.style.flexGrow = 1;
            KVRow(parent, k, val);
        }

        private static void KVRow(VisualElement parent, string k, VisualElement v)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; row.style.marginBottom = 2;
            var kl = new Label(k); kl.style.color = Dim; kl.style.width = 96; kl.style.flexShrink = 0;
            row.Add(kl); row.Add(v);
            parent.Add(row);
        }

        /// <summary>
        /// The catalog's <c>licence_url</c> for this model and engine, with a reminder to
        /// check it before shipping. Models without one (user catalogs, local paths) say so
        /// rather than hiding the section — no link is not the same as no licence terms.
        /// </summary>
        private void AddLicenceSection(ModelRow m)
        {
            var box = SectionBox("Licence");
            var notice = new Label(TryllEditorUiContract.ModelLicenceNotice);
            notice.style.whiteSpace = WhiteSpace.Normal; notice.style.marginBottom = 4;

            if (TryllModelCatalogFile.TryGetLicenceUrl(m.Info.Name, m.Engine, out var url))
            {
                box.Add(notice);
                var link = new Label($"<u>{url}</u>");
                link.style.color = Accent;
                link.style.whiteSpace = WhiteSpace.Normal; link.style.flexGrow = 1;
                link.tooltip = url;
                link.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL(url));
                box.Add(link);
            }
            else
            {
                var missing = new Label(TryllEditorUiContract.ModelLicenceMissing);
                missing.style.color = Dim; missing.style.whiteSpace = WhiteSpace.Normal;
                box.Add(missing);
            }
            _propsScroll.Add(box);
        }

        /// <summary>
        /// Languages this model declares, plus whether it covers the project's
        /// selected language. Shown for every model type — a language model
        /// declares nothing today, and saying so explicitly is more useful than
        /// an absent row that reads as "no languages".
        /// </summary>
        private void AddLanguageRow(ModelRow m)
        {
            var codes = m.Info.Languages ?? new string[0];
            var box = SectionBox("Languages");

            if (codes.Length == 0)
            {
                var none = new Label(
                    "Not declared in the model catalog. Tryll will not pass this model a "
                    + "language hint, and it is never treated as blocking a language.");
                none.style.color = Dim;
                none.style.whiteSpace = WhiteSpace.Normal;
                box.Add(none);
                _propsScroll.Add(box);
                return;
            }

            var chips = new VisualElement();
            chips.style.flexDirection = FlexDirection.Row;
            chips.style.flexWrap = Wrap.Wrap;
            foreach (var code in codes)
                chips.Add(Badge($"{TryllLanguages.DisplayName(code)} ({code})"));
            box.Add(chips);

            // The actionable part: does this model cover what the project selected?
            string selected = TryllRuntimeSettings.Load()?.Locale;
            if (!string.IsNullOrEmpty(selected))
            {
                string lang = TryllLanguages.LanguageOf(selected);
                bool covered = TryllLanguages.Supports(m.Info, selected);
                var note = new Label(covered
                    ? $"Covers the project language: {TryllLanguages.DisplayName(lang)} ({lang})."
                    : $"Does NOT cover the project language {TryllLanguages.DisplayName(lang)} ({lang}). "
                      + "Agents relying on the session locale with this model are rejected at creation.");
                note.style.whiteSpace = WhiteSpace.Normal;
                note.style.marginTop = 6;
                note.style.color = covered ? ColOk : ColErr;
                box.Add(note);
            }

            _propsScroll.Add(box);
        }

        /// <summary>Compact row marker for a non-Auto placement choice.</summary>
        private static Label PlacementPill(TryllMemoryPlacement place)
        {
            var p = new Label(place == TryllMemoryPlacement.Vram ? "VRAM" : "RAM");
            p.style.fontSize = 10;
            p.style.color = ColOk;
            p.style.width = 46;
            p.style.unityTextAlign = TextAnchor.MiddleCenter;
            p.style.backgroundColor = new Color(ColOk.r, ColOk.g, ColOk.b, 0.12f);
            p.style.marginRight = 4;
            SetRadius(p, 8);
            return p;
        }

        private static Label Badge(string text)
        {
            var b = new Label(text);
            b.style.fontSize = 10; b.style.color = Dim;
            b.style.paddingLeft = 6; b.style.paddingRight = 6; b.style.paddingTop = 1; b.style.paddingBottom = 1;
            b.style.marginRight = 5; b.style.marginBottom = 3;
            b.style.borderTopWidth = 1; b.style.borderBottomWidth = 1; b.style.borderLeftWidth = 1; b.style.borderRightWidth = 1;
            SetBorderColor(b, Hairline);
            SetRadius(b, 8);
            return b;
        }

        private static Label RegPill(Registration reg)
        {
            string t = reg == Registration.Production ? "PROD" : reg == Registration.Experimental ? "EXP" : "—";
            var p = new Label(t);
            p.style.fontSize = 10; p.style.unityFontStyleAndWeight = FontStyle.Bold;
            p.style.paddingLeft = 7; p.style.paddingRight = 7; p.style.paddingTop = 1; p.style.paddingBottom = 1;
            p.style.unityTextAlign = TextAnchor.MiddleCenter; p.style.minWidth = 38;
            SetRadius(p, 8);
            Color c = reg == Registration.Production ? ColOk : reg == Registration.Experimental ? ColWarn : Dim;
            p.style.color = c;
            p.style.borderTopWidth = 1; p.style.borderBottomWidth = 1; p.style.borderLeftWidth = 1; p.style.borderRightWidth = 1;
            SetBorderColor(p, new Color(c.r, c.g, c.b, 0.5f));
            if (reg != Registration.None) p.style.backgroundColor = new Color(c.r, c.g, c.b, 0.12f);
            return p;
        }

        private static VisualElement StatusDot(TryllModelStatus s)
        {
            var d = new VisualElement();
            d.style.width = 9; d.style.height = 9; d.style.flexShrink = 0;
            SetRadius(d, 5);
            switch (s)
            {
                case TryllModelStatus.Loaded:
                    d.style.backgroundColor = Loaded; break;
                case TryllModelStatus.Downloaded:
                    d.style.backgroundColor = Accent; break;
                case TryllModelStatus.Downloading:
                    d.style.backgroundColor = ColWarn; break;
                case TryllModelStatus.Local:
                    d.style.backgroundColor = Accent;
                    d.style.borderTopWidth = 2; d.style.borderBottomWidth = 2;
                    d.style.borderLeftWidth = 2; d.style.borderRightWidth = 2;
                    SetBorderColor(d, new Color(Accent.r, Accent.g, Accent.b, 0.35f));
                    break;
                case TryllModelStatus.Incomplete:
                    d.style.backgroundColor = ColWarn; break;
                default:
                    // Not downloaded — a solid red dot so missing models stand out.
                    d.style.backgroundColor = ColErr;
                    break;
            }
            return d;
        }

        private static void SetBorderColor(VisualElement e, Color c)
        {
            e.style.borderTopColor = c; e.style.borderBottomColor = c;
            e.style.borderLeftColor = c; e.style.borderRightColor = c;
        }

        // ── Label / format helpers ───────────────────────────────────────────────

        private static string TypeLabel(ModelType t)
        {
            switch (t)
            {
                case ModelType.Language:  return "Language";
                case ModelType.Embedding: return "Embedding";
                case ModelType.Stt:       return "Speech-to-text";
                case ModelType.Tts:       return "Text-to-speech";
                case ModelType.Vad:       return "Voice activity";
                default:                  return t.ToString();
            }
        }

        private static string StatusLabel(TryllModelStatus s)
        {
            switch (s)
            {
                case TryllModelStatus.Loaded:      return "Loaded in memory";
                case TryllModelStatus.Downloaded:  return "Downloaded";
                case TryllModelStatus.Local:       return "Local path";
                case TryllModelStatus.Downloading: return "Downloading";
                case TryllModelStatus.Incomplete:  return "Incomplete (missing shared asset)";
                default:                           return "Absent";
            }
        }

        private static string RegLabel(Registration r)
        {
            switch (r)
            {
                case Registration.Production:   return "● Production";
                case Registration.Experimental: return "◐ Experimental";
                default:                        return "○ Not registered";
            }
        }

        private static string FmtBytes(long b) => TryllModelManagerPresentation.FmtDecimalBytes(b);
    }
}
