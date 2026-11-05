// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using Tryll;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Tryll.Client.Internal;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Editor-only chat window for poking a <see cref="TryllAgentComponent"/> without
    /// entering Play mode. Built with UI Toolkit.
    ///
    /// Voice Input is Off, Chat (editor-owned STT), or Actor component.
    /// Actor component uses a <see cref="TryllVoiceInputComponent"/> on the
    /// agent's GameObject, or the optional override field. That path captures
    /// only in Play Mode. Chat owns the handle and pumps the mic from
    /// <see cref="EditorApplication.update"/> (Edit Mode safe). Off skips
    /// <c>CreateVoiceInput</c>.
    /// </summary>
    public sealed class TryllChatWindow : EditorWindow
    {
        // ── Config (serialized) ────────────────────────────────────────────────

        [SerializeField] private TryllRuntimeSettings     _settings;
        [SerializeField] private TryllAgentComponent      _agent;
        [SerializeField] private TryllVoiceInputComponent _voice;
        // EditorWindows do not reliably keep scene-object refs across domain reload.
        // GlobalObjectId is the stable handle we rebind from when Unity-nulls them.
        [SerializeField] private string _agentGlobalId;
        [SerializeField] private string _voiceGlobalId;

        // Inspector pane geometry — chat-owned, independent of Dialog Lab.
        // Seeded from the window layout once; after that TryllChatConfig (EditorPrefs) wins.
        private const float MinChatWidth = TryllEditorUiContract.ChatMinWidth;
        [SerializeField] private float _inspectorWidth   = TryllEditorUiContract.InspectorDefaultWidth;
        [SerializeField] private bool  _inspectorVisible = true;

        private TryllChatVoiceSource _voiceSource;
        private bool VoiceEnabled => _voiceSource != TryllChatVoiceSource.Off;
        /// <summary>Component hooked for this session. Kept so Stop unhooks the same object Start resolved.</summary>
        private TryllVoiceInputComponent _boundVoice;
        private string    _sttModelName  = "";
        private string    _micDeviceName = "";
        private VoiceMode _voiceMode     = VoiceMode.PushToTalk;

        // ── Connection state ───────────────────────────────────────────────────

        internal enum State { Idle, Connecting, Configuring, CheckingModels, CreatingAgent, CreatingVoiceInput, Ready, Error }
        private State  _state  = State.Idle;
        private string _status = TryllEditorUiContract.StatusIdle;
        private bool   _hooked;

        // Generic connection lifecycle (launch/connect/CreateSession/model-guard) lives
        // in TryllEditorSession, shared with TryllDialogLabWindow — see its doc comment.
        private TryllEditorSession _session;

        // ── Chat (turn-aware) ──────────────────────────────────────────────────

        private readonly List<TryllChatTurnRecord> _turns = new List<TryllChatTurnRecord>();
        private TryllChatTurnRecord _activeTurn;
        private int                 _turnSequence;
        private string              _selectedTurnKey;
        private bool                _ownsAgentLifecycle;
        [SerializeField] private string _input = "";

        // ── Voice state ────────────────────────────────────────────────────────

        private TryllVoiceInput _ownedVoice;
        private AudioClip       _micClip;
        private int             _nativeRate;
        private int             _readPos;
        private TryllChatVoiceUi _voiceUi;
        private bool            _voiceHooked;
        private int             _lastMicPos = -1;
        private double          _lastMicAdvance;

        private TryllVoiceInputComponent ResolvedVoice =>
            TryllChatVoiceState.ResolveComponent(
                _voice, _agent != null ? _agent.GetComponent<TryllVoiceInputComponent>() : null);
        private TryllVoiceInputComponent ActiveVoice => _voiceHooked ? _boundVoice : ResolvedVoice;
        private TryllVoiceInput VoiceHandle    => UsingComponent ? ActiveVoice?.Voice : _ownedVoice;
        private bool            HasVoice       => VoiceHandle != null && VoiceHandle.IsValid;
        private bool            VoiceActive    => VoiceHandle != null && VoiceHandle.IsUtteranceActive;
        private bool            UsingComponent =>
            _voiceSource == TryllChatVoiceSource.Component && ActiveVoice != null;
        private bool            Recording      =>
            TryllChatVoiceState.IsRecording(_voiceUi, UsingComponent && VoiceActive);
        private string          AssistantName  => _agent != null ? _agent.gameObject.name : "Assistant";

        // ── UI references ──────────────────────────────────────────────────────

        private VisualElement _configBox;
        private DropdownField _voiceSourceField;
        private bool          _voiceAutoPicked;
        private VisualElement _voiceSection;
        private ObjectField   _settingsField, _agentField, _voiceField;
        private VisualElement _sttModelRow;
        private DropdownField _sttModelField;
        private Label         _sttTierBadge;
        private DropdownField _micDeviceField;
        private Button        _startStopButton;
        private TryllStatusBar _statusBar;
        private TryllBanner    _banner;
        private ScrollView    _chatScroll;
        private Label         _streamingLabel;
        private TextField     _inputField;
        private Button        _sendButton, _stopButton, _caretButton, _inspToggle;
        private Label         _micButton;
        private bool          _isTurnInFlight;

        private TryllInspectorSplit   _bodySplit;
        private TryllTurnInspectorView _inspectorView;
        private TryllTailFollow        _chatTail;

        // UITK ObjectFields can fire a spurious null while CreateGUI rebuilds.
        // BindSceneRef is skipped until the delayed restore has pushed live refs.
        private bool _suppressSceneBind;


        // ── Lifecycle ──────────────────────────────────────────────────────────

        [MenuItem("Window/Tryll/Chat")]
        public static void Open()
        {
            var w = GetWindow<TryllChatWindow>("Tryll Chat");
            w.minSize = new Vector2(720, 480);
        }

        private void OnEnable()
        {
            RestoreSceneRefs();
            if (_settings == null) AutofindSettings();
            LoadChatConfig();
            EditorApplication.update += OnEditorUpdate;
            TryllEditorTheme.HostColorsChanged += OnHostColorsChanged;
        }

        private void OnDisable()
        {
            TryllEditorTheme.HostColorsChanged -= OnHostColorsChanged;
            EditorApplication.update -= OnEditorUpdate;
            SaveChatConfig();
            if (_state != State.Idle) Stop();
        }

        void OnHostColorsChanged()
        {
            if (_chatScroll == null) return;
            RebuildChat();
            RenderInspector();
            RefreshUI();
        }

        private void OnEditorUpdate()
        {
            if (_voiceUi.InlineCapturing && !UsingComponent) PumpMic();
        }

        // ── UI construction ────────────────────────────────────────────────────

        private void CreateGUI()
        {
            _suppressSceneBind = true;
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            TryllEditorTheme.Attach(root, paintWindowBackground: false);
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;
            root.RegisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyUpEvent>(OnRootKeyUp, TrickleDown.TrickleDown);

            var config   = BuildConfig();
            var toolbar  = BuildToolbar();
            var body     = BuildBody();

            config.style.flexShrink  = 0;
            toolbar.style.flexShrink = 0;

            root.Add(config);
            root.Add(toolbar);
            _banner = new TryllBanner();
            root.Add(_banner);
            root.Add(body);

            RebuildChat();
            RenderInspector();
            RefreshUI();
            // UITK ObjectFields can emit a delayed null after rebuild; rebind once
            // the tree has settled so a ghost/missing assignment does not stick.
            root.schedule.Execute(() =>
            {
                RestoreSceneRefs();
                PushSceneRefsToFields();
                _suppressSceneBind = false;
            }).ExecuteLater(1);
        }

        private VisualElement BuildBody()
        {
            var chatColumn = new VisualElement();
            chatColumn.style.flexGrow = 1;
            chatColumn.style.flexDirection = FlexDirection.Column;
            chatColumn.Add(BuildChat());
            chatColumn.Add(BuildInputRow());

            _inspectorView = new TryllTurnInspectorView();
            _bodySplit = new TryllInspectorSplit(
                chatColumn,
                _inspectorView.Root,
                TryllSplitFixedSide.Second,
                () => MinChatWidth,
                () => TryllTurnInspectorView.MinWidth,
                Mathf.Max(TryllTurnInspectorView.MinWidth, _inspectorWidth),
                () => _inspectorWidth,
                w => _inspectorWidth = w,
                () => _inspectorVisible);
            return _bodySplit;
        }

        private void ToggleInspector()
        {
            _inspectorVisible = !_inspectorVisible;
            if (_inspectorVisible) _bodySplit.Restore();
            else                   _bodySplit.Collapse();
            _inspToggle.text = _inspectorVisible ? "Inspector ▸" : "Inspector ◂";
            SaveChatConfig();
        }

        private void RenderInspector()
        {
            TryllChatTurnRecord selected = null;
            if (!string.IsNullOrEmpty(_selectedTurnKey))
            {
                for (int i = 0; i < _turns.Count; i++)
                {
                    if (_turns[i] != null && _turns[i].TurnKey == _selectedTurnKey)
                    {
                        selected = _turns[i];
                        break;
                    }
                }
            }
            _inspectorView?.Render(TryllChatTurnLogic.ToInspection(selected));
        }

        private VisualElement BuildConfig()
        {
            _configBox = new VisualElement();
            _configBox.style.marginBottom = 6;

            _settingsField = new ObjectField("Tryll Settings")
            {
                objectType = typeof(TryllRuntimeSettings),
                allowSceneObjects = false,
                value = _settings,
            };
            _settingsField.labelElement.style.minWidth = 150;
            _settingsField.RegisterValueChangedCallback(e =>
            {
                _settings = e.newValue as TryllRuntimeSettings;
                RefreshUI();
            });
            _configBox.Add(_settingsField);

            _agentField = new ObjectField("Agent Component")
            {
                objectType = typeof(TryllAgentComponent),
                allowSceneObjects = true,
                value = _agent,
            };
            _agentField.labelElement.style.minWidth = 150;
            _agentField.RegisterValueChangedCallback(e =>
            {
                if (_suppressSceneBind) return;
                var previous = _agent;
                BindSceneRef(ref _agent, ref _agentGlobalId, e.newValue as TryllAgentComponent);
                if (_voiceSource == TryllChatVoiceSource.Component
                    && (_voice == null || _voiceAutoPicked || (previous != null && _voice.gameObject == previous.gameObject)))
                    AssignAgentVoice();
                RefreshUI();
            });
            _configBox.Add(_agentField);

            _voiceSourceField = new DropdownField(
                "Voice Input",
                new List<string> { "Off", "Chat", "Actor component" },
                VoiceSourceLabel());
            _voiceSourceField.labelElement.style.minWidth = 150;
            _voiceSourceField.RegisterValueChangedCallback(e =>
            {
                if (!TryParseVoiceSource(e.newValue, out var source) || source == _voiceSource)
                    return;
                if (source == TryllChatVoiceSource.Chat && !CanEnableVoice())
                {
                    _voiceSourceField.SetValueWithoutNotify(VoiceSourceLabel());
                    return;
                }
                SetVoiceSource(source);
            });
            _configBox.Add(_voiceSourceField);

            _voiceSection = new VisualElement();
            _voiceSection.style.marginLeft = 14;

            _voiceField = new ObjectField("Voice Component")
            {
                objectType = typeof(TryllVoiceInputComponent),
                allowSceneObjects = true,
                value = _voice,
            };
            _voiceField.labelElement.style.minWidth = 136;
            _voiceField.labelElement.style.width = 136;
            _voiceField.RegisterValueChangedCallback(e =>
            {
                if (_suppressSceneBind) return;
                _voiceAutoPicked = false;
                BindSceneRef(ref _voice, ref _voiceGlobalId, e.newValue as TryllVoiceInputComponent);
                RefreshUI();
            });
            _voiceField.tooltip =
                "Filled from the agent's GameObject. Drag another TryllVoiceInputComponent to use that instead. Play Mode only.";
            _voiceSection.Add(_voiceField);

            var sttManifest = TryllModelManifest.FindProjectManifest();
            var sttNames = CollectSttDropdownNames();
            if (sttNames.Count == 0) sttNames.Add("");
            int sttIndex = Mathf.Max(0, sttNames.IndexOf(_sttModelName));

            _sttModelRow = new VisualElement();
            _sttModelRow.style.flexDirection = FlexDirection.Row;
            _sttModelRow.style.alignItems = Align.Center;

            _sttModelField = new DropdownField("STT Model", sttNames, sttIndex);
            _sttModelField.labelElement.style.minWidth = 136;
            _sttModelField.labelElement.style.width = 136;
            _sttModelField.style.flexGrow = 1;
            _sttModelField.RegisterValueChangedCallback(e =>
            {
                _sttModelName = e.newValue ?? "";
                RefreshSttTierBadge(sttManifest);
                SaveChatConfig();
            });
            _sttModelRow.Add(_sttModelField);

            _sttTierBadge = new Label();
            _sttTierBadge.style.fontSize = 10;
            _sttTierBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
            _sttTierBadge.style.unityTextAlign = TextAnchor.MiddleCenter;
            _sttTierBadge.style.width = 92;
            _sttTierBadge.style.marginLeft = 4;
            SetRadius(_sttTierBadge, RadiusCard);
            _sttModelRow.Add(_sttTierBadge);
            RefreshSttTierBadge(sttManifest);

            _voiceSection.Add(_sttModelRow);

            const string defaultMic = "Default (system)";
            var micChoices = new List<string> { defaultMic };
            micChoices.AddRange(Microphone.devices);
            int micIndex = string.IsNullOrEmpty(_micDeviceName)
                ? 0
                : Mathf.Max(0, micChoices.IndexOf(_micDeviceName));
            _micDeviceField = new DropdownField("Mic Device", micChoices, micIndex);
            _micDeviceField.labelElement.style.minWidth = 136;
            _micDeviceField.labelElement.style.width = 136;
            _micDeviceField.RegisterValueChangedCallback(e =>
            {
                _micDeviceName = e.newValue == defaultMic ? "" : e.newValue;
                SaveChatConfig();
            });
            _voiceSection.Add(_micDeviceField);

            _configBox.Add(_voiceSection);

            return _configBox;
        }

        private VisualElement BuildToolbar()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 6;

            _startStopButton = new Button(OnStartStopClicked) { text = "Start", name = TryllEditorElementNames.ChatStart };
            _startStopButton.style.width = 80;
            _startStopButton.style.height = 22;
            row.Add(_startStopButton);

            _statusBar = new TryllStatusBar();
            _statusBar.style.marginLeft = 8;
            _statusBar.style.flexGrow = 1;
            row.Add(_statusBar);

            var spacer = new VisualElement { style = { flexGrow = 1 } };
            row.Add(spacer);

            row.Add(new TryllServerModeBadge(() => _session?.Client ?? TryllClient.Instance));

            // R10.2: opens the Lab pre-seeded with this agent and the current history as
            // a script's seeded steps — the discovery path for a second window plus the
            // capture→promote loop, in one click.
            var compareInLab = new Button(OnCompareInLabClicked) { text = "Compare in Lab…" };
            compareInLab.tooltip =
                "Open Tryll Dialog Lab with this agent, the conversation as seeded history, "
                + "and one blank generated step to fill in before Run";
            row.Add(compareInLab);

            _inspToggle = new Button(ToggleInspector) { text = _inspectorVisible ? "Inspector ▸" : "Inspector ◂" };
            _inspToggle.style.marginLeft = 4;
            row.Add(_inspToggle);

            return row;
        }

        private void OnCompareInLabClicked()
        {
            RestoreSceneRefs();
            SyncConfigFromFields();
            RememberLiveSceneRefs();
            PushSceneRefsToFields();
            if (_agent == null)
            {
                Warn(MissingAgentMessage("comparing in Lab"));
                return;
            }
            TryllDialogLabWindow.OpenFromChat(_agent, BuildLabHistory());
        }

        private List<(bool IsUser, string Text)> BuildLabHistory()
        {
            var result = TryllChatTurnLogic.BuildLabHistory(_turns);
            if (result.SkippedTurnCount > 0)
                Debug.LogWarning(
                    $"[Tryll] Compare in Lab skipped {result.SkippedTurnCount} "
                    + "turn(s) with no assistant text.");
            return result.Items;
        }

        private VisualElement BuildChat()
        {
            _chatScroll = new ScrollView(ScrollViewMode.Vertical);
            _chatScroll.style.flexGrow = 1;
            _chatScroll.style.minHeight = 200;
            _chatScroll.style.backgroundColor = Panel;
            SetRadius(_chatScroll, RadiusCell);
            _chatScroll.style.paddingLeft = 6;
            _chatScroll.style.paddingRight = 6;
            _chatScroll.style.paddingTop = 4;
            _chatScroll.style.paddingBottom = 4;
            _chatTail = new TryllTailFollow(
                () => TryllTailFollow.DistanceFromBottom(_chatScroll),
                ScrollChatToEnd);
            _chatScroll.verticalScroller.valueChanged += _ => _chatTail.NotifyUserScroll();
            _chatScroll.contentContainer.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (_chatTail.State == TryllTailFollowController.FollowState.Following)
                    _chatTail.NotifyAppend();
            });
            return _chatScroll;
        }

        private VisualElement BuildInputRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 6;
            row.style.flexShrink = 0;

            _micButton = new Label("●") { pickingMode = PickingMode.Position };
            _micButton.style.width = 28;
            _micButton.style.height = 24;
            _micButton.style.fontSize = 14;
            _micButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            SetRadius(_micButton, RadiusCard);
            var micBorder = new Color(0f, 0f, 0f, 0.35f);
            _micButton.style.borderTopWidth = 1;
            _micButton.style.borderBottomWidth = 1;
            _micButton.style.borderLeftWidth = 1;
            _micButton.style.borderRightWidth = 1;
            _micButton.style.borderTopColor = micBorder;
            _micButton.style.borderBottomColor = micBorder;
            _micButton.style.borderLeftColor = micBorder;
            _micButton.style.borderRightColor = micBorder;
            _micButton.RegisterCallback<PointerDownEvent>(OnMicPointerDown);
            _micButton.RegisterCallback<PointerUpEvent>(OnMicPointerUp);
            row.Add(_micButton);

            _caretButton = new Button(ShowModeMenu) { text = "▾" };
            _caretButton.style.width = 16;
            _caretButton.style.height = 24;
            _caretButton.style.marginRight = 4;
            row.Add(_caretButton);

            _inputField = new TextField { name = TryllEditorElementNames.ChatInput };
            _inputField.style.flexGrow = 1;
            _inputField.style.height = 24;
            _inputField.RegisterValueChangedCallback(e => { if (!Recording) _input = e.newValue; });
            _inputField.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
            row.Add(_inputField);

            _sendButton = new Button(Send) { text = "Send", name = TryllEditorElementNames.ChatSend };
            _sendButton.style.width = 64;
            _sendButton.style.height = 24;
            _sendButton.style.marginLeft = 4;
            row.Add(_sendButton);

            _stopButton = new Button(CancelTurn) { text = "Stop" };
            _stopButton.style.width = 64;
            _stopButton.style.height = 24;
            _stopButton.style.marginLeft = 4;
            _stopButton.style.display = DisplayStyle.None; // shown only while a turn is in flight
            row.Add(_stopButton);

            return row;
        }

        // ── UI refresh ─────────────────────────────────────────────────────────

        private void RefreshUI()
        {
            if (_configBox == null) return;
            SyncConfigFromFields();

            _configBox.SetEnabled(IsStartable(_state));
            _voiceSourceField?.SetValueWithoutNotify(VoiceSourceLabel());
            bool showChatFields = _voiceSource == TryllChatVoiceSource.Chat;
            bool showComponent  = _voiceSource == TryllChatVoiceSource.Component;
            _voiceSection.style.display = VoiceEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            if (_voiceSection.style.display == DisplayStyle.Flex)
            {
                _voiceField.style.display     = showComponent ? DisplayStyle.Flex : DisplayStyle.None;
                _sttModelRow.style.display    = showChatFields ? DisplayStyle.Flex : DisplayStyle.None;
                _micDeviceField.style.display = showChatFields ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _startStopButton.text = IsStartable(_state) ? "Start" : "Stop";

            RefreshStatusChrome();
            RefreshInputRow();
        }

        private void RefreshInputRow()
        {
            bool turnBusy   = _activeTurn != null || _isTurnInFlight;
            bool playMode   = Application.isPlaying;
            bool voiceReady = TryllChatVoiceState.VoiceControlsReady(
                VoiceEnabled, _state == State.Ready, HasVoice, turnBusy, UsingComponent, playMode);
            bool isRec = TryllChatVoiceState.IsMicHighlighted(
                _voiceUi, UsingComponent && VoiceActive);

            _micButton.style.display   = VoiceEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            _caretButton.style.display = VoiceEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            _micButton.SetEnabled(voiceReady);
            _caretButton.SetEnabled(voiceReady);

            _micButton.style.backgroundColor = isRec ? MicActive : Panel2;
            _micButton.style.color = isRec ? Color.white : Txt;

            _micButton.tooltip = TryllChatVoiceState.MicTooltip(
                VoiceEnabled, UsingComponent, playMode, voiceReady, _voiceMode, _voiceUi.HandsFreeActive);

            bool inputDisabled = _state != State.Ready || Recording || turnBusy;
            _inputField.SetEnabled(!inputDisabled);

            // While a turn streams, the Send button is replaced by a Stop button that
            // cancels the turn (stop-and-leave-as-is).
            _sendButton.style.display = _isTurnInFlight ? DisplayStyle.None : DisplayStyle.Flex;
            _stopButton.style.display = _isTurnInFlight ? DisplayStyle.Flex : DisplayStyle.None;
            _sendButton.SetEnabled(_state == State.Ready && !Recording && !turnBusy);
            _stopButton.SetEnabled(_state == State.Ready);

            if (Recording)
                _inputField.SetValueWithoutNotify(
                    string.IsNullOrEmpty(_voiceUi.Partial) ? "Listening…" : _voiceUi.Partial);
            else
                _inputField.SetValueWithoutNotify(_input);
        }

        // ── Chat rendering ─────────────────────────────────────────────────────

        private void RebuildChat()
        {
            if (_chatScroll == null) return;

            _chatScroll.Clear();
            _streamingLabel = null;

            foreach (var turn in _turns)
            {
                if (turn == null) continue;
                if (!string.IsNullOrEmpty(turn.UserText))
                    _chatScroll.Add(MakeUserBubble(turn.UserText));

                bool isActive = !turn.IsComplete && turn == _activeTurn;
                if (turn.IsComplete || isActive || !string.IsNullOrEmpty(turn.AssistantText))
                {
                    bool trackLabel = isActive;
                    _chatScroll.Add(MakeAssistantBubble(turn, trackLabel, out var bodyLabel));
                    if (trackLabel) _streamingLabel = bodyLabel;
                }
            }

            _chatTail?.NotifyAppend();
        }

        private void AppendUserBubble(string text) =>
            _chatScroll.Add(MakeUserBubble(text));

        private VisualElement MakeUserBubble(string text)
        {
            var row = new VisualElement { name = TryllEditorElementNames.ChatUserBubble };
            row.style.flexDirection = FlexDirection.Column;
            row.style.alignItems = Align.FlexEnd;
            row.style.marginTop = 3;
            row.style.marginBottom = 3;

            var bubble = new VisualElement();
            bubble.style.maxWidth = Length.Percent(80);
            bubble.style.paddingLeft = 10;
            bubble.style.paddingRight = 10;
            bubble.style.paddingTop = 6;
            bubble.style.paddingBottom = 6;
            SetRadius(bubble, 10);
            bubble.style.backgroundColor = UserBub;

            var name = new Label("You");
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.fontSize = 10;
            name.style.opacity = 0.65f;
            name.style.marginBottom = 2;
            bubble.Add(name);

            var body = new Label(text);
            body.style.whiteSpace = WhiteSpace.Normal;
            bubble.Add(body);

            row.Add(bubble);
            return row;
        }

        private VisualElement MakeAssistantBubble(TryllChatTurnRecord turn, bool trackLabel, out Label bodyLabel)
        {
            bool statusRow = turn.IsComplete && TryllChatTurnLogic.UsesStatusRow(turn);
            bool selected  = turn.IsComplete && turn.TurnKey == _selectedTurnKey;
            string displayText = statusRow
                ? TryllChatTurnLogic.StatusRowLabel(turn.Display)
                : (turn.AssistantText ?? "");

            var row = new VisualElement { name = TryllEditorElementNames.ChatAssistantBubble };
            row.style.flexDirection = FlexDirection.Column;
            row.style.alignItems = Align.FlexStart;
            row.style.marginTop = 3;
            row.style.marginBottom = 3;

            var bubble = new VisualElement();
            bubble.style.maxWidth = Length.Percent(80);
            bubble.style.paddingLeft = statusRow ? 8 : 10;
            bubble.style.paddingRight = statusRow ? 8 : 10;
            bubble.style.paddingTop = statusRow ? 4 : 6;
            bubble.style.paddingBottom = statusRow ? 4 : 6;
            SetRadius(bubble, statusRow ? RadiusCell : 10);
            bubble.style.backgroundColor = statusRow
                ? Panel2
                : selected ? SelBub : AsstBub;

            ApplySelectionChrome(bubble, selected);

            if (!statusRow)
            {
                var name = new Label(AssistantName);
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.fontSize = 10;
                name.style.opacity = 0.65f;
                name.style.marginBottom = 2;
                bubble.Add(name);
            }

            bodyLabel = new Label(displayText);
            bodyLabel.style.whiteSpace = WhiteSpace.Normal;
            if (statusRow)
            {
                bodyLabel.style.fontSize = 11;
                bodyLabel.style.opacity = 0.75f;
                bodyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            }
            bubble.Add(bodyLabel);

            string terminalBadge = TryllChatTurnLogic.TerminalBadgeLabel(turn);
            if (!string.IsNullOrEmpty(terminalBadge))
            {
                var badge = new Label(terminalBadge);
                badge.style.fontSize = 9;
                badge.style.opacity = 0.7f;
                badge.style.marginTop = 3;
                badge.style.unityFontStyleAndWeight = FontStyle.Bold;
                bubble.Add(badge);
            }

            if (turn.IsComplete)
            {
                string key = turn.TurnKey;
                bubble.RegisterCallback<PointerDownEvent>(e =>
                {
                    SelectTurn(key);
                    e.StopPropagation();
                });
            }

            row.Add(bubble);
            return row;
        }

        private static void ApplySelectionChrome(VisualElement bubble, bool selected)
        {
            float w = selected ? 2f : 1f;
            Color c = selected ? Accent : Line;
            bubble.style.borderTopWidth = w;
            bubble.style.borderBottomWidth = w;
            bubble.style.borderLeftWidth = w;
            bubble.style.borderRightWidth = w;
            bubble.style.borderTopColor = c;
            bubble.style.borderBottomColor = c;
            bubble.style.borderLeftColor = c;
            bubble.style.borderRightColor = c;
        }

        private void SelectTurn(string turnKey)
        {
            _selectedTurnKey = turnKey;
            RebuildChat();
            RenderInspector();
        }

        private void ScrollChatToEnd()
        {
            if (_chatScroll == null || _chatTail == null) return;
            _chatTail.BeginProgrammaticScroll();
            _chatScroll.schedule.Execute(() =>
            {
                if (_chatScroll != null)
                    _chatScroll.scrollOffset = new Vector2(0, float.MaxValue);
                _chatTail?.EndProgrammaticScroll();
            }).ExecuteLater(1);
        }

        // Mirrors TryllModelTypeDrawer's tier labels for consistency across pickers.

        private void RefreshSttTierBadge(TryllModelManifest manifest)
        {
            if (_sttTierBadge == null) return;

            var tier = (manifest != null && !string.IsNullOrEmpty(_sttModelName))
                ? manifest.Get(_sttModelName)
                : TryllModelRegistration.None;

            Color color =
                tier == TryllModelRegistration.Production   ? ColOk   :
                tier == TryllModelRegistration.Experimental ? ColWarn :
                                                               Faint;
            _sttTierBadge.text = TierLabel(tier);
            _sttTierBadge.style.color = color;
            var fill = color;
            fill.a = 0.22f;
            _sttTierBadge.style.backgroundColor = fill;
        }

        private static string TierLabel(TryllModelRegistration tier)
        {
            switch (tier)
            {
                case TryllModelRegistration.Production:   return "Production";
                case TryllModelRegistration.Experimental: return "Experimental";
                default:                                  return "Unregistered";
            }
        }

        // ── Input events ───────────────────────────────────────────────────────

        private void OnInputKeyDown(KeyDownEvent e)
        {
            // TrickleDown: TextField's inner text element otherwise consumes Enter before
            // a bubble-phase handler sees it (Unreal's OnTextCommitted OnEnter path).
            if (Recording || _isTurnInFlight || _state != State.Ready) return;
            if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && !e.shiftKey)
            {
                Send();
                e.StopImmediatePropagation();
            }
        }

        private void OnRootKeyDown(KeyDownEvent e)
        {
            if (!CanPttKey()) return;
            if (e.keyCode == KeyCode.D && e.ctrlKey
                && !_voiceUi.PttHeld && !_voiceUi.AwaitingUtteranceFinal)
            {
                TryllChatVoiceState.OnPttPress(ref _voiceUi);
                BeginUtterance(autoFinishOnSilence: false);
                e.StopPropagation();
            }
        }

        private void OnRootKeyUp(KeyUpEvent e)
        {
            if (e.keyCode == KeyCode.D && _voiceUi.PttHeld)
            {
                _voiceUi.PttHeld = false;
                EndUtterance();
                e.StopPropagation();
            }
        }

        private bool CanPttKey()
            => CanDriveVoice() && _voiceMode == VoiceMode.PushToTalk;

        private bool CanDriveVoice()
            => TryllChatVoiceState.VoiceControlsReady(
                VoiceEnabled, _state == State.Ready, HasVoice,
                _activeTurn != null || _isTurnInFlight, UsingComponent, Application.isPlaying);

        private void OnMicPointerDown(PointerDownEvent e)
        {
            if (!CanDriveVoice()) return;
            var mode = _voiceMode;

            if (mode == VoiceMode.PushToTalk)
            {
                if (_voiceUi.PttHeld || _voiceUi.AwaitingUtteranceFinal) return;
                TryllChatVoiceState.OnPttPress(ref _voiceUi);
                _micButton.CapturePointer(e.pointerId);
                BeginUtterance(autoFinishOnSilence: false);
            }
            else
            {
                if (_voiceUi.HandsFreeActive) ExitHandsFree();
                else                          EnterHandsFree();
            }
        }

        private void OnMicPointerUp(PointerUpEvent e)
        {
            if (_voiceUi.PttHeld)
            {
                _voiceUi.PttHeld = false;
                if (_micButton.HasPointerCapture(e.pointerId))
                    _micButton.ReleasePointer(e.pointerId);
                EndUtterance();
            }
        }

        // ── Mode menu ──────────────────────────────────────────────────────────

        private void ShowModeMenu()
        {
            if (_activeTurn != null || _isTurnInFlight) return;
            var menu = new GenericMenu();
            var cur  = _voiceMode;
            menu.AddItem(new GUIContent("Push-to-talk"), cur == VoiceMode.PushToTalk, () => SetMode(VoiceMode.PushToTalk));
            menu.AddItem(new GUIContent("Hands-free"),   cur == VoiceMode.HandsFree,   () => SetMode(VoiceMode.HandsFree));
            menu.ShowAsContext();
        }

        private void SetMode(VoiceMode mode)
        {
            if (_activeTurn != null || _isTurnInFlight) return;
            if (_voiceUi.HandsFreeActive && mode != VoiceMode.HandsFree) ExitHandsFree();
            if (_voiceUi.PttHeld && mode != VoiceMode.PushToTalk)
            {
                _voiceUi.PttHeld = false;
                EndUtterance();
            }
            _voiceMode = mode;
            SaveChatConfig();
            RefreshInputRow();
        }

        // ── Chat config (EditorPrefs) ──────────────────────────────────────────

        private void LoadChatConfig()
        {
            if (!TryllChatConfig.Exists())
            {
                // Discard any leftover EditorWindow-serialized Whisper default.
                _voiceSource  = TryllChatVoiceSource.Off;
                _sttModelName = "";
                _voiceMode    = VoiceMode.PushToTalk;
                return;
            }

            var cfg = TryllChatConfig.Load();
            var hadSource = cfg.HasVoiceSource;
            _voiceSource     = hadSource
                ? cfg.VoiceSource
                : TryllChatVoiceState.FromLegacy(
                    cfg.EnableVoice, _voice != null || !string.IsNullOrEmpty(_voiceGlobalId));
            _sttModelName    = cfg.SttModelName ?? "";
            _voiceMode       = cfg.VoiceMode;
            _inspectorWidth  = cfg.InspectorWidth;
            _inspectorVisible = cfg.InspectorVisible;
            _micDeviceName   = cfg.MicDeviceName ?? "";
            if (_voiceSource == TryllChatVoiceSource.Component && _voice == null)
                AssignAgentVoice();
            if (!hadSource)
                SaveChatConfig();
        }

        private void SaveChatConfig()
        {
            new TryllChatConfig
            {
                EnableVoice      = VoiceEnabled,
                VoiceSource      = _voiceSource,
                HasVoiceSource   = true,
                SttModelName     = _sttModelName ?? "",
                VoiceMode        = _voiceMode,
                InspectorWidth   = _inspectorWidth,
                InspectorVisible = _inspectorVisible,
                MicDeviceName    = _micDeviceName ?? "",
            }.Save();
        }

        private static List<string> RegisteredSttNames()
        {
            var manifest = TryllModelManifest.FindProjectManifest();
            return manifest != null ? manifest.ModelsOfType("stt") : new List<string>();
        }

        private List<string> CollectSttDropdownNames()
        {
            var names = RegisteredSttNames();
            if (!string.IsNullOrEmpty(_sttModelName) && !names.Contains(_sttModelName))
                names.Add(_sttModelName);
            return names;
        }

        private bool CanEnableVoice()
            => RegisteredSttNames().Count > 0 || !string.IsNullOrEmpty(_sttModelName);

        private static bool TryParseVoiceSource(string label, out TryllChatVoiceSource source)
        {
            switch (label)
            {
                case "Chat": source = TryllChatVoiceSource.Chat; return true;
                case "Actor component": source = TryllChatVoiceSource.Component; return true;
                case "Off": source = TryllChatVoiceSource.Off; return true;
                default: source = TryllChatVoiceSource.Off; return false;
            }
        }

        private static string VoiceSourceLabel(TryllChatVoiceSource source) => source switch
        {
            TryllChatVoiceSource.Chat => "Chat",
            TryllChatVoiceSource.Component => "Actor component",
            _ => "Off",
        };

        private string VoiceSourceLabel() => VoiceSourceLabel(_voiceSource);

        private void SetVoiceSource(TryllChatVoiceSource source)
        {
            if (source == TryllChatVoiceSource.Chat)
            {
                var registered = RegisteredSttNames();
                if (registered.Count == 0 && string.IsNullOrEmpty(_sttModelName))
                    return;
                if (string.IsNullOrEmpty(_sttModelName) || !registered.Contains(_sttModelName))
                {
                    if (registered.Count > 0)
                        _sttModelName = registered[0];
                }
                SyncSttDropdown();
            }

            _voiceSource = source;
            if (source == TryllChatVoiceSource.Component && _voice == null)
                AssignAgentVoice();
            SaveChatConfig();
            RefreshUI();
        }

        private void AssignAgentVoice()
        {
            var sibling = _agent != null ? _agent.GetComponent<TryllVoiceInputComponent>() : null;
            _voiceAutoPicked = sibling != null;
            BindSceneRef(ref _voice, ref _voiceGlobalId, sibling);
            if (_voiceField != null && _voiceField.value != sibling)
                _voiceField.SetValueWithoutNotify(sibling);
        }

        private void SyncSttDropdown()
        {
            if (_sttModelField == null) return;
            var names = CollectSttDropdownNames();
            if (names.Count == 0) names.Add("");
            _sttModelField.choices = names;
            if (!string.IsNullOrEmpty(_sttModelName) && names.Contains(_sttModelName))
                _sttModelField.SetValueWithoutNotify(_sttModelName);
            RefreshSttTierBadge(TryllModelManifest.FindProjectManifest());
        }

        // ── Actions ────────────────────────────────────────────────────────────

        private void AutofindSettings() => _settings = TryllEditorSettingsLookup.Find();

        internal TryllEditorSession DebugSession => _session;
        internal State DebugState => _state;
        internal bool DebugTurnInFlight => _isTurnInFlight || _activeTurn != null;
        internal bool DebugStartEnabled => _startStopButton != null && IsStartable(_state);

        /// <summary>
        /// Assign settings and agent as the ObjectFields would, with voice off so a
        /// developer's saved Chat voice config cannot route the test through STT.
        /// </summary>
        internal void DebugBindForTests(TryllRuntimeSettings settings, TryllAgentComponent agent)
        {
            _settings = settings;
            _agent = agent;
            _voice = null;
            _voiceSource = TryllChatVoiceSource.Off;
            _sttModelName = "";
            PushSceneRefsToFields();
            RefreshUI();
        }

        /// <summary>Press Start/Stop as a click would.</summary>
        internal void DebugStartStopForTests() => OnStartStopClicked();

        /// <summary>Type <paramref name="text"/> and press Send.</summary>
        internal void DebugSendForTests(string text)
        {
            _input = text;
            Send();
        }

        internal TryllEditorWindowSnapshot DebugSnapshot()
        {
            var rows = new List<string>();
            foreach (var turn in _turns)
                if (turn != null) rows.Add(turn.AssistantText ?? "");
            return new TryllEditorWindowSnapshot(
                _session?.OwnerPhase ?? TryllOwnerPhase.Idle,
                _state.ToString(),
                _statusBar?.DebugCaption,
                _statusBar?.DebugActionText,
                _banner?.DebugText,
                rows,
                _session?.HoldsLease ?? false,
                _turns.Count);
        }

        /// <summary>Idle and Error are both recoverable — label "Start" and allow a retry.</summary>
        internal static bool IsStartable(State state) => state == State.Idle || state == State.Error;

        private void OnStartStopClicked()
        {
            if (IsStartable(_state))
            {
                if (_state == State.Error) Stop();
                Start();
            }
            else
            {
                Stop();
            }
        }

        private void Start()
        {
            RestoreSceneRefs();
            SyncConfigFromFields();
            RememberLiveSceneRefs();
            PushSceneRefsToFields();
            if (_settings == null) { RejectStart("Assign a TryllRuntimeSettings asset"); return; }
            if (_agent    == null) { RejectStart(MissingAgentMessage("starting")); return; }
            if (_voiceSource == TryllChatVoiceSource.Component && ResolvedVoice == null)
            {
                string name = _agent.gameObject.name;
                RejectStart(string.Format(TryllChatVoiceState.MissingComponent, name));
                return;
            }
            if (!_agent.StageAgentCreationWithTransientDiagnostics())
            {
                Fail("Agent already exists or is being created outside Tryll Chat.");
                return;
            }
            _ownsAgentLifecycle = true;

            _turns.Clear();
            _turnSequence = 0;
            _activeTurn = null;
            _selectedTurnKey = null;
            TryllChatVoiceState.OnStop(ref _voiceUi);
            RebuildChat();
            RenderInspector();

            _session = new TryllEditorSession(_settings);
            _session.StateChanged += OnSessionStateChanged;
            _session.SessionReady += OnSessionReady;
            _session.Start();
            if (_session.Client != null) HookEvents(_session.Client);
        }

        private void Stop()
        {
            ExitHandsFree();
            StopCapture();
            DisposeOwnedVoice();
            UnhookVoiceEvents();

            if (_ownsAgentLifecycle) _agent?.DestroyAgent();
            _ownsAgentLifecycle = false;
            if (_session?.Client != null) UnhookEvents(_session.Client);
            _session?.Stop();
            _session = null;

            _activeTurn = null;
            _isTurnInFlight = false;
            TryllChatVoiceState.OnStop(ref _voiceUi);
            _selectedTurnKey = TryllChatTurnLogic.SanitizeSelection(_selectedTurnKey, _turns);
            RenderInspector();

            SetState(State.Idle, TryllEditorUiContract.StatusIdle);
        }

        private bool BeginUserTurn(string userText, bool fromVoice)
        {
            if (!TryllChatTurnLogic.CanBeginTurn(_activeTurn, _isTurnInFlight))
                return false;

            _turnSequence++;
            _activeTurn = TryllChatTurnLogic.BeginTurn(_turnSequence, userText, fromVoice);
            _turns.Add(_activeTurn);
            AppendUserBubble(userText);
            _chatTail?.ResetFollowing();
            _streamingLabel = null;
            _isTurnInFlight = true;
            RefreshInputRow();
            return true;
        }

        private void OnSessionStateChanged(TryllEditorSession.State s, string status)
        {
            // CreatingAgent/CreatingVoiceInput have no session-level equivalent — they
            // are set explicitly around _agent.CreateAgent()/voice setup below, not
            // driven by the session. Error maps through Fail() for the same log-and-set
            // treatment every other failure path here uses.
            switch (s)
            {
                case TryllEditorSession.State.Idle:           SetState(State.Idle, status); break;
                case TryllEditorSession.State.LaunchingServer:
                case TryllEditorSession.State.Connecting:      SetState(State.Connecting, status); break;
                case TryllEditorSession.State.Configuring:     SetState(State.Configuring, status); break;
                case TryllEditorSession.State.CheckingModels:  SetState(State.CheckingModels, status); break;
                case TryllEditorSession.State.Ready:           break; // handled by OnSessionReady
                case TryllEditorSession.State.Error:
                    // Release the failed helper before surfacing Error so a retry cannot
                    // stack another AcquireConnection on a still-held client.
                    var failed = _session;
                    _session = null;
                    if (failed?.Client != null) UnhookEvents(failed.Client);
                    failed?.Stop();
                    if (_ownsAgentLifecycle) _agent?.DestroyAgent();
                    _ownsAgentLifecycle = false;
                    Fail(status);
                    break;
            }
        }

        private void OnSessionReady()
        {
            if (_agent.HasAgent) return;
            SetState(State.CreatingAgent, TryllEditorUiContract.StatusCreatingAgent);
            if (!_agent.CreateAgentWithTransientDiagnostics())
                Fail("Agent creation is already in flight without Chat diagnostics.");
        }

        private void Send()
        {
            var text = _input?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (_agent == null || !_agent.HasAgent) return;
            if (!BeginUserTurn(text, fromVoice: false)) return;

            _input = "";
            _inputField.SetValueWithoutNotify("");
            _agent.SendMessage(text);
        }

        private void CancelTurn()
        {
            if (_agent == null || !_agent.HasAgent) return;
            _agent.Cancel(TryllCancelMode.StopAndKeep);
            // The turn ends via OnTurnComplete (status=Cancelled), which clears the flag.
        }

        // ── Event wiring ───────────────────────────────────────────────────────

        private void HookEvents(TryllClient client)
        {
            if (_hooked) return;
            client.Error += OnClientError;

            _agent.OnAgentCreated.AddListener(OnAgentCreated);
            _agent.OnAnswerText  .AddListener(OnAnswerText);
            _agent.OnTurnComplete.AddListener(OnTurnComplete);
            _agent.OnError       .AddListener(OnAgentError);
            _hooked = true;
        }

        private void UnhookEvents(TryllClient client)
        {
            if (!_hooked) return;
            client.Error -= OnClientError;

            if (_agent != null)
            {
                _agent.OnAgentCreated.RemoveListener(OnAgentCreated);
                _agent.OnAnswerText  .RemoveListener(OnAnswerText);
                _agent.OnTurnComplete.RemoveListener(OnTurnComplete);
                _agent.OnError       .RemoveListener(OnAgentError);
            }
            _hooked = false;
        }

        private async void OnAgentCreated()
        {
            // Agent Log (and other observers) read DisplayName via SnapshotRegisteredAgents.
            if (_agent?.Agent != null)
                _agent.Agent.DisplayName = AssistantName;

            if (_voiceSource == TryllChatVoiceSource.Off
                || (_voiceSource == TryllChatVoiceSource.Chat && string.IsNullOrEmpty(_sttModelName)))
            {
                SetState(State.Ready, TryllEditorUiContract.StatusReady);
                return;
            }

            SetState(State.CreatingVoiceInput, TryllEditorUiContract.StatusCreatingVoiceInput);

            if (_voiceSource == TryllChatVoiceSource.Component)
            {
                _boundVoice = ResolvedVoice;
                if (_boundVoice == null)
                {
                    Fail(string.Format(TryllChatVoiceState.MissingComponent,
                        _agent != null ? _agent.gameObject.name : "the agent"));
                    return;
                }
                _boundVoice.OnTranscriptUpdate.AddListener(OnTranscriptUpdate);
                _boundVoice.OnError           .AddListener(OnVoiceError);
                _boundVoice.OnVoiceInputCreated.AddListener(RefreshInputRow);
                _voiceHooked = true;
                // Coroutine mic capture only ticks in Play Mode. Skip the
                // CreateVoiceInput load in Edit Mode so text chat stays usable.
                if (Application.isPlaying)
                    _boundVoice.CreateVoiceInput();
                SetState(State.Ready, TryllEditorUiContract.StatusReady);
            }
            else
            {
                _nativeRate = ResolveNativeRate();
                var cfg = new VoiceInputConfig
                {
                    ModelName   = _sttModelName?.Trim() ?? "",
                    InputFormat = new AudioFormat
                    {
                        SampleRate    = (uint)_nativeRate,
                        Channels      = 1,
                        BitsPerSample = 16,
                    },
                    VadThreshold    = 0.5f,
                    VadMinSilenceMs = 1000,
                    VadSpeechPadMs  = 250,
                };
                var (voice, err) = await TryllClient.Instance.CreateVoiceInputAsync(cfg);
                if (!err.IsOk) { Fail($"CreateVoiceInput failed: {err.Message}"); return; }
                _ownedVoice = voice;
                _ownedVoice.OnTranscriptUpdate = OnTranscriptUpdate;
                _ownedVoice.OnError            = OnVoiceError;
                _voiceHooked = true;
                SetState(State.Ready, TryllEditorUiContract.StatusReady);
            }
        }

        private void UnhookVoiceEvents()
        {
            if (!_voiceHooked) return;
            if (_boundVoice != null)
            {
                _boundVoice.OnTranscriptUpdate.RemoveListener(OnTranscriptUpdate);
                _boundVoice.OnError           .RemoveListener(OnVoiceError);
                _boundVoice.OnVoiceInputCreated.RemoveListener(RefreshInputRow);
            }
            _boundVoice = null;
            if (_ownedVoice != null)
            {
                _ownedVoice.OnTranscriptUpdate = null;
                _ownedVoice.OnError            = null;
            }
            _voiceHooked = false;
        }

        private void DisposeOwnedVoice()
        {
            _ownedVoice?.Dispose();
            _ownedVoice = null;
        }

        private void OnAnswerText(string nodeName, string text, bool isDelta, bool isFinal)
        {
            if (_activeTurn == null) return;

            TryllChatTurnLogic.AppendAssistantDelta(_activeTurn, text, isDelta);

            if (_streamingLabel == null)
            {
                _chatScroll.Add(MakeAssistantBubble(_activeTurn, trackLabel: true, out _streamingLabel));
            }
            else
            {
                _streamingLabel.text = _activeTurn.AssistantText;
            }
            _chatTail?.NotifyAppend();
        }

        private void OnTurnComplete(TryllTurnStatus status, string debugInfo, int tokens)
        {
            string errorText = null;
            if (status == TryllTurnStatus.Error
                && !string.IsNullOrEmpty(debugInfo)
                && TryllJson.TryParse(debugInfo, out var parsed, out _))
                errorText = TryllChatTurnLogic.ExtractErrorText(parsed);
            if (status == TryllTurnStatus.Error && string.IsNullOrEmpty(errorText))
                errorText = "Turn failed";

            SettleActiveTurn(status, debugInfo, tokens, errorText);

            if (_voiceUi.HandsFreeActive && _state == State.Ready)
                BeginUtterance(autoFinishOnSilence: true);
        }

        private bool SettleActiveTurn(
            TryllTurnStatus status,
            string debugInfo,
            int tokens,
            string errorText)
        {
            _isTurnInFlight = false;
            if (_activeTurn == null)
            {
                RefreshInputRow();
                return false;
            }

            var completed = _activeTurn;
            TryllChatTurnLogic.FinalizeTurn(
                completed, status, debugInfo, tokens, errorText);
            _selectedTurnKey = TryllChatTurnLogic.OnTurnCompleted(
                _selectedTurnKey, _turns, completed);
            _activeTurn = null;
            _streamingLabel = null;
            RebuildChat();
            RenderInspector();
            RefreshInputRow();
            return true;
        }

        private void OnAgentError(TryllError error)
        {
            // A busy rejection is re-queued on the agent component and flushed when
            // the busy window closes. Keep this bubble; the later turn delivers the reply.
            if (error.Code == (int)TryllErrorCode.AgentBusy
                && _agent != null
                && _agent.HasPendingMessage
                && _activeTurn != null
                && _agent.PendingMessage == _activeTurn.UserText)
            {
                return;
            }

            string message = string.IsNullOrEmpty(error.Message) ? "Agent error" : error.Message;
            SettleActiveTurn(TryllTurnStatus.Error, "", 0, message);
            Fail($"Agent error: {message}");
        }
        private void OnClientError(TryllError error) => Fail($"Client error: {error.Message}");
        private void OnVoiceError(TryllError error)
        {
            TryllChatVoiceState.OnVoiceError(ref _voiceUi);
            Debug.LogWarning($"[Tryll] Voice error: {error.Message}");
            RefreshInputRow();
        }

        // ── Voice operations ───────────────────────────────────────────────────

        private void BeginUtterance(bool autoFinishOnSilence)
        {
            if (!HasVoice) return;
            if (_activeTurn != null || _isTurnInFlight) return;
            if (UsingComponent && !Application.isPlaying) return;
            if (!TryllChatVoiceState.CanBeginUtterance(_voiceUi, VoiceActive)) return;

            if (UsingComponent)
            {
                ActiveVoice.autoFinishOnSilence = autoFinishOnSilence;
                ActiveVoice.maxUtteranceMs      = 60000;
                TryllChatVoiceState.OnComponentBegin(ref _voiceUi);
                ActiveVoice.BeginUtterance(_agent);
            }
            else
            {
                var opts = new UtteranceOptions
                {
                    AutoFinishOnSilence = autoFinishOnSilence,
                    MaxUtteranceMs      = 60000,
                };
                // Reserve the input lane before the async begin callback so a typed send
                // cannot slip between BeginUtterance and microphone capture startup.
                TryllChatVoiceState.OnInlineBeginQueued(ref _voiceUi);
                _ownedVoice.BeginUtterance(opts, err =>
                {
                    TryllChatVoiceState.OnInlineBeginAcked(ref _voiceUi);
                    if (!err.IsOk)
                    {
                        TryllChatVoiceState.OnBeginFailed(ref _voiceUi);
                        Debug.LogWarning($"[Tryll] BeginUtterance: {err.Message}");
                        RefreshInputRow();
                        return;
                    }
                    StartCapture();
                    RefreshInputRow();
                });
            }
            RefreshInputRow();
        }

        private void EndUtterance()
        {
            if (!HasVoice)
            {
                TryllChatVoiceState.OnEndFailed(ref _voiceUi);
                RefreshInputRow();
                return;
            }
            if (!UsingComponent) StopCapture();
            if (VoiceActive)
            {
                TryllChatVoiceState.OnEndCommitted(ref _voiceUi);
                if (UsingComponent)
                {
                    ActiveVoice.EndUtterance();
                    // End errors arrive via OnVoiceError (clears awaiting / PTT).
                }
                else
                {
                    _ownedVoice.EndUtterance(err =>
                    {
                        if (!err.IsOk)
                        {
                            TryllChatVoiceState.OnEndFailed(ref _voiceUi);
                            Debug.LogWarning($"[Tryll] EndUtterance: {err.Message}");
                            RefreshInputRow();
                        }
                    });
                }
            }
            else
            {
                TryllChatVoiceState.OnEndFailed(ref _voiceUi);
            }
            RefreshInputRow();
        }

        private void CancelUtterance()
        {
            if (!UsingComponent) StopCapture();
            TryllChatVoiceState.OnCancel(ref _voiceUi);
            if (HasVoice && VoiceActive)
            {
                if (UsingComponent) ActiveVoice.CancelUtterance();
                else                _ownedVoice.CancelUtterance();
            }
            RefreshInputRow();
        }

        private void EnterHandsFree()
        {
            if (_activeTurn != null || _isTurnInFlight) return;
            if (UsingComponent && !Application.isPlaying) return;
            _voiceUi.HandsFreeActive = true;
            BeginUtterance(autoFinishOnSilence: true);
        }

        private void ExitHandsFree()
        {
            if (!_voiceUi.HandsFreeActive) return;
            _voiceUi.HandsFreeActive = false;
            CancelUtterance();
        }

        // ── Microphone capture (inline path only) ──────────────────────────────

        private int ResolveNativeRate()
        {
            string device = string.IsNullOrEmpty(_micDeviceName) ? null : _micDeviceName;
            Microphone.GetDeviceCaps(device, out int minFreq, out int maxFreq);
            if (maxFreq == 0) return 48000;
            return Mathf.Clamp(48000, minFreq <= 0 ? 8000 : minFreq, maxFreq);
        }

        private void StartCapture()
        {
            if (_micClip != null) return;
            string device = string.IsNullOrEmpty(_micDeviceName) ? null : _micDeviceName;
            _micClip = Microphone.Start(device, true, 2, _nativeRate);
            if (_micClip == null)
            {
                Debug.LogWarning("[Tryll Voice] Microphone.Start failed — capture not started");
                return;
            }
            _readPos        = Mathf.Max(0, Microphone.GetPosition(device));
            _lastMicPos     = -1;
            _lastMicAdvance = EditorApplication.timeSinceStartup;
            _voiceUi.InlineCapturing = true;
        }

        private void StopCapture()
        {
            _voiceUi.InlineCapturing = false;
            if (_micClip != null)
            {
                string device = string.IsNullOrEmpty(_micDeviceName) ? null : _micDeviceName;
                Microphone.End(device);
                _micClip = null;
            }
            _readPos = 0;
        }

        private void PumpMic()
        {
            string device = string.IsNullOrEmpty(_micDeviceName) ? null : _micDeviceName;
            double now = EditorApplication.timeSinceStartup;

            bool stalled = _micClip == null || !Microphone.IsRecording(device);
            if (!stalled)
            {
                int p = Microphone.GetPosition(device);
                if (p != _lastMicPos) { _lastMicPos = p; _lastMicAdvance = now; }
                else if (now - _lastMicAdvance > 2.0) stalled = true;
            }
            if (stalled)
            {
                Debug.LogWarning("[Tryll Voice] Mic capture stalled — restarting");
                if (_micClip != null) Microphone.End(device);
                _micClip        = Microphone.Start(device, true, 2, _nativeRate);
                _readPos        = 0;
                _lastMicPos     = -1;
                _lastMicAdvance = now;
                return;
            }

            int pos = Microphone.GetPosition(device);
            if (pos < 0) return;

            int clipSamples = _micClip.samples;
            int avail = pos - _readPos;
            if (avail < 0) avail += clipSamples;
            if (avail <= 0) return;
            if (avail > clipSamples) avail = clipSamples;

            var floats = new float[avail];
            _micClip.GetData(floats, _readPos);
            _readPos = (_readPos + avail) % clipSamples;

            var pcm = new short[avail];
            for (int i = 0; i < avail; i++)
            {
                float s = Mathf.Clamp(floats[i], -1f, 1f);
                pcm[i] = (short)Mathf.RoundToInt(s * 32767f);
            }
            _ownedVoice?.SendAudioBuffer(pcm);
        }

        // ── Transcript handler ─────────────────────────────────────────────────

        private void OnTranscriptUpdate(TranscriptUpdate update)
        {
            switch (update.Kind)
            {
            case TranscriptUpdateKind.SpeechStart:
                _voiceUi.Partial = "";
                break;

            case TranscriptUpdateKind.Partial:
            case TranscriptUpdateKind.SegmentFinal:
                _voiceUi.Partial = update.Text ?? "";
                break;

            case TranscriptUpdateKind.UtteranceFinal:
                TryllChatVoiceState.OnUtteranceFinal(ref _voiceUi);
                if (!UsingComponent && !_voiceUi.HandsFreeActive) StopCapture();
                if (!string.IsNullOrEmpty(update.Text))
                {
                    if (!BeginUserTurn(update.Text, fromVoice: true))
                        Debug.LogWarning("[Tryll] Ignored overlapping final voice transcript.");
                    else if (!UsingComponent)
                        _agent?.SendMessage(update.Text);
                }
                else if (_voiceUi.HandsFreeActive && _state == State.Ready)
                {
                    BeginUtterance(autoFinishOnSilence: true);
                }
                break;
            }
            RefreshInputRow();
        }

        // ── State helpers ──────────────────────────────────────────────────────

        private void SetState(State s, string status)
        {
            _state = s;
            _status = status;
            RefreshUI();
        }

        // ObjectField and the serialized backing ref can desync after a domain reload:
        // the UI still shows the assigned component while `_agent` is Unity-null (or the
        // reverse). Read the field the user sees, and restore the field from backing
        // when the field is empty. Does not rebind from GlobalObjectId — that happens
        // in RestoreSceneRefs so a mid-session field clear is not immediately overwritten.
        private void SyncConfigFromFields()
        {
            ReconcileObjectField(ref _settings, _settingsField);
            ReconcileObjectField(ref _agent, _agentField);
            ReconcileObjectField(ref _voice, _voiceField);
            if (_agent == null) ClearIfMissing(_agentField);
            if (_voice == null) ClearIfMissing(_voiceField);
            RememberLiveSceneRefs();
        }

        private void RestoreSceneRefs()
        {
            RestoreSceneRef(ref _agent, ref _agentGlobalId);
            RestoreSceneRef(ref _voice, ref _voiceGlobalId);
        }

        private void RememberLiveSceneRefs()
        {
            if (_agent != null)
                _agentGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(_agent).ToString();
            if (_voice != null)
                _voiceGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(_voice).ToString();
        }

        private static void RestoreSceneRef<T>(ref T backing, ref string globalId)
            where T : Object
        {
            if (backing != null)
            {
                globalId = GlobalObjectId.GetGlobalObjectIdSlow(backing).ToString();
                return;
            }

            backing = null; // collapse Unity fake-null
            var restored = ResolveGlobalId<T>(globalId);
            if (restored == null) return;
            backing = restored;
            globalId = GlobalObjectId.GetGlobalObjectIdSlow(restored).ToString();
        }

        private void PushSceneRefsToFields()
        {
            if (_settingsField != null && _settingsField.value != _settings)
                _settingsField.SetValueWithoutNotify(_settings);
            if (_agentField != null && _agentField.value != _agent)
                _agentField.SetValueWithoutNotify(_agent);
            if (_voiceField != null && _voiceField.value != _voice)
                _voiceField.SetValueWithoutNotify(_voice);
        }

        internal static void BindSceneRef<T>(ref T backing, ref string globalId, T value)
            where T : Object
        {
            if (value != null)
            {
                backing = value;
                globalId = GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
                return;
            }

            backing = null;
            globalId = null;
        }

        private static T ResolveGlobalId<T>(string idString)
            where T : Object
        {
            if (string.IsNullOrEmpty(idString)) return null;
            if (!GlobalObjectId.TryParse(idString, out var id)) return null;
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as T;
        }

        private static void ReconcileObjectField<T>(ref T backing, ObjectField field)
            where T : Object
        {
            if (field == null) return;
            var fromField = field.value as T;
            if (fromField != null)
                backing = fromField;
            else if (backing != null)
                field.SetValueWithoutNotify(backing);
            else
                backing = null;
        }

        private static void ClearIfMissing(ObjectField field)
        {
            if (field == null) return;
            var value = field.value;
            if (!ReferenceEquals(value, null) && value == null)
                field.SetValueWithoutNotify(null);
        }

        private string MissingAgentMessage(string action)
        {
            return !string.IsNullOrEmpty(_agentGlobalId)
                ? "Agent Component reference was lost after domain reload. "
                  + $"Re-assign it from the scene, then {action}."
                : $"Assign an Agent Component before {action}.";
        }

        private static void Warn(string msg)
        {
            Debug.LogWarning($"[Tryll] TryllChatWindow: {msg}");
        }

        // Missing config is a recoverable Idle precondition — do not enter Error,
        // which disables the ObjectFields and leaves Start looking like Stop.
        private void RejectStart(string msg)
        {
            Warn(msg);
            SetState(State.Idle, msg);
        }

        private void RefreshStatusChrome()
        {
            if (_statusBar == null) return;

            TryllPanelOperation local = _state switch
            {
                State.CreatingAgent => TryllPanelOperation.CreatingAgent,
                State.CreatingVoiceInput => TryllPanelOperation.CreatingVoiceInput,
                _ => TryllPanelOperation.None,
            };

            TryllStatusPresentation pres;
            if (_state == State.CheckingModels)
            {
                pres = new TryllStatusPresentation(
                    TryllStatusSeverity.Progress, "Checking required models…");
            }
            else
            {
                TryllOwnerPhase owner = _session?.OwnerPhase ?? TryllOwnerPhase.Idle;
                string detail = _session?.OwnerDetail;
                if (_state == State.Error)
                {
                    owner = TryllOwnerPhase.Error;
                    detail = _status;
                }
                pres = TryllStatusPresentation.Compose(
                    owner, detail, local, TryllEditorPanel.Chat);
            }

            _statusBar.Bind(pres, pres.Action == TryllStatusActionIntent.Retry
                ? OnStartStopClicked
                : null);

            if (_banner == null) return;
            if (_state == State.Error && !string.IsNullOrEmpty(_status))
                _banner.Bind(_status, TryllStatusSeverity.Error);
            else if (_voiceSource == TryllChatVoiceSource.Component && UsingComponent && !Application.isPlaying)
                _banner.Bind(TryllChatVoiceState.ComponentNeedsPlayMode, TryllStatusSeverity.Warning);
            else
                _banner.Dismiss();
        }

        private void Fail(string msg)
        {
            Warn(msg);
            SetState(State.Error, msg);
        }
    }
}
