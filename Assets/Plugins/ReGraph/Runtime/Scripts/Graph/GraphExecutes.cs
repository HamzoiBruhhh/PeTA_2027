using System;
using System.Collections.Generic;
using Reshape.Unity;
using UnityEngine;

namespace Reshape.ReGraph
{
    [Serializable]
    public class GraphExecutes
    {
        private static GraphExecution[] cacheList;

        [SerializeField]
        private List<GraphExecution> executionList;

        public int count
        {
            get
            {
                if (executionList == null)
                    return 0;
                return executionList.Count;
            }
        }

        public GraphExecution Add (long id, TriggerNode.Type triggerType)
        {
            executionList ??= new List<GraphExecution>();
            var execution = TakeFromCache() ?? new GraphExecution();
            execution.Init(id, triggerType);
            executionList.Add(execution);
            return execution;
        }

        public void Remove (int index)
        {
            if (executionList == null)
                return;
            if (index >= executionList.Count)
                return;
            MoveToCache(executionList[index]);
            executionList.RemoveAt(index);
        }

        public void Remove (GraphExecution execution)
        {
            if (executionList == null || execution == null)
                return;
            for (var i = 0; i < executionList.Count; i++)
            {
                if (executionList[i].id == execution.id)
                {
                    MoveToCache(execution);
                    executionList.RemoveAt(i);
                    break;
                }
            }
        }

        public GraphExecution Get (int index)
        {
            if (executionList == null)
                return null;
            if (index >= executionList.Count)
                return null;
            return executionList[index];
        }

        public GraphExecution Find (long id)
        {
            if (executionList == null)
                return null;
            for (var i = 0; i < executionList.Count; i++)
            {
                if (id == executionList[i].id)
                    return executionList[i];
            }

            return null;
        }

        public void Stop ()
        {
            if (executionList == null)
                return;
            for (var i = 0; i < executionList.Count; i++)
                executionList[i].Stop();
        }

        public void Clear ()
        {
            for (var i = 0; i < executionList.Count; i++)
                MoveToCache(executionList[i]);
            executionList.Clear();
        }

        public void MoveToCache (GraphExecution execution)
        {
            if (execution == null)
                return;
            if (GraphManager.instance && GraphManager.instance.cacheSize > 0)
                cacheList ??= new GraphExecution[GraphManager.instance.cacheSize];
            if (cacheList != null)
            {
                var moved = false;
                for (var i = 0; i < cacheList.Length; i++)
                {
                    if (cacheList[i] == execution)
                    {
                        cacheList[i] = execution;
                        moved = true;
                        break;
                    }
                }


                if (!moved)
                {
                    for (var i = 0; i < cacheList.Length; i++)
                    {
                        if (cacheList[i] == null)
                        {
                            cacheList[i] = execution;
                            moved = true;
                            break;
                        }
                    }
                }

                if (!moved)
                {
#if DEVELOPMENT_BUILD || (UNITY_EDITOR && REGRAPH_DEV_DEBUG)
                    ReDebug.LogWarning("Graph Warning", "Graph executes cache have full!");
#endif
                }
            }
        }

        public GraphExecution TakeFromCache ()
        {
            if (cacheList != null)
            {
                for (var i = 0; i < cacheList.Length; i++)
                {
                    if (cacheList[i] != null && !cacheList[i].isReserved)
                    {
                        var temp = cacheList[i];
                        cacheList[i] = null;
                        temp.Reset();
                        return temp;
                    }
                }
            }

            return null;
        }
    }
}