using UnityEngine;
using Reshape.ReGraph;
#if UNITY_EDITOR
using Reshape.Unity.Editor;
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Target Aim Pack", fileName = "TargetAimPack", order = 403)]
    public class TargetAimPack : GraphScriptable
    {
        private GraphContext context;

        public TargetAimData ChooseTarget (TargetAimData aimData)
        {
            if (aimData != null)
            {
                InitContext();
                aimData.lastExecuteResult = Activate(TriggerNode.Type.ChooseAimTarget, aimData.id, aimData: aimData);
            }

            return aimData;
        }
        
        public TargetAimData ChooseHurtTarget (TargetAimData aimData)
        {
            if (aimData != null)
            {
                InitContext();
                aimData.lastExecuteResult = Activate(TriggerNode.Type.ChooseHurtTarget, aimData.id, aimData: aimData);
            }

            return aimData;
        }

        public override GraphExecution TriggerAction (ActionNameChoice type, GraphExecution execution)
        {
            if (type != null)
            {
                InitContext();
                return Activate(TriggerNode.Type.ActionTrigger, actionName: type, aimData: execution.parameters.targetAimData);
            }

            return null;
        }

        public override GraphExecution InternalTrigger (string type, GraphExecution execution)
        {
            return Activate(TriggerNode.Type.All, actionName: type, aimData: execution.parameters.targetAimData);
        }

        protected override void CreateGraph ()
        {
            graph.Create(Graph.GraphType.TargetAimPack);
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
        public static void CreateNew (CharacterBrain brain)
        {
            var created = CreateNew();
            if (created != null)
                brain.targetAimPack = created;
        }
        
        public static TargetAimPack CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Graph Scriptable", "New Target Aim Pack", "asset", "Select a location to create graph scriptable");
            return path.Length == 0 ? null : ReEditorHelper.CreateScriptableObject<TargetAimPack>(null, false, false, string.Empty, path);
        }
#endif
    }
}