using Reshape.ReGraph;
using Reshape.Unity;

namespace Reshape.ReFramework
{
    public class TargetAimData
    {
        public GraphExecution lastExecuteResult;

        private string reid;
        private TargetAimPack aimPack;
        private AttackDamageData damageData;
        private CharacterOperator owner;

        public string id => reid;
        public CharacterOperator attacker => damageData == null ? owner : damageData.attacker;
        public CharacterOperator defender => damageData?.defender;

        public void Init (CharacterOperator characterOperator, TargetAimPack pack)
        {
            reid = ReUniqueId.GenerateId(false);
            owner = characterOperator;
            aimPack = pack;
        }
        
        public void Init (AttackDamageData damage, TargetAimPack pack)
        {
            reid = ReUniqueId.GenerateId(false);
            damageData = damage;
            aimPack = pack;
        }
        
        public void Terminate ()
        {
            ReUniqueId.ReturnId(reid);
            lastExecuteResult = null;
            aimPack = null;
            damageData = null;
            owner = null;
        }

        public void Choose ()
        {
            if (owner == null || aimPack == null)
            {
                ReDebug.LogWarning("Target Aim Data Warning", "Choose activation being ignored due to missing require params");
                return;
            }
            
            aimPack.ChooseTarget(this);
        }
        
        public void ReactHurt ()
        {
            if (damageData == null || aimPack == null)
            {
                ReDebug.LogWarning("Target Aim Data Warning", "React Hurt activation being ignored due to missing require params");
                return;
            }
            
            aimPack.ChooseHurtTarget(this);
        }
        
        public CharacterOperator GetTarget ()
        {
            var value = lastExecuteResult.variables.GetCharacter(reid, null, GraphVariables.PREFIX_RETURN);
            return value;
        }
    }
}