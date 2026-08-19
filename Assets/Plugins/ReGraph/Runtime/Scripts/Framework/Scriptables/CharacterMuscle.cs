using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReGraph;
using Reshape.Unity;
#if UNITY_EDITOR
using Reshape.Unity.Editor;
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Character Muscle", fileName = "CharacterMuscle", order = 305)]
    [Serializable]
    [HideMonoScript]
    public partial class CharacterMuscle : BaseScriptable
    {
        public enum State
        {
            None = 0,
            Idle = 10,
            Walk = 25,
            Push = 26,
            Flee = 27,
            StanceIdle = 100,
            StanceActivate = 110,
            StanceDeactivate = 115,
            StanceWalk = 120,
            StancePush = 121,
            StanceStandby = 130,
            FightOnTheMark = 200, // Think about what to do when attack activated
            FightGoMelee = 210, // Move toward the attack target
            FightGoMeleeStandby = 220, // Move toward the standby point
            FightMeleeReady = 500, // Think about what melee attack to choose
            FightMeleeAttack = 510,
            FightMeleeAttackHit = 512,
            FightMeleeAttackCooldown = 520,
            FightRangedReady = 1000, // Think about what ranged attack to choose
            FightRangedBegin = 1010,
            FightRangedAim = 1011,
            FightRangedDraw = 1012,
            FightRangedFire = 1013,
            FightRangedAttack = 1014,
            FightRangedAttackCooldown = 1015,
            FightGoRangedStandby = 1030,
            FightSkillReady = 1500,
            FightSkillAttack = 1510,
            FightSkillAttackCooldown = 1520,
        }

        public enum AttackGoTowardFruitlessChoice
        {
            StandStillToDecide = 10,
            ContinueTowardClosest = 11,
            TowardAndDecide = 12,
        }

        public enum AttackOnTheMarkFruitlessChoice
        {
            StandStillAndWait = 10,
            TowardStandbyAndWait = 12,
        }

        public enum AttackGoStandbyFruitlessChoice
        {
            StandStillAndWait = 10,
        }

        public enum MeleeCooldownTypeChoice
        {
            StartFromAttackLaunch = 10,
            StartAfterAttackFinish = 20,
        }

        public enum RangedCooldownTypeChoice
        {
            StartAfterDraw = 10,
            StartAfterShot = 20,
        }

        public enum RangedStandbyTypeChoice
        {
            ChooseFromFurther = 10,
            ChooseShortestDistance = 20,
        }

        public enum AttackGoMeleeCrashChoice
        {
            StandStillAndWait = 10,
        }

        public enum AttackRangedReadyFruitlessChoice
        {
            StandStillToDecide = 10,
            ContinueAttack = 20,
        }

        public enum AttackMeleeReadyFruitlessChoice
        {
            StandStillToDecide = 10,
            ContinueAttack = 20,
        }

        public enum AttackMeleeCooldownFruitlessChoice
        {
            StandStillToDecide = 10,
            StandStillAndWait = 20,
        }

        public enum AttackRangedCooldownFruitlessChoice
        {
            StandStillToDecide = 10,
            StandStillAndWait = 20,
        }

        [Hint("showHints", "Define the unit default in stance mode.")]
        [BoxGroup("General"), LabelText("Stance By Default")]
        [LabelWidth(180)]
        public bool defaultStance;

        [Hint("showHints",
            "Define the unit reset cooldown on actions (move, attack, skill & etc). Without reset, the attack might not launch immediate after command an attack because it might still under cooldown period.")]
        [BoxGroup("General"), LabelText("Reset Cooldown on Actions")]
        [LabelWidth(180)]
        public bool cooldownResetOnActions;

        [Hint("showHints", "Define which muscle state is allow to be interrupt.")]
        [TableList(AlwaysExpanded = true)]
        [SerializeField]
        private List<MuscleStateSettings> stateSettings;

        [Hint("showHints", "Define how many unit can attack this unit at the same time.")]
        [BoxGroup("Melee"), LabelText("Engage Amount")]
        [SuffixLabel("units", true)]
        public int meleeEngageAmount = 3;

        [Hint("showHints", "Define the range of melee attacker stand point. Advice to set at least double of agent size, system will set value to agent size if value < agent size")]
        [SerializeField, BoxGroup("Melee"), LabelText("Engage Distance")]
        [SuffixLabel("meters", true)]
        private float meleeEngageDistance = 0.2f;

        [Hint("showHints", "Define the range of attacker stand point when the unit have occupied by other melee attackers.")]
        [SerializeField, BoxGroup("Melee"), LabelText("Standby Distance")]
        [SuffixLabel("meters", true)]
        private float meleeStandbyDistance = 1f;

        [Hint("showHints", "Define how many points allow to be scan while searching for a melee standby point.")]
        [SerializeField, BoxGroup("Melee"), LabelText("Scan Limit"), Indent]
        private int meleeScanPointLimit = 5;

        [Hint("showHints", "Define max time to scan while searching for a melee standby point.\n\nGive up scanning on the target after passed the duration.")]
        [SerializeField, BoxGroup("Melee"), LabelText("Scan Duration"), Indent]
        private int meleeScanDuration = 3;

        [Hint("showHints", "Define multiple or single attacker should approach to target when the unit become available for melee attack.")]
        [SerializeField, BoxGroup("Melee"), LabelText("Single Approaching")]
        private bool meleeStandbySingleApproach;

        [Hint("showHints", "Define the unit should use raycast to detect target location. Only check this when the unit is a long range melee attacker.")]
        [SerializeField, BoxGroup("Melee"), LabelText("Raycast Detection")]
        private bool meleeRaycastDetection;

        [Hint("showHints", "Define the cooldown timer should start ticking from which moment.")]
        [SerializeField, BoxGroup("Melee"), LabelText("Cooldown Type")]
        private MeleeCooldownTypeChoice meleeCooldownType = MeleeCooldownTypeChoice.StartFromAttackLaunch;

        [Hint("showHints", "Define the damage pack that use to calculate attack damage.")]
        [SerializeField, BoxGroup("Melee"), LabelText("Damage Pack")]
        private AttackDamagePack meleeAttackDamagePack;

        [Hint("showHints", "Define unit using the stat where before the attack depart from unit.")]
        [SerializeField, BoxGroup("Melee"), LabelText("Use Pre-Attack Stat")]
        private bool meleeUsePreAttackStat;

        [Hint("showHints",
            "Define attack as missed when unit out of the missed range.\n\nValue 0 means no missed ranged, this checking is off.\n\nValue > 0 will add on top of melee attack range to form final value as missed range.")]
        [SerializeField, BoxGroup("Melee"), LabelText("Missed Range")]
        private float meleeMissRange = 0.4f;

        [Hint("showHints", "Define the unit should use linear raycast to forecast projectile is reachable.")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Linear Detection")]
        private bool rangedLinearDetection;

        [Hint("showHints", "Define the size use for unit linear raycast to forecast projectile is reachable.")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Detect Size")]
        [ShowIf("@rangedLinearDetection == true"), Indent]
        private float rangedLinearDetectSize;

        [Hint("showHints", "Define the cooldown timer should start ticking from which moment.")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Cooldown Type")]
        private RangedCooldownTypeChoice rangedCooldownType = RangedCooldownTypeChoice.StartAfterShot;

        [Hint("showHints", "Define the prefab of the ranged attack projectile (bullet, arrow).")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Projectile")]
        private AttackProjectile rangedProjectile;

        [Hint("showHints", "Define the method of choosing a standby ranged location.")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Standby Type")]
        private RangedStandbyTypeChoice rangedStandbyType = RangedStandbyTypeChoice.ChooseFromFurther;

        [Hint("showHints", "Define how many points allow to be scan while searching for a ranged standby point.")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Scan Limit"), Indent]
        private int rangedScanPointLimit = 5;

        [Hint("showHints", "Define max time to scan while searching for a ranged standby point.\n\nGive up scanning on the target after passed the duration.")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Scan Duration"), Indent]
        private int rangedScanDuration = 3;

        [Hint("showHints", "Define how close the ranged standby point toward the ranged attack range.")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Range Offset"), Indent]
        [SuffixLabel("%", true)]
        [Range(0.5f, 0.9f)]
        private float rangedStandbyOffset = 0.8f;

        [Hint("showHints", "Define the unit should always facing the target during ranged attack stance.")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Always Face Target")]
        private bool rangedFaceTarget;

        [Hint("showHints", "Define unit using the stat where before the attack depart from unit.")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Use Pre-Attack Stat")]
        private bool rangeUsePreAttackStat;

        [Hint("showHints", "Define unit using melee attack when attack target engage with him.")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Melee On Engage")]
        private bool rangeMeleeOnEngage;

        [Hint("showHints", "Define unit using melee attack after a ranged attack cooldown.")]
        [SerializeField, BoxGroup("Ranged"), LabelText("Melee On Cooldown")]
        private bool rangeMeleeOnCooldown;

        [Hint("showHints", "Define unit's skills.")]
        [SerializeField, BoxGroup("Skill")]
        [LabelText("Skill Packs")]
        private List<AttackSkillMuscle> skillMuscles;

        [Hint("showHints", "Define how frequent unit detect a skill attack.")]
        [SerializeField, BoxGroup("Skill")]
        [LabelText("Skill Attack Interval")]
        public float skillAttackInterval = 0.1f;

        [Hint("showHints", "Define the reaction when the unit have no result on this action.")]
        [SerializeField, BoxGroup("Fruitless Action"), LabelText("Get Melee Point")]
        private AttackOnTheMarkFruitlessChoice attackOnTheMarkFruitlessAction = AttackOnTheMarkFruitlessChoice.StandStillAndWait;

        [Hint("showHints", "Define the reaction when the unit have no result on this action.")]
        [SerializeField, BoxGroup("Fruitless Action"), LabelText("Go Toward Attack")]
        private AttackGoTowardFruitlessChoice attackGoTowardFruitlessAction = AttackGoTowardFruitlessChoice.ContinueTowardClosest;

        [Hint("showHints", "Define the reaction when the unit have no result on this action.")]
        [SerializeField, BoxGroup("Fruitless Action"), LabelText("Go Standby Attack")]
        private AttackGoStandbyFruitlessChoice attackGoStandbyFruitlessAction = AttackGoStandbyFruitlessChoice.StandStillAndWait;

        [Hint("showHints", "Define the reaction when the unit have its target coming toward his melee range.")]
        [SerializeField, BoxGroup("Fruitless Action"), LabelText("Go Melee Attack")]
        private AttackGoMeleeCrashChoice attackGoMeleeCrashAction = AttackGoMeleeCrashChoice.StandStillAndWait;

        [Hint("showHints", "Define the reaction when the unit have its target walking away when he ready for melee attack.")]
        [SerializeField, BoxGroup("Fruitless Action"), LabelText("Ready Melee")]
        private AttackMeleeReadyFruitlessChoice attackMeleeReadyFruitlessAction = AttackMeleeReadyFruitlessChoice.StandStillToDecide;

        [Hint("showHints", "Define the reaction when the unit have its target walking away when he ready for ranged attack.")]
        [SerializeField, BoxGroup("Fruitless Action"), LabelText("Ready Ranged")]
        private AttackRangedReadyFruitlessChoice attackRangedReadyFruitlessAction = AttackRangedReadyFruitlessChoice.StandStillToDecide;

        [Hint("showHints", "Define the reaction when the unit have its target walking away during his melee attack cooldown right after attack.")]
        [SerializeField, BoxGroup("Fruitless Action"), LabelText("Cooldown Melee")]
        private AttackMeleeCooldownFruitlessChoice attackMeleeCooldownFruitlessAction = AttackMeleeCooldownFruitlessChoice.StandStillToDecide;

        [Hint("showHints", "Define the reaction when the unit have its target walking away during his melee attack cooldown right after attack.")]
        [SerializeField, BoxGroup("Fruitless Action"), LabelText("Cooldown Ranged")]
        private AttackRangedCooldownFruitlessChoice attackRangedCooldownFruitlessAction = AttackRangedCooldownFruitlessChoice.StandStillToDecide;

        [SerializeField, HideInEditorMode, DisableInPlayMode]
        private State pendingState;

        [SerializeField, HideInEditorMode, DisableInPlayMode]
        private State previousState;

        [SerializeField, HideInEditorMode, DisableInPlayMode]
        private State currentState;

        [SerializeField, HideInEditorMode, DisableInPlayMode]
        private State nextState;

        [SerializeField, HideInEditorMode, DisableInPlayMode]
        private List<CharacterOperator> meleeEngagedList;

        [SerializeField, HideInEditorMode, DisableInPlayMode]
        private bool prepareApproaching;

        [HideInEditorMode, DisableInPlayMode]
        private List<AttackDamageData> sentDamagePacks;

        [SerializeField, HideInEditorMode, DisableInPlayMode]
        private CharacterOperator attackTarget;

        [SerializeField, HideInEditorMode, DisableInPlayMode]
        private float stateTimer;

        private CharacterOperator owner;
        private CharacterBrain brain;
        private CharacterMotor motor;
        private int scanPointStep;
        private int scanPointOuter;
        private AttackSkillData attackSkill;
        private CharacterOperator previousAttackTarget;
        private bool recogniseAttackTarget;
        private bool attackTargetCarryFromRanged;

        public bool isMeleeEngageAvailable => meleeEngagedList.Count < meleeEngageAmount;

        public int meleeEngageAvailableCount => meleeEngageAmount - meleeEngagedList.Count;

        public bool isInited => owner != null;

        public bool underIdle
        {
            get
            {
                switch (currentState)
                {
                    case State.Idle:
                    case State.StanceIdle:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underWalk
        {
            get
            {
                switch (currentState)
                {
                    case State.Walk:
                    case State.StanceWalk:
                    case State.FightGoMelee:
                    case State.FightGoMeleeStandby:
                    case State.FightGoRangedStandby:
                    case State.Flee:
                    case State.Push:
                    case State.StancePush:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underAppear
        {
            get
            {
                switch (currentState)
                {
                    case State.None:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underPush
        {
            get
            {
                switch (currentState)
                {
                    case State.Push:
                    case State.StancePush:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underFlee
        {
            get
            {
                switch (currentState)
                {
                    case State.Flee:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underStance
        {
            get
            {
                switch (currentState)
                {
                    case State.StanceIdle:
                    case State.StanceWalk:
                    case State.StancePush:
                    case State.StanceStandby:
                    case State.StanceActivate:
                    case State.FightOnTheMark:
                    case State.FightGoMelee:
                    case State.FightGoMeleeStandby:
                    case State.FightMeleeReady:
                    case State.FightMeleeAttack:
                    case State.FightMeleeAttackHit:
                    case State.FightMeleeAttackCooldown:
                    case State.FightRangedReady:
                    case State.FightRangedBegin:
                    case State.FightRangedAim:
                    case State.FightRangedDraw:
                    case State.FightRangedFire:
                    case State.FightRangedAttack:
                    case State.FightRangedAttackCooldown:
                    case State.FightGoRangedStandby:
                    case State.FightSkillAttack:
                    case State.FightSkillReady:
                    case State.FightSkillAttackCooldown:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underStanceSwitching
        {
            get
            {
                switch (currentState)
                {
                    case State.StanceActivate:
                    case State.StanceDeactivate:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underFight
        {
            get
            {
                switch (currentState)
                {
                    case State.StanceStandby:
                    case State.FightOnTheMark:
                    case State.FightGoMelee:
                    case State.FightGoMeleeStandby:
                    case State.FightMeleeReady:
                    case State.FightMeleeAttack:
                    case State.FightMeleeAttackHit:
                    case State.FightMeleeAttackCooldown:
                    case State.FightRangedReady:
                    case State.FightRangedBegin:
                    case State.FightRangedAim:
                    case State.FightRangedDraw:
                    case State.FightRangedFire:
                    case State.FightRangedAttack:
                    case State.FightRangedAttackCooldown:
                    case State.FightGoRangedStandby:
                    case State.FightSkillAttack:
                    case State.FightSkillReady:
                    case State.FightSkillAttackCooldown:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underAttackCooldown
        {
            get
            {
                switch (currentState)
                {
                    case State.FightMeleeAttackCooldown:
                    case State.FightRangedAttackCooldown:
                    case State.FightSkillAttackCooldown:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underMeleeFight
        {
            get
            {
                switch (currentState)
                {
                    case State.StanceStandby:
                    case State.FightMeleeReady:
                    case State.FightMeleeAttack:
                    case State.FightMeleeAttackHit:
                    case State.FightMeleeAttackCooldown:
                    case State.FightGoMelee:
                    case State.FightGoMeleeStandby:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underMeleeAttacking
        {
            get
            {
                switch (currentState)
                {
                    case State.FightMeleeReady:
                    case State.FightMeleeAttack:
                    case State.FightMeleeAttackHit:
                    case State.FightMeleeAttackCooldown:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underSkillFight
        {
            get
            {
                switch (currentState)
                {
                    case State.StanceStandby:
                    case State.FightSkillReady:
                    case State.FightSkillAttack:
                    case State.FightSkillAttackCooldown:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underSkillAttacking
        {
            get
            {
                switch (currentState)
                {
                    case State.FightSkillReady:
                    case State.FightSkillAttack:
                    case State.FightSkillAttackCooldown:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underRangedFight
        {
            get
            {
                switch (currentState)
                {
                    case State.StanceStandby:
                    case State.FightRangedReady:
                    case State.FightRangedBegin:
                    case State.FightRangedAim:
                    case State.FightRangedDraw:
                    case State.FightRangedFire:
                    case State.FightRangedAttack:
                    case State.FightRangedAttackCooldown:
                    case State.FightGoRangedStandby:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underRangedAttacking
        {
            get
            {
                switch (currentState)
                {
                    case State.FightRangedReady:
                    case State.FightRangedBegin:
                    case State.FightRangedAim:
                    case State.FightRangedDraw:
                    case State.FightRangedFire:
                    case State.FightRangedAttack:
                    case State.FightRangedAttackCooldown:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool underPrepareApproach => prepareApproaching;

        public CharacterOperator currentTarget => attackTarget;
        public CharacterOperator previousTarget => previousAttackTarget;

        public bool HaveSameAttackTarget (CharacterOperator target)
        {
            if (!attackTarget)
                return false;
            if (target == attackTarget)
                return true;
            return false;
        }

        public void RememberAttackTarget ()
        {
            recogniseAttackTarget = true;
        }

        public bool HaveRangedAttack ()
        {
            return owner.rangedAttackRange > 0;
        }

        public bool HaveMeleeAttack ()
        {
            return owner.meleeAttackRange > 0;
        }

        public bool GetDropPoint (Transform location, out Transform result)
        {
            var stepInterval = GraphManager.instance.runtimeSettings.dropScanCircleAngleStep;
            var step = 0;
            var outer = 0;
            if (location == null)
                location = owner.agentTransform;
            result = owner.ScanPointsInCircle(owner.GetEmpty().transform, location, owner.agentTransform, owner.GetInteractRange(), stepInterval, 0, VerifyPoint, null,
                ref step, ref outer, out _);
            return result != null;

            float VerifyPoint (Transform source, Transform output, List<CharacterOperator> excluded)
            {
                if (CharacterOperator.CanStandOnLocation(output.position, owner.agentSize, excluded))
                {
                    if (GetPathDestinationPoint(source.position, output.position, out var destinationPoint, out var destinationDistance))
                    {
                        var destPoint = destinationPoint + ReTransform.smallUp;
                        if (!IsCharacterBlockBetweenMeleeTarget(destPoint, owner))
                        {
                            var startPos = owner.agentTransform.position;
                            var distance = Vector3.Distance(destPoint, startPos) + owner.agentSize;
                            if (owner.printLog)
                                Debug.DrawRay(startPos, destPoint - startPos, Color.red, 600);
                            if (!ReRaycast.CylinderCast(startPos, owner.agentSize, destPoint - startPos, distance, out _))
                                return destinationDistance;
                        }
                    }
                }

                return 0;
            }
        }

        public bool GetMeleeEngagePoint (CharacterOperator target, out Transform result)
        {
            var stepInterval = GraphManager.instance.runtimeSettings.meleeScanCircleAngleStep;
            var targetTrans = target.agentTransform;
            var excluding = new List<CharacterOperator>() {target, owner};
            var step = 0;
            var outer = 0;
            result = owner.ScanPointsInCircle(owner.GetEmpty().transform, owner.agentTransform, targetTrans, GetMeleeAttackStandDistance(target), stepInterval, 0, VerifyPoint, excluding, ref step,
                ref outer, out _);
            return result;

            float VerifyPoint (Transform source, Transform output, List<CharacterOperator> excluded)
            {
                if (CharacterOperator.CanStandOnLocation(output.position, owner.agentSize, excluded))
                {
                    if (GetPathDestinationPoint(source.position, output.position, out var destinationPoint, out var destinationDistance))
                    {
                        if (excluded[0].IsWithinMeleeEngageRange(destinationPoint, owner.meleeAttackRange))
                        {
                            if (!meleeRaycastDetection)
                                return destinationDistance;
                            if (!IsCharacterBlockBetweenMeleeTarget(destinationPoint + ReTransform.smallUp, excluded[0]))
                                return destinationDistance;
                        }
                    }
                }

                return 0;
            }
        }

        public bool GetRangedStandbyPoint (CharacterOperator target, out Transform result, out bool reachScanLimit)
        {
            var stepInterval = GraphManager.instance.runtimeSettings.rangedScanCircleAngleStep;
            var targetTrans = target.agentTransform;
            var excluding = new List<CharacterOperator>() {target, owner};
            var range = GetRangedStandbyDistance(target);
            if (rangedStandbyType == RangedStandbyTypeChoice.ChooseFromFurther)
            {
                //-- nothing have to implement here
            }
            else
            {
                var distance = Vector3.Distance(owner.agentTransform.position, targetTrans.position);
                if (distance < range)
                    range = distance;
            }

            result = owner.ScanPointsInCircle(owner.GetEmpty().transform, owner.agentTransform, targetTrans, range, stepInterval, -0.1f, VerifyPoint,
                excluding, ref scanPointStep, ref scanPointOuter, out reachScanLimit, timesLimit: rangedScanPointLimit);
            return result != null;

            float VerifyPoint (Transform source, Transform output, List<CharacterOperator> excluded)
            {
                if (CharacterOperator.CanStandOnLocation(output.position, owner.agentSize, excluded))
                {
                    if (GetPathDestinationPoint(source.position, output.position, out var destinationPoint, out var destinationDistance))
                    {
                        if (Vector3.Distance(output.position, destinationPoint) <= owner.agentSize)
                        {
                            if (excluded[0].IsWithinRangedAttackRange(destinationPoint, owner.rangedAttackRange))
                            {
                                if (!rangedLinearDetection)
                                    return destinationDistance;
                                if (!IsBlockProjectileToRangedTarget(destinationPoint + ReTransform.smallUp, attackTarget))
                                    return destinationDistance;
                            }
                        }
                    }
                }

                return 0;
            }
        }

        public bool IsTargetInRangedRange ()
        {
            return Vector3.Distance(owner.agentTransform.position, attackTarget.agentTransform.position) <= owner.rangedAttackRange;
        }

        public float GetRangedStandbyDistance (CharacterOperator target)
        {
            return owner.rangedAttackRange * rangedStandbyOffset;
        }

        public bool GetFleePoint (float distance, int direction, out Transform result)
        {
            distance = distance <= 0 || distance < owner.agentSize ? GetMeleeStandbyDistance(owner) : distance;
            var target = direction >= 1 && attackTarget ? attackTarget : owner;
            return GetCirclingPoint(target, distance, meleeScanPointLimit, direction, out result, out _, ref scanPointStep, ref scanPointOuter);
        }

        public bool GetMeleeStandbyPoint (CharacterOperator target, out Transform result, out bool reachScanLimit, ref int step, ref int outer, int scanLimit = 0)
        {
            return GetCirclingPoint(target, GetMeleeStandbyDistance(target), scanLimit, 0, out result, out reachScanLimit, ref step, ref outer);
        }

        public bool GetCirclingPoint (CharacterOperator target, float circleDistance, int scanLimit, int direction, out Transform result, out bool reachScanLimit, ref int step, ref int outer)
        {
            var stepInterval = GraphManager.instance.runtimeSettings.meleeScanCircleAngleStep;
            var targetTrans = target.agentTransform;
            var excluding = new List<CharacterOperator>() {target, owner};
            result = owner.ScanPointsInCircle(owner.GetEmpty().transform, owner.agentTransform, targetTrans, circleDistance, stepInterval, 0.1f, VerifyPoint, excluding, ref step, ref outer,
                out reachScanLimit, timesLimit: scanLimit, direction: direction);
            return result != null;

            float VerifyPoint (Transform source, Transform output, List<CharacterOperator> excluded)
            {
                if (CharacterOperator.CanStandOnLocation(output.position, owner.agentSize, excluded))
                {
                    if (GetPathDestinationPoint(source.position, output.position, out var destinationPoint, out var destinationDistance))
                        if (Vector3.Distance(output.position, destinationPoint) <= owner.agentSize)
                            return destinationDistance;
                }

                return 0;
            }
        }

        public bool IsTargetInMeleeRange ()
        {
            if (attackTarget == null)
                return false;
            return attackTarget.IsWithinMeleeEngageRange(owner.agentTransform.position, owner.meleeAttackRange);
        }

        public bool IsGoMeleeTowardSameTarget (CharacterOperator unit)
        {
            if (currentState == State.FightGoMelee)
            {
                if (HaveSameAttackTarget(unit))
                    return true;
            }

            return false;
        }

        public float GetMeleeAttackStandDistance (CharacterOperator target)
        {
            return target.meleeEngageDistance + owner.meleeAttackRange - motor.GetAgentStoppingDistance();
        }

        public float GetMeleeAttackDistance (CharacterOperator target)
        {
            return target.meleeEngageDistance + owner.meleeAttackRange;
        }

        public float GetMeleeStandbyDistance (CharacterOperator target)
        {
            return target.meleeStandbyDistance + owner.agentSize;
        }

        public float GetMeleeStandbyDistance ()
        {
            if (meleeStandbyDistance < owner.agentSize)
                return owner.agentSize;
            return meleeStandbyDistance;
        }

        public float GetMeleeEngageDistance ()
        {
            if (meleeEngageDistance < owner.agentSize)
                return owner.agentSize;
            return meleeEngageDistance;
        }

        public bool GetStatValue (string statName, out float value)
        {
            if (isInited)
            {
                if (!string.IsNullOrEmpty(statName))
                {
                    if (statName == StatType.STAT_MELEE_ATTACK_DISTANCE)
                    {
                        if (owner.statValueCharacterParam)
                        {
                            value = -1f;
                            var distance = owner.statValueCharacterParam.muscle.GetMeleeAttackDistance(owner);
                            if (Vector3.Distance(owner.agentTransform.position, owner.statValueCharacterParam.agentTransform.position) < distance)
                                value = 1f;
                            return true;
                        }
                    }
                    else if (statName == StatType.STAT_RANGED_ATTACK_REACHABLE)
                    {
                        if (owner.statValueCharacterParam)
                        {
                            value = -1f;
                            if (!owner.statValueCharacterParam.muscle.IsBlockProjectileToRangedTarget(owner.statValueCharacterParam.agentTransform.position + ReTransform.smallUp, owner))
                                value = 1f;
                            return true;
                        }
                    }
                }
            }

            value = 0f;
            return false;
        }

        public void ImmediateStance ()
        {
            if (pendingState == State.None)
            {
                if (currentState == State.Walk)
                    SetState(State.StanceWalk);
                else if (currentState == State.Push)
                    SetState(State.StancePush);
            }
            else if (pendingState == State.FightGoMelee)
            {
                SetState(State.FightGoMelee);
            }
            else if (pendingState == State.FightGoMeleeStandby)
            {
                SetState(State.FightGoMeleeStandby);
            }
            else if (pendingState == State.FightGoRangedStandby)
            {
                SetState(State.FightGoRangedStandby);
            }

            pendingState = State.None;
            owner.AdmitStance();
            owner.AdmitCompleteStance();
        }

        public void ImmediateUnstance ()
        {
            if (currentState == State.StanceWalk)
            {
                SetState(State.Walk);
            }
            else if (currentState == State.StancePush)
            {
                SetState(State.Push);
            }
            else if (currentState is State.FightGoMelee or State.FightGoMeleeStandby or State.FightGoRangedStandby)
            {
                pendingState = currentState;
                SetState(State.Walk);
            }

            UntoggleAllSkill();
            owner.AdmitUnstance();
            owner.AdmitCompleteUnstance();
        }

        public void InterruptForceNone ()
        {
            owner.PrintLog($"Muscle being interrupt force none");
            SetAttackTarget(null);
            MaxCooldownTimer();
            nextState = State.Idle;
        }

        public void InterruptForceIdle ()
        {
            owner.PrintLog($"Muscle being interrupt force idle");
            SetAttackTarget(null);
            MaxCooldownTimer();
            nextState = !underStance ? State.Idle : State.StanceIdle;
        }

        public void InterruptForceMoveAway ()
        {
            owner.PrintLog($"Muscle being interrupt force move away");
            SetAttackTarget(null);
            MaxCooldownTimer();
            nextState = !underStance ? State.Walk : State.StanceWalk;
        }

        public void InterruptForcePush ()
        {
            owner.PrintLog($"Muscle being interrupt force push");
            SetAttackTarget(null);
            MaxCooldownTimer();
            nextState = !underStance ? State.Push : State.StancePush;
        }

        public bool InterruptForceStop ()
        {
            if (currentState == State.FightMeleeAttack)
            {
                owner.PrintLog($"Muscle being interrupt force stop melee attack");
                SetState(State.FightMeleeAttackCooldown);
                return true;
            }

            if (currentState is State.FightRangedBegin or State.FightRangedAim or State.FightRangedDraw)
            {
                owner.PrintLog($"Muscle being interrupt force stop ranged attack");
                SetState(State.FightRangedAttackCooldown);
                return true;
            }

            return false;
        }

        public void InterruptFlee ()
        {
            owner.PrintLog($"Muscle being interrupt flee");
            SetAttackTarget(null);
            MaxCooldownTimer();
            nextState = State.Flee;
        }

        public void InterruptCancelFlee ()
        {
            owner.PrintLog($"Muscle being interrupt cancel flee");
            SetState(State.Idle);
        }

        public bool InterruptForceAttack (CharacterOperator target)
        {
            if (SetAttackTarget(target))
            {
                owner.PrintLog($"Muscle being interrupt force attack {target}");
                MaxCooldownTimer();
                nextState = State.FightOnTheMark;
                return true;
            }

            return false;
        }

        public void InterruptForceSkillAttack (AttackSkillData attackSkillData)
        {
            owner.PrintLog($"Muscle being interrupt force skill attack");
            attackSkill = attackSkillData;
            MaxCooldownTimer();
            nextState = State.FightSkillReady;
        }

        public void InterruptStance ()
        {
            SetAttackTarget(null);
            MaxCooldownTimer();
            pendingState = State.None;
            nextState = State.StanceActivate;
        }

        public void InterruptUnstance ()
        {
            SetAttackTarget(null);
            MaxCooldownTimer();
            pendingState = State.None;
            nextState = State.StanceDeactivate;
        }

        public bool AllowForceStop ()
        {
            if (currentState == State.FightMeleeAttack)
                return true;
            if (currentState is State.FightRangedBegin or State.FightRangedAim or State.FightRangedDraw)
                return true;
            return false;
        }

        public void ActivateMuscle ()
        {
            owner.PrintLog($"Muscle being activate");
            SetState(defaultStance ? State.StanceIdle : State.Idle);
        }

        public void ReachDestination ()
        {
            if (currentState == State.FightGoMelee)
            {
                motor.ResetMove();
                SetReachMeleeDestination();
            }
            else if (currentState == State.FightGoMeleeStandby)
            {
                SetReachMeleeStandbyDestination();
            }
            else if (currentState == State.FightGoRangedStandby)
            {
                SetReachRangedStandbyDestination();
            }
            else if (currentState == State.Walk)
            {
                nextState = State.Idle;
                if (pendingState != State.None)
                {
                    SetAttackTarget(null);
                    MaxCooldownTimer();
                    pendingState = State.None;
                }
            }
            else if (currentState == State.StanceWalk)
            {
                nextState = State.StanceIdle;
            }
            else if (currentState is State.Push)
            {
                SetState(State.Idle);
            }
            else if (currentState is State.StancePush)
            {
                SetState(State.StanceIdle);
            }
            else if (currentState is State.Flee)
            {
                owner.AdmitCompleteFlee();
                if (recogniseAttackTarget && previousAttackTarget && !previousAttackTarget.die)
                {
                    recogniseAttackTarget = false;
                    SetState(State.StanceStandby);
                    if (!InterruptForceAttack(previousAttackTarget))
                        SetState(State.Idle);
                }
                else
                {
                    SetState(State.Idle);
                }
            }
        }

        public float GetSkillStaminaConsume ()
        {
            if (attackSkill != null)
                return attackSkill.skillMuscle.pack.staminaSpent;
            return 0;
        }

        public void ReactAttack (AttackDamageData damageData)
        {
            if (damageData.isDeadAtAttack)
            {
                if (damageData.GetImpairedDamage() > 0)
                    LaunchAllSkill(TriggerNode.Type.CharacterGetHurt);
                LaunchAllSkill(TriggerNode.Type.CharacterDead);
                return;
            }

            if (damageData.GetImpairedDamage() > 0)
            {
                if (brain.targetAimPack != null && !owner.underOutOfTargetAim)
                {
                    var aimData = new TargetAimData();
                    aimData.Init(damageData, brain.targetAimPack);
                    aimData.ReactHurt();
                    var character = aimData.GetTarget();
                    aimData.Terminate();
                    if (character != null)
                    {
                        owner.PrintLog($"Muscle get hurt and set {character} as attack target");
                        owner.SetActionAttackTarget(character);
                    }
                }

                LaunchAllSkill(TriggerNode.Type.CharacterGetHurt);
            }
        }

        public void CooldownAttack (CharacterOperator.AttackType attackType)
        {
            if (attackType == CharacterOperator.AttackType.MeleeNormal && currentState is State.FightMeleeAttack or State.FightMeleeAttackHit)
            {
                SetState(State.FightMeleeAttackCooldown);
                if (meleeCooldownType == MeleeCooldownTypeChoice.StartAfterAttackFinish)
                    ResetCooldownTimer();
            }
            else if (attackType == CharacterOperator.AttackType.RangedNormal && currentState == State.FightRangedAttack)
            {
                SetState(State.FightRangedAttackCooldown);
                if (rangedCooldownType == RangedCooldownTypeChoice.StartAfterShot)
                    ResetCooldownTimer();
            }
        }

        public void StandbyRangedDraw (CharacterOperator.AttackType attackType)
        {
            if (attackType == CharacterOperator.AttackType.RangedNormal)
            {
                if (currentState == State.FightRangedBegin)
                {
                    SetState(State.FightRangedDraw);
                }
                else if (currentState == State.FightRangedAim)
                {
                    SetState(State.FightRangedFire);
                }
            }
        }

        public void EndAttack (CharacterOperator.AttackType attack, AttackDamageData damageData)
        {
            if (attack is CharacterOperator.AttackType.MeleeNormal or CharacterOperator.AttackType.RangedNormal)
                LaunchAllSkill(TriggerNode.Type.CharacterAttackEnd, damageData);
        }

        public AttackDamageData SendAttackPack (CharacterOperator.AttackType attackType)
        {
            sentDamagePacks ??= new List<AttackDamageData>();
            AttackDamageData attackData = null;
            if (attackType == CharacterOperator.AttackType.MeleeNormal && currentState == State.FightMeleeAttack)
            {
                if (meleeAttackDamagePack)
                {
                    if (attackTarget != null)
                    {
                        var missed = false;
                        if (meleeMissRange > 0)
                            missed = Vector3.Distance(owner.agentTransform.position, attackTarget.agentTransform.position) >= meleeMissRange + GetMeleeAttackDistance(attackTarget);
                        attackData = new AttackDamageData();
                        attackData.Init(owner, attackType, meleeAttackDamagePack, meleeUsePreAttackStat);
                        sentDamagePacks.Add(attackData);
                        attackTarget.ObtainAttackPack(attackData, missed);
                        SetState(State.FightMeleeAttackHit);
                    }
                }
                else
                {
                    ReDebug.LogWarning("Character Attack", "Missing melee attack damage pack at " + owner);
                }
            }
            else if (attackType == CharacterOperator.AttackType.RangedNormal && currentState == State.FightRangedAttack)
            {
                if (rangedProjectile != null && rangedProjectile.damagePack != null && rangedProjectile.ammo != null)
                {
                    if (attackTarget != null)
                    {
                        owner.Face(attackTarget.agentTransform.position);

                        attackData = new AttackDamageData();
                        attackData.Init(owner, attackType, rangedProjectile.damagePack, rangeUsePreAttackStat);
                        sentDamagePacks.Add(attackData);

                        var muzzle = owner.GetRangedMuzzle();
                        var go = owner.TakeFromPool(rangedProjectile.ammo, muzzle != null ? muzzle : owner.agentTransform, true);
                        if (go)
                        {
                            if (!go.TryGetComponent(out ProjectileController projectile))
                                projectile = go.AddComponent<ProjectileController>();
                            projectile.Init(attackData, attackTarget, rangedProjectile);
                        }
                    }
                }
                else
                {
                    ReDebug.LogWarning("Character Attack", "Missing ranged attack damage pack at " + owner);
                }
            }
            else if (attackType == CharacterOperator.AttackType.SkillNormal && currentState == State.FightSkillAttack)
            {
                if (attackSkill != null)
                {
                    attackData = attackSkill.damageData;
                    attackSkill.damageData.Init(owner, attackType, null, false);
                    sentDamagePacks.Add(attackSkill.damageData);
                    LaunchSkill(TriggerNode.Type.CharacterAttackSkill, attackSkill.damageData);
                }
                else
                {
                    ReDebug.LogWarning("Character Attack", "Missing attack skill pack at " + owner);
                }
            }

            return attackData;
        }

        public void RemoveAttackPack (AttackDamageData pack)
        {
            sentDamagePacks?.Remove(pack);
        }

        public int GetAttackPackCount ()
        {
            return sentDamagePacks?.Count ?? 0;
        }

        public void Init (CharacterOperator charOp, CharacterBrain charBrain, CharacterMotor charMotor, bool activate)
        {
            owner = charOp;
            brain = charBrain;
            motor = charMotor;
            if (activate)
                ActivateMuscle();
            meleeEngagedList = new List<CharacterOperator>();
            InitCooldown();
            InitSkill();
        }

        public void ChangeBehaviours (CharacterMuscle muscle, MultiTag changeFlags)
        {
            if (changeFlags.ContainAny(1, false))
                meleeEngageAmount = muscle.meleeEngageAmount;
            if (changeFlags.ContainAny(2, false))
                meleeEngageDistance = muscle.meleeEngageDistance;
            if (changeFlags.ContainAny(4, false))
                meleeStandbyDistance = muscle.meleeStandbyDistance;
            if (changeFlags.ContainAny(8, false))
                meleeScanPointLimit = muscle.meleeScanPointLimit;
            if (changeFlags.ContainAny(16, false))
                meleeScanDuration = muscle.meleeScanDuration;
            if (changeFlags.ContainAny(32, false))
                meleeStandbySingleApproach = muscle.meleeStandbySingleApproach;
            if (changeFlags.ContainAny(64, false))
                meleeRaycastDetection = muscle.meleeRaycastDetection;
            if (changeFlags.ContainAny(128, false))
                meleeCooldownType = muscle.meleeCooldownType;
            if (changeFlags.ContainAny(256, false))
                meleeAttackDamagePack = muscle.meleeAttackDamagePack;
            if (changeFlags.ContainAny(512, false))
                meleeUsePreAttackStat = muscle.meleeUsePreAttackStat;
            if (changeFlags.ContainAny(1024, false))
                meleeMissRange = muscle.meleeMissRange;
            if (changeFlags.ContainAny(2048, false))
                rangedLinearDetection = muscle.rangedLinearDetection;
            if (changeFlags.ContainAny(4096, false))
                rangedLinearDetectSize = muscle.rangedLinearDetectSize;
            if (changeFlags.ContainAny(8192, false))
                rangedCooldownType = muscle.rangedCooldownType;
            if (changeFlags.ContainAny(16384, false))
                rangedProjectile = muscle.rangedProjectile;
            if (changeFlags.ContainAny(32768, false))
                rangedStandbyType = muscle.rangedStandbyType;
            if (changeFlags.ContainAny(65536, false))
                rangedScanPointLimit = muscle.rangedScanPointLimit;
            if (changeFlags.ContainAny(131072, false))
                rangedScanDuration = muscle.rangedScanDuration;
            if (changeFlags.ContainAny(262144, false))
                rangedStandbyOffset = muscle.rangedStandbyOffset;
            if (changeFlags.ContainAny(524288, false))
                rangedFaceTarget = muscle.rangedFaceTarget;
            if (changeFlags.ContainAny(1048576, false))
                rangeUsePreAttackStat = muscle.rangeUsePreAttackStat;
            if (changeFlags.ContainAny(2097152, false))
                rangeMeleeOnEngage = muscle.rangeMeleeOnEngage;
            if (changeFlags.ContainAny(4194304, false))
                attackOnTheMarkFruitlessAction = muscle.attackOnTheMarkFruitlessAction;
            if (changeFlags.ContainAny(8388608, false))
                attackGoTowardFruitlessAction = muscle.attackGoTowardFruitlessAction;
            if (changeFlags.ContainAny(16777216, false))
                attackGoStandbyFruitlessAction = muscle.attackGoStandbyFruitlessAction;
            if (changeFlags.ContainAny(33554432, false))
                attackGoMeleeCrashAction = muscle.attackGoMeleeCrashAction;
            if (changeFlags.ContainAny(67108864, false))
                attackMeleeReadyFruitlessAction = muscle.attackMeleeReadyFruitlessAction;
            if (changeFlags.ContainAny(134217728, false))
                attackRangedReadyFruitlessAction = muscle.attackRangedReadyFruitlessAction;
        }

        public void Tick (float deltaTime)
        {
            if (!owner.die)
            {
                ProcessStateAssign(false, deltaTime);
                if (ProcessStateUpdate(deltaTime))
                    ProcessStateAssign(true, deltaTime);

                UpdateSkill(deltaTime);
            }
            else 
            {
                if (attackSkill != null)
                {
                    attackSkill.damageData?.Terminate();
                    attackSkill.Terminate();
                    attackSkill = null;
                }

                if (owner.IsPendingKnockAction())
                {
                    ProcessStateAssign(false, deltaTime);
                    motor.ResumeMove();
                }
            }
        }

        private void ProcessStateAssign (bool reUpdate, float deltaTime)
        {
            if (currentState is State.Idle or State.Walk or State.StanceIdle or State.StanceWalk or State.StanceStandby)
            {
                AssignNextState(reUpdate, deltaTime);
            }
            else if (IsStateInterruptible(currentState))
            {
                if (currentState is State.FightMeleeReady or State.FightRangedReady or State.FightOnTheMark)
                    AssignNextState(reUpdate, deltaTime);
                else if (owner.IsInterruptiblePendingAction())
                    AssignNextState(reUpdate, deltaTime);
            }
            else if (owner.IsPendingUrgentAction())
            {
                AssignNextState(reUpdate, deltaTime);
            }
        }

        private void AssignNextState (bool reUpdate, float deltaTime)
        {
            if (!reUpdate)
            {
                var actionExecuted = owner.ExecutePendingAction();
                if (actionExecuted)
                {
                    if (underFight)
                        LaunchAllSkill(TriggerNode.Type.CharacterAttackBreak);
                }
                else
                {
                    DetectSkillAttack(deltaTime);
                }
            }

            if (nextState == State.None)
                return;
            if (nextState == currentState)
            {
                nextState = State.None;
                return;
            }

            switch (nextState)
            {
                case State.FightOnTheMark when !underStance:
                    SetState(State.StanceActivate);
                    break;
                case State.Idle or State.Walk or State.Push or State.Flee:
                case State.StanceIdle or State.StanceWalk or State.StancePush or State.StanceStandby:
                case State.FightOnTheMark:
                case State.FightGoMelee or State.FightGoMeleeStandby or State.FightMeleeReady or State.FightMeleeAttack or State.FightMeleeAttackHit:
                case State.FightRangedBegin or State.FightGoRangedStandby or State.FightRangedReady:
                case State.FightSkillReady or State.FightSkillAttack:
                    SetState(nextState);
                    nextState = State.None;
                    break;
                case State.StanceActivate:
                    if (!underStance)
                        SetState(nextState);
                    nextState = State.None;
                    break;
                case State.StanceDeactivate:
                    if (underStance)
                        SetState(nextState);
                    nextState = State.None;
                    break;
            }
        }

        private bool ProcessStateUpdate (float deltaTime)
        {
            UpdateCooldown(deltaTime);

            if (currentState is State.Idle or State.StanceIdle)
            {
                if (nextState == State.None && brain.targetAimPack && !owner.underOutOfTargetAim)
                {
                    if ((brain.searchTargetAtIdle && currentState == State.Idle) || brain.searchTargetAtStanceIdle && currentState == State.StanceIdle)
                    {
                        var checkGuard = true;
                        if (stateTimer >= brain.searchTargetInterval)
                        {
                            stateTimer = 0;
                            var aimData = new TargetAimData();
                            aimData.Init(owner, brain.targetAimPack);
                            aimData.Choose();
                            var character = aimData.GetTarget();
                            aimData.Terminate();
                            if (character)
                            {
                                owner.PrintLog($"Muscle choose {character} as attack target");
                                owner.SetActionAttackTarget(character);
                                checkGuard = false;
                            }
                        }
                        else
                            stateTimer += deltaTime;

                        if (checkGuard && owner.asGuardian)
                            MoveToGuardPoint();
                    }
                }
            }
            else if (currentState == State.StanceActivate)
            {
                stateTimer += deltaTime;
                if (stateTimer >= owner.stanceDuration)
                {
                    SetState(State.StanceIdle);
                    owner.AdmitCompleteStance();
                    return true;
                }
            }
            else if (currentState == State.StanceDeactivate)
            {
                stateTimer += deltaTime;
                if (stateTimer >= owner.unStanceDuration)
                {
                    SetState(State.Idle);
                    owner.AdmitCompleteUnstance();
                    return true;
                }
            }
            else if (currentState == State.FightOnTheMark)
            {
                if (IsTargetDead())
                {
                    SetAttackTarget(null);
                    SetState(State.StanceIdle);
                    return true;
                }

                if (DecideFightOnTheMark(deltaTime))
                    return true;
            }
            else if (currentState == State.FightGoMelee)
            {
                if (IsTargetDead() || IsTargetOutOfSight())
                {
                    motor.CancelMove();
                    SetAttackTarget(null);
                    SetState(State.StanceIdle);
                    return true;
                }

                if (motor.IsWithinDestination(owner.agentSize))
                {
                    stateTimer += deltaTime;
                    if (stateTimer > 0.2f)
                    {
                        motor.CancelMove();
                        motor.ResetMove();
                        SetReachMeleeDestination();
                        return true;
                    }
                }

                bool notAbleGoToward = false;
                if (!attackTarget.muscle.isMeleeEngageAvailable) //-- Handle melee engage point occupied while go toward it
                {
                    notAbleGoToward = true;
                }
                else if (!motor.IsTargetReachable(attackTarget)) //-- Handle target melee stand point is not reachable
                {
#if DEVELOPMENT_BUILD || (UNITY_EDITOR && REGRAPH_DEV_DEBUG)
                    owner.PrintLog($"Muscle go melee attack fruitless due to target not reachable.");
#endif
                    notAbleGoToward = true;
                }

                if (notAbleGoToward)
                {
                    if (attackGoTowardFruitlessAction == AttackGoTowardFruitlessChoice.ContinueTowardClosest)
                    {
                        //-- nothing need to be implement here
                    }
                    else if (attackGoTowardFruitlessAction == AttackGoTowardFruitlessChoice.StandStillToDecide)
                    {
                        motor.CancelMove();
                        SetState(State.StanceStandby);
                        if (nextState == State.None)
                            nextState = State.FightOnTheMark;
                        return true;
                    }
                    else if (attackGoTowardFruitlessAction == AttackGoTowardFruitlessChoice.TowardAndDecide)
                    {
#if DEVELOPMENT_BUILD || (UNITY_EDITOR && REGRAPH_DEV_DEBUG)
                        owner.PrintLog($"Muscle go melee attack fruitless then choose to go toward while decide next action.");
#endif
                        SetState(State.StanceStandby);
                        if (nextState == State.None)
                            nextState = State.FightOnTheMark;
                        return true;
                    }
                }
            }
            else if (currentState == State.FightGoMeleeStandby)
            {
                if (IsTargetDead() || IsTargetOutOfSight())
                {
                    motor.CancelMove();
                    SetAttackTarget(null);
                    SetState(State.StanceIdle);
                    return true;
                }

                //-- Handle target suddenly have something blocking the reachable way
                if (!motor.IsMoveDestinationReachable())
                {
                    if (attackGoStandbyFruitlessAction == AttackGoStandbyFruitlessChoice.StandStillAndWait)
                    {
                        motor.CancelMove();
                        SetState(State.StanceStandby);
                        if (nextState == State.None)
                            nextState = State.FightOnTheMark;
                        return true;
                    }
                }

                if (nextState == State.None)
                {
                    if (attackTarget.muscle.isMeleeEngageAvailable)
                    {
                        SetState(State.StanceStandby);
                        nextState = State.FightOnTheMark;
                        return true;
                    }
                }
                else
                {
                    SetState(State.StanceStandby);
                    return true;
                }
            }
            else if (currentState == State.FightGoRangedStandby)
            {
                if (IsTargetDead() || IsTargetOutOfSight())
                {
                    motor.CancelMove();
                    SetAttackTarget(null);
                    SetState(State.StanceIdle);
                    return true;
                }

                //-- Handle target suddenly have something blocking the reachable way
                if (!motor.IsMoveDestinationReachable())
                {
                    if (attackGoStandbyFruitlessAction == AttackGoStandbyFruitlessChoice.StandStillAndWait)
                    {
                        motor.CancelMove();
                        SetState(State.StanceStandby);
                        if (nextState == State.None)
                            nextState = State.FightOnTheMark;
                        return true;
                    }
                }

                if (nextState != State.None)
                {
                    SetState(State.StanceStandby);
                    return true;
                }
            }
            else if (currentState == State.FightRangedReady)
            {
                if (IsRangedTargetGone())
                {
                    if (!IsTargetDead())
                    {
                        if (attackRangedReadyFruitlessAction == AttackRangedReadyFruitlessChoice.StandStillToDecide)
                        {
                            SetAttackTarget(null);
                        }
                        else if (attackRangedReadyFruitlessAction == AttackRangedReadyFruitlessChoice.ContinueAttack)
                        {
                            //-- nothing need to be implement here
                        }
                    }

                    SetState(State.StanceStandby);
                    if (nextState == State.None)
                        nextState = State.FightOnTheMark;
                    return true;
                }

                if (CheckCooldown(owner.rangedAttackCooldown))
                {
                    if (HaveMeleeAttack() && rangeMeleeOnCooldown)
                    {
                        if (Vector3.Distance(owner.agentTransform.position, attackTarget.agentTransform.position) < GetMeleeAttackDistance(attackTarget))
                        {
                            if (attackTarget.muscle.isMeleeEngageAvailable)
                            {
                                var setReach = false;
                                if (!meleeRaycastDetection)
                                    setReach = true;
                                else if (!IsCharacterBlockBetweenMeleeTarget(owner.agentTransform.position + ReTransform.smallUp, attackTarget))
                                    setReach = true;
                                if (setReach)
                                {
                                    attackTargetCarryFromRanged = true;
                                    SetReachMeleeDestination();
                                    return true;
                                }
                            }
                        }
                    }

                    SetState(State.StanceStandby);
                    if (nextState == State.None)
                    {
                        if (rangedProjectile)
                        {
                            FaceRangedTarget();
                            nextState = State.FightRangedBegin;
                        }
                        else
                        {
                            nextState = State.FightOnTheMark;
                        }

                        return true;
                    }
                }
                else
                {
                    FaceRangedTarget();
                }
            }
            else if (currentState is State.FightRangedBegin or State.FightRangedAim or State.FightRangedDraw)
            {
                if (IsRangedTargetGone())
                {
                    if (!IsTargetDead())
                    {
                        if (attackRangedReadyFruitlessAction == AttackRangedReadyFruitlessChoice.StandStillToDecide)
                        {
                            owner.AdmitAttackCancel();
                            SetAttackTarget(null);
                        }
                        else if (attackRangedReadyFruitlessAction == AttackRangedReadyFruitlessChoice.ContinueAttack)
                        {
                            //-- nothing need to be implement here
                        }
                    }

                    SetState(State.StanceStandby);
                    if (nextState == State.None)
                        nextState = State.FightOnTheMark;
                    return true;
                }

                FaceRangedTarget();

                stateTimer += deltaTime;
                if (stateTimer >= owner.rangedAimDuration)
                {
                    if (currentState == State.FightRangedBegin)
                    {
                        SetState(State.FightRangedAim);
                        return true;
                    }

                    if (currentState == State.FightRangedDraw)
                    {
                        var proceed = true;
                        if (brain.canStaminaConsume)
                        {
                            proceed = false;
                            var require = brain.GetStatStaminaConsume(Stamina.Type.RangedAttack);
                            if (owner.currentStamina >= require)
                                proceed = true;
                        }

                        if (owner.underOutOfAttack)
                            proceed = false;

                        if (proceed)
                        {
                            SetState(State.FightRangedFire);
                            return true;
                        }
                    }
                }
            }
            else if (currentState == State.FightRangedFire)
            {
                SetState(State.FightRangedAttack);
                return true;
            }
            else if (currentState == State.FightRangedAttack)
            {
                FaceRangedTarget();
            }
            else if (currentState == State.FightRangedAttackCooldown)
            {
                FaceRangedTarget();

                if (IsRangedTargetGone())
                {
                    if (attackRangedCooldownFruitlessAction == AttackRangedCooldownFruitlessChoice.StandStillToDecide)
                    {
                        SetAttackTarget(null);
                        SetState(State.StanceStandby);
                        if (nextState == State.None)
                            nextState = State.FightOnTheMark;
                        return true;
                    }
                    else if (attackRangedCooldownFruitlessAction == AttackRangedCooldownFruitlessChoice.StandStillAndWait)
                    {
                        //-- nothing need to be implement here
                    }
                }

                if (rangedCooldownType is RangedCooldownTypeChoice.StartAfterShot or RangedCooldownTypeChoice.StartAfterDraw)
                {
                    if (CheckCooldown(owner.rangedAttackCooldown))
                    {
                        SetState(State.StanceStandby);
                        if (nextState == State.None)
                            nextState = State.FightRangedReady;
                        return true;
                    }
                }
            }
            else if (currentState == State.FightMeleeReady)
            {
                if (IsMeleeTargetGone())
                {
                    var backToStandby = false;
                    if (!IsTargetDead())
                    {
                        if (!CheckCooldown(owner.meleeAttackCooldown))
                        {
                            if (attackMeleeCooldownFruitlessAction == AttackMeleeCooldownFruitlessChoice.StandStillToDecide)
                            {
                                backToStandby = true;
                            }
                            else if (attackMeleeCooldownFruitlessAction == AttackMeleeCooldownFruitlessChoice.StandStillAndWait)
                            {
                                //-- nothing need to be implement here
                            }
                        }
                        else
                        {
                            if (attackMeleeReadyFruitlessAction == AttackMeleeReadyFruitlessChoice.StandStillToDecide)
                            {
                                backToStandby = true;
                            }
                            else if (attackMeleeReadyFruitlessAction == AttackMeleeReadyFruitlessChoice.ContinueAttack)
                            {
                                //-- nothing need to be implement here
                            }
                        }
                    }
                    else
                    {
                        backToStandby = true;
                    }

                    if (backToStandby)
                    {
                        if (!attackTargetCarryFromRanged)
                            SetAttackTarget(null);
                        SetState(State.StanceStandby);
                        if (nextState == State.None)
                            nextState = State.FightOnTheMark;
                        return true;
                    }
                }

                var proceed = true;
                if (brain.canStaminaConsume)
                {
                    proceed = false;
                    var require = brain.GetStatStaminaConsume(Stamina.Type.MeleeAttack);
                    if (owner.currentStamina >= require)
                        proceed = true;
                }

                if (!CheckCooldown(owner.meleeAttackCooldown))
                    proceed = false;

                if (owner.underOutOfAttack)
                    proceed = false;

                if (proceed)
                {
                    SetState(State.StanceStandby);
                    if (nextState == State.None)
                        if (owner.meleeAttackSpeed > 0)
                            nextState = State.FightMeleeAttack;
                    return true;
                }
            }
            else if (currentState is State.FightMeleeAttack or State.FightMeleeAttackHit)
            {
                if (meleeCooldownType == MeleeCooldownTypeChoice.StartFromAttackLaunch)
                {
                    if (CheckCooldown(owner.meleeAttackCooldown))
                    {
                        if (IsMeleeTargetGone())
                        {
                            if (!attackTargetCarryFromRanged)
                                SetAttackTarget(null);
                            SetState(State.StanceStandby);
                            if (nextState == State.None)
                                nextState = State.FightOnTheMark;
                            return true;
                        }

                        SetState(State.StanceStandby);
                        if (nextState == State.None)
                            nextState = State.FightMeleeReady;
                        return true;
                    }
                }
            }
            else if (currentState == State.FightMeleeAttackCooldown)
            {
                if (IsMeleeTargetGone())
                {
                    if (attackMeleeCooldownFruitlessAction == AttackMeleeCooldownFruitlessChoice.StandStillToDecide)
                    {
                        if (!attackTargetCarryFromRanged)
                            SetAttackTarget(null);
                        SetState(State.StanceStandby);
                        if (nextState == State.None)
                            nextState = State.FightOnTheMark;
                        return true;
                    }
                    else if (attackMeleeCooldownFruitlessAction == AttackMeleeCooldownFruitlessChoice.StandStillAndWait)
                    {
                        //-- nothing need to be implement here
                    }
                }

                if (meleeCooldownType is MeleeCooldownTypeChoice.StartFromAttackLaunch or MeleeCooldownTypeChoice.StartAfterAttackFinish)
                {
                    if (CheckCooldown(owner.meleeAttackCooldown))
                    {
                        if (IsMeleeTargetGone())
                        {
                            SetAttackTarget(null);
                            SetState(State.StanceStandby);
                            if (nextState == State.None)
                                nextState = State.FightOnTheMark;
                            return true;
                        }

                        SetState(State.StanceStandby);
                        if (nextState is State.None)
                            nextState = State.FightMeleeReady;
                        return true;
                    }
                }
            }
            else if (currentState == State.FightSkillReady)
            {
                var proceed = true;
                var requireStamina = GetSkillStaminaConsume();
                if (requireStamina > 0)
                    proceed = owner.currentStamina >= requireStamina;
                if (owner.underOutOfSkill)
                    proceed = false;
                if (proceed)
                {
                    SetState(State.StanceStandby);
                    if (nextState == State.None)
                        nextState = State.FightSkillAttack;
                    return true;
                }
            }
            else if (currentState == State.FightSkillAttack)
            {
                if (DetectSkillComplete())
                {
                    SetState(State.FightSkillAttackCooldown);
                    ResetCooldownTimer();
                    owner.CompleteAttack(CharacterOperator.AttackType.SkillNormal, attackSkill.damageData);
                    return true;
                }
            }
            else if (currentState == State.FightSkillAttackCooldown)
            {
                if (CheckCooldown(attackSkill.skillMuscle.pack.cooldownTime))
                {
                    SetState(State.StanceStandby);
                    if (nextState == State.None)
                    {
                        if (attackTarget == null)
                            nextState = State.StanceIdle;
                        else
                            nextState = State.FightOnTheMark;
                    }

                    attackSkill.damageData.Terminate();
                    attackSkill.Terminate();
                    return true;
                }
            }

            return false;
        }

        public void SetAttackMeleeReadyFruitlessAction (AttackMeleeReadyFruitlessChoice choice)
        {
            attackMeleeReadyFruitlessAction = choice;
        }

        public void SetAttackRangedReadyFruitlessAction (AttackRangedReadyFruitlessChoice choice)
        {
            attackRangedReadyFruitlessAction = choice;
        }

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private bool DecideFightOnTheMark (float deltaTime)
        {
            if (HaveMeleeAttack())
            {
                var engageMelee = !(HaveRangedAttack() && !rangeMeleeOnEngage);
                if (engageMelee)
                {
                    //-- direct set reach destination when command a character attack a target that already within melee max distance 
                    if (Vector3.Distance(owner.agentTransform.position, attackTarget.agentTransform.position) < GetMeleeAttackDistance(attackTarget))
                    {
                        if (attackTarget.muscle.isMeleeEngageAvailable)
                        {
                            var setReach = false;
                            if (!meleeRaycastDetection)
                                setReach = true;
                            else if (!IsCharacterBlockBetweenMeleeTarget(owner.agentTransform.position + ReTransform.smallUp, attackTarget))
                                setReach = true;
                            if (setReach)
                            {
                                SetReachMeleeDestination();
                                return true;
                            }
                        }
                        else if (previousState != State.FightGoMeleeStandby)
                        {
                            if (GetMeleeStandbyPoint(attackTarget, out var trans, out var pending, ref scanPointStep, ref scanPointOuter, meleeScanPointLimit))
                            {
                                MoveToMeleeStandby(trans.position);
                                return true;
                            }

                            if (pending)
                                return HandlePending(meleeScanDuration, "Muscle hold a frame to optimise melee standby point scan performance.");
                        }
                    }
                }
            }

            if (HaveRangedAttack())
            {
                if (IsTargetInRangedRange())
                {
                    var proceed = true;
                    if (rangedLinearDetection)
                        if (IsBlockProjectileToRangedTarget(owner.agentTransform.position + ReTransform.smallUp, attackTarget))
                            proceed = false;
                    if (proceed)
                    {
                        SetState(State.FightRangedReady);
                        return true;
                    }
                }

                if (GetRangedStandbyPoint(attackTarget, out var trans, out var pending))
                {
                    MoveToRangedStandby(trans.position);
                    return true;
                }

                if (pending)
                    HandlePending(rangedScanDuration, "Muscle hold a frame to optimise ranged standby point scan performance.");
            }

            if (HaveMeleeAttack())
            {
                if (attackTarget.muscle.isMeleeEngageAvailable)
                {
                    if (GetMeleeEngagePoint(attackTarget, out var trans))
                    {
                        var goTowardState = 1;
                        if (previousState == State.FightGoMeleeStandby)
                        {
                            if (meleeStandbySingleApproach)
                            {
                                goTowardState = 0;
                                var currentApproaching = CharacterOperator.GetPrepareApproachingCount(attackTarget);
                                if (currentApproaching < attackTarget.muscle.meleeEngageAvailableCount)
                                    goTowardState = 2;
                            }
                        }

                        if (goTowardState > 0)
                        {
                            if (owner.asGuardian && !owner.IsWithinGuardLocation(trans.position))
                            {
                                SetState(State.StanceIdle);
                                MoveToGuardPoint();
                                return true;
                            }
                            else
                            {
                                if (attackTarget.IsGoMeleeToward(owner))
                                {
                                    if (attackGoMeleeCrashAction == AttackGoMeleeCrashChoice.StandStillAndWait)
                                    {
                                        //-- keeping in FightOnTheMark state until target no longer go melee toward this unit 
                                    }
                                }
                                else
                                {
                                    SetState(State.StanceIdle);
                                    motor.SetDestination(trans.position);
                                    if (nextState == State.None)
                                    {
                                        nextState = State.FightGoMelee;
                                        if (goTowardState == 2)
                                            prepareApproaching = true;
                                    }

                                    return true;
                                }
                            }
                        }
                    }
                    else
                    {
                        //-- Handle situation of no reachable melee engage point
                        if (attackOnTheMarkFruitlessAction == AttackOnTheMarkFruitlessChoice.StandStillAndWait)
                        {
                            //-- nothing need to be implement here
                        }
                        else if (attackOnTheMarkFruitlessAction == AttackOnTheMarkFruitlessChoice.TowardStandbyAndWait)
                        {
                            if (previousState != State.FightGoMeleeStandby)
                            {
                                if (GetMeleeStandbyPoint(attackTarget, out trans, out var pending, ref scanPointStep, ref scanPointOuter, meleeScanPointLimit))
                                {
                                    MoveToMeleeStandby(trans.position);
                                    return true;
                                }

                                if (pending)
                                    HandlePending(meleeScanDuration, "Muscle hold a frame to optimise melee standby point scan performance.");
                            }
                        }
                    }
                }
                else
                {
                    //-- Handle situation of no more melee engage point available
                    if (attackOnTheMarkFruitlessAction == AttackOnTheMarkFruitlessChoice.StandStillAndWait)
                    {
                        //-- nothing need to be implement here
                    }
                    else if (attackOnTheMarkFruitlessAction == AttackOnTheMarkFruitlessChoice.TowardStandbyAndWait)
                    {
                        if (previousState != State.FightGoMeleeStandby)
                        {
                            if (GetMeleeStandbyPoint(attackTarget, out var trans, out var pending, ref scanPointStep, ref scanPointOuter, meleeScanPointLimit))
                            {
                                MoveToMeleeStandby(trans.position);
                                return true;
                            }

                            if (pending)
                                HandlePending(meleeScanDuration, "Muscle hold a frame to optimise melee standby point scan performance.");
                        }
                        else
                        {
                            motor.CancelMove();
                            owner.Face(attackTarget.agentTransform.position);
                        }
                    }
                }
            }

            return false;

            bool HandlePending (float scanDuration, string logMessage)
            {
                stateTimer += deltaTime;
                if (stateTimer >= scanDuration)
                {
                    SetAttackTarget(null);
                    SetState(State.StanceIdle);
                    return true;
                }
                else
                {
                    owner.PrintLog(logMessage);
                    return false;
                }
            }
        }

        private void FaceRangedTarget ()
        {
            if (rangedFaceTarget && attackTarget != null)
                owner.Face(attackTarget.agentTransform.position);
        }

        private bool IsTargetDead ()
        {
            return attackTarget == null || attackTarget.die;
        }

        private bool IsTargetOutOfSight ()
        {
            return attackTarget == null || attackTarget.underOutOfGoSight;
        }

        private bool IsRangedTargetGone ()
        {
            return IsTargetDead() || !IsTargetInRangedRange();
        }

        private bool IsMeleeTargetGone ()
        {
            return IsTargetDead() || !IsTargetInMeleeRange();
        }

        private void MoveToGuardPoint ()
        {
            SetAttackTarget(null);
            if (brain.guardReturn)
            {
                var distance = Vector3.Distance(owner.agentTransform.position, brain.guardLocation);
                if (distance > owner.agentSize)
                {
                    if (Vector3.Distance(brain.guardLocation, motor.moveDestination) > 0)
                    {
                        if (brain.guardReturnRange <= 0 || distance >= brain.guardReturnRange)
                        {
                            motor.SetDestination(brain.guardLocation);
                        }
                        else if (currentState is State.Idle or State.StanceIdle)
                        {
                            motor.CancelMove();
                        }
                    }
                }
            }
        }

        private void MoveToMeleeStandby (Vector3 standbyLocation)
        {
            SetState(State.StanceIdle);
            if (nextState is State.None or State.FightGoMelee)
            {
                if (Vector3.Distance(owner.agentTransform.position, standbyLocation) > owner.agentSize)
                {
                    if (owner.asGuardian && !owner.IsWithinGuardLocation(standbyLocation))
                    {
                        MoveToGuardPoint();
                    }
                    else
                    {
                        motor.SetDestination(standbyLocation);
                        nextState = State.FightGoMeleeStandby;
                    }
                }
                else
                {
                    SetReachMeleeStandbyDestination();
                }
            }
        }

        private void MoveToRangedStandby (Vector3 standbyLocation)
        {
            SetState(State.StanceIdle);
            if (nextState is State.None)
            {
                if (Vector3.Distance(owner.agentTransform.position, standbyLocation) > owner.agentSize)
                {
                    if (owner.asGuardian && !owner.IsWithinGuardLocation(standbyLocation))
                    {
                        MoveToGuardPoint();
                    }
                    else
                    {
                        motor.SetDestination(standbyLocation);
                        nextState = State.FightGoRangedStandby;
                    }
                }
                else
                {
                    SetReachRangedStandbyDestination();
                }
            }
        }

        private void SetReachRangedStandbyDestination ()
        {
            SetState(State.FightOnTheMark);
        }

        private void SetReachMeleeDestination ()
        {
            if (attackTarget)
                owner.Face(attackTarget.agentTransform.position);
            SetState(State.FightMeleeReady);
            if (attackTarget)
                attackTarget.muscle.RegisterMeleeEngaged(owner);
        }

        private void SetReachMeleeStandbyDestination ()
        {
            if (attackTarget)
                owner.Face(attackTarget.agentTransform.position);
            SetState(State.FightOnTheMark);
        }

        private bool SetAttackTarget (CharacterOperator target)
        {
            if (!attackTarget && !target)
                return false;
            if (attackTarget && attackTarget == target)
                return false;
            //-- Handle situation of attackTarget is already have someone in set
            if (attackTarget)
                attackTarget.muscle.UnregisterMeleeEngaged(owner);
            previousAttackTarget = attackTarget;
            attackTarget = target;
            attackTargetCarryFromRanged = false;
            return true;
        }

        private bool RegisterMeleeEngaged (CharacterOperator engagedUnit)
        {
            if (meleeEngagedList.Count < meleeEngageAmount)
            {
                if (!meleeEngagedList.Contains(engagedUnit))
                {
                    meleeEngagedList.Add(engagedUnit);
                    return true;
                }
            }

            return false;
        }

        private bool UnregisterMeleeEngaged (CharacterOperator engagedUnit)
        {
            if (meleeEngagedList.Contains(engagedUnit))
            {
                meleeEngagedList.Remove(engagedUnit);
                return true;
            }

            return false;
        }

        private void SetState (State state)
        {
            previousState = currentState;
            currentState = state;

#if DEVELOPMENT_BUILD || (UNITY_EDITOR && REGRAPH_DEV_DEBUG)
            owner.PrintLog($"Muscle state change from  {previousState} to {currentState}, next state is {nextState}");
#endif

            motor.CancelChase();

            if (previousState == State.FightGoMelee)
                prepareApproaching = false;

            if (currentState is State.Idle or State.StanceIdle)
            {
                stateTimer = brain.searchTargetInterval;
            }
            else if (currentState == State.StanceActivate)
            {
                stateTimer = 0;
                owner.AdmitStance();
            }
            else if (currentState == State.StanceDeactivate)
            {
                stateTimer = 0;
                UntoggleAllSkill();
                owner.AdmitUnstance();
            }
            else if (currentState == State.FightGoMelee)
            {
                LaunchAllSkill(TriggerNode.Type.CharacterAttackGoMelee);
                stateTimer = 0;
                owner.AdmitApproachingMeleeAttack();
            }
            else if (currentState is State.FightSkillAttack)
            {
                owner.AdmitAttackSkill();
            }
            else if (currentState is State.FightMeleeAttack)
            {
                owner.Face(attackTarget.agentTransform.position);
                owner.AdmitAttackMelee();
                if (meleeCooldownType == MeleeCooldownTypeChoice.StartFromAttackLaunch)
                    ResetCooldownTimer();
            }
            else if (currentState is State.FightRangedAttackCooldown) { }
            else if (currentState is State.FightRangedBegin)
            {
                stateTimer = 0;
                owner.AdmitAttackRangedDraw();
            }
            else if (currentState == State.FightRangedFire)
            {
                owner.AdmitAttackRangedFire();
                if (rangedCooldownType == RangedCooldownTypeChoice.StartAfterDraw)
                    ResetCooldownTimer();
            }
            else if (currentState is State.FightRangedAttack)
            {
                FaceRangedTarget();
                owner.AdmitAttackRanged();
            }
            else if (currentState == State.FightOnTheMark)
            {
                stateTimer = 0;
                ResetScanData();
                owner.AdmitAttackIdle();
            }
            else if (currentState is State.Flee)
            {
                owner.AdmitFlee();
            }
        }

        private bool IsStateInterruptible (State state)
        {
            if (stateSettings != null)
            {
                for (var i = 0; i < stateSettings.Count; i++)
                {
                    if (stateSettings[i].state == state)
                        return stateSettings[i].interruptible;
                }
            }

            return false;
        }

        private bool GetPathDestinationPoint (Vector3 source, Vector3 dest, out Vector3 destPoint, out float destDistance)
        {
            return motor.GetDestinationPoint(source, dest, out destPoint, out destDistance);
        }

        private bool IsBlockProjectileToRangedTarget (Vector3 startPos, CharacterOperator target)
        {
            var muzzlePos = Vector3.zero;
            var muzzle = owner.GetRangedMuzzle();
            if (muzzle != null)
                muzzlePos = muzzle.position - owner.agentTransform.position;
            startPos += muzzlePos;
            var targetPos = target.GetRangedHitPoint().position + new Vector3(0, rangedLinearDetectSize, 0);
            if (owner.printLog)
                Debug.DrawRay(startPos, targetPos - startPos, Color.red, 60);
            if (ReRaycast.CylinderCast(startPos, rangedLinearDetectSize, targetPos - startPos, owner.rangedAttackRange, out var result, rangedProjectile.hitColliderLayer))
            {
                var hitAgent = result.transform.GetComponentInParent<CharacterOperator>();
                if (hitAgent == target)
                    return false;
            }

            return true;
        }

        private bool IsCharacterBlockBetweenRangedTarget (Vector3 startPos, CharacterOperator target)
        {
            return IsCharacterBlockBetweenTarget(startPos, target, owner.rangedAttackRange);
        }

        private bool IsCharacterBlockBetweenMeleeTarget (Vector3 startPos, CharacterOperator target)
        {
            return IsCharacterBlockBetweenTarget(startPos, target, owner.meleeAttackRange);
        }

        private bool IsCharacterBlockBetweenTarget (Vector3 startPos, CharacterOperator target, float distance)
        {
            var targetPos = target.agentTransform.position + ReTransform.smallUp;
            if (owner.printLog)
                Debug.DrawRay(startPos, targetPos - startPos, Color.red, 60);
            var hits = ReRaycast.CylinderCastAll(startPos, owner.agentSize, targetPos - startPos, distance);
            var targetHitDistance = 0f;
            var operators = new CharacterOperator[hits.Length];
            var operatorsChecked = new bool[hits.Length];
            for (var i = 0; i < hits.Length; i++)
            {
                operatorsChecked[i] = true;
                var hitUnit = hits[i].transform.GetComponentInParent<CharacterOperator>();
                if (hitUnit != null)
                {
                    operators[i] = hitUnit;
                    if (hitUnit == target)
                    {
                        targetHitDistance = Vector3.Distance(startPos, hits[i].point);
                        break;
                    }
                }
            }

            for (var i = 0; i < hits.Length; i++)
            {
                var hitUnit = operatorsChecked[i] ? operators[i] : hits[i].transform.GetComponentInParent<CharacterOperator>();
                if (hitUnit != null)
                {
                    operators[i] = hitUnit;
                    if (hitUnit == owner)
                        continue;
                    else if (hitUnit == target)
                        continue;
                    else if (targetHitDistance > 0 && Vector3.Distance(startPos, hits[i].point) >= targetHitDistance)
                        continue;
                    owner.PrintLog(hitUnit.gameObject.name + " have blocked between " + owner.gameObject.name + " and " + target.gameObject.name);
                    return true;
                }
            }

            return false;
        }

        private void ResetScanData ()
        {
            scanPointStep = 0;
            scanPointOuter = 0;
        }

#if UNITY_EDITOR
        public static CharacterMuscle CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Character Muscle", "New Character Muscle", "asset", "Select a location to create character muscle");
            return path.Length == 0 ? null : ReEditorHelper.CreateScriptableObject<CharacterMuscle>(null, false, false, string.Empty, path);
        }
#endif
    }
}