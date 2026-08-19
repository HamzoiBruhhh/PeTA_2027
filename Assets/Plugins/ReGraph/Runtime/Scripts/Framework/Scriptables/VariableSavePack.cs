using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Variable Save Pack", fileName = "VariableSavePack", order = 14)]
    [Serializable]
    [HideMonoScript]
    public class VariableSavePack : SavePack
    {
        [Space(4)]
        [DisableInPlayMode]
        public List<VariableScriptableObject> variables;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

#if REGRAPH_DEV_DEBUG
        [BoxGroup("Debug")]
        [Button]
#endif
        public void Save ()
        {
            if (string.IsNullOrEmpty(fileName) || variables is not {Count: > 0})
                return;
            var dict = new Dictionary<string, object>();
            for (var i = 0; i < variables.Count; i++)
                if (variables[i].supportSaveLoad)
                    dict.Add(variables[i].uniqueId, variables[i].GetObject());
            SaveFile(dict, TYPE_VAR);
        }

#if REGRAPH_DEV_DEBUG
        [BoxGroup("Debug")]
        [Button]
#endif
        public void Load ()
        {
            if (string.IsNullOrEmpty(fileName) || variables is not {Count: > 0})
                return;
            var dict = LoadFile(TYPE_VAR);
            if (dict != null)
            {
                foreach (var save in dict)
                {
                    for (var i = 0; i < variables.Count; i++)
                    {
                        if (variables[i].supportSaveLoad)
                        {
                            if (variables[i].uniqueId == save.Key)
                            {
                                variables[i].SetObject(save.Value);
                                break;
                            }
                        }
                    }
                }
            }
        }

#if REGRAPH_DEV_DEBUG
        [BoxGroup("Debug")]
        [Button]
#endif
        public void Delete ()
        {
            if (string.IsNullOrEmpty(fileName) || variables is not {Count: > 0})
                return;
            DeleteFile(TYPE_VAR);
        }

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- BaseScriptable methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}