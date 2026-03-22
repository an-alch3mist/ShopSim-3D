using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

public class ShelfTier : MonoBehaviour
{
	public int capacity = 10;
	public int occupied;
	public float slotSpacing = 0.25f;

	public bool isFull { get => occupied == capacity; }
	public bool isEmpty { get => occupied == 0; }
	public SO_ItemData currItemData;

	// ── Homogeneous rule check ───────────────────────────────
	// true when this tier can accept one more unit of item
	public bool CanAccept(SO_ItemData itemData)
	{
		if (isFull) return false;
		if (isEmpty) return true;

		return (currItemData == itemData);
	}
	// ── Init (called by ShelfPOI.SetStock / AutoStockService) ─
	// clears existing visuals then spawns `count` units of item
	public void InitStock(SO_ItemData itemData, int count)
	{

	}
	// ── Public mutation ──────────────────────────────────────
	// add one unit; caller must call CanAccept first
	public bool AddOne(SO_ItemData itemData)
	{
		if (CanAccept(itemData) == false)
			return false;

		if(isEmpty)
		{
			currItemData = itemData;
			// OBJ_SLOT = new GameObject[capacity];  // ensure array ready
		}
		SpawnAt(occupied, itemData);
		occupied += 1;
		return true;
	}
	// remove rightmost item; fires TierCleared if reaching zero
	public bool RemoveOne(ShelfPOI poi)
	{
		if (occupied == 0)
			return false;
		occupied -= 1;
		if (OBJ_SLOT[occupied] != null)
		{
			Destroy(OBJ_SLOT[occupied]);
			OBJ_SLOT[occupied] = null;
		}
		if (occupied == 0)
		{
			currItemData = null;
			GameEvents.RaiseShelfTierCleared(poi, this);
		}
		return true;
	}
	// destroy all visuals, reset state
	public void Clear()
	{
		foreach (var go in OBJ_SLOT)
			if (go != null)
				GameObject.Destroy(go);
		OBJ_SLOT = new GameObject[capacity];
		occupied = 0;
		currItemData = null;
	}

	#region private API
	private GameObject[] OBJ_SLOT;
	// ── Slot position ────────────────────────────────────────
	// world-space center of slot i, centered on board local X
	private Vector3 SlotPosition(int index)
	{
		float totalWidth = (capacity - 1) * slotSpacing;
		float localX = -totalWidth * 0.5f + index * slotSpacing;
		// +0.1f lifts item to sit flush on board surface (half of 0.2-tall cube)
		return transform.TransformPoint(new Vector3(localX, 0.1f, 0f));
	}

	// ── Spawn / tint ─────────────────────────────────────────
	private void SpawnAt(int index, SO_ItemData data)
	{
		if (data.slotPrefab == null)
		{
			Debug.LogWarning($"[ShelfTier] {name}: SO_ItemData '{data.id}' has no slotPrefab.");
			return;
		}
		OBJ_SLOT[index] = Instantiate(data.slotPrefab, SlotPosition(index),
								 transform.rotation, transform);
		TintSlot(OBJ_SLOT[index], data.displayColor);
	}
	private static void TintSlot(GameObject go, Color color)
	{
		var mr = go.GetComponentInChildren<MeshRenderer>();
		if (mr != null) mr.material.color = color;
	}

	#region Unity Life Cycle
	private void Awake()
	{
		Debug.Log(C.method(this));
		this.OBJ_SLOT = new GameObject[capacity];
	}

	private void OnDrawGizmos()
	{
		if (this.OBJ_SLOT == null)
			return;
		OBJ_SLOT.forEach((obj, index) =>
		{
			if (obj == null)
				Gizmos.color = Color.red;
			else
				Gizmos.color = Color.limeGreen;

			Gizmos.DrawWireCube(SlotPosition(index), Vector3.one * 0.2f);

		});
		//
		string label = currItemData != null
			? $"{currItemData.itemName} {occupied}/{capacity}"
			: $"[free] 0/{capacity}";
		UnityEditor.Handles.Label(transform.position + Vector3.up * 0.45f, label);
	} 
	#endregion

	#endregion
}
