// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>Where Chat gets speech-to-text. Persisted as an int.</summary>
    internal enum TryllChatVoiceSource
    {
        Off = 0,
        Chat = 1,
        Component = 2,
    }

    /// <summary>
    /// Transient Chat voice-UI flags. Hardware capture (_micClip / pump) stays on
    /// the window; this type is the part tests can drive without a microphone.
    ///
    /// <see cref="InlineCapturing"/> is only the editor-owned mic path. An assigned
    /// <c>TryllVoiceInputComponent</c> reports activity through
    /// <c>Voice.IsUtteranceActive</c> instead.
    /// </summary>
    internal struct TryllChatVoiceUi
    {
        public bool   PttHeld;
        public bool   HandsFreeActive;
        public bool   AwaitingUtteranceFinal;
        public bool   VoiceBeginInFlight;
        public bool   InlineCapturing;
        public string Partial;
    }

    /// <summary>
    /// Pure Chat voice lifecycle helpers. Extracted so press/release/error/final
    /// sequences can be asserted without an EditorWindow or microphone.
    /// </summary>
    internal static class TryllChatVoiceState
    {
        public const string ComponentNeedsPlayMode =
            "Voice Component captures the microphone in Play Mode only. " +
            "Enter Play Mode, or switch Voice Input to Chat to use editor-owned STT.";

        public const string MissingComponent =
            "No TryllVoiceInputComponent on {0} — add one or drag one here";

        /// <summary>
        /// Old checkbox plus an optional dragged component. A saved component
        /// assignment becomes the override under Actor component.
        /// </summary>
        public static TryllChatVoiceSource Clamp(int saved)
        {
            if (saved < (int)TryllChatVoiceSource.Off || saved > (int)TryllChatVoiceSource.Component)
                return TryllChatVoiceSource.Off;
            return (TryllChatVoiceSource)saved;
        }

        public static TryllChatVoiceSource FromLegacy(bool enableVoice, bool hadComponentRef)
        {
            if (!enableVoice) return TryllChatVoiceSource.Off;
            return hadComponentRef ? TryllChatVoiceSource.Component : TryllChatVoiceSource.Chat;
        }

        public static TryllVoiceInputComponent ResolveComponent(
            TryllVoiceInputComponent componentOverride,
            TryllVoiceInputComponent sibling)
            => componentOverride != null ? componentOverride : sibling;

        public static bool CanUseAssignedComponent(bool playMode) => playMode;

        public static bool IsRecording(in TryllChatVoiceUi ui, bool componentUtteranceActive) =>
            ui.InlineCapturing
            || ui.AwaitingUtteranceFinal
            || ui.VoiceBeginInFlight
            || componentUtteranceActive;

        public static bool IsMicHighlighted(in TryllChatVoiceUi ui, bool componentUtteranceActive) =>
            ui.InlineCapturing
            || ui.HandsFreeActive
            || ui.PttHeld
            || componentUtteranceActive;

        public static bool CanBeginUtterance(in TryllChatVoiceUi ui, bool utteranceActive) =>
            !utteranceActive && !ui.AwaitingUtteranceFinal && !ui.VoiceBeginInFlight;

        public static bool VoiceControlsReady(
            bool enableVoice,
            bool ready,
            bool hasVoice,
            bool turnBusy,
            bool usingComponent,
            bool playMode)
        {
            if (!enableVoice || !ready || turnBusy || !hasVoice) return false;
            return !usingComponent || playMode;
        }

        public static string MicTooltip(
            bool enableVoice,
            bool usingComponent,
            bool playMode,
            bool voiceReady,
            VoiceMode mode,
            bool handsFreeActive)
        {
            if (!enableVoice) return "";
            if (usingComponent && !playMode) return ComponentNeedsPlayMode;
            if (!voiceReady) return "Voice not ready";
            if (mode == VoiceMode.PushToTalk) return "Push-to-talk: hold to record (Ctrl+D)";
            return handsFreeActive ? "Hands-free: click to stop" : "Hands-free: click to start";
        }

        public static void OnPttPress(ref TryllChatVoiceUi ui)
        {
            ui.PttHeld = true;
            ui.Partial = "";
        }

        public static void OnComponentBegin(ref TryllChatVoiceUi ui)
        {
            ui.Partial = "";
        }

        public static void OnInlineBeginQueued(ref TryllChatVoiceUi ui)
        {
            ui.Partial = "";
            ui.VoiceBeginInFlight = true;
        }

        public static void OnBeginFailed(ref TryllChatVoiceUi ui)
        {
            ui.VoiceBeginInFlight = false;
            ui.PttHeld = false;
            ui.AwaitingUtteranceFinal = false;
            ui.InlineCapturing = false;
            ui.Partial = "";
        }

        public static void OnInlineBeginAcked(ref TryllChatVoiceUi ui)
        {
            ui.VoiceBeginInFlight = false;
        }

        public static void OnEndCommitted(ref TryllChatVoiceUi ui)
        {
            ui.PttHeld = false;
            ui.AwaitingUtteranceFinal = true;
        }

        public static void OnEndFailed(ref TryllChatVoiceUi ui)
        {
            ui.PttHeld = false;
            ui.AwaitingUtteranceFinal = false;
            ui.VoiceBeginInFlight = false;
            ui.Partial = "";
        }

        public static void OnCancel(ref TryllChatVoiceUi ui)
        {
            ui.PttHeld = false;
            ui.AwaitingUtteranceFinal = false;
            ui.VoiceBeginInFlight = false;
            ui.InlineCapturing = false;
            ui.Partial = "";
        }

        public static void OnStop(ref TryllChatVoiceUi ui)
        {
            OnCancel(ref ui);
            ui.HandsFreeActive = false;
        }

        public static void OnUtteranceFinal(ref TryllChatVoiceUi ui)
        {
            ui.AwaitingUtteranceFinal = false;
            ui.PttHeld = false;
            ui.Partial = "";
        }

        public static void OnVoiceError(ref TryllChatVoiceUi ui) => OnStop(ref ui);
    }
}
