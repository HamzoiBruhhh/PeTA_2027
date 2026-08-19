using System;
using System.Collections.Generic;
using Reshape.Unity;
using UnityEngine;
using Sirenix.OdinInspector;
#if REGRAPH_FOW
using FoW;
#endif

namespace Reshape.ReFramework
{
    public class FogOfWarManager : BaseBehaviour
    {
        private static FogOfWarManager me;

        [Hint("showHints", "A variable to store the FOW is enable.")]
        public NumberVariable enabledVariable;
        [Hint("showHints", "Enable/Disable all fog agent events.")]
        [LabelText("Agent Events")]
        public bool fogAgentHidden = true;
        [LabelText("Team Update Rate")]
        [Hint("showHints", "Value <= 0 means not apply custom update rate, update rate control automatically by FOW Team.\nValue > 0 means apply custom update rate by second.")]
        [SerializeField]
        [DisableInPlayMode]
        private float fogTeamUpdateRate = -1;
#if REGRAPH_FOW
        [ShowIf("@fogTeamUpdateRate > 0")]
        [LabelText("Team")]
        [Indent(1)]
        public FogOfWarTeam[] fogTeamCustomUpdate;
#endif
        [Hint("showHints", "Define player's fog team.")]
        public int fovTeam;
        [Hint("showHints", "Define materials use by player's fog team.")]
        public Material[] fovMaterials;

        private float customUpdateTimer;

        public static bool agentFogEvents => me == null || me.fogAgentHidden;
        
        public static void ApplyTeamFovMaterial ()
        {
            me.AssignTeamFovToMaterial();
        }

        public void AssignTeamFovToMaterial ()
        {
#if REGRAPH_FOW
            var fowTeam = FogOfWarTeam.GetTeam(fovTeam);
            if (fowTeam != null)
                for (var i = 0; i < fovMaterials.Length; i++)
                    fowTeam.ApplyToMaterial(fovMaterials[i]);
#endif
        }

        public override void PostBegin ()
        {
#if REGRAPH_FOW
            for (var i = 0; i < fogTeamCustomUpdate.Length; i++)
            {
                if (fogTeamCustomUpdate[i] != null)
                    fogTeamCustomUpdate[i].updateAutomatically = false;
            }
#endif
            
            DonePostBegin();
            PlanTick();
        }

        protected void Awake ()
        {
            me = this;
            if (fogTeamUpdateRate > 0)
                PlanPostBegin();
            enabledVariable.SetValue(1);
        }
        
        protected void OnDestroy ()
        {
            me = null;
            OmitTick();
            enabledVariable.SetValue(0);
        }
        
        public override void Tick ()
        {
            customUpdateTimer += ReTime.deltaTime;
            if (customUpdateTimer >= fogTeamUpdateRate)
            {
#if REGRAPH_FOW
                for (var i = 0; i < fogTeamCustomUpdate.Length; i++)
                {
                    if (fogTeamCustomUpdate[i] != null)
                        fogTeamCustomUpdate[i].ManualUpdate(customUpdateTimer);
                }
#endif

                customUpdateTimer = 0;
            }
        }
    }
}