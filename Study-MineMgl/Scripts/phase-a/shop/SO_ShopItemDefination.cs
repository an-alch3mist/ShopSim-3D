using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

using SPACE_UTIL;

namespace SPACE_MineMGL
{
	[CreateAssetMenu(fileName = "new SO_shopItemDef",
					 menuName = "Shop/SO_ShopItemDef")]
	/// <summary>
	/// scriptable object defining a purchasable - shop item.
	/// </summary>
	public class SO_ShopItemDefination : ScriptableObject
	{
		public string itemDefName = "shopItemDefName";
		[TextArea(minLines: 2, maxLines: 3)]
		public string descr = @"";

		public bool isLockedByDefaultStart = false;
		public int price = 25;
		public Sprite icon;
		public GameObject pfToSpawn;
		public int maxStackSize = 10;

		#region public API
		public string GetName() { return this.itemDefName; }
		public string GetDescr() { return this.descr; }
		public Sprite GetIcon() { return this.icon; }
		#endregion
	}
}