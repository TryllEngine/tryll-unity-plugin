// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Tryll.Client;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// feat-011 editor guard. Before Play starts, and (via <see cref="EnsureRegisteredModelsReady"/>)
    /// before the Tryll Chat window creates its agent, verify every Production/Experimental
    /// registered model (all kinds — language/embedding/stt/tts/vad) is downloaded, blocking with
    /// a cancelable progress bar to download anything <see cref="TryllModelStatus.Absent"/> using
    /// the existing per-model download API. No new wire messages.
    ///
    /// Editor-only: lives under Editor/ and is never compiled into a packaged Player build.
    /// </summary>
    [InitializeOnLoad]
    public static class TryllPlayModeModelGuard
    {
        // Cached for the life of the domain (reset on script reload / recompile) so a developer
        // isn't forced through a cancel+relaunch cycle on every single Play press — only when
        // something is actually missing does Play get interrupted at all.
        private static bool _verifiedReady;
        private static bool _resumePending;

        static TryllPlayModeModelGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>Call after a manifest edit or model deletion so the next Play re-checks.</summary>
        public static void Invalidate() => _verifiedReady = false;

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;

            // This is our own isPlaying = true from RunGuardThenResume — let it through untouched.
            if (_resumePending) { _resumePending = false; return; }

            // Project Settings → Tryll Client → Play Mode: when the guard is disabled, enter
            // Play immediately with no pre-flight model check.
            if (!TryllRuntimeSettings.Load().EnablePlayModeModelGuard) return;

            if (_verifiedReady) return;

            // EditorApplication.playModeStateChanged has no "pause the transition" option — only
            // allow or cancel. Cancel now, run the async ensure, then re-enter Play automatically
            // once models are confirmed ready.
            EditorApplication.isPlaying = false;
            RunGuardThenResume();
        }

        private static async void RunGuardThenResume()
        {
            var (success, error) = await EnsureRegisteredModelsReady();
            if (success)
            {
                _verifiedReady = true;
                _resumePending = true;
                EditorApplication.isPlaying = true;
            }
            else if (error == TryllEditorUiContract.ServerNeedsLicence)
            {
                // The licence gate has already opened the Welcome window: no second dialog.
                TryllLog.Warning("Play was not started. " + error);
            }
            else if (!string.IsNullOrEmpty(error))
            {
                TryllEditorPrompts.Notify("Tryll — cannot enter Play", error, "OK");
            }
            // Cancelled by the user (error == null): stay in edit mode quietly.
        }

        /// <summary>
        /// Message for a failed server launch. The licence block keeps its contract text, so
        /// every blocked start reads the same in Unity and Unreal.
        /// </summary>
        internal static string LaunchFailureMessage(string launchError)
            => launchError == TryllEditorUiContract.ServerNeedsLicence
                ? launchError
                : $"Server launch failed: {launchError}";

        /// <summary>
        /// Connects the shared editor <see cref="TryllClient"/>, compares the project's
        /// Production/Experimental manifest entries against live <c>ListModels</c> status, and
        /// downloads anything <see cref="TryllModelStatus.Absent"/> — blocking behind a cancelable
        /// progress bar. Returns <c>(true, null)</c> on success; <c>(false, null)</c> on user
        /// cancel; <c>(false, message)</c> on a hard error (e.g. a non-downloadable Absent model).
        /// </summary>
        public static async Task<(bool success, string error)> EnsureRegisteredModelsReady()
        {
            var client = TryllClient.EnsureEditorInstance();
            if (client == null)
                return (false, "Failed to create TryllClient.");

            // A session that is already up owns its server. Launching again from the
            // cached project settings (port 9100, Default variant, product name) is a
            // different launch than a caller that just spawned its own child. After the
            // v24 same-launch check (project name, storage root, and mode), TryLaunch
            // drops that child and tries to start another exe. On a machine where
            // .Server/Default is not the build that child used, the second launch fails
            // and the caller never leaves "checking models".
            if (!client.IsSessionReady)
            {
                var settings = TryllRuntimeSettings.Load();
                if (settings == null)
                    return (false, "No TryllRuntimeSettings asset found.");

                if (!TryllServerProcess.TryLaunch(settings, out var launchErr))
                    return (false, LaunchFailureMessage(launchErr));

                // Outside Play, TryllClient's own connect-retry coroutine never fires (it is
                // gated on Application.isPlaying), so a connect failure would otherwise leave
                // CreateSessionComplete unfired and this await hanging forever. Race it against
                // ConnectionChanged(false)/Error too so a failed connect resolves promptly.
                var sessionTcs = new TaskCompletionSource<TryllError>();
                void Unhook()
                {
                    client.CreateSessionComplete -= OnCreateSessionComplete;
                    client.ConnectionChanged     -= OnConnectionChanged;
                }
                void OnCreateSessionComplete(TryllError e) { Unhook(); sessionTcs.TrySetResult(e); }
                void OnConnectionChanged(bool ok)
                {
                    if (ok) return; // wait for CreateSessionComplete once actually connected
                    Unhook();
                    sessionTcs.TrySetResult(new TryllError(TryllErrorCode.ConnectionFailed, "Failed to connect to the Tryll server."));
                }

                client.CreateSessionComplete += OnCreateSessionComplete;
                client.ConnectionChanged     += OnConnectionChanged;
                client.StartSession();
                var sessionErr = await sessionTcs.Task;
                if (!sessionErr.IsOk)
                    return (false, $"CreateSession failed: {sessionErr.Message}");
            }

            var manifest = TryllModelManifest.FindProjectManifest();
            if (manifest == null)
                return (true, null); // nothing registered — nothing to ensure

            var (models, listErr) = await AwaitListModels(client);
            if (!listErr.IsOk)
                return (false, $"ListModels failed: {listErr.Message}");

            var byName = models.ToDictionary(m => m.Name);
            var missing = new List<TryllModelInfo>();
            var nonDownloadable = new List<string>();
            foreach (var entry in manifest.Entries)
            {
                if (entry.Registration == TryllModelRegistration.None) continue;
                if (!byName.TryGetValue(entry.Name, out var info)) continue;
                if (info.Status != TryllModelStatus.Absent) continue;

                if (string.IsNullOrEmpty(info.HuggingFaceRepo)) nonDownloadable.Add(entry.Name);
                else missing.Add(info);
            }

            if (nonDownloadable.Count > 0)
            {
                return (false,
                    "The following registered models are missing and have no download source " +
                    "(no HuggingFace repo configured):\n\n" +
                    string.Join("\n", nonDownloadable.Select(n => $"  • {n}")) +
                    "\n\nProvide them via a local path or update the catalog.");
            }

            if (missing.Count == 0)
                return (true, null);

            return await DownloadAllBlocking(client, missing);
        }

        /// <summary>
        /// ListModels used to hang forever when the socket dropped before the
        /// reply (or never replied). Race the request against disconnect and a
        /// deadline so Chat/Play cannot stay on "Checking required models…".
        /// </summary>
        private static async Task<(List<TryllModelInfo> models, TryllError error)> AwaitListModels(TryllClient client)
        {
            System.Threading.Tasks.Task<(System.Collections.Generic.List<TryllModelInfo>, TryllError)> listTask;
            using (client.AsEditorCall())
                listTask = client.RequestListModelsAsync();
            var dropTcs = new TaskCompletionSource<(List<TryllModelInfo>, TryllError)>();
            void OnConnectionChanged(bool ok)
            {
                if (ok) return;
                dropTcs.TrySetResult((null, new TryllError(
                    TryllErrorCode.ConnectionDropped, "Connection dropped")));
            }
            client.ConnectionChanged += OnConnectionChanged;
            client.Error += OnError;
            void OnError(TryllError err)
            {
                if (err.Code == (int)TryllErrorCode.ConnectionDropped || err.Code == (int)TryllErrorCode.ConnectionFailed)
                    dropTcs.TrySetResult((null, err));
            }
            try
            {
                var timeout = Task.Delay(System.TimeSpan.FromSeconds(60));
                var completed = await Task.WhenAny(listTask, dropTcs.Task, timeout);
                if (completed == listTask)
                    return await listTask;
                if (completed == dropTcs.Task)
                    return await dropTcs.Task;
                return (null, new TryllError(TryllErrorCode.ConnectionFailed,
                    "ListModels timed out waiting for the server."));
            }
            finally
            {
                client.ConnectionChanged -= OnConnectionChanged;
                client.Error -= OnError;
            }
        }

        private static async Task<(bool, string)> DownloadAllBlocking(TryllClient client, List<TryllModelInfo> missing)
        {
            int total = missing.Count;
            var pending = new HashSet<string>(missing.Select(m => m.Name));
            var percentByName = new Dictionary<string, float>();
            var failed = new List<string>();
            var tcs = new TaskCompletionSource<bool>();
            bool cancelled = false;
            bool dropped = false;

            void OnProgress(string name, ulong bytes, ulong totalBytes, float percent)
            {
                if (pending.Contains(name)) percentByName[name] = percent;
            }
            void OnComplete(string name, bool success, string error)
            {
                if (!pending.Remove(name)) return;
                percentByName.Remove(name);
                if (!success) failed.Add($"{name}: {error}");
                if (pending.Count == 0) tcs.TrySetResult(true);
            }

            void OnConnectionChanged(bool ok)
            {
                if (ok) return;
                dropped = true;
                tcs.TrySetResult(false);
            }

            client.DownloadProgress += OnProgress;
            client.DownloadComplete += OnComplete;
            client.ConnectionChanged += OnConnectionChanged;
            try
            {
                foreach (var m in missing) client.RequestDownloadModel(m.Name);

                while (!tcs.Task.IsCompleted)
                {
                    int done = total - pending.Count;
                    float sum = done * 100f;
                    foreach (var name in pending) sum += percentByName.TryGetValue(name, out var p) ? p : 0f;
                    float frac = total > 0 ? (sum / total) / 100f : 1f;

                    if (EditorUtility.DisplayCancelableProgressBar("Tryll",
                        $"Downloading {pending.Count} required model(s)…", frac))
                    {
                        cancelled = true;
                        break;
                    }
                    await Task.Delay(100);
                }
            }
            finally
            {
                client.DownloadProgress -= OnProgress;
                client.DownloadComplete -= OnComplete;
                client.ConnectionChanged -= OnConnectionChanged;
                EditorUtility.ClearProgressBar();
            }

            if (cancelled) return (false, null);
            if (dropped) return (false, "Connection dropped while downloading required models.");
            if (failed.Count > 0) return (false, "Failed to download:\n\n" + string.Join("\n", failed));
            return (true, null);
        }
    }
}
