using Reshape.ReGraph;
using Reshape.Unity;

namespace Reshape.ReFramework
{
    public class AttackDamageData
    {
        public GraphExecution lastExecuteResult;

        private string reid;
        private AttackDamagePack pack;
        private CharacterOperator owner;
        private CharacterOperator target;
        private CharacterOperator.AttackType type;
        private float impairedDamage;
        private float impairedHurt;
        private float totalDamageDeal;
        private bool useRecordedBrain;
        private CharacterBrain ownerBrain;
        private bool manualMissed;

        public string id => reid;
        public CharacterOperator attacker => owner;
        public CharacterOperator defender => target;
        public CharacterBrain attackerBrain => useRecordedBrain ? ownerBrain : owner.brain;
        public CharacterBrain defenderBrain => target.brain;
        
        public void Init (CharacterOperator characterOperator, CharacterOperator.AttackType attackType, AttackDamagePack attackPack, bool usePreAttackStat)
        {
            reid = ReUniqueId.GenerateId(false);
            owner = characterOperator;
            type = attackType;
            pack = attackPack;

            if (usePreAttackStat && owner != null)
            {
                useRecordedBrain = true;
                ownerBrain = owner.brain.Clone();
            }
        }
        
        public void Terminate ()
        {
            if (!string.IsNullOrEmpty(reid))
                ReUniqueId.ReturnId(reid);
            ReleaseOwner();
            lastExecuteResult?.ReleaseReverse();
            lastExecuteResult = null;
            pack = null;
            owner = null;
            target = null;
            ownerBrain = null;
        }

        public void Calculate (CharacterOperator character)
        {
            if (pack == null || character == null)
            {
                ReDebug.LogWarning("Attack Damage Data Warning", "Calculate activation being ignored due to missing require params");
                return;
            }

            target = character;
            pack.CalculateDamage(this);
        }
        
        public float GetDamage ()
        {
            if (lastExecuteResult != null)
            {
                var value = lastExecuteResult.variables.GetNumber(reid, float.PositiveInfinity, int.MaxValue, GraphVariables.PREFIX_RETURN);
                if (!float.IsPositiveInfinity(value))
                    return value;
            }

            return 0;
        }

        public bool isMissed => lastExecuteResult != null ? lastExecuteResult.variables.GetInt(reid, int.MaxValue, GraphVariables.PREFIX_RETURN) == (int) ReturnBehaviourNode.ExecutionType.ReturnMiss : manualMissed;
        public bool isDodged => lastExecuteResult?.variables.GetInt(reid, int.MaxValue, GraphVariables.PREFIX_RETURN) == (int)ReturnBehaviourNode.ExecutionType.ReturnDodge;
        public bool isBlocked => lastExecuteResult?.variables.GetInt(reid, int.MaxValue, GraphVariables.PREFIX_RETURN) == (int)ReturnBehaviourNode.ExecutionType.ReturnBlock;
        public bool isBackstab => lastExecuteResult?.variables.GetInt(reid, int.MaxValue, GraphVariables.PREFIX_RETURN) == (int)ReturnBehaviourNode.ExecutionType.ReturnBackstab;
        public bool isCritical => lastExecuteResult?.variables.GetInt(reid, int.MaxValue, GraphVariables.PREFIX_RETURN) == (int)ReturnBehaviourNode.ExecutionType.ReturnCritical;
        public bool isParry => lastExecuteResult?.variables.GetInt(reid, int.MaxValue, GraphVariables.PREFIX_RETURN) == (int)ReturnBehaviourNode.ExecutionType.ReturnParry;
        public bool isAttackEffect => type == CharacterOperator.AttackType.EffectNormal;
        public bool isAttackMelee => type == CharacterOperator.AttackType.MeleeNormal;
        public bool isAttackRanged => type == CharacterOperator.AttackType.RangedNormal;

        public void SetMissed ()
        {
            manualMissed = true;
        }
        
        public void SetTarget (CharacterOperator character)
        {
            target = character;
        }
        
        public void SetImpairedHurt (float impaired)
        {
            impairedHurt = impaired;
        }

        public void SetImpairedDamage (float impaired)
        {
            impairedDamage = impaired;
            totalDamageDeal += impairedDamage;
        }
        
        public float GetImpairedDamage ()
        {
            return impairedDamage;
        }
        
        public float GetTotalDamageDeal ()
        {
            return totalDamageDeal;
        }
        
        public float GetImpairedHurt ()
        {
            return impairedHurt;
        }

        public bool isDeadAtAttack => impairedDamage > 0 && target.die;

        public void DetectTargetDead ()
        {
            if (target != null)
            {
                if (isDeadAtAttack)
                {
                    if (owner != null)
                        owner.AdmitAttackKill(this);
                    var friends = target.GetFriendlyUnit();
                    for (var i = 0; i < friends.Count; i++)
                        friends[i].AdmitFriendlyLoss(this);
                }
            }
        }
        
        public void DetectBackstabAttack ()
        {
            if (isBackstab)
            {
                if (owner != null)
                    owner.AdmitAttackBackstab(this);
                if (target != null)
                    target.AdmitGetBackstab(this);
            }
        }
        
        public void DetectParryAttack ()
        {
            if (isParry)
            {
                if (owner != null)
                    owner.AdmitGetParry(this);
                if (target != null)
                    target.AdmitParry(this);
            }
        }
        
        private void ReleaseOwner ()
        {
            if (owner != null)
                owner.LetGoAttackPack(this);
        }
    }
}