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
	// ┌───────────────────────────────────────────────────────────┐
	// │  CustomerFSM (WalkIn state)                               │
	// │       │                                                   │
	// │  GameEvents.RaiseCustomerEntered(this)					   │
	// │       │                                                   │
	// │       ├──► StoreManager   → _customersInsideCount++       │
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
}
