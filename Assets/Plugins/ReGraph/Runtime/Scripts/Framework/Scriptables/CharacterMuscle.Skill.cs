using System;
using System.Collections.Generic;
using Reshape.ReGraph;
using Reshape.Unity;
using UnityEngine;

namespace Reshape.ReFramework
{
    public partial class CharacterMuscle
    {
        private float skillAttackTimer;
        private float skillActivateTimer;

        public List<AttackSkillMuscle> GetActiveToggleSkills ()
        {
            var skills = new List<AttackSkillMuscle>();
            if (skillMuscles is {Count: > 0})
            {
                for (var i = 0; i < skillMuscles.Count; i++)
                {
                    if (skillMuscles[i] != null)
                        if (skillMuscles[i].isActiveSkill || skillMuscles[i].isToggleSkill)
                            skills.Add(skillMuscles[i]);
                }
            }

            return skills;
        }
        
        public AttackSkillMuscle GetActiveSkill ()
        {
            if (skillMuscles is {Count: > 0})
            {
                for (var i = 0; i < skillMuscles.Count; i++)
                {
                    if (skillMuscles[i] != null)
                    {
                        if (skillMuscles[i].isActiveSkill)
                            return skillMuscles[i];
                    }
                }
            }

            return null;
        }
        
        public AttackSkillMuscle GetToggleSkill ()
        {
            if (skillMuscles is {Count: > 0})
            {
                for (var i = 0; i < skillMuscles.Count; i++)
                {
                    if (skillMuscles[i] != null)
                    {
                        if (skillMuscles[i].isToggleSkill)
                            return skillMuscles[i];
                    }
                }
            }

            return null;
        }
        
        public void ToggleSkill (AttackSkillMuscle skill, bool toggle)
        {
            var skillMuscle = FindSkill(skill.pack);
            skillMuscle?.SetToggle(toggle);
        }
        
        public void UntoggleAllSkill ()
        {
            if (skillMuscles is {Count: > 0})
            {
                DetectSkillToggleable();
                for (var i = 0; i < skillMuscles.Count; i++)
                {
                    var skill = skillMuscles[i];
                    if (skill is {isToggleSkill: true, isToggleable: false})
                        skill.SetToggle(false);
                }
            }
        }

        public AttackSkillMuscle FindSkill (AttackSkillPack pack)
        {
            if (skillMuscles is {Count: > 0})
            {
                for (var i = 0; i < skillMuscles.Count; i++)
                {
                    if (skillMuscles[i] != null)
                    {
                        if (skillMuscles[i].pack == pack)
                            return skillMuscles[i];
                    }
                }
            }

            return null;
        }

        private void DetectSkillAttack (float deltaTime)
        {
            if (currentState is State.FightMeleeReady or State.FightRangedReady)
            {
                ResetSkillAttackTimer();
                ExecuteSkillDetect(TriggerNode.Type.CharacterAttackSkill);
            }
            else if (skillAttackTimer >= skillAttackInterval)
            {
                ResetSkillAttackTimer();
                ExecuteSkillDetect(TriggerNode.Type.CharacterAttackSkill);
            }
            else
            {
                skillAttackTimer += deltaTime;
            }
        }

        private void DetectSkillToggleable ()
        {
            var haveDetect = false;
            for (var i = 0; i < skillMuscles.Count; i++)
            {
                if (skillMuscles[i] != null)
                {
                    skillMuscles[i].ResetSeal();
                    if (skillMuscles[i].isToggleSkill && skillMuscles[i].HaveDetect(TriggerNode.Type.AttackSkillToggle))
                    {
                        skillMuscles[i].MarkSeal();
                        haveDetect = true;
                    }
                }
            }

            if (haveDetect)
            {
                var attackDamageData = new AttackDamageData();
                attackDamageData.Init(owner, CharacterOperator.AttackType.None, null, false);
                attackDamageData.SetTarget(attackTarget);
                for (var i = 0; i < skillMuscles.Count; i++)
                {
                    if (skillMuscles[i].IsSeal())
                    {
                        var skillData = skillMuscles[i].Detect(TriggerNode.Type.AttackSkillToggle, attackDamageData);
                        skillMuscles[i].SetToggleable(skillData.GetDetectResult(out var result));
                        skillData.Terminate();
                    }
                }
            }
        }

        private void ExecuteSkillDetect (TriggerNode.Type triggerType)
        {
            AttackSkillData attackSkillData = null;
            if (skillMuscles is {Count: > 0})
            {
                DetectSkillToggleable();
                
                //-- NOTE detect skill attack for passive skill and toggle skill
                var haveDetect = false;
                for (var i = 0; i < skillMuscles.Count; i++)
                {
                    if (skillMuscles[i] != null)
                    {
                        skillMuscles[i].ResetSeal();
                        if (skillMuscles[i].HaveDetect(triggerType))
                        {
                            if (skillMuscles[i].isToggleSkill)
                                if (!skillMuscles[i].isToggleable || !skillMuscles[i].isToggled)
                                    continue;
                            skillMuscles[i].MarkSeal();
                            haveDetect = true;
                        }
                    }
                }

                if (haveDetect)
                {
                    var attackDamageData = new AttackDamageData();
                    attackDamageData.Init(owner, CharacterOperator.AttackType.None, null, false);
                    attackDamageData.SetTarget(attackTarget);
                    for (var i = 0; i < skillMuscles.Count; i++)
                    {
                        if (skillMuscles[i].IsSeal())
                        {
                            var skillData = skillMuscles[i].Detect(triggerType, attackDamageData);
                            if (skillData.GetDetectResult(out _))
                            {
                                attackSkillData = skillData;
                                break;
                            }

                            skillData.Terminate();
                        }
                    }
                    
                    if (attackSkillData == null)
                        attackDamageData?.Terminate();
                }
            }

            if (attackSkillData != null)
                owner.ExecutePendingSkillAction(attackSkillData);
        }

        private bool DetectSkillComplete ()
        {
            var skillData = attackSkill.skillMuscle.Completing(attackSkill.damageData);
            if (skillData != null)
            {
                var result = skillData.GetDetectResult(out _);
                skillData.Terminate();
                return result;
            }

            return true;
        }

        private float LaunchAllSkill (TriggerNode.Type triggerType)
        {
            var totalCooldown = 0f;
            if (skillMuscles is {Count: > 0})
            {
                var haveDetect = false;
                for (var i = 0; i < skillMuscles.Count; i++)
                {
                    if (skillMuscles[i] != null)
                    {
                        skillMuscles[i].ResetSeal();
                        if (skillMuscles[i].HaveLaunch(triggerType))
                        {
                            skillMuscles[i].MarkSeal();
                            haveDetect = true;
                        }
                    }
                }

                if (haveDetect)
                {
                    var damageData = new AttackDamageData();
                    damageData.Init(owner, CharacterOperator.AttackType.None, null, false);
                    damageData.SetTarget(attackTarget);
                    for (var i = 0; i < skillMuscles.Count; i++)
                    {
                        var skill = skillMuscles[i];
                        if (skill != null && skill.IsSeal())
                            if (skill.Launch(triggerType, damageData, true))
                                totalCooldown += skill.pack.cooldownTime;
                    }

                    damageData.Terminate();
                }
            }

            return totalCooldown;
        }

        private float LaunchAllSkill (TriggerNode.Type triggerType, AttackDamageData damageData)
        {
            var totalCooldown = 0f;
            if (skillMuscles != null)
            {
                for (var i = 0; i < skillMuscles.Count; i++)
                {
                    var skill = skillMuscles[i];
                    if (skill != null)
                        if (skill.Launch(triggerType, damageData))
                            totalCooldown += skill.pack.cooldownTime;
                }
            }

            return totalCooldown;
        }

        private float LaunchSkill (TriggerNode.Type triggerType, AttackDamageData damageData)
        {
            if (attackSkill != null && attackSkill.skillMuscle != null)
                if (attackSkill.skillMuscle.Launch(triggerType, damageData))
                    return attackSkill.skillMuscle.pack.cooldownTime;
            return 0;
        }

        public void UpdateSkill (float deltaTime)
        {
            if (skillMuscles != null)
            {
                if (skillMuscles.Count > 0)
                {
                    //-- NOTE detect skill toggle for toggle skill

                    for (var i = 0; i < skillMuscles.Count; i++)
                    {
                        if (skillMuscles[i] != null && !skillMuscles[i].isToggleSkill)
                            if (skillMuscles[i].HaveDetect(TriggerNode.Type.AttackSkillToggle))
                                skillMuscles[i].SetToggleSkill(true);
                    }

                    //-- NOTE detect skill activate for active skill

                    var haveDetect = false;
                    for (var i = 0; i < skillMuscles.Count; i++)
                    {
                        if (skillMuscles[i] != null)
                        {
                            skillMuscles[i].ResetSeal();
                            if (skillMuscles[i].HaveDetect(TriggerNode.Type.AttackSkillActivate))
                            {
                                skillMuscles[i].MarkSeal();
                                haveDetect = true;
                            }
                        }
                    }

                    if (haveDetect)
                    {
                        var attackDamageData = new AttackDamageData();
                        attackDamageData.Init(owner, CharacterOperator.AttackType.None, null, false);
                        attackDamageData.SetTarget(attackTarget);
                        for (var i = 0; i < skillMuscles.Count; i++)
                        {
                            if (skillMuscles[i].IsSeal())
                            {
                                var skillData = skillMuscles[i].Detect(TriggerNode.Type.AttackSkillActivate, attackDamageData);
                                skillMuscles[i].SetActivatable(skillData.GetDetectResult(out var result));
                                if (result < int.MaxValue)
                                    skillMuscles[i].SetActiveSkill(true);
                                skillData.Terminate();
                            }
                        }

                        attackDamageData.Terminate();
                    }
                }

                for (var i = 0; i < skillMuscles.Count; i++)
                    skillMuscles[i].Tick(attackSkill, deltaTime);

                if (!underSkillFight)
                {
                    for (var i = 0; i < skillMuscles.Count; i++)
                    {
                        if (skillMuscles[i] != null)
                        {
                            if (skillMuscles[i].isTerminateLater)
                            {
                                skillMuscles[i].Terminate();
                                skillMuscles.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }

        public void InitSkill ()
        {
            ResetSkillAttackTimer();
            if (skillMuscles != null)
            {
                for (var i = 0; i < skillMuscles.Count; i++)
                {
                    if (skillMuscles[i] != null)
                        skillMuscles[i].Begin(owner);
                }
            }
        }

        private void ResetSkillAttackTimer ()
        {
            skillAttackTimer = ReRandom.Range(0.005f, 0.03f) * -1;
        }

        public void LearnSkill (AttackSkillPack pack)
        {
            skillMuscles ??= new List<AttackSkillMuscle>();
            var index = -1;
            for (var i = 0; i < skillMuscles.Count; i++)
            {
                if (skillMuscles[i] != null && skillMuscles[i].pack == pack)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                var skill = new AttackSkillMuscle();
                skill.Begin(owner, pack);
                skillMuscles.Add(skill);
            }
            else
            {
                skillMuscles[index].IncreaseStack();
            }
        }

        public void GiveUpSkill (AttackSkillPack pack)
        {
            if (skillMuscles != null)
            {
                var index = -1;
                for (var i = 0; i < skillMuscles.Count; i++)
                {
                    if (skillMuscles[i] != null && skillMuscles[i].pack == pack)
                    {
                        index = i;
                        break;
                    }
                }

                if (index > -1)
                {
                    skillMuscles[index].DecreaseStack();
                    if (!skillMuscles[index].isFunctioning)
                    {
                        if (skillMuscles[index].isToggleSkill)
                            skillMuscles[index].SetToggle(false);
                        if (underSkillFight && skillMuscles[index] == attackSkill.skillMuscle)
                        {
                            skillMuscles[index].TerminateLater();
                        }
                        else
                        {
                            skillMuscles[index].Terminate();
                            skillMuscles.RemoveAt(index);
                        }
                    }
                }
            }
        }
    }
}