using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    public static class Morale
    {
        public enum EventType
        {
            None,
            KillOpponent = 101,
            LaunchBackstab = 201,
            FriendlyVicinity = 1001,
            FriendDead = 10101,
            ReceiveBackstab = 10201,
            HostileVicinity = 11001,
        }

#if UNITY_EDITOR
        private static IEnumerable DrawTypeChoiceDropdown (List<MoraleEventItem> list)
        {
            var listDropdown = new ValueDropdownList<EventType>();
            foreach(int i in Enum.GetValues(typeof(EventType)))
            {
                var name = Enum.GetName(typeof(EventType), i);
                listDropdown = AddTypeChoice(listDropdown, list, name, Enum.Parse<EventType>(name));
            }
            
            return listDropdown;
        }

        private static ValueDropdownList<EventType> AddTypeChoice (ValueDropdownList<EventType> dropdown, List<MoraleEventItem> list, string name, EventType eventType)
        {
            var found = false;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].eventType == eventType)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                dropdown.Add(name, eventType);
            return dropdown;
        }
#endif
    }
}