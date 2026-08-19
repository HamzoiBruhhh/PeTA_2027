using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System;
using System.Linq;

namespace Reshape.ReGraph
{
    public class GraphViewer : GraphView
    {
        public new class UxmlFactory : UxmlFactory<GraphViewer, GraphView.UxmlTraits> { }

        public static List<GraphNode> copiedNodes;

        public Action<GraphNodeView> OnNodeSelected;
        public Action<GraphNodeView> OnNodeUnselected;

        SerializedGraph serializer;
        GraphSettings settings;
        GraphNodeView selectedNodeView;

        public GraphViewer ()
        {
            settings = GraphSettings.GetSettings();

            Insert(0, new GridBackground());

            var zoomer = new ContentZoomer();
            zoomer.maxScale = settings.graphZoomInCap;
            this.AddManipulator(zoomer);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new GraphDoubleClickSelection());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.RegisterCallback<KeyDownEvent>(new EventCallback<KeyDownEvent>(this.OnKeyDown));

            var styleSheet = settings.graphStyle;
            styleSheets.Add(styleSheet);

            viewTransformChanged += OnViewTransformChanged;
        }

        void OnKeyDown (KeyDownEvent evt)
        {
            if (serializer != null && serializer.graph != null)
            {
                if (evt.keyCode == KeyCode.Space)
                {
                    //Debug.Log("Press Spacebar : " + (selectedNodeView == null ? "None" : "Selected"));
                }
            }
        }

        void OnViewTransformChanged (GraphView graphView)
        {
            Vector3 pos = contentViewContainer.transform.position;
            Vector3 size = contentViewContainer.transform.scale;
            serializer?.SetViewTransform(pos, size);
        }

        public GraphNodeView FindNodeView (GraphNode node)
        {
            if (node == null) return null;
            return FindNodeView(node.guid);
        }

        public GraphNodeView FindNodeView (string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            return GetNodeByGuid(guid) as GraphNodeView;
        }

        public void CleanView ()
        {
            CleanElements(graphElements.ToList());
        }

        public void ClearView ()
        {
            CleanView();
            serializer = null;
        }

        public void PopulateView (SerializedGraph tree)
        {
            serializer = tree;
            CleanView();
            Debug.Assert(serializer.graph.isCreated);

            serializer.graph.nodes.ForEach(n =>
            {
                if (n != null)
                {
                    n.SetGraphEditorContext(serializer.graphSelectionObject);
                    CreateNodeView(n);
                }
            });

            serializer.graph.nodes.ForEach(n =>
            {
                if (n != null)
                    AddChildElements(n);
            });

            contentViewContainer.transform.position = serializer.graph.viewPosition;
            contentViewContainer.transform.scale = serializer.graph.viewScale;
        }

        public void RefreshEdge (GraphNodeView nodeView)
        {
            if (nodeView == null) return;
            CleanElements(nodeView.output.connections);
            AddChildElements(nodeView.node);
        }

        public override List<Port> GetCompatiblePorts (Port startPort, NodeAdapter nodeAdapter)
        {
            var returnList = new List<Port>();
            var portList = ports.ToList();
            var startNodeView = (GraphNodeView) startPort.node;
            for (var i = 0; i < portList.Count; i++)
            {
                var endPort = portList[i];
                var endNodeView = (GraphNodeView) endPort.node;
                var startValidateNode = startNodeView;
                var endValidateNode = endNodeView;
                if (serializer.graph.Type is Graph.GraphType.BehaviourGraph or Graph.GraphType.StatGraph or Graph.GraphType.AttackDamagePack or Graph.GraphType.TargetAimPack
                    or Graph.GraphType.MoralePack or Graph.GraphType.AttackStatusPack or Graph.GraphType.LootPack or Graph.GraphType.StaminaPack or Graph.GraphType.AttackSkillPack
                    or Graph.GraphType.BehaviourPack)
                {
                    if (startPort.direction == Direction.Output)
                    {
                        if (startNodeView.node is RootNode && endNodeView.node is TriggerNode == false)
                            continue;
                        if (startNodeView.node is TriggerNode && endNodeView.node is BehaviourNode == false)
                            continue;
                        if (startNodeView.node is BehaviourNode && endNodeView.node is BehaviourNode == false)
                            continue;
                        if (startNodeView.node is VariableBehaviourNode or DialogBehaviourNode or TargetAimBehaviourNode or CharacterBehaviourNode or InventoryBehaviourNode or StaminaBehaviourNode == false && endNodeView.node is ConditionNode)
                            continue;
                    }
                    else
                    {
                        startValidateNode = endNodeView;
                        endValidateNode = startNodeView;
                        if (startNodeView.node is TriggerNode && endNodeView.node is RootNode == false)
                            continue;
                        if (startNodeView.node is ConditionNode && endNodeView.node is VariableBehaviourNode or DialogBehaviourNode or CharacterBehaviourNode or TargetAimBehaviourNode or InventoryBehaviourNode or StaminaBehaviourNode == false)
                            continue;
                        if (startNodeView.node is BehaviourNode && endNodeView.node is BehaviourNode or TriggerNode == false)
                            continue;
                    }
                }

                if (startNodeView.node is BehaviourNode)
                {
                    if (endNodeView.node is BehaviourNode)
                    {
                        var startNodeId = startValidateNode.node.guid;
                        var endNodeId = endValidateNode.node.guid;
                        if (startValidateNode.node.FindParents(endNodeId, true))
                            continue;
                    }
                }

                if (endPort.direction != startPort.direction && endPort.node != startPort.node)
                    returnList.Add(endPort);
            }

            return returnList;
        }

        private GraphViewChange OnGraphViewChanged (GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                graphViewChange.elementsToRemove.ForEach(elem =>
                {
                    GraphNodeView nodeView = elem as GraphNodeView;
                    if (nodeView != null)
                    {
                        nodeView.UnhighlightReference();
                        serializer.DeleteNode(nodeView.node);
                        OnNodeSelected(null);
                    }

                    Edge edge = elem as Edge;
                    if (edge != null)
                    {
                        GraphNodeView parentView = edge.output.node as GraphNodeView;
                        GraphNodeView childView = edge.input.node as GraphNodeView;
                        serializer.RemoveChild(parentView.node, childView.node);
                        childView.ResetInputPortColor();
                    }
                });
            }

            if (graphViewChange.edgesToCreate != null)
            {
                graphViewChange.edgesToCreate.ForEach(edge =>
                {
                    GraphNodeView parentView = edge.output.node as GraphNodeView;
                    GraphNodeView childView = edge.input.node as GraphNodeView;
                    serializer.AddChild(parentView.node, childView.node);
                    childView.UpdateInputPortColor(parentView);
                });
            }

            bool changes = false;
            nodes.ForEach((n) =>
            {
                GraphNodeView view = n as GraphNodeView;
                List<GraphNode> sorted = view.SortChildren();
                if (sorted != null)
                    if (serializer.SortChildren(view.node, sorted))
                        changes = true;
            });
            if (changes)
                serializer.SaveSerializedObjectAfterSorted();

            return graphViewChange;
        }

        public override void BuildContextualMenu (ContextualMenuPopulateEvent evt)
        {
            if (serializer != null && serializer.graph != null)
            {
                Vector2 nodePosition = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
                if (evt.target is not GraphNodeView nodeView)
                {
                    evt = GetPasteAction(evt, nodePosition);
                    evt.menu.AppendSeparator();
                }

                Dictionary<string, System.Type> list = GetContextualList(serializer.graph);
                foreach (var menuItem in list)
                    evt.menu.AppendAction(menuItem.Key, (a) => CreateNode(menuItem.Value, nodePosition));
            }
        }

        /* START - Custom View start here */
        public Dictionary<string, Type> GetContextualList (Graph graph)
        {
            var existed = new List<Type>();
            var list = new Dictionary<string, Type>();
            if (graph.isBehaviourGraph)
            {
                var triggerList = new Dictionary<string, Type>();
                var types = TypeCache.GetTypesDerivedFrom<TriggerNode>();
                foreach (var type in types)
                {
                    if (type == typeof(AttackDamageTriggerNode))
                        continue;
                    if (type == typeof(TargetAimTriggerNode))
                        continue;
                    if (type == typeof(TargetAimBehaviourNode))
                        continue;
                    if (type == typeof(AttackStatusTriggerNode))
                        continue;
                    if (type == typeof(MoraleTriggerNode))
                        continue;
                    if (type == typeof(StaminaTriggerNode))
                        continue;
                    if (type == typeof(AttackSkillTriggerNode))
                        continue;
                    if (type == typeof(LootTriggerNode))
                        continue;
                    if (!existed.Contains(type))
                    {
                        triggerList.Add(type.Name.Substring(0, type.Name.IndexOf("TriggerNode", StringComparison.Ordinal)), type);
                        existed.Add(type);
                    }
                }

                foreach (var node in triggerList.OrderBy(key => key.Key))
                    list.Add($"Trigger/{node.Key}", node.Value);

                types = TypeCache.GetTypesDerivedFrom<ConditionNode>();
                foreach (var type in types)
                {
                    if (!existed.Contains(type))
                    {
                        list.Add($"Condition/{type.Name.Substring(0, type.Name.IndexOf("ConditionNode", StringComparison.Ordinal))}", type);
                        existed.Add(type);
                    }
                }

                existed.Add(typeof(NoteBehaviourNode));
                existed.Add(typeof(ConnectorBehaviourNode));
                if (graph.isBehaviourGraph)
                    existed.Add(typeof(ReturnBehaviourNode));

                var behaviourList = new Dictionary<string, System.Type>();
                types = TypeCache.GetTypesDerivedFrom<BehaviourNode>();
                foreach (var type in types)
                {
                    if (!existed.Contains(type) && type != typeof(ConditionNode))
                    {
                        var behaviourNode = (BehaviourNode) Activator.CreateInstance(type);
                        behaviourList.Add(behaviourNode.GetNodeMenuDisplayName(), type);
                        existed.Add(type);
                    }
                }

                foreach (var node in behaviourList.OrderByDescending(key => key.Key.Contains("/")).ThenBy(x => x.Key))
                    list.Add($"Behaviour/{node.Key}", node.Value);

                list.Add("Note", typeof(NoteBehaviourNode));
                list.Add("Connector", typeof(ConnectorBehaviourNode));
            }
            else if (graph.isStatGraph)
            {
                AddTrigger(typeof(BrainStatTriggerNode));
                AddTrigger(typeof(ActionTriggerNode));
                AddCondition(typeof(YesConditionNode));
                AddCondition(typeof(NoConditionNode));
                AddBehaviour(typeof(BrainStatBehaviourNode));
                AddBehaviour(typeof(CharacterBehaviourNode));
                AddBehaviour(typeof(InventoryBehaviourNode));
                AddBehaviour(typeof(VariableBehaviourNode));
                AddBehaviour(typeof(TriggerBehaviourNode));
                AddBehaviour(typeof(ReturnBehaviourNode));
                AddBehaviour(typeof(DebugBehaviourNode));
                AddBehaviour(typeof(ListBehaviourNode));
                Add(typeof(NoteBehaviourNode));
                Add(typeof(ConnectorBehaviourNode));
            }
            else if (graph.isAttackDamagePack)
            {
                AddTrigger(typeof(AttackDamageTriggerNode));
                AddCondition(typeof(YesConditionNode));
                AddCondition(typeof(NoConditionNode));
                AddBehaviour(typeof(BrainStatBehaviourNode));
                AddBehaviour(typeof(CharacterBehaviourNode));
                AddBehaviour(typeof(TargetAimBehaviourNode));
                AddBehaviour(typeof(StaminaBehaviourNode));
                AddBehaviour(typeof(InventoryBehaviourNode));
                AddBehaviour(typeof(VariableBehaviourNode));
                AddBehaviour(typeof(ListBehaviourNode));
                AddBehaviour(typeof(ReturnBehaviourNode));
                AddBehaviour(typeof(DebugBehaviourNode));
                Add(typeof(NoteBehaviourNode));
                Add(typeof(ConnectorBehaviourNode));
            }
            else if (graph.isTargetAimPack)
            {
                AddTrigger(typeof(TargetAimTriggerNode));
                AddTrigger(typeof(ActionTriggerNode));
                AddCondition(typeof(YesConditionNode));
                AddCondition(typeof(NoConditionNode));
                AddBehaviour(typeof(TargetAimBehaviourNode));
                AddBehaviour(typeof(BrainStatBehaviourNode));
                AddBehaviour(typeof(CharacterBehaviourNode));
                AddBehaviour(typeof(InventoryBehaviourNode));
                AddBehaviour(typeof(ListBehaviourNode));
                AddBehaviour(typeof(VariableBehaviourNode));
                AddBehaviour(typeof(ReturnBehaviourNode));
                AddBehaviour(typeof(TriggerBehaviourNode));
                AddBehaviour(typeof(ActionBehaviourNode));
                AddBehaviour(typeof(DebugBehaviourNode));
                Add(typeof(NoteBehaviourNode));
                Add(typeof(ConnectorBehaviourNode));
            }
            else if (graph.isMoralePack)
            {
                AddTrigger(typeof(MoraleTriggerNode));
                AddTrigger(typeof(ActionTriggerNode));
                AddCondition(typeof(YesConditionNode));
                AddCondition(typeof(NoConditionNode));
                AddBehaviour(typeof(BrainStatBehaviourNode));
                AddBehaviour(typeof(CharacterBehaviourNode));
                AddBehaviour(typeof(VariableBehaviourNode));
                AddBehaviour(typeof(TriggerBehaviourNode));
                AddBehaviour(typeof(DebugBehaviourNode));
                Add(typeof(NoteBehaviourNode));
                Add(typeof(ConnectorBehaviourNode));
            }
            else if (graph.isAttackStatusPack)
            {
                AddTrigger(typeof(AttackStatusTriggerNode));
                AddCondition(typeof(YesConditionNode));
                AddCondition(typeof(NoConditionNode));
                AddBehaviour(typeof(CharacterBehaviourNode));
                AddBehaviour(typeof(BrainStatBehaviourNode));
                AddBehaviour(typeof(VariableBehaviourNode));
                AddBehaviour(typeof(TriggerBehaviourNode));
                AddBehaviour(typeof(CacheBehaviourNode));
                AddBehaviour(typeof(DebugBehaviourNode));
                Add(typeof(NoteBehaviourNode));
                Add(typeof(ConnectorBehaviourNode));
            }
            else if (graph.isLootPack)
            {
                AddTrigger(typeof(LootTriggerNode));
                AddTrigger(typeof(ActionTriggerNode));
                AddCondition(typeof(YesConditionNode));
                AddCondition(typeof(NoConditionNode));
                AddBehaviour(typeof(VariableBehaviourNode));
                AddBehaviour(typeof(InventoryBehaviourNode));
                AddBehaviour(typeof(TriggerBehaviourNode));
                AddBehaviour(typeof(DebugBehaviourNode));
                Add(typeof(NoteBehaviourNode));
                Add(typeof(ConnectorBehaviourNode));
            }
            else if (graph.isBehaviourPack)
            {
                AddTrigger(typeof(ActionTriggerNode));
                AddCondition(typeof(YesConditionNode));
                AddCondition(typeof(NoConditionNode));
                AddBehaviour(typeof(VariableBehaviourNode));
                AddBehaviour(typeof(CharacterBehaviourNode));
                AddBehaviour(typeof(BrainStatBehaviourNode));
                AddBehaviour(typeof(InventoryBehaviourNode));
                AddBehaviour(typeof(TargetAimBehaviourNode));
                AddBehaviour(typeof(SaveBehaviourNode));
                AddBehaviour(typeof(ActionBehaviourNode));
                AddBehaviour(typeof(TriggerBehaviourNode));
                AddBehaviour(typeof(ListBehaviourNode));
                AddBehaviour(typeof(DebugBehaviourNode));
                Add(typeof(NoteBehaviourNode));
                Add(typeof(ConnectorBehaviourNode));
            }
            else if (graph.isStaminaPack)
            {
                AddTrigger(typeof(StaminaTriggerNode));
                AddTrigger(typeof(ActionTriggerNode));
                AddCondition(typeof(YesConditionNode));
                AddCondition(typeof(NoConditionNode));
                AddBehaviour(typeof(BrainStatBehaviourNode));
                AddBehaviour(typeof(CharacterBehaviourNode));
                AddBehaviour(typeof(VariableBehaviourNode));
                AddBehaviour(typeof(TriggerBehaviourNode));
                AddBehaviour(typeof(ActionBehaviourNode));
                AddBehaviour(typeof(ReturnBehaviourNode));
                AddBehaviour(typeof(DebugBehaviourNode));
                Add(typeof(NoteBehaviourNode));
                Add(typeof(ConnectorBehaviourNode));
            }
            else if (graph.isAttackSkillPack)
            {
                AddTrigger(typeof(AttackSkillTriggerNode));
                AddTrigger(typeof(ActionTriggerNode));
                AddCondition(typeof(YesConditionNode));
                AddCondition(typeof(NoConditionNode));
                AddBehaviour(typeof(CharacterBehaviourNode));
                AddBehaviour(typeof(BrainStatBehaviourNode));
                AddBehaviour(typeof(TargetAimBehaviourNode));
                AddBehaviour(typeof(VariableBehaviourNode));
                AddBehaviour(typeof(ReturnBehaviourNode));
                AddBehaviour(typeof(CacheBehaviourNode));
                AddBehaviour(typeof(ListBehaviourNode));
                AddBehaviour(typeof(TriggerBehaviourNode));
                AddBehaviour(typeof(ActionBehaviourNode));
                AddBehaviour(typeof(AnimatorBehaviourNode));
                AddBehaviour(typeof(DebugBehaviourNode));
                Add(typeof(NoteBehaviourNode));
                Add(typeof(ConnectorBehaviourNode));
            }

            return list;

            void AddTrigger (Type type)
            {
                if (!existed.Contains(type))
                {
                    list.Add($"Trigger/{type.Name.Substring(0, type.Name.IndexOf("TriggerNode", StringComparison.Ordinal))}", type);
                    existed.Add(type);
                }
            }

            void AddCondition (Type type)
            {
                if (!existed.Contains(type))
                {
                    list.Add($"Condition/{type.Name.Substring(0, type.Name.IndexOf("ConditionNode", StringComparison.Ordinal))}", type);
                    existed.Add(type);
                }
            }

            void AddBehaviour (Type type)
            {
                if (!existed.Contains(type))
                {
                    var behaviourNode = (BehaviourNode) Activator.CreateInstance(type);
                    list.Add($"Behaviour/{type.Name.Substring(0, type.Name.IndexOf("BehaviourNode", StringComparison.Ordinal))}", type);
                    existed.Add(type);
                }
            }

            void Add (Type type)
            {
                if (!existed.Contains(type))
                {
                    var behaviourNode = (BehaviourNode) Activator.CreateInstance(type);
                    list.Add($"{type.Name.Substring(0, type.Name.IndexOf("BehaviourNode", StringComparison.Ordinal))}", type);
                    existed.Add(type);
                }
            }
        }

        public string GetStyle (GraphNode node)
        {
            if (node is TriggerNode)
            {
                return "trigger";
            }
            else if (node is RootNode)
            {
                return "root";
            }
            else if (node is ConditionNode)
            {
                return "condition";
            }
            else if (node is BehaviourNode)
            {
                return "behaviour";
            }

            return string.Empty;
        }

        public string GetDisableStyle ()
        {
            return "disable";
        }

        public string GetRedLabelStyle ()
        {
            return "red-label";
        }
        /* END - Custom View end here */

        public ContextualMenuPopulateEvent GetDeleteAction (ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Delete", (Action<DropdownMenuAction>) (a => this.DeleteSelectionCallback(UnityEditor.Experimental.GraphView.GraphView.AskUser.DontAskUser)),
                (Func<DropdownMenuAction, DropdownMenuAction.Status>) (a => this.canDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled));
            return evt;
        }

        public ContextualMenuPopulateEvent GetConnectStartAction (ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Connect to Start", (Action<DropdownMenuAction>) (a =>
            {
                var list = selection.OfType<GraphElement>();
                var connected = false;
                foreach (var element in list)
                    if (element is GraphNodeView nodeView)
                        if (ConnectStartNode(nodeView))
                            connected = true;
                if (connected)
                {
                    //RefreshEdge(FindNodeView(serializer.graph.RootNode));
                    //GraphEditorWindow.RefreshCurrentGraph(true);
                }
            }), (Func<DropdownMenuAction, DropdownMenuAction.Status>) (a => DropdownMenuAction.Status.Normal));
            return evt;
        }

        public ContextualMenuPopulateEvent GetCopyAction (ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Copy", (Action<DropdownMenuAction>) (a =>
            {
                if (copiedNodes == null)
                {
                    copiedNodes = new List<GraphNode>();
                }
                else
                {
                    for (var i = 0; i < copiedNodes.Count(); i++)
                        copiedNodes[i] = null;
                    copiedNodes.Clear();
                }

                var pairing = new Dictionary<string, string>();
                var list = selection.OfType<GraphElement>();
                var enumerable = list as GraphElement[] ?? list.ToArray();
                var firstNodePos = Vector2.zero;
                foreach (var element in enumerable)
                {
                    if (element is GraphNodeView nodeView)
                    {
                        if (nodeView.node is RootNode) continue;
                        var pos = nodeView.GetPosition();
                        var newPos = Vector2.zero;
                        if (firstNodePos == Vector2.zero)
                        {
                            firstNodePos = new Vector2(pos.x, pos.y);
                        }
                        else
                        {
                            newPos.x = pos.x - firstNodePos.x;
                            newPos.y = pos.y - firstNodePos.y;
                        }

                        var cloned = CopyNode(nodeView.node, newPos);
                        pairing.Add(nodeView.node.guid, cloned.guid);
                        copiedNodes.Add(cloned);
                    }
                }

                if (copiedNodes.Count > 0)
                {
                    foreach (var element in enumerable)
                    {
                        if (element is GraphNodeView nodeView)
                        {
                            nodeView.node.RepairParents();
                            if (nodeView.node.parents != null)
                            {
                                for (var j = 0; j < nodeView.node.parents.Count; j++)
                                {
                                    var nodeParent = nodeView.node.parents[j];
                                    if (nodeParent != null)
                                    {
                                        if (pairing.TryGetValue(nodeParent.guid, out var parentId))
                                        {
                                            if (pairing.TryGetValue(nodeView.node.guid, out var clonedId))
                                            {
                                                GraphNode clonedNode = null;
                                                for (var i = 0; i < copiedNodes.Count; i++)
                                                {
                                                    if (copiedNodes[i].guid == clonedId)
                                                    {
                                                        clonedNode = copiedNodes[i];
                                                        break;
                                                    }
                                                }

                                                GraphNode parentNode = null;
                                                for (var i = 0; i < copiedNodes.Count; i++)
                                                {
                                                    if (copiedNodes[i].guid == parentId)
                                                    {
                                                        parentNode = copiedNodes[i];
                                                        break;
                                                    }
                                                }

                                                if (clonedNode != null && parentNode != null)
                                                {
                                                    parentNode.children.Add(clonedNode);
                                                    clonedNode.AddParent(parentNode);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (nodeView.node is TriggerBehaviourNode triggerBehaviourNode)
                            {
                                if (pairing.TryGetValue(triggerBehaviourNode.triggerNodeId, out var triggerNodeId))
                                {
                                    if (pairing.TryGetValue(triggerBehaviourNode.guid, out var clonedId))
                                    {
                                        GraphNode clonedNode = null;
                                        for (var i = 0; i < copiedNodes.Count; i++)
                                        {
                                            if (copiedNodes[i].guid == clonedId)
                                            {
                                                clonedNode = copiedNodes[i];
                                                break;
                                            }
                                        }

                                        if (clonedNode is TriggerBehaviourNode behaviourNode)
                                            behaviourNode.SetTriggerNode(triggerNodeId);
                                    }
                                }
                            }

                            if (nodeView.node is NodeBehaviourNode nodeBehaviourNode)
                            {
                                if (pairing.TryGetValue(nodeBehaviourNode.behaviourNodeId, out var behaviourNodeId))
                                {
                                    if (pairing.TryGetValue(nodeBehaviourNode.guid, out var clonedId))
                                    {
                                        GraphNode clonedNode = null;
                                        for (var i = 0; i < copiedNodes.Count; i++)
                                        {
                                            if (copiedNodes[i].guid == clonedId)
                                            {
                                                clonedNode = copiedNodes[i];
                                                break;
                                            }
                                        }

                                        if (clonedNode is NodeBehaviourNode behaviourNode)
                                            behaviourNode.SetBehaviourNode(behaviourNodeId);
                                    }
                                }
                            }

                            if (nodeView.node is InventoryTriggerNode inventoryTriggerNode)
                            {
                                if (pairing.TryGetValue(inventoryTriggerNode.ItemCache, out var cacheNodeId))
                                {
                                    if (pairing.TryGetValue(inventoryTriggerNode.guid, out var clonedId))
                                    {
                                        GraphNode clonedNode = null;
                                        for (var i = 0; i < copiedNodes.Count; i++)
                                        {
                                            if (copiedNodes[i].guid == clonedId)
                                            {
                                                clonedNode = copiedNodes[i];
                                                break;
                                            }
                                        }

                                        if (clonedNode is InventoryTriggerNode triggerNode)
                                            triggerNode.SetItemCache(cacheNodeId);
                                    }
                                }
                            }

                            if (nodeView.node is InventoryBehaviourNode inventoryBehaviourNode)
                            {
                                if (pairing.TryGetValue(inventoryBehaviourNode.ItemCache, out var cacheNodeId))
                                {
                                    if (pairing.TryGetValue(inventoryBehaviourNode.guid, out var clonedId))
                                    {
                                        GraphNode clonedNode = null;
                                        for (var i = 0; i < copiedNodes.Count; i++)
                                        {
                                            if (copiedNodes[i].guid == clonedId)
                                            {
                                                clonedNode = copiedNodes[i];
                                                break;
                                            }
                                        }

                                        if (clonedNode is InventoryBehaviourNode behaviourNode)
                                            behaviourNode.SetItemCache(cacheNodeId);
                                    }
                                }
                            }
                        }
                    }
                }
            }), (Func<DropdownMenuAction, DropdownMenuAction.Status>) (a => this.canCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled));
            return evt;
        }

        public ContextualMenuPopulateEvent GetPasteAction (ContextualMenuPopulateEvent evt, Vector2 nodePosition)
        {
            evt.menu.AppendAction("Paste", (Action<DropdownMenuAction>) (a =>
            {
                if (copiedNodes is {Count: > 0})
                {
                    var pairing = new Dictionary<string, string>();
                    var mousePosition = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
                    for (var i = 0; i < copiedNodes.Count; i++)
                    {
                        var cloned = PasteNode(copiedNodes[i], copiedNodes[i].position + nodePosition);
                        pairing.Add(copiedNodes[i].guid, cloned.node.guid);
                    }

                    for (var i = 0; i < copiedNodes.Count; i++)
                    {
                        copiedNodes[i].RepairParents();
                        if (copiedNodes[i].parents != null)
                        {
                            for (var j = 0; j < copiedNodes[i].parents.Count; j++)
                            {
                                var nodeParent = copiedNodes[i].parents[j];
                                if (nodeParent != null)
                                {
                                    if (pairing.TryGetValue(nodeParent.guid, out var parentId))
                                    {
                                        if (pairing.TryGetValue(copiedNodes[i].guid, out var clonedId))
                                        {
                                            var clonedView = FindNodeView(clonedId);
                                            var clonedParentView = FindNodeView(parentId);
                                            if (clonedView != null && clonedParentView != null)
                                            {
                                                serializer.AddChild(clonedParentView.node, clonedView.node);
                                                clonedView.AddParentElement(clonedParentView.node);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (copiedNodes[i] is TriggerBehaviourNode triggerBehaviourNode)
                        {
                            if (pairing.TryGetValue(triggerBehaviourNode.triggerNodeId, out var triggerNodeId))
                            {
                                if (pairing.TryGetValue(triggerBehaviourNode.guid, out var clonedId))
                                {
                                    var clonedView = FindNodeView(clonedId);
                                    if (clonedView != null)
                                    {
                                        var serializedNode = serializer.FindNode(serializer.Nodes, clonedView.node);
                                        if (serializedNode != null)
                                        {
                                            serializedNode.serializedObject.Update();
                                            serializedNode.FindPropertyRelative("triggerNode").stringValue = triggerNodeId;
                                            serializedNode.FindPropertyRelative("dirty").boolValue = true;
                                            serializedNode.serializedObject.ApplyModifiedProperties();
                                        }
                                    }
                                }
                            }
                        }

                        if (copiedNodes[i] is NodeBehaviourNode nodeBehaviourNode)
                        {
                            if (pairing.TryGetValue(nodeBehaviourNode.behaviourNodeId, out var behaviourNodeId))
                            {
                                if (pairing.TryGetValue(nodeBehaviourNode.guid, out var clonedId))
                                {
                                    var clonedView = FindNodeView(clonedId);
                                    if (clonedView != null)
                                    {
                                        var serializedNode = serializer.FindNode(serializer.Nodes, clonedView.node);
                                        if (serializedNode != null)
                                        {
                                            serializedNode.serializedObject.Update();
                                            serializedNode.FindPropertyRelative("behaviourNode").stringValue = behaviourNodeId;
                                            serializedNode.FindPropertyRelative("dirty").boolValue = true;
                                            serializedNode.serializedObject.ApplyModifiedProperties();
                                        }
                                    }
                                }
                            }
                        }

                        if (copiedNodes[i] is InventoryTriggerNode inventoryTriggerNode)
                        {
                            if (pairing.TryGetValue(inventoryTriggerNode.ItemCache, out var cacheNodeId))
                            {
                                if (pairing.TryGetValue(inventoryTriggerNode.guid, out var clonedId))
                                {
                                    var clonedView = FindNodeView(clonedId);
                                    if (clonedView != null)
                                    {
                                        var serializedNode = serializer.FindNode(serializer.Nodes, clonedView.node);
                                        if (serializedNode != null)
                                        {
                                            serializedNode.serializedObject.Update();
                                            serializedNode.FindPropertyRelative("itemCache").stringValue = cacheNodeId;
                                            serializedNode.FindPropertyRelative("dirty").boolValue = true;
                                            serializedNode.serializedObject.ApplyModifiedProperties();
                                        }
                                    }
                                }
                            }
                        }

                        if (copiedNodes[i] is InventoryBehaviourNode inventoryBehaviourNode)
                        {
                            if (pairing.TryGetValue(inventoryBehaviourNode.ItemCache, out var cacheNodeId))
                            {
                                if (pairing.TryGetValue(inventoryBehaviourNode.guid, out var clonedId))
                                {
                                    var clonedView = FindNodeView(clonedId);
                                    if (clonedView != null)
                                    {
                                        var serializedNode = serializer.FindNode(serializer.Nodes, clonedView.node);
                                        if (serializedNode != null)
                                        {
                                            serializedNode.serializedObject.Update();
                                            serializedNode.FindPropertyRelative("itemCache").stringValue = cacheNodeId;
                                            serializedNode.FindPropertyRelative("dirty").boolValue = true;
                                            serializedNode.serializedObject.ApplyModifiedProperties();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }), (Func<DropdownMenuAction, DropdownMenuAction.Status>) (a => copiedNodes is {Count: > 0} ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled));
            return evt;
        }

        public ContextualMenuPopulateEvent GetDuplicateAction (ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Duplicate", (Action<DropdownMenuAction>) (a =>
            {
                var pairing = new Dictionary<string, string>();
                var list = selection.OfType<GraphElement>();
                var enumerable = list as GraphElement[] ?? list.ToArray();
                foreach (var element in enumerable)
                {
                    if (element is GraphNodeView nodeView)
                    {
                        if (nodeView.node is RootNode) continue;
                        var pos = nodeView.GetPosition();
                        var cloned = DuplicateNode(nodeView.node, new Vector2(pos.x + 10, pos.y + 10));
                        pairing.Add(nodeView.node.guid, cloned.node.guid);
                    }
                }

                foreach (var element in enumerable)
                {
                    if (element is GraphNodeView nodeView)
                    {
                        nodeView.node.RepairParents();        
                        if (nodeView.node.parents != null)
                        {
                            for (var j = 0; j < nodeView.node.parents.Count; j++)
                            {
                                var nodeParent = nodeView.node.parents[j];
                                if (nodeParent != null)
                                {
                                    if (pairing.TryGetValue(nodeParent.guid, out var parentId))
                                    {
                                        if (pairing.TryGetValue(nodeView.node.guid, out var clonedId))
                                        {
                                            var clonedView = FindNodeView(clonedId);
                                            var clonedParentView = FindNodeView(parentId);
                                            if (clonedView != null && clonedParentView != null)
                                            {
                                                serializer.AddChild(clonedParentView.node, clonedView.node);
                                                clonedView.AddParentElement(clonedParentView.node);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }), (Func<DropdownMenuAction, DropdownMenuAction.Status>) (a => this.canDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled));
            return evt;
        }

        void SelectFolder (string path)
        {
            // https://forum.unity.com/threads/selecting-a-folder-in-the-project-via-button-in-editor-window.355357/
            // Check the path has no '/' at the end, if it does remove it,
            // Obviously in this example it doesn't but it might
            // if your getting the path some other way.

            if (path[path.Length - 1] == '/')
                path = path.Substring(0, path.Length - 1);

            // Load object
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

            // Select the object in the project folder
            Selection.activeObject = obj;

            // Also flash the folder yellow to highlight it
            EditorGUIUtility.PingObject(obj);
        }

        void CreateNode (System.Type type, Vector2 position)
        {
            var node = serializer.CreateNode(type, position);
            CreateNodeView(node);
        }

        GraphNode CopyNode (GraphNode selectedNode, Vector2 position)
        {
            return serializer.CloneNode(selectedNode, position, false);
        }

        GraphNodeView PasteNode (GraphNode selectedNode, Vector2 position)
        {
            var node = serializer.CloneNode(selectedNode, position);
            if (selectedNode is NoteBehaviourNode noteBehaviourNode)
            {
                var graphId = serializer.GetLatestGraphId();
                if (!graphId.Equals("0"))
                {
                    var settings = GraphSettings.GetSettings();
                    if (settings != null && settings.graphNoteDb != null)
                    {
                        var previousMessage = settings.graphNoteDb.GetNote(noteBehaviourNode.Message.reid);
                        if (!string.IsNullOrEmpty(previousMessage))
                        {
                            if (node is NoteBehaviourNode newNoteBehaviourNode)
                            {
                                settings.graphNoteDb.SetNote($"{graphId}_{newNoteBehaviourNode.Message.reid}", previousMessage);
                                EditorUtility.SetDirty(settings.graphNoteDb);
                            }
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Paste Graph Note", "Some graph note does not paste successfully due to the graph is not saved.", "OK");
                }
            }
            else if (selectedNode is EventBehaviourNode)
            {
                EditorUtility.DisplayDialog("Paste Graph Node", "EventBehaviourNode does not paste successfully due to it is not supported.", "OK");
            }

            return CreateNodeView(node);
        }

        GraphNodeView DuplicateNode (GraphNode selectedNode, Vector2 position)
        {
            var node = serializer.CloneNode(selectedNode, position);
            return CreateNodeView(node);
        }

        bool ConnectStartNode (GraphNodeView selectedNode)
        {
            return serializer.ConnectStartNode(selectedNode);
        }

        GraphNodeView CreateNodeView (GraphNode node)
        {
            GraphNodeView nodeView = new GraphNodeView(serializer, node, this);
            nodeView.OnNodeSelected = OnSelectNode;
            nodeView.OnNodeUnselected = OnUnselectNode;
            AddElement(nodeView);
            return nodeView;
        }

        private void OnSelectNode (GraphNodeView node)
        {
            selectedNodeView = node;
            OnNodeSelected(node);
        }

        private void OnUnselectNode (GraphNodeView node)
        {
            OnNodeUnselected(node);
            selectedNodeView = null;
        }

        public void UpdateNodeStates ()
        {
            if (serializer != null)
                serializer.graph.selectedViewNode = selection;
            nodes.ForEach(n =>
            {
                GraphNodeView view = n as GraphNodeView;
                view.Update();
            });
        }

        public void UpdateReferenceDescriptionLabel (string nodeId)
        {
            nodes.ForEach(n =>
            {
                GraphNodeView view = n as GraphNodeView;
                if (view.node != null)
                    view.ApplyUpdateDescriptionLabel(nodeId);
            });
        }

        public bool HighlightReferenceNode (string nodeId)
        {
            var highlighted = false;
            nodes.ForEach(n =>
            {
                GraphNodeView view = n as GraphNodeView;
                if (view.node != null && view.node.guid == nodeId)
                {
                    view.ApplyRunningHighlight();
                    highlighted = true;
                }
            });

            return highlighted;
        }

        public void HighlightBeingReferenceNode (string nodeId)
        {
            nodes.ForEach(n =>
            {
                if (n is GraphNodeView {node: { }} view)
                {
                    if (view.node is TriggerBehaviourNode triggerBehaviourNode)
                    {
                        if (triggerBehaviourNode.triggerNodeId == nodeId)
                            view.ApplyRunningHighlight();
                    }
                    else if (view.node is ActionBehaviourNode actionBehaviourNode)
                    {
                        if (serializer.IsSelectedGraphObject(actionBehaviourNode))
                        {
                            var refNode = FindNodeView(nodeId);
                            if (refNode is {node: ActionTriggerNode actionTriggerNode})
                            {
                                if (actionBehaviourNode.ActionName == actionTriggerNode.ActionName)
                                    view.ApplyRunningHighlight();
                            }
                        }
                    }
                    else if (view.node is ActionTriggerNode actionTriggerNode)
                    {
                        var refNode = FindNodeView(nodeId);
                        if (refNode is {node: ActionBehaviourNode actionBehaviourNode2})
                        {
                            if (serializer.IsSelectedGraphObject(actionBehaviourNode2))
                            {
                                if (actionBehaviourNode2.ActionName == actionTriggerNode.ActionName)
                                    view.ApplyRunningHighlight();
                            }
                        }
                    }
                    else if (view.node is VariableBehaviourNode variableBehaviourNode)
                    {
                        var refNode = FindNodeView(nodeId);
                        if (refNode != null && view.node != refNode.node && refNode.node is VariableBehaviourNode variableBehaviourNode2)
                        {
                            var variable = variableBehaviourNode.GetVariable();
                            if (variable != null)
                            {
                                var variable2 = variableBehaviourNode2.GetVariable();
                                if (variable2 != null && variable == variable2)
                                    view.ApplyRunningHighlight();
                            }
                        }
                    }
                }
            });
        }

        public void UnhighlightReferenceNode (string nodeId)
        {
            nodes.ForEach(n =>
            {
                GraphNodeView view = n as GraphNodeView;
                if (view.node != null && view.node.guid == nodeId)
                    view.UnapplyRunningHighlight();
            });
        }

        public void UnhighlightBeingReferenceNode (string nodeId)
        {
            nodes.ForEach(n =>
            {
                if (n is GraphNodeView {node: { }} view)
                {
                    if (view.node is TriggerBehaviourNode triggerBehaviourNode)
                    {
                        if (triggerBehaviourNode.triggerNodeId == nodeId)
                            view.UnapplyRunningHighlight();
                    }
                    else if (view.node is ActionBehaviourNode actionBehaviourNode)
                    {
                        if (serializer.IsSelectedGraphObject(actionBehaviourNode))
                        {
                            var refNode = FindNodeView(nodeId);
                            if (refNode is {node: ActionTriggerNode actionTriggerNode})
                            {
                                if (actionBehaviourNode.ActionName == actionTriggerNode.ActionName)
                                    view.UnapplyRunningHighlight();
                            }
                        }
                    }
                    else if (view.node is ActionTriggerNode actionTriggerNode)
                    {
                        var refNode = FindNodeView(nodeId);
                        if (refNode is {node: ActionBehaviourNode actionBehaviourNode2})
                        {
                            if (serializer.IsSelectedGraphObject(actionBehaviourNode2))
                            {
                                if (actionBehaviourNode2.ActionName == actionTriggerNode.ActionName)
                                    view.UnapplyRunningHighlight();
                            }
                        }
                    }
                    else if (view.node is VariableBehaviourNode variableBehaviourNode)
                    {
                        var refNode = FindNodeView(nodeId);
                        if (refNode != null && view.node != refNode.node && refNode.node is VariableBehaviourNode variableBehaviourNode2)
                        {
                            var variable = variableBehaviourNode.GetVariable();
                            if (variable != null)
                            {
                                var variable2 = variableBehaviourNode2.GetVariable();
                                if (variable2 != null && variable == variable2)
                                    view.UnapplyRunningHighlight();
                            }
                        }
                    }
                }
            });
        }

        public void UnhighlightAllReferenceNode ()
        {
            nodes.ForEach(n =>
            {
                GraphNodeView view = n as GraphNodeView;
                view.UnapplyRunningHighlight();
            });
        }

        private void CleanElements (IEnumerable<GraphElement> elements)
        {
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(elements.ToList());
            graphViewChanged += OnGraphViewChanged;
        }

        private void AddChildElements (GraphNode parentNode)
        {
            var children = Graph.GetChildren(parentNode);
            children.ForEach(c =>
            {
                GraphNodeView parentView = FindNodeView(parentNode);
                GraphNodeView childView = FindNodeView(c);
                if (childView != null && parentView != null)
                {
                    var edge = parentView.output.ConnectTo(childView.input);
                    if (edge != null)
                    {
                        AddElement(edge);
                        childView.UpdateInputPortColor();
                    }
                }
            });
        }
    }
}