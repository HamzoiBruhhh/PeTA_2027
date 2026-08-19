using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace Reshape.ReFramework
{
    [Serializable]
    public class MoraleEventItem
    {
        [HorizontalGroup(Width = 0.5F)]
        [ValueDropdown("@Morale.DrawTypeChoiceDropdown(pack.moraleEvents)")]
        [HideLabel]
        public Morale.EventType eventType;

        [HorizontalGroup]
        [InlineProperty]
        [HideLabel]
        public FloatProperty value;

        [HideInInspector]
        public MoralePack pack;

        public MoraleEventItem (MoralePack s)
        {
            pack = s;
        }
    }
}