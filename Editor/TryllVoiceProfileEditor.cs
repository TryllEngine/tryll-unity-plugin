// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Inspector for <see cref="TryllVoiceProfile"/>. Capability-driven: once the model is
    /// picked (through <see cref="TryllModelTypeDrawer"/>), only the fields that model can
    /// use are shown, read from the catalog's <c>tts_capabilities</c>
    /// (<see cref="TryllModelCatalogFile"/>). A field the model cannot use is still shown
    /// while it holds a non-default value, so a stale value is visible and fixable — the
    /// server rejects it with InvalidVoiceProfile. When the catalog is unavailable every
    /// field is shown.
    ///
    /// Speaker Id is a preset dropdown for models with named presets (Orpheus, Maya1),
    /// a 0..N-1 slider for multi-speaker models (Supertonic 3), and hidden for
    /// single-speaker models. Delivery modes are a reorderable list whose rows show the
    /// same capability filtering. Validation findings from
    /// <see cref="TryllWorkflowValidator.ValidateVoiceProfile(TryllVoiceProfile)"/> are
    /// drawn at the bottom.
    /// </summary>
    [CustomEditor(typeof(TryllVoiceProfile))]
    [CanEditMultipleObjects]
    internal sealed class TryllVoiceProfileEditor : UnityEditor.Editor
    {
        private SerializedProperty _data;
        private SerializedProperty _model;
        private SerializedProperty _speakerId;
        private SerializedProperty _referenceVoice;
        private SerializedProperty _voiceDescription;
        private SerializedProperty _speed;
        private SerializedProperty _lang;
        private SerializedProperty _expressiveness;
        private SerializedProperty _prepare;
        private SerializedProperty _emitVisemes;
        private SerializedProperty _modes;
        private SerializedProperty _defaultMode;

        private ReorderableList _modeList;
        private TryllTtsCapabilities? _caps;

        private const float RowGap = 2f;

        private void OnEnable()
        {
            _data             = serializedObject.FindProperty("Data");
            _model            = _data.FindPropertyRelative("TtsModelName");
            _speakerId        = _data.FindPropertyRelative("SpeakerId");
            _referenceVoice   = _data.FindPropertyRelative("ReferenceVoice");
            _voiceDescription = _data.FindPropertyRelative("VoiceDescription");
            _speed            = _data.FindPropertyRelative("Speed");
            _lang             = _data.FindPropertyRelative("Lang");
            _expressiveness   = _data.FindPropertyRelative("Expressiveness");
            _prepare          = _data.FindPropertyRelative("Prepare");
            _emitVisemes      = _data.FindPropertyRelative("EmitVisemes");
            _modes            = _data.FindPropertyRelative("DeliveryModes");
            _defaultMode      = _data.FindPropertyRelative("DefaultDeliveryMode");

            _modeList = new ReorderableList(serializedObject, _modes, true, true, true, true)
            {
                drawHeaderCallback = r => EditorGUI.LabelField(r,
                    new GUIContent("Delivery Modes",
                        "Named deliveries switched with SetDeliveryModeAsync. 'neutral' is built in.")),
                elementHeightCallback = ModeHeight,
                drawElementCallback = DrawMode,
                onAddCallback = l =>
                {
                    int i = _modes.arraySize;
                    _modes.InsertArrayElementAtIndex(i);
                    var e = _modes.GetArrayElementAtIndex(i);
                    // InsertArrayElement copies the previous row; start from the type's defaults.
                    e.FindPropertyRelative("Name").stringValue = i == 0 ? "angry" : $"mode_{i}";
                    e.FindPropertyRelative("ReferenceVoice").stringValue = string.Empty;
                    e.FindPropertyRelative("Instruction").stringValue = string.Empty;
                    e.FindPropertyRelative("Expressiveness").floatValue = -1f;
                    e.FindPropertyRelative("SpeedScale").floatValue = 1f;
                },
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_model, new GUIContent("Tts Model Name", _model.tooltip));

            _caps = null;
            bool mixedModel = _model.hasMultipleDifferentValues;
            if (!mixedModel && !string.IsNullOrEmpty(_model.stringValue) &&
                TryllModelCatalogFile.TryGetTtsCapabilities(_model.stringValue, out var caps))
                _caps = caps;
            if (!mixedModel && !string.IsNullOrEmpty(_model.stringValue) && !_caps.HasValue)
                EditorGUILayout.HelpBox(
                    "The catalog declares no TTS capabilities for this model (or the catalog " +
                    "could not be read), so every field is shown. The server still rejects " +
                    "fields the model cannot use.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            DrawSpeakerId();
            if (Show(c => c.ReferenceVoice, _referenceVoice, IsEmptyString))
                EditorGUILayout.PropertyField(_referenceVoice);
            if (Show(c => c.VoiceDescription, _voiceDescription, IsEmptyString))
                EditorGUILayout.PropertyField(_voiceDescription);
            if (Show(c => c.Speed, _speed, p => Mathf.Approximately(p.floatValue, 1f)))
                EditorGUILayout.PropertyField(_speed);
            if (Show(c => c.LanguagePerCall, _lang, IsEmptyString))
                EditorGUILayout.PropertyField(_lang);
            // Profile expressiveness is ignored (not rejected) by other families, so it is
            // hidden for them regardless of value.
            if (!_caps.HasValue || _caps.Value.Expressiveness)
                EditorGUILayout.PropertyField(_expressiveness);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Speech", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_emitVisemes);
            EditorGUILayout.PropertyField(_prepare);

            EditorGUILayout.Space();
            _modeList.DoLayoutList();
            DrawDefaultMode();

            serializedObject.ApplyModifiedProperties();

            if (!serializedObject.isEditingMultipleObjects)
            {
                foreach (var issue in TryllWorkflowValidator.ValidateVoiceProfile((TryllVoiceProfile)target))
                    EditorGUILayout.HelpBox(issue.Message,
                        issue.Level == TryllWorkflowValidator.Issue.Severity.Error
                            ? MessageType.Error : MessageType.Warning);
            }
        }

        // ── Identity ──────────────────────────────────────────────────────────

        private void DrawSpeakerId()
        {
            if (!_caps.HasValue || _speakerId.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(_speakerId);
                return;
            }
            var c = _caps.Value;
            var presets = c.VoicePresets;
            if (presets != null && presets.Length > 0)
            {
                var labels = new List<GUIContent>(presets.Length + 1);
                for (int i = 0; i < presets.Length; i++)
                {
                    string label = string.IsNullOrEmpty(presets[i].Label) ? presets[i].Id : presets[i].Label;
                    labels.Add(new GUIContent($"{i}: {label}", presets[i].Description ?? string.Empty));
                }
                int current = _speakerId.intValue;
                if (current < 0 || current >= presets.Length)
                    labels.Add(new GUIContent($"{current}: (out of range)"));
                int shown = current >= 0 && current < presets.Length ? current : presets.Length;
                int picked = EditorGUILayout.Popup(
                    new GUIContent("Speaker", _speakerId.tooltip), shown, labels.ToArray());
                if (picked != shown && picked < presets.Length)
                    _speakerId.intValue = picked;
                return;
            }
            if (c.NumSpeakers > 1)
            {
                EditorGUILayout.IntSlider(_speakerId, 0, c.NumSpeakers - 1,
                    new GUIContent("Speaker Id", _speakerId.tooltip));
                return;
            }
            // Single speaker: only surface a stale non-zero value.
            if (_speakerId.intValue != 0)
                EditorGUILayout.PropertyField(_speakerId);
        }

        private bool Show(System.Func<TryllTtsCapabilities, bool> supported, SerializedProperty p,
                          System.Func<SerializedProperty, bool> isDefault)
        {
            if (!_caps.HasValue || supported(_caps.Value)) return true;
            return p.hasMultipleDifferentValues || !isDefault(p);
        }

        private static bool IsEmptyString(SerializedProperty p) => string.IsNullOrEmpty(p.stringValue);

        // ── Delivery modes ────────────────────────────────────────────────────

        private IEnumerable<SerializedProperty> VisibleModeFields(SerializedProperty e)
        {
            yield return e.FindPropertyRelative("Name");
            var clip = e.FindPropertyRelative("ReferenceVoice");
            if (Show(c => c.ReferenceVoice, clip, IsEmptyString)) yield return clip;
            var instr = e.FindPropertyRelative("Instruction");
            if (Show(c => c.DeliveryInstruction, instr, IsEmptyString)) yield return instr;
            var expr = e.FindPropertyRelative("Expressiveness");
            if (Show(c => c.Expressiveness, expr, p => p.floatValue < 0f)) yield return expr;
            var scale = e.FindPropertyRelative("SpeedScale");
            if (Show(c => c.Speed, scale, p => Mathf.Approximately(p.floatValue, 1f))) yield return scale;
        }

        private float ModeHeight(int index)
        {
            if (index < 0 || index >= _modes.arraySize) return EditorGUIUtility.singleLineHeight;
            var e = _modes.GetArrayElementAtIndex(index);
            float h = RowGap * 2;
            foreach (var p in VisibleModeFields(e))
                h += EditorGUI.GetPropertyHeight(p, true) + RowGap;
            return h;
        }

        private void DrawMode(Rect rect, int index, bool isActive, bool isFocused)
        {
            var e = _modes.GetArrayElementAtIndex(index);
            float y = rect.y + RowGap;
            foreach (var p in VisibleModeFields(e))
            {
                float h = EditorGUI.GetPropertyHeight(p, true);
                EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, h), p, true);
                y += h + RowGap;
            }
        }

        private void DrawDefaultMode()
        {
            if (_defaultMode.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(_defaultMode);
                return;
            }
            var names = new List<string> { string.Empty };
            for (int i = 0; i < _modes.arraySize; i++)
            {
                string n = _modes.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue;
                if (!string.IsNullOrEmpty(n) && !names.Contains(n)) names.Add(n);
            }
            string current = _defaultMode.stringValue ?? string.Empty;
            if (current == TryllVoiceProfileData.NeutralMode) current = string.Empty;
            if (!names.Contains(current)) names.Add(current);  // keep a stale value visible
            var labels = new GUIContent[names.Count];
            for (int i = 0; i < names.Count; i++)
                labels[i] = new GUIContent(i == 0 ? "neutral (built in)" : names[i]);
            int sel = names.IndexOf(current);
            int next = EditorGUILayout.Popup(
                new GUIContent("Default Delivery Mode", _defaultMode.tooltip), sel, labels);
            if (next != sel)
                _defaultMode.stringValue = names[next];
        }
    }
}
