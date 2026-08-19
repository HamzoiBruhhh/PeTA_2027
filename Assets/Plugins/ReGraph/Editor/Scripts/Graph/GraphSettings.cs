using System.Collections.Generic;
using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Reshape.ReGraph
{
#if REGRAPH_DEV_DEBUG
    [CreateAssetMenu(menuName = "Reshape/Graph Settings", order = 202)]
#endif
    [HideMonoScript]
    class GraphSettings : ScriptableObject
    {
        private static GraphSettings graphSettings;
        
        [BoxGroup("Graph"), LabelWidth(180)]
        public VisualTreeAsset graphXml;

        [BoxGroup("Graph"), LabelWidth(180)]
        public StyleSheet graphStyle;

        [BoxGroup("Graph"), LabelWidth(180)]
        public VisualTreeAsset graphNodeXml;
        
        [BoxGroup("Graph"), LabelWidth(180)]
        public GraphNoteDatabase graphNoteDb;

        [BoxGroup("Graph"), LabelWidth(180)]
        [OnValueChanged("CloseWindow")]
        public float graphZoomInCap = 1.3f;

        public void CloseWindow ()
        {
            if (GraphEditorWindow.CloseWindow())
            {
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.DisplayDialog("Graph Settings", "Changing value of Graph Zoom In Cap will close the Graph Editor window automatically. ", "Got it");
                };                
            }
        }
        
        static GraphSettings FindSettings ()
        {
            var guids = AssetDatabase.FindAssets("t:GraphSettings");
            if (guids.Length > 1)
            {
                ReDebug.LogWarning("Graph Editor", $"Found multiple settings files, currently is using the first found settings file.", false);
            }

            switch (guids.Length)
            {
                case 0:
                    return null;
                default:
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<GraphSettings>(path);
            }
        }

        internal static GraphSettings GetSettings ()
        {
            if (!graphSettings)
                graphSettings = FindSettings();
            return graphSettings;
        }

        internal static SerializedObject GetSerializedSettings ()
        {
            return new SerializedObject(GetSettings());
        }

        public static List<T> LoadAssets<T> () where T : Object
        {
            string[] assetIds = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<T> assets = new List<T>();
            foreach (var assetId in assetIds)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetId);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                assets.Add(asset);
            }

            return assets;
        }

        public static List<string> GetAssetPaths<T> () where T : Object
        {
            string[] assetIds = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<string> paths = new List<string>();
            foreach (var assetId in assetIds)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetId);
                paths.Add(path);
            }

            return paths;
        }
    }

    // Register a SettingsProvider using UIElements for the drawing framework:
    static class MyCustomSettingsUIElementsRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider ()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
            var provider = new SettingsProvider("Project/MyCustomUIElementsSettings", SettingsScope.Project)
            {
                label = "ReGraph",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = (_, rootElement) =>
                {
                    var graphSettings = GraphSettings.GetSerializedSettings();
                    var frameworkSettings = FrameworkSettings.GetSerializedSettings();
                    var runtimeSettings = RuntimeSettings.GetSerializedSettings();

                    // rootElement is a VisualElement. If you add any children to it, the OnGUI function
                    // isn't called because the SettingsProvider uses the UIElements drawing framework.
                    var editorTitle = new Label()
                    {
                        text = "  Editor Settings"
                    };
                    editorTitle.AddToClassList("title");
                    editorTitle.style.fontSize = new StyleLength(19);
                    editorTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                    editorTitle.style.paddingTop = new StyleLength(3);
                    editorTitle.style.paddingBottom = new StyleLength(3);

                    var runtimeTitle = new Label()
                    {
                        text = "  Runtime Settings"
                    };
                    runtimeTitle.AddToClassList("title");
                    runtimeTitle.style.fontSize = new StyleLength(19);
                    runtimeTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                    runtimeTitle.style.paddingTop = new StyleLength(13);
                    runtimeTitle.style.paddingBottom = new StyleLength(3);

                    var editorProperties = new VisualElement() {style = {flexDirection = FlexDirection.Column}};
                    editorProperties.AddToClassList("property-list");
                    var runtimeProperties = new VisualElement() {style = {flexDirection = FlexDirection.Column}};
                    runtimeProperties.AddToClassList("property-list");

                    var scrollView = new ScrollView(ScrollViewMode.Vertical);
                    rootElement.Add(scrollView);

                    scrollView.Add(editorTitle);
                    scrollView.Add(editorProperties);
                    editorProperties.Add(new InspectorElement(graphSettings));
                    editorProperties.Add(new InspectorElement(frameworkSettings));
                    scrollView.Bind(graphSettings);
                    scrollView.Bind(frameworkSettings);

                    scrollView.Add(runtimeTitle);
                    scrollView.Add(runtimeProperties);
                    runtimeProperties.Add(new InspectorElement(runtimeSettings));
                    scrollView.Bind(runtimeSettings);
                },
            };

            return provider;
        }
    }
}