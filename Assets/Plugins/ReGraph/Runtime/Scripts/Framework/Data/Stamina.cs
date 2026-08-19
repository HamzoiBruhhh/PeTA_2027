using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    public static class Stamina
    {
        public enum Type
        {
            None,
            MeleeAttack = 100,
            RangedAttack = 200,
            BackstabAttack = 500,
            DodgeAttack = 1000,
            ParryAttack = 1100,
            BlockAttack = 1200,
            GetHurt = 5000,
            GetBackstab = 5500,
        }

#if UNITY_EDITOR
        private static IEnumerable DrawTypeChoiceDropdown ()
        {
            var listDropdown = new ValueDropdownList<Type>
            {
                {"Melee Attack", Type.MeleeAttack},
                {"Ranged Attack", Type.RangedAttack},
                {"Backstab Attack", Type.BackstabAttack},
                {"Dodge Attack", Type.DodgeAttack},
                {"Parry Attack", Type.ParryAttack},
                {"Block Attack", Type.BlockAttack},
                {"Get Hurt", Type.GetHurt},
                {"Get Backstab", Type.GetBackstab},
            };
            return listDropdown;
        }
        
        private static IEnumerable DrawTypeDropdownAvailableChoice (List<StaminaSheetItem> list)
        {
            var listDropdown = new ValueDropdownList<Type>();
            foreach(int i in Enum.GetValues(typeof(Type)))
            {
                var name = Enum.GetName(typeof(Type), i);
                listDropdown = AddTypeChoice(listDropdown, list, name, Enum.Parse<Type>(name));
            }
            
            return listDropdown;
        }

        private static ValueDropdownList<Type> AddTypeChoice (ValueDropdownList<Type> dropdown, List<StaminaSheetItem> list, string name, Type type)
        {
            var found = false;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].type == type)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                dropdown.Add(name, type);
            return dropdown;
        }
#endif
    }
}