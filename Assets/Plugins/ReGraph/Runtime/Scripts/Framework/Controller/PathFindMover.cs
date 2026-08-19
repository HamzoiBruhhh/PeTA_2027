using UnityEngine;
#if REGRAPH_PATHFIND
using Pathfinding;
using Reshape.Unity;
#endif

namespace Reshape.ReFramework
{
#if REGRAPH_PATHFIND
    public class PathFindMover : AIPath
    {
        public delegate void CallDelegate ();

        public delegate void PathDelegate (Path path);

        public event PathDelegate onPathCalculated;
        public event CallDelegate onPathReached;

        public Path calculatedPath;

        private bool pathing;

        public bool isPathing => pathing;

        public void CalculatePath ()
        {
            isStopped = false;
            calculatedPath = null;
            SearchPath();
            pathing = true;
        }

        public void ClearCurrentPath ()
        {
            isStopped = true;
            ClearPath();
            pathing = false;
        }

        public bool GetPathEndPoint (out Vector3 endPoint)
        {
            if (interpolator.valid)
            {
                endPoint = interpolator.endPoint;
                return true;
            }

            endPoint = Vector3.zero;
            return false;
        }

        protected override void MovementUpdateInternal (float deltaTime, out Vector3 nextPosition, out Quaternion nextRotation)
        {
            deltaTime *= ReTime.deltaModifier;
            base.MovementUpdateInternal(deltaTime, out nextPosition, out nextRotation);
        }

        public override void OnTargetReached ()
        {
            base.OnTargetReached();
            pathing = false;
            onPathReached?.Invoke();
        }

        protected override void OnPathComplete (Path newPath)
        {
            calculatedPath = newPath;
            base.OnPathComplete(newPath);
            onPathCalculated?.Invoke(newPath);
        }

        public float GetShapeRadius ()
        {
            return radius;
        }

        public override void Teleport (Vector3 newPosition, bool clearPath = true)
        {
            var nearest = AstarPath.active != null ? AstarPath.active.GetNearest(newPosition) : new NNInfo();
            movementPlane.ToPlane(newPosition, out var elevation);
            newPosition = movementPlane.ToWorld(movementPlane.ToPlane(nearest.node != null ? nearest.position : newPosition), elevation);
            base.Teleport(newPosition, clearPath);
        }
    }
#else
    public class PathFindMover : MonoBehaviour { }
#endif
}