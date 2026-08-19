using System.Collections.Generic;
using Reshape.ReGraph;
using UnityEngine;
using Sirenix.Utilities;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
#endif

namespace Reshape.ReFramework
{
#if UNITY_EDITOR
    public class HandleBeforeAfterAttributeDrawer : OdinAttributeDrawer<HandleBeforeAfterAttribute, List<Collider>>
    {
        private ValueResolver<Object> formattedRunnerResolver;
        private ValueResolver<string> formattedIdResolver;
        private Collider[] previousList;

        protected override void Initialize ()
        {
            formattedRunnerResolver = ValueResolver.Get<Object>(Property, Attribute.graphObject);
            formattedIdResolver = ValueResolver.Get<string>(Property, Attribute.triggerId);
        }

        protected override void DrawPropertyLayout (GUIContent label)
        {
            if (string.Equals(Attribute.type, "CollisionTriggerNodeCollisionChanged"))
            {
                CallNextDrawer(label);

                var currentList = ValueEntry.SmartValue;
                if (previousList != null)
                {
                    if (previousList.Length != currentList.Count)
                    {
                        for (var i = 0; i < previousList.Length; i++)
                        {
                            var collider = previousList[i];
                            if (!currentList.Contains(collider))
                            {
                                var runnerObj = formattedRunnerResolver.GetValue();
                                if (runnerObj != null)
                                {
                                    var runner = GraphRunner.GetFromSelectionObject(runnerObj);
                                    if (runner != null)
                                    {
                                        var id = formattedIdResolver.GetValue();
                                        CollisionController.RemoveBond(collider, runner, id);
                                    }
                                }
                            }
                        }
                    }
                }

                previousList = new Collider[currentList.Count];
                currentList.CopyTo(previousList);
                return;
            }

            CallNextDrawer(label);
        }
    }
#endif
}