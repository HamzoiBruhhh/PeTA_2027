using System;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections;
using Reshape.ReFramework;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class InputBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            MouseRotationEnable = 10,
            MouseRotationDisable = 11,
            InputEnable = 100,
            InputDisable = 101,
            KeyEnable = 200,
            KeyDisable = 201,
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [HideIf("HideParamGo")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(gameObject)")]
        [InlineButton("@gameObject.SetObjectValue(AssignGameObject())", "♺", ShowIf = "@gameObject.IsObjectValueType()")]
        [InfoBox("@gameObject.GetMismatchWarningMessage()", InfoMessageType.Error, "@gameObject.IsShowMismatchWarning()")]
        private SceneObjectProperty gameObject = new SceneObjectProperty(SceneObject.ObjectType.GameObject);

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("@ParamVectorTwo1Name()")]
        [HideIf("HideParamVectorTwo1")]
        private Vector2 paramVectorTwo1;

#if ENABLE_INPUT_SYSTEM
        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowParamInputAction")]
        private InputActionAsset inputAction;
#endif

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("@ParamString1Name()")]
        [ShowIf("ShowParamString1")]
        [ValueDropdown("ParamString1Choice", ExpandAllMenuItems = false, AppendNextDrawer = true)]
        private string paramString1;

        [SerializeField]
        [HideIf("HideParamCameraView")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(cameraView)")]
        [InlineButton("@cameraView.SetObjectValue(AssignCamera())", "♺", ShowIf = "@cameraView.IsObjectValueType()")]
        [InfoBox("Allow object rotation base on the camera view, using world rotation if camera have not assigned")]
        [InfoBox("@cameraView.GetMismatchWarningMessage()", InfoMessageType.Error, "@cameraView.IsShowMismatchWarning()")]
        private SceneObjectProperty cameraView = new SceneObjectProperty(SceneObject.ObjectType.Camera);

        protected override void OnStart (GraphExecution execution, int updateId)
        {
#if ENABLE_INPUT_SYSTEM
            if (executionType is ExecutionType.None)
            {
                LogWarning("Found an empty Input Behaviour node in " + context.objectName);
            }
            else if (executionType is ExecutionType.MouseRotationEnable)
            {

                if (gameObject.IsEmpty || !gameObject.IsMatchType() || inputAction == null || string.IsNullOrEmpty(paramString1))
                {
                    LogWarning("Found an empty Input Behaviour node in " + context.objectName);
                }
                else
                {
                    var go = (GameObject) gameObject;
                    if (!go.TryGetComponent(out MouseRotationController controller))
                        controller = go.AddComponent<MouseRotationController>();
                    controller.Initial(paramVectorTwo1, inputAction, paramString1, (Camera)cameraView);
                }
            }
            else if (executionType is ExecutionType.MouseRotationDisable)
            {
                if (gameObject.IsEmpty || !gameObject.IsMatchType())
                {
                    LogWarning("Found an empty Input Behaviour node in " + context.objectName);
                }
                else
                {
                    var go = (GameObject) gameObject;
                    if (go.TryGetComponent(out MouseRotationController controller))
                        controller.Terminate();
                }
            }
            else if (executionType is ExecutionType.InputEnable)
            {
                if (inputAction == null)
                {
                    LogWarning("Found an empty Input Behaviour node in " + context.objectName);
                }
                else
                {
                    inputAction.Enable();
                }
            }
            else if (executionType is ExecutionType.InputDisable)
            {
                if (inputAction == null)
                {
                    LogWarning("Found an empty Input Behaviour node in " + context.objectName);
                }
                else
                {
                    inputAction.Disable();
                }
            }
            else if (executionType is ExecutionType.KeyEnable)
            {
                if (inputAction == null || string.IsNullOrEmpty(paramString1))
                {
                    LogWarning("Found an empty Input Behaviour node in " + context.objectName);
                }
                else
                {
                    var action = inputAction.FindAction(paramString1);
                    action?.Enable();
                }
            }
            else if (executionType is ExecutionType.KeyDisable)
            {
                if (inputAction == null || string.IsNullOrEmpty(paramString1))
                {
                    LogWarning("Found an empty Input Behaviour node in " + context.objectName);
                }
                else
                {
                    var action = inputAction.FindAction(paramString1);
                    action?.Disable();
                }
            }

#endif
            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private IEnumerable ParamString1Choice ()
        {
            ValueDropdownList<string> menu = new ValueDropdownList<string>();
            if (executionType is ExecutionType.MouseRotationEnable or ExecutionType.MouseRotationDisable or ExecutionType.KeyEnable or ExecutionType.KeyDisable)
            {
#if ENABLE_INPUT_SYSTEM
                if (inputAction != null)
                {
                    for (int i = 0; i < inputAction.actionMaps.Count; i++)
                    {
                        string mapName = inputAction.actionMaps[i].name;
                        for (int j = 0; j < inputAction.actionMaps[i].actions.Count; j++)
                        {
                            menu.Add(mapName + "//" + inputAction.actionMaps[i].actions[j].name, inputAction.actionMaps[i].actions[j].name);
                        }
                    }
                }
#endif
            }

            return menu;
        }

        private string ParamVectorTwo1Name ()
        {
            if (executionType is ExecutionType.MouseRotationEnable)
                return "Rotate Speed";
            return string.Empty;
        }

        private string ParamString1Name ()
        {
            if (executionType is ExecutionType.MouseRotationEnable or ExecutionType.KeyEnable or ExecutionType.KeyDisable)
                return "Input Name";
            return string.Empty;
        }

        private bool HideParamGo ()
        {
            if (executionType is ExecutionType.InputEnable or ExecutionType.KeyEnable)
                return true;
            if (executionType is ExecutionType.InputDisable or ExecutionType.KeyDisable)
                return true;
            return false;
        }

        private bool ShowParamInputAction ()
        {
            if (executionType is ExecutionType.MouseRotationEnable)
                return true;
            if (executionType is ExecutionType.InputEnable or ExecutionType.KeyEnable)
                return true;
            if (executionType is ExecutionType.InputDisable or ExecutionType.KeyDisable)
                return true;
            return true;
        }

        private bool HideParamCameraView ()
        {
#if ENABLE_INPUT_SYSTEM
            if (executionType is ExecutionType.MouseRotationEnable)
                return false;
#endif
            return true;
        }

        private bool HideParamVectorTwo1 ()
        {
#if ENABLE_INPUT_SYSTEM
            if (executionType is ExecutionType.MouseRotationEnable)
                return false;
#endif
            return true;
        }

        private bool ShowParamString1 ()
        {
#if ENABLE_INPUT_SYSTEM
            if (executionType is ExecutionType.MouseRotationEnable or ExecutionType.KeyEnable or ExecutionType.KeyDisable)
                return true;
#endif
            return true;
        }

        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Enable Input", ExecutionType.InputEnable},
            {"Disable Input", ExecutionType.InputDisable},
            {"Enable Key", ExecutionType.KeyEnable},
            {"Disable Key", ExecutionType.KeyDisable},
            {"Enable Mouse To Rotation", ExecutionType.MouseRotationEnable},
            {"Disable Mouse To Rotation", ExecutionType.MouseRotationDisable},
        };

        public static string displayName = "Input Behaviour Node";
        public static string nodeName = "Input";

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
#if ENABLE_INPUT_SYSTEM
            if (executionType is ExecutionType.MouseRotationEnable)
            {

                if (inputAction != null && !string.IsNullOrEmpty(paramString1) && !gameObject.IsNull && gameObject.IsMatchType())
                    return "Enable Mouse Control Rotation on " + gameObject.name;
            }
            else if (executionType is ExecutionType.MouseRotationDisable && !gameObject.IsNull && gameObject.IsMatchType())
            {
                return "Disable Mouse Control Rotation on " + gameObject.name;
            }
            else if (executionType is ExecutionType.InputEnable && inputAction != null)
            {
                return "Enable " + inputAction.name;
            }
            else if (executionType is ExecutionType.InputDisable && inputAction != null)
            {
                return "Disable " + inputAction.name;
            }
            else if (executionType is ExecutionType.KeyEnable && inputAction != null && !string.IsNullOrEmpty(paramString1))
            {
                return "Enable " + paramString1 + " at " + inputAction.name;
            }
            else if (executionType is ExecutionType.KeyDisable && inputAction != null && !string.IsNullOrEmpty(paramString1))
            {
                return "Disable " + paramString1 + " at " + inputAction.name;
            }
#endif
            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (executionType == ExecutionType.MouseRotationEnable)
                tip += "This will disable mouse movement to control a gameObject rotation.\n\n";
            else if (executionType == ExecutionType.MouseRotationDisable)
                tip += "This will enable mouse movement to control a gameObject rotation.\n\n";
            else if (executionType == ExecutionType.InputEnable)
                tip += "This will enable a Input Action.\n\n";
            else if (executionType == ExecutionType.InputDisable)
                tip += "This will disable a Input Action.\n\n";
            else if (executionType == ExecutionType.KeyEnable)
                tip += "This will enable a Input Key.\n\n";
            else if (executionType == ExecutionType.KeyDisable)
                tip += "This will disable a Input Key.\n\n";
            else
                tip += "This will execute all Input related behaviour.\n\n";
            return tip + "We are using Input System from Unity.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}