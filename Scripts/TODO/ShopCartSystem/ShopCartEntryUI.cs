using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  ShopCartEntryUI.cs
//  One row in the cart panel.  Layout:
//
//    [Icon] [Name]  [unitPrice]  [-10][-1][qty input][+1][+10]  [total]  [×]
//
//  Quantity input field:
//    - Displays current quantity as editable number.
//    - On EndEdit parses the value and calls SetQuantity.
//    - Non-numeric or ≤ 0 input calls Remove.
//
//  Created and destroyed exclusively by ShopCartUI.
//  Does NOT subscribe to GameEvents — ShopCartUI owns lifecycle.
// ============================================================
public class ShopCartEntryUI : MonoBehaviour
{
	[Header("Display")]
	[SerializeField] Image    _img_icon;
	[SerializeField] TMP_Text _txt_name;
	[SerializeField] TMP_Text _txt_unitPrice;
	[SerializeField] TMP_Text _txt_totalPrice;

	[Header("Quantity controls")]
	[SerializeField] Button         _btn_minus10;
	[SerializeField] Button         _btn_minus1;
	[SerializeField] Button         _btn_plus1;
	[SerializeField] Button         _btn_plus10;
	[SerializeField] TMP_InputField _input_qty;

	[Header("Remove")]
	[SerializeField] Button _btn_remove;

	// ── Runtime ───────────────────────────────────────────────
	SO_PurchasableItem _item;

	// ── Setup (called by ShopCartUI after Instantiate) ────────
	public void Bind(ShopCartEntry entry)
	{
		_item = entry.item;

		if (_img_icon     != null) _img_icon.sprite   = entry.item.icon;
		if (_txt_name     != null) _txt_name.text      = entry.item.displayName;
		if (_txt_unitPrice!= null) _txt_unitPrice.text = $"${entry.item.unitPrice:F2}/u";

		_btn_minus10.onClick.AddListener(() => Adjust(-10));
		_btn_minus1 .onClick.AddListener(() => Adjust(-1));
		_btn_plus1  .onClick.AddListener(() => Adjust(+1));
		_btn_plus10 .onClick.AddListener(() => Adjust(+10));
		_btn_remove .onClick.AddListener(Remove);

		_input_qty.onEndEdit.AddListener(OnQtyEndEdit);

		RefreshDisplay(entry.quantity, entry.TotalPrice);
	}

	// Called by ShopCartUI when OnCartUpdated fires for this item.
	public void Refresh(int qty, float totalPrice)
	{
		RefreshDisplay(qty, totalPrice);
	}

	// ── Private ───────────────────────────────────────────────
	void Adjust(int delta)
	{
		ShopCartService.Ins.AdjustQuantity(_item, delta);
	}

	void Remove()
	{
		ShopCartService.Ins.Remove(_item);
	}

	void OnQtyEndEdit(string raw)
	{
		if (int.TryParse(raw, out int parsed) && parsed > 0)
			ShopCartService.Ins.SetQuantity(_item, parsed);
		else
			ShopCartService.Ins.Remove(_item);
	}

	void RefreshDisplay(int qty, float totalPrice)
	{
		if (_input_qty    != null) _input_qty.text       = qty.ToString();
		if (_txt_totalPrice!= null) _txt_totalPrice.text = $"${totalPrice:F2}";
	}
}
