using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

// ============================================================
//  PlayerStockingController.cs
//  Handles player-driven item stocking via key bindings.
//
//  How it works:
//    1. Player presses a bound key (1 / 2 / 3 by default).
//    2. Controller resolves the mapped SO_ItemData from inspector.
//    3. OverlapSphere finds all colliders on the Stockable layer.
//    4. Picks the nearest IStockable that CanReceiveStock.
//    5. Calls TryReceiveStock — the implementor fires GameEvents.
//    6. GameEvents.RaisePlayerStockAttempted fires for UI/audio hooks.
//
//  Dependencies:
//    IStockable  — only interface this controller ever calls.
//    GameEvents  — event bus, no direct component references.
//
//  Phase 2 extension note:
//    When physical stock boxes land (FirstPersonGrabber carries a
//    StockBox prefab), replace the key binding with:
//      IStockSource source = heldObject.GetComponent<IStockSource>();
//      SO_ItemData item = source?.TakeOne();
//    The OverlapSphere + IStockable proximity logic stays identical.
//
//  Setup:
//    • Attach to the Player GameObject (or a child).
//    • Set _Tr_stockOrigin to the player root (or camera for tighter range).
//    • Create a "Stockable" Physics Layer and assign to _stockableLayer.
//    • Add that layer to ShelfPOI's collider (or a child trigger collider).
//    • Populate _BINDINGS in the inspector.
// ============================================================
public class PlayerStockingController : MonoBehaviour
{
	// ── Inspector ──────────────────────────────────────────────
	[System.Serializable]
	public struct StockKeyBinding
	{
		[Tooltip("Key the player presses to stock this item.")]
		public KeyCode key;
		[Tooltip("Item deposited when key is pressed.")]
		public SO_ItemData itemData;
	}

	[Header("Key → Item Bindings")]
	[Tooltip("Index 0 = Key1 by default. Add entries freely — no code changes needed.")]
	[SerializeField] List<StockKeyBinding> _BINDINGS;

	[Header("Proximity Detection")]
	[Tooltip("Radius around _Tr_stockOrigin to search for stockable surfaces.")]
	[SerializeField] float _stockRadius = 2.5f;
	[Tooltip("Only colliders on this layer are considered stockable. Create a 'Stockable' layer and assign it here AND on ShelfPOI's collider.")]
	[SerializeField] LayerMask _stockableLayer;

	[Header("Refs")]
	[Tooltip("Origin for radius check — typically the player root or camera.")]
	[SerializeField] Transform _Tr_stockOrigin;

	// ── Unity ─────────────────────────────────────────────────
	private void Awake()
	{
		Debug.Log(C.method(this));
	}
	private void Update()
	{
		foreach (StockKeyBinding binding in _BINDINGS)
		{
			if (Input.GetKeyDown(binding.key))
			{
				TryStockNearest(binding.itemData);
				break; // only one key per frame
			}
		}
	}

	#region  private API
	// ── Core logic ────────────────────────────────────────────
	void TryStockNearest(SO_ItemData itemData)
	{
		if (itemData == null)
		{
			Debug.LogWarning("[PlayerStocking] binding has no itemData assigned.".colorTag("orange"));
			return;
		}

		IStockable best = FindNearestStockable(itemData);

		if (best == null)
		{
			Debug.Log($"[PlayerStocking] No surface within {_stockRadius}m can accept {itemData.id}".colorTag("orange"));
			GameEvents.RaisePlayerStockSendAttempted(itemData, isNearestStockableSuccess: false);
			return;
		}

		bool placed = best.TryReciveStock(itemData);
		// ShelfPOI.TryReceiveStock already fires GameEvents.RaiseShelfRestocked internally.
		// This event is specifically for player-action feedback (sound, UI flash, etc.).
		GameEvents.RaisePlayerStockSendAttempted(itemData, placed);

		Debug.Log($"[PlayerStocking] {itemData.id} → {best.stockableId}: {(placed ? "stocked" : "rejected")}".colorTag(placed ? "lime" : "orange"));
	}

	/// <summary>
	/// OverlapSphere → GetComponentInParent → CanReceiveStock filter → nearest wins.
	/// Returns null if nothing valid in range.
	/// </summary>
	IStockable FindNearestStockable(SO_ItemData itemData)
	{
		Collider[] hits = Physics.OverlapSphere(
			_Tr_stockOrigin.position,
			_stockRadius,
			_stockableLayer
		);

		IStockable best = null;
		float bestDist = float.MaxValue;

		foreach (Collider col in hits)
		{
			// Walk up — the IStockable may live on a parent (e.g. ShelfPOI root, collider on a child trigger zone).
			IStockable candidate = col.GetComponentInParent<IStockable>();
			if (candidate == null) continue;
			if (!candidate.CanRecieveStock(itemData)) continue;

			float dist = Vector3.Distance(_Tr_stockOrigin.position, col.transform.position);
			if (dist < bestDist)
			{
				bestDist = dist;
				best = candidate;
			}
		}
		return best;
	}
	#endregion

	// ── Gizmos ────────────────────────────────────────────────
	private void OnDrawGizmosSelected()
	{
		if (_Tr_stockOrigin == null) return;
		Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
		Gizmos.DrawSphere(_Tr_stockOrigin.position, _stockRadius);
		Gizmos.color = new Color(0f, 1f, 0.5f, 0.9f);
		Gizmos.DrawWireSphere(_Tr_stockOrigin.position, _stockRadius);
	}
}