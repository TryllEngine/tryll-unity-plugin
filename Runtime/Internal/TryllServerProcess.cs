// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Diagnostics;
#endif

namespace Tryll.Client.Internal
{
    /// <summary>
    /// Process-level helper for launching and killing <c>tryll_server.exe</c>.
    /// Single static slot — anything in the Unity process (runtime module, editor
    /// window, etc.) shares the same handle so we never spawn two servers by
    /// accident.
    ///
    /// The server location needs no path configuration:
    ///   * Editor  → <c>Packages/com.tryll.client/.Server/&lt;variant&gt;/tryll_server.exe</c>
    ///               when the package bundles a server (plugin development, partner
    ///               archives), else the per-user install for this package version,
    ///               <c>%LOCALAPPDATA%\Tryll\server\&lt;version&gt;\&lt;full|lite&gt;\</c>
    ///               (<see cref="TryllServerCache"/>), which the Welcome window downloads.
    ///   * Build   → <c>&lt;game&gt;/Server/tryll_server.exe</c>, copied next to the
    ///               player exe at build time by <c>TryllServerBuildPostprocessor</c>.
    /// </summary>
    public static class TryllServerProcess
    {
        /// <summary>UPM package name; used to resolve the bundled server in the Editor.</summary>
        internal const string PackageName = "com.tryll.client";

        /// <summary>
        /// Unity-ignored folder inside the package holding the server payload. The leading dot
        /// keeps Unity from importing it (it does not hide the folder in Windows Explorer).
        /// </summary>
        internal const string PackageServerDir = ".Server";

        /// <summary>Folder next to the player exe that the build post-processor copies the payload into.</summary>
        internal const string BuildServerDir = "Server";

        /// <summary>Default build-variant subfolder under <see cref="PackageServerDir"/>.</summary>
        internal const string DefaultVariantDir = "Default";

        /// <summary>Release build-variant subfolder under <see cref="PackageServerDir"/>.</summary>
        internal const string ReleaseVariantDir = "Release";

        /// <summary>Server executable file name.</summary>
        internal const string ExeName = "tryll_server.exe";

        /// <summary>
        /// Choose the variant subfolder name (<c>Default</c>/<c>Release</c>) under
        /// <paramref name="serverRoot"/>. The settings selection is only honored when more than
        /// one subfolder is present; otherwise <c>Default</c> is always used. Falls back to
        /// <c>Default</c> if the chosen variant folder is missing.
        /// </summary>
        public static string ChooseVariantDir(string serverRoot, TryllRuntimeSettings settings)
        {
            // The variant option only matters when more than one recognized build folder
            // (Default/Release) is present; otherwise Default is always used.
            if (VariantDirs(serverRoot).Length <= 1)
                return DefaultVariantDir;

            string chosen = settings != null && settings.ServerBuildVariant == ServerBuild.Release
                ? ReleaseVariantDir
                : DefaultVariantDir;

            return Directory.Exists(Path.Combine(serverRoot, chosen)) ? chosen : DefaultVariantDir;
        }

        /// <summary>
        /// The recognized build-variant subfolders (<c>Default</c>/<c>Release</c>) that actually
        /// exist under <paramref name="serverRoot"/>. Other folders (e.g. a loose <c>data/</c>)
        /// are ignored.
        /// </summary>
        private static string[] VariantDirs(string serverRoot)
        {
            if (string.IsNullOrEmpty(serverRoot) || !Directory.Exists(serverRoot))
                return System.Array.Empty<string>();

            var present = new System.Collections.Generic.List<string>(2);
            if (Directory.Exists(Path.Combine(serverRoot, DefaultVariantDir))) present.Add(DefaultVariantDir);
            if (Directory.Exists(Path.Combine(serverRoot, ReleaseVariantDir))) present.Add(ReleaseVariantDir);
            return present.ToArray();
        }

#if UNITY_EDITOR
        /// <summary>Absolute path to the package's <c>.Server/</c> folder (Editor only).</summary>
        public static string EditorPackageServerRoot()
        {
            string root = UnityEditor.PackageManager.PackageInfo
                .FindForAssembly(typeof(TryllServerProcess).Assembly)?.resolvedPath;
            if (string.IsNullOrEmpty(root))
                root = Path.GetFullPath(Path.Combine("Packages", PackageName));
            return Path.Combine(root, PackageServerDir);
        }

        /// <summary>
        /// Recognized build-variant subfolders (<c>Default</c>/<c>Release</c>) present under
        /// <c>.Server/</c> (Editor only). Used to decide whether to surface the variant option.
        /// </summary>
        public static string[] EditorAvailableVariants() => VariantDirs(EditorPackageServerRoot());

        /// <summary>True when the package itself carries a server (<c>.Server/Default</c> or
        /// <c>.Server/Release</c>): plugin development links and partner archives.</summary>
        public static bool EditorHasBundledServer() => VariantDirs(EditorPackageServerRoot()).Length > 0;

        /// <summary>
        /// Folder holding <c>tryll_server.exe</c> in the Editor, in this order:
        /// <list type="number">
        /// <item>the package's <c>.Server/&lt;variant&gt;</c> when the package bundles a server;</item>
        /// <item>the per-user install for this package version (<see cref="TryllServerCache"/>),
        /// written by the Welcome window's download or "Install from zip";</item>
        /// <item>otherwise the package's <c>.Server/Default</c> path, which does not exist, so
        /// callers report "not installed".</item>
        /// </list>
        /// </summary>
        public static string EditorServerDir(TryllRuntimeSettings settings)
        {
            string root = EditorPackageServerRoot();
            if (VariantDirs(root).Length > 0)
                return Path.Combine(root, ChooseVariantDir(root, settings));
            string installed = TryllServerCache.FindInstalled(TryllVersion.Version);
            return installed ?? Path.Combine(root, DefaultVariantDir);
        }

        /// <summary>
        /// Message for a missing server: a package without a bundled server needs the
        /// one-time download, a development package needs a local server build.
        /// </summary>
        internal static string EditorMissingServerHint()
        {
            // A development package has a .Server/ folder even when its links are broken
            // (no local server build yet); a released package has no .Server/ at all.
            return Directory.Exists(EditorPackageServerRoot())
                ? "Build the Tryll server (Debug/Release) so the bundled .Server/ link resolves, " +
                  "or place a complete server build there."
                : "The Tryll server is not installed for this package version. " +
                  "Open Window > Tryll > Welcome and download it, or use \"Install from zip\".";
        }

        /// <summary>
        /// EditorPrefs key for the backend last used to spawn a server on
        /// <paramref name="port"/>. Survives domain reload so a setting change
        /// does not silently reuse a warm process started with the other plugin.
        /// </summary>
        private static string LlamaCppBackendPrefsKey(int port)
            => "Tryll.LlamaCppBackend." + ProjectRootDirectorySafe() + "." + port;

        private static string ProjectRootDirectorySafe()
        {
            try { return TryllRuntimeSettings.ProjectRootDirectory(); }
            catch { return ""; }
        }

        private static void RememberLlamaCppBackend(int port, string backend)
        {
            UnityEditor.EditorPrefs.SetString(LlamaCppBackendPrefsKey(port), backend ?? "");
        }

        private static string RememberedLlamaCppBackend(int port)
            => UnityEditor.EditorPrefs.GetString(LlamaCppBackendPrefsKey(port), "");

        private static string SaveRootPrefsKey(int port)
            => "Tryll.SaveRoot." + ProjectRootDirectorySafe() + "." + port;

        private static void RememberSaveRoot(int port, string saveRoot)
        {
            UnityEditor.EditorPrefs.SetString(SaveRootPrefsKey(port), saveRoot ?? "");
        }

        private static string RememberedSaveRoot(int port)
            => UnityEditor.EditorPrefs.GetString(SaveRootPrefsKey(port), "");

        private static string StorageRootPrefsKey(int port)
            => "Tryll.StorageRoot." + ProjectRootDirectorySafe() + "." + port;

        private static void RememberStorageRoot(int port, string storageRoot)
        {
            UnityEditor.EditorPrefs.SetString(StorageRootPrefsKey(port), storageRoot ?? "");
        }

        private static string RememberedStorageRoot(int port)
            => UnityEditor.EditorPrefs.GetString(StorageRootPrefsKey(port), "");

        private static string ProjectNamePrefsKey(int port)
            => "Tryll.ProjectName." + ProjectRootDirectorySafe() + "." + port;

        private static void RememberProjectName(int port, string projectName)
        {
            UnityEditor.EditorPrefs.SetString(ProjectNamePrefsKey(port), projectName ?? "");
        }

        private static string RememberedProjectName(int port)
            => UnityEditor.EditorPrefs.GetString(ProjectNamePrefsKey(port), "");

        private static string UserModelsCatalogPrefsKey(int port)
            => "Tryll.UserModelsCatalog." + ProjectRootDirectorySafe() + "." + port;

        private static void RememberUserModelsCatalog(int port, string path)
        {
            UnityEditor.EditorPrefs.SetString(UserModelsCatalogPrefsKey(port), path ?? "");
        }

        private static string RememberedUserModelsCatalog(int port)
            => UnityEditor.EditorPrefs.GetString(UserModelsCatalogPrefsKey(port), "");

        private static string ModePrefsKey(int port)
            => "Tryll.ServerMode." + ProjectRootDirectorySafe() + "." + port;

        private static void RememberMode(int port, string mode)
        {
            UnityEditor.EditorPrefs.SetString(ModePrefsKey(port), mode ?? "");
        }

        private static string RememberedMode(int port)
            => UnityEditor.EditorPrefs.GetString(ModePrefsKey(port), "");
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private static Process s_process;
        private static string s_launchedLlamaCppBackend;
        private static string s_launchedSaveRoot;
        private static string s_launchedStorageRoot;
        private static string s_launchedProjectName;
        private static string s_launchedMode;
        private static string s_launchedUserModelsCatalog;
        /// <summary>
        /// True after <see cref="WaitForTcpReady"/> observed the owned child accepting.
        /// Distinguishes a warming cold start (alive, not listening yet) from a
        /// zombie that finished idle-exit, closed its ports, and then hung in
        /// teardown — <see cref="IsRunning"/> is true in both cases.
        /// </summary>
        private static bool s_observedReady;

        public static bool IsRunning => s_process != null && !s_process.HasExited;
        public static int  Pid       => s_process?.Id ?? -1;

        /// <summary>
        /// The TCP port the managed server is actually listening on.
        /// Set by <see cref="TryLaunch"/> and consumed by the connection layer.
        /// 0 before the first successful launch.
        /// </summary>
        public static int ActualPort { get; private set; } = 0;

        /// <summary>
        /// Idle-shutdown grace period (seconds) passed to every server we spawn (managed mode).
        /// Both editor and player builds run managed: the server self-exits after this many
        /// seconds with no live sessions and no in-flight downloads. The grace comfortably
        /// covers editor domain reloads (a few seconds), so the warm server is reused across
        /// reloads/play sessions rather than dying when a window closes, yet still self-reaps
        /// when truly idle. See docs/research/clients/server-lifetime-ownership-and-port-selection.md §4.2.
        /// </summary>
        private const int IdleShutdownTimeoutSeconds = 60;

        /// <summary>Number of times <see cref="PickFreePort"/> retries before giving up.</summary>
        private const int FreePortProbeAttempts = 3;

        /// <summary>
        /// Process exit code the server returns when it cannot bind its listen port because
        /// the address is already in use (EADDRINUSE). Must match Tryll::ServerExitPortInUse
        /// in tryll/common/include/tryll/Protocol.h. Lets the launcher distinguish a bind
        /// collision (retry on a fresh port) from any other early exit.
        /// </summary>
        private const int ServerExitPortInUse = 3;

        /// <summary>Seconds to wait for the spawned server's TCP port to accept connections
        /// before handing a still-alive child to the editor connect retry
        /// (see <see cref="WaitForTcpReady"/>).</summary>
        private const float ReadinessTimeoutSeconds = 10f;

#if UNITY_EDITOR
        /// <summary>
        /// Extra CLI arguments appended to the next <see cref="TryLaunch"/> spawn
        /// (fault-injection flags for editor recovery tests). Reset in teardown.
        /// </summary>
        internal static string DebugExtraCliArgsForTests { get; set; }

        /// <summary>
        /// Override the spawn readiness wait for editor recovery tests. Null uses
        /// <see cref="ReadinessTimeoutSeconds"/>. Reset in teardown.
        /// </summary>
        internal static float? DebugReadinessTimeoutSecondsForTests { get; set; }

        /// <summary>
        /// Absolute <c>--storage-root</c> / <c>--save-root</c> for the next spawn
        /// instead of the project-derived folders, and no custom catalog or
        /// <c>Tryll/models.d</c>. Fixture-server window tests set these so the
        /// host project's state never reaches the test server. Reset in teardown.
        /// </summary>
        internal static string DebugStorageRootForTests { get; set; }
        internal static string DebugSaveRootForTests { get; set; }
        internal static bool DebugNoUserCatalogsForTests { get; set; }

        /// <summary>Clear every editor-test launch override.</summary>
        internal static void DebugResetLaunchOverridesForTests()
        {
            DebugExtraCliArgsForTests = null;
            DebugReadinessTimeoutSecondsForTests = null;
            DebugStorageRootForTests = null;
            DebugSaveRootForTests = null;
            DebugNoUserCatalogsForTests = false;
            DebugApplyLicenceGateInBatchModeForTests = false;
            s_warnedUnknownEnvMode = null;
        }
#endif

        /// <summary>Number of launch attempts when the port was auto-allocated (a fresh port
        /// can be re-picked on a bind collision); fixed-port launches get exactly one.</summary>
        private const int MaxLaunchAttempts = 4;

        /// <summary>
        /// Try to bind a socket to port 0 and return the OS-assigned ephemeral port.
        /// Retries a few times, since the probe can fail transiently under port
        /// exhaustion or an odd network stack. Returns 0 only when every attempt
        /// failed — the caller treats that as a launch error rather than silently
        /// falling back to the shared default port.
        /// </summary>
        private static int PickFreePort()
        {
            for (int attempt = 1; attempt <= FreePortProbeAttempts; ++attempt)
            {
                TcpListener listener = null;
                try
                {
                    listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
                    listener.Start();
                    return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
                }
                catch
                {
                    if (attempt < FreePortProbeAttempts)
                        System.Threading.Thread.Sleep(50);
                }
                finally
                {
                    listener?.Stop();
                }
            }
            return 0;
        }

        internal enum PortProbeResult
        {
            /// <summary>Nothing answered — the port is free to bind.</summary>
            Empty,
            /// <summary>A Tryll server answered and its exe_path matches ours (or predates identity fields).</summary>
            ReusableServer,
            /// <summary>Something answered but is not our server — do not reuse it.</summary>
            Rejected,
        }

        /// <summary>
        /// Connect briefly to <paramref name="port"/>, read the first frame, and verify it is
        /// a <c>ConnectionReady</c> whose <c>exe_path</c> canonically matches
        /// <paramref name="intendedExe"/>. A server that predates the identity fields (empty
        /// exe_path) is accepted, since it cannot be verified either way.
        ///
        /// This is the identity check that makes known-port reuse fail-closed: a stale/foreign
        /// process on the editor's fixed port is rejected instead of silently adopted, matching
        /// the same exe_path comparison TryllClient performs after a full connect.
        /// </summary>
        /// <summary>Exposed for EditMode tests (InternalsVisibleTo).</summary>
        internal static PortProbeResult ProbeAndVerify(int port, string intendedExe, out string reason)
        {
            reason = null;
            TcpClient client = null;
            bool connected = false;
            try
            {
                client = new TcpClient();
                var connectResult = client.BeginConnect("127.0.0.1", port, null, null);
                if (!connectResult.AsyncWaitHandle.WaitOne(200) || !client.Connected)
                    return PortProbeResult.Empty;

                connected = true;
                using var stream = client.GetStream();
                stream.ReadTimeout = 1000;

                byte[] frame = ClientWire.ReadFrame(stream);
                if (frame == null)
                {
                    reason = "connected but did not send a valid frame (likely not a Tryll server)";
                    return PortProbeResult.Rejected;
                }

                var evt = ClientCodec.Decode(frame);
                if (evt == null || evt.Type != TryllConnEventType.Connected)
                {
                    reason = "connected but the first frame was not ConnectionReady";
                    return PortProbeResult.Rejected;
                }

                if (string.IsNullOrEmpty(evt.ServerExePath))
                {
                    // Server predates the identity fields — cannot verify, but it spoke
                    // the Tryll protocol correctly, so treat it as reusable.
                    return PortProbeResult.ReusableServer;
                }

                string intended = TryllPathUtil.Canonicalize(intendedExe);
                string actual   = TryllPathUtil.Canonicalize(evt.ServerExePath);
                if (!string.Equals(intended, actual, StringComparison.OrdinalIgnoreCase))
                {
                    reason = $"identity mismatch (expected '{intended}', found '{actual}')";
                    return PortProbeResult.Rejected;
                }

                return PortProbeResult.ReusableServer;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                // A completed TCP connect means something is listening. A later
                // timeout / IOException (HTTP, any request/response peer that
                // waits for the client to speak) is a stranger, not an empty port.
                return connected ? PortProbeResult.Rejected : PortProbeResult.Empty;
            }
            finally
            {
                try { client?.Close(); } catch { /* best-effort */ }
            }
        }

        /// <summary>
        /// Poll <paramref name="port"/> until it accepts a TCP connection or
        /// <paramref name="timeoutSeconds"/> elapses, checking <paramref name="process"/> for
        /// early exit on every iteration. Without this, <see cref="TryLaunch"/> would report
        /// launch success as soon as the OS process handle exists, even though cold startup
        /// (config/model-catalog loading, antivirus scanning, or a port collision) can leave
        /// the port unreachable for a while — or forever, if the server already exited.
        /// Mirrors the C++ client's <c>ManagedServer::WaitForTcp</c> readiness loop.
        /// </summary>
        /// <param name="exitCode">
        /// Set to the process's exit code if it had already exited by the time this returns
        /// <c>false</c>; <c>null</c> if it was still running when the timeout elapsed.
        /// </param>
        /// <summary>
        /// Policy for an owned child that is still alive: only a process we
        /// previously saw accepting, and that now refuses TCP, is a teardown
        /// zombie worth killing. A warming cold start must be left alone.
        /// </summary>
        /// <summary>
        /// True when a server stdout line is logged at warning, error or critical. The
        /// server's pattern is "[%H:%M:%S.%e] [%l] %v" (ServerLogging.cpp); a line
        /// without that prefix (continuation, banner) is not.
        /// </summary>
        internal static bool IsWarningOrErrorLine(string line)
        {
            if (string.IsNullOrEmpty(line) || line[0] != '[') return false;
            int timeEnd = line.IndexOf(']');
            if (timeEnd < 0 || string.CompareOrdinal(line, timeEnd, "] [", 0, 3) != 0) return false;
            int levelStart = timeEnd + 3;
            int levelEnd = line.IndexOf(']', levelStart);
            if (levelEnd < 0) return false;
            string level = line.Substring(levelStart, levelEnd - levelStart);
            return level == "warning" || level == "error" || level == "critical";
        }

        internal static bool OwnedProcessLooksDead(bool observedReady, bool tcpAccepts)
            => observedReady && !tcpAccepts;

        /// <summary>Single 200 ms TCP connect attempt. Does not read a frame.</summary>
        private static bool TcpAccepts(int port)
        {
            try
            {
                using var probe = new TcpClient();
                var connectResult = probe.BeginConnect("127.0.0.1", port, null, null);
                return connectResult.AsyncWaitHandle.WaitOne(200) && probe.Connected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// If our owned child previously accepted and now refuses, kill it so
        /// the next spawn can bind. Returns true when the handle is still
        /// usable (healthy or still warming).
        /// </summary>
        private static bool OwnedProcessIsUsable()
        {
            if (!IsRunning)
                return false;

            int port = ActualPort > 0 ? ActualPort : 9100;
            if (TcpAccepts(port))
            {
                s_observedReady = true;
                return true;
            }

            // Warming cold start: we have never seen it accept, and it is still
            // younger than readiness + idle grace. A hung idle-exit is older.
            if (!s_observedReady)
            {
                try
                {
                    double ageSec = (System.DateTime.Now - s_process.StartTime).TotalSeconds;
                    if (ageSec < IdleShutdownTimeoutSeconds + ReadinessTimeoutSeconds + 10)
                        return true;
                }
                catch
                {
                    return true;
                }
            }

            // Idle-exit briefly closes the acceptor while it re-checks sessions.
            System.Threading.Thread.Sleep(250);
            if (TcpAccepts(port))
            {
                s_observedReady = true;
                return true;
            }

            TryllLog.Warning($"TryllServerProcess: owned server PID {Pid} is alive but not " +
                             $"accepting on port {port} — treating it as a hung idle-exit and relaunching.");
            Kill();
            return false;
        }

        /// <returns><c>true</c> once the port accepts a connection while the process is still running.</returns>
        private static bool WaitForTcpReady(Process process, int port, float timeoutSeconds, out int? exitCode)
        {
            exitCode = null;
            var deadline = System.DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (System.DateTime.UtcNow < deadline)
            {
                if (process.HasExited)
                {
                    exitCode = process.ExitCode;
                    return false;
                }

                try
                {
                    using var probe = new TcpClient();
                    var connectResult = probe.BeginConnect("127.0.0.1", port, null, null);
                    if (connectResult.AsyncWaitHandle.WaitOne(200) && probe.Connected)
                    {
                        // A process that raced to exit right as the connect succeeded must
                        // not be reported as ready — re-check before returning true.
                        if (process.HasExited)
                        {
                            exitCode = process.ExitCode;
                            return false;
                        }
                        return true;
                    }
                }
                catch
                {
                    // Connection refused / not listening yet — keep polling.
                }
            }
            return false; // timed out while still running; exitCode stays null
        }

        static string QuoteCliArg(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";
            if (value.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
                return value;
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        /// <summary>
        /// Environment variable read by the Unity/Unreal launchers: <c>tryll-dev</c> or
        /// <c>client-dev</c>. <c>tryll-dev</c> launches <c>--mode tryll-dev</c> and adds
        /// <c>--telemetry-level off</c> so internal CI and developers emit nothing. It wins
        /// over the editor developer toggle and the project telemetry level so CI scripts
        /// stay deterministic.
        /// </summary>
        internal const string ServerModeEnvVar = "TRYLL_SERVER_MODE";

        internal const string TryllDevMode  = "tryll-dev";
        internal const string ClientDevMode = "client-dev";

#if UNITY_EDITOR
        /// <summary>
        /// Launch mode chosen by the monorepo-only developer menu
        /// (<c>Window/Tryll/Developer</c>). Null leaves the default. The menu source is
        /// not shipped, so in a released package nothing ever sets this.
        /// </summary>
        internal static string EditorDeveloperModeOverride { get; set; }

        /// <summary>
        /// Licence gate for a server the editor starts, set by the Editor assembly
        /// (<c>TryllLicenceGate</c>; Runtime code cannot reference it). Returns null when the
        /// start may go ahead, else the message to show.
        /// </summary>
        internal static Func<string> EditorLaunchBlockReason { get; set; }

        /// <summary>
        /// Test seam: apply the licence gate in batch mode too, so a test can see a blocked
        /// start. Reset by <see cref="DebugResetLaunchOverridesForTests"/>.
        /// </summary>
        internal static bool DebugApplyLicenceGateInBatchModeForTests { get; set; }

        /// <summary>Shown when the gate cannot ask the Editor assembly (it did not load).</summary>
        internal const string LicenceGateMissingError =
            "The Tryll editor scripts did not load, so the licence cannot be checked. " +
            "Fix the errors in the Console, then try again.";

        /// <summary>
        /// Why the editor may not start a server now, or null. Batch mode and tryll-dev are
        /// never blocked: nobody can accept the licence there. In an interactive editor a
        /// missing <paramref name="blockReason"/> hook fails closed.
        /// </summary>
        internal static string EditorLaunchGateError(Func<string> blockReason, bool batchMode, string mode)
        {
            if (batchMode || mode == TryllDevMode)
                return null;
            return blockReason != null ? blockReason() : LicenceGateMissingError;
        }
#endif

        private static string s_warnedUnknownEnvMode;

        /// <summary>
        /// Canonical launch mode for <paramref name="value"/>, or null when it is empty
        /// or not a mode a plugin may launch.
        /// </summary>
        internal static string NormalizeLaunchMode(string value)
        {
            string trimmed = value?.Trim();
            if (string.Equals(trimmed, TryllDevMode, StringComparison.OrdinalIgnoreCase))
                return TryllDevMode;
            if (string.Equals(trimmed, ClientDevMode, StringComparison.OrdinalIgnoreCase))
                return ClientDevMode;
            return null;
        }

        /// <summary>
        /// Mode for the next spawn: <see cref="ServerModeEnvVar"/>, then (editor only)
        /// <see cref="EditorDeveloperModeOverride"/>, then <c>client-dev</c>. Players and
        /// the editor share this.
        /// </summary>
        internal static string ResolveLaunchMode()
        {
#if !UNITY_EDITOR
            return "client-prod";
#else
            string envRaw = Environment.GetEnvironmentVariable(ServerModeEnvVar);
            string envMode = NormalizeLaunchMode(envRaw);
            if (envMode != null)
                return envMode;
            if (!string.IsNullOrWhiteSpace(envRaw)
                && !string.Equals(s_warnedUnknownEnvMode, envRaw, StringComparison.Ordinal))
            {
                s_warnedUnknownEnvMode = envRaw;
                TryllLog.Warning($"{ServerModeEnvVar}='{envRaw}' is not '{TryllDevMode}' or " +
                                 $"'{ClientDevMode}'; ignoring it.");
            }
#if UNITY_EDITOR
            string developerMode = NormalizeLaunchMode(EditorDeveloperModeOverride);
            if (developerMode != null)
                return developerMode;
#endif
            return ClientDevMode;
#endif
        }

        /// <summary>
        /// Build the CLI argument string for a managed spawn (without the exe path).
        /// Exposed for EditMode tests.
        /// </summary>
        internal static string BuildLaunchArguments(
            int listenPort,
            string wantedBackend,
            string wantedSaveRoot,
            string wantedStorageRoot,
            string projectName,
            string projectDir,
            string wantedUserCatalog,
            bool warnMissingUserCatalog,
            TryllRuntimeSettings settings,
            out bool clearMissingCatalogWarning)
        {
            clearMissingCatalogWarning = false;
            string mode = ResolveLaunchMode();

            // Always launch in managed mode (editor and player): the server
            // self-exits after the idle grace once no client is connected.
            var sb = new System.Text.StringBuilder();
            sb.Append("--port ").Append(listenPort);
            sb.Append(" --idle-shutdown-timeout ").Append(IdleShutdownTimeoutSeconds);
            sb.Append(" --llama-backend ").Append(wantedBackend);
            sb.Append(" --allow-absolute-save-paths");
            sb.Append(" --mode ").Append(mode);
#if !UNITY_EDITOR
            string logDir = Path.GetFullPath(Path.Combine(
                Application.persistentDataPath, "Tryll", "logs"));
            Directory.CreateDirectory(logDir);
            sb.Append(" --log-dir ").Append(QuoteCliArg(logDir));
#endif
            if (mode == TryllDevMode)
                sb.Append(" --telemetry-level off");
            else if (mode == ClientDevMode)
            {
                TryllTelemetryLevel level = settings != null
                    ? settings.TelemetryLevel
                    : TryllTelemetryLevel.Basic;
                sb.Append(" --telemetry-level ")
                    .Append(TryllRuntimeSettings.TelemetryLevelCliToken(level));
            }
            if (!string.IsNullOrWhiteSpace(projectName))
                sb.Append(" --project-name ").Append(QuoteCliArg(projectName));
            if (!string.IsNullOrWhiteSpace(projectDir))
                sb.Append(" --project-dir ").Append(QuoteCliArg(projectDir));
            if (mode == "client-dev" && settings != null
                && TryllRuntimeSettings.IsProjectId(settings.ProjectId))
                sb.Append(" --project-id ").Append(settings.ProjectId.ToLowerInvariant());
            if (!string.IsNullOrWhiteSpace(wantedStorageRoot))
                sb.Append(" --storage-root ").Append(QuoteCliArg(wantedStorageRoot));
            if (!string.IsNullOrWhiteSpace(wantedSaveRoot))
                sb.Append(" --save-root ").Append(QuoteCliArg(wantedSaveRoot));
            if (warnMissingUserCatalog)
            {
                TryllLog.Warning(
                    "Custom models catalog '" +
                    (settings != null ? settings.UserModelsCatalogPath : "") +
                    "' was not found. The server will start without it.");
                clearMissingCatalogWarning = true;
            }
            if (!string.IsNullOrEmpty(wantedUserCatalog))
                sb.Append(" --user-models-path ").Append(QuoteCliArg(wantedUserCatalog));
            // Extra drop-ins, when a project still uses Tryll/models.d.
            string userModelsDir = Path.Combine(
                TryllRuntimeSettings.ProjectRootDirectory(), "Tryll", "models.d");
            bool includeModelsDir = Directory.Exists(userModelsDir);
#if UNITY_EDITOR
            includeModelsDir &= !DebugNoUserCatalogsForTests;
#endif
            if (includeModelsDir)
                sb.Append(" --user-models-dir ").Append(QuoteCliArg(Path.GetFullPath(userModelsDir)));
#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(DebugExtraCliArgsForTests))
                sb.Append(' ').Append(DebugExtraCliArgsForTests.Trim());
#endif
            return sb.ToString();
        }

        public static bool TryLaunch(TryllRuntimeSettings settings, out string error)
        {
            error = null;
#if UNITY_EDITOR
            // The editor starts or reuses a server only under an accepted licence. Player
            // builds compile without this block, so a shipped game is never gated.
            error = EditorLaunchGateError(EditorLaunchBlockReason,
                Application.isBatchMode && !DebugApplyLicenceGateInBatchModeForTests,
                ResolveLaunchMode());
            if (error != null)
                return false;
            if (settings != null
                && !TryllRuntimeSettings.TryNormalizeStorageDataFolder(
                    TryllRuntimeSettings.ProjectRootDirectory(),
                    settings.StorageDataFolder,
                    out _,
                    out string storageError))
            {
                error = storageError;
                return false;
            }
#endif
            string wantedBackend = settings != null ? settings.LlamaCppBackendCliValue() : "vulkan";
            string wantedSaveRoot = settings != null
                ? settings.ResolveSaveDataFolderForLaunch()
                : TryllRuntimeSettings.ResolveSaveDataFolderForLaunch(string.Empty);
            string wantedStorageRoot = settings != null
                ? settings.ResolveStorageRootForRuntime()
                : TryllRuntimeSettings.ResolveStorageRootForRuntime(string.Empty);
            string wantedProjectName = settings != null
                ? settings.ResolveProjectName()
                : (Application.productName ?? string.Empty);
            string wantedProjectDir = TryllRuntimeSettings.ProjectRootDirectory();
            string wantedUserCatalog = settings != null
                ? settings.ResolveUserModelsCatalogForLaunch()
                : string.Empty;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(DebugStorageRootForTests))
                wantedStorageRoot = DebugStorageRootForTests;
            if (!string.IsNullOrEmpty(DebugSaveRootForTests))
                wantedSaveRoot = DebugSaveRootForTests;
            if (DebugNoUserCatalogsForTests)
                wantedUserCatalog = string.Empty;
#endif
            bool warnMissingUserCatalog = false;
            // Only a file that exists is passed. Remember that, so creating the
            // file later (or clearing the setting) does not reuse a server that
            // was started without it.
            if (!string.IsNullOrEmpty(wantedUserCatalog) && !File.Exists(wantedUserCatalog))
            {
                warnMissingUserCatalog = true;
                wantedUserCatalog = string.Empty;
            }
            if (IsRunning)
            {
                bool sameLaunch =
                    string.Equals(s_launchedLlamaCppBackend, wantedBackend, StringComparison.Ordinal)
                    && string.Equals(s_launchedSaveRoot ?? "", wantedSaveRoot ?? "", StringComparison.Ordinal)
                    && string.Equals(s_launchedStorageRoot ?? "", wantedStorageRoot ?? "", StringComparison.Ordinal)
                    && string.Equals(s_launchedProjectName ?? "", wantedProjectName ?? "", StringComparison.Ordinal)
                    && string.Equals(s_launchedUserModelsCatalog ?? "", wantedUserCatalog ?? "", StringComparison.Ordinal)
                    && string.Equals(s_launchedMode ?? "", ResolveLaunchMode(), StringComparison.Ordinal);
#if UNITY_EDITOR
                if (!sameLaunch)
                {
                    // Setting changed while our handle is live — drop it and treat the
                    // still-running process as a stranger so we spawn with the new flag.
                    ReleaseHandle();
                }
                else if (OwnedProcessIsUsable())
                {
                    return true;
                }
#else
                if (sameLaunch && OwnedProcessIsUsable())
                    return true;
                if (!sameLaunch)
                {
                    TryllLog.Info("Restarting the Tryll server so the launch settings take effect.");
                    Kill();
                }
#endif
            }

            string exe = ResolveExePath(settings);
            if (string.IsNullOrEmpty(exe))
            {
                error = "could not resolve the bundled server location.";
                return false;
            }
            if (!File.Exists(exe))
            {
                string dir = Path.GetDirectoryName(exe);
#if UNITY_EDITOR
                string hint = EditorMissingServerHint();
#else
                string hint = "Rebuild the player so the Tryll server is copied next to it.";
#endif
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                {
                    error = $"server folder not found: '{dir}'. {hint}";
                }
                else
                {
                    error = $"'{ExeName}' not found in server folder '{dir}' (folder is empty " +
                            $"or incomplete). {hint}";
                }
                return false;
            }

            bool knownPortHasStranger = false;

#if UNITY_EDITOR
            // Editor path: probe the known port first so a server that survived a
            // domain reload (process still alive, socket still bound) can be reused
            // without spawning a duplicate. Fail closed: only reuse when the probe
            // confirms a matching exe_path (or the server predates identity fields).
            // A mismatched/unverifiable listener is treated as a stranger — we fall
            // back to spawning our own server on a free port instead of the known
            // port, since the known port is occupied and not ours to bind.
            int knownPort = settings != null ? settings.ServerPort : 9100;
            switch (ProbeAndVerify(knownPort, exe, out string probeReason))
            {
                case PortProbeResult.ReusableServer:
                    string remembered = RememberedLlamaCppBackend(knownPort);
                    bool backendMatches = string.Equals(remembered, wantedBackend, StringComparison.Ordinal)
                        || (string.IsNullOrEmpty(remembered) && wantedBackend == "vulkan");
                    string rememberedSave = RememberedSaveRoot(knownPort);
                    string rememberedStorage = RememberedStorageRoot(knownPort);
                    string rememberedProject = RememberedProjectName(knownPort);
                    string rememberedCatalog = RememberedUserModelsCatalog(knownPort);
                    bool saveMatches = string.Equals(rememberedSave ?? "", wantedSaveRoot ?? "", StringComparison.Ordinal);
                    bool storageMatches = string.Equals(
                        rememberedStorage ?? "", wantedStorageRoot ?? "", StringComparison.Ordinal);
                    bool projectMatches = string.Equals(
                        rememberedProject ?? "", wantedProjectName ?? "", StringComparison.Ordinal);
                    bool catalogMatches = string.Equals(
                        rememberedCatalog ?? "", wantedUserCatalog ?? "", StringComparison.Ordinal);
                    string rememberedMode = RememberedMode(knownPort);
                    bool modeMatches = string.Equals(
                        rememberedMode ?? "", ResolveLaunchMode(), StringComparison.Ordinal);
                    if (backendMatches && saveMatches && storageMatches && projectMatches && catalogMatches && modeMatches)
                    {
                        TryllLog.Info($"TryllServerProcess: port {knownPort} already in use by a verified " +
                                      "Tryll server — reusing.");
                        ActualPort = knownPort;
                        s_launchedLlamaCppBackend = wantedBackend;
                        s_launchedSaveRoot = wantedSaveRoot;
                        s_launchedStorageRoot = wantedStorageRoot;
                        s_launchedProjectName = wantedProjectName;
                        s_launchedUserModelsCatalog = wantedUserCatalog;
                        s_launchedMode = ResolveLaunchMode();
                        // A domain reload cleared the handle of a server this editor spawned.
                        // Take it back, so Withdraw and Kill can still stop it.
                        if (TryReattachSpawned(knownPort, exe))
                            s_observedReady = true;
                        return true;
                    }
                    TryllLog.Warning($"TryllServerProcess: port {knownPort} is a verified Tryll server " +
                                      $"started with backend '{(string.IsNullOrEmpty(remembered) ? "unknown" : remembered)}', " +
                                      $"save-root '{rememberedSave}', storage-root '{rememberedStorage}', " +
                                      $"project '{rememberedProject}', catalog '{rememberedCatalog}' but this project wants " +
                                      $"'{wantedBackend}' / '{wantedSaveRoot}' / '{wantedStorageRoot}' / " +
                                      $"'{wantedProjectName}' / '{wantedUserCatalog}' — will not reuse it.");
                    knownPortHasStranger = true;
                    break;

                case PortProbeResult.Rejected:
                    TryllLog.Warning($"TryllServerProcess: port {knownPort} is occupied by a different " +
                                      $"process ({probeReason}); will not reuse it — spawning our own " +
                                      "server on a free port instead.");
                    knownPortHasStranger = true;
                    break;

                case PortProbeResult.Empty:
                default:
                    break; // nothing there — proceed to spawn on the known port as usual
            }
#endif

            // Whether the port for this launch was auto-allocated (a fresh port can be
            // re-picked on a bind collision) vs. a fixed known port (one attempt, report
            // the conflict instead of retrying blindly).
            // Editor: fixed known port, unless a stranger already forced a fallback.
            // Player: always auto-allocated so concurrent game instances don't collide.
#if UNITY_EDITOR
            bool autoPort = knownPortHasStranger;
#else
            bool autoPort = true;
#endif
            int maxAttempts = autoPort ? MaxLaunchAttempts : 1;
            string lastReason = "unknown";

            for (int attempt = 1; attempt <= maxAttempts; ++attempt)
            {
                // Determine the port to use for this attempt.
                // Editor: bind on the known port so subsequent domain-reload probes find
                // it, unless a stranger already holds it (fall back to a free ephemeral
                // port). Player: always pick a free ephemeral port.
                int listenPort;
#if UNITY_EDITOR
                if (knownPortHasStranger)
                {
                    listenPort = PickFreePort();
                    if (listenPort == 0) listenPort = knownPort; // last resort; will likely fail to bind
                }
                else
                {
                    listenPort = knownPort;
                }
#else
                listenPort = PickFreePort();
                if (listenPort == 0)
                {
                    // Fail closed rather than silently reusing the shared default port,
                    // which would reintroduce the cross-process collision the free-port
                    // policy exists to prevent — on exactly the machines (port exhaustion,
                    // odd network stacks) where a collision is most likely.
                    error = "failed to allocate a free port after several attempts " +
                            "(port exhaustion?); refusing to silently fall back to the shared default port.";
                    return false;
                }
#endif

                Process proc;
                try
                {
#if UNITY_EDITOR
                    if (ResolveLaunchMode() == "client-dev")
                    {
                        string projectId = TryllRuntimeSettings.EnsureSavedProjectId();
                        if (settings != null)
                            settings.ProjectId = projectId;
                    }
#endif
                    string args = BuildLaunchArguments(
                        listenPort,
                        wantedBackend,
                        wantedSaveRoot,
                        wantedStorageRoot,
                        wantedProjectName,
                        wantedProjectDir,
                        wantedUserCatalog,
                        warnMissingUserCatalog,
                        settings,
                        out bool clearMissingCatalogWarning);
                    if (clearMissingCatalogWarning)
                        warnMissingUserCatalog = false;
                    string command = exe + " " + args;
                    if (command.Length > TryllRuntimeSettings.MaxWindowsLaunchCodeUnits)
                    {
                        error = "The server launch command is " + command.Length
                            + " UTF-16 code units, above the "
                            + TryllRuntimeSettings.MaxWindowsLaunchCodeUnits
                            + " limit. Shorten paths or extra arguments.";
                        return false;
                    }
                    var psi = new ProcessStartInfo
                    {
                        FileName               = exe,
                        Arguments              = args,
                        WorkingDirectory       = Path.GetDirectoryName(exe),
                        UseShellExecute        = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        CreateNoWindow         = true,
                    };
                    proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                    proc.OutputDataReceived += (_, a) =>
                    {
                        if (string.IsNullOrEmpty(a.Data)) return;
                        // Server warnings and errors stand out in the Console as warnings.
                        // Not errors: many are per-request failures the caller already gets.
                        if (IsWarningOrErrorLine(a.Data)) TryllLog.Warning($"[Server] {a.Data}");
                        else TryllLog.Info($"[Server] {a.Data}");
                    };
                    proc.ErrorDataReceived += (_, a) =>
                    {
                        if (!string.IsNullOrEmpty(a.Data)) TryllLog.Warning($"[Server] {a.Data}");
                    };
                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    s_process = proc;
#if UNITY_EDITOR
                    RememberSpawned(proc, listenPort, exe);
#endif
                    TryllLog.Info($"TryllServerProcess: spawned PID {proc.Id} ({exe}) on port {listenPort}, " +
                                  "waiting for it to become ready...");
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    s_process = null;
                    s_observedReady = false;
                    ActualPort = 0;
                    return false;
                }

                float readinessTimeout = ReadinessTimeoutSeconds;
#if UNITY_EDITOR
                if (DebugReadinessTimeoutSecondsForTests.HasValue)
                    readinessTimeout = Math.Max(0f, DebugReadinessTimeoutSecondsForTests.Value);
#endif
                if (WaitForTcpReady(proc, listenPort, readinessTimeout, out int? exitCode))
                {
                    s_observedReady = true;
                    ActualPort = listenPort;
                    s_launchedLlamaCppBackend = wantedBackend;
                    s_launchedSaveRoot = wantedSaveRoot;
                    s_launchedStorageRoot = wantedStorageRoot;
                    s_launchedProjectName = wantedProjectName;
                    s_launchedUserModelsCatalog = wantedUserCatalog;
                    s_launchedMode = ResolveLaunchMode();
#if UNITY_EDITOR
                    RememberLlamaCppBackend(listenPort, wantedBackend);
                    RememberSaveRoot(listenPort, wantedSaveRoot);
                    RememberStorageRoot(listenPort, wantedStorageRoot);
                    RememberProjectName(listenPort, wantedProjectName);
                    RememberUserModelsCatalog(listenPort, wantedUserCatalog);
                    RememberMode(listenPort, s_launchedMode);
#endif
                    TryllLog.Info($"TryllServerProcess: server ready on port {listenPort} (PID {proc.Id}).");
                    return true;
                }

                // Not ready: an early exit is a real launch failure (retry on a
                // bind collision). A still-alive child is a warming cold start —
                // keep it and let the editor connect retry own the wait, matching
                // Unreal FTryllManagedServerProcess::Spawn.
                if (exitCode.HasValue)
                {
                    s_process = null;
#if UNITY_EDITOR
                    ForgetSpawned();
#endif
                    s_observedReady = false;
                    ActualPort = 0;
                    lastReason = $"process exited with code {exitCode.Value}";
                    if (exitCode.Value == ServerExitPortInUse && autoPort && attempt < maxAttempts)
                    {
                        TryllLog.Warning($"TryllServerProcess: port {listenPort} was taken before the " +
                                          $"server could bind (attempt {attempt}/{maxAttempts}); retrying " +
                                          "on a new free port.");
                        try { proc.Dispose(); } catch { /* best-effort */ }
                        continue;
                    }
                }
                else
                {
                    s_observedReady = false;
                    ActualPort = listenPort;
                    s_launchedLlamaCppBackend = wantedBackend;
                    s_launchedSaveRoot = wantedSaveRoot;
                    s_launchedStorageRoot = wantedStorageRoot;
                    s_launchedProjectName = wantedProjectName;
                    s_launchedUserModelsCatalog = wantedUserCatalog;
                    s_launchedMode = ResolveLaunchMode();
#if UNITY_EDITOR
                    RememberLlamaCppBackend(listenPort, wantedBackend);
                    RememberSaveRoot(listenPort, wantedSaveRoot);
                    RememberStorageRoot(listenPort, wantedStorageRoot);
                    RememberProjectName(listenPort, wantedProjectName);
                    RememberUserModelsCatalog(listenPort, wantedUserCatalog);
                    RememberMode(listenPort, s_launchedMode);
#endif
                    TryllLog.Warning($"TryllServerProcess: server not yet accepting on port {listenPort} " +
                                     $"after {readinessTimeout:0.#}s; proceeding — the client will keep " +
                                     "retrying while the server warms up.");
                    return true;
                }

                try { proc.Dispose(); } catch { /* best-effort */ }
                error = $"failed to start the Tryll server on port {listenPort}: {lastReason}";
                return false;
            }

            error = $"gave up starting the Tryll server after {maxAttempts} attempts — {lastReason}";
            return false;
        }

        /// <summary>
        /// Release the process handle without killing the server.
        /// The server self-exits via its idle-shutdown timer (managed mode).
        /// </summary>
        public static void ReleaseHandle()
        {
#if UNITY_EDITOR
            ForgetSpawned();
#endif
            if (s_process == null) return;
            try { s_process.Dispose(); } catch { }
            s_process = null;
            s_observedReady = false;
        }

        /// <summary>
        /// Force-terminates the server process (Withdraw, emergency and test cleanup). In the
        /// editor this also stops a server this editor spawned before a domain reload cleared
        /// the handle (see <c>TryReattachSpawned</c>).
        /// </summary>
        public static void Kill()
        {
#if UNITY_EDITOR
            TryReattachSpawned(0, null);
            ForgetSpawned();
#endif
            if (s_process == null || s_process.HasExited)
            {
                s_process = null;
                s_observedReady = false;
                return;
            }
            try
            {
                TryllLog.Info($"TryllServerProcess: killing PID {s_process.Id}");
                s_process.Kill();
                s_process.WaitForExit(3000);
            }
            catch (Exception ex)
            {
                TryllLog.Warning($"TryllServerProcess: failed to kill server: {ex.Message}");
            }
            finally
            {
                s_process = null;
                s_observedReady = false;
            }
        }

#if UNITY_EDITOR
        // The server this editor spawned: PID, start time, port and exe. SessionState survives
        // a domain reload (script compile, Play with domain reload) but not an editor restart,
        // and each editor has its own, so another editor's server is never taken over.
        private const string SpawnedPidKey = "Tryll.ServerProcess.SpawnedPid";
        private const string SpawnedStartKey = "Tryll.ServerProcess.SpawnedStartUtcTicks";
        private const string SpawnedPortKey = "Tryll.ServerProcess.SpawnedPort";
        private const string SpawnedExeKey = "Tryll.ServerProcess.SpawnedExe";

        private static void RememberSpawned(Process proc, int port, string exe)
        {
            long startTicks;
            try { startTicks = proc.StartTime.ToUniversalTime().Ticks; }
            catch (Exception) { ForgetSpawned(); return; }
            RememberSpawned(proc.Id, startTicks, port, exe);
        }

        private static void RememberSpawned(int pid, long startUtcTicks, int port, string exe)
        {
            UnityEditor.SessionState.SetInt(SpawnedPidKey, pid);
            UnityEditor.SessionState.SetString(SpawnedStartKey,
                startUtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture));
            UnityEditor.SessionState.SetInt(SpawnedPortKey, port);
            UnityEditor.SessionState.SetString(SpawnedExeKey, exe ?? "");
        }

        private static void ForgetSpawned()
        {
            UnityEditor.SessionState.EraseInt(SpawnedPidKey);
            UnityEditor.SessionState.EraseString(SpawnedStartKey);
            UnityEditor.SessionState.EraseInt(SpawnedPortKey);
            UnityEditor.SessionState.EraseString(SpawnedExeKey);
        }

        /// <summary>
        /// Take back the handle of the server this editor spawned after a domain reload cleared
        /// it. Only the remembered PID counts, and only while it is still that process: same
        /// name, start time and exe. A dead or reused PID is forgotten, so a process this
        /// editor did not spawn is never adopted or killed. <paramref name="port"/> 0 and
        /// <paramref name="exe"/> null match any remembered port and exe.
        /// </summary>
        private static bool TryReattachSpawned(int port, string exe)
        {
            if (s_process != null)
                return false;
            int pid = UnityEditor.SessionState.GetInt(SpawnedPidKey, 0);
            if (pid <= 0)
                return false;
            string rememberedExe = UnityEditor.SessionState.GetString(SpawnedExeKey, "");
            // Another port or exe: the remembered server is not the one asked about. Keep it,
            // so Kill can still stop it.
            if (port > 0 && UnityEditor.SessionState.GetInt(SpawnedPortKey, 0) != port)
                return false;
            if (exe != null && !SamePath(exe, rememberedExe))
                return false;
            long.TryParse(UnityEditor.SessionState.GetString(SpawnedStartKey, ""),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out long startTicks);

            Process proc = null;
            string mismatch;
            try
            {
                proc = Process.GetProcessById(pid);
                mismatch = SpawnedProcessMismatch(proc, startTicks, rememberedExe);
            }
            catch (Exception ex)
            {
                mismatch = "not running (" + ex.Message + ")";
            }
            if (mismatch != null)
            {
                try { proc?.Dispose(); } catch { /* best-effort */ }
                ForgetSpawned();
                TryllLog.Info($"TryllServerProcess: not taking back PID {pid}: {mismatch}.");
                return false;
            }
            s_process = proc;
            TryllLog.Info($"TryllServerProcess: took back PID {pid} after a domain reload.");
            return true;
        }

        /// <summary>
        /// Null when <paramref name="proc"/> is alive and is the process started at
        /// <paramref name="startUtcTicks"/> from <paramref name="exe"/>, else why not. PID and
        /// start time are the identity: a reused PID belongs to a process with a later start
        /// time. The name and image path must match too whenever Windows lets us read them
        /// (it may not while a process is still starting).
        /// </summary>
        private static string SpawnedProcessMismatch(Process proc, long startUtcTicks, string exe)
        {
            if (string.IsNullOrEmpty(exe) || startUtcTicks <= 0)
                return "no start time or exe was remembered";
            try
            {
                if (proc.HasExited)
                    return "it has exited";
                long ticks = proc.StartTime.ToUniversalTime().Ticks;
                if (Math.Abs(ticks - startUtcTicks) > SpawnedStartToleranceTicks)
                    return "it started at another time";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            string name = null;
            try { name = proc.ProcessName; }
            catch (Exception) { /* not readable yet: PID and start time decide */ }
            if (!string.IsNullOrEmpty(name))
            {
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 4);
                if (!string.Equals(name, Path.GetFileNameWithoutExtension(exe), StringComparison.OrdinalIgnoreCase))
                    return $"its name is '{name}'";
            }

            string image = null;
            try { image = proc.MainModule?.FileName; }
            catch (Exception) { /* not readable: PID and start time decide */ }
            if (string.IsNullOrEmpty(image) || !Path.IsPathRooted(image) || SamePath(image, exe))
                return null;
            return $"its exe is '{image}'";
        }

        /// <summary>Both start times come from the same kernel value; this only absorbs rounding.</summary>
        private const long SpawnedStartToleranceTicks = 10 * TimeSpan.TicksPerMillisecond;

        private static bool SamePath(string a, string b)
            => string.Equals(TryllPathUtil.Canonicalize(a), TryllPathUtil.Canonicalize(b),
                StringComparison.OrdinalIgnoreCase);

        /// <summary>Test seam: remember a process as the one this editor spawned.</summary>
        internal static void DebugRememberSpawnedForTests(int pid, long startUtcTicks, int port, string exe)
            => RememberSpawned(pid, startUtcTicks, port, exe);

        /// <summary>Test seam: the remembered spawned PID, or 0.</summary>
        internal static int DebugRememberedSpawnedPidForTests
            => UnityEditor.SessionState.GetInt(SpawnedPidKey, 0);

        /// <summary>Test seam: drop the handle the way a domain reload does, keeping SessionState.</summary>
        internal static void DebugSimulateDomainReloadForTests()
        {
            s_process = null;
            s_observedReady = false;
        }

        /// <summary>Test seam: forget the remembered spawned process.</summary>
        internal static void DebugForgetSpawnedForTests() => ForgetSpawned();
#endif

        /// <summary>
        /// Absolute path to the bundled <c>tryll_server.exe</c>. The <paramref name="settings"/>
        /// argument is accepted for call-site compatibility but is no longer used — the path is
        /// always derived from the package payload.
        /// </summary>
        public static string ResolveExePath(TryllRuntimeSettings settings)
        {
            string dir = ResolveServerDir(settings);
            return string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, ExeName);
        }

        /// <summary>
        /// Directory containing <c>tryll_server.exe</c> and its <c>data/</c> payload.
        /// Editor: see <see cref="EditorServerDir"/> (bundled <c>.Server/&lt;variant&gt;</c>,
        /// else the per-user install). Build: <c>Server/</c> next to the player exe (the
        /// post-processor flattens the chosen payload into it).
        /// </summary>
        public static string ResolveServerDir(TryllRuntimeSettings settings)
        {
#if UNITY_EDITOR
            return EditorServerDir(settings);
#else
            // Standalone build: payload copied next to the player exe by the build post-processor.
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", BuildServerDir));
#endif
        }
#else
        public static bool IsRunning => false;
        public static int  Pid       => -1;
        public static int  ActualPort => 0;
        public static bool TryLaunch(TryllRuntimeSettings settings, out string error)
        { error = "TryllServerProcess: only supported on Windows"; return false; }
        public static void ReleaseHandle() { }
        public static void Kill() { }
        public static string ResolveExePath(TryllRuntimeSettings settings) => null;
        public static string ResolveServerDir(TryllRuntimeSettings settings) => null;
#endif
    }
}
