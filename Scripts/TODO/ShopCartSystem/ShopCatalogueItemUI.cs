using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  ShopCatalogueItemUI.cs
//  Attached to each item card prefab in the catalogue grid.
//
//  Layout (horizontal):
//    [Icon]  [Name / Price]  [+ Add]  [qty-in-cart badge]
//
//  Clicking "+" fires ShopCartService.AdjustQuantity(item, +1).
//  The badge updates whenever OnCartUpdated fires for this item.
//
//  Created and bound by ShopCatalogueUI.SpawnItemCards().
// ============================================================
public class ShopCatalogueItemUI : MonoBehaviour
{
	[Header("Visuals")]
	[SerializeField] Image     _img_icon;
	[SerializeField] TMP_Text  _txt_name;
	[SerializeField] TMP_Text  _txt_price;
	[SerializeField] TMP_Text  _txt_cartBadge;   // "×3" overlay, hidden when 0
	[SerializeField] GameObject _go_cartBadge;

	[Header("Button")]
	[SerializeField] Button _btn_add;

	// ── Runtime ───────────────────────────────────────────────
	SO_PurchasableItem _item;

	// ── Setup (called by ShopCatalogueUI) ─────────────────────
	public void Bind(SO_PurchasableItem item)
	{
		_item = item;

		if (_img_icon  != null) _img_icon.sprite = item.icon;
		if (_txt_name  != null) _txt_name.text   = item.displayName;
		if (_txt_price != null) _txt_price.text  = $"${item.unitPrice:F2} / unit";

		_btn_add.onClick.AddListener(OnClickAdd);
		RefreshBadge(ShopCartService.Ins.GetQuantity(item));

		GameEvents.OnCartUpdated += OnCartUpdated;
	}

	// ── Handlers ──────────────────────────────────────────────
	void OnClickAdd()
	{
		ShopCartService.Ins.AdjustQuantity(_item, 1);
	}

	void OnCartUpdated(SO_PurchasableItem item, int newQty)
	{
		if (item != _item) return;
		RefreshBadge(newQty);
	}

	void RefreshBadge(int qty)
	{
		bool show = qty > 0;
		if (_go_cartBadge  != null) _go_cartBadge.SetActive(show);
		if (_txt_cartBadge != null) _txt_cartBadge.text = $"×{qty}";
	}

	// ── Cleanup ───────────────────────────────────────────────
	private void OnDestroy()
	{
		GameEvents.OnCartUpdated -= OnCartUpdated;
	}
}
