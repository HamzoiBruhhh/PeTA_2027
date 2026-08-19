using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class SpawnTriggerNode : TriggerNode
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
                if (execution.type == Type.GameObjectSpawn)
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
        
        public string GetActionName ()
        {
            if (actionName == null)
                return string.Empty;
            return actionName;
        }
        
        public override bool IsTrigger (TriggerNode.Type type, int paramInt = 0)
        {
            return type == Type.GameObjectSpawn;
        }

#if UNITY_EDITOR
        private static IEnumerable DrawActionName1ListDropdown ()
        {
            return ActionNameChoice.GetActionNameListDropdown();
        }

        public static string displayName = "Spawn Trigger Node";
        public static string nodeName = "Spawn";

        public override string GetNodeInspectorTitle ()
        {
            return displayName;
        }

        public override string GetNodeViewTitle ()
        {
            return nodeName;
        }
        
        public override string GetNodeViewDescription ()
        {
            if (actionName != null)
                return "Being Spawn at "+actionName+" action";
            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will get trigger when its action name is same with the OnSpawn Action name set at GameObject Behaviour node's Spawn execution.\n\nIt get trigger right after the spawn have successful put the gameObject into the scene.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}