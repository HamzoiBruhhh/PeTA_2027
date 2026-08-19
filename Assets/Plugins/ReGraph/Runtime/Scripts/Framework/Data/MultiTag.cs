using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Reshape.ReFramework
{
    /*--- Usage
        [1]
        var tag = flags.GenerateTagValue("Player","Melee","Fighter");
        Debug.Log(flags.Equals(tag));
        
        [2]
        Debug.Log(flags.Contains("Melee"));
        
        [3]
        Debug.Log(flags.Contains("Melee", "Player"));
    */
    [Serializable]
    public class MultiTag : IClone<MultiTag>
    {
        public string name;
        public Type tagType;
        public int tagRaw;
        public MultiTagData data;

        public MultiTag (string n, Type t, int defaultValue = 0)
        {
            name = n;
            tagType = t;
            tagRaw = defaultValue;
        }
        
        public static implicit operator int (MultiTag tag) => tag.tagRaw;

        public int value
        {
            get => tagRaw;
            set => tagRaw = value;
        }
        
        public MultiTag ShallowCopy()
        {
            return (MultiTag) this.MemberwiseClone();
        }
        
        public bool Equals (MultiTag tag)
        {
            return tagRaw == tag.tagRaw;
        }

        public bool Equals (int raw)
        {
            return tagRaw == raw;
        }
        
        public bool ContainAll (int tag)
        {
            return (tagRaw & tag) == tag;
        }
        
        public bool ContainAny (int checkTags, bool ifNoTag)
        {
            if (tagRaw == 0)
                return ifNoTag;
            return (tagRaw & checkTags) > 0;
        }
        
        public bool ContainAll (params string[] tagNames)
        {
            for (var i = 0; i < tagNames.Length; i++)
                if (!Contain(tagNames[i]))
                    return false;
            return true;
        }
        
        public bool Contain (string tagName)
        {
            var raw = GenerateTagValue(tagName);
            return (tagRaw & raw) != 0;
        }

        //public static extern string TagToName (int tag);
        //public static extern int NameToIndex (string tagName);
        
        public int GenerateTagValue (params string[] tagNames)
        {
            var tag = 0;
            if (tagNames != null && data != null)
            {
                var tagData = data.tags;
                for (var i=0; i < tagNames.Length; i++)
                {
                    for (var j = 0; j < tagData.Length; j++)
                    {
                        if (tagNames[i] == tagData[j])
                        {
                            if (j != -1)
                                tag |= 1 << j;
                            break;
                        }
                    }
                }
            }
            
            return tag;
        }

        public string GetSelectedString (int displayMaxNames = 32)
        {
            if (tagRaw == 0)
                return "Nothing";
            var tags = GetSelectedTags(data.tags);
            return GetSelectedString(data.tags, tags, displayMaxNames);
        }
        
        public List<bool> GetSelectedTags (string[] tagNames)
        {
            var selectedTags = new List<bool>(); 
            for (var i = 0; i < tagNames.Length; i++)
            {
                var bitVal = (int) Mathf.Pow(2, i);
                var isSelected = (tagRaw & bitVal) == bitVal;
                selectedTags.Add(isSelected);
            }

            return selectedTags;
        }

        public string GetSelectedString (string[] tagNames, List<bool> selectedTags, int maxText = 1)
        {
            var all = true;
            var none = true;
            var selected = 0;
            
            var sb = new StringBuilder();
            for (var i = 0; i < tagNames.Length; i++)
            {
                if (selectedTags[i])
                {
                    none = false;
                    selected++;
                    sb.Append($"{tagNames[i]}, ");
                }
                else
                {
                    all = false;
                }
            }
            
            var buttonText = sb.ToString();
            if (none) buttonText = "Nothing";
            else if (all) buttonText = "Everything";
            else if (selected <= maxText) buttonText = buttonText.Remove(buttonText.Length - 2);
            else if (buttonText.Length > maxText) buttonText = selected + " selected";
            return buttonText;
        }
        
        
#if UNITY_EDITOR
        [HideInInspector]
        public bool dirty;
#endif
    }
}