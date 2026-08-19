using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReFramework;
using Reshape.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class ConnectorBehaviourNode : BehaviourNode
    {
#if UNITY_EDITOR
        public static string displayName = "Connector Behaviour Node";
        public static string nodeName = "Connector";

        public override string GetNodeInspectorTitle ()
        {
            return displayName;
        }

        public override string GetNodeViewTitle ()
        {
            return string.Empty;
        }

        public override string GetNodeMenuDisplayName ()
        {
            return nodeName;
        }

        public override string GetNodeViewDescription ()
        {
            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            return string.Empty;
        }
#endif
    }
}