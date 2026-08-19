using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.SceneManagement;

namespace Reshape.ReGraph
{
    public class GraphEditorWindow : EditorWindow
    {
        SerializedGraph serializer;
        GraphSettings settings;
        GraphViewer treeView;

        /*WindowInspectorView inspectorView;
        WindowBlackboardView blackboardView;
        WindowOverlayView overlayView;*/
        ToolbarMenu toolBarMenu;
        ToolbarMenu settingBarMenu;
        Label titleLabel;
        VisualElement autoSavePanel;
        Toggle autoSaveToggle;
        Button autoSaveButton;

        private bool refreshAfterNewlySaved;
        private int changeNotSaveCount;

        [MenuItem("Tools/Reshape/Graph Editor", priority = 11000)]
        public static void OpenWindow ()
        {
            GraphEditorWindow wd = GetWindow<GraphEditorWindow>();
            wd.titleContent = new GUIContent("GraphEditor");
            wd.minSize = new Vector2(800, 600);
        }

        public static void OpenWindow (SerializedObject serialized)
        {
            GraphEditorWindow wd = GetWindow<GraphEditorWindow>();
            wd.titleContent = new GUIContent("Graph Editor");
            wd.minSize = new Vector2(800, 600);
            wd.SelectGraph(serialized);
        }

        public static bool CloseWindow ()
        {
            if (HasOpenInstances<GraphEditorWindow>())
            {
                var wd = GetWindow<GraphEditorWindow>();
                wd.Close();
                return true;
            }

            return false;
        }

        public static bool HasFocus ()
        {
            EditorWindow[] ed = (EditorWindow[]) Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (int i = 0; i < ed.Length; i++)
            {
                if (ed[i].GetType() == typeof(GraphEditorWindow))
                {
                    var wnd = (GraphEditorWindow) ed[i];
                    return wnd.hasFocus;
                }
            }

            return false;
        }

        public static void RefreshCurrentGraph (bool forceRefresh = false)
        {
            var ed = (EditorWindow[]) Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (var i = 0; i < ed.Length; i++)
            {
                if (ed[i].GetType() == typeof(GraphEditorWindow))
                {
                    var wnd = (GraphEditorWindow) ed[i];
                    if (wnd.hasFocus && wnd.serializer != null)
                    {
                        if (Selection.activeObject is GraphScriptable)
                            wnd.SelectGraph(new SerializedObject(Selection.activeObject), forceRefresh);
                        else if (Selection.activeObject is GameObject go)
                        {
                            if (go.TryGetComponent(out GraphRunner runner))
                                wnd.SelectGraph(new SerializedObject(runner), forceRefresh);
                        }
                    }

                    return;
                }
            }
        }

        public static bool IsAutoSave ()
        {
            var ed = (EditorWindow[]) Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (var i = 0; i < ed.Length; i++)
            {
                if (ed[i].GetType() == typeof(GraphEditorWindow))
                {
                    var wnd = (GraphEditorWindow) ed[i];
                    if (wnd.hasFocus && wnd.serializer != null)
                    {
                        return wnd.autoSaveToggle.value;
                    }
                }
            }

            return true;
        }

        public void CreateGUI ()
        {
            settings = GraphSettings.GetSettings();

            VisualElement root = rootVisualElement;
            var visualTree = settings.graphXml;
            visualTree.CloneTree(root);
            var styleSheet = settings.graphStyle;
            root.styleSheets.Add(styleSheet);

            treeView = root.Q<GraphViewer>();
            //inspectorView = root.Q<InspectorView>();
            //blackboardView = root.Q<BlackboardView>();
            //overlayView = root.Q<OverlayView>("OverlayView");
            toolBarMenu = root.Q<ToolbarMenu>("Tools");
            settingBarMenu = root.Q<ToolbarMenu>("Settings");
            titleLabel = root.Q<Label>("TitleLabel");
            autoSavePanel = root.Q<VisualElement>("AutoSave");
            autoSavePanel.visible = false;
            autoSavePanel.style.left = new StyleLength(58);
            autoSaveToggle = root.Q<Toggle>("AutoSaveToggle");
            autoSaveToggle.value = GraphEditorPrefs.autoSaveModification;
            autoSaveToggle.RegisterCallback<ChangeEvent<bool>>((evt) => { UpdateAutoSaveButtonVisibility(); });
            autoSaveButton = root.Q<Button>("AutoSaveButton");
            autoSaveButton.clickable = new Clickable(SaveSerializedGraph);

            toolBarMenu.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                toolBarMenu.menu.MenuItems().Clear();
                toolBarMenu.menu.AppendAction($"Open Graph Inspector", (a) => { GraphInspector.OpenWindow(); });
                toolBarMenu.menu.AppendAction($"Open Graph Finder", (a) => { GraphFinder.OpenWindow(); });
                toolBarMenu.menu.AppendSeparator();
            });
            
            settingBarMenu.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                settingBarMenu.menu.MenuItems().Clear();
                settingBarMenu.menu.AppendAction($"Label Behaviour/Show Text", (a) => { OnSettingLabelBehaviourShowText(); }, GraphPrefs.showLabelBehaviourText ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                settingBarMenu.menu.AppendAction($"Label Behaviour/Hide Text", (a) => { OnSettingLabelBehaviourHideText(); }, !GraphPrefs.showLabelBehaviourText ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                settingBarMenu.menu.AppendSeparator();
            });

            // Overlay view
            treeView.OnNodeSelected = OnNodeSelectionChanged;
            treeView.OnNodeUnselected = OnNodeUnselectChanged;
            //overlayView.OnTreeSelected += SelectTree;
            Undo.undoRedoPerformed += OnUndoRedo;

            if (serializer == null)
            {
                //overlayView.Show();
            }
        }

        void OnUndoRedo ()
        {
            if (serializer != null)
            {
                treeView.PopulateView(serializer);
            }
        }

        private void OnSelectionChange ()
        {
            if (Selection.objects.Length == 1)
            {
                if (Selection.activeGameObject)
                {
                    if (Selection.activeGameObject.scene.IsValid())
                    {
                        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                        if (PrefabUtility.GetPrefabInstanceStatus(Selection.activeGameObject) != PrefabInstanceStatus.Connected)
                        {
                            if (prefabStage != null)
                                if (prefabStage.mode == PrefabStage.Mode.InContext)
                                    return;
                            var runner = Selection.activeGameObject.GetComponent<GraphRunner>();
                            if (runner != null)
                            {
                                SelectGraph(new SerializedObject(runner));
                                return;
                            }
                        }
                        else
                        {
                            if (prefabStage == null)
                            {
                                var runner = Selection.activeGameObject.GetComponent<GraphRunner>();
                                if (runner != null && !PrefabUtility.IsPartOfNonAssetPrefabInstance(runner))
                                {
                                    SelectGraph(new SerializedObject(runner));
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            ClearSelection();
        }

        void SelectGraph (SerializedObject graphCreatorObj, bool forceRefresh = false)
        {
            if (graphCreatorObj == null)
            {
                ClearSelection();
                return;
            }

            var instanceId = 0;
            Graph graph = null;
            var isScriptable = false;
            if (graphCreatorObj.targetObject is GraphRunner runner)
            {
                instanceId = runner.GetInstanceID();
                if (runner)
                    graph = runner.graph;
            }
            else if (graphCreatorObj.targetObject is GraphScriptable scriptable)
            {
                instanceId = scriptable.GetInstanceID();
                if (scriptable)
                {
                    graph = scriptable.graph;
                    isScriptable = true;
                }
            }

            if (graph is {isCreated: false})
            {
                ClearSelection();
                return;
            }

            if (!forceRefresh && serializer != null && serializer.graphObjectId == instanceId && serializer.graph == graph)
            {
                if (graph is {isCreated: true} && treeView != null && treeView.graphElements.Any())
                    return;
            }

            serializer = new SerializedGraph(graphCreatorObj);
            serializer.OnUpdate -= OnSerializerUpdate;
            serializer.OnUpdate += OnSerializerUpdate;
            ResetViewPreviewNode();

            refreshAfterNewlySaved = false;

            if (PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                if (string.IsNullOrEmpty(serializer.graphPath))
                {
                    UpdateTitleLabel(true);
                    EditorSceneManager.sceneSaved -= OnNewSceneSaved;
                    EditorSceneManager.sceneSaved += OnNewSceneSaved;
                }
                else
                {
                    UpdateTitleLabel(false);
                }
            }
            else
            {
                UpdateTitleLabel(false);
            }

            //overlayView.Hide();
            if (treeView != null)
                treeView.PopulateView(serializer);
            //blackboardView.Bind(serializer);

            if (autoSavePanel != null)
            {
                autoSavePanel.visible = true;
                UpdateAutoSaveButtonVisibility();
                autoSaveToggle.label = "Auto Apply";
                autoSaveToggle.tooltip = "Auto Apply Changes After Modification";
                if (!isScriptable)
                    autoSaveToggle.MarkDirtyRepaint();
            }
        }

        private void OnSerializerUpdate (int type)
        {
            if (type == SerializedGraph.UpdateGraphId)
            {
                refreshAfterNewlySaved = true;
            }
        }

        private void UpdateAutoSaveButtonVisibility ()
        {
            autoSaveButton.visible = !autoSaveToggle.value;
            GraphEditorPrefs.autoSaveModification = autoSaveToggle.value;
        }

        private void ClearSelection ()
        {
            if (serializer != null)
            {
                ResetViewPreviewNode();
                serializer = null;
            }

            //overlayView.Show();
            if (treeView != null)
                treeView.ClearView();
            //inspectorView.ClearSelection(serializer);

            if (titleLabel != null)
                titleLabel.text = "Graph View";
            if (autoSavePanel != null)
                autoSavePanel.visible = false;
            if (autoSaveButton != null)
                autoSaveButton.visible = false;
        }

        private void OnNewSceneSaved (Scene scene)
        {
            EditorSceneManager.sceneSaved -= OnNewSceneSaved;
            EditorApplication.delayCall += () => { refreshAfterNewlySaved = true; };
        }

        private void OnNodeSelectionChanged (GraphNodeView node)
        {
            if (serializer != null && serializer.graph != null && node != null)
            {
                AssignViewPreviewNode(node.node);
                //RepaintInspector("UnityEditor.GameObjectInspector");
            }
            //inspectorView.UpdateSelection(serializer, node);
        }

        private void OnNodeUnselectChanged (GraphNodeView node)
        {
            if (serializer != null)
            {
                ResetViewPreviewNode();
                //RepaintInspector("UnityEditor.GameObjectInspector");
            }
        }

        private void ResetViewPreviewNode ()
        {
            serializer.SetViewPreviewNode(null);
            serializer.SetViewPreviewSelected(false);
        }

        private void AssignViewPreviewNode (GraphNode node)
        {
            serializer.SetViewPreviewNode(node);
            serializer.SetViewPreviewSelected(true);
        }

        private void OnInspectorUpdate ()
        {
            if (Selection.objects.Length == 1)
            {
                if (Selection.activeObject is GameObject go)
                {
                    var runner = go.GetComponent<GraphRunner>();
                    if (runner)
                    {
                        if (serializer == null || runner.graph != serializer.graph)
                        {
                            if (Selection.activeGameObject.scene.IsValid() && PrefabUtility.GetPrefabInstanceStatus(Selection.activeGameObject) != PrefabInstanceStatus.Connected)
                            {
                                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                                if (prefabStage != null && prefabStage.mode != PrefabStage.Mode.InContext)
                                    SelectGraph(new SerializedObject(runner));
                            }
                        }
                        else if (runner.graph.isCreated && !treeView.graphElements.Any())
                        {
                            if (Selection.activeGameObject.scene.IsValid() && PrefabUtility.GetPrefabInstanceStatus(Selection.activeGameObject) != PrefabInstanceStatus.Connected)
                            {
                                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                                if (prefabStage != null && prefabStage.mode != PrefabStage.Mode.InContext)
                                    SelectGraph(new SerializedObject(runner));
                            }
                        }

                        if (runner.graph.selectedViewNode is {Count: 1} && !runner.graph.HavePreviewNode())
                        {
                            if (runner.graph.selectedViewNode[0] is GraphNodeView nodeView)
                                AssignViewPreviewNode(nodeView.node);
                        }
                    }
                    else
                    {
                        if (serializer is {graph: { }})
                        {
                            ClearSelection();
                        }
                    }
                }
                else if (Selection.activeObject is GraphScriptable scriptable)
                {
                    if (serializer == null || scriptable.graph != serializer.graph)
                    {
                        SelectGraph(new SerializedObject(scriptable));
                    }
                    else if (scriptable.graph.isCreated && !treeView.graphElements.Any())
                    {
                        SelectGraph(new SerializedObject(scriptable));
                    }

                    if (scriptable.graph.selectedViewNode is {Count: 1} && !scriptable.graph.HavePreviewNode())
                    {
                        GraphNodeView nodeView = scriptable.graph.selectedViewNode[0] as GraphNodeView;
                        if (nodeView != null)
                            AssignViewPreviewNode(nodeView.node);
                    }
                }
            }

            if (refreshAfterNewlySaved)
            {
                if (serializer != null)
                {
                    if (serializer.serializedObject.targetObject == null)
                        ClearSelection();
                    else
                        UpdateTitleLabel(false);
                }

                refreshAfterNewlySaved = false;
            }

            if (serializer != null)
            {
                if (serializer.graphRequireUpdateAndSave)
                {
                    if (serializer.graphSelectionObject is GameObject go)
                    {
                        UpdateTitleLabel(false);
                        EditorSceneManager.MarkSceneDirty(go.scene);
                        EditorSceneManager.SaveScene(go.scene, serializer.graphPath);
                        serializer.graphRequireUpdateAndSave = false;
                    }
                }

                if (autoSaveToggle.value)
                {
                    if (serializer.haveChangeNotSave > 0)
                    {
                        if (serializer.haveChangeNotSave == int.MaxValue || changeNotSaveCount == serializer.haveChangeNotSave)
                        {
                            SaveSerializedGraph();
                        }
                        else
                        {
                            changeNotSaveCount = serializer.haveChangeNotSave;
                        }
                    }
                }

                if (serializer.graph != null && !serializer.graph.previewSelected)
                {
                    treeView.UnhighlightAllReferenceNode();
                }
            }

            treeView?.UpdateNodeStates();
        }

        private void SaveSerializedGraph ()
        {
            if (EditorApplication.isPlaying)
                return;
            if (serializer != null)
            {
                serializer.haveChangeNotSave = 0;
                serializer.SaveGraph();
                changeNotSaveCount = 0;
            }
        }

        private void OnEnable ()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable ()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged (PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.delayCall += OnSelectionChange;
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.delayCall += OnSelectionChange;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }

        private void RepaintInspector (string typeName)
        {
            Editor[] ed = (Editor[]) Resources.FindObjectsOfTypeAll<Editor>();
            for (int i = 0; i < ed.Length; i++)
            {
                if (ed[i].GetType().ToString() == typeName)
                {
                    ed[i].Repaint();
                    return;
                }
            }
        }

        private void UpdateTitleLabel (bool newly)
        {
            if (titleLabel != null && serializer != null)
            {
                if (newly)
                    titleLabel.text = $"Graph from GameObject {serializer.graphSelectionObject.name} in a newly created scene";
                else
                {
                    var graphPath = string.Empty;
                    if (!string.IsNullOrEmpty(serializer.graphPath))
                        graphPath = $" in {serializer.graphPath}";
                    titleLabel.text = $"Graph from GameObject {serializer.graphSelectionObject.name}{graphPath} [ID:{serializer.graph.id}]";
                    if (string.IsNullOrEmpty(serializer.graph.id))
                    {
                        EditorSceneManager.sceneSaved -= OnExistingSceneSaved;
                        EditorSceneManager.sceneSaved += OnExistingSceneSaved;
                    }
                }
            }
        }

        private void OnExistingSceneSaved (Scene scene)
        {
            EditorApplication.delayCall += () =>
            {
                EditorSceneManager.sceneSaved -= OnExistingSceneSaved;
                UpdateTitleLabel(false);
            };
        }
        
        void OnSettingLabelBehaviourShowText ()
        {
            GraphPrefs.showLabelBehaviourText = true;
            RefreshCurrentGraph(true);
        }
        
        void OnSettingLabelBehaviourHideText ()
        {
            GraphPrefs.showLabelBehaviourText = false;
            RefreshCurrentGraph(true);
        }
    }
}