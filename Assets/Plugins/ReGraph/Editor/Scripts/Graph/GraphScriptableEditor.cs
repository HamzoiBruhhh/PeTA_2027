using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [CustomEditor(typeof(GraphScriptable))]
    public class GraphScriptableEditor : GraphRunnerEditor
    {
        public override void OnInspectorGUI ()
        {
            saveSceneMessage = "All graph data in saved in this scriptable file.";
            base.OnInspectorGUI();
        }
    }

    [CustomEditor(typeof(AttackDamagePack))]
    public class AttackDamagePackEditor : GraphScriptableEditor { }
    
    [CustomEditor(typeof(TargetAimPack))]
    public class TargetAimPackEditor : GraphScriptableEditor { }
    
    [CustomEditor(typeof(StaminaPack))]
    public class StaminaPackEditor : GraphScriptableEditor { }
    
    [CustomEditor(typeof(MoralePack))]
    public class MoralePackEditor : GraphScriptableEditor { }
    
    [CustomEditor(typeof(AttackStatusPack))]
    public class AttackStatusPackEditor : GraphScriptableEditor { }
    
    [CustomEditor(typeof(LootPack))]
    public class LootPackEditor : GraphScriptableEditor { }
    
    [CustomEditor(typeof(AttackSkillPack))]
    public class AttackSkillPackEditor : GraphScriptableEditor { }
    
    [CustomEditor(typeof(BehaviourPack))]
    public class BehaviourPackEditor : GraphScriptableEditor { }
}