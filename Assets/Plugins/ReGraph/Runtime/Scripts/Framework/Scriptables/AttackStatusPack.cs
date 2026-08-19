using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReGraph;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Attack Status Pack", fileName = "Attack Status Pack", order = 404)]
    [HideMonoScript]
    [Serializable]
    public class AttackStatusPack : GraphScriptable
    {
        [FoldoutGroup("Pack Properties")]
        public int customId;

        [Hint("showHints", "Define the attack status is stackable.")]
        [FoldoutGroup("Pack Properties")]
        public bool stackable;

        [Hint("showHints", "Define the existing attack status get replace when the same pack found on unit.")]
        [ShowIf("@stackable == false")]
        [Indent]
        [FoldoutGroup("Pack Properties")]
        public bool replaceable;

        [Hint("showHints", "Define the attack status is constantly update, mainly use for constant effects.")]
        [FoldoutGroup("Pack Properties")]
        public bool constantly;

        [Hint("showHints", "Define how frequent it update once.")]
        [ShowIf("@constantly == true")]
        [Indent]
        [InlineProperty]
        [FoldoutGroup("Pack Properties")]
        [LabelText("Update Time")]
        public FloatProperty constantUpdateTime;

        private GraphContext context;

        public AttackStatusData TriggerActivation (AttackStatusData statusData)
        {
            if (statusData != null)
            {
                InitContext();
                statusData.lastExecuteResult = Activate(TriggerNode.Type.AttackStatusBegin, statusData.id, attackStatusData: statusData);
            }

            return statusData;
        }

        public AttackStatusData TriggerUpdateActive (AttackStatusData statusData)
        {
            if (statusData != null)
            {
                InitContext();
                statusData.lastExecuteResult = Activate(TriggerNode.Type.AttackStatusUpdate, statusData.id, attackStatusData: statusData);
            }

            return statusData;
        }

        public AttackStatusData TriggerDeactivation (AttackStatusData statusData)
        {
            if (statusData != null)
            {
                InitContext();
                statusData.lastExecuteResult = Activate(TriggerNode.Type.AttackStatusEnd, statusData.id, attackStatusData: statusData);
            }

            return statusData;
        }

        public override GraphExecution InternalTrigger (string type, GraphExecution execution)
        {
            return Activate(TriggerNode.Type.All, actionName: type, attackStatusData: execution.parameters.attackStatusData);
        }

        protected override void CreateGraph ()
        {
            graph.Create(Graph.GraphType.AttackStatusPack);
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
        public static ValueDropdownList<ScriptableObject> GetListDropdown ()
        {
            var dropdown = new ValueDropdownList<ScriptableObject>();
            var guids = AssetDatabase.FindAssets("t:AttackStatusPack");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var pack = AssetDatabase.LoadAssetAtPath<AttackStatusPack>(path);
                dropdown.Add(pack.name, pack);
            }

            return dropdown;
        }
#endif
    }
}