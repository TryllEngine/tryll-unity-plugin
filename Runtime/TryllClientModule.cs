// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEngine;
using Tryll.Client.Internal;

namespace Tryll.Client
{
    /// <summary>
    /// Domain-load entry point for the Tryll Client package.
    ///
    /// Responsibilities
    /// ----------------
    /// 1. <see cref="RuntimeInitializeOnLoadMethod"/> creates the
    ///    <see cref="TryllClient"/> singleton GameObject before any scene loads
    ///    and hooks <see cref="Application.quitting"/> for orderly teardown.
    ///
    /// 2. When <see cref="TryllRuntimeSettings.AutoLaunchServer"/> is enabled,
    ///    spawns <c>tryll_server.exe</c> via <see cref="TryllServerProcess"/>.
    /// </summary>
    internal static class TryllClientModule
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            // Package version, first Tryll line in every Unity log. Logged at bootstrap
            // rather than on connect so it is present even when no connection is ever
            // made — a "cannot connect" ticket is exactly the case where the later
            // "Server identity: ... version=..." line never appears.
            TryllLog.Info($"Tryll Client package v{TryllVersion.Version} " +
                          $"(protocol v{TryllProtocol.Version})");

            if (TryllClient.Instance == null)
            {
                var go = new GameObject("[TryllClient]");
                var client = go.AddComponent<TryllClient>();
                // Marks this instance as a default-configured singleton: a scene-placed
                // TryllClient may transfer its inspector configuration onto it.
                client.CreatedByModule = true;
                Object.DontDestroyOnLoad(go);
            }
            // May already be shared with an editor tool window (Chat / Model Manager);
            // acquiring here keeps their connection alive independently of Play mode.
            TryllClient.Instance.AcquireConnection();
            TryllClient.Instance.BeginPlaySession();

            // Before the spawn, so a subscriber can point at a catalog on the
            // cached settings instance. Awake is later than this method.
            TryllRuntimeSettings.NotifyBeforeAutoLaunch();
            var settings = TryllRuntimeSettings.Load();
            if (settings.AutoLaunchServer)
            {
                if (!TryllServerProcess.TryLaunch(settings, out var err) && err != null)
                {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                    // Surface a loud, actionable error: most often the bundled server
                    // folder is missing or empty (server not built yet).
                    TryllLog.Error($"AutoLaunchServer failed — {err}");
#else
                    // The bundled server is Windows-only; not an error elsewhere.
                    TryllLog.Warning($"AutoLaunchServer: {err}");
#endif
                }
                else
                {
                    // TryLaunch may have allocated a free ephemeral port (player builds)
                    // or determined the reuse port (editor domain-reload path). The
                    // singleton's Awake() already ran (above) and copied the old
                    // settings.ServerPort into its own ServerPort field by value, so
                    // mutating the settings asset here would not reach it — assign the
                    // live instance directly instead. Deliberately not writing back to
                    // the TryllRuntimeSettings asset: that would risk an editor session
                    // persisting a runtime-only port into the asset on save.
                    int actualPort = TryllServerProcess.ActualPort;
                    if (actualPort > 0)
                        TryllClient.Instance.ServerPort = actualPort;
                }
            }

            // Unsubscribe before subscribing: OnBeforeSceneLoad runs on every
            // play-mode entry, and with Enter Play Mode Options → domain reload
            // disabled, statics (including this handler registration) survive
            // across sessions. Without this, N play sessions would stack N
            // handlers, and one play-mode stop would fire N releases against a
            // single acquire — see ReleaseConnection's over-release guard below.
            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
        }

        private static void OnApplicationQuitting()
        {
            // Release only — Disconnect (via the refcount) only fires once every
            // other consumer (editor tool windows) has released it too. The server
            // self-exits via its idle-shutdown timer regardless (managed mode).
            TryllClient.Instance?.EndPlaySession();
            TryllClient.Instance?.ReleaseConnection();
        }
    }
}
