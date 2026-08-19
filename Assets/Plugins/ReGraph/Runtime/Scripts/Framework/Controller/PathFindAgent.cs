using System;
using System.Collections.Generic;
using UnityEngine;
using Reshape.Unity;
using Sirenix.OdinInspector;
#if REGRAPH_PATHFIND
using Pathfinding;
#endif

namespace Reshape.ReFramework
{
    public class PathFindAgent : MotorAgent
    {
        [Serializable]
        public class Execution
        {
            public string id;
            public bool reached;
            public Vector3 destination;

            public Execution (string id, Vector3 dest)
            {
                this.id = id;
                destination = dest;
                reached = false;
            }
        }

        public const float PUSH_THRESHOLD = -10000000;

#if REGRAPH_PATHFIND
        public PathFindMover mover;
        public PathFindGraphSelection graphSelector;
        public NavmeshCut blocker;
        public bool linearMovement;

        private AutoRepathPolicy neverAutoRepath;
        private AutoRepathPolicy defaultAutoRepath;
#else
        [InfoBox("This component is not functioning due to REGRAPH_PATHFIND not enable in ScriptingDefine Symbols", InfoMessageType.Warning, GUIAlwaysEnabled = true)]
#endif
        public bool printLog;

#pragma warning disable CS0414
        private Vector3 destination;
#pragma warning restore CS0414
        private Vector3 reachLastPosition;
        private List<Execution> executions;
        private float defaultEndReachDistance;
        private float pushSpeed;
        private bool inLinearMove;

        private UpdateDelegate updateDelegate;

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public override bool GetEndPoint (out Vector3 point)
        {
#if REGRAPH_PATHFIND
            if (inLinearMove)
            {
                point = destination;
                return true;
            }

            if (mover.calculatedPath is {vectorPath: {Count: > 0}})
            {
                point = mover.calculatedPath.vectorPath[^1];
                return true;
            }
#endif
            point = Vector3.zero;
            return false;
        }

        public override List<Vector3> GetPoints ()
        {
#if REGRAPH_PATHFIND
            if (mover.calculatedPath is {vectorPath: {Count: > 0}})
            {
                /*var path = mover.calculatedPath.path;
                var v3Path = new List<Vector3>();
                for (var i = 0; i < path.Count; i++)
                    v3Path.Add((Vector3)path[i].position);
                return v3Path;*/
                return mover.calculatedPath.vectorPath;
            }
#endif
            return base.GetPoints();
        }

        public override bool CalculateDestinationPoint (Vector3 source, Vector3 dest, out Vector3 destPoint, out float destDistance)
        {
            destPoint = dest;
            destDistance = Vector3.Distance(source, dest);
#if REGRAPH_PATHFIND
            //-- NOTE this might create performance since system might calc many times in the same frame
            var path = ABPath.Construct(source, dest);
            AstarPath.StartPath(path, true);
            path.BlockUntilCalculated();
            if (path.vectorPath is not {Count: > 0})
                return false;
            destPoint = path.vectorPath[^1];
            destDistance = path.GetTotalLength();
#endif
            return true;
        }

        public override float GetStoppingDistance ()
        {
#if REGRAPH_PATHFIND
            return mover.endReachedDistance;
#else
            return base.GetStoppingDistance();
#endif
        }

        public override float GetInteractDistance ()
        {
#if REGRAPH_PATHFIND
            return blocker.circleRadius;
#else
            return base.GetInteractDistance();
#endif
        }

        public override void Push (Vector3 dest, float speed, string executeId = "")
        {
            destination = dest;
            pushSpeed = speed;
            CancelMove();
#if REGRAPH_FOW
            mover.autoRepath = neverAutoRepath;
            mover.endReachedDistance = 0f;
            EnableBlocker(true);
            graphSelector.SetGraphToDefault();
            updateDelegate = UpdatePushToDestination;
#endif
        }

        public override bool SetDestination (Vector3 dest, string executeId = "", string layerName = "", bool ignoreRotation = false, float rotationSpeed = 0)
        {
#if REGRAPH_PATHFIND
            if (Vector3.Distance(destination, dest) > 0)
            {
                destination = dest;
#if DEVELOPMENT_BUILD || (UNITY_EDITOR && REGRAPH_DEV_DEBUG)
                PrintLog($"SetDestination executing : dest({dest}) layerName({layerName}) executeId({executeId})");
#endif
                var useLinearMove = false;
                if (linearMovement)
                {
                    var distance = Vector3.Distance(transform.position, destination);
                    if (distance <= mover.radius)
                    {
                        useLinearMove = true;
                    }
                    else if (blocker && distance <= blocker.circleRadius)
                    {
                        useLinearMove = true;
                    }
                }

                if (useLinearMove)
                {
                    ApplyLinearMovement();
                    updateDelegate = UpdateMoveToDestination;
                }
                else
                {
                    EnableBlocker(false);
                    UpdatePathDestination();
                    if (!string.IsNullOrEmpty(layerName))
                        graphSelector.SetGraph(layerName);
                    mover.rotationSpeed = rotationSpeed > 0 ? rotationSpeed : defaultMoveTurnSpeed;
                    mover.enableRotation = !ignoreRotation;
                    mover.CalculatePath();
                    mover.onPathCalculated += OnMoverCalculated;
                }

                if (!string.IsNullOrEmpty(executeId))
                {
                    executions.Add(new Execution(executeId, destination));
                }

                return true;
            }
#endif
            return false;
        }

        public override bool SetTeleport (Vector3 dest, string executeId = "")
        {
#if REGRAPH_PATHFIND
            if (Vector3.Distance(destination, dest) > 0)
            {
                destination = dest;
                ApplyLinearMovement();
                updateDelegate = UpdateTeleportToDestination;

                if (!string.IsNullOrEmpty(executeId))
                {
                    executions.Add(new Execution(executeId, destination));
                }

                return true;
            }
#endif
            return false;
        }

        public override bool IsReachDestination (string executeId, bool clearIfReached = false)
        {
#if REGRAPH_PATHFIND
            if (!string.IsNullOrEmpty(executeId))
            {
                for (var i = 0; i < executions.Count; i++)
                {
                    if (executions[i].id.Equals(executeId))
                    {
                        if (executions[i].reached)
                        {
                            if (clearIfReached)
                                executions.RemoveAt(i);
                            return true;
                        }

                        break;
                    }
                }
            }
#endif
            return false;
        }

        public override void ResetMove ()
        {
            destination = default;
        }

        public override void ClearMove ()
        {
#if REGRAPH_PATHFIND
            CancelLinearMove();
            mover.ClearCurrentPath();
#endif
        }

        public override void CancelMove (bool fakeReach = false)
        {
#if REGRAPH_PATHFIND
            CancelLinearMove();
            mover.ClearCurrentPath();

            for (var i = 0; i < executions.Count; i++)
                if (!executions[i].reached)
                    executions[i].reached = true;

            if (fakeReach)
            {
                mover.autoRepath = neverAutoRepath;
                mover.endReachedDistance = defaultEndReachDistance;
                reachLastPosition = transform.position;
                updateDelegate = null;
                ReachDestination();
            }
#endif
        }

        public override void StopMove ()
        {
#if REGRAPH_PATHFIND
            mover.canMove = false;
            EnableBlocker(true);
#endif
        }

        public override void ResumeMove ()
        {
#if REGRAPH_PATHFIND
            mover.canMove = true;
            EnableBlocker(false);
#endif
        }

        public override void PauseMove ()
        {
#if REGRAPH_PATHFIND
            mover.canMove = false;
#endif
        }

        public override void UnpauseMove ()
        {
#if REGRAPH_PATHFIND
            mover.canMove = true;
#endif
        }

        public override void SetIgnoreRotation (bool active)
        {
#if REGRAPH_PATHFIND
            mover.enableRotation = !active;
#endif
        }

        public override void SetMoveSpeed (float speed)
        {
#if REGRAPH_PATHFIND
            mover.maxSpeed = speed;
#endif
        }

        public override float GetMoveSpeed ()
        {
#if REGRAPH_PATHFIND
            return mover.maxSpeed;
#else
            return base.GetMoveSpeed();
#endif
        }

        public override bool IsMoving ()
        {
#if REGRAPH_PATHFIND
            return (mover.isPathing || inLinearMove) && base.IsMoving();
#else
            return base.IsMoving();
#endif
        }

        public override void SetMoveTurnSpeed (float speed, bool immediate = false)
        {
#if REGRAPH_PATHFIND
            defaultMoveTurnSpeed = speed;
            if (immediate)
                mover.rotationSpeed = defaultMoveTurnSpeed;
#endif
        }

        public override float GetBodySize ()
        {
#if REGRAPH_PATHFIND
            return mover == null ? base.GetBodySize() : mover.GetShapeRadius();
#else
            return base.GetBodySize();
#endif
        }

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

#if REGRAPH_PATHFIND

        protected void Awake ()
        {
            executions ??= new List<Execution>();
            neverAutoRepath = new AutoRepathPolicy {mode = AutoRepathPolicy.Mode.Never};
            if (mover != null)
            {
                defaultMoveTurnSpeed = mover.rotationSpeed;
                defaultAutoRepath = mover.autoRepath;
                defaultEndReachDistance = mover.endReachedDistance;
                Init(mover.transform);
            }
            else
            {
                Init(gameObject.transform);
            }
        }

        protected override void Start ()
        {
            EnableBlocker(true);
            if (mover != null)
            {
                mover.autoRepath = neverAutoRepath;
                if (immediateTurn)
                {
                    //-- Force mover quickly make U-turn instead of naturally stop and move when U-turn happen
                    mover.maxAcceleration = mover.maxSpeed * 100;
                }
            }

            base.Start();
        }

        protected void Update ()
        {
            updateDelegate?.Invoke();
        }

        protected void OnEnable ()
        {
            if (mover != null)
            {
                mover.onSearchPath += UpdatePathDestination;
                mover.onPathReached += OnReachDestination;
            }
        }

        protected void OnDisable ()
        {
            if (mover != null)
            {
                mover.onSearchPath -= UpdatePathDestination;
                mover.onPathReached -= OnReachDestination;
            }
        }

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void ApplyLinearMovement ()
        {
            CancelMove();
            mover.autoRepath = neverAutoRepath;
            mover.endReachedDistance = 0f;
            EnableBlocker(true);
            graphSelector.SetGraphToDefault();
            inLinearMove = true;
        }

        private void UpdateMoveToDestination ()
        {
            if (mover.canMove)
            {
                var moveSpeed = GetMoveSpeed();
                if (moveSpeed > 0)
                {
                    var moveRate = moveSpeed * ReTime.deltaTime;
                    var curPos = transform.position;
                    if (Vector3.Distance(curPos, destination) <= moveRate)
                    {
                        mover.Move(destination - transform.position);
                        inLinearMove = false;
                        updateDelegate = null;
                        mover.endReachedDistance = defaultEndReachDistance;

                        MarkExecutionsReached();
                        PrintLog("Reached Destination");
                        CallbackReachDestination();
                    }
                    else
                    {
                        var nextPos = Vector3.MoveTowards(curPos, destination, moveRate);
                        mover.Move(nextPos - transform.position);
                    }
                }
            }
        }

        private void CancelLinearMove ()
        {
            inLinearMove = false;
            updateDelegate = null;
        }

        private void UpdateTeleportToDestination ()
        {
            if (mover.canMove)
            {
                mover.Teleport(destination, false);
                updateDelegate = null;
                mover.endReachedDistance = defaultEndReachDistance;

                MarkExecutionsReached();
                PrintLog("Teleported Destination");
                CallbackReachDestination();
            }
        }

        private void UpdatePushToDestination ()
        {
            if (mover.canMove)
            {
                var moveSpeed = GetMoveSpeed();
                if (moveSpeed > PUSH_THRESHOLD)
                {
                    var moveRate = pushSpeed * ReTime.deltaTime;
                    var curPos = transform.position;
                    if (Vector3.Distance(curPos, destination) <= moveRate)
                    {
                        mover.Move(destination - transform.position);
                        updateDelegate = null;
                        mover.endReachedDistance = defaultEndReachDistance;
                        CallbackReachDestination();
                    }
                    else
                    {
                        var nextPos = Vector3.MoveTowards(curPos, destination, moveRate);
                        mover.Move(nextPos - transform.position);
                    }
                }
            }
        }

        private void UpdateReachDestination ()
        {
            Vector3 curPosition = transform.position;
            var distance = Vector3.Distance(reachLastPosition, curPosition);
            if (distance <= 0.001f)
            {
                if (mover.reachedDestination || mover.reachedEndOfPath)
                {
                    updateDelegate = null;
                    ReachDestination();
                }
                else
                {
                    //-- Detect agent not able to move very close to the destination due to collision like wall
                    if (mover.GetPathEndPoint(out var endPoint))
                    {
                        distance = Vector3.Distance(endPoint, curPosition);
                        if (distance <= mover.radius)
                        {
                            mover.endReachedDistance = distance;
                        }
                    }
                }
            }

            reachLastPosition = curPosition;
        }

        private void OnMoverCalculated (Path path)
        {
            PrintLog("Calculate path to destination");
            mover.onPathCalculated -= OnMoverCalculated;
            mover.autoRepath = defaultAutoRepath;
            updateDelegate = UpdateReachDestination;
        }

        private void OnReachDestination ()
        {
            PrintLog("OnReachDestination");
            mover.autoRepath = neverAutoRepath;
            mover.endReachedDistance = defaultEndReachDistance;
            reachLastPosition = transform.position;
            updateDelegate = UpdateReachDestination;
        }

        private void ReachDestination ()
        {
            mover.rotationSpeed = defaultMoveTurnSpeed;
            EnableBlocker(true);
            MarkExecutionsReached();
            graphSelector.SetGraphToDefault();
            PrintLog("Reached Destination");
            CallbackReachDestination();
        }

        private void MarkExecutionsReached ()
        {
            for (int i = 0; i < executions.Count; i++)
            {
                if (!executions[i].reached)
                {
                    if (Vector3.Distance(destination, executions[i].destination) <= 0)
                    {
                        executions[i].reached = true;
                    }
                }
            }
        }

        private void UpdatePathDestination ()
        {
            mover.destination = destination;
        }

        private void EnableStandStill ()
        {
            mover.canMove = true;
        }

        private void EnableBlocker (bool enable)
        {
            if (blocker != null)
                blocker.enabled = enable;
        }

        private void PrintLog (string message)
        {
            if (printLog)
            {
                ReDebug.Log("Path Find Agent", message);
            }
        }

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
#endif
    }
}