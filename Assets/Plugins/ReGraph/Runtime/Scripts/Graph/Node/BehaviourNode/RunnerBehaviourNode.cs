using System;
using System.Collections;
using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class RunnerBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            GetInstanceId = 11,
        }

        [SerializeField]
        [OnValueChanged("OnChangeType")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [LabelText("Number")]
        [ShowIf("@executionType == ExecutionType.GetInstanceId")]
        [OnInspectorGUI("@MarkPropertyDirty(number1)")]
        [InlineProperty]
        public FloatProperty number1;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType == ExecutionType.GetInstanceId)
            {
                if (!number1.IsVariable())
                {
                    LogWarning("Found an empty Runner Behaviour node in " + context.objectName);
                }
                else
                {
                    number1.SetVariableValue(context.runner.GetInstanceID());
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        public void OnChangeType ()
        {
            if (executionType == ExecutionType.GetInstanceId)
                number1.AllowVariableOnly();
            else
                number1.AllowAll();
            MarkDirty();
        }

        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Set Instance Id", ExecutionType.GetInstanceId}
        };

        public static string displayName = "Runner Behaviour Node";
        public static string nodeName = "Runner";

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
        
        public override string GetNodeIdentityName ()
        {
            return executionType.ToString();
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType == ExecutionType.GetInstanceId)
            {
                if (!number1.IsNull())
                {
                    if (number1.IsVariable())
                        return $"Get instance id into {number1.GetVariableName()}";
                }
            }

            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will execute all Graph Runner related behaviour.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}