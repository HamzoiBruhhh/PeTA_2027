using System;
using UnityEngine;
using Reshape.ReGraph;
using Reshape.Unity;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Attack Damage Pack", fileName = "AttackDamagePack", order = 401)]
    public class AttackDamagePack : GraphScriptable
    {
        private GraphContext context;
        private float impairedDamage;

        public AttackDamageData CalculateDamage (AttackDamageData attackData)
        {
            if (attackData != null)
            {
                if (context.isUnassigned)
                {
                    context = new GraphContext(this);
                    graph.Bind(context);
                }

                attackData.lastExecuteResult = Activate(TriggerNode.Type.CalculateAttackDamage, attackData.id, attackData: attackData);
                attackData.lastExecuteResult.MarkAsReverse();
            }

            return attackData;
        }
        
        public override GraphExecution InternalTrigger (string type, GraphExecution execution)
        {
            return Activate(TriggerNode.Type.All, actionName: type, attackData:execution.parameters.attackDamageData);
        }

        protected override void CreateGraph ()
        {
            graph.Create(Graph.GraphType.AttackDamagePack);
        }

#if UNITY_EDITOR
        public static void CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Graph Scriptable", "New Attack Damage Pack", "asset", "Select a location to create graph scriptable");
            if (path.Length == 0)
                return;
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                return;

            var scriptable = ScriptableObject.CreateInstance<AttackDamagePack>();
            AssetDatabase.CreateAsset(scriptable, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }
}