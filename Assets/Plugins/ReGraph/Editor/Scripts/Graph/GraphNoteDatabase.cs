using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
#if REGRAPH_DEV_DEBUG
    [CreateAssetMenu(menuName = "Reshape/Graph Note Database", order = 212)]
#endif
    [HideMonoScript]
    public class GraphNoteDatabase : ScriptableObject
    {
        [SerializeField]
#if !REGRAPH_DEV_DEBUG
        [ReadOnly]
#endif
        private List<string> noteIds;
        
        [SerializeField]
#if !REGRAPH_DEV_DEBUG
        [ReadOnly]
#endif
        private List<string> noteContents;

        public static void DeleteNode (GraphRunner runner, GraphNode node)
        {
            if (node is NoteBehaviourNode noteBehave)
            {
                var settings = GraphSettings.GetSettings();
                if (settings != null && settings.graphNoteDb != null)
                {
                    var uid = $"{runner.GetInstanceID().ToString()}_{noteBehave.Message.reid}";
                    settings.graphNoteDb.RemoveNote(uid);
#if UNITY_EDITOR
                    EditorUtility.SetDirty(settings.graphNoteDb);
#endif
                }
            }
        }

        public void SetNote (string id, string message)
        {
            if (string.IsNullOrEmpty(id))
                return;
            noteIds ??= new List<string>();
            noteContents ??= new List<string>();
            if (noteIds.Contains(id))
            {
                if (string.IsNullOrEmpty(message))
                {
                    RemoveNote(id);
                }
                else
                {
                    var i = noteIds.IndexOf(id);
                    noteContents[i] = message;
                }
            }
            else if (!string.IsNullOrEmpty(message))
            {
                noteIds.Add(id);
                noteContents.Add(message);
            }
        }
        
        public string GetNote (string id)
        {
            if (!string.IsNullOrEmpty(id) && noteIds != null && noteIds.Contains(id))
            {
                var i = noteIds.IndexOf(id);
                return noteContents[i];
            }
            
            return default;
        }
        
        public void RemoveNote (string id)
        {
            if (!string.IsNullOrEmpty(id) && noteIds != null && noteIds.Contains(id))
            {
                var i = noteIds.IndexOf(id);
                noteIds.RemoveAt(i);
                noteContents.RemoveAt(i);
            }
        }
    }
}