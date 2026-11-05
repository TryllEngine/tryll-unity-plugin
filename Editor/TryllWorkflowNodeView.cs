// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using Tryll.Client;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// One canvas node. Ports mirror the data model: a single generic input (a node is just
    /// a routing target) and one output per <c>[TryllExit]</c> field. Each output port carries
    /// its <see cref="TryllNodeTypeRegistry.ExitField"/> in <c>userData</c> so edge write-back
    /// can find the exit string to set.
    ///
    /// The synthetic Entry node (<see cref="IsEntry"/>) has no input and a single "start"
    /// output whose edge maps to <c>TryllGraphDescription.StartNode</c>.
    /// </summary>
    internal sealed class TryllWorkflowNodeView : Node
    {
        public TryllNodeDescription Desc { get; private set; }
        public bool IsEntry { get; private set; }
        public Port InputPort { get; private set; }
        public Port StartPort { get; private set; }
        public IReadOnlyDictionary<string, Port> OutputPorts => _outputPorts;

        private readonly Dictionary<string, Port> _outputPorts = new Dictionary<string, Port>();

        private TryllWorkflowNodeView() { }

        public static TryllWorkflowNodeView CreateEntry(Action onSelected = null)
        {
            var view = new TryllWorkflowNodeView { IsEntry = true, title = "Entry" };
            view.StartPort = MakePort(Direction.Output, Port.Capacity.Single);
            view.StartPort.portName = "start";
            view.outputContainer.Add(view.StartPort);
            view.capabilities &= ~Capabilities.Deletable;
            if (onSelected != null)
                view.RegisterCallback<MouseDownEvent>(_ => onSelected.Invoke());
            view.RefreshPorts();
            view.RefreshExpandedState();
            return view;
        }

        public static TryllWorkflowNodeView CreateNode(TryllNodeDescription desc, Action onSelected, Action<string> onRenameRequested = null)
        {
            var view = new TryllWorkflowNodeView { Desc = desc, title = desc.Name };

            var typeLabel = new Label(TryllNodeTypeRegistry.DisplayName(desc.Params?.GetType()));
            typeLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            typeLabel.style.opacity = 0.7f;
            typeLabel.style.marginLeft = 6;
            view.titleContainer.Add(typeLabel);

            view.InputPort = MakePort(Direction.Input, Port.Capacity.Multi);
            view.InputPort.portName = "in";
            view.inputContainer.Add(view.InputPort);

            if (desc.Params != null)
            {
                foreach (var exit in TryllNodeTypeRegistry.GetExitFields(desc.Params.GetType()))
                {
                    var port = MakePort(Direction.Output, Port.Capacity.Single);
                    port.portName = exit.ExitName;
                    port.userData = exit;
                    view.outputContainer.Add(port);
                    view._outputPorts[exit.ExitName] = port;
                }
            }

            if (onRenameRequested != null)
            {
                view.titleContainer.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.clickCount != 2) return;
                    evt.StopPropagation();
                    TryllRenamePopup.Show(view.titleContainer, desc.Name, onRenameRequested);
                });
            }
            view.RegisterCallback<MouseDownEvent>(_ => onSelected?.Invoke());
            view.RefreshPorts();
            view.RefreshExpandedState();
            return view;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            string url = Desc?.Params?.DocsUrl;
            if (string.IsNullOrEmpty(url)) return;
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Open Documentation", _ => Application.OpenURL(url));
        }

        private static Port MakePort(Direction direction, Port.Capacity capacity)
            => Port.Create<Edge>(Orientation.Horizontal, direction, capacity, typeof(bool));
    }

    /// <summary>Minimal single-field rename popup for the double-click-title gesture.</summary>
    internal sealed class TryllRenamePopup : PopupWindowContent
    {
        private string _value;
        private readonly Action<string> _onCommit;
        private bool _focused;

        private TryllRenamePopup(string initialValue, Action<string> onCommit)
        {
            _value = initialValue;
            _onCommit = onCommit;
        }

        public static void Show(VisualElement anchor, string initialValue, Action<string> onCommit)
        {
            var rect = anchor.worldBound;
            UnityEditor.PopupWindow.Show(new Rect(rect.x, rect.y, rect.width, rect.height), new TryllRenamePopup(initialValue, onCommit));
        }

        public override Vector2 GetWindowSize() => new Vector2(220, 42);

        public override void OnGUI(Rect rect)
        {
            GUI.SetNextControlName("TryllRenamePopupField");
            _value = EditorGUILayout.TextField(_value);

            if (!_focused)
            {
                EditorGUI.FocusTextInControl("TryllRenamePopupField");
                _focused = true;
            }

            bool commit = (Event.current.type == EventType.KeyDown &&
                           (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter));
            if (commit)
            {
                editorWindow.Close();
                _onCommit?.Invoke(_value);
                Event.current.Use();
            }
            else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
            }
        }
    }
}
