using UnityEngine;
using TMPro;

using SPACE_UTIL;

// ============================================================
//  DeliveryBox.cs
//  Attach to every box prefab referenced by SO_PurchasableItem.boxPrefab.
//
//  Receives Init() from DeliveryService after instantiation so
//  the box knows what it contains and how many units.
//
//  Phase 2+: player can walk up, press [E] to open the box,
//  and items spill out as individual item prefabs.
//  The Phase-2 hook is already stubbed as OpenBox().
// ============================================================
public class DeliveryBox : MonoBehaviour
{
	[Header("Optional label shown on the box mesh")]
	[SerializeField] TMP_Text _label;

	// ── Runtime payload ───────────────────────────────────────
	public SO_PurchasableItem ContainedItem  { get; private set; }
	public int                UnitsInside    { get; private set; }

	// ── API ───────────────────────────────────────────────────
	/// Called by DeliveryService immediately after Instantiate().
	public void Init(SO_PurchasableItem item, int unitsInside)
	{
		ContainedItem = item;
		UnitsInside   = unitsInside;

		if (_label != null)
			_label.text = $"{item.displayName}\n×{unitsInside}";

		Debug.Log($"[DeliveryBox] spawned: {item.itemId} ×{unitsInside}".colorTag("lime"));
	}

	// ── Phase 2 stub ──────────────────────────────────────────
	/// Call this when the player interacts with the box.
	/// Spawns UnitsInside × item.itemPrefab, then destroys this box.
	public void OpenBox()
	{
		if (ContainedItem == null) return;

		if (ContainedItem.itemPrefab != null)
		{
			for (int i = 0; i < UnitsInside; i++)
			{
				Vector3 offset = Random.insideUnitSphere * 0.4f;
				Instantiate(ContainedItem.itemPrefab,
				            transform.position + offset,
				            Quaternion.identity);
			}
		}

		Destroy(gameObject);
	}
}
