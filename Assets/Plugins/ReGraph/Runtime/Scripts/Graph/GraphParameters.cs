using System;
using Reshape.ReFramework;
using Reshape.Unity;
using UnityEngine;

namespace Reshape.ReGraph
{
    [Serializable]
    public class GraphParameters
    {
        public string actionName;
        public GameObject interactedGo;
        public ReMonoBehaviour interactedMono;
        public CharacterBrain characterBrain;
        public CharacterBrain characterBrainAddon;
        public AttackDamageData attackDamageData;
        public TargetAimData targetAimData;
        public MoraleData moraleData;
        public AttackStatusData attackStatusData;
        public LootData lootData;
        public StaminaData staminaData;
        public AttackSkillData attackSkillData;
        public BehaviourData behaviourData;

        public GraphParameters ()
        {
            actionName = string.Empty;
        }

        public void Reset ()
        {
            actionName = string.Empty;
            interactedGo = null;
            interactedMono = null;
            characterBrain = null;
            characterBrainAddon = null;
            attackDamageData = null;
            targetAimData = null;
            moraleData = null;
            attackStatusData = null;
            lootData = null;
            staminaData = null;
            attackSkillData = null;
            behaviourData = null;
        }
    }
}