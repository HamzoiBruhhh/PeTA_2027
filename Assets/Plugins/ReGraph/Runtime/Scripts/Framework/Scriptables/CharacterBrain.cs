using System;
using System.Collections.Generic;
using Reshape.Unity;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Reshape.Unity.Editor;
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Character Brain", fileName = "CharacterBrain", order = 303)]
    [Serializable]
    [HideMonoScript]
    public class CharacterBrain : BaseScriptable
    {
        [SerializeField]
        [InlineButton("CreateNewCharacterStat", "✚")]
        private CharacterStat stat;

        [Hint("showHints", "Define stamina pack that use by the unit.")]
        [InlineButton("@StaminaPack.CreateNew(this)", "✚")]
        public StaminaPack staminaPack;

        [Hint("showHints", "Define morale pack that use by the unit.")]
        [InlineButton("@MoralePack.CreateNew(this)", "✚")]
        public MoralePack moralePack;

        [Hint("showHints", "Define target aim pack that use by the unit.")]
        [InlineButton("@TargetAimPack.CreateNew(this)", "✚")]
        public TargetAimPack targetAimPack;

        [Hint("showHints", "Define how often the unit search for target to attack in seconds.")]
        [ShowIf("@targetAimPack != null")]
        [BoxGroup("Search Target"), LabelText("Interval")]
        public float searchTargetInterval = 0.2f;

        [Hint("showHints", "Define the unit to search for target to attack during unstance idle.")]
        [ShowIf("@targetAimPack != null")]
        [BoxGroup("Search Target"), LabelText("During Idle")]
        public bool searchTargetAtIdle;

        [Hint("showHints", "Define the unit to search for target to attack during stance idle.")]
        [ShowIf("@targetAimPack != null")]
        [BoxGroup("Search Target"), LabelText("During Stance Idle")]
        public bool searchTargetAtStanceIdle;

        [Hint("showHints", "Define the detect range for morale vicinity affection.")]
        [InlineProperty]
        [BoxGroup("Scan Vicinity")]
        public FloatProperty vicinityRange;

        [Hint("showHints", "Define how frequent the detect of morale vicinity affection.")]
        [InlineProperty]
        [BoxGroup("Scan Vicinity")]
        public FloatProperty vicinityRate;

        [Hint("showHints", "Define the unit flag for friendly target.")]
        [BoxGroup("Scan Vicinity")]
        public MultiTag friendlyFlag = new MultiTag("Unit Flags", typeof(MultiTagUnit));

        [Hint("showHints", "Define the unit flag for hostile target.")]
        [BoxGroup("Scan Vicinity")]
        public MultiTag hostileFlag = new MultiTag("Unit Flags", typeof(MultiTagUnit));

        [SerializeField, ReadOnly, HideInEditorMode]
        [BoxGroup("Guardian"), LabelText("Location")]
        internal Vector3 guardLocation;

        [SerializeField, ReadOnly, HideInEditorMode]
        [BoxGroup("Guardian"), LabelText("Range")]
        internal float guardRange;

        [SerializeField, ReadOnly, HideInEditorMode]
        [BoxGroup("Guardian"), LabelText("Return Guard")]
        internal bool guardReturn;

        [SerializeField, ReadOnly, HideInEditorMode]
        [BoxGroup("Guardian"), LabelText("Return Range")]
        internal float guardReturnRange;

        private CharacterOperator owner;
        private CharacterMuscle muscle;
        private CharacterMotor motor;
        private float detectTimer;

        public CharacterOperator Owner => owner;

        public void Init (CharacterOperator charOp, CharacterMuscle charMuscle, CharacterMotor charMotor)
        {
            owner = charOp;
            muscle = charMuscle;
            motor = charMotor;
            guardLocation = Vector3.negativeInfinity;
            detectTimer = ReRandom.Range(0.005f, 0.03f) * -1;
            if (stat != null)
                stat.Init(owner);
        }

        public void ChangeBehaviours (CharacterBrain brain, MultiTag changeFlags)
        {
            if (changeFlags.ContainAny(1, false))
                staminaPack = brain.staminaPack;
            if (changeFlags.ContainAny(2, false))
                moralePack = brain.moralePack;
            if (changeFlags.ContainAny(4, false))
                targetAimPack = brain.targetAimPack;
            if (changeFlags.ContainAny(8, false))
                searchTargetAtIdle = brain.searchTargetAtIdle;
            if (changeFlags.ContainAny(16, false))
                searchTargetAtStanceIdle = brain.searchTargetAtStanceIdle;
            if (changeFlags.ContainAny(32, false))
                searchTargetInterval = brain.searchTargetInterval;
            if (changeFlags.ContainAny(64, false))
                vicinityRange = brain.vicinityRange;
            if (changeFlags.ContainAny(128, false))
                vicinityRate = brain.vicinityRate;
            if (changeFlags.ContainAny(256, false))
                friendlyFlag = brain.friendlyFlag;
            if (changeFlags.ContainAny(512, false))
                hostileFlag = brain.hostileFlag;
        }

        public void Tick (float deltaTime)
        {
            if (vicinityRate > 0 && vicinityRange > 0)
            {
                detectTimer += deltaTime;
                if (detectTimer >= vicinityRate)
                {
                    detectTimer -= vicinityRate;
                    var position = owner.agentTransform.position;
                    var friends = CharacterOperator.GetUnitInRange(position, vicinityRange, friendlyFlag, new List<CharacterOperator>() {owner}).Count;
                    var enemies = CharacterOperator.GetUnitInRange(position, vicinityRange, hostileFlag, null).Count;
                    owner.AdmitScanVicinity(friends, enemies);
                }
            }

            if (stat)
                stat.Tick(deltaTime);
        }

        public void OnDestroy ()
        {
            Destroy(stat);
        }

        public CharacterBrain Clone ()
        {
            var brain = Instantiate(this);
            if (stat != null)
                brain.stat = stat.Clone();
            return brain;
        }

        public float ModifyHealthStat (float value)
        {
            var updatedValue = stat.ModifyHealth(value);
            if (isDie)
            {
                motor.StopMove();
                muscle.InterruptForceNone();
            }

            return updatedValue;
        }

        public bool isDie => stat != null && stat.currentHealth <= 0;
        public int healthBarIndex => stat.currentBarIndex;
        public int healthBarCount => stat.barCount;

        public bool healVisible
        {
            get => stat != null && stat.visibleHeal;
            set
            {
                if (stat) stat.visibleHeal = value;
            }
        }

        public void DeductStamina (float value)
        {
            stat.ReduceStamina(value);
        }

        public void SetGuardian (Vector3 position, float range, bool backGuardPoint, float backRange)
        {
            guardLocation = position;
            guardRange = range;
            guardReturn = backGuardPoint;
            guardReturnRange = backRange;
        }

        public void CancelGuardian ()
        {
            guardLocation = Vector3.negativeInfinity;
        }

        public bool IsWithinGuardian (Vector3 pos)
        {
            if (!asGuardian)
                return false;
            return Vector3.Distance(guardLocation, pos) <= guardRange;
        }

        public bool asGuardian => !float.IsInfinity(guardLocation.x);

        public void SetSearchTarget (bool idle, bool stanceIdle)
        {
            searchTargetAtIdle = idle;
            searchTargetAtStanceIdle = stanceIdle;
        }

        public bool canStaminaConsume => staminaPack != null;

        public float GetStatStaminaConsume (Stamina.Type type)
        {
            var value = stat.GetStaminaConsume(type);
            if (float.IsPositiveInfinity(value))
                value = 0;
            return value;
        }

        public bool canMoraleAffect => moralePack != null;

        public void DetectStatMoraleEvent (Morale.EventType eventType, AttackDamageData damageData)
        {
            stat.DetectMoraleEvent(eventType, damageData);
        }

        public bool IsLiveStatValue (string statName)
        {
            return statName is StatType.STAT_CURRENT_HEALTH or StatType.STAT_CURRENT_MAX_HEALTH or StatType.STAT_CURRENT_BAR_HEALTH or StatType.STAT_CURRENT_BAR_MAX_HEALTH
                or StatType.STAT_CURRENT_STAMINA or StatType.STAT_CURRENT_MAX_STAMINA or StatType.STAT_CURRENT_MORALE or StatType.STAT_CURRENT_MAX_MORALE
                or StatType.STAT_CURRENT_TARGET_MORALE or StatType.STAT_STAMINA_DODGE_COST or StatType.STAT_GUARD_ZONE;
        }

        public void RefreshLiveStatValue (string statName)
        {
            stat.RefreshLiveStat(statName);
        }

        public bool GetStatValue (string statName, out float value)
        {
            if (stat == null)
            {
                value = 0;
                return false;
            }

            if (statName == StatType.STAT_CURRENT_HEALTH)
            {
                value = stat.currentHealth;
                return true;
            }

            if (statName == StatType.STAT_CURRENT_MAX_HEALTH)
            {
                value = stat.currentMaxHealth;
                return true;
            }

            if (statName == StatType.STAT_CURRENT_BAR_HEALTH)
            {
                value = stat.currentBarHealth;
                return true;
            }

            if (statName == StatType.STAT_CURRENT_BAR_MAX_HEALTH)
            {
                value = stat.currentBarMaxHealth;
                return true;
            }

            if (statName == StatType.STAT_CURRENT_STAMINA)
            {
                value = stat.currentStamina;
                return true;
            }

            if (statName == StatType.STAT_CURRENT_MAX_STAMINA)
            {
                value = stat.currentMaxStamina;
                return true;
            }

            if (statName == StatType.STAT_CURRENT_MORALE)
            {
                value = stat.currentMorale;
                return true;
            }

            if (statName == StatType.STAT_CURRENT_MAX_MORALE)
            {
                value = stat.currentMaxMorale;
                return true;
            }

            if (statName == StatType.STAT_CURRENT_TARGET_MORALE)
            {
                value = stat.currentTargetMorale;
                return true;
            }

            if (statName == StatType.STAT_STAMINA_DODGE_COST)
            {
                value = stat.GetStaminaConsume(Stamina.Type.DodgeAttack);
                if (!float.IsPositiveInfinity(value))
                {
                    return true;
                }
                else
                {
                    value = 0;
                    return false;
                }
            }

            if (statName == StatType.STAT_GUARD_ZONE)
            {
                if (owner.statValueCharacterParam != null)
                {
                    if (!owner.statValueCharacterParam.asGuardian)
                    {
                        value = 0f;
                        return true;
                    }

                    value = -1f;
                    if (owner.statValueCharacterParam.IsWithinGuardLocation(owner.agentTransform.position))
                        value = 1f;
                    return true;
                }

                value = 0f;
                return false;
            }

            value = CalcStat(stat.GetValue(statName), stat.GetMtp(statName), stat.GetMod(statName), stat.GetMag(statName));
            return true;
        }

        public float CalcStat (float value, float mtp, float mod, float mag)
        {
            return ((value * (1 + mtp)) + mod) * (1 + mag);
        }

        public void AddStatMod (string id, string statName, float mod)
        {
            if (stat == null) return;
            if (IsLiveStatValue(statName))
            {
                if (statName == StatType.STAT_CURRENT_HEALTH && mod > 0)
                {
                    var changed = stat.ModifyHealth(mod);
                    if (changed != 0)
                        owner.AdmitStatChanged(statName);
                }
                else if (statName == StatType.STAT_CURRENT_STAMINA && mod != 0)
                {
                    var changed = stat.ModifyStamina(mod);
                    if (changed != 0)
                        owner.AdmitStatChanged(statName);
                }
                else if (statName == StatType.STAT_CURRENT_MORALE && mod > 0)
                {
                    owner.PrintLog(owner.gameObject.name + " have " + mod + " morale from stat modification");
                    var changed = stat.ModifyMorale(mod);
                    if (changed != 0)
                        owner.AdmitStatChanged(statName);
                }
            }
            else
            {
                stat.AddMod(id, statName, mod);
                owner.AdmitStatChanged(statName);
            }
        }

        public void AddStatValue (string id, string statName, float value)
        {
            if (stat == null) return;
            stat.AddValue(id, statName, value);
            owner.AdmitStatChanged(statName);
        }

        public void AddStatMtp (string id, string statName, float mtp)
        {
            if (stat == null) return;
            stat.AddMtp(id, statName, mtp);
            owner.AdmitStatChanged(statName);
        }

        public void AddStatMag (string id, string statName, float mag)
        {
            if (stat == null) return;
            stat.AddMag(id, statName, mag);
            owner.AdmitStatChanged(statName);
        }

        public void RemoveStatValue (string id, string statName, float value)
        {
            if (stat == null) return;
            if (stat.RemoveValue(id, statName, value))
                owner.AdmitStatChanged(statName);
        }

        public void RemoveStatMod (string id, string statName, float mod)
        {
            if (stat == null) return;
            if (stat.RemoveMod(id, statName, mod))
                owner.AdmitStatChanged(statName);
        }

        public void RemoveStatMtp (string id, string statName, float mtp)
        {
            if (stat == null) return;
            if (stat.RemoveMtp(id, statName, mtp))
                owner.AdmitStatChanged(statName);
        }

        public void RemoveStatMag (string id, string statName, float mag)
        {
            if (stat == null) return;
            if (stat.RemoveMag(id, statName, mag))
                owner.AdmitStatChanged(statName);
        }

#if UNITY_EDITOR
        private void CreateNewCharacterStat ()
        {
            var created = CharacterStat.CreateNew();
            if (created != null)
                stat = created;
        }

        public static CharacterBrain CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Character Brain", "New Character Brain", "asset", "Select a location to create character brain");
            return path.Length == 0 ? null : ReEditorHelper.CreateScriptableObject<CharacterBrain>(null, false, false, string.Empty, path);
        }
#endif
    }
}