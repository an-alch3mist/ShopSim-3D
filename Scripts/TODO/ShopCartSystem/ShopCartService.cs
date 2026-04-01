using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

// ============================================================
//  ShopCartService.cs
//  Single source of truth for cart state.
//  UI reads from here; nothing else mutates the cart directly.
//
//  Flow:
//    ShopCatalogueItemUI.OnClickAdd()
//      └─► ShopCartService.AdjustQuantity(item, +1)
//              └─► GameEvents.RaiseCartUpdated(item, newQty)
//                      └─► ShopCartUI refreshes row
//
//    ShopCartUI.OnClickConfirm()
//      └─► ShopCartService.ConfirmPurchase()
//              └─► GameEvents.RaisePurchaseConfirmed(entries)
//                      └─► DeliveryService spawns boxes
//                      └─► ShopCartService.ClearCart()
// ============================================================
public class ShopCartService : MonoBehaviour
{
	public static ShopCartService Ins { get; private set; }

	// ── State ─────────────────────────────────────────────────
	// Key = item SO (reference equality) — one entry per unique item.
	readonly Dictionary<SO_PurchasableItem, ShopCartEntry> _cart
		= new Dictionary<SO_PurchasableItem, ShopCartEntry>();

	// ── Read API (used by UI / other services) ────────────────
	public IEnumerable<ShopCartEntry>  Entries    => _cart.Values;
	public int                         ItemCount  => _cart.Count;
	public float                       TotalPrice
	{
		get
		{
			float sum = 0f;
			foreach (var e in _cart.Values) sum += e.TotalPrice;
			return sum;
		}
	}
	public int TotalBoxCount
	{
		get
		{
			int sum = 0;
			foreach (var e in _cart.Values) sum += e.BoxCount;
			return sum;
		}
	}
	public bool IsEmpty => _cart.Count == 0;

	public int GetQuantity(SO_PurchasableItem item)
		=> _cart.ContainsKey(item) ? _cart[item].quantity : 0;

	// ── Write API ─────────────────────────────────────────────

	/// Set absolute quantity for an item.  qty ≤ 0 removes the entry.
	public void SetQuantity(SO_PurchasableItem item, int qty)
	{
		if (item == null) return;

		if (qty <= 0)
		{
			Remove(item);
			return;
		}

		if (!_cart.ContainsKey(item))
			_cart[item] = new ShopCartEntry { item = item, quantity = 0 };

		_cart[item].quantity = qty;
		GameEvents.RaiseCartUpdated(item, qty);
		Debug.Log($"[Cart] Set {item.itemId} × {qty}  (${_cart[item].TotalPrice:F2})".colorTag("cyan"));
	}

	/// Add or subtract from current quantity.  Clamps to ≥ 0.
	public void AdjustQuantity(SO_PurchasableItem item, int delta)
	{
		int current = GetQuantity(item);
		SetQuantity(item, Mathf.Max(0, current + delta));
	}

	/// Remove item from cart entirely.
	public void Remove(SO_PurchasableItem item)
	{
		if (_cart.Remove(item))
		{
			GameEvents.RaiseCartUpdated(item, 0);
			Debug.Log($"[Cart] Removed {item.itemId}".colorTag("orange"));
		}
	}

	/// Wipe all entries (called after purchase confirmed).
	public void ClearCart()
	{
		var keys = new List<SO_PurchasableItem>(_cart.Keys);
		_cart.Clear();
		foreach (var k in keys)
			GameEvents.RaiseCartUpdated(k, 0);
		Debug.Log("[Cart] Cleared.".colorTag("cyan"));
	}

	/// Snapshot entries, fire purchase event, clear cart.
	public void ConfirmPurchase()
	{
		if (IsEmpty)
		{
			Debug.Log("[Cart] Confirm attempted on empty cart.".colorTag("orange"));
			return;
		}

		var snapshot = new List<ShopCartEntry>(_cart.Values);
		Debug.Log($"[Cart] Purchase confirmed — {snapshot.Count} line(s), ${TotalPrice:F2}, {TotalBoxCount} box(es)".colorTag("lime"));
		GameEvents.RaisePurchaseConfirmed(snapshot);
		ClearCart();
	}

	// ── Unity ─────────────────────────────────────────────────
	private void Awake()
	{
		Debug.Log(C.method(this));
		if (Ins != null && Ins != this) { Destroy(gameObject); return; }
		Ins = this;
	}
}
