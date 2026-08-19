using System;
using System.Collections.Generic;
using Reshape.ReGraph;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    public class BehaviourData
    {
        public GraphExecution lastExecuteResult;

        private List<GraphInputData> inParams;
        private List<GraphOutputData> outParams;
        private BehaviourPack behaviourPack;
        private VariableScriptableObject[] savedGraphInput;
        private VariableScriptableObject[] savedGraphOutput;

        public string id { get; private set; }
        public string behaviourPackName => behaviourPack == null ? string.Empty : behaviourPack.name;
        public BehaviourPack behaviour => behaviourPack;

        public BehaviourData (BehaviourPack pack)
        {
            id = ReUniqueId.GenerateId(false);
            behaviourPack = pack;
        }

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public void Terminate ()
        {
            ReUniqueId.ReturnId(id);
            lastExecuteResult = null;
            behaviourPack = null;
            inParams = null;
            outParams = null;
            savedGraphInput = null;
            savedGraphOutput = null;
        }

        public void TriggerBehaviour (ActionNameChoice type, List<GraphInputData> input, List<GraphOutputData> output)
        {
            if (!behaviourPack)
                ReDebug.LogWarning("Behaviour Data Warning", "TriggerBehaviour activation being ignored due to missing behaviour pack");
            else
            {
                inParams = input;
                outParams = output;
                behaviourPack.TriggerBehaviour(type, this);
            }
        }

        public bool HaveExecuted ()
        {
            if (!behaviourPack)
                return false;
            if (lastExecuteResult == null)
                return false;
            return lastExecuteResult.isSucceed;
        }

        public void PrepareExecution ()
        {
            BackupInParam();
            ResetOutParam();
            SaveInput();
            SaveOutput();
            ResetOutput();
            TransferInput();
        }

        public void ClosingExecution ()
        {
            ReturnOutput();
            LoadInput();
            LoadOutput();
            RestoreInParam();
        }

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void ReturnOutput ()
        {
            var graphOutput = behaviourPack.graph.outParameters;
            for (var j = 0; j < graphOutput.Length; j++)
            {
                for (var i = 0; i < outParams.Count; i++)
                {
                    if (graphOutput[j] && graphOutput[j].name == outParams[i].name)
                    {
                        outParams[i].Return(graphOutput[j]);
                        break;
                    }
                }
            }
        }

        private void ResetOutput ()
        {
            var graphOutput = behaviourPack.graph.outParameters;
            for (var j = 0; j < graphOutput.Length; j++)
                if (graphOutput[j])
                    graphOutput[j].Rebase();
        }

        private void ResetOutParam ()
        {
            for (var j = 0; j < outParams.Count; j++)
                if (outParams[j] != null)
                    outParams[j].Reset();
        }

        private void BackupInParam ()
        {
            for (var i = 0; i < inParams.Count; i++)
                inParams[i].Backup();
        }

        private void RestoreInParam ()
        {
            for (var i = 0; i < inParams.Count; i++)
            {
                var skip = false;
                for (var k = 0; k < outParams.Count; k++)
                {
                    if (inParams[i].variableName == outParams[k].variableName)
                    {
                        skip = true;
                        break;
                    }
                }

                if (!skip)
                    inParams[i].Restore();
            }
        }

        private void TransferInput ()
        {
            var graphInput = behaviourPack.graph.inParameters;
            for (var j = 0; j < graphInput.Length; j++)
            {
                if (graphInput[j])
                {
                    graphInput[j].Rebase();
                    for (var i = 0; i < inParams.Count; i++)
                    {
                        if (graphInput[j].name == inParams[i].name)
                        {
                            inParams[i].Transfer(graphInput[j]);
                            break;
                        }
                    }
                }
            }
        }

        private void SaveOutput ()
        {
            var graphOutput = behaviourPack.graph.outParameters;
            savedGraphOutput = new VariableScriptableObject[graphOutput.Length];
            for (var i = 0; i < graphOutput.Length; i++)
            {
                if (graphOutput[i])
                {
                    savedGraphOutput[i] = ScriptableObject.Instantiate(graphOutput[i]);
                    savedGraphOutput[i].name = graphOutput[i].name;
                }
            }
        }

        private void LoadOutput ()
        {
            var graphOutput = behaviourPack.graph.outParameters;
            for (var i = 0; i < graphOutput.Length; i++)
            {
                if (graphOutput[i])
                {
                    var skip = false;
                    for (var k = 0; k < outParams.Count; k++)
                    {
                        if (graphOutput[i].name == outParams[k].variableName)
                        {
                            skip = true;
                            break;
                        }
                    }

                    if (skip)
                        continue;
                    
                    for (var j = 0; j < savedGraphOutput.Length; j++)
                    {
                        if (graphOutput[i].name == savedGraphOutput[j].name)
                        {
                            if (graphOutput[i] is NumberVariable number)
                            {
                                var saved = (NumberVariable) savedGraphOutput[j];
                                number.SetValue(saved);
                            }
                            else if (graphOutput[i] is WordVariable word)
                            {
                                var saved = (WordVariable) savedGraphOutput[j];
                                word.SetValue(saved);
                            }
                            else if (graphOutput[i] is SceneObjectVariable sceneObject)
                            {
                                var saved = (SceneObjectVariable) savedGraphOutput[j];
                                sceneObject.SetValue(saved.sceneObject);
                            }

                            break;
                        }
                    }
                }
            }
        }

        private void SaveInput ()
        {
            var graphInput = behaviourPack.graph.inParameters;
            savedGraphInput = new VariableScriptableObject[graphInput.Length];
            for (var i = 0; i < graphInput.Length; i++)
            {
                if (graphInput[i])
                {
                    savedGraphInput[i] = ScriptableObject.Instantiate(graphInput[i]);
                    savedGraphInput[i].name = graphInput[i].name;
                }
            }
        }

        private void LoadInput ()
        {
            var graphInput = behaviourPack.graph.inParameters;
            for (var i = 0; i < graphInput.Length; i++)
            {
                if (graphInput[i])
                {
                    var skip = false;
                    for (var k = 0; k < outParams.Count; k++)
                    {
                        if (graphInput[i].name == outParams[k].variableName)
                        {
                            skip = true;
                            break;
                        }
                    }

                    if (!skip)
                    {
                        for (var j = 0; j < savedGraphInput.Length; j++)
                        {
                            if (graphInput[i].name == savedGraphInput[j].name)
                            {
                                if (graphInput[i] is NumberVariable number)
                                {
                                    var saved = (NumberVariable) savedGraphInput[j];
                                    number.SetValue(saved);
                                }
                                else if (graphInput[i] is WordVariable word)
                                {
                                    var saved = (WordVariable) savedGraphInput[j];
                                    word.SetValue(saved);
                                }
                                else if (graphInput[i] is SceneObjectVariable sceneObject)
                                {
                                    var saved = (SceneObjectVariable) savedGraphInput[j];
                                    sceneObject.SetValue(saved.sceneObject);
                                }

                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}