using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class ActionTriggerNode : TriggerNode
    {
        [SerializeField]
        [ValueDropdown("DrawActionName1ListDropdown", ExpandAllMenuItems = true)]
        [OnValueChanged("MarkDirty")]
        private ActionNameChoice actionName;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            State state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                if (execution.type == Type.ActionTrigger)
                {
                    if (actionName != null && execution.parameters.actionName != null && execution.parameters.actionName.Equals(actionName))
                    {
                        execution.variables.SetState(guid, State.Success);
                        state = State.Success;
                        if (context.graph.isBehaviourPack)
                            execution.parameters.behaviourData.PrepareExecution();
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

        public string GetActionName ()
        {
            if (actionName == null)
                return string.Empty;
            return actionName;
        }

        public override bool IsTrigger (TriggerNode.Type type, int paramInt = 0)
        {
            return type == Type.ActionTrigger;
        }

#if UNITY_EDITOR
        public string ActionName => actionName == null ? string.Empty : actionName;

        private static IEnumerable DrawActionName1ListDropdown ()
        {
            return ActionNameChoice.GetActionNameListDropdown();
        }

        public static string displayName = "Action Trigger Node";
        public static string nodeName = "Action";

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
            return actionName;
        }

        public override string GetNodeViewDescription ()
        {
            return actionName == null ? string.Empty : "Action Name : " + actionName;
        }

        public override string GetNodeViewTooltip ()
        {
            return "This will get trigger when its action name is same with the Action Behaviour node have execution the action.\n\nAn action can be understand as a group of Behaviour nodes.\n\n" +
                   base.GetNodeViewTooltip();
        }
#endif
    }
}