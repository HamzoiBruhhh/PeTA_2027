using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReGraph;
using Reshape.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class CharacterOperator : BaseBehaviour, IVisible
    {
        public class PendingInterrupt
        {
            public enum ActionType
            {
                None,
                Interact,
                Move,
                Teleport,
                Wander,
                Chase,
                Attack,
                Stance,
                Unstance,
                Knock,
                Flee,
                Interrupt,
                LaunchSkill,
                CancelMove,
            }

            public ActionType action = ActionType.None;
            public CharacterOperator target;
            public Vector3 destination;
            public string executeId;
            public string layerName;
            public float range;
            public bool check;
            public ActionPriority priority;
            public AttackSkillPack skillPack;
        }

        public enum AttackType
        {
            None = 0,
            MeleeNormal = 1,
            RangedNormal = 2,
            EffectNormal = 3,
            SkillNormal = 100,
        }

        public enum ActionPriority
        {
            None = 0,
            Low = 10,
            Medium = 20,
            High = 30,
            Urgent = 100
        }

        public static List<CharacterOperator> tempList;

        private static List<CharacterOperator> characters;
        private static MultiTag tempInvTags = new("Tags", typeof(MultiTagInv));

        [PropertyOrder(-1000)]
        [BoxGroup("Core")]
        [Hint("showHints", "Define the unit can be mark as attack target by opponent.")]
        public bool targetable = true;

        [HideInPlayMode]
        [BoxGroup("Core")]
        [Hint("showHints", "Define the motor agent use by battle unit.")]
        public MotorAgent motorAgent;

        [HideInPlayMode]
        [BoxGroup("Core")]
        [Hint("showHints", "Define the FOW agent use by battle unit.")]
        public FogOfWarAgent fogOfWarAgent;

        [HideInPlayMode]
        [BoxGroup("Core")]
        [Hint("showHints", "Define the graph to do stat calculation.")]
        [InfoBox("The assigned graph is not match the require type : Stat Graph", InfoMessageType.Error, "@IsShowStatGraphMismatchWarning()")]
        public GraphRunner statGraph;

        [DisableInPlayMode]
        [BoxGroup("Core")]
        [Hint("showHints", "Define all specific behaviour of the battle unit, for instance Morale behaviour.")]
        public GraphRunner[] behaviourList;

        [ShowIf("DisplaySecretCore")]
        [SerializeField]
#if REGRAPH_DEV_DEBUG
        [InlineEditor]
#endif
        internal CharacterBrain brain;

        [ShowIf("DisplaySecretCore")]
        [SerializeField]
#if REGRAPH_DEV_DEBUG
        [InlineEditor]
#endif
        internal CharacterMuscle muscle;

        [ShowIf("DisplaySecretCore")]
        [SerializeField]
#if REGRAPH_DEV_DEBUG
        [InlineEditor]
#endif
        internal CharacterMotor motor;

        [BoxGroup("Core")]
        [Hint("showHints", "Define which inventory the battle unit have.")]
        public InventoryBehaviour[] inventoryList;

        [BoxGroup("Core")]
        [Hint("showHints", "Define a set of character save data.")]
        [HideInPlayMode]
        [InfoBox("Require Character ID not equal to 0 in order to make save data working.", InfoMessageType.Warning, "CheckSaveDataList")]
        public DataSavePack[] saveDataList;

        [PropertyOrder(900)]
        [BoxGroup("Core")]
        public bool printLog;

        [PropertyOrder(-200)]
        [BoxGroup("Core")]
        [LabelText("Unit Flag")]
        [Hint("showHints", "Define which flag / category / type the battle unit belong to, it allow multiple choices selection.")]
        public MultiTag flags = new MultiTag("Unit Flag", typeof(MultiTagUnit));

        [HideInEditorMode]
        [ShowInInspector]
        [ListDrawerSettings(ListElementLabelName = "statusPackName")]
        private List<AttackStatusData> affectedAttackStatus;

        [ShowInInspector]
        [HideInEditorMode]
        [BoxGroup("General")]
        private string currentPendingAction => pendingAction == null ? string.Empty : pendingAction.action.ToString();

        [SerializeField]
        [DisableInPlayMode]
        [BoxGroup("General")]
        [LabelText("Character ID")]
        [InlineProperty]
        private FloatProperty characterId;

        [SerializeField]
        [DisableInPlayMode]
        [BoxGroup("General")]
        [LabelText("Spawn ID")]
        private int spawnerId;

        internal CharacterOperator statValueCharacterParam;
        private bool paused;
        private bool statEmptiness;
        private PendingInterrupt pendingAction;
        private GraphRunner runner;
        private ActionPriority currentActionPriority;
        private CharacterOperator interactTarget;
        private string[] statGraphGetTriggers;
        private string[] statGraphChangeTriggers;
        private string customPhase;
        private int prepareDestroy;
        private CharacterOperator currentInteractWith;
        private bool currentSelected;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static List<CharacterOperator> GetAllWithTags (MultiTag unitFlags, bool includeDead, bool includeNotInit, bool includeNotTargetable)
        {
            tempList.Clear();
            for (var i = 0; i < characters.Count; i++)
            {
                if (characters[i].flags.ContainAll(unitFlags))
                {
                    if (!includeNotInit && !characters[i].inited)
                        continue;
                    if (!includeDead && characters[i].die)
                        continue;
                    if (!includeNotTargetable && !characters[i].targetable)
                        continue;
                    tempList.Add(characters[i]);
                }
            }

            return tempList;
        }

        public static CharacterOperator GetWithInventory (string inv, bool activeOnly)
        {
            for (var i = 0; i < characters.Count; i++)
                if (characters[i].HaveInventory(inv) && (!activeOnly || characters[i].gameObject.activeSelf))
                    return characters[i];
            return null;
        }
        
        public static CharacterOperator GetWithId (int id)
        {
            for (var i = 0; i < characters.Count; i++)
                if (characters[i].characterId == id)
                    return characters[i];
            return null;
        }

        public static bool CanStandOnLocation (Vector3 position, float radius, List<CharacterOperator> excluded)
        {
            var canStand = true;
            for (var i = 0; i < characters.Count; i++)
            {
                if (!characters[i])
                    continue;
                if (excluded != null && excluded.Contains(characters[i]))
                    continue;
                var unitTrans = characters[i].agentTransform;
                var distance = Vector3.Distance(position, unitTrans.position);
                if (distance <= radius + characters[i].agentSize)
                {
                    canStand = false;
                    break;
                }
            }

            return canStand;
        }

        public static int GetPrepareApproachingCount (CharacterOperator character)
        {
            int underStandbyApproach = 0;
            for (var i = 0; i < characters.Count; i++)
            {
                if (characters[i].muscle.underPrepareApproach && characters[i].muscle.HaveSameAttackTarget(character))
                    underStandbyApproach++;
            }

            return underStandbyApproach;
        }

        public static void AddItemAttackStatus (string inv, AttackStatusPack attackStatusPack)
        {
            for (var i = 0; i < characters.Count; i++)
            {
                if (characters[i].HaveInventory(inv))
                {
                    characters[i].AddAttackStatus(attackStatusPack, characters[i]);
                }
            }
        }

        public static void RemoveItemAttackStatus (string inv, AttackStatusPack attackStatusPack)
        {
            for (var i = 0; i < characters.Count; i++)
            {
                if (characters[i].HaveInventory(inv))
                {
                    characters[i].RemoveAttackStatus(attackStatusPack);
                }
            }
        }

        public static void AddItemAttackSkill (string inv, AttackSkillPack attackSkillPack)
        {
            for (var i = 0; i < characters.Count; i++)
            {
                if (characters[i].HaveInventory(inv))
                {
                    characters[i].SetAttackSkill(attackSkillPack, true);
                }
            }
        }

        public static void RemoveItemAttackSkill (string inv, AttackSkillPack attackSkillPack)
        {
            for (var i = 0; i < characters.Count; i++)
            {
                if (characters[i].HaveInventory(inv))
                {
                    characters[i].SetAttackSkill(attackSkillPack, false);
                }
            }
        }

        public static InventoryBehaviour.ApplyStatusTrigger GetInvApplyStatusType (string inv)
        {
            for (var i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                for (var j = 0; j < character.inventoryList.Length; j++)
                {
                    if (inv == character.inventoryList[j].Name)
                    {
                        return character.inventoryList[j].ApplyStatusType;
                    }
                }
            }

            return InventoryBehaviour.ApplyStatusTrigger.None;
        }

        public static InventoryBehaviour.ApplySkillTrigger GetInvApplySkillType (string inv)
        {
            for (var i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                for (var j = 0; j < character.inventoryList.Length; j++)
                {
                    if (inv == character.inventoryList[j].Name)
                    {
                        return character.inventoryList[j].ApplySkillType;
                    }
                }
            }

            return InventoryBehaviour.ApplySkillTrigger.None;
        }

        public static void ApplyInventoryStatus (string inv)
        {
            if (characters == null)
                return;
            for (var i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                for (var j = 0; j < character.inventoryList.Length; j++)
                {
                    if (inv == character.inventoryList[j].Name)
                    {
                        if (character.inventoryList[j].isSlotInApplyStatus)
                        {
                            InventoryManager.ApplyCharacterAttackStatus(character, inv);
                        }

                        break;
                    }
                }
            }
        }

        public static void ApplyInventorySkill (string inv)
        {
            if (characters == null)
                return;
            for (var i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                for (var j = 0; j < character.inventoryList.Length; j++)
                {
                    if (inv == character.inventoryList[j].Name)
                    {
                        if (character.inventoryList[j].isSlotInApplySkill)
                        {
                            InventoryManager.ApplyCharacterAttackSkill(character, inv);
                        }

                        break;
                    }
                }
            }
        }

        public static void ApplyAllInventoryStatus ()
        {
            if (characters == null)
                return;
            for (var i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                character.AddAllInventoryStatus();
            }
        }

        public static void ApplyAllInventorySkill ()
        {
            if (characters == null)
                return;
            for (var i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                character.LearnAllInventorySkill();
            }
        }

        public static void LoadAllSave ()
        {
            if (characters == null)
                return;
            for (var i = 0; i < characters.Count; i++)
                if (characters[i])
                    characters[i].LoadAllSaveValue();
        }

        public static void StoreAllSave ()
        {
            if (characters == null)
                return;
            for (var i = 0; i < characters.Count; i++)
                if (characters[i])
                    characters[i].StoreAllSaveValue();
        }

        public static void DeleteAllSave (string saveTag, string password)
        {
            DataSavePack.DeleteAllCharacters(saveTag, password);
        }

        public static void ClearAllSave ()
        {
            if (characters == null)
                return;
            for (var i = 0; i < characters.Count; i++)
                if (characters[i])
                    characters[i].ClearAllSaveValue();
        }

        public static List<CharacterOperator> GetUnitInRange (Vector3 location, float range = 0, MultiTag unitFlag = default, List<CharacterOperator> excluded = default)
        {
            tempList.Clear();
            if (characters != null)
            {
                for (var i = 0; i < characters.Count; i++)
                {
                    if (characters[i] && characters[i].inited && !characters[i].die)
                    {
                        if (range <= 0)
                        {
                            AddUnit(characters[i]);
                        }
                        else
                        {
                            var distance = Vector3.Distance(location, characters[i].agentTransform.position);
                            if (distance <= range)
                                AddUnit(characters[i]);
                        }
                    }
                }
            }

            return tempList;

            void AddUnit (CharacterOperator co)
            {
                if (excluded != null)
                    for (var i = 0; i < excluded.Count; i++)
                        if (excluded[i] == co)
                            return;
                if (unitFlag != null)
                {
                    if (unitFlag == 0)
                        return;
                    var result = co.flags.ContainAll(unitFlag);
                    if (!result)
                        return;
                }

                tempList.Add(co);
            }
        }

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public bool inited => motor != null && muscle != null && brain != null;
        public float agentSize => motor.GetAgentSize();
        public string phase => customPhase;
        public int spawnId => spawnerId;
        public int charId => characterId;
        public bool selected => currentSelected;

        public bool asGuardian => brain.asGuardian;
        public bool asRanger => muscle.HaveRangedAttack();
        public bool underHighPriorityAction => currentActionPriority == ActionPriority.High;
        public bool underMediumPriorityAction => currentActionPriority == ActionPriority.Medium;
        public bool underLowPriorityAction => currentActionPriority == ActionPriority.Low;
        public bool underUrgentPriorityAction => currentActionPriority == ActionPriority.Urgent;

        public float maxStamina => GetStatValue(StatType.STAT_CURRENT_MAX_STAMINA);
        public float currentStamina => GetStatValue(StatType.STAT_CURRENT_STAMINA);
        public float maxMorale => GetStatValue(StatType.STAT_CURRENT_MAX_MORALE);
        public float currentMorale => GetStatValue(StatType.STAT_CURRENT_MORALE);
        public float targetMorale => GetStatValue(StatType.STAT_CURRENT_TARGET_MORALE);

        public bool underOutOfControl => GetStatValue(StatType.STAT_OUT_OF_CONTROL) > 0;
        public bool underOutOfTargetAim => GetStatValue(StatType.STAT_OUT_OF_TARGET_AIM) > 0;
        public bool underOutOfSkill => GetStatValue(StatType.STAT_OUT_OF_SKILL) > 0;
        public bool underOutOfAttack => GetStatValue(StatType.STAT_OUT_OF_ATTACK) > 0;
        public bool underOutOfGoSight => GetStatValue(StatType.STAT_OUT_OF_GO_SIGHT) > 0;
        public bool underOutOfMove => GetStatValue(StatType.STAT_OUT_OF_MOVE) > 0;

        public float moveSpeed => motor.GetMoveSpeed();
        public bool moveInCustomRotateSpeed => motor.underCustomRotateSpeed;
        public bool isMoving => motor && motor.IsAgentMoving();
        public Vector3 movingDirection => motor.GetAgentMovingDirection();
        public float moveStopDistance => motor.GetAgentStoppingDistance();

        public bool die => brain != null && brain.isDie;
        public bool dead => prepareDestroy > 0;
        public float maxHealth => GetStatValue(StatType.STAT_CURRENT_MAX_HEALTH);
        public float currentHealth => GetStatValue(StatType.STAT_CURRENT_HEALTH);
        public float healthBarMax => GetStatValue(StatType.STAT_CURRENT_BAR_MAX_HEALTH);
        public float healthBarCurrent => GetStatValue(StatType.STAT_CURRENT_BAR_HEALTH);
        public int healthBarIndex => brain.healthBarIndex;
        public int healthBarCount => brain.healthBarCount;
        public bool healVisible => brain.healVisible;

        public bool underStance => muscle != null && muscle.underStance;
        public bool underStanceSwitching => muscle != null && muscle.underStanceSwitching;
        public bool underIdle => muscle != null && muscle.underIdle;
        public bool underWalk => muscle != null && muscle.underWalk;
        public bool underPush => muscle != null && muscle.underPush;
        public bool underFlee => muscle != null && muscle.underFlee;
        public bool underFight => muscle != null && muscle.underFight;
        public bool underAttackCooldown => muscle != null && muscle.underAttackCooldown;
        public bool underRangedFight => muscle != null && muscle.underRangedFight;
        public bool underMeleeFight => muscle != null && muscle.underMeleeFight;
        public bool underAppear => muscle != null && muscle.underAppear;
        public bool underSkillFight => muscle != null && muscle.underSkillFight;
        public bool underRangedAttacking => muscle != null && muscle.underRangedAttacking;
        public bool underMeleeAttacking => muscle != null && muscle.underMeleeAttacking;
        public bool underSkillAttacking => muscle != null && muscle.underSkillAttacking;

        public float stanceDuration => GetStatValue(StatType.STAT_STANCE_DURATION);
        public float unStanceDuration => GetStatValue(StatType.STAT_UNSTANCE_DURATION);

        [BoxGroup("Combat Stat"), ShowInInspector, HideInEditorMode]
        public float meleeAttackSpeed => GetStatValue(StatType.STAT_MELEE_ATTACK_SPEED);

        [BoxGroup("Combat Stat"), ShowInInspector, HideInEditorMode]
        public float meleeAttackDuration => GetStatValue(StatType.STAT_MELEE_ATTACK_DURATION);

        public Transform agentTransform
        {
            get
            {
                if (motor != null)
                {
                    var t = motor.GetAgentTransform();
                    if (t == null)
                        t = transform;
                    return t;
                }

                return null;
            }
        }

        public float meleeAttackRange
        {
            get
            {
                if (GetStatValue(StatType.STAT_MELEE_ATTACK_RANGE, out var range))
                    if (range > agentSize)
                        return range;
                return agentSize;
            }
        }

        public float meleeStandbyDistance => muscle.GetMeleeStandbyDistance();
        public float meleeEngageDistance => muscle.GetMeleeEngageDistance();
        public float meleeEngageAmount => muscle.meleeEngageAmount;
        public float meleeAttackCooldown => GetStatValue(StatType.STAT_MELEE_ATTACK_COOLDOWN);
        public float rangedAttackCooldown => GetStatValue(StatType.STAT_RANGED_ATTACK_COOLDOWN);
        public float rangedAttackRange => GetStatValue(StatType.STAT_RANGED_ATTACK_RANGE);
        public float rangedAimDuration => GetStatValue(StatType.STAT_RANGED_AIM_DURATION);
        public float rangedAttackSpeed => GetStatValue(StatType.STAT_RANGED_ATTACK_SPEED);
        public float rangedReloadSpeed => GetStatValue(StatType.STAT_RANGED_RELOAD_SPEED);
        public float rangedAmmoCount => GetStatValue(StatType.STAT_RANGED_AMMO_COUNT);
        public float rangedAmmoTravelSpeed => GetStatValue(StatType.STAT_RANGED_AMMO_TRAVEL_SPEED);
        public float rangedAmmoAccuracy => GetStatValue(StatType.STAT_RANGED_AMMO_ACCURACY);

        public float GetAttackCooldown (int type = 0)
        {
            if (type == 0)
            {
                if (muscle.underMeleeFight)
                    type = (int) AttackType.MeleeNormal;
                else if (muscle.underRangedFight)
                    type = (int) AttackType.RangedNormal;
            }

            if (type == (int) AttackType.MeleeNormal)
                return 1f - Mathf.Clamp(muscle.attackCooldown / meleeAttackCooldown, 0, 1f);
            if (type == (int) AttackType.RangedNormal)
                return 1f - Mathf.Clamp(muscle.attackCooldown / rangedAttackCooldown, 0, 1f);
            return 1f;
        }

        public Vector2 GetAttackCooldownValue (int type = 0)
        {
            if (type == 0)
            {
                if (muscle.underMeleeFight)
                    type = (int) AttackType.MeleeNormal;
                else if (muscle.underRangedFight)
                    type = (int) AttackType.RangedNormal;
            }

            if (type == 0)
            {
                if (muscle.HaveMeleeAttack())
                    type = (int) AttackType.MeleeNormal;
                else if (muscle.HaveRangedAttack())
                    type = (int) AttackType.RangedNormal;
            }

            var value = Vector2.zero;
            value.x = muscle.attackCooldown;
            if (value.x is float.PositiveInfinity)
                value.x = 0;
            if (type == (int) AttackType.MeleeNormal)
                value.y = meleeAttackCooldown;
            if (type == (int) AttackType.RangedNormal)
                value.y = rangedAttackCooldown;
            if (value.x > value.y)
                value.x = value.y;
            return value;
        }

        public bool IsWithinMeleeEngageRange (Vector3 point, float addOn)
        {
            return Vector3.Distance(point, agentTransform.position) < meleeEngageDistance + addOn;
        }

        public bool IsWithinRangedAttackRange (Vector3 point, float addOn)
        {
            return Vector3.Distance(point, agentTransform.position) <= addOn;
        }

        public bool IsWithinInteractRange (Vector3 point)
        {
            var distance = Vector3.Distance(agentTransform.position, point);
            return distance <= GetInteractRange();
        }

        public float GetInteractRange ()
        {
            return motor.GetAgentInteractDistance() + motor.GetAgentStoppingDistance();
        }

        public void ReceiveSelect ()
        {
            if (!currentSelected)
                FeedbackGraphTrigger(TriggerNode.Type.SelectReceive);
        }

        public void ConfirmSelect ()
        {
            if (!currentSelected)
            {
                currentSelected = true;
                FeedbackGraphTrigger(TriggerNode.Type.SelectConfirm);
            }
        }

        public void FinishSelect ()
        {
            if (currentSelected)
            {
                currentSelected = false;
                FeedbackGraphTrigger(TriggerNode.Type.SelectFinish);
            }
        }

        public List<Vector3> GetMovementPath ()
        {
            return motor.GetDestinationPath();
        }

        public Transform GetDropPoint (Transform location)
        {
            return muscle.GetDropPoint(location, out var trans) ? trans : null;
        }

        public virtual void SetCustomPhase (string phase)
        {
            customPhase = phase;
        }

        protected void ClearMoveDestinationFacing ()
        {
            motor.ClearDestinationFacing();
        }

        public virtual void Face (Vector3 destination)
        {
            if (motor)
                motor.Face(destination);
            else
                transform.LookAt(destination);
        }

        public virtual Animator GetAnimator ()
        {
            return null;
        }

        public void SetVisibility (bool visible)
        {
            if (visible)
                FeedbackShow();
            else
                FeedbackHide();
        }
        
        public void LoadAllSaveValue ()
        {
            if (saveDataList != null && characterId != 0)
            {
                for (var j = 0; j < saveDataList.Length; j++)
                {
                    if (saveDataList[j])
                        LoadSaveValue(saveDataList[j].fileName);
                }
            }
        }
        
        public void StoreAllSaveValue ()
        {
            if (saveDataList != null && characterId != 0)
            {
                for (var j = 0; j < saveDataList.Length; j++)
                {
                    if (saveDataList[j])
                        StoreSaveValue(saveDataList[j].fileName);
                }
            }
        }
        
        public void ClearAllSaveValue ()
        {
            if (saveDataList != null && characterId != 0)
            {
                for (var j = 0; j < saveDataList.Length; j++)
                {
                    if (saveDataList[j])
                        ClearSaveValue(saveDataList[j].fileName);
                }
            }
        }

        public VariableScriptableObject GetSaveValue (string fileName, string dataName, VariableScriptableObject variable)
        {
            if (saveDataList != null && characterId != 0)
                for (var i = 0; i < saveDataList.Length; i++)
                    if (saveDataList[i] && saveDataList[i].Match(fileName))
                        return DataSaveManager.GetCharacterSaveValue(saveDataList[i], characterId.ToString(), dataName, variable);
            return variable;
        }

        public string GetSaveStringValue (string fileName, string dataName)
        {
            if (saveDataList != null && characterId != 0)
                for (var i = 0; i < saveDataList.Length; i++)
                    if (saveDataList[i] && saveDataList[i].Match(fileName))
                        if (DataSaveManager.GetCharacterSaveValue(saveDataList[i], characterId.ToString(), dataName, out string value))
                            return value;
            return string.Empty;
        }

        public float GetSaveFloatValue (string fileName, string dataName)
        {
            if (saveDataList != null && characterId != 0)
                for (var i = 0; i < saveDataList.Length; i++)
                    if (saveDataList[i] && saveDataList[i].Match(fileName))
                        if (DataSaveManager.GetCharacterSaveValue(saveDataList[i], characterId.ToString(), dataName, out float value))
                            return value;
            return 0;
        }

        public void SetSaveValue (string fileName, string dataName, string value)
        {
            if (saveDataList != null && characterId != 0)
            {
                for (var i = 0; i < saveDataList.Length; i++)
                {
                    if (saveDataList[i] && saveDataList[i].Match(fileName))
                    {
                        DataSaveManager.SetCharacterSaveValue(saveDataList[i], characterId.ToString(), dataName, value);
                        break;
                    }
                }
            }
        }

        public void LoadSaveValue (string fileName)
        {
            if (saveDataList != null && characterId != 0)
            {
                for (var i = 0; i < saveDataList.Length; i++)
                {
                    if (saveDataList[i] && saveDataList[i].Match(fileName))
                    {
                        DataSaveManager.LoadCharacterSaveValue(saveDataList[i], characterId.ToString());
                        break;
                    }
                }
            }
        }

        public void StoreSaveValue (string fileName)
        {
            if (saveDataList != null && characterId != 0)
            {
                for (var i = 0; i < saveDataList.Length; i++)
                {
                    if (saveDataList[i] && saveDataList[i].Match(fileName))
                    {
                        DataSaveManager.StoreCharacterSaveValue(saveDataList[i], characterId.ToString());
                        break;
                    }
                }
            }
        }

        public void DeleteSaveValue (string fileName)
        {
            if (saveDataList != null && characterId != 0)
            {
                for (var i = 0; i < saveDataList.Length; i++)
                {
                    if (saveDataList[i] && saveDataList[i].Match(fileName))
                    {
                        DataSaveManager.DeleteCharacterSaveValue(saveDataList[i], characterId.ToString());
                        break;
                    }
                }
            }
        }

        public void ClearSaveValue (string fileName)
        {
            if (saveDataList != null && characterId != 0)
            {
                for (var i = 0; i < saveDataList.Length; i++)
                {
                    if (saveDataList[i] && saveDataList[i].Match(fileName))
                    {
                        DataSaveManager.ClearCharacterSaveValue(saveDataList[i], characterId.ToString());
                        break;
                    }
                }
            }
        }

        public virtual void FeedbackAttackDraw (AttackType attack, float attackSpeed = 1f)
        {
            muscle.StandbyRangedDraw(attack);
        }

        public virtual void FeedbackAttackFire (AttackType attack, float attackSpeed = 1f, float attackDuration = 0f)
        {
            if (attack is AttackType.MeleeNormal or AttackType.RangedNormal)
            {
                TriggerAttack(attack);
                FinishAttack(attack);
            }
            else if (attack is AttackType.SkillNormal)
            {
                TriggerAttack(attack);
            }
        }

        public virtual bool FeedbackAppear ()
        {
            return false;
        }

        public virtual void FeedbackStance () { }
        public virtual void FeedbackUnstance () { }
        public virtual void FeedbackWalkStart (Vector3 destination) { }
        public virtual void FeedbackAttackCancel () { }
        public virtual void FeedbackAttackIdle () { }
        public virtual void FeedbackReceiveAttack (AttackDamageData damageData) { }
        public virtual void FeedbackReceiveHeal (float healAmount) { }
        public virtual void FeedbackStatusEffectAdd (string statName) { }
        public virtual void FeedbackStatusEffectRemove (string statName) { }
        public virtual void FeedbackKnock () { }
        public virtual void FeedbackKnockDone () { }
        public virtual void FeedbackInterrupt () { }
        public virtual void FeedbackFlee () { }
        public virtual void FeedbackDie () { }
        public virtual void FeedbackDead () { }
        public virtual void FeedbackShow () { }
        public virtual void FeedbackHide () { }
        public virtual void FeedbackChangeModel (GameObject original, GameObject latest) { }

        public bool HaveGraphTrigger (TriggerNode.Type type)
        {
            if (behaviourList != null)
                for (var i = 0; i < behaviourList.Length; i++)
                    if (behaviourList[i] && behaviourList[i].DetectCharacterTrigger(type))
                        return true;
            return false;
        }

        public void FeedbackGraphTrigger (TriggerNode.Type type, AttackDamageData damageData = null, string actionName = null, CharacterBrain addonBrain = null)
        {
            if (behaviourList != null)
                for (var i = 0; i < behaviourList.Length; i++)
                    if (behaviourList[i])
                        behaviourList[i].TriggerCharacter(type, brain, damageData, actionName, addonBrain);
        }

        public void FinishAttack (AttackType attack)
        {
            if (muscle)
                muscle.CooldownAttack(attack);
        }

        public void FinishAppear ()
        {
            if (muscle)
                muscle.ActivateMuscle();
        }

        public void TriggerAttack (AttackType attack)
        {
            if (attack == AttackType.MeleeNormal)
            {
                if (muscle)
                {
                    var damageData = muscle.SendAttackPack(attack);
                    if (damageData != null)
                    {
                        CompleteAttack(AttackType.MeleeNormal, damageData);
                        damageData.Terminate();
                    }
                }
            }
            else if (attack == AttackType.RangedNormal)
            {
                if (muscle)
                    muscle.SendAttackPack(attack);
            }
            else if (attack == AttackType.SkillNormal)
            {
                if (muscle != null)
                {
                    if (muscle != null)
                        muscle.SendAttackPack(attack);
                }
            }
        }

        public void StandbyAttack (AttackType attack)
        {
            muscle.StandbyRangedDraw(attack);
        }

        public void Terminate ()
        {
            prepareDestroy = 1;
        }

        private bool IsReadyForTerminate ()
        {
            if (muscle != null && muscle.GetAttackPackCount() > 0)
                return false;
            return true;
        }

        protected virtual int[] GetCharacterLayers ()
        {
            return Array.Empty<int>();
        }

        protected virtual int[] GetGroundLayers ()
        {
            return Array.Empty<int>();
        }

        protected virtual bool IsReadyForDestroy ()
        {
            return true;
        }

        public virtual Transform GetRangedMuzzle ()
        {
            return null;
        }

        public virtual Transform GetRangedHitPoint ()
        {
            return agentTransform;
        }

        public virtual Vector3 GetInteractPoint (Transform comer, float distance = 0)
        {
            if (!comer && agentTransform)
                return agentTransform.position;
            var position = Vector3.zero;
            if (comer && agentTransform)
            {
                if (distance <= 0 && motor)
                    distance = motor.GetAgentInteractDistance();
                position = Vector3.MoveTowards(agentTransform.position, comer.position, distance);
            }

            return position;
        }
        
        public bool IsCurrentInterestWith (CharacterOperator co)
        {
            return currentInteractWith == co;
        }

        public virtual void CompleteAttack (AttackType attack, AttackDamageData damageData)
        {
            muscle.EndAttack(attack, damageData);
        }

        public bool IsGoMeleeToward (CharacterOperator unit)
        {
            return muscle.IsGoMeleeTowardSameTarget(unit);
        }

        public void ObtainHealPack (string id, float value, bool visible)
        {
            if (value > 0)
            {
                var previousValue = brain.healVisible;
                brain.healVisible = visible;
                brain.AddStatMod(id, StatType.STAT_CURRENT_HEALTH, value);
                brain.healVisible = previousValue;
            }

            FeedbackStatusEffectAdd(StatType.STAT_CURRENT_HEALTH);
        }

        public void ObtainAttackPack (AttackDamageData damageData, bool missed = false)
        {
            if (missed)
            {
                damageData.SetMissed();
            }
            else
            {
                damageData.Calculate(this);
            }

            if (damageData.isMissed) { }
            else if (damageData.isDodged)
            {
                AdmitGetDodge(damageData);
            }
            else if (damageData.isBlocked)
            {
                AdmitGetBlock(damageData);
            }
            else
            {
                var damage = damageData.GetDamage();
                var hurt = GetHurt(damage);
                damageData.SetImpairedHurt(hurt);
                damageData.SetImpairedDamage(damage);
                damageData.DetectTargetDead();
                damageData.DetectBackstabAttack();
                damageData.DetectParryAttack();
            }

            ReceiveAttackPack(damageData);

            muscle.ReactAttack(damageData);
        }

        protected void ReceiveAttackPack (AttackDamageData damageData)
        {
            FeedbackReceiveAttack(damageData);
            FeedbackGraphTrigger(TriggerNode.Type.CharacterGetAttack, damageData);

            if (damageData.isDeadAtAttack)
            {
                FeedbackDie();
                FeedbackGraphTrigger(TriggerNode.Type.CharacterDead, damageData);
            }
        }

        public void LetGoAttackPack (AttackDamageData damageData)
        {
            muscle.RemoveAttackPack(damageData);
        }

        public void AdmitAttackKill (AttackDamageData damageData)
        {
            FeedbackGraphTrigger(TriggerNode.Type.CharacterKill, damageData);
        }

        public void AdmitAttackBackstab (AttackDamageData damageData)
        {
            ConsumeStamina(brain.GetStatStaminaConsume(Stamina.Type.BackstabAttack));
            FeedbackGraphTrigger(TriggerNode.Type.CharacterAttackBackstab, damageData);
        }

        public void AdmitGetBackstab (AttackDamageData damageData)
        {
            ConsumeStamina(brain.GetStatStaminaConsume(Stamina.Type.GetBackstab));
            FeedbackGraphTrigger(TriggerNode.Type.CharacterGetBackstab, damageData);
        }

        public void AdmitGetInterrupt ()
        {
            FeedbackInterrupt();
            FeedbackGraphTrigger(TriggerNode.Type.CharacterGetInterrupt);
        }

        public void AdmitParry (AttackDamageData damageData)
        {
            ConsumeStamina(brain.GetStatStaminaConsume(Stamina.Type.ParryAttack));
        }

        public void AdmitGetParry (AttackDamageData damageData) { }

        public void AdmitFriendlyLoss (AttackDamageData damageData)
        {
            FeedbackGraphTrigger(TriggerNode.Type.CharacterFriendDead, damageData);
        }

        public void AdmitScanVicinity (int friendsCount, int enemiesCount)
        {
            if (HaveGraphTrigger(TriggerNode.Type.CharacterScanVicinity))
            {
                var attackData = new AttackDamageData();
                attackData.Init(this, AttackType.None, null, false);
                attackData.SetImpairedDamage(friendsCount - enemiesCount);
                attackData.SetImpairedDamage(enemiesCount);
                FeedbackGraphTrigger(TriggerNode.Type.CharacterScanVicinity, attackData);
                attackData.Terminate();
            }
        }

        public void AdmitCompleteStance ()
        {
            FeedbackGraphTrigger(TriggerNode.Type.CharacterStanceDone);
        }

        public void AdmitCompleteUnstance ()
        {
            FeedbackGraphTrigger(TriggerNode.Type.CharacterUnstanceDone);
        }

        public void AdmitGetHurt (float hurtAmount)
        {
            ConsumeStamina(brain.GetStatStaminaConsume(Stamina.Type.GetHurt));
        }

        public void AdmitGetHeal (float healAmount)
        {
            if (healAmount > 0)
                FeedbackReceiveHeal(healAmount);
        }

        public void AdmitGetDodge (AttackDamageData pack)
        {
            ConsumeStamina(brain.GetStatStaminaConsume(Stamina.Type.DodgeAttack));
        }

        public void AdmitGetBlock (AttackDamageData pack)
        {
            ConsumeStamina(brain.GetStatStaminaConsume(Stamina.Type.BlockAttack));
        }

        public void AdmitStatChanged (string statName)
        {
            if (statGraph && statGraphChangeTriggers is {Length: > 0})
            {
                var haveBrainStatTrigger = false;
                for (var i = 0; i < statGraphChangeTriggers.Length; i++)
                {
                    if (statGraphChangeTriggers[i].Equals(statName, StringComparison.InvariantCulture))
                    {
                        haveBrainStatTrigger = true;
                        break;
                    }
                }

                if (haveBrainStatTrigger)
                    statGraph.CacheExecute(statGraph.TriggerBrainStat(TriggerNode.Type.BrainStatChange, brain, statName));
            }

            FeedbackGraphTrigger(TriggerNode.Type.BrainStatChange, actionName: statName);
        }

        public void AdmitAttackSkill ()
        {
            ConsumeStamina(muscle.GetSkillStaminaConsume());
            FeedbackAttackFire(AttackType.SkillNormal);
            FeedbackGraphTrigger(TriggerNode.Type.CharacterAttackSkill);
        }

        public void AdmitApproachingMeleeAttack () { }

        public void AdmitFlee ()
        {
            AdmitCompleteUnstance();
            FeedbackFlee();
        }

        public void AdmitCompleteFlee () { }

        public void AdmitAppear ()
        {
            var gotFeedback = FeedbackAppear();
            if (!gotFeedback)
                muscle.ActivateMuscle();
        }

        public void AdmitStance ()
        {
            FeedbackStance();
        }

        public void AdmitUnstance ()
        {
            FeedbackUnstance();
        }

        public void AdmitAttackIdle ()
        {
            FeedbackAttackIdle();
        }

        public void AdmitAttackCancel ()
        {
            FeedbackAttackCancel();
        }

        public void AdmitAttackMelee ()
        {
            ConsumeStamina(brain.GetStatStaminaConsume(Stamina.Type.MeleeAttack));
            FeedbackAttackFire(AttackType.MeleeNormal, meleeAttackSpeed, meleeAttackDuration);
            FeedbackGraphTrigger(TriggerNode.Type.CharacterAttackFire);
        }

        public void AdmitAttackRangedDraw ()
        {
            FeedbackAttackDraw(AttackType.RangedNormal, rangedAttackSpeed);
        }

        public void AdmitAttackRanged ()
        {
            FeedbackAttackFire(AttackType.RangedNormal, rangedAttackSpeed);
            FeedbackGraphTrigger(TriggerNode.Type.CharacterAttackFire);
        }

        public void AdmitAttackRangedFire ()
        {
            ConsumeStamina(brain.GetStatStaminaConsume(Stamina.Type.RangedAttack));
        }

        public virtual List<CharacterOperator> GetFriendlyUnit (float range = 0)
        {
            return new List<CharacterOperator>();
        }

        public virtual List<CharacterOperator> GetHostileUnit (float range = 0)
        {
            return new List<CharacterOperator>();
        }

        public List<CharacterOperator> GetAdjacentUnit (MultiTag unitFlag, float range, float angle)
        {
            tempList.Clear();
            if (characters != null)
            {
                for (var i = 0; i < characters.Count; i++)
                {
                    if (characters[i] && characters[i].inited && !characters[i].die && characters[i] != this)
                    {
                        var passed = false;
                        if (range <= 0f)
                            passed = true;
                        else
                        {
                            var distance = Vector3.Distance(agentTransform.position, characters[i].agentTransform.position);
                            if (distance <= range)
                                passed = true;
                        }

                        if (passed)
                        {
                            passed = false;
                            if (angle <= 0f)
                                passed = true;
                            else
                            {
                                var facing = motor.GetFacingAngle(characters[i]);
                                if (facing >= 90 - (angle / 2) && facing <= 90 + (angle / 2))
                                    passed = true;
                            }
                        }

                        if (passed)
                        {
                            passed = false;
                            if (unitFlag == null || unitFlag == 0)
                                passed = true;
                            else
                            {
                                var result = characters[i].flags.ContainAll(unitFlag);
                                if (result)
                                    passed = true;
                            }
                        }

                        if (passed)
                            tempList.Add(characters[i]);
                    }
                }
            }

            return tempList;
        }

        public bool IsInterruptiblePendingAction ()
        {
            return pendingAction.priority >= ActionPriority.Medium;
        }

        public bool IsPendingUrgentAction ()
        {
            return pendingAction.priority == ActionPriority.Urgent;
        }

        public bool IsPendingLaunchSkillAction ()
        {
            return pendingAction.action == PendingInterrupt.ActionType.LaunchSkill;
        }

        public bool IsPendingKnockAction ()
        {
            return pendingAction.action == PendingInterrupt.ActionType.Knock;
        }

        public bool ExecutePendingAction ()
        {
            if (pendingAction == null)
                return false;
            if (pendingAction.action != PendingInterrupt.ActionType.Knock && pendingAction.action != PendingInterrupt.ActionType.Flee && pendingAction.action != PendingInterrupt.ActionType.Interrupt)
                if (underOutOfControl)
                    return false;
            if (pendingAction.action == PendingInterrupt.ActionType.Attack)
            {
                currentActionPriority = pendingAction.priority;
                pendingAction.action = PendingInterrupt.ActionType.None;
                pendingAction.priority = ActionPriority.None;
                if (muscle.InterruptForceAttack(pendingAction.target))
                {
                    SetInteractTarget(null);
                    motor.CancelMove();
                    return true;
                }
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.Interact)
            {
                currentActionPriority = pendingAction.priority;
                pendingAction.action = PendingInterrupt.ActionType.None;
                pendingAction.priority = ActionPriority.None;
                SetInteractTarget(pendingAction.target);
                muscle.InterruptForceMoveAway();
                motor.CancelWander();
                var interactPoint = interactTarget.GetInteractPoint(agentTransform);
                if (IsWithinInteractRange(interactPoint))
                {
                    LaunchInteract();
                }
                else
                {
                    var sameLoc = !motor.SetDestination(interactPoint);
                    if (sameLoc && !isMoving)
                    {
                        motor.ResetMove();
                        interactTarget = null;
                    }
                }

                return true;
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.Move)
            {
                currentActionPriority = pendingAction.priority;
                pendingAction.action = PendingInterrupt.ActionType.None;
                pendingAction.priority = ActionPriority.None;
                SetInteractTarget(null);
                muscle.InterruptForceMoveAway();
                motor.CancelWander();
                motor.SetDestination(pendingAction.destination, pendingAction.executeId, pendingAction.layerName, pendingAction.check, pendingAction.range);
                FeedbackWalkStart(pendingAction.destination);
                return true;
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.CancelMove)
            {
                currentActionPriority = ActionPriority.None;
                pendingAction.action = PendingInterrupt.ActionType.None;
                pendingAction.priority = ActionPriority.None;
                SetInteractTarget(null);
                muscle.InterruptForceIdle();
                motor.CancelMove(true);
                motor.CancelWander();
                return true;
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.Teleport)
            {
                currentActionPriority = pendingAction.priority;
                pendingAction.action = PendingInterrupt.ActionType.None;
                pendingAction.priority = ActionPriority.None;
                SetInteractTarget(null);
                muscle.InterruptForceMoveAway();
                motor.CancelWander();
                motor.SetTeleport(pendingAction.destination, pendingAction.executeId);
                return true;
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.Wander)
            {
                currentActionPriority = pendingAction.priority;
                pendingAction.action = PendingInterrupt.ActionType.None;
                pendingAction.priority = ActionPriority.None;
                SetInteractTarget(null);
                if (pendingAction.range > 0)
                {
                    muscle.InterruptForceMoveAway();
                    motor.SetWander(pendingAction.range, pendingAction.check, pendingAction.executeId);
                }
                else
                {
                    motor.CancelWander();
                }

                return true;
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.Chase)
            {
                currentActionPriority = pendingAction.priority;
                pendingAction.action = PendingInterrupt.ActionType.None;
                pendingAction.priority = ActionPriority.None;
                SetInteractTarget(null);
                muscle.InterruptForceMoveAway();
                motor.SetChase(pendingAction.target, pendingAction.executeId);
                return true;
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.Stance)
            {
                currentActionPriority = pendingAction.priority;
                pendingAction.action = PendingInterrupt.ActionType.None;
                pendingAction.priority = ActionPriority.None;
                if (!underStance)
                {
                    if (motor.stopAtStance || !motor.IsAgentMoving())
                    {
                        SetInteractTarget(null);
                        motor.CancelMove();
                        muscle.InterruptStance();
                    }
                    else
                    {
                        motor.SetInstantWalkBackward(true);
                        muscle.ImmediateStance();
                    }
                }

                return true;
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.Unstance)
            {
                currentActionPriority = pendingAction.priority;
                pendingAction.action = PendingInterrupt.ActionType.None;
                pendingAction.priority = ActionPriority.None;
                if (underStance)
                {
                    if (motor.stopAtStance || !motor.IsAgentMoving())
                    {
                        SetInteractTarget(null);
                        motor.CancelMove();
                        muscle.InterruptUnstance();
                    }
                    else
                    {
                        motor.SetInstantWalkBackward(false);
                        muscle.ImmediateUnstance();
                    }
                }

                return true;
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.Knock)
            {
                if (muscle.currentTarget != null && pendingAction.check)
                {
                    (currentActionPriority, pendingAction.priority) = (pendingAction.priority, currentActionPriority);
                    pendingAction.action = PendingInterrupt.ActionType.Attack;
                    pendingAction.target = muscle.currentTarget;
                }
                else if (interactTarget != null && pendingAction.check)
                {
                    (currentActionPriority, pendingAction.priority) = (pendingAction.priority, currentActionPriority);
                    pendingAction.action = PendingInterrupt.ActionType.Interact;
                    pendingAction.target = interactTarget;
                }
                else
                {
                    currentActionPriority = pendingAction.priority;
                    pendingAction.action = PendingInterrupt.ActionType.None;
                    pendingAction.priority = ActionPriority.None;
                }

                SetInteractTarget(null);
                muscle.InterruptForcePush();
                motor.SetPush(pendingAction.destination, pendingAction.range, pendingAction.executeId);
                FeedbackKnock();
                return true;
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.Interrupt)
            {
                var interrupted = muscle.InterruptForceStop();
                pendingAction.action = PendingInterrupt.ActionType.None;
                pendingAction.priority = ActionPriority.None;
                if (interrupted)
                {
                    currentActionPriority = ActionPriority.None;
                    AdmitGetInterrupt();
                    return true;
                }
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.Flee)
            {
                if (pendingAction.check)
                {
                    var execute = false;
                    var fleeLocation = Vector3.zero;
                    if (asGuardian)
                    {
                        if (IsWithinGuardLocation(agentTransform.position))
                        {
                            if (muscle.GetFleePoint(pendingAction.range, pendingAction.destination.y > 0f ? 2 : 3, out var trans))
                            {
                                fleeLocation = trans.position;
                                execute = true;
                            }
                        }
                        else
                        {
                            fleeLocation = brain.guardLocation;
                            execute = true;
                        }
                    }
                    else if (muscle.GetFleePoint(pendingAction.range, pendingAction.destination.y > 0f ? 2 : 3, out var trans))
                    {
                        fleeLocation = trans.position;
                        execute = true;
                    }

                    if (execute)
                    {
                        currentActionPriority = pendingAction.priority;
                        pendingAction.action = PendingInterrupt.ActionType.None;
                        pendingAction.priority = ActionPriority.None;
                        SetInteractTarget(null);
                        muscle.InterruptFlee();
                        motor.SetFlee(fleeLocation, pendingAction.executeId);
                        if (pendingAction.destination.x > 0f)
                            muscle.RememberAttackTarget();
                        return true;
                    }
                }
                else
                {
                    currentActionPriority = ActionPriority.None;
                    pendingAction.action = PendingInterrupt.ActionType.None;
                    pendingAction.priority = ActionPriority.None;
                    if (muscle.underFlee)
                    {
                        muscle.InterruptCancelFlee();
                        motor.CancelMove();
                        return true;
                    }
                }
            }
            else if (pendingAction.action == PendingInterrupt.ActionType.LaunchSkill)
            {
                currentActionPriority = pendingAction.priority;
                pendingAction.action = PendingInterrupt.ActionType.None;
                pendingAction.priority = ActionPriority.None;
                var skillMuscle = muscle.FindSkill(pendingAction.skillPack);
                if (skillMuscle != null)
                {
                    var attackDamageData = new AttackDamageData();
                    attackDamageData.Init(this, AttackType.SkillNormal, null, false);
                    attackDamageData.SetTarget(muscle.currentTarget);
                    var attackSkillData = new AttackSkillData();
                    attackSkillData.Init(this, skillMuscle, attackDamageData);
                    ExecutePendingSkillAction(attackSkillData);
                    return true;
                }
            }

            return false;
        }

        public void ExecutePendingSkillAction (AttackSkillData attackSkillData)
        {
            SetInteractTarget(null);
            motor.CancelMove();
            motor.ResetMove();
            muscle.InterruptForceSkillAttack(attackSkillData);
        }

        protected void HurtTen ()
        {
            Hurt(maxHealth * 0.1f);
        }

        protected void Kill ()
        {
            Hurt(maxHealth);
        }

        private void Hurt (float value)
        {
            GetHurt(value);
            var damageData = new AttackDamageData();
            damageData.SetTarget(this);
            damageData.SetImpairedDamage(value);
            damageData.DetectTargetDead();
            damageData.DetectBackstabAttack();
            damageData.DetectParryAttack();
            ReceiveAttackPack(damageData);
            muscle.ReactAttack(damageData);
        }

        private float GetHurt (float value)
        {
            return brain.ModifyHealthStat(-value) * -1;
        }

        public List<AttackSkillMuscle> GetActiveToggleSkillsData ()
        {
            return muscle.GetActiveToggleSkills();
        }

        public AttackSkillMuscle GetActiveSkillData ()
        {
            return muscle.GetActiveSkill();
        }

        public AttackSkillMuscle GetToggleSkillData ()
        {
            return muscle.GetToggleSkill();
        }

        public void ConsumeStamina (float value)
        {
            brain.DeductStamina(value);
        }

        public float GetStaminaConsume (Stamina.Type type)
        {
            return brain.GetStatStaminaConsume(type);
        }

        public int GetFogTeam ()
        {
            return fogOfWarAgent ? fogOfWarAgent.GetTeam() : -1;
        }

        public FogOfWarAgent GetFogUnit ()
        {
            return fogOfWarAgent;
        }

        public bool IsVisibleInFogTeam (int team)
        {
            return !fogOfWarAgent || fogOfWarAgent.IsVisibleAtTeam(team);
        }

        public bool IsVisibleInFogUnit (FogOfWarAgent agent)
        {
            if (agent == null) return true;
            return !fogOfWarAgent || fogOfWarAgent.IsVisibleAtUnit(agent, agentSize, GetCharacterLayers(), GetGroundLayers());
        }

        public virtual List<CharacterOperator> GetViewVisibleTargets ()
        {
            return null;
        }

        public string GetInventoryName (int tagValue)
        {
            tempInvTags.value = tagValue;
            return GetInventoryName(tempInvTags);
        }

        public string GetInventoryName (MultiTag tags)
        {
            for (var i = 0; i < inventoryList.Length; i++)
            {
                var inv = InventoryManager.GetInventory(inventoryList[i].Name);
                if (inv != null)
                {
                    bool ifNoTag = tags == 0;
                    if (inv.HaveTag(tags, ifNoTag))
                        return inventoryList[i].Name;
                }
            }

            return string.Empty;
        }

        public string[] GetInventoryNameList ()
        {
            var list = new string[inventoryList.Length];
            for (var i = 0; i < inventoryList.Length; i++)
                list[i] = inventoryList[i].Name;
            return list;
        }

        public InventoryBehaviour[] GetInventoryList ()
        {
            return inventoryList;
        }

        private bool HaveInventory (string invName)
        {
            for (var i = 0; i < inventoryList.Length; i++)
            {
                if (invName == inventoryList[i].Name)
                {
                    return true;
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryList[i].Name);
                    for (var j = 0; j < inv.Count; j++)
                    {
                        var item = inv.GetItem(j);
                        if (item != null)
                        {
                            if (item.isSolid && item.InstanceId == invName)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        public virtual string GetDisplayName ()
        {
            return string.Empty;
        }
        
        public virtual Sprite GetDisplayAvatar ()
        {
            return null;
        }

        public void PrintLog (string message)
        {
            if (printLog)
            {
                ReDebug.Log("Character Operator from " + goName, message);
            }
        }

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        public virtual void Initialize (CharacterBrain charBrain, CharacterMuscle charMuscle, CharacterMotor charMotor, bool appearState, int sId)
        {
            brain = null;
            muscle = null;
            paused = false;
            statEmptiness = charBrain == null;
            brain = charBrain != null ? charBrain.Clone() : ScriptableObject.CreateInstance<CharacterBrain>();
            muscle = charMuscle != null ? Instantiate(charMuscle) : ScriptableObject.CreateInstance<CharacterMuscle>();
            motor = charMotor != null ? Instantiate(charMotor) : ScriptableObject.CreateInstance<CharacterMotor>();

            if (statGraph != null)
            {
                var getNodes = new List<string>();
                var changeNodes = new List<string>();
                var triggers = Graph.GetChildren(statGraph.graph.RootNode);
                for (var i = 0; i < triggers.Count; i++)
                {
                    if (triggers[i] is BrainStatTriggerNode node)
                    {
                        if (!string.IsNullOrEmpty(node.statType))
                        {
                            if (node.triggerType == TriggerNode.Type.BrainStatGet)
                                getNodes.Add(node.statType);
                            else if (node.triggerType == TriggerNode.Type.BrainStatChange)
                                changeNodes.Add(node.statType);
                        }
                    }
                }

                statGraphGetTriggers = getNodes.ToArray();
                statGraphChangeTriggers = changeNodes.ToArray();
            }

            brain.Init(this, muscle, motor);
            muscle.Init(this, brain, motor, appearState == false);
            motor.Init(this, brain, muscle, motorAgent);
            customPhase = string.Empty;

            if (inventoryList != null)
            {
                for (var i = 0; i < inventoryList.Length; i++)
                {
                    if (inventoryList[i].isSlotInApplyStatus || inventoryList[i].isSlotInApplySkill)
                    {
                        var inv = InventoryManager.GetInventory(inventoryList[i].Name);
                        if (inv != null)
                        {
                            inv.OnDecayChange += OnInventoryDecayChange;
                        }
                    }
                }
            }

            if (appearState)
                AdmitAppear();
            if (sId != 0)
                spawnerId = sId;
        }

        protected virtual void Uninitialize ()
        {
            Destroy(brain);
            Destroy(muscle);
            Destroy(motor);
            brain = null;
            muscle = null;
            motor = null;
        }

        protected virtual void Pause ()
        {
            if (!HaveInited()) return;
            paused = true;
            motor.PauseMove();
        }

        protected virtual void Unpause ()
        {
            if (!HaveInited()) return;
            paused = false;
            motor.UnpauseMove();
        }

        public void SetActionToggleSkill (AttackSkillMuscle skill, bool toggle)
        {
            muscle.ToggleSkill(skill, toggle);
        }

        protected void SetActionStance ()
        {
            pendingAction.action = PendingInterrupt.ActionType.Stance;
            pendingAction.priority = ActionPriority.Medium;
        }

        protected void SetActionUnstance ()
        {
            pendingAction.action = PendingInterrupt.ActionType.Unstance;
            pendingAction.priority = ActionPriority.Medium;
        }

        public void SetActionAttackTarget (CharacterOperator target)
        {
            FinishInteract();
            pendingAction.action = PendingInterrupt.ActionType.Attack;
            pendingAction.priority = ActionPriority.Medium;
            pendingAction.target = target;
        }

        public void SetActionInteractTarget (CharacterOperator target)
        {
            FinishInteract();
            pendingAction.action = PendingInterrupt.ActionType.Interact;
            pendingAction.priority = ActionPriority.Medium;
            pendingAction.target = target;
        }

        public void SetActionLaunchActiveSkill (AttackSkillMuscle skillMuscle)
        {
            FinishInteract();
            pendingAction.action = PendingInterrupt.ActionType.LaunchSkill;
            pendingAction.priority = ActionPriority.Low;
            pendingAction.skillPack = skillMuscle.pack;
        }

        public void SetActionMoveDestination (Vector3 dest, string executeId = "", string layerName = "", ActionPriority priority = ActionPriority.Medium, bool walkBackward = false,
            float endFacing = float.PositiveInfinity)
        {
            if (underOutOfMove) return;
            FinishInteract();
            pendingAction.action = PendingInterrupt.ActionType.Move;
            pendingAction.priority = priority;
            pendingAction.destination = dest;
            pendingAction.executeId = executeId;
            pendingAction.layerName = layerName;
            pendingAction.check = walkBackward;
            pendingAction.range = endFacing;
        }

        public void SetActionTeleport (Vector3 dest, string executeId = "", ActionPriority priority = ActionPriority.Medium)
        {
            FinishInteract();
            pendingAction.action = PendingInterrupt.ActionType.Teleport;
            pendingAction.priority = priority;
            pendingAction.destination = dest;
            pendingAction.executeId = executeId;
        }

        public void SetActionWander (float range, bool relocate, string executeId = "")
        {
            FinishInteract();
            pendingAction.action = PendingInterrupt.ActionType.Wander;
            pendingAction.priority = ActionPriority.Medium;
            pendingAction.range = range;
            pendingAction.check = relocate;
            pendingAction.executeId = executeId;
        }

        public void SetActionChase (CharacterOperator target, string executeId = "")
        {
            FinishInteract();
            pendingAction.action = PendingInterrupt.ActionType.Chase;
            pendingAction.priority = ActionPriority.Medium;
            pendingAction.target = target;
            pendingAction.executeId = executeId;
        }

        public void SetActionKnock (Vector3 dest, float pushSpeed, bool continuousAction, string executeId = "")
        {
            FinishInteract();
            pendingAction.action = PendingInterrupt.ActionType.Knock;
            pendingAction.priority = ActionPriority.Urgent;
            pendingAction.executeId = executeId;
            pendingAction.destination = dest;
            pendingAction.range = pushSpeed;
            pendingAction.check = continuousAction;
        }

        public bool SetActionInterrupt (string executeId = "")
        {
            if (muscle.AllowForceStop())
            {
                pendingAction.action = PendingInterrupt.ActionType.Interrupt;
                pendingAction.priority = ActionPriority.Urgent;
                pendingAction.executeId = executeId;
                return true;
            }

            return false;
        }

        public void SetActionApplyFlee (float range, bool keepTarget, bool fromBehind, string executeId = "")
        {
            FinishInteract();
            pendingAction.action = PendingInterrupt.ActionType.Flee;
            pendingAction.priority = ActionPriority.Urgent;
            pendingAction.executeId = executeId;
            pendingAction.check = true;
            pendingAction.range = range;
            var state = pendingAction.destination;
            state.x = keepTarget ? 1f : 0f;
            state.y = fromBehind ? 1f : 0f;
            pendingAction.destination = state;
        }

        public void SetActionCancelFlee (string executeId = "")
        {
            pendingAction.action = PendingInterrupt.ActionType.Flee;
            pendingAction.priority = ActionPriority.Urgent;
            pendingAction.executeId = executeId;
            pendingAction.check = false;
        }

        public void SetActionCancelMove (string executeId = "")
        {
            pendingAction.action = PendingInterrupt.ActionType.CancelMove;
            pendingAction.priority = ActionPriority.Urgent;
            pendingAction.executeId = executeId;
        }

        public void SetBehaviourGuard (Vector3 position, float range, bool backGuardPoint, float backRange)
        {
            if (inited)
                brain.SetGuardian(position, range, backGuardPoint, backRange);
        }

        public void SetBehaviourSearchTarget (bool idle, bool stanceIdle)
        {
            if (inited)
                brain.SetSearchTarget(idle, stanceIdle);
        }

        public void CancelBehaviourGuard ()
        {
            if (inited)
                brain.CancelGuardian();
        }

        public bool IsWithinGuardLocation (Vector3 pos)
        {
            return inited && brain.IsWithinGuardian(pos);
        }

        public bool HaveReachWanderDestination (string executionId)
        {
            return inited && motor.IsReachExecutionDestination(executionId);
        }

        public bool IsStayBehindTarget (CharacterOperator target)
        {
            return IsStayBehindTarget(target.agentTransform.position, target.agentTransform.rotation);
        }

        public bool IsStayBehindTarget (Vector3 targetPos, Quaternion targetRot)
        {
            return inited && motor.IsBehindLocation(targetPos, targetRot, out _, out _, out _);
        }

        public void ChangeBrainBehaviours (CharacterBrain paramBrain, MultiTag changeFlags)
        {
            if (inited)
                brain.ChangeBehaviours(paramBrain, changeFlags);
        }

        public void ChangeMuscleBehaviours (CharacterMuscle paramMuscle, MultiTag changeFlags)
        {
            if (inited)
                muscle.ChangeBehaviours(paramMuscle, changeFlags);
        }

        protected virtual void OnReachDestination ()
        {
            if (!underStance || motor.interactAtStance)
            {
                if (interactTarget != null)
                {
                    var interactPoint = interactTarget.GetInteractPoint(agentTransform);
                    if (IsWithinInteractRange(interactPoint))
                    {
                        LaunchInteract();
                    }
                    else
                    {
                        SetInteractTarget(null);
                        motor.ResetMove();
                    }
                }
            }
            else
            {
                SetInteractTarget(null);
            }

            var pushDead = false;
            if (muscle.underPush)
            {
                FeedbackKnockDone();
                if (die)
                    pushDead = true;
            }

            currentActionPriority = ActionPriority.None;
            motor.ReachDestination();

            if (pushDead)
                motor.StopMove();
        }

        protected void OnInventoryDecayChange (string itemId, int before, int after)
        {
            if (before == 0 && after > 0)
            {
                var manager = GraphManager.instance.runtimeSettings.itemManager;
                var itemData = manager.GetItemData(itemId);
                if (itemData != null)
                {
                    if (!itemData.isEmptyStatus)
                    {
                        for (var i = 0; i < itemData.attackStatusPack.Length; i++)
                        {
                            var status = itemData.attackStatusPack[i];
                            if (status != null)
                                AddAttackStatus(status, this);
                        }
                    }

                    if (!itemData.isEmptySkill)
                    {
                        for (var i = 0; i < itemData.attackSkillPack.Length; i++)
                        {
                            var skill = itemData.attackSkillPack[i];
                            if (skill != null)
                                SetAttackSkill(skill, true);
                        }
                    }
                }
            }
            else if (before > 0 && after == 0)
            {
                var manager = GraphManager.instance.runtimeSettings.itemManager;
                var itemData = manager.GetItemData(itemId);
                if (itemData != null)
                {
                    if (!itemData.isEmptyStatus)
                    {
                        if (!itemData.isEmptyStatus)
                        {
                            for (var i = 0; i < itemData.attackStatusPack.Length; i++)
                            {
                                var status = itemData.attackStatusPack[i];
                                if (status != null)
                                    RemoveAttackStatus(status);
                            }
                        }
                    }

                    if (!itemData.isEmptySkill)
                    {
                        for (var i = 0; i < itemData.attackSkillPack.Length; i++)
                        {
                            var skill = itemData.attackSkillPack[i];
                            if (skill != null)
                                SetAttackSkill(skill, false);
                        }
                    }
                }
            }
        }

        public AttackStatusData AddAttackStatus (AttackStatusPack attackStatus, CharacterOperator applier)
        {
            var haveIndex = -1;
            for (var i = 0; i < affectedAttackStatus.Count; i++)
            {
                if (affectedAttackStatus[i].IsSamePack(attackStatus))
                {
                    haveIndex = i;
                    break;
                }
            }

            if (haveIndex < 0)
                return AddAttackStatusData();
            if (attackStatus.stackable)
                return AddAttackStatusData();
            if (attackStatus.replaceable)
            {
                RemoveAttackStatusData(haveIndex);
                return AddAttackStatusData();
            }

            affectedAttackStatus[haveIndex].IncreaseApply();
            return null;

            AttackStatusData AddAttackStatusData ()
            {
                var attackStatusData = new AttackStatusData();
                attackStatusData.Init(this, attackStatus, applier);
                affectedAttackStatus.Add(attackStatusData);
                attackStatusData.TriggerBegin();
                return attackStatusData;
            }
        }

        public AttackStatusData RemoveAttackStatus (AttackStatusPack attackStatus)
        {
            var haveIndex = -1;
            for (var i = 0; i < affectedAttackStatus.Count; i++)
            {
                if (affectedAttackStatus[i].IsSamePack(attackStatus))
                {
                    haveIndex = i;
                    break;
                }
            }

            if (haveIndex >= 0)
            {
                if (affectedAttackStatus[haveIndex].isMultipleApplied)
                    affectedAttackStatus[haveIndex].DecreaseApply();
                else
                    return RemoveAttackStatusData(haveIndex);
            }

            return null;
        }

        public bool HaveAttackStatus (AttackStatusPack attackStatus)
        {
            var haveIndex = -1;
            for (var i = 0; i < affectedAttackStatus.Count; i++)
            {
                if (affectedAttackStatus[i].IsSamePack(attackStatus))
                {
                    haveIndex = i;
                    break;
                }
            }

            return haveIndex >= 0;
        }

        public void AddAllInventoryStatus ()
        {
            for (var j = 0; j < inventoryList.Length; j++)
            {
                if (inventoryList[j].isSlotInApplyStatus)
                    InventoryManager.ApplyCharacterAttackStatus(this, inventoryList[j].Name);
            }
        }

        public void LearnAllInventorySkill ()
        {
            for (var j = 0; j < inventoryList.Length; j++)
            {
                if (inventoryList[j].isSlotInApplySkill)
                    InventoryManager.ApplyCharacterAttackSkill(this, inventoryList[j].Name);
            }
        }

        public void SetAttackSkill (AttackSkillPack attackSkill, bool add)
        {
            if (add)
                muscle.LearnSkill(attackSkill);
            else
                muscle.GiveUpSkill(attackSkill);
        }

        public void UpdateStatusEffect (string statName)
        {
            brain.RefreshLiveStatValue(statName);
            AdmitStatChanged(statName);
            FeedbackStatusEffectAdd(statName);
        }

        public void AddStatusEffect (string id, string statName, float value, float mod, float mtp, float mag)
        {
            if (brain.IsLiveStatValue(statName))
            {
                if (mod != 0)
                    brain.AddStatMod(id, statName, mod);
            }
            else
            {
                if (value != 0)
                    brain.AddStatValue(id, statName, value);
                if (mod != 0)
                    brain.AddStatMod(id, statName, mod);
                if (mtp != 0)
                    brain.AddStatMtp(id, statName, mtp);
                if (mag != 0)
                    brain.AddStatMag(id, statName, mag);
            }

            FeedbackStatusEffectAdd(statName);
        }

        public void RemoveStatusEffect (string id, string statName, float value, float mod, float mtp, float mag)
        {
            if (value != 0)
                brain.RemoveStatValue(id, statName, value);
            if (mod != 0)
                brain.RemoveStatMod(id, statName, mod);
            if (mtp != 0)
                brain.RemoveStatMtp(id, statName, mtp);
            if (mag != 0)
                brain.RemoveStatMag(id, statName, mag);
            FeedbackStatusEffectRemove(statName);
        }

        internal float GetStatValue (string statName)
        {
            GetStatValue(statName, out var value);
            return value;
        }

        internal float GetStatValue (string statName, CharacterOperator character)
        {
            statValueCharacterParam = character;
            GetStatValue(statName, out var value);
            return value;
        }

        internal float GetStatCalcValue (string statName)
        {
            GetStatValue(statName, out var value);
            return value;
        }

        protected bool GetStatValue (string statName, out float value)
        {
            var found = false;
            value = 0;
            if (brain && !statEmptiness)
            {
                if (statName is StatType.STAT_DISTANCE or StatType.STAT_ANGLE or StatType.STAT_UNIT_FOV or StatType.STAT_TEAM_FOV)
                {
                    if (motor.GetStatValue(statName, out var result))
                    {
                        value = result;
                        found = true;
                    }
                }
                else if (statName is StatType.STAT_MELEE_ATTACK_DISTANCE or StatType.STAT_RANGED_ATTACK_REACHABLE)
                {
                    if (muscle.GetStatValue(statName, out var result))
                    {
                        value = result;
                        found = true;
                    }
                }
                else
                {
                    if (!brain.IsLiveStatValue(statName))
                    {
                        if (GetStatGraphValue(statName, out var graphValue))
                        {
                            value = graphValue;
                            found = true;
                        }
                    }

                    if (!found && brain.GetStatValue(statName, out var result))
                    {
                        value = result;
                        found = true;
                    }
                }
            }

            if (GetStatExternalValue(statName, value, out var externalValue))
            {
                value = externalValue;
                found = true;
            }

            return found;
        }

        protected virtual bool GetStatExternalValue (string statName, float value, out float externalValue)
        {
            externalValue = 0;
            return false;
        }

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected virtual void Awake ()
        {
            tempList ??= new List<CharacterOperator>();
            characters ??= new List<CharacterOperator>();
            characters.Add(this);
            pendingAction = new PendingInterrupt();
            affectedAttackStatus ??= new List<AttackStatusData>();
        }

        protected virtual void OnDestroy ()
        {
            Uninitialize();
            characters?.Remove(this);
        }

        protected virtual void Update ()
        {
            //-- TODO Control update timing, eg: 30 updates per seconds

            if (!paused)
            {
                if (brain)
                    brain.Tick(ReTime.deltaTime);
                if (muscle)
                    muscle.Tick(ReTime.deltaTime);
                if (motor)
                    motor.Tick(ReTime.deltaTime);
                if (affectedAttackStatus != null)
                    for (var i = 0; i < affectedAttackStatus.Count; i++)
                        affectedAttackStatus[i].Tick(ReTime.deltaTime);
            }
        }

        protected void LateUpdate ()
        {
            if (prepareDestroy == 1)
            {
                if (IsReadyForTerminate())
                {
                    FeedbackDead();
                    FeedbackGraphTrigger(TriggerNode.Type.CharacterTerminate);
                    prepareDestroy = 2;
                }
            }
            else if (prepareDestroy == 2)
            {
                if (IsReadyForDestroy())
                    Destroy(gameObject);
                else
                    gameObject.SetActiveOpt(false);
            }
        }

        protected virtual void OnEnable ()
        {
            if (motorAgent != null)
            {
                motorAgent.onReachDestination += OnReachDestination;
            }
        }

        protected virtual void OnDisable ()
        {
            if (motorAgent != null)
            {
                motorAgent.onReachDestination -= OnReachDestination;
            }
        }

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void SetInteractTarget (CharacterOperator co)
        {
            if (interactTarget && !co)
                CancelInteract();
            interactTarget = co;
        }

        private void LaunchInteract ()
        {
            Face(interactTarget.agentTransform.position);
            if (interactTarget)
            {
                FeedbackGraphTrigger(TriggerNode.Type.InteractLaunch, addonBrain: interactTarget.brain);
                interactTarget.FeedbackGraphTrigger(TriggerNode.Type.InteractReceive, addonBrain: brain);
                currentInteractWith = interactTarget;
                interactTarget = null;
            }
        }

        private void CancelInteract ()
        {
            if (interactTarget)
            {
                FeedbackGraphTrigger(TriggerNode.Type.InteractCancel, addonBrain: interactTarget.brain);
                interactTarget.FeedbackGraphTrigger(TriggerNode.Type.InteractGiveUp, addonBrain: brain);
            }
        }

        private void FinishInteract ()
        {
            if (currentInteractWith)
            {
                FeedbackGraphTrigger(TriggerNode.Type.InteractFinish, addonBrain: currentInteractWith.brain);
                currentInteractWith.FeedbackGraphTrigger(TriggerNode.Type.InteractLeave, addonBrain: brain);
                currentInteractWith = null;
            }
        }

        private AttackStatusData RemoveAttackStatusData (int index)
        {
            var status = affectedAttackStatus[index];
            status.TriggerTerminate();
            affectedAttackStatus.Remove(status);
            status.Terminate();
            return status;
        }

        private bool GetStatGraphValue (string statName, out float value)
        {
            if (statGraph && statGraphGetTriggers is {Length: > 0})
            {
                var haveBrainStatTrigger = false;
                for (var i = 0; i < statGraphGetTriggers.Length; i++)
                {
                    if (statGraphGetTriggers[i].Equals(statName, StringComparison.InvariantCulture))
                    {
                        haveBrainStatTrigger = true;
                        break;
                    }
                }

                if (haveBrainStatTrigger)
                {
                    var executionResult = statGraph.TriggerBrainStat(TriggerNode.Type.BrainStatGet, brain, statName);
                    if (executionResult is {variables: { }})
                    {
                        var statValue = executionResult.variables.GetNumber(statName, float.PositiveInfinity, int.MaxValue, GraphVariables.PREFIX_RETURN);
                        statGraph.CacheExecute(executionResult);
                        if (!float.IsPositiveInfinity(statValue))
                        {
                            value = statValue;
                            return true;
                        }
                    }
                }
            }

            value = 0;
            return false;
        }

        private bool HaveInited ()
        {
            if (!inited)
                ReDebug.LogError($"{gameObject.name} contains Character Operator that not yet initialized. This might cause by setup it's spawning in not proper way.");
            return inited;
        }

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
#if UNITY_EDITOR
        private bool CheckSaveDataList ()
        {
            if (saveDataList is {Length: > 0} && characterId == 0)
                for (var i = 0; i < saveDataList.Length; i++)
                    if (saveDataList[i] != null)
                        return true;
            return false;
        }

        private bool IsShowStatGraphMismatchWarning ()
        {
            if (statGraph != null)
                return !statGraph.IsStatGraph();
            return false;
        }

        private bool DisplaySecretCore ()
        {
            if (Application.isPlaying)
                return true;
            return showCore;
        }

        [HideInInspector]
        public bool showCore = true;

        [MenuItem("CONTEXT/CharacterOperator/Core Display/Show", false)]
        public static void ShowCore (MenuCommand command)
        {
            var comp = (CharacterOperator) command.context;
            comp.showCore = true;
        }

        [MenuItem("CONTEXT/CharacterOperator/Core Display/Show", true)]
        public static bool IsShowCore (MenuCommand command)
        {
            var comp = (CharacterOperator) command.context;
            if (comp.showCore)
                return false;
            return true;
        }

        [MenuItem("CONTEXT/CharacterOperator/Core Display/Hide", false)]
        public static void HideCore (MenuCommand command)
        {
            var comp = (CharacterOperator) command.context;
            comp.showCore = false;
        }

        [MenuItem("CONTEXT/CharacterOperator/Core Display/Hide", true)]
        public static bool IsHideCore (MenuCommand command)
        {
            var comp = (CharacterOperator) command.context;
            if (!comp.showCore)
                return false;
            return true;
        }
#endif
    }
}