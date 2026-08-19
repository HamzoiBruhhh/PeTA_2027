using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReGraph;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using Reshape.Unity.Editor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Word List", order = 102)]
    [Serializable]
    public class WordList : VariableList
    {
        [ListDrawerSettings(DraggableItems = false, Expanded = true)]
        public List<StringProperty> words;
        
        public void RemoveObject (List<StringProperty> list)
        {
            if (list == null || words == null) return;
            for (var i = 0; i < list.Count; i++)
                RemoveObject(list[i]);
        }

        public bool RemoveObject (StringProperty obj)
        {
            if (words == null || obj == null) return false;
            for (var i = 0; i < words.Count; i++)
            {
                if (words[i].Equals(obj))
                {
                    words.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool RemoveObject (WordVariable obj)
        {
            if (words == null || obj == null) return false;
            for (var i = 0; i < words.Count; i++)
            {
                if (words[i].Equals(obj))
                {
                    words.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public override bool IsNoneType ()
        {
            return false;
        }
        
        public void InsertObject (StringProperty addon)
        {
            words = InsertObject(words, addon);
        }
        
        public void InsertObject (List<StringProperty> addon)
        {
            words = InsertObject(words, addon);
        }
        
        public void InsertValue (StringProperty addon)
        {
            words = InsertValue(words, addon);
        }

        public void PushObject (List<StringProperty> addon)
        {
            words = PushObject(words, addon);
        }

        public StringProperty TakeObject ()
        {
            return TakeObject(words);
        }

        public StringProperty PopObject ()
        {
            return PopObject(words);
        }

        public StringProperty RandomObject ()
        {
            return RandomObject(words);
        }

        public void ClearObject ()
        {
            words = ClearObject(words);
        }

        public int GetCount ()
        {
            return GetCount(words);
        }
        
        public StringProperty GetByIndex (int index)
        {
            return GetByIndex(words, index);
        }
        
        public StringProperty GetById (int id)
        {
            return GetById(words, id);
        }

#if UNITY_EDITOR
        private List<StringProperty> originWords;
        
        private void KeepOrigin ()
        {
            if (words != null)
            {
                originWords = new List<StringProperty>();
                for (var i = 0; i < words.Count; i++)
                    originWords.Add(words[i].ShallowCopy());
            }
        }
        
        private void RestoreOrigin ()
        {
            if (originWords != null)
            {
                words = new List<StringProperty>();
                for (var i = 0; i < originWords.Count; i++)
                    words.Add(originWords[i].ShallowCopy());
                originWords = null;
            }
        }
        
        public static void OpenCreateMenu (WordList wList)
        {
            var created = CreateNew(wList);
            if (created != null && created != wList)
                SetGraphEditorVariable(wList);
        }

        private static void SetGraphEditorVariable (WordList created)
        {
            GraphEditorVariable.SetString(Selection.activeObject.GetInstanceID().ToString(), "createVariable", AssetDatabase.GetAssetPath(created));
        }

        public static WordList CreateNew (WordList variable)
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

        public static WordList CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Graph Variable", "New Word List", "asset", "Select a location to create variable list");
            if (path.Length == 0)
                return null;
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                return null;

            var list = ScriptableObject.CreateInstance<WordList>();
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
                    var guids = AssetDatabase.FindAssets("t:WordList");
                    if (guids.Length > 0)
                    {
                        for (var i = 0; i < guids.Length; i++)
                        {
                            var list = (WordList) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(UnityEngine.Object));
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