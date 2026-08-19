using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
#if REGRAPH_PATHFIND
    [System.Serializable]
    public class PathFindBehaviourNode : BehaviourNode
    {
        public const string VAR_PROCEED = "_proceed";
        public const string VAR_DEST_X = "_dest_x";
        public const string VAR_DEST_Y = "_dest_y";
        public const string VAR_DEST_Z = "_dest_z";

        public enum ExecutionType
        {
            None,
            AgentWalk = 10,
            AgentStop = 50,
            AgentResume = 60,
            AgentSpeed = 100,
            AgentChase = 200,
            AgentGiveUp = 210,
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [ShowIf("@executionType != ExecutionType.None")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(agent)")]
        [InlineButton("@agent.SetObjectValue(AssignComponent<PathFindAgent>())", "♺", ShowIf = "@agent.IsObjectValueType()")]
        [InfoBox("@agent.GetMismatchWarningMessage()", InfoMessageType.Error, "@agent.IsShowMismatchWarning()")]
        private SceneObjectProperty agent = new SceneObjectProperty(SceneObject.ObjectType.PathFindAgent);

        [SerializeField]
        [ShowIf("@executionType == ExecutionType.AgentWalk || executionType == ExecutionType.AgentChase || executionType == ExecutionType.AgentGiveUp")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(transform)")]
        [InlineButton("@transform.SetObjectValue(AssignComponent<UnityEngine.Transform>())", "♺", ShowIf = "@transform.IsObjectValueType()")]
        [InfoBox("@transform.GetMismatchWarningMessage()", InfoMessageType.Error, "@transform.IsShowMismatchWarning()")]
        private SceneObjectProperty transform = new SceneObjectProperty(SceneObject.ObjectType.Transform);

        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(speed)")]
        [InlineProperty]
        [ShowIf("@executionType == ExecutionType.AgentSpeed")]
        private FloatProperty speed = new FloatProperty(0);

        private string proceedKey;
        private string destXKey;
        private string destYKey;
        private string destZKey;

        private void InitVariables ()
        {
            if (string.IsNullOrEmpty(proceedKey))
                proceedKey = guid + VAR_PROCEED;
            if (string.IsNullOrEmpty(destXKey))
                destXKey = guid + VAR_DEST_X;
            if (string.IsNullOrEmpty(destYKey))
                destYKey = guid + VAR_DEST_Y;
            if (string.IsNullOrEmpty(destZKey))
                destZKey = guid + VAR_DEST_Z;
        }

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType == ExecutionType.None || agent.IsEmpty || !agent.IsMatchType())
            {
                LogWarning("Found an empty PathFind Behaviour node in " + context.objectName);
            }
            else
            {
                InitVariables();
                var agt = (PathFindAgent) agent;
                if (!agt.enabled || !agt.gameObject.activeInHierarchy)
                {
                    LogWarning("Found invalid PathFind Behaviour node in " + context.objectName);
                }
                else if (executionType == ExecutionType.AgentWalk)
                {
                    if (transform.IsEmpty || !transform.IsMatchType())
                    {
                        LogWarning("Found an empty PathFind Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        execution.variables.SetInt(proceedKey, 0);

                        var pos = ((Transform) transform).position;
                        agt.SetDestination(pos, execution.id.ToString());
                        execution.variables.SetFloat(destXKey, pos.x);
                        execution.variables.SetFloat(destYKey, pos.y);
                        execution.variables.SetFloat(destZKey, pos.z);
                    }
                }
                else if (executionType == ExecutionType.AgentStop)
                {
                    agt.StopMove();
                }
                else if (executionType == ExecutionType.AgentResume)
                {
                    agt.ResumeMove();
                }
                else if (executionType == ExecutionType.AgentSpeed)
                {
                    agt.SetMoveSpeed(speed);
                }
                else if (executionType == ExecutionType.AgentChase)
                {
                    if (!agt.TryGetComponent(out AgentChaseController chase))
                        chase = agt.gameObject.AddComponent<AgentChaseController>();
                    chase.Initial(agt, (Transform) transform);
                }
                else if (executionType == ExecutionType.AgentGiveUp)
                {
                    if (agt.TryGetComponent(out AgentChaseController chase))
                        chase.Terminate();
                }
            }

            base.OnStart(execution, updateId);
        }

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            if (executionType is ExecutionType.AgentWalk)
            {
                int key = execution.variables.GetInt(proceedKey, -1);
                if (key == 0)
                {
                    if (agent.IsEmpty || !agent.IsMatchType())
                        return State.Failure;
                    var agt = (PathFindAgent) agent;
                    if (agt.IsReachDestination(execution.id.ToString(), true))
                    {
                        execution.variables.SetInt(proceedKey, 1);
                        key = 1;
                    }
                }

                if (key > 0)
                    return base.OnUpdate(execution, updateId);
                return State.Running;
            }

            return base.OnUpdate(execution, updateId);
        }

        public override bool IsRequireUpdate ()
        {
            return enabled;
        }

#if UNITY_EDITOR
        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Agent Walk", ExecutionType.AgentWalk},
            {"Agent Stop Walk", ExecutionType.AgentStop},
            {"Agent Resume Walk", ExecutionType.AgentResume},
            {"Agent Chase", ExecutionType.AgentChase},
            {"Agent Give Up Chase", ExecutionType.AgentGiveUp},
            {"Agent Speed", ExecutionType.AgentSpeed},
        };

        public static string displayName = "PathFind Behaviour Node";
        public static string nodeName = "PathFind";

        public override string GetNodeInspectorTitle ()
        {
            return displayName;
        }

        public override string GetNodeViewTitle ()
        {
            return nodeName;
        }
        
        public override string GetNodeIdentityName ()
        {
            return executionType.ToString();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Gameplay/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType != ExecutionType.None && !agent.IsNull && agent.IsMatchType())
            {
                if (executionType == ExecutionType.AgentWalk && !transform.IsNull && transform.IsMatchType())
                {
                    var pos = ((Transform) transform).position;
                    return agent.name + " walk to " + transform.name + " " + pos + "\n<color=#FFF600>Continue at arrival";
                }

                if (executionType == ExecutionType.AgentChase && !transform.IsNull && transform.IsMatchType())
                {
                    return agent.name + " chase " + transform.name;
                }

                if (executionType == ExecutionType.AgentGiveUp && !transform.IsNull && transform.IsMatchType())
                {
                    return agent.name + " give up chase " + transform.name;
                }

                if (executionType == ExecutionType.AgentStop)
                    return agent.name + " stop walking";
                if (executionType == ExecutionType.AgentResume)
                    return agent.name + " resume walking";
                if (executionType == ExecutionType.AgentSpeed)
                    return agent.name + "'s agent speed change to " + speed;
            }

            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (executionType == ExecutionType.AgentWalk)
                tip += "This will command a Path Find Agent walk to specific location.\n\n";
            else if (executionType == ExecutionType.AgentChase)
                tip += "This will command a Path Find Agent chase to specific transform.\n\n";
            else if (executionType == ExecutionType.AgentGiveUp)
                tip += "This will command a Path Find Agent stop chase on specific transform.\n\n";
            else if (executionType == ExecutionType.AgentStop)
                tip += "This will command a Path Find Agent stop walking.\n\n";
            else if (executionType == ExecutionType.AgentResume)
                tip += "This will command a Path Find Agent resume walking.\n\n";
            else if (executionType == ExecutionType.AgentSpeed)
                tip += "This will change Path Find Agent move speed.\n\n";
            else
                tip += "This will execute all Path Find related behaviour.\n\n";
            return tip + "We are using AStarPathfindingProject as our Path Find system.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
#endif
}