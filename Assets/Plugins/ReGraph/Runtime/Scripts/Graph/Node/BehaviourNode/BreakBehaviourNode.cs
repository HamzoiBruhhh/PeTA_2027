using Sirenix.OdinInspector;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class BreakBehaviourNode : BehaviourNode
    {
        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            return State.Break;
        }
        
        public override bool IsRequireUpdate ()
        {
            return enabled;
        }

#if UNITY_EDITOR
        public static string displayName = "Break Behaviour Node";
        public static string nodeName = "Break";

        public override string GetNodeInspectorTitle ()
        {
            return displayName;
        }

        public override string GetNodeViewTitle ()
        {
            return nodeName;
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Logic/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            return "Stop execution";
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will execute a stop the execution and return fail.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}