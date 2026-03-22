using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  AutoStockService.cs
//  Populates shelves at scene start from inspector-configured
//  entries. No code changes needed to adjust starting stock —
//  just edit the inspector list.
//
//  Each entry targets a specific shelf + tier index + item + amount.
//  A shelf can appear multiple times with different tiers and items.
//
//  Execution:
//    Start() → for each entry → ShelfPOI.SetStockOnTier(tier, item, amount)
//
//  Phase 2: replace with player stocking via IInteractable on StockBox.
// ============================================================
public class AutoStockService : MonoBehaviour
{
	[System.Serializable]
	public struct ShelfStockEntry
	{
		public ShelfPOI poi;
		[Tooltip("0 = lower tier, 1 = upper tier....")]
		public int tierIndex;
		public SO_ItemData itemData;
		[Tooltip("Units to stock on this tier (capped at tier capacity).")]
		[Min(0)]
		public int count;
	}

	[Header("Initial stock — one entry per shelf+tier combination")]
	[SerializeField] List<ShelfStockEntry> _STOCK_ENTRY;

	[Header("Cosmetic stagger")]
	[Tooltip("Delay between each entry. Gives a visual fill effect on start.")]
	[SerializeField] float _staggerDelay = 0.05f;

	private void Start()
	{
		Debug.Log(C.method(this));
		StartCoroutine(this.RoutineStock());
	}
	IEnumerator RoutineStock()
	{
		foreach (var entry in this._STOCK_ENTRY)
		{
			if (entry.poi == null || entry.itemData == null || entry.count <= 0)
			{
				Debug.Log($"skipped {entry.poi.gameObject.name} {entry.tierIndex}".colorTag("orange"));
				continue;
			}
			entry.poi.SetStockOnTier(entry.tierIndex, entry.itemData, entry.count);
			LOG.AddLog(entry.poi.GetStockedItems().ToTable(name: "LIST<> autoStock"));
			yield return new WaitForSeconds(_staggerDelay);
		}
		yield return null; // settle frame
		Debug.Log("[AutoStockService] Stocking complete.".colorTag("cyan"));
	}
}
