using System.Collections.Generic;
using Reshape.ReGraph;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Reshape.Unity.Editor;
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Stamina Pack", fileName = "StaminaPack", order = 307)]
    public class StaminaPack : GraphScriptable
    {
        [Hint("showHints", "Define which unit's action will consume how many stamina.")]
        [ListDrawerSettings(ShowPaging = false, CustomAddFunction = "CreateNewStaminaSheetItem")]
        [LabelText("Consume")]
        [HideInInspector]
        public List<StaminaSheetItem> staminaConsume;

        [Hint("showHints", "Define the unit is charging his stamina while walking.")]
        [FoldoutGroup("Pack Properties"), LabelText("Charge During Walk")]
        public bool chargeDuringWalk;
        
        [Hint("showHints", "Define the unit is charging his stamina while fighting.")]
        [FoldoutGroup("Pack Properties"), LabelText("Charge During Fight")]
        public bool chargeDuringFight;
        
        [Hint("showHints", "Define the unit is charging his stamina while idling.")]
        [FoldoutGroup("Pack Properties"), LabelText("Charge During Idle")]
        public bool chargeDuringIdle;
        
        private GraphContext context;

        public float GetConsumeValue (Stamina.Type type)
        {
            if (staminaConsume == null) 
                return 0f;
            var count = staminaConsume.Count;
            for (var i = 0; i < count; i++)
                if (staminaConsume[i].type == type)
                    return staminaConsume[i].value;
            return 0f;
        }

        public StaminaData TriggerConsume (StaminaData staminaData)
        {
            if (staminaData != null)
            {
                InitContext();
                staminaData.lastExecuteResult = Activate(TriggerNode.Type.StaminaConsume, staminaData.id, staminaData: staminaData);
            }

            return staminaData;
        }
        
        public override GraphExecution TriggerAction (ActionNameChoice type, GraphExecution execution)
        {
            if (type)
            {
                InitContext();
                return Activate(TriggerNode.Type.ActionTrigger, actionName: type, staminaData: execution.parameters.staminaData);
            }

            return null;
        }

        public override GraphExecution InternalTrigger (string type, GraphExecution execution)
        {
            return Activate(TriggerNode.Type.All, actionName: type, staminaData: execution.parameters.staminaData);
        }

        protected override void CreateGraph ()
        {
            graph.Create(Graph.GraphType.StaminaPack);
        }

        private void InitContext ()
        {
            if (context.isUnassigned)
            {
                context = new GraphContext(this);
                graph.Bind(context);
            }
        }
        
#if UNITY_EDITOR
        public void CreateNewStaminaSheetItem ()
        {
            var item = new StaminaSheetItem(this);
            staminaConsume.Add(item);
        }

        public static void CreateNew (CharacterBrain brain)
        {
            var created = CreateNew();
            if (created != null)
                brain.staminaPack = created;
        }
        
        public static StaminaPack CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Graph Scriptable", "New Stamina Pack", "asset", "Select a location to create graph scriptable");
            return path.Length == 0 ? null : ReEditorHelper.CreateScriptableObject<StaminaPack>(null, false, false, string.Empty, path);
        }
#endif
    }
}