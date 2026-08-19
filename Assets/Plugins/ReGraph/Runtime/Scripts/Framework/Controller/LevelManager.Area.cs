using System;
using System.Collections.Generic;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    public partial class LevelManager
    {
       [Serializable]
        private class AreaObject
        {
            public readonly GameObject go;
            public readonly int areaId;

            private IVisible visibleGo;
            private bool visibleGoDetected;

            public AreaObject (GameObject obj, int area)
            {
                go = obj;
                areaId = area;
            }

            public bool Match (GameObject obj)
            {
                return go == obj;
            }
            
            public void Hide ()
            {
                DetectVisibleGo();
                if (visibleGo != null)
                    visibleGo.SetVisibility(false);
                else
                    go.SetActiveOpt(false);
            }

            public void ShowArea (int area)
            {
                if (areaId == area)
                {
                    DetectVisibleGo();
                    if (visibleGo != null)
                        visibleGo.SetVisibility(true);
                    else
                        go.SetActiveOpt(true);
                }
            }
            
            public void HideArea (int area)
            {
                if (areaId == area)
                {
                    DetectVisibleGo();
                    if (visibleGo != null)
                        visibleGo.SetVisibility(false);
                    else
                        go.SetActiveOpt(false);
                }
            }

            private void DetectVisibleGo ()
            {
                if (!visibleGoDetected)
                {
                    if (go.TryGetComponent<IVisible>(out var visible))
                        visibleGo = visible;
                    visibleGoDetected = true;
                }
            }
        }

        [ShowInInspector, ReadOnly, HideInEditorMode]
        private List<AreaObject> areaObjects;
        
        [ShowInInspector, ReadOnly, HideInEditorMode]
        private int currentArea;
        
        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static void InsertAreaObject (GameObject trans)
        {
            if (me)
            {
                me.CleanAreaObject();
                me.AddAreaObject(trans);
            }
        }
        
        public static void DeleteAreaObject (GameObject trans)
        {
            if (me)
            {
                me.RemoveAreaObject(trans);
                me.CleanAreaObject();
            }
        }
        
        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public void EnterArea (NumberVariable variable)
        {
            EnterArea((int)variable);
        }
        
        public void EnterArea (int area)
        {
            HideAllAreaObjects();
            currentArea = area;
            ShowAreaObjects();
        }
        
        public void ExitArea (NumberVariable variable)
        {
            ExitArea((int)variable);
        }
        
        public void ExitArea (int area)
        {
            currentArea = 0;
            HideAreaObjects();
        }
        
        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void ShowAreaObjects ()
        {
            if (areaObjects != null)
                for (var i = 0; i < areaObjects.Count; i++)
                    areaObjects[i].ShowArea(currentArea);
        }
        
        private void HideAreaObjects ()
        {
            if (areaObjects != null)
                for (var i = 0; i < areaObjects.Count; i++)
                    areaObjects[i].HideArea(currentArea);
        }
        
        private void HideAllAreaObjects ()
        {
            if (areaObjects != null)
                for (var i = 0; i < areaObjects.Count; i++)
                    areaObjects[i].Hide();
        }

        private void AddAreaObject (GameObject go)
        {
            areaObjects ??= new List<AreaObject>();
            if (currentArea != 0 && go)
                if (!HaveAreaObject(go, out _))
                    areaObjects.Add(new AreaObject(go, currentArea));
        }
        
        private void RemoveAreaObject (GameObject go)
        {
            areaObjects ??= new List<AreaObject>();
            if (go)
                if (HaveAreaObject(go, out var existed))
                    areaObjects.Remove(existed);
        }
        
        private void CleanAreaObject ()
        {
            areaObjects?.RemoveAll(areaObject => !areaObject.go);
        }
        
        private void ClearArea ()
        {
            areaObjects?.Clear();
            areaObjects = null;
        }

        private bool HaveAreaObject (GameObject go, out AreaObject ao)
        {
            ao = null;
            if (areaObjects != null)
            {
                for (var i = 0; i < areaObjects.Count; i++)
                {
                    if (areaObjects[i].Match(go))
                    {
                        ao = areaObjects[i];
                        return true;
                    }
                }
            }

            return false;
        }
        
        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}