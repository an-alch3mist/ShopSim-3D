using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using SPACE_UTIL;

/*
The three things the comments nail down for each event:

- who fires it — the FSM state that calls GameEvents.RaiseXyz(this)
- who listens — every class that does GameEvents.OnXyz += Handle
- what the listener does with (agent) — so it's obvious the payload is specific, not broadcast-to-all
*/
/*
[CustomerFSM]                  [GameEvents]               [StoreManager]
     │                              │                           │
     │── RaiseCustomerEntered() ───►│                           │
     │                              │── HandleCustomerEntered ─►│
     │                              │                           │ _count += 1
*/
public static class GameEvents
{
	#region Phase-0
	// ┌───────────────────────────────────────────────────────────┐
	// │  CustomerFSM (WalkIn state)                               │
	// │       │                                                   │
	// │  GameEvents.RaiseCustomerEntered(this)					   │
	// │       │                                                   │
	// │       ├──► StoreManager   → _customersInsideCount += 1       │
	// │       └──► DebugLogger    → Debug.Log(agent.name)         │
	// │                                                           │
	// │  agent = the specific CustomerAgent that crossed entrance.│
	// │  Customer_02 entering does NOT fire for Customer_07.      │
	// └───────────────────────────────────────────────────────────┘
	// when npc crossed entrance point into the store.
	public static event Action<CustomerAgent> OnCustomerEntered;
	public static void RaiseCustomerEntered(CustomerAgent agent)
	{
		OnCustomerEntered?
			.Invoke(agent);
	}

	// ┌─────────────────────────────────────────────────────────┐
	// │  CustomerFSM (JoinQueue state)                          │
	// │       │                                                 │
	// │  GameEvents.CustomerJoinedQueue(this)                   │
	// │       │                                                 │
	// │       ├──► StoreManager   → track queue occupancy       │
	// │       └──► QueueUIDisplay → refresh queue count badge   │
	// │                                                         │
	// │  Example with payload check (VIP fast-lane logic):      │
	// │    void Handle(CustomerAgent agent						 │
	// │	{													 │
	// │        if (agent.Profile.profileName == "VIP			 │
	// │            OpenFastLane();                              │
	// │    }                                                    │
	// └─────────────────────────────────────────────────────────┘
	// when npc books a queue slot and beings walking to it.
	public static event Action<CustomerAgent> OnCustomerJoinedQ;
	public static void RaiseCustomerJoinedQ(CustomerAgent agent)
	{
		OnCustomerJoinedQ?
			.Invoke(agent);
	}

	// ┌─────────────────────────────────────────────────────────────┐
	// │  CustomerFSM (LeaveStore state)							 │
	// │       │													 │
	// │  GameEvents.CustomerLeft(this)								 │
	// │       │													 │
	// │       ├──► StoreManager   → _customersInsideCount--		 │
	// │       │                     if !IsOpen && count == 0		 │
	// │       │                       → StoreEmpty()				 │
	// │       └──► AnalyticsLogger → record visit duration			 │
	// │															 │
	// │  agent = the customer that just reached ExitPoint			 │
	// │  StoreManager uses agent.name for logging; it never stores  │
	// │  a direct reference to agent — just reads what it needs     │
	// │  from the payload and discards it.							 │
	// └─────────────────────────────────────────────────────────────┘
	// when npc reaches exit point before the despawn walk.
	public static event Action<CustomerAgent> OnCustomerLeft;
	public static void RaiseCustomerLeft(CustomerAgent agent)
	{
		OnCustomerLeft?
			.Invoke(agent);
	}
	#endregion

	#region Phase-1
	// ┌──────────────────────────────────────────────────────────┐
	// │  ShelfPOI / ShelfTier (TryTakeItem)                      │
	// │    └─ RaiseItemTaken(poi, tier, data)                    │
	// │         ├──► StoreManager  → track units sold            │
	// │         └──► ShelfUI       → refresh stock badge         │
	// └──────────────────────────────────────────────────────────┘
	public static event Action<ShelfPOI, ShelfTier, SO_ItemData> OnItemTaken;
	public static void RaiseItemTaken(ShelfPOI poi, ShelfTier tier, SO_ItemData data)
	{
		GameEvents.OnItemTaken?
			.Invoke(poi, tier, data);
	}

	// ┌──────────────────────────────────────────────────────────┐
	// │  ShelfTier (RemoveOne — when tier hits zero)             │
	// │    └─ RaiseShelfTierCleared(poi, tier)					  │
	// │         └──► ShelfUI  → show "needs restock" badge       │
	// │              (Phase 2: player stocking highlight)        │
	// └──────────────────────────────────────────────────────────┘
	public static event Action<ShelfPOI, ShelfTier> OnShelfTierCleared;
	public static void RaiseShelfTierCleared(ShelfPOI poi, ShelfTier tier)
	{
		GameEvents.OnShelfTierCleared?
			.Invoke(poi, tier);
	}

	// ┌──────────────────────────────────────────────────────────┐
	// │  AutoStockService / ShelfPOI (SetStock / AddStock)       │
	// │    └─ RaiseShelfRestocked(poi, tier, data, count)		  │
	// │         └──► ShelfUI  → refresh count display            │
	// └──────────────────────────────────────────────────────────┘
	public static event Action<ShelfPOI, ShelfTier, SO_ItemData, int> OnShelfRestocked;
	public static void RaiseShelfRestocked(ShelfPOI poi, ShelfTier tier, SO_ItemData data, int count)
	{
		GameEvents.OnShelfRestocked?
			.Invoke(poi, tier, data, count);
	}
	#endregion

	#region Phase-1.1
	// ┌──────────────────────────────────────────────────────────┐
	// │  PlayerStockingController (TryStockNearest)              │
	// │    └─ RaisePlayerStockAttempted(data, success)           │
	// │         ├──► StockingUI  → flash green/red indicator     │
	// │         ├──► AudioManager → play place/deny sfx          │
	// │         └──► InventoryManager → deduct item from hand    │
	// │                                                          │
	// │  Fires AFTER TryReceiveStock so 'success' reflects the   │
	// │  actual shelf acceptance, not just the keypress.         │
	// │                                                          │
	// │  Note: OnShelfRestocked also fires on success (from      │
	// │  ShelfPOI.TryStockItem). Listeners that only care about  │
	// │  shelf state use OnShelfRestocked; listeners that care   │
	// │  about player action feedback use this one.              │
	// └──────────────────────────────────────────────────────────┘
	public static event Action<SO_ItemData, bool> OnPlayerStockSendAttempted;
	public static void RaisePlayerStockSendAttempted(SO_ItemData itemData, bool isNearestStockableSuccess)
	{
		GameEvents.OnPlayerStockSendAttempted?
			.Invoke(itemData, isNearestStockableSuccess);
	}
	#endregion

	// ============================================================
	//  GameEvents_ShopAdditions.cs
	//
	//  !! DO NOT compile this file as-is !!
	//  Copy the regions below into the existing GameEvents.cs
	//  inside the static class body.
	//
	//  Paste location: after the existing #region Phase-1.1 block.
	// ============================================================

	/*
	────────────────────────────────────────────────────────────────
	PASTE INTO GameEvents.cs  — add these two usings at the top if
	they are not already present:

		using System.Collections.Generic;

	Then paste the region below after Phase-1.1:
	────────────────────────────────────────────────────────────────
	*/

	#region Shop-Cart

	// ┌──────────────────────────────────────────────────────────────┐
	// │  ShopCartService (SetQuantity / Remove / ClearCart)          │
	// │    └─ RaiseCartUpdated(item, newQty)                         │
	// │         ├──► ShopCartUI       → refresh or remove row        │
	// │         └──► ShopCatalogueItemUI → update "in cart" badge    │
	// │                                                              │
	// │  newQty == 0  means the item was removed from the cart.      │
	// └──────────────────────────────────────────────────────────────┘
	public static event Action<SO_PurchasableItem, int> OnCartUpdated;
	public static void RaiseCartUpdated(SO_PurchasableItem item, int newQty)
	{
		GameEvents.OnCartUpdated?
			.Invoke(item, newQty);
	}

	// ┌──────────────────────────────────────────────────────────────┐
	// │  ShopCartService (ConfirmPurchase)                           │
	// │    └─ RaisePurchaseConfirmed(entries)                        │
	// │         ├──► DeliveryService  → spawn boxes at delivery pt   │
	// │         └──► AnalyticsLogger  → record spend                 │
	// │                                                              │
	// │  entries is a snapshot — ShopCartService clears _cart        │
	// │  immediately after firing, so listeners must not mutate it.  │
	// └──────────────────────────────────────────────────────────────┘
	public static event Action<List<ShopCartEntry>> OnPurchaseConfirmed;
	public static void RaisePurchaseConfirmed(List<ShopCartEntry> entries)
	{
		GameEvents.OnPurchaseConfirmed?
			.Invoke(entries);
	}

	// ┌──────────────────────────────────────────────────────────────┐
	// │  DeliveryService (after all boxes for an item are spawned)   │
	// │    └─ RaiseItemDelivered(item, boxCount)                     │
	// │         └──► StoreManager / UI → "Your order arrived!" toast │
	// └──────────────────────────────────────────────────────────────┘
	public static event Action<SO_PurchasableItem, int> OnItemDelivered;
	public static void RaiseItemDelivered(SO_PurchasableItem item, int boxCount)
	{
		GameEvents.OnItemDelivered?
			.Invoke(item, boxCount);
	}
	#endregion

}
