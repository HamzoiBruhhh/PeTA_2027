using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using Reshape.ReFramework;
using Reshape.Unity;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class BrainStatTriggerNode : TriggerNode
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("MarkDirty")]
        public Type triggerType;
        
        [ValueDropdown("@StatType.DrawStatNameListDropdown()", DropdownWidth = 250, AppendNextDrawer = true)]
        [OnValueChanged("MarkDirty")]
        [PropertyOrder(1)]
        [DisplayAsString]
        public string statType;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            var state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                var proceed = false;
                if (execution.type == triggerType)
                {
                    if (execution.type is Type.BrainStatGet)
                    {
                        if (execution.parameters.characterBrain != null)
                            if (execution.parameters.actionName.Equals(statType))
                                proceed = true;
                    }
                    else if (execution.type == Type.BrainStatChange)
                    {
                        if (execution.parameters.actionName.Equals(statType))
                            proceed = true;
                    }
                }
                else if (execution.type == Type.All)
                {
                    if (execution.parameters.actionName.Equals(TriggerId))
                        if (execution.parameters.characterBrain != null)
                            proceed = true;
                }

                if (proceed)
                {
                    execution.variables.SetState(guid, State.Success);
                    state = State.Success;
                }

                if (state != State.Success)
                {
                    execution.variables.SetState(guid, State.Failure);
                    state = State.Failure;
                }
                else
                    OnSuccess();
            }

            if (state == State.Success)
                return base.OnUpdate(execution, updateId);
            return State.Failure;
        }

        public override bool IsTrigger (TriggerNode.Type type, int paramInt = 0)
        {
            return type == triggerType;
        }
        
#if UNITY_EDITOR
        private IEnumerable TriggerTypeChoice ()
        {
            var menu = new ValueDropdownList<Type>();
            var graph = GetGraph();
            if (graph != null)
            {
                if (graph.isStatGraph)
                {
                    menu.Add("Get Stat", Type.BrainStatGet);
                    menu.Add("Update Stat", Type.BrainStatChange);
                }
                else if (graph.isBehaviourGraph)
                    menu.Add("Stat Changed", Type.BrainStatChange);
            }

            return menu;
        }
        
        public static string displayName = "Brain Stat Trigger Node";
        public static string nodeName = "Brain Stat";
        
        public override Type GetNodeType ()
        {
            return triggerType;
        }

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
            return triggerType.ToString();
        }

        public override string GetNodeViewDescription ()
        {
            if (triggerType == Type.BrainStatGet)
                if (!string.IsNullOrEmpty(statType))
                    return "Get " + statType + " from brain";
            if (triggerType == Type.BrainStatChange)
                if (!string.IsNullOrEmpty(statType))
                    return statType + " have changed";
            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.BrainStatGet)
                tip += "This will get trigger when the stat system get the final value of the selected stat.\n\n";
            else if (triggerType == Type.BrainStatChange)
                tip += "This will get trigger when the value of the selected stat have changed.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}