using Reshape.ReGraph;
using Reshape.Unity;

namespace Reshape.ReFramework
{
    public class StaminaData
    {
        public GraphExecution lastExecuteResult;

        private StaminaPack staminaPack;

        public Stamina.Type staminaType { get; private set; }
        public string id { get; private set; }
        public CharacterOperator owner { get; private set; }

        public void Init (CharacterOperator characterOperator, StaminaPack pack, Stamina.Type type)
        {
            id = ReUniqueId.GenerateId(false);
            owner = characterOperator;
            staminaPack = pack;
            staminaType = type;
        }

        public void Terminate ()
        {
            ReUniqueId.ReturnId(id);
            lastExecuteResult = null;
            staminaPack = null;
            owner = null;
        }

        public void TriggerConsume ()
        {
            if (!owner || !staminaPack)
            {
                ReDebug.LogWarning("Stamina Data Warning", "TriggerConsume activation being ignored due to missing require params");
                return;
            }

            staminaPack.TriggerConsume(this);
        }
    }
}