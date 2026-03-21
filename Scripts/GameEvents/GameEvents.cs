using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using SPACE_UTIL;

/*
The three things the comments nail down for each event:

- who fires it — the FSM state that calls GameEvents.RaiseXyz(this)
- who listens — every class that does GameEvents.OnXyz += Handle
- what the listener does with c — so it's obvious the payload is specific, not broadcast-to-all
*/
public static class GameEvents
{
	// ┌─────────────────────────────────────────────────────────┐
	// │  CustomerFSM (WalkIn state)                             │
	// │       │                                                 │
	// │  GameEvents.RaiseCustomerEntered(this)                       │
	// │       │                                                 │
	// │       ├──► StoreManager   → _customersInsideCount++     │
	// │       └──► DebugLogger    → Debug.Log(c.name)           │
	// │                                                         │
	// │  c = the specific CustomerAgent that crossed entrance.  │
	// │  Customer_02 entering does NOT fire for Customer_07.    │
	// └─────────────────────────────────────────────────────────┘
	public static event Action<CustomerAgent> OnCustomerEntered;
	public static void RaiseCustomerEntered(CustomerAgent c)
	{
		OnCustomerEntered?
			.Invoke(c);
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
	// │    void Handle(CustomerAgent c)						 │
	// │	{													 │
	// │        if (c.Profile.profileName == "VIP")              │
	// │            OpenFastLane();                              │
	// │    }                                                    │
	// └─────────────────────────────────────────────────────────┘
	public static event Action<CustomerAgent> OnCustomerJoinedQ;
	public static void RaiseCustomerJoinedQ(CustomerAgent c)
	{
		OnCustomerJoinedQ?
			.Invoke(c);
	}

	// ┌─────────────────────────────────────────────────────────┐
	// │  CustomerFSM (LeaveStore state)                         │
	// │       │                                                 │
	// │  GameEvents.CustomerLeft(this)                          │
	// │       │                                                 │
	// │       ├──► StoreManager   → _customersInsideCount--     │
	// │       │                     if !IsOpen && count == 0    │
	// │       │                       → StoreEmpty()            │
	// │       └──► AnalyticsLogger → record visit duration      │
	// │                                                         │
	// │  c = the customer that just reached ExitPoint.          │
	// │  StoreManager uses c.name for logging; it never stores  │
	// │  a direct reference to c — just reads what it needs     │
	// │  from the payload and discards it.                      │
	// └─────────────────────────────────────────────────────────┘
	public static event Action<CustomerAgent> OnCustomerLeft;
	public static void RaiseCustomerLeft(CustomerAgent c)
	{
		OnCustomerLeft?
			.Invoke(c);
	}
}
