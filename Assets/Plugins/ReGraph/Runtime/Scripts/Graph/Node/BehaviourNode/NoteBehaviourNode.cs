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
    public class NoteBehaviourNode : BehaviourNode
    {
        [SerializeField]
        [OnInspectorGUI("UpdateMessage")]
        private GraphNoteProperty message;
        
        public GraphNoteProperty Message => message;

#if UNITY_EDITOR
        private void UpdateMessage ()
        {
            if (message.dirty)
            {
                MarkDirty();
                message.dirty = false;
            }
        }
        
        public override ChildrenType GetChildrenType ()
        {
            return ChildrenType.None;
        }
        
        public static string displayName = "Note Behaviour Node";
        public static string nodeName = "Note";

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
            return "Write down dev note here ...";
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will provide developer put notes for troubleshooting.";
        }
#endif
    }
}