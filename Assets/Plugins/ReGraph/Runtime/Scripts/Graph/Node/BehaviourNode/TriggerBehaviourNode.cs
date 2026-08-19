using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class TriggerBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            EnableIt = 10,
            DisableIt = 11,
            RunIt = 50
        }

        [SerializeReference]
        [OnValueChanged("MarkDirty")]
        [ValueDropdown("DrawTriggerListDropdown", ExpandAllMenuItems = true)]
        private string triggerNode;

        [SerializeField]
        [LabelText("Execution")]
        [OnValueChanged("MarkDirty")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        public string triggerNodeId => triggerNode;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (string.IsNullOrEmpty(triggerNode) || executionType == ExecutionType.None)
            {
                LogWarning("Found an empty Trigger Behaviour node in " + context.objectName);
            }
            else
            {
                if (context.graph == null)
                {
                    LogWarning("Found an invalid Trigger Behaviour node in " + context.objectName);
                }
                else
                {
                    var triggers = Graph.GetChildren(context.graph.RootNode);
                    for (int i = 0; i < triggers.Count; i++)
                    {
                        TriggerNode trigger = (TriggerNode) triggers[i];
                        if (trigger.TriggerId == triggerNode)
                        {
                            if (executionType == ExecutionType.EnableIt)
                                trigger.enabled = true;
                            else if (executionType == ExecutionType.DisableIt)
                                trigger.enabled = false;
                            else if (executionType == ExecutionType.RunIt)
                                context.Trigger(trigger.TriggerId, execution);
#if UNITY_EDITOR
                            triggers[i].OnEnableChange();
#endif
                            break;
                        }
                    }
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        public string TriggerNode => triggerNode;
        public bool IsRunExecution => executionType == ExecutionType.RunIt;

        public void SetTriggerNode (string value)
        {
            triggerNode = value;
        }

        private IEnumerable DrawTriggerListDropdown ()
        {
            var actionNameListDropdown = new ValueDropdownList<string>();
            var graph = GetGraph();
            if (graph is {nodes: { }})
            {
                for (var i = 0; i < graph.nodes.Count; i++)
                {
                    if (graph.nodes[i] is ActionTriggerNode actionTrigger)
                    {
                        var actionName = actionTrigger.GetActionName();
                        if (!string.IsNullOrEmpty(actionName))
                            actionNameListDropdown.Add(actionTrigger.GetNodeViewTitle() + " : " + actionTrigger.GetActionName() + " (" + actionTrigger.TriggerId + ")", actionTrigger.TriggerId);
                        else
                            actionNameListDropdown.Add(actionTrigger.GetNodeViewTitle() + " (" + actionTrigger.TriggerId + ")", actionTrigger.TriggerId);
                    }
                    else if (graph.nodes[i] is SpawnTriggerNode spawnTrigger)
                    {
                        var actionName = spawnTrigger.GetActionName();
                        if (!string.IsNullOrEmpty(actionName))
                            actionNameListDropdown.Add(spawnTrigger.GetNodeViewTitle() + " : " + spawnTrigger.GetActionName() + " (" + spawnTrigger.TriggerId + ")", spawnTrigger.TriggerId);
                        else
                            actionNameListDropdown.Add(spawnTrigger.GetNodeViewTitle() + " (" + spawnTrigger.TriggerId + ")", spawnTrigger.TriggerId);
                    }
                    else if (graph.nodes[i] is TriggerNode trigger)
                    {
                        var triggerDisplayName = trigger.GetNodeIdentityName();
                        if (!string.IsNullOrEmpty(triggerDisplayName))
                            actionNameListDropdown.Add(trigger.GetNodeViewTitle() + " : " + trigger.GetNodeIdentityName() + " (" + trigger.TriggerId + ")", trigger.TriggerId);
                        else
                            actionNameListDropdown.Add(trigger.GetNodeViewTitle() + " (" + trigger.TriggerId + ")", trigger.TriggerId);
                    }
                }
            }

            return actionNameListDropdown;
        }

        private IEnumerable TypeChoice ()
        {
            var listDropdown = new ValueDropdownList<ExecutionType>();
            var graph = GetGraph();
            if (graph != null)
            {
                if (graph.isAttackStatusPack || graph.isTargetAimPack || graph.isMoralePack || graph.isStaminaPack || graph.isAttackSkillPack || graph.isBehaviourPack)
                {
                    listDropdown.Add("Run It", ExecutionType.RunIt);
                }
                else
                {
                    listDropdown.Add("Enable It", ExecutionType.EnableIt);
                    listDropdown.Add("Disable It", ExecutionType.DisableIt);
                    listDropdown.Add("Run It", ExecutionType.RunIt);
                }
            }

            return listDropdown;
        }

        public static string displayName = "Trigger Behaviour Node";
        public static string nodeName = "Trigger";

        public override string GetNodeInspectorTitle ()
        {
            return displayName;
        }

        public override string GetNodeViewTitle ()
        {
            return nodeName;
        }

        public override string GetNodeIdentityName ()
        {
            return GetNodeViewDescription();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Logic/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (!string.IsNullOrEmpty(triggerNode))
            {
                var triggerNodeName = string.Empty;
                var graph = GetGraph();
                if (graph is {nodes: { }})
                {
                    for (var i = 0; i < graph.nodes.Count; i++)
                    {
                        if (graph.nodes[i] is TriggerNode trigger)
                        {
                            if (trigger.guid == triggerNode)
                            {
                                if (trigger is ActionTriggerNode actionTrigger)
                                {
                                    triggerNodeName = trigger.GetNodeViewTitle() + " (" + actionTrigger.GetActionName() + ")";
                                }
                                else if (trigger is SpawnTriggerNode spawnTrigger)
                                {
                                    triggerNodeName = trigger.GetNodeViewTitle() + " (" + spawnTrigger.GetActionName() + ")";
                                }
                                else
                                {
                                    var triggerDisplayName = trigger.GetNodeIdentityName();
                                    if (!string.IsNullOrEmpty(triggerDisplayName))
                                        triggerNodeName = trigger.GetNodeViewTitle() + " (" + trigger.GetNodeIdentityName() + ")";
                                    else
                                        triggerNodeName = trigger.GetNodeViewTitle() + " (" + trigger.TriggerId + ")";
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(triggerNodeName))
                {
                    if (executionType == ExecutionType.EnableIt)
                        return "Enable " + triggerNodeName;
                    if (executionType == ExecutionType.DisableIt)
                        return "Disable " + triggerNodeName;
                    if (executionType == ExecutionType.RunIt)
                        return "Run " + triggerNodeName;
                }
            }

            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            return "This will provide several controls (enable, disable, run) to a specific Trigger node in the same graph.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}