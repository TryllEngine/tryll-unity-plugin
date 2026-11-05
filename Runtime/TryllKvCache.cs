// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client
{
    public enum TryllKvCacheInitialization : byte
    {
        AllocateOnly = 0,
        Prefill = 1,
        DeferAllocation = 2,
    }

    public enum TryllKvCacheResidency : byte { Evicted = 0, Resident = 1 }
    public enum TryllKvCachePrefixStatus : byte { NotCurrent = 0, Current = 1 }

    // TryllWorkload used to live here, which had nothing to do with KV cache.
    // It now sits in TryllThrottle.cs, mirroring Unreal's TryllThrottle.h.

    public class TryllKvCacheStatus
    {
        public ulong AgentId;
        public bool Applicable;
        public TryllKvCacheResidency Residency;
        public TryllKvCachePrefixStatus PrefixStatus;
        public uint EligibleNodeCount;
        public string DebugInfo;
    }

    public sealed class TryllKvCachePrefillResult : TryllKvCacheStatus
    {
        public uint ContextsCreated;
        public uint TokensDecoded;
        public uint TokensReused;
    }

    public sealed class TryllKvCacheEvictResult : TryllKvCacheStatus
    {
        public uint ContextsEvicted;
    }
}
