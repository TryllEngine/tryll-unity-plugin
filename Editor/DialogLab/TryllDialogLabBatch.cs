// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Headless Dialog Lab: one agent against a Dialog Script or Suite for N seeds, for CI.
    ///
    /// <code>
    /// Unity.exe -batchmode -nographics -projectPath &lt;proj&gt; \
    ///   -executeMethod Tryll.Client.Editor.TryllDialogLabBatch.Run -tryllLabConfig &lt;run.json&gt;
    /// </code>
    ///
    /// Run it <b>without</b> <c>-quit</c>: the client pumps its socket from
    /// <c>EditorApplication.update</c>, which only ticks while the editor keeps running. The
    /// driver ends the process itself with exit code 0 (thresholds met), 1 (a threshold
    /// missed) or 2 (the run itself failed). It writes <c>&lt;run_id&gt;.jsonl</c> and
    /// <c>&lt;run_id&gt;.summary.json</c> into the config's <c>output_dir</c>.
    /// </summary>
    public static class TryllDialogLabBatch
    {
        const string ConfigArg = "-tryllLabConfig";
        const string LogPrefix = "[TryllDialogLab] ";

        /// <summary>The <c>-executeMethod</c> entry point. Never returns control to a caller that waits.</summary>
        public static void Run()
        {
            string path = ArgValue(Environment.GetCommandLineArgs(), ConfigArg);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"{LogPrefix}Pass the run config with {ConfigArg} <path>.");
                EditorApplication.Exit(TryllDialogLabBatchSummary.ExitRunFailed);
                return;
            }

            Task<int> task = RunCoreAsync(path);
            double deadline = double.MaxValue;
            void Watch()
            {
                if (task.IsCompleted)
                {
                    EditorApplication.update -= Watch;
                    int code = task.Status == TaskStatus.RanToCompletion ? task.Result : TryllDialogLabBatchSummary.ExitRunFailed;
                    if (task.IsFaulted) Debug.LogError($"{LogPrefix}{task.Exception?.GetBaseException()}");
                    EditorApplication.Exit(code);
                    return;
                }
                // Belt and braces over the turn watchdog: a batch job must never hang.
                if (deadline == double.MaxValue && SafetyTimeoutSeconds > 0)
                    deadline = EditorApplication.timeSinceStartup + SafetyTimeoutSeconds;
                if (EditorApplication.timeSinceStartup > deadline)
                {
                    EditorApplication.update -= Watch;
                    Debug.LogError($"{LogPrefix}Dialog Lab: ERROR, the run passed its {SafetyTimeoutSeconds:0} s safety limit (exit 2)");
                    EditorApplication.Exit(TryllDialogLabBatchSummary.ExitRunFailed);
                }
            }
            EditorApplication.update += Watch;
        }

        /// <summary>Set once the plan is known: turns x the turn timeout, plus startup room. 0 = none.</summary>
        static double SafetyTimeoutSeconds;

        /// <summary>
        /// Everything but the process exit, so tests can run it. Every failure becomes exit
        /// code 2 with a logged reason, and a summary file once the output folder is known.
        /// </summary>
        /// <param name="settingsOverride">Tests pass the fixture server's settings.</param>
        internal static async Task<int> RunCoreAsync(
            string configPath, TryllRuntimeSettings settingsOverride = null, Action<string> log = null)
        {
            log = log ?? LogLine;
            SafetyTimeoutSeconds = 0;

            string text;
            try { text = File.ReadAllText(configPath); }
            catch (Exception ex)
            {
                return Fail(log, null, $"Cannot read config '{configPath}': {ex.Message}");
            }
            if (!TryllDialogLabBatchConfig.TryParse(text, out var config, out var configErr))
                return Fail(log, null, $"{configPath}: {configErr}");

            var workflow = AssetDatabase.LoadAssetAtPath<TryllWorkflowAsset>(config.Workflow);
            if (workflow == null)
                return Fail(log, config, $"agent.workflow: no Tryll Workflow asset at '{config.Workflow}'.");
            var source = AssetDatabase.LoadAssetAtPath<ScriptableObject>(config.Dialogues);
            if (source == null)
                return Fail(log, config, $"dialogues: no asset at '{config.Dialogues}'.");
            var settings = settingsOverride;
            if (settings == null && !TryFindSettings(config.Settings, out settings, out var settingsErr))
                return Fail(log, config, settingsErr);

            return await RunLoadedAsync(config, workflow, source, settings, log);
        }

        /// <summary>The run itself, from loaded assets: tests call this with in-memory ones.</summary>
        internal static async Task<int> RunLoadedAsync(TryllDialogLabBatchConfig config,
            TryllWorkflowAsset workflow, ScriptableObject source, TryllRuntimeSettings settings, Action<string> log)
        {
            log = log ?? LogLine;
            if (!TryPrepare(config, workflow, source, settings, out var snapshot, out var prepareErr))
                return Fail(log, config, prepareErr);

            int turns = snapshot.TotalSteps * snapshot.Repeats;
            SafetyTimeoutSeconds = config.TurnTimeoutSeconds > 0 ? turns * config.TurnTimeoutSeconds + 300 : 0;
            log($"Running {snapshot.PlannedGeneratedTurns} generated turns: "
              + $"{snapshot.Dialogues.Count} dialogue(s) x {snapshot.Repeats} seed(s), agent '{config.AgentName}'.");

            var session = new TryllEditorSession(settings);
            var ready = new TaskCompletionSource<string>();
            session.SessionReady += () => ready.TrySetResult(null);
            session.StateChanged += (state, status) =>
            {
                if (state == TryllEditorSession.State.Error) ready.TrySetResult(status ?? "Session failed");
                else log(status);
            };

            TryllDialogLabRunResult result;
            try
            {
                session.Start();
                string sessionErr = await ready.Task;
                if (sessionErr != null)
                    return Fail(log, config, sessionErr);

                var runner = new TryllDialogLabRunner { TurnTimeoutSeconds = config.TurnTimeoutSeconds };
                runner.Progress += (done, total) =>
                {
                    if (done == total || done % 10 == 0) log($"{done}/{total} turns");
                };
                result = await runner.RunAsync(session.Client, snapshot);
            }
            finally
            {
                session.Stop();
            }

            var summary = TryllDialogLabBatchSummary.Build(result, config);
            string dir = OutputDirectory(config);
            try
            {
                Directory.CreateDirectory(dir);
                var noBom = new UTF8Encoding(false);
                File.WriteAllText(Path.Combine(dir, snapshot.RunId + ".jsonl"), TryllDialogLabExport.ExportToJsonl(result), noBom);
                File.WriteAllText(Path.Combine(dir, snapshot.RunId + ".summary.json"), summary.Json, noBom);
            }
            catch (Exception ex)
            {
                return Fail(log, null, $"Cannot write results to '{dir}': {ex.Message}");
            }
            log($"Results: {Path.Combine(dir, snapshot.RunId)}.jsonl / .summary.json");
            foreach (var line in summary.Lines) log(line);
            return summary.ExitCode;
        }

        /// <summary>Config to snapshot: assets loaded, variables typed, the same pre-run checks as the window.</summary>
        static bool TryPrepare(TryllDialogLabBatchConfig config, TryllWorkflowAsset workflow, ScriptableObject source,
            TryllRuntimeSettings settings, out TryllDialogLabRunSnapshot snapshot, out string error)
        {
            snapshot = null;
            if (string.IsNullOrEmpty(config.AgentName)) config.AgentName = workflow.name;
            if (!TryllDialogLabDialogues.TryResolve(source, out var scripts, out var sourceErr))
            {
                error = $"dialogues: {sourceErr}";
                return false;
            }

            if (!config.TryBuildOverrides(workflow.Variables, out var overrides, out error))
                return false;

            var scriptData = scripts.Select(TryllDialogScriptData.FromAsset).ToList();
            if (!TryllDialogLabRunValidation.TryValidateScripts(scriptData, out error))
                return false;
            var variant = new TryllDialogLabVariant { Label = config.AgentName, ModelOverride = config.Model };
            var runAgent = new TryllLabRunAgent
            {
                Name = config.AgentName,
                Graph = workflow.Graph,
                MaintainDialogueHistory = config.MaintainDialogueHistory,
            };
            if (!TryllDialogLabRunValidation.TryValidateVariant(variant, runAgent, scriptData, out error))
                return false;

            snapshot = TryllDialogLabRunSnapshot.CaptureFromSpec(
                settings,
                new TryllLabAgentSpec
                {
                    Workflow = workflow,
                    Name = config.AgentName,
                    VariableOverrides = overrides,
                    MaintainDialogueHistory = config.MaintainDialogueHistory,
                    Model = config.Model,
                },
                scripts, config.Seeds, config.BaseSeed);
            error = null;
            return true;
        }

        static bool TryFindSettings(string path, out TryllRuntimeSettings settings, out string error)
        {
            error = null;
            if (!string.IsNullOrEmpty(path))
            {
                settings = AssetDatabase.LoadAssetAtPath<TryllRuntimeSettings>(path);
                if (settings == null) error = $"settings: no TryllRuntimeSettings asset at '{path}'.";
                return settings != null;
            }
            var found = AssetDatabase.FindAssets("t:" + nameof(TryllRuntimeSettings))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .ToList();
            if (found.Count == 1)
            {
                settings = AssetDatabase.LoadAssetAtPath<TryllRuntimeSettings>(found[0]);
                return settings != null;
            }
            settings = null;
            error = found.Count == 0
                ? "settings: the project has no TryllRuntimeSettings asset."
                : $"settings: the project has {found.Count} TryllRuntimeSettings assets; name one ({string.Join(", ", found)}).";
            return false;
        }

        /// <summary>Relative output folders resolve against the project folder, not the editor's cwd.</summary>
        internal static string OutputDirectory(TryllDialogLabBatchConfig config)
        {
            string dir = string.IsNullOrEmpty(config.OutputDir) ? "Build/DialogLab" : config.OutputDir;
            return Path.IsPathRooted(dir) ? dir : Path.Combine(Directory.GetParent(Application.dataPath).FullName, dir);
        }

        /// <summary>A run that never produced results: log the verdict, and write the summary when there is a folder for it.</summary>
        static int Fail(Action<string> log, TryllDialogLabBatchConfig config, string reason)
        {
            var summary = TryllDialogLabBatchSummary.Build(null, config ?? new TryllDialogLabBatchConfig(), reason);
            if (config != null)
            {
                try
                {
                    string dir = OutputDirectory(config);
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "failed.summary.json"), summary.Json, new UTF8Encoding(false));
                }
                catch (Exception) { /* the log line below still carries the reason */ }
            }
            foreach (var line in summary.Lines) log(line);
            return summary.ExitCode;
        }

        /// <summary>One line per message, no stack trace: CI reads these in the job log.</summary>
        static void LogLine(string line) =>
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}{1}", LogPrefix, line);

        internal static string ArgValue(IReadOnlyList<string> args, string name)
        {
            for (int i = 0; i < args.Count - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
