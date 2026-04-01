using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

// ============================================================
//  DeliveryService.cs
//  Listens to GameEvents.OnPurchaseConfirmed.
//  For each cart entry it calculates how many boxes to spawn
//  (see ShopCartEntry.BoxCount) then Instantiates them at
//  _Tr_deliveryPoint with a small stagger delay.
//
//  Packaging example:
//    Milk (unitsPerBox=100), qty 440
//      → 4 boxes × 100 units + 1 box × 40 units
//    Shelf (unitsPerBox=1), qty 3
//      → 3 boxes × 1 unit each
//
//  Place this MonoBehaviour on the StoreManager or a dedicated
//  "Services" GameObject.  Assign _Tr_deliveryPoint to a
//  Transform just inside the store entrance / loading bay.
// ============================================================
public class DeliveryService : MonoBehaviour
{
	[Header("Delivery point — boxes spawn here")]
	[Tooltip("Boxes are stacked by offsetting on the Y axis.")]
	[SerializeField] Transform _Tr_deliveryPoint;

	[Header("Timing")]
	[SerializeField] [Min(0f)] float _delayBetweenBoxes = 0.25f;

	[Header("Stack offset per box (world-space Y)")]
	[SerializeField] float _stackOffsetY = 0.15f;

	// ── Unity ─────────────────────────────────────────────────
	private void OnEnable()  => GameEvents.OnPurchaseConfirmed += HandlePurchaseConfirmed;
	private void OnDisable() => GameEvents.OnPurchaseConfirmed -= HandlePurchaseConfirmed;

	// ── Handler ───────────────────────────────────────────────
	void HandlePurchaseConfirmed(List<ShopCartEntry> entries)
	{
		Debug.Log($"[DeliveryService] Delivering {entries.Count} line(s).".colorTag("lime"));
		StartCoroutine(RoutineDeliverAll(entries));
	}

	// ── Coroutines ────────────────────────────────────────────
	IEnumerator RoutineDeliverAll(List<ShopCartEntry> entries)
	{
		int totalBoxesSpawned = 0;

		foreach (ShopCartEntry entry in entries)
		{
			int boxCount = entry.BoxCount;

			for (int b = 0; b < boxCount; b++)
			{
				// Last box may be partial
				bool isLastBox    = (b == boxCount - 1);
				int  unitsInBox   = isLastBox ? entry.UnitsInLastBox : entry.item.unitsPerBox;

				SpawnBox(entry.item, unitsInBox, totalBoxesSpawned);
				totalBoxesSpawned++;

				yield return new WaitForSeconds(_delayBetweenBoxes);
			}

			GameEvents.RaiseItemDelivered(entry.item, boxCount);
		}

		Debug.Log($"[DeliveryService] All {totalBoxesSpawned} box(es) delivered.".colorTag("lime"));
	}

	void SpawnBox(SO_PurchasableItem item, int unitsInside, int stackIndex)
	{
		if (item.boxPrefab == null)
		{
			Debug.LogWarning($"[DeliveryService] {item.itemId} has no boxPrefab — skipping.".colorTag("orange"));
			return;
		}

		Vector3 spawnPos = _Tr_deliveryPoint.position
		                   + Vector3.up * (_stackOffsetY * stackIndex);

		GameObject go = Instantiate(item.boxPrefab, spawnPos, _Tr_deliveryPoint.rotation);
		go.name = $"Box_{item.itemId}_{stackIndex:D3}";

		DeliveryBox box = go.GetComponent<DeliveryBox>();
		if (box != null)
			box.Init(item, unitsInside);
		else
			Debug.LogWarning($"[DeliveryService] boxPrefab for '{item.itemId}' is missing a DeliveryBox component.".colorTag("orange"));
	}

	// ── Gizmo ─────────────────────────────────────────────────
	private void OnDrawGizmosSelected()
	{
		if (_Tr_deliveryPoint == null) return;
		Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
		Gizmos.DrawWireCube(_Tr_deliveryPoint.position, Vector3.one * 0.5f);
#if UNITY_EDITOR
		UnityEditor.Handles.Label(_Tr_deliveryPoint.position + Vector3.up * 0.6f, "Delivery");
#endif
	}
}
