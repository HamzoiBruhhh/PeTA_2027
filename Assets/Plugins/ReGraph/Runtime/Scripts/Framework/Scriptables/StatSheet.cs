using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Reshape.Unity.Editor;
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Stat Sheet", fileName = "StatSheet", order = 301)]
    [HideMonoScript]
    [Serializable]
    public class StatSheet : BaseScriptable
    {
        private const float SMALLEST_STAT_VALUE = 0.0001f;

        [Hint("showHints", "Define this stat sheet link with another stat sheet as a base value.")]
        [InlineButton("CreateNewStatSheet", "✚")]
        public StatSheet baseStat;

        [ListDrawerSettings(ShowPaging = false)]
        [OnInspectorInit("OnInitStats")]
        [OnValueChanged("OnInitStats")]
        public List<StatSheetItem> stats;

        [ListDrawerSettings(ShowPaging = false)]
        [OnInspectorInit("OnInitModifiers")]
        [OnValueChanged("OnInitModifiers")]
        [HideInEditorMode]
        public List<StatSheetItem> modifiers;

        [ListDrawerSettings(ShowPaging = false)]
        [OnInspectorInit("OnInitMultipliers")]
        [OnValueChanged("OnInitMultipliers")]
        [HideInEditorMode]
        public List<StatSheetItem> multipliers;

        [ListDrawerSettings(ShowPaging = false)]
        [OnInspectorInit("OnInitMagnifiers")]
        [OnValueChanged("OnInitMagnifiers")]
        [HideInEditorMode]
        public List<StatSheetItem> magnifiers;
            
        public float GetBaseStatValue (string type)
        {
            if (baseStat != null)
                return baseStat.GetValue(type);
            return 0;
        }

        public float GetBaseStatMod (string type)
        {
            if (baseStat != null)
                return baseStat.GetMod(type);
            return 0;
        }

        public float GetBaseStatMtp (string type)
        {
            if (baseStat != null)
                return baseStat.GetMtp(type);
            return 0;
        }
        
        public float GetBaseStatMag (string type)
        {
            if (baseStat != null)
                return baseStat.GetMag(type);
            return 0;
        }

        public float GetValue (string type)
        {
            if (string.IsNullOrEmpty(type)) return 0;
            var value = GetBaseStatValue(type);
            if (stats == null) return value;
            for (var i = 0; i < stats.Count; i++)
                if (stats[i].type == type)
                    value += stats[i].value;
            return value;
        }

        public float GetMod (string type)
        {
            if (string.IsNullOrEmpty(type)) return 0;
            var value = GetBaseStatMod(type);
            if (modifiers == null) return value;
            for (var i = 0; i < modifiers.Count; i++)
                if (modifiers[i].type == type)
                    value += modifiers[i].value;
            return value;
        }

        public float GetMtp (string type)
        {
            if (string.IsNullOrEmpty(type)) return 0;
            var value = GetBaseStatMtp(type);
            if (multipliers == null) return value;
            for (var i = 0; i < multipliers.Count; i++)
                if (multipliers[i].type == type)
                    value += multipliers[i].value;
            return value;
        }
        
        public float GetMag (string type)
        {
            if (string.IsNullOrEmpty(type)) return 0;
            var value = GetBaseStatMag(type);
            if (magnifiers == null) return value;
            for (var i = 0; i < magnifiers.Count; i++)
                if (magnifiers[i].type == type)
                    value += magnifiers[i].value;
            return value;
        }
        
        public void AddValue (string id, string statName, float value)
        {
            stats?.Add(new StatSheetItem(id, statName, value, true));
        }

        public void AddMtp (string id, string statName, float mtp)
        {
            multipliers?.Add(new StatSheetItem(id, statName, mtp, true));
        }

        public void AddMod (string id, string statName, float mod)
        {
            modifiers?.Add(new StatSheetItem(id, statName, mod, true));
        }
        
        public void AddMag (string id, string statName, float mag)
        {
            magnifiers?.Add(new StatSheetItem(id, statName, mag, true));
        }
        
        public bool RemoveValue (string id, string statName, float value)
        {
            if (stats == null) return false;
            for (var i = 0; i < stats.Count; i++)
            {
                if (stats[i].type != statName) continue;
                if (!stats[i].AddedAfter) continue;
                if (!string.IsNullOrEmpty(id))
                    if (!string.Equals(stats[i].Id, id, StringComparison.InvariantCulture))
                        continue;
                if (Math.Abs(stats[i].value - value) >= SMALLEST_STAT_VALUE) continue;
                stats?.RemoveAt(i);
                return true;
            }

            return false;
        }

        public bool RemoveMtp (string id, string statName, float mtp)
        {
            if (multipliers == null) return false;
            for (var i = 0; i < multipliers.Count; i++)
            {
                if (multipliers[i].type != statName) continue;
                if (!string.IsNullOrEmpty(id))
                    if (!string.Equals(multipliers[i].Id, id, StringComparison.InvariantCulture))
                        continue;
                if (Math.Abs(multipliers[i].value - mtp) >= SMALLEST_STAT_VALUE) continue;
                multipliers?.RemoveAt(i);
                return true;
            }

            return false;
        }

        public bool RemoveMod (string id, string statName, float mod)
        {
            if (modifiers == null) return false;
            for (var i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].type != statName) continue;
                if (!string.IsNullOrEmpty(id))
                    if (!string.Equals(modifiers[i].Id, id, StringComparison.InvariantCulture))
                        continue;
                if (Math.Abs(modifiers[i].value - mod) >= SMALLEST_STAT_VALUE) continue;
                modifiers?.RemoveAt(i);
                return true;
            }

            return false;
        }
        
        public bool RemoveMag (string id, string statName, float mag)
        {
            if (magnifiers == null) return false;
            for (var i = 0; i < magnifiers.Count; i++)
            {
                if (magnifiers[i].type != statName) continue;
                if (!string.IsNullOrEmpty(id))
                    if (!string.Equals(magnifiers[i].Id, id, StringComparison.InvariantCulture))
                        continue;
                if (Math.Abs(magnifiers[i].value - mag) >= SMALLEST_STAT_VALUE) continue;
                magnifiers?.RemoveAt(i);
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnInitStats ()
        {
            if (stats == null) return;
            for (var i = 0; i < stats.Count; i++)
            {
                stats[i].cs = this;
                stats[i].group = 1;
                stats[i].type ??= string.Empty;
            }
        }

        private void OnInitModifiers ()
        {
            if (modifiers == null) return;
            for (var i = 0; i < modifiers.Count; i++)
            {
                modifiers[i].cs = this;
                modifiers[i].group = 2;
                modifiers[i].type ??= string.Empty;
            }
        }

        private void OnInitMultipliers ()
        {
            if (multipliers == null) return;
            for (var i = 0; i < multipliers.Count; i++)
            {
                multipliers[i].cs = this;
                multipliers[i].group = 3;
                multipliers[i].type ??= string.Empty;
            }
        }
        
        private void OnInitMagnifiers ()
        {
            if (magnifiers == null) return;
            for (var i = 0; i < magnifiers.Count; i++)
            {
                magnifiers[i].cs = this;
                magnifiers[i].group = 4;
                magnifiers[i].type ??= string.Empty;
            }
        }

        public static StatSheet CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Character Stat", "New Character Stat", "asset", "Select a location to create character stat");
            return path.Length == 0 ? null : ReEditorHelper.CreateScriptableObject<StatSheet>(null, false, false, string.Empty, path);
        }
        
        private void CreateNewStatSheet ()
        {
            var created = CreateNew();
            if (created != null)
                baseStat = created;
        }
#endif
    }
}