using System.Collections.Generic;
using UnityEngine;
using Reshape.Unity;

namespace Reshape.ReFramework
{
    public class VariableList : ScriptableObject
    {
        public List<T> InsertObject<T> (List<T> variableList, T addon)
        {
            if (addon != null && variableList != null)
                variableList.Add(addon);
            return variableList;
        }

        public List<T> InsertObject<T> (List<T> variableList, List<T> addon)
        {
            if (addon != null && variableList != null)
                for (var i = 0; i < addon.Count; i++)
                    variableList.Add(addon[i]);
            return variableList;
        }

        public List<T> InsertValue<T> (List<T> variableList, T addon)
        {
            if (addon != null && variableList != null)
                variableList.Add(addon);
            return variableList;
        }

        public List<T> PushObject<T> (List<T> variableList, List<T> addon)
        {
            if (addon != null && variableList != null)
                for (var i = addon.Count - 1; i >= 0; i--)
                    variableList.Insert(0, addon[i]);
            return variableList;
        }

        public T TakeObject<T> (List<T> variableList) where T : ReProperty
        {
            return variableList?[^1];
        }

        public T PopObject<T> (List<T> variableList) where T : ReProperty
        {
            return variableList?[0];
        }

        public T RandomObject<T> (List<T> variableList) where T : ReProperty
        {
            if (variableList == null || variableList.Count == 0) return null;
            return variableList[ReRandom.Range(0, variableList.Count - 1)];
        }

        public List<T> ClearObject<T> (List<T> variableList)
        {
            variableList?.Clear();
            return variableList;
        }

        public int GetCount<T> (List<T> variableList)
        {
            return variableList?.Count ?? 0;
        }

        public T GetByIndex<T> (List<T> variableList, int index) where T : ReProperty
        {
            if (variableList == null || variableList.Count == 0 || index >= variableList.Count) return null;
            return variableList[index];
        }
        
        public T GetById<T> (List<T> variableList, int id) where T : ReProperty
        {
            if (variableList == null || variableList.Count == 0 || id == 0) return null;
            for (var i = 0; i < variableList.Count; i++)
                if (variableList[i].GetCustomId() == id)
                    return variableList[i];
            return null;
        }

        public virtual bool IsNoneType ()
        {
            return false;
        }
    }
}