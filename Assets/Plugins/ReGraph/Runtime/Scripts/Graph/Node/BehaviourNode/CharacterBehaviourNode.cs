using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReFramework;
using Reshape.Unity;
#if UNITY_EDITOR
using System.Collections;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class CharacterBehaviourNode : BehaviourNode
    {
        public const string VAR_PROCEED = "_proceed";

        private const string MELEE_ATTACK = "Melee Attack";
        private const string RANGED_ATTACK = "Ranged Attack";

        public enum ExecutionType
        {
            None,
            Visible = 50,
            Wander = 101,
            GoTo = 111,
            Teleport = 112,
            Chase = 121,
            Guard = 131,
            InteractTarget = 401,
            SearchTarget = 501,
            AttackTarget = 601,
            AddAttackStatus = 1001,
            RemoveAttackStatus = 1002,
            HaveAttackStatus = 1003,
            AddStatusEffect = 1011,
            RemoveStatusEffect = 1012,
            HurtTarget = 1051,
            HealTarget = 1052,
            KnockTarget = 1071,
            InterruptTarget = 1072,
            CancelMoveTarget = 1073,
            FleeTarget = 1091,
            CancelFleeTarget = 1092,
            RemoveSkill = 4001,
            AddSkill = 4002,
            CheckState = 5001,
            CheckFlag = 5002,
            CheckAttack = 5003,
            GetDistance = 6001,
            GetMeleeAttackDistance = 6011,
            GetSpawnId = 6101,
            GetCharacterId = 6201,
            GetCharacter = 6202,
            GetInvName = 7001,
            GetStatValue = 7201,
            GetAdjacentCount = 7301,
            AtBehind = 10001,
            InTeamFov = 15001,
            InUnitFov = 15002,
            TriggerAction = 30000,
            SetBrainProperties = 31000,
            SetMuscleProperties = 31010,
            SetPhase = 32000,
        }

        public enum CheckStateType
        {
            None,
            InDead = 10,
            InIdle = 50,
            InFight = 101,
            InMeleeFight = 102,
            InRangedFight = 103,
            InMeleeAttacking = 104,
            InRangedAttacking = 105,
            InFightCooldown = 106,
            UnderStance = 201,
            UnderFlee = 210,
            UnderMove = 250,
            UnderSelect = 280,
            UnderPlayerInput = 1001
        }

        [SerializeField]
        [OnValueChanged("OnChangeType")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        [InfoBox("@GetTypeWarningMessage()", InfoMessageType.Warning, "@ShowTypeWarning()")]
        private ExecutionType executionType;

        [SerializeField]
        [ShowIf("ShowCharacter")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(character)")]
        [InlineButton("@character.SetObjectValue(AssignComponent<CharacterOperator>())", "♺", ShowIf = "@character.IsObjectValueType()")]
        [InfoBox("@character.GetMismatchWarningMessage()", InfoMessageType.Error, "@character.IsShowMismatchWarning()")]
        private SceneObjectProperty character = new SceneObjectProperty(SceneObject.ObjectType.CharacterOperator, "Character");

        [SerializeField]
        [ShowIf("ShowCharacterParam")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(paramCharacter)")]
        [InlineButton("@paramCharacter.SetObjectValue(AssignComponent<CharacterOperator>())", "♺", ShowIf = "@paramCharacter.IsObjectValueType()")]
        [InfoBox("@paramCharacter.GetMismatchWarningMessage()", InfoMessageType.Error, "@paramCharacter.IsShowMismatchWarning()")]
        private SceneObjectProperty paramCharacter = new SceneObjectProperty(SceneObject.ObjectType.CharacterOperator, "Target");

        [SerializeField]
        [ShowIf("ShowLocation")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(location)")]
        [InlineButton("AssignLocationObjectValue", "♺", ShowIf = "ShowLocationAssignObjectValue")]
#if REGRAPH_DEV_DEBUG
        [InlineButton("FixAttackStatus", "Fix", ShowIf = "@attackStatus != null")]
#endif
        [InfoBox("@location.GetMismatchWarningMessage()", InfoMessageType.Error, "@location.IsShowMismatchWarning()")]
        private SceneObjectProperty location = new SceneObjectProperty(SceneObject.ObjectType.Transform, "GameObjectLocation");

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ValueDropdown("CheckStateTypeChoice")]
        [ShowIf("ShowCheckStateParam")]
        [LabelText("State")]
        private CheckStateType checkState;

        [ValueDropdown("StatTypeChoice", DropdownWidth = 250, AppendNextDrawer = true)]
        [OnValueChanged("MarkDirty")]
        [DisplayAsString]
        [ShowIf("ShowStatType")]
        [LabelText("@StatTypeLabel()")]
        public string statType;

        [LabelText("@Number1Label()")]
        [ShowIf("ShowNumber1")]
        [OnInspectorGUI("@MarkPropertyDirty(number1)")]
        [InlineProperty]
        [InfoBox("@Number1WarningMessage()", InfoMessageType.Warning, "ShowNumber1Warning", GUIAlwaysEnabled = true)]
        public FloatProperty number1;

        [InfoBox("Attack Status Value property is deprecated, please make change to use Attack Status property once you see it have value in it.", InfoMessageType.Error, "@attackStatus != null")]
        [DisableIf("@true")]
        [LabelText("Attack Status Value")]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowAttackStatus")]
        public AttackStatusPack attackStatus;

        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowAttackDamagePack")]
        public AttackDamagePack attackDamagePack;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowBool1")]
        [LabelText("@Bool1Label()")]
        private bool bool1;

        [SerializeField]
        [OnInspectorGUI("MarkUnitFlagsDirty")]
        [LabelText("Unit Flag")]
        [ShowIf("@executionType == ExecutionType.CheckFlag || executionType == ExecutionType.GetAdjacentCount")]
        private MultiTag unitFlags = new MultiTag("Unit Flags", typeof(MultiTagUnit));

        [LabelText("@Number2Label()")]
        [ShowIf("ShowNumber2")]
        [OnInspectorGUI("@MarkPropertyDirty(number2)")]
        [InlineProperty]
        public FloatProperty number2;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowBool2")]
        [LabelText("@Bool2Label()")]
        private bool bool2;

        [OnInspectorGUI("MarkInvFlagsDirty")]
        [ShowIf("@executionType == ExecutionType.GetInvName")]
        [LabelText("Tags")]
        public MultiTag invTags = new MultiTag("Tags", typeof(MultiTagInv));

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.GetInvName")]
        [LabelText("Store To")]
        private WordVariable paramWord;

        [SerializeField]
        [ValueDropdown("DrawActionNameListDropdown", ExpandAllMenuItems = true)]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.TriggerAction")]
        private ActionNameChoice actionName;

        [SerializeField]
        [OnInspectorGUI("MarkBrainFlagsDirty")]
        [LabelText("Modify Properties")]
        [ShowIf("@executionType == ExecutionType.SetBrainProperties")]
        private MultiTag brainFlags = new MultiTag("Brain Flags", typeof(MultiTagBrainProperty));

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.SetBrainProperties")]
        [LabelText("From Brain")]
        private CharacterBrain paramBrain;

        [SerializeField]
        [OnInspectorGUI("MarkMuscleFlagsDirty")]
        [LabelText("Modify Properties")]
        [ShowIf("@executionType == ExecutionType.SetMuscleProperties")]
        private MultiTag muscleFlags = new MultiTag("Muscle Flags", typeof(MultiTagMuscleProperty));

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.SetMuscleProperties")]
        [LabelText("From Muscle")]
        private CharacterMuscle paramMuscle;

        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(parameterStr)")]
        [ShowIf("@executionType == ExecutionType.SetPhase")]
        [InlineProperty]
        [LabelText("Phase")]
        private StringProperty parameterStr;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.AddSkill || executionType == ExecutionType.RemoveSkill")]
        private AttackSkillPack skillPack;

        [LabelText("@Number3Label()")]
        [ShowIf("ShowNumber3")]
        [OnInspectorGUI("@MarkPropertyDirty(number3)")]
        [InlineProperty]
        public FloatProperty number3;

        [LabelText("@Number4Label()")]
        [ShowIf("ShowNumber4")]
        [OnInspectorGUI("@MarkPropertyDirty(number4)")]
        [InlineProperty]
        public FloatProperty number4;

        private string proceedKey;

        private void InitVariables ()
        {
            if (string.IsNullOrEmpty(proceedKey))
                proceedKey = guid + VAR_PROCEED;
        }

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType == ExecutionType.None)
            {
                LogWarning("Found an empty Character Behaviour node in " + context.objectName);
            }
            else if (context.graph.isAttackDamagePack && executionType == ExecutionType.GetDistance)
            {
                if (!number1.IsVariable() || number1.IsNull())
                    LogWarning("Found an empty Character Get Distance Behaviour node in " + context.objectName);
                else
                {
                    var attacker = execution.parameters.attackDamageData.attacker;
                    var defender = execution.parameters.attackDamageData.defender;
                    var distance = Vector3.Distance(attacker.agentTransform.position, defender.agentTransform.position);
                    number1.SetVariableValue(distance);
                }
            }
            else if (context.graph.isAttackDamagePack && executionType == ExecutionType.GetMeleeAttackDistance)
            {
                if (!number1.IsVariable() || number1.IsNull())
                    LogWarning("Found an empty Character Get Distance Behaviour node in " + context.objectName);
                else
                {
                    var attacker = execution.parameters.attackDamageData.attacker;
                    var defender = execution.parameters.attackDamageData.defender;
                    var distance = attacker.muscle.GetMeleeAttackDistance(defender);
                    number1.SetVariableValue(distance);
                }
            }
            else if (context.graph.isAttackDamagePack && executionType == ExecutionType.AtBehind)
            {
                if (!number1.IsVariable() || number1.IsNull())
                    LogWarning("Found an empty Character At Behind Behaviour node in " + context.objectName);
                else
                {
                    var attacker = execution.parameters.attackDamageData.attacker;
                    var defender = execution.parameters.attackDamageData.defender;
                    var atBehind = 0;
                    if (attacker != null && defender != null)
                        atBehind = attacker.IsStayBehindTarget(defender) ? 1 : 0;
                    number1.SetVariableValue(atBehind);
                }
            }
            else if (context.graph.isAttackDamagePack && executionType == ExecutionType.InTeamFov)
            {
                var attacker = execution.parameters.attackDamageData.attacker;
                var defender = execution.parameters.attackDamageData.defender;
                var inside = attacker.IsVisibleInFogTeam(defender.GetFogTeam());
                UpdateConditionNode(execution, updateId, inside);
            }
            else if (context.graph.isAttackDamagePack && executionType == ExecutionType.InUnitFov)
            {
                var attacker = execution.parameters.attackDamageData.attacker;
                var defender = execution.parameters.attackDamageData.defender;
                var inside = attacker.IsVisibleInFogUnit(defender.GetFogUnit());
                UpdateConditionNode(execution, updateId, inside);
            }
            else if ((context.graph.isMoralePack || context.graph.isTargetAimPack) && executionType == ExecutionType.AddAttackStatus)
            {
                if (!HaveAttackStatus())
                    LogWarning("Found an empty Character Add Attack Status Behaviour node in " + context.objectName);
                else
                {
                    if (context.graph.isMoralePack)
                        execution.parameters.moraleData.owner.AddAttackStatus(GetAttackStatus(), execution.parameters.moraleData.owner);
                    else if (context.graph.isTargetAimPack)
                        execution.parameters.targetAimData.attacker.AddAttackStatus(GetAttackStatus(), execution.parameters.targetAimData.attacker);
                }
            }
            else if (context.graph.isTargetAimPack && executionType == ExecutionType.HaveAttackStatus)
            {
                if (!HaveAttackStatus())
                    LogWarning("Found an empty Character Have Attack Status Behaviour node in " + context.objectName);
                else
                {
                    var result = execution.parameters.targetAimData.attacker.HaveAttackStatus(GetAttackStatus());
                    UpdateConditionNode(execution, updateId, result);
                }
            }
            else if (context.graph.isMoralePack && executionType == ExecutionType.RemoveAttackStatus)
            {
                if (!HaveAttackStatus())
                    LogWarning("Found an empty Character Remove Attack Status Behaviour node in " + context.objectName);
                else
                    execution.parameters.moraleData.owner.RemoveAttackStatus(GetAttackStatus());
            }
            else if (context.graph.isAttackStatusPack && executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
            {
                if (string.IsNullOrEmpty(statType) || number1.IsNull() || number2.IsNull() || number3.IsNull() || number4.IsNull())
                {
                    LogWarning("Found an empty Character Add/Remove Status Effect Behaviour node in " + context.objectName);
                }
                else
                {
                    if (executionType == ExecutionType.AddStatusEffect)
                    {
                        execution.parameters.attackStatusData.owner.AddStatusEffect(execution.parameters.attackStatusData.id, statType, number4, number1, number2, number3);
                    }
                    else if (executionType == ExecutionType.RemoveStatusEffect)
                    {
                        execution.parameters.attackStatusData.owner.RemoveStatusEffect(execution.parameters.attackStatusData.id, statType, number4, number1, number2, number3);
                    }
                }
            }
            else if (context.graph.isAttackStatusPack && executionType is ExecutionType.HealTarget)
            {
                if (number2 > 0f)
                    execution.parameters.attackStatusData.owner.ObtainHealPack(execution.parameters.attackStatusData.id, number2, bool2);
            }
            else if (context.graph.isAttackStatusPack && executionType is ExecutionType.HurtTarget)
            {
                if (attackDamagePack == null)
                {
                    LogWarning("Found an empty Character Hurt Target Behaviour node in " + context.objectName);
                }
                else
                {
                    var attackData = new AttackDamageData();
                    attackData.Init(execution.parameters.attackStatusData.applier, CharacterOperator.AttackType.EffectNormal, attackDamagePack, false);
                    execution.parameters.attackStatusData.owner.ObtainAttackPack(attackData, false);
                    if (attackData.isMissed) { }
                    else if (attackData.isDodged) { }
                    else if (attackData.isBlocked) { }
                    else execution.parameters.attackStatusData.RecordDamage(attackData.GetImpairedDamage());

                    attackData.Terminate();
                }
            }
            else if (context.graph.isAttackStatusPack && executionType is ExecutionType.RemoveAttackStatus)
            {
                execution.parameters.attackStatusData.owner.RemoveAttackStatus(execution.parameters.attackStatusData.attackStatus);
            }
            else if (context.graph.isAttackSkillPack && executionType is ExecutionType.RemoveSkill)
            {
                if (character == null || character.IsEmpty || !character.IsMatchType() || skillPack == null)
                    LogWarning("Found an empty Character Remove Skill Behaviour node in " + context.objectName);
                else
                    execution.parameters.attackSkillData.owner.SetAttackSkill(skillPack, false);
            }
            else if (context.graph.isAttackSkillPack && executionType is ExecutionType.AddSkill)
            {
                if (character == null || character.IsEmpty || !character.IsMatchType() || skillPack == null)
                    LogWarning("Found an empty Character Add Skill Behaviour node in " + context.objectName);
                else
                    execution.parameters.attackSkillData.owner.SetAttackSkill(skillPack, true);
            }
            else if ((context.graph.isStaminaPack || context.graph.isStatGraph || context.graph.isAttackStatusPack) && executionType is ExecutionType.CheckState)
            {
                if (checkState == CheckStateType.None)
                {
                    LogWarning("Found an empty Character Check State Behaviour node in " + context.objectName);
                }
                else
                {
                    CharacterOperator charOp = null;
                    if (context.graph.isStaminaPack)
                        charOp = execution.parameters.staminaData.owner;
                    else if (context.graph.isStatGraph)
                        charOp = execution.parameters.characterBrain.Owner;
                    else if (context.graph.isAttackStatusPack)
                        charOp = execution.parameters.attackStatusData.owner;
                    var result = false;
                    if (charOp)
                    {
                        if (checkState == CheckStateType.InFight)
                            result = charOp.underFight;
                        else if (checkState == CheckStateType.InFightCooldown)
                            result = charOp.underAttackCooldown;
                        else if (checkState == CheckStateType.InMeleeFight)
                            result = charOp.underMeleeFight;
                        else if (checkState == CheckStateType.InRangedFight)
                            result = charOp.underRangedFight;
                        else if (checkState == CheckStateType.InMeleeAttacking)
                            result = charOp.underMeleeAttacking;
                        else if (checkState == CheckStateType.InRangedAttacking)
                            result = charOp.underRangedAttacking;
                        else if (checkState == CheckStateType.UnderStance)
                            result = charOp.underStance;
                        else if (checkState == CheckStateType.InIdle)
                            result = charOp.underIdle;
                        else if (checkState == CheckStateType.InDead)
                            result = charOp.die;
                        else if (checkState == CheckStateType.UnderFlee)
                            result = charOp.underFlee;
                        else if (checkState == CheckStateType.UnderMove)
                            result = charOp.isMoving;
                        else if (checkState == CheckStateType.UnderSelect)
                            result = charOp.selected;
                        else if (checkState == CheckStateType.UnderPlayerInput)
                            result = charOp.underHighPriorityAction || charOp.underUrgentPriorityAction;
                    }

                    UpdateConditionNode(execution, updateId, result);
                }
            }
            else if (context.graph.isStatGraph && executionType is ExecutionType.CheckFlag)
            {
                var result = execution.parameters.characterBrain.Owner.flags.ContainAll(unitFlags);
                UpdateConditionNode(execution, updateId, result);
            }
            else if (context.graph.isStatGraph && executionType == ExecutionType.GetInvName)
            {
                if (paramWord == null)
                    LogWarning("Found an empty Character Get Inv Name Behaviour node in " + context.objectName);
                else
                {
                    var invName = execution.parameters.characterBrain.Owner.GetInventoryName(invTags);
                    paramWord.SetValue(!string.IsNullOrEmpty(invName) ? invName : string.Empty);
                }
            }
            else if (context.graph.isAttackStatusPack && executionType == ExecutionType.FleeTarget)
            {
                execution.parameters.attackStatusData.owner.SetActionApplyFlee(number2, bool1, bool2);
            }
            else if (context.graph.isAttackStatusPack && executionType == ExecutionType.CancelFleeTarget)
            {
                execution.parameters.attackStatusData.owner.SetActionCancelFlee();
            }
            else if (context.graph.isAttackStatusPack && executionType == ExecutionType.CancelMoveTarget)
            {
                execution.parameters.attackStatusData.owner.SetActionCancelMove();
            }
            else if (executionType == ExecutionType.GetCharacter)
            {
                if (character.IsNull || number2.IsNull())
                    LogWarning("Found an empty Character Get Character Behaviour node in " + context.objectName);
                else
                    character.SetVariableValue(CharacterOperator.GetWithId(number2));
            }
            else if (character == null || character.IsEmpty || !character.IsMatchType())
            {
                LogWarning("Found an empty Character Behaviour node in " + context.objectName);
            }
            else
            {
                var charOperator = (CharacterOperator) character;
                if (charOperator == null)
                {
                    LogWarning("Found an invalid Character Behaviour node in " + context.objectName);
                }
                else if (!charOperator.enabled || !charOperator.gameObject.activeInHierarchy)
                {
                    LogWarning("Found an inactive Character Behaviour node in " + context.objectName);
                }
                else
                {
                    if (executionType == ExecutionType.Wander)
                    {
                        if (number1 < 0f)
                        {
                            LogWarning("Found an empty Character Wander Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            InitVariables();
                            execution.variables.SetInt(proceedKey, number1 > 0f ? 0 : 1);
                            charOperator.SetActionWander(number1, bool1, execution.id.ToString());
                        }
                    }
                    else if (executionType == ExecutionType.GoTo)
                    {
                        if (location.IsEmpty || !location.IsMatchType())
                        {
                            LogWarning("Found an empty Character Go To Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            InitVariables();
                            execution.variables.SetInt(proceedKey, 0);
                            charOperator.SetActionMoveDestination(((Transform) location).position, execution.id.ToString());
                        }
                    }
                    else if (executionType == ExecutionType.Teleport)
                    {
                        if (location.IsEmpty || !location.IsMatchType())
                        {
                            LogWarning("Found an empty Character Teleport Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            InitVariables();
                            execution.variables.SetInt(proceedKey, 0);
                            charOperator.SetActionTeleport(((Transform) location).position, execution.id.ToString());
                        }
                    }
                    else if (executionType == ExecutionType.Guard)
                    {
                        if (location.IsEmpty)
                            charOperator.CancelBehaviourGuard();
                        else
                        {
                            if (number1 <= 0f)
                            {
                                LogWarning("Found an empty Character Guard Behaviour node in " + context.objectName);
                            }
                            else
                            {
                                charOperator.SetBehaviourGuard(((Transform) location).position, number1, bool1, number2);
                            }
                        }
                    }
                    else if (executionType == ExecutionType.Chase)
                    {
                        if (paramCharacter.IsEmpty || !paramCharacter.IsMatchType())
                        {
                            LogWarning("Found an empty Character Chase Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            charOperator.SetActionChase((CharacterOperator) paramCharacter, execution.id.ToString());
                        }
                    }
                    else if (executionType == ExecutionType.Visible)
                    {
                        charOperator.SetVisibility(bool1);
                    }
                    else if (executionType == ExecutionType.SearchTarget)
                    {
                        charOperator.SetBehaviourSearchTarget(bool1, bool2);
                    }
                    else if (executionType == ExecutionType.AttackTarget)
                    {
                        if (paramCharacter.IsEmpty || !paramCharacter.IsMatchType())
                        {
                            LogWarning("Found an empty Character Attack Target Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var target = (CharacterOperator) paramCharacter;
                            if (target.inited)
                                charOperator.SetActionAttackTarget(target);
                            else
                                LogWarning("Found an invalid Character Attack Target Behaviour node in " + context.objectName);
                        }
                    }
                    else if (executionType == ExecutionType.InteractTarget)
                    {
                        if (paramCharacter.IsEmpty || !paramCharacter.IsMatchType())
                        {
                            LogWarning("Found an empty Character Interact Target Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var target = (CharacterOperator) paramCharacter;
                            charOperator.SetActionInteractTarget(target);
                        }
                    }
                    else if (executionType == ExecutionType.KnockTarget)
                    {
                        if (number1.IsNull() || number2.IsNull())
                        {
                            LogWarning("Found an empty Character Knock Target Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            CharacterOperator attacker = null;
                            if (!paramCharacter.IsEmpty && paramCharacter.IsMatchType())
                                attacker = (CharacterOperator) paramCharacter;
                            var dest = charOperator.agentTransform.position;
                            if (attacker == null)
                            {
                                dest.x += ReRandom.Range((float) number1 * -1f, (float) number1);
                                dest.z += ReRandom.Range((float) number1 * -1f, (float) number1);
                            }
                            else
                            {
                                var attackPos = attacker.agentTransform.position;
                                var distance = Vector3.Distance(attackPos, dest);
                                distance += number1;
                                dest = attackPos + (attacker.agentTransform.forward * distance);
                            }

                            charOperator.SetActionKnock(dest, number2, bool1);
                        }
                    }
                    else if (executionType == ExecutionType.InterruptTarget)
                    {
                        if (context.graph.isAttackSkillPack)
                        {
                            var pass = false;
                            if (!execution.parameters.attackSkillData.damageData.isDodged && !execution.parameters.attackSkillData.damageData.isBlocked)
                                pass = true;
                            else if (execution.parameters.attackSkillData.damageData.isDodged && bool1)
                                pass = true;
                            else if (execution.parameters.attackSkillData.damageData.isBlocked && bool2)
                                pass = true;
                            if (pass)
                            {
                                var result = charOperator.SetActionInterrupt();
                                number1.SetVariableValue(result ? 1 : 0);
                            }
                            else
                            {
                                number1.SetVariableValue(0);
                            }
                        }
                    }
                    else if (executionType == ExecutionType.AddAttackStatus)
                    {
                        if (!HaveAttackStatus())
                        {
                            LogWarning("Found an empty Character Add Attack Status Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            CharacterOperator applier = null;
                            if (context.graph.isAttackDamagePack)
                                applier = execution.parameters.attackDamageData.attacker;
                            else if (context.graph.isAttackSkillPack)
                                applier = execution.parameters.attackSkillData.owner;
                            var statusData = charOperator.AddAttackStatus(GetAttackStatus(), applier);
                            if (context.graph.isAttackSkillPack)
                                execution.parameters.attackSkillData.AddAppliedAttackStatus(statusData);
                        }
                    }
                    else if (executionType == ExecutionType.RemoveAttackStatus)
                    {
                        if (!HaveAttackStatus())
                        {
                            LogWarning("Found an empty Character Remove Attack Status Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            charOperator.RemoveAttackStatus(GetAttackStatus());
                        }
                    }
                    else if (executionType == ExecutionType.HaveAttackStatus)
                    {
                        if (!HaveAttackStatus())
                        {
                            LogWarning("Found an empty Character Have Attack Status Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var result = charOperator.HaveAttackStatus(GetAttackStatus());
                            UpdateConditionNode(execution, updateId, result);
                        }
                    }
                    else if (executionType == ExecutionType.CheckState)
                    {
                        if (checkState == CheckStateType.None)
                        {
                            LogWarning("Found an empty Character Check State Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var result = false;
                            if (checkState == CheckStateType.InFight)
                                result = charOperator.underFight;
                            else if (checkState == CheckStateType.InFightCooldown)
                                result = charOperator.underAttackCooldown;
                            else if (checkState == CheckStateType.InMeleeFight)
                                result = charOperator.underMeleeFight;
                            else if (checkState == CheckStateType.InRangedFight)
                                result = charOperator.underRangedFight;
                            else if (checkState == CheckStateType.InMeleeAttacking)
                                result = charOperator.underMeleeAttacking;
                            else if (checkState == CheckStateType.InRangedAttacking)
                                result = charOperator.underRangedAttacking;
                            else if (checkState == CheckStateType.UnderStance)
                                result = charOperator.underStance;
                            else if (checkState == CheckStateType.InIdle)
                                result = charOperator.underIdle;
                            else if (checkState == CheckStateType.InDead)
                                result = charOperator.die;
                            else if (checkState == CheckStateType.UnderFlee)
                                result = charOperator.underFlee;
                            else if (checkState == CheckStateType.UnderMove)
                                result = charOperator.isMoving;
                            else if (checkState == CheckStateType.UnderSelect)
                                result = charOperator.selected;
                            else if (checkState == CheckStateType.UnderPlayerInput)
                                result = charOperator.underHighPriorityAction || charOperator.underUrgentPriorityAction;
                            UpdateConditionNode(execution, updateId, result);
                        }
                    }
                    else if (executionType == ExecutionType.CheckFlag)
                    {
                        var result = charOperator.flags.ContainAll(unitFlags);
                        UpdateConditionNode(execution, updateId, result);
                    }
                    else if (executionType == ExecutionType.CheckAttack)
                    {
                        if (string.IsNullOrEmpty(statType))
                        {
                            LogWarning("Found an empty Character Check Attack Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var result = false;
                            if (statType.Equals(MELEE_ATTACK))
                                result = charOperator.muscle.HaveMeleeAttack();
                            else if (statType.Equals(RANGED_ATTACK))
                                result = charOperator.muscle.HaveRangedAttack();
                            UpdateConditionNode(execution, updateId, result);
                        }
                    }
                    else if (executionType == ExecutionType.GetDistance)
                    {
                        if (paramCharacter.IsEmpty || !paramCharacter.IsMatchType() || !number1.IsVariable() || number1.IsNull())
                        {
                            LogWarning("Found an empty Character Get Distance Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var target = (CharacterOperator) paramCharacter;
                            var distance = Vector3.Distance(charOperator.agentTransform.position, target.agentTransform.position);
                            number1.SetVariableValue(distance);
                        }
                    }
                    else if (executionType == ExecutionType.GetMeleeAttackDistance)
                    {
                        if (paramCharacter.IsEmpty || !paramCharacter.IsMatchType() || !number1.IsVariable() || number1.IsNull())
                        {
                            LogWarning("Found an empty Character Get Melee Attack Distance Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var target = (CharacterOperator) paramCharacter;
                            var distance = charOperator.muscle.GetMeleeAttackDistance(target);
                            number1.SetVariableValue(distance);
                        }
                    }
                    else if (executionType == ExecutionType.GetAdjacentCount)
                    {
                        if (!number1.IsVariable() || number1.IsNull() || number2 <= 0f || number3 <= 0f)
                        {
                            LogWarning("Found an empty Character Get Adjacent Count Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var units = charOperator.GetAdjacentUnit(unitFlags, number2, number3);
                            number1.SetVariableValue(units.Count);
                        }
                    }
                    else if (executionType == ExecutionType.GetStatValue)
                    {
                        if (character.IsEmpty || !character.IsMatchType() || !number1.IsVariable() || number1.IsNull() || string.IsNullOrEmpty(statType))
                        {
                            LogWarning("Found an empty Character Get Stat Value Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var value = charOperator.GetStatValue(statType);
                            number1.SetVariableValue(value);
                        }
                    }
                    else if (executionType == ExecutionType.GetSpawnId)
                    {
                        if (character.IsEmpty || !character.IsMatchType() || !number1.IsVariable() || number1.IsNull())
                        {
                            LogWarning("Found an empty Character Get Spawn Id Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            number1.SetVariableValue(charOperator.spawnId);
                        }
                    }
                    else if (executionType == ExecutionType.GetCharacterId)
                    {
                        if (character.IsEmpty || !character.IsMatchType() || !number1.IsVariable() || number1.IsNull())
                        {
                            LogWarning("Found an empty Character Get Spawn Id Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            number1.SetVariableValue(charOperator.charId);
                        }
                    }
                    else if (executionType == ExecutionType.GetInvName)
                    {
                        if (paramWord == null)
                        {
                            LogWarning("Found an empty Character Get Inv Name Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var invName = charOperator.GetInventoryName(invTags);
                            paramWord.SetValue(!string.IsNullOrEmpty(invName) ? invName : string.Empty);
                        }
                    }
                    else if (executionType == ExecutionType.AtBehind)
                    {
                        if (paramCharacter.IsEmpty || !paramCharacter.IsMatchType() || !number1.IsVariable() || number1.IsNull())
                        {
                            LogWarning("Found an empty Character At Behind Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var target = (CharacterOperator) paramCharacter;
                            var atBehind = charOperator.IsStayBehindTarget(target) ? 1 : 0;
                            number1.SetVariableValue(atBehind);
                        }
                    }
                    else if (executionType == ExecutionType.InTeamFov)
                    {
                        if (paramCharacter.IsEmpty || !paramCharacter.IsMatchType())
                        {
                            LogWarning("Found an empty Character In Team Fov Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var target = (CharacterOperator) paramCharacter;
                            var inside = charOperator.IsVisibleInFogTeam(target.GetFogTeam());
                            UpdateConditionNode(execution, updateId, inside);
                        }
                    }
                    else if (executionType == ExecutionType.TriggerAction)
                    {
                        if (actionName == null)
                        {
                            LogWarning("Found an empty Character Trigger Action Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            charOperator.FeedbackGraphTrigger(TriggerNode.Type.ActionTrigger, actionName: actionName);
                        }
                    }
                    else if (executionType == ExecutionType.SetBrainProperties)
                    {
                        if (paramBrain == null)
                        {
                            LogWarning("Found an empty Character Set Brain Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            charOperator.ChangeBrainBehaviours(paramBrain, brainFlags);
                        }
                    }
                    else if (executionType == ExecutionType.SetMuscleProperties)
                    {
                        if (paramMuscle == null)
                        {
                            LogWarning("Found an empty Character Set Brain Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            charOperator.ChangeMuscleBehaviours(paramMuscle, muscleFlags);
                        }
                    }
                    else if (executionType == ExecutionType.SetPhase)
                    {
                        if (character.IsEmpty || !character.IsMatchType())
                        {
                            LogWarning("Found an empty Character Set Phase Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            charOperator.SetCustomPhase(parameterStr);
                        }
                    }
                }
            }

            base.OnStart(execution, updateId);
        }

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            if (executionType is ExecutionType.Wander or ExecutionType.GoTo or ExecutionType.Teleport)
            {
                int key = execution.variables.GetInt(proceedKey, -1);
                if (key == 0)
                {
                    if (character.IsEmpty || !character.IsMatchType())
                        return State.Failure;
                    var charOperator = (CharacterOperator) character;
                    if (charOperator.HaveReachWanderDestination(execution.id.ToString()))
                    {
                        execution.variables.SetInt(proceedKey, 1);
                        key = 1;
                    }
                }

                if (key > 0)
                    return base.OnUpdate(execution, updateId);
                return State.Running;
            }

            return base.OnUpdate(execution, updateId);
        }

        public override bool IsRequireUpdate ()
        {
            if (executionType is ExecutionType.Wander or ExecutionType.GoTo or ExecutionType.Teleport)
                return enabled;
            return false;
        }

        private bool HaveAttackStatus (bool checkEmpty = true)
        {
            if (attackStatus)
                return true;
            if (!checkEmpty && !location.IsNull && location.IsMatchType())
                return true;
            if (checkEmpty && !location.IsEmpty && location.IsMatchType())
                return true;
            return false;
        }

        private string GetAttackStatusName ()
        {
            if (attackStatus)
                return attackStatus.name;
            return location.objectName;
        }

        private AttackStatusPack GetAttackStatus ()
        {
            if (attackStatus)
            {
                ReDebug.LogWarning("Graph Warning", "<Color='Red'>Please inform developer</Color> : Attack Status Value property is being deprecated and being use in " + context.objectName);
                return attackStatus;
            }

            if (!location.IsEmpty && location.IsMatchType())
                return (AttackStatusPack) location;
            return null;
        }

#if UNITY_EDITOR
        public string GetTypeWarningMessage ()
        {
            if (executionType == ExecutionType.Teleport)
                return "Teleport is not fully functional, it only working in certain specific case.";
            return string.Empty;
        }

        public bool ShowTypeWarning ()
        {
            return executionType == ExecutionType.Teleport;
        }

        private static IEnumerable DrawActionNameListDropdown ()
        {
            return ActionNameChoice.GetActionNameListDropdown();
        }

        private void MarkBrainFlagsDirty ()
        {
            if (brainFlags.dirty)
            {
                brainFlags.dirty = false;
                MarkDirty();
            }
        }

        private void MarkMuscleFlagsDirty ()
        {
            if (muscleFlags.dirty)
            {
                muscleFlags.dirty = false;
                MarkDirty();
            }
        }

        private void MarkInvFlagsDirty ()
        {
            if (invTags.dirty)
            {
                invTags.dirty = false;
                MarkDirty();
            }
        }

        private void MarkUnitFlagsDirty ()
        {
            if (unitFlags.dirty)
            {
                unitFlags.dirty = false;
                MarkDirty();
            }
        }

        private void FixAttackStatus ()
        {
            location = new SceneObjectProperty(SceneObject.ObjectType.AttackStatusPack, "Attack Status");
            location.SetObjectValue(attackStatus);
            attackStatus = null;
        }

        private bool ShowAttackStatus ()
        {
#if REGRAPH_DESCENT
            if (executionType == ExecutionType.AddAttackStatus)
                return attackStatus;
            if (executionType == ExecutionType.HaveAttackStatus)
                return attackStatus;
            if (executionType == ExecutionType.RemoveAttackStatus)
            {
                var graph = GetGraph();
                if (graph is {isAttackStatusPack: true})
                    return false;
                return attackStatus;
            }

            return false;
#else
            return false;
#endif
        }

        private bool ShowAttackDamagePack ()
        {
            if (executionType == ExecutionType.HurtTarget)
            {
                var graph = GetGraph();
                if (graph is {isAttackStatusPack: true})
                    return true;
            }

            return false;
        }

        private bool ShowCharacter ()
        {
            if (executionType == ExecutionType.None)
                return false;
            if (executionType is ExecutionType.GetDistance or ExecutionType.AtBehind or ExecutionType.InTeamFov or ExecutionType.InUnitFov or ExecutionType.GetMeleeAttackDistance)
            {
                var graph = GetGraph();
                if (graph is {isAttackDamagePack: true})
                    return false;
            }
            else if (executionType is ExecutionType.AddAttackStatus or ExecutionType.RemoveAttackStatus or ExecutionType.HurtTarget or ExecutionType.HealTarget or ExecutionType.HaveAttackStatus)
            {
                var graph = GetGraph();
                if (graph != null)
                {
                    if (graph.isMoralePack)
                        return false;
                    if (graph.isAttackStatusPack)
                        return false;
                    if (graph.isTargetAimPack)
                        return false;
                }
            }
            else if (executionType is ExecutionType.FleeTarget or ExecutionType.CancelFleeTarget)
            {
                var graph = GetGraph();
                if (graph is {isAttackStatusPack: true})
                    return false;
            }
            else if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
            {
                var graph = GetGraph();
                if (graph is {isAttackStatusPack: true})
                    return false;
            }
            else if (executionType is ExecutionType.CheckState or ExecutionType.CheckAttack)
            {
                var graph = GetGraph();
                if (graph != null)
                {
                    if (graph.isStaminaPack)
                        return false;
                    if (graph.isStatGraph)
                        return false;
                    if (graph.isAttackStatusPack)
                        return false;
                }
            }
            else if (executionType is ExecutionType.GetInvName)
            {
                var graph = GetGraph();
                if (graph != null)
                {
                    if (graph.isStatGraph)
                        return false;
                }
            }
            else if (executionType is ExecutionType.CheckFlag)
            {
                var graph = GetGraph();
                if (graph is {isStatGraph: true})
                    return false;
            }

            return true;
        }

        private string StatTypeLabel ()
        {
            if (executionType is ExecutionType.CheckAttack)
                return "Attack Type";
            return "Stat Type";
        }

        private bool ShowStatType ()
        {
            if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
            {
                var graph = GetGraph();
                if (graph is {isAttackStatusPack: true})
                    return true;
            }
            else if (executionType is ExecutionType.CheckAttack)
            {
                return true;
            }
            else if (executionType is ExecutionType.GetStatValue)
            {
                return true;
            }

            return false;
        }

        private bool ShowCharacterParam ()
        {
            if (executionType is ExecutionType.Chase or ExecutionType.AttackTarget or ExecutionType.InteractTarget)
                return true;
            if (executionType is ExecutionType.GetDistance or ExecutionType.AtBehind or ExecutionType.InTeamFov or ExecutionType.InUnitFov or ExecutionType.GetMeleeAttackDistance)
            {
                var graph = GetGraph();
                if (graph is {isAttackDamagePack: true})
                    return false;
                return true;
            }

            if (executionType is ExecutionType.KnockTarget)
            {
                var graph = GetGraph();
                if (graph is {isAttackSkillPack: true})
                    return true;
            }

            return false;
        }

        private string Number1WarningMessage ()
        {
            return "Must use variable to save the get value.";
        }

        private bool ShowNumber1Warning ()
        {
            if (executionType is ExecutionType.GetDistance or ExecutionType.AtBehind or ExecutionType.GetStatValue or ExecutionType.GetMeleeAttackDistance or ExecutionType.GetAdjacentCount
                or ExecutionType.GetSpawnId or ExecutionType.GetCharacterId or ExecutionType.InterruptTarget)
                if (!number1.IsVariable())
                    return true;
            return false;
        }

        private bool ShowCheckStateParam ()
        {
            if (executionType == ExecutionType.CheckState)
                return true;
            return false;
        }

        private bool ShowLocation ()
        {
            if (executionType is ExecutionType.GoTo or ExecutionType.Guard or ExecutionType.Teleport)
                return true;
            if (executionType is ExecutionType.AddAttackStatus or ExecutionType.HaveAttackStatus)
                return true;
            if (executionType == ExecutionType.RemoveAttackStatus)
            {
                var graph = GetGraph();
                return graph is not {isAttackStatusPack: true};
            }

            return false;
        }

        private void AssignLocationObjectValue ()
        {
            if (executionType is ExecutionType.GoTo or ExecutionType.Guard or ExecutionType.Teleport)
                location.SetObjectValue(AssignComponent<Transform>());
        }

        private bool ShowLocationAssignObjectValue ()
        {
            if (executionType is ExecutionType.GoTo or ExecutionType.Guard or ExecutionType.Teleport)
                return location.IsObjectValueType();
            return false;
        }

        private string Number1Label ()
        {
            if (executionType is ExecutionType.Wander or ExecutionType.Guard or ExecutionType.KnockTarget)
                return "Radius";
            if (executionType is ExecutionType.GetDistance or ExecutionType.AtBehind or ExecutionType.GetStatValue or ExecutionType.GetMeleeAttackDistance or ExecutionType.GetAdjacentCount
                or ExecutionType.GetSpawnId or ExecutionType.GetCharacterId)
                return "Save To";
            if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
                return "Modifier";
            if (executionType is ExecutionType.InterruptTarget)
                return "Result";
            return string.Empty;
        }

        private bool ShowNumber1 ()
        {
            if (executionType is ExecutionType.Wander or ExecutionType.GetDistance or ExecutionType.AtBehind or ExecutionType.AddStatusEffect 
                or ExecutionType.GetSpawnId or ExecutionType.GetCharacterId
                or ExecutionType.RemoveStatusEffect or ExecutionType.GetStatValue or ExecutionType.GetMeleeAttackDistance or ExecutionType.KnockTarget or ExecutionType.GetAdjacentCount
                or ExecutionType.InterruptTarget)
                return true;
            if (executionType is ExecutionType.Guard)
                if (!location.IsNull)
                    return true;
            return false;
        }

        private string Number2Label ()
        {
            if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
                return "Multiplier";
            if (executionType is ExecutionType.KnockTarget)
                return "Speed";
            if (executionType is ExecutionType.HealTarget)
                return "Value";
            if (executionType is ExecutionType.FleeTarget or ExecutionType.GetAdjacentCount)
                return "Distance";
            if (executionType is ExecutionType.Guard)
                return "Return Radius";
            if (executionType is ExecutionType.GetCharacter)
                return "ID";
            return string.Empty;
        }

        private string Number3Label ()
        {
            if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
                return "Magnifier";
            if (executionType is ExecutionType.GetAdjacentCount)
                return "Angle";
            return string.Empty;
        }

        private string Number4Label ()
        {
            if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
                return "Base";
            return string.Empty;
        }

        private bool ShowNumber2 ()
        {
            if (executionType == ExecutionType.AddStatusEffect && statType is StatType.STAT_CURRENT_HEALTH or StatType.STAT_CURRENT_STAMINA or StatType.STAT_CURRENT_MORALE)
                return false;
            if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
                return true;
            if (executionType is ExecutionType.FleeTarget or ExecutionType.KnockTarget or ExecutionType.GetAdjacentCount)
                return true;
            if (executionType is ExecutionType.Guard && bool1)
                return true;
            if (executionType is ExecutionType.HealTarget)
                return true;
            if (executionType is ExecutionType.GetCharacter)
                return true;
            return false;
        }

        private bool ShowNumber3 ()
        {
            if (executionType == ExecutionType.AddStatusEffect && statType is StatType.STAT_CURRENT_HEALTH or StatType.STAT_CURRENT_STAMINA or StatType.STAT_CURRENT_MORALE)
                return false;
            if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect or ExecutionType.GetAdjacentCount)
                return true;
            return false;
        }

        private bool ShowNumber4 ()
        {
            if (executionType == ExecutionType.AddStatusEffect && statType is StatType.STAT_CURRENT_HEALTH or StatType.STAT_CURRENT_STAMINA or StatType.STAT_CURRENT_MORALE)
                return false;
            if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
                return true;
            return false;
        }

        private string Bool1Label ()
        {
            if (executionType is ExecutionType.Wander)
                return "Relocate";
            if (executionType is ExecutionType.Wander or ExecutionType.SearchTarget)
                return "Idle";
            if (executionType is ExecutionType.FleeTarget)
                return "Keep Target";
            if (executionType is ExecutionType.KnockTarget)
                return "Continuous";
            if (executionType is ExecutionType.Visible)
                return "Visible";
            if (executionType is ExecutionType.InterruptTarget)
                return "Include Dodge";
            if (executionType is ExecutionType.Guard)
                return "Return Guard";
            return string.Empty;
        }

        private string Bool2Label ()
        {
            if (executionType is ExecutionType.FleeTarget)
                return "Behind Target";
            if (executionType is ExecutionType.SearchTarget)
                return "Stance Idle";
            if (executionType is ExecutionType.InterruptTarget)
                return "Include Block";
            if (executionType is ExecutionType.HealTarget)
                return "Visible";
            return string.Empty;
        }

        private bool ShowBool1 ()
        {
            if (executionType is ExecutionType.Wander or ExecutionType.SearchTarget or ExecutionType.KnockTarget or ExecutionType.Visible or ExecutionType.FleeTarget or ExecutionType.InterruptTarget
                or ExecutionType.Guard)
                return true;
            return false;
        }

        private bool ShowBool2 ()
        {
            if (executionType is ExecutionType.FleeTarget or ExecutionType.SearchTarget or ExecutionType.InterruptTarget or ExecutionType.HealTarget)
                return true;
            return false;
        }

        public ValueDropdownList<string> StatTypeChoice ()
        {
            var typeListDropdown = new ValueDropdownList<string>();
            if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
            {
                var graph = GetGraph();
                if (graph is {isAttackStatusPack: true})
                {
                    if (executionType == ExecutionType.AddStatusEffect)
                    {
                        typeListDropdown.Add("Character/" + StatType.STAT_CURRENT_HEALTH, StatType.STAT_CURRENT_HEALTH);
                        typeListDropdown.Add("Character/" + StatType.STAT_CURRENT_STAMINA, StatType.STAT_CURRENT_STAMINA);
                        typeListDropdown.Add("Character/" + StatType.STAT_CURRENT_MORALE, StatType.STAT_CURRENT_MORALE);
                        typeListDropdown = StatType.GetStatNameListDropdown(typeListDropdown);
                    }
                    else if (executionType == ExecutionType.RemoveStatusEffect)
                        typeListDropdown = StatType.GetStatNameListDropdown(typeListDropdown);
                }
            }
            else if (executionType is ExecutionType.GetStatValue)
            {
                typeListDropdown = StatType.GetAllStatNameListDropdown(typeListDropdown);
            }
            else if (executionType is ExecutionType.CheckAttack)
            {
                typeListDropdown.Add(MELEE_ATTACK, MELEE_ATTACK);
                typeListDropdown.Add(RANGED_ATTACK, RANGED_ATTACK);
            }

            return typeListDropdown;
        }

        public ValueDropdownList<ExecutionType> TypeChoice ()
        {
            var typeListDropdown = new ValueDropdownList<ExecutionType>();
            var graph = GetGraph();
            if (graph != null)
            {
                if (graph.isTargetAimPack)
                {
                    typeListDropdown.Add("Check State", ExecutionType.CheckState);
                    typeListDropdown.Add("Have Attack", ExecutionType.CheckAttack);
                    typeListDropdown.Add("Have Attack Status", ExecutionType.HaveAttackStatus);
                    typeListDropdown.Add("In Team Fov", ExecutionType.InTeamFov);
                    typeListDropdown.Add("Get Stat Value", ExecutionType.GetStatValue);
                    typeListDropdown.Add("Get Distance", ExecutionType.GetDistance);
                    typeListDropdown.Add("Get Spawn ID", ExecutionType.GetSpawnId);
                    typeListDropdown.Add("Get Melee Distance", ExecutionType.GetMeleeAttackDistance);
                    typeListDropdown.Add("Get Behind", ExecutionType.AtBehind);
                    typeListDropdown.Add("Add Attack Status", ExecutionType.AddAttackStatus);
                    return typeListDropdown;
                }
                else if (graph.isStaminaPack)
                {
                    typeListDropdown.Add("Check State", ExecutionType.CheckState);
                    return typeListDropdown;
                }
                else if (graph.isAttackDamagePack || graph.isAttackSkillPack)
                {
                    if (graph.isAttackSkillPack)
                    {
                        typeListDropdown.Add("Knock Target", ExecutionType.KnockTarget);
                        typeListDropdown.Add("Interrupt Target", ExecutionType.InterruptTarget);
                        typeListDropdown.Add("Add Skill", ExecutionType.AddSkill);
                        typeListDropdown.Add("Remove Skill", ExecutionType.RemoveSkill);
                        typeListDropdown.Add("Have Attack Status", ExecutionType.HaveAttackStatus);
                    }

                    typeListDropdown.Add("Add Attack Status", ExecutionType.AddAttackStatus);
                    typeListDropdown.Add("Remove Attack Status", ExecutionType.RemoveAttackStatus);
                    typeListDropdown.Add("Check Flag", ExecutionType.CheckFlag);
                    typeListDropdown.Add("Check State", ExecutionType.CheckState);
                    typeListDropdown.Add("Have Attack", ExecutionType.CheckAttack);
                    typeListDropdown.Add("Get Stat Value", ExecutionType.GetStatValue);
                    typeListDropdown.Add("Get Distance", ExecutionType.GetDistance);
                    typeListDropdown.Add("Get Melee Distance", ExecutionType.GetMeleeAttackDistance);
                    typeListDropdown.Add("Get Adjacent Count", ExecutionType.GetAdjacentCount);
                    typeListDropdown.Add("Get Inv Name", ExecutionType.GetInvName);
                    typeListDropdown.Add("Get Behind", ExecutionType.AtBehind);
                    typeListDropdown.Add("In Unit Fov", ExecutionType.InUnitFov);
                    typeListDropdown.Add("In Team Fov", ExecutionType.InTeamFov);
                    return typeListDropdown;
                }
                else if (graph.isMoralePack)
                {
                    typeListDropdown.Add("Add Attack Status", ExecutionType.AddAttackStatus);
                    typeListDropdown.Add("Remove Attack Status", ExecutionType.RemoveAttackStatus);
                    return typeListDropdown;
                }
                else if (graph.isAttackStatusPack)
                {
                    typeListDropdown.Add("Add Status Effect", ExecutionType.AddStatusEffect);
                    typeListDropdown.Add("Remove Status Effect", ExecutionType.RemoveStatusEffect);
                    typeListDropdown.Add("Remove Attack Status", ExecutionType.RemoveAttackStatus);
                    typeListDropdown.Add("Hurt Owner", ExecutionType.HurtTarget);
                    typeListDropdown.Add("Heal Owner", ExecutionType.HealTarget);
                    typeListDropdown.Add("Check State", ExecutionType.CheckState);
                    typeListDropdown.Add("Flee", ExecutionType.FleeTarget);
                    typeListDropdown.Add("Stop Flee", ExecutionType.CancelFleeTarget);
                    typeListDropdown.Add("Cancel Move", ExecutionType.CancelMoveTarget);
                    return typeListDropdown;
                }
            }

            typeListDropdown.Add("Check State", ExecutionType.CheckState);
            typeListDropdown.Add("Check Flag", ExecutionType.CheckFlag);
            typeListDropdown.Add("Have Attack", ExecutionType.CheckAttack);
            typeListDropdown.Add("Wander", ExecutionType.Wander);
            typeListDropdown.Add("Go To", ExecutionType.GoTo);
            typeListDropdown.Add("Teleport", ExecutionType.Teleport);
            typeListDropdown.Add("Chase", ExecutionType.Chase);
            typeListDropdown.Add("Guard", ExecutionType.Guard);
            typeListDropdown.Add("Visible", ExecutionType.Visible);
            typeListDropdown.Add("Interact Target", ExecutionType.InteractTarget);
            typeListDropdown.Add("Search Target", ExecutionType.SearchTarget);
            typeListDropdown.Add("Attack Target", ExecutionType.AttackTarget);
            typeListDropdown.Add("Add Attack Status", ExecutionType.AddAttackStatus);
            typeListDropdown.Add("Remove Attack Status", ExecutionType.RemoveAttackStatus);
            typeListDropdown.Add("Get Distance", ExecutionType.GetDistance);
            typeListDropdown.Add("Get Melee Distance", ExecutionType.GetMeleeAttackDistance);
            typeListDropdown.Add("Get Behind", ExecutionType.AtBehind);
            typeListDropdown.Add("Get Spawn ID", ExecutionType.GetSpawnId);
            typeListDropdown.Add("Get Character ID", ExecutionType.GetCharacterId);
            typeListDropdown.Add("Get Stat Value", ExecutionType.GetStatValue);
            typeListDropdown.Add("Get Character", ExecutionType.GetCharacter);
            typeListDropdown.Add("In Team Fov", ExecutionType.InTeamFov);
            typeListDropdown.Add("Get Inv Name", ExecutionType.GetInvName);
            typeListDropdown.Add("Trigger Action", ExecutionType.TriggerAction);
            typeListDropdown.Add("Set Phase", ExecutionType.SetPhase);
            typeListDropdown.Add("Change Brain", ExecutionType.SetBrainProperties);
            typeListDropdown.Add("Change Muscle", ExecutionType.SetMuscleProperties);
            return typeListDropdown;
        }

        private static IEnumerable CheckStateTypeChoice = new ValueDropdownList<CheckStateType>()
        {
            {"In Idle", CheckStateType.InIdle},
            {"In Stance", CheckStateType.UnderStance},
            {"In Fight", CheckStateType.InFight},
            {"In Fight Cooldown", CheckStateType.InFightCooldown},
            {"In Melee Fight", CheckStateType.InMeleeFight},
            {"In Melee Attacking", CheckStateType.InMeleeAttacking},
            {"In Ranged Fight", CheckStateType.InRangedFight},
            {"In Ranged Attacking", CheckStateType.InRangedAttacking},
            {"In Dead", CheckStateType.InDead},
            {"In Flee", CheckStateType.UnderFlee},
            {"In Move", CheckStateType.UnderMove},
            {"In Select", CheckStateType.UnderSelect},
            {"Under Input Move", CheckStateType.UnderPlayerInput},
        };

        private void OnChangeType ()
        {
            if (executionType is ExecutionType.GoTo or ExecutionType.Guard or ExecutionType.Teleport)
                location = new SceneObjectProperty(SceneObject.ObjectType.Transform, "GameObjectLocation");
            else if (executionType is ExecutionType.AddAttackStatus or ExecutionType.HaveAttackStatus or ExecutionType.RemoveAttackStatus)
                location = new SceneObjectProperty(SceneObject.ObjectType.AttackStatusPack, "Attack Status");
            if (executionType == ExecutionType.RemoveStatusEffect)
                if (statType is StatType.STAT_CURRENT_HEALTH or StatType.STAT_CURRENT_STAMINA or StatType.STAT_CURRENT_MORALE)
                    statType = string.Empty;
            if (executionType == ExecutionType.GetCharacter)
                character.AllowVariableOnly();
            else
                character.AllowAll();
            
            var graph = GetGraph();
            if (graph is {isBehaviourPack: true})
            {
                character.AllowVariableOnly();
                paramCharacter.AllowVariableOnly();
                location.AllowVariableOnly();
            }
            
            MarkDirty();
            MarkRepaint();
        }

        public static string displayName = "Character Behaviour Node";
        public static string nodeName = "Character";

        public override bool IsPortReachable (GraphNode node)
        {
            if (node is YesConditionNode or NoConditionNode)
                return AcceptConditionNode();
            if (node is ChoiceConditionNode)
                return false;
            return true;
        }

        public bool AcceptConditionNode ()
        {
            return executionType is ExecutionType.CheckState or ExecutionType.InTeamFov or ExecutionType.InUnitFov or ExecutionType.CheckFlag or ExecutionType.CheckAttack
                or ExecutionType.HaveAttackStatus;
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
            return executionType.ToString();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Gameplay/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType != ExecutionType.None)
            {
                if (executionType == ExecutionType.Wander && !character.IsNull && character.IsMatchType())
                {
                    if (number1 > 0f)
                        return character.objectName + " wander in range of " + number1 + "\n<color=#FFF600>Continue at arrival";
                    return character.objectName + " cancel wander";
                }

                if (executionType == ExecutionType.Visible && !character.IsNull && character.IsMatchType())
                {
                    return character.objectName + "'s visible : " + bool1.ToString();
                }

                if (executionType == ExecutionType.GoTo && !character.IsNull && character.IsMatchType() && !location.IsNull && location.IsMatchType())
                {
                    return character.objectName + " go to " + location.name + "\n<color=#FFF600>Continue at arrival";
                }

                if (executionType == ExecutionType.Teleport && !character.IsNull && character.IsMatchType() && !location.IsNull && location.IsMatchType())
                {
                    return character.objectName + " teleport " + location.name;
                }

                if (executionType == ExecutionType.Chase && !character.IsNull && character.IsMatchType() && !paramCharacter.IsNull && paramCharacter.IsMatchType())
                {
                    return character.objectName + " chase " + paramCharacter.objectName;
                }

                if (executionType == ExecutionType.Guard && !character.IsNull && character.IsMatchType())
                {
                    if (location.IsNull)
                        return character.objectName + " cancel guard";
                    else if (number1.IsVariable() || (!number1.IsVariable() && number1 > 0))
                        return character.objectName + " guard " + location.name + " within range " + number1;
                }

                if (executionType == ExecutionType.CheckState && checkState != CheckStateType.None)
                {
                    var graph = GetGraph();
                    if (graph != null && (graph.isStaminaPack || graph.isStatGraph || graph.isAttackStatusPack))
                        return "owner check " + checkState + " state";
                    if (!character.IsNull && character.IsMatchType())
                        return character.objectName + " check " + checkState + " state";
                }

                if (executionType == ExecutionType.CheckFlag)
                {
                    var graph = GetGraph();
                    if (graph is {isStatGraph: true})
                        return "owner check " + unitFlags.GetSelectedString(3) + " flags";
                    if (!character.IsNull && character.IsMatchType())
                        return character.objectName + " check " + unitFlags.GetSelectedString(3) + " flags";
                }

                if (executionType == ExecutionType.CheckAttack && !character.IsNull && character.IsMatchType())
                {
                    if (!string.IsNullOrEmpty(statType))
                        return character.objectName + " check have " + statType;
                }

                if (executionType == ExecutionType.SearchTarget && !character.IsNull && character.IsMatchType())
                {
                    if (bool1 && !bool2)
                        return character.objectName + " target search during idle";
                    if (!bool1 && bool2)
                        return character.objectName + " target search during stance idle";
                    if (bool1 && bool2)
                        return character.objectName + " activate target search";
                    return character.objectName + " deactivate target search";
                }

                if (executionType == ExecutionType.InteractTarget && !character.IsNull && character.IsMatchType() && !paramCharacter.IsNull && paramCharacter.IsMatchType())
                {
                    return character.objectName + " interact a target";
                }

                if (executionType == ExecutionType.AttackTarget && !character.IsNull && character.IsMatchType() && !paramCharacter.IsNull && paramCharacter.IsMatchType())
                {
                    return character.objectName + " activate attack target";
                }

                if (executionType == ExecutionType.RemoveSkill && !character.IsNull && character.IsMatchType())
                {
                    if (skillPack != null)
                        return $"Remove {skillPack.name} from {character.objectName}";
                }

                if (executionType == ExecutionType.AddSkill && !character.IsNull && character.IsMatchType())
                {
                    if (skillPack)
                        return $"Add {skillPack.name} to {character.objectName}";
                }

                if (executionType == ExecutionType.SetBrainProperties && !character.IsNull && character.IsMatchType())
                {
                    if (paramBrain)
                        return $"Change brain base on {paramBrain.name}";
                }

                if (executionType == ExecutionType.SetMuscleProperties && !character.IsNull && character.IsMatchType())
                {
                    if (paramMuscle)
                        return $"Change muscle base on {paramMuscle.name}";
                }

                if (executionType == ExecutionType.AddAttackStatus && HaveAttackStatus(false))
                {
                    var graph = GetGraph();
                    if (graph != null)
                    {
                        if (graph.isMoralePack || graph.isTargetAimPack)
                            return "owner add attack status : " + GetAttackStatusName();
                        if (!character.IsNull)
                            return character.objectName + " add attack status : " + GetAttackStatusName();
                    }
                }

                if (executionType == ExecutionType.RemoveAttackStatus)
                {
                    var graph = GetGraph();
                    if (HaveAttackStatus(false))
                    {
                        if (graph is {isMoralePack: true})
                            return "owner remove attack status : " + GetAttackStatusName();
                        if (!character.IsNull)
                            return character.objectName + " remove attack status : " + GetAttackStatusName();
                    }
                    else
                    {
                        if (graph is {isAttackStatusPack: true})
                            return "owner remove this attack status";
                    }
                }

                if (executionType == ExecutionType.HaveAttackStatus && HaveAttackStatus(false))
                {
                    var graph = GetGraph();
                    if (graph is {isTargetAimPack: true})
                        return "owner have attack status : " + GetAttackStatusName();
                    if (!character.IsNull)
                        return character.objectName + " have attack status : " + GetAttackStatusName();
                }

                if (executionType == ExecutionType.HurtTarget)
                {
                    if (attackDamagePack)
                    {
                        var graph = GetGraph();
                        if (graph is {isAttackStatusPack: true})
                            return "owner receive hurt damage";
                    }
                }

                if (executionType == ExecutionType.HealTarget)
                {
                    var graph = GetGraph();
                    if (graph is {isAttackStatusPack: true})
                        return "owner receive healing";
                }

                if (executionType == ExecutionType.KnockTarget && !character.IsNull && character.IsMatchType() && !number1.IsNull() && !number2.IsNull())
                {
                    var graph = GetGraph();
                    if (graph is {isAttackSkillPack: true})
                        return character.objectName + " get knock";
                }

                if (executionType == ExecutionType.InterruptTarget)
                {
                    var graph = GetGraph();
                    if (graph is {isAttackSkillPack: true})
                        return character.objectName + " get interrupt";
                }

                if (executionType == ExecutionType.CancelMoveTarget)
                {
                    var graph = GetGraph();
                    if (graph is {isAttackStatusPack: true})
                        return character.objectName + " get cancel movement";
                }

                if (executionType == ExecutionType.FleeTarget)
                {
                    var graph = GetGraph();
                    if (graph is {isAttackStatusPack: true})
                        return "owner get flee";
                }

                if (executionType == ExecutionType.CancelFleeTarget)
                {
                    var graph = GetGraph();
                    if (graph is {isAttackStatusPack: true})
                        return "owner stop flee";
                }

                if (executionType == ExecutionType.GetDistance && number1.IsVariable() && !number1.IsNull())
                {
                    var graph = GetGraph();
                    if (graph is {isAttackDamagePack: true})
                        return $"Distance between defender and attacker save to {number1.GetVariableName()}";
                    if (!paramCharacter.IsNull && !character.IsNull)
                        return $"Distance between {character.objectName} and {paramCharacter.objectName} save to {number1.GetVariableName()}";
                }

                if (executionType == ExecutionType.GetMeleeAttackDistance && number1.IsVariable() && !number1.IsNull())
                {
                    var graph = GetGraph();
                    if (graph is {isAttackDamagePack: true})
                        return $"Melee Attack Distance between defender and attacker save to {number1.GetVariableName()}";
                    if (!paramCharacter.IsNull && !character.IsNull)
                        return $"Melee Attack Distance between {character.objectName} and {paramCharacter.objectName} save to {number1.GetVariableName()}";
                }

                if (executionType == ExecutionType.GetAdjacentCount && !character.IsNull && character.IsMatchType() && number1.IsVariable() && !number1.IsNull() && number2.IsValidPositive() &&
                    number3.IsValidPositive())
                {
                    var graph = GetGraph();
                    if (graph is {isAttackDamagePack: true})
                        return $"{character.objectName}'s adjacent count save to {number1.GetVariableName()}";
                }

                if (executionType == ExecutionType.AtBehind && number1.IsVariable() && !number1.IsNull())
                {
                    var graph = GetGraph();
                    if (graph is {isAttackDamagePack: true})
                        return $"Is attacker behind defender save to {number1.GetVariableName()}";
                    if (!paramCharacter.IsNull && !character.IsNull)
                        return $"Is {character.objectName} behind {paramCharacter.objectName} save to {number1.GetVariableName()}";
                }

                if (executionType == ExecutionType.InTeamFov)
                {
                    var graph = GetGraph();
                    if (graph is {isAttackDamagePack: true})
                        return $"Check attacker in defender's team FOV";
                    if (!paramCharacter.IsNull && !character.IsNull)
                        return $"Check {character.objectName} in {paramCharacter.objectName}'s team FOV";
                }

                if (executionType == ExecutionType.InUnitFov)
                {
                    var graph = GetGraph();
                    if (graph is {isAttackDamagePack: true})
                        return $"Check attacker in defender's FOV";
                }

                if (executionType == ExecutionType.TriggerAction && !character.IsNull && character.IsMatchType() && actionName != null)
                {
                    return "Trigger Behaviour Action : " + actionName;
                }

                if (executionType == ExecutionType.GetInvName && paramWord != null)
                {
                    var graph = GetGraph();
                    if (graph is {isStatGraph: true})
                        return $"Get owner's inventory name into {paramWord.name}";
                    if (!character.IsNull && character.IsMatchType())
                        return $"Get {character.objectName}'s inventory name into {paramWord.name}";
                }

                if (executionType == ExecutionType.GetSpawnId && !character.IsNull && character.IsMatchType() && number1.IsVariable() && !number1.IsNull())
                {
                    return $"Get spawn id to {number1.GetVariableName()}";
                }
                
                if (executionType == ExecutionType.GetCharacterId && !character.IsNull && character.IsMatchType() && number1.IsVariable() && !number1.IsNull())
                {
                    return $"Get character id to {number1.GetVariableName()}";
                }
                
                if (executionType == ExecutionType.GetCharacter && !character.IsNull && !number2.IsNull())
                {
                    return $"Get character {number2} to {character.GetVariableName()}";
                }

                if (executionType == ExecutionType.GetStatValue && !character.IsNull && character.IsMatchType() && number1.IsVariable() && !number1.IsNull())
                {
                    if (!string.IsNullOrEmpty(statType))
                        return $"Get {statType} value to {number1.GetVariableName()}";
                }

                if (executionType == ExecutionType.SetPhase && !character.IsNull && character.IsMatchType())
                {
                    return $"Set {parameterStr} into {character.objectName}'s phase";
                }

                if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
                {
                    var graph = GetGraph();
                    if (graph is {isAttackStatusPack: true})
                    {
                        if (!string.IsNullOrEmpty(statType) && !number1.IsNull() && !number2.IsNull() && !number3.IsNull() && !number4.IsNull())
                        {
                            if (executionType == ExecutionType.AddStatusEffect)
                            {
                                if (statType is StatType.STAT_CURRENT_HEALTH or StatType.STAT_CURRENT_STAMINA or StatType.STAT_CURRENT_MORALE)
                                    return $"Add {statType} {number1} mod";
                                return $"Add {statType} {number1} mod, {number2} mtp & {number3} mag";
                            }

                            if (executionType == ExecutionType.RemoveStatusEffect)
                                return $"Remove {statType} {number1} mod, {number2} mtp & {number3} mag";
                        }
                    }
                }
            }

            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (executionType is ExecutionType.Wander or ExecutionType.GoTo or ExecutionType.Chase or ExecutionType.Teleport)
                tip += "This will provide several movement controls to specific character.\n\n";
            else if (executionType is ExecutionType.AddAttackStatus or ExecutionType.RemoveAttackStatus or ExecutionType.HaveAttackStatus)
                tip += "This will add/remove/check attack status to/from/on character.\n\nAttack Status is a group of attack effect which condition setup in Graph.\n\n";
            else if (executionType is ExecutionType.AddStatusEffect or ExecutionType.RemoveStatusEffect)
                tip += "This will add/remove attack effect to/from character.\n\nAttack Effect is modifier (MOD) or multiply (MTP) for a stat.\n\n";
            else if (executionType is ExecutionType.RemoveSkill)
                tip += "This will remove skill from character.\n\n";
            else if (executionType is ExecutionType.Visible)
                tip += "This will set visibility of character.\n\n";
            else if (executionType is ExecutionType.AddSkill)
                tip += "This will add skill to character.\n\n";
            else if (executionType is ExecutionType.HurtTarget)
                tip += "This will apply attack damage to character.\n\n";
            else if (executionType is ExecutionType.HealTarget)
                tip += "This will apply healing to character.\n\n";
            else if (executionType is ExecutionType.KnockTarget)
                tip += "This will apply knock status to character.\n\n";
            else if (executionType is ExecutionType.InterruptTarget)
                tip += "This will interrupt character's current attack.\n\n";
            else if (executionType is ExecutionType.CancelMoveTarget)
                tip += "This will cancel character's current movement.\n\n";
            else if (executionType is ExecutionType.FleeTarget or ExecutionType.CancelFleeTarget)
                tip += "This will apply/unapply flee status to character.\n\n";
            else if (executionType is ExecutionType.CheckState or ExecutionType.AtBehind or ExecutionType.InTeamFov or ExecutionType.InUnitFov or ExecutionType.CheckFlag or ExecutionType.CheckAttack)
                tip += "This will several condition checking on specific character.\n\n";
            else if (executionType is ExecutionType.SearchTarget)
                tip += "This will command specific character to search for an aim target.\n\n";
            else if (executionType is ExecutionType.InteractTarget)
                tip += "This will command specific character to interact a target.\n\n";
            else if (executionType is ExecutionType.AttackTarget)
                tip += "This will command specific character direct attack a Character Operator without go thru the aim target logic.\n\n";
            else if (executionType is ExecutionType.GetDistance)
                tip += "This will get the distance between 2 characters.\n\n";
            else if (executionType is ExecutionType.GetMeleeAttackDistance)
                tip += "This will get the melee attack distance between 2 characters which involving engage distance and attack range.\n\n";
            else if (executionType is ExecutionType.GetAdjacentCount)
                tip += "This will get the unit count with adjacent conditions which involving unit flag, distance, angle.\n\n";
            else if (executionType is ExecutionType.GetStatValue)
                tip += "This will get the stat value from specific character.\n\n";
            else if (executionType is ExecutionType.GetSpawnId)
                tip += "This will get the spawn id from specific character.\n\n";
            else if (executionType is ExecutionType.GetCharacterId)
                tip += "This will get the character id from specific character.\n\n";
            else if (executionType is ExecutionType.GetCharacter)
                tip += "This will get the specific character base on character id.\n\n";
            else if (executionType is ExecutionType.TriggerAction)
                tip += "This will trigger the specific action in character behaviour graphs.\n\n";
            else if (executionType is ExecutionType.GetInvName)
                tip += "This will get the first tag-match inventory name assigned to the Character Operator.\n\n";
            else if (executionType is ExecutionType.SetBrainProperties or ExecutionType.SetMuscleProperties)
                tip += "This will change the Character Operator's brain/muscle behaviours during runtime.\n\n";
            else if (executionType is ExecutionType.SetBrainProperties or ExecutionType.SetPhase)
                tip += "This will set the Character Operator's custom phase.\n\n";
            else
                tip += "This will execute all Character Operator related behaviour.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}