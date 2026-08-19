using System.Collections;
using System.Collections.Generic;
using Reshape.ReFramework;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class CollisionTriggerNode : TriggerNode
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("MarkDirty")]
        [PropertyOrder(1)]
        public Type triggerType;

        [FoldoutGroup("Settings")]
        [OnValueChanged("OnChangeCollision")]
        [PropertyOrder(3)]
        [HandleBeforeAfter("CollisionTriggerNodeCollisionChanged", "graphSelectionObject", "TriggerId")]
        public List<Collider> specificCollision;

        [HideLabel]
        [FoldoutGroup("Settings")]
        [OnInspectorGUI("MarkDirtyCollisionMatchInfo")]
        [PropertyOrder(4)]
        public CollisionMatchInfo matchInfo;

        [LabelText("Interactor")]
        [OnValueChanged("MarkDirty")]
        [PropertyOrder(2)]
        public SceneObjectVariable interactorVariable;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            var state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                if (execution.type is Type.CollisionEnter or Type.CollisionExit or Type.CollisionStepIn or Type.CollisionStepOut)
                {
                    if (execution.type == triggerType)
                    {
                        var inGo = execution.parameters.interactedGo;
                        if (inGo != null)
                        {
                            var execute = false;
                            if (!string.IsNullOrEmpty(execution.parameters.actionName))
                            {
                                if (execution.parameters.actionName.Equals(TriggerId))
                                    execute = true;
                            }
                            else
                            {
                                execute = true;
                            }

                            if (execute)
                            {
                                if (context.runner.IsGameObjectMatch(inGo, matchInfo.excludeTags, matchInfo.excludeLayers, matchInfo.onlyTags, matchInfo.onlyLayers, matchInfo.specificNames))
                                {
                                    if (!matchInfo.inOutDetection)
                                    {
                                        context.runner.TriggerCollisionStep(execution.type, inGo, true, !string.IsNullOrEmpty(execution.parameters.actionName));

                                        if (interactorVariable != null)
                                        {
                                            if (interactorVariable.sceneObject.IsComponent())
                                            {
                                                interactorVariable.Rebase();
                                                var t = interactorVariable.sceneObject.ComponentType();
                                                var comp = inGo.GetComponentInChildren(t);
                                                if (comp == null)
                                                    comp = inGo.GetComponentInParent(t);
                                                if (comp != null)
                                                    interactorVariable.SetValue(comp);
                                            }
                                        }

                                        execution.variables.SetState(guid, State.Success);
                                        state = State.Success;
                                    }
                                }
                                else if (GraphManager.instance.runtimeSettings.deepLogging)
                                {
                                    LogWarning("Collision trigger not match collision" + inGo.name);
                                }
                            }
                        }
                    }
                }
                else if (execution.type == Type.All)
                {
                    if (execution.parameters.actionName != null && execution.parameters.actionName.Equals(TriggerId))
                    {
                        execution.variables.SetState(guid, State.Success);
                        state = State.Success;
                    }
                }

                if (state != State.Success)
                {
                    execution.variables.SetState(guid, State.Failure);
                    state = State.Failure;
                }
                else
                    OnSuccess();
            }

            if (state == State.Success)
                return base.OnUpdate(execution, updateId);
            return State.Failure;
        }
        
        public override bool IsTrigger (TriggerNode.Type type, int paramInt = 0)
        {
            return type == triggerType;
        }

        /* [PropertyOrder(2)]
        [OnValueChanged("MarkDirty")]
        [Tooltip("Only use in Graph Editor")]*/
        //-- NOTE public string devNote;

#if UNITY_EDITOR
        private void OnChangeCollision ()
        {
            MarkDirty();
            for (var i = 0; i < specificCollision.Count; i++)
            {
                var collider = specificCollision[i];
                if (collider != null)
                {
                    if (collider.TryGetComponent(out GraphRunner runner))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            EditorUtility.DisplayDialog("Collision Trigger Node",
                                $"There is Graph Runner in the {collider.gameObject.name} gameobject, advice not to use drag its collider here to avoid redundant collision detection.", "OK");
                        };
                    }

                    CollisionController.AddBond(collider, GetRunner(), TriggerId);
                }
            }
        }

        public override void OnDelete ()
        {
            for (var i = 0; i < specificCollision.Count; i++)
            {
                SetGraphEditorContext(Selection.activeGameObject);
                CollisionController.RemoveBond(specificCollision[i], GetRunner(), TriggerId);
            }
        }

        private void MarkDirtyCollisionMatchInfo ()
        {
            if (matchInfo.dirty)
            {
                matchInfo.dirty = false;
                MarkDirty();
            }
        }

        private IEnumerable TriggerTypeChoice ()
        {
            var menu = new ValueDropdownList<Type>
            {
                {"Being Enter", Type.CollisionEnter},
                {"Being Exit", Type.CollisionExit},
                {"Step In", Type.CollisionStepIn},
                {"Step Out", Type.CollisionStepOut}
            };
            return menu;
        }

        public static string displayName = "Collision Trigger Node";
        public static string nodeName = "Collision";
        
        public override Type GetNodeType ()
        {
            return triggerType;
        }

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
            return triggerType.ToString();
        }

        public override string GetNodeViewDescription ()
        {
            var desc = string.Empty;
            if (triggerType == Type.CollisionEnter)
                desc += "Being Enter";
            else if (triggerType == Type.CollisionExit)
                desc += "Being Exit";
            else if (triggerType == Type.CollisionStepIn)
                desc += "Step In";
            else if (triggerType == Type.CollisionStepOut)
                desc += "Step Out";
            /* if (!string.IsNullOrEmpty(devNote))
                desc += "\\n" + devNote;*/
            return desc;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.CollisionEnter)
                tip += "This will get trigger when a collider enter into the collision in this gameObject.\n\n";
            else if (triggerType == Type.CollisionExit)
                tip += "This will get trigger when a collider exit from the collision in this gameObject.\n\n";
            else if (triggerType == Type.CollisionStepIn)
                tip += "This will get trigger when the collider in this gameObject enter into a collision.\n\n";
            else if (triggerType == Type.CollisionStepOut)
                tip += "This will get trigger when the collider in this gameObject exit from a collision.\n\n";
            else
                tip += "This will get trigger all Collision related events.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}