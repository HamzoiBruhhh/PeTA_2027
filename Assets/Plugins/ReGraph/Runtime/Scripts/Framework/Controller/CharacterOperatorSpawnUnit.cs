using System;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class CharacterOperatorSpawnUnit : BaseScriptable
    {
        [Hint("showHints", "Define the model prefab use by the unit.")]
        public GameObject prefab;

        [Hint("showHints", "Define the brain behaviour of the unit.\nBrain contains all decision making related behaviour.")]
        [InlineButton("CreateNewCharacterBrain", "✚")]
        public CharacterBrain brain;

        [Hint("showHints", "Define the muscle behaviour of the unit.\nMuscle contains all attack related behaviour.")]
        [InlineButton("CreateNewCharacterMuscle", "✚")]
        public CharacterMuscle muscle;

        [Hint("showHints", "Define the motor behaviour of the unit.\nMotor contains all movement related behaviour.")]
        [InlineButton("CreateNewCharacterMotor", "✚")]
        public CharacterMotor motor;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------
        
        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public bool isValid
        {
            get
            {
                if (prefab != null)
                    return true;
                return false;
            }
        }
        
        public virtual void Set (CharacterOperatorSpawnUnit spawnUnit)
        {
#if UNITY_EDITOR
            Keep();
#endif
            if (spawnUnit != null)
            {
                prefab = spawnUnit.prefab;
                brain = spawnUnit.brain;
                muscle = spawnUnit.muscle;
                motor = spawnUnit.motor;
            }
            else
            {
                prefab = null;
                brain = null;
                muscle = null;
                motor = null;
            }
        }
        
        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------
        
        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- BaseScriptable methods
        //-----------------------------------------------------------------
        
        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
#if UNITY_EDITOR
        [HideInInspector]
        public bool changed;

        [HideInInspector]
        public CharacterOperatorSpawnUnit origin;

        public virtual void Keep ()
        {
            if (!changed)
            {
                changed = true;
                origin = Instantiate(this);
            }
        }

        public virtual void Reset ()
        {
            if (changed)
            {
                prefab = origin.prefab;
                brain = origin.brain;
                muscle = origin.muscle;
                motor = origin.motor;
                changed = false;
                origin = null;
            }
        }

        [InitializeOnLoad]
        public static class SceneObjectListResetOnPlay
        {
            static SceneObjectListResetOnPlay ()
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }

            private static void OnPlayModeChanged (PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        var guids = AssetDatabase.FindAssets("t:LevelBattleSpawnUnit");
                        if (guids.Length > 0)
                        {
                            for (int i = 0; i < guids.Length; i++)
                            {
                                var list = (CharacterOperatorSpawnUnit) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(UnityEngine.Object));
                                if (list != null)
                                    list.Reset();
                            }

                            AssetDatabase.SaveAssets();
                        }
                    }
                }
            }
        }
        
        private void CreateNewCharacterBrain ()
        {
            var created = CharacterBrain.CreateNew();
            if (created != null)
                brain = created;
        }
        
        private void CreateNewCharacterMuscle ()
        {
            var created = CharacterMuscle.CreateNew();
            if (created != null)
                muscle = created;
        }
        
        private void CreateNewCharacterMotor ()
        {
            var created = CharacterMotor.CreateNew();
            if (created != null)
                motor = created;
        }
#endif
    }
}