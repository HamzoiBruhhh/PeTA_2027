using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
#if REGRAPH_DEV_DEBUG
    [CreateAssetMenu(menuName = "Reshape/Body Part Tag", fileName = "MultiTagBodyPart", order = 207)]
#endif
    public class MultiTagBodyPart : MultiTagData
    {
#if UNITY_EDITOR
        public static ValueDropdownList<string> GetBodyPartChoice ()
        {
            var typeListDropdown = new ValueDropdownList<string>();
            var assets = AssetDatabase.FindAssets("t:MultiTagBodyPart");
            if (assets is {Length: > 0} && assets[0] != null)
            {
                var listPath = AssetDatabase.GUIDToAssetPath(assets[0]);
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(listPath))
                {
                    var data = (MultiTagData) obj;
                    for (var j = 0; j <= 31; j++)
                    {
                        var name = data.tags[j];
                        if (name.Length > 0)
                            typeListDropdown.Add(name, name);
                    }

                }
            }

            return typeListDropdown;
        }
#endif
    }
}