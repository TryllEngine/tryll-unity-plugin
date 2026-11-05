// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Feature-local Memory presentation core: formatters, stale-state reducers and
    /// row projections. No UI Toolkit or editor-lifecycle dependency.
    /// </summary>
    internal static class TryllMemoryPresentation
    {
        public struct State
        {
            public TryllMemorySnapshot Snapshot;
            public string StaleError;
            public bool RefreshInFlight;
        }

        public static string FmtBinaryBytes(ulong bytes)
        {
            const double Kib = 1024.0;
            const double Mib = Kib * 1024.0;
            const double Gib = Mib * 1024.0;
            if (bytes >= Gib) return (bytes / Gib).ToString("0.00", CultureInfo.InvariantCulture) + " GiB";
            if (bytes >= Mib) return (bytes / Mib).ToString("0.0", CultureInfo.InvariantCulture) + " MiB";
            if (bytes >= Kib) return (bytes / Kib).ToString("0.0", CultureInfo.InvariantCulture) + " KiB";
            return bytes.ToString(CultureInfo.InvariantCulture) + " B";
        }

        public static string FmtClock(long unixMs)
        {
            if (unixMs <= 0) return "—";
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).ToLocalTime();
            return dt.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        public static string FmtIdle(long lastTurnAtMs, long nowMs)
        {
            if (lastTurnAtMs <= 0) return "never run";
            double s = Math.Max(0, (nowMs - lastTurnAtMs) / 1000.0);
            if (s < 1) return "now";
            if (s < 60) return $"{s:0}s ago";
            if (s < 3600) return $"{s / 60:0}m ago";
            return $"{s / 3600:0.0}h ago";
        }

        public static int ClassOrder(TryllInferenceConsumerClass cls) => cls switch
        {
            TryllInferenceConsumerClass.Background          => 0,
            TryllInferenceConsumerClass.InteractiveBuffered => 1,
            TryllInferenceConsumerClass.InteractiveVisible  => 2,
            _ => 9,
        };

        public static string ConsumerClassLabel(TryllInferenceConsumerClass cls, bool resident)
        {
            string label = cls switch
            {
                TryllInferenceConsumerClass.InteractiveVisible  => "visible",
                TryllInferenceConsumerClass.InteractiveBuffered => "buffered",
                TryllInferenceConsumerClass.Background          => "background",
                _ => "",
            };
            if (!resident) label = string.IsNullOrEmpty(label) ? "evicted" : label + " · evicted";
            return label;
        }

        public static string FormatRamRange(ulong ramBytes, ulong ramBytesHigh, TryllMemoryTier tier)
        {
            string ram = ramBytesHigh > 0
                ? $"~{FmtBinaryBytes(ramBytes)} – {FmtBinaryBytes(ramBytesHigh)}"
                : FmtBinaryBytes(ramBytes);
            if (tier == TryllMemoryTier.Estimated && ramBytesHigh == 0)
                ram = "~" + ram;
            return ram;
        }

        public static string ContextCaption(uint usedTokens, uint capacityTokens, bool occupancyStale)
        {
            if (capacityTokens == 0) return "—";
            float frac = capacityTokens == 0 ? 0f : Math.Clamp(usedTokens / (float)capacityTokens, 0f, 1f);
            string caption = $"{usedTokens} / {capacityTokens} ({frac * 100f:0}%)";
            if (occupancyStale) caption += "  stale";
            return caption;
        }

        public static string StaleBannerText(TryllMemorySnapshot snapshot, string staleError)
        {
            if (string.IsNullOrEmpty(staleError)) return null;
            if (snapshot != null)
                return $"Showing a stale snapshot from {FmtClock(snapshot.CapturedAtMs)}.\n{staleError}";
            return staleError;
        }

        public static State ApplyRefreshStarted(State state)
        {
            state.RefreshInFlight = true;
            return state;
        }

        public static State ApplyRefreshSucceeded(State state, TryllMemorySnapshot snapshot)
        {
            state.RefreshInFlight = false;
            state.StaleError = null;
            state.Snapshot = snapshot;
            return state;
        }

        public static State ApplyRefreshFailed(State state, string error)
        {
            state.RefreshInFlight = false;
            state.StaleError = string.IsNullOrEmpty(error) ? "Refresh failed" : error;
            return state;
        }

        public static State ApplyDisconnected(State state, string reason)
        {
            if (state.Snapshot != null && string.IsNullOrEmpty(state.StaleError))
                state.StaleError = string.IsNullOrEmpty(reason) ? "Disconnected" : reason;
            return state;
        }

        public enum Tone : byte { Dim, Ok, Accent, Warn, Err }

        public enum DeviceSection : byte { Uninitialized, Empty, Table }

        public const string UnattributedMappedNote =
            "Most of the working set is unattributed. Memory-mapped model files count as resident pages but are not allocations we make, so a VRAM-placed model shows up here rather than under its own row.";
        public const string DevicesUninitializedNote =
            "No backend initialised yet — devices register on the first model load. This is not an error.";
        public const string DevicesEmptyNote =
            "Backend is initialised but no devices were reported.";
        public const string DevicesFooterNote =
            "Other is everything on the device that is not Tryll — the editor, the compositor, other processes. The editor's own VRAM cannot be split out of that number.";
        public const string ContextHeadroomNote =
            "Context used is headroom to the limit, not memory growth: the KV buffer is allocated for the full context size when the agent is created and does not grow with the conversation.";
        public const string EmptyKvNote = "No language-model contexts yet.";

        public struct DeviceRow
        {
            public string Label;
            public string Kind;
            public string Self;
            public string Other;
            public string Free;
            public string Total;
        }

        public struct KvRow
        {
            public string Name;
            public string ClassLabel;
            public Tone ClassTone;
            public bool IsBackground;
            public bool OccupancyStale;
            public uint UsedTokens;
            public uint CapacityTokens;
            public float OccupancyFrac;
            public string ContextCaption;
            public string When;
            public string Reserved;
        }

        public struct ComponentRow
        {
            public string Kind;
            public string Name;
            public string TierLabel;
            public Tone TierTone;
            public string Ram;
            public bool RamWarn;
            public string Vram;
            public string Tooltip;
        }

        public sealed class Tables
        {
            public List<string> SystemNotes = new List<string>();
            public DeviceSection DevicesKind;
            public List<DeviceRow> Devices = new List<DeviceRow>();
            public string KvTotalLabel = "0 B";
            public string BackgroundKvNote;
            public List<KvRow> KvRows = new List<KvRow>();
            public List<ComponentRow> ComponentRows = new List<ComponentRow>();
        }

        public static Tone ClassTone(TryllInferenceConsumerClass cls, bool resident)
        {
            if (!resident) return Tone.Err;
            return cls switch
            {
                TryllInferenceConsumerClass.InteractiveVisible => Tone.Ok,
                TryllInferenceConsumerClass.InteractiveBuffered => Tone.Accent,
                TryllInferenceConsumerClass.Background => Tone.Warn,
                _ => Tone.Dim,
            };
        }

        public static (string Label, Tone Tone) TierPresentation(TryllMemoryTier tier)
        {
            return tier switch
            {
                TryllMemoryTier.Measured => ("measured", Tone.Ok),
                TryllMemoryTier.Derived => ("derived", Tone.Accent),
                TryllMemoryTier.Estimated => ("estimated", Tone.Warn),
                _ => (tier.ToString().ToLowerInvariant(), Tone.Dim),
            };
        }

        public static string DisplayName(string name) =>
            string.IsNullOrEmpty(name) ? "—" : name;

        public static string DeviceLabel(string description, string name)
        {
            if (!string.IsNullOrEmpty(description)) return description;
            if (!string.IsNullOrEmpty(name)) return name;
            return "—";
        }

        public static int CompareKv(TryllMemoryComponent a, TryllMemoryComponent b)
        {
            int oa = ClassOrder(a.ConsumerClass);
            int ob = ClassOrder(b.ConsumerClass);
            if (oa != ob) return oa.CompareTo(ob);
            ulong ba = a.RamBytes + a.VramBytes;
            ulong bb = b.RamBytes + b.VramBytes;
            return bb.CompareTo(ba);
        }

        public static Tables Project(TryllMemorySnapshot snapshot, long nowMs)
        {
            var tables = new Tables();
            if (snapshot == null)
                return tables;

            TryllMemoryComponent residual = null;
            var kv = new List<TryllMemoryComponent>();
            bool anyModelResident = false;
            if (snapshot.Components != null)
            {
                foreach (var c in snapshot.Components)
                {
                    if (c == null) continue;
                    if (c.Owner == TryllMemoryOwnerKind.AgentNode)
                        kv.Add(c);
                    else if (c.Owner == TryllMemoryOwnerKind.Model
                        && (c.RamBytes > 0 || c.VramBytes > 0))
                    {
                        anyModelResident = true;
                    }
                    if (c.Owner == TryllMemoryOwnerKind.Unattributed && residual == null)
                        residual = c;
                    if (c.Owner != TryllMemoryOwnerKind.AgentNode)
                        tables.ComponentRows.Add(ProjectComponent(c));
                }
            }

            double residPct = snapshot.ProcessWorkingSetBytes > 0
                ? (snapshot.UnattributedRamBytes / (double)snapshot.ProcessWorkingSetBytes) * 100.0
                : 0;
            if (residPct > 50 && anyModelResident)
                tables.SystemNotes.Add(UnattributedMappedNote);
            if (residual != null && residual.Tier == TryllMemoryTier.Estimated)
                tables.SystemNotes.Add(
                    "Unattributed inherits Estimated because some components are estimates. "
                    + (residual.EstimateBasis ?? ""));

            if (!snapshot.BackendInitialized)
                tables.DevicesKind = DeviceSection.Uninitialized;
            else if (snapshot.Devices == null || snapshot.Devices.Count == 0)
                tables.DevicesKind = DeviceSection.Empty;
            else
            {
                tables.DevicesKind = DeviceSection.Table;
                foreach (var d in snapshot.Devices)
                {
                    if (d == null) continue;
                    tables.Devices.Add(new DeviceRow
                    {
                        Label = DeviceLabel(d.Description, d.Name),
                        Kind = string.IsNullOrEmpty(d.Kind) ? "—" : d.Kind,
                        Self = FmtBinaryBytes(d.SelfBytes),
                        Other = FmtBinaryBytes(d.OtherBytes),
                        Free = FmtBinaryBytes(d.FreeBytes),
                        Total = FmtBinaryBytes(d.TotalBytes),
                    });
                }
            }

            kv.Sort(CompareKv);
            ulong kvTotal = 0, bgTotal = 0;
            foreach (var c in kv)
            {
                ulong bytes = c.RamBytes + c.VramBytes;
                kvTotal += bytes;
                if (c.ConsumerClass == TryllInferenceConsumerClass.Background)
                    bgTotal += bytes;
                tables.KvRows.Add(ProjectKv(c, nowMs));
            }
            tables.KvTotalLabel = FmtBinaryBytes(kvTotal);
            if (bgTotal > 0)
                tables.BackgroundKvNote =
                    FmtBinaryBytes(bgTotal) + " of KV belongs to background work — nobody is waiting on it.";
            return tables;
        }

        static KvRow ProjectKv(TryllMemoryComponent c, long nowMs)
        {
            uint cap = c.CapacityTokens;
            float frac = cap == 0 ? 0f : Math.Clamp(c.UsedTokens / (float)cap, 0f, 1f);
            return new KvRow
            {
                Name = DisplayName(c.Name),
                ClassLabel = ConsumerClassLabel(c.ConsumerClass, c.Resident),
                ClassTone = ClassTone(c.ConsumerClass, c.Resident),
                IsBackground = c.ConsumerClass == TryllInferenceConsumerClass.Background,
                OccupancyStale = c.OccupancyStale,
                UsedTokens = c.UsedTokens,
                CapacityTokens = cap,
                OccupancyFrac = frac,
                ContextCaption = ContextCaption(c.UsedTokens, cap, c.OccupancyStale),
                When = FmtIdle(c.LastTurnAtMs, nowMs),
                Reserved = FmtBinaryBytes(c.RamBytes + c.VramBytes),
            };
        }

        static ComponentRow ProjectComponent(TryllMemoryComponent c)
        {
            var (tierLabel, tierTone) = TierPresentation(c.Tier);
            return new ComponentRow
            {
                Kind = DisplayName(c.Kind),
                Name = DisplayName(c.Name),
                TierLabel = tierLabel,
                TierTone = tierTone,
                Ram = FormatRamRange(c.RamBytes, c.RamBytesHigh, c.Tier),
                RamWarn = c.Tier == TryllMemoryTier.Estimated,
                Vram = FmtBinaryBytes(c.VramBytes),
                Tooltip = c.EstimateBasis ?? "",
            };
        }
    }
}
