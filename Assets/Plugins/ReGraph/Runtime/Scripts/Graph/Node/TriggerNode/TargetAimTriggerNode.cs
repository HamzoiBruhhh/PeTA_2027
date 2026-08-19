using System.Collections;
using Reshape.ReFramework;
using Sirenix.OdinInspector;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class TargetAimTriggerNode : TriggerNode
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("MarkDirty")]
        public Type triggerType;

        [LabelText("Owner Store To")]
        [OnValueChanged("MarkDirty")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariableWarning", GUIAlwaysEnabled = true)]
        public SceneObjectVariable objectVariable;

        [LabelText("Attacker Store To")]
        [OnValueChanged("MarkDirty")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariable2Warning", GUIAlwaysEnabled = true)]
        [ShowIf("@triggerType == Type.ChooseHurtTarget")]
        public SceneObjectVariable objectVariable2;

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
                    if (context.isScriptableGraph)
                    {
                        if (triggerType == Type.ChooseAimTarget)
                        {
                            objectVariable.Rebase();
                            objectVariable.SetValue(execution.parameters.targetAimData.attacker);
                        }
                        else if (triggerType == Type.ChooseHurtTarget)
                        {
                            objectVariable.Rebase();
                            objectVariable2.Rebase();
                            objectVariable.SetValue(execution.parameters.targetAimData.defender);
                            objectVariable2.SetValue(execution.parameters.targetAimData.attacker);
                        }
                    }
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
        private bool ShowObjectVariableWarning ()
        {
            if (objectVariable != null)
                if (objectVariable.sceneObject.type != SceneObject.ObjectType.CharacterOperator)
                    return true;
            return false;
        }

        private bool ShowObjectVariable2Warning ()
        {
            if (objectVariable2 != null)
                if (objectVariable2.sceneObject.type != SceneObject.ObjectType.CharacterOperator)
                    return true;
            return false;
        }

        private IEnumerable TriggerTypeChoice ()
        {
            var menu = new ValueDropdownList<Type> {{"Choose", Type.ChooseAimTarget}, {"React Hurt", Type.ChooseHurtTarget}};
            return menu;
        }

        public static string displayName = "Target Aim Trigger Node";
        public static string nodeName = "Target Aim";
        
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
            if (triggerType == Type.ChooseAimTarget && objectVariable != null)
                return "Choose Aim Target";
            if (triggerType == Type.ChooseHurtTarget && objectVariable != null && objectVariable2 != null)
                return "React Get Hurt";
            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.ChooseAimTarget)
                tip += "This will get trigger when the Target Aim pack being ask to choose a target to aim as attack target.\n\n";
            else if (triggerType == Type.ChooseHurtTarget)
                tip += "This will get trigger when the Target Aim pack get hurt and would like to choose a target to aim as attack target.\n\n";
            else
                tip += "This will get trigger all Target Aim related events.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}