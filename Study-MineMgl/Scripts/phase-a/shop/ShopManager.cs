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
	/// init shop item wrappers from shop category, and later manage all runtimes.
	/// </summary>
	[DefaultExecutionOrder(-100)] // after INITManager
	public class ShopManager : Singleton<ShopManager>
	{
		[SerializeField] List<SO_ShopCategory> _ALL_SHOP_CATEGORY = new List<SO_ShopCategory>();
		// List<SO_ShopItemDefination> ALL_ITEM_DEF = new List<SO_ShopItemDefination>();
		List<ShopItem> ALL_ITEM = new List<ShopItem>();

		#region Unity Life Cycle
		protected override void Awake()
		{
			Debug.Log(C.method(this));
			base.Awake();
		}

		private void Start()
		{
			Debug.Log(C.method(this));
			this.Init_AllCATEGORY_ITEM();
		}
		#endregion

		#region private API
		void Init_AllCATEGORY_ITEM()
		{
			this.ALL_ITEM = new List<ShopItem>();
			this._ALL_SHOP_CATEGORY.forEach(category =>
			{
				List<ShopItem> ITEM = new List<ShopItem>();
				category.SHOP_ITEM_DEF.forEach(itemDef =>
				{
					ShopItem itemExisting = this.FindItemByDef(itemDef);
					if (itemExisting != null)
						ITEM.Add(itemExisting);
					else
					{
						ShopItem newItem = new ShopItem(itemDef);
						ITEM.Add(newItem);
						ALL_ITEM.Add(newItem);
					}
				});
				category.Set_SHOP_ITEM(ITEM);
			});
			//
			LOG.AddLog(PhaseALOG.SHOP_CATEGORY_LIST__TO__JSON(this._ALL_SHOP_CATEGORY), "json");
		}
		#endregion

		#region public API
		public List<SO_ShopCategory> GetAllCategories() { return this._ALL_SHOP_CATEGORY; }
		public List<ShopItem> GetAllItems() { return this.ALL_ITEM; }

		public ShopItem FindItemByDef(SO_ShopItemDefination itemDef)
		{
			foreach (var item in this.ALL_ITEM)
				if (item.itemDef == itemDef)
					return item;
			return null;
		}
		public void UnlockShopItemByDef(SO_ShopItemDefination itemDef)
		{
			ShopItem item = this.FindItemByDef(itemDef);
			if(item != null)
				if(item.isLockedCurr == true)
				{
					item.isLockedCurr = false;
					GameEvents.RaiseShopItemUnlocked(item);
				}
		}
		/// <summary>
		/// unlock all shop items in every category
		/// </summary>
		public void UnlockAllShopItems()
		{
			this.ALL_ITEM.ForEach(item =>
			{
				item.isLockedCurr = false;
			});
		}
		#endregion
	}
}