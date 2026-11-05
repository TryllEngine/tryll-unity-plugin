// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Default <see cref="TryllLipSync"/> inspector plus a warning when the agent's voice
    /// profile will not produce a viseme track (no profile, or Emit Visemes off).
    /// </summary>
    [CustomEditor(typeof(TryllLipSync))]
    [CanEditMultipleObjects]
    internal sealed class TryllLipSyncEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (!serializedObject.isEditingMultipleObjects)
            {
                var lipSync = (TryllLipSync)target;
                var agentProp = serializedObject.FindProperty("_agent");
                var agent = agentProp != null ? agentProp.objectReferenceValue as TryllAgentComponent : null;
                if (agent == null)
                    agent = lipSync.GetComponent<TryllAgentComponent>();
                string problem = TryllLipSync.VisemeSetupProblem(agent);
                if (problem != null)
                    EditorGUILayout.HelpBox(problem, MessageType.Warning);
            }
            DrawDefaultInspector();
        }
    }
}
