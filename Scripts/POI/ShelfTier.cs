using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

public class ShelfTier : MonoBehaviour
{
	public int capacity;
	private int occupied;
	public bool isFull { get => occupied == capacity; }
	public bool isEmpty { get => occupied == 0; }
	public SO_ItemData currItemData;

	// ── Homogeneous rule check ───────────────────────────────
	// true when this tier can accept one more unit of item
	public bool CanAccept(SO_ItemData item)
	{
		if (isFull) return false;
		if (isEmpty) return true;

		return (currItemData == item);
	}
}
