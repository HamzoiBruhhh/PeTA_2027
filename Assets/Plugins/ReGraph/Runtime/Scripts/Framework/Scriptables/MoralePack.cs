using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReGraph;
#if UNITY_EDITOR
using Reshape.Unity.Editor;
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Morale Pack", fileName = "MoralePack", order = 308)]
    [HideMonoScript]
    [Serializable]
    public class MoralePack : GraphScriptable
    {
        [Hint("showHints", "Define which event will affects morale increase/decrease.")]
        [ListDrawerSettings(ShowPaging = false, CustomAddFunction = "CreateNewMoraleEventItem")]
        [LabelText("Events")]
        [FoldoutGroup("Properties & Events")]
        [HideInInspector]
        public List<MoraleEventItem> moraleEvents;

        private GraphContext context;

        public float GetAffect (Morale.EventType eventType, AttackDamageData damageData)
        {
            if (moraleEvents == null)
                return 0f;
            var count = moraleEvents.Count;
            for (var i = 0; i < count; i++)
                if (moraleEvents[i].eventType == eventType)
                    return moraleEvents[i].value;
            return 0f;
        }

        public MoraleData TriggerValueChanged (MoraleData moraleData)
        {
            if (moraleData != null)
            {
                InitContext();
                moraleData.lastExecuteResult = Activate(TriggerNode.Type.MoraleChange, moraleData.id, moraleData: moraleData);
            }

            return moraleData;
        }

        public override GraphExecution InternalTrigger (string type, GraphExecution execution)
        {
            return Activate(TriggerNode.Type.All, actionName: type, moraleData: execution.parameters.moraleData);
        }

        protected override void CreateGraph ()
        {
            graph.Create(Graph.GraphType.MoralePack);
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
        public void CreateNewMoraleEventItem ()
        {
            var item = new MoraleEventItem(this);
            moraleEvents.Add(item);
        }

        public static void CreateNew (CharacterBrain brain)
        {
            var created = CreateNew();
            if (created != null)
                brain.moralePack = created;
        }

        public static MoralePack CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Graph Scriptable", "New Morale Pack", "asset", "Select a location to create graph scriptable");
            return path.Length == 0 ? null : ReEditorHelper.CreateScriptableObject<MoralePack>(null, false, false, string.Empty, path);
        }
#endif
    }
}