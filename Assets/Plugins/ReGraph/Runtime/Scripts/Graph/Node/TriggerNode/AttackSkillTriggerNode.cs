using System.Collections;
using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class AttackSkillTriggerNode : TriggerNode
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("OnChangeType")]
        public Type triggerType;

        [ShowIf("@triggerType == Type.AttackSkillLaunch || triggerType == Type.AttackSkillDetect")]
        [ValueDropdown("EventTypeChoice")]
        [OnValueChanged("MarkDirty")]
        public Type eventType;

        [ShowIf("ShowObjVer")]
        [LabelText("Owner")]
        [OnValueChanged("MarkDirty")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariableWarning", GUIAlwaysEnabled = true)]
        public SceneObjectVariable objectVariable;

        [LabelText("@Character2Name()")]
        [OnValueChanged("MarkDirty")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariable2Warning", GUIAlwaysEnabled = true)]
        [ShowIf("ShowObjVer2")]
        public SceneObjectVariable objectVariable2;

        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(parameterStr)")]
        [ShowIf("@triggerType == Type.AttackSkillUpdate")]
        [InlineProperty]
        [LabelText("Owner Phase")]
        private StringProperty parameterStr;
        
        [LabelText("Active Store To")]
        [ShowIf("@triggerType == Type.AttackSkillToggle")]
        [OnInspectorGUI("@MarkPropertyDirty(number1)")]
        [InlineProperty]
        public FloatProperty number1;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            var state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                var proceed = false;
                if (execution.type == triggerType)
                {
                    if (execution.type == Type.AttackSkillLaunch)
                    {
                        if (eventType == execution.parameters.attackSkillData.triggerType)
                        {
                            proceed = true;
                            if (objectVariable != null)
                            {
                                objectVariable.Rebase();
                                objectVariable.SetValue(execution.parameters.attackSkillData.owner);
                            }

                            if (objectVariable2 != null)
                            {
                                objectVariable2.Rebase();
                                objectVariable2.SetValue(execution.parameters.attackSkillData.damageData.defender);
                            }
                        }
                    }
                    else if (execution.type == Type.AttackSkillDetect)
                    {
                        if (eventType == execution.parameters.attackSkillData.triggerType)
                        {
                            proceed = true;
                            if (objectVariable != null)
                            {
                                objectVariable.Rebase();
                                objectVariable.SetValue(execution.parameters.attackSkillData.owner);
                            }

                            if (objectVariable2 != null)
                            {
                                objectVariable2.Rebase();
                                objectVariable2.SetValue(execution.parameters.attackSkillData.damageData.defender);
                            }
                        }
                    }
                    else if (execution.type == Type.AttackSkillToggle)
                    {
                        proceed = true;
                        if (objectVariable != null)
                        {
                            objectVariable.Rebase();
                            objectVariable.SetValue(execution.parameters.attackSkillData.owner);
                        }

                        number1?.SetVariableValue(execution.parameters.attackSkillData.skillMuscle.isToggled ? 1 : 0);
                    }
                    else
                    {
                        proceed = true;
                        if (objectVariable != null)
                        {
                            objectVariable.Rebase();
                            objectVariable.SetValue(execution.parameters.attackSkillData.owner);
                        }

                        if (execution.type == Type.AttackSkillUpdate)
                        {
                            if (parameterStr.IsVariable() && !parameterStr.IsNull())
                                parameterStr.SetVariableValue(execution.parameters.attackSkillData.owner.phase);
                        }
                    }
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
            return type == triggerType && (int) eventType == paramInt;
        }

#if UNITY_EDITOR
        public void OnChangeType ()
        {
            if (triggerType == Type.AttackSkillUpdate)
                parameterStr.AllowVariableOnly();
            else
                parameterStr.AllowAll();
            if (triggerType == Type.AttackSkillToggle)
                number1.AllowVariableOnly();
            eventType = 0;
            MarkDirty();
        }

        private bool ShowObjVer ()
        {
            if (triggerType is Type.AttackSkillLaunch or Type.AttackSkillDetect or Type.AttackSkillComplete or Type.AttackSkillToggle)
                return true;
            if (triggerType is Type.AttackSkillBegin or Type.AttackSkillEnd or Type.AttackSkillUpdate)
                return true;
            return false;
        }

        private bool ShowObjVer2 ()
        {
            if (triggerType is Type.AttackSkillLaunch)
                if (eventType is Type.CharacterAttackEnd or Type.CharacterAttackBreak or Type.CharacterAttackSkill or Type.CharacterAttackGoMelee)
                    return true;
            if (triggerType is Type.AttackSkillDetect && eventType is Type.CharacterAttackSkill)
                return true;
            return false;
        }

        private string Character2Name ()
        {
            if (triggerType is Type.AttackSkillLaunch)
                if (eventType is Type.CharacterAttackEnd or Type.CharacterAttackBreak or Type.CharacterAttackSkill or Type.CharacterAttackGoMelee)
                    return "Attack Target";
            if (triggerType is Type.AttackSkillDetect && eventType is Type.CharacterAttackSkill)
                return "Attack Target";
            return string.Empty;
        }

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
            var menu = new ValueDropdownList<Type>
            {
                {"Detect Skill", Type.AttackSkillDetect},
                {"Launch Skill", Type.AttackSkillLaunch},
                {"Complete Skill", Type.AttackSkillComplete},
                {"Toggle Skill", Type.AttackSkillToggle},
                {"Begin", Type.AttackSkillBegin},
                {"End", Type.AttackSkillEnd},
                {"Update", Type.AttackSkillUpdate},
            };
            return menu;
        }

        private IEnumerable EventTypeChoice ()
        {
            var menu = new ValueDropdownList<Type>();
            if (triggerType == Type.AttackSkillLaunch)
            {
                menu.Add("Skill Attack", Type.CharacterAttackSkill);
                menu.Add("Go Melee Attack", Type.CharacterAttackGoMelee);
                menu.Add("Attack Break", Type.CharacterAttackBreak);
                menu.Add("Attack End", Type.CharacterAttackEnd);
                menu.Add("Hurt", Type.CharacterGetHurt);
                menu.Add("Dead", Type.CharacterDead);
            }
            else if (triggerType == Type.AttackSkillDetect)
            {
                menu.Add("Skill Attack", Type.CharacterAttackSkill);
                menu.Add("Skill Activate", Type.AttackSkillActivate);
                menu.Add("Skill Toggle", Type.AttackSkillToggle);
            }

            return menu;
        }

        public static string displayName = "Attack Skill Trigger Node";
        public static string nodeName = "Attack Skill";
        
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
            if (triggerType == Type.AttackSkillBegin)
                return "Begin";
            if (triggerType == Type.AttackSkillEnd)
                return "End";
            if (triggerType == Type.AttackSkillUpdate)
                return "Update";
            if (triggerType == Type.AttackSkillComplete)
                return "Complete Skill";
            if (triggerType == Type.AttackSkillToggle)
                return "Toggle Skill";
            if (triggerType == Type.AttackSkillLaunch)
                if (eventType != Type.None)
                    return "Launch skill at " + eventType.ToString().SplitCamelCase();
            if (triggerType == Type.AttackSkillDetect)
                if (eventType != Type.None)
                    return "Detect skill at " + eventType.ToString().SplitCamelCase();
            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.AttackSkillBegin)
                tip += "This will get trigger when the Attack Skill pack being add to the target.\n\n";
            else if (triggerType == Type.AttackSkillEnd)
                tip += "This will get trigger when the Attack Skill pack being remove from the target.\n\n";
            else if (triggerType == Type.AttackSkillUpdate)
                tip += "This will get trigger when the Attack Skill pack being update base on the time interval configuration.\n\n";
            else if (triggerType == Type.AttackSkillLaunch)
                tip += "This will get trigger when character is available to launch the skill.\n\n";
            else if (triggerType == Type.AttackSkillComplete)
                tip += "This will get trigger right after character skill launch to check the completion of the skill.\n\n";
            else if (triggerType == Type.AttackSkillToggle)
                tip += "This will get trigger right after character skill toggle the skill to be active or inactive.\n\n";
            else if (triggerType == Type.AttackSkillDetect)
                tip += "This will get trigger when character detect availability of a skill attack.\n\n";
            else
                tip += "This will get trigger all attack skill related events.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}