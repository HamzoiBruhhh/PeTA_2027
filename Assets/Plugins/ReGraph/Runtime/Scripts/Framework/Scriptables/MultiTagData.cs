using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    [Serializable]
    public class MultiTagData : ScriptableObject
    {
        [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, ShowPaging = false, Expanded = true, DraggableItems = false)]
        public string[] tags = new string[32];
    }
}