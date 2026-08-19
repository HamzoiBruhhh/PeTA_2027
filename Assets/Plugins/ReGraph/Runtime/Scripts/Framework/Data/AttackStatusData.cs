using System;
using System.Collections.Generic;
using Reshape.ReGraph;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    public class AttackStatusData
    {
        public GraphExecution lastExecuteResult;

        [ShowInInspector, HideInEditorMode, ReadOnly]
        private AttackStatusPack statusPack;
        
        [ShowInInspector, HideInEditorMode, ReadOnly]
        private float updateTimer;
        
        [ShowInInspector, HideInEditorMode, ReadOnly]
        private int applyCount = 1;
        
        private Dictionary<string, object> cacheList;
        
        public string id { get; private set; }
        public CharacterOperator owner { get; private set; }
        public CharacterOperator applier { get; private set; }
        public string statusPackName => statusPack == null ? string.Empty : statusPack.name;
        public AttackStatusPack attackStatus => statusPack;
        public float damageDeal { get; private set; }
        public bool isMultipleApplied => applyCount > 1;

        public void IncreaseApply ()
        {
            applyCount++;
        }

        public void DecreaseApply ()
        {
            applyCount--;
        }
        
        public void Init (CharacterOperator characterOperator, AttackStatusPack pack, CharacterOperator applyCharacterOperator)
        {
            id = ReUniqueId.GenerateId(false);
            owner = characterOperator;
            if (applyCharacterOperator != owner)
                applier = applyCharacterOperator;
            statusPack = pack;
            updateTimer = 0;
            cacheList = new Dictionary<string, object>();
        }
        
        public void RecordDamage (float damage)
        {
            damageDeal += damage;
        }

        public void Terminate ()
        {
            ReUniqueId.ReturnId(id);
            lastExecuteResult = null;
            statusPack = null;
            owner = null;
            cacheList = null;
        }
        
        public bool IsSamePack (AttackStatusPack pack)
        {
            if (pack != null)
                return statusPack == pack;
            return false;
        }
        
        public void TriggerBegin ()
        {
            if (owner == null || statusPack == null)
            {
                ReDebug.LogWarning("Attack Status Data Warning", "TriggerBegin activation being ignored due to missing require params");
                return;
            }
            
            statusPack.TriggerActivation(this);
        }

        public void Tick (float deltaTime)
        {
            if (statusPack != null && statusPack.constantly)
            {
                updateTimer += deltaTime;
                float updateTime = statusPack.constantUpdateTime;
                if (updateTimer >= updateTime)
                {
                    updateTimer -= updateTime;
                    statusPack.TriggerUpdateActive(this);
                }
            }
        }

        public void TriggerTerminate ()
        {
            if (owner == null || statusPack == null)
            {
                ReDebug.LogWarning("Attack Status Data Warning", "TriggerTerminate activation being ignored due to missing require params");
                return;
            }
            
            statusPack.TriggerDeactivation(this);
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
    }
}