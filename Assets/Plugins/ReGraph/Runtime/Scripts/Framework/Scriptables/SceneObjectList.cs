using System;
using System.Collections;
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
    [CreateAssetMenu(menuName = "Reshape/Scene Object List", order = 101)]
    [Serializable]
    public class SceneObjectList : VariableList
    {
        [DisableIf("DisableType")]
        [ValueDropdown("TypeChoice")]
        public SceneObject.ObjectType type;

        [OnInspectorGUI("BeforeDrawObject", "AfterDrawObject")]
        [ListDrawerSettings(DraggableItems = false, Expanded = true)]
        public List<SceneObjectProperty> objects;

        public void RemoveObject (List<SceneObjectProperty> list)
        {
            if (list == null || objects == null) return;
            for (var i = 0; i < list.Count; i++)
                RemoveObject(list[i]);
        }

        public bool RemoveObject (SceneObjectProperty obj)
        {
            if (objects == null || obj == null) return false;
            for (var i = 0; i < objects.Count; i++)
            {
                if (objects[i].Equals(obj))
                {
                    objects.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool RemoveObject (SceneObjectVariable obj)
        {
            if (objects == null || obj == null) return false;
            for (int i = 0; i < objects.Count; i++)
            {
                SceneObject so = objects[i];
                if (so.Equals(obj.sceneObject))
                {
                    objects.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public override bool IsNoneType ()
        {
            if (type == SceneObject.ObjectType.None)
                return true;
            return false;
        }
        
        public void InsertObject (SceneObjectProperty addon)
        {
            objects = InsertObject(objects, addon);
        }
        
        public void InsertObject (List<SceneObjectProperty> addon)
        {
            objects = InsertObject(objects, addon);
        }
        
        public void InsertValue (SceneObjectProperty addon)
        {
            objects = InsertValue(objects, addon);
        }

        public void PushObject (List<SceneObjectProperty> addon)
        {
            objects = PushObject(objects, addon);
        }

        public SceneObjectProperty TakeObject ()
        {
            return TakeObject(objects);
        }

        public SceneObjectProperty PopObject ()
        {
            return PopObject(objects);
        }

        public SceneObjectProperty RandomObject ()
        {
            return RandomObject(objects);
        }

        public void ClearObject ()
        {
            objects = ClearObject(objects);
        }

        public int GetCount ()
        {
            return GetCount(objects);
        }
        
        public SceneObjectProperty GetByIndex (int index)
        {
            return GetByIndex(objects, index);
        }
        
        public SceneObjectProperty GetById (int id)
        {
            return GetById(objects, id);
        }

#if UNITY_EDITOR
        private bool DisableType ()
        {
            if (Application.isPlaying)
                return true;
            return type != SceneObject.ObjectType.None && objects.Count > 0;
        }
        
        private List<SceneObjectProperty> originObjects;
        
        private void KeepOrigin ()
        {
            if (objects != null)
            {
                originObjects = new List<SceneObjectProperty>();
                for (var i = 0; i < objects.Count; i++)
                    originObjects.Add(objects[i].ShallowCopy());
            }
        }
        
        private void RestoreOrigin ()
        {
            if (originObjects != null)
            {
                objects = new List<SceneObjectProperty>();
                for (var i = 0; i < originObjects.Count; i++)
                    objects.Add(originObjects[i].ShallowCopy());
                originObjects = null;
            }
        }
        
        private static IEnumerable TypeChoice ()
        {
            var list = SceneObject.GetObjectTypeChoice;
            var clone = new ValueDropdownList<SceneObject.ObjectType>();
            foreach (var item in list)
                clone.Add(item.Text, item.Value); 
            clone.Add("Material", SceneObject.ObjectType.Material);
            return clone;
        }

        private bool IsClearAfterPlayMode ()
        {
            if (type is SceneObject.ObjectType.Material or SceneObject.ObjectType.Sprite or SceneObject.ObjectType.AttackStatusPack)
                return false;
            return true;
        }

        private void BeforeDrawObject ()
        {
            if (type is SceneObject.ObjectType.Material or SceneObject.ObjectType.ItemData or SceneObject.ObjectType.AttackStatusPack or SceneObject.ObjectType.Sprite)
            {
                for (var i = 0; i < objects.Count; i++)
                {
                    if (type == SceneObject.ObjectType.Material && !objects[i].objectValue.IsMaterial())
                        objects[i].objectValue.type = SceneObject.ObjectType.Material;
                    else if (type == SceneObject.ObjectType.ItemData && !objects[i].objectValue.IsItemData())
                        objects[i].objectValue.type = SceneObject.ObjectType.ItemData;
                    else if (type == SceneObject.ObjectType.AttackStatusPack && !objects[i].objectValue.IsAttackStatusPack())
                        objects[i].objectValue.type = SceneObject.ObjectType.AttackStatusPack;
                    else if (type == SceneObject.ObjectType.Sprite && !objects[i].objectValue.IsSprite())
                        objects[i].objectValue.type = SceneObject.ObjectType.Sprite;
                    objects[i].type = 3;
                    objects[i].objectValue.showAsNodeProperty = true;
                    objects[i].objectValue.showType = false;
                }
            }
            else
            {
                GUI.enabled = false;
            }
        }
        
        private void AfterDrawObject ()
        {
            GUI.enabled = true;
        }

        public static void OpenCreateMenu (SceneObjectList variable)
        {
            OpenCreateMenu(variable, false);
        }

        public static void OpenCreateMenu (SceneObjectList variable, bool withMaterial)
        {
            var menu = new GenericMenu();
            IEnumerable objectChoices = SceneObject.GetObjectTypeChoice;
            var choices = (ValueDropdownList<SceneObject.ObjectType>) objectChoices;
            foreach (ValueDropdownItem<SceneObject.ObjectType> choice in choices)
                menu.AddItem(new GUIContent(choice.Text), false, CreateSceneObjectList, choice.Value);
            if (withMaterial)
                menu.AddItem(new GUIContent("Material"), false, CreateSceneObjectList, SceneObject.ObjectType.Material);
            menu.ShowAsContext();

            void CreateSceneObjectList (object varObj)
            {
                SceneObject.ObjectType type = (SceneObject.ObjectType) varObj;
                if (type != SceneObject.ObjectType.None)
                {
                    var created = CreateNew(variable, type);
                    if (created != null && created != variable)
                        SetGraphEditorVariable(created);
                }
            }
        }

        private static void SetGraphEditorVariable (SceneObjectList created)
        {
            GraphEditorVariable.SetString(Selection.activeObject.GetInstanceID().ToString(), "createVariable", AssetDatabase.GetAssetPath(created));
        }

        public static SceneObjectList CreateNew (SceneObjectList variable, SceneObject.ObjectType type)
        {
            if (variable == null)
                return CreateNew(type);

            bool proceed = EditorUtility.DisplayDialog("Graph Variable", "Are you sure you want to create a new variable to replace the existing assigned variable ?", "OK", "Cancel");
            if (proceed)
            {
                var list = CreateNew(type);
                if (list != null)
                    return list;
            }

            return variable;
        }

        public static SceneObjectList CreateNew (SceneObject.ObjectType type)
        {
            var path = EditorUtility.SaveFilePanelInProject("Graph Variable", "New Scene Object List", "asset", "Select a location to create variable list");
            if (path.Length == 0)
                return null;
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                return null;

            SceneObjectList list = ScriptableObject.CreateInstance<SceneObjectList>();
            list.type = type;
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
                    var guids = AssetDatabase.FindAssets("t:SceneObjectList");
                    if (guids.Length > 0)
                    {
                        for (var i = 0; i < guids.Length; i++)
                        {
                            var list = (SceneObjectList) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(UnityEngine.Object));
                            if (list != null)
                            {
                                if (exit)
                                {
                                    if (list.IsClearAfterPlayMode())
                                        list.ClearObject();
                                    else
                                        list.RestoreOrigin();
                                }
                                else
                                {
                                    if (!list.IsClearAfterPlayMode())
                                        list.KeepOrigin();
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