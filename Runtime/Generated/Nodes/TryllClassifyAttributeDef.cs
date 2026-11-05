// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/nodes.fbs  +  tools/codegen/templates/unity_node.cs.j2
// See: docs/dev/codegen.md
using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using UnityEngine;

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_TryllClassifyAttributeDef {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// One specialist question scored in the same batched call as its siblings.
    /// </summary>
    /// <summary>Sub-table: One specialist question scored in the same batched call as its siblings.</summary>
    [Serializable]
    public sealed class TryllClassifyAttributeDef
    {
        public static readonly TryllClassifyAttributeDef Default = new TryllClassifyAttributeDef();

        [Tooltip("Attribute name; the answer is written to <Output Name>_<Name>. Must be unique within the node.")]
        public string Name;

        [Tooltip("Choice: picks one option and writes its id. Boolean: decides one Statement, writes true or false. Score: writes a 0–1 level.")]
        public TryllAttributeKind Kind = TryllAttributeKind.Choice;

        [Tooltip("Choice and Score only: the options, 2–26. For Score, order them from lowest to highest.")]
        public TryllClassifyAttributeChoice[] Choices;

        [Tooltip("Boolean only. The statement judged true or false for the input.")]
        [TryllLocalizable(true)]
        public string Statement;
        [HideInInspector] public bool StatementLocalized;
        [HideInInspector] public string StatementTextId;

        [Tooltip("Instructions for this attribute. Empty = a built-in default. Choice and Score must include the intents_block tag; Boolean must include the statement tag.")]
        [TryllLocalizable(true)]
        public string SystemPrompt;
        [HideInInspector] public bool SystemPromptLocalized;
        [HideInInspector] public string SystemPromptTextId;

        [Tooltip("Choice: minimum probability of the winner. Boolean: probability of True needed to answer true.")]
        [Range(0.0f, 1.0f)]
        public float Threshold = 0.5f;

        [Tooltip("Choice only. Minimum lead of the winner over the runner-up.")]
        [Range(0.0f, 1.0f)]
        public float Margin = 0.15f;

        [Tooltip("Choice only. Answer written when the winner is too weak: a choice id or #uncertain. A choice id fires Triggered if that choice does.")]
        public string UncertainChoice;

        [Tooltip("Boolean only. Which answer fires the node's Triggered exit: Never, True or False.")]
        public TryllBooleanTrigger BooleanTrigger = TryllBooleanTrigger.Never;

        [Tooltip("Score only. Fire the node's Triggered exit when the score is AtOrAbove or Below Score Threshold, or Never.")]
        public TryllScoreTrigger ScoreTrigger = TryllScoreTrigger.Never;

        [Tooltip("Score only. Level (0–1) that Score Trigger compares against.")]
        [Range(0.0f, 1.0f)]
        public float ScoreThreshold = 0.5f;


        internal int Pack(FlatBufferBuilder fbb)
        {
            // Pack nested objects and strings first (bottom-up). CreateString
            // after Start* throws "object serialization must not be nested".
            StringOffset nameOff = default;
            if (Name != null)
                nameOff = fbb.CreateString(Name);
            StringOffset statementOff = default;
            if (Statement != null)
                statementOff = fbb.CreateString(Statement);
            StringOffset system_promptOff = default;
            if (SystemPrompt != null)
                system_promptOff = fbb.CreateString(SystemPrompt);
            StringOffset uncertain_choiceOff = default;
            if (UncertainChoice != null)
                uncertain_choiceOff = fbb.CreateString(UncertainChoice);
            VectorOffset choicesVecOff = default;
            if (Choices != null && Choices.Length > 0)
            {
                var choicesOffs = new Offset<Tryll.NodeParams.ClassifyAttributeChoice>[Choices.Length];
                for (int i = 0; i < Choices.Length; i++)
                    choicesOffs[i] = new Offset<Tryll.NodeParams.ClassifyAttributeChoice>(Choices[i].Pack(fbb));
                choicesVecOff = Tryll.NodeParams.ClassifyAttributeDef.CreateChoicesVector(fbb, choicesOffs);
            }
            Tryll.NodeParams.ClassifyAttributeDef.StartClassifyAttributeDef(fbb);
            if (Name != null)
                Tryll.NodeParams.ClassifyAttributeDef.AddName(fbb, nameOff);
            Tryll.NodeParams.ClassifyAttributeDef.AddKind(fbb, (Tryll.AttributeKind)(byte)Kind);
            if (Choices != null && Choices.Length > 0)
                Tryll.NodeParams.ClassifyAttributeDef.AddChoices(fbb, choicesVecOff);
            if (Statement != null)
                Tryll.NodeParams.ClassifyAttributeDef.AddStatement(fbb, statementOff);
            if (SystemPrompt != null)
                Tryll.NodeParams.ClassifyAttributeDef.AddSystemPrompt(fbb, system_promptOff);
            Tryll.NodeParams.ClassifyAttributeDef.AddThreshold(fbb, Threshold);
            Tryll.NodeParams.ClassifyAttributeDef.AddMargin(fbb, Margin);
            if (UncertainChoice != null)
                Tryll.NodeParams.ClassifyAttributeDef.AddUncertainChoice(fbb, uncertain_choiceOff);
            Tryll.NodeParams.ClassifyAttributeDef.AddBooleanTrigger(fbb, (Tryll.BooleanTrigger)(byte)BooleanTrigger);
            Tryll.NodeParams.ClassifyAttributeDef.AddScoreTrigger(fbb, (Tryll.ScoreTrigger)(byte)ScoreTrigger);
            Tryll.NodeParams.ClassifyAttributeDef.AddScoreThreshold(fbb, ScoreThreshold);
            return Tryll.NodeParams.ClassifyAttributeDef.EndClassifyAttributeDef(fbb).Value;
        }

        public TryllClassifyAttributeDef Clone()
        {
            var c = (TryllClassifyAttributeDef)MemberwiseClone();
            if (Choices != null)
            {
                c.Choices = new TryllClassifyAttributeChoice[Choices.Length];
                for (int i = 0; i < Choices.Length; i++)
                    c.Choices[i] = Choices[i]?.Clone();
            }
            return c;
        }
    }
}
