using System;
using System.Collections.Generic;
using Reshape.ReGraph;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    [Serializable]
    public class AttackSkillMuscle
    {
        [HorizontalGroup("SkillMuscle")]
        [HideLabel]
        [SerializeField]
        [ShowInInspector]
        private AttackSkillPack skillPack;

        [ShowInInspector]
        [HideInEditorMode]
        private bool activatable;
        
        [ShowInInspector]
        [HideInEditorMode]
        private bool toggleable;
        
        [ShowInInspector]
        [HideInEditorMode]
        private bool toggled;

        private Dictionary<string, object> cacheList;
        private CharacterOperator owner;
        private int stackCount = 1;
        private float updateTimer;
        private bool activeSkill;
        private bool toggleSkill;
        private bool executionSeal;
        private bool terminateInPlan;

        public bool isActivatable => activatable;
        public bool isToggleable => toggleable;
        public bool isToggled => toggled;
        public bool isToggleSkill => toggleSkill;
        public bool isActiveSkill => activeSkill;
        public string displayName => skillPack.displayName;
        public Sprite displayIcon => skillPack.displayIcon;
        public string displayDescription => skillPack.displayDescription;

        internal AttackSkillMuscle ()
        {
            cacheList = new Dictionary<string, object>();
        }

        internal AttackSkillPack pack => skillPack;

        internal void Begin (CharacterOperator characterOperator)
        {
            updateTimer = ReRandom.Range(0.005f, 0.03f) * -1;
            owner = characterOperator;

            if (skillPack.HaveBegin())
            {
                var attackSkillData = new AttackSkillData();
                attackSkillData.Init(owner, this);
                attackSkillData.TriggerBegin();
                attackSkillData.Terminate();
            }
        }

        internal void Begin (CharacterOperator characterOperator, AttackSkillPack pack)
        {
            skillPack = pack;
            Begin(characterOperator);
        }

        internal bool HaveDetect (TriggerNode.Type triggerType)
        {
            return skillPack.HaveDetecting(triggerType);
        }

        internal AttackSkillData Detect (TriggerNode.Type triggerType, AttackDamageData damageData)
        {
            var attackSkillData = new AttackSkillData();
            attackSkillData.Init(owner, this, damageData);
            attackSkillData.TriggerDetect(triggerType);
            return attackSkillData;
        }

        internal bool HaveLaunch (TriggerNode.Type triggerType)
        {
            return skillPack.HaveLaunching(triggerType);
        }
        
        internal bool HaveToggle ()
        {
            return skillPack.HaveLToggling();
        }

        internal bool Launch (TriggerNode.Type triggerType, AttackDamageData damageData, bool skipHaveCheck = false)
        {
            if (HaveLaunch(triggerType) || skipHaveCheck)
            {
                var attackSkillData = new AttackSkillData();
                attackSkillData.Init(damageData.attacker, this, damageData);
                attackSkillData.TriggerLaunching(triggerType);
                var result = attackSkillData.lastExecuteResult.isSucceed;
                damageData.SetImpairedDamage(attackSkillData.GetDamageDeal());
                attackSkillData.Terminate();
                return result;
            }

            return false;
        }
        
        internal void Toggle ()
        {
            if (HaveToggle())
            {
                var attackSkillData = new AttackSkillData();
                attackSkillData.Init(owner, this);
                attackSkillData.TriggerToggling();
                attackSkillData.Terminate();
            }
        }
        
        internal AttackSkillData Completing (AttackDamageData damageData)
        {
            if (skillPack.HaveCompleting())
            {
                var attackSkillData = new AttackSkillData();
                attackSkillData.Init(owner, this, damageData);
                attackSkillData.TriggerCompleting();
                return attackSkillData;
            }

            return null;
        }

        internal void Tick (AttackSkillData attackSkill, float deltaTime)
        {
            if (skillPack.HaveUpdate())
            {
                if (skillPack != null && skillPack.haveConstantUpdate)
                {
                    updateTimer += deltaTime;
                    float updateTime = skillPack.constantUpdateInterval;
                    if (updateTimer >= updateTime)
                    {
                        updateTimer -= updateTime;
                        var attackSkillData = new AttackSkillData();
                        attackSkillData.Init(owner, this);
                        attackSkillData.TriggerUpdate();
                        if (attackSkill != null && attackSkill.skillMuscle == this)
                            attackSkill.damageData.SetImpairedDamage(attackSkillData.GetDamageDeal());
                        attackSkillData.Terminate();
                    }
                }
            }
        }

        internal void Terminate ()
        {
            if (skillPack.HaveEnd())
            {
                var attackSkillData = new AttackSkillData();
                attackSkillData.Init(owner, this);
                attackSkillData.TriggerTerminate();
                attackSkillData.Terminate();
            }

            cacheList = null;
            owner = null;
            skillPack = null;
        }
        
        internal void TerminateLater ()
        {
            terminateInPlan = true;
        }

        internal bool isTerminated => cacheList == null;
        internal bool isFunctioning => stackCount > 0;
        internal bool isTerminateLater => terminateInPlan;

        internal void SetToggleSkill (bool value)
        {
            toggleSkill = value;
        }
        
        internal void SetToggleable (bool value)
        {
            toggleable = value;
        }
        
        internal void SetToggle (bool value)
        {
            if (toggled != value)
            {
                toggled = value;
                Toggle();
            }
        }

        internal void SetActiveSkill (bool value)
        {
            activeSkill = value;
        }
        
        internal void SetActivatable (bool value)
        {
            activatable = value;
        }

        internal void IncreaseStack ()
        {
            stackCount++;
        }

        internal void DecreaseStack ()
        {
            stackCount--;
        }

        internal object GetCache (string cacheId)
        {
            if (cacheList.ContainsKey(cacheId))
                if (cacheList.TryGetValue(cacheId, out object outCache))
                    return outCache;
            return null;
        }

        internal void SetCache (string cacheId, object value)
        {
            if (cacheList != null)
                if (!cacheList.TryAdd(cacheId, value))
                    cacheList[cacheId] = value;
        }

        internal void ResetSeal ()
        {
            executionSeal = false;
        }
        
        internal void MarkSeal ()
        {
            executionSeal = true;
        }
        
        internal bool IsSeal ()
        {
            return executionSeal;
        }
    }
}