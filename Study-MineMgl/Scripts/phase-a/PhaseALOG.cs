using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

namespace SPACE_MineMGL
{
	public static class PhaseALOG
	{
		public static string SHOP_CATEGORY_LIST__TO__JSON(List<SO_ShopCategory> LIST)
		{
			/*
			var snapshot_prev = LIST.map(category =>
			{
				return new
				{
					category.categoryName,
					category.SHOP_ITEM,
				};
			});
			*/

			var snapshot = LIST.map(cat => new
			{
				cat.categoryName,
				cat.shouldHideIfAllLocked,
				SHOP_ITEM_DEF = cat.SHOP_ITEM_DEF.map(def => new
				{
					def.itemDefName,
					def.descr,
					def.price,
					def.maxStackSize,
					def.isLockedByDefaultStart,
					icon = def.icon.name,
					pfToSpawn = def.pfToSpawn.name,
				}),
				SHOP_ITEM = cat.SHOP_ITEM.map(item => new
				{
					item.itemDef.itemDefName,   // pull from the def, not the wrapper
					item.isLockedCurr,
					timesPurchasedRef = item.GetTimesPurchased(),
				})
			});
			return snapshot.ToNSJson(pretify: true);
		}
	} 
}
