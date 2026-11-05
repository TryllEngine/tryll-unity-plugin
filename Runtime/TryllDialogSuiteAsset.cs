// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// An ordered set of <see cref="TryllDialogScriptAsset"/>s that the Dialog Lab runs
    /// together. Drop it on the Lab's Dialog Script field instead of a single script:
    /// every variant × seed column then runs each dialogue in order, and the results group
    /// per dialogue. Each dialogue gets a fresh agent, so nothing one dialogue sets
    /// (variables, slots, summaries) leaks into the next.
    /// </summary>
    [CreateAssetMenu(menuName = "Tryll/Dialog Suite", fileName = "NewDialogSuite")]
    public sealed class TryllDialogSuiteAsset : ScriptableObject
    {
        [TextArea(1, 3)]
        public string Description = "";

        public List<TryllDialogScriptAsset> Dialogues = new List<TryllDialogScriptAsset>();
    }
}
