using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Reshape.ReFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using Reshape.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
    [AddComponentMenu("ReGraph/Graph Runner", 1)]
    [HideMonoScript]
    [DisallowMultipleComponent]
    public class GraphRunner : BaseBehaviour
    {
        private const string TickName = "GraphRunner";

        private static List<GraphRunner> list;

        [FoldoutGroup("Settings")]
        [ShowIf("ShowSettings")]
        [DisableIf("DisableSettings")]
        [Hint("showHints", "Collision detection direct come from Unity Engine, not pass thru graph. This is require more processing and no optimise.")]
        public bool collisionDirect;

        [FoldoutGroup("Settings", expanded: false)]
        [ShowIf("ShowSettings")]
        [DisableIf("DisableSettings")]
        public bool runEvenInactive;

        [FoldoutGroup("Settings")]
        [ShowIf("ShowSettings")]
        [DisableIf("DisableSettings")]
        public bool runEvenDisabled;

        [FoldoutGroup("Settings")]
        [ShowIf("ShowSettings")]
        [DisableIf("DisableSettings")]
        public bool stopOnDeactivate;

        [FoldoutGroup("Settings")]
        [ShowIf("ShowSettings")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariableWarning", GUIAlwaysEnabled = true)]
        public SceneObjectVariable runnerVariable;

        [FoldoutGroup("Settings")]
        [ShowIf("ShowSettings")]
        [DisableIf("DisableSettings")]
        public MultiTag customTag = new MultiTag("Tag", typeof(MultiTagRunner), 1);

        [FoldoutGroup("Settings")]
        [ShowIf("ShowSettings")]
        public bool printFlowLog;

        [HideLabel]
        [OnInspectorDispose("OnLeaveInspector")]
        public Graph graph;

        private GraphContext context;
        private bool disabled;
        private bool paused;
        private bool started;

        /*[Button]
        [HideInEditorMode]
        private void DebugInfo ()
        {
            object cacheObj = context.GetCache("TheSlotIndex");
            if (cacheObj != null)
            {
                Debug.Log((float) cacheObj);
            }
        }*/

        public bool activated
        {
            get
            {
                if (this == null)
                    return false;
                if (!enabled)
                    if (!runEvenDisabled)
                        return false;
                if (!gameObject.activeInHierarchy)
                    if (!runEvenInactive)
                        return false;
                return true;
            }
        }

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static void PauseAll (MultiTag pauseTag)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].customTag.ContainAll(pauseTag))
                {
                    list[i].Pause();
                }
            }
        }

        public static void UnpauseAll (MultiTag pauseTag)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].customTag.ContainAll(pauseTag))
                {
                    list[i].Unpause();
                }
            }
        }

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public bool IsStatGraph ()
        {
            return graph is {isStatGraph: true};
        }

        public void ExecuteAction (ActionNameChoice type)
        {
            ExecuteAction(type.ToString());
        }

        public void ExecuteAction (string type)
        {
            if (!string.IsNullOrEmpty(type))
                CacheExecute(TriggerAction(type));
        }

        public GraphExecution TriggerAction (ActionNameChoice type)
        {
            return type ? TriggerAction(type.ToString()) : null;
        }

        public GraphExecution TriggerAction (string type)
        {
            return !string.IsNullOrEmpty(type) ? Activate(TriggerNode.Type.ActionTrigger, actionName: type) : null;
        }

        public GraphExecution TriggerDeactivate ()
        {
            return Activate(TriggerNode.Type.OnDeactivate);
        }

        public GraphExecution TriggerActivate ()
        {
            return Activate(TriggerNode.Type.OnActivate);
        }

        public GraphExecution TriggerCollision (TriggerNode.Type type, GameObject go, string triggerId = "")
        {
            return OnTrigger(type, go, triggerId);
        }

        public void TriggerCollisionStep (TriggerNode.Type type, GameObject interactGo, bool fromGraph, bool externalController)
        {
            if (externalController)
                return;
            if ((!collisionDirect && fromGraph) || (collisionDirect && !fromGraph))
                if (type is TriggerNode.Type.CollisionEnter or TriggerNode.Type.CollisionExit)
                    if (interactGo.TryGetComponent(out GraphRunner behave))
                        behave.CacheExecute(behave.TriggerCollision(type is TriggerNode.Type.CollisionEnter ? TriggerNode.Type.CollisionStepIn : TriggerNode.Type.CollisionStepOut, this.gameObject));
        }

        [SpecialName]
        public void TriggerSpawn (ActionNameChoice type)
        {
            CacheExecute(Activate(TriggerNode.Type.GameObjectSpawn, actionName: type));
        }

        public void TriggerInput (TriggerNode.Type type, string triggerId)
        {
            CacheExecute(Activate(type, actionName: triggerId));
        }

        public void TriggerVideo (TriggerNode.Type type, string triggerId)
        {
            CacheExecute(Activate(type, actionName: triggerId));
        }

        public void TriggerAudio (TriggerNode.Type type, string triggerId)
        {
            CacheExecute(Activate(type, actionName: triggerId));
        }

        public void TriggerInventory (TriggerNode.Type type, string triggerId)
        {
            CacheExecute(Activate(type, actionName: triggerId));
        }

        public bool DetectCharacterTrigger (TriggerNode.Type triggerType)
        {
            return DetectActivate(triggerType);
        }

        public void TriggerCharacter (TriggerNode.Type type, CharacterBrain brain, AttackDamageData damageData = null, string actionName = null, CharacterBrain addonBrain = null)
        {
            CacheExecute(Activate(type, brain: brain, attackData: damageData, actionName: actionName, addonBrain: addonBrain));
        }

        public GraphExecution TriggerBrainStat (TriggerNode.Type type, CharacterBrain brain, string statName)
        {
            return Activate(type, actionName: statName, brain: brain);
        }

        public void TriggerVariable (TriggerNode.Type type, string triggerId)
        {
            CacheExecute(Activate(type, actionName: triggerId));
        }

        public void TriggerRay (TriggerNode.Type type, string triggerId)
        {
            CacheExecute(Activate(type, actionName: triggerId));
        }

        public void TriggerOcclusion (TriggerNode.Type type, string triggerId)
        {
            CacheExecute(Activate(type, actionName: triggerId));
        }

        public void ResumeTrigger (long executionId, int updateId)
        {
            var execution = graph?.FindExecute(executionId);
            if (execution == null)
            {
                ReDebug.LogWarning("Graph Warning", "Trigger " + executionId + " re-activation have not found in " + goName);
                return;
            }

            if (!activated)
            {
                graph?.StopExecute(execution, ReTime.frameCount);
                try
                {
                    ReDebug.LogWarning("Graph Warning", "Trigger " + executionId + " re-activation being ignored in " + goName);
                }
                catch (Exception)
                {
                    ReDebug.LogWarning("Graph Warning", "Trigger " + executionId + " re-activation being ignored in a destroyed gamaObject");
                }

                return;
            }

            graph?.ResumeExecute(execution, ReTime.frameCount);
        }

        [SpecialName]
        public GraphExecution InternalTrigger (string type)
        {
            return Activate(TriggerNode.Type.All, actionName: type);
        }
        
        [SpecialName]
        public void CacheExecute (GraphExecution execute)
        {
            if (execute != null)
                graph?.CleanExecute(execute);
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        [SpecialName]
        public override void Init ()
        {
            if (graph.HaveRequireInit())
                Activate();
            if (graph.HaveRequireUpdate())
                PlanTick(TickName);
            PlanUninit();
            DoneInit();
        }

        [SpecialName]
        public override void Begin ()
        {
            CacheExecute(Activate(TriggerNode.Type.OnStart));
            DoneBegin();
        }

        [SpecialName]
        public override void Tick ()
        {
            if (!activated)
                return;
            graph?.Update(ReTime.frameCount);
        }

        [SpecialName]
        public override void PreUninit ()
        {
            CacheExecute(Activate(TriggerNode.Type.OnEnd));
            DonePreUninit();
        }

        [SpecialName]
        public override void Uninit ()
        {
            if (graph.HaveRequireUpdate())
                OmitTick();
            if (graph.HaveRequireInit())
                Deactivate();
            DoneUninit();
        }

        [SpecialName]
        public override bool ReceivedRayCast (ReMonoBehaviour mono, string rayName, RaycastHit? hit)
        {
            if (!activated)
                return false;
            GraphExecution execute = Activate(TriggerNode.Type.RayStay, actionName: rayName, interactedMono: mono);
            if (execute == null)
                return false;
            if (execute.isFailed)
                return false;
            return true;
        }

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected void Awake ()
        {
            list ??= new List<GraphRunner>();
            list.Add(this);
            if (graph != null)
            {
                context = new GraphContext(this);
                graph.Bind(context);
                if (runnerVariable && runnerVariable.sceneObject.type == SceneObject.ObjectType.GraphRunner)
                    runnerVariable.SetValue(this);
                if (graph.HaveRequireUpdate() || graph.HaveRequireInit())
                    PlanInit();
                if (graph.HaveRequireBegin())
                    PlanBegin();
                if (graph.HaveRequirePreUninit())
                    PlanPreUninit();
            }
        }

        protected override void Start ()
        {
#if UNITY_EDITOR
            if (GraphManager.instance == null)
                ReDebug.LogWarning("Graph Warning", "Graph Manager is not yet add into the scene.");
#endif
            started = true;
        }

        protected void LateUpdate ()
        {
            if (!activated)
                return;
            graph?.CleanExecutes();
        }

        protected void OnDisable ()
        {
            Disable();
        }

        protected void OnEnable ()
        {
            Enable();
        }

        protected void OnDestroy ()
        {
            list?.Remove(this);
            if (graph?.isTerminated == false)
            {
                OmitUninit();
                if (graph.HaveRequireUpdate())
                    OmitTick();
                if (graph.HaveRequireInit())
                    Deactivate();
            }
        }

        protected void OnTriggerEnter (Collider other)
        {
            CacheExecute(OnTrigger(TriggerNode.Type.CollisionEnter, other.gameObject));
        }

        protected void OnTriggerExit (Collider other)
        {
            CacheExecute(OnTrigger(TriggerNode.Type.CollisionExit, other.gameObject));
        }

        protected void OnTriggerEnter2D (Collider2D other)
        {
            CacheExecute(OnTrigger(TriggerNode.Type.CollisionEnter, other.gameObject));
        }

        protected void OnTriggerExit2D (Collider2D other)
        {
            CacheExecute(OnTrigger(TriggerNode.Type.CollisionExit, other.gameObject));
        }

        //-----------------------------------------------------------------
        //-- internal methods
        //-----------------------------------------------------------------

        private bool DetectActivate (TriggerNode.Type type, int paramInt = 0)
        {
            return graph.HaveTrigger(type, true, paramInt);
        }

        private GraphExecution Activate (TriggerNode.Type type, string actionName = null, long executeId = 0, GameObject interactedGo = null,
            ReMonoBehaviour interactedMono = null, CharacterBrain brain = null, AttackDamageData attackData = null, CharacterBrain addonBrain = null)
        {
            if (!activated)
            {
                if (type == TriggerNode.Type.ActionTrigger)
                    ReDebug.LogWarning("Graph Warning", type + " " + actionName + " activation being ignored in " + goName);
                else
                    ReDebug.LogWarning("Graph Warning", type + " activation being ignored in " + goName);
                return null;
            }

            if (type != TriggerNode.Type.All && !DetectActivate(type))
                return null;
            if (executeId == 0)
                executeId = ReUniqueId.GenerateLong();
            var execute = graph?.InitExecute(executeId, type);
            if (execute != null)
            {
                switch (type)
                {
                    case TriggerNode.Type.ActionTrigger:
                    case TriggerNode.Type.GameObjectSpawn:
                    case TriggerNode.Type.InputPress:
                    case TriggerNode.Type.InputRelease:
                    case TriggerNode.Type.VideoFinished:
                    case TriggerNode.Type.AudioFinished:
                    case TriggerNode.Type.VariableChange:
                    case TriggerNode.Type.OcclusionStart:
                    case TriggerNode.Type.OcclusionEnd:
                    case TriggerNode.Type.RayAccepted:
                    case TriggerNode.Type.RayMissed:
                    case TriggerNode.Type.RayHit:
                    case TriggerNode.Type.RayLeave:
                    case TriggerNode.Type.RayArrive:
                    case TriggerNode.Type.InventoryQuantityChange:
                    case TriggerNode.Type.InventorySlotChange:
                    case TriggerNode.Type.InventoryDecayChange: 
                    case TriggerNode.Type.InventoryItemChange:
                    case TriggerNode.Type.InventoryUnLockRequest:
                    case TriggerNode.Type.All:
                        execute.parameters.actionName = actionName;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.RayStay:
                        execute.parameters.actionName = actionName;
                        execute.parameters.interactedMono = interactedMono;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.CollisionEnter:
                    case TriggerNode.Type.CollisionExit:
                    case TriggerNode.Type.CollisionStepIn:
                    case TriggerNode.Type.CollisionStepOut:
                        execute.parameters.interactedGo = interactedGo;
                        execute.parameters.actionName = actionName;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.OnStart:
                    case TriggerNode.Type.OnEnd:
                    case TriggerNode.Type.OnEnable:
                    case TriggerNode.Type.OnDeactivate:
                    case TriggerNode.Type.OnActivate:
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.BrainStatGet:
                    case TriggerNode.Type.BrainStatChange:
                        execute.parameters.actionName = actionName;
                        execute.parameters.characterBrain = brain;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.SelectReceive:
                    case TriggerNode.Type.SelectConfirm:
                    case TriggerNode.Type.SelectFinish:
                    case TriggerNode.Type.CharacterAttackFire:
                    case TriggerNode.Type.CharacterStanceDone:
                    case TriggerNode.Type.CharacterUnstanceDone:
                    case TriggerNode.Type.CharacterGetInterrupt:
                        execute.parameters.characterBrain = brain;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.InteractLaunch:
                    case TriggerNode.Type.InteractReceive:
                    case TriggerNode.Type.InteractFinish:
                    case TriggerNode.Type.InteractLeave:
                    case TriggerNode.Type.InteractCancel:
                    case TriggerNode.Type.InteractGiveUp:
                        execute.parameters.characterBrain = brain;
                        execute.parameters.characterBrainAddon = addonBrain;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                    case TriggerNode.Type.CharacterKill:
                    case TriggerNode.Type.CharacterDead:
                    case TriggerNode.Type.CharacterTerminate:
                    case TriggerNode.Type.CharacterAttackBackstab:
                    case TriggerNode.Type.CharacterFriendDead:
                    case TriggerNode.Type.CharacterGetAttack:
                    case TriggerNode.Type.CharacterGetBackstab:
                    case TriggerNode.Type.CharacterScanVicinity:
                    case TriggerNode.Type.CharacterAttackSkill:
                        execute.parameters.characterBrain = brain;
                        execute.parameters.attackDamageData = attackData;
                        graph?.RunExecute(execute, ReTime.frameCount);
                        break;
                }
            }

            return execute;
        }

        private GraphExecution OnTrigger (TriggerNode.Type type, GameObject go, string triggerId = "")
        {
            TriggerCollisionStep(type, go, false, !string.IsNullOrEmpty(triggerId));
            return Activate(type, interactedGo: go, actionName: triggerId);
        }

        private void Enable ()
        {
            if (disabled && !stopOnDeactivate)
                graph?.UnpauseExecutes();
            disabled = false;
            if (started)
                CacheExecute(Activate(TriggerNode.Type.OnEnable));
        }

        private void Disable ()
        {
            if (disabled) return;
            disabled = true;
            if (stopOnDeactivate)
                graph?.StopExecutes();
            if (activated) return;
            if (!stopOnDeactivate)
                graph?.PauseExecutes();
        }

        private void Pause ()
        {
            //-- NOTE purposely allow pause after paused, reason is the previous pause action will have some nodes is continue running at the same execution. Removed "if (paused) return;"
            paused = true;
            graph?.PauseExecutes();
        }

        private void Unpause ()
        {
            if (paused)
            {
                graph?.UnpauseExecutes();
                paused = false;
            }
        }

        private void Activate ()
        {
            graph?.Start();
        }

        private void Deactivate ()
        {
            graph?.Stop();
        }

#if UNITY_EDITOR
        public static GraphRunner GetFromSelectionObject (UnityEngine.Object selectionObject)
        {
            if (selectionObject is GameObject go)
            {
                if (go.TryGetComponent(out GraphRunner runner))
                    return runner;
            }

            return null;
        }

        [Button]
        [ShowIf("IsApplicationPlaying")]
        private void Execute (string actionName)
        {
            CacheExecute(Activate(TriggerNode.Type.ActionTrigger, actionName: actionName));
        }
        
        private bool ShowObjectVariableWarning ()
        {
            if (runnerVariable != null)
                if (runnerVariable.sceneObject.type != SceneObject.ObjectType.GraphRunner)
                    return true;
            return false;
        }

        public bool IsApplicationPlaying ()
        {
            return EditorApplication.isPlaying;
        }

        private bool ShowExecuteButton ()
        {
            return Application.isPlaying && graph?.HavePreviewNode() == false;
        }

        private bool ShowSettings ()
        {
            return graph?.HavePreviewNode() == false && graph.isCreated;
        }

        private bool DisableSettings ()
        {
            return Application.isPlaying;
        }

        private void OnLeaveInspector ()
        {
            if (graph != null && graph.previewNode != null)
            {
                graph.previewNode = null;
                graph.editInPrefabModeWarning = string.Empty;
            }
        }

        private void OnDrawGizmosSelected ()
        {
            if (graph == null)
                return;
            Graph.Traverse(graph.RootNode, (n) =>
            {
                if (n.drawGizmos)
                    n.OnDrawGizmos();
            });
        }

        public bool ContainNode (Type nodeType)
        {
            if (graph == null)
                return false;
            return graph.IsNodeTypeInUse(nodeType);
        }
        
        public bool ContainTriggerNode (TriggerNode.Type triggerType)
        {
            if (graph == null)
                return false;
            return graph.IsTriggerNodeTypeInUse(triggerType);
        }
        
        public bool HaveNodeLinkAction (GraphRunner actionRunner, string actionName, string triggerId)
        {
            if (graph == null)
                return false;
            var result = false;
            if (graph.nodes != null)
            {
                for (var i = 0; i < graph.nodes.Count; i++)
                {
                    if (graph.nodes[i] != null && graph.nodes[i] is ActionBehaviourNode actionBehNode)
                    {
                        if (!string.IsNullOrEmpty(actionName) && string.Equals(actionBehNode.ActionName, actionName))
                        {
                            result = true;
                            break;
                        }
                    }
                    else if (graph.nodes[i] != null && graph.nodes[i] is TriggerBehaviourNode triggerBehNode)
                    {
                        if (!string.IsNullOrEmpty(triggerId) && string.Equals(triggerBehNode.TriggerNode, triggerId) && triggerBehNode.IsRunExecution && triggerBehNode.GetRunner() == actionRunner)
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
            
            return result;
        }
#endif
    }
}