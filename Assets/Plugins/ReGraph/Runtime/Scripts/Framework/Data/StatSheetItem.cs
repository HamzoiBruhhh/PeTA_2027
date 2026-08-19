using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    [Serializable]
    public class StatSheetItem
    {
        [HorizontalGroup(Width = 0.6F)]
        [ValueDropdown("@StatType.DrawStatNameListDropdown()", DropdownWidth = 250, AppendNextDrawer = true)]
        [HideLabel]
        [DisplayAsString]
        [SuffixLabel("$DisplaySumValue")]
        public string type;

        [HorizontalGroup]
        [InlineProperty]
        [HideLabel]
        public FloatProperty value;

        private string id;
        private bool addedAfter;

        public string Id => id;
        public bool AddedAfter => addedAfter;

        public StatSheetItem (string t, float v)
        {
            type = t;
            value = new FloatProperty(v);
        }
        
        public StatSheetItem (string d, string t, float v, bool a)
        {
            id = d;
            type = t;
            value = new FloatProperty(v);
            addedAfter = a;
        }
        
#if UNITY_EDITOR
        private string DisplaySumValue ()
        {
            float sum = value;
            if (cs != null)
            {
                if (group == 1)
                {
                    sum += cs.GetBaseStatValue(type);
                    return $"({sum})";
                }
                else if (group == 2)
                {
                    sum += cs.GetBaseStatMod(type);
                    return $"({sum})";
                }
                else if (group == 3)
                {
                    sum += cs.GetBaseStatMtp(type);
                    return $"({sum})";
                }
                else if (group == 4)
                {
                    sum += cs.GetBaseStatMag(type);
                    return $"({sum})";
                }
            }

            return string.Empty;
        }

        [HideInInspector]
        public StatSheet cs;
        
        [HideInInspector]
        public int group;
#endif
    }
}