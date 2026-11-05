// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Keeps only the agents currently in use holding language-model KV cache, evicting the rest to
    /// reclaim VRAM.
    ///
    /// <para><b>This is a memory-for-latency trade, and it should be opt-in.</b> With every agent's
    /// contexts resident, switching between agents is nearly free. Evicted, the first message to an
    /// agent also pays context allocation and a full prefill — on the order of a second or two — in
    /// exchange for a large VRAM saving. That is the right trade on a GPU that would otherwise not fit
    /// and the wrong one on a GPU with headroom, which is why <see cref="Enabled"/> exists rather than
    /// this being unconditional. Drive it from a graphics-quality or "low VRAM" setting.</para>
    ///
    /// <para><b>Why this is in the plugin rather than in your game.</b> Nothing here is game-specific,
    /// and every part of it is a mistake waiting to be made: forgetting the debounce turns a click
    /// through a character list into one uncancellable prefill per entry; forgetting the hot set
    /// punishes the most common interaction pattern there is; and forgetting that cache operations are
    /// idle-only produces intermittent <c>AgentBusy</c> failures that look like server bugs.</para>
    ///
    /// <para><b>Correctness never depends on this.</b> The server transparently restores an evicted
    /// context before the next send, so a badly tuned policy costs time to first token, not behaviour.
    /// That is what makes it safe to tune against a real profile.</para>
    ///
    /// <para><b>Usage.</b> Add to any GameObject, call <see cref="Register"/> for each agent that
    /// should participate (or <see cref="RegisterAll"/>), and call <see cref="SetActive"/> whenever the
    /// player's focus moves. Agents that must always stay warm — a background summariser that runs
    /// after every turn, say — should simply not be registered: evicting one guarantees a restore on
    /// its next turn, which is strictly worse than never evicting it.</para>
    ///
    /// <para>For creation-time behaviour, pair this with
    /// <see cref="TryllAgentComponent.KvCacheInitialization"/> = <c>DeferAllocation</c> on the agents
    /// it manages. That is where most of the saving comes from: a context costs its full
    /// <c>context_size</c> the moment it is created, so never allocating it is worth more than
    /// evicting it later.</para>
    /// </summary>
    [AddComponentMenu("Tryll/Tryll KV Residency Policy")]
    public sealed class TryllKvResidencyPolicy : MonoBehaviour
    {
        /// <summary>
        /// Whether the policy evicts at all. When false, nothing is evicted and anything already
        /// evicted is restored, so toggling this at runtime is safe in both directions.
        ///
        /// <para>Note that agents created with <c>DeferAllocation</c> stay that way — creation mode is
        /// structural. Turning the policy off mid-session restores what it evicted; it cannot undo an
        /// allocation that never happened.</para>
        /// </summary>
        [Tooltip("Off = every registered agent stays resident. Toggling at runtime is safe.")]
        public bool Enabled = true;

        /// <summary>
        /// Temporarily stops new prefill and eviction work while continuing to flush queued
        /// messages. Use around save/restore transactions that require every agent lifecycle-idle.
        /// Calls to <see cref="SetActive"/> still update recency while suspended, so resuming acts on
        /// the latest selection.
        /// </summary>
        public bool SchedulingSuspended { get; set; }

        /// <summary>
        /// How many agents keep their contexts resident, including the active one.
        ///
        /// <para>Two is the useful default: "go back to the one I was just using" is the commonest
        /// pattern, and a hot set of one punishes it with a full restore every time. One maximises the
        /// saving; raise it if switching is frequent and VRAM allows.</para>
        /// </summary>
        [Tooltip("Agents kept resident, including the active one. Lower saves more VRAM.")]
        [Range(1, 8)]
        public int HotSetSize = 2;

        /// <summary>
        /// Seconds an agent must stay active before its prefill starts.
        ///
        /// <para>Not cosmetic: a prefill cannot be cancelled once started, so the only way to avoid
        /// paying for agents the player merely passed over is not to start. Set to 0 only if selection
        /// is deliberate and cannot be scrubbed through.</para>
        /// </summary>
        [Tooltip("Seconds a selection must hold before prefill starts. Absorbs scrubbing through a list.")]
        [Range(0f, 2f)]
        public float PrefillDebounceSeconds = 0.35f;

        /// <summary>Seconds to back off after an operation was refused with <c>AgentBusy</c>.</summary>
        [Tooltip("Back-off after an AgentBusy refusal.")]
        [Range(0.1f, 5f)]
        public float BusyRetrySeconds = 0.5f;

        private readonly List<TryllAgentComponent> _agents = new List<TryllAgentComponent>();

        /// <summary>Most-recently-active first. The first <see cref="HotSetSize"/> entries stay resident.</summary>
        private readonly List<TryllAgentComponent> _recency = new List<TryllAgentComponent>();

        /// <summary>Agents believed to hold contexts, so repeat work is skipped.</summary>
        private readonly HashSet<TryllAgentComponent> _resident = new HashSet<TryllAgentComponent>();

        /// <summary>Agents with an operation in flight, so a second is not issued.</summary>
        private readonly HashSet<TryllAgentComponent> _inFlight = new HashSet<TryllAgentComponent>();

        private TryllAgentComponent _pendingActive;
        private float _pendingSince;
        private float _retryAfter;

        /// <summary>The agent most recently passed to <see cref="SetActive"/>, or null.</summary>
        public TryllAgentComponent ActiveAgent => _recency.Count > 0 ? _recency[0] : null;

        /// <summary>
        /// Registers an agent. <paramref name="startsResident"/> should be false when the agent was
        /// created with <c>DeferAllocation</c> (nothing is allocated yet) and true otherwise.
        /// Re-registering an agent is a no-op.
        /// </summary>
        public void Register(TryllAgentComponent agent, bool startsResident)
        {
            if (agent == null || _agents.Contains(agent)) return;
            _agents.Add(agent);
            if (startsResident) _resident.Add(agent);
        }

        /// <summary>Registers every agent component in the scene. Convenience for simple setups.</summary>
        public void RegisterAll(bool startResident)
        {
            foreach (var agent in FindObjectsByType<TryllAgentComponent>(FindObjectsSortMode.None))
                Register(agent, startResident);
        }

        /// <summary>Stops managing an agent. Does not evict or restore it.</summary>
        public void Unregister(TryllAgentComponent agent)
        {
            if (agent == null) return;
            _agents.Remove(agent);
            _recency.Remove(agent);
            _resident.Remove(agent);
            _inFlight.Remove(agent);
            if (_pendingActive == agent) _pendingActive = null;
        }

        /// <summary>
        /// Records that an external operation successfully prefetched a registered agent.
        /// This avoids a duplicate policy prefill after a restore/loading-screen warm-up.
        /// </summary>
        public void MarkResident(TryllAgentComponent agent)
        {
            if (agent != null && _agents.Contains(agent))
                _resident.Add(agent);
        }

        /// <summary>
        /// Tells the policy which agent the player is now interacting with. Safe to call on every
        /// selection, including repeats — the debounce and the resident set absorb the noise.
        /// </summary>
        public void SetActive(TryllAgentComponent agent)
        {
            if (agent == null) return;

            // Recency is maintained even while disabled, so enabling mid-session has a meaningful hot
            // set to work from instead of evicting whatever happens to be first in the list.
            _recency.Remove(agent);
            _recency.Insert(0, agent);

            _pendingActive = agent;
            _pendingSince = Time.unscaledTime;
        }

        private void Update()
        {
            // Runs regardless of Enabled: a send can be refused for reasons unrelated to this policy,
            // and a swallowed message is the worst outcome available.
            for (var i = 0; i < _agents.Count; i++)
                if (_agents[i] != null) _agents[i].TryFlushPendingMessage();

            if (SchedulingSuspended || !Enabled || _agents.Count == 0) return;
            if (Time.unscaledTime < _retryAfter) return;

            if (_pendingActive != null &&
                Time.unscaledTime - _pendingSince >= PrefillDebounceSeconds)
            {
                var target = _pendingActive;
                _pendingActive = null;
                EnsureResident(target);
                EvictCold();
            }
        }

        /// <summary>Prefills <paramref name="agent"/> unless it is already resident or busy.</summary>
        private void EnsureResident(TryllAgentComponent agent)
        {
            if (agent == null || !agent.HasAgent) return;
            if (_resident.Contains(agent) || _inFlight.Contains(agent)) return;

            _inFlight.Add(agent);
            agent.Agent.PrefillKvCache((result, error) =>
            {
                _inFlight.Remove(agent);
                if (agent != null) agent.TryFlushPendingMessage();

                if (error.IsOk) { _resident.Add(agent); return; }
                if (error.Code == (int)TryllErrorCode.AgentBusy) { Defer(); return; }

                // Anything else is not actionable here: the next send restores the context anyway, so
                // report it and let that turn pay the cost rather than retrying blindly.
                TryllLog.Warning($"TryllKvResidencyPolicy: prefill failed for '{agent.name}': {error}");
            });
        }

        /// <summary>Evicts every resident agent outside the hot set.</summary>
        private void EvictCold()
        {
            for (var i = 0; i < _agents.Count; i++)
            {
                var agent = _agents[i];
                if (agent == null || !agent.HasAgent) continue;

                var hot = false;
                var hotCount = Mathf.Min(HotSetSize, _recency.Count);
                for (var j = 0; j < hotCount; j++)
                {
                    if (_recency[j] != agent) continue;
                    hot = true;
                    break;
                }
                if (hot) continue;

                if (!_resident.Contains(agent) || _inFlight.Contains(agent)) continue;

                // A queued message is about to need this cache — evicting now would make the player
                // wait for a restore of exactly what we just threw away.
                if (agent.HasPendingMessage) continue;

                _inFlight.Add(agent);
                agent.Agent.EvictKvCache((result, error) =>
                {
                    _inFlight.Remove(agent);
                    if (error.IsOk) { _resident.Remove(agent); return; }
                    if (error.Code == (int)TryllErrorCode.AgentBusy) { Defer(); return; }
                    TryllLog.Warning($"TryllKvResidencyPolicy: evict failed for '{agent.name}': {error}");
                });
            }
        }

        /// <summary>Backs off after an <c>AgentBusy</c> and re-arms, so deferred work is retried.</summary>
        private void Defer()
        {
            _retryAfter = Time.unscaledTime + BusyRetrySeconds;
            if (_pendingActive == null && _recency.Count > 0)
            {
                _pendingActive = _recency[0];
                _pendingSince = Time.unscaledTime;
            }
        }

        /// <summary>
        /// Restores every registered agent. Call after setting <see cref="Enabled"/> to false so nobody
        /// is left paying restore costs for a policy that is no longer running.
        /// </summary>
        public void RestoreAll()
        {
            foreach (var agent in _agents)
                EnsureResident(agent);
        }
    }
}
