// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Coroutine wrappers for the request/callback agent API, intended for save/load
    /// transactions. Every helper bounds its whole wait with a wall-clock deadline
    /// (<see cref="Time.unscaledTime"/>) so a dropped connection reports
    /// <see cref="TryllErrorCode.Timeout"/> instead of hanging the caller, and the
    /// idle-only dialog helpers retry <see cref="TryllErrorCode.AgentBusy"/> with a
    /// short delay instead of re-sending every frame.
    /// </summary>
    public static class TryllAgentCoroutines
    {
        /// <summary>Default wall-clock bound for one helper call.</summary>
        public const float DefaultTimeoutSeconds = 60f;

        /// <summary>Delay between AgentBusy retries.</summary>
        public const float BusyRetryDelaySeconds = 0.25f;

        /// <summary>
        /// <see cref="TryllAgent.ExportDialog"/> with AgentBusy retry and a deadline.
        /// On timeout the callback receives a null snapshot and <see cref="TryllErrorCode.Timeout"/>;
        /// if the agent is still busy at the deadline, the last AgentBusy error is reported instead.
        /// </summary>
        public static IEnumerator ExportDialog(
            TryllAgent agent,
            Action<TryllDialogSnapshot, TryllError> onComplete,
            float timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (agent == null)
            {
                onComplete?.Invoke(null, NoAgent());
                yield break;
            }

            var deadline = Time.unscaledTime + timeoutSeconds;
            while (true)
            {
                TryllDialogSnapshot snapshot = null;
                TryllError error = default;
                var finished = false;
                agent.ExportDialog((s, e) => { snapshot = s; error = e; finished = true; });
                yield return WaitUntilOrDeadline(() => finished, deadline);
                if (!finished)
                {
                    onComplete?.Invoke(null, Timeout(nameof(ExportDialog), timeoutSeconds));
                    yield break;
                }
                if (error.IsOk || error.Code != (int)TryllErrorCode.AgentBusy || Time.unscaledTime >= deadline)
                {
                    onComplete?.Invoke(snapshot, error);
                    yield break;
                }
                yield return BusyDelay(deadline);
            }
        }

        /// <summary>
        /// <see cref="TryllAgent.ImportDialog"/> with AgentBusy retry and a deadline.
        /// Same timeout semantics as <see cref="ExportDialog"/>.
        /// </summary>
        public static IEnumerator ImportDialog(
            TryllAgent agent,
            TryllDialogSnapshot snapshot,
            TryllDialogCompatibility compatibility,
            Action<TryllDialogImportResult, TryllError> onComplete,
            float timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (agent == null)
            {
                onComplete?.Invoke(null, NoAgent());
                yield break;
            }

            var deadline = Time.unscaledTime + timeoutSeconds;
            while (true)
            {
                TryllDialogImportResult result = null;
                TryllError error = default;
                var finished = false;
                agent.ImportDialog(snapshot, compatibility, (r, e) => { result = r; error = e; finished = true; });
                yield return WaitUntilOrDeadline(() => finished, deadline);
                if (!finished)
                {
                    onComplete?.Invoke(null, Timeout(nameof(ImportDialog), timeoutSeconds));
                    yield break;
                }
                if (error.IsOk || error.Code != (int)TryllErrorCode.AgentBusy || Time.unscaledTime >= deadline)
                {
                    onComplete?.Invoke(result, error);
                    yield break;
                }
                yield return BusyDelay(deadline);
            }
        }

        /// <summary>
        /// <see cref="TryllAgent.PrefillKvCache"/> bounded by a deadline. No AgentBusy retry —
        /// a warm-up that cannot start immediately is usually better skipped than queued.
        /// </summary>
        public static IEnumerator PrefillKvCache(
            TryllAgent agent,
            Action<TryllKvCachePrefillResult, TryllError> onComplete,
            float timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (agent == null)
            {
                onComplete?.Invoke(null, NoAgent());
                yield break;
            }

            var deadline = Time.unscaledTime + timeoutSeconds;
            TryllKvCachePrefillResult result = null;
            TryllError error = default;
            var finished = false;
            agent.PrefillKvCache((r, e) => { result = r; error = e; finished = true; });
            yield return WaitUntilOrDeadline(() => finished, deadline);
            if (!finished)
                onComplete?.Invoke(null, Timeout(nameof(PrefillKvCache), timeoutSeconds));
            else
                onComplete?.Invoke(result, error);
        }

        /// <summary>
        /// Flush locally staged variable writes, then refresh the local mirror from the
        /// server's authoritative values. Bounded by one shared deadline. Use before a
        /// save that captures variable values.
        /// </summary>
        public static IEnumerator SyncVariablesFromServer(
            TryllAgent agent,
            Action<TryllError> onComplete,
            float timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (agent == null)
            {
                onComplete?.Invoke(NoAgent());
                yield break;
            }

            var deadline = Time.unscaledTime + timeoutSeconds;
            TryllError error = default;
            var flushed = false;
            agent.Variables.FlushIfDirty(e => { error = e; flushed = true; });
            yield return WaitUntilOrDeadline(() => flushed, deadline);
            if (!flushed)
            {
                onComplete?.Invoke(Timeout(nameof(FlushVariables), timeoutSeconds));
                yield break;
            }
            if (!error.IsOk)
            {
                onComplete?.Invoke(error);
                yield break;
            }

            var synced = false;
            agent.Variables.SyncFromServerAsync(null, e => { error = e; synced = true; });
            yield return WaitUntilOrDeadline(() => synced, deadline);
            onComplete?.Invoke(synced ? error : Timeout(nameof(SyncVariablesFromServer), timeoutSeconds));
        }

        /// <summary>
        /// <see cref="TryllAgentVariables.FlushIfDirty(Action{TryllError})"/> bounded by a
        /// deadline. Use after restoring variable values on load.
        /// </summary>
        public static IEnumerator FlushVariables(
            TryllAgent agent,
            Action<TryllError> onComplete,
            float timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (agent == null)
            {
                onComplete?.Invoke(NoAgent());
                yield break;
            }

            var deadline = Time.unscaledTime + timeoutSeconds;
            TryllError error = default;
            var finished = false;
            agent.Variables.FlushIfDirty(e => { error = e; finished = true; });
            yield return WaitUntilOrDeadline(() => finished, deadline);
            onComplete?.Invoke(finished ? error : Timeout(nameof(FlushVariables), timeoutSeconds));
        }

        private static IEnumerator WaitUntilOrDeadline(Func<bool> condition, float deadline)
        {
            while (!condition() && Time.unscaledTime < deadline)
                yield return null;
        }

        private static IEnumerator BusyDelay(float deadline)
        {
            var until = Mathf.Min(Time.unscaledTime + BusyRetryDelaySeconds, deadline);
            while (Time.unscaledTime < until)
                yield return null;
        }

        private static TryllError NoAgent()
            => new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready");

        private static TryllError Timeout(string operation, float timeoutSeconds)
            => new TryllError(TryllErrorCode.Timeout,
                $"{operation} did not complete within {timeoutSeconds:0.#}s.");
    }
}
