using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReFramework;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class ActionBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            Graph = 10,
            Character = 20,
            TargetAim = 30,
            Stamina = 40,
            AttackSkill = 50,
            Behaviour = 60,
            GameObject = 70,
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [ShowIf("@executionType == ExecutionType.Graph")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(graph)")]
        [InlineButton("@graph.SetObjectValue(AssignComponent<GraphRunner>())", "♺", ShowIf = "@graph.IsObjectValueType()")]
        [InfoBox("@graph.GetMismatchWarningMessage()", InfoMessageType.Error, "@graph.IsShowMismatchWarning()")]
        private SceneObjectProperty graph = new SceneObjectProperty(SceneObject.ObjectType.GraphRunner);

        [SerializeField]
        [ShowIf("@executionType == ExecutionType.Character")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(character)")]
        [InlineButton("@character.SetObjectValue(AssignComponent<CharacterOperator>())", "♺", ShowIf = "@character.IsObjectValueType()")]
        [InfoBox("@character.GetMismatchWarningMessage()", InfoMessageType.Error, "@character.IsShowMismatchWarning()")]
        private SceneObjectProperty character = new SceneObjectProperty(SceneObject.ObjectType.CharacterOperator);

        [SerializeField]
        [ShowIf("@executionType == ExecutionType.GameObject")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(gameObject)")]
        [InlineButton("@gameObject.SetObjectValue(AssignComponent<GameObject>())", "♺", ShowIf = "@gameObject.IsObjectValueType()")]
        [InfoBox("@gameObject.GetMismatchWarningMessage()", InfoMessageType.Error, "@gameObject.IsShowMismatchWarning()")]
        private SceneObjectProperty gameObject = new SceneObjectProperty(SceneObject.ObjectType.GameObject);
            
        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.TargetAim")]
        private TargetAimPack targetAim;
        
        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.Stamina")]
        private StaminaPack stamina;
        
        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.AttackSkill")]
        private AttackSkillPack attackSkill;
        
        [SerializeField]
        [OnValueChanged("OnChangeBehaviourPack")]
        [OnInspectorInit("OnInitBehaviourPack")]
        [ShowIf("@executionType == ExecutionType.Behaviour")]
        private BehaviourPack behaviour;

        [SerializeField]
        [ValueDropdown("DrawActionNameListDropdown", ExpandAllMenuItems = true)]
        [OnValueChanged("MarkDirty")]
        private ActionNameChoice actionName;

        [SerializeField]
        [ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true, OnTitleBarGUI = "RefreshDrawInButton")]
        [ShowIf("@executionType == ExecutionType.Behaviour && behaviour")]
        [LabelText("Input Parameters")]
        [OnInspectorGUI("@MarkInParamsDirty()")]
        private List<GraphInputData> actionInParams;
        
        [SerializeField]
        [ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true, OnTitleBarGUI = "RefreshDrawOutButton")]
        [ShowIf("@executionType == ExecutionType.Behaviour && behaviour")]
        [LabelText("Output Parameters")]
        private List<GraphOutputData> actionOutParams;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (!actionName)
            {
                LogWarning("Found an empty Action Behaviour node in " + context.objectName);
            }
            else
            {
                if (executionType == ExecutionType.Graph)
                {
                    if (graph.IsEmpty || !graph.IsMatchType())
                        LogWarning("Found an empty Action Behaviour node in " + context.objectName);
                    else
                    {
                        var runner = (GraphRunner) graph;
                        if (runner)
                            runner.CacheExecute(runner.TriggerAction(actionName));
                    }
                }
                else if (executionType == ExecutionType.Character)
                {
                    if (character.IsEmpty || !graph.IsMatchType())
                        LogWarning("Found an empty Action Behaviour node in " + context.objectName);
                    else
                        ((CharacterOperator) character)?.FeedbackGraphTrigger(TriggerNode.Type.ActionTrigger, actionName: actionName);
                }
                else if (executionType == ExecutionType.GameObject)
                {
                    if (gameObject.IsEmpty || !gameObject.IsMatchType())
                        LogWarning("Found an empty Action Behaviour node in " + context.objectName);
                    else
                    {
                        var go = (GameObject) gameObject;
                        if (go)
                        {
                            if (go.TryGetComponent<GraphRunner>(out var runner))
                                runner.CacheExecute(runner.TriggerAction(actionName));
                        }
                    }
                }
                else if (executionType == ExecutionType.TargetAim)
                {
                    if (!targetAim)
                        LogWarning("Found an empty Action Behaviour node in " + context.objectName);
                    else
                        targetAim.TriggerAction(actionName, execution);
                }
                else if (executionType == ExecutionType.Stamina)
                {
                    if (!stamina)
                        LogWarning("Found an empty Action Behaviour node in " + context.objectName);
                    else
                        stamina.TriggerAction(actionName, execution);
                }
                else if (executionType == ExecutionType.AttackSkill)
                {
                    if (!attackSkill)
                        LogWarning("Found an empty Action Behaviour node in " + context.objectName);
                    else
                        attackSkill.TriggerAction(actionName, execution);
                }
                else if (executionType == ExecutionType.Behaviour)
                {
                    if (!behaviour)
                        LogWarning("Found an empty Action Behaviour node in " + context.objectName);
                    else
                    {
                        var beh = new BehaviourData(behaviour);
                        beh.TriggerBehaviour(actionName, actionInParams, actionOutParams);
                        if (beh.HaveExecuted())
                            beh.ClosingExecution();
                        beh.Terminate();
                    }
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        protected void MarkInParamsDirty ()
        {
            for (var i = 0; i < actionInParams.Count; i++)
            {
                if (actionInParams[i].dirty)
                    MarkDirty();
                actionInParams[i].dirty = false;
            }
        }

        private void RefreshDrawInButton ()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                DrawInParameters();
            }
        }
        
        private void RefreshDrawOutButton ()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                DrawOutParameters();
            }
        }

        private void OnInitBehaviourPack ()
        {
            DrawInParameters();
            DrawOutParameters();
        }
        
        private void OnChangeBehaviourPack ()
        {
            DrawInParameters();
            DrawOutParameters();
            MarkDirty();
        }
        
        private void DrawInParameters ()
        {
            if (behaviour && behaviour.graph is {inParameters: { }})
            {
                var tempParams = new List<GraphInputData>(actionInParams);
                actionInParams.Clear();
                for (var i = 0; i < behaviour.graph.inParameters.Length; i++)
                {
                    if (behaviour.graph.inParameters[i])
                    {
                        var found = false;
                        for (var j = 0; j < tempParams.Count; j++)
                        {

                            if (tempParams[j].name == behaviour.graph.inParameters[i].name)
                            {
                                if (tempParams[j].type == (int) GraphInputData.Type.Object)
                                {
                                    if (behaviour.graph.inParameters[i] is SceneObjectVariable)
                                    {
                                        var inParam = (SceneObjectVariable) behaviour.graph.inParameters[i];
                                        if (tempParams[j].objectVar.objectValue.type == inParam.sceneObject.type)
                                        {
                                            actionInParams.Add(tempParams[j]);
                                            found = true;
                                            break;
                                        }
                                    }

                                }
                                else
                                {
                                    actionInParams.Add(tempParams[j]);
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if (!found)
                            actionInParams.Add(new GraphInputData(behaviour.graph.inParameters[i]));
                    }
                }
            }
        }
        
        private void DrawOutParameters ()
        {
            if (behaviour && behaviour.graph is {outParameters: { }})
            {
                var tempParams = new List<GraphOutputData>(actionOutParams);
                actionOutParams.Clear();
                for (var i = 0; i < behaviour.graph.outParameters.Length; i++)
                {
                    if (behaviour.graph.outParameters[i])
                    {
                        var found = false;
                        for (var j = 0; j < tempParams.Count; j++)
                        {
                            if (tempParams[j].name == behaviour.graph.outParameters[i].name)
                            {
                                actionOutParams.Add(tempParams[j]);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                            actionOutParams.Add(new GraphOutputData(behaviour.graph.outParameters[i]));
                    }
                }
            }
        }
        
        public string ActionName => actionName == null ? string.Empty : actionName;
        public GraphRunner Runner => executionType == ExecutionType.Graph && !graph.IsEmpty && graph.IsMatchType() ? (GraphRunner) graph : null;

        public GraphScriptable Scriptable
        {
            get
            {
                if (executionType == ExecutionType.TargetAim && targetAim != null)
                    return targetAim;
                if (executionType == ExecutionType.Stamina && stamina != null)
                    return stamina;
                if (executionType == ExecutionType.AttackSkill && attackSkill != null)
                    return attackSkill;
                if (executionType == ExecutionType.Behaviour && behaviour != null)
                    return behaviour;
                return null;
            }
        }

        private ValueDropdownList<ExecutionType> TypeChoice ()
        {
            var listDropdown = new ValueDropdownList<ExecutionType>();
            var curGraph = GetGraph();
            if (curGraph is {isTargetAimPack: true})
            {
                listDropdown.Add("TargetAim", ExecutionType.TargetAim);
            }
            else if (curGraph is {isStaminaPack: true})
            {
                listDropdown.Add("Stamina", ExecutionType.Stamina);
            }
            else if (curGraph is {isAttackSkillPack: true})
            {
                listDropdown.Add("Attack Skill", ExecutionType.AttackSkill);
                listDropdown.Add("Character", ExecutionType.Character);
            }
            else if (curGraph is {isBehaviourPack: true})
            {
                listDropdown.Add("Behaviour", ExecutionType.Behaviour);
                listDropdown.Add("GameObject", ExecutionType.GameObject);
                listDropdown.Add("Character", ExecutionType.Character);
            }
            else
            {
                listDropdown.Add("Graph", ExecutionType.Graph);
                listDropdown.Add("GameObject", ExecutionType.GameObject);
                listDropdown.Add("Character", ExecutionType.Character);
                listDropdown.Add("Behaviour", ExecutionType.Behaviour);
            }

            return listDropdown;
        }

        private static IEnumerable DrawActionNameListDropdown ()
        {
            return ActionNameChoice.GetActionNameListDropdown();
        }

        public static string displayName = "Action Behaviour Node";
        public static string nodeName = "Action";

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
            return $"Logic/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType == ExecutionType.Graph)
                if (!graph.IsNull && graph.IsMatchType() && actionName != null)
                    return "Execute " + actionName + " in graph of " + graph.objectName;
            if (executionType == ExecutionType.GameObject)
                if (!gameObject.IsNull && gameObject.IsMatchType() && actionName != null)
                    return "Execute " + actionName + " in graph of " + gameObject.objectName;
            if (executionType == ExecutionType.Character)
                if (!character.IsNull && graph.IsMatchType() && actionName != null)
                    return "Execute " + actionName + " in graph of " + character.objectName;
            if (executionType == ExecutionType.TargetAim)
                if (targetAim != null && actionName != null)
                    return "Execute " + actionName + " in graph of " + targetAim.name;
            if (executionType == ExecutionType.Stamina)
                if (stamina != null && actionName != null)
                    return "Execute " + actionName + " in graph of " + stamina.name;
            if (executionType == ExecutionType.AttackSkill)
                if (attackSkill != null && actionName != null)
                    return "Execute " + actionName + " in graph of " + attackSkill.name;
            if (executionType == ExecutionType.Behaviour)
                if (behaviour != null && actionName != null)
                    return "Execute " + actionName + " in graph of " + behaviour.name;
            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            return "This will execute another Action Trigger node at specific graph.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}