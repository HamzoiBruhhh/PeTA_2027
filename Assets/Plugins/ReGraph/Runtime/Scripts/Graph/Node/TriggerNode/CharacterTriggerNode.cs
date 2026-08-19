using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class CharacterTriggerNode : TriggerNode
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("OnChangeType")]
        [InfoBox("This graph have to assign into Character Operator's behaviour list in order to make the trigger connecting with the character.")]
        public Type triggerType;

        [LabelText("Character Store To")]
        [OnValueChanged("MarkDirty")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariableWarning", GUIAlwaysEnabled = true)]
        public SceneObjectVariable objectVariable;

        [LabelText("@Character2Name()")]
        [OnValueChanged("MarkDirty")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariable2Warning", GUIAlwaysEnabled = true)]
        [ShowIf("ShowObjVer2")]
        public SceneObjectVariable objectVariable2;

        [LabelText("@NumberName()")]
        [ShowIf("@triggerType == Type.CharacterScanVicinity || triggerType == Type.CharacterGetAttack")]
        [OnInspectorGUI("@MarkPropertyDirty(numberVariable)")]
        [InlineProperty]
        public FloatProperty numberVariable;

        [LabelText("Hostile Count")]
        [ShowIf("@triggerType == Type.CharacterScanVicinity")]
        [OnInspectorGUI("@MarkPropertyDirty(numberVariable2)")]
        [InlineProperty]
        public FloatProperty numberVariable2;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            State state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                if (execution.type == triggerType && execution.type is Type.CharacterAttackFire or Type.CharacterDead or Type.CharacterTerminate or Type.CharacterKill or Type.CharacterAttackBackstab
                        or Type.CharacterFriendDead or Type.CharacterGetBackstab or Type.CharacterScanVicinity or Type.CharacterGetAttack or Type.CharacterStanceDone or Type.CharacterUnstanceDone
                        or Type.CharacterGetInterrupt or Type.SelectReceive or Type.SelectConfirm or Type.SelectFinish)
                {
                    execution.variables.SetState(guid, State.Success);
                    state = State.Success;
                    if (objectVariable)
                    {
                        objectVariable.Rebase();
                        objectVariable.SetValue(execution.parameters.characterBrain.Owner);
                    }

                    if (execution.type is Type.CharacterKill or Type.CharacterAttackBackstab or Type.CharacterFriendDead)
                    {
                        if (objectVariable2)
                        {
                            objectVariable2.Rebase();
                            objectVariable2.SetValue(execution.parameters.attackDamageData.defender);
                        }
                    }
                    else if (execution.type is Type.CharacterGetAttack or Type.CharacterGetBackstab or Type.CharacterDead)
                    {
                        if (objectVariable2)
                        {
                            objectVariable2.Rebase();
                            objectVariable2.SetValue(execution.parameters.attackDamageData.attacker);
                        }

                        if (numberVariable.IsVariable() && !numberVariable.IsNull())
                            numberVariable.SetVariableValue(execution.parameters.attackDamageData.GetImpairedDamage());
                    }
                    else if (execution.type is Type.CharacterScanVicinity)
                    {
                        if (!numberVariable.IsNull())
                            numberVariable.SetVariableValue(execution.parameters.attackDamageData.GetTotalDamageDeal());
                        if (!numberVariable2.IsNull())
                            numberVariable2.SetVariableValue(execution.parameters.attackDamageData.GetImpairedDamage());
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
        private bool ShowObjVer2 ()
        {
            if (triggerType is Type.CharacterKill or Type.CharacterAttackBackstab or Type.CharacterFriendDead or Type.CharacterGetAttack or Type.CharacterGetBackstab or Type.CharacterDead)
                return true;
            return false;
        }

        public void OnChangeType ()
        {
            if (triggerType is Type.CharacterScanVicinity or Type.CharacterGetAttack)
            {
                numberVariable.AllowVariableOnly();
                numberVariable2.AllowVariableOnly();
            }
            else
            {
                numberVariable.AllowAll();
                numberVariable2.AllowAll();
            }

            MarkDirty();
        }

        private string Character2Name ()
        {
            if (triggerType is Type.CharacterFriendDead)
                return "Teammate Store To";
            return "Opponent Store To";
        }

        private string NumberName ()
        {
            if (triggerType is Type.CharacterScanVicinity)
                return "Friendly Count";
            if (triggerType is Type.CharacterGetAttack)
                return "Damage Received";
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
            var menu = new ValueDropdownList<Type>();
            var graph = GetGraph();
            if (graph != null)
            {
                menu.Add("Stance Complete", Type.CharacterStanceDone);
                menu.Add("Unstance Complete", Type.CharacterUnstanceDone);
                menu.Add("Launch Attack", Type.CharacterAttackFire);
                menu.Add("Launch Backstab", Type.CharacterAttackBackstab);
                menu.Add("Launch Skill", Type.CharacterAttackSkill);
                menu.Add("Receive Attack", Type.CharacterGetAttack);
                menu.Add("Receive Backstab", Type.CharacterGetBackstab);
                menu.Add("Get Interrupt", Type.CharacterGetInterrupt);
                menu.Add("Kill Opponent", Type.CharacterKill);
                menu.Add("Dead", Type.CharacterDead);
                menu.Add("Teammate Dead", Type.CharacterFriendDead);
                menu.Add("Scan Vicinity", Type.CharacterScanVicinity);
                menu.Add("Terminate", Type.CharacterTerminate);
                menu.Add("Tapped", Type.SelectReceive);
                menu.Add("Selected", Type.SelectConfirm);
                menu.Add("Unselected", Type.SelectFinish);
            }

            return menu;
        }

        public static string displayName = "Character Trigger Node";
        public static string nodeName = "Character";
        
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
            if (triggerType == Type.CharacterAttackFire)
                desc = "Character launch attack";
            else if (triggerType == Type.CharacterAttackBackstab)
                desc = "Character launch backstab";
            else if (triggerType == Type.CharacterDead)
                desc = "Character dead";
            else if (triggerType == Type.CharacterTerminate)
                desc = "Character terminate";
            else if (triggerType == Type.CharacterGetAttack)
                desc = "Character receive attack";
            else if (triggerType == Type.CharacterGetBackstab)
                desc = "Character receive backstab";
            else if (triggerType == Type.CharacterGetInterrupt)
                desc = "Character get interrupt";
            else if (triggerType == Type.CharacterKill)
                desc = "Character kill an opponent";
            else if (triggerType == Type.CharacterFriendDead)
                desc = "Character's friend have dead";
            else if (triggerType == Type.CharacterScanVicinity)
                desc = "Character scan surrounding units";
            else if (triggerType == Type.CharacterStanceDone)
                desc = "Character completely stance";
            else if (triggerType == Type.CharacterUnstanceDone)
                desc = "Character completely unstance";
            else if (triggerType == Type.SelectReceive)
                desc = "Character receive selection";
            else if (triggerType == Type.SelectConfirm)
                desc = "Character selected";
            else if (triggerType == Type.SelectFinish)
                desc = "Character unselected";
            return desc;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.CharacterAttackFire)
                tip += "This will get trigger when Character Operator launch an attack.\n\n";
            else if (triggerType == Type.CharacterAttackBackstab)
                tip += "This will get trigger when Character Operator launch an backstab.\n\n";
            else if (triggerType == Type.CharacterDead)
                tip += "This will get trigger when Character Operator dead.\n\n";
            else if (triggerType == Type.CharacterTerminate)
                tip += "This will get trigger when Character Operator destroy from scene.\n\n";
            else if (triggerType == Type.CharacterKill)
                tip += "This will get trigger when Character Operator kill an opponent.\n\n";
            else if (triggerType == Type.CharacterGetAttack)
                tip += "This will get trigger when Character Operator receive an attack by opponent.\n\n";
            else if (triggerType == Type.CharacterGetBackstab)
                tip += "This will get trigger when Character Operator receive backstab by opponent.\n\n";
            else if (triggerType == Type.CharacterGetInterrupt)
                tip += "This will get trigger when Character Operator's attack get interrupted.\n\n";
            else if (triggerType == Type.CharacterFriendDead)
                tip += "This will get trigger when Character Operator have a friendly unit get dead.\n\n";
            else if (triggerType == Type.CharacterScanVicinity)
                tip += "This will get trigger when Character Operator scan surrounding units base on settings at Character Brain.\n\n";
            else if (triggerType == Type.CharacterStanceDone)
                tip += "This will get trigger when Character Operator complete turn into stance state from unstance state.\n\n";
            else if (triggerType == Type.CharacterUnstanceDone)
                tip += "This will get trigger when Character Operator complete turn into unstance state from stance state.\n\n";
            else if (triggerType == Type.SelectReceive)
                tip += "This will get trigger when Character Operator receive a select input.\n\n";
            else if (triggerType == Type.SelectConfirm)
                tip += "This will get trigger when Character Operator being selected.\n\n";
            else if (triggerType == Type.SelectFinish)
                tip += "This will get trigger when Character Operator being unselected.\n\n";
            else
                tip += "This will get trigger all Character related events.\n\n";
            return tip + "This graph must assign as statGraph in Character Operator in order to receive trigger.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}