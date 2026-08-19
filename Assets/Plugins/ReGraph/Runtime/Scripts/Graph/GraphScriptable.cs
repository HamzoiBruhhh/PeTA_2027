using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReFramework;
using Reshape.Unity;
#if UNITY_EDITOR
using UnityEditor;
using Reshape.Unity.Editor;
#endif

namespace Reshape.ReGraph
{
    [HideMonoScript]
    public class GraphScriptable : BaseScriptable
    {
        [HideLabel]
        [HideIf("ShowCreateButton")]
        public Graph graph;

        private GraphContext context;

        public virtual GraphExecution TriggerAction (ActionNameChoice type, GraphExecution execution)
        {
            if (type != null)
                return Activate(TriggerNode.Type.ActionTrigger, actionName: type);
            return null;
        }

        public virtual GraphExecution InternalTrigger (string type, GraphExecution execution)
        {
            return Activate(TriggerNode.Type.All, actionName: type);
        }

        protected virtual void CreateGraph ()
        {
            graph.Create(Graph.GraphType.GraphScriptable);
        }

        protected bool DetectActivate (TriggerNode.Type type, int paramInt = 0)
        {
            return graph.HaveTrigger(type, true, paramInt);
        }

        protected GraphExecution Activate (TriggerNode.Type type, string actionName = null, AttackDamageData attackData = null, TargetAimData aimData = null, MoraleData moraleData = null,
            AttackStatusData attackStatusData = null, LootData lootData = null, StaminaData staminaData = null, AttackSkillData attackSkillData = null, BehaviourData behaviourData = null)
        {
            var execute = graph?.InitExecute(ReUniqueId.GenerateLong(), type);
            if (execute != null)
            {
                switch (type)
                {
                    case TriggerNode.Type.All:
                    case TriggerNode.Type.ActionTrigger:
                        execute.parameters.actionName = actionName;
                        execute.parameters.targetAimData = aimData;
                        execute.parameters.attackDamageData = attackData;
                        execute.parameters.attackStatusData = attackStatusData;
                        execute.parameters.moraleData = moraleData;
                        execute.parameters.lootData = lootData;
                        execute.parameters.staminaData = staminaData;
                        execute.parameters.attackSkillData = attackSkillData;
                        execute.parameters.behaviourData = behaviourData;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.ChooseAimTarget:
                    case TriggerNode.Type.ChooseHurtTarget:
                        execute.parameters.actionName = actionName;
                        execute.parameters.targetAimData = aimData;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.CalculateAttackDamage:
                        execute.parameters.actionName = actionName;
                        execute.parameters.attackDamageData = attackData;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.MoraleChange:
                        execute.parameters.actionName = actionName;
                        execute.parameters.moraleData = moraleData;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        execute.MarkAsReverse();
                        break;
                    case TriggerNode.Type.AttackStatusBegin:
                    case TriggerNode.Type.AttackStatusEnd:
                    case TriggerNode.Type.AttackStatusUpdate:
                        execute.parameters.actionName = actionName;
                        execute.parameters.attackStatusData = attackStatusData;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.LootGenerate:
                        execute.parameters.actionName = actionName;
                        execute.parameters.lootData = lootData;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.StaminaConsume:
                        execute.parameters.actionName = actionName;
                        execute.parameters.staminaData = staminaData;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.AttackSkillBegin:
                    case TriggerNode.Type.AttackSkillEnd:
                    case TriggerNode.Type.AttackSkillUpdate:
                    case TriggerNode.Type.AttackSkillLaunch:
                    case TriggerNode.Type.AttackSkillDetect:
                    case TriggerNode.Type.AttackSkillToggle:
                    case TriggerNode.Type.AttackSkillComplete:
                        execute.parameters.actionName = actionName;
                        execute.parameters.attackSkillData = attackSkillData;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        execute.MarkAsReverse();
                        break;
                }

                graph?.CleanExecutes();
            }

            return execute;
        }

#if UNITY_EDITOR
        [ShowIf("ShowCreateButton")]
        [Button("Create Graph")]
        public void Initial ()
        {
            graph = new Graph();
            CreateGraph();
        }

        private bool ShowCreateButton ()
        {
            if (graph == null)
                return true;
            if (graph.Type == Reshape.ReGraph.Graph.GraphType.None)
                return true;
            if (!graph.isCreated)
                return true;
            return false;
        }

        [ShowIf("ShowSaveButton")]
        [Button("Save")]
        private void SaveScriptableGraph ()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private bool ShowSaveButton ()
        {
            if (graph == null)
                return false;
            if (graph.Type == Reshape.ReGraph.Graph.GraphType.None)
                return false;
            if (!graph.isCreated)
                return false;
            if (Application.isPlaying)
                return false;
            return true;
        }
#endif
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class GraphScriptableResetOnPlay
    {
        static GraphScriptableResetOnPlay ()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged (PlayModeStateChange state)
        {
            ReEditorHelper.HavePlayModeStateChange(state, out var enter, out var exit);
            if (exit)
            {
                var guids = AssetDatabase.FindAssets("t:GraphScriptable");
                if (guids.Length > 0)
                {
                    for (var i = 0; i < guids.Length; i++)
                    {
                        var scriptable = (GraphScriptable) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(UnityEngine.Object));
                        if (scriptable != null && scriptable.graph != null)
                        {
                            scriptable.graph.ClearExecutes();
                        }
                    }

                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
#endif
}