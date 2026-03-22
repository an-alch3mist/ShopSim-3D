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
	public string POIId => throw new NotImplementedException();

	public Transform BookSlot(CustomerAgent agent)
	{
		throw new NotImplementedException();
	}

	public bool HasAnyAvailableSlot()
	{
		throw new NotImplementedException();
	}

	public void ReleaseSlot(CustomerAgent agent)
	{
		throw new NotImplementedException();
	}
}
