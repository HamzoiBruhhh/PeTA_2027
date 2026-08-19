using System.Collections;
using Reshape.ReFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class LabelBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            StringToLabel = 10,
            VariableToLabel = 11,
            StringToTextMesh = 100,
            VariableToTextMesh = 101,
            StringToTextMeshPro = 200,
            VariableToTextMeshPro = 201
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [HideIf("@executionType != ExecutionType.StringToLabel && executionType != ExecutionType.VariableToLabel")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(textLabel)")]
        [InlineButton("@textLabel.SetObjectValue(AssignComponent<Text>())", "♺", ShowIf = "@textLabel.IsObjectValueType()")]
        [InfoBox("@textLabel.GetMismatchWarningMessage()", InfoMessageType.Error, "@textLabel.IsShowMismatchWarning()")]
        private SceneObjectProperty textLabel = new SceneObjectProperty(SceneObject.ObjectType.Text);

        [SerializeField]
        [HideIf("@executionType != ExecutionType.StringToTextMesh && executionType != ExecutionType.VariableToTextMesh")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(textMeshLabel)")]
        [InlineButton("@textMeshLabel.SetObjectValue(AssignComponent<TextMesh>())", "♺", ShowIf = "@textMeshLabel.IsObjectValueType()")]
        [InfoBox("@textMeshLabel.GetMismatchWarningMessage()", InfoMessageType.Error, "@textMeshLabel.IsShowMismatchWarning()")]
        private SceneObjectProperty textMeshLabel = new SceneObjectProperty(SceneObject.ObjectType.TextMesh);

        [SerializeField]
        [HideIf("@executionType != ExecutionType.StringToTextMeshPro && executionType != ExecutionType.VariableToTextMeshPro")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(textMeshProLabel)")]
        [InlineButton("@textMeshProLabel.SetObjectValue(AssignComponent<TMP_Text>())", "♺", ShowIf = "@textMeshProLabel.IsObjectValueType()")]
        [InfoBox("@textMeshProLabel.GetMismatchWarningMessage()", InfoMessageType.Error, "@textMeshProLabel.IsShowMismatchWarning()")]
        private SceneObjectProperty textMeshProLabel = new SceneObjectProperty(SceneObject.ObjectType.TextMeshProText);

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [HideIf("@executionType != ExecutionType.VariableToLabel && executionType != ExecutionType.VariableToTextMesh && executionType != ExecutionType.VariableToTextMeshPro")]
        private VariableScriptableObject variable;

        [SerializeField]
        [MultiLineProperty]
        [OnValueChanged("MarkDirty")]
        [HideIf("@executionType != ExecutionType.StringToLabel && executionType != ExecutionType.StringToTextMesh && executionType != ExecutionType.StringToTextMeshPro")]
        [LabelText("String")]
        private string message;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            bool error = false;
            if (executionType is ExecutionType.StringToLabel or ExecutionType.VariableToLabel)
            {
                if (textLabel.IsEmpty || !textLabel.IsMatchType())
                    error = true;
            }
            else if (executionType is ExecutionType.StringToTextMesh or ExecutionType.VariableToTextMesh)
            {
                if (textMeshLabel.IsEmpty || !textMeshLabel.IsMatchType())
                    error = true;
            }
            else if (executionType is ExecutionType.StringToTextMeshPro or ExecutionType.VariableToTextMeshPro)
            {
                if (textMeshProLabel.IsEmpty || !textMeshProLabel.IsMatchType())
                    error = true;
            }

            if (executionType is ExecutionType.StringToLabel or ExecutionType.StringToTextMesh or ExecutionType.StringToTextMeshPro)
            {
                if (message == null)
                    error = true;
            }
            else if (executionType is ExecutionType.VariableToLabel or ExecutionType.VariableToTextMesh or ExecutionType.VariableToTextMeshPro)
            {
                if (variable == null)
                    error = true;
            }

            if (error)
            {
                LogWarning("Found an empty Label Behaviour node in " + context.objectName);
            }
            else
            {
                string outputString = string.Empty;
                if (executionType is ExecutionType.StringToLabel or ExecutionType.StringToTextMesh or ExecutionType.StringToTextMeshPro)
                    outputString = message;
                else
                {
                    if (variable is WordVariable word)
                        word.RefreshIfNecessary();
                    outputString = variable.ToString();
                }

                if (executionType is ExecutionType.StringToLabel or ExecutionType.VariableToLabel)
                    ((Text) textLabel).text = outputString;
                else if (executionType is ExecutionType.StringToTextMesh or ExecutionType.VariableToTextMesh)
                    ((TextMesh) textMeshLabel).text = outputString;
                else if (executionType is ExecutionType.StringToTextMeshPro or ExecutionType.VariableToTextMeshPro)
                    ((TMP_Text) textMeshProLabel).text = outputString;
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"String To Text", ExecutionType.StringToLabel},
            {"Variable To Text", ExecutionType.VariableToLabel},
            {"String To TextMesh", ExecutionType.StringToTextMesh},
            {"Variable To TextMesh", ExecutionType.VariableToTextMesh},
            {"String To TMPro", ExecutionType.StringToTextMeshPro},
            {"Variable To TMPro", ExecutionType.VariableToTextMeshPro}
        };

        public static string displayName = "Label Behaviour Node";
        public static string nodeName = "Label";

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
            if (executionType is ExecutionType.StringToLabel or ExecutionType.VariableToLabel)
                if (textLabel.IsNull || !textLabel.IsMatchType())
                    return string.Empty;
            if (executionType is ExecutionType.StringToTextMesh or ExecutionType.VariableToTextMesh)
                if (textMeshLabel.IsNull || !textMeshLabel.IsMatchType())
                    return string.Empty;
            if (executionType is ExecutionType.StringToTextMeshPro or ExecutionType.VariableToTextMeshPro)
                if (textMeshProLabel.IsNull || !textMeshProLabel.IsMatchType())
                    return string.Empty;

            string description = "Set variable to ";
            if (variable != null)
                description = "Set " + variable.name + " to ";
            if (executionType is ExecutionType.StringToLabel or ExecutionType.StringToTextMesh or ExecutionType.StringToTextMeshPro)
                description = "Set string to ";
            if (executionType is ExecutionType.StringToLabel or ExecutionType.VariableToLabel)
                description += textLabel.name + " (Text)";
            else if (executionType is ExecutionType.StringToTextMesh or ExecutionType.VariableToTextMesh)
                description += textMeshLabel.name + " (Text Mesh)";
            else if (executionType is ExecutionType.StringToTextMeshPro or ExecutionType.VariableToTextMeshPro)
                description += textMeshProLabel.name + " (TMPro)";

            if (GraphPrefs.showLabelBehaviourText)
            {
                string outputString = string.Empty;
                if (executionType is ExecutionType.StringToLabel or ExecutionType.StringToTextMesh or ExecutionType.StringToTextMeshPro)
                    outputString = message;
                else if (variable)
                    outputString = variable.ToString();
                if (!string.IsNullOrWhiteSpace(outputString))
                    description += "\n\n" + outputString;
            }

            return description;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will provide several controls to Text / Text Mesh / TextMeshProText.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}