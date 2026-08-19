using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    public partial class CharacterStat
    {
        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Morale")]
        private float morale;

        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Morale")]
        private float baseMorale;

        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Morale")]
        private float targetMorale;

        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Morale")]
        private float maxMorale;

        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Morale")]
        private float highMorale;

        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Morale")]
        private float lowMorale;

        [Hint("showHints", "Define the unit is in full morale when it get spawned.")]
        [SerializeField, BoxGroup("Morale"), LabelText("Full On Init")]
        private bool fullMoraleOnInit = true;

        [Hint("showHints", "Define the unit current morale can be underflow (value less than 0).")]
        [SerializeField, BoxGroup("Morale"), LabelText("Underflow")]
        private bool allowMoraleUnderflow;

        public float currentMorale => morale;
        public float currentTargetMorale => targetMorale;
        public float currentMaxMorale => maxMorale;

        private MoralePack moralePack;
        private float updateDuration;
        private float updateTimer;
        private float incrementMod;
        private float incrementMtp;
        private float incrementMin;

        private void InitMorale ()
        {
            updateTimer = 0;
            GetMoraleStat();
            baseMorale = fullMoraleOnInit ? maxMorale : owner.GetStatValue(StatType.STAT_BASE_MORALE);
            morale = baseMorale;
            GetTargetMoraleToLatest();
            moralePack = owner.brain.moralePack;
            RefreshMoraleEffect();
        }

        public void UpdateMoraleStat (float deltaTime)
        {
            if (moralePack) { }

            GetTargetMoraleToLatest();
            UpdateMoraleToTargetValue(deltaTime);
        }

        public void DetectMoraleEvent (Morale.EventType eventType, AttackDamageData damageData = null)
        {
            var affect = GetMoraleAffect(eventType, damageData);
            if (affect != 0)
            {
                owner.PrintLog(owner.gameObject.name + " have " + affect + " morale due to " + eventType);
                ModifyMorale(affect);
            }
        }

        public float GetMoraleAffect (Morale.EventType eventType, AttackDamageData damageData)
        {
            return moralePack != null ? moralePack.GetAffect(eventType, damageData) : 0;
        }

        [Button]
        [HideInEditorMode]
        public float ModifyMorale (float value)
        {
            GetMoraleStat();
            var previous = morale;
            morale += value;
            if (highMorale != 0 && morale > highMorale)
                morale = highMorale;
            if (lowMorale != 0 && morale < lowMorale)
                morale = lowMorale;
            if (!allowMoraleUnderflow && morale < 0)
                morale = 0;
            RefreshMoraleEffect();
            return morale - previous;
        }

        private void RefreshMoraleStat (string statName)
        {
            if (HaveStatValueChanged(statName, StatType.STAT_MAX_MORALE, maxMorale, out var newValue))
            {
                maxMorale = newValue;
                RefreshMoraleEffect();
            }
        }

        private void RefreshMoraleEffect ()
        {
            if (moralePack)
            {
                var moraleData = new MoraleData();
                moraleData.Init(owner, moralePack);
                moraleData.TriggerChanges();
                moraleData.Terminate();
            }
        }

        private void GetTargetMoraleToLatest ()
        {
            targetMorale = baseMorale + owner.GetStatValue(StatType.STAT_TARGET_MORALE);
        }

        private void UpdateMoraleToTargetValue (float deltaTime)
        {
            var diff = targetMorale - morale;
            if (Math.Abs(diff) > TOLERANCE)
            {
                GetMoraleStat();
                if (updateDuration <= 0)
                {
                    ModifyMorale(diff);
                }
                else
                {
                    updateTimer += deltaTime;
                    if (updateTimer >= updateDuration)
                    {
                        updateTimer -= updateDuration;
                        var increment = (diff * incrementMtp) + incrementMod;
                        if (diff < 0)
                            increment = Mathf.Abs(increment) * -1;
                        if (increment != 0)
                        {
                            if (Math.Abs(increment) < incrementMin)
                            {
                                increment = incrementMin;
                                if (diff < 0)
                                    increment *= -1;
                            }

                            if (diff > 0 && increment > diff)
                            {
                                ModifyMorale(diff);
                            }
                            else if (diff < 0 && increment < diff)
                            {
                                ModifyMorale(diff);
                            }
                            else
                            {
                                ModifyMorale(increment);
                            }
                        }
                    }
                }
            }
            else
            {
                if (diff != 0)
                    ModifyMorale(diff);
                updateTimer = 0;
            }
        }

        private void GetMoraleStat ()
        {
            maxMorale = owner.GetStatValue(StatType.STAT_MAX_MORALE);
            highMorale = owner.GetStatValue(StatType.STAT_HIGH_CAP_MORALE);
            lowMorale = owner.GetStatValue(StatType.STAT_LOW_CAP_MORALE);
            updateDuration = owner.GetStatValue(StatType.STAT_MORALE_UPDATE_TIME);
            incrementMod = owner.GetStatValue(StatType.STAT_MORALE_UPDATE_MOD);
            incrementMtp = owner.GetStatValue(StatType.STAT_MORALE_UPDATE_MTP);
            incrementMin = owner.GetStatValue(StatType.STAT_MORALE_UPDATE_MIN);
        }
    }
}