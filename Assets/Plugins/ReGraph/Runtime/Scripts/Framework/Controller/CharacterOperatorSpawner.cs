using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using Reshape.Unity;
using UnityEngine;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class CharacterOperatorSpawner : BaseBehaviour
    {
        [Hint("showHints", "Define using which CharacterOperatorSpawnUnit to spawn the enemy.")]
        public CharacterOperatorSpawnUnit unit;
        
        [Hint("showHints", "Define the spawned unit's character operator saved into which variable.\nThis is optional.")]
        public SceneObjectVariable variable;
        
        [Hint("showHints", "Define the spawned unit's custom spawn ID to be use for searching.\nThis is optional. System will do nothing if value is 0.")]
        [InlineProperty]
        [LabelText("Spawn ID")]
        public FloatProperty customId;
        
        [Hint("showHints", "Define the unit spawn as a child gameObject of the defined parent.")]
        public Transform parent;
        
        [Hint("showHints", "Tick to make the spawner spawn the unit after the scene have loaded.")]
        public bool onStart;
        
        [Hint("showHints", "Tick to make the spawner spawn the unit in random rotation.")]
        public bool randomFacing;
        
        [Hint("showHints", "Tick to make the spawner spawn the unit with appear animation.")]
        public bool appearAnim;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public void Activate ()
        {
            if (isActiveAndEnabled)
                Spawn();
        }

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected void Awake ()
        {
            if (onStart)
                PlanPreBegin();
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        [SpecialName]
        public override void PreBegin ()
        {
            Spawn();
            DonePreBegin();
        }

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        protected virtual CharacterOperator Initialize (GameObject go)
        {
            go.TryGetComponent<CharacterOperator>(out var co);
            if (co)
                co.Initialize(unit.brain, unit.muscle, unit.motor, appearAnim, customId);
            return co;
        }
        
        protected virtual void Spawn ()
        {
            if (unit != null && unit.isValid)
            {
                var facePos = transform.position + transform.forward;
                if (randomFacing)
                {
                    facePos = transform.position;
                    facePos.x = ReRandom.Range(0f, 10f);
                    facePos.z = ReRandom.Range(0f, 10f);
                }
                
                var go = Instantiate(unit.prefab, transform.position, Quaternion.identity);
                if (go != null)
                {
                    go.name = gameObject.name + ReExtensions.STRING_UNDERSCORE + unit.prefab.name;
                    var co = Initialize(go);
                    if (co != null)
                    {
                        if (variable != null)
                            variable.SetValue(co);
                        co.Face(facePos);
                        if (parent)
                            go.transform.parent = parent;
                    }
                }
            }
        }

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}