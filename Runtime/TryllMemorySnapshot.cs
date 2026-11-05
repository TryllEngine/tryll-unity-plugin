// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using Tryll;

namespace Tryll.Client
{
    /// <summary>
    /// How much to trust a reported memory figure. Rendering a number without
    /// its tier is a client bug — an unmarked estimate will be compared against
    /// Task Manager and believed.
    /// </summary>
    public enum TryllMemoryTier : byte
    {
        Measured  = 0,
        Derived   = 1,
        Estimated = 2,
    }

    /// <summary>What owns a component's bytes. Weights belong to a model; KV to an agent × node.</summary>
    public enum TryllMemoryOwnerKind : byte
    {
        Process      = 0,
        Device       = 1,
        Model        = 2,
        AgentNode    = 3,
        Storage      = 4,
        Unattributed = 5,
    }

    /// <summary>
    /// Scheduler class of the last turn on a language-model context. Describes
    /// work, not the agent — always read alongside <see cref="TryllMemoryComponent.LastTurnAtMs"/>.
    /// </summary>
    public enum TryllInferenceConsumerClass : byte
    {
        Unspecified          = 0,
        InteractiveVisible   = 1,
        InteractiveBuffered  = 2,
        Background           = 3,
    }

    /// <summary>One memory space a component occupies (host RAM when <see cref="DeviceIndex"/> is -1).</summary>
    public sealed class TryllMemoryComponentDevice
    {
        public int    DeviceIndex;
        public ulong  Bytes;
        public ulong  ComputeBytes;
    }

    /// <summary>One row of the memory breakdown.</summary>
    public sealed class TryllMemoryComponent
    {
        public string                     Kind;
        public string                     Name;
        public TryllMemoryOwnerKind       Owner = TryllMemoryOwnerKind.Model;
        public TryllMemoryTier            Tier  = TryllMemoryTier.Measured;
        public string                     EstimateBasis;
        public ulong                      RamBytes;
        public ulong                      VramBytes;
        public ulong                      RamBytesHigh;
        public int                        DeviceIndex = -1;
        public List<TryllMemoryComponentDevice> PerDevice = new List<TryllMemoryComponentDevice>();
        public ulong                      SessionId;
        public ulong                      AgentId;
        public string                     NodeName;
        public uint                       UsedTokens;
        public uint                       CapacityTokens;
        public bool                       Resident = true;
        public bool                       OccupancyStale;
        public ulong                      ComputeBytes;
        public TryllInferenceConsumerClass ConsumerClass = TryllInferenceConsumerClass.Unspecified;
        public long                       LastTurnAtMs;
    }

    /// <summary>One device's memory situation.</summary>
    public sealed class TryllMemoryDevice
    {
        public int    Index = -1;
        public string Name;
        public string Description;
        public string Kind;
        public ulong  TotalBytes;
        public ulong  FreeBytes;
        public ulong  SelfModelBytes;
        public ulong  SelfContextBytes;
        public ulong  SelfComputeBytes;
        public ulong  OtherBytes;

        public ulong SelfBytes => SelfModelBytes + SelfContextBytes + SelfComputeBytes;
    }

    /// <summary>A complete memory picture at one instant, pulled over the wire.</summary>
    public sealed class TryllMemorySnapshot
    {
        public long   CapturedAtMs;
        public ulong  ProcessWorkingSetBytes;
        public ulong  ProcessPrivateBytes;
        public bool   ProcessAvailable;
        public bool   BackendInitialized;
        public ulong  UnattributedRamBytes;
        public List<TryllMemoryDevice>    Devices    = new List<TryllMemoryDevice>();
        public List<TryllMemoryComponent> Components = new List<TryllMemoryComponent>();

        internal static TryllMemorySnapshot FromWire(GetMemoryConsumptionResponse resp)
        {
            var snap = new TryllMemorySnapshot();
            if (!resp.Snapshot.HasValue)
                return snap;

            var s = resp.Snapshot.Value;
            snap.CapturedAtMs            = s.CapturedAtMs;
            snap.ProcessWorkingSetBytes  = s.ProcessWorkingSetBytes;
            snap.ProcessPrivateBytes     = s.ProcessPrivateBytes;
            snap.ProcessAvailable        = s.ProcessAvailable;
            snap.BackendInitialized      = s.BackendInitialized;
            snap.UnattributedRamBytes    = s.UnattributedRamBytes;

            for (int i = 0; i < s.DevicesLength; i++)
            {
                var d = s.Devices(i);
                if (d == null) continue;
                var dv = d.Value;
                snap.Devices.Add(new TryllMemoryDevice
                {
                    Index            = dv.Index,
                    Name             = dv.Name,
                    Description      = dv.Description,
                    Kind             = dv.Kind,
                    TotalBytes       = dv.TotalBytes,
                    FreeBytes        = dv.FreeBytes,
                    SelfModelBytes   = dv.SelfModelBytes,
                    SelfContextBytes = dv.SelfContextBytes,
                    SelfComputeBytes = dv.SelfComputeBytes,
                    OtherBytes       = dv.OtherBytes,
                });
            }

            for (int i = 0; i < s.ComponentsLength; i++)
            {
                var c = s.Components(i);
                if (c == null) continue;
                var cv = c.Value;
                var row = new TryllMemoryComponent
                {
                    Kind            = cv.Kind,
                    Name            = cv.Name,
                    Owner           = (TryllMemoryOwnerKind)cv.Owner,
                    Tier            = (TryllMemoryTier)cv.Tier,
                    EstimateBasis   = cv.EstimateBasis,
                    RamBytes        = cv.RamBytes,
                    VramBytes       = cv.VramBytes,
                    RamBytesHigh    = cv.RamBytesHigh,
                    DeviceIndex     = cv.DeviceIndex,
                    SessionId       = cv.SessionId,
                    AgentId         = cv.AgentId,
                    NodeName        = cv.NodeName,
                    UsedTokens      = cv.UsedTokens,
                    CapacityTokens  = cv.CapacityTokens,
                    Resident        = cv.Resident,
                    OccupancyStale  = cv.OccupancyStale,
                    ComputeBytes    = cv.ComputeBytes,
                    ConsumerClass   = (TryllInferenceConsumerClass)cv.ConsumerClass,
                    LastTurnAtMs    = cv.LastTurnAtMs,
                };
                for (int j = 0; j < cv.PerDeviceLength; j++)
                {
                    var p = cv.PerDevice(j);
                    if (p == null) continue;
                    var pv = p.Value;
                    row.PerDevice.Add(new TryllMemoryComponentDevice
                    {
                        DeviceIndex  = pv.DeviceIndex,
                        Bytes        = pv.Bytes,
                        ComputeBytes = pv.ComputeBytes,
                    });
                }
                snap.Components.Add(row);
            }

            return snap;
        }

        internal static TryllMemorySnapshot FromWire(GetMemoryConsumptionResponseT resp)
        {
            var snap = new TryllMemorySnapshot();
            var s = resp?.Snapshot;
            if (s == null)
                return snap;

            snap.CapturedAtMs           = s.CapturedAtMs;
            snap.ProcessWorkingSetBytes = s.ProcessWorkingSetBytes;
            snap.ProcessPrivateBytes    = s.ProcessPrivateBytes;
            snap.ProcessAvailable       = s.ProcessAvailable;
            snap.BackendInitialized     = s.BackendInitialized;
            snap.UnattributedRamBytes   = s.UnattributedRamBytes;

            if (s.Devices != null)
            {
                foreach (var dv in s.Devices)
                {
                    if (dv == null) continue;
                    snap.Devices.Add(new TryllMemoryDevice
                    {
                        Index            = dv.Index,
                        Name             = dv.Name,
                        Description      = dv.Description,
                        Kind             = dv.Kind,
                        TotalBytes       = dv.TotalBytes,
                        FreeBytes        = dv.FreeBytes,
                        SelfModelBytes   = dv.SelfModelBytes,
                        SelfContextBytes = dv.SelfContextBytes,
                        SelfComputeBytes = dv.SelfComputeBytes,
                        OtherBytes       = dv.OtherBytes,
                    });
                }
            }

            if (s.Components != null)
            {
                foreach (var cv in s.Components)
                {
                    if (cv == null) continue;
                    var row = new TryllMemoryComponent
                    {
                        Kind           = cv.Kind,
                        Name           = cv.Name,
                        Owner          = (TryllMemoryOwnerKind)cv.Owner,
                        Tier           = (TryllMemoryTier)cv.Tier,
                        EstimateBasis  = cv.EstimateBasis,
                        RamBytes       = cv.RamBytes,
                        VramBytes      = cv.VramBytes,
                        RamBytesHigh   = cv.RamBytesHigh,
                        DeviceIndex    = cv.DeviceIndex,
                        SessionId      = cv.SessionId,
                        AgentId        = cv.AgentId,
                        NodeName       = cv.NodeName,
                        UsedTokens     = cv.UsedTokens,
                        CapacityTokens = cv.CapacityTokens,
                        Resident       = cv.Resident,
                        OccupancyStale = cv.OccupancyStale,
                        ComputeBytes   = cv.ComputeBytes,
                        ConsumerClass  = (TryllInferenceConsumerClass)cv.ConsumerClass,
                        LastTurnAtMs   = cv.LastTurnAtMs,
                    };
                    if (cv.PerDevice != null)
                    {
                        foreach (var pv in cv.PerDevice)
                        {
                            if (pv == null) continue;
                            row.PerDevice.Add(new TryllMemoryComponentDevice
                            {
                                DeviceIndex  = pv.DeviceIndex,
                                Bytes        = pv.Bytes,
                                ComputeBytes = pv.ComputeBytes,
                            });
                        }
                    }
                    snap.Components.Add(row);
                }
            }

            return snap;
        }
    }
}
