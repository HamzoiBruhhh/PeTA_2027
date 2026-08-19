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
    [CreateAssetMenu(menuName = "Reshape/Attack Skill Pack", fileName = "AttackSkillPack", order = 405)]
    public class AttackSkillPack : GraphScriptable
    {
        [Hint("showHints", "Define the attack skill's name display on UI.")]
        [FoldoutGroup("Pack Properties")]
        [InlineProperty]
        [SerializeField]
        [LabelText("Display Name")]
        private StringProperty uiName;
        
        [Hint("showHints", "Define the attack skill's description display on UI.")]
        [FoldoutGroup("Pack Properties")]
        [InlineProperty]
        [SerializeField]
        [LabelText("Display Desc.")]
        private StringProperty uiDescription;
        
        [Hint("showHints", "Define the attack skill's icon display on UI.")]
        [FoldoutGroup("Pack Properties")]
        [SerializeField]
        [LabelText("Display Icon")]
        private Sprite uiIcon;
        
        [Hint("showHints", "Define the attack skill is constantly update.")]
        [FoldoutGroup("Pack Properties")]
        [SerializeField]
        private bool constantly;
        
        [Hint("showHints", "Define how frequent it update once.")]
        [ShowIf("@constantly == true")]
        [Indent]
        [InlineProperty]
        [FoldoutGroup("Pack Properties")]
        [LabelText("Update Time")]
        [SuffixLabel("sec        ", true)]
        [SerializeField]
        private FloatProperty constantUpdateTime;
        
        [Hint("showHints", "Define the attack skill will consume how many stamina.")]
        [FoldoutGroup("Pack Properties")]
        [InlineProperty]
        [SerializeField]
        private FloatProperty staminaConsume;
        
        [Hint("showHints", "Define the attack skill will consume how many stamina.")]
        [FoldoutGroup("Pack Properties")]
        [InlineProperty]
        [SuffixLabel("sec        ", true)]
        [SerializeField]
        private FloatProperty cooldown;
        
        private GraphContext context;

        public string displayName => this.uiName;
        public Sprite displayIcon => this.uiIcon;
        public string displayDescription => this.uiDescription;
        public float cooldownTime => this.cooldown;
        public float staminaSpent => this.staminaConsume;
        public bool haveConstantUpdate => constantly;
        public float constantUpdateInterval => constantUpdateTime;
        
        public bool HaveLaunching (TriggerNode.Type triggerType)
        {
            return DetectActivate(TriggerNode.Type.AttackSkillLaunch, (int)triggerType);
        }

        public void TriggerLaunching (AttackSkillData skillData)
        {
            if (skillData != null)
            {
                InitContext();
                skillData.lastExecuteResult = Activate(TriggerNode.Type.AttackSkillLaunch, skillData.id, attackSkillData: skillData);
            }
        }
        
        public bool HaveCompleting ()
        {
            return DetectActivate(TriggerNode.Type.AttackSkillComplete);
        }
        
        public void TriggerCompleting (AttackSkillData skillData)
        {
            if (skillData != null)
            {
                InitContext();
                skillData.lastExecuteResult = Activate(TriggerNode.Type.AttackSkillComplete, skillData.id, attackSkillData: skillData);
            }
        }
        
        public bool HaveLToggling ()
        {
            return DetectActivate(TriggerNode.Type.AttackSkillToggle);
        }
        
        public void TriggerToggling (AttackSkillData skillData)
        {
            if (skillData != null)
            {
                InitContext();
                skillData.lastExecuteResult = Activate(TriggerNode.Type.AttackSkillToggle, skillData.id, attackSkillData: skillData);
            }
        }

        public bool HaveDetecting (TriggerNode.Type triggerType)
        {
            return DetectActivate(TriggerNode.Type.AttackSkillDetect, (int)triggerType);
        }
        
        public void TriggerDetecting (AttackSkillData skillData)
        {
            if (skillData != null)
            {
                InitContext();
                skillData.lastExecuteResult = Activate(TriggerNode.Type.AttackSkillDetect, skillData.id, attackSkillData: skillData);
            }
        }
        
        public bool HaveBegin ()
        {
            return DetectActivate(TriggerNode.Type.AttackSkillBegin);
        }
        
        public void TriggerBegin (AttackSkillData skillData)
        {
            if (skillData != null)
            {
                InitContext();
                skillData.lastExecuteResult = Activate(TriggerNode.Type.AttackSkillBegin, skillData.id, attackSkillData: skillData);
            }
        }
        
        public bool HaveUpdate ()
        {
            return DetectActivate(TriggerNode.Type.AttackSkillUpdate);
        }
        
        public void TriggerUpdate (AttackSkillData skillData)
        {
            if (skillData != null)
            {
                InitContext();
                skillData.lastExecuteResult = Activate(TriggerNode.Type.AttackSkillUpdate, skillData.id, attackSkillData: skillData);
            }
        }
        
        public bool HaveEnd ()
        {
            return DetectActivate(TriggerNode.Type.AttackSkillEnd);
        }
        
        public void TriggerEnd (AttackSkillData skillData)
        {
            if (skillData != null)
            {
                InitContext();
                skillData.lastExecuteResult = Activate(TriggerNode.Type.AttackSkillEnd, skillData.id, attackSkillData: skillData);
            }
        }

        public override GraphExecution TriggerAction (ActionNameChoice type, GraphExecution execution)
        {
            if (type != null)
            {
                InitContext();
                return Activate(TriggerNode.Type.ActionTrigger, actionName: type, attackSkillData: execution.parameters.attackSkillData);
            }

            return null;
        }

        public override GraphExecution InternalTrigger (string type, GraphExecution execution)
        {
            return Activate(TriggerNode.Type.All, actionName: type, attackSkillData: execution.parameters.attackSkillData);
        }

        protected override void CreateGraph ()
        {
            graph.Create(Graph.GraphType.AttackSkillPack);
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
        public static AttackSkillPack CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Graph Scriptable", "New Attack Skill Pack", "asset", "Select a location to create graph scriptable");
            return path.Length == 0 ? null : ReEditorHelper.CreateScriptableObject<AttackSkillPack>(null, false, false, string.Empty, path);
        }
#endif
    }
}