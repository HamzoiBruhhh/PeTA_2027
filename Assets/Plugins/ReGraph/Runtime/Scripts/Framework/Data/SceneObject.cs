using System;
using System.Collections;
using Reshape.ReGraph;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Video;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [Serializable]
    public class SceneObject : IClone<SceneObject>
    {
        public const string CHAR_OP = "Character Operator";

        public enum ObjectType
        {
            None = 0,
            GameObject = 100,
            Transform = 1000,
            RectTransform = 1003,
            Camera = 1010,
            Rigidbody = 1020,
            Rigidbody2D = 1025,
            Collider = 1030,
            Collider2D = 1031,
            NavMeshAgent = 1070,
            AudioSource = 1110,
            Animator = 1130,
            Light = 1150,
            ParticleSystem = 1170,
            SpriteRenderer = 1200,
            LineRenderer = 1210,
            TrailRenderer = 1220,
            Renderer = 1225,
            MeshRenderer = 1230,
            SkinnedMeshRenderer = 1235,
            Graphic = 1300,
            Image = 1310,
            Slider = 1320,
            CanvasGroup = 1330,
            LayoutElement = 1340,
            ScrollRect = 1350,
            GraphRunner = 4900,
            Text = 5000,
            TextMesh = 5001,
            TextMeshPro = 5002,
            TextMeshProGUI = 5003,
            TextMeshProText = 5004,
            PathFindAgent = 5030,
            CharacterOperator = 5500,
            VideoController = 5600,
            AudioController = 5601,
            Material = 6000,
            AudioMixer = 6010,
            Mesh = 6020,
            ItemData = 6030,
            Sprite = 6040,
            AttackStatusPack = 6050,
        }

        [SerializeField]
        [ValueDropdown("ObjectTypeChoice")]
        [OnInspectorGUI("DrawComp")]
        [OnValueChanged("TypeChanged")]
        [ShowIf("showType")]
        [EnableIf("EnableType")]
        public ObjectType type;

        [SerializeField]
        [HideInInspector]
        [OnValueChanged("MarkDirty")]
        [EnableIf("EnableType")]
        public GameObject gameObject;

        [SerializeField]
        [ShowIf("ShowAudioMixer")]
        [OnValueChanged("MarkDirty")]
        [EnableIf("EnableType")]
        private AudioMixer audioMixer;

        [SerializeField]
        [ShowIf("ShowMaterial")]
        [OnValueChanged("MarkDirty")]
        [EnableIf("EnableType")]
        private Material material;

        [SerializeField]
        [ShowIf("ShowSprite")]
        [OnValueChanged("MarkDirty")]
        [EnableIf("EnableType")]
        [LabelText("@SpriteLabel()")]
        private Sprite sprite;

        [SerializeField]
        [ShowIf("ShowMesh")]
        [OnValueChanged("MarkDirty")]
        [EnableIf("EnableType")]
        private Mesh mesh;

        [SerializeField]
        [ShowIf("ShowScriptableObject")]
        [OnValueChanged("MarkDirty")]
        [EnableIf("EnableType")]
        [LabelText("@ScriptableObjectLabel()")]
        [ValueDropdown("ScriptableObjectChoice")]
        public ScriptableObject scriptableObject;

        [SerializeField]
        [HideInInspector]
        [OnValueChanged("MarkDirty")]
        public Component component;

        [HideInInspector]
        public bool showType = true;

        [HideInInspector]
        public bool showAsNodeProperty;

        [HideInInspector]
        public string displayName;

        [HideInInspector]
        public bool dirty;

        public SceneObject ShallowCopy ()
        {
            return (SceneObject) this.MemberwiseClone();
        }

        public bool TryGetValue (ref GameObject go)
        {
            if (IsGameObject() && gameObject)
            {
                go = gameObject;
                return true;
            }

            return false;
        }

        public bool TryGetValue (ref ScriptableObject so)
        {
            if (IsScriptableObject() && scriptableObject)
            {
                so = scriptableObject;
                return true;
            }

            return false;
        }

        public bool TryGetValue (ref Component comp)
        {
            if (IsComponent() && component)
            {
                comp = component;
                return true;
            }

            return false;
        }

        public bool TryGetValue (ref Material mat)
        {
            if (IsMaterial() && material)
            {
                mat = material;
                return true;
            }

            return false;
        }

        public bool TryGetValue (ref Sprite sp)
        {
            if (IsSprite() && sprite != null)
            {
                sp = sprite;
                return true;
            }

            return false;
        }

        public bool TryGetValue (ref Mesh mes)
        {
            if (IsMesh() && mesh)
            {
                mes = mesh;
                return true;
            }

            return false;
        }

        public bool TryGetValue (ref AudioMixer mixer)
        {
            if (IsAudioMixer() && audioMixer)
            {
                mixer = audioMixer;
                return true;
            }

            return false;
        }

        public bool TrySetValue (GameObject go)
        {
            if (IsGameObject() && go)
            {
                gameObject = go;
                MarkDirty();
                return true;
            }

            return false;
        }

        public bool TrySetValue (Component comp)
        {
            if (IsComponent() && comp)
            {
                if (type == ObjectType.CharacterOperator)
                {
                    var charOp = comp as CharacterOperator;
                    if (charOp)
                    {
                        component = comp;
                        MarkDirty();
                        return true;
                    }
                }
                else if (type == ObjectType.VideoController)
                {
                    var vc = comp as VideoController;
                    if (vc)
                    {
                        component = comp;
                        MarkDirty();
                        return true;
                    }
                }
                else if (type == ObjectType.AudioController)
                {
                    var ac = comp as AudioController;
                    if (ac)
                    {
                        component = comp;
                        MarkDirty();
                        return true;
                    }
                }
                else if (ComponentType() == comp.GetType())
                {
                    component = comp;
                    MarkDirty();
                    return true;
                }
                else if ((comp.GetType() == typeof(TextMeshProUGUI) || comp.GetType() == typeof(TextMeshPro)) && ComponentType() == typeof(TMP_Text))
                {
                    component = (TMP_Text) comp;
                    MarkDirty();
                    return true;
                }
                else if (comp is BoxCollider or SphereCollider or MeshCollider or CapsuleCollider && ComponentType() == typeof(Collider))
                {
                    component = (Collider) comp;
                    MarkDirty();
                    return true;
                }
                else if (comp is MeshRenderer or SkinnedMeshRenderer && ComponentType() == typeof(Renderer))
                {
                    component = (Renderer) comp;
                    MarkDirty();
                    return true;
                }
                else if (comp is BoxCollider2D or CircleCollider2D or CompositeCollider2D or CapsuleCollider2D && ComponentType() == typeof(Collider2D))
                {
                    component = (Collider2D) comp;
                    MarkDirty();
                    return true;
                }
            }

            return false;
        }

        public bool TrySetValue (Material mat)
        {
            if (IsMaterial() && mat != null)
            {
                material = mat;
                MarkDirty();
                return true;
            }

            return false;
        }

        public bool TrySetValue (Sprite sp)
        {
            if (IsSprite() && sp != null)
            {
                sprite = sp;
                MarkDirty();
                return true;
            }

            return false;
        }

        public bool TrySetValue (ScriptableObject so)
        {
            if (IsScriptableObject() && so != null)
            {
                scriptableObject = so;
                MarkDirty();
                return true;
            }

            return false;
        }

        public bool TrySetValue (Mesh mes)
        {
            if (IsMesh() && mes != null)
            {
                mesh = mes;
                MarkDirty();
                return true;
            }

            return false;
        }

        public bool TrySetValue (AudioMixer mixer)
        {
            if (IsAudioMixer() && mixer != null)
            {
                audioMixer = mixer;
                MarkDirty();
                return true;
            }

            return false;
        }

        public void Reset ()
        {
            gameObject = null;
            component = null;
            material = null;
            sprite = null;
            mesh = null;
            audioMixer = null;
            scriptableObject = null;
        }
        
        public void EditorReset (Sprite sp)
        {
            gameObject = null;
            component = null;
            material = null;
            sprite = sp;
            mesh = null;
            audioMixer = null;
            scriptableObject = null;
        }

        public bool Equals (SceneObject obj)
        {
            if (type != obj.type)
                return false;
            if (IsGameObject())
            {
                if (!gameObject) return false;
                GameObject go = null;
                if (obj.TryGetValue(ref go))
                    if (gameObject == go)
                        return true;
            }
            else if (IsMaterial())
            {
                if (!material) return false;
                Material mat = null;
                if (obj.TryGetValue(ref mat))
                    if (material == mat)
                        return true;
            }
            else if (IsSprite())
            {
                if (!sprite) return false;
                Sprite sp = null;
                if (obj.TryGetValue(ref sp))
                    if (sprite == sp)
                        return true;
            }
            else if (IsScriptableObject())
            {
                if (!scriptableObject) return false;
                ScriptableObject so = null;
                if (obj.TryGetValue(ref so))
                    if (scriptableObject == so)
                        return true;
            }
            else if (IsMesh())
            {
                if (!mesh) return false;
                Mesh mes = null;
                if (obj.TryGetValue(ref mes))
                    if (mesh == mes)
                        return true;
            }
            else if (IsAudioMixer())
            {
                if (!audioMixer) return false;
                AudioMixer am = null;
                if (obj.TryGetValue(ref am))
                    if (audioMixer == am)
                        return true;
            }
            else if (IsComponent())
            {
                if (!component) return false;
                Component comp = null;
                if (obj.TryGetValue(ref comp))
                    if (component == comp)
                        return true;
            }

            return false;
        }

        public override string ToString ()
        {
            if (IsGameObject() && gameObject != null)
                return gameObject.ToString();
            if (IsMaterial() && material != null)
                return material.ToString();
            if (IsSprite() && sprite != null)
                return sprite.ToString();
            if (IsScriptableObject() && scriptableObject != null)
                return scriptableObject.ToString();
            if (IsMesh() && mesh != null)
                return mesh.ToString();
            if (IsAudioMixer() && audioMixer != null)
                return audioMixer.ToString();
            if (IsComponent() && component != null)
                return component.ToString();
            return string.Empty;
        }

        public bool IsGameObject ()
        {
            return type == ObjectType.GameObject;
        }

        public bool IsMaterial ()
        {
            return type == ObjectType.Material;
        }

        public bool IsScriptableObject ()
        {
            return type is ObjectType.ItemData or ObjectType.AttackStatusPack;
        }

        public bool IsItemData ()
        {
            return type == ObjectType.ItemData;
        }

        public bool IsAttackStatusPack ()
        {
            return type == ObjectType.AttackStatusPack;
        }

        public bool IsSprite ()
        {
            return type == ObjectType.Sprite;
        }

        public bool IsMesh ()
        {
            return type == ObjectType.Mesh;
        }

        public bool IsAudioMixer ()
        {
            if (type == ObjectType.AudioMixer)
                return true;
            return false;
        }

        public bool IsComponent ()
        {
            if (type is ObjectType.None or ObjectType.GameObject)
                return false;
            if (type is ObjectType.Material or ObjectType.AudioMixer or ObjectType.Mesh or ObjectType.ItemData or ObjectType.Sprite or ObjectType.AttackStatusPack)
                return false;
            return true;
        }

        public bool IsCharacterOperator ()
        {
            return type == ObjectType.CharacterOperator;
        }

        private bool HaveDisplayName ()
        {
            return !string.IsNullOrEmpty(displayName);
        }

        private string GetDisplayName ()
        {
            if (displayName.Equals("GameObjectLocation"))
                return "Location";
            if (displayName.Equals("GameObjectParent"))
                return "Parent";
            return displayName;
        }

        public string SceneObjectName ()
        {
            if (HaveDisplayName())
                return GetDisplayName();
            if (type == ObjectType.GameObject)
                return "Game Object";
            if (type == ObjectType.Material)
                return "Material";
            if (type == ObjectType.Mesh)
                return "Mesh";
            if (type == ObjectType.ItemData)
                return "Item";
            if (type == ObjectType.AttackStatusPack)
                return "Attack Status";
            if (type == ObjectType.Sprite)
                return "Sprite";
            if (type == ObjectType.AudioMixer)
                return "Audio Mixer";
            if (type == ObjectType.None)
                return "None";
            return ComponentName();
        }

        public Type ComponentType ()
        {
            if (type == ObjectType.Transform)
                return typeof(Transform);
            if (type == ObjectType.RectTransform)
                return typeof(RectTransform);
            if (type == ObjectType.Camera)
                return typeof(Camera);
            if (type == ObjectType.Rigidbody)
                return typeof(Rigidbody);
            if (type == ObjectType.Rigidbody2D)
                return typeof(Rigidbody2D);
            if (type == ObjectType.Collider)
                return typeof(Collider);
            if (type == ObjectType.Collider2D)
                return typeof(Collider2D);
            if (type == ObjectType.NavMeshAgent)
                return typeof(NavMeshAgent);
            if (type == ObjectType.AudioSource)
                return typeof(AudioSource);
            if (type == ObjectType.Animator)
                return typeof(Animator);
            if (type == ObjectType.Light)
                return typeof(Light);
            if (type == ObjectType.ParticleSystem)
                return typeof(ParticleSystem);
            if (type == ObjectType.SpriteRenderer)
                return typeof(SpriteRenderer);
            if (type == ObjectType.LineRenderer)
                return typeof(LineRenderer);
            if (type == ObjectType.TrailRenderer)
                return typeof(TrailRenderer);
            if (type == ObjectType.Renderer)
                return typeof(Renderer);
            if (type == ObjectType.MeshRenderer)
                return typeof(MeshRenderer);
            if (type == ObjectType.SkinnedMeshRenderer)
                return typeof(SkinnedMeshRenderer);
            if (type == ObjectType.Graphic)
                return typeof(Graphic);
            if (type == ObjectType.Image)
                return typeof(Image);
            if (type == ObjectType.Slider)
                return typeof(Slider);
            if (type == ObjectType.CanvasGroup)
                return typeof(CanvasGroup);
            if (type == ObjectType.LayoutElement)
                return typeof(LayoutElement);
            if (type == ObjectType.ScrollRect)
                return typeof(ScrollRect);
            if (type == ObjectType.GraphRunner)
                return typeof(GraphRunner);
            if (type == ObjectType.Text)
                return typeof(Text);
            if (type == ObjectType.TextMesh)
                return typeof(TextMesh);
            if (type == ObjectType.TextMeshProGUI)
                return typeof(TextMeshProUGUI);
            if (type == ObjectType.TextMeshProText)
                return typeof(TMP_Text);
            if (type == ObjectType.TextMeshPro)
                return typeof(TMP_Text);
#if REGRAPH_PATHFIND
            if (type == ObjectType.PathFindAgent)
                return typeof(PathFindAgent);
#endif
            if (type == ObjectType.CharacterOperator)
                return typeof(CharacterOperator);
            if (type == ObjectType.VideoController)
                return typeof(VideoController);
            if (type == ObjectType.AudioController)
                return typeof(AudioController);
            return null;
        }

        public string ComponentName ()
        {
            if (HaveDisplayName())
                return GetDisplayName();
            if (type == ObjectType.Transform)
                return "Transform";
            if (type == ObjectType.RectTransform)
                return "Rect Transform";
            if (type == ObjectType.Camera)
                return "Camera";
            if (type == ObjectType.Rigidbody)
                return "Rigidbody";
            if (type == ObjectType.Rigidbody2D)
                return "Rigidbody 2D";
            if (type == ObjectType.Collider)
                return "Collider";
            if (type == ObjectType.Collider2D)
                return "Collider 2D";
            if (type == ObjectType.NavMeshAgent)
                return "NavMesh Agent";
            if (type == ObjectType.AudioSource)
                return "Audio Source";
            if (type == ObjectType.Animator)
                return "Animator";
            if (type == ObjectType.Light)
                return "Light";
            if (type == ObjectType.ParticleSystem)
                return "Particle System";
            if (type == ObjectType.SpriteRenderer)
                return "Sprite Renderer";
            if (type == ObjectType.LineRenderer)
                return "Line Renderer";
            if (type == ObjectType.TrailRenderer)
                return "Trail Renderer";
            if (type == ObjectType.Renderer)
                return "Renderer";
            if (type == ObjectType.MeshRenderer)
                return "Mesh Renderer";
            if (type == ObjectType.SkinnedMeshRenderer)
                return "Skinned Mesh Renderer";
            if (type == ObjectType.Graphic)
                return "Graphic";
            if (type == ObjectType.Image)
                return "Image";
            if (type == ObjectType.Slider)
                return "Slider";
            if (type == ObjectType.CanvasGroup)
                return "Canvas Group";
            if (type == ObjectType.LayoutElement)
                return "Layout Element";
            if (type == ObjectType.ScrollRect)
                return "Scroll Rect";
            if (type == ObjectType.GraphRunner)
                return "Graph Runner";
            if (type == ObjectType.Text)
                return "Text";
            if (type == ObjectType.TextMesh)
                return "Text Mesh";
            if (type == ObjectType.TextMeshPro)
                return "Text Mesh Pro";
            if (type == ObjectType.TextMeshProGUI)
                return "Text Mesh Pro GUI";
            if (type == ObjectType.TextMeshProText)
                return "Text Mesh Pro Text";
            if (type == ObjectType.PathFindAgent)
                return "PathFind Agent";
            if (type == ObjectType.CharacterOperator)
                return CHAR_OP;
            if (type == ObjectType.VideoController)
                return "Video Controller";
            if (type == ObjectType.AudioController)
                return "Audio Controller";
            return string.Empty;
        }

        public string GameObjectName ()
        {
            if (HaveDisplayName())
                return GetDisplayName();
            if (type == ObjectType.GameObject)
                return "Game Object";
            return string.Empty;
        }

        public string ScriptableObjectName ()
        {
            if (HaveDisplayName())
                return GetDisplayName();
            if (type == ObjectType.ItemData)
                return "Item";
            if (type == ObjectType.AttackStatusPack)
                return "Attack Status";
            return string.Empty;
        }

        private void MarkDirty ()
        {
            dirty = true;
        }

#if UNITY_EDITOR
        private bool EnableType ()
        {
            if (EditorApplication.isPlaying)
                return false;
            return true;
        }
        
        private static IEnumerable ObjectTypeChoice ()
        {
            var list = GetObjectTypeChoice;
            var clone = new ValueDropdownList<ObjectType>();
            foreach (var item in list)
                clone.Add(item.Text, item.Value); 
            clone.Add("Material", ObjectType.Material);
            return clone;
        }

        public static ValueDropdownList<ObjectType> GetObjectTypeChoice = new ValueDropdownList<ObjectType>()
        {
            {"GameObject", ObjectType.GameObject},
            {"Transform", ObjectType.Transform},
            {"Rect Transform", ObjectType.RectTransform},
            {"Camera", ObjectType.Camera},
            {"Rigidbody", ObjectType.Rigidbody},
            {"Rigidbody 2D", ObjectType.Rigidbody2D},
            {"Collider", ObjectType.Collider},
            {"Collider 2D", ObjectType.Collider2D},
            {"NavMesh Agent", ObjectType.NavMeshAgent},
#if REGRAPH_PATHFIND
            {"PathFind Agent", ObjectType.PathFindAgent},
#endif
            {"Audio Source", ObjectType.AudioSource},
            {"Animator", ObjectType.Animator},
            {"Light", ObjectType.Light},
            {"Particle System", ObjectType.ParticleSystem},
            {"Sprite Renderer", ObjectType.SpriteRenderer},
            {"Line Renderer", ObjectType.LineRenderer},
            {"Trail Renderer", ObjectType.TrailRenderer},
            {"Renderer", ObjectType.Renderer},
            {"Mesh Renderer", ObjectType.MeshRenderer},
            {"Skinned Mesh Renderer", ObjectType.SkinnedMeshRenderer},
            {"Sprite", ObjectType.Sprite},
            {"Graphic", ObjectType.Graphic},
            {"Image", ObjectType.Image},
            {"Slider", ObjectType.Slider},
            {"Canvas Group", ObjectType.CanvasGroup},
            {"Layout Element", ObjectType.LayoutElement},
            {"Scroll Rect", ObjectType.ScrollRect},
            {"Graph Runner", ObjectType.GraphRunner},
            {CHAR_OP, ObjectType.CharacterOperator},
            {"Video Controller", ObjectType.VideoController},
            {"Text", ObjectType.Text},
            {"Text Mesh", ObjectType.TextMesh},
            {"Text Mesh Pro", ObjectType.TextMeshPro},
            {"Text Mesh Pro GUI", ObjectType.TextMeshProGUI},
            {"Text Mesh Pro Text", ObjectType.TextMeshProText},
            {"Game Item", ObjectType.ItemData},
            {"Attack Status Pack", ObjectType.AttackStatusPack},
        };

        private void TypeChanged ()
        {
            component = null;
            MarkDirty();
        }

        public bool ShowGo ()
        {
            if (showAsNodeProperty)
                return type == ObjectType.GameObject;
            if (!Application.isPlaying)
                return false;
            return type == ObjectType.GameObject;
        }

        public bool ShowAudioMixer ()
        {
            if (showAsNodeProperty)
                return type == ObjectType.AudioMixer;
            if (!Application.isPlaying)
                return false;
            return type == ObjectType.AudioMixer;
        }

        public bool ShowMaterial ()
        {
            if (showAsNodeProperty)
                return type == ObjectType.Material;
            if (!Application.isPlaying)
                return false;
            return type == ObjectType.Material;
        }

        public bool ShowSprite ()
        {
            if (showAsNodeProperty)
                return type == ObjectType.Sprite;
            return type == ObjectType.Sprite;
        }

        public string SpriteLabel ()
        {
            return displayName.Equals("HIDE", StringComparison.InvariantCulture) ? string.Empty : "Sprite";
        }

        public bool ShowScriptableObject ()
        {
            if (showAsNodeProperty)
                return type is ObjectType.ItemData or ObjectType.AttackStatusPack;
            if (!Application.isPlaying)
                return false;
            return type is ObjectType.ItemData or ObjectType.AttackStatusPack;
        }

        public bool ShowMesh ()
        {
            if (showAsNodeProperty)
                return type == ObjectType.Mesh;
            if (!Application.isPlaying)
                return false;
            return type == ObjectType.Mesh;
        }

        private void DrawComp ()
        {
            if (ShowComp())
            {
                component = (Component) EditorGUILayout.ObjectField(ComponentName(), component, ComponentType(), false);
            }
        }

        private bool ShowComp ()
        {
            if (showAsNodeProperty)
                return IsComponent();
            if (!Application.isPlaying)
                return false;
            if (!IsComponent())
                return false;
            return true;
        }

        private string ScriptableObjectLabel ()
        {
            if (type == ObjectType.ItemData)
                return "Item";
            if (type == ObjectType.AttackStatusPack)
                return "Attack Status";
            return string.Empty;
        }

        private IEnumerable ScriptableObjectChoice ()
        {
            if (type == ObjectType.ItemData)
                return ItemData.GetListDropdown();
            if (type == ObjectType.AttackStatusPack)
                return AttackStatusPack.GetListDropdown();
            return default;
        }

        private void DisableGUIAfter ()
        {
            GUI.enabled = false;
        }
#endif
    }
}