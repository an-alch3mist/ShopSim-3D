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
	/// <summary>
	/// runtime wrapper around SO_ShopItemDef
	/// </summary>
	[System.Serializable]
	public class ShopItem : MonoBehaviour
	{
		[SerializeField] public SO_ShopItemDefination itemDef;
		[SerializeField] public bool isLockedCurr;
		[SerializeField] private int timesPurchased;
		public ShopItem(SO_ShopItemDefination itemDef)
		{
			this.itemDef = itemDef;
			this.isLockedCurr = itemDef.isLockedByDefaultStart;
			this.timesPurchased = 0;
		}

		#region public API
		public string GetName() { return this.itemDef.itemDefName; }
		public string GetDescr() { return this.itemDef.descr; }
		public int GetTimesPurchased() { return this.timesPurchased; }
		public void AddPurchasedQuantity(int quantity)
		{
			this.timesPurchased += quantity;
		}
		/// <summary>
		/// locked initially, and now its no longer locked at haven't purchased yet.
		/// </summary>
		/// <returns></returns>
		public bool IsNewlyUnlocked()
		{
			return (this.itemDef.isLockedByDefaultStart == true) && // locked initially
				   (this.isLockedCurr == false) && (this.timesPurchased == 0); // now its no longer locked at haven't purchased yet
		}
		#endregion
	}
}