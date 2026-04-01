using UnityEngine;

// ============================================================
//  ShopCartEntry.cs
//  Plain runtime data — not a MonoBehaviour, not an SO.
//  Owned and mutated exclusively by ShopCartService.
//  Read by ShopCartUI for display, DeliveryService for spawning.
// ============================================================
[System.Serializable]
public class ShopCartEntry
{
	public SO_PurchasableItem item;
	public int quantity;

	// ── Derived ───────────────────────────────────────────────
	public float TotalPrice => item.unitPrice * quantity;

	// e.g. qty 440, unitsPerBox 100  →  5 boxes
	public int BoxCount => Mathf.CeilToInt((float)quantity / item.unitsPerBox);

	// units in the last (possibly partial) box
	// e.g. 440 % 100 = 40; if 0 last box is full
	public int UnitsInLastBox
	{
		get
		{
			int rem = quantity % item.unitsPerBox;
			return (rem == 0) ? item.unitsPerBox : rem;
		}
	}
}
