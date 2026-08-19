using System;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Reshape.Unity.Editor;
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Character Stat", fileName = "CharacterStat", order = 302)]
    [HideMonoScript]
    [Serializable]
    public partial class CharacterStat : BaseScriptable
    {
        [Hint("showHints", "Define this stat sheet that use by this unit.")]
        [PropertyOrder(-99)]
        [InlineButton("CreateNewStatSheet", "✚")]
        public StatSheet born;

        [ShowInInspector]
        [HideInEditorMode]
        [InlineEditor(InlineEditorModes.GUIOnly, InlineEditorObjectFieldModes.Foldout, Expanded = false)]
        private StatSheet current;

        private CharacterOperator owner;

        public float GetValue (string statName)
        {
            return current.GetValue(statName);
        }

        public float GetMtp (string statName)
        {
            return current.GetMtp(statName);
        }

        public float GetMod (string statName)
        {
            return current.GetMod(statName);
        }
        
        public float GetMag (string statName)
        {
            return current.GetMag(statName);
        }
        
        public void AddValue (string id, string statName, float value)
        {
            current.AddValue(id, statName, value);
            RefreshLiveStat(statName);
        }

        public void AddMtp (string id, string statName, float mtp)
        {
            current.AddMtp(id, statName, mtp);
            RefreshLiveStat(statName);
        }

        public void AddMod (string id, string statName, float mod)
        {
            current.AddMod(id, statName, mod);
            RefreshLiveStat(statName);
        }
        
        public void AddMag (string id, string statName, float mag)
        {
            current.AddMag(id, statName, mag);
            RefreshLiveStat(statName);
        }
        
        public bool RemoveValue (string id, string statName, float value)
        {
            if (current.RemoveValue(id, statName, value))
            {
                RefreshLiveStat(statName);
                return true;
            }

            return false;
        }

        public bool RemoveMtp (string id, string statName, float mtp)
        {
            if (current.RemoveMtp(id, statName, mtp))
            {
                RefreshLiveStat(statName);
                return true;
            }

            return false;
        }

        public bool RemoveMod (string id, string statName, float mod)
        {
            if (current.RemoveMod(id, statName, mod))
            {
                RefreshLiveStat(statName);
                return true;
            }

            return false;
        }
        
        public bool RemoveMag (string id, string statName, float mag)
        {
            if (current.RemoveMag(id, statName, mag))
            {
                RefreshLiveStat(statName);
                return true;
            }

            return false;
        }

        public void Init (CharacterOperator charOp)
        {
            current = Instantiate(born);
            owner = charOp;
            InitHealth();
            InitStamina();
            InitMorale();
        }

        public void Tick (float deltaTime)
        {
            UpdateStaminaStat(deltaTime);
            UpdateMoraleStat(deltaTime);
        }

        public void OnDestroy ()
        {
            Destroy(current);
        }

        public CharacterStat Clone ()
        {
            var stat = Instantiate(this);
            if (current != null)
                stat.current = Instantiate(current);
            return stat;
        }

        public void RefreshLiveStat (string statName)
        {
            RefreshHealthStat(statName);
            RefreshStaminaStat(statName);
            RefreshMoraleStat(statName);
        }

        private bool HaveStatValueChanged (string changedStat, string statType, float currentValue, out float updatedValue)
        {
            if (changedStat.Equals(statType))
            {
                var newValue = owner.GetStatValue(statType);
                if (Math.Abs(newValue - currentValue) > TOLERANCE)
                {
                    updatedValue = newValue;
                    return true;
                }
            }

            updatedValue = 0;
            return false;
        }
        
#if UNITY_EDITOR
        public static CharacterStat CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Character Stat", "New Character Stat", "asset", "Select a location to create character stat");
            return path.Length == 0 ? null : ReEditorHelper.CreateScriptableObject<CharacterStat>(null, false, false, string.Empty, path);
        }
        
        private void CreateNewStatSheet ()
        {
            var created = StatSheet.CreateNew();
            if (created != null)
                born = created;
        }
#endif
    }
}