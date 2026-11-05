// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tryll.Client
{
    /// <summary>
    /// What the tracker needs from a live agent. <see cref="TryllAgent"/>
    /// implements it; tests use a fake.
    /// </summary>
    internal interface ITryllLocalizedTarget
    {
        /// <summary>Table name passed to the provider with every Text ID.</summary>
        string TextTable { get; }

        /// <summary>Asset name used in Text ID errors.</summary>
        string TextAssetName { get; }

        /// <summary>Nodes that have a params baseline.</summary>
        IReadOnlyList<string> BaselineNodeNames { get; }

        /// <summary>A deep copy of the node's current params, or null.</summary>
        TryllNodeParamsBase GetNodeParamsBaseline(string nodeName);

        /// <summary>
        /// Send already-resolved params. Does not resolve Text IDs again and
        /// does not report <c>AgentBusy</c> as a client error.
        /// </summary>
        void SendResolvedParams(string nodeName, TryllNodeParamsBase parms, Action<TryllError> onComplete);
    }

    /// <summary>
    /// Re-resolves each live agent's ticked, non-structural fields when the
    /// provider's language changes. Always starts from the agent's current
    /// params baseline, so runtime <c>ChangeParams</c> edits survive. One agent
    /// is all or nothing: if one field fails, nothing is sent for that agent.
    /// Once every update has a reply, the agent gets one outcome: failed if the
    /// server rejected any update, applied otherwise. <c>AgentBusy</c> marks the
    /// agent dirty and holds the outcome; the next turn end refreshes it.
    /// </summary>
    internal sealed class TryllLocalizedAgentTracker
    {
        sealed class Binding
        {
            public bool Dirty;
            public int Generation;
        }

        readonly Dictionary<ITryllLocalizedTarget, Binding> _byTarget
            = new Dictionary<ITryllLocalizedTarget, Binding>();

        public void Track(ITryllLocalizedTarget target)
        {
            if (target == null || _byTarget.ContainsKey(target)) return;
            _byTarget[target] = new Binding();
        }

        public bool IsTracked(ITryllLocalizedTarget target)
            => target != null && _byTarget.ContainsKey(target);

        public void Forget(ITryllLocalizedTarget target)
        {
            if (target != null) _byTarget.Remove(target);
        }

        public void ForgetAll() => _byTarget.Clear();

        /// <summary>Refresh this agent at its next turn end.</summary>
        public void MarkDirty(ITryllLocalizedTarget target)
        {
            if (target != null && _byTarget.TryGetValue(target, out var binding))
                binding.Dirty = true;
        }

        public bool HasPending(ITryllLocalizedTarget target)
            => target != null && _byTarget.TryGetValue(target, out var binding) && binding.Dirty;

        /// <summary>Refresh every tracked agent.</summary>
        public async Task RefreshAllAsync(
            ITryllTextProvider provider,
            Action<ITryllLocalizedTarget, TryllError> onFailed,
            Action<ITryllLocalizedTarget> onApplied)
        {
            if (provider == null || _byTarget.Count == 0) return;
            var targets = new List<ITryllLocalizedTarget>(_byTarget.Keys);
            foreach (var target in targets)
                await RefreshAsync(target, provider, onFailed, onApplied);
        }

        /// <summary>
        /// A turn ended. If an update came back <c>AgentBusy</c>, refresh that
        /// agent again from its current baseline.
        /// </summary>
        public Task NoteTurnEndedAsync(
            ITryllLocalizedTarget target,
            ITryllTextProvider provider,
            Action<ITryllLocalizedTarget, TryllError> onFailed,
            Action<ITryllLocalizedTarget> onApplied)
        {
            if (!HasPending(target)) return Task.CompletedTask;
            return RefreshAsync(target, provider, onFailed, onApplied);
        }

        /// <summary>Refresh one agent from its current params baseline.</summary>
        public async Task RefreshAsync(
            ITryllLocalizedTarget target,
            ITryllTextProvider provider,
            Action<ITryllLocalizedTarget, TryllError> onFailed,
            Action<ITryllLocalizedTarget> onApplied)
        {
            if (provider == null || target == null) return;
            if (!_byTarget.TryGetValue(target, out var binding)) return;
            binding.Dirty = false;
            int generation = ++binding.Generation;

            // Scratch graph of baseline copies. Nothing is sent until every
            // ticked field of every node resolved.
            var graph = new TryllGraphDescription
            {
                TextTable = target.TextTable ?? "",
                Nodes = new List<TryllNodeDescription>(),
            };
            var names = target.BaselineNodeNames;
            if (names != null)
            {
                foreach (var name in names)
                {
                    var copy = target.GetNodeParamsBaseline(name);
                    if (copy == null || !TryllTextResolver.HasTickedFields(copy)) continue;
                    graph.Nodes.Add(new TryllNodeDescription { Name = name, Params = copy });
                }
            }
            if (graph.Nodes.Count == 0)
            {
                onApplied?.Invoke(target);
                return;
            }

            TryllTextResolveResult result;
            try
            {
                result = await TryllTextResolver.ResolveAsync(
                    graph, provider, target.TextAssetName, liveOnly: true);
            }
            catch (Exception ex)
            {
                result = new TryllTextResolveResult(
                    new TryllError(TryllErrorCode.InvalidParamValue,
                        $"Localized text: provider failed: {ex.GetBaseException().Message}"),
                    true, null);
            }

            // Forgotten, or a newer refresh of this agent started while we waited.
            if (!_byTarget.TryGetValue(target, out var current) || current != binding
                || generation != binding.Generation)
                return;

            if (!result.Error.IsOk)
            {
                TryllLog.Warning(result.Error.Message);
                onFailed?.Invoke(target, result.Error);
                return;
            }

            if (result.LiveNodes.Count == 0)
            {
                onApplied?.Invoke(target);
                return;
            }

            int remaining = result.LiveNodes.Count;
            bool busy = false;
            var firstError = TryllError.Ok();
            foreach (var node in result.LiveNodes)
            {
                string nodeName = node.NodeName;
                target.SendResolvedParams(nodeName, node.Params, error =>
                {
                    if (error.Code == (int)TryllErrorCode.AgentBusy)
                    {
                        if (_byTarget.TryGetValue(target, out var b)) b.Dirty = true;
                        busy = true;
                    }
                    else if (!error.IsOk)
                    {
                        TryllLog.Warning(
                            $"Localized text: node '{nodeName}' was not updated: {error.Message}");
                        if (firstError.IsOk) firstError = error;
                    }
                    if (--remaining > 0 || busy) return;
                    if (!_byTarget.TryGetValue(target, out var latest) || latest != binding
                        || generation != binding.Generation)
                        return;
                    if (!firstError.IsOk)
                    {
                        onFailed?.Invoke(target, firstError);
                        return;
                    }
                    onApplied?.Invoke(target);
                });
            }
        }
    }
}
