using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.Unity;
#if REGRAPH_PATHFIND
using Pathfinding;
#endif

namespace Reshape.ReFramework
{
    [AddComponentMenu("")]
    [HideMonoScript]
    public class MotorAgent : BaseBehaviour
    {
        public delegate void UpdateDelegate ();

        public event UpdateDelegate onReachDestination;
        
        public bool immediateTurn;
        public float defaultMoveTurnSpeed;
        
        private Transform body;
        private Vector3 lastPosition;
        private Vector3 moveDirection;
        private bool moving;

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public virtual bool SetDestination (Vector3 dest, string executeId = "", string layerName = "", bool ignoreRotation = false, float rotationSpeed = 0)
        {
            return false;
        }

        public virtual bool SetTeleport (Vector3 dest, string executeId = "")
        {
            return false;
        }

        public virtual void Push (Vector3 dest, float speed, string executeId = "") { }

        public virtual bool GetEndPoint (out Vector3 point)
        {
            point = Vector3.zero;
            return false;
        }

        public virtual List<Vector3> GetPoints ()
        {
            return null;
        }

        public virtual bool CalculateDestinationPoint (Vector3 source, Vector3 dest, out Vector3 destPoint, out float destDistance)
        {
            destPoint = Vector3.zero;
            destDistance = 0f;
            return false;
        }

        public virtual bool IsReachDestination (string executeId, bool clearIfReached = false)
        {
            return false;
        }

        public virtual float GetStoppingDistance ()
        {
            return default;
        }

        public virtual float GetInteractDistance ()
        {
            return default;
        }
        
        public virtual float GetFacing ()
        {
            return body.eulerAngles.y;
        }

        public virtual void SetFacing (float face)
        {
            Vector3 rot = body.eulerAngles;
            rot.y = face;
            body.eulerAngles = rot;
        }

        public virtual void LookAt (Vector3 location)
        {
            var pos = location;
            pos.y = body.position.y;
            body.LookAt(pos);
        }

        public virtual Vector3 GetMoveDirection ()
        {
            return moveDirection;
        }
        
        public virtual void SetIgnoreRotation (bool active) { }

        public virtual void SetMoveSpeed (float moveSpeed) { }

        public virtual float GetMoveSpeed ()
        {
            return 0;
        }

        public virtual bool IsMoving ()
        {
            return moving;
        }

        public virtual void SetMoveTurnSpeed (float turnSpeed, bool immediate = false) { }

        public virtual void ResetMove () { }

        public virtual void ClearMove () { }

        public virtual void CancelMove (bool fakeReach = false) { }

        public virtual void StopMove () { }

        public virtual void ResumeMove () { }

        public virtual void PauseMove () { }

        public virtual void UnpauseMove () { }

        public Transform GetBody ()
        {
            return body;
        }

        public virtual float GetBodySize ()
        {
            return body.lossyScale.x;
        }

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        protected void Init (Transform trans)
        {
            body = trans;
        }

        protected void CallbackReachDestination ()
        {
            onReachDestination?.Invoke();
        }

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected virtual void LateUpdate ()
        {
            moving = false;
            if (body != null)
            {
                var bodyPos = body.transform.position;
                if (Vector3.Distance(lastPosition, bodyPos) > 0f)
                {
                    moving = true;
                    moveDirection = bodyPos - lastPosition;
                    lastPosition = bodyPos;
                }
            }
        }

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}