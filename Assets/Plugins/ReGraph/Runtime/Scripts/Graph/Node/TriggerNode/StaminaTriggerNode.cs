using System.Collections;
using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class StaminaTriggerNode : TriggerNode
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("MarkDirty")]
        public Type triggerType;
        
        [ValueDropdown("@Stamina.DrawTypeChoiceDropdown()")]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@triggerType == Type.StaminaConsume")]
        public Stamina.Type eventType;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            var state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                var proceed = false;
                if (execution.type == triggerType && eventType == execution.parameters.staminaData.staminaType)
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
            var menu = new ValueDropdownList<Type> {{"Stamina Consume", Type.StaminaConsume}};
            return menu;
        }

        public static string displayName = "Stamina Trigger Node";
        public static string nodeName = "Stamina";
        
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
            if (triggerType == Type.StaminaConsume)
                if (eventType != Stamina.Type.None)
                    return "Stamina Consume at " + eventType.ToString().SplitCamelCase();
            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.StaminaConsume)
                tip += "This will get trigger when character checking for stamina consume.\n\n";
            else
                tip += "This will get trigger all Stamina related events.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}