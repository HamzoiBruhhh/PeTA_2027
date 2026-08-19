using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using Reshape.ReFramework;
using Reshape.Unity;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class RunnerTriggerNode : TriggerNode 
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("MarkDirty")]
        public Type triggerType;
        
        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            State state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                if (execution.type is Type.OnStart or Type.OnEnd or Type.OnEnable or Type.OnDeactivate or Type.OnActivate && execution.type == triggerType)
                {
                    execution.variables.SetState(guid, State.Success);
                    state = State.Success;
                }
                else if (execution.type == Type.All)
                {
                    if (execution.parameters.actionName.Equals(TriggerId))
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

        public override bool IsRequireBegin ()
        {
            if (triggerType == Type.OnStart)
                return true;
            return false;
        }
        
        public override bool IsRequirePreUninit ()
        {
            if (triggerType == Type.OnEnd)
                return true;
            return false;
        }
        
        public override bool IsTrigger (TriggerNode.Type type, int paramInt = 0)
        {
            return type == triggerType;
        }

#if UNITY_EDITOR
        private IEnumerable TriggerTypeChoice ()
        {
            var menu = new ValueDropdownList<Type> {{"On Start", Type.OnStart}, {"On End", Type.OnEnd}, {"On Enable", Type.OnEnable}, {"On Deactivate", Type.OnDeactivate}, {"On Activate", Type.OnActivate}};
            return menu;
        }

        public static string displayName = "Runner Trigger Node";
        public static string nodeName = "Runner";
        
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
            if (triggerType == Type.OnStart)
                return "When graph runner first time enable";
            if (triggerType == Type.OnEnd)
                return "When graph runner terminate with scene unloading";
            if (triggerType == Type.OnEnable)
                return "When graph runner get enable again";
            if (triggerType == Type.OnDeactivate)
                return "When graph runner get deactivate";
            if (triggerType == Type.OnActivate)
                return "When graph runner get activate";
            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.OnStart)
                tip += "This will get trigger at the first time this graph runner get enable.\n\n";
            if (triggerType == Type.OnEnd)
                tip +=  "This will get trigger at scene unloading which control by Graph Manager. Graph Manager will terminate all graph runners on the scene before unload the scene.\n\n";
            if (triggerType == Type.OnEnable)
                tip += "This will get trigger at this graph runner get enable again after the first time enable.\n\n";
            if (triggerType == Type.OnDeactivate)
                tip += "This will get trigger at this graph runner get deactivate by GameObjectBehaviourNode.\n\n";
            if (triggerType == Type.OnActivate)
                tip += "This will get trigger at this graph runner get activate by GameObjectBehaviourNode.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}