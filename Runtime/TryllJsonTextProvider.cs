// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// One JSON <see cref="TextAsset"/> per language, each a flat object
    /// <c>{ "id": "text" }</c>. For demos and prototypes that have no localization
    /// package. The workflow's table name is ignored; the file is the table.
    /// </summary>
    /// <remarks>
    /// Lookup order: the current locale, then a language-only match
    /// (<c>de-DE</c> → <c>de</c>), then the first other file that has the key.
    /// The last step uses another language, so it logs a warning once per key.
    /// A key missing from every file returns null. A file that is not valid
    /// JSON logs an error and counts as empty. Smart-string formatting is not a
    /// concept here — the file text is sent as authored.
    /// </remarks>
    [CreateAssetMenu(menuName = "Tryll/JSON Text Provider", fileName = "TryllJsonTextProvider")]
    public sealed class TryllJsonTextProvider : TryllTextProviderAsset, ITryllTextProviderEditor, ITryllTextProviderDiagnostics
    {
        [Serializable]
        public sealed class LocaleFile
        {
            [Tooltip("BCP-47 tag, for example en or de-DE.")]
            public string Locale = "en";

            public TextAsset File;
        }

        [Tooltip("Language at startup. SetLocale changes the language at runtime " +
                 "without changing this asset.")]
        public string Locale = "en";

        public List<LocaleFile> Languages = new List<LocaleFile>();

        sealed class Parsed
        {
            public int Revision;
            public Dictionary<string, string> Map;
            public string Error;
        }

        // Runtime language from SetLocale. Not serialized, so Play mode does
        // not write it into the asset.
        [NonSerialized] string _runtimeLocale;
        [NonSerialized] bool _hasRuntimeLocale;
        [NonSerialized] readonly Dictionary<TextAsset, Parsed> _parsed = new Dictionary<TextAsset, Parsed>();
        [NonSerialized] readonly HashSet<string> _warned = new HashSet<string>();

        string Active => _hasRuntimeLocale ? _runtimeLocale : Locale;

        public override string CurrentLocale => string.IsNullOrEmpty(Active) ? null : Active;

        /// <summary>
        /// Switch language and notify Tryll so live prompts are re-resolved.
        /// The asset's <see cref="Locale"/> field does not change.
        /// </summary>
        public void SetLocale(string locale)
        {
            _runtimeLocale = locale ?? "";
            _hasRuntimeLocale = true;
            RaiseLocaleChanged();
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
        }

#if UNITY_EDITOR
        void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.ExitingEditMode
                || change == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                _hasRuntimeLocale = false;
                _runtimeLocale = null;
            }
        }

        void OnValidate()
        {
            _parsed.Clear();
            TryllTextEditorHooks.BumpRevision();
        }
#endif

        public override Task<string> GetTextAsync(string table, string id)
        {
            string locale = Active;
            string text = Lookup(locale, id, out bool otherLanguage);
            if (text != null && otherLanguage && _warned.Add(locale + "|" + id))
            {
                TryllLog.Warning(
                    $"Text ID '{id}' has no '{locale}' entry; using another language's text.");
            }
            return Task.FromResult(text);
        }

        public IReadOnlyList<string> ListLocales()
        {
            var list = new List<string>();
            if (Languages == null) return list;
            foreach (var entry in Languages)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Locale))
                    list.Add(entry.Locale);
            }
            return list;
        }

        public IReadOnlyList<string> ListIds(string table)
        {
            var ids = new List<string>();
            var seen = new HashSet<string>();
            if (Languages == null) return ids;
            foreach (var entry in Languages)
            {
                if (!TryMap(entry, out var map)) continue;
                foreach (var key in map.Keys)
                {
                    if (seen.Add(key)) ids.Add(key);
                }
            }
            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        public string GetTextForPreview(string table, string id, string locale)
        {
            if (!TryMap(Find(locale, prefix: false), out var map)) return null;
            return map.TryGetValue(id ?? "", out var text) ? text : null;
        }

        public void OpenInEditor(string table, string id)
            => TryllTextEditorHooks.Open?.Invoke(this, table, id);

        public string Diagnose(string table, string locale)
        {
            var entry = Find(locale, prefix: true);
            if (entry == null)
                return $"no JSON file is set for '{locale}'";
            if (entry.File == null)
                return $"the '{entry.Locale}' entry has no file";
            var parsed = Parse(entry.File);
            if (parsed.Error != null)
                return $"'{entry.File.name}' is not valid JSON: {parsed.Error}";
            return null;
        }

        /// <summary>The TextAsset that holds <paramref name="id"/> for the editor ping.</summary>
        public TextAsset AssetFor(string id)
        {
            if (Languages == null) return null;
            foreach (var entry in Languages)
            {
                if (!TryMap(entry, out var map)) continue;
                if (map.ContainsKey(id ?? "")) return entry.File;
            }
            return Languages.Count > 0 ? Languages[0].File : null;
        }

        string Lookup(string locale, string id, out bool otherLanguage)
        {
            otherLanguage = false;
            if (string.IsNullOrEmpty(id)) return null;

            if (TryMap(Find(locale, prefix: false), out var exact) && exact.TryGetValue(id, out var text))
                return text;

            string language = LanguageOf(locale);
            if (!string.IsNullOrEmpty(language)
                && !string.Equals(language, locale, StringComparison.OrdinalIgnoreCase)
                && TryMap(Find(language, prefix: false), out var byLanguage)
                && byLanguage.TryGetValue(id, out text))
            {
                return text;
            }

            if (Languages == null) return null;
            foreach (var entry in Languages)
            {
                if (!TryMap(entry, out var map)) continue;
                if (map.TryGetValue(id, out text))
                {
                    otherLanguage = true;
                    return text;
                }
            }
            return null;
        }

        LocaleFile Find(string locale, bool prefix)
        {
            if (Languages == null || string.IsNullOrEmpty(locale)) return null;
            foreach (var entry in Languages)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Locale)) continue;
                if (string.Equals(entry.Locale, locale, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }
            if (!prefix) return null;
            string language = LanguageOf(locale);
            foreach (var entry in Languages)
            {
                if (entry != null && string.Equals(entry.Locale, language, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }
            return null;
        }

        bool TryMap(LocaleFile entry, out Dictionary<string, string> map)
        {
            map = null;
            if (entry?.File == null) return false;
            var parsed = Parse(entry.File);
            map = parsed.Map;
            return parsed.Error == null;
        }

        // Parses each file once. In the editor a re-import bumps the revision.
        Parsed Parse(TextAsset file)
        {
            int revision = TryllTextEditorHooks.Revision;
            if (_parsed.TryGetValue(file, out var cached) && cached.Revision == revision)
                return cached;

            var parsed = new Parsed { Revision = revision };
            if (TryllFlatJson.TryParseObject(file.text, out var map, out var error))
                parsed.Map = map;
            else
            {
                parsed.Map = new Dictionary<string, string>();
                parsed.Error = error ?? "parse error";
                TryllLog.Error($"Text file '{file.name}' is not valid JSON: {parsed.Error}");
            }
            _parsed[file] = parsed;
            return parsed;
        }

        static string LanguageOf(string locale)
        {
            if (string.IsNullOrEmpty(locale)) return "";
            int dash = locale.IndexOf('-');
            return dash < 0 ? locale : locale.Substring(0, dash);
        }
    }
}
