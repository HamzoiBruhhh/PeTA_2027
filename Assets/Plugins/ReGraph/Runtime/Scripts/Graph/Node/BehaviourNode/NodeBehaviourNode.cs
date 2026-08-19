using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class NodeBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            Enable = 10,
            Disable = 11,
        }
        
        [SerializeReference]
        [OnValueChanged("MarkDirty")]
        [ValueDropdown("DrawBehaviourListDropdown", ExpandAllMenuItems = true)]
        private string behaviourNode;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        public string behaviourNodeId => behaviourNode;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (string.IsNullOrEmpty(behaviourNode) || executionType == ExecutionType.None)
            {
                LogWarning("Found an empty Behaviour Behaviour node in " + context.objectName);
            }
            else
            {
                for (int i = 0; i < context.graph.nodes.Count; i++)
                {
                    if (context.graph.nodes[i] is BehaviourNode behaviour)
                    {
                        if (behaviour.BehaviourId == behaviourNode)
                        {
                            if (executionType == ExecutionType.Enable)
                                behaviour.enabled = true;
                            else if (executionType == ExecutionType.Disable)
                                behaviour.enabled = false;
#if UNITY_EDITOR
                            behaviour.OnEnableChange();
#endif
                            break;
                        }
                    }
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        public void SetBehaviourNode (string value)
        {
            behaviourNode = value;
        }
        
        private IEnumerable DrawBehaviourListDropdown ()
        {
            var actionNameListDropdown = new ValueDropdownList<string>();
            var graph = GetGraph();
            if (graph is {nodes: { }})
            {
                for (var i = 0; i < graph.nodes.Count; i++)
                {
                    if (graph.nodes[i] is BehaviourNode)
                    {
                        var node = (BehaviourNode) graph.nodes[i];
                        {
                            if (node is not NoteBehaviourNode)
                            {
                                var identityName = node.GetNodeIdentityName();
                                if (!string.IsNullOrEmpty(identityName))
                                    actionNameListDropdown.Add(node.GetNodeViewTitle() + " : " + identityName + " (" + node.BehaviourId + ")", node.BehaviourId);
                                else
                                    actionNameListDropdown.Add(node.GetNodeViewTitle() + " (" + node.BehaviourId + ")", node.BehaviourId);
                            }
                        }
                    }
                }
            }

            return actionNameListDropdown;
        }
        
        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Enable", ExecutionType.Enable},
            {"Disable", ExecutionType.Disable}
        };

        public static string displayName = "Node Behaviour Node";
        public static string nodeName = "Node";

        public override string GetNodeInspectorTitle ()
        {
            return displayName;
        }

        public override string GetNodeViewTitle ()
        {
            return nodeName;
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Logic/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (!string.IsNullOrEmpty(behaviourNode))
            {
                var behaviourNodeName = string.Empty;
                var graph = GetGraph();
                if (graph is {nodes: { }})
                {
                    for (int i = 0; i < graph.nodes.Count; i++)
                    {
                        if (graph.nodes[i] is BehaviourNode)
                        {
                            var behaviour = (BehaviourNode) graph.nodes[i];
                            if (behaviour.guid == behaviourNode)
                            {
                                var identityName = behaviour.GetNodeIdentityName();
                                if (!string.IsNullOrEmpty(identityName))
                                    behaviourNodeName = behaviour.GetNodeViewTitle() + " : " + identityName + " (" + behaviour.BehaviourId + ")";
                                else
                                    behaviourNodeName = behaviour.GetNodeViewTitle() + " (" + behaviour.BehaviourId + ")";
                            }
                        }
                    }
                }

                if (executionType == ExecutionType.Enable)
                    return "Enable " + behaviourNodeName;
                if (executionType == ExecutionType.Disable)
                    return "Disable " + behaviourNodeName;
            }

            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will provide several controls (enable, disable) to a specific node in the same graph.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}