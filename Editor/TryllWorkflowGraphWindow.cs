// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Linq;
using Tryll.Client;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Spike visual editor for <see cref="TryllWorkflowAsset"/> graphs. Renders the asset's
    /// nodes on a GraphView canvas; dragging/deleting edges writes straight back into the
    /// asset's exit fields / StartNode. The asset is the single source of truth — the canvas
    /// is rebuilt from it on open and on undo.
    /// </summary>
    public sealed class TryllWorkflowGraphWindow : EditorWindow
    {
        [SerializeField] private TryllWorkflowAsset _asset;

        private TryllWorkflowGraphView _graphView;
        private ObjectField _assetField;
        private VisualElement _issuesBox;
        private Label _inspectorTitle;
        private Button _inspectorHelp;
        private string _inspectorHelpUrl;
        private ScrollView _inspectorScroll;
        private TryllNodeDescription _selectedDesc;

        [MenuItem("Window/Tryll/Workflow Graph")]
        public static void Open()
        {
            var w = GetWindow<TryllWorkflowGraphWindow>("Tryll Workflow");
            w.minSize = new Vector2(700, 480);
        }

        // Unity introduced EntityId (and EditorUtility.EntityIdToObject) around 6000.2/6000.3
        // and started promoting the old int-based InstanceIDToObject from an obsolete
        // *warning* to a hard compile error at 6000.5. This package's floor is Unity 6000.0
        // (package.json), which predates EntityId entirely, so a single signature can't cover
        // the whole supported range — hence the #if. 6000.3 is the gate because that's the
        // first release both DLLs on this machine (6000.3.12f1, 6000.4.5f1) confirm
        // EntityId/EntityIdToObject exist in; the branch below it is unavoidable until the
        // package's declared floor moves past 6000.2, at which point it can be deleted.
#if UNITY_6000_3_OR_NEWER
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(EntityId entityId, int line) =>
            TryOpenWorkflowAsset(EditorUtility.EntityIdToObject(entityId));
#else
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
#pragma warning disable CS0618
            var obj = EditorUtility.InstanceIDToObject(instanceID);
#pragma warning restore CS0618
            return TryOpenWorkflowAsset(obj);
        }
#endif

        private static bool TryOpenWorkflowAsset(UnityEngine.Object obj)
        {
            if (obj is TryllWorkflowAsset asset)
            {
                var w = GetWindow<TryllWorkflowGraphWindow>("Tryll Workflow");
                w.minSize = new Vector2(700, 480);
                w.LoadAsset(asset);
                return true;
            }
            return false;
        }

        private void OnEnable()  => Undo.undoRedoPerformed += OnUndoRedo;
        private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

        private void OnUndoRedo() => _graphView?.Populate(_asset);

        private void CreateGUI()
        {
            var root = rootVisualElement;

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingLeft = 6;
            toolbar.style.paddingTop = 4;
            toolbar.style.paddingBottom = 4;

            _assetField = new ObjectField("Workflow") { objectType = typeof(TryllWorkflowAsset), value = _asset };
            _assetField.style.flexGrow = 1;
            _assetField.RegisterValueChangedCallback(evt => LoadAsset(evt.newValue as TryllWorkflowAsset));
            toolbar.Add(_assetField);

            var reload = new Button(() => _graphView?.Populate(_asset)) { text = "Reload" };
            toolbar.Add(reload);
            root.Add(toolbar);

            _issuesBox = new VisualElement();
            root.Add(_issuesBox);

            // Resizable split: graph canvas (flexes) | property panel (fixed pane, draggable).
            var split = new TwoPaneSplitView(1, 320, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;
            root.Add(split);

            _graphView = new TryllWorkflowGraphView(this);
            _graphView.style.flexGrow = 1;
            _graphView.style.minWidth = 200;
            split.Add(_graphView);

            var inspector = new VisualElement();
            inspector.style.minWidth = 180;
            inspector.style.borderLeftWidth = 1;
            inspector.style.borderLeftColor = new Color(0f, 0f, 0f, 0.3f);
            inspector.style.paddingLeft = 6;
            inspector.style.paddingRight = 6;
            inspector.style.paddingTop = 6;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 6;

            _inspectorTitle = new Label("No selection");
            _inspectorTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _inspectorTitle.style.flexGrow = 1;
            header.Add(_inspectorTitle);

            // Same book-with-? icon Unity draws for [HelpURL] in a component header.
            _inspectorHelp = new Button(() => { if (!string.IsNullOrEmpty(_inspectorHelpUrl)) Application.OpenURL(_inspectorHelpUrl); })
            {
                tooltip = "Open documentation",
            };
            _inspectorHelp.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("_Help").image;
            _inspectorHelp.style.width = 18;
            _inspectorHelp.style.height = 18;
            _inspectorHelp.style.backgroundColor = Color.clear;
            _inspectorHelp.style.borderLeftWidth = _inspectorHelp.style.borderRightWidth =
                _inspectorHelp.style.borderTopWidth = _inspectorHelp.style.borderBottomWidth = 0;
            header.Add(_inspectorHelp);
            inspector.Add(header);
            SetInspectorHelp(null);

            _inspectorScroll = new ScrollView();
            _inspectorScroll.style.flexGrow = 1;
            inspector.Add(_inspectorScroll);
            split.Add(inspector);

            if (_asset != null) _graphView.Populate(_asset);
        }

        /// <summary>Point the header's help button at <paramref name="url"/>; null hides it.</summary>
        private void SetInspectorHelp(string url)
        {
            _inspectorHelpUrl = url;
            if (_inspectorHelp != null)
                _inspectorHelp.style.display = string.IsNullOrEmpty(url) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>Bind the side panel to the selected node's Params, reusing the existing drawers.</summary>
        public void SelectNode(TryllNodeDescription desc)
        {
            if (_inspectorScroll == null) return;
            _inspectorScroll.Clear();
            _selectedDesc = null;

            int index = _asset?.Graph?.Nodes?.FindIndex(n => ReferenceEquals(n, desc)) ?? -1;
            if (index < 0)
            {
                ShowWorkflowInspector();
                return;
            }

            _selectedDesc = desc;
            _inspectorTitle.text = desc.Name;
            SetInspectorHelp(desc.Params?.DocsUrl);

            var so = new SerializedObject(_asset);
            var nameProp = so.FindProperty($"Graph.Nodes.Array.data[{index}].Name");
            var prop = so.FindProperty($"Graph.Nodes.Array.data[{index}].Params");
            if (prop == null)
            {
                _inspectorTitle.text = $"{desc.Name} (no params)";
                return;
            }

            // Render the params through IMGUI so the existing field drawers
            // (TryllModelType, TryllExit, TextArea, sub-objects, …) fire exactly as
            // they do in the Inspector. A UI Toolkit PropertyField bound to this
            // [SerializeReference] property does not reliably invoke those IMGUI
            // drawers, which left model-bearing nodes (Generate, ToolCall, …) blank.
            // Iterate the managed reference's children so we draw the params without
            // the redundant subclass-type selector.
            var imgui = new IMGUIContainer(() =>
            {
                if (so.targetObject == null) return;
                so.Update();
                EditorGUI.BeginChangeCheck();

                if (nameProp != null)
                    EditorGUILayout.PropertyField(nameProp);

                DrawGroupedParams(prop, desc.Params?.GetType());

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    // A Name commit (TryllNodeRenameDrawer) renames in place but knows
                    // nothing about the canvas, whose titles are set once at construction.
                    RefreshNodeTitles();
                    _graphView?.ScheduleValidation();
                }
            });
            _inspectorScroll.Add(imgui);
        }

        private const string AdvancedFoldoutKey = "Tryll.WorkflowGraph.AdvancedOpen";

        /// <summary>
        /// Draws the params' children section by section (<see cref="TryllGroupAttribute"/>):
        /// a bold header per group, Advanced as a foldout (closed by default), and no
        /// Exits — the canvas ports already show and edit them.
        /// </summary>
        private static void DrawGroupedParams(SerializedProperty prop, System.Type paramsType)
        {
            bool advancedOpen = SessionState.GetBool(AdvancedFoldoutKey, false);
            string current = null;
            int indent = EditorGUI.indentLevel;
            TryllGroupDecorator.Suppress = true;
            try
            {
                var it  = prop.Copy();
                var end = prop.GetEndProperty();
                bool enter = true;
                while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
                {
                    enter = false;
                    var group = paramsType?.GetField(it.name)?
                        .GetCustomAttributes(typeof(TryllGroupAttribute), true)
                        .FirstOrDefault() as TryllGroupAttribute;
                    if (group?.Name == TryllGroupAttribute.Exits) continue;

                    if (group != null && group.Name != current)
                    {
                        current = group.Name;
                        EditorGUI.indentLevel = indent;
                        EditorGUILayout.Space(6f);
                        if (current == TryllGroupAttribute.Advanced)
                        {
                            bool open = EditorGUILayout.Foldout(advancedOpen, current, true, EditorStyles.foldoutHeader);
                            if (open != advancedOpen)
                            {
                                advancedOpen = open;
                                SessionState.SetBool(AdvancedFoldoutKey, open);
                            }
                            EditorGUI.indentLevel = indent + 1;
                        }
                        else
                        {
                            EditorGUILayout.LabelField(current, EditorStyles.boldLabel);
                        }
                    }
                    if (current == TryllGroupAttribute.Advanced && !advancedOpen) continue;

                    EditorGUILayout.PropertyField(it, includeChildren: true);
                }
            }
            finally
            {
                TryllGroupDecorator.Suppress = false;
                EditorGUI.indentLevel = indent;
            }
        }

        /// <summary>
        /// Re-reads node names into the canvas titles and the inspector header after an
        /// in-place rename, without rebuilding the graph (which would re-run auto-layout).
        /// </summary>
        public void RefreshNodeTitles()
        {
            _graphView?.RefreshTitles();
            if (_selectedDesc != null && _inspectorTitle != null)
                _inspectorTitle.text = _selectedDesc.Name;
        }

        /// <summary>
        /// Bind the side panel to workflow-level asset fields (DefaultModelName +
        /// Variables). Shown when the graph opens, when Entry is selected, and when
        /// no ordinary node is selected — so declarations can be authored without
        /// leaving the graph window.
        /// </summary>
        public void ShowWorkflowInspector()
        {
            if (_inspectorScroll == null) return;
            _inspectorScroll.Clear();
            _selectedDesc = null;

            if (_asset == null)
            {
                _inspectorTitle.text = "No selection";
                SetInspectorHelp(null);
                return;
            }

            _inspectorTitle.text = "Workflow";
            SetInspectorHelp(TryllDocs.TryllWorkflowAsset);

            var so = new SerializedObject(_asset);
            var defaultModelProp = so.FindProperty("Graph.DefaultModelName");
            var variablesProp = so.FindProperty("Variables");
            var textTableProp = so.FindProperty("TextTable");

            var imgui = new IMGUIContainer(() =>
            {
                if (so.targetObject == null) return;
                so.Update();
                EditorGUI.BeginChangeCheck();

                if (defaultModelProp != null)
                    EditorGUILayout.PropertyField(defaultModelProp, includeChildren: true);
                if (textTableProp != null)
                    EditorGUILayout.PropertyField(textTableProp);
                if (variablesProp != null)
                    EditorGUILayout.PropertyField(variablesProp, includeChildren: true);

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    // Declaration edits can invalidate {{var.*}} / filter refs.
                    _graphView?.ScheduleValidation();
                }

                if (GUILayout.Button("Check texts"))
                    TryllTextCheckWindow.Open(_asset);
            });
            _inspectorScroll.Add(imgui);
        }

        public void ClearInspector() => ShowWorkflowInspector();

        internal void DebugLoadForTests(TryllWorkflowAsset asset) => LoadAsset(asset);
        internal List<string> DebugNodeNames() => _graphView?.DebugNodeNames() ?? new List<string>();
        /// <summary>Edges as "source.exit->target"; the entry edge is "Entry.start->target".</summary>
        internal List<string> DebugEdges() => _graphView?.DebugEdges() ?? new List<string>();
        /// <summary>Validation issues currently shown.</summary>
        internal int DebugIssueCount => _issuesBox?.childCount ?? 0;
        /// <summary>Delete one edge as the user would; false when it is not on the canvas.</summary>
        internal bool DebugDeleteEdgeForTests(string source, string exitName)
            => _graphView != null && _graphView.DebugDeleteEdge(source, exitName);

        private void LoadAsset(TryllWorkflowAsset asset)
        {
            _asset = asset;
            if (_assetField != null) _assetField.SetValueWithoutNotify(asset);
            _graphView?.Populate(asset);
        }

        public void ShowIssues(List<TryllWorkflowValidator.Issue> issues)
        {
            if (_issuesBox == null) return;
            _issuesBox.Clear();
            if (issues == null || issues.Count == 0) return;

            foreach (var issue in issues)
            {
                var type = issue.Level == TryllWorkflowValidator.Issue.Severity.Error
                    ? HelpBoxMessageType.Error
                    : HelpBoxMessageType.Warning;
                _issuesBox.Add(new HelpBox(issue.Message, type));
            }
        }
    }

    /// <summary>The canvas. Marshals <see cref="TryllGraphDescription"/> ⇄ nodes/ports/edges.</summary>
    internal sealed class TryllWorkflowGraphView : GraphView
    {
        private readonly TryllWorkflowGraphWindow _window;
        private TryllWorkflowAsset _asset;
        private IVisualElementScheduledItem _validationItem;
        private IVisualElementScheduledItem _inspectorSyncItem;

        private const float XGap = 260f;
        private const float YGap = 140f;

        public TryllWorkflowGraphView(TryllWorkflowGraphWindow window)
        {
            _window = window;
            style.flexGrow = 1;

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // Nodes bind the inspector from their own click callback, but a deselect
            // (empty-canvas click, rectangle-select of nothing, ctrl-click on the only
            // selected node) has no such callback — the panel would keep showing the
            // previously selected node. Re-derive the empty case from the selection set
            // after every click so "nothing selected" falls back to the workflow
            // inspector, exactly as selecting Entry does.
            RegisterCallback<MouseUpEvent>(_ => ScheduleInspectorSync());
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatible = new List<Port>();
            ports.ForEach(p =>
            {
                if (p.direction == startPort.direction) return; // out → in only
                if (p.node == startPort.node) return;            // no self-loop (mirrors TryllExitDrawer)
                compatible.Add(p);
            });
            return compatible;
        }

        public void Populate(TryllWorkflowAsset asset)
        {
            _asset = asset;
            graphViewChanged = null; // detach while we rebuild programmatically
            DeleteElements(graphElements.ToList());
            _window.ClearInspector();

            if (asset?.Graph == null)
            {
                _window.ShowIssues(null);
                graphViewChanged = OnGraphViewChanged;
                return;
            }

            var graph = asset.Graph;

            var entry = TryllWorkflowNodeView.CreateEntry(() => _window.ShowWorkflowInspector());
            AddElement(entry);
            _window.ShowWorkflowInspector();

            var views = new Dictionary<string, TryllWorkflowNodeView>(System.StringComparer.Ordinal);
            foreach (var node in graph.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Name) || views.ContainsKey(node.Name)) continue;
                var captured = node;
                var view = TryllWorkflowNodeView.CreateNode(node, () => _window.SelectNode(captured),
                    newName => RenameNode(captured, newName));
                views[node.Name] = view;
                AddElement(view);
            }

            AutoLayout(entry, views, graph);
            ConnectEdges(entry, views, graph);

            graphViewChanged = OnGraphViewChanged;
            RunValidation();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (_asset?.Graph != null)
            {
                Vector2 screenMouse = (evt.triggerEvent is IMouseEvent me) ? me.mousePosition : Vector2.zero;
                Vector2 graphPos = contentViewContainer.WorldToLocal(screenMouse);
                foreach (var type in TryllNodeTypeRegistry.NodeParamTypes.OrderBy(TryllNodeTypeRegistry.DisplayName))
                {
                    var captured = type;
                    evt.menu.AppendAction($"Add Node/{TryllNodeTypeRegistry.DisplayName(captured)}",
                        _ => AddNode(captured, graphPos));
                }
                evt.menu.AppendSeparator();
            }
            base.BuildContextualMenu(evt);
        }

        private void AddNode(System.Type paramsType, Vector2 position)
        {
            if (_asset?.Graph == null) return;

            var prms = (TryllNodeParamsBase)System.Activator.CreateInstance(paramsType);
            string name = MakeUniqueName(TryllNodeTypeRegistry.DisplayName(paramsType));
            var desc = new TryllNodeDescription { Name = name, Params = prms };

            ApplyGraphEdit($"Add {name}", () => _asset.Graph.Nodes.Add(desc));

            var view = TryllWorkflowNodeView.CreateNode(desc, () => _window.SelectNode(desc),
                newName => RenameNode(desc, newName));
            AddElement(view);
            view.SetPosition(new Rect(position, Vector2.zero));
            _window.SelectNode(desc);
        }

        private string MakeUniqueName(string baseName)
        {
            var existing = new HashSet<string>(
                _asset.Graph.Nodes.Where(n => n != null && !string.IsNullOrEmpty(n.Name)).Select(n => n.Name),
                System.StringComparer.Ordinal);
            string candidate = baseName;
            int i = 1;
            while (existing.Contains(candidate)) candidate = $"{baseName}{++i}";
            return candidate;
        }

        /// <summary>
        /// Commits a node-name rename requested via the double-click-title gesture, through
        /// the same grammar/uniquify/propagation rules as <see cref="TryllNodeRenameDrawer"/>
        /// (see <see cref="TryllRenamePropagation"/>), routed through <see cref="ApplyGraphEdit"/>
        /// for undo, then refreshes the titles in place. Edges hold view references, not
        /// names, so no rebuild is needed — and a rebuild would discard the user's layout.
        /// </summary>
        private void RenameNode(TryllNodeDescription desc, string requestedName)
        {
            if (_asset?.Graph?.Nodes == null || desc == null) return;

            string oldName = desc.Name;
            if (requestedName == oldName) return;

            if (!TryllRenamePropagation.IsValidGrammar(requestedName) || TryllRenamePropagation.IsReserved(requestedName))
            {
                Debug.LogWarning($"[Tryll] Invalid node name '{requestedName}': must match [A-Za-z_][A-Za-z0-9_]* " +
                                  $"and not be the reserved '{TryllRenamePropagation.ReservedUserMessage}'.");
                return;
            }

            var existingNames = _asset.Graph.Nodes
                .Where(n => n != null && !string.IsNullOrEmpty(n.Name) && n.Name != oldName)
                .Select(n => n.Name);
            string newName = TryllRenamePropagation.Uniquify(requestedName, existingNames);
            bool rewriteSlot = TryllRenamePropagation.EffectiveSlotIsNodeName(desc);

            ApplyGraphEdit($"Rename {oldName}", () =>
            {
                desc.Name = newName;
                TryllRenamePropagation.PropagateNodeRename(_asset.Graph, oldName, newName);
                if (rewriteSlot)
                    TryllRenamePropagation.PropagateOwnedSlotRename(_asset.Graph, desc, oldName, newName);
            });

            _window.RefreshNodeTitles();
        }

        /// <summary>Re-reads each node's title from its description (titles are not bound).</summary>
        internal void RefreshTitles()
        {
            nodes.ForEach(n =>
            {
                if (n is TryllWorkflowNodeView view && !view.IsEntry && view.Desc != null)
                    view.title = view.Desc.Name;
            });
        }

        private void ApplyNodeDelete(TryllWorkflowNodeView view)
        {
            var desc = view.Desc;
            string name = desc.Name;
            ApplyGraphEdit($"Delete {name}", () =>
            {
                _asset.Graph.Nodes.RemoveAll(n => ReferenceEquals(n, desc));
                if (_asset.Graph.StartNode == name) _asset.Graph.StartNode = string.Empty;

                foreach (var node in _asset.Graph.Nodes)
                {
                    if (node?.Params == null) continue;
                    foreach (var exit in TryllNodeTypeRegistry.GetExitFields(node.Params.GetType()))
                        if (TryllNodeTypeRegistry.ReadExit(node.Params, exit.Field) == name)
                            TryllNodeTypeRegistry.WriteExit(node.Params, exit.Field, string.Empty);
                }
            });
        }

        private void ConnectEdges(TryllWorkflowNodeView entry, Dictionary<string, TryllWorkflowNodeView> views, TryllGraphDescription graph)
        {
            if (!string.IsNullOrEmpty(graph.StartNode) && views.TryGetValue(graph.StartNode, out var startView))
                AddElement(entry.StartPort.ConnectTo(startView.InputPort));

            foreach (var node in graph.Nodes)
            {
                if (node?.Params == null || !views.TryGetValue(node.Name, out var srcView)) continue;
                foreach (var exit in TryllNodeTypeRegistry.GetExitFields(node.Params.GetType()))
                {
                    string target = TryllNodeTypeRegistry.ReadExit(node.Params, exit.Field);
                    if (string.IsNullOrEmpty(target) || !views.TryGetValue(target, out var dstView)) continue;
                    if (srcView.OutputPorts.TryGetValue(exit.ExitName, out var outPort))
                        AddElement(outPort.ConnectTo(dstView.InputPort));
                }
            }
        }

        private void AutoLayout(TryllWorkflowNodeView entry, Dictionary<string, TryllWorkflowNodeView> views, TryllGraphDescription graph)
        {
            var depth = new Dictionary<string, int>(System.StringComparer.Ordinal);
            var queue = new Queue<string>();

            if (!string.IsNullOrEmpty(graph.StartNode) && views.ContainsKey(graph.StartNode))
            {
                depth[graph.StartNode] = 1;
                queue.Enqueue(graph.StartNode);
            }

            while (queue.Count > 0)
            {
                string name = queue.Dequeue();
                var node = views[name].Desc;
                if (node?.Params == null) continue;
                foreach (var exit in TryllNodeTypeRegistry.GetExitFields(node.Params.GetType()))
                {
                    string target = TryllNodeTypeRegistry.ReadExit(node.Params, exit.Field);
                    if (string.IsNullOrEmpty(target) || !views.ContainsKey(target) || depth.ContainsKey(target)) continue;
                    depth[target] = depth[name] + 1;
                    queue.Enqueue(target);
                }
            }

            int maxDepth = depth.Count > 0 ? depth.Values.Max() : 0;
            foreach (var name in views.Keys.ToList())
                if (!depth.ContainsKey(name)) depth[name] = maxDepth + 1;

            entry.SetPosition(new Rect(0, 0, 0, 0));

            var rowInColumn = new Dictionary<int, int>();
            foreach (var kv in depth.OrderBy(k => k.Value))
            {
                int col = kv.Value;
                rowInColumn.TryGetValue(col, out int row);
                rowInColumn[col] = row + 1;
                views[kv.Key].SetPosition(new Rect(col * XGap, row * YGap, 0, 0));
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                    if (element is Edge edge) ApplyEdgeDelete(edge);

                foreach (var element in change.elementsToRemove)
                    if (element is TryllWorkflowNodeView node && !node.IsEntry && node.Desc != null)
                    {
                        ApplyNodeDelete(node);
                        _window.ClearInspector();
                    }
            }

            if (change.edgesToCreate != null)
                foreach (var edge in change.edgesToCreate) ApplyEdgeCreate(edge);

            return change;
        }

        private void ApplyEdgeCreate(Edge edge)
        {
            var src = edge.output?.node as TryllWorkflowNodeView;
            var dst = edge.input?.node as TryllWorkflowNodeView;
            if (src == null || dst?.Desc == null) return;

            if (src.IsEntry)
            {
                ApplyGraphEdit("Set Start Node", () => _asset.Graph.StartNode = dst.Desc.Name);
            }
            else
            {
                var exit = (TryllNodeTypeRegistry.ExitField)edge.output.userData;
                ApplyGraphEdit($"Wire '{exit.ExitName}'", () => TryllNodeTypeRegistry.WriteExit(src.Desc.Params, exit.Field, dst.Desc.Name));
            }
        }

        private void ApplyEdgeDelete(Edge edge)
        {
            var src = edge.output?.node as TryllWorkflowNodeView;
            if (src == null) return;

            if (src.IsEntry)
            {
                ApplyGraphEdit("Clear Start Node", () => _asset.Graph.StartNode = string.Empty);
            }
            else if (src.Desc != null)
            {
                var exit = (TryllNodeTypeRegistry.ExitField)edge.output.userData;
                ApplyGraphEdit($"Unwire '{exit.ExitName}'", () => TryllNodeTypeRegistry.WriteExit(src.Desc.Params, exit.Field, string.Empty));
            }
        }

        private void ApplyGraphEdit(string label, System.Action mutate)
        {
            if (_asset == null) return;
            Undo.RecordObject(_asset, label);
            mutate();
            EditorUtility.SetDirty(_asset);
            ScheduleValidation();
        }

        internal List<string> DebugNodeNames() => nodes.ToList()
            .OfType<TryllWorkflowNodeView>()
            .Where(n => !n.IsEntry && n.Desc != null)
            .Select(n => n.Desc.Name)
            .ToList();

        internal List<string> DebugEdges() => edges.ToList().Select(DescribeEdge).ToList();

        internal bool DebugDeleteEdge(string source, string exitName)
        {
            var edge = edges.ToList().FirstOrDefault(e => DescribeEdge(e).StartsWith($"{source}.{exitName}->"));
            if (edge == null) return false;
            DeleteElements(new GraphElement[] { edge }); // fires graphViewChanged, like a user delete
            return true;
        }

        private static string DescribeEdge(Edge edge)
        {
            var src = edge.output?.node as TryllWorkflowNodeView;
            var dst = edge.input?.node as TryllWorkflowNodeView;
            string from = src == null ? "?" : src.IsEntry ? "Entry" : src.Desc?.Name;
            string exit = src != null && src.IsEntry
                ? "start"
                : edge.output?.userData is TryllNodeTypeRegistry.ExitField f ? f.ExitName : "?";
            return $"{from}.{exit}->{dst?.Desc?.Name}";
        }

        internal void ScheduleValidation()
        {
            _validationItem ??= schedule.Execute(RunValidation);
            _validationItem.ExecuteLater(300);
        }

        /// <summary>
        /// Deferred so the read happens after the click has finished mutating the
        /// selection (a click on a node clears the set before re-adding, so an
        /// immediate read can observe a transient empty selection).
        /// </summary>
        private void ScheduleInspectorSync()
        {
            _inspectorSyncItem ??= schedule.Execute(SyncInspectorToSelection);
            _inspectorSyncItem.ExecuteLater(0);
        }

        private void SyncInspectorToSelection()
        {
            // Only the empty case is derived here; a non-empty selection was already
            // bound by the clicked node's own callback.
            if (selection.Count == 0) _window.ShowWorkflowInspector();
        }

        private void RunValidation()
        {
            if (_asset?.Graph == null) { _window.ShowIssues(null); return; }
            // Pass the asset's declared Variables so {{var.name}} / filter var
            // references validate against them — otherwise every valid reference
            // is falsely flagged as undeclared.
            _window.ShowIssues(TryllWorkflowValidator.Validate(_asset.Graph, _asset.Variables));
        }
    }
}
