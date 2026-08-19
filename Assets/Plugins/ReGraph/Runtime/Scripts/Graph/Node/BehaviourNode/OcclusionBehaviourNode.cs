using System.Collections;
using Reshape.ReFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class OcclusionBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            AddReceiver = 11,
            RemoveReceiver = 12,
            AddEmitter = 101,
            RemoveEmitter = 102,
            AddBlocker = 201,
            RemoveBlocker = 202,
        }

        [SerializeField]
        [LabelText("Execution")]
        [OnValueChanged("MarkDirty")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [HideIf("HideGameObject")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(gameObject)")]
        [InlineButton("@gameObject.SetObjectValue(AssignGameObject())", "♺", ShowIf = "@gameObject.IsObjectValueType()")]
        [InfoBox("@gameObject.GetMismatchWarningMessage()", InfoMessageType.Error, "@gameObject.IsShowMismatchWarning()")]
        private SceneObjectProperty gameObject = new SceneObjectProperty(SceneObject.ObjectType.GameObject);

        [SerializeField]
        [ValueDropdown("DrawActionNameListDropdown", ExpandAllMenuItems = true)]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowActionName")]
        private ActionNameChoice actionName;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [HideIf("HideLayer")]
        private LayerMask emitLayer;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType == ExecutionType.None || gameObject.IsEmpty || !gameObject.IsMatchType())
            {
                LogWarning("Found an empty Occlusion Behaviour node in " + context.objectName);
            }
            else
            {
                GameObject go = gameObject;
                if (executionType is ExecutionType.AddBlocker)
                {
                    if (!actionName)
                    {
                        LogWarning("Found an empty Occlusion Add Blocker Behaviour node in " + context.objectName);
                    }
                    else if (go.TryGetComponent(out EnvOcclusionController controller))
                    {
                        LogWarning("Found existing Occlusion Add Blocker Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        controller = go.AddComponent<EnvOcclusionController>();
                        controller.entity = EnvOcclusionController.OcclusionEntity.Blocker;
                        controller.actionName = actionName;
                    }
                }
                else if (executionType == ExecutionType.RemoveBlocker)
                {
                    if (go.TryGetComponent(out EnvOcclusionController controller))
                        if (controller.entity == EnvOcclusionController.OcclusionEntity.Blocker)
                            controller.Terminate();
                }
                else if (executionType == ExecutionType.AddEmitter)
                {
                    if (!actionName)
                    {
                        LogWarning("Found an empty Occlusion Add Emitter Behaviour node in " + context.objectName);
                    }
                    else if (go.TryGetComponent(out EnvOcclusionController controller))
                    {
                        LogWarning("Found existing Occlusion Add Emitter Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        controller = go.AddComponent<EnvOcclusionController>();
                        controller.entity = EnvOcclusionController.OcclusionEntity.Emitter;
                        controller.actionName = actionName;
                        controller.emitLayer = emitLayer;
                    }
                }
                else if (executionType == ExecutionType.RemoveEmitter)
                {
                    if (go.TryGetComponent(out EnvOcclusionController controller))
                        if (controller.entity == EnvOcclusionController.OcclusionEntity.Emitter)
                            controller.Terminate();
                }
                else if (executionType == ExecutionType.AddReceiver)
                {
                    if (!actionName)
                    {
                        LogWarning("Found an empty Occlusion Add Receiver Behaviour node in " + context.objectName);
                    }
                    else if (go.TryGetComponent(out EnvOcclusionController controller))
                    {
                        LogWarning("Found existing Occlusion Add Receiver Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        if (go.TryGetComponent(out Collider collision))
                        {
                            controller = go.AddComponent<EnvOcclusionController>();
                            controller.entity = EnvOcclusionController.OcclusionEntity.Receiver;
                            controller.actionName = actionName;
                        }
                        else
                        {
                            LogWarning("Occlusion Add Receiver Behaviour node not find collider in " + context.objectName);
                        }
                    }
                }
                else if (executionType == ExecutionType.RemoveReceiver)
                {
                    if (go.TryGetComponent(out EnvOcclusionController controller))
                        if (controller.entity == EnvOcclusionController.OcclusionEntity.Receiver)
                            controller.Terminate();
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private bool HideGameObject ()
        {
            if (executionType != ExecutionType.None)
                return false;
            return true;
        }

        private bool ShowActionName ()
        {
            if (executionType is ExecutionType.AddEmitter or ExecutionType.AddBlocker or ExecutionType.AddReceiver)
                return true;
            return false;
        }
        
        private bool HideLayer ()
        {
            if (executionType != ExecutionType.AddEmitter)
                return true;
            return false;
        }

        private static IEnumerable DrawActionNameListDropdown ()
        {
            return ActionNameChoice.GetActionNameListDropdown();
        }

        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Add Blocker", ExecutionType.AddBlocker},
            {"Remove Blocker", ExecutionType.RemoveBlocker},
            {"Add Receiver", ExecutionType.AddReceiver},
            {"Remove Receiver", ExecutionType.RemoveReceiver},
            {"Add Emitter", ExecutionType.AddEmitter},
            {"Remove Emitter", ExecutionType.RemoveEmitter},
        };

        public static string displayName = "Occlusion Behaviour Node";
        public static string nodeName = "Occlusion";

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
            if (!gameObject.IsEmpty && gameObject.IsMatchType())
            {
                if (executionType == ExecutionType.AddBlocker && actionName != null)
                    return "Add " + actionName + " action blocker at " + gameObject.name;
                if (executionType == ExecutionType.RemoveBlocker)
                    return "Remove blocker at " + gameObject.name;
                if (executionType == ExecutionType.AddEmitter && actionName != null)
                    return "Add " + actionName + " action emitter at " + gameObject.name;
                if (executionType == ExecutionType.RemoveEmitter)
                    return "Remove emitter at " + gameObject.name;
                if (executionType == ExecutionType.AddReceiver && actionName != null)
                    return "Add " + actionName + " action receiver at " + gameObject.name;
                if (executionType == ExecutionType.RemoveReceiver)
                    return "Remove receiver at " + gameObject.name;
            }

            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (executionType == ExecutionType.AddBlocker)
                tip += "This will set the gameObject as blocker. Occlusion event happen when blocker appear in between emitter and receiver.\n\n";
            else if (executionType == ExecutionType.RemoveBlocker)
                tip += "This will remove the gameObject as blocker.\n\n";
            else if (executionType == ExecutionType.AddReceiver)
                tip += "This will set the gameObject as receiver. General usage is a scene only have one receiver at camera position.\n\n";
            else if (executionType == ExecutionType.RemoveReceiver)
                tip += "This will remove the gameObject as receiver.\n\n";
            else if (executionType == ExecutionType.AddEmitter)
                tip += "This will set the gameObject as emitter. Emitter gameObject will cast a ray to receiver position.\n\n";
            else if (executionType == ExecutionType.RemoveEmitter)
                tip += "This will remove the gameObject as emitter.\n\n";
            else
                tip += "This will execute all Raycast related behaviour.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}