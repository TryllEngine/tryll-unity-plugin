// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;

namespace Tryll.Client
{
    /// <summary>
    /// Embedding <c>ClassifyIntent</c> payload (<c>event_type="intent_classified"</c>).
    /// </summary>
    public sealed class TryllIntentClassification
    {
        public string NodeName;
        public string Intent;
        public string RecordId;
        public ulong RecordIndex;
        public float Distance;
        public string NotFoundReason;

        public bool Accepted => string.IsNullOrEmpty(NotFoundReason);

        public override string ToString()
            => Accepted
                ? $"{NodeName}: {Intent} (distance {Distance:0.###})"
                : $"{NodeName}: {Intent} ({NotFoundReason})";
    }

    /// <summary>One <c>&lt;label&gt;.prob</c> pair from ClassifyIntentLLM.</summary>
    public struct TryllIntentLlmLabelProb
    {
        public string Label;
        public float Prob;
    }

    /// <summary>
    /// <c>ClassifyIntentLLM</c> payload (<c>event_type="intent_llm_classified"</c>).
    /// </summary>
    public sealed class TryllIntentLlmClassification
    {
        public string NodeName;
        public string Intent;
        public float TopProb;
        public float SecondProb;
        public string NotFoundReason;
        public List<TryllIntentLlmLabelProb> LabelProbs = new List<TryllIntentLlmLabelProb>();

        public bool Accepted => string.IsNullOrEmpty(NotFoundReason);

        public override string ToString()
            => Accepted
                ? $"{NodeName}: {Intent} (top {TopProb:0.###})"
                : $"{NodeName}: {Intent} ({NotFoundReason})";
    }
}
