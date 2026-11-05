// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using Tryll.Client;
using UnityEditor;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Persisted Test Chat window state. EditorPrefs, keyed per Unity project so two
    /// projects on the same machine do not share an STT name or inspector layout.
    /// </summary>
    internal sealed class TryllChatConfig
    {
        public bool      EnableVoice;
        public TryllChatVoiceSource VoiceSource;
        /// <summary>False when this project has never saved the selector (pre-migration prefs).</summary>
        public bool      HasVoiceSource;
        public string    SttModelName    = "";
        public VoiceMode VoiceMode       = VoiceMode.PushToTalk;
        public float     InspectorWidth  = TryllEditorUiContract.InspectorDefaultWidth;
        public bool      InspectorVisible = true;
        public string    MicDeviceName   = "";

        const string KeyEnableVoice      = "EnableVoice";
        const string KeyVoiceSource      = "VoiceSource";
        const string KeySttModelName     = "SttModelName";
        const string KeyVoiceMode        = "VoiceMode";
        const string KeyInspectorWidth   = "InspectorWidth";
        const string KeyInspectorVisible = "InspectorVisible";
        const string KeyMicDeviceName    = "MicDeviceName";

        static string Prefix() => TryllEditorPrefs.ProjectPrefix("Chat");

        public static bool Exists()
            => EditorPrefs.HasKey(Prefix() + KeyEnableVoice)
            || EditorPrefs.HasKey(Prefix() + KeyVoiceSource);

        public static TryllChatConfig Load()
        {
            string dest = Prefix();
            string source = TryllEditorPrefs.SourcePrefixIfEmpty(
                dest, TryllEditorPrefs.LegacyHashPrefix("Chat"), KeyEnableVoice);
            var cfg = Read(source);
            if (source != dest)
                cfg.Save();
            return cfg;
        }

        static TryllChatConfig Read(string p)
        {
            return new TryllChatConfig
            {
                EnableVoice      = EditorPrefs.GetBool(p + KeyEnableVoice, false),
                HasVoiceSource   = EditorPrefs.HasKey(p + KeyVoiceSource),
                VoiceSource      = TryllChatVoiceState.Clamp(
                    EditorPrefs.GetInt(p + KeyVoiceSource, (int)TryllChatVoiceSource.Off)),
                SttModelName     = EditorPrefs.GetString(p + KeySttModelName, ""),
                VoiceMode        = (VoiceMode)EditorPrefs.GetInt(p + KeyVoiceMode, (int)VoiceMode.PushToTalk),
                InspectorWidth   = EditorPrefs.GetFloat(p + KeyInspectorWidth, TryllEditorUiContract.InspectorDefaultWidth),
                InspectorVisible = EditorPrefs.GetBool(p + KeyInspectorVisible, true),
                MicDeviceName    = EditorPrefs.GetString(p + KeyMicDeviceName, ""),
            };
        }

        public void Save()
        {
            string p = Prefix();
            EditorPrefs.SetBool(p + KeyEnableVoice, EnableVoice);
            if (HasVoiceSource)
                EditorPrefs.SetInt(p + KeyVoiceSource, (int)VoiceSource);
            EditorPrefs.SetString(p + KeySttModelName, SttModelName ?? "");
            EditorPrefs.SetInt(p + KeyVoiceMode, (int)VoiceMode);
            EditorPrefs.SetFloat(p + KeyInspectorWidth, InspectorWidth);
            EditorPrefs.SetBool(p + KeyInspectorVisible, InspectorVisible);
            EditorPrefs.SetString(p + KeyMicDeviceName, MicDeviceName ?? "");
        }
    }
}
