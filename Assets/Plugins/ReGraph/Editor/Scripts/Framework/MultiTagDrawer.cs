using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

namespace Reshape.ReFramework
{
    public class MultiTagDrawer : OdinValueDrawer<MultiTag>
    {
        private Rect buttonRect;
        private readonly List<string> tagNames = new();
        private readonly List<int> tagIndexes = new();
        private readonly List<bool> selectedTags = new();
        private string buttonText;

        protected override void DrawPropertyLayout (GUIContent label)
        {
            var data = GetAllTagNames(tagNames, tagIndexes, ValueEntry.SmartValue);
            var me = ValueEntry.SmartValue;

            EditorGUILayout.BeginHorizontal();
            if (label != null)
                EditorGUILayout.PrefixLabel(label);

            selectedTags.Clear();
            var tagNameArray = tagNames.ToArray();
            var outputTags = me.GetSelectedTags(tagNameArray);
            for (var i = 0; i < outputTags.Count; i++)
                selectedTags.Add(outputTags[i]);

            buttonText = me.GetSelectedString(tagNameArray, selectedTags, 1);
            
            if (GUILayout.Button(buttonText, SirenixGUIStyles.DropDownMiniButton))
            {
                buttonText = null;
                PopupWindow.Show(buttonRect, new MultiTagPopupSelector()
                {
                    tagNames = tagNames,
                    tagIndexes = tagIndexes,
                    selectedTags = selectedTags,
                    multiTag = ValueEntry.SmartValue,
                    bitmask = ValueEntry.SmartValue,
                    tagNameData = data,
                    onSet = (updated) =>
                    {
                        ValueEntry.SmartValue = updated;
                        ValueEntry.Property.MarkSerializationRootDirty();
                        buttonText = null;
                    }
                });
            }

            if (Event.current.type == EventType.Repaint)
                buttonRect = GUILayoutUtility.GetLastRect();
            EditorGUILayout.EndHorizontal();
        }


        private class MultiTagPopupSelector : PopupWindowContent
        {
            public List<string> tagNames = new();
            public List<int> tagIndexes = new();
            public List<bool> selectedTags = new();
            public Action<MultiTag> onSet;
            public int bitmask;
            public MultiTag multiTag;
            public MultiTagData tagNameData;

            public override Vector2 GetWindowSize ()
            {
                const float heightOfOne = 28f;
                var height = heightOfOne * (2 + tagNames.Count) + 20;
                return new Vector2(200, height);
            }

            public override void OnGUI (Rect rect)
            {
                GUILayout.Label(multiTag.name, EditorStyles.boldLabel);
                var all = true;
                var none = true;
                foreach (var b in selectedTags)
                {
                    if (b)
                        none = false;
                    else
                        all = false;
                    if (!all && !none) break;
                }

                Texture2D noneIcon = none ? EditorIcons.TestPassed : EditorIcons.TestNormal;
                if (SirenixEditorGUI.MenuButton(0, " None", false, noneIcon))
                {
                    SetBitmask(-1, true);
                    SaveBitmask();
                    for (int i = 0; i < selectedTags.Count; i++)
                    {
                        selectedTags[i] = false;
                    }
                }

                for (int i = 0; i < tagNames.Count; i++)
                {
                    var currentIcon = selectedTags[i] ? EditorIcons.TestPassed : EditorIcons.TestNormal;
                    if (SirenixEditorGUI.MenuButton(0, " " + tagNames[i], false, currentIcon))
                    {
                        selectedTags[i] = !selectedTags[i];
                        SetBitmask(tagIndexes[i], selectedTags[i]);
                        SaveBitmask();
                    }
                }

                var allIcon = all ? EditorIcons.TestPassed : EditorIcons.TestNormal;
                if (SirenixEditorGUI.MenuButton(0, " All", false, allIcon))
                {
                    SetBitmask(-2, true);
                    SaveBitmask();
                    for (var i = 0; i < selectedTags.Count; i++)
                        selectedTags[i] = true;
                }

                SaveBitmask();
                editorWindow.Repaint();
            }

            private void SetBitmask (int index, bool set)
            {
                if (index == -1)
                {
                    bitmask = 0;
                    return;
                }

                if (index == -2)
                {
                    bitmask = ~0;
                    return;
                }

                int bitVal = (int) Mathf.Pow(2, index);
                if (set)
                    bitmask |= bitVal;
                else
                    bitmask &= ~bitVal;
            }

            private void SaveBitmask ()
            {
                if (multiTag.value != bitmask)
                {
                    multiTag.value = bitmask;
                    multiTag.data = tagNameData;
                    multiTag.dirty = true;
                    onSet(multiTag);
                }
            }


            public override void OnOpen () { }

            public override void OnClose ()
            {
                SaveBitmask();
            }
        }

        private MultiTagData GetAllTagNames (List<string> names, List<int> indexes, MultiTag multiTag)
        {
            names.Clear();
            indexes.Clear();

            var assets = AssetDatabase.FindAssets("t:" + multiTag.tagType);
            if (assets != null && assets.Length > 0 && assets[0] != null)
            {
                var listPath = AssetDatabase.GUIDToAssetPath(assets[0]);
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(listPath))
                {
                    var data = (MultiTagData) obj;
                    for (var j = 0; j <= 31; j++)
                    {
                        var name = data.tags[j];
                        if (name.Length > 0)
                        {
                            indexes.Add(j);
                            names.Add(name);
                        }
                    }

                    return data;
                }
            }

            return null;
        }
    }
}