// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Exported editor UI contract values. Parity tests compare these with
    /// <c>tryll/schema/editor_ui.json</c>. Not a theme or widget layer.
    /// </summary>
    internal static class TryllEditorUiContract
    {
        public const float AgentListMinWidth = 160f;
        public const float InspectorMinWidth = 260f;
        public const float InspectorDefaultWidth = 400f;
        public const float ChatMinWidth = 280f;
        public const float TimelineMinWidth = 240f;
        public const float StageMinWidth = 200f;
        public const float PanelMinWidth = 760f;
        public const float PanelMinHeight = 520f;
        public const float CellWidthSmall = 212f;
        public const float CellWidthMedium = 276f;
        public const float CellWidthLarge = 360f;
        public const float FollowTailThreshold = 24f;
        public const float ModelFilterMinWidth = 250f;
        public const float ModelFilterDefaultWidth = 250f;
        public const float ModelListMinWidth = 340f;
        public const float ModelPropsMinWidth = 280f;
        public const float ModelPropsDefaultWidth = 360f;
        public const float KnowledgeStoreMinWidth = 220f;
        public const float KnowledgeStoreDefaultWidth = 260f;
        public const float KnowledgeRecordsMinWidth = 280f;
        public const float KnowledgeInspectorMinWidth = 280f;
        public const float KnowledgeInspectorDefaultWidth = 320f;

        public static readonly int[] DensityLines = { 3, 8, 0 };

        public static readonly float[] MemorySystemWeights = { 2.2f, 1.2f, 2.6f };
        public static readonly float[] MemoryDevicesWeights = { 2.6f, 0.7f, 0.9f, 0.9f, 0.9f, 0.9f };
        public static readonly float[] MemoryKvWeights = { 2.0f, 1.1f, 0.9f, 2.2f, 0.9f };
        public static readonly float[] MemoryComponentsWeights = { 1.4f, 2.2f, 1.2f, 1.4f, 1.0f };

        public const int BinaryByteBase = 1024;
        public const int DecimalByteBase = 1000;

        public const string InspectorEmptyLab =
            "Select a cell to inspect its turn.\n\n"
            + "Pin two columns (⇧-click a cell, or ⊙ in a header) to diff their rendered prompts.";
        public const string InspectorEmptyChat = "Select an assistant answer to inspect its turn.";
        public const string InspectorNoParameters = "No parameters on this turn's nodes.";
        public const string InspectorNoDiffPeer = "No diff — peer carries no diagnostics for this turn.";
        public const string ModelManagerSelect = "Select a model to view its details and controls.";
        public const string ModelLicenceNotice = "Check the model's licence before you ship it with your game.";
        public const string ModelLicenceMissing = "No licence_url in the catalog for this model. Check its licence at the source before you ship it.";
        public const string AgentLogEmpty = "No observations captured yet.";
        public const string KnowledgeSelectStore = "Select a store to inspect its records.";
        public const string KnowledgeSelectRecord = "Select a record to edit it, or write a new one below.";
        public const string KnowledgeFilterHint = "Substring match on this store's loaded records. Does not call the server.";
        public const string KnowledgeQueryHint = "Dense retrieval against the selected store. Results replace the list until you switch back to Filter.";
        public const string KnowledgeTruncated = "This store has more records than the window can show. Showing the first {count}.";

        public const string StatusCreatingAgent = "Creating agent…";
        public const string StatusCreatingVoiceInput = "Creating VoiceInput…";
        public const string StatusRefreshing = "Refreshing…";
        public const string StatusListing = "Listing…";
        public const string StatusWaiting = "Waiting…";
        public const string StatusCapturing = "Capturing…";
        public const string StatusIdle = "Idle";
        public const string StatusConnecting = "Connecting…";
        public const string StatusConfiguring = "Configuring…";
        public const string StatusReady = "Ready";
        public const string StatusReadyModels = "Ready · {count} models";
        public const string StatusError = "Error";
        public const string StatusAgentLogIdle = "Waiting for a shared Tryll connection";

        // Welcome and Help & Community windows.
        public const string WelcomeTitle = "Welcome to Tryll";
        public const string WelcomeIntro = "Run language, speech and voice models on the player's own machine. Finish the setup below once, then open the Tryll Chat window from Window > Tryll to talk to your first agent.";
        public const string WelcomeGetStarted = "Get started";
        public const string WelcomeJoinDiscord = "Join our Discord";
        public const string WelcomeSetupHeader = "Setup";
        public const string WelcomeSetupProgress = "{done} of {total} done";
        public const string WelcomeStepDone = "Done";
        public const string WelcomeStepRequired = "Required";
        public const string WelcomeStepUnavailable = "Not available";
        public const string WelcomeLicenceHeader = "Licence";
        public const string WelcomeLicenceBody = "The Tryll server is covered by the Tryll Runtime Licence. It includes telemetry, which is always on in the editor. Accept the licence to run the server.";
        public const string WelcomeLicencePlayers = "Your players are never measured. Shipped games contain no telemetry.";
        public const string WelcomeLicencePending = "This plugin has no LICENSE.md yet. Until the licence is published, the server is for evaluation only.";
        public const string WelcomeLicenceAccept = "Accept licence";
        public const string WelcomeLicenceAccepted = "You accepted the licence.";
        public const string WelcomeLicenceWithdraw = "Withdraw";
        public const string WelcomeLicenceRead = "Read the licence";
        public const string WelcomeTelemetryLink = "Telemetry";
        public const string WelcomeLicenceViewerTitle = "Tryll Runtime Licence";
        public const string TextEditorOpen = "Edit in a window";
        public const string TextEditorTitle = "Edit {field}";
        public const string TextEditorGone = "This field is gone: its object was deleted or reloaded. Open it again from the inspector.";
        public const string WelcomeServerHeader = "Server";
        public const string WelcomeServerBundled = "This plugin bundles its server. Nothing to download.";
        public const string WelcomeServerAfterSetup = "The server starts after you accept the licence.";
        public const string WelcomeServerNotFound = "No server found in this plugin. Add the Tryll server zip as the README describes.";
        public const string WelcomeServerInstalled = "Installed: Tryll server {version} ({variant}).";
        public const string WelcomeServerMissing = "Not installed. Download it once; every project on this computer shares it.";
        public const string WelcomeServerNoPin = "This plugin does not name a server build. Use a plugin from a Tryll release.";
        public const string WelcomeDownloadLite = "Download server ({size})";
        public const string WelcomeDownloadFull = "Download CUDA build ({size})";
        public const string WelcomeDownloadFullHint = "For NVIDIA GPUs. Includes the CPU and Vulkan builds.";
        public const string WelcomeInstallZip = "Install from zip…";
        public const string WelcomeCancel = "Cancel";
        public const string WelcomeDownloading = "Downloading… {percent}%";
        public const string WelcomeInstalling = "Installing…";
        public const string WelcomeNeedsLicence = "Accept the licence first.";
        public const string ServerNeedsLicence = "Accept the Tryll Runtime Licence in the Tryll Welcome window first.";
        public const string WelcomeInstallFailed = "Install failed: {reason}";
        public const string WelcomeInstallDone = "Server installed.";
        public const string WelcomeCommunityHeader = "Community";
        public const string WelcomeShowAtStartup = "Show this window at startup";
        public const string WelcomeOpenHelp = "Help & Community";
        public const string HelpTitle = "Help & Community";
        public const string HelpIntro = "Questions, bugs or ideas? Ask on Discord, read the docs, or email the team.";
        public const string HelpAboutHeader = "About this install";
        public const string HelpRowClient = "Tryll client";
        public const string HelpRowServer = "Tryll server";
        public const string HelpRowProtocol = "Protocol";
        public const string HelpRowUpdates = "Updates";
        public const string HelpServerBundled = "Bundled with the plugin";
        public const string HelpServerMissing = "Not installed";
        public const string HelpInstallServer = "Install";
        public const string HelpProtocolMismatch = "Server uses protocol {protocol}";
        public const string HelpGetHelpHeader = "Get help";
        public const string HelpReportBug = "Report a bug";
        public const string HelpOpenWelcome = "Open the Welcome window";
        public const string HelpCheckUpdates = "Check for updates";
        public const string HelpChecking = "Checking…";
        public const string HelpUpToDate = "You have the latest version.";
        public const string HelpUpdateAvailable = "Tryll {version} is available.";
        public const string HelpViewRelease = "View release";
        public const string HelpUpdateFailed = "Could not check for updates.";
        public const string HelpOpenLogs = "Open logs folder";
        public const string HelpLogsNotCreated = "This project has no Tryll logs yet. Opened {folder} instead.";
        public const string HelpUpdatesUnavailable = "Not available for this build.";
        public const string LinkDocs = "Documentation";
        public const string LinkDiscord = "Discord";
        public const string LinkWebsite = "Website";
        public const string LinkGithub = "GitHub";
        public const string LinkReportIssue = "Report an issue";
        public const string LinkYoutube = "YouTube";
        public const string LinkX = "X";
        public const string LinkLinkedin = "LinkedIn";
        public const string LinkInstagram = "Instagram";
        public const string LinkTiktok = "TikTok";
        public const string LinkEmail = "Email us";

        public static string FormatReadyModels(int count) =>
            StatusReadyModels.Replace("{count}", count.ToString());

        public static string FormatKnowledgeTruncated(int count) =>
            KnowledgeTruncated.Replace("{count}", count.ToString());

        public static readonly string[] Roles =
        {
            "ok", "warn", "error", "accent", "diff_added", "diff_removed", "loaded",
        };

        public static readonly string[] Components =
        {
            "bubble", "chip", "heading", "section", "placeholder", "banner",
            "timing_segment", "code_block",
        };

        public static readonly string[] IconIds =
        {
            "warning", "download", "capture_gap", "voice_input", "expand", "collapse",
        };

        public const string IconWarning = "⚠";
        public const string IconDownload = "⤓";
        public const string IconCaptureGap = "·";
        public const string IconVoiceInput = "🎤";
        public const string IconExpand = "▶";
        public const string IconCollapse = "▼";

        public const string IconWarningUnreal = "Icons.Warning";
        public const string IconDownloadUnreal = "Icons.Download";
        public const string IconCaptureGapUnreal = "·";
        public const string IconVoiceInputUnreal = "ClassIcon.AudioComponent";
        public const string IconExpandUnreal = "▶";
        public const string IconCollapseUnreal = "▼";

        public static readonly (string Id, string Unity, string Unreal)[] IconBindings =
        {
            (IconIds[0], IconWarning, IconWarningUnreal),
            (IconIds[1], IconDownload, IconDownloadUnreal),
            (IconIds[2], IconCaptureGap, IconCaptureGapUnreal),
            (IconIds[3], IconVoiceInput, IconVoiceInputUnreal),
            (IconIds[4], IconExpand, IconExpandUnreal),
            (IconIds[5], IconCollapse, IconCollapseUnreal),
        };

        public static readonly string[] Nodes =
        {
            "RetrieveNode", "TransformNode", "GenerateNode", "CannedResponseNode",
            "BranchNode", "GenerateAndSpeakNode", "ToolCallNode", "RegexGuardrailNode",
            "ClassifyIntentNode", "ClassifyIntentLLMNode", "ClassifyAttributesLLMNode", "IntentToInstructionNode",
            "InstructionNode", "SpeakNode", "PauseNode",
        };
    }
}
