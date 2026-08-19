using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    public class BaseScriptable : ScriptableObject
    {
        [ShowIf("@ReEditorHelper.IsInspectorDebugMode()")]
        public string uniqueId;

        public BaseScriptable ()
        {
            uniqueId = ReUniqueId.GenerateId();
        }

#if UNITY_EDITOR
        [Button]
        [ShowIf("@string.IsNullOrWhiteSpace(uniqueId)")]
        [PropertySpace(10)]
        public void FillUniqueId ()
        {
            uniqueId = ReUniqueId.GenerateId();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Reshape/Fill Graph Scriptable ID", priority = 15106)]
        public static void FillInUniqueId ()
        {
            var guids = AssetDatabase.FindAssets("t:BaseScriptable");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var variable = AssetDatabase.LoadAssetAtPath<BaseScriptable>(path);
                if (variable && string.IsNullOrWhiteSpace(variable.uniqueId))
                {
                    variable.uniqueId = ReUniqueId.GenerateId();
                    ReDebug.Log("Graph Scriptable", $"{variable.name} have been assign an unique id.");
                    EditorUtility.SetDirty(variable);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [HideInInspector]
        public bool showHints;

        [MenuItem("CONTEXT/BaseScriptable/Hints Display/Show", false)]
        public static void ShowHints (MenuCommand command)
        {
            var comp = (BaseScriptable) command.context;
            comp.showHints = true;
        }

        [MenuItem("CONTEXT/BaseScriptable/Hints Display/Show", true)]
        public static bool IsShowHints (MenuCommand command)
        {
            var comp = (BaseScriptable) command.context;
            if (comp.showHints)
                return false;
            return true;
        }

        [MenuItem("CONTEXT/BaseScriptable/Hints Display/Hide", false)]
        public static void HideHints (MenuCommand command)
        {
            var comp = (BaseScriptable) command.context;
            comp.showHints = false;
        }

        [MenuItem("CONTEXT/BaseScriptable/Hints Display/Hide", true)]
        public static bool IsHideHints (MenuCommand command)
        {
            var comp = (BaseScriptable) command.context;
            if (!comp.showHints)
                return false;
            return true;
        }
#endif
    }
}