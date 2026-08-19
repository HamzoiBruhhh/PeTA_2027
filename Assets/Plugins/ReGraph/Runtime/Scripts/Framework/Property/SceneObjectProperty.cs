using System;
using Reshape.ReGraph;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace Reshape.ReFramework
{
    [Serializable]
    public class SceneObjectProperty : ReProperty, IClone<SceneObjectProperty>
    {
        [SerializeField]
        [HideLabel]
        [ShowIf("@type == 0 || type == 3")]
        [InlineButton("SwitchToVariable", "▼", ShowIf = "ShowSwitchButton")]
        [OnValueChanged("MarkDirty")]
        [InlineProperty]
        [HideReferenceObjectPicker]
        [OnInspectorGUI("CheckSceneObjectDirty")]
        public SceneObject objectValue;

        [SerializeField]
        [LabelText("@VariableLabel()")]
        [ShowIf("@type == 1 || type == 2")]
        [InlineButton("SwitchToObject", "▼", ShowIf = "ShowSwitchButton")]
        [InlineButton("CreateSceneObjectVariable", "✚")]
        [OnValueChanged("MarkDirty")]
        public SceneObjectVariable variableValue;

        [HideInInspector]
        public int type = 0;

        public SceneObjectProperty () { }

        public SceneObjectProperty (SceneObject.ObjectType type)
        {
            objectValue = new SceneObject();
            objectValue.type = type;
            objectValue.showType = false;
            objectValue.showAsNodeProperty = true;
        }

        public SceneObjectProperty (SceneObject.ObjectType type, string name)
        {
            objectValue = new SceneObject();
            objectValue.type = type;
            objectValue.showType = false;
            objectValue.showAsNodeProperty = true;
            objectValue.displayName = name;
        }

        public SceneObjectProperty ShallowCopy ()
        {
            var copy = new SceneObjectProperty();
            copy.type = type;
            copy.objectValue = objectValue.ShallowCopy();
            copy.variableValue = variableValue;
            return copy;
        }

        public static implicit operator GameObject (SceneObjectProperty f)
        {
            if (f != null)
            {
                if (f.type is 0 or 3)
                {
                    if (f.objectValue != null)
                    {
                        if (f.objectValue.IsGameObject())
                        {
                            GameObject go = null;
                            if (f.objectValue.TryGetValue(ref go))
                                return go;
                        }
                    }

                    return default;
                }

                if (f.variableValue == null)
                    return default;
                return f.variableValue.GetGameObject();
            }
            
            return default;
        }

        public static implicit operator Material (SceneObjectProperty f)
        {
            if (f != null)
            {
                if (f.type is 0 or 3)
                {
                    if (f.objectValue != null)
                    {
                        if (f.objectValue.IsMaterial())
                        {
                            Material mat = null;
                            if (f.objectValue.TryGetValue(ref mat))
                                return mat;
                        }
                    }

                    return default;
                }

                if (f.variableValue == null)
                    return default;
                return f.variableValue.GetMaterial();
            }

            return default;
        }

        public static implicit operator Sprite (SceneObjectProperty f)
        {
            if (f != null)
            {
                if (f.type is 0 or 3)
                {
                    if (f.objectValue != null)
                    {
                        if (f.objectValue.IsSprite())
                        {
                            Sprite sp = null;
                            if (f.objectValue.TryGetValue(ref sp))
                                return sp;
                        }
                    }

                    return default;
                }

                if (f.variableValue == null)
                    return default;
                return f.variableValue.GetSprite();
            }

            return default;
        }

        public static implicit operator ScriptableObject (SceneObjectProperty f)
        {
            if (f != null)
            {
                if (f.type is 0 or 3)
                {
                    if (f.objectValue != null)
                    {
                        if (f.objectValue.IsScriptableObject())
                        {
                            ScriptableObject so = null;
                            if (f.objectValue.TryGetValue(ref so))
                                return so;
                        }
                    }

                    return default;
                }

                if (f.variableValue == null)
                    return default;
                return f.variableValue.GetScriptableObject();
            }

            return default;
        }

        public static implicit operator Mesh (SceneObjectProperty f)
        {
            if (f != null)
            {
                if (f.type is 0 or 3)
                {
                    if (f.objectValue != null)
                    {
                        if (f.objectValue.IsMesh())
                        {
                            Mesh mes = null;
                            if (f.objectValue.TryGetValue(ref mes))
                                return mes;
                        }
                    }

                    return default;
                }

                if (f.variableValue == null)
                    return default;
                return f.variableValue.GetMesh();
            }

            return default;
        }

        public static implicit operator AudioMixer (SceneObjectProperty f)
        {
            if (f != null)
            {
                if (f.type is 0 or 3)
                {
                    if (f.objectValue != null)
                    {
                        if (f.objectValue.IsAudioMixer())
                        {
                            AudioMixer mixer = null;
                            if (f.objectValue.TryGetValue(ref mixer))
                                return mixer;
                        }
                    }

                    return default;
                }

                if (f.variableValue == null)
                    return default;
                return f.variableValue.GetAudioMixer();
            }

            return default;
        }

        public static implicit operator Component (SceneObjectProperty f)
        {
            if (f != null)
            {
                if (f.type is 0 or 3)
                {
                    if (f.objectValue != null)
                    {
                        if (f.objectValue.IsComponent())
                        {
                            Component comp = null;
                            if (f.objectValue.TryGetValue(ref comp))
                                return comp;
                        }
                    }

                    return default;
                }

                if (f.variableValue == null)
                    return default;
                return f.variableValue.GetComponent();
            }

            return default;
        }

        public static implicit operator SceneObject (SceneObjectProperty f)
        {
            if (f != null)
            {
                if (f.type is 0 or 3)
                {
                    if (f.objectValue != null)
                        return f.objectValue;
                }
                else
                {
                    if (f.variableValue != null)
                        return f.variableValue.sceneObject;
                }
            }

            return default;
        }
        
        public SceneObject GetSceneObject ()
        {
            if (type is 0 or 3)
            {
                if (objectValue != null)
                    return objectValue.ShallowCopy();
            }
            else
            {
                if (variableValue != null)
                    return variableValue.sceneObject.ShallowCopy();
            }

            return null;
        }

        public void SetObjectValue (Component comp)
        {
            objectValue.TrySetValue(comp);
        }

        public void SetObjectValue (GameObject go)
        {
            objectValue.TrySetValue(go);
        }

        public void SetObjectValue (AudioMixer mixer)
        {
            objectValue.TrySetValue(mixer);
        }

        public void SetObjectValue (Material mat)
        {
            objectValue.TrySetValue(mat);
        }

        public void SetObjectValue (Sprite sp)
        {
            objectValue.TrySetValue(sp);
        }

        public void SetObjectValue (ScriptableObject so)
        {
            objectValue.TrySetValue(so);
        }

        public void SetObjectValue (Mesh mes)
        {
            objectValue.TrySetValue(mes);
        }

        public void SetVariableValue (Component value)
        {
            variableValue.SetValue(value);
        }

        public void SetVariableValue (SceneObject value)
        {
            variableValue.SetValue(value);
        }
        
        public void SetVariableValue (Sprite sp)
        {
            variableValue.SetValue(sp);
        }

        public bool IsObjectValueType ()
        {
            return type is 0 or 3;
        }

        public bool IsVariableValueType ()
        {
            return type is 1 or 2;
        }
        
        public string GetVariableName ()
        {
            if (variableValue)
                return variableValue.name;
            return string.Empty;
        }

        public bool Equals (SceneObjectProperty prop)
        {
            SceneObject objA = null;
            if (IsObjectValueType())
            {
                objA = objectValue;
            }
            else
            {
                objA = variableValue.sceneObject;
            }

            SceneObject objB = null;
            if (prop.IsObjectValueType())
            {
                objB = prop.objectValue;
            }
            else
            {
                objB = prop.variableValue.sceneObject;
            }

            if (objA.Equals(objB))
                return true;
            return false;
        }

        public bool IsMatchType ()
        {
            if (type is 1 or 2)
                if (variableValue != null)
                    return objectValue.type == variableValue.sceneObject.type;
            return true;
        }


        public override int GetCustomId ()
        {
            ScriptableObject so = null;
            if (type is 0 or 3)
            {
                if (objectValue != null)
                    if (objectValue.IsScriptableObject())
                        objectValue.TryGetValue(ref so);
            }
            else
            {
                if (variableValue != null)
                    so = variableValue.GetScriptableObject();
            }

            if (so)
            {
                if (so is AttackStatusPack asp)
                    return asp.customId;
            }
            
            return 0;
        }

        public bool IsEmpty
        {
            get
            {
                GameObject go = null;
                if (type is 0 or 3)
                {
                    if (objectValue != null)
                    {
                        if (objectValue.IsGameObject())
                        {
                            if (objectValue.TryGetValue(ref go))
                                return false;
                        }
                        else if (objectValue.IsComponent())
                        {
                            Component comp = null;
                            if (objectValue.TryGetValue(ref comp))
                                return false;
                        }
                        else if (objectValue.IsMaterial())
                        {
                            Material mat = null;
                            if (objectValue.TryGetValue(ref mat))
                                return false;
                        }
                        else if (objectValue.IsSprite())
                        {
                            Sprite sp = null;
                            if (objectValue.TryGetValue(ref sp))
                                return false;
                        }
                        else if (objectValue.IsScriptableObject())
                        {
                            ScriptableObject so = null;
                            if (objectValue.TryGetValue(ref so))
                                return false;
                        }
                        else if (objectValue.IsMesh())
                        {
                            Mesh mes = null;
                            if (objectValue.TryGetValue(ref mes))
                                return false;
                        }
                        else if (objectValue.IsAudioMixer())
                        {
                            AudioMixer mixer = null;
                            if (objectValue.TryGetValue(ref mixer))
                                return false;
                        }
                    }
                }
                else
                {
                    if (variableValue)
                    {
                        return variableValue.IsUnassigned();
                    }
                }

                return true;
            }
        }

        public bool IsNull
        {
            get
            {
                GameObject go = null;
                if (type is 0 or 3)
                {
                    if (objectValue != null)
                    {
                        if (objectValue.IsGameObject())
                        {
                            if (objectValue.TryGetValue(ref go))
                                return false;
                        }
                        else if (objectValue.IsComponent())
                        {
                            Component comp = null;
                            if (objectValue.TryGetValue(ref comp))
                                return false;
                        }
                        else if (objectValue.IsMaterial())
                        {
                            Material mat = null;
                            if (objectValue.TryGetValue(ref mat))
                                return false;
                        }
                        else if (objectValue.IsSprite())
                        {
                            Sprite sp = null;
                            if (objectValue.TryGetValue(ref sp))
                                return false;
                        }
                        else if (objectValue.IsScriptableObject())
                        {
                            ScriptableObject so = null;
                            if (objectValue.TryGetValue(ref so))
                                return false;
                        }
                        else if (objectValue.IsMesh())
                        {
                            Mesh mes = null;
                            if (objectValue.TryGetValue(ref mes))
                                return false;
                        }
                        else if (objectValue.IsAudioMixer())
                        {
                            AudioMixer mixer = null;
                            if (objectValue.TryGetValue(ref mixer))
                                return false;
                        }
                    }
                }
                else
                {
                    if (variableValue)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public string name
        {
            get
            {
                GameObject go = null;
                if (type is 0 or 3)
                {
                    if (objectValue != null)
                    {
                        if (objectValue.IsGameObject())
                        {
                            if (objectValue.TryGetValue(ref go))
                                return go.name;
                        }
                        else if (objectValue.IsComponent())
                        {
                            Component comp = null;
                            if (objectValue.TryGetValue(ref comp))
                                return comp.gameObject.name;
                        }
                        else if (objectValue.IsMaterial())
                        {
                            Material mat = null;
                            if (objectValue.TryGetValue(ref mat))
                                return mat.name;
                        }
                        else if (objectValue.IsSprite())
                        {
                            Sprite sp = null;
                            if (objectValue.TryGetValue(ref sp))
                                return sp.name;
                        }
                        else if (objectValue.IsScriptableObject())
                        {
                            ScriptableObject so = null;
                            if (objectValue.TryGetValue(ref so))
                                return so.name;
                        }
                        else if (objectValue.IsMesh())
                        {
                            Mesh mes = null;
                            if (objectValue.TryGetValue(ref mes))
                                return mes.name;
                        }
                        else if (objectValue.IsAudioMixer())
                        {
                            AudioMixer mixer = null;
                            if (objectValue.TryGetValue(ref mixer))
                                return mixer.name;
                        }
                    }
                }
                else
                {
                    if (variableValue != null)
                    {
                        if (objectValue.IsGameObject())
                        {
                            go = variableValue.GetGameObject();
                            if (go != null)
                                return go.name;
                        }
                        else if (objectValue.IsComponent())
                        {
                            var comp = variableValue.GetComponent();
                            if (comp != null)
                                return comp.gameObject.name;
                        }
                        else if (objectValue.IsMaterial())
                        {
                            var mat = variableValue.GetMaterial();
                            if (mat != null)
                                return mat.name;
                        }
                        else if (objectValue.IsSprite())
                        {
                            var sp = variableValue.GetSprite();
                            if (sp != null)
                                return sp.name;
                        }
                        else if (objectValue.IsScriptableObject())
                        {
                            var so = variableValue.GetScriptableObject();
                            if (so != null)
                                return so.name;
                        }
                        else if (objectValue.IsMesh())
                        {
                            var mes = variableValue.GetMesh();
                            if (mes != null)
                                return mes.name;
                        }
                        else if (objectValue.IsAudioMixer())
                        {
                            var mixer = variableValue.GetAudioMixer();
                            if (mixer != null)
                                return mixer.name;
                        }
                    }
                }

                return string.Empty;
            }
        }

        public string objectName
        {
            get
            {
                GameObject go = null;
                if (type is 0 or 3)
                {
                    if (objectValue != null)
                    {
                        if (objectValue.IsGameObject())
                        {
                            if (objectValue.TryGetValue(ref go))
                                return go.name;
                        }
                        else if (objectValue.IsComponent())
                        {
                            Component comp = null;
                            if (objectValue.TryGetValue(ref comp))
                                return comp.gameObject.name;
                        }
                        else if (objectValue.IsMaterial())
                        {
                            Material mat = null;
                            if (objectValue.TryGetValue(ref mat))
                                return mat.name;
                        }
                        else if (objectValue.IsSprite())
                        {
                            Sprite sp = null;
                            if (objectValue.TryGetValue(ref sp))
                                return sp.name;
                        }
                        else if (objectValue.IsScriptableObject())
                        {
                            ScriptableObject so = null;
                            if (objectValue.TryGetValue(ref so))
                                return so.name;
                        }
                        else if (objectValue.IsMesh())
                        {
                            Mesh mes = null;
                            if (objectValue.TryGetValue(ref mes))
                                return mes.name;
                        }
                        else if (objectValue.IsAudioMixer())
                        {
                            AudioMixer mixer = null;
                            if (objectValue.TryGetValue(ref mixer))
                                return mixer.name;
                        }
                    }
                }
                else
                {
                    if (variableValue != null)
                    {
                        return variableValue.name;
                    }
                }

                return string.Empty;
            }
        }

        public override string ToString ()
        {
            if (type is 0 or 3)
                return objectValue.ToString();
            if (variableValue != null)
                return variableValue.ToString();
            return string.Empty;
        }

        public bool isInited => objectValue.type != SceneObject.ObjectType.None;

#if UNITY_EDITOR
        public string VariableLabel ()
        {
            var label = objectValue.SceneObjectName();
            return label.Equals("HIDE", StringComparison.InvariantCulture) ? string.Empty : label;
        }

        public void ChangeDisplayName (string value)
        {
            objectValue.displayName = value;
        }

        public bool ShowSwitchButton ()
        {
            if (type is 2 or 3)
                return false;
            return true;
        }

        public void AllowObjectOnly ()
        {
            if (type != 3)
            {
                dirty = true;
                type = 3;
            }
        }

        public void AllowVariableOnly ()
        {
            if (type != 2)
            {
                dirty = true;
                type = 2;
            }
        }

        public void AllowAll ()
        {
            if (type == 3)
            {
                dirty = true;
                type = 0;
            }
            else if (type == 2)
            {
                dirty = true;
                type = 1;
            }
        }

        public string GetMismatchWarningMessage ()
        {
            return "The assigned variable is not match the require type : " + objectValue.type;
        }

        public bool IsShowMismatchWarning ()
        {
            return !IsMatchType();
        }

        private void CheckSceneObjectDirty ()
        {
            if (objectValue.dirty)
            {
                objectValue.dirty = false;
                dirty = true;
            }
        }

        private void MarkDirty ()
        {
            dirty = true;
        }

        private void SwitchToVariable ()
        {
            dirty = true;
            type = 1;
        }

        private void SwitchToObject ()
        {
            dirty = true;
            type = 0;
        }

        private void CreateSceneObjectVariable ()
        {
            variableValue = SceneObjectVariable.CreateNew(variableValue, objectValue.type);
            dirty = true;
        }
#endif
    }
}