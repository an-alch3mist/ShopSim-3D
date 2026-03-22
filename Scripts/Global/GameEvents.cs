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
}
