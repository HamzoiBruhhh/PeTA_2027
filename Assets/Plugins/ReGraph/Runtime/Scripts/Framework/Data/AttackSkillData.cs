using System.Collections.Generic;
using Reshape.ReGraph;
using Reshape.Unity;
using UnityEngine;

namespace Reshape.ReFramework
{
    public class AttackSkillData
    {
        public GraphExecution lastExecuteResult;
        public AttackSkillMuscle skillMuscle;

        public AttackDamageData damageData { get; private set; }
        public string id { get; private set; }
        public CharacterOperator owner { get; private set; }
        public TriggerNode.Type triggerType { get; private set; }

        private List<AttackStatusData> appliedAttackStatus;

        public void AddAppliedAttackStatus (AttackStatusData statusData)
        {
            if (statusData != null)
            {
                appliedAttackStatus ??= new List<AttackStatusData>();
                appliedAttackStatus.Add(statusData);
            }
        }
        
        public float GetDamageDeal ()
        {
            var value = 0f;
            if (appliedAttackStatus != null)
                for (var i = 0; i < appliedAttackStatus.Count; i++)
                    value += appliedAttackStatus[i].damageDeal;
            return value;
        }
        
        public bool GetDetectResult (out int result)
        {
            result = 0;
            if (lastExecuteResult != null)
            {
                result = lastExecuteResult.variables.GetInt(id, int.MaxValue, GraphVariables.PREFIX_RETURN);
                if (result is < int.MaxValue and > 0)
                    return true;
            }

            return false;
        }
        
        public void Init (CharacterOperator characterOperator, AttackSkillMuscle muscle)
        {
            id = ReUniqueId.GenerateId(false);
            owner = characterOperator;
            skillMuscle = muscle;
        }

        public void Init (CharacterOperator characterOperator, AttackSkillMuscle skillMuscle, AttackDamageData damage)
        {
            Init(characterOperator, skillMuscle);
            damageData = damage;
        }

        public void Terminate ()
        {
            if (!string.IsNullOrEmpty(id))
                ReUniqueId.ReturnId(id);
            lastExecuteResult?.ReleaseReverse();
            lastExecuteResult = null;
            skillMuscle = null;
            owner = null;
        }

        public void TriggerLaunching (TriggerNode.Type type)
        {
            if (!owner || skillMuscle == null || damageData == null)
            {
                ReDebug.LogWarning("Attack Skill Data Warning", "TriggerLaunching activation being ignored due to missing require params");
                return;
            }

            triggerType = type;
            skillMuscle.pack.TriggerLaunching(this);
        }
        
        public void TriggerCompleting ()
        {
            if (!owner || skillMuscle == null || damageData == null)
            {
                ReDebug.LogWarning("Attack Skill Data Warning", "TriggerLaunching activation being ignored due to missing require params");
                return;
            }
            
            skillMuscle.pack.TriggerCompleting(this);
        }
        
        public void TriggerToggling ()
        {
            if (!owner || skillMuscle == null)
            {
                ReDebug.LogWarning("Attack Skill Data Warning", "TriggerToggling activation being ignored due to missing require params");
                return;
            }
            
            skillMuscle.pack.TriggerToggling(this);
        }
        
        public void TriggerDetect (TriggerNode.Type type)
        {
            if (!owner || skillMuscle == null)
            {
                ReDebug.LogWarning("Attack Skill Data Warning", "TriggerDetect activation being ignored due to missing require params");
                return;
            }

            triggerType = type;
            skillMuscle.pack.TriggerDetecting(this);
        }
        
        public void TriggerBegin ()
        {
            if (!owner || skillMuscle == null)
            {
                ReDebug.LogWarning("Attack Skill Data Warning", "TriggerBegin activation being ignored due to missing require params");
                return;
            }
            
            skillMuscle.pack.TriggerBegin(this);
        }
        
        public void TriggerUpdate ()
        {
            if (!owner || skillMuscle == null)
            {
                ReDebug.LogWarning("Attack Skill Data Warning", "TriggerUpdate activation being ignored due to missing require params");
                return;
            }
            
            skillMuscle.pack.TriggerUpdate(this);
        }
        
        public void TriggerTerminate ()
        {
            if (!owner || skillMuscle == null)
            {
                ReDebug.LogWarning("Attack Skill Data Warning", "TriggerTerminate activation being ignored due to missing require params");
                return;
            }
            
            skillMuscle.pack.TriggerEnd(this);
        }
    }
}