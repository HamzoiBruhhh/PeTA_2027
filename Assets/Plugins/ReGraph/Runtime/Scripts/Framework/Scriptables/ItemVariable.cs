using System;
using Reshape.Unity;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
#if REGRAPH_DEV_DEBUG
	[CreateAssetMenu(menuName="Reshape/Item Variable", order = 502)]
#endif
	[Serializable]
	public class ItemVariable : VariableScriptableObject
	{
		private const string Item = "Item ";
		
		[ReadOnly]
		[HideInEditorMode]
		public ItemData runtimeValue;
		
		public override bool supportSaveLoad => false;
		
		public ItemData GetValue ()
		{
			return runtimeValue;
		}

		public void SetValue (ItemData value)
		{
			if (value != null && !IsEqual(value))
			{
				runtimeValue = value;
				OnChanged();
			}
		}

		public bool IsEqual(ItemData value)
		{
			return runtimeValue.id == value.id;
		}

		public static implicit operator string(ItemVariable i)
	    {
	        return i.ToString();
	    }

	    public override string ToString()
	    {
		    return Item + runtimeValue.displayName;
	    }

	    protected override void OnChanged()
	    {
		    if (!resetLinked)
		    {
			    onReset -= OnReset;
			    onReset += OnReset;
			    resetLinked = true;
		    }
		    
		    base.OnEarlyChanged();
		    base.OnChanged();
	    }
	    
	    public override void Rebase ()
	    {
		    runtimeValue = null;
	    }

	    public override void OnReset()
	    {
		    Rebase();
		    base.OnReset();
	    }
	}
}