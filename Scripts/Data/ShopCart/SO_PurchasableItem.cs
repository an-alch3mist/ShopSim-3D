using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;
// ============================================================
//  SO_PurchasableItem.cs
//  One asset per item sold in the shop.
//
//  Packaging rule:
//    unitsPerBox = 100  →  440 ordered = 5 boxes (4 × 100, 1 × 40)
//    unitsPerBox = 1    →  shelf, table, belt = 1 item, 1 box
//
//  linkedItemData:  optional bridge to the shelf-stocking system.
//                   A milk SO_PurchasableItem can point at the
//                   milk SO_ItemData so AutoStockService and the
//                   shop share the same item identity.
//
//  gridSize:        reserved for Phase-2 placement (integer grid).
//                   Not read at runtime during the shop-cart phase.
// ============================================================
[CreateAssetMenu(fileName = "SO_purchasableItem", menuName = "ShopSim_SO/SO_purchasableItem")]
public class SO_PurchasableItem : ScriptableObject
{
	[Header("Identity")]
	public string itemId = "item_id";
	public string displayName = "Item Name";
	public Sprite icon;

	[Header("Category")]
	public SO_ShopCategory category;

	[Header("Pricing")]
	[Min(0f)] public float unitPrice = 1f;

	[Header("Packaging")]
	[Tooltip("Units that fit in one physical delivery box.\n" +
			 "e.g. Milk = 100, Shelf = 1, Chair = 1")]
	[Min(1)] public int unitsPerBox = 1;
	[Tooltip("Prefab spawned at the delivery point — the outer box.")]
	public GameObject boxPrefab;

	[Header("In-World Object")]
	[Tooltip("The actual GameObject placed / used in the world (Phase 2+).")]
	public GameObject itemPrefab;

	[Header("Grid (Phase 2 — reserved, not read yet)")]
	[Tooltip("Footprint in integer grid units  (x width, y height, z depth).")]
	public Vector3Int gridSize = Vector3Int.one;

	[Header("Link to Shelf-Stocking System (optional)")]
	[Tooltip("Binds this purchasable item to an SO_ItemData used by ShelfPOI / AutoStockService.")]
	public SO_ItemData linkedItemData;
}
