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
	[CreateAssetMenu(fileName = "new SO_Shopcategory",
					 menuName = "Shop/SO_ShopCategory")]
	// scriptable object to group items into browsable category form.
	public class SO_ShopCategory : ScriptableObject
	{
		public string categoryName = "categoryName";
		public List<SO_ShopItemDefination> SHOP_ITEM_DEF = new List<SO_ShopItemDefination>();
		public bool shouldHideIfAllLocked = false;

		#region public API, nonSerialized
		public List<ShopItem> SHOP_ITEM = new List<ShopItem>();
		public void Set_SHOP_ITEM(List<ShopItem> SHOP_ITEM)
		{
			this.SHOP_ITEM = SHOP_ITEM;
		}

		/// <summary>
		/// returns true if any item is newly unlocked && not-purchased
		/// </summary>
		/// <returns></returns>
		public bool ContainsNewItems()
		{
			foreach (var shopItem in this.SHOP_ITEM)
				if (shopItem.IsNewlyUnlocked())
					return true;
			return false;
		}
		#endregion
	}
}