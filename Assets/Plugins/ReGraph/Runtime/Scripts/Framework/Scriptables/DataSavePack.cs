using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Reshape.Unity;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Data Save Pack", fileName = "DataSavePack", order = 15)]
    [Serializable]
    [HideMonoScript]
    public class DataSavePack : SavePack
    {
        private const string SAVE_ALL_NAME = "names";
        private const string SAVE_ALL_NAME_PASSWORD = "namespassword";
        
        [Serializable]
        public struct DataInfo
        {
            public enum Type
            {
                Number = 0,
                Word = 1,
            }

            [HorizontalGroup(Width = 0.7f)]
            [HideLabel]
            [InlineProperty]
            public StringProperty name;

            [HorizontalGroup(Width = 0.3f)]
            [HideLabel, HideInInlineEditors]
            [ValueDropdown("TypeChoice")]
            public Type type;

            [HideInInspector]
            public string strValue;

            [HideInInspector]
            public float floatValue;

            private bool inited;

            [HideIf("HideValue")]
            [ShowInInspector]
            [HorizontalGroup(Width = 0.3f)]
            [HideLabel]
            public string value
            {
                get
                {
                    if (type == Type.Number)
                        return floatValue.ToString(CultureInfo.InvariantCulture);
                    if (type == Type.Word)
                        return strValue;
                    return string.Empty;
                }
            }

            public void Init ()
            {
                inited = true;
            }

            public new string ToString ()
            {
                if (type == Type.Number)
                    return floatValue.ToString(CultureInfo.InvariantCulture);
                if (type == Type.Word)
                    return strValue;
                return string.Empty;
            }

            public void Reset ()
            {
                floatValue = 0f;
                strValue = string.Empty;
            }

#if UNITY_EDITOR
            private bool HideValue ()
            {
                return !inited;
            }

            private IEnumerable TypeChoice ()
            {
                var listDropdown = new ValueDropdownList<Type> {{"Number", Type.Number}, {"Word", Type.Word}};
                return listDropdown;
            }
#endif
        }

        [Space(4)]
        [DisableInPlayMode]
        public List<DataInfo> datas;
        
        private int type = TYPE_DATA;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static void DeleteAllCharacters (string saveTag, string password)
        {
            var op = ReSave.Load(GetSaveFileName(SAVE_ALL_NAME, saveTag, TYPE_CHARACTER), SAVE_ALL_NAME_PASSWORD, false);
            if (op.success)
            {
                var names = op.savedString.Split(ReExtensions.STRING_COMMA);
                var nameList = new List<string>(names);
                for (var i = 0; i < names.Length; i++)
                {
                    if (string.IsNullOrEmpty(names[i]))
                    {
                        nameList.Remove(string.Empty);
                        continue;
                    }
                    
                    var deleted = ReSave.Delete(names[i], password);
                    if (deleted.success)
                        nameList.Remove(names[i]);
                }
                
                if (nameList.Count > 0)
                    SaveCharacterNames(nameList, saveTag);
                else
                    ReSave.Delete(GetSaveFileName(SAVE_ALL_NAME, saveTag, TYPE_CHARACTER), SAVE_ALL_NAME_PASSWORD);
            }
        }
        
        private static void SaveCharacterNames (List<string> nameList, string saveTag)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < nameList.Count; i++)
                if (!string.IsNullOrEmpty(nameList[i]))
                    sb.Append($"{nameList[i]}{ReExtensions.STRING_COMMA}"); 
            var op = ReSave.Save(GetSaveFileName(SAVE_ALL_NAME, saveTag, TYPE_CHARACTER), sb.ToString(), SAVE_ALL_NAME_PASSWORD);
            if (!op.success)
                ReDebug.LogWarning("CharacterSavePack", SAVE_ALL_NAME + " not successfully save!");
        }
        
        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public bool GetValue (string dataName, out string value)
        {
            value = string.Empty;
            for (var j = 0; j < datas.Count; j++)
            {
                var data = datas[j];
                if (data.name.Equals(dataName))
                {
                    if (data.type == DataInfo.Type.Word)
                    {
                        value = data.strValue;
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        public bool GetValue (string dataName, out float value)
        {
            value = 0f;
            for (var j = 0; j < datas.Count; j++)
            {
                var data = datas[j];
                if (data.name.Equals(dataName))
                {
                    if (data.type == DataInfo.Type.Number)
                    {
                        value = data.floatValue;
                        return true;
                    }
                }
            }

            return false;
        }
        
        public VariableScriptableObject GetValue (string dataName, VariableScriptableObject variable)
        {
            for (var j = 0; j < datas.Count; j++)
            {
                var data = datas[j];
                if (data.name.Equals(dataName))
                {
                    if (data.type == DataInfo.Type.Number && variable is NumberVariable num)
                    {
                        num.SetValue(data.floatValue);
                        return num;
                    }
                     
                    if (data.type == DataInfo.Type.Word && variable is WordVariable word)
                    {
                        word.SetValue(data.strValue);
                        return word;
                    }

                    break;
                }
            }

            return variable;
        }
        
        public void SetValue (string dataName, string value)
        {
            for (var j = 0; j < datas.Count; j++)
            {
                var data = datas[j];
                if (data.name.Equals(dataName))
                {
                    if (data.type == DataInfo.Type.Number)
                    {
                        if (float.TryParse(value, out var result))
                        {
                            data.floatValue = result;
                            datas[j] = data;
                        }
                    }
                     
                    if (data.type == DataInfo.Type.Word)
                    {
                        data.strValue = value;
                        datas[j] = data;
                    }

                    break;
                }
            }
        }

        public bool Match (string value)
        {
            return string.Equals(fileName, value);
        }
        
        public bool Match (string n, int t, string id)
        {
            return string.Equals(fileName, n) && type == t && string.Equals(fileNamePostfix, id);
        }

        [SpecialName]
        public void SetId (string value)
        {
            fileNamePostfix = value;
        }
        
        [SpecialName]
        public void SetType (int t)
        {
            type = t;
        }

        [SpecialName]
        public void Init ()
        {
            printLog = false;
            for (var i = 0; i < datas.Count; i++)
            {
                var data = datas[i];
                data.Init();
                datas[i] = data;
            }
        }

        [SpecialName]
        public void Save ()
        {
            if (string.IsNullOrEmpty(fileName) || datas is not {Count: > 0})
                return;
            var dict = new Dictionary<string, object>();
            for (var i = 0; i < datas.Count; i++)
            {
                var data = datas[i];
                if (!string.IsNullOrEmpty(data.name))
                    dict.Add(data.name, data.value);
            }

            var save = SaveFile(dict, type);
            if (save.success && type == TYPE_CHARACTER)
            {
                var nameList = new List<string>() { save.fileName };
                var op = ReSave.Load(GetSaveFileName(SAVE_ALL_NAME, saveTag, TYPE_CHARACTER), SAVE_ALL_NAME_PASSWORD, false);
                if (op.success)
                {
                    
                    var names = op.savedString.Split(ReExtensions.STRING_COMMA);
                    nameList = new List<string>(names);
                    if (!nameList.Contains(save.fileName))
                    {
                        nameList.Add(save.fileName);
                        SaveCharacterNames(nameList, saveTag);
                    }
                }
                else
                {
                    SaveCharacterNames(nameList, saveTag);
                }
            }
        }

        [SpecialName]
        public bool Load ()
        {
            if (string.IsNullOrEmpty(fileName) || datas is not {Count: > 0})
                return false;
            var dict = LoadFile(type);
            if (dict != null)
            {
                foreach (var save in dict)
                {
                    for (var i = 0; i < datas.Count; i++)
                    {
                        var data = datas[i];
                        if (data.name == save.Key)
                        {
                            if (data.type == DataInfo.Type.Number)
                                if (float.TryParse(save.Value.ToString(), out var result))
                                    data.floatValue = result;
                            if (data.type == DataInfo.Type.Word)
                                data.strValue = save.Value.ToString();
                            datas[i] = data;
                            break;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        [SpecialName]
        public void Delete (bool handleNames)
        {
            if (string.IsNullOrEmpty(fileName) || datas is not {Count: > 0})
                return;
            var deleted = DeleteFile(type);
            if (handleNames)
                HandleCharacterNamesDelete(deleted);
        }
        
        [SpecialName]
        public void Clear ()
        {
            if (string.IsNullOrEmpty(fileName) || datas is not {Count: > 0})
                return;
            for (var i = 0; i < datas.Count; i++)
            {
                var data = datas[i];
                data.Reset();
                datas[i] = data;
            }
        }

        public void DeleteSave (string value)
        {
            if (string.IsNullOrEmpty(fileName) || datas is not {Count: > 0})
                return;
            var previousName = fileNamePostfix;
            fileNamePostfix = value;
            var deleted = DeleteFile(type);
            fileNamePostfix = previousName;
            HandleCharacterNamesDelete(deleted);
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

        private void HandleCharacterNamesDelete (SaveOperation deleted)
        {
            if (deleted.success && type == TYPE_CHARACTER)
            {
                var op = ReSave.Load(GetSaveFileName(SAVE_ALL_NAME, saveTag, TYPE_CHARACTER), SAVE_ALL_NAME_PASSWORD, false);
                if (op.success)
                {
                    
                    var names = op.savedString.Split(ReExtensions.STRING_COMMA);
                    var nameList = new List<string>(names);
                    nameList.Remove(deleted.fileName);
                    SaveCharacterNames(nameList, saveTag);
                }
            }
        }

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}