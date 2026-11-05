// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

#if TRYLL_HAS_UNITY_LOCALIZATION
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Tryll.Client
{
    /// <summary>
    /// Adapter for <c>com.unity.localization</c> string tables. Compiled only when
    /// that package is installed. Returns the raw table entry — Smart String
    /// formatting is off, because <c>{}</c> placeholders collide with Mustache
    /// <c>{{ }}</c> if an author nests them.
    /// </summary>
    [CreateAssetMenu(menuName = "Tryll/Unity Localization Text Provider", fileName = "TryllUnityLocalizationTextProvider")]
    public sealed class TryllUnityLocalizationTextProvider : TryllTextProviderAsset, ITryllTextProviderEditor
    {
        public override string CurrentLocale =>
            LocalizationSettings.SelectedLocale != null
                ? LocalizationSettings.SelectedLocale.Identifier.Code
                : null;

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }

        void HandleLocaleChanged(Locale _) => RaiseLocaleChanged();

        public override async Task<string> GetTextAsync(string table, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var locale in FallbackChain())
            {
                string text = await RawAsync(table, id, locale);
                if (!string.IsNullOrEmpty(text)) return text;
            }
            return null;
        }

        public IReadOnlyList<string> ListLocales()
        {
            var list = new List<string>();
            var available = LocalizationSettings.AvailableLocales;
            if (available == null) return list;
            foreach (var locale in available.Locales)
            {
                if (locale != null) list.Add(locale.Identifier.Code);
            }
            return list;
        }

        public IReadOnlyList<string> ListIds(string table)
        {
            var ids = new List<string>();
            var stringTable = LocalizationSettings.StringDatabase.GetTable(table);
            var shared = stringTable?.SharedData;
            if (shared == null) return ids;
            foreach (var entry in shared.Entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                    ids.Add(entry.Key);
            }
            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        public string GetTextForPreview(string table, string id, string locale)
        {
            var loc = FindLocale(locale);
            if (loc == null) return null;
            var stringTable = LocalizationSettings.StringDatabase.GetTable(table, loc);
            return stringTable?.GetEntry(id)?.Value;
        }

        public void OpenInEditor(string table, string id)
            => TryllTextEditorHooks.Open?.Invoke(this, table, id);

        static readonly HashSet<string> s_Warned = new HashSet<string>();

        static async Task<string> RawAsync(string table, string id, Locale locale)
        {
            try
            {
                var handle = LocalizationSettings.StringDatabase.GetTableAsync(table, locale);
                var stringTable = await handle.Task;
                if (stringTable == null && s_Warned.Add(table + "|" + locale.Identifier.Code))
                {
                    TryllLog.Warning(
                        $"Unity Localization has no string table '{table}' for '{locale.Identifier.Code}'.");
                }
                return stringTable?.GetEntry(id)?.Value;
            }
            catch (Exception ex)
            {
                // Treated as "not found" so the fallback chain continues, but say why once.
                if (s_Warned.Add(table + "|" + locale.Identifier.Code + "|error"))
                {
                    TryllLog.Warning(
                        $"Unity Localization could not load table '{table}' for '{locale.Identifier.Code}': {ex.GetBaseException().Message}");
                }
                return null;
            }
        }

        static List<Locale> FallbackChain()
        {
            var chain = new List<Locale>();
            var selected = LocalizationSettings.SelectedLocale;
            if (selected == null) return chain;
            chain.Add(selected);
            var fallbacks = selected.GetFallbacks();
            if (fallbacks == null) return chain;
            foreach (var locale in fallbacks)
            {
                if (locale != null && !chain.Contains(locale))
                    chain.Add(locale);
            }
            return chain;
        }

        static Locale FindLocale(string code)
        {
            var available = LocalizationSettings.AvailableLocales;
            if (available == null || string.IsNullOrEmpty(code)) return null;
            foreach (var locale in available.Locales)
            {
                if (locale != null && string.Equals(locale.Identifier.Code, code, StringComparison.OrdinalIgnoreCase))
                    return locale;
            }
            return null;
        }
    }
}
#endif
