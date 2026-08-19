using System;
using System.Collections.Generic;
using System.Linq;
using Reshape.Unity;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor;

namespace Reshape.ReGraph
{
    public class GraphNodeView : UnityEditor.Experimental.GraphView.Node
    {
        private const string BEHAVIOUR = "Behaviour";
        private const string BEHAVIOUR_CONDITION = "Behaviour / Condition";

        private static readonly Color portUnreachableColor = new Color(0.89f, 0.46f, 0.46f);
        private static readonly Color portDefaultColor = new Color(0.9411765f, 0.9411765f, 0.9411765f);

        public Action<GraphNodeView> OnNodeSelected;
        public Action<GraphNodeView> OnNodeUnselected;
        public SerializedGraph serializer;
        public GraphNode node;
        public GraphViewer viewer;
        public Port input;
        public Port output;
        public Port export;

        private Label titleLabel;
        private Label descriptionLabel;
        private Label connectLabel;

        public GraphNodeView (SerializedGraph tree, GraphNode node, GraphViewer viewer) : base(AssetDatabase.GetAssetPath(GraphSettings.GetSettings().graphNodeXml))
        {
            serializer = tree;
            this.node = node;
            this.viewer = viewer;
            if (node != null)
            {
                if (node is RootNode)
                    capabilities -= Capabilities.Deletable;
                title = node.GetNodeViewTitle();
                viewDataKey = node.guid;
                style.left = node.position.x;
                style.top = node.position.y;

                CreateInputPorts();
                CreateOutputPorts();
                /*
                //~~ TODO extra output port as export node
                CreateExportPorts();
                */
                SetupClasses();
                SetupDataBinding();
            }
            else
            {
                ReDebug.LogWarning("Graph Editor", "System found a null graph node inside " + node.GetGraphSelectionName(), false);
            }
        }

        private void SetupDataBinding ()
        {
            var nodeProp = serializer.FindNode(serializer.Nodes, node);

            descriptionLabel = this.Q<Label>("description");
            UpdateDescriptionLabel();

            Label categoryLabel = this.Q<Label>("category");
            if (node is TriggerNode)
                categoryLabel.text = "Trigger";
            else if (node is ConditionNode)
                categoryLabel.text = "Condition";
            else if (node is BehaviourNode)
                categoryLabel.text = BEHAVIOUR;

            titleLabel = this.Q<Label>("title-label");
            UpdateTooltipLabel();

            connectLabel = this.Q<Label>("connectTo");
            if (node is RootNode)
                connectLabel.text = "Trigger";
            else if (node is TriggerNode)
                connectLabel.text = BEHAVIOUR;
            else if (node is BehaviourNode)
                connectLabel.text = BEHAVIOUR;
            UpdateConnectLabel();

            this.node.onEnableChange -= OnEnableChange;
            this.node.onEnableChange += OnEnableChange;
        }

        private void OnEnableChange ()
        {
            if (this.node.enabled)
                RemoveFromClassList(viewer.GetDisableStyle());
            else
                RemoveFromClassList(viewer.GetStyle(node));
            SetupClasses();
        }

        private void SetupClasses ()
        {
            if (node is ConnectorBehaviourNode)
            {
                AddToClassList("connectorTitle");
                AddToClassList("connectorDescription");
            }

            if (node is NoteBehaviourNode)
            {
                AddToClassList("noteInput");
                AddToClassList("noteOutput");
                AddToClassList("noteTitle");
                AddToClassList("noteNode");
                AddToClassList("noteDescription");
            }
            else if (!node.enabled)
                AddToClassList(viewer.GetDisableStyle());
            else
                AddToClassList(viewer.GetStyle(node));
        }

        private void CreateInputPorts ()
        {
            if (node is RootNode or NoteBehaviourNode == false)
                input = new GraphNodePort(Direction.Input, Port.Capacity.Multi);
            if (input != null)
            {
                input.portName = string.Empty;
                input.style.flexDirection = FlexDirection.Column;
                inputContainer.Add(input);
            }
        }

        private void CreateOutputPorts ()
        {
            if (node.GetChildrenType() == GraphNode.ChildrenType.Single)
                output = new GraphNodePort(Direction.Output, Port.Capacity.Single);
            else if (node.GetChildrenType() == GraphNode.ChildrenType.Multiple)
                output = new GraphNodePort(Direction.Output, Port.Capacity.Multi);
            if (output != null)
            {
                output.portName = string.Empty;
                output.style.flexDirection = FlexDirection.ColumnReverse;
                outputContainer.Add(output);
            }
        }

        private void CreateExportPorts ()
        {
            export = new GraphNodePort(Direction.Output, Port.Capacity.Single);
            if (export != null)
            {
                export.portName = "";
                export.style.flexDirection = FlexDirection.RowReverse;
                export.style.position = new StyleEnum<Position>(Position.Absolute);
                export.style.top = new StyleLength(-3f);
                export.style.left = new StyleLength(115f);
                extensionContainer.Add(export);
            }
        }

        public override void SetPosition (Rect newPos)
        {
            base.SetPosition(newPos);

            Vector2 position = new Vector2(newPos.xMin, newPos.yMin);
            serializer.SetNodePosition(node, position);
        }

        public override void OnSelected ()
        {
            base.OnSelected();
            if (OnNodeSelected != null)
            {
                OnNodeSelected.Invoke(this);
            }

            if (serializer.graph.selectedViewNode.Count == 1)
                HighlightReference();
            else
                viewer.UnhighlightAllReferenceNode();
        }

        public override void OnUnselected ()
        {
            base.OnUnselected();
            if (OnNodeUnselected != null)
            {
                OnNodeUnselected.Invoke(this);
            }

            if (node != null)
            {
                if (node is TriggerBehaviourNode triggerBehaviourNode)
                {
                    if (!string.IsNullOrEmpty(triggerBehaviourNode.triggerNodeId))
                        viewer.UnhighlightReferenceNode(triggerBehaviourNode.triggerNodeId);
                }
                else if (node is NodeBehaviourNode nodeBehaviourNode)
                {
                    if (!string.IsNullOrEmpty(nodeBehaviourNode.behaviourNodeId))
                        viewer.UnhighlightReferenceNode(nodeBehaviourNode.behaviourNodeId);
                }
                else if (node is ActionTriggerNode actionTriggerNode)
                {
                    if (!string.IsNullOrEmpty(actionTriggerNode.TriggerId))
                        viewer.UnhighlightBeingReferenceNode(actionTriggerNode.TriggerId);
                }
                else if (node is ActionBehaviourNode actionBehaviourNode)
                {
                    if (actionBehaviourNode.Runner != null || actionBehaviourNode.Scriptable != null)
                        if (!string.IsNullOrEmpty(actionBehaviourNode.BehaviourId))
                            viewer.UnhighlightBeingReferenceNode(actionBehaviourNode.BehaviourId);
                }
                else if (node is VariableBehaviourNode variableBehaviourNode)
                {
                    var variable = variableBehaviourNode.GetVariable();
                    if (variable != null)
                        viewer.UnhighlightBeingReferenceNode(variableBehaviourNode.BehaviourId);
                }
            }
        }

        public List<GraphNode> SortChildren ()
        {
            if (node is RootNode or TriggerNode or BehaviourNode)
            {
                var gNode = (GraphNode) node;
                List<GraphNode> sorted = gNode.children.ToList();
                sorted.Sort(SortByHorizontalPosition);
                return sorted;
            }

            return null;
        }

        private int SortByHorizontalPosition (GraphNode left, GraphNode right)
        {
            var leftX = left == null ? float.MaxValue : left.position.x;
            var rightX = right == null ? float.MaxValue : right.position.x;
            return leftX < rightX ? -1 : 1;
        }

        public override void BuildContextualMenu (ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphNodeView nodeView)
            {
                if (nodeView.node is RootNode == false)
                {
                    if (nodeView.node is TriggerNode trigger)
                        evt = viewer.GetConnectStartAction(evt);
                    evt = viewer.GetDeleteAction(evt);
                    evt = viewer.GetDuplicateAction(evt);
                    evt = viewer.GetCopyAction(evt);
                    evt.menu.AppendSeparator();
                }
            }

            base.BuildContextualMenu(evt);
        }

        public void Update ()
        {
            if (node is {dirty: true})
            {
                UpdateDescriptionLabel();
                UpdateTooltipLabel();
                node.dirty = false;
                serializer.SaveNode(node);

                viewer.UnhighlightAllReferenceNode();
                HighlightReference();

                if (node.forceRepaint)
                {
                    node.forceRepaint = false;
                    UpdateConnectLabel();
                    UpdateChildrenPortColor();
                }
            }

            UpdateState();
        }

        private void UpdateDescriptionLabel ()
        {
            if (node is NoteBehaviourNode noteNode)
            {
                var settings = GraphSettings.GetSettings();
                if (settings != null && settings.graphNoteDb != null)
                {
                    string graphId = serializer.GetLatestGraphId();
                    var message = string.Empty;
                    if (!graphId.Equals("0"))
                    {
                        var uid = $"{graphId}_{noteNode.Message.reid}";
                        message = settings.graphNoteDb.GetNote(uid);
                    }

                    if (!string.IsNullOrEmpty(message))
                        descriptionLabel.text = message;
                    else
                        descriptionLabel.text = node.GetNodeViewDescription();
                }
                else
                {
                    descriptionLabel.text = node.GetNodeViewDescription();
                }
            }
            else
            {
                descriptionLabel.text = node.GetNodeViewDescription();
                if (string.IsNullOrEmpty(descriptionLabel.text))
                    AddToClassList(viewer.GetRedLabelStyle());
                else
                    RemoveFromClassList(viewer.GetRedLabelStyle());
            }

            if (node is ActionTriggerNode triggerNode)
            {
                viewer.UpdateReferenceDescriptionLabel(triggerNode.TriggerId);
            }
        }

        private void UpdateTooltipLabel ()
        {
            titleLabel.tooltip = node.GetNodeViewTooltip();
        }

        public void HighlightReference ()
        {
            if (node != null)
            {
                if (node is TriggerBehaviourNode triggerBehaviourNode)
                {
                    if (!string.IsNullOrEmpty(triggerBehaviourNode.triggerNodeId))
                        if (viewer.HighlightReferenceNode(triggerBehaviourNode.triggerNodeId))
                            UpdateDescriptionLabel();
                }
                else if (node is NodeBehaviourNode nodeBehaviourNode)
                {
                    if (!string.IsNullOrEmpty(nodeBehaviourNode.behaviourNodeId))
                        if (viewer.HighlightReferenceNode(nodeBehaviourNode.behaviourNodeId))
                            UpdateDescriptionLabel();
                }
                else if (node is ActionBehaviourNode actionBehaviourNode)
                {
                    if (actionBehaviourNode.Runner != null || actionBehaviourNode.Scriptable != null)
                        if (!string.IsNullOrEmpty(actionBehaviourNode.ActionName))
                            viewer.HighlightBeingReferenceNode(actionBehaviourNode.BehaviourId);
                }
                else if (node is ActionTriggerNode actionTriggerNode)
                {
                    viewer.HighlightBeingReferenceNode(actionTriggerNode.TriggerId);
                }
                else if (node is VariableBehaviourNode variableBehaviourNode)
                {
                    var variable = variableBehaviourNode.GetVariable();
                    if (variable != null)
                        viewer.HighlightBeingReferenceNode(variableBehaviourNode.BehaviourId);
                }
            }
        }

        public void UnhighlightReference ()
        {
            if (node != null)
            {
                if (node is TriggerBehaviourNode triggerBehaviourNode)
                {
                    if (!string.IsNullOrEmpty(triggerBehaviourNode.triggerNodeId))
                        viewer.UnhighlightReferenceNode(triggerBehaviourNode.triggerNodeId);
                }
                else if (node is NodeBehaviourNode nodeBehaviourNode)
                {
                    if (!string.IsNullOrEmpty(nodeBehaviourNode.behaviourNodeId))
                        viewer.UnhighlightReferenceNode(nodeBehaviourNode.behaviourNodeId);
                }
                else if (node is ActionBehaviourNode actionBehaviourNode)
                {
                    if (actionBehaviourNode.Runner != null || actionBehaviourNode.Scriptable != null)
                    {
                        if (!string.IsNullOrEmpty(actionBehaviourNode.ActionName))
                            viewer.UnhighlightBeingReferenceNode(actionBehaviourNode.BehaviourId);
                    }
                }
                else if (node is ActionTriggerNode actionTriggerNode)
                {
                    if (!string.IsNullOrEmpty(actionTriggerNode.TriggerId))
                        viewer.UnhighlightBeingReferenceNode(actionTriggerNode.TriggerId);
                }
                else if (node is VariableBehaviourNode variableBehaviourNode)
                {
                    var variable = variableBehaviourNode.GetVariable();
                    if (variable != null)
                        viewer.UnhighlightBeingReferenceNode(variableBehaviourNode.BehaviourId);
                }
            }
        }

        public void ApplyUpdateDescriptionLabel (string nodeId)
        {
            if (node != null)
            {
                if (node is TriggerBehaviourNode triggerNode)
                {
                    if (!string.IsNullOrEmpty(triggerNode.triggerNodeId))
                        if (string.Equals(triggerNode.triggerNodeId, nodeId))
                            UpdateDescriptionLabel();
                }
                else if (node is NodeBehaviourNode behaviourNode)
                {
                    if (!string.IsNullOrEmpty(behaviourNode.behaviourNodeId))
                        if (string.Equals(behaviourNode.behaviourNodeId, nodeId))
                            UpdateDescriptionLabel();
                }
            }
        }

        public void ApplyRunningHighlight ()
        {
            AddToClassList("running");
        }

        public void UnapplyRunningHighlight ()
        {
            RemoveFromClassList("running");
        }

        public void ResetInputPortColor ()
        {
            if (input == null)
                return;
            if (input.portColor != portDefaultColor)
            {
                input.portColor = portDefaultColor;
                input.MarkDirtyRepaint();
                input.highlight = false;
            }
        }

        public void UpdateInputPortColor (GraphNodeView parentView)
        {
            if (input == null || parentView == null)
                return;
            if (!parentView.node.IsPortReachable(node))
            {
                if (input.portColor != portUnreachableColor)
                {
                    input.portColor = portUnreachableColor;
                    input.MarkDirtyRepaint();
                    input.highlight = false;
                }
            }
        }

        public void UpdateInputPortColor ()
        {
            if (input == null)
                return;
            if (input.node != null && input.connected)
            {
                var connection = input.connections.First();
                if (connection.output != null)
                {
                    var viewNode = (GraphNodeView) connection.output.node;
                    UpdateInputPortColor(viewNode);
                }
            }
        }

        public void UpdateChildrenPortColor ()
        {
            if (output != null && output.node != null && output.connected)
            {
                foreach (var connection in output.connections)
                {
                    if (connection.input != null)
                    {
                        var viewNode = (GraphNodeView) connection.input.node;
                        viewNode.ResetInputPortColor();
                        viewNode.UpdateInputPortColor(this);
                        connection.MarkDirtyRepaint();
                    }
                }

                viewer.RefreshEdge(this);
            }
        }

        private void UpdateConnectLabel ()
        {
            if (node is VariableBehaviourNode behaviourNode)
            {
                connectLabel.text = behaviourNode.AcceptConditionNode() ? BEHAVIOUR_CONDITION : BEHAVIOUR;
            }
            else if (node is InventoryBehaviourNode inventoryNode)
            {
                connectLabel.text = inventoryNode.AcceptConditionNode() ? BEHAVIOUR_CONDITION : BEHAVIOUR;
            }
            else if (node is CharacterBehaviourNode characterBehaviourNode)
            {
                connectLabel.text = characterBehaviourNode.AcceptConditionNode() ? BEHAVIOUR_CONDITION : BEHAVIOUR;
            }
            else if (node is StaminaBehaviourNode staminaBehaviourNode)
            {
                connectLabel.text = staminaBehaviourNode.AcceptConditionNode() ? BEHAVIOUR_CONDITION : BEHAVIOUR;
            }
            else if (node is DialogBehaviourNode dialogBehaviourNode)
            {
                connectLabel.text = dialogBehaviourNode.AcceptConditionNode() ? BEHAVIOUR_CONDITION : BEHAVIOUR;
            }
            else if (node is TargetAimBehaviourNode behave)
            {
                connectLabel.text = behave.AcceptConditionNode() ? BEHAVIOUR_CONDITION : BEHAVIOUR;
            }
        }

        public void AddParentElement (GraphNode parentNode)
        {
            var parentView = viewer.FindNodeView(parentNode);
            var edge = parentView?.output.ConnectTo(input);
            if (edge != null)
                viewer.AddElement(edge);
        }

        public void UpdateState ()
        {
            /*RemoveFromClassList("running");
            RemoveFromClassList("failure");
            RemoveFromClassList("success");

            if (EditorApplication.isPlaying)
            {
                switch (node.state)
                {
                    case GraphNode.State.Running:
                        if (node.started)
                        {
                            AddToClassList("running");
                        }

                        break;
                    case GraphNode.State.Failure:
                        AddToClassList("failure");
                        break;
                    case GraphNode.State.Success:
                        AddToClassList("success");
                        break;
                }
            }*/
        }
    }
}