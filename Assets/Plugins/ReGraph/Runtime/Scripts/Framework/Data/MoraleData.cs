using Reshape.ReGraph;
using Reshape.Unity;

namespace Reshape.ReFramework
{
    public class MoraleData
    {
        public GraphExecution lastExecuteResult;

        private MoralePack moralePack;

        public string id { get; private set; }
        public CharacterOperator owner { get; private set; }

        public void Init (CharacterOperator characterOperator, MoralePack pack)
        {
            id = ReUniqueId.GenerateId(false);
            owner = characterOperator;
            moralePack = pack;
        }

        public void Terminate ()
        {
            ReUniqueId.ReturnId(id);
            lastExecuteResult?.ReleaseReverse();
            lastExecuteResult = null;
            moralePack = null;
            owner = null;
        }

        public void TriggerChanges ()
        {
            if (!owner || !moralePack)
            {
                ReDebug.LogWarning("Morale Data Warning", "TriggerChanges activation being ignored due to missing require params");
                return;
            }

            moralePack.TriggerValueChanged(this);
        }
    }
}