using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReGraph;

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Loot Pack", fileName = "Loot Pack", order = 503)]
    [HideMonoScript]
    [Serializable]
    public class LootPack : GraphScriptable
    {
        [InlineProperty]
        [FoldoutGroup("Properties & Events")]
        public FloatProperty size = new FloatProperty(InventoryBehaviour.DEFAULT_SIZE);
        
        [InlineProperty]
        [FoldoutGroup("Properties & Events")]
        public FloatProperty stack = new FloatProperty(InventoryBehaviour.DEFAULT_STACK);
        
        [InlineProperty]
        [FoldoutGroup("Properties & Events")]
        [LabelText("# Per Row")]
        public FloatProperty rows = new FloatProperty(0);
        
        private GraphContext context;
        
        public LootData TriggerGenerate (LootData lootData)
        {
            if (lootData != null)
            {
                InitContext();
                lootData.lastExecuteResult = Activate(TriggerNode.Type.LootGenerate, lootData.id, lootData: lootData);
            }

            return lootData;
        }

        public override GraphExecution InternalTrigger (string type, GraphExecution execution)
        {
            return Activate(TriggerNode.Type.All, actionName: type, lootData: execution.parameters.lootData);
        }

        protected override void CreateGraph ()
        {
            graph.Create(Graph.GraphType.LootPack);
        }

        private void InitContext ()
        {
            if (context.isUnassigned)
            {
                context = new GraphContext(this);
                graph.Bind(context);
            }
        }
    }
}