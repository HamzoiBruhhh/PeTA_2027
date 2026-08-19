using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Reshape.ReFramework;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class RendererBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            SetMeshMaterials = 10,
            SetMeshMaterial = 11,
            SetMesh = 1000,
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(renderer)")]
        [InlineButton("SetRenderer", "♺", ShowIf = "@renderer.IsObjectValueType()")]
        [InfoBox("@GetMismatchWarningMessage()", InfoMessageType.Error, "@IsShowMismatchWarning()")]
        [ShowIf("ShowRenderer")]
        private SceneObjectProperty renderer = new SceneObjectProperty(SceneObject.ObjectType.Renderer);

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [InlineButton("@SceneObjectList.OpenCreateMenu(materialList, true)", "✚")]
        [OnInspectorGUI("CheckSceneObjectListDirty")]
        [ShowIf("@executionType == ExecutionType.SetMeshMaterials")]
        [InfoBox("The assigned list have not specific type!", InfoMessageType.Warning, "ShowListWarning", GUIAlwaysEnabled = true)]
        private SceneObjectList materialList;
        
        [SerializeField]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(material)")]
        [InfoBox("@material.GetMismatchWarningMessage()", InfoMessageType.Error, "@material.IsShowMismatchWarning()")]
        [ShowIf("@executionType == ExecutionType.SetMeshMaterial")]
        private SceneObjectProperty material = new SceneObjectProperty(SceneObject.ObjectType.Material);

        [SerializeField]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(mesh)")]
        [InfoBox("@mesh.GetMismatchWarningMessage()", InfoMessageType.Error, "@mesh.IsShowMismatchWarning()")]
        [ShowIf("@executionType == ExecutionType.SetMesh")]
        private SceneObjectProperty mesh = new SceneObjectProperty(SceneObject.ObjectType.Mesh);

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType is ExecutionType.None)
            {
                LogWarning("Found an empty Renderer Behaviour node in " + context.objectName);
            }
            else if (executionType is ExecutionType.SetMeshMaterials)
            {
                if (renderer.IsEmpty || !renderer.IsMatchType() || materialList == null || materialList.GetCount() <= 0)
                {
                    LogWarning("Found an empty Renderer Behaviour node in " + context.objectName);
                }
                else
                {
                    var mat = new List<Material>();
                    var count = materialList.GetCount();
                    for (var i = 0; i < count; i++)
                    {
                        var obj = materialList.GetByIndex(i);
                        if (obj != null)
                            mat.Add(obj);
                    }

                    if (mat.Count > 0)
                    {
                        var rend = (Renderer) renderer;
                        if (rend is MeshRenderer defMesh)
                        {
                            defMesh.SetMaterials(mat);
                        }
                        else if (rend is SkinnedMeshRenderer skinnedMesh)
                        {
                            skinnedMesh.SetMaterials(mat);
                        }
                    }
                }
            }
            else if (executionType is ExecutionType.SetMeshMaterial)
            {
                if (renderer.IsEmpty || !renderer.IsMatchType() || material.IsEmpty || !material.IsMatchType())
                {
                    LogWarning("Found an empty Renderer Behaviour node in " + context.objectName);
                }
                else
                {
                    var mat = (Material) material;
                    if (mat)
                    {
                        var rend = (Renderer) renderer;
                        if (rend is MeshRenderer defMesh)
                        {
                            defMesh.material = mat;
                        }
                        else if (rend is SkinnedMeshRenderer skinnedMesh)
                        {
                            skinnedMesh.material = mat;
                        }
                    }
                }
            }
            else if (executionType is ExecutionType.SetMesh)
            {
                if (renderer.IsEmpty || !renderer.IsMatchType())
                {
                    LogWarning("Found an empty Renderer Behaviour node in " + context.objectName);
                }
                else
                {
                    var rend = (Renderer) renderer;
                    if (rend is MeshRenderer defMesh)
                    {
                        if (defMesh.TryGetComponent<MeshFilter>(out var filter))
                        {
                            if (mesh.IsEmpty || !mesh.IsMatchType())
                                filter.sharedMesh = null;
                            else
                                filter.sharedMesh = mesh;
                        }
                    }
                    else if (rend is SkinnedMeshRenderer skinnedMesh)
                    {
                        if (mesh.IsEmpty || !mesh.IsMatchType())
                            skinnedMesh.sharedMesh = null;
                        else
                            skinnedMesh.sharedMesh = mesh;
                    }
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private void SetRenderer ()
        {
            var re = AssignComponent<MeshRenderer>();
            if (re)
                renderer.SetObjectValue(re);
            else
            {
                var re1 = AssignComponent<SkinnedMeshRenderer>();
                if (re1)
                    renderer.SetObjectValue(re1);
                else
                {
                    var re2 = AssignComponent<Renderer>();
                    if (re2)
                        renderer.SetObjectValue(re2);
                }
            }
        }
        
        private bool ShowRenderer ()
        {
            return executionType is ExecutionType.SetMeshMaterials or ExecutionType.SetMesh or ExecutionType.SetMeshMaterial;
        }

        private string GetMismatchWarningMessage ()
        {
            if (renderer.IsShowMismatchWarning())
                return renderer.GetMismatchWarningMessage();
            if (executionType is ExecutionType.SetMeshMaterials)
            {
                if (!renderer.IsEmpty)
                {
                    var rend = (Renderer) renderer;
                    if (rend is MeshRenderer) { }
                    else if (rend is SkinnedMeshRenderer) { }
                    else
                    {
                        return "The assigned variable is not match the require type : MeshRenderer or SkinnedMeshRenderer";
                    }
                }
            }

            return string.Empty;
        }

        private bool IsShowMismatchWarning ()
        {
            if (renderer.IsShowMismatchWarning())
                return true;
            if (executionType is ExecutionType.SetMeshMaterials)
            {
                if (!renderer.IsEmpty)
                {
                    var rend = (Renderer) renderer;
                    if (rend is MeshRenderer) { }
                    else if (rend is SkinnedMeshRenderer) { }
                    else
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void CheckSceneObjectListDirty ()
        {
            var createVarPath = GraphEditorVariable.GetString(GetGraphSelectionInstanceID(), "createVariable");
            if (!string.IsNullOrEmpty(createVarPath))
            {
                GraphEditorVariable.SetString(GetGraphSelectionInstanceID(), "createVariable", string.Empty);
                var createVar = (SceneObjectList) AssetDatabase.LoadAssetAtPath(createVarPath, typeof(SceneObjectList));
                materialList = createVar;
                MarkDirty();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        private bool ShowListWarning ()
        {
            if (materialList != null && materialList.IsNoneType())
                return true;
            return false;
        }

        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Set Material", ExecutionType.SetMeshMaterial},
            {"Set Material List", ExecutionType.SetMeshMaterials},
            {"Set Mesh", ExecutionType.SetMesh}
        };

        public static string displayName = "Renderer Behaviour Node";
        public static string nodeName = "Renderer";

        public override string GetNodeInspectorTitle ()
        {
            return displayName;
        }

        public override string GetNodeViewTitle ()
        {
            return nodeName;
        }
        
        public override string GetNodeIdentityName ()
        {
            return executionType.ToString();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Audio & Visual/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType is ExecutionType.None)
                return string.Empty;
            var message = "";
            if (executionType is ExecutionType.SetMeshMaterials)
            {
                if (!renderer.IsEmpty && renderer.IsMatchType() && materialList != null && materialList.GetCount() > 0)
                    message = "Set " + materialList.name + " (" + materialList.GetCount() + ") to " + renderer.name + "'s materials";
            }
            if (executionType is ExecutionType.SetMeshMaterial)
            {
                if (!renderer.IsEmpty && renderer.IsMatchType() && !material.IsEmpty && material.IsMatchType())
                    message = "Set " + material.objectName + " to " + renderer.name + "'s material";
            }
            else if (executionType is ExecutionType.SetMesh)
            {
                if (!renderer.IsEmpty && renderer.IsMatchType())
                {
                    if (!mesh.IsEmpty && mesh.IsMatchType())
                        message = "Set " + mesh.objectName + " to " + renderer.name + "'s mesh";
                    else
                        message = "Set none to " + renderer.name + "'s mesh";
                }
            }

            return message;
        }

        public override string GetNodeViewTooltip ()
        {
            return "This will provide several controls to a specific Renderer.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}