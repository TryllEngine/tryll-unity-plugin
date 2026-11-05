// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// ScriptableObject text provider. The same asset serves the editor preview
    /// and the game. Assign it on <see cref="TryllRuntimeSettings.TextProvider"/>.
    /// </summary>
    public abstract class TryllTextProviderAsset : ScriptableObject, ITryllTextProvider
    {
        public abstract Task<string> GetTextAsync(string table, string id);

        public abstract string CurrentLocale { get; }

        public event Action LocaleChanged;

        protected void RaiseLocaleChanged() => LocaleChanged?.Invoke();
    }
}
