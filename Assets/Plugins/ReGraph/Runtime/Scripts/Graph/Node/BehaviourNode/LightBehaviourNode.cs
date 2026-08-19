using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Reshape.ReFramework;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class LightBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            SetEnvIntensity = 10,
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;
        
        [SerializeField]
        [InlineProperty]
        [LabelText("Value")]
        private FloatProperty paramNumber1 = new FloatProperty(0);

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType is ExecutionType.None)
            {
                LogWarning("Found an empty Light Behaviour node in " + context.objectName);
            }
            else if (executionType is ExecutionType.SetEnvIntensity)
            {
                if (paramNumber1.IsVariable() && paramNumber1.IsNull() )
                {
                    LogWarning("Found an empty Light Behaviour node in " + context.objectName);
                }
                else
                {
                    RenderSettings.ambientIntensity = paramNumber1;
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Set Env Intensity", ExecutionType.SetEnvIntensity},
        };

        public static string displayName = "Light Behaviour Node";
        public static string nodeName = "Light";

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
            return $"Audio & Visual/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType is ExecutionType.None)
                return string.Empty;
            var message = "";
            if (executionType is ExecutionType.SetEnvIntensity)
            {
                if (!paramNumber1.IsVariable() || !paramNumber1.IsNull() )
                    message = "Set Env Intensity to " + paramNumber1;
            }

            return message;
        }

        public override string GetNodeViewTooltip ()
        {
            return "This will provide several controls to a specific Lighting.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}