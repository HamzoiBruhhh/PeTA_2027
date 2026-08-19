using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.Unity;
using Reshape.ReGraph;
using UnityEngine.Serialization;
#if REGRAPH_FOW
using FoW;
#endif

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class FogOfWarAgent : BaseBehaviour
    {
        private static int raycastMask = 0;
#if REGRAPH_FOW
        public FoW.FogOfWarUnit fogUnit;
#endif
        public Transform rootTransform;

        [Hint("showHints", "Tick this to initialize this component at start. Functionality of this component is not execute without initialization.")]
        public bool enableOnStart;

        [BoxGroup("Reveal FOW")]
        [LabelText("Enable")]
        [Hint("showHints", "Tick this to disable fog unit at start. This is use to remove fog but not under visible state.")]
        public bool enableRevealFow;

        [BoxGroup("Sight Raycast")]
        public bool enableOptimise;

        [BoxGroup("Sight Raycast")]
        [LabelText("Min Amount")]
        public int minSightRaycast = 30;

        [BoxGroup("Sight Raycast")]
        [LabelText("Max Amount")]
        public int maxSightRaycast = 60;

        [BoxGroup("Fog Events")]
        [Hint("showHints", "Define the opponent's fog team id. The fog events functionality will not execute if not able to find the opponent team.")]
        public int opponentTeam = -1;

        [BoxGroup("Fog Events")]
        [Range(0.0f, 1.0f)]
        [Hint("showHints", "Fog visibility is base on opponent team and the fog strength define the value to trigger fog events.")]
        public float minFogStrength = 0.2f;

        [BoxGroup("Fog Events")]
        [Hint("showHints",
            "Value 0 means use the fog unit exact position to check fog hide area. Value more than 0 means the object have extra radius to check fog area, the object radius edge use to check fog hide area.")]
        public float thickness;

        [BoxGroup("Fog Events/Skin In Fog")]
        [LabelText("Enable")]
        [Hint("showHints", "Tick this to run hide execution if it is under fog, it will \"unhide\" if not under fog.")]
        public bool enableFogHide;

        [BoxGroup("Fog Events/Skin In Fog")]
        [Hint("showHints", "Tick this to disable Hidden In Fog functionality after reveal FOW.")]
        [LabelText("Disable At Reveal")]
        public bool disableInReveal;

        [BoxGroup("Fog Events/Skin In Fog")]
        public Renderer[] hideRenderers;

        [BoxGroup("Fog Events/Collider In Fog")]
        [LabelText("Enable")]
        [Hint("showHints", "Tick this to run \"off\" collider execution if it is under fog, it will \"on\" if not under fog.")]
        public bool enableFogCollider;

        [BoxGroup("Fog Events/Collider In Fog")]
        public Collider[] hideColliders;

        [BoxGroup("Fog Events/Object In Fog")]
        [LabelText("Enable")]
        [Hint("showHints", "Tick this to deactivate gameObjects if it is under fog, it will activate gameObjects if not under fog.")]
        public bool enableFogObject;

        [BoxGroup("Fog Events/Object In Fog")]
        public GameObject[] hideObjects;

        [BoxGroup("Fog Events/Action In Fog")]
        [LabelText("Enable")]
        [Hint("showHints", "Tick this to run show/hide action if it is enter/exit fog.")]
        public bool enableFogAction;

        [BoxGroup("Fog Events/Action In Fog")]
        public FlowAction[] showAction;

        [BoxGroup("Fog Events/Action In Fog")]
        public FlowAction[] hideAction;

        private bool? currentVisibility;

        private Dictionary<float, float> fovRangeCustomRate = new()
        {
            {90, 90f},
            {80, 75f},
            {70, 60f},
            {60, 45f},
            {50, 35f},
            {40, 20f},
            {30, 12f},
            {20, 5f},
            {10, 1f},
            {1, 0.1f},
        };

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public void SetFovRange (float value)
        {
#if REGRAPH_FOW
            if (value > 0)
            {
                fogUnit.angle = AdjustCustomRange(value);
                if (enableOptimise)
                    AdjustLineOfSightRaycastElements();
            }
#endif
        }

        public float GetFovDistance ()
        {
#if REGRAPH_FOW
            return fogUnit.circleRadius;
#else
            return 0;
#endif
        }

        public void SetFovDistance (float value)
        {
#if REGRAPH_FOW
            if (value > 0)
                fogUnit.circleRadius = value;
#endif
        }

        public int GetTeam ()
        {
#if REGRAPH_FOW
            return fogUnit.team;
#else
            return -1;
#endif
        }

        public bool IsVisibleAtTeam (int team)
        {
#if REGRAPH_FOW
            var fow = FogOfWarTeam.GetTeam(team);
            if (fow)
                return fow.GetFogValue(FogOfWarValueType.Visible, transform.position) < minFogStrength * 255;
#endif
            return true;
        }

        public bool IsVisibleAtUnit (FogOfWarAgent agent)
        {
#if REGRAPH_FOW
            var fow = FogOfWarTeam.GetTeam(agent.fogUnit.team);
            if (fow != null)
            {
                var fogPos = fow.WorldPositionToFogPosition(transform.position);
                var map = new FogOfWarMap(fow);
                var result = fogUnit.GetVisibility(fow, map, fogPos);
                return result < minFogStrength * 255;
            }
#endif
            return true;
        }

        public bool IsVisibleAtUnit (FogOfWarAgent targetAgent, float agentSize, int[] allowInBetween, int[] grounds)
        {
#if REGRAPH_FOW
            if (IsVisibleAtTeam(targetAgent.fogUnit.team))
            {
                var fow = FogOfWarTeam.GetTeam(targetAgent.fogUnit.team);
                if (fow)
                {
                    var myPos = transform.position;
                    var targetAgentPos = targetAgent.transform.position;
                    var targetFovPos = targetAgentPos;
                    targetFovPos.x += fogUnit.centerPoint.x;
                    targetFovPos.z += fogUnit.centerPoint.y;
                    var inDistance = false;
                    float distance = 0;
                    if (targetAgent.fogUnit.shapeType == FogOfWarShapeType.Box)
                    {
                        distance = targetAgent.fogUnit.boxSize.magnitude / 2f;
                        if (Vector3.Distance(targetFovPos, myPos) <= distance)
                            inDistance = true;
                    }
                    else if (targetAgent.fogUnit.shapeType == FogOfWarShapeType.Circle)
                    {
                        distance = targetAgent.fogUnit.circleRadius;
                        if (Vector3.Distance(targetFovPos, myPos) <= distance)
                            inDistance = true;
                    }

                    if (inDistance)
                    {
                        inDistance = false;
                        var direction = myPos - targetFovPos;
                        if (targetAgent.fogUnit.shapeType == FogOfWarShapeType.Box)
                        {
                            if (Mathf.Abs(direction.x) <= targetAgent.fogUnit.boxSize.x / 2)
                                if (Mathf.Abs(direction.y) <= targetAgent.fogUnit.boxSize.y / 2)
                                    inDistance = true;
                        }
                        else if (targetAgent.fogUnit.shapeType == FogOfWarShapeType.Circle)
                        {
                            var angle = Vector3.Angle(direction, targetAgent.transform.forward);
                            if (Mathf.Abs(angle) <= targetAgent.fogUnit.angle)
                                inDistance = true;
                        }
                    }

                    if (inDistance)
                    {
                        if (allowInBetween is {Length: > 0})
                        {
                            var hits = ReRaycast.CylinderCastAll(targetAgentPos, agentSize, myPos - targetAgentPos, distance, raycastMask);
                            if (hits.Length > 0)
                            {
                                for (var i = 0; i < hits.Length; i++)
                                {
                                    var hitAgent = hits[i].transform.gameObject.GetComponentInParent<FogOfWarAgent>();
                                    if (hitAgent)
                                    {
                                        if (hitAgent == this)
                                            return true;
                                        var found = false;
                                        for (var j = 0; j < allowInBetween.Length; j++)
                                        {
                                            if (hits[i].transform.gameObject.layer == allowInBetween[j])
                                            {
                                                found = true;
                                                break;
                                            }
                                        }

                                        if (found)
                                            continue;
                                        return false;
                                    }

                                    var hitGround = false;
                                    if (grounds != null)
                                    {
                                        for (var j = 0; j < grounds.Length; j++)
                                        {
                                            if (hits[i].transform.gameObject.layer == grounds[j])
                                            {
                                                hitGround = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (!hitGround)
                                        return false;
                                }
                            }
                        }
                        else
                        {
                            if (ReRaycast.CylinderCast(targetAgentPos, agentSize, myPos - targetAgentPos, distance, out var result, raycastMask))
                            {
                                var hitAgent = result.transform.gameObject.GetComponentInParent<FogOfWarAgent>();
                                if (hitAgent == this)
                                    return true;
                            }
                        }
                    }

                    return false;
                }
            }
#endif
            return true;
        }

#if REGRAPH_FOW
        public void ActivateHiddenInFog (FogOfWarUnit unit, Transform trans, int opponent, float fogStrength, Renderer[] renderers, Collider[] colliders, GameObject[] objects)
        {
            enableFogHide = true;
            fogUnit = unit;
            rootTransform = trans;
            opponentTeam = opponent;
            minFogStrength = fogStrength;
            hideRenderers = renderers;
            hideColliders = colliders;
            hideObjects = objects;
        }

        public void ActivateSightRaycastOptimise (FogOfWarUnit unit, Transform trans, int minRaycastAmount, int maxRaycastAmount)
        {
            enableOptimise = true;
            fogUnit = unit;
            rootTransform = trans;
            minSightRaycast = minRaycastAmount;
            maxSightRaycast = maxRaycastAmount;
            AdjustLineOfSightRaycastElements();
        }
#endif

        public void Initialize ()
        {
            if (raycastMask == 0)
                raycastMask = ~GraphManager.instance.runtimeSettings.ignoreFOWLayers.value;
#if REGRAPH_FOW
            if (enableRevealFow && fogUnit.enabled)
                fogUnit.enabled = false;
#endif
            PlanPreTick();
        }

        [SpecialName]
        public void Terminate ()
        {
            Destroy(this);
        }

        public bool GetVisibility ()
        {
            for (var i = 0; i < hideRenderers.Length; i++)
                if (hideRenderers[i] && hideRenderers[i].enabled)
                    return true;
            return false;
        }

        public void SetVisibility (bool value)
        {
            for (var i = 0; i < hideRenderers.Length; i++)
                if (hideRenderers[i] && hideRenderers[i].enabled != value)
                    hideRenderers[i].enabled = value;
        }

        public void SetColliderActive (bool value)
        {
            for (var i = 0; i < hideColliders.Length; i++)
                if (hideColliders[i] != null && hideColliders[i].enabled != value)
                    hideColliders[i].enabled = value;
        }

        public bool HaveCollider (int id)
        {
            for (var i = 0; i < hideColliders.Length; i++)
                if (hideColliders[i] && hideColliders[i].transform.GetInstanceID() == id)
                    return true;
            return false;
        }

        public void SetObjectActive (bool active)
        {
            for (var i = 0; i < hideObjects.Length; i++)
                if (hideObjects[i] != null && hideObjects[i].activeSelf != active)
                    hideObjects[i].SetActiveOpt(active);
        }

        //-----------------------------------------------------------------
        //-- Base Behaviour methods
        //-----------------------------------------------------------------

        [SpecialName]
        public override void PostBegin ()
        {
            Initialize();
            DonePostBegin();
        }

        [SpecialName]
        public override void PreTick ()
        {
#if REGRAPH_FOW
            if (enableOptimise && fogUnit.enabled && fogUnit.lineOfSightMask != 0)
            {
                fogUnit.lineOfSightRaycastAngleOffset = rootTransform.rotation.eulerAngles.y;
            }

            if (enableFogAction || enableFogHide || enableFogCollider || enableFogObject)
            {
                var fow = FogOfWarTeam.GetTeam(opponentTeam);
                if (fow != null)
                {
                    var position = rootTransform.position;
                    var visible = fow.GetFogValue(FogOfWarValueType.Visible, position) < minFogStrength * 255;
                    if (!visible && thickness > 0)
                    {
                        var newPos = position;
                        for (var i = 0; i < 36; i++)
                        {
                            newPos.x = position.x + (thickness * Mathf.Cos(i * 10));
                            newPos.z = position.z + (thickness * Mathf.Sin(i * 10));
                            visible = fow.GetFogValue(FogOfWarValueType.Visible, newPos) < minFogStrength * 255;
                            if (visible)
                                break;
                        }
                    }

                    if (currentVisibility == null || currentVisibility != visible)
                    {
                        currentVisibility = visible;
                        if (FogOfWarManager.agentFogEvents)
                        {
                            if (enableFogHide)
                            {
                                SetVisibility(visible);
                                if (visible && disableInReveal)
                                    enableFogHide = false;
                            }

                            if (enableFogCollider)
                                SetColliderActive(visible);
                            if (enableFogObject)
                                SetObjectActive(visible);
                            if (enableFogAction)
                            {
                                if (visible)
                                    FlowAction.ExecuteList(showAction);
                                else
                                    FlowAction.ExecuteList(hideAction);
                            }
                        }
                    }
                }
            }
#endif
        }

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected void Awake ()
        {
            if (enableOnStart)
                PlanPostBegin();
        }

        protected void OnDestroy ()
        {
            OmitPreTick();
        }

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void AdjustLineOfSightRaycastElements ()
        {
#if REGRAPH_FOW
            if (fogUnit.enabled && fogUnit.lineOfSightMask != 0)
            {
                if (fogUnit.angle * 2 > 180)
                {
                    fogUnit.lineOfSightRaycastAngle = 360;
                    fogUnit.lineOfSightRaycastCount = maxSightRaycast;
                }
                else
                {
                    fogUnit.lineOfSightRaycastAngle = 180;
                    fogUnit.lineOfSightRaycastCount = minSightRaycast;
                }
            }
#endif
        }

        private float AdjustCustomRange (float value)
        {
            if (value <= 0) return 0;
            if (value <= 90)
            {
                var previous = 90f;
                for (var i = 90; i > -1; i -= 10)
                {
                    if (i == 0)
                        i = 1;
                    if (Math.Abs(value - i) < 0.001f)
                        return fovRangeCustomRate[i];
                    if (value > i)
                    {
                        var diff = (value - i) / (previous - i);
                        return fovRangeCustomRate[i] + ((fovRangeCustomRate[previous] - fovRangeCustomRate[i]) * diff);
                    }

                    previous = i;
                }
            }

            return value;
        }

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}