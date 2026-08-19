using System;
using Reshape.Unity;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using Reshape.Unity.Editor;
#endif

namespace Reshape.ReFramework
{
    [HideMonoScript]
    [CreateAssetMenu(menuName = "Reshape/Scene Object Variable", order = 13)]
    public class SceneObjectVariable : VariableScriptableObject
    {
        [SerializeField]
        [HideLabel]
        [InlineProperty]
        public SceneObject sceneObject;

        public bool IsUnassigned ()
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsGameObject())
                {
                    var go = GetGameObject();
                    if (!go)
                        return true;
                }
                else if (sceneObject.IsComponent())
                {
                    var comp = GetComponent();
                    if (!comp)
                        return true;
                }
                else if (sceneObject.IsMaterial())
                {
                    var mat = GetMaterial();
                    if (!mat)
                        return true;
                }
                else if (sceneObject.IsSprite())
                {
                    var sp = GetSprite();
                    if (!sp)
                        return true;
                }
                else if (sceneObject.IsScriptableObject())
                {
                    var so = GetScriptableObject();
                    if (!so)
                        return true;
                }
                else if (sceneObject.IsMesh())
                {
                    var mes = GetMesh();
                    if (!mes)
                        return true;
                }
                else if (sceneObject.IsAudioMixer())
                {
                    var mixer = GetAudioMixer();
                    if (!mixer)
                        return true;
                }

                return false;
            }

            return true;
        }

        public int GetSceneObjectUniqueID ()
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsGameObject())
                {
                    var go = GetGameObject();
                    if (go)
                        return go.GetInstanceID();
                }
                else if (sceneObject.IsComponent())
                {
                    var comp = GetComponent();
                    if (comp)
                        return comp.GetInstanceID();
                }
                else if (sceneObject.IsMaterial())
                {
                    var mat = GetMaterial();
                    if (mat)
                        return mat.GetInstanceID();
                }
                else if (sceneObject.IsSprite())
                {
                    var sp = GetSprite();
                    if (sp)
                        return sp.GetInstanceID();
                }
                else if (sceneObject.IsScriptableObject())
                {
                    var so = GetScriptableObject();
                    if (so)
                    {
                        if (so is AttackStatusPack pack)
                            return pack.customId;
                        return so.GetInstanceID();
                    }
                }
                else if (sceneObject.IsMesh())
                {
                    var mes = GetMesh();
                    if (mes)
                        return mes.GetInstanceID();
                }
                else if (sceneObject.IsAudioMixer())
                {
                    var mixer = GetAudioMixer();
                    if (mixer)
                        return mixer.GetInstanceID();
                }
            }

            return 0;
        }
        
        public GameObject GetGameObject ()
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsGameObject())
                {
                    GameObject go = null;
                    if (sceneObject.TryGetValue(ref go))
                        return go;
                }
            }
            
            return default;
        }
        
        public Material GetMaterial ()
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsMaterial())
                {
                    Material mat = null;
                    if (sceneObject.TryGetValue(ref mat))
                        return mat;
                }
            }
            
            return default;
        }
        
        public Sprite GetSprite ()
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsSprite())
                {
                    Sprite sp = null;
                    if (sceneObject.TryGetValue(ref sp))
                        return sp;
                }
            }
            
            return default;
        }
        
        public ScriptableObject GetScriptableObject ()
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsScriptableObject())
                {
                    ScriptableObject so = null;
                    if (sceneObject.TryGetValue(ref so))
                        return so;
                }
            }
            
            return default;
        }
        
        public Mesh GetMesh ()
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsMesh())
                {
                    Mesh mes = null;
                    if (sceneObject.TryGetValue(ref mes))
                        return mes;
                }
            }
            
            return default;
        }
        
        public AudioMixer GetAudioMixer ()
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsAudioMixer())
                {
                    AudioMixer mixer = null;
                    if (sceneObject.TryGetValue(ref mixer))
                        return mixer;
                }
            }
            
            return default;
        }
        
        public Component GetComponent ()
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsComponent())
                {
                    Component comp = null;
                    if (sceneObject.TryGetValue(ref comp))
                        return comp;
                }
            }
            
            return default;
        }

        public void SetValue (GameObject go)
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsGameObject())
                    if (sceneObject.TrySetValue(go))
                        OnChanged();
            }
        }
        
        public void SetValue (Component comp)
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsComponent())
                    if (sceneObject.TrySetValue(comp))
                        OnChanged();
            }
        }
        
        public void SetValue (Material mat)
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsMaterial())
                    if (sceneObject.TrySetValue(mat))
                        OnChanged();
            }
        }
        
        public void SetValue (Sprite sp)
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsSprite())
                    if (sceneObject.TrySetValue(sp))
                        OnChanged();
            }
        }
        
        public void SetValue (ScriptableObject so)
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsScriptableObject())
                    if (sceneObject.TrySetValue(so))
                        OnChanged();
            }
        }
        
        public void SetValue (Mesh mes)
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsMesh())
                    if (sceneObject.TrySetValue(mes))
                        OnChanged();
            }
        }
        
        public void SetValue (AudioMixer mixer)
        {
            if (sceneObject != null)
            {
                if (sceneObject.IsAudioMixer())
                    if (sceneObject.TrySetValue(mixer))
                        OnChanged();
            }
        }
        
        public void SetValue (SceneObject so)
        {
            if (sceneObject != null && so != null)
            {
                SceneObject tempSo = sceneObject;
                sceneObject = so.ShallowCopy();
                sceneObject.showType = tempSo.showType;
                sceneObject.showAsNodeProperty = tempSo.showAsNodeProperty;
                sceneObject.displayName = tempSo.displayName;
            }
        }
        
        public override void Rebase ()
        {
            sceneObject?.Reset();
        }
        
        public override void OnReset()
        {
            Rebase();
            base.OnReset();
        }
        
        protected override void OnChanged ()
        {
            if (!resetLinked)
            {
                onReset -= OnReset;
                onReset += OnReset;
                resetLinked = true;
            }
            
            base.OnEarlyChanged();
            base.OnChanged();
        }

        public static implicit operator string (SceneObjectVariable s)
        {
            return s.ToString();
        }

        public override string ToString ()
        {
            return sceneObject.ToString();
        }
        
        public override bool supportSaveLoad => false;

#if UNITY_EDITOR
        private Sprite backupSprite;
        
        private void EditorReset ()
        {
            sceneObject?.EditorReset(backupSprite);
            backupSprite = null;
            resetLinked = false;
            sceneUnloadLinked = false;
        }

        private void EditorBackUp ()
        {
            if (sceneObject != null && sceneObject.IsSprite())
                backupSprite = GetSprite();
        }
        
        public static SceneObjectVariable CreateNew (SceneObjectVariable sceneObject, SceneObject.ObjectType type)
        {
            if (sceneObject != null)
            {
                bool proceed = EditorUtility.DisplayDialog("Graph Variable", "Are you sure you want to create a new variable to replace the existing assigned variable ?", "OK", "Cancel");
                if (!proceed)
                    return sceneObject;
            }

            var path = EditorUtility.SaveFilePanelInProject("Graph Variable", "New Scene Object Variable", "asset", "Select a location to create variable");
            if (path.Length == 0)
                return sceneObject;
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                return sceneObject;

            SceneObjectVariable variable = ScriptableObject.CreateInstance<SceneObjectVariable>();
            variable.sceneObject = new SceneObject();
            variable.sceneObject.type = type;
            AssetDatabase.CreateAsset(variable, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return variable;
        }
        
        [InitializeOnLoad]
        public static class SceneObjectVariableResetOnPlay
        {
            static SceneObjectVariableResetOnPlay ()
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }

            private static void OnPlayModeChanged (PlayModeStateChange state)
            {
                ReEditorHelper.HavePlayModeStateChange(state, out var enter, out var exit);
                if (exit)
                {
                    var guids = AssetDatabase.FindAssets("t:SceneObjectVariable");
                    if (guids.Length > 0)
                    {
                        for (var i = 0; i < guids.Length; i++)
                        {
                            var list = (SceneObjectVariable) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(UnityEngine.Object));
                            if (list != null)
                                list.EditorReset();
                        }

                        AssetDatabase.SaveAssets();
                    }
                }
                else if (enter)
                {
                    var guids = AssetDatabase.FindAssets("t:SceneObjectVariable");
                    if (guids.Length > 0)
                    {
                        for (var i = 0; i < guids.Length; i++)
                        {
                            var list = (SceneObjectVariable) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(UnityEngine.Object));
                            if (list != null)
                                list.EditorBackUp();
                        }
                    }
                }
            }
        }
#endif
    }
}