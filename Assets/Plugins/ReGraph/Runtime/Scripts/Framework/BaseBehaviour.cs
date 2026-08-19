using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Reshape.Unity;
using Sirenix.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [AddComponentMenu("")]
    public class BaseBehaviour : ReMonoBehaviour
    {
        private static GameObject empty;

        protected virtual void Start () { }

        protected string goName => gameObject != null && !gameObject.Equals(null) && !gameObject.SafeIsUnityNull() ? gameObject.name : string.Empty;

        protected string baseId => reid;

        public bool IsGameObjectMatch (GameObject go, string[] excludeTags, string[] excludeLayers, string[] onlyTags, string[] onlyLayers, string[] specificNames)
        {
            return IsGameObjectMatchFilter(go, excludeTags, excludeLayers, onlyTags, onlyLayers, specificNames);
        }

        [SpecialName]
        public GameObject GetEmpty ()
        {
            if (empty == null)
                empty = new GameObject("StaticEmptyGo");
            DontDestroyOnLoad(empty);
            empty.hideFlags = HideFlags.HideInHierarchy;
            return empty;
        }

        [SpecialName]
        public bool IsPositionBehindLocation (Vector3 ownerPos, Vector3 targetPos, Quaternion targetRot, out float yaw, out float pitch, out Vector3Int direction)
        {
            var localDir = Quaternion.Inverse(targetRot) * (ownerPos - targetPos);
            var isForward = localDir.z > 0;
            var isUp = localDir.y > 0;
            var isRight = localDir.x > 0;
            //-- NOTE yaw more than 90 and less than -90 is represent behind the unit
            yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            pitch = Vector3.Angle(Vector3.down, localDir) - 90f;
            direction = new Vector3Int(isRight ? -1 : 1, isUp ? 1 : -1, isForward ? 1 : -1);
            return !isForward;
        }

        [SpecialName]
        public Transform ScanPointsInCircle (Transform tool, Transform source, Transform center, float radius, int angleIncrement, float outer,
            Func<Transform, Transform, List<CharacterOperator>, float> pointVerification, List<CharacterOperator> pointVerificationExclude, ref int step, ref int outerCount, out bool reachScanLimit,
            int maxOuterLoop = 10, int timesLimit = 0, int direction = 0)
        {
            reachScanLimit = false;
            var times = 0;
            var totalStep = 360 / angleIncrement;
            var firstPos = Vector3.zero;
            var firstRot = Vector3.zero;
            var firstSize = Vector3.zero;
            var firstDistance = 0f;
            var haveFirst = false;
            var currentRadius = radius;
            while (step < totalStep)
            {
                times++;
                if (timesLimit > 0 && times > timesLimit)
                {
                    reachScanLimit = true;
                    break;
                }

                GetPointInCircle(tool, source, center, currentRadius, angleIncrement, step, direction);
                var resultDistance = pointVerification.Invoke(source, tool, pointVerificationExclude);
                if (resultDistance > 0)
                {
                    if (!haveFirst)
                    {
                        haveFirst = true;
                        firstPos = tool.position;
                        firstRot = tool.eulerAngles;
                        firstSize = tool.localScale;
                        firstDistance = resultDistance;
                    }
                    else
                    {
                        if (firstDistance <= resultDistance)
                        {
                            tool.position = firstPos;
                            tool.eulerAngles = firstRot;
                            tool.localScale = firstSize;
                        }

                        return tool;
                    }
                }

                step++;
                if (outer != 0)
                {
                    if (step >= totalStep)
                    {
                        step = 0;
                        outerCount++;
                        currentRadius = radius + (outer * outerCount);
                        if (outerCount >= maxOuterLoop)
                            break;
                    }
                }
            }

            if (haveFirst)
            {
                tool.position = firstPos;
                tool.eulerAngles = firstRot;
                tool.localScale = firstSize;
                return tool;
            }

            return null;
        }

        [SpecialName]
        public void GetPointInCircle (Transform tool, Transform source, Transform center, float radius, int angleIncrement, int step, int direction)
        {
            if (direction == 2)
            {
                tool.position = center.position;
                tool.position += -center.forward * radius;
            }
            else if (direction == 3)
            {
                tool.position = center.position;
                tool.position += center.forward * radius;
            }
            else
            {
                var sourcePos = source.position;
                sourcePos.y = center.position.y;
                if (Vector3.Distance(center.position, sourcePos) < radius)
                {
                    tool.position = center.position;
                    tool.LookAt(source);
                    tool.position += tool.forward * radius;
                }
                else
                {
                    tool.position = Vector3.MoveTowards(center.position, sourcePos, radius);
                }
            }

            tool.LookAt(center);
            var angle = 0f;
            if (step > 0)
                angle += step % 2 == 1 ? ((step / 2) + 1) * angleIncrement : (step / 2) * -angleIncrement;
            if (direction == 1)
                angle += 180f;
            tool.RotateAround(center.position, Vector3.up, angle);
        }

        [SpecialName]
        public new void ClearPool (string type)
        {
            ReMonoBehaviour.ClearPool(type);
        }

        public new void InsertIntoPool (string type, GameObject poolObject, bool parentPoolGo = false)
        {
            ReMonoBehaviour.InsertIntoPool(type, poolObject, parentPoolGo);
        }

        public new void InsertIntoPool (GameObject poolObject, bool parentPoolGo = false)
        {
            InsertIntoPool(poolObject.name, poolObject, parentPoolGo);
        }

        public new GameObject TakeFromPool (string type, GameObject cloneObject, Transform cloneLocation, bool activeOn = false, Transform parent = null)
        {
            return ReMonoBehaviour.TakeFromPool(type, cloneObject, cloneLocation, activeOn, parent);
        }

        public new GameObject TakeFromPool (GameObject cloneObject, Transform cloneLocation, bool activeOn = false, Transform parent = null)
        {
            return ReMonoBehaviour.TakeFromPool(cloneObject, cloneLocation, activeOn, parent);
        }

        public new GameObject TakeFromPool (GameObject cloneObject, Vector3 clonePos, Quaternion closeRot, bool activeOn = false, Transform parent = null)
        {
            return ReMonoBehaviour.TakeFromPool(cloneObject, clonePos, closeRot, activeOn, parent);
        }

        [SpecialName]
        public override bool ReceivedRayCast (ReMonoBehaviour mono, string rayName, RaycastHit? hit)
        {
            return base.ReceivedRayCast(mono, rayName, hit);
        }

        [SpecialName]
        public override void InitSystemFlow ()
        {
            base.InitSystemFlow();
        }

        [SpecialName]
        public override void ClearSystemFlow ()
        {
            base.ClearSystemFlow();
        }

        [SpecialName]
        public override void UpdateSystemFlow ()
        {
            base.UpdateSystemFlow();
        }

        [SpecialName]
        public override void StartSystemInitFlow ()
        {
            base.StartSystemInitFlow();
        }

        [SpecialName]
        public override void StartSystemTickFlow ()
        {
            base.StartSystemTickFlow();
        }

        [SpecialName]
        public override void StartSystemBeginFlow ()
        {
            base.StartSystemBeginFlow();
        }

        [SpecialName]
        public override void StartSystemUninitFlow ()
        {
            base.StartSystemUninitFlow();
        }

        [SpecialName]
        public void GenerateBaseId ()
        {
            GenerateReId();
        }

        [SpecialName]
        public void GenerateBaseIdIfNotExist ()
        {
            GenerateReIdIfNotExist();
        }

        [SpecialName]
        public override void GenerateReId ()
        {
            base.GenerateReId();
        }
        
        [SpecialName]
        public override void GenerateReIdIfNotExist ()
        {
            base.GenerateReIdIfNotExist();
        }

        [SpecialName]
        public override void PlanPreInit ()
        {
            base.PlanPreInit();
        }

        [SpecialName]
        public override void PlanInit ()
        {
            base.PlanInit();
        }

        [SpecialName]
        public override void PlanPostInit ()
        {
            base.PlanPostInit();
        }

        [SpecialName]
        public override void PlanPreBegin ()
        {
            base.PlanPreBegin();
        }

        [SpecialName]
        public override void PlanBegin ()
        {
            base.PlanBegin();
        }

        [SpecialName]
        public override void PlanPostBegin ()
        {
            base.PlanPostBegin();
        }

        [SpecialName]
        public override void PlanPreTick (string @group = null)
        {
            base.PlanPreTick(@group);
        }

        [SpecialName]
        public override void PlanTick (string @group = null)
        {
            base.PlanTick(@group);
        }

        [SpecialName]
        public override void PlanPostTick (string @group = null)
        {
            base.PlanPostTick(@group);
        }

        [SpecialName]
        public override void PlanPreUninit ()
        {
            base.PlanPreUninit();
        }

        [SpecialName]
        public override void PlanUninit ()
        {
            base.PlanUninit();
        }

        [SpecialName]
        public override void PlanPostUninit ()
        {
            base.PlanPostUninit();
        }

        [SpecialName]
        public override void OmitPreTick ()
        {
            base.OmitPreTick();
        }

        [SpecialName]
        public override void OmitTick ()
        {
            base.OmitTick();
        }

        [SpecialName]
        public override void OmitPostTick ()
        {
            base.OmitPostTick();
        }

        [SpecialName]
        public override void OmitPreUninit ()
        {
            base.OmitPreUninit();
        }

        [SpecialName]
        public override void OmitUninit ()
        {
            base.OmitUninit();
        }

        [SpecialName]
        public override void OmitPostUninit ()
        {
            base.OmitPostUninit();
        }

        [SpecialName]
        public override void PausePreTick (string @group)
        {
            base.PausePreTick(@group);
        }

        [SpecialName]
        public override void PauseTick (string @group)
        {
            base.PauseTick(@group);
        }

        [SpecialName]
        public override void PausePostTick (string @group)
        {
            base.PausePostTick(@group);
        }

        [SpecialName]
        public override void UnpausePreTick (string @group)
        {
            base.UnpausePreTick(@group);
        }

        [SpecialName]
        public override void UnpauseTick (string @group)
        {
            base.UnpauseTick(@group);
        }

        [SpecialName]
        public override void UnpausePostTick (string @group)
        {
            base.UnpausePostTick(@group);
        }

        [SpecialName]
        public override void PreInit ()
        {
            base.PreInit();
        }

        [SpecialName]
        public override void Init ()
        {
            base.Init();
        }

        [SpecialName]
        public override void PostInit ()
        {
            base.PostInit();
        }

        [SpecialName]
        public override void PreBegin ()
        {
            base.PreBegin();
        }

        [SpecialName]
        public override void Begin ()
        {
            base.Begin();
        }

        [SpecialName]
        public override void PostBegin ()
        {
            base.PostBegin();
        }

        [SpecialName]
        public override void PreTick ()
        {
            base.PreTick();
        }

        [SpecialName]
        public override void Tick ()
        {
            base.Tick();
        }

        [SpecialName]
        public override void PostTick ()
        {
            base.PostTick();
        }

        [SpecialName]
        public override void PreUninit ()
        {
            base.PreUninit();
        }

        [SpecialName]
        public override void Uninit ()
        {
            base.Uninit();
        }

        [SpecialName]
        public override void PostUninit ()
        {
            base.PostUninit();
        }

        [SpecialName]
        public override void CancelWait (string id)
        {
            base.CancelWait(id);
        }

        [SpecialName]
        public override void StopWait (string id)
        {
            base.StopWait(id);
        }

        [SpecialName]
        public override void ResumeWait (string id)
        {
            base.ResumeWait(id);
        }

        [SpecialName]
        public override void Forget (string key)
        {
            base.Forget(key);
        }

        [SpecialName]
        public override void Forget (int key)
        {
            base.Forget(key);
        }

#if UNITY_EDITOR
        [HideInInspector]
        public bool showHints;

        [MenuItem("CONTEXT/BaseBehaviour/Hints Display/Show", false)]
        public static void ShowHints (MenuCommand command)
        {
            var comp = (BaseBehaviour) command.context;
            comp.showHints = true;
        }

        [MenuItem("CONTEXT/BaseBehaviour/Hints Display/Show", true)]
        public static bool IsShowHints (MenuCommand command)
        {
            var comp = (BaseBehaviour) command.context;
            if (comp.showHints)
                return false;
            return true;
        }

        [MenuItem("CONTEXT/BaseBehaviour/Hints Display/Hide", false)]
        public static void HideHints (MenuCommand command)
        {
            var comp = (BaseBehaviour) command.context;
            comp.showHints = false;
        }

        [MenuItem("CONTEXT/BaseBehaviour/Hints Display/Hide", true)]
        public static bool IsHideHints (MenuCommand command)
        {
            var comp = (BaseBehaviour) command.context;
            if (!comp.showHints)
                return false;
            return true;
        }
#endif
    }
}