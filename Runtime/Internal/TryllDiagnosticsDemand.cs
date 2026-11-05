// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// Ref-counted diagnostics-demand lease on the shared client connection (§7.5).
    /// While active, agent creation ORs <c>enable_diagnostics</c> into the wire request.
    /// </summary>
    internal sealed class TryllDiagnosticsDemand
    {
        private int _refcount;

        public bool IsActive => _refcount > 0;

        public IDisposable Acquire()
        {
            _refcount++;
            return new Lease(this);
        }

        private void Release()
        {
            if (_refcount > 0) _refcount--;
        }

        private sealed class Lease : IDisposable
        {
            private TryllDiagnosticsDemand _owner;

            public Lease(TryllDiagnosticsDemand owner) => _owner = owner;

            public void Dispose()
            {
                var owner = _owner;
                _owner = null;
                owner?.Release();
            }
        }
    }
}
