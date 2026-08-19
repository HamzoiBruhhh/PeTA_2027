using System.Collections.Generic;
using UnityEngine;

namespace Reshape.ReGraph
{
    public struct GraphContext
    {
        public GameObject gameObject;
        public Transform transform;
        public GraphRunner runner;
        public GraphScriptable scriptable;
        public Graph graph;
        private Dictionary<string, Component> compList;
        private Dictionary<string, object> cacheList;

        public GraphContext (GraphRunner runner)
        {
            this.runner = runner;
            scriptable = null;
            graph = runner.graph;
            gameObject = runner.gameObject;
            transform = gameObject.transform;
            compList = new Dictionary<string, Component>();
            cacheList = new Dictionary<string, object>();
        }

        public GraphContext (GraphScriptable scriptable)
        {
            runner = null;
            this.scriptable = scriptable;
            graph = scriptable.graph;
            gameObject = null;
            transform = null;
            compList = null;
            cacheList = null;
        }

        public bool isScriptableGraph => scriptable != null;

        public string objectName
        {
            get
            {
                if (gameObject != null)
                    return gameObject.name;
                if (scriptable != null)
                    return scriptable.name;
                return string.Empty;
            }
        }

        public void Trigger (string type, GraphExecution execution)
        {
            if (runner != null)
            {
                var executed = runner.InternalTrigger(type);
                if (executed != null)
                {
                    execution.CollectReturnVariableData(executed, type, execution.parameters.actionName);
                    runner.CacheExecute(executed);
                }
            }
            else if (scriptable != null)
            {
                var executed = scriptable.InternalTrigger(type, execution);
                if (executed != null)
                    execution.CollectReturnVariableData(executed, type, execution.parameters.actionName);
            }
        }

        public Component GetComp (string varId)
        {
            if (compList.ContainsKey(varId))
                if (compList.TryGetValue(varId, out Component outComp))
                    return outComp;
            return null;
        }

        public void SetComp (string varId, Component value)
        {
            if (!compList.TryAdd(varId, value))
                compList[varId] = value;
        }

        public object GetCache (string cacheId)
        {
            if (cacheList.ContainsKey(cacheId))
                if (cacheList.TryGetValue(cacheId, out object outCache))
                    return outCache;
            return null;
        }

        public void SetCache (string cacheId, object value)
        {
            if (cacheList != null)
                if (!cacheList.TryAdd(cacheId, value))
                    cacheList[cacheId] = value;
        }

        public bool isUnassigned => runner == null && scriptable == null;
    }
}