using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

// ============================================================
//  Pure label data.  One asset per category.
//  Create → ShopSim_SO → SO_shopCategory
//  e.g. "Furniture", "Stock", "Equipment", "Decor"
// ============================================================
[CreateAssetMenu(fileName = "SO_shopCategory",
				 menuName = "ShopSim_SO/SO_shopCategory")]
public class SO_ShopCategory : ScriptableObject
{
	public string categoryId = "category-id";
	public string displayName = "categoryName";
}
