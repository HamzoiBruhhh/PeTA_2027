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
    public class DebugBehaviourNode : BehaviourNode
    {
        private const string DEBUG_PREFIX = "Graph Debug";
        
        public enum ExecutionType
        {
            None,
            Message = 101,
            Variable = 102,
        }
        
        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType = ExecutionType.Message;
        
        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(message)")]
        [InlineProperty]
        [ShowIf("@executionType == ExecutionType.Message")]
        private StringProperty message;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.Variable")]
        private VariableScriptableObject variable;
        
        [SerializeField]
        [OnValueChanged("MarkDirty")]
        private bool breakPoint;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (!GraphManager.instance.runtimeSettings.skipDebugNode)
            {
                if (executionType == ExecutionType.Message)
                {
                    if (string.IsNullOrEmpty(message) && !breakPoint)
                    {
                        LogWarning("Found an invalid Debug Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(message))
                            ReDebug.Log($"{context.objectName} {DEBUG_PREFIX}", message);
                    }
                }
                else if (executionType == ExecutionType.Variable)
                {
                    if (variable == null && !breakPoint)
                    {
                        LogWarning("Found an invalid Debug Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        if (variable != null)
                            ReDebug.Log($"{context.objectName} {DEBUG_PREFIX}", variable.name + " = " + variable);
                    }
                }
#if UNITY_EDITOR
                if (breakPoint)
                    EditorApplication.isPaused = true;
#endif
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Message", ExecutionType.Message},
            {"Variable", ExecutionType.Variable},
        };
        
        public static string displayName = "Debug Behaviour Node";
        public static string nodeName = "Debug";

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
            return "Log" + executionType;
        }
        
        public override string GetNodeMenuDisplayName ()
        {
            return $"Logic/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType == ExecutionType.Message)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    string header = "[Log] ";
                    if (breakPoint)
                        header = "[Log+Break] ";
                    return header + message;
                }
                else if (breakPoint)
                {
                    return "[Break]";
                }
            }
            else if (executionType == ExecutionType.Variable)
            {
                if (variable != null)
                {
                    string header = "[Log] ";
                    if (breakPoint)
                        header = "[Log+Break] ";
                    return header + variable.name;
                }
                else if (breakPoint)
                {
                    return "[Break]";
                }
            }

            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will provide several debug control for troubleshooting.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}