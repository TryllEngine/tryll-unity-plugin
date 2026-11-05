// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client
{
    internal static class TryllDocs
    {
        // Pinned to the repo VERSION's /vX.Y/ docs folder by codegen (TryllDocsBase.gen.cs).
        public const string BaseUrl = TryllDocsBase.Url;

        public const string TryllClient              = BaseUrl + "reference/client-api/unity/tryll-client.html";
        public const string TryllAgentComponent      = BaseUrl + "reference/client-api/unity/tryll-agent-component.html";
        public const string TryllVoiceInputComponent = BaseUrl + "reference/client-api/unity/tryll-voice-input-component.html";
        public const string TryllWorkflowAsset       = BaseUrl + "reference/client-api/unity/tryll-workflow-asset.html";
        public const string TryllVoiceProfile        = BaseUrl + "reference/client-api/unity/tryll-voice-profile.html";
        public const string TryllRuntimeSettings     = BaseUrl + "reference/client-api/unity/tryll-runtime-settings.html";
        public const string UseVoiceInput            = BaseUrl + "how-to/use-voice-input.html";
        public const string GettingStartedUnity      = BaseUrl + "getting-started/first-inference-unity.html";
        public const string Telemetry                = BaseUrl + "reference/telemetry.html";
        public const string ReportABug               = BaseUrl + "how-to/report-a-bug.html";
        public const string Licence                  = BaseUrl + "reference/legal/licence.html";
    }
}
