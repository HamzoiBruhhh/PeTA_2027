using System;
using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class TransformBehaviourNode : BehaviourNode
    {
        [Serializable]
        public class VectorElement : IClone<VectorElement>
        {
            [HideLabel]
            [HorizontalGroup(width: 10)]
            [OnValueChanged("MarkDirty")]
            public bool enabled;

            [HideLabel]
            [HorizontalGroup]
            [DisableIf("DisableValue")]
            [InlineProperty]
            [OnInspectorGUI("@MarkPropertyDirty(value)")]
            public FloatProperty value;

            [HorizontalGroup]
            [DisableIf("DisableLinkedValue")]
            [HideIf("HideLinkedValue")]
            [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(linkedValue)")]
            [InfoBox("@linkedValue.GetMismatchWarningMessage()", InfoMessageType.Error, "@linkedValue.IsShowMismatchWarning()")]
            public SceneObjectProperty linkedValue = new SceneObjectProperty(SceneObject.ObjectType.Transform, "HIDE");

            public Transform transform => (Transform) linkedValue;

            public VectorElement ShallowCopy ()
            {
                var e = new VectorElement();
                e.enabled = enabled;
                e.value = value.ShallowCopy();
                e.linkedValue = linkedValue.ShallowCopy();
                return e;
            }

#if UNITY_EDITOR
            [HideInInspector]
            public bool dirty;

            public void MarkDirty ()
            {
                dirty = true;
            }

            private bool DisableLinkedValue ()
            {
                if (!enabled)
                    return true;
                return false;
            }
            
            private bool HideLinkedValue ()
            {
                return !linkedValue.isInited;
            }

            private bool DisableValue ()
            {
                if (!enabled)
                    return true;
                if (!linkedValue.IsNull)
                    return true;
                return false;
            }

            protected void MarkPropertyDirty (SceneObjectProperty p)
            {
                if (p.dirty)
                {
                    p.dirty = false;
                    MarkDirty();
                }
            }

            protected void MarkPropertyDirty (FloatProperty f)
            {
                if (f.dirty)
                {
                    f.dirty = false;
                    MarkDirty();
                }
            }
#endif
        }

        public enum ExecutionType
        {
            None,
            SetGlobalPosition = 10,
            SetLocalPosition = 11,
            AddGlobalPosition = 12,
            AddLocalPosition = 13,
            GetGlobalPosition = 14,
            GetLocalPosition = 15,
            SetGlobalRotation = 50,
            SetLocalRotation = 51,
            AddGlobalRotation = 52,
            AddLocalRotation = 53,
            SetGlobalScale = 90,
            SetLocalScale = 91,
            AddGlobalScale = 92,
            AddLocalScale = 93,
            LookAt = 150,
            LimitFacing = 160,
            CancelLimitFacing = 161,
        }

        [SerializeField]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(transform)")]
        [InlineButton("@transform.SetObjectValue(AssignComponent<UnityEngine.Transform>())", "♺", ShowIf = "@transform.IsObjectValueType()")]
        [InfoBox("@transform.GetMismatchWarningMessage()", InfoMessageType.Error, "@transform.IsShowMismatchWarning()")]
        private SceneObjectProperty transform = new SceneObjectProperty(SceneObject.ObjectType.Transform);

        [SerializeField]
        [OnValueChanged("OnChangeType")]
        [LabelText("Execution")]
        private ExecutionType executionType;

        [SerializeField]
        [BoxGroup("X", LabelText = "@XLabel()")]
        [HideLabel]
        [OnInspectorGUI("UpdateValueX")]
        [HideIf("@executionType==ExecutionType.None || executionType==ExecutionType.LimitFacing || executionType==ExecutionType.CancelLimitFacing")]
        [InlineButton("@x.linkedValue.SetObjectValue(AssignComponent<UnityEngine.Transform>())", "♺", ShowIf = "@x.linkedValue.IsObjectValueType()")]
        private VectorElement x;

        [SerializeField]
        [BoxGroup("Y", LabelText = "@YLabel()")]
        [HideLabel]
        [OnInspectorGUI("UpdateValueY")]
        [HideIf("@executionType==ExecutionType.None || executionType==ExecutionType.LimitFacing || executionType==ExecutionType.CancelLimitFacing")]
        [InlineButton("@y.linkedValue.SetObjectValue(AssignComponent<UnityEngine.Transform>())", "♺", ShowIf = "@y.linkedValue.IsObjectValueType()")]
        private VectorElement y;

        [SerializeField]
        [BoxGroup("Z", LabelText = "@ZLabel()")]
        [HideLabel]
        [OnInspectorGUI("UpdateValueZ")]
        [HideIf("@executionType==ExecutionType.None || executionType==ExecutionType.LimitFacing || executionType==ExecutionType.CancelLimitFacing")]
        [InlineButton("@z.linkedValue.SetObjectValue(AssignComponent<UnityEngine.Transform>())", "♺", ShowIf = "@z.linkedValue.IsObjectValueType()")]
        private VectorElement z;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.LimitFacing")]
        [InfoBox("The assigned list must be number type!", InfoMessageType.Warning, "ShowListWarning", GUIAlwaysEnabled = true)]
        private VariableList listVariable;
        
        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (transform.IsEmpty || !transform.IsMatchType() || executionType == ExecutionType.None)
            {
                LogWarning("Found an empty Transform Behaviour node in " + context.objectName);
            }
            else
            {
                var trans = (Transform) transform;
                if (executionType is ExecutionType.SetGlobalPosition or ExecutionType.AddGlobalPosition)
                {
                    if (x.enabled && y.enabled && z.enabled)
                    {
                        Vector3 value = Vector3.one;
                        value.x = x.transform == null ? x.value : x.transform.position.x;
                        value.y = y.transform == null ? y.value : y.transform.position.y;
                        value.z = z.transform == null ? z.value : z.transform.position.z;
                        if (executionType is ExecutionType.SetGlobalPosition)
                            trans.SetPosition(value);
                        else if (executionType is ExecutionType.AddGlobalPosition)
                            trans.SetPosition(trans.position + value);
                    }
                    else
                    {
                        if (x.enabled)
                        {
                            var xValue = x.transform == null ? x.value : x.transform.position.x;
                            if (executionType is ExecutionType.SetGlobalPosition)
                                trans.SetPositionX(xValue);
                            else if (executionType is ExecutionType.AddGlobalPosition)
                                trans.SetPositionX(trans.position.x + xValue);
                        }

                        if (y.enabled)
                        {
                            var yValue = y.transform == null ? y.value : y.transform.position.y;
                            if (executionType is ExecutionType.SetGlobalPosition)
                                trans.SetPositionY(yValue);
                            else if (executionType is ExecutionType.AddGlobalPosition)
                                trans.SetPositionY(trans.position.y + yValue);
                        }

                        if (z.enabled)
                        {
                            var zValue = z.transform == null ? z.value : z.transform.position.z;
                            if (executionType is ExecutionType.SetGlobalPosition)
                                trans.SetPositionZ(zValue);
                            else if (executionType is ExecutionType.AddGlobalPosition)
                                trans.SetPositionZ(trans.position.z + zValue);
                        }
                    }
                }
                else if (executionType is ExecutionType.GetGlobalPosition or ExecutionType.GetLocalPosition)
                {
                    if (x is {value: { }} && x.value.IsVariable() && x.value.IsAssigned() )
                        x.value.SetVariableValue(executionType == ExecutionType.GetGlobalPosition ? trans.position.x : trans.localPosition.x);
                    if (y is {value: { }} && y.value.IsVariable() && y.value.IsAssigned())
                    {
                        var yy = executionType == ExecutionType.GetGlobalPosition ? trans.position.y : trans.localPosition.y;
                        y.value.SetVariableValue(yy);
                    }
                    if (z is {value: { }} && z.value.IsVariable() && z.value.IsAssigned() )
                        z.value.SetVariableValue(executionType == ExecutionType.GetGlobalPosition ? trans.position.z : trans.localPosition.z);
                }
                else if (executionType is ExecutionType.SetLocalPosition or ExecutionType.AddLocalPosition)
                {
                    if (x.enabled && y.enabled && z.enabled)
                    {
                        Vector3 value = Vector3.one;
                        value.x = x.transform == null ? x.value : x.transform.localPosition.x;
                        value.y = y.transform == null ? y.value : y.transform.localPosition.y;
                        value.z = z.transform == null ? z.value : z.transform.localPosition.z;
                        if (executionType is ExecutionType.SetLocalPosition)
                            trans.SetLocalPosition(value);
                        else if (executionType is ExecutionType.AddLocalPosition)
                            trans.SetLocalPosition(trans.localPosition + value);
                    }
                    else
                    {
                        if (x.enabled)
                        {
                            var xValue = x.transform == null ? x.value : x.transform.localPosition.x;
                            if (executionType is ExecutionType.SetLocalPosition)
                                trans.SetLocalPositionX(xValue);
                            else if (executionType is ExecutionType.AddLocalPosition)
                                trans.SetLocalPositionX(trans.localPosition.x + xValue);
                        }

                        if (y.enabled)
                        {
                            var yValue = y.transform == null ? y.value : y.transform.localPosition.y;
                            if (executionType is ExecutionType.SetLocalPosition)
                                trans.SetLocalPositionY(yValue);
                            else if (executionType is ExecutionType.AddLocalPosition)
                                trans.SetLocalPositionY(trans.localPosition.y + yValue);
                        }

                        if (z.enabled)
                        {
                            var zValue = z.transform == null ? z.value : z.transform.localPosition.z;
                            if (executionType is ExecutionType.SetLocalPosition)
                                trans.SetLocalPositionZ(zValue);
                            else if (executionType is ExecutionType.AddLocalPosition)
                                trans.SetLocalPositionZ(trans.localPosition.z + zValue);
                        }
                    }
                }
                else if (executionType is ExecutionType.SetGlobalRotation or ExecutionType.AddGlobalRotation)
                {
                    if (x.enabled && y.enabled && z.enabled)
                    {
                        Vector3 value = Vector3.one;
                        value.x = x.transform == null ? x.value : x.transform.eulerAngles.x;
                        value.y = y.transform == null ? y.value : y.transform.eulerAngles.y;
                        value.z = z.transform == null ? z.value : z.transform.eulerAngles.z;
                        if (executionType is ExecutionType.SetGlobalRotation)
                            trans.SetRotation(value);
                        else if (executionType is ExecutionType.AddGlobalRotation)
                            trans.SetRotation(trans.eulerAngles + value);
                    }
                    else
                    {
                        if (x.enabled)
                        {
                            var xValue = x.transform == null ? x.value : x.transform.eulerAngles.x;
                            if (executionType is ExecutionType.SetGlobalRotation)
                                trans.SetRotationX(xValue);
                            else if (executionType is ExecutionType.AddGlobalRotation)
                                trans.SetRotationX(trans.eulerAngles.x + xValue);
                        }

                        if (y.enabled)
                        {
                            var yValue = y.transform == null ? y.value : y.transform.eulerAngles.y;
                            if (executionType is ExecutionType.SetGlobalRotation)
                                trans.SetRotationY(yValue);
                            else if (executionType is ExecutionType.AddGlobalRotation)
                                trans.SetRotationY(trans.eulerAngles.y + yValue);
                        }

                        if (z.enabled)
                        {
                            var zValue = z.transform == null ? z.value : z.transform.eulerAngles.z;
                            if (executionType is ExecutionType.SetGlobalRotation)
                                trans.SetRotationZ(zValue);
                            else if (executionType is ExecutionType.AddGlobalRotation)
                                trans.SetRotationZ(trans.eulerAngles.z + zValue);
                        }
                    }
                }
                else if (executionType is ExecutionType.SetLocalRotation or ExecutionType.AddLocalRotation)
                {
                    if (x.enabled && y.enabled && z.enabled)
                    {
                        Vector3 value = Vector3.one;
                        value.x = x.transform == null ? x.value : x.transform.localEulerAngles.x;
                        value.y = y.transform == null ? y.value : y.transform.localEulerAngles.y;
                        value.z = z.transform == null ? z.value : z.transform.localEulerAngles.z;
                        if (executionType is ExecutionType.SetLocalRotation)
                            trans.SetLocalRotation(value);
                        else if (executionType is ExecutionType.AddLocalRotation)
                            trans.SetLocalRotation(trans.localEulerAngles + value);
                    }
                    else
                    {
                        if (x.enabled)
                        {
                            var xValue = x.transform == null ? x.value : x.transform.localEulerAngles.x;
                            if (executionType is ExecutionType.SetLocalRotation)
                                trans.SetLocalRotationX(xValue);
                            else if (executionType is ExecutionType.AddLocalRotation)
                                trans.SetLocalRotationX(trans.localEulerAngles.x + xValue);
                        }

                        if (y.enabled)
                        {
                            var yValue = y.transform == null ? y.value : y.transform.localEulerAngles.y;
                            if (executionType is ExecutionType.SetLocalRotation)
                                trans.SetLocalRotationY(yValue);
                            else if (executionType is ExecutionType.AddLocalRotation)
                                trans.SetLocalRotationY(trans.localEulerAngles.y + yValue);
                        }

                        if (z.enabled)
                        {
                            var zValue = z.transform == null ? z.value : z.transform.localEulerAngles.z;
                            if (executionType is ExecutionType.SetLocalRotation)
                                trans.SetLocalRotationZ(zValue);
                            else if (executionType is ExecutionType.AddLocalRotation)
                                trans.SetLocalRotationZ(trans.localEulerAngles.z + zValue);
                        }
                    }
                }
                else if (executionType is ExecutionType.SetGlobalScale or ExecutionType.AddGlobalScale)
                {
                    if (x.enabled && y.enabled && z.enabled)
                    {
                        Vector3 value = Vector3.one;
                        value.x = x.transform == null ? x.value : x.transform.lossyScale.x;
                        value.y = y.transform == null ? y.value : y.transform.lossyScale.y;
                        value.z = z.transform == null ? z.value : z.transform.lossyScale.z;
                        if (executionType is ExecutionType.SetGlobalScale)
                            trans.SetScale(value);
                        else if (executionType is ExecutionType.AddGlobalScale)
                            trans.SetScale(trans.lossyScale + value);
                    }
                    else
                    {
                        if (x.enabled)
                        {
                            var xValue = x.transform == null ? x.value : x.transform.lossyScale.x;
                            if (executionType is ExecutionType.SetGlobalScale)
                                trans.SetScaleX(xValue);
                            else if (executionType is ExecutionType.AddGlobalScale)
                                trans.SetScaleX(trans.lossyScale.x + xValue);
                        }

                        if (y.enabled)
                        {
                            var yValue = y.transform == null ? y.value : y.transform.lossyScale.y;
                            if (executionType is ExecutionType.SetGlobalScale)
                                trans.SetScaleY(yValue);
                            else if (executionType is ExecutionType.AddGlobalScale)
                                trans.SetScaleY(trans.lossyScale.y + yValue);
                        }

                        if (z.enabled)
                        {
                            var zValue = z.transform == null ? z.value : z.transform.lossyScale.z;
                            if (executionType is ExecutionType.SetGlobalScale)
                                trans.SetScaleZ(zValue);
                            else if (executionType is ExecutionType.AddGlobalScale)
                                trans.SetScaleZ(trans.lossyScale.y + zValue);
                        }
                    }
                }
                else if (executionType is ExecutionType.SetLocalScale or ExecutionType.AddLocalScale)
                {
                    if (x.enabled && y.enabled && z.enabled)
                    {
                        Vector3 value = Vector3.one;
                        value.x = x.transform == null ? x.value : x.transform.localScale.x;
                        value.y = y.transform == null ? y.value : y.transform.localScale.y;
                        value.z = z.transform == null ? z.value : z.transform.localScale.z;
                        if (executionType is ExecutionType.SetLocalScale)
                            trans.SetLocalScale(value);
                        else if (executionType is ExecutionType.AddLocalScale)
                            trans.SetLocalScale(trans.localScale + value);
                    }
                    else
                    {
                        if (x.enabled)
                        {
                            var xValue = x.transform == null ? x.value : x.transform.localScale.x;
                            if (executionType is ExecutionType.SetLocalScale)
                                trans.SetLocalScaleX(xValue);
                            else if (executionType is ExecutionType.AddLocalScale)
                                trans.SetLocalScaleX(trans.localScale.x + xValue);
                        }

                        if (y.enabled)
                        {
                            var yValue = y.transform == null ? y.value : y.transform.localScale.y;
                            if (executionType is ExecutionType.SetLocalScale)
                                trans.SetLocalScaleY(yValue);
                            else if (executionType is ExecutionType.AddLocalScale)
                                trans.SetLocalScaleY(trans.localScale.y + yValue);
                        }

                        if (z.enabled)
                        {
                            var zValue = z.transform == null ? z.value : z.transform.localScale.z;
                            if (executionType is ExecutionType.SetLocalScale)
                                trans.SetLocalScaleZ(zValue);
                            else if (executionType is ExecutionType.AddLocalScale)
                                trans.SetLocalScaleZ(trans.localScale.z + zValue);
                        }
                    }
                }
                else if (executionType is ExecutionType.LookAt)
                {
                    Vector3 lookAtPos = Vector3.zero;
                    if (x.enabled)
                        lookAtPos.x = x.transform == null ? x.value : x.transform.position.x;
                    if (y.enabled)
                        lookAtPos.y = y.transform == null ? y.value : y.transform.position.y;
                    if (z.enabled)
                        lookAtPos.z = z.transform == null ? z.value : z.transform.position.z;
                    trans.LookAt(lookAtPos);
                }
                else if (executionType is ExecutionType.LimitFacing)
                {
                    if (listVariable && listVariable is NumberList numbers)
                    {
                        if (!trans.gameObject.TryGetComponent<ModelFacingController>(out var facingController))
                            facingController = trans.gameObject.AddComponent<ModelFacingController>();
                        facingController.SetupAngles(numbers);
                    }
                }
                else if (executionType is ExecutionType.CancelLimitFacing)
                {
                    trans.RemoveComponent<ModelFacingController>();
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private void OnChangeType ()
        {
            if (executionType is ExecutionType.GetGlobalPosition or ExecutionType.GetLocalPosition)
            {
                x.value.AllowVariableOnly();
                if (x.linkedValue == null || x.linkedValue.isInited)
                    x.linkedValue = new SceneObjectProperty();
                x.MarkDirty();
                
                y.value.AllowVariableOnly();
                if (y.linkedValue == null || y.linkedValue.isInited)
                    y.linkedValue = new SceneObjectProperty();
                y.MarkDirty();
                
                z.value.AllowVariableOnly();
                if (z.linkedValue == null || z.linkedValue.isInited)
                    z.linkedValue = new SceneObjectProperty();
                z.MarkDirty();
            }
            else
            {
                if (x.linkedValue is not {isInited: true})
                {
                    x.linkedValue = new SceneObjectProperty(SceneObject.ObjectType.Transform, "HIDE");
                    x.value.AllowAll();
                    x.value.SwitchToFloat();
                    x.MarkDirty();
                }

                if (y.linkedValue is not {isInited: true})
                {
                    y.linkedValue = new SceneObjectProperty(SceneObject.ObjectType.Transform, "HIDE");
                    y.value.AllowAll();
                    y.value.SwitchToFloat();
                    y.MarkDirty();
                }

                if (z.linkedValue is not {isInited: true})
                {
                    z.linkedValue = new SceneObjectProperty(SceneObject.ObjectType.Transform, "HIDE");
                    z.value.AllowAll();
                    z.value.SwitchToFloat();
                    z.MarkDirty();
                }
            }
            
            MarkRepaint();
            MarkDirty();
        }
        
        private bool ShowListWarning ()
        {
            if (listVariable != null && listVariable is NumberList == false)
                return true;
            return false;
        }
        
        private void UpdateValueX ()
        {
            if (x.enabled)
            {
                if (x.transform != null)
                {
                    var value = 0f;
                    if (executionType is ExecutionType.SetGlobalPosition or ExecutionType.AddGlobalPosition )
                    {
                        value = x.transform.position.x;
                    }
                    else if (executionType is ExecutionType.SetLocalPosition or ExecutionType.AddLocalPosition)
                    {
                        value = x.transform.localPosition.x;
                    }
                    else if (executionType is ExecutionType.SetGlobalRotation or ExecutionType.AddGlobalRotation)
                    {
                        value = x.transform.eulerAngles.x;
                    }
                    else if (executionType is ExecutionType.SetLocalRotation or ExecutionType.AddLocalRotation)
                    {
                        value = x.transform.localEulerAngles.x;
                    }
                    else if (executionType is ExecutionType.SetGlobalScale or ExecutionType.AddGlobalScale)
                    {
                        value = x.transform.lossyScale.x;
                    }
                    else if (executionType is ExecutionType.SetLocalScale or ExecutionType.AddLocalScale)
                    {
                        value = x.transform.localScale.x;
                    }

                    x.value = new FloatProperty(value);
                }
            }

            if (x.dirty)
            {
                x.dirty = false;
                MarkDirty();
            }
        }

        private void UpdateValueY ()
        {
            if (y.enabled)
            {
                if (y.transform != null)
                {
                    var value = 0f;
                    if (executionType is ExecutionType.SetGlobalPosition or ExecutionType.AddGlobalPosition )
                    {
                        value = y.transform.position.y;
                    }
                    else if (executionType is ExecutionType.SetLocalPosition or ExecutionType.AddLocalPosition)
                    {
                        value = y.transform.localPosition.y;
                    }
                    else if (executionType is ExecutionType.SetGlobalRotation or ExecutionType.AddGlobalRotation)
                    {
                        value = y.transform.eulerAngles.y;
                    }
                    else if (executionType is ExecutionType.SetLocalRotation or ExecutionType.AddLocalRotation)
                    {
                        value = y.transform.localEulerAngles.y;
                    }
                    else if (executionType is ExecutionType.SetGlobalScale or ExecutionType.AddGlobalScale)
                    {
                        value = y.transform.lossyScale.y;
                    }
                    else if (executionType is ExecutionType.SetLocalScale or ExecutionType.AddLocalScale)
                    {
                        value = y.transform.localScale.y;
                    }

                    y.value = new FloatProperty(value);
                }
            }

            if (y.dirty)
            {
                y.dirty = false;
                MarkDirty();
            }
        }

        private void UpdateValueZ ()
        {
            if (z.enabled)
            {
                if (z.transform != null)
                {
                    var value = 0f;
                    if (executionType is ExecutionType.SetGlobalPosition or ExecutionType.AddGlobalPosition )
                    {
                        value = z.transform.position.z;
                    }
                    else if (executionType is ExecutionType.SetLocalPosition or ExecutionType.AddLocalPosition)
                    {
                        value = z.transform.localPosition.z;
                    }
                    else if (executionType is ExecutionType.SetGlobalRotation or ExecutionType.AddGlobalRotation)
                    {
                        value = z.transform.eulerAngles.z;
                    }
                    else if (executionType is ExecutionType.SetLocalRotation or ExecutionType.AddLocalRotation)
                    {
                        value = z.transform.localEulerAngles.z;
                    }
                    else if (executionType is ExecutionType.SetGlobalScale or ExecutionType.AddGlobalScale)
                    {
                        value = z.transform.lossyScale.z;
                    }
                    else if (executionType is ExecutionType.SetLocalScale or ExecutionType.AddLocalScale)
                    {
                        value = z.transform.localScale.z;
                    }

                    z.value = new FloatProperty(value);
                }
            }

            if (z.dirty)
            {
                z.dirty = false;
                MarkDirty();
            }
        }

        private string XLabel ()
        {
            switch (executionType)
            {
                case ExecutionType.AddGlobalPosition:
                case ExecutionType.AddGlobalRotation:
                case ExecutionType.AddGlobalScale:
                case ExecutionType.AddLocalPosition:
                case ExecutionType.AddLocalRotation:
                case ExecutionType.AddLocalScale:
                    return "+ X";
            }

            return "X";
        }

        private string YLabel ()
        {
            switch (executionType)
            {
                case ExecutionType.AddGlobalPosition:
                case ExecutionType.AddGlobalRotation:
                case ExecutionType.AddGlobalScale:
                case ExecutionType.AddLocalPosition:
                case ExecutionType.AddLocalRotation:
                case ExecutionType.AddLocalScale:
                    return "+ Y";
            }

            return "Y";
        }

        private string ZLabel ()
        {
            switch (executionType)
            {
                case ExecutionType.AddGlobalPosition:
                case ExecutionType.AddGlobalRotation:
                case ExecutionType.AddGlobalScale:
                case ExecutionType.AddLocalPosition:
                case ExecutionType.AddLocalRotation:
                case ExecutionType.AddLocalScale:
                    return "+ Z";
            }

            return "Z";
        }

        public static string displayName = "Transform Behaviour Node";
        public static string nodeName = "Transform";

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
            return nodeName;
        }

        public override string GetNodeViewDescription ()
        {
            if (!transform.IsNull && transform.IsMatchType())
            {
                string message = "Set ";
                switch (executionType)
                {
                    case ExecutionType.AddGlobalPosition:
                    case ExecutionType.AddGlobalRotation:
                    case ExecutionType.AddGlobalScale:
                    case ExecutionType.AddLocalPosition:
                    case ExecutionType.AddLocalRotation:
                    case ExecutionType.AddLocalScale:
                        message = "Add ";
                        break;
                    case ExecutionType.LookAt:
                        message = "Make ";
                        break;
                }

                if (executionType is ExecutionType.SetGlobalPosition or ExecutionType.AddGlobalPosition)
                {
                    message += $"{transform.name}'s global position to ";
                }
                else if (executionType is ExecutionType.SetLocalPosition or ExecutionType.AddLocalPosition)
                {
                    message += $"{transform.name}'s local position to ";
                }
                else if (executionType is ExecutionType.GetGlobalPosition)
                {
                    return $"Get {transform.name}'s global position";
                }
                else if (executionType is ExecutionType.GetLocalPosition)
                {
                    return $"Get {transform.name}'s local position";
                }
                else if (executionType is ExecutionType.SetGlobalRotation or ExecutionType.AddGlobalRotation)
                {
                    message += $"{transform.name}'s global rotation to ";
                }
                else if (executionType is ExecutionType.SetLocalRotation or ExecutionType.AddLocalRotation)
                {
                    message += $"{transform.name}'s local rotation to ";
                }
                else if (executionType is ExecutionType.SetGlobalScale or ExecutionType.AddGlobalScale)
                {
                    message += $"{transform.name}'s global scale to ";
                }
                else if (executionType is ExecutionType.SetLocalScale or ExecutionType.AddLocalScale)
                {
                    message += $"{transform.name}'s local scale to ";
                }
                else if (executionType is ExecutionType.LimitFacing)
                {
                    if (listVariable != null && !ShowListWarning())
                        return $"{transform.name} limit facing by {listVariable.name}";
                    return string.Empty;
                }
                else if (executionType is ExecutionType.CancelLimitFacing)
                {
                    return $"{transform.name} cancel limit facing";
                }

                if (!x.enabled)
                    message += "-,";
                else if (x.transform != null)
                    message += x.transform.gameObject.name + "'x,";
                else
                    message += x.value + ",";
                if (!y.enabled)
                    message += "-,";
                else if (y.transform != null)
                    message += y.transform.gameObject.name + "'y,";
                else
                    message += y.value + ",";
                if (!z.enabled)
                    message += "-";
                else if (z.transform != null)
                    message += z.transform.gameObject.name + "'z,";
                else
                    message += z.value;
                return message;
            }

            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will provide several controls (position, rotate, scale, look at) to a specific Transform.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}