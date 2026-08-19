using System;
using System.Collections;
using Reshape.ReFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class OcclusionTriggerNode : TriggerNode
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
                if (execution.type == triggerType && execution.type is Type.OcclusionStart or Type.OcclusionEnd)
                {
                    if (actionName != null && execution.parameters.actionName != null && execution.parameters.actionName.Equals(actionName))
                    {
                        execution.variables.SetState(guid, State.Success);
                        state = State.Success;
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
            menu.Add("On Activate", Type.OcclusionStart);
            menu.Add("On Deactivate", Type.OcclusionEnd);
            return menu;
        }

        private static IEnumerable DrawActionName1ListDropdown ()
        {
            return ActionNameChoice.GetActionNameListDropdown();
        }

        public static string displayName = "Occlusion Trigger Node";
        public static string nodeName = "Occlusion";
        
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
            var desc = string.Empty;
            if (triggerType == Type.OcclusionStart && actionName != null)
                desc = "Activated at " + actionName + " action";
            else if (triggerType == Type.OcclusionEnd && actionName != null)
                desc = "Deactivated at " + actionName + " action";
            return desc;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.OcclusionStart)
                tip += "This will get trigger when the occlusion being activate.\n\nActivate means the occlusion object appear in between occlusion emitter and receiver.\n\n";
            else if (triggerType == Type.OcclusionEnd)
                tip += "This will get trigger when the occlusion being deactivate.\n\n";
            else
                tip += "This will get trigger all Occlusion related events.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}