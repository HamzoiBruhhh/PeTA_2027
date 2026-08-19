using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class ViewportBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            CursorLocked = 10,
            CursorConfined = 11,
            CursorNormal = 12,
            ChangeCursor = 30
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Image")]
        [ShowIf("@executionType == ExecutionType.ChangeCursor")]
        private Texture2D cursorTexture;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType == ExecutionType.CursorLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else if (executionType == ExecutionType.CursorConfined)
            {
                Cursor.lockState = CursorLockMode.Confined;
            }
            else if (executionType == ExecutionType.CursorNormal)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (executionType == ExecutionType.ChangeCursor && cursorTexture != null)
            {
                Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Normal Cursor State", ExecutionType.CursorNormal},
            {"Confined Cursor State", ExecutionType.CursorConfined},
            {"Lock and Hide Cursor", ExecutionType.CursorLocked},
            {"Change Cursor Display", ExecutionType.ChangeCursor},
        };

        public static string displayName = "Viewport Behaviour Node";
        public static string nodeName = "Viewport";

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
            if (executionType == ExecutionType.CursorNormal)
                return "Set cursor to normal state";
            if (executionType == ExecutionType.CursorConfined)
                return "Set cursor to confined state";
            if (executionType == ExecutionType.CursorLocked)
                return "Hide and lock cursor to center";
            if (executionType == ExecutionType.ChangeCursor)
                if (cursorTexture != null)
                    return "Change cursor display to " + cursorTexture.name;
            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (executionType == ExecutionType.CursorNormal)
                tip += "This will set the mouse cursor to default state which work act a normal mouse cursor.\n\n";
            else if (executionType == ExecutionType.CursorConfined)
                tip += "This will set the mouse cursor not able to move outside of the game windows, not support in MacOS.\n\n";
            else if (executionType == ExecutionType.CursorLocked)
                tip += "This will set the mouse cursor to be hidden and always lock at center of the game.\n\n";
            else if (executionType == ExecutionType.ChangeCursor)
                tip += "This will change the mouse cursor icon.\n\n";
            else
                tip += "This will execute all Viewport related behaviour.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}