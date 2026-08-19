namespace Reshape.ReFramework
{
    public partial class CharacterMuscle
    {
        private float cooldownTimer;

        public float attackCooldown => cooldownTimer;

        private void InitCooldown ()
        {
            cooldownTimer = float.MaxValue;
        }
        
        private void ResetCooldownTimer ()
        {
            cooldownTimer = 0;
        }
        
        private void MaxCooldownTimer ()
        {
            if (cooldownResetOnActions)
                cooldownTimer = float.MaxValue;
        }

        private void UpdateCooldown (float deltaTime)
        {
            if (cooldownTimer + deltaTime < float.MaxValue)
                cooldownTimer += deltaTime;
        }
        
        private bool CheckCooldown (float duration)
        {
            if (cooldownTimer >= duration)
                return true;
            return false;
        }
    }
}