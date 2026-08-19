using System;
using System.Collections.Generic;
using Reshape.ReGraph;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class CollisionController : BaseBehaviour
    {
        [Serializable]
        public class BondTriggerData
        {
            public GraphRunner runner;
            public string triggerId;

            public BondTriggerData (GraphRunner r, string t)
            {
                runner = r;
                triggerId = t;
            }
        }

        [ReadOnly]
        [SerializeReference]
        public List<BondTriggerData> bondTriggers;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static void AddBond (Collider collider, GraphRunner runner, string triggerId)
        {
            if (!collider.TryGetComponent(out CollisionController controller))
                controller = collider.gameObject.AddComponent<CollisionController>();
            controller.AddBond(runner, triggerId);
        }

        public static void RemoveBond (Collider collider, GraphRunner runner, string triggerId)
        {
            if (collider.TryGetComponent(out CollisionController controller))
                controller.RemoveBond(runner, triggerId);
        }

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected void OnTriggerEnter (Collider other)
        {
            ExecuteBond(TriggerNode.Type.CollisionEnter, other.gameObject);
        }

        protected void OnTriggerExit (Collider other)
        {
            ExecuteBond(TriggerNode.Type.CollisionExit, other.gameObject);
        }

        protected void OnTriggerEnter2D (Collider2D other)
        {
            ExecuteBond(TriggerNode.Type.CollisionEnter, other.gameObject);
        }

        protected void OnTriggerExit2D (Collider2D other)
        {
            ExecuteBond(TriggerNode.Type.CollisionExit, other.gameObject);
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void ExecuteBond (TriggerNode.Type type, GameObject go)
        {
            var haveOneSucees = false;
            for (var i = 0; i < bondTriggers.Count; i++)
            {
                var trigger = bondTriggers[i];
                if (trigger.runner != null)
                {
                    var result = trigger.runner.TriggerCollision(type, go, trigger.triggerId);
                    if (result is {isSucceed: true})
                    {
                        haveOneSucees = true;
                        trigger.runner.CacheExecute(result);
                    }
                }
                else
                {
                    bondTriggers.RemoveAt(i);
                    i--;
                }
            }

            if (go.TryGetComponent(out GraphRunner behave))
            {
                var directFeedback = false;
                for (var i = 0; i < bondTriggers.Count; i++)
                {
                    var trigger = bondTriggers[i];
                    if (trigger.runner != null && trigger.runner == behave)
                    {
                        behave.CacheExecute(behave.TriggerCollision(type is TriggerNode.Type.CollisionEnter ? TriggerNode.Type.CollisionStepIn : TriggerNode.Type.CollisionStepOut, gameObject, trigger.triggerId));
                        directFeedback = true;
                    }
                }

                if (!directFeedback)
                    if (behave.collisionDirect || (!behave.collisionDirect && haveOneSucees))
                        behave.CacheExecute(behave.TriggerCollision(type is TriggerNode.Type.CollisionEnter ? TriggerNode.Type.CollisionStepIn : TriggerNode.Type.CollisionStepOut, gameObject));
            }
        }

        private void AddBond (GraphRunner runner, string triggerId)
        {
            bondTriggers ??= new List<BondTriggerData>();
            var get = GetBondTriggerData(runner, triggerId);
            if (get == null)
                bondTriggers.Add(new BondTriggerData(runner, triggerId));
        }

        private void RemoveBond (GraphRunner runner, string triggerId)
        {
            if (bondTriggers == null)
            {
                DestroyImmediate(this);
                return;
            }

            var get = GetBondTriggerData(runner, triggerId);
            if (get != null)
                bondTriggers.Remove(get);
            if (bondTriggers.Count == 0)
                DestroyImmediate(this);
        }

        private BondTriggerData GetBondTriggerData (GraphRunner runner, string triggerId)
        {
            for (var i = 0; i < bondTriggers.Count; i++)
            {
                var trigger = bondTriggers[i];
                if (trigger.runner != null)
                {
                    if (trigger.runner == runner && trigger.triggerId == triggerId)
                        return trigger;
                }
                else
                {
                    bondTriggers.RemoveAt(i);
                    i--;
                }
            }

            return null;
        }

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------

        /*
        EditorUtility.DisplayProgressBar("Deleting Node", $"Cleaning collision control ... ({(i+0)}/{specificCollision.Count})", (float)i / specificCollision.Count);
        SetGraphEditorContext(Selection.activeGameObject);
        Thread.Sleep(100);
        EditorUtility.DisplayProgressBar("Deleting Node", $"Cleaning collision control ... Done", 1);
        Thread.Sleep(500);
        EditorUtility.ClearProgressBar();
        
        [SerializeField]
        [PropertyOrder(2)]
        [HideLabel, InlineProperty, OnInspectorGUI("OnUpdateCollider")]
        [InlineButton("@collider.SetObjectValue(AssignComponent<Collider>())", "♺", ShowIf = "@collider.IsObjectValueType()")]
        [InfoBox("@collider.GetMismatchWarningMessage()", InfoMessageType.Error, "@collider.IsShowMismatchWarning()")]
        private SceneObjectProperty collider = new SceneObjectProperty(SceneObject.ObjectType.Collider, "Collider (3D)");
        
        private void OnUpdateCollider ()
        {
            collider.AllowObjectOnly();
            if (collider.IsNull)
            {
                collider.SetObjectValue(AssignComponent<Collider>());
                MarkDirty();
            }

            MarkPropertyDirty(collider);
            if (dirty)
            {
                if (!collider.IsEmpty)
                {
                    var assigned = (Collider) collider;
                    var ownCol = AssignComponent<Collider>();
                    if (assigned != ownCol)
                    {
                        if (externalCollider == null)
                        {
                            externalCollider = assigned;
                            CollisionController.AddBond(assigned, this.GetRunner(), this);
                        }
                    }
                    else if (externalCollider != null)
                    {
                        CollisionController.RemoveBond(externalCollider, this.GetRunner(), this);
                        externalCollider = null;
                    }
                }
            }
        }
        */
    }
}