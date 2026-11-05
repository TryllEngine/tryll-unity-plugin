// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;

namespace Tryll.Client
{
    /// <summary>
    /// Works out which languages your game can actually offer, from the models
    /// it actually uses.
    ///
    /// The rule is an intersection: a language is offerable only if *every*
    /// model in the pipeline covers it. A game with a 31-language voice but an
    /// English-only recognizer can offer exactly one language for hands-free
    /// play — and the point of this helper is that you find that out from the
    /// catalog instead of from a bug report.
    ///
    /// <example>
    /// <code>
    /// var (models, err) = await TryllClient.Instance.RequestListModelsAsync();
    /// var codes = TryllLanguages.Offerable(models, "Supertonic 3 (int8)",
    ///                                              "Whisper Base Multilingual (int8)");
    /// foreach (var c in codes)
    ///     dropdown.options.Add(new Dropdown.OptionData(TryllLanguages.DisplayName(c)));
    /// </code>
    /// </example>
    /// </summary>
    public static class TryllLanguages
    {
        /// <summary>
        /// Languages every one of <paramref name="models"/> declares, sorted.
        ///
        /// A model that declares nothing is **skipped, not treated as
        /// restrictive** — an empty list means "the catalog didn't say", and
        /// letting unknown shrink the result would hide languages that work.
        ///
        /// An empty return therefore means one of two things: the declared sets
        /// genuinely do not overlap, or nothing declared anything. Use
        /// <see cref="HasLanguageInfo"/> to tell those apart before showing the
        /// user "no languages available".
        /// </summary>
        public static IReadOnlyList<string> Offerable(IEnumerable<TryllModelInfo> models)
        {
            if (models == null) return Array.Empty<string>();

            HashSet<string> intersection = null;
            foreach (var m in models)
            {
                if (m.Languages == null || m.Languages.Length == 0)
                    continue;   // undeclared = no information, not "supports nothing"

                var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var code in m.Languages)
                    if (!string.IsNullOrEmpty(code))
                        declared.Add(code);

                if (intersection == null) intersection = declared;
                else                      intersection.IntersectWith(declared);
            }

            if (intersection == null || intersection.Count == 0)
                return Array.Empty<string>();

            var result = new List<string>(intersection);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        /// <summary>
        /// Same, but picks the models out of a <c>ListModels</c> result by name —
        /// the usual shape, since you know your TTS/STT/language model names.
        /// Names that are not present are ignored (a model you have not
        /// downloaded yet should not silently empty the menu).
        /// </summary>
        public static IReadOnlyList<string> Offerable(
            IEnumerable<TryllModelInfo> allModels, params string[] modelNames)
        {
            if (allModels == null || modelNames == null || modelNames.Length == 0)
                return Array.Empty<string>();

            var wanted = new HashSet<string>(modelNames, StringComparer.Ordinal);
            var picked = new List<TryllModelInfo>();
            foreach (var m in allModels)
                if (wanted.Contains(m.Name))
                    picked.Add(m);

            return Offerable(picked);
        }

        /// <summary>
        /// True when at least one model declared its languages. Distinguishes
        /// "these models share no language" from "we have no idea", which are
        /// very different things to show a player.
        /// </summary>
        public static bool HasLanguageInfo(IEnumerable<TryllModelInfo> models)
        {
            if (models == null) return false;
            foreach (var m in models)
                if (m.Languages != null && m.Languages.Length > 0)
                    return true;
            return false;
        }

        /// <summary>
        /// Whether <paramref name="model"/> covers <paramref name="locale"/>
        /// (a tag like <c>"de-DE"</c> or a bare code like <c>"de"</c>).
        ///
        /// Returns <c>true</c> for a model that declares nothing: without a
        /// catalog claim we have no evidence either way, and the server applies
        /// the same rule — it simply withholds the language hint rather than
        /// refusing to load the model.
        /// </summary>
        public static bool Supports(TryllModelInfo model, string locale)
        {
            if (model.Languages == null || model.Languages.Length == 0)
                return true;

            var code = LanguageOf(locale);
            if (string.IsNullOrEmpty(code)) return false;

            foreach (var declared in model.Languages)
                if (string.Equals(declared, code, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>
        /// Primary language subtag of a locale, lowercase: <c>"pt-BR"</c> →
        /// <c>"pt"</c>. This is the granularity models work at — none of the
        /// shipped voice or speech models distinguish pt-BR from pt-PT.
        /// </summary>
        public static string LanguageOf(string locale)
        {
            if (string.IsNullOrEmpty(locale)) return string.Empty;

            var cut = locale.Length;
            for (int i = 0; i < locale.Length; i++)
            {
                if (locale[i] == '-' || locale[i] == '_') { cut = i; break; }
            }
            return locale.Substring(0, cut).ToLowerInvariant();
        }

        /// <summary>
        /// English name for a language code — <c>"de"</c> → <c>"German"</c> —
        /// for labelling a menu. Unknown codes come back unchanged, so a menu
        /// entry is never blank.
        ///
        /// Deliberately a fixed table rather than <c>CultureInfo</c>: it must
        /// match what the server renders into the <c>{{language}}</c> prompt key
        /// exactly (see SessionLocale.cpp), and IL2CPP builds can strip culture
        /// data. Keep the two lists in sync when adding a language.
        /// </summary>
        public static string DisplayName(string code)
        {
            var key = LanguageOf(code);
            return Names.TryGetValue(key, out var name) ? name : (code ?? string.Empty);
        }

        /// <summary>Mirrors kLanguageNames in tryll/server/workflow/src/SessionLocale.cpp.</summary>
        private static readonly Dictionary<string, string> Names =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ar", "Arabic"     }, { "bg", "Bulgarian" }, { "bn", "Bengali"    },
            { "ca", "Catalan"    }, { "cs", "Czech"     }, { "da", "Danish"     },
            { "de", "German"     }, { "el", "Greek"     }, { "en", "English"    },
            { "es", "Spanish"    }, { "et", "Estonian"  }, { "fa", "Persian"    },
            { "fi", "Finnish"    }, { "fr", "French"    }, { "he", "Hebrew"     },
            { "hi", "Hindi"      }, { "hr", "Croatian"  }, { "hu", "Hungarian"  },
            { "id", "Indonesian" }, { "it", "Italian"   }, { "ja", "Japanese"   },
            { "ko", "Korean"     }, { "lt", "Lithuanian"}, { "lv", "Latvian"    },
            { "ms", "Malay"      }, { "nl", "Dutch"     }, { "no", "Norwegian"  },
            { "pl", "Polish"     }, { "pt", "Portuguese"}, { "ro", "Romanian"   },
            { "ru", "Russian"    }, { "sk", "Slovak"    }, { "sl", "Slovenian"  },
            { "sr", "Serbian"    }, { "sv", "Swedish"   }, { "ta", "Tamil"      },
            { "th", "Thai"       }, { "tr", "Turkish"   }, { "uk", "Ukrainian"  },
            { "ur", "Urdu"       }, { "vi", "Vietnamese"}, { "yue", "Cantonese" },
            { "zh", "Chinese"    }, { "zu", "Zulu"      },
        };
    }
}
