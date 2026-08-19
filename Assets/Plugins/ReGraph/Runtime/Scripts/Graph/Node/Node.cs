using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    public abstract class Node
    {
        public enum State
        {
            None = 0,
            Running = 1,
            Failure = 50,
            Stop = 60,
            Success = 100,
            Break = 1000
        }

        [HideIf("HideEnable")]
        [OnValueChanged("OnEnableChange")]
        [InlineButton("ShowAdvanceSettings", "≡")]
        public bool enabled = true;

        [ShowIf("showAdvanceSettings"), BoxGroup("Debug Info")]
        [ReadOnly]
        [PropertyOrder(-50)]
        public string guid = System.Guid.NewGuid().ToString();

        [ShowIf("showAdvanceSettings"), BoxGroup("Debug Info")]
        [PropertyOrder(-51)]
        [LabelText("Print Log")]
        public bool printDebugLog = true;

        [HideInInspector]
        public Vector2 position;

        [HideInInspector]
        public bool dirty;

        [HideInInspector]
        public bool forceRepaint;

#if UNITY_EDITOR
        protected void MarkDirty ()
        {
            dirty = true;
        }

        protected bool MarkPropertyDirty (FloatProperty p)
        {
            if (p.dirty)
            {
                p.dirty = false;
                MarkDirty();
                return true;
            }

            return false;
        }

        protected bool MarkPropertyDirty (StringProperty p)
        {
            if (p.dirty)
            {
                p.dirty = false;
                MarkDirty();
                return true;
            }

            return false;
        }

        protected bool MarkPropertyDirty (SceneObjectProperty p)
        {
            if (p.dirty)
            {
                p.dirty = false;
                MarkDirty();
                return true;
            }

            return false;
        }

        protected void MarkRepaint ()
        {
            forceRepaint = true;
        }

        public void OnEnableChange ()
        {
            MarkDirty();
            onEnableChange?.Invoke();
        }

        private bool HideEnable ()
        {
            var classStr = GetType().ToString();
            if (classStr.Contains("RootNode"))
                return true;
            if (classStr.Contains("NoteBehaviourNode"))
                return true;
            if (classStr.Contains("ConnectorBehaviourNode"))
                return true;
            return false;
        }

        public virtual void OnPrintFlow (int state)
        {
            
        }

        private void ShowAdvanceSettings ()
        {
            showAdvanceSettings = !showAdvanceSettings;
        }
#endif
        public delegate void NodeDelegate ();

        public event NodeDelegate onEnableChange;
        protected bool showAdvanceSettings;

        public State Update (GraphExecution execution, int updateId)
        {
            if (!enabled)
                return OnDisabled(execution, updateId);

            bool started = execution.variables.GetStarted(guid, false);
            if (!started)
            {
#if UNITY_EDITOR
                OnPrintFlow(1);
#endif
                OnStart(execution, updateId);
                execution.variables.SetStarted(guid, true);
            }

            State state = OnUpdate(execution, updateId);

            if (state != State.Running)
            {
                OnStop(execution, updateId);
                execution.variables.SetStarted(guid, false);
            }

            return state;
        }

        public void Init ()
        {
            OnInit();
        }

        public void Reset ()
        {
            OnReset();
        }

        public void Pause (GraphExecution execution)
        {
            OnPause(execution);
        }

        public void Unpause (GraphExecution execution)
        {
            OnUnpause(execution);
        }

        protected void LogWarning (string message)
        {
            if (printDebugLog)
                ReDebug.LogWarning("Graph Node", $@"{message} [{guid}]");
        }

#if UNITY_EDITOR
        public virtual void OnDrawGizmos () { }
        public virtual void OnUpdateGraphId (string previousId, string newId) { }
        public virtual void OnClone (GraphNode selectedNode) { }
        public virtual void OnDelete () { }
#endif

        protected abstract void OnInit ();
        protected abstract void OnReset ();
        protected abstract void OnPause (GraphExecution execution);
        protected abstract void OnUnpause (GraphExecution execution);
        protected abstract void OnStart (GraphExecution execution, int updateId);
        protected abstract void OnStop (GraphExecution execution, int updateId);
        protected abstract State OnDisabled (GraphExecution execution, int updateId);
        protected abstract State OnUpdate (GraphExecution execution, int updateId);
    }
}