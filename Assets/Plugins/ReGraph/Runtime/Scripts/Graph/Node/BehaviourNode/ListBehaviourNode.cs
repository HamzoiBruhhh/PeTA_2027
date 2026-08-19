using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReFramework;
using UnityEngine.Audio;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class ListBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            Insert = 11,
            Push = 12,
            Take = 21,
            Pop = 22,
            Random = 23,
            GetByIndex = 24,
            GetById = 25,
            Remove = 31,
            Clear = 32,
            InsertValue = 111,
            GetCount = 210
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [OnValueChanged("OnChangeList")]
        [ShowIf("@executionType != ExecutionType.None")]
        [InfoBox("@GetListWarningMessage()", InfoMessageType.Warning, "ShowListWarning", GUIAlwaysEnabled = true)]
        private VariableList list;

        [SerializeField]
        [ListDrawerSettings(CustomAddFunction = "CreateNewListItem", Expanded = true)]
        [ShowIf("ShowAddObjects")]
        [OnInspectorGUI("OnUpdateObjects")]
        private List<SceneObjectProperty> objects;

        [SerializeField]
        [LabelText("@ObjectVariableLabel()")]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowObjectVariable")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariableWarning", GUIAlwaysEnabled = true)]
        public SceneObjectVariable objectVariable;
        
        [SerializeField]
        [ShowIf("ShowAddWords")]
        [OnInspectorGUI("OnUpdateWords")]
        private List<StringProperty> words;

        [SerializeField]
        [LabelText("@ObjectVariableLabel()")]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowWordVariable")]
        public WordVariable wordVariable;
        
        [SerializeField]
        [ShowIf("ShowAddDigits")]
        [OnInspectorGUI("OnUpdateDigits")]
        private List<FloatProperty> digits;
        
        [SerializeField]
        [LabelText("@ObjectVariableLabel()")]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowDigitVariable")]
        public NumberVariable digitVariable;

        [SerializeField]
        [ShowIf("ShowNumberVariable")]
        [LabelText("@NumberVariableLabel()")]
        [InlineButton("CreateNumberVariable", "✚")]
        [OnValueChanged("MarkDirty")]
        private NumberVariable numberVariable;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            var error = false;
            if (!list || list.IsNoneType())
            {
                error = true;
            }
            else if (executionType is ExecutionType.Pop or ExecutionType.Take or ExecutionType.Random or ExecutionType.Remove or ExecutionType.GetByIndex or ExecutionType.GetById)
            {
                if (list is SceneObjectList soList)
                {
                    if (!objectVariable || soList.type != objectVariable.sceneObject.type)
                        error = true;
                    else if (soList.GetCount() == 0)
                        error = true;
                    else if (executionType is ExecutionType.GetByIndex or ExecutionType.GetById && !numberVariable)
                        error = true;
                }
                else if (list is WordList wList)
                {
                    if (!wordVariable)
                        error = true;
                    else if (wList.GetCount() == 0)
                        error = true;
                    else if (executionType is ExecutionType.GetByIndex or ExecutionType.GetById && !numberVariable)
                        error = true;
                }
                else if (list is NumberList numList)
                {
                    if (!digitVariable)
                        error = true;
                    else if (numList.GetCount() == 0)
                        error = true;
                    else if (executionType is ExecutionType.GetByIndex or ExecutionType.GetById && !numberVariable)
                        error = true;
                }
            }
            else if (executionType is ExecutionType.Clear)
            {
                
            }
            else if (executionType is ExecutionType.GetCount)
            {
                if (!numberVariable)
                    error = true;
            }
            else if (executionType is ExecutionType.Insert or ExecutionType.Push or ExecutionType.InsertValue)
            {
                if (list is SceneObjectList)
                {
                    if (objects.Count == 0)
                        error = true;
                    var found = false;
                    for (var i = 0; i < objects.Count; i++)
                    {
                        if (!objects[i].IsNull)
                        {
                            found = true;
                        }
                        else
                        {
                            objects.RemoveAt(i);
                            i--;
                        }
                    }

                    if (!found)
                        error = true;
                }
                else if (list is WordList)
                {
                    if (words.Count == 0)
                        error = true;
                    var found = false;
                    for (var i = 0; i < words.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(words[i]))
                        {
                            found = true;
                        }
                        else
                        {
                            words.RemoveAt(i);
                            i--;
                        }
                    }

                    if (!found)
                        error = true;
                }
                else if (list is NumberList)
                {
                    if (digits.Count == 0)
                        error = true;
                }
            }

            if (error)
            {
                LogWarning("Found an empty List Behaviour node in " + context.objectName);
            }
            else if (list is SceneObjectList soList)
            {
                if (executionType is ExecutionType.Pop)
                {
                    objectVariable.SetValue((SceneObject) soList.PopObject());
                }
                else if (executionType is ExecutionType.Take)
                {
                    objectVariable.SetValue((SceneObject) soList.TakeObject());
                }
                else if (executionType is ExecutionType.GetByIndex)
                {
                    objectVariable.SetValue((SceneObject) soList.GetByIndex(numberVariable));
                }
                else if (executionType is ExecutionType.GetById)
                {
                    objectVariable.SetValue((SceneObject) soList.GetById(numberVariable));
                }
                else if (executionType is ExecutionType.Random)
                {
                    objectVariable.SetValue((SceneObject) soList.RandomObject());
                }
                else if (executionType is ExecutionType.Remove)
                {
                    soList.RemoveObject(objectVariable);
                }
                else if (executionType is ExecutionType.Clear)
                {
                    soList.ClearObject();
                }
                else if (executionType is ExecutionType.GetCount)
                {
                    numberVariable.SetValue(soList.GetCount());
                }
                else if (executionType is ExecutionType.Insert)
                {
                    soList.InsertObject(objects);
                }
                else if (executionType is ExecutionType.InsertValue)
                {
                    for (var i = 0; i < objects.Count; i++)
                    {
                        var valueObj = objects[i];
                        if (!valueObj.IsEmpty)
                        {
                            var so = new SceneObjectProperty(soList.type);
                            if (valueObj.objectValue.IsComponent())
                                so.SetObjectValue((Component) valueObj);
                            else if (valueObj.objectValue.IsGameObject())
                                so.SetObjectValue((GameObject) valueObj);
                            else if (valueObj.objectValue.IsCharacterOperator())
                                so.SetObjectValue((CharacterOperator) valueObj);
                            else if (valueObj.objectValue.IsScriptableObject())
                                so.SetObjectValue((ScriptableObject) valueObj);
                            else if (valueObj.objectValue.IsMesh())
                                so.SetObjectValue((Mesh) valueObj);
                            else if (valueObj.objectValue.IsMaterial())
                                so.SetObjectValue((Material) valueObj);
                            else if (valueObj.objectValue.IsSprite())
                                so.SetObjectValue((Sprite) valueObj);
                            else if (valueObj.objectValue.IsAudioMixer())
                                so.SetObjectValue((AudioMixer) valueObj);
                            soList.InsertValue(so);
                        }
                    }
                }
                else if (executionType is ExecutionType.Push)
                {
                    soList.PushObject(objects);
                }
            }
            else if (list is WordList wList)
            {
                if (executionType is ExecutionType.Pop)
                {
                    wordVariable.SetValue(wList.PopObject());
                }
                else if (executionType is ExecutionType.Take)
                {
                    wordVariable.SetValue(wList.TakeObject());
                }
                else if (executionType is ExecutionType.GetByIndex)
                {
                    wordVariable.SetValue(wList.GetByIndex(numberVariable));
                }
                else if (executionType is ExecutionType.GetById)
                {
                    wordVariable.SetValue(wList.GetById(numberVariable));
                }
                else if (executionType is ExecutionType.Random)
                {
                    wordVariable.SetValue(wList.RandomObject());
                }
                else if (executionType is ExecutionType.Remove)
                {
                    wList.RemoveObject(wordVariable);
                }
                else if (executionType is ExecutionType.Clear)
                {
                    wList.ClearObject();
                }
                else if (executionType is ExecutionType.GetCount)
                {
                    numberVariable.SetValue(wList.GetCount());
                }
                else if (executionType is ExecutionType.Insert)
                {
                    wList.InsertObject(words);
                }
                else if (executionType is ExecutionType.InsertValue)
                {
                    for (var i = 0; i < words.Count; i++)
                        wList.InsertValue(words[i].ShallowCopy());
                }
                else if (executionType is ExecutionType.Push)
                {
                    wList.PushObject(words);
                }
            }
            else if (list is NumberList numList)
            {
                if (executionType is ExecutionType.Pop)
                {
                    digitVariable.SetValue(numList.PopObject());
                }
                else if (executionType is ExecutionType.Take)
                {
                    digitVariable.SetValue(numList.TakeObject());
                }
                else if (executionType is ExecutionType.GetByIndex)
                {
                    digitVariable.SetValue(numList.GetByIndex(numberVariable));
                }
                else if (executionType is ExecutionType.GetById)
                {
                    digitVariable.SetValue(numList.GetById(numberVariable));
                }
                else if (executionType is ExecutionType.Random)
                {
                    digitVariable.SetValue(numList.RandomObject());
                }
                else if (executionType is ExecutionType.Remove)
                {
                    numList.RemoveObject(digitVariable);
                }
                else if (executionType is ExecutionType.Clear)
                {
                    numList.ClearObject();
                }
                else if (executionType is ExecutionType.GetCount)
                {
                    numberVariable.SetValue(numList.GetCount());
                }
                else if (executionType is ExecutionType.Insert)
                {
                    numList.InsertObject(digits);
                }
                else if (executionType is ExecutionType.InsertValue)
                {
                    for (var i = 0; i < digits.Count; i++)
                        numList.InsertValue(digits[i].ShallowCopy());
                }
                else if (executionType is ExecutionType.Push)
                {
                    numList.PushObject(digits);
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private void OnChangeList ()
        {
            objects = new List<SceneObjectProperty>();
            MarkDirty();
        }

        private bool ShowListWarning ()
        {
            if (list == null || list.IsNoneType())
                return true;
            if (executionType is ExecutionType.GetById)
                if (list is not SceneObjectList {type: SceneObject.ObjectType.AttackStatusPack})
                    return true;
            return false;
        }

        public string GetListWarningMessage ()
        {
            if (list == null || list.IsNoneType())
                return "The assigned list have not specific type!";
            if (executionType is ExecutionType.GetById)
                if (list is not SceneObjectList {type: SceneObject.ObjectType.AttackStatusPack})
                    return "Only support for Attack Status Pack!";
            return string.Empty;
        }

        private void OnUpdateWords ()
        {
            if (words != null && list is WordList)
            {
                for (var i = 0; i < words.Count; i++)
                    MarkPropertyDirty(words[i]);
            }
        }
        
        private void OnUpdateDigits ()
        {
            if (digits != null && list is NumberList)
            {
                for (var i = 0; i < digits.Count; i++)
                    MarkPropertyDirty(digits[i]);
            }
        }

        private void OnUpdateObjects ()
        {
            if (objects != null && list is SceneObjectList soList)
            {
                var found = false;
                var save = false;
                for (var i = 0; i < objects.Count; i++)
                {
                    if (!objects[i].IsObjectValueType() && objects[i].variableValue != null)
                    {
                        if (soList.type != objects[i].variableValue.sceneObject.type)
                        {
                            objects[i].variableValue = null;
                            found = true;
                            save = true;
                        }
                    }

                    if (MarkPropertyDirty(objects[i]))
                    {
                        save = true;
                    }
                }

                if (found)
                {
                    EditorApplication.delayCall += () => { EditorUtility.DisplayDialog("List Behaviour Node Warning", "Found one or multiple variable have not match type!", "OK"); };
                }

                if (save)
                {
                    //-- NOTE Force scene dirty here due to property dirty is not working in this situation
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
        }

        private bool ShowAddObjects ()
        {
            if (executionType is ExecutionType.Insert or ExecutionType.Push or ExecutionType.InsertValue)
                if (list != null && !list.IsNoneType() && list is SceneObjectList)
                    return true;
            return false;
        }
        
        private bool ShowAddWords ()
        {
            if (executionType is ExecutionType.Insert or ExecutionType.Push or ExecutionType.InsertValue)
                if (list != null && list is WordList)
                    return true;
            return false;
        }
        
        private bool ShowAddDigits ()
        {
            if (executionType is ExecutionType.Insert or ExecutionType.Push or ExecutionType.InsertValue)
                if (list != null && list is NumberList)
                    return true;
            return false;
        }

        public void CreateNewListItem ()
        {
            if (list != null && !list.IsNoneType() && list is SceneObjectList soList)
            {
                var so = new SceneObjectProperty(soList.type);
                objects.Add(so);
                MarkDirty();
            }
        }

        private bool ShowObjectVariableWarning ()
        {
            if (objectVariable != null && list is SceneObjectList soList)
            {
                if (soList.type != objectVariable.sceneObject.type)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ShowObjectVariable ()
        {
            if (executionType is ExecutionType.Pop or ExecutionType.Take or ExecutionType.Remove or ExecutionType.Random or ExecutionType.GetByIndex)
                if (list != null && !list.IsNoneType() && list is SceneObjectList)
                    return true;
            if (executionType is ExecutionType.GetById)
                if (list != null && list is SceneObjectList {type: SceneObject.ObjectType.AttackStatusPack})
                    return true;
            return false;
        }
        
        public bool ShowWordVariable ()
        {
            if (executionType is ExecutionType.Pop or ExecutionType.Take or ExecutionType.Remove or ExecutionType.Random or ExecutionType.GetByIndex)
                if (list != null && list is WordList)
                    return true;
            return false;
        }
        
        public bool ShowDigitVariable ()
        {
            if (executionType is ExecutionType.Pop or ExecutionType.Take or ExecutionType.Remove or ExecutionType.Random or ExecutionType.GetByIndex)
                if (list != null && list is NumberList)
                    return true;
            return false;
        }

        public string ObjectVariableLabel ()
        {
            if (executionType is ExecutionType.Remove)
                return "Find";
            return "Store To";
        }

        public string NumberVariableLabel ()
        {
            if (executionType is ExecutionType.GetCount)
                return "Store To";
            if (executionType is ExecutionType.GetByIndex)
                return "Index";
            if (executionType is ExecutionType.GetById)
                return "Custom ID";
            return string.Empty;
        }

        public bool ShowNumberVariable ()
        {
            if (executionType is ExecutionType.GetCount or ExecutionType.GetByIndex)
                if (list != null && !list.IsNoneType())
                    return true;
            if (executionType is ExecutionType.GetById)
                if (list != null && list is SceneObjectList {type: SceneObject.ObjectType.AttackStatusPack})
                    return true;
            return false;
        }

        private void CreateNumberVariable ()
        {
            numberVariable = NumberVariable.CreateNew(numberVariable);
            dirty = true;
        }
        
        public ValueDropdownList<ExecutionType> TypeChoice ()
        {
            var typeListDropdown = new ValueDropdownList<ExecutionType>();
            typeListDropdown.Add("None", ExecutionType.None);
            typeListDropdown.Add("Insert", ExecutionType.Insert);
            typeListDropdown.Add("Push", ExecutionType.Push);
            typeListDropdown.Add("Take", ExecutionType.Take);
            typeListDropdown.Add("Pop", ExecutionType.Pop);
            typeListDropdown.Add("Random", ExecutionType.Random);
            typeListDropdown.Add("Get By Index", ExecutionType.GetByIndex);
            typeListDropdown.Add("Get By ID", ExecutionType.GetById);
            typeListDropdown.Add("Remove", ExecutionType.Remove);
            typeListDropdown.Add("Clear", ExecutionType.Clear);
            typeListDropdown.Add("InsertValue", ExecutionType.InsertValue);
            typeListDropdown.Add("GetCount", ExecutionType.GetCount);
            return typeListDropdown;
        }

        public static string displayName = "List Behaviour Node";
        public static string nodeName = "List";

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
            return $"Logic/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType is ExecutionType.Pop)
            {
                if (list && !list.IsNoneType())
                {
                    if (list is SceneObjectList soList)
                    {
                        if (objectVariable && soList.type == objectVariable.sceneObject.type)
                            return "Pop a list item";
                    }
                    else if (list is WordList)
                    {
                        if (wordVariable)
                            return "Pop a list item";
                    }
                    else if (list is NumberList)
                    {
                        if (digitVariable)
                            return "Pop a list item";
                    }
                }
            }
            else if (executionType is ExecutionType.Take)
            {
                if (list && !list.IsNoneType())
                {
                    if (list is SceneObjectList soList)
                    {
                        if (objectVariable && soList.type == objectVariable.sceneObject.type)
                            return "Take a list item";
                    }
                    else if (list is WordList)
                    {
                        if (wordVariable)
                            return "Take a list item";
                    }
                    else if (list is NumberList)
                    {
                        if (digitVariable)
                            return "Take a list item";
                    }
                }
            }
            else if (executionType is ExecutionType.GetByIndex)
            {
                if (list && !list.IsNoneType())
                {
                    if (list is SceneObjectList soList)
                    {
                        if (objectVariable && soList.type == objectVariable.sceneObject.type && numberVariable)
                            return "Get a list item at index " + numberVariable;
                    }
                    else if (list is WordList)
                    {
                        if (wordVariable && numberVariable)
                            return "Get a list item at index " + numberVariable;
                    }
                    else if (list is NumberList)
                    {
                        if (digitVariable && numberVariable)
                            return "Get a list item at index " + numberVariable;
                    }
                }
            }
            else if (executionType is ExecutionType.GetById)
            {
                if (list && !list.IsNoneType())
                {
                    if (list is SceneObjectList soList)
                    {
                        if (objectVariable && soList.type == objectVariable.sceneObject.type && numberVariable)
                            return "Get a list item with ID " + numberVariable;
                    }
                    else if (list is WordList)
                    {
                        if (wordVariable && numberVariable)
                            return "Get a list item with ID " + numberVariable;
                    }
                    else if (list is NumberList)
                    {
                        if (digitVariable && numberVariable)
                            return "Get a list item with ID " + numberVariable;
                    }
                }
            }
            else if (executionType is ExecutionType.Random)
            {
                if (list && !list.IsNoneType())
                {
                    if (list is SceneObjectList soList)
                    {
                        if (objectVariable && soList.type == objectVariable.sceneObject.type)
                            return "Random a list item";
                    }
                    else if (list is WordList)
                    {
                        if (wordVariable)
                            return "Random a list item";
                    }
                    else if (list is NumberList)
                    {
                        if (digitVariable)
                            return "Random a list item";
                    }
                }
            }
            else if (executionType is ExecutionType.Remove)
            {
                if (list && !list.IsNoneType())
                {
                    if (list is SceneObjectList soList)
                    {
                        if (objectVariable && soList.type == objectVariable.sceneObject.type)
                            return "Remove a list item";
                    }
                    else if (list is WordList)
                    {
                        if (wordVariable)
                            return "Remove a list item";
                    }
                    else if (list is NumberList)
                    {
                        if (digitVariable)
                            return "Remove a list item";
                    }
                }
            }
            else if (executionType is ExecutionType.Clear)
            {
                if (list && !list.IsNoneType())
                    return "Clear the list";
            }
            else if (executionType is ExecutionType.GetCount)
            {
                if (list && !list.IsNoneType() && numberVariable != null)
                    return $"Get the list count into {numberVariable.name}";
            }
            else if (executionType is ExecutionType.Insert)
            {
                if (list && !list.IsNoneType())
                {
                    if (list is SceneObjectList)
                    {
                        if (objects.Count > 0)
                        {
                            for (var i = 0; i < objects.Count; i++)
                                if (!objects[i].IsNull)
                                    return "Insert item(s) into list";
                        }
                    }
                    else if (list is WordList)
                    {
                        if (words.Count > 0)
                        {
                            for (var i = 0; i < words.Count; i++)
                                if (!words[i].IsNull())
                                    return "Insert item(s) into list";
                        }
                    }
                    else if (list is NumberList)
                    {
                        if (digits.Count > 0)
                        {
                            for (var i = 0; i < digits.Count; i++)
                                if (!digits[i].IsNull())
                                    return "Insert item(s) into list";
                        }
                    }
                }
            }
            else if (executionType is ExecutionType.InsertValue)
            {
                if (list && !list.IsNoneType())
                {
                    if (list is SceneObjectList)
                    {
                        if (objects.Count > 0)
                        {
                            for (var i = 0; i < objects.Count; i++)
                                if (!objects[i].IsNull)
                                    return "Insert items' value into list";
                        }
                    }
                    else if (list is WordList)
                    {
                        if (words.Count > 0)
                        {
                            for (var i = 0; i < words.Count; i++)
                                if (!words[i].IsNull())
                                    return "Insert items' value into list";
                        }
                    }
                    else if (list is NumberList)
                    {
                        if (digits.Count > 0)
                        {
                            for (var i = 0; i < digits.Count; i++)
                                if (!digits[i].IsNull())
                                    return "Insert items' value into list";
                        }
                    }
                }
            }
            else if (executionType is ExecutionType.Push)
            {
                if (list && !list.IsNoneType())
                {
                    if (list is SceneObjectList)
                    {
                        if (objects.Count > 0)
                        {
                            for (var i = 0; i < objects.Count; i++)
                                if (!objects[i].IsNull)
                                    return "Push item(s) into list";
                        }
                    }
                    else if (list is WordList)
                    {
                        if (words.Count > 0)
                        {
                            for (var i = 0; i < words.Count; i++)
                                if (!words[i].IsNull())
                                    return "Push item(s) into list";
                        }
                    }
                    else if (list is NumberList)
                    {
                        if (digits.Count > 0)
                        {
                            for (var i = 0; i < digits.Count; i++)
                                if (!digits[i].IsNull())
                                    return "Push item(s) into list";
                        }
                    }
                }
            }

            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            return "This will provide several controls to a specific Scene Object List.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}