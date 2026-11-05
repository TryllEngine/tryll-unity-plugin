// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Engine-local Memory table descriptors. Weights come from
    /// <see cref="TryllEditorUiContract"/>; labels stay here.
    /// </summary>
    internal readonly struct TryllMemoryColumn
    {
        public readonly string Header;
        public readonly float Weight;
        public readonly bool Numeric;

        public TryllMemoryColumn(string header, float weight, bool numeric = false)
        {
            Header = header;
            Weight = weight;
            Numeric = numeric;
        }
    }

    internal static class TryllMemoryTableColumns
    {
        public static readonly TryllMemoryColumn[] System =
        {
            new TryllMemoryColumn("Source", TryllEditorUiContract.MemorySystemWeights[0]),
            new TryllMemoryColumn("Bytes", TryllEditorUiContract.MemorySystemWeights[1], numeric: true),
            new TryllMemoryColumn("Note", TryllEditorUiContract.MemorySystemWeights[2]),
        };

        public static readonly TryllMemoryColumn[] Devices =
        {
            new TryllMemoryColumn("Device", TryllEditorUiContract.MemoryDevicesWeights[0]),
            new TryllMemoryColumn("Kind", TryllEditorUiContract.MemoryDevicesWeights[1]),
            new TryllMemoryColumn("Ours", TryllEditorUiContract.MemoryDevicesWeights[2], numeric: true),
            new TryllMemoryColumn("Other", TryllEditorUiContract.MemoryDevicesWeights[3], numeric: true),
            new TryllMemoryColumn("Free", TryllEditorUiContract.MemoryDevicesWeights[4], numeric: true),
            new TryllMemoryColumn("Total", TryllEditorUiContract.MemoryDevicesWeights[5], numeric: true),
        };

        public static readonly TryllMemoryColumn[] Kv =
        {
            new TryllMemoryColumn("Agent / node", TryllEditorUiContract.MemoryKvWeights[0]),
            new TryllMemoryColumn("Class", TryllEditorUiContract.MemoryKvWeights[1]),
            new TryllMemoryColumn("When", TryllEditorUiContract.MemoryKvWeights[2]),
            new TryllMemoryColumn("Context used", TryllEditorUiContract.MemoryKvWeights[3]),
            new TryllMemoryColumn("Reserved", TryllEditorUiContract.MemoryKvWeights[4], numeric: true),
        };

        public static readonly TryllMemoryColumn[] Components =
        {
            new TryllMemoryColumn("Kind", TryllEditorUiContract.MemoryComponentsWeights[0]),
            new TryllMemoryColumn("Name", TryllEditorUiContract.MemoryComponentsWeights[1]),
            new TryllMemoryColumn("Confidence", TryllEditorUiContract.MemoryComponentsWeights[2]),
            new TryllMemoryColumn("RAM", TryllEditorUiContract.MemoryComponentsWeights[3], numeric: true),
            new TryllMemoryColumn("VRAM", TryllEditorUiContract.MemoryComponentsWeights[4], numeric: true),
        };
    }
}
