using System.Collections;
using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class GameObjectBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            Show = 10,
            Hide = 11,
            Deactivate = 12,
            Activate = 13,
            EnableComponent = 30,
            DisableComponent = 31,
            Spawn = 50,
            Expel = 51,
            SetLayer = 70,
            SetParent = 71
        }

        private enum GoType
        {
            None,
            WithRunner,
            WithoutRunner
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        private ExecutionType executionType;

        [SerializeField]
        [HideLabel, InlineProperty, OnInspectorGUI("@OnChangeGameObject()")]
        [InlineButton("@gameObject.SetObjectValue(AssignGameObject())", "♺", ShowIf = "@gameObject.IsObjectValueType()")]
        [InfoBox("@gameObject.GetMismatchWarningMessage()", InfoMessageType.Error, "@gameObject.IsShowMismatchWarning()")]
        private SceneObjectProperty gameObject = new SceneObjectProperty(SceneObject.ObjectType.GameObject);

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowComponent")]
        [ValueDropdown("DrawComponentListDropdown", ExpandAllMenuItems = true)]
        private Component component;

        [SerializeField]
        [ShowIf("ShowLocation")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(location)")]
        [InlineButton("@location.SetObjectValue(AssignComponent<UnityEngine.Transform>())", "♺", ShowIf = "@location.IsObjectValueType()")]
        [InfoBox("@location.GetMismatchWarningMessage()", InfoMessageType.Error, "@location.IsShowMismatchWarning()")]
        private SceneObjectProperty location = new SceneObjectProperty(SceneObject.ObjectType.Transform, "GameObjectLocation");

        [SerializeField]
        [ShowIf("ShowParentTransform")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(parentTransform)")]
        [InlineButton("@parentTransform.SetObjectValue(AssignComponent<UnityEngine.Transform>())", "♺", ShowIf = "@parentTransform.IsObjectValueType()")]
        [InfoBox("@parentTransform.GetMismatchWarningMessage()", InfoMessageType.Error, "@parentTransform.IsShowMismatchWarning()")]
        private SceneObjectProperty parentTransform = new SceneObjectProperty(SceneObject.ObjectType.Transform, "GameObjectParent");

        [SerializeField]
        [ValueDropdown("DrawActionNameListDropdown", ExpandAllMenuItems = true)]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowLocation")]
        [LabelText("OnSpawn Action")]
        private ActionNameChoice actionName;

        [SerializeField]
        [LabelText("Store To")]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.Spawn")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariableWarning", GUIAlwaysEnabled = true)]
        public SceneObjectVariable objectVariable;

        [ShowIf("@executionType == ExecutionType.SetLayer")]
        [Layer]
        public int uiLayer;
        
        [ShowIf("@executionType == ExecutionType.SetLayer")]
        [LabelText("Recursively")]
        public bool paramBool;

        private GoType spawnType;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (gameObject.IsEmpty || !gameObject.IsMatchType() || executionType == ExecutionType.None)
            {
                LogWarning("Found an empty GameObject Behaviour node in " + context.objectName);
            }
            else if (executionType is ExecutionType.DisableComponent or ExecutionType.EnableComponent)
            {
                if (component == null)
                {
                    LogWarning("Found an empty GameObject Behaviour node in " + context.objectName);
                }
                else
                {
                    bool value = executionType == ExecutionType.EnableComponent;
                    if (component is Renderer)
                    {
                        var comp = (Renderer) component;
                        comp.enabled = value;
                    }
                    else if (component is Collider)
                    {
                        var comp = (Collider) component;
                        comp.enabled = value;
                    }
                    else if (component is Behaviour)
                    {
                        var comp = (Behaviour) component;
                        comp.enabled = value;
                    }
                }
            }
            else if (executionType == ExecutionType.Show)
            {
                ((GameObject) gameObject).SetActiveOpt(true);
            }
            else if (executionType == ExecutionType.Hide)
            {
                ((GameObject) gameObject).SetActiveOpt(false);
            }
            else if (executionType == ExecutionType.Deactivate)
            {
                ((GameObject) gameObject).Deactivate();
            }
            else if (executionType == ExecutionType.Activate)
            {
                ((GameObject) gameObject).Activate();
            }
            else if (executionType == ExecutionType.SetLayer)
            {
                ((GameObject) gameObject).transform.SetLayer(uiLayer, paramBool);
            }
            else if (executionType == ExecutionType.SetParent)
            {
                if (parentTransform.IsEmpty || !parentTransform.IsMatchType())
                {
                    LogWarning("Found an empty GameObject Behaviour node in " + context.objectName);
                }
                else
                {
                    ((GameObject) gameObject).transform.SetParent((Transform)parentTransform);
                }
            }
            else if (executionType == ExecutionType.Spawn)
            {
                GameObject go = null;
                var loc = context.transform;
                Transform par = null;
                if (!location.IsEmpty && location.IsMatchType())
                    loc = (Transform) location;
                if (!parentTransform.IsEmpty)
                    par = (Transform) parentTransform;
                go = context.runner.TakeFromPool(gameObject, loc, false, par);
                if (go != null)
                {
                    GraphRunner gr = null;
                    bool haveGetGraphRunner = false;
                    if (actionName != null)
                    {
                        haveGetGraphRunner = true;
                        gr = HandleGetGraphRunner(go);
                        if (gr != null)
                            gr.TriggerSpawn(actionName);
                    }

                    if (objectVariable != null)
                    {
                        if (objectVariable.sceneObject.type == SceneObject.ObjectType.GraphRunner)
                        {
                            if (!haveGetGraphRunner)
                                gr = HandleGetGraphRunner(go);
                            if (gr != null)
                                objectVariable.SetValue(gr);
                        }
                        else if (objectVariable.sceneObject.type == SceneObject.ObjectType.GameObject)
                        {
                            objectVariable.SetValue(go);
                        }
                    }
                }
            }
            else if (executionType == ExecutionType.Expel)
            {
                context.runner.InsertIntoPool(gameObject, true);
            }

            base.OnStart(execution, updateId);

            GraphRunner HandleGetGraphRunner (GameObject go)
            {
                if (spawnType == GoType.None)
                {
                    if (go.TryGetComponent(out GraphRunner gr))
                    {
                        spawnType = GoType.WithRunner;
                        return gr;
                    }

                    spawnType = GoType.WithoutRunner;
                }
                else if (spawnType == GoType.WithRunner)
                {
                    go.TryGetComponent(out GraphRunner runner);
                    return runner;
                }

                return null;
            }
        }

#if UNITY_EDITOR
        private void OnChangeGameObject ()
        {
            if (gameObject.dirty)
                component = default;
            MarkPropertyDirty(gameObject);
        }

        private bool ShowObjectVariableWarning ()
        {
            if (objectVariable != null)
            {
                if (objectVariable.sceneObject.type != SceneObject.ObjectType.GraphRunner && objectVariable.sceneObject.type != SceneObject.ObjectType.GameObject)
                    return true;
            }

            return false;
        }

        private bool ShowComponent ()
        {
            if (executionType is ExecutionType.DisableComponent or ExecutionType.EnableComponent)
                return true;
            return false;
        }

        private bool ShowLocation ()
        {
            if (executionType is ExecutionType.Spawn)
                return true;
            return false;
        }

        private bool ShowParentTransform ()
        {
            if (executionType is ExecutionType.Spawn or ExecutionType.SetParent)
                return true;
            return false;
        }

        private IEnumerable DrawComponentListDropdown ()
        {
            var actionNameListDropdown = new ValueDropdownList<Component>();
            if (!gameObject.IsEmpty)
            {
                var components = ((GameObject) gameObject).GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp is Collider or Renderer or Behaviour)
                        actionNameListDropdown.Add(comp.GetType().ToString(), comp);
                }
            }

            return actionNameListDropdown;
        }

        private static IEnumerable DrawActionNameListDropdown ()
        {
            return ActionNameChoice.GetActionNameListDropdown();
        }

        public static string displayName = "GameObject Behaviour Node";
        public static string nodeName = "GameObject";

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
            if (!gameObject.IsNull && !gameObject.IsShowMismatchWarning())
            {
                if (executionType == ExecutionType.DisableComponent && component != null)
                    return "Disable " + component;
                if (executionType == ExecutionType.EnableComponent && component != null)
                    return "Enable " + component;
                if (executionType == ExecutionType.Show)
                    return "Show " + gameObject.name;
                if (executionType == ExecutionType.Hide)
                    return "Hide " + gameObject.name;
                if (executionType == ExecutionType.Deactivate)
                    return "Deactivate " + gameObject.name;
                if (executionType == ExecutionType.Activate)
                    return "Activate " + gameObject.name;
                if (executionType == ExecutionType.Expel)
                    return "Expel " + gameObject.name;
                if (executionType == ExecutionType.SetLayer)
                    return "Set " + gameObject.name + " layer";
                if (executionType == ExecutionType.SetParent && !parentTransform.IsNull && !parentTransform.IsShowMismatchWarning())
                    return "Set " + gameObject.name + " parent";
                if (executionType == ExecutionType.Spawn)
                {
                    if (actionName != null)
                        return "Spawn " + gameObject.name + " at " + actionName + " action";
                    else
                        return "Spawn " + gameObject.name;
                }
            }

            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (executionType is ExecutionType.DisableComponent or ExecutionType.EnableComponent)
                tip += "This will enable / disable a component.\n\n";
            else if (executionType is ExecutionType.Show or ExecutionType.Hide)
                tip += "This will active / inactive a gameObject.\n\n";
            else if (executionType is ExecutionType.Deactivate)
                tip += "This will try to execute Runner Deactivate at the gameObject, it will inactive the gameObject if not found Deactivate trigger.\n\n";
            else if (executionType is ExecutionType.Activate)
                tip += "This will try to execute Runner Activate at the gameObject, it will active the gameObject if not found Activate trigger.\n\n";
            else if (executionType == ExecutionType.Spawn)
                tip += "This will spawn out a gameObject by using the Pool system. It will not set active to the spawned gameObject if it is inactive by default.\n\n";
            else if (executionType == ExecutionType.Expel)
                tip += "This will put a gameObject back to the Pool system.\n\n";
            else if (executionType == ExecutionType.SetLayer)
                tip += "This will set a gameObject to a specific layer.\n\n";
            else if (executionType == ExecutionType.SetParent)
                tip += "This will set the gameObject as a child gameobject of a gameobject.\n\n";
            else
                tip += "This will execute all Input related behaviour.\n\n";
            return tip + "We are using Input System from Unity.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}