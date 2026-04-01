using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

// ============================================================
//  ShopCartUI.cs
//  Right panel.  Owns the live list of ShopCartEntryUI rows.
//
//  Responds to GameEvents.OnCartUpdated:
//    newQty > 0 → create row if missing, else refresh existing row
//    newQty == 0 → destroy that row
//
//  Also shows:
//    - Running total price
//    - Total box count (useful so player knows how many boxes arrive)
//    - "Confirm Purchase" button → ShopCartService.ConfirmPurchase()
//    - "Clear Cart" button
// ============================================================
public class ShopCartUI : MonoBehaviour
{
	[Header("Row container + prefab")]
	[SerializeField] Transform  _Tr_rowContainer;
	[SerializeField] GameObject _pfCartEntry;      // prefab: ShopCartEntryUI

	[Header("Summary")]
	[SerializeField] TMP_Text _txt_totalPrice;
	[SerializeField] TMP_Text _txt_boxCount;
	[SerializeField] TMP_Text _txt_emptyHint;    // "Your cart is empty." hint

	[Header("Buttons")]
	[SerializeField] Button _btn_confirm;
	[SerializeField] Button _btn_clear;

	// ── State ─────────────────────────────────────────────────
	readonly Dictionary<SO_PurchasableItem, ShopCartEntryUI> _rows
		= new Dictionary<SO_PurchasableItem, ShopCartEntryUI>();

	// ── Unity ─────────────────────────────────────────────────
	private void Awake()
	{
		Debug.Log(C.method(this));
		_btn_confirm.onClick.AddListener(OnClickConfirm);
		_btn_clear  .onClick.AddListener(OnClickClear);
		RefreshSummary();
	}

	private void OnEnable()
	{
		GameEvents.OnCartUpdated += HandleCartUpdated;
	}

	private void OnDisable()
	{
		GameEvents.OnCartUpdated -= HandleCartUpdated;
	}

	// ── Event handler ─────────────────────────────────────────
	void HandleCartUpdated(SO_PurchasableItem item, int newQty)
	{
		if (newQty <= 0)
		{
			RemoveRow(item);
		}
		else if (_rows.TryGetValue(item, out ShopCartEntryUI existing))
		{
			// already have a row — just refresh it
			existing.Refresh(newQty, item.unitPrice * newQty);
		}
		else
		{
			// new item in cart — spawn a row
			AddRow(item, newQty);
		}

		RefreshSummary();
	}

	// ── Row management ────────────────────────────────────────
	void AddRow(SO_PurchasableItem item, int qty)
	{
		GameObject go = Instantiate(_pfCartEntry, _Tr_rowContainer);
		go.name = $"CartRow_{item.itemId}";

		ShopCartEntryUI row = go.GetComponent<ShopCartEntryUI>();
		var entry = new ShopCartEntry { item = item, quantity = qty };
		row.Bind(entry);

		_rows[item] = row;
	}

	void RemoveRow(SO_PurchasableItem item)
	{
		if (_rows.TryGetValue(item, out ShopCartEntryUI row))
		{
			Destroy(row.gameObject);
			_rows.Remove(item);
		}
	}

	// ── Summary ───────────────────────────────────────────────
	void RefreshSummary()
	{
		bool isEmpty = ShopCartService.Ins.IsEmpty;

		if (_txt_emptyHint != null) _txt_emptyHint.gameObject.SetActive(isEmpty);
		if (_txt_totalPrice!= null)
			_txt_totalPrice.text = isEmpty
				? "$0.00"
				: $"Total: ${ShopCartService.Ins.TotalPrice:F2}";

		if (_txt_boxCount != null)
			_txt_boxCount.text = isEmpty
				? ""
				: $"{ShopCartService.Ins.TotalBoxCount} box(es)";

		_btn_confirm.interactable = !isEmpty;
	}

	// ── Button handlers ───────────────────────────────────────
	void OnClickConfirm()
	{
		ShopCartService.Ins.ConfirmPurchase();
		// rows are removed by HandleCartUpdated as ClearCart fires OnCartUpdated per item
	}

	void OnClickClear()
	{
		ShopCartService.Ins.ClearCart();
	}
}
