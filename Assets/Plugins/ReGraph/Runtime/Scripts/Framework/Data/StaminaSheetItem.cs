using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    [Serializable]
    public class StaminaSheetItem
    {
        [HorizontalGroup(Width = 0.5F)]
        [ValueDropdown("@Stamina.DrawTypeDropdownAvailableChoice(sheet.staminaConsume)")]
        [HideLabel]
        public Stamina.Type type;

        [HorizontalGroup]
        [InlineProperty]
        [HideLabel]
        public FloatProperty value;

        [HideInInspector]
        public StaminaPack pack;

        public StaminaSheetItem (StaminaPack s)
        {
            pack = s;
        }
    }
}