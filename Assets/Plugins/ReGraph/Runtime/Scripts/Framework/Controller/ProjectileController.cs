using System;
using System.Collections;
using System.Collections.Generic;
using Reshape.ReGraph;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.Unity;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class ProjectileController : VisualEffectController
    {
        private const string EASE_STRING = "ease";
        private const string DELAY_STRING = "delay";
        private const string DISABLE_STRING = "Disable";
        private const string ONCOMPLETE_STRING = "onComplete";

        private enum State
        {
            NotInUse = 10,
            InBattle = 11,
            Paused = 12
        }

        private enum BezierState
        {
            Setup = 10,
            Travel = 11,
            Complete = 12
        }

        private static List<ProjectileController> list;

        [ShowInInspector, ReadOnly]
        private AttackDamageData damageData;

        [ShowInInspector, ReadOnly]
        private AttackProjectile projectileData;

        [ShowInInspector, ReadOnly]
        private CharacterOperator destinationTarget;

        [ShowInInspector, ReadOnly]
        private Vector3 destination;

        [ShowInInspector, ReadOnly]
        private float travelSpeed;

        [ShowInInspector, ReadOnly]
        private State state = State.NotInUse;

        [ShowInInspector, ReadOnly]
        private GameObject collidedGo;

        private List<int> tweenIds;
        private BezierState bezierState;
        private bool freeTravel;
        private Vector3 lastPosition;
        private Vector3 moveDirection;
        private GraphEvent graphProjectileEvent;
        private GraphExecution graphProjectileExecution;
        private float travelMaxDistance;
        private Vector3 defaultScale;
        
        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static void CleanAll ()
        {
            if (list != null)
            {
                for (var i = 0; i < list.Count; i++)
                    list[i].Clear();
                list.Clear();
            }

            Destroy(LeanTween.tweenEmpty);
            LeanTween.reset();
        }

        public new static void PauseAll ()
        {
            if (list != null)
                for (var i = 0; i < list.Count; i++)
                    list[i].Pause();
            LeanTween.forceStop = true;
        }

        public new static void UnpauseAll ()
        {
            if (list != null)
                for (var i = 0; i < list.Count; i++)
                    list[i].Unpause();
            LeanTween.forceStop = false;
        }

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------
        
        public void Init (AttackDamageData attackData, CharacterOperator attackTarget, AttackProjectile projectile)
        {
            damageData = attackData;
            destinationTarget = attackTarget;
            projectileData = projectile;

            collidedGo = null;
            freeTravel = false;
            tweenIds = new List<int>();
            bezierState = BezierState.Setup;
            state = State.InBattle;

            transform.SetLayer(projectile.flag, false);
            transform.localScale = defaultScale;
            var speed = attackData.attacker.rangedAmmoTravelSpeed;
            if (speed > 0)
                travelSpeed = (speed * (1 + projectileData.speedMtp)) + projectileData.speedMod;
            else
                travelSpeed = projectileData.speedMod * (1 + projectileData.speedMtp);
            travelMaxDistance = damageData.attacker.rangedAttackRange;

            destination = destinationTarget.GetRangedHitPoint().position;
            if (projectileData.isStraightToTargetBallistic)
            {
                SetupStraightToTargetBallistic();
            }
            else if (projectileData.isCurveToTargetBallistic)
            {
                if (projectileData.curveHeight <= 0)
                {
                    SetupStraightToTargetBallistic();
                }
            }

            TriggerProjectileStartUsage();

            void SetupStraightToTargetBallistic ()
            {
                var distance = Vector3.Distance(destination, transform.position);
                var duration = distance / travelSpeed;
                destination = ForecastDestinationHitPoint(duration);
                destination += RandomDestinationHitPoint(distance);
            }
        }
        
        public override void Pause ()
        {
            if (state == State.InBattle)
            {
                LeanTween.pause(gameObject);
                state = State.Paused;
            }
            
            base.Pause();
        }

        public override void Unpause ()
        {
            if (state == State.Paused)
            {
                LeanTween.resume(gameObject);
                state = State.InBattle;
            }
            
            base.Unpause();
        }
        
        public override void FinishUsage ()
        {
            BackToPool();
        }
        
        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        protected virtual void StartProjectileUsage ()
        {
            Show();
        }
            
        protected virtual void FinishProjectileUsage ()
        {
            damageData.Terminate();
            ClearVelocity();
            Hide();
        }
        
        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected override void Awake ()
        {
            defaultScale = transform.localScale;
            graphProjectileEvent = GetComponent<GraphEvent>();
            list ??= new List<ProjectileController>();
            list.Add(this);
            PlanTick();
            base.Awake();
        }

        protected override void OnDestroy ()
        {
            list?.Remove(this);
            OmitTick();
            base.OnDestroy();
        }

        protected void OnCollisionEnter (Collision collision)
        {
            if (state == State.InBattle && projectileData.hitCollider)
            {
                var colTrans = collision.transform;
                collidedGo = colTrans.gameObject;
                if (colTrans.IsInLayer(projectileData.hitColliderLayer))
                {
                    var contactPoint = collision.GetContact(0);
                    transform.position = contactPoint.point;

                    var co = collidedGo.GetComponentInParent<CharacterOperator>();
                    if (co != null)
                    {
                        ClearVelocity();
                        co.ObtainAttackPack(damageData);
                        Reached(colTrans);
                    }
                    else
                    {
                        var pc = collidedGo.GetComponentInParent<ProjectileController>();
                        if (pc != null)
                        {
                            if (projectileData.hitOnAirReact == AttackProjectile.HitOnAirFeedback.StaticOnAir)
                            {
                                ClearVelocity();
                                Reached(null);
                            }
                            else if (projectileData.hitOnAirReact == AttackProjectile.HitOnAirFeedback.KeepVelocityOnAir)
                            {
                                Reached(null);
                            }
                            else if (projectileData.hitOnAirReact == AttackProjectile.HitOnAirFeedback.FallFromAir)
                            {
                                var velocity = rigidbody.velocity;
                                velocity.y -= 1;
                                rigidbody.velocity = velocity;
                                Reached(null);
                            }
                        }
                        else
                        {
                            ClearVelocity();
                            Reached(colTrans, true);
                        }
                    }
                }
            }
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------
        
        public override void Tick ()
        {
            if (state == State.InBattle)
            {
                var arrived = false;
                if (projectileData.isStraightToTargetBallistic)
                {
                    arrived = transform.GoTo(destination, travelSpeed);
                }
                else if (projectileData.isStraightFollowTargetBallistic)
                {
                    if (destinationTarget != null)
                        destination = destinationTarget.GetRangedHitPoint().position;
                    arrived = transform.GoTo(destination, travelSpeed);
                }
                else if (projectileData.isCurveToTargetBallistic)
                {
                    if (freeTravel || projectileData.curveHeight <= 0)
                    {
                        arrived = transform.GoTo(destination, travelSpeed);
                    }
                    else
                    {
                        if (bezierState == BezierState.Setup)
                        {
                            bezierState = BezierState.Travel;
                            var distance = Vector3.Distance(destination, transform.position);
                            var rangeMod = projectileData.curveHeightCurve.Evaluate(distance / travelMaxDistance);
                            SetupBezierBallistic(projectileData.curveHeight * rangeMod, projectileData.curveControlPoint, travelSpeed, projectileData.curveBaseTime, projectileData.curveSpeedRange,
                                projectileData.curveRotation);
                        }
                        else if (bezierState == BezierState.Complete)
                        {
                            arrived = true;
                        }
                    }
                }

                if (arrived)
                {
                    if (!projectileData.hitCollider)
                    {
                        if (destinationTarget != null)
                        {
                            destinationTarget.ObtainAttackPack(damageData);
                            Reached(destinationTarget.agentTransform);
                        }
                        else
                        {
                            Reached(null, true);
                        }
                    }
                    else
                    {
                        if (!freeTravel)
                        {
                            freeTravel = true;
                            destination = transform.position + (moveDirection * projectileData.hitTravelDuration);
                        }
                        else
                        {
                            rigidbody.velocity = Vector3.zero;
                            Reached(null, true);
                        }
                    }
                }
            }

            var bodyPos = transform.position;
            if (Vector3.Distance(lastPosition, bodyPos) > 0f)
            {
                moveDirection = bodyPos - lastPosition;
                moveDirection *= 1 / ReTime.deltaTime;
                lastPosition = bodyPos;
            }
        }
        
        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void Reached (Transform target, bool setMissed = false)
        {
            CancelTween();
            if (target != null && projectileData.stickTarget)
                transform.SetParent(target);
            state = State.NotInUse;
            if (setMissed)
                damageData.SetMissed();
            if (damageData.attacker != null)
                damageData.attacker.CompleteAttack(CharacterOperator.AttackType.RangedNormal, damageData);
            TriggerProjectileFinishUsage();
        }

        private void ClearVelocity ()
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        private void TriggerProjectileStartUsage ()
        {
            var haveEvent = false;
            if (graphProjectileEvent != null)
            {
                var execution = graphProjectileEvent.Execute(GraphEventListener.EventType.ProjectileStartUsage);
                if (execution != null)
                {
                    haveEvent = true;
                    if (execution.isCompleted)
                    {
                        StartProjectileUsage();
                    }
                    else
                    {
                        graphProjectileExecution = execution;
                        graphProjectileExecution.OnComplete += OnCompleteStartProjectileUsage;
                    }
                }
            }

            if (!haveEvent)
                StartProjectileUsage();
        }
        
        private void OnCompleteStartProjectileUsage ()
        {
            graphProjectileExecution.OnComplete -= OnCompleteStartProjectileUsage;
            graphProjectileExecution = null;
            StartProjectileUsage();
        }

        private void TriggerProjectileFinishUsage ()
        {
            var haveEvent = false;
            if (graphProjectileEvent != null)
            {
                var execution = graphProjectileEvent.Execute(GraphEventListener.EventType.ProjectileFinishUsage);
                if (execution != null)
                {
                    haveEvent = true;
                    if (execution.isCompleted)
                    {
                        FinishProjectileUsage();
                    }
                    else
                    {
                        graphProjectileExecution = execution;
                        graphProjectileExecution.OnComplete += OnCompleteFinishProjectileUsage;
                    }
                }
            }

            if (!haveEvent)
                FinishProjectileUsage();
        }

        private void OnCompleteFinishProjectileUsage ()
        {
            graphProjectileExecution.OnComplete -= OnCompleteFinishProjectileUsage;
            graphProjectileExecution = null;
            FinishProjectileUsage();
        }

        private void BackToPool ()
        {
            var me = gameObject;
            me.SetActiveOpt(false);
            InsertIntoPool(me, true);
        }

        private void Clear ()
        {
            if (state == State.InBattle)
            {
                CancelTween();
                ClearPool(gameObject.name);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private Vector3 ForecastDestinationHitPoint (float travelTime)
        {
            if (projectileData.hitForecast && travelTime > 0)
            {
                if (destinationTarget.isMoving)
                {
                    var direction = destinationTarget.movingDirection * 10000;
                    return Vector3.MoveTowards(destination, direction, destinationTarget.moveSpeed * travelTime * projectileData.hitForecastAccuracy);
                }
            }

            return destination;
        }

        private Vector3 RandomDestinationHitPoint (float distance)
        {
            var accuracy = damageData.attacker.rangedAmmoAccuracy;
            accuracy = Mathf.Clamp(accuracy, 0f, 1f);
            accuracy = 1 - accuracy;
            var randDest = Vector3.zero;
            var rangeMod = projectileData.hitRangeCurve.Evaluate(distance / travelMaxDistance);
            float rand;
            if (projectileData.hitRange.x > 0)
            {
                rand = projectileData.hitRange.x * accuracy * rangeMod;
                if (rand > 0)
                    randDest.x = ReRandom.Range(-rand, rand);
            }

            if (projectileData.hitRange.y > 0)
            {
                rand = projectileData.hitRange.y * accuracy * rangeMod;
                if (rand > 0)
                    randDest.y = ReRandom.Range(-rand, rand);
            }

            if (projectileData.hitRange.z > 0)
            {
                rand = projectileData.hitRange.z * accuracy * rangeMod;
                if (rand > 0)
                    randDest.z = ReRandom.Range(-rand, rand);
            }

            return randDest;
        }

        private void SetupBezierBallistic (float curveHeight, float controlPointWidth, float speed, float curveBaseTime, float curveSpeedRange, bool curveRotate)
        {
            CancelTween();

            var curPos = transform.position;
            var spawnLocation = curPos;

            var travelDistance = Vector3.Distance(destination, curPos);
            var travelTime = travelDistance / speed;
            if (curveBaseTime > 0)
            {
                if (curveSpeedRange == 0)
                    curveSpeedRange = travelDistance;
                travelTime = curveBaseTime + ((travelDistance / curveSpeedRange) * (curveSpeedRange / speed));
            }

            destination = ForecastDestinationHitPoint(travelTime);
            destination += RandomDestinationHitPoint(travelDistance);

            var midPoint = (destination - spawnLocation) / 2f;
            midPoint += spawnLocation + new Vector3(0f, curveHeight, 0f);
            var midPointBefore = (midPoint - spawnLocation) * controlPointWidth;
            midPointBefore += spawnLocation;
            var midPointAfter = (destination - midPoint) * controlPointWidth;
            midPointAfter += midPoint;
            var bezierPoints = new Vector3 [4];
            bezierPoints[0] = curPos;
            bezierPoints[1] = midPointBefore;
            bezierPoints[2] = midPointAfter;
            bezierPoints[3] = destination;
            var bezier = GetBezierPath(bezierPoints[0], bezierPoints[1], bezierPoints[2], bezierPoints[3]);

            if (curveRotate)
            {
                var startDir = (bezier[1] - bezier[0]).normalized;
                var startRot = Quaternion.LookRotation(startDir, Vector3.up);
                var startAngle = startRot.eulerAngles;
                transform.eulerAngles = startAngle;
            }

            var pointHeights = new float[bezier.Count];
            for (var i = 0; i < pointHeights.Length; i++)
                pointHeights[i] = bezier[i].y;
            float bezierCount = bezier.Count;
            var maxIndex = Array.IndexOf(pointHeights, Mathf.Max(pointHeights));
            var arcFirstDuration = maxIndex / bezierCount * travelTime;
            var arcSecondDuration = (bezierCount - maxIndex) / bezierCount * travelTime;
            var divTimeFirst = arcFirstDuration / maxIndex;
            var divTimeSecond = arcSecondDuration / (bezierCount - maxIndex);

            Vector3 bezierPos;
            object easeObj = LeanTweenType.easeOutQuad;
            Vector3 rotDir;
            Quaternion rot;
            Vector3 angle;
            for (var i = 0; i < maxIndex; i++)
            {
                var hash = new Hashtable
                {
                    {EASE_STRING, easeObj},
                    {DELAY_STRING, (divTimeFirst * i)}
                };
                bezierPos = bezier[i];
                tweenIds.Add(LeanTween.move(gameObject, bezierPos, divTimeFirst, hash));
                if (curveRotate)
                {
                    if (i < bezier.Count - 1)
                    {
                        rotDir = (bezier[i + 1] - bezierPos).normalized;
                        rot = Quaternion.LookRotation(rotDir, Vector3.up);
                        angle = rot.eulerAngles;
                        tweenIds.Add(LeanTween.rotate(gameObject, angle, divTimeFirst, hash));
                    }
                }
            }

            easeObj = LeanTweenType.easeInQuad;
            for (var i = maxIndex; i < bezier.Count; i++)
            {
                var hash = new Hashtable
                {
                    {EASE_STRING, easeObj},
                    {DELAY_STRING, (divTimeSecond * (i - maxIndex) + arcFirstDuration)}
                };
                bezierPos = bezier[i];
                if (i == bezier.Count - 1)
                    hash.Add(ONCOMPLETE_STRING, DISABLE_STRING);
                tweenIds.Add(LeanTween.move(gameObject, bezierPos, divTimeSecond, hash));
                if (curveRotate)
                {
                    if (i < bezier.Count - 1)
                    {
                        rotDir = (bezier[i + 1] - bezierPos).normalized;
                        rot = Quaternion.LookRotation(rotDir, Vector3.right);
                        angle = rot.eulerAngles;
                        tweenIds.Add(LeanTween.rotate(gameObject, angle, divTimeSecond, hash));
                    }
                }
            }
        }

        private void Disable ()
        {
            bezierState = BezierState.Complete;
            CancelTween();
        }

        private void CancelTween ()
        {
            if (tweenIds.Count > 0)
            {
                while (tweenIds.Count > 0)
                {
                    var id = tweenIds[0];
                    LeanTween.cancel(gameObject, id);
                    tweenIds.RemoveAt(0);
                }
            }
        }

        private List<Vector3> GetBezierPath (Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var path = new List<Vector3> {a};
            var aa = (-a + 3 * (b - c) + d);
            var bb = 3 * (a + c) - 6 * b;
            var cc = 3 * (b - a);
            for (var k = 1.0f; k <= 30.0f; k++)
            {
                var t = k / 30.0f;
                var p = ((aa * t + (bb)) * t + cc) * t + a;
                path.Add(p);
            }

            return path;
        }

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}