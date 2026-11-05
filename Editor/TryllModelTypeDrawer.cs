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
    /// Inspector property drawer for fields annotated with <see cref="TryllModelTypeAttribute"/>.
    ///
    /// Renders the model-name string as a dropdown of the project's registered models
    /// (the <see cref="TryllModelManifest"/>) filtered to the attribute's model type, plus
    /// the special <c>(none)</c> option that maps to an empty string. Each option is
    /// suffixed with its registration tier, and a coloured tier badge for the current
    /// selection is drawn to the right of the popup.
    ///
    /// A model name that is set but no longer registered for this type still shows in the
    /// list (so the value is never silently dropped) and reads as <c>Unregistered</c>.
    ///
    /// When the project has a language selected (Project Settings ▸ Tryll Client)
    /// and the picked voice model does not declare it, a warning is drawn beneath
    /// the popup — the same mismatch the server rejects at agent creation, surfaced
    /// at the moment the model is chosen.
    ///
    /// This is the Unity counterpart to Unreal's <c>FTryllModelNameCustomization</c>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllModelTypeAttribute))]
    internal sealed class TryllModelTypeDrawer : PropertyDrawer
    {
        private const string NoneLabel = "(none)";
        private const float  BadgeWidth = 92f;
        private const float  BadgeGap   = 4f;

        private static readonly Color s_Production   = new Color(0.30f, 0.78f, 0.45f);
        private static readonly Color s_Experimental = new Color(0.95f, 0.70f, 0.25f);
        private static readonly Color s_Unregistered = new Color(0.55f, 0.55f, 0.58f);

        private static TryllModelManifest s_Manifest;
        private static GUIStyle s_BadgeStyle;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight;
            string warning = LanguageWarning(property);
            if (warning != null)
                h += EditorGUIUtility.standardVerticalSpacing + WarningHeight(warning);
            return h;
        }

        /// <summary>
        /// Height the help box needs for <paramref name="text"/> at the inspector's
        /// current width. Measured rather than fixed: these messages name a model
        /// and a language, so their length varies enough that a constant would
        /// clip some and leave dead space under others.
        ///
        /// <c>currentViewWidth</c> is an approximation (the drawer does not know
        /// its own width until OnGUI), so a floor keeps a mis-measure from
        /// collapsing the box.
        /// </summary>
        private static float WarningHeight(string text)
        {
            float width = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - HelpBoxWidthSlack);
            float measured = EditorStyles.helpBox.CalcHeight(new GUIContent(text), width);
            return Mathf.Max(MinWarningHeight, measured);
        }

        private const float MinWarningHeight   = 38f;
        private const float HelpBoxWidthSlack  = 40f;

        /// <summary>
        /// Warning text when the picked model cannot do the project's selected
        /// language, or null when there is nothing to say.
        ///
        /// Silent (returns null) in three cases, all of which mean "no evidence of
        /// a problem" rather than "fine": no project language chosen, the model is
        /// unregistered or undeclared, or this is not a voice model. Only TTS and
        /// STT consume the session locale — warning on a language model would be
        /// noise, since language competence is a quality gradient we do not
        /// declare.
        /// </summary>
        private string LanguageWarning(SerializedProperty property)
        {
            if (property == null || property.propertyType != SerializedPropertyType.String)
                return null;

            var attr = attribute as TryllModelTypeAttribute;
            if (attr == null) return null;
            if (attr.ModelType != "tts" && attr.ModelType != "stt") return null;

            string modelName = property.stringValue;
            if (string.IsNullOrEmpty(modelName)) return null;

            var settings = TryllRuntimeSettings.Load();
            string locale = settings != null ? settings.Locale : null;
            if (string.IsNullOrEmpty(locale)) return null;

            var manifest = Manifest();
            if (manifest == null) return null;

            var declared = manifest.LanguagesOf(modelName);
            if (declared == null || declared.Length == 0) return null;  // undeclared = unknown

            string lang = TryllLanguages.LanguageOf(locale);
            foreach (var code in declared)
                if (string.Equals(code, lang, System.StringComparison.OrdinalIgnoreCase))
                    return null;

            string prefix = $"'{modelName}' does not support the project language "
                          + $"{TryllLanguages.DisplayName(lang)} ({lang}). ";
            if (attr.ModelType == "tts")
            {
                return prefix + "Set the voice profile's Lang explicitly, pick another model, "
                     + "or change the language in Project Settings ▸ Tryll Client. "
                     + "Agent creation fails otherwise.";
            }
            return prefix + "Pick another STT model or change the language in "
                 + "Project Settings ▸ Tryll Client. Voice input creation fails otherwise.";
        }



        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var attr = (TryllModelTypeAttribute)attribute;
            var manifest = Manifest();
            string current = property.stringValue ?? string.Empty;

            // ── Build the option list (index 0 = none / empty) ─────────────────
            var names = new List<string> { string.Empty };
            if (manifest != null)
                names.AddRange(manifest.ModelsOfType(attr.ModelType));

            // Keep a set value that is no longer registered for this type so it is
            // never silently dropped.
            if (!string.IsNullOrEmpty(current) && !names.Contains(current))
                names.Add(current);

            var display = new string[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                if (string.IsNullOrEmpty(names[i]))
                {
                    display[i] = NoneLabel;
                    continue;
                }
                display[i] = $"{names[i]}  ({TierLabel(Tier(manifest, names[i]))})";
            }

            int selected = Mathf.Max(0, names.IndexOf(current));

            // ── Draw ───────────────────────────────────────────────────────────
            EditorGUI.BeginProperty(position, label, property);

            // Pin the control row to one line rather than `position.height`:
            // GetPropertyHeight grows the property when a language warning is
            // shown, and stretching the popup and tier badge to the full height
            // would draw them straight over the warning box below.
            float rowHeight = EditorGUIUtility.singleLineHeight;

            var popupRect = new Rect(
                position.x, position.y,
                position.width - BadgeWidth - BadgeGap, rowHeight);
            var badgeRect = new Rect(
                popupRect.xMax + BadgeGap, position.y,
                BadgeWidth, rowHeight);

            int next = EditorGUI.Popup(popupRect, label.text, selected, display);
            if (next != selected)
                property.stringValue = names[next];

            DrawTierBadge(badgeRect, manifest, property.stringValue);

            string warning = LanguageWarning(property);
            if (warning != null)
            {
                var warnRect = new Rect(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                    position.width, WarningHeight(warning));
                EditorGUI.HelpBox(warnRect, warning, MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }

        private static void DrawTierBadge(Rect rect, TryllModelManifest manifest, string name)
        {
            if (string.IsNullOrEmpty(name))
                return; // nothing selected: no badge

            var tier = Tier(manifest, name);
            Color color =
                tier == TryllModelRegistration.Production   ? s_Production   :
                tier == TryllModelRegistration.Experimental ? s_Experimental :
                                                              s_Unregistered;

            var fill = color;
            fill.a = 0.22f;
            EditorGUI.DrawRect(rect, fill);

            if (s_BadgeStyle == null)
            {
                s_BadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                };
            }
            s_BadgeStyle.normal.textColor = color;
            GUI.Label(rect, TierLabel(tier), s_BadgeStyle);
        }

        private static TryllModelRegistration Tier(TryllModelManifest manifest, string name)
            => manifest != null ? manifest.Get(name) : TryllModelRegistration.None;

        private static string TierLabel(TryllModelRegistration tier)
        {
            switch (tier)
            {
                case TryllModelRegistration.Production:   return "Production";
                case TryllModelRegistration.Experimental: return "Experimental";
                default:                                  return "Unregistered";
            }
        }

        private static TryllModelManifest Manifest()
        {
            if (s_Manifest != null)
                return s_Manifest;

            var guids = AssetDatabase.FindAssets("t:TryllModelManifest");
            if (guids.Length > 0)
            {
                s_Manifest = AssetDatabase.LoadAssetAtPath<TryllModelManifest>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return s_Manifest;
        }
    }
}
