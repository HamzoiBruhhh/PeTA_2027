using System;
using System.Collections.Generic;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;

namespace Reshape.ReFramework
{
    public class ItemExplorer : OdinEditorWindow
    {
        [PropertyOrder(-10)]
        [InlineEditor(InlineEditorModes.GUIOnly, InlineEditorObjectFieldModes.Hidden, Expanded = true)]
        public ItemManager manager;

        [MenuItem("Tools/Reshape/Item Explorer", priority = 11100)]
        public static void OpenWindow ()
        {
            var window = GetWindow<ItemExplorer>();
            window.manager = Get();
            window.Show();
        }

        [DisplayAsString]
        [PropertyOrder(-1)]
        [OnInspectorGUI("DisableGUIAfter")]
        [HideLabel]
        public string disableGUIAfter = "";

        private void DisableGUIAfter ()
        {
            GUI.enabled = false;
        }

        private static ItemManager Get ()
        {
            var guids = AssetDatabase.FindAssets("t:ItemManager");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var manager = AssetDatabase.LoadAssetAtPath<ItemManager>(path);
                if (manager != null)
                    return manager;
            }

            return null;
        }
    }
}