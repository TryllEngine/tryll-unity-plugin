// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="TryllAgentComponent"/>: declaration-driven
    /// Material-Instance style Variable Overrides ([checkbox] [name] [value]), plus
    /// Play Mode live application onto <see cref="TryllAgent.Variables"/> with
    /// deferred flush (and an explicit Flush Now button).
    /// </summary>
    [CustomEditor(typeof(TryllAgentComponent))]
    [CanEditMultipleObjects]
    internal sealed class TryllAgentComponentEditor : UnityEditor.Editor
    {
        private const float ToggleWidth = 18f;
        private const float Spacing = 2f;

        private SerializedProperty _workflowAsset;
        private SerializedProperty _variableOverrides;
        private SerializedProperty _enableDiagnostics;
        private SerializedProperty _maintainDialogueHistory;
        private SerializedProperty _kvCacheInitialization;
        private SerializedProperty _workload;
        private SerializedProperty _autoCreateOnConnect;
        private SerializedProperty _speaker;
        private SerializedProperty _voice;
        private SerializedProperty _overrideVoiceDelivery;
        private SerializedProperty _voiceDelivery;
        private SerializedProperty _onAnswerText;
        private SerializedProperty _onTurnComplete;
        private SerializedProperty _onMessageQueued;
        private SerializedProperty _onMessageDequeued;
        private SerializedProperty _onError;
        private SerializedProperty _onAgentCreated;
        private SerializedProperty _onAgentDestroyed;
        private SerializedProperty _onParamChanged;
        private SerializedProperty _onResumed;
        private SerializedProperty _onKvCachePrefilled;
        private SerializedProperty _onKvCacheEvicted;
        private SerializedProperty _onKvCacheStatus;
        private SerializedProperty _onInteractionsAppended;
        private SerializedProperty _onInteractionsRemoved;
        private SerializedProperty _onDialogExported;
        private SerializedProperty _onDialogImported;
        private SerializedProperty _onDialogSaved;
        private SerializedProperty _onDialogLoaded;

        /// <summary>
        /// Every serialized property of <see cref="TryllAgentComponent"/> this
        /// inspector accounts for — drawn unconditionally, custom-rendered
        /// (<c>VariableOverrides</c>), or deliberately not shown.
        /// <para>
        /// A <c>[CustomEditor]</c> replaces the default reflected inspector wholesale,
        /// so a serialized field with no matching draw call is invisible with no
        /// compile error, no runtime error, and no visual cue. Seven fields had
        /// accumulated that way before this list existed. Coverage is therefore kept
        /// as data, with two consumers: <c>DrawUnaccountedProperties</c> renders
        /// anything missing so it is never unreachable, and
        /// <c>TryllAgentComponentInspectorCoverageTests</c> fails CI so it cannot
        /// recur. Adding a serialized field means adding it here too — that is the
        /// intended friction.
        /// </para>
        /// </summary>
        internal static readonly string[] AccountedProperties =
        {
            "WorkflowAsset",
            "VariableOverrides",
            "EnableDiagnostics",
            "MaintainDialogueHistory",
            "KvCacheInitialization",
            "Workload",
            "AutoCreateOnConnect",
            "speaker",
            "voice",
            "overrideVoiceDelivery",
            "voiceDelivery",
            "OnAnswerText",
            "OnTurnComplete",
            "OnMessageQueued",
            "OnMessageDequeued",
            "OnError",
            "OnAgentCreated",
            "OnAgentDestroyed",
            "OnParamChanged",
            "OnResumed",
            "OnKvCachePrefilled",
            "OnKvCacheEvicted",
            "OnKvCacheStatus",
            "OnInteractionsAppended",
            "OnInteractionsRemoved",
            "OnDialogExported",
            "OnDialogImported",
            "OnDialogSaved",
            "OnDialogLoaded",
        };

        private bool _overridesFoldout = true;
        private bool _staleFoldout;
        private string _liveVarsStatus;
        private MessageType _liveVarsStatusType = MessageType.None;
        private string _voiceDeliveryStatus;
        private MessageType _voiceDeliveryStatusType = MessageType.None;

        private const string MissingWorkflowAssetNotice =
            "Assign a Workflow Asset. The component cannot create an agent without one. " +
            "Create ▸ Tryll ▸ Workflow Asset, then assign it above.";

        private static readonly GUIContent OverridesFoldoutLabel = new GUIContent(
            "Variable Overrides",
            "Check a variable to override its initial value for this agent instance. " +
            "While an agent is live (Play Mode or Tryll Chat), edits update the local mirror and " +
            "flush before the next Send/Resume/ChangeParams (or use Flush Now).");

        private void OnEnable()
        {
            _workflowAsset            = serializedObject.FindProperty("WorkflowAsset");
            _variableOverrides        = serializedObject.FindProperty("VariableOverrides");
            _enableDiagnostics        = serializedObject.FindProperty("EnableDiagnostics");
            _maintainDialogueHistory  = serializedObject.FindProperty("MaintainDialogueHistory");
            _kvCacheInitialization    = serializedObject.FindProperty("KvCacheInitialization");
            _workload                 = serializedObject.FindProperty("Workload");
            _autoCreateOnConnect      = serializedObject.FindProperty("AutoCreateOnConnect");
            _speaker                  = serializedObject.FindProperty("speaker");
            _voice                    = serializedObject.FindProperty("voice");
            _overrideVoiceDelivery    = serializedObject.FindProperty("overrideVoiceDelivery");
            _voiceDelivery            = serializedObject.FindProperty("voiceDelivery");
            _onAnswerText             = serializedObject.FindProperty("OnAnswerText");
            _onTurnComplete           = serializedObject.FindProperty("OnTurnComplete");
            _onMessageQueued          = serializedObject.FindProperty("OnMessageQueued");
            _onMessageDequeued        = serializedObject.FindProperty("OnMessageDequeued");
            _onError                  = serializedObject.FindProperty("OnError");
            _onAgentCreated           = serializedObject.FindProperty("OnAgentCreated");
            _onAgentDestroyed         = serializedObject.FindProperty("OnAgentDestroyed");
            _onParamChanged           = serializedObject.FindProperty("OnParamChanged");
            _onResumed                = serializedObject.FindProperty("OnResumed");
            _onKvCachePrefilled       = serializedObject.FindProperty("OnKvCachePrefilled");
            _onKvCacheEvicted         = serializedObject.FindProperty("OnKvCacheEvicted");
            _onKvCacheStatus          = serializedObject.FindProperty("OnKvCacheStatus");
            _onInteractionsAppended   = serializedObject.FindProperty("OnInteractionsAppended");
            _onInteractionsRemoved    = serializedObject.FindProperty("OnInteractionsRemoved");
            _onDialogExported         = serializedObject.FindProperty("OnDialogExported");
            _onDialogImported         = serializedObject.FindProperty("OnDialogImported");
            _onDialogSaved            = serializedObject.FindProperty("OnDialogSaved");
            _onDialogLoaded           = serializedObject.FindProperty("OnDialogLoaded");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_workflowAsset);

            bool hasWorkflow = !targetsHaveMixedWorkflow() &&
                               _workflowAsset.objectReferenceValue != null;

            if (!hasWorkflow)
                EditorGUILayout.HelpBox(MissingWorkflowAssetNotice, MessageType.Error);

            DrawVariableOverridesSection();

            // EnableDiagnostics authoring control retired (Package 2): Chat and Agent Log
            // request diagnostics via a ref-counted lease / transient create override.
            // The runtime field remains for API/wire compatibility and stays accounted
            // so DrawUnaccountedProperties does not resurface it.
            EditorGUILayout.PropertyField(_maintainDialogueHistory);
            EditorGUILayout.PropertyField(_autoCreateOnConnect);
            EditorGUILayout.PropertyField(_speaker);
            EditorGUILayout.PropertyField(_voice);
            DrawVoiceDelivery();
            if (!serializedObject.isEditingMultipleObjects)
            {
                foreach (var issue in TryllWorkflowValidator.ValidateAgentComponent((TryllAgentComponent)target))
                    EditorGUILayout.HelpBox(issue.Message,
                        issue.Level == TryllWorkflowValidator.Issue.Severity.Error
                            ? MessageType.Error : MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scheduling", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_workload);
            EditorGUILayout.PropertyField(_kvCacheInitialization);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_onAnswerText);
            EditorGUILayout.PropertyField(_onTurnComplete);
            EditorGUILayout.PropertyField(_onMessageQueued);
            EditorGUILayout.PropertyField(_onMessageDequeued);
            EditorGUILayout.PropertyField(_onError);
            EditorGUILayout.PropertyField(_onAgentCreated);
            EditorGUILayout.PropertyField(_onAgentDestroyed);
            EditorGUILayout.PropertyField(_onParamChanged);
            EditorGUILayout.PropertyField(_onResumed);
            EditorGUILayout.PropertyField(_onInteractionsAppended);
            EditorGUILayout.PropertyField(_onInteractionsRemoved);
            EditorGUILayout.PropertyField(_onDialogExported);
            EditorGUILayout.PropertyField(_onDialogImported);
            EditorGUILayout.PropertyField(_onDialogSaved);
            EditorGUILayout.PropertyField(_onDialogLoaded);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("KV Cache Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_onKvCachePrefilled);
            EditorGUILayout.PropertyField(_onKvCacheEvicted);
            EditorGUILayout.PropertyField(_onKvCacheStatus);

            if (!serializedObject.isEditingMultipleObjects)
            {
                var live = (TryllAgentComponent)target;
                if (live.HasAgent)
                    DrawLiveVariablesFlushSection(live);
            }

            DrawUnaccountedProperties();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Fallback for serialized fields missing from <see cref="AccountedProperties"/>:
        /// render them so they stay editable, and say so loudly. Without this, a new
        /// field added to <see cref="TryllAgentComponent"/> is simply unreachable from
        /// the Inspector with nothing to indicate why.
        /// </summary>
        private void DrawUnaccountedProperties()
        {
            List<SerializedProperty> stragglers = null;

            var it = serializedObject.GetIterator();
            it.NextVisible(enterChildren: true);          // steps onto m_Script
            while (it.NextVisible(enterChildren: false))  // top-level siblings only
            {
                if (it.propertyPath == "m_Script")
                    continue;
                if (System.Array.IndexOf(AccountedProperties, it.propertyPath) >= 0)
                    continue;
                (stragglers ??= new List<SerializedProperty>()).Add(it.Copy());
            }

            if (stragglers == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"{stragglers.Count} serialized field(s) are not placed by this " +
                "Inspector. They are drawn below so nothing is unreachable — give " +
                "them a home in TryllAgentComponentEditor.OnInspectorGUI and add them " +
                "to AccountedProperties.",
                MessageType.Warning);
            foreach (var p in stragglers)
                EditorGUILayout.PropertyField(p, includeChildren: true);
        }

        private void DrawVoiceDelivery()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.PropertyField(_overrideVoiceDelivery);
                using (new EditorGUI.DisabledScope(!_overrideVoiceDelivery.boolValue))
                    EditorGUILayout.PropertyField(_voiceDelivery, new GUIContent("Voice Delivery"));
                return;
            }

            var component = (TryllAgentComponent)target;
            var profile = component.Voice != null ? component.Voice.Data : null;
            var modes = new List<string> { string.Empty };
            if (profile?.DeliveryModes != null)
            {
                foreach (var mode in profile.DeliveryModes)
                {
                    if (mode != null && !string.IsNullOrEmpty(mode.Name) && !modes.Contains(mode.Name))
                        modes.Add(mode.Name);
                }
            }

            bool wasEnabled = _overrideVoiceDelivery.boolValue;
            string stored = TryllAgentComponent.NormalizeVoiceDelivery(_voiceDelivery.stringValue);
            string current = component.HasAgent
                ? TryllAgentComponent.NormalizeVoiceDelivery(component.Agent.DeliveryMode)
                : (wasEnabled
                    ? stored
                    : TryllAgentComponent.NormalizeVoiceDelivery(profile?.DefaultDeliveryMode));
            if (!modes.Contains(current))
                modes.Add(current);

            var labels = new GUIContent[modes.Count];
            for (int i = 0; i < modes.Count; i++)
            {
                string suffix = i > 0 && profile != null &&
                                (profile.DeliveryModes == null ||
                                 !profile.DeliveryModes.Exists(m => m != null && m.Name == modes[i]))
                    ? " (not in Voice Profile)"
                    : string.Empty;
                labels[i] = new GUIContent(i == 0 ? "neutral (built in)" : modes[i] + suffix);
            }

            EditorGUILayout.BeginHorizontal();
            bool enabledNow = EditorGUILayout.Toggle(
                new GUIContent("Voice Delivery",
                    "Override the Voice Profile's default delivery for this agent instance. " +
                    "Changing this while an agent is live applies immediately."),
                wasEnabled);

            if (enabledNow && !wasEnabled)
            {
                stored = current;
                _voiceDelivery.stringValue = stored;
            }

            int selected = Mathf.Max(0, modes.IndexOf(enabledNow ? current :
                TryllAgentComponent.NormalizeVoiceDelivery(profile?.DefaultDeliveryMode)));
            int picked;
            using (new EditorGUI.DisabledScope(!enabledNow || profile == null))
                picked = EditorGUILayout.Popup(selected, labels);
            EditorGUILayout.EndHorizontal();

            bool modeChanged = enabledNow && picked != selected;
            bool toggleChanged = enabledNow != wasEnabled;
            if (modeChanged)
                _voiceDelivery.stringValue = modes[picked];
            if (toggleChanged)
                _overrideVoiceDelivery.boolValue = enabledNow;

            if (toggleChanged || modeChanged)
            {
                serializedObject.ApplyModifiedProperties();
                string desired = enabledNow
                    ? TryllAgentComponent.NormalizeVoiceDelivery(_voiceDelivery.stringValue)
                    : TryllAgentComponent.NormalizeVoiceDelivery(profile?.DefaultDeliveryMode);
                if (component.HasAgent)
                    ApplyVoiceDeliveryToLiveAgent(component, desired);
                else
                {
                    _voiceDeliveryStatus = enabledNow
                        ? "Override saved for the next CreateAgent."
                        : "Voice Profile default restored for the next CreateAgent.";
                    _voiceDeliveryStatusType = MessageType.Info;
                }
            }

            if (!string.IsNullOrEmpty(_voiceDeliveryStatus))
                EditorGUILayout.HelpBox(_voiceDeliveryStatus, _voiceDeliveryStatusType);
        }

        private async void ApplyVoiceDeliveryToLiveAgent(TryllAgentComponent component, string mode)
        {
            var error = await component.SetDeliveryModeAsync(mode);
            if (error.IsOk)
            {
                _voiceDeliveryStatus =
                    $"Applied {(string.IsNullOrEmpty(mode) ? TryllVoiceProfileData.NeutralMode : mode)} to the live agent.";
                _voiceDeliveryStatusType = MessageType.Info;
            }
            else
            {
                _voiceDeliveryStatus = $"Apply failed: {error.Code} {error.Message}";
                _voiceDeliveryStatusType = MessageType.Error;
            }
            Repaint();
        }

        private void DrawVariableOverridesSection()
        {
            EditorGUILayout.Space();
            _overridesFoldout = EditorGUILayout.Foldout(
                _overridesFoldout, OverridesFoldoutLabel, toggleOnLabelClick: true, style: EditorStyles.foldoutHeader);
            if (!_overridesFoldout) return;

            EditorGUI.indentLevel++;
            try
            {
                if (serializedObject.isEditingMultipleObjects)
                {
                    EditorGUILayout.HelpBox(
                        "Variable Overrides cannot be multi-edited. Select a single component.",
                        MessageType.Info);
                    return;
                }

                var component = (TryllAgentComponent)target;
                var declared = TryllVariableOverrideListUtil.ResolveDeclared(component);

                if (declared == null || declared.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No variables declared. Add them on the Workflow Asset.",
                        MessageType.Info);
                }
                else
                {
                    for (int i = 0; i < declared.Count; i++)
                    {
                        var decl = declared[i];
                        if (decl == null || string.IsNullOrEmpty(decl.Name)) continue;
                        DrawDeclarationRow(component, decl);
                    }
                }

                DrawStaleFoldout(declared);
            }
            finally
            {
                EditorGUI.indentLevel--;
            }
        }

        private void DrawDeclarationRow(TryllAgentComponent component, TryllVariableDecl decl)
        {
            int idx = FindSerializedOverrideIndex(decl.Name);
            bool hasEntry = idx >= 0;
            SerializedProperty enabledProp = null;
            SerializedProperty valueProp = null;
            if (hasEntry)
            {
                var ovProp = _variableOverrides.GetArrayElementAtIndex(idx);
                enabledProp = ovProp.FindPropertyRelative("Enabled");
                valueProp = ovProp.FindPropertyRelative("Value");
                valueProp.FindPropertyRelative("Name").stringValue = decl.Name;
                var typeProp = valueProp.FindPropertyRelative("Type");
                if (typeProp.enumValueIndex != (int)decl.Type)
                    TryllVariableDeclGui.WriteDecl(valueProp, decl); // type drift → re-seed
                else
                    typeProp.enumValueIndex = (int)decl.Type;
            }

            bool wasEnabled = enabledProp != null && enabledProp.boolValue;
            float line = EditorGUIUtility.singleLineHeight;
            float valueHeight = wasEnabled && valueProp != null
                ? TryllVariableDeclGui.GetValueHeight(valueProp, decl.Type)
                : line;
            bool compact = valueHeight <= line + 1f;
            float totalHeight = compact ? Mathf.Max(line, valueHeight) : line + Spacing + valueHeight;

            // GetControlRect already applies indentLevel to the rect. Toggle /
            // PropertyField would indent again (same hit-target bug as Sampling
            // optionals), so draw the row with indentLevel 0.
            var rowRect = EditorGUILayout.GetControlRect(false, totalHeight);
            int savedIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            try
            {
                var toggleRect = new Rect(rowRect.x, rowRect.y, ToggleWidth, line);
                var labelRect = new Rect(
                    toggleRect.xMax + Spacing,
                    rowRect.y,
                    EditorGUIUtility.labelWidth - ToggleWidth - Spacing,
                    line);

                EditorGUI.BeginChangeCheck();
                bool enabledNow;
                if (enabledProp != null)
                {
                    EditorGUI.PropertyField(toggleRect, enabledProp, GUIContent.none);
                    enabledNow = enabledProp.boolValue;
                }
                else
                {
                    enabledNow = EditorGUI.Toggle(toggleRect, false);
                    if (enabledNow)
                    {
                        // First enable → create a sparse entry seeded from the declaration.
                        EnsureSerializedEntry(decl, seedIfNew: true);
                        idx = FindSerializedOverrideIndex(decl.Name);
                        if (idx >= 0)
                        {
                            var ovProp = _variableOverrides.GetArrayElementAtIndex(idx);
                            enabledProp = ovProp.FindPropertyRelative("Enabled");
                            enabledProp.boolValue = true;
                            valueProp = ovProp.FindPropertyRelative("Value");
                        }
                    }
                }

                EditorGUI.LabelField(labelRect, new GUIContent(decl.Name, $"Type: {decl.Type}"));

                Rect valueRect;
                if (compact)
                {
                    valueRect = new Rect(
                        rowRect.x + EditorGUIUtility.labelWidth,
                        rowRect.y,
                        rowRect.width - EditorGUIUtility.labelWidth,
                        valueHeight);
                }
                else
                {
                    valueRect = new Rect(
                        rowRect.x,
                        rowRect.y + line + Spacing,
                        rowRect.width,
                        valueHeight);
                }

                using (new EditorGUI.DisabledScope(!enabledNow))
                {
                    if (enabledNow && valueProp != null)
                        TryllVariableDeclGui.DrawValueField(valueRect, valueProp, decl.Type, GUIContent.none);
                    else
                        DrawReadonlyDefault(valueRect, decl);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    // Live apply whenever an agent exists — Play Mode or Editor Tryll Chat.
                    if (component.HasAgent)
                    {
                        SerializedProperty applyValue = null;
                        int applyIdx = FindSerializedOverrideIndex(decl.Name);
                        if (applyIdx >= 0)
                            applyValue = _variableOverrides.GetArrayElementAtIndex(applyIdx)
                                .FindPropertyRelative("Value");
                        ApplyRowToLiveAgent(component, decl, enabledNow, applyValue);
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel = savedIndent;
            }
        }

        private static void DrawReadonlyDefault(Rect rect, TryllVariableDecl decl)
        {
            switch (decl.Type)
            {
                case TryllVariableType.Int:
                    EditorGUI.LongField(rect, decl.IntValue);
                    break;
                case TryllVariableType.Float:
                    EditorGUI.DoubleField(rect, decl.FloatValue);
                    break;
                case TryllVariableType.String:
                    EditorGUI.TextField(rect, decl.StringValue ?? "");
                    break;
                case TryllVariableType.Bool:
                    EditorGUI.Toggle(rect, decl.BoolValue);
                    break;
                case TryllVariableType.StringSet:
                    EditorGUI.LabelField(rect, FormatStringSetPreview(decl.StringSetValue));
                    break;
                default:
                    EditorGUI.LabelField(rect, "—");
                    break;
            }
        }

        private static string FormatStringSetPreview(List<string> values)
        {
            if (values == null || values.Count == 0) return "(empty set)";
            return string.Join(", ", values);
        }

        private void ApplyRowToLiveAgent(
            TryllAgentComponent component,
            TryllVariableDecl decl,
            bool enabled,
            SerializedProperty valueProp)
        {
            if (component == null || !component.HasAgent || component.Agent?.Variables == null)
            {
                _liveVarsStatus = "No live agent — override saved for next CreateAgent.";
                _liveVarsStatusType = MessageType.Info;
                return;
            }

            var overrideValue = TryllVariableDeclGui.ReadDecl(valueProp);
            var err = TryllVariableOverrideListUtil.ApplyEffectiveValue(
                component.Agent.Variables, decl, overrideValue, enabled);
            if (!err.IsOk)
            {
                _liveVarsStatus = $"Apply failed: {err.Code} {err.Message}";
                _liveVarsStatusType = MessageType.Error;
                Debug.LogWarning(
                    $"[Tryll] VariableOverrides: apply '{decl.Name}' failed: {err.Code} {err.Message}");
                return;
            }

            string valuePreview = FormatDeclValuePreview(enabled ? overrideValue : decl);
            _liveVarsStatus = enabled
                ? $"Updated '{decl.Name}'={valuePreview} on local mirror (flush deferred until next Send/Resume/ChangeParams, or Flush Now)."
                : $"'{decl.Name}' restored to workflow default ({valuePreview}) on local mirror (flush deferred).";
            _liveVarsStatusType = MessageType.Info;
            Debug.Log(
                $"[Tryll] VariableOverrides: staged '{decl.Name}'={valuePreview} " +
                $"(enabled={enabled}, dirty={component.Agent.Variables.IsDirty}, " +
                $"playMode={Application.isPlaying}). Flush on next Send/Resume/ChangeParams or Flush Now.");
            Repaint();
        }

        private static string FormatDeclValuePreview(TryllVariableDecl d)
        {
            if (d == null) return "?";
            switch (d.Type)
            {
                case TryllVariableType.Int:       return d.IntValue.ToString();
                case TryllVariableType.Float:     return d.FloatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case TryllVariableType.String:    return $"\"{d.StringValue}\"";
                case TryllVariableType.Bool:      return d.BoolValue ? "true" : "false";
                case TryllVariableType.StringSet: return FormatStringSetPreview(d.StringSetValue);
                default:                          return "?";
            }
        }

        private void DrawLiveVariablesFlushSection(TryllAgentComponent component)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live Agent — Variables", EditorStyles.boldLabel);

            if (!component.HasAgent || component.Agent?.Variables == null)
            {
                EditorGUILayout.HelpBox("Agent not created yet.", MessageType.Info);
                return;
            }

            var vars = component.Agent.Variables;
            EditorGUILayout.LabelField("Dirty", vars.IsDirty ? "Yes — pending flush" : "No");

            using (new EditorGUI.DisabledScope(!vars.IsDirty))
            {
                if (GUILayout.Button("Flush Now"))
                {
                    Debug.Log("[Tryll] VariableOverrides: Flush Now requested.");
                    vars.FlushIfDirty(err =>
                    {
                        if (err.IsOk)
                        {
                            _liveVarsStatus = "Flush succeeded — server has the staged values.";
                            _liveVarsStatusType = MessageType.Info;
                            Debug.Log("[Tryll] VariableOverrides: Flush Now succeeded.");
                        }
                        else
                        {
                            _liveVarsStatus = $"Flush failed: {err.Code} {err.Message}";
                            _liveVarsStatusType = MessageType.Error;
                            Debug.LogWarning(
                                $"[Tryll] VariableOverrides: Flush Now failed: {err.Code} {err.Message}");
                        }
                        Repaint();
                    });
                }
            }

            if (_liveVarsStatusType != MessageType.None && !string.IsNullOrEmpty(_liveVarsStatus))
                EditorGUILayout.HelpBox(_liveVarsStatus, _liveVarsStatusType);
        }

        private void DrawStaleFoldout(List<TryllVariableDecl> declared)
        {
            // Build from the SerializedProperty so in-progress Inspector edits are included
            // before ApplyModifiedProperties runs at the end of OnInspectorGUI.
            var snapshot = new List<TryllVariableOverride>(_variableOverrides.arraySize);
            for (int i = 0; i < _variableOverrides.arraySize; i++)
            {
                var el = _variableOverrides.GetArrayElementAtIndex(i);
                snapshot.Add(new TryllVariableOverride
                {
                    Name    = el.FindPropertyRelative("Name").stringValue,
                    Enabled = el.FindPropertyRelative("Enabled").boolValue,
                    // Value unused by CollectLegacyIndices — name-only classification.
                    Value   = null,
                });
            }
            var legacy = TryllVariableOverrideListUtil.CollectLegacyIndices(snapshot, declared);
            if (legacy.Count == 0) return;

            EditorGUILayout.Space();
            _staleFoldout = EditorGUILayout.Foldout(
                _staleFoldout,
                $"Stale / legacy overrides ({legacy.Count})",
                true);
            if (!_staleFoldout) return;

            EditorGUILayout.HelpBox(
                "These serialized overrides no longer match a declared variable (renamed/removed) " +
                "or are duplicate names. They are ignored at CreateAgent. Remove them to clean up.",
                MessageType.Warning);

            // Draw highest index first so Remove buttons stay valid within one GUI frame
            // when the user clicks multiple — we only offer Remove all / per-row via index.
            foreach (int idx in legacy)
            {
                if (idx < 0 || idx >= _variableOverrides.arraySize) continue;
                var el = _variableOverrides.GetArrayElementAtIndex(idx);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(el, includeChildren: true);
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    _variableOverrides.DeleteArrayElementAtIndex(idx);
                    break; // indices invalidated; redraw next frame
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Remove all stale / legacy overrides"))
            {
                // Delete from high index to low against the serialized property.
                for (int i = legacy.Count - 1; i >= 0; i--)
                {
                    int idx = legacy[i];
                    if (idx >= 0 && idx < _variableOverrides.arraySize)
                        _variableOverrides.DeleteArrayElementAtIndex(idx);
                }
            }
        }

        private void EnsureSerializedEntry(TryllVariableDecl decl, bool seedIfNew)
        {
            int idx = FindSerializedOverrideIndex(decl.Name);
            if (idx >= 0)
            {
                var existing = _variableOverrides.GetArrayElementAtIndex(idx);
                var valueProp = existing.FindPropertyRelative("Value");
                var typeProp = valueProp.FindPropertyRelative("Type");
                if (typeProp.enumValueIndex != (int)decl.Type)
                {
                    // Type changed on the declaration — re-seed to avoid merge mismatch.
                    TryllVariableDeclGui.WriteDecl(valueProp, decl);
                }
                else
                {
                    valueProp.FindPropertyRelative("Name").stringValue = decl.Name;
                }
                return;
            }

            int newIndex = _variableOverrides.arraySize;
            _variableOverrides.InsertArrayElementAtIndex(newIndex);
            // InsertArrayElementAtIndex duplicates the previous element when size > 0 —
            // always rewrite the new slot fully.
            var created = _variableOverrides.GetArrayElementAtIndex(newIndex);
            created.FindPropertyRelative("Name").stringValue = decl.Name;
            created.FindPropertyRelative("Enabled").boolValue = false;
            var createdValue = created.FindPropertyRelative("Value");
            if (seedIfNew)
                TryllVariableDeclGui.WriteDecl(createdValue, decl);
            else
            {
                createdValue.FindPropertyRelative("Name").stringValue = decl.Name;
                createdValue.FindPropertyRelative("Type").enumValueIndex = (int)decl.Type;
            }
        }

        private int FindSerializedOverrideIndex(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            for (int i = 0; i < _variableOverrides.arraySize; i++)
            {
                var el = _variableOverrides.GetArrayElementAtIndex(i);
                if (el.FindPropertyRelative("Name").stringValue == name)
                    return i;
            }
            return -1;
        }

        private bool targetsHaveMixedWorkflow()
        {
            if (!serializedObject.isEditingMultipleObjects) return false;
            Object first = _workflowAsset.objectReferenceValue;
            foreach (var t in targets)
            {
                if (t is TryllAgentComponent c)
                {
                    var wa = c.WorkflowAsset;
                    if ((Object)wa != first) return true;
                }
            }
            return _workflowAsset.hasMultipleDifferentValues;
        }
    }
}
