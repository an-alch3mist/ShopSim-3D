using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using SPACE_UTIL;

// ============================================================
//  Ordered queue of Transform slots.
//  Index 0 = front (close to counter side).
//  Self-registers with POIRegistry on enable.
// ============================================================
public class ShelfPOI : MonoBehaviour, IPOI
{
	[SerializeField] string _POIId = "shelf-id";
	[Tooltip(tooltip: "index 0 -> lower board, index 1 -> higher board ....")]
	[SerializeField] List<ShelfTier> TIER = new List<ShelfTier>();

	[SerializeField] Transform _Tr_interactionPoint;
	[Header("just to log")]
	[SerializeField] CustomerAgent occupant;

	#region IPOI
	public string POIId => this._POIId;
	public Transform BookSlot(CustomerAgent agent)
	{
		Debug.Log(C.method(this, "lime", adMssg: agent.gameObject.name));
		if (this.occupant != null)
			return null;
		this.occupant = agent;
		return this._Tr_interactionPoint;
	}
	// for customer agent searching for an itemData shelfTier
	public bool HasSlotForBooking()
	{
		return this.occupant == null;
	}
	public void ReleaseSlot(CustomerAgent agent)
	{
		Debug.Log(C.method(this, "orange", adMssg: $"{agent.gameObject.name}"));
		if (this.occupant == agent)
			this.occupant = null;
	}
	#endregion

	#region Shelf POI Specific public API
	/// ── Aggregated reads ──────────────────────────────────────
	// true if any tier holds at least 1 unit of this specific item
	public bool HasStockOfItem(SO_ItemData itemData)
	{
		foreach (var tier in this.TIER)
			if (tier.hasItem)
				if (tier.currItemData == itemData)
					return true;
		return false;
	}

	// all distinct items currently stocked across tiers
	public List<SO_ItemData> GetStockedItems()
	{
		return this.TIER
			.refine(tier => tier.hasItem) // curr item must  !=  null
			.map(tier => tier.currItemData)
			.rmdup()
			.ToList();
	}

	/// ── Tier selection ────────────────────────────────────────
	// tier selection for stocking
	public ShelfTier GetTierForStocking(SO_ItemData itemData)
	{
		foreach (var tier in this.TIER)
		{
			if (tier.hasItem && (tier.isFull == false))
				if (tier.currItemData == itemData)
					return tier;
		}
		foreach (var tier in this.TIER)
		{
			if (tier.isEmpty)
				return tier;
		}
		return null;
	}

	// tier selection for buying
	public ShelfTier GetTierForBuying(SO_ItemData itemData)
	{
		// highest tier holding this item
		for (int i0 = TIER.Count - 1; i0 >= 0; i0 -= 1)
		{
			ShelfTier tier = TIER[i0];
			if (tier.hasItem)
				if (tier.currItemData == itemData)
					return tier;
		}
		return null;
	}

	/// ── Stock operations ──────────────────────────────────────
	// bulk stock a specific tier by index (used by AutoStockService)
	public void SetStockOnTier(int tierIndex, SO_ItemData itemData, int count)
	{
		ShelfTier tier = TIER[tierIndex];
		tier.InitStock(itemData, count);
		GameEvents.RaiseShelfRestocked(this, tier, itemData, count);
	}

	// remove one unit of item from topmost holding tier
	public bool TryTakeItem(SO_ItemData itemData)
	{
		ShelfTier tier = this.GetTierForBuying(itemData);
		if (tier == null)
			return false;

		bool itemRemoved = tier.RemoveOne(this);
		if (itemRemoved == true)
			GameEvents.RaiseItemTaken(this, tier, itemData);
		return itemRemoved;
	}

	// add one unit of item to best available tier (enforces homogeneous rule)
	public bool TryStockItem(SO_ItemData itemData)
	{
		ShelfTier tier = this.GetTierForStocking(itemData);
		if (tier == null)
		{
			Debug.Log($"no tier in this shelf poi can accept {itemData.id}");
			return false;
		}
		bool addedItem = tier.AddOne(itemData);
		if (addedItem == true)
			GameEvents.RaiseShelfRestocked(this, tier, itemData, 1);
		return addedItem;
	}
	#endregion

	#region Unity Life Cycle
	private void Awake()
	{
		// Debug.Log(C.method(this));
	}
	private void Start()
	{
		Debug.Log(C.method(this));
		POIRegistry.Ins.RegisterShelf(this);
	}
	private void OnDisable()
	{
		Debug.Log(C.method(this, "orange"));
		POIRegistry.Ins.UnRegisterShelf(this);
	}

	private void OnDrawGizmos()
	{
		if (TIER.Count == 0)
			return;

		bool hasStockAny = (this.TIER.refine(tier => tier.hasItem).ToList().Count > 0);
		Gizmos.color = (hasStockAny) ? Color.limeGreen : Color.red;
		Gizmos.DrawWireSphere(this.transform.position + Vector3.up * 1.5f, 0.25f);

		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(_Tr_interactionPoint.position, Vector3.one * 0.18f);

		TIER.forEach((tier, index) =>
		{
			string label = tier.currItemData != null
					? $"[{index}] {tier.currItemData.itemName} {tier.occupied}/{tier.capacity}"
					: $"[{index}] free";
			UnityEditor.Handles.Label(
				tier.transform.position + Vector3.up * 0.55f, label);
		});
	}
	#endregion

	#region private API

	#endregion
}
