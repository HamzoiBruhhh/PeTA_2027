using System.Collections;
using Reshape.ReFramework;
using Sirenix.OdinInspector;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class MoraleTriggerNode : TriggerNode
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
            var menu = new ValueDropdownList<Type> {{"Value Changed", Type.MoraleChange}};
            return menu;
        }

        public static string displayName = "Morale Trigger Node";
        public static string nodeName = "Morale";
        
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
            if (triggerType == Type.MoraleChange)
                return "Value Changed";
            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.MoraleChange)
                tip += "This will get trigger when the value of the Morale pack have changes.\n\n";
            else
                tip += "This will get trigger all Morale related events.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}