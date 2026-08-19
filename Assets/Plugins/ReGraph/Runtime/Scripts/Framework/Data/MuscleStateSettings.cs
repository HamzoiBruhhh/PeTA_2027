using System;
using System.Collections;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    [Serializable]
    public class MuscleStateSettings
    {
        [ValueDropdown("StateSettingDropDown")]
        public CharacterMuscle.State state;

        public bool interruptible;

#if UNITY_EDITOR
        public static IEnumerable StateSettingDropDown = new ValueDropdownList<CharacterMuscle.State>()
        {
            {"Stance Activate", CharacterMuscle.State.StanceActivate},
            {"Stance Deactivate", CharacterMuscle.State.StanceDeactivate},
            {"Evaluate Attack", CharacterMuscle.State.FightOnTheMark},
            {"Go Toward Melee", CharacterMuscle.State.FightGoMelee},
            {"Go Toward Standby", CharacterMuscle.State.FightGoMeleeStandby},
            {"Prepare Melee Attack", CharacterMuscle.State.FightMeleeReady},
            {"Cooldown Melee Attack", CharacterMuscle.State.FightMeleeAttackCooldown},
            {"Go Toward Ranged", CharacterMuscle.State.FightGoRangedStandby},
            {"Prepare Ranged Attack", CharacterMuscle.State.FightRangedReady},
            {"Ranged Aim + Draw", CharacterMuscle.State.FightRangedBegin},
            {"Ranged Pending Draw", CharacterMuscle.State.FightRangedAim},
            {"Ranged Pending Aim", CharacterMuscle.State.FightRangedDraw},
            {"Cooldown Ranged Attack", CharacterMuscle.State.FightRangedAttackCooldown},
            {"Cooldown Skill Attack", CharacterMuscle.State.FightSkillAttackCooldown},
        };
#endif
    }
}