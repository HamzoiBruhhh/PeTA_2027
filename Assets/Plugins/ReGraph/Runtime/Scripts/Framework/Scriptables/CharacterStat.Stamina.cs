using Reshape.ReGraph;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    public partial class CharacterStat
    {
        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Stamina")]
        private float stamina;

        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Stamina")]
        private float maxStamina;

        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Stamina"), LabelText("Charge Rate"), SuffixLabel("sec", true)]
        private float staminaChargeRate;
        
        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Stamina"), LabelText("Charge Value")]
        private float staminaChargeValue;

        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Stamina"), LabelText("Charge Timer"), SuffixLabel("sec", true)]
        private float staminaTimer;

        [Hint("showHints", "Define the unit is in full stamina when it get spawned.")]
        [SerializeField, BoxGroup("Stamina"), LabelText("Full On Init")]
        private bool fullStaminaOnInit = true;

        [Hint("showHints", "Define the unit current stamina can be overflow (value more than max stamina value).")]
        [SerializeField, BoxGroup("Stamina"), LabelText("Overflow")]
        private bool allowStaminaOverflow;

        public float currentStamina => stamina;
        public float currentMaxStamina => maxStamina;

        private StaminaPack staminaPack;

        private void InitStamina ()
        {
            maxStamina = owner.GetStatValue(StatType.STAT_MAX_STAMINA);
            stamina = fullStaminaOnInit ? maxStamina : owner.GetStatValue(StatType.STAT_BASE_STAMINA);
            staminaChargeRate = owner.GetStatValue(StatType.STAT_STAMINA_CHARGE_RATE);
            staminaChargeValue = owner.GetStatValue(StatType.STAT_STAMINA_CHARGE_VALUE);
            staminaPack = owner.brain.staminaPack;
            ControlStaminaOverflow();
        }

        public void UpdateStaminaStat (float deltaTime)
        {
            if (staminaPack != null)
            {
                var charge = false;
                if (staminaPack.chargeDuringFight && owner.underFight && !owner.isMoving)
                    charge = true;
                else if (staminaPack.chargeDuringIdle && owner.underIdle && !owner.isMoving)
                    charge = true;
                else if (staminaPack.chargeDuringWalk && owner.isMoving)
                    charge = true;
                if (charge)
                {
                    if (stamina < maxStamina)
                    {
                        staminaTimer += deltaTime;
                        if (staminaTimer >= staminaChargeRate)
                        {
                            staminaTimer -= staminaChargeRate;
                            AddStamina(staminaChargeValue);
                        }
                    }
                }
            }
        }

        public float GetStaminaConsume (Stamina.Type type)
        {
            if (staminaPack)
            {
                var staminaData = new StaminaData();
                staminaData.Init(owner, staminaPack, type);
                staminaData.TriggerConsume();
                var value = staminaData.lastExecuteResult.variables.GetNumber(staminaData.id, float.PositiveInfinity, int.MaxValue, GraphVariables.PREFIX_RETURN);
                staminaData.Terminate();
                return value;
            }

            return float.PositiveInfinity;
        }
        
        [Button]
        [HideInEditorMode]
        public float ModifyStamina (float value)
        {
            if (value < 0)
                return ReduceStamina(value * -1);
            else if (value > 0)
                return AddStamina(value);
            return 0;
        }

        public float AddStamina (float value)
        {
            var previous = stamina;
            stamina += value;
            ControlStaminaOverflow();
            return stamina - previous;
        }

        public float ReduceStamina (float value)
        {
            if (value > 0)
            {
                if (stamina >= value)
                {
                    stamina -= value;
                    return value * -1;
                }
                else
                {
                    var previous = stamina;
                    stamina = 0;
                    return previous;
                }
            }

            return 0;
        }

        private void RefreshStaminaStat (string statName)
        {
            if (HaveStatValueChanged(statName, StatType.STAT_MAX_STAMINA, maxStamina, out var newValue))
            {
                maxStamina = newValue;
                ControlStaminaOverflow();
            }
            else if (HaveStatValueChanged(statName, StatType.STAT_STAMINA_CHARGE_RATE, staminaChargeRate, out newValue))
            {
                staminaChargeRate = newValue;
            }
            else if (HaveStatValueChanged(statName, StatType.STAT_STAMINA_CHARGE_VALUE, staminaChargeValue, out newValue))
            {
                staminaChargeValue = newValue;
            }
        }

        private float ControlStaminaOverflow ()
        {
            if (!allowStaminaOverflow)
            {
                if (stamina > maxStamina)
                {
                    var overflowed = stamina - maxStamina;
                    stamina = maxStamina;
                    return overflowed;
                }
            }

            return 0;
        }
    }
}