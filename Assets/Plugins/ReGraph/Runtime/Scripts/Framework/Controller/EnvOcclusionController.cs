using System.Collections.Generic;
using Reshape.ReGraph;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.Unity;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class EnvOcclusionController : BaseBehaviour
    {
        private static List<EnvOcclusionController> list;

        public enum OcclusionEntity
        {
            None,
            Emitter = 10,
            Receiver = 50,
            Blocker = 100,
        }

        private class OccludeInfo
        {
            public GameObject go;
            public EnvOcclusionController controller;
            public bool isHit;
        }

        [LabelText("Type")]
        public OcclusionEntity entity;

        public ActionNameChoice actionName;

        [ShowIf("@entity == OcclusionEntity.Blocker")]
        public Renderer occludeRenderer;

        [ShowIf("@entity == OcclusionEntity.Blocker")]
        public Material occludeMaterial;

        [ShowIf("@entity == OcclusionEntity.Emitter")]
        public LayerMask emitLayer;

        public bool printLog;

        private List<OccludeInfo> occludeList;
        private int occlusionState;
        private GraphRunner runner;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        private static GameObject GetReceiverObject (ActionNameChoice actionName)
        {
            if (list != null)
                for (var i = 0; i < list.Count; i++)
                    if (list[i].entity == OcclusionEntity.Receiver && list[i].actionName == actionName && list[i].enabled)
                        return list[i].gameObject;
            return null;
        }

        private static EnvOcclusionController FindBlocker (GameObject go, ActionNameChoice actionName)
        {
            if (list != null)
                for (var i = 0; i < list.Count; i++)
                    if (list[i].entity == OcclusionEntity.Blocker && list[i].gameObject == go && list[i].actionName == actionName && list[i].enabled)
                        return list[i];
            return null;
        }

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public void Terminate ()
        {
            Disable();
            Destroy(this);
        }

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected void Awake ()
        {
            list ??= new List<EnvOcclusionController>();
            list.Add(this);
            occludeList ??= new List<OccludeInfo>();
            gameObject.TryGetComponent(out runner);
        }

        protected void OnDestroy ()
        {
            list?.Remove(this);
        }

        protected void OnDisable ()
        {
            Disable();
        }

        protected void Update ()
        {
            if (entity == OcclusionEntity.Emitter && actionName)
            {
                if (occludeList != null)
                    for (var i = 0; i < occludeList.Count; i++)
                        occludeList[i].isHit = false;
                var targetObject = GetReceiverObject(actionName);
                if (targetObject)
                {
                    var hitBlocker = false;
                    var hitReceiver = false;
                    var startPosition = transform.position;
                    var targetPosition = targetObject.transform.position;
                    var distance = Vector3.Distance(targetPosition, startPosition);
                    var direction = (targetPosition - startPosition).normalized;
                    var hits = ReRaycast.LineCastAll(startPosition, direction, distance, emitLayer, 20);
                    for (var i = 0; i < hits.Length; i++)
                    {
                        var hit = hits[i];
                        if (hit.collider.gameObject == targetObject)
                            hitReceiver = true;
                        else
                        {
                            var colliderGo = hit.collider.gameObject;
                            var occlude = FindOcclude(colliderGo);
                            if (occlude == null)
                            {
                                var controller = FindBlocker(colliderGo, actionName);
                                if (controller)
                                {
                                    occlude = new OccludeInfo {controller = controller, go = colliderGo, isHit = true};
                                    occludeList?.Add(occlude);
                                    occlude.controller.ExecuteOcclusion();
                                    hitBlocker = true;
                                }
                            }
                            else
                            {
                                if (occlude.controller && occlude.controller.enabled && occlude.controller.actionName == actionName)
                                {
                                    occlude.isHit = true;
                                    hitBlocker = true;
                                }
                            }

                            if (printLog)
                            {
                                Debug.DrawRay(startPosition, direction * distance, Color.green);
                                Debug.Log(gameObject.name + " is blocked by: " + colliderGo.name);
                            }
                        }
                    }

                    if (hitReceiver && !hitBlocker)
                        ClearOcclusion();
                    else
                        ClearNonHitOcclusion();
                }
                else
                    ClearNonHitOcclusion();
            }
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void Disable ()
        {
            if (entity == OcclusionEntity.Emitter)
                ClearOcclusion();
            else if (entity == OcclusionEntity.Blocker)
            {
                if (occlusionState > 0)
                    DeactivateOcclusion();
                occlusionState = 0;
            }
        }

        private void ClearNonHitOcclusion ()
        {
            ClearOcclusion(0, false);
        }

        private void ClearOcclusion (int hit = -1, bool all = true)
        {
            if (occludeList != null)
            {
                for (var i = 0; i < occludeList.Count; i++)
                {
                    if (hit == -1 || (hit == 0 && !occludeList[i].isHit) || (hit == 1 && occludeList[i].isHit))
                    {
                        var blocker = occludeList[i];
                        if (blocker.controller)
                            if (all || blocker.controller.enabled)
                                blocker.controller.ResetOcclusion();
                        occludeList.Remove(blocker);
                        i--;
                    }
                }
            }
        }

        private OccludeInfo FindOcclude (GameObject go)
        {
            if (occludeList != null)
            {
                for (var i = 0; i < occludeList.Count; i++)
                    if (occludeList[i].go == go)
                        return occludeList[i];
            }

            return null;
        }

        private void ExecuteOcclusion ()
        {
            occlusionState++;
            if (occlusionState == 1)
                ActivateOcclusion();
        }

        private void ResetOcclusion ()
        {
            occlusionState--;
            if (occlusionState == 0)
                DeactivateOcclusion();
        }

        private void ActivateOcclusion ()
        {
            if (runner)
                runner.TriggerOcclusion(TriggerNode.Type.OcclusionStart, actionName);
        }

        private void DeactivateOcclusion ()
        {
            if (runner)
                runner.TriggerOcclusion(TriggerNode.Type.OcclusionEnd, actionName);
        }

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}