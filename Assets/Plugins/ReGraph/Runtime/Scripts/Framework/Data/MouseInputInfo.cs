using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Reshape.ReFramework
{
    [Serializable]
    public class MouseInputInfo
    {
        private static List<RaycastResult> cachedRaycastResult;
        private static PointerEventData eventDataCurrentPosition;

        public RaycastHit currentHitPoint;
        public RaycastHit mouseRightDownHitPoint;
        public RaycastHit mouseLeftDownHitPoint;
        public Vector3 lastMousePosition;
        public Vector3 currentMousePosition;
        public bool haveLeftMouseButtonHitDowned;
        public bool haveRightMouseButtonHitDowned;
        public bool haveMouseMovedDuringLeftButtonDowned;
        public bool haveMouseMovedDuringRightButtonDowned;
        public bool haveLeftMouseButtonDowned;
        public bool haveMiddleMouseButtonDowned;

        private bool? haveHit;
        private bool? haveMouseMoved;
        private bool? haveHitHiddenArea;
        private Vector2 mouseMovingDelta;

        public MouseInputInfo ()
        {
            lastMousePosition = Vector3.positiveInfinity;
            ResetEndOfFrame();
        }

        public static bool IsPointerOverUIObject (Vector2 inputPos)
        {
            if (cachedRaycastResult == null)
                cachedRaycastResult = new List<RaycastResult>();
            else
                cachedRaycastResult.Clear();
            if (EventSystem.current)
            {
                eventDataCurrentPosition ??= new PointerEventData(EventSystem.current);
                eventDataCurrentPosition.position = new Vector2(inputPos.x, inputPos.y);
                EventSystem.current.RaycastAll(eventDataCurrentPosition, cachedRaycastResult);
                var totalResults = cachedRaycastResult.Count;
                return totalResults > 0;
                /*for (var i = 0; i < totalResults; i++)
                    if (cachedRaycastResult[i].gameObject.layer == LayerMask.NameToLayer("UI_Canvas"))
                        return true;*/
            }

            return false;
        }

        public void SetupBeginOfFrame (Camera camara, LayerMask inputLayer, LayerMask hiddenLayer, Vector2 mousePosition)
        {
            currentMousePosition = mousePosition;
            if (camara != null)
            {
                haveHit = Physics.Raycast(camara.ScreenPointToRay(currentMousePosition), out currentHitPoint, camara.farClipPlane, inputLayer);
                haveHitHiddenArea = Physics.Raycast(camara.ScreenPointToRay(currentMousePosition), out _, camara.farClipPlane, hiddenLayer);
            }
        }

        public virtual void ResetEndOfFrame ()
        {
            haveHit = null;
            haveHitHiddenArea = null;
            haveMouseMoved = null;
            mouseMovingDelta = Vector2.positiveInfinity;
            lastMousePosition = currentMousePosition;
        }

        public void DetectLeftMouseMovedDuringButtonDowned (float moveThreshold)
        {
            if (!haveMouseMovedDuringLeftButtonDowned)
                haveMouseMovedDuringLeftButtonDowned = Vector3.Distance(mouseLeftDownHitPoint.point, currentHitPoint.point) >= moveThreshold;
        }
        
        public void DetectRightMouseMovedDuringButtonDowned (float moveThreshold)
        {
            if (!haveMouseMovedDuringRightButtonDowned)
                haveMouseMovedDuringRightButtonDowned = Vector3.Distance(mouseRightDownHitPoint.point, currentHitPoint.point) >= moveThreshold;
        }

        public bool isHit
        {
            get => haveHit is true;
        }
        
        public bool isHitHiddenArea
        {
            get => haveHitHiddenArea is true;
        }

        public bool isMouseMoved
        {
            get
            {
                if (haveMouseMoved == null)
                    haveMouseMoved = Vector3.Distance(lastMousePosition, currentMousePosition) > 0;
                return haveMouseMoved is true;
            }
        }

        public Vector2 mouseMoveDelta
        {
            get
            {
                if (float.IsPositiveInfinity(mouseMovingDelta.x))
                {
                    mouseMovingDelta.x = lastMousePosition.x - currentMousePosition.x;
                    mouseMovingDelta.y = lastMousePosition.y - currentMousePosition.y;
                }

                return (Vector2) mouseMovingDelta;
            }
        }
    }
}