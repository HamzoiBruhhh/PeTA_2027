using System.Collections.Generic;
using Reshape.Unity;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public abstract class TriggerNode : GraphNode
    {
        public enum Type
        {
            None,
            CollisionEnter = 11,
            CollisionExit = 12,
            CollisionStepIn = 13,
            CollisionStepOut = 14,
            ActionTrigger = 21,
            GameObjectSpawn = 31,
            InputPress = 61,
            InputRelease = 62,
            VideoFinished = 71,
            AudioFinished = 81,
            VariableChange = 91,
            RayAccepted = 150,
            RayMissed = 151,
            RayHit = 152,
            RayStay = 153,
            RayLeave = 154,
            RayArrive = 155,
            OcclusionStart = 160,
            OcclusionEnd = 161,
            OnStart = 200,
            OnEnd = 201,
            OnEnable = 202,
            OnDeactivate = 205,
            OnActivate = 206,
            InventoryQuantityChange = 251,
            InventorySlotChange = 252,
            InventoryDecayChange = 253,
            InventoryItemChange = 254,
            InventoryUnLockRequest = 261,
            BrainStatGet = 301,
            BrainStatChange = 302,
            SelectReceive = 491,
            SelectConfirm = 492,
            SelectFinish = 493,
            InteractLaunch = 501,
            InteractReceive = 502,
            InteractFinish = 503,
            InteractLeave = 504,
            InteractCancel = 505,
            InteractGiveUp = 506,
            CharacterAttackGoMelee = 531,
            CharacterAttackFire = 551,
            CharacterAttackBackstab = 552,
            CharacterAttackEnd = 553,
            CharacterAttackSkill = 554,
            CharacterAttackBreak = 555,
            CharacterKill = 561,
            CharacterStanceDone = 581,
            CharacterUnstanceDone = 582,
            CharacterDead = 701,
            CharacterTerminate = 702,
            CharacterGetBackstab = 751,
            CharacterGetAttack = 752,
            CharacterGetHurt = 753,
            CharacterGetInterrupt = 754,
            CharacterFriendDead = 901,
            CharacterScanVicinity = 1201,
            CalculateAttackDamage = 5000,
            ChooseAimTarget = 5100,
            ChooseHurtTarget = 5110,
            MoraleChange = 5200,
            AttackStatusBegin = 5300,
            AttackStatusEnd = 5301,
            AttackStatusUpdate = 5302,
            LootGenerate = 5400,
            StaminaConsume = 5500,
            AttackSkillDetect = 5600,
            AttackSkillLaunch = 5601,
            AttackSkillComplete = 5602,
            AttackSkillActivate = 5603,
            AttackSkillToggle = 5604,
            AttackSkillBegin = 5620,
            AttackSkillEnd = 5621,
            AttackSkillUpdate = 5622,
            All = 99999
        }

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (children == null) return;
            for (var i = 0; i < children.Count; i++)
            {
                if (children[i] != null)
                    execution.variables.SetInt(children[i].guid, (int) State.Running);
            }
        }

        protected virtual void OnSuccess ()
        {
#if UNITY_EDITOR
            OnPrintFlow(99);
#endif
        }

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            if (children == null) return State.Failure;

            var stillRunning = false;
            var containFailure = false;
            for (var i = 0; i < children.Count; ++i)
            {
                if (children[i] != null)
                {
                    var state = execution.variables.GetInt(children[i].guid);
                    if (state == (int) State.Running)
                    {
                        var status = children[i].Update(execution, updateId);
                        execution.variables.SetInt(children[i].guid, (int) status);
                        if (status == State.Failure)
                            containFailure = true;
                        else if (status == State.Running)
                            stillRunning = true;
                        else if (status == State.Break)
                        {
                            containFailure = true;
                            break;
                        }
                    }
                    else if (state == (int) State.Failure)
                    {
                        containFailure = true;
                    }
                }
            }

            if (stillRunning)
                return State.Running;
            if (containFailure)
                return State.Failure;
            return State.Success;
        }

        protected override void OnStop (GraphExecution execution, int updateId) { }
        protected override void OnInit () { }
        protected override void OnReset () { }
        
        protected override void OnPause (GraphExecution execution)
        {
            if (children != null)
                for (int i = 0; i < children.Count; ++i)
                    children[i].Pause(execution);
        }
        
        protected override void OnUnpause (GraphExecution execution)
        {
            if (children != null)
                for (int i = 0; i < children.Count; ++i)
                    children[i].Unpause(execution);
        }

        protected override State OnDisabled (GraphExecution execution, int updateId)
        {
            return State.Failure;
        }

        public string TriggerId => guid;

        public override ChildrenType GetChildrenType ()
        {
            return ChildrenType.Multiple;
        }

        public override void GetChildren (ref List<GraphNode> list)
        {
            if (children != null)
                for (var i = 0; i < children.Count; i++)
                    list.Add(children[i]);
        }
        
        public override void GetParents (ref List<GraphNode> list)
        {
            if (parents != null)
                for (var i = 0; i < parents.Count; i++)
                    list.Add(parents[i]);
        }
        
        public override bool IsRequireUpdate ()
        {
            return false;
        }
        
        public override bool IsRequireInit ()
        {
            return false;
        }
        
        public override bool IsRequireBegin ()
        {
            return false;
        }
        
        public override bool IsRequirePreUninit ()
        {
            return false;
        }
        
        public override bool IsTrigger (TriggerNode.Type type, int paramInt = 0)
        {
            return true;
        }
        
#if UNITY_EDITOR
        public override void OnPrintFlow (int state)
        {
            if (state == 99 && context.runner && context.runner.printFlowLog)
                ReDebug.Log($"{context.runner.gameObject.name} GraphFlow", $"Start {GetNodeInspectorTitle().Replace(" ","")}.{GetNodeIdentityName()} [{guid}]");
        }

        public virtual Type GetNodeType ()
        {
            return Type.None;
        }

        public override string GetNodeMenuDisplayName ()
        {
            return string.Empty;
        }
        
        public override string GetNodeIdentityName ()
        {
            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "Trigger node act as event trigger. It only can connect to Behaviour nodes, Behaviour nodes linked to this will get trigger when this event happen.";
        }
#endif
    }
}