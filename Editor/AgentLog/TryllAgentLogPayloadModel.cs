// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using Tryll.Client.Internal;

namespace Tryll.Client.Editor.AgentLog
{
    /// <summary>
    /// Pretty-prints one journaled frame the same way the export writer does.
    /// </summary>
    internal static class TryllAgentLogPayloadModel
    {
        public static string WriteJson(TryllObservationEnvelope env, bool indent)
        {
            if (env?.Frame == null || env.Frame.Length == 0)
                return "";
            var writer = new TryllJsonWriter(indent);
            if (env.IsLocal)
                TryllAgentLogJson.WriteLocal(writer, env.Frame, env.Omitted);
            else
                TryllAgentLogJson.WriteMessage(writer, env.Frame, env.Omitted);
            return writer.ToString();
        }
    }
}
