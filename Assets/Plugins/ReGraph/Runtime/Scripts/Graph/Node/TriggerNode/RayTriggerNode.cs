using System;
using System.Collections;
using Reshape.ReFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class RayTriggerNode : TriggerNode
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("MarkDirty")]
        public Type triggerType;

        [SerializeField]
        [ValueDropdown("DrawActionName1ListDropdown", ExpandAllMenuItems = true)]
        [OnValueChanged("MarkDirty")]
        private ActionNameChoice actionName;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            State state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                if (execution.type == triggerType && execution.type is Type.RayAccepted or Type.RayMissed or Type.RayHit or Type.RayLeave or Type.RayArrive)
                {
                    if (actionName != null && execution.parameters.actionName != null && execution.parameters.actionName.Equals(actionName))
                    {
                        execution.variables.SetState(guid, State.Success);
                        state = State.Success;
                    }
                }
                else if (execution.type == triggerType && execution.type is Type.RayStay)
                {
                    if (actionName != null && execution.parameters.actionName != null && execution.parameters.actionName.Equals(actionName))
                    {
                        var controller = execution.parameters.interactedMono.Remember<RayCastingController>(RayCastingController.INSTANCE);
                        if (controller)
                        {
                            if (!controller.AddReceiver(context.runner, execution.parameters.actionName))
                            {
                                execution.variables.SetState(guid, State.Success);
                                state = State.Success;
                            }
                        }
                    }
                }
                else if (triggerType is Type.RayArrive && execution.type is Type.RayStay)
                {
                    if (actionName != null && execution.parameters.actionName != null && execution.parameters.actionName.Equals(actionName))
                    {
                        var controller = execution.parameters.interactedMono.Remember<RayCastingController>(RayCastingController.INSTANCE);
                        if (controller)
                            controller.AddReceiver(context.runner, execution.parameters.actionName);
                    }
                }
                else if (execution.type == Type.All)
                {
                    if (execution.parameters.actionName != null && execution.parameters.actionName.Equals(TriggerId))
                    {
                        execution.variables.SetState(guid, State.Success);
                        state = State.Success;
                    }
                }

                if (state != State.Success)
                {
                    execution.variables.SetState(guid, State.Failure);
                    state = State.Failure;
                }
                else
                    OnSuccess();
            }

            if (state == State.Success)
                return base.OnUpdate(execution, updateId);
            return State.Failure;
        }
        
        public override bool IsTrigger (TriggerNode.Type type, int paramInt = 0)
        {
            return type == triggerType;
        }

#if UNITY_EDITOR
        private IEnumerable TriggerTypeChoice ()
        {
            ValueDropdownList<Type> menu = new ValueDropdownList<Type>();
            menu.Add("Being Accepted", Type.RayAccepted);
            menu.Add("Being Missed", Type.RayMissed);
            menu.Add("Being Hit Yet Accept", Type.RayHit);
            menu.Add("Arrive", Type.RayArrive);
            menu.Add("Stay", Type.RayStay);
            menu.Add("Leave", Type.RayLeave);
            return menu;
        }

        private static IEnumerable DrawActionName1ListDropdown ()
        {
            return ActionNameChoice.GetActionNameListDropdown();
        }

        public static string displayName = "Ray Trigger Node";
        public static string nodeName = "Ray";
        
        public override Type GetNodeType ()
        {
            return triggerType;
        }

        public override string GetNodeInspectorTitle ()
        {
            return displayName;
        }

        public override string GetNodeViewTitle ()
        {
            return nodeName;
        }
        
        public override string GetNodeIdentityName ()
        {
            return triggerType.ToString();
        }

        public override string GetNodeViewDescription ()
        {
            string desc = String.Empty;
            if (triggerType == Type.RayAccepted && actionName != null)
                desc = "Ray Being Accepted at " + actionName + " action";
            else if (triggerType == Type.RayMissed && actionName != null)
                desc = "Ray Being Missed at " + actionName + " action";
            else if (triggerType == Type.RayHit && actionName != null)
                desc = "Ray Being Hit Yet Accept at " + actionName + " action";
            else if (triggerType == Type.RayArrive && actionName != null)
                desc = "Ray arrive at " + actionName + " action";
            else if (triggerType == Type.RayStay && actionName != null)
                desc = "Ray stay at " + actionName + " action";
            else if (triggerType == Type.RayLeave && actionName != null)
                desc = "Ray leave at " + actionName + " action";
            return desc;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.RayAccepted)
                tip += "This will get trigger when the ray cast out have been accepted.\n\nAccepted means the ray hit an collision and the hit collision's gameObject have Graph that contain Ray Received Trigger node.\n\n";
            else if (triggerType == Type.RayMissed)
                tip += "This will get trigger when the ray cast out and not hit anything.\n\n";
            else if (triggerType == Type.RayHit)
                tip += "This will get trigger when the ray cast out and hit anything but the hit collision does not have Graph to accept the ray.\n\n";
            else if (triggerType == Type.RayArrive)
                tip += "This will get trigger when the Graph first receive a ray cast.\n\n";
            else if (triggerType == Type.RayStay)
                tip += "This will get trigger when the Graph keep receiving the ray cast.\n\n";
            else if (triggerType == Type.RayLeave)
                tip += "This will get trigger when the Graph have received a ray cast previously and right now the ray have leave the collision.\n\n";
            else
                tip += "This will get trigger all Raycast related events.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}