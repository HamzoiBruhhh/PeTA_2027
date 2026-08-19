using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class ImageBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            FillAmount = 10,
            SetMaterial = 20,
            SetSprite = 30,
        }

        [SerializeField]
        [OnValueChanged("OnChangeType")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(image)")]
        [InlineButton("@image.SetObjectValue(AssignComponent<UnityEngine.UI.Image>())", "♺", ShowIf = "@image.IsObjectValueType()")]
        [InfoBox("@image.GetMismatchWarningMessage()", InfoMessageType.Error, "@image.IsShowMismatchWarning()")]
        private SceneObjectProperty image = new SceneObjectProperty(SceneObject.ObjectType.Image);

        [LabelText("Value")]
        [ShowIf("@executionType == ExecutionType.FillAmount")]
        [OnInspectorGUI("@MarkPropertyDirty(number)")]
        [InlineProperty]
        public FloatProperty number;
        
        [SerializeField]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(material)")]
        [InfoBox("@material.GetMismatchWarningMessage()", InfoMessageType.Error, "@material.IsShowMismatchWarning()")]
        [ShowIf("@executionType == ExecutionType.SetMaterial || executionType == ExecutionType.SetSprite")]
        private SceneObjectProperty material = new SceneObjectProperty(SceneObject.ObjectType.Material);

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (image.IsEmpty || !image.IsMatchType() || executionType is ExecutionType.None)
                LogWarning("Found an empty Image Behaviour node in " + context.objectName);
            else
            {
                if (executionType is ExecutionType.FillAmount)
                {
                    ((Image) image).fillAmount = number;
                }
                else if (executionType is ExecutionType.SetMaterial)
                {
                    if (material.IsEmpty || !material.IsMatchType())
                        LogWarning("Found an empty Image Behaviour node in " + context.objectName);
                    else
                    {
                        var mat = (Material) material;
                        var img = (Image) image;
                        if (img.material != mat)
                            img.material = mat;
                    }
                }
                else if (executionType is ExecutionType.SetSprite)
                {
                    if (material.IsEmpty || !material.IsMatchType())
                        LogWarning("Found an empty Image Behaviour node in " + context.objectName);
                    else
                    {
                        var sp = (Sprite) material;
                        var img = (Image) image;
                        if (img.sprite != sp)
                            img.sprite = sp;
                    }
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private void OnChangeType ()
        {
            if (executionType is ExecutionType.SetMaterial)
                material = new SceneObjectProperty(SceneObject.ObjectType.Material);
            else if (executionType is ExecutionType.SetSprite)
                material = new SceneObjectProperty(SceneObject.ObjectType.Sprite);
            MarkRepaint();
            MarkDirty();
        }
        
        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Set Sprite", ExecutionType.SetSprite},
            {"Fill Amount", ExecutionType.FillAmount},
            {"Set Material", ExecutionType.SetMaterial},
        };

        public static string displayName = "Image Behaviour Node";
        public static string nodeName = "Image";

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
            if (image.IsEmpty || !image.IsMatchType() || executionType is ExecutionType.None)
                return string.Empty;
            var message = "";
            if (executionType is ExecutionType.FillAmount)
                message = "Set " + number + " Fill Amount to " + image.name;
            else if (executionType is ExecutionType.SetMaterial && !material.IsNull && material.IsMatchType())
                message = "Set material " + material.objectName + " to " + image.name;
            else if (executionType is ExecutionType.SetSprite && !material.IsNull && material.IsMatchType())
                message = "Set sprite " + material.objectName + " to " + image.name;
            return message;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will provide several controls to a specific Image.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}