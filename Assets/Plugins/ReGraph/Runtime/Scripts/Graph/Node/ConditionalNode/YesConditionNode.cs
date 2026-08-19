using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class YesConditionNode : ConditionNode
    {
        public override void MarkExecute (GraphExecution execution, int updateId, bool condition)
        {
            if (condition)
                MarkExecute(execution, updateId);
            else
                MarkNotExecute(execution, updateId);
        }

        protected override State OnDisabled (GraphExecution execution, int updateId)
        {
            MarkExecute(execution, updateId);
            bool started = execution.variables.GetStarted(guid, false);
            if (!started)
            {
                OnStart(execution, updateId);
                execution.variables.SetStarted(guid, true);
            }

            State state = OnUpdate(execution, updateId);
            if (state != State.Running)
            {
                OnStop(execution, updateId);
                execution.variables.SetStarted(guid, false);
            }

            return state;
        }

#if UNITY_EDITOR
        public static string displayName = "Yes Condition Node";
        public static string nodeName = "Yes";

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
            return "<color=#FFF600>Continue if condition is true</color>";
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "When the parent node is a decision making node, this node will execute when the decision result is positive.";
        }
#endif
    }
}