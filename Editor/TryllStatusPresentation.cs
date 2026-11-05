// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client.Editor
{
    internal enum TryllOwnerPhase
    {
        Idle,
        Connecting,
        Configuring,
        Ready,
        Error,
    }

    internal enum TryllPanelOperation
    {
        None,
        CreatingAgent,
        CreatingVoiceInput,
        Refreshing,
        Listing,
        Waiting,
        Capturing,
    }

    internal enum TryllEditorPanel
    {
        Chat,
        ModelManager,
        Memory,
        AgentLog,
        DialogLab,
        Knowledge,
    }

    internal enum TryllStatusSeverity
    {
        Neutral,
        Progress,
        Success,
        Warning,
        Error,
    }

    internal enum TryllStatusActionIntent
    {
        None,
        Retry,
        Start,
        Stop,
    }

    /// <summary>
    /// Pure owner-phase + panel-operation status projection. Phase 8 wires this to
    /// real window presenters; Phase 1 only characterizes the concurrent captions.
    /// </summary>
    internal readonly struct TryllStatusPresentation
    {
        public readonly TryllStatusSeverity Severity;
        public readonly string Caption;
        public readonly string Detail;
        public readonly TryllStatusActionIntent Action;

        public TryllStatusPresentation(
            TryllStatusSeverity severity,
            string caption,
            string detail = null,
            TryllStatusActionIntent action = TryllStatusActionIntent.None)
        {
            Severity = severity;
            Caption = caption;
            Detail = detail;
            Action = action;
        }

        public static TryllStatusPresentation Compose(
            TryllOwnerPhase ownerPhase,
            string ownerDetail,
            TryllPanelOperation localOp,
            TryllEditorPanel panel,
            int modelCount = 0)
        {
            // Attach-only Agent Log never starts a session; Idle is a wait, not a start offer.
            if (panel == TryllEditorPanel.AgentLog && ownerPhase == TryllOwnerPhase.Idle)
            {
                return new TryllStatusPresentation(
                    TryllStatusSeverity.Neutral,
                    TryllEditorUiContract.StatusAgentLogIdle);
            }

            if (ownerPhase == TryllOwnerPhase.Error)
            {
                return new TryllStatusPresentation(
                    TryllStatusSeverity.Error,
                    string.IsNullOrEmpty(ownerDetail) ? TryllEditorUiContract.StatusError : ownerDetail,
                    ownerDetail,
                    TryllStatusActionIntent.Retry);
            }

            if (localOp != TryllPanelOperation.None)
            {
                return localOp switch
                {
                    TryllPanelOperation.CreatingAgent =>
                        new TryllStatusPresentation(
                            TryllStatusSeverity.Progress, TryllEditorUiContract.StatusCreatingAgent),
                    TryllPanelOperation.CreatingVoiceInput =>
                        new TryllStatusPresentation(
                            TryllStatusSeverity.Progress, TryllEditorUiContract.StatusCreatingVoiceInput),
                    TryllPanelOperation.Refreshing =>
                        new TryllStatusPresentation(
                            TryllStatusSeverity.Progress, TryllEditorUiContract.StatusRefreshing),
                    TryllPanelOperation.Listing =>
                        new TryllStatusPresentation(
                            TryllStatusSeverity.Progress, TryllEditorUiContract.StatusListing),
                    TryllPanelOperation.Waiting =>
                        new TryllStatusPresentation(
                            TryllStatusSeverity.Neutral, TryllEditorUiContract.StatusWaiting),
                    TryllPanelOperation.Capturing =>
                        new TryllStatusPresentation(
                            TryllStatusSeverity.Progress, TryllEditorUiContract.StatusCapturing),
                    _ => new TryllStatusPresentation(
                        TryllStatusSeverity.Neutral, TryllEditorUiContract.StatusIdle),
                };
            }

            return ownerPhase switch
            {
                TryllOwnerPhase.Connecting =>
                    new TryllStatusPresentation(
                        TryllStatusSeverity.Progress,
                        string.IsNullOrEmpty(ownerDetail)
                            ? TryllEditorUiContract.StatusConnecting
                            : ownerDetail),
                TryllOwnerPhase.Configuring =>
                    new TryllStatusPresentation(
                        TryllStatusSeverity.Progress,
                        string.IsNullOrEmpty(ownerDetail)
                            ? TryllEditorUiContract.StatusConfiguring
                            : ownerDetail),
                TryllOwnerPhase.Ready =>
                    new TryllStatusPresentation(
                        TryllStatusSeverity.Success,
                        panel == TryllEditorPanel.ModelManager
                            ? TryllEditorUiContract.FormatReadyModels(modelCount)
                            : TryllEditorUiContract.StatusReady),
                _ => new TryllStatusPresentation(
                    TryllStatusSeverity.Neutral, TryllEditorUiContract.StatusIdle),
            };
        }
    }

    /// <summary>
    /// Shared Unity owner-phase mapping for attach-only windows that do not own
    /// a <see cref="TryllEditorSession"/>.
    /// </summary>
    internal static class TryllEditorOwnerPhase
    {
        public static (TryllOwnerPhase Phase, string Detail) FromClient(
            Tryll.Client.TryllClient client, string lastError)
        {
            if (client == null)
                return FromState(false, false, false, false, lastError);
            return FromState(
                true,
                client.IsSessionReady,
                client.IsConnected,
                client.HasConnectRetriesRemaining,
                lastError);
        }

        public static (TryllOwnerPhase Phase, string Detail) FromState(
            bool hasClient,
            bool sessionReady,
            bool connected,
            bool retriesRemaining,
            string lastError)
        {
            if (!hasClient)
                return (TryllOwnerPhase.Idle, null);
            if (sessionReady)
                return (TryllOwnerPhase.Ready, null);
            if (connected)
                return (TryllOwnerPhase.Configuring, null);
            if (retriesRemaining)
                return (TryllOwnerPhase.Connecting, null);
            if (!string.IsNullOrEmpty(lastError))
                return (TryllOwnerPhase.Error, lastError);
            return (TryllOwnerPhase.Idle, null);
        }
    }
}
