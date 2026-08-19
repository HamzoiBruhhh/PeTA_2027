using System.Collections;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class ApplicationBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            Quit = 10
        }

        [SerializeField]
        [LabelText("Execution")]
        [OnValueChanged("MarkDirty")]
        private ExecutionType executionType;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType == ExecutionType.None)
            {
                LogWarning("Found an empty Application Behaviour node in " + context.objectName);
            }
            else
            {
                if (executionType == ExecutionType.Quit)
                {
#if UNITY_EDITOR
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
#else
                    Application.Quit();
#endif
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        public static string displayName = "Application Behaviour Node";
        public static string nodeName = "Application";

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
            return executionType.ToString();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return nodeName;
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType == ExecutionType.Quit)
            {
                return "Quit Application";
            }

            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will provide several Application controls (quit).\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}