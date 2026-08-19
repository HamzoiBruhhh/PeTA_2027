using System.Collections;
using Sirenix.OdinInspector;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class AttackStatusTriggerNode : TriggerNode
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("MarkDirty")]
        public Type triggerType;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            var state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                var proceed = false;
                if (execution.type == triggerType)
                {
                    proceed = true;
                }
                else if (execution.type == Type.All)
                {
                    if (execution.parameters.actionName.Equals(TriggerId))
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
            var menu = new ValueDropdownList<Type> {{"Begin", Type.AttackStatusBegin}, {"End", Type.AttackStatusEnd}, {"Update", Type.AttackStatusUpdate}};
            return menu;
        }

        public static string displayName = "Attack Status Trigger Node";
        public static string nodeName = "Attack Status";

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
            if (triggerType == Type.AttackStatusBegin)
                return "Begin";
            if (triggerType == Type.AttackStatusEnd)
                return "End";
            if (triggerType == Type.AttackStatusUpdate)
                return "Update";
            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.AttackStatusBegin)
                tip += "This will get trigger when the Attack Status pack being add to the target.\n\n";
            else if (triggerType == Type.AttackStatusEnd)
                tip += "This will get trigger when the Attack Status pack being remove from the target.\n\n";
            else if (triggerType == Type.AttackStatusUpdate)
                tip += "This will get trigger when the Attack Status pack being update base on the time interval configuration.\n\n";
            else
                tip += "This will get trigger all Attack Status related events.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}