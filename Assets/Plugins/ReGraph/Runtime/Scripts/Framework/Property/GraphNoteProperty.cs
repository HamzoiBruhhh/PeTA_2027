using System;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    [Serializable]
    public class GraphNoteProperty : ReProperty, IClone<GraphNoteProperty>
    {
        public string reid;
        
        public GraphNoteProperty ()
        {
            reid = ReUniqueId.GenerateId();
        }
        
        public GraphNoteProperty ShallowCopy ()
        {
            var cloned = (GraphNoteProperty) this.MemberwiseClone();
            cloned.reid = ReUniqueId.GenerateId();
            return cloned;
        }
        
        public static implicit operator string (GraphNoteProperty n)
        {
            return n.ToString();
        }
    }
}