using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using SPACE_UTIL;

// ============================================================
//  IPOI.cs
//  Contract for any bookable world-space slot.
//  Queue counters, shelves, beds, etc —> all implement this.
//  CustomerFSM only ever talks to IPOI, never to concrete types.
// ============================================================
public interface IPOI
{
	string POIId { get; }

	// true if at least one slot is unbooked
	bool HasSlotForBooking();
	// reserve earliest free slot, returns Transform or null if full
	Transform BookSlot(CustomerAgent agent);
	// release the slot this agent, item etc was holding
	void ReleaseSlot(CustomerAgent agent);
}
