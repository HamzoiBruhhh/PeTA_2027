using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReGraph;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using Reshape.Unity.Editor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Number List", order = 103)]
    [Serializable]
    public class NumberList : VariableList
    {
        [ListDrawerSettings(DraggableItems = false, Expanded = true)]
        public List<FloatProperty> numbers;
        
        public void RemoveObject (List<FloatProperty> list)
        {
            if (list == null || numbers == null) return;
            for (var i = 0; i < list.Count; i++)
                RemoveObject(list[i]);
        }

        public bool RemoveObject (FloatProperty obj)
        {
            if (numbers == null || obj == null) return false;
            for (var i = 0; i < numbers.Count; i++)
            {
                if (numbers[i].Equals(obj))
                {
                    numbers.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool RemoveObject (NumberVariable obj)
        {
            if (numbers == null || obj == null) return false;
            for (var i = 0; i < numbers.Count; i++)
            {
                if (numbers[i].Equals(obj))
                {
                    numbers.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public override bool IsNoneType ()
        {
            return false;
        }
        
        public void InsertObject (FloatProperty addon)
        {
            numbers = InsertObject(numbers, addon);
        }
        
        public void InsertObject (List<FloatProperty> addon)
        {
            numbers = InsertObject(numbers, addon);
        }
        
        public void InsertValue (FloatProperty addon)
        {
            numbers = InsertValue(numbers, addon);
        }

        public void PushObject (List<FloatProperty> addon)
        {
            numbers = PushObject(numbers, addon);
        }

        public FloatProperty TakeObject ()
        {
            return TakeObject(numbers);
        }

        public FloatProperty PopObject ()
        {
            return PopObject(numbers);
        }

        public FloatProperty RandomObject ()
        {
            return RandomObject(numbers);
        }

        public void ClearObject ()
        {
            numbers = ClearObject(numbers);
        }

        public int GetCount ()
        {
            return GetCount(numbers);
        }
        
        public FloatProperty GetByIndex (int index)
        {
            return GetByIndex(numbers, index);
        }
        
        public FloatProperty GetById (int id)
        {
            return GetById(numbers, id);
        }

#if UNITY_EDITOR
        private List<FloatProperty> originNumbers;
        
        private void KeepOrigin ()
        {
            if (numbers != null)
            {
                originNumbers = new List<FloatProperty>();
                for (var i = 0; i < numbers.Count; i++)
                    originNumbers.Add(numbers[i].ShallowCopy());
            }
        }
        
        private void RestoreOrigin ()
        {
            if (originNumbers != null)
            {
                numbers = new List<FloatProperty>();
                for (var i = 0; i < originNumbers.Count; i++)
                    numbers.Add(originNumbers[i].ShallowCopy());
                originNumbers = null;
            }
        }
        
        public static void OpenCreateMenu (NumberList numList)
        {
            var created = CreateNew(numList);
            if (created != null && created != numList)
                SetGraphEditorVariable(numList);
        }

        private static void SetGraphEditorVariable (NumberList created)
        {
            GraphEditorVariable.SetString(Selection.activeObject.GetInstanceID().ToString(), "createVariable", AssetDatabase.GetAssetPath(created));
        }

        public static NumberList CreateNew (NumberList variable)
        {
            if (variable == null)
                return CreateNew();

            bool proceed = EditorUtility.DisplayDialog("Graph Variable", "Are you sure you want to create a new variable list to replace the existing assigned variable list ?", "OK", "Cancel");
            if (proceed)
            {
                var list = CreateNew();
                if (list != null)
                    return list;
            }

            return variable;
        }

        public static NumberList CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Graph Variable", "New Number List", "asset", "Select a location to create variable list");
            if (path.Length == 0)
                return null;
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                return null;

            var list = ScriptableObject.CreateInstance<NumberList>();
            AssetDatabase.CreateAsset(list, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return list;
        }

        [InitializeOnLoad]
        public static class SceneObjectListResetOnPlay
        {
            static SceneObjectListResetOnPlay ()
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }

            private static void OnPlayModeChanged (PlayModeStateChange state)
            {
                ReEditorHelper.HavePlayModeStateChange(state, out var enter, out var exit);
                if (enter || exit)
                {
                    var guids = AssetDatabase.FindAssets("t:NumberList");
                    if (guids.Length > 0)
                    {
                        for (var i = 0; i < guids.Length; i++)
                        {
                            var list = (NumberList) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(UnityEngine.Object));
                            if (list != null)
                            {
                                if (enter)
                                {
                                    list.KeepOrigin();
                                }
                                else
                                {
                                    list.RestoreOrigin();
                                }
                            }
                        }
                        
                        if (exit)
                            AssetDatabase.SaveAssets();
                    }
                }
            }
        }
#endif
    }
}