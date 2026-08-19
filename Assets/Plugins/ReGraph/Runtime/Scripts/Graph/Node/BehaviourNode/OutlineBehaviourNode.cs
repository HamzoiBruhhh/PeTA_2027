using System;
using System.Collections;
using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class OutlineBehaviourNode : BehaviourNode
    {
        public const string VAR_OUTLINEEFFECT_COMP = "_outlineeffect_component";
        public const string VAR_OUTLINE_COMP = "_outline_component";

        public enum ExecutionType
        {
            None,
            Highlight = 10,
            Unhighlight = 11,
        }

        [SerializeField]
        [LabelText("Execution")]
        [OnValueChanged("MarkDirty")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(gameObject)")]
        [InlineButton("@gameObject.SetObjectValue(AssignGameObject())", "♺", ShowIf = "@gameObject.IsObjectValueType()")]
        [InfoBox("@gameObject.GetMismatchWarningMessage()", InfoMessageType.Error, "@gameObject.IsShowMismatchWarning()")]
        private SceneObjectProperty gameObject = new SceneObjectProperty(SceneObject.ObjectType.GameObject);

        [SerializeField]
        [HideIf("@!IsHighlightExecution()")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(camera)")]
        [InlineButton("@camera.SetObjectValue(AssignCamera())", "♺", ShowIf = "@camera.IsObjectValueType()")]
        [InfoBox("@camera.GetMismatchWarningMessage()", InfoMessageType.Error, "@camera.IsShowMismatchWarning()")]
        private SceneObjectProperty camera = new SceneObjectProperty(SceneObject.ObjectType.Camera);

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [HideIf("@!IsHighlightExecution()")]
        private Color color = Color.black;

        private string outlineEffectCompKey;
        private string outlineCompKey;

        private void InitVariables ()
        {
            if (string.IsNullOrEmpty(outlineEffectCompKey))
                outlineEffectCompKey = guid + VAR_OUTLINEEFFECT_COMP;
            if (string.IsNullOrEmpty(outlineCompKey))
                outlineCompKey = guid + VAR_OUTLINE_COMP;
        }

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType == ExecutionType.None || gameObject.IsEmpty || !gameObject.IsMatchType())
            {
                LogWarning("Found an empty Outline Behaviour node in " + context.objectName);
            }
            else
            {
                if (executionType == ExecutionType.Highlight)
                {
                    if (camera.IsEmpty || !camera.IsMatchType())
                    {
                        LogWarning("Found an empty Outline Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        InitVariables();
                        
                        Component comp = context.GetComp(outlineEffectCompKey);
                        OutlineEffect effect;
                        if (comp == null)
                        {
                            var cam = (Camera) camera;
                            if (!cam.gameObject.TryGetComponent(out effect))
                            {
                                effect = cam.gameObject.AddComponent<OutlineEffect>();
                                effect.sourceCamera = cam;
                                effect.shader = GraphManager.instance.runtimeSettings.outlineShader;
                                effect.bufferShader = GraphManager.instance.runtimeSettings.outlineBufferShader;
                            }
                            context.SetComp(outlineEffectCompKey, effect);
                        }
                        else
                        {
                            effect = (OutlineEffect) comp;
                        }

                        int colorIndex = effect.AddColor(color);
                        if (effect.IsValidColorIndex(colorIndex))
                        {
                            GameObject go = gameObject;
                            comp = context.GetComp(outlineCompKey);
                            Outline outline;
                            if (comp == null)
                            {
                                if (!go.TryGetComponent(out outline))
                                    outline = go.AddComponent<Outline>();
                                context.SetComp(outlineCompKey, outline);
                            }
                            else
                            {
                                outline = (Outline) comp;
                            }

                            outline.SetColor(color, colorIndex);
                            outline.Enable(true);
                        }
                        else
                        {
                            LogWarning("Found Outline Behaviour node overload colors in " + context.objectName);
                        }
                    }
                }
                else if (executionType == ExecutionType.Unhighlight)
                {
                    GameObject go = gameObject;
                    if (go.TryGetComponent(out Outline outline))
                        outline.Enable(false);
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private bool IsHighlightExecution ()
        {
            return executionType == ExecutionType.Highlight;
        }

        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Highlight", ExecutionType.Highlight},
            {"Unhighlight", ExecutionType.Unhighlight},
        };

        public static string displayName = "Outline Trigger Node";
        public static string nodeName = "Outline";

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
            string desc = String.Empty;
            if (!gameObject.IsNull && gameObject.IsMatchType())
            {
                if (executionType == ExecutionType.Highlight)
                    desc = "Highlight " + gameObject.name;
                else if (executionType == ExecutionType.Unhighlight)
                    desc = "Unhighlight " + gameObject.name;
            }

            return desc;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will add outline to a 3D object base on configuration.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}