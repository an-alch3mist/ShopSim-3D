using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ============================================================
//  Pure data. No MonoBehaviour, no runtime state.
//  Referenced by ShelfTier (what is stocked) and CustomerAgent (what is wanted). Neither holds the other's reference.
// ============================================================
[CreateAssetMenu(fileName = "SO_itemData", menuName = "ShopSim_SO/SO_itemData")]
public class SO_ItemData : ScriptableObject
{
	[Header("id")]
	public string id = "item_id";
	public string itemName = "item_name";

	[Header("price")]
	public float basePrice = 1f;

	[Header("prototype visual")]
	public Color displayColor = Color.white;
	public GameObject slotPrefab;
}