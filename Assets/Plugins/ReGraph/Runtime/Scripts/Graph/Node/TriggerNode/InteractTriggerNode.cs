using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class InteractTriggerNode : TriggerNode
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("MarkDirty")]
        [InfoBox("This graph have to assign into Character Operator's behaviour list in order to make the trigger connecting with the character.")]
        public Type triggerType;

        [LabelText("Owner Store To")]
        [OnValueChanged("MarkDirty")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariableWarning", GUIAlwaysEnabled = true)]
        public SceneObjectVariable objectVariable;
        
        [LabelText("Target Store To")]
        [OnValueChanged("MarkDirty")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariable2Warning", GUIAlwaysEnabled = true)]
        public SceneObjectVariable objectVariable2;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            State state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                if ((execution.type is Type.InteractLaunch or Type.InteractReceive or Type.InteractFinish or Type.InteractLeave or Type.InteractCancel or Type.InteractGiveUp && execution.type == triggerType))
                {
                    execution.variables.SetState(guid, State.Success);
                    state = State.Success;
                    if (objectVariable != null)
                    {
                        objectVariable.Rebase();
                        objectVariable.SetValue(execution.parameters.characterBrain.Owner);
                    }
                    
                    if (objectVariable2 != null)
                    {
                        objectVariable2.Rebase();
                        objectVariable2.SetValue(execution.parameters.characterBrainAddon.Owner);
                    }
                }
                else if (execution.type == Type.All)
                {
                    if (execution.parameters.actionName.Equals(TriggerId))
                    {
                        execution.variables.SetState(guid, State.Success);
                        state = State.Success;
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
            var menu = new ValueDropdownList<Type> {{"Launch", Type.InteractLaunch}, {"Receive", Type.InteractReceive}, {"Finish", Type.InteractFinish}, {"Leave", Type.InteractLeave}, {"Cancel", Type.InteractCancel}, {"Give Up", Type.InteractGiveUp}};
            return menu;
        }

        public static string displayName = "Interact Trigger Node";
        public static string nodeName = "Interact";
        
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
            string desc = String.Empty;
            if (triggerType == Type.InteractLaunch)
            {
                desc = "Character launch interaction";
            }
            else if (triggerType == Type.InteractReceive)
            {
                desc = "Character receive interaction";
            }
            else if (triggerType == Type.InteractFinish)
            {
                desc = "Character finish interaction";
            }
            else if (triggerType == Type.InteractLeave)
            {
                desc = "Character interaction being leave";
            }
            else if (triggerType == Type.InteractCancel)
            {
                desc = "Character cancel interaction";
            }
            else if (triggerType == Type.InteractGiveUp)
            {
                desc = "Character interaction being give up";
            }

            return desc;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.InteractLaunch)
                tip += "This will get trigger when Character Operator launch an interaction, interaction can be launch from SetActionInterestTarget in Character Operator.\n\n";
            else if (triggerType == Type.InteractReceive)
                tip += "This will get trigger when Character Operator receive an interaction. It happen right after Interact Launch trigger.\n\n";
            else if (triggerType == Type.InteractFinish)
                tip += "This will get trigger when Character Operator complete an interaction after successful launch.\n\n";
            else if (triggerType == Type.InteractLeave)
                tip += "This will get trigger when Character Operator receive an interaction completion after successful launch.\n\n";
            else if (triggerType == Type.InteractCancel)
                tip += "This will get trigger when Character Operator cancel an interaction before successful launch, interaction can be end by various method, for instance interrupt by attack or being command to move to other location.\n\n";
            else if (triggerType == Type.InteractGiveUp)
                tip += "This will get trigger when Character Operator receive an interaction cancellation before successful launch, interaction can be end by various method, for instance interrupt by attack or being command to move to other location.\n\n";
            else
                tip += "This will get trigger all Interact related events.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}