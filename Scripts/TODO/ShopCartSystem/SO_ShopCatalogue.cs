using UnityEngine;
using System.Collections.Generic;

// ============================================================
//  SO_ShopCatalogue.cs
//  Single asset that owns the master item list shown in the shop.
//  Drag SO_PurchasableItem assets into the list in the inspector.
//
//  Decouples the UI from hard-coded item references:
//    ShopCatalogueUI only needs this one SO, never a concrete item.
// ============================================================
[CreateAssetMenu(fileName = "SO_shopCatalogue", menuName = "ShopSim_SO/SO_shopCatalogue")]
public class SO_ShopCatalogue : ScriptableObject
{
	[Tooltip("Every item available for purchase.  Order = display order.")]
	public List<SO_PurchasableItem> items = new List<SO_PurchasableItem>();
}
