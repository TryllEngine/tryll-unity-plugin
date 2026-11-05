// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tryll.Client
{
    /// <summary>
    /// Game-owned lookup for Text IDs on a workflow. Tryll does not own a string
    /// table. Assign one on <see cref="TryllClient.TextProvider"/> or on
    /// <see cref="TryllRuntimeSettings.TextProvider"/>.
    /// </summary>
    public interface ITryllTextProvider
    {
        /// <summary>
        /// Text for <paramref name="id"/> in <paramref name="table"/>, in the
        /// provider's current language, after the provider's own fallback.
        /// Null when the ID is unknown in the current language and every fallback.
        /// </summary>
        Task<string> GetTextAsync(string table, string id);

        /// <summary>BCP-47 tag of the game's current language, for example "de-DE". Null = unset.</summary>
        string CurrentLocale { get; }

        /// <summary>Raised after the game's language changed.</summary>
        event Action LocaleChanged;
    }

    /// <summary>
    /// Optional editor surface. Built-in providers implement it so the inspector
    /// can preview a Text ID without Play mode. A provider set only from code
    /// at runtime can omit it; the inspector then shows no preview.
    /// </summary>
    public interface ITryllTextProviderEditor
    {
        /// <summary>Languages that have tables. BCP-47 tags.</summary>
        IReadOnlyList<string> ListLocales();

        /// <summary>Keys in <paramref name="table"/>. Custom providers may ignore the table.</summary>
        IReadOnlyList<string> ListIds(string table);

        /// <summary>
        /// Text for this locale only. Null when that locale has no entry.
        /// Does not apply another language's fallback — the drawer does that
        /// and marks it.
        /// </summary>
        string GetTextForPreview(string table, string id, string locale);

        /// <summary>Jump to the entry in the game's localization editor.</summary>
        void OpenInEditor(string table, string id);
    }

    /// <summary>
    /// Optional. A provider that can say why a lookup fails (for example a
    /// JSON file that does not parse). The inspector shows the reason next to
    /// "not found".
    /// </summary>
    public interface ITryllTextProviderDiagnostics
    {
        /// <summary>A problem with <paramref name="table"/> in <paramref name="locale"/>, or null.</summary>
        string Diagnose(string table, string locale);
    }

    /// <summary>
    /// Editor assemblies register the real open action. Runtime providers call
    /// <see cref="Open"/> so they do not reference <c>UnityEditor</c>.
    /// </summary>
    public static class TryllTextEditorHooks
    {
        public static Action<ITryllTextProviderEditor, string, string> Open;

        /// <summary>
        /// Goes up when text data may have changed (a table or JSON file was
        /// imported, a provider was edited). Caches compare it to drop old text.
        /// Stays 0 in a player build.
        /// </summary>
        public static int Revision { get; private set; }

        public static void BumpRevision() => Revision++;
    }

    /// <summary>Inspector preview for one Text ID. Shared by the drawer and Edit Mode tests.</summary>
    public static class TryllTextPreview
    {
        public enum Kind
        {
            Found,
            MissingHere,
            EmptyId,
            NoEditorSupport,
        }

        public sealed class Result
        {
            public Kind Kind;
            public string Text;
            public bool IsFallback;
            public string Message;
            public List<string> MissingLocales = new List<string>();
        }

        public static Result Evaluate(
            ITryllTextProvider provider, string table, string id, string previewLocale)
        {
            if (provider is not ITryllTextProviderEditor editor)
            {
                string name = provider == null ? "none" : provider.GetType().Name;
                return new Result
                {
                    Kind = Kind.NoEditorSupport,
                    Message = $"No preview — {name} has no editor support.",
                };
            }

            if (string.IsNullOrEmpty(id))
            {
                return new Result { Kind = Kind.EmptyId, Message = "Enter a Text ID" };
            }

            var locales = editor.ListLocales() ?? Array.Empty<string>();
            var missing = new List<string>();
            string fallback = null;
            foreach (var locale in locales)
            {
                string text = editor.GetTextForPreview(table, id, locale);
                if (text == null)
                    missing.Add(locale);
                else if (fallback == null && !string.Equals(locale, previewLocale, StringComparison.OrdinalIgnoreCase))
                    fallback = text;
            }

            string exact = editor.GetTextForPreview(table, id, previewLocale);
            if (exact == null)
            {
                string problem = (provider as ITryllTextProviderDiagnostics)?.Diagnose(table, previewLocale);
                string message = $"`{id}` not found in table `{table}` ({previewLocale})";
                if (!string.IsNullOrEmpty(problem))
                    message += " — " + problem;
                return new Result
                {
                    Kind = Kind.MissingHere,
                    Text = fallback,
                    IsFallback = fallback != null,
                    Message = message,
                    MissingLocales = missing,
                };
            }

            return new Result
            {
                Kind = Kind.Found,
                Text = exact,
                MissingLocales = missing,
            };
        }
    }
}
