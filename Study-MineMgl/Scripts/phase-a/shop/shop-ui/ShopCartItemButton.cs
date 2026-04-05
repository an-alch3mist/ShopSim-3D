using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

using SPACE_UTIL;

namespace SPACE_MineMGL
{
	/// <summary>
	/// refering to single UI element item entry in shopping cart
	/// </summary>
	public class ShopCartItemButton : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI _itemNameText, _itemPriceText;
		[SerializeField] Image _itemIcon;

		[SerializeField] TMP_InputField _quantityInputField;

		#region private API
		ShopUI shopUI;
		int quantity = 1;
		/// <summary>
		/// when input field UI is submitted
		/// </summary>
		void HandleInputSubmitted(string str)
		{
			// done internally inside .onEndEdit.AddListener((str) => { /* do somthng */ });
		}
		#endregion

		#region public API
		public ShopItem shopItem { get; private set; }

		public void Init(ShopUI shopUI, int quantity, ShopItem shopItem)
		{
			this.shopUI = shopUI;
			this.quantity = quantity;
			this.shopItem = shopItem;

			this._itemIcon.sprite = shopItem.itemDef.GetIcon();
			this.UpdateUI();
		}
		public int GetQuantity() { return this.quantity; }
		public void ChangeQuantity(int newQuantity)
		{
			int maxAffordable = quantity;
			var economyManager = Singleton<EconomyManager>.Ins;
			if (economyManager != null)
			{
				float avaialble = economyManager.GetMoney()
									- shopUI.getTotalCartPrice
									+ (shopItem.GetPrice() * quantity);
				maxAffordable = (avaialble / shopItem.GetPrice()).floor();
			}

			this.quantity = newQuantity.clamp(0, Mathf.Min(maxAffordable, shopItem.GetMaxStackSize()));
			this.UpdateUI();
		}
		public void AddQuantity(int deltaQuantity)
		{
			this.ChangeQuantity(quantity + deltaQuantity);
		}
		public void RemoveFromCart()
		{
			this.ChangeQuantity(newQuantity: 0);
		}
		

		public void UpdateUI()
		{
			this._itemNameText.text = shopItem.GetName();

			float totalPrice = shopItem.GetPrice() * quantity;
			this._itemPriceText.text = totalPrice.formatMoney();

			this._quantityInputField.text = quantity.ToString();
		}
		#endregion

		#region Unity Life Cycle
		private void OnEnable()
		{
			Debug.Log(C.method(this));
			this._quantityInputField.onEndEdit.AddListener((string str) =>
			{
				if (int.TryParse(str, out int result))
					this.ChangeQuantity(result);
			});
		}
		private void OnDisable()
		{
			Debug.Log(C.method(this, "orange"));
			this._quantityInputField.onEndEdit.RemoveAllListeners();
		}
		#endregion
	}
}