using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.Unity;
#if UNITY_EDITOR
using Reshape.Unity.Editor;
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Character Motor", fileName = "CharacterMotor", order = 304)]
    [Serializable]
    [HideMonoScript]
    public class CharacterMotor : BaseScriptable
    {
        [BoxGroup("Generic"), SuffixLabel("sec", true)]
        public float chaseUpdateTime = 1f;

        [Hint("showHints", "Define the unit stop movement during stance and unstance.")]
        [BoxGroup("Generic"), LabelText("Stop At Stance")]
        public bool stopAtStance;

        [Hint("showHints", "Define the unit could perform interact action during stance.")]
        [BoxGroup("Generic"), LabelText("Interact During Stance")]
        public bool interactAtStance;

        [Hint("showHints", "Define the unit allow external control facing during walking.")]
        [BoxGroup("Generic"), LabelText("Facing Walk")]
        public bool facingWalk;

        [Hint("showHints", "Define the unit auto facing at destination base on the direction from starting location, value is the max distance to enable it. Value 0 means not enable.")]
        [BoxGroup("Generic"), LabelText("Directional Facing")]
        public float facingBaseDirection;

        [ShowIf("@facingBaseDirection > 0")]
        [Hint("showHints", "Define the unit have special move rotation speed when Directional Facing is enabled.")]
        [BoxGroup("Generic"), LabelText("Rotate Speed"), Indent]
        public float facingBaseDirectionRotateSpeed;

        [Hint("showHints", "Define the backward angle threshold to activate walk backward, activation when player input within the threshold, value 0 is disable walk backward.")]
        [BoxGroup("Directional Walk"), LabelText("Backward Angle")]
        [MinValue(0)]
        [MaxValue(360)]
        public int walkBackwardAngleThreshold;

        [Hint("showHints", "Define the destination facing threshold to activate walk backward, activation when player input within the threshold, value 0 is disable walk backward.")]
        [BoxGroup("Directional Walk"), LabelText("Backward Facing")]
        [MinValue(0)]
        [MaxValue(360)]
        public int walkBackwardFacingThreshold;

        [Hint("showHints", "Define only activate walk backward during stance.")]
        [BoxGroup("Directional Walk"), LabelText("Stance Only")]
        public bool walkBackwardFacingStance;

        [Hint("showHints", "Allow walk backward state change during walking.")]
        [BoxGroup("Directional Walk"), LabelText("Walk Update")]
        public bool walkBackwardWalkUpdate;

        [HideInEditorMode, DisableInPlayMode]
        public Vector3 moveDestination;

        [HideInEditorMode, DisableInPlayMode]
        public CharacterOperator chaseTarget;

        [HideInEditorMode, DisableInPlayMode]
        public float chaseTimer;

        [HideInEditorMode, DisableInPlayMode]
        public Vector3 wanderCenterPoint = Vector3.negativeInfinity;

        private CharacterOperator owner;
        private CharacterBrain brain;
        private CharacterMuscle muscle;
        private MotorAgent agent;
        private float moveDestinationFacing;
        private bool customRotation;
        private bool customRotateSpeed;
        private bool backwardWalking;
        private bool paused;

        public void Init (CharacterOperator charOp, CharacterBrain charBrain, CharacterMuscle charMuscle, MotorAgent motorAgent)
        {
            owner = charOp;
            brain = charBrain;
            muscle = charMuscle;
            agent = motorAgent;
            moveDestinationFacing = float.PositiveInfinity;
        }

        public bool isInited => agent != null;
        public bool underCustomRotateSpeed => customRotateSpeed;

        public void Tick (float deltaTime)
        {
            if (!isInited) return;
            if (paused) return;
            if (chaseTarget)
            {
                chaseTimer += deltaTime;
                if (chaseTimer >= chaseUpdateTime)
                {
                    chaseTimer -= deltaTime;
                    moveDestination = chaseTarget.agentTransform.position;
                    agent.SetDestination(moveDestination);
                }
            }

            if (customRotation)
            {
                if (!float.IsPositiveInfinity(moveDestinationFacing))
                {
                    var turnSpeed = deltaTime * agent.defaultMoveTurnSpeed;
                    var current = agent.GetFacing();
                    var diff = moveDestinationFacing - current;
                    if (Mathf.Abs(diff) <= turnSpeed)
                    {
                        agent.SetFacing(moveDestinationFacing);
                        customRotation = false;
                        customRotateSpeed = false;
                    }
                    else
                    {
                        turnSpeed = diff > 0 ? turnSpeed : -turnSpeed;
                        turnSpeed = Mathf.Abs(diff) > 180 ? -turnSpeed : turnSpeed;
                        agent.SetFacing(current + turnSpeed);
                    }
                }
            }
        }

        public bool SetDestination (Vector3 dest, string executeId = "", string layerName = "", bool walkBackward = false, float endFacing = float.PositiveInfinity)
        {
            if (!isInited) return false;
            customRotation = false;
            customRotateSpeed = false;
            CancelChase();
            moveDestination = dest;
            moveDestinationFacing = endFacing;

            backwardWalking = false;
            if (walkBackward)
            {
                if (!walkBackwardFacingStance || (walkBackwardFacingStance && owner.underStance))
                {
                    if (walkBackwardAngleThreshold is > 0 and <= 360 && walkBackwardFacingThreshold is > 0 and <= 360)
                    {
                        var agentBody = GetAgentTransform();
                        owner.IsPositionBehindLocation(moveDestination, agentBody.position, agentBody.rotation, out var yaw, out _, out var direction);
                        var checkAngle = false;
                        if (walkBackwardAngleThreshold == 360)
                            checkAngle = true;
                        else if (walkBackwardAngleThreshold >= 180 && direction.z == -1)
                            checkAngle = true;
                        else if (walkBackwardAngleThreshold < 180 && direction.z == -1)
                        {
                            var angleThreshold = 90f - (walkBackwardAngleThreshold / 2f);
                            if (yaw > 90f + angleThreshold || yaw < -90f - angleThreshold)
                                checkAngle = true;
                        }
                        else if (walkBackwardAngleThreshold >= 180 && direction.z == 1)
                        {
                            var angleThreshold = (walkBackwardAngleThreshold - 180f) / 2f;
                            if (yaw > 90f - angleThreshold || yaw < -90f + angleThreshold)
                                checkAngle = true;
                        }

                        if (checkAngle)
                        {
                            if (walkBackwardFacingThreshold == 360)
                                backwardWalking = true;
                            else if (!float.IsPositiveInfinity(endFacing) && Mathf.Abs(endFacing - agentBody.eulerAngles.y) < walkBackwardFacingThreshold / 2f)
                                backwardWalking = true;
                        }
                    }
                }
            }

            var rotateSpeed = 0f;
            if (backwardWalking)
            {
                if (!float.IsPositiveInfinity(moveDestinationFacing))
                    customRotation = true;
            }
            else
            {
                if (float.IsPositiveInfinity(moveDestinationFacing) && facingBaseDirection > 0f)
                {
                    var currentPos = GetAgentTransform().position;
                    var distance = Vector3.Distance(currentPos, moveDestination);
                    if (distance <= facingBaseDirection)
                    {
                        if (!ReRaycast.CylinderCast(currentPos, owner.agentSize, moveDestination - currentPos, distance, out _))
                        {
                            moveDestinationFacing = (360 + Mathf.Atan2(moveDestination.x - currentPos.x, moveDestination.z - currentPos.z) * (180 / Mathf.PI)) % 360;
                            rotateSpeed = facingBaseDirectionRotateSpeed;
                            customRotateSpeed = true;
                        }
                    }
                }
            }

            return agent.SetDestination(moveDestination, executeId, layerName, backwardWalking, rotateSpeed);
        }

        public void SetTeleport (Vector3 dest, string executeId = "")
        {
            if (!isInited) return;
            customRotation = false;
            customRotateSpeed = false;
            backwardWalking = false;
            CancelChase();
            moveDestination = dest;
            agent.SetTeleport(moveDestination, executeId);
        }

        public void SetPush (Vector3 dest, float speed, string executeId = "")
        {
            if (!isInited) return;
            CancelMove();
            ResetMove();
            CancelChase();
            CancelWander();
            moveDestination = dest;
            agent.Push(moveDestination, speed, executeId);
        }

        public void SetFlee (Vector3 dest, string executeId = "")
        {
            if (!isInited) return;
            CancelMove();
            ResetMove();
            CancelChase();
            CancelWander();
            moveDestination = dest;
            agent.SetDestination(moveDestination, executeId);
        }

        public void SetChase (CharacterOperator target, string executeId = "")
        {
            if (!isInited) return;
            customRotation = false;
            customRotateSpeed = false;
            backwardWalking = false;
            CancelWander();
            chaseTarget = target;
            chaseTimer = 0;
            moveDestination = chaseTarget.agentTransform.position;
            agent.SetDestination(moveDestination);
        }

        public void CancelChase ()
        {
            if (!isInited) return;
            chaseTarget = null;
            chaseTimer = 0;
        }

        public void SetWander (float range, bool relocate, string executeId = "")
        {
            if (!isInited) return;
            customRotation = false;
            customRotateSpeed = false;
            backwardWalking = false;
            CancelChase();
            var firstWander = false;
            var pos = GetAgentTransform().position;
            if (relocate || float.IsInfinity(wanderCenterPoint.x))
            {
                firstWander = true;
                wanderCenterPoint = pos;
            }

            var dest = wanderCenterPoint + ReRandom.Circle(range, true);
            if (!firstWander)
            {
                var loopLimit = 50;
                while (Vector3.Distance(pos, dest) < range)
                {
                    dest = wanderCenterPoint + ReRandom.Circle(range, true);
                    loopLimit--;
                    if (loopLimit <= 0)
                        break;
                }
            }

            moveDestination = dest;
            agent.SetDestination(moveDestination, executeId);
        }

        public void CancelWander ()
        {
            if (!isInited) return;
            wanderCenterPoint = Vector3.negativeInfinity;
        }

        public void ClearDestinationFacing ()
        {
            moveDestinationFacing = float.PositiveInfinity;
        }

        public void ReachDestination ()
        {
            if (!isInited) return;
            customRotation = false;
            customRotateSpeed = false;
            backwardWalking = false;
            ClearMove();
            if (!float.IsPositiveInfinity(moveDestinationFacing))
                if (agent.immediateTurn)
                    agent.SetFacing(moveDestinationFacing);
            muscle.ReachDestination();
        }

        public void SetInstantWalkBackward (bool active)
        {
            if (!IsAgentMoving()) return;
            if (!backwardWalking) return;
            if (!walkBackwardWalkUpdate) return;
            if (!active)
            {
                agent.SetIgnoreRotation(false);
            }
            else
            {
                agent.SetIgnoreRotation(true);
                if (!float.IsPositiveInfinity(moveDestinationFacing))
                    customRotation = true;
            }
        }

        public bool IsTargetReachable (CharacterOperator target)
        {
            if (!isInited) return true;
            var attackRange = owner.meleeAttackRange;
            if (target.IsWithinMeleeEngageRange(owner.agentTransform.position, attackRange))
                return true;
            if (agent.GetEndPoint(out var point))
                return target.IsWithinMeleeEngageRange(point, attackRange);
            return true;
        }

        public bool IsBehindTarget (CharacterOperator target, out float yaw, out float pitch, out Vector3Int direction)
        {
            pitch = yaw = 0;
            direction = Vector3Int.zero;
            return target && IsBehindLocation(target.agentTransform.position, target.agentTransform.rotation, out yaw, out pitch, out direction);
        }

        public bool IsBehindLocation (Vector3 targetPos, Quaternion targetRot, out float yaw, out float pitch, out Vector3Int direction)
        {
            pitch = yaw = 0;
            direction = Vector3Int.zero;
            return isInited && owner.IsPositionBehindLocation(GetAgentTransform().position, targetPos, targetRot, out yaw, out pitch, out direction);
        }

        public bool IsMoveDestinationReachable ()
        {
            if (!isInited) return true;
            if (Vector3.Distance(moveDestination, owner.agentTransform.position) < owner.agentSize)
                return true;
            if (agent.GetEndPoint(out var point))
                return Vector3.Distance(moveDestination, point) < owner.agentSize;
            return true;
        }

        public bool IsReachExecutionDestination (string executionId)
        {
            if (!isInited) return false;
            return agent.IsReachDestination(executionId, true);
        }

        public bool IsWithinDestination (float range)
        {
            if (!isInited) return false;
            if (Vector3.Distance(owner.agentTransform.position, moveDestination) <= range)
                return true;
            return false;
        }

        public bool GetDestinationPoint (Vector3 source, Vector3 dest, out Vector3 destPoint, out float destDistance)
        {
            if (!isInited)
            {
                destPoint = Vector3.zero;
                destDistance = 0f;
                return false;
            }

            return agent.CalculateDestinationPoint(source, dest, out destPoint, out destDistance);
        }

        public List<Vector3> GetDestinationPath ()
        {
            return isInited ? agent.GetPoints() : null;
        }

        public void Face (Vector3 destination)
        {
            if (!isInited) return;
            if (!facingWalk && IsAgentMoving()) return;
            agent.LookAt(destination);
        }

        public void CancelMove (bool fakeReach = false)
        {
            if (!isInited) return;
            customRotation = false;
            customRotateSpeed = false;
            backwardWalking = false;
            agent.CancelMove(fakeReach);
        }

        public void ClearMove ()
        {
            if (!isInited) return;
            agent.ClearMove();
        }

        public void ResetMove ()
        {
            if (!isInited) return;
            agent.ResetMove();
        }

        public void StopMove ()
        {
            if (!isInited) return;
            customRotation = false;
            customRotateSpeed = false;
            backwardWalking = false;
            agent.StopMove();
        }
        
        public void ResumeMove ()
        {
            if (!isInited) return;
            agent.ResumeMove();
        }

        public void PauseMove ()
        {
            if (!isInited) return;
            paused = true;
            agent.PauseMove();
        }

        public void UnpauseMove ()
        {
            if (!isInited) return;
            paused = false;
            agent.UnpauseMove();
        }

        public bool IsAgentMoving ()
        {
            if (!isInited) return default;
            return agent.IsMoving();
        }

        public Vector3 GetAgentMovingDirection ()
        {
            if (!isInited) return default;
            return agent.GetMoveDirection();
        }

        public float GetAgentStoppingDistance ()
        {
            if (!isInited) return default;
            return agent.GetStoppingDistance();
        }

        public float GetAgentInteractDistance ()
        {
            if (!isInited) return default;
            return agent.GetInteractDistance();
        }

        public Transform GetAgentTransform ()
        {
            if (!isInited) return default;
            return agent.GetBody();
        }

        public float GetAgentSize ()
        {
            if (!isInited) return 0;
            return agent.GetBodySize();
        }

        public float GetMoveSpeed ()
        {
            if (!isInited) return 0;
            return agent.GetMoveSpeed();
        }
        
        public float GetFacingAngle (CharacterOperator target)
        {
            var direction = GetAgentTransform().position - target.agentTransform.position;
            var facing = Vector3.Angle(direction, GetAgentTransform().forward);
            return facing;
        }

        public bool GetStatValue (string statName, out float value)
        {
            if (isInited)
            {
                if (!string.IsNullOrEmpty(statName))
                {
                    if (statName == StatType.STAT_DISTANCE)
                    {
                        if (owner.statValueCharacterParam)
                        {
                            value = Vector3.Distance(owner.statValueCharacterParam.agentTransform.position, GetAgentTransform().position);
                            return true;
                        }
                    }
                    if (statName == StatType.STAT_ANGLE)
                    {
                        if (owner.statValueCharacterParam)
                        {
                            value = owner.statValueCharacterParam.motor.GetFacingAngle(owner);
                            return true;
                        }
                    }
                    else if (statName == StatType.STAT_UNIT_FOV)
                    {
                        if (owner.statValueCharacterParam)
                        {
                            value = -1f;
                            if (owner.IsVisibleInFogUnit(owner.statValueCharacterParam.fogOfWarAgent))
                                value = 1f;
                            return true;
                        }
                    }
                    else if (statName == StatType.STAT_TEAM_FOV)
                    {
                        if (owner.statValueCharacterParam)
                        {
                            value = -1f;
                            if (owner.IsVisibleInFogTeam(owner.statValueCharacterParam.GetFogTeam()))
                                value = 1f;
                            return true;
                        }
                    }
                }
            }

            value = 0f;
            return false;
        }

#if UNITY_EDITOR
        public static CharacterMotor CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Character Motor", "New Character Motor", "asset", "Select a location to create character motor");
            return path.Length == 0 ? null : ReEditorHelper.CreateScriptableObject<CharacterMotor>(null, false, false, string.Empty, path);
        }
#endif
    }
}