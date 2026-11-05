// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Builds the transient Dialog Script used by Compare-in-Lab: captured chat
    /// history as seeded steps, plus one blank generated continuation step.
    /// </summary>
    internal static class TryllDialogLabCompareFromChat
    {
        public static TryllDialogScriptData BuildScriptData(IReadOnlyList<(bool IsUser, string Text)> history)
        {
            var script = new TryllDialogScriptData
            {
                Name = "Captured from Chat",
                Description = "Captured via 'Compare in Lab' from Tryll Chat.",
            };
            int pairs = 0;
            if (history != null)
            {
                for (int i = 0; i + 1 < history.Count; i += 2)
                {
                    if (!history[i].IsUser || history[i + 1].IsUser) break;
                    script.Steps.Add(new TryllDialogStepData
                    {
                        User = history[i].Text ?? "",
                        Assistant = history[i + 1].Text ?? "",
                    });
                    pairs++;
                }
            }
            script.SeedTurnCount = pairs;
            script.Steps.Add(new TryllDialogStepData { User = "", Assistant = "" });
            return script;
        }

        public static void ApplyToAsset(TryllDialogScriptAsset asset, IReadOnlyList<(bool IsUser, string Text)> history)
        {
            var data = BuildScriptData(history);
            asset.name = data.Name;
            asset.Description = data.Description;
            asset.SeedTurnCount = data.SeedTurnCount;
            asset.GlobalChecks = TryllDialogScriptData.CloneChecks(data.GlobalChecks);
            asset.Steps.Clear();
            foreach (var s in data.Steps)
            {
                asset.Steps.Add(new TryllDialogScriptAsset.Step
                {
                    User = s.User,
                    Assistant = s.Assistant,
                });
            }
        }
    }
}
