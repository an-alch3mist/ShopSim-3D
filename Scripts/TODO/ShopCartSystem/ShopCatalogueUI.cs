using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

// ============================================================
//  ShopCatalogueUI.cs
//  Left panel.  Two responsibilities:
//
//  1. Category filter bar
//     - One toggle-button per category found in the catalogue.
//     - ALL categories enabled by default.
//     - Clicking a button enables / disables that category as a filter.
//     - Active = coloured; inactive = greyed.
//
//  2. Item grid
//     - Spawns one ShopCatalogueItemUI card per item that passes
//       the current filter set.
//     - Re-spawns on every filter change (catalogue is small).
// ============================================================
public class ShopCatalogueUI : MonoBehaviour
{
	[Header("Data")]
	[SerializeField] SO_ShopCatalogue _catalogue;

	[Header("Category filter bar")]
	[SerializeField] Transform        _Tr_filterBar;
	[SerializeField] GameObject       _pfCategoryButton;   // prefab: Button + TMP_Text (+ optional Image)

	[Header("Item grid")]
	[SerializeField] Transform        _Tr_itemGrid;
	[SerializeField] GameObject       _pfItemCard;         // prefab: ShopCatalogueItemUI

	[Header("Filter button colours")]
	[SerializeField] Color _colActive   = Color.white;
	[SerializeField] Color _colInactive = new Color(0.4f, 0.4f, 0.4f, 1f);

	// ── State ─────────────────────────────────────────────────
	// All active by default — toggled on click
	readonly HashSet<SO_ShopCategory> _activeCategories = new HashSet<SO_ShopCategory>();
	// Map category → its filter button Image (for colour swap)
	readonly Dictionary<SO_ShopCategory, Image> _categoryBtnImage
		= new Dictionary<SO_ShopCategory, Image>();

	// ── Unity ─────────────────────────────────────────────────
	private void Awake()
	{
		Debug.Log(C.method(this));
		BuildFilterBar();
		RefreshItemGrid();
	}

	// ── Filter bar ────────────────────────────────────────────
	void BuildFilterBar()
	{
		// Collect distinct categories from catalogue (preserve order)
		var seen       = new HashSet<SO_ShopCategory>();
		var categories = new List<SO_ShopCategory>();
		foreach (var item in _catalogue.items)
		{
			if (item.category == null) continue;
			if (seen.Add(item.category))
			{
				categories.Add(item.category);
				_activeCategories.Add(item.category); // all on by default
			}
		}

		// Spawn a filter button per category
		foreach (SO_ShopCategory cat in categories)
		{
			GameObject go  = Instantiate(_pfCategoryButton, _Tr_filterBar);
			go.name        = $"FilterBtn_{cat.categoryId}";

			// Label
			TMP_Text label = go.GetComponentInChildren<TMP_Text>();
			if (label != null) label.text = cat.displayName;

			// Optional icon
			Image[] images = go.GetComponentsInChildren<Image>();
			Image btnImage = (images.Length > 0) ? images[0] : null;
			if (btnImage != null) _categoryBtnImage[cat] = btnImage;

			// Click
			Button btn = go.GetComponent<Button>();
			SO_ShopCategory captured = cat;
			btn?.onClick.AddListener(() => ToggleCategory(captured));

			UpdateButtonVisual(cat);
		}
	}

	void ToggleCategory(SO_ShopCategory cat)
	{
		if (_activeCategories.Contains(cat))
			_activeCategories.Remove(cat);
		else
			_activeCategories.Add(cat);

		UpdateButtonVisual(cat);
		RefreshItemGrid();
		Debug.Log($"[CatalogueUI] Filter toggled: {cat.displayName} → {(_activeCategories.Contains(cat) ? "ON" : "OFF")}".colorTag("cyan"));
	}

	void UpdateButtonVisual(SO_ShopCategory cat)
	{
		if (!_categoryBtnImage.TryGetValue(cat, out Image img)) return;
		img.color = _activeCategories.Contains(cat) ? _colActive : _colInactive;
	}

	// ── Item grid ─────────────────────────────────────────────
	void RefreshItemGrid()
	{
		// Destroy existing cards
		foreach (Transform child in _Tr_itemGrid)
			Destroy(child.gameObject);

		// Spawn one card per filtered item
		foreach (SO_PurchasableItem item in _catalogue.items)
		{
			// items without a category always show (safety fallback)
			if (item.category != null && !_activeCategories.Contains(item.category))
				continue;

			GameObject go = Instantiate(_pfItemCard, _Tr_itemGrid);
			go.name = $"Card_{item.itemId}";
			go.GetComponent<ShopCatalogueItemUI>()?.Bind(item);
		}
	}
}
