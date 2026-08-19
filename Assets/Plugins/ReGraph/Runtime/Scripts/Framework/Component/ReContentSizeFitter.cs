using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    public class ReContentSizeFitter : ContentSizeFitter
    {

        public Vector2 sizeMin = new Vector2(0f, 0f);
        public Vector2 sizeMax = new Vector2(1920f, 1080f);

        public override void SetLayoutHorizontal ()
        {
            base.SetLayoutHorizontal();
            var rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                var sizeDelta = rectTransform.sizeDelta;
                sizeDelta.x = Mathf.Clamp(sizeDelta.x, sizeMin.x, sizeMax.x);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizeDelta.x);
            }
        }


        public override void SetLayoutVertical ()
        {
            base.SetLayoutVertical();
            var rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                var sizeDelta = rectTransform.sizeDelta;
                sizeDelta.y = Mathf.Clamp(sizeDelta.y, sizeMin.y, sizeMax.y);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sizeDelta.y);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ReContentSizeFitter))]
    public class ReContentSizeFitterEditor : Editor
    {
        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();
        }
    }
#endif
}