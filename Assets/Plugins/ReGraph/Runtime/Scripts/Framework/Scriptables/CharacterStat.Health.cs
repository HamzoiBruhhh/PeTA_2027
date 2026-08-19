using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    public partial class CharacterStat
    {
        [Serializable]
        public class HealthSection
        {
            public string id;
            public float current;
            public float max;
        }

        public const int MASTER_INDEX = 0;
        private const float TOLERANCE = 0.001f;

        [Hint("showHints", "Define the unit is in full health when it get spawned.")]
        [SerializeField, BoxGroup("Health"), LabelText("Full On Init")]
        private bool fullHealthOnInit = true;

        [Hint("showHints", "Define the unit current health can be overflow (value more than max health value).")]
        [SerializeField, BoxGroup("Health"), LabelText("Overflow")]
        private bool allowHealthOverflow;
        
        [Hint("showHints", "Define the unit have extra layers of health bar on top of the base health.")]
        [SerializeField, BoxGroup("Health"), LabelText("Extra Layer")]
        private FloatProperty[] extraHealthLayer;

        [SerializeField, HideInEditorMode, DisableInPlayMode, BoxGroup("Health")]
        private List<HealthSection> healths;
        
        [HideInInspector]
        public bool visibleHeal;

        public float currentHealth
        {
            get
            {
                var value = 0f;
                if (healths != null)
                    for (var i = 0; i < healths.Count; i++)
                        value += healths[i].current;
                return value;
            }
        }

        public float currentMaxHealth
        {
            get
            {
                var value = 0f;
                if (healths != null)
                    for (var i = 0; i < healths.Count; i++)
                        value += healths[i].max;
                return value;
            }
        }

        public float currentBarHealth
        {
            get
            {
                if (healths != null)
                    for (var i = healths.Count - 1; i >= 0; i--)
                        if (healths[i].current > 0)
                            return healths[i].current;
                return 0;
            }
        }

        public float currentBarMaxHealth
        {
            get
            {
                if (healths != null)
                    for (var i = healths.Count - 1; i >= 0; i--)
                        if (healths[i].current > 0)
                            return healths[i].max;
                return 0;
            }
        }

        public int currentBarIndex
        {
            get
            {
                if (healths != null)
                    for (var i = healths.Count - 1; i >= 0; i--)
                        if (healths[i].current > 0)
                            return i;
                return -1;
            }
        }

        public string currentBarId
        {
            get
            {
                if (healths != null)
                    for (var i = healths.Count - 1; i >= 0; i--)
                        if (healths[i].current > 0)
                            return healths[i].id;
                return default;
            }
        }

        public float masterBarHealth => healths is {Count: > 0} ? healths[MASTER_INDEX].current : 0;

        public float masterBarMaxHealth => healths is {Count: > 0} ? healths[MASTER_INDEX].max : 0;

        public int barCount => healths?.Count ?? 0;

        private float GetBarHealth (string id)
        {
            if (healths != null)
                for (var i = 0; i < healths.Count; i++)
                    if (healths[i].id == id)
                        return healths[i].current;
            return 0;
        }

        private float GetBarHealth (int index)
        {
            if (healths != null && healths.Count < index)
                return healths[index].current;
            return 0;
        }

        private float GetBarMaxHealth (string id)
        {
            if (healths != null)
                for (var i = 0; i < healths.Count; i++)
                    if (healths[i].id == id)
                        return healths[i].max;
            return 0;
        }

        private float GetBarMaxHealth (int index)
        {
            if (healths != null && healths.Count < index)
                return healths[index].max;
            return 0;
        }

        private HealthSection GetBar (string id)
        {
            for (var i = 0; i < healths.Count; i++)
                if (healths[i].id == id)
                    return healths[i];
            return null;
        }

        private HealthSection GetBar (int index)
        {
            if (healths != null && healths.Count < index)
                return healths[index];
            return null;
        }

        public float ModifyHealth (float value)
        {
            if (value < 0)
                return DecreaseHealth(value);
            if (value > 0)
                return IncreaseHealth(value);
            return 0;
        }

        public float DecreaseHealth (float value)
        {
            if (healths is {Count: >= 0})
            {
                for (var i = healths.Count - 1; i >= 0; i--)
                {
                    if (healths[i].current > 0)
                    {
                        var bar = healths[i];
                        bar.current += value;
                        if (bar.current < 0)
                        {
                            var actualMinus = value - bar.current;
                            bar.current = 0;
                            value = actualMinus;
                        }

                        owner.AdmitGetHurt(value);
                        return value;
                    }
                }
            }

            return 0;
        }

        public float IncreaseHealth (float value)
        {
            if (healths is {Count: >= 0})
            {
                for (var i = healths.Count - 1; i >= 0; i--)
                {
                    if (healths[i].current > 0)
                    {
                        var bar = healths[i];
                        bar.current += value;
                        var overflowed = ControlHealthOverflow(i);
                        if (overflowed > 0)
                            value -= overflowed;
                        owner.AdmitGetHeal(value);
                        return value;
                    }
                }
            }

            return 0;
        }

        private void RefreshHealthStat (string statName)
        {
            if (healths is {Count: >= 0})
            {
                var master = healths[MASTER_INDEX];
                if (HaveStatValueChanged(statName, StatType.STAT_MAX_HEALTH, master.max, out var newValue))
                {
                    master.max = newValue;
                    if (master.max < 1)
                        master.max = 1;
                    ControlHealthOverflow(MASTER_INDEX);
                }
            }
        }

        private void InitHealth ()
        {
            visibleHeal = true;
            healths = new List<HealthSection>();
            var master = new HealthSection
            {
                id = "master",
                max = owner.GetStatValue(StatType.STAT_MAX_HEALTH)
            };
            if (master.max <= 0)
                master.max = 1;
            master.current = fullHealthOnInit ? master.max : owner.GetStatValue(StatType.STAT_BASE_HEALTH);
            if (master.current <= 0)
                master.current = 1;
            healths.Add(master);
            ControlHealthOverflow(MASTER_INDEX);

            for (var i = extraHealthLayer.Length - 1; i >= 0; i--)
            {
                if (extraHealthLayer[i] > 0)
                    AddSubHealth("sub"+i, extraHealthLayer[i], extraHealthLayer[i]);
            }
        }

        private void AddSubHealth (string id, float currentHp, float maxHp)
        {
            var sub = new HealthSection
            {
                id = id,
                max = maxHp,
                current = currentHp
            };
            healths.Add(sub);
        }

        private float ControlHealthOverflow (int index)
        {
            if (!allowHealthOverflow && healths != null && index < healths.Count)
            {
                var master = healths[index];
                if (master.current > master.max)
                {
                    var overflowed = master.current - master.max;
                    master.current = master.max;
                    return overflowed;
                }
            }

            return 0;
        }
    }
}