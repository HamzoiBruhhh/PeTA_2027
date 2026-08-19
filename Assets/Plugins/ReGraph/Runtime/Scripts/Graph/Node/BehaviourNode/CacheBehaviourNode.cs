using System;
using System.Collections;
using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class CacheBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            SetItem = 101,
            SetNumber = 1001,
            SetWord = 1011,
            SetCharacter = 1101,
            GetNumber = 101001,
            GetCharacter = 101101,
        }

        [SerializeField]
        [OnValueChanged("OnChangeType")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType = ExecutionType.SetItem;

        [SerializeField]
        [OnInspectorGUI("OnUpdateName")]
        [InlineProperty]
        [LabelText("Cache Name")]
        private StringProperty name;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ValueDropdown("ItemChoice")]
        [InlineButton("@ItemExplorer.OpenWindow()", "✚")]
        [ShowIf("@executionType == ExecutionType.SetItem")]
        [OnInspectorGUI("OnUpdateItem")]
        private ItemData item;

        [LabelText("Number")]
        [ShowIf("@executionType == ExecutionType.SetNumber || executionType == ExecutionType.GetNumber")]
        [OnInspectorGUI("@MarkPropertyDirty(number1)")]
        [InlineProperty]
        public FloatProperty number1;

        [LabelText("Word")]
        [ShowIf("@executionType == ExecutionType.SetWord")]
        [OnInspectorGUI("@MarkPropertyDirty(word1)")]
        [InlineProperty]
        public StringProperty word1;

        [ShowIf("@executionType == ExecutionType.SetCharacter || executionType == ExecutionType.GetCharacter")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(character)")]
        [InfoBox("@character.GetMismatchWarningMessage()", InfoMessageType.Error, "@character.IsShowMismatchWarning()")]
        public SceneObjectProperty character = new SceneObjectProperty(SceneObject.ObjectType.CharacterOperator, "Character");

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType == ExecutionType.SetItem)
            {
                if (string.IsNullOrEmpty(name) || item == null)
                {
                    LogWarning("Found an empty Cache Behaviour node in " + context.objectName);
                }
                else
                {
                    if (IsItemNotExist())
                    {
                        LogWarning("Found an empty Cache Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        if (!context.isScriptableGraph)
                            context.SetCache(name, item);
                        else if (context.scriptable.graph.isAttackSkillPack)
                            execution.parameters.attackSkillData.skillMuscle.SetCache(name, item);
                    }
                }
            }
            else if (executionType == ExecutionType.SetNumber)
            {
                if (string.IsNullOrEmpty(name) || number1.IsNull())
                {
                    LogWarning("Found an empty Cache Behaviour node in " + context.objectName);
                }
                else
                {
                    if (!context.isScriptableGraph)
                        context.SetCache(name, number1.value);
                    else if (context.scriptable.graph.isAttackSkillPack)
                        execution.parameters.attackSkillData.skillMuscle.SetCache(name, number1.value);
                    else if (context.scriptable.graph.isAttackStatusPack)
                        execution.parameters.attackStatusData.SetCache(name, number1.value);
                }
            }
            else if (executionType == ExecutionType.GetNumber)
            {
                if (string.IsNullOrEmpty(name) || !number1.IsVariable())
                {
                    LogWarning("Found an empty Cache Behaviour node in " + context.objectName);
                }
                else
                {
                    object cacheObj = null;
                    if (!context.isScriptableGraph)
                        cacheObj = context.GetCache(name);
                    else if (context.scriptable.graph.isAttackSkillPack)
                        cacheObj = execution.parameters.attackSkillData.skillMuscle.GetCache(name);
                    else if (context.scriptable.graph.isAttackStatusPack)
                        cacheObj = execution.parameters.attackStatusData.GetCache(name);
                    if (cacheObj != null)
                        number1.SetVariableValue((float) cacheObj);
                }
            }
            else if (executionType == ExecutionType.SetWord)
            {
                if (string.IsNullOrEmpty(name) || word1.IsNull())
                {
                    LogWarning("Found an empty Cache Behaviour node in " + context.objectName);
                }
                else
                {
                    if (!context.isScriptableGraph)
                        context.SetCache(name, word1.value);
                    else if (context.scriptable.graph.isAttackSkillPack)
                        execution.parameters.attackSkillData.skillMuscle.SetCache(name, word1.value);
                }
            }
            else if (executionType == ExecutionType.SetCharacter)
            {
                if (string.IsNullOrEmpty(name) || character == null || character.IsEmpty || !character.IsMatchType())
                {
                    LogWarning("Found an empty Cache Behaviour node in " + context.objectName);
                }
                else
                {
                    var charOperator = (CharacterOperator) character;
                    if (charOperator == null)
                    {
                        LogWarning("Found an empty Cache Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        if (!context.isScriptableGraph)
                            context.SetCache(name, charOperator);
                        else if (context.scriptable.graph.isAttackSkillPack)
                            execution.parameters.attackSkillData.skillMuscle.SetCache(name, charOperator);
                    }
                }
            }
            else if (executionType == ExecutionType.GetCharacter)
            {
                if (string.IsNullOrEmpty(name) || character == null || character.IsNull || !character.IsMatchType() || !character.IsVariableValueType())
                {
                    LogWarning("Found an empty Cache Behaviour node in " + context.objectName);
                }
                else
                {
                    object cacheObj = null;
                    if (!context.isScriptableGraph)
                        cacheObj = context.GetCache(name);
                    else if (context.scriptable.graph.isAttackSkillPack)
                        cacheObj = execution.parameters.attackSkillData.skillMuscle.GetCache(name);
                    if (cacheObj != null)
                        character.SetVariableValue((CharacterOperator) cacheObj);
                }
            }

            base.OnStart(execution, updateId);
        }

        public bool IsItemNotExist ()
        {
            if (item == null)
                return true;
            object tempObj = item;
            if (tempObj != null && tempObj.ToString() == "null")
                return true;
            return false;
        }

        public string GetCacheSavedName ()
        {
            return name;
        }

#if UNITY_EDITOR
        public void OnChangeType ()
        {
            if (executionType == ExecutionType.GetNumber)
                number1.AllowVariableOnly();
            else
                number1.AllowAll();
            if (executionType == ExecutionType.GetCharacter)
                character.AllowVariableOnly();
            else
                character.AllowAll();
            MarkDirty();
        }

        public void OnUpdateName ()
        {
            var graph = GetGraph();
            if (MarkPropertyDirty(name))
            {
                if (graph is {nodes: { }})
                {
                    for (var i = 0; i < graph.nodes.Count; i++)
                    {
                        if (graph.nodes[i] is InventoryBehaviourNode)
                        {
                            var invNode = (InventoryBehaviourNode) graph.nodes[i];
                            if (invNode.GetCacheItemType())
                                invNode.dirty = true;
                        }
                        else if (graph.nodes[i] is InventoryTriggerNode)
                        {
                            var invNode = (InventoryTriggerNode) graph.nodes[i];
                            if (invNode.GetCacheItemType())
                                invNode.dirty = true;
                        }
                    }
                }
            }
            
            if (graph != null)
            {
                if (graph.isAttackStatusPack && executionType == ExecutionType.SetItem)
                {
                    executionType = ExecutionType.SetNumber;
                }
            }
        }

        public void OnUpdateItem ()
        {
            if (IsItemNotExist())
            {
                item = null;
                MarkDirty();
            }
        }

        public ItemData GetItemCacheChoice ()
        {
            if (executionType == ExecutionType.SetItem)
            {
                if (!string.IsNullOrEmpty(name) && item != null)
                    if (!IsItemNotExist())
                        return item;
            }

            return null;
        }

        public FloatProperty GetNumberCacheChoice ()
        {
            if (executionType is ExecutionType.SetNumber or ExecutionType.GetNumber)
            {
                if (!string.IsNullOrEmpty(name) && !number1.IsNull())
                    return number1;
            }

            return null;
        }

        public string GetCacheName ()
        {
            if (executionType == ExecutionType.SetItem)
            {
                if (!string.IsNullOrEmpty(name) && item != null)
                    return name;
            }
            else if (executionType is ExecutionType.SetNumber or ExecutionType.GetNumber)
            {
                if (!string.IsNullOrEmpty(name) && !number1.IsNull())
                    return name;
            }

            return string.Empty;
        }

        private IEnumerable ItemChoice ()
        {
            var itemListDropdown = new ValueDropdownList<ItemData>();
            var guids = AssetDatabase.FindAssets("t:ItemList");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var itemList = AssetDatabase.LoadAssetAtPath<ItemList>(path);
                for (int j = 0; j < itemList.items.Count; j++)
                {
                    var item = itemList.items[j];
                    itemListDropdown.Add(itemList.name + "/" + item.displayName, item);
                }
            }

            return itemListDropdown;
        }

        private IEnumerable TypeChoice ()
        {
            var listDropdown = new ValueDropdownList<ExecutionType>();
            var graph = GetGraph();
            if (graph != null)
            {
                if (graph.isAttackStatusPack)
                {
                    listDropdown.Add("Set Number", ExecutionType.SetNumber);
                    listDropdown.Add("Get Number", ExecutionType.GetNumber);
                }
                else
                {
                    listDropdown.Add("Set Number", ExecutionType.SetNumber);
                    listDropdown.Add("Get Number", ExecutionType.GetNumber);
                    listDropdown.Add("Set Word", ExecutionType.SetWord);
                    listDropdown.Add("Set Item", ExecutionType.SetItem);
                    listDropdown.Add("Set Character", ExecutionType.SetCharacter);
                    listDropdown.Add("Get Character", ExecutionType.GetCharacter);
                }
            }
            
            return listDropdown;
        }

        public static string displayName = "Cache Behaviour Node";
        public static string nodeName = "Cache";

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
            if (name.IsAssigned())
            {
                var nameDisplay = string.IsNullOrEmpty(name) ? name.GetDisplayName() : name;
                if (executionType == ExecutionType.SetItem)
                {
                    if (item != null)
                        return $"Set {item.displayName} into cache {nameDisplay}";
                }
                else if (executionType == ExecutionType.SetNumber)
                {
                    if (!number1.IsNull())
                    {
                        if (number1.IsVariable())
                            return $"Set {number1.GetVariableName()} into cache {nameDisplay}";
                        return $"Set {number1} into cache {nameDisplay}";
                    }
                }
                else if (executionType == ExecutionType.GetNumber)
                {
                    if (!number1.IsNull())
                    {
                        if (number1.IsVariable())
                            return $"Get cache {nameDisplay} into {number1.GetVariableName()}";
                        return $"Set cache {nameDisplay} into {number1}";
                    }
                }
                else if (executionType == ExecutionType.SetWord)
                {
                    if (!word1.IsNull())
                    {
                        if (word1.IsVariable())
                            return $"Set {word1.GetVariableName()} into cache {nameDisplay}";
                        return $"Set {word1} into cache {nameDisplay}";
                    }
                }
                else if (executionType == ExecutionType.SetCharacter)
                {
                    if (!character.IsNull && character.IsMatchType())
                        return $"Set {character.objectName} into cache {nameDisplay}";
                }
                else if (executionType == ExecutionType.GetCharacter)
                {
                    if (!character.IsNull && character.IsMatchType())
                    {
                        if (character.IsVariableValueType())
                            return $"Get cache {nameDisplay} into {character.objectName}";
                    }
                }
            }

            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            return "This will execute all Cache Aim related behaviour.\n\nCache is save the value into the graph internal memory. This allow the graph to operate more flexibility.\n\n" +
                   base.GetNodeViewTooltip();
        }
#endif
    }
}