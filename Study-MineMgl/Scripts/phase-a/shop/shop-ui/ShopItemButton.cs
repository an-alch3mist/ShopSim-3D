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
	public class ShopItemButton : MonoBehaviour
	{
		[SerializeField] Button _addButton;
		[SerializeField] TextMeshProUGUI _buttonText, _itemNameText, _itemDescrText, _itemPriceText;
		[SerializeField] Image _itemIcon;

		[Header("Colors")]
		[SerializeField] Color _canBuyButtonColor = Color.limeGreen, _canBuyTextColor = Color.limeGreen;
		[SerializeField] Color _cannotBuyButtonColor = Color.grey, _cannotBuyTextColor = Color.grey;

		#region private API

		ShopUI shopUI;
		int quantity = 1; 
		#endregion

		#region public API
		public ShopItem shopItem { get; private set; }
		public void Init(ShopItem shopItem, ShopUI shopUI)
		{
			this.shopItem = shopItem;
			this.shopUI = shopUI;

			if (this._itemNameText != null) this._itemNameText.text = this.shopItem.GetName();
			if (this._itemDescrText != null) this._itemDescrText.text = shopItem.GetDescr();
			if (this._itemIcon != null) this._itemIcon.sprite = shopItem.itemDef.icon;

			if (this._addButton != null)
				this._addButton.onClick.AddListener(() =>
				{
					this.shopUI.AddToCart(this.shopItem, this.quantity);
					this.UpdateUI();
				});
			this.UpdateUI();
		}
		public void ChangeQualntity(int quantity)
		{
			this.quantity = quantity;
			this.UpdateUI();
		}
		public void UpdateUI()
		{
			float itemCost = this.shopItem.itemDef.price * this.quantity;
			float availableMoney = 0f;
			if (Singleton<EconomyManager>.Ins != null)
				availableMoney = Singleton<EconomyManager>.Ins.GetMoney() - this.shopUI.getTotalCartPrice;
			bool canAfford = (availableMoney >= itemCost) && (this.shopItem.isLockedCurr == false);

			this._addButton.interactable = canAfford;

			if (this._itemPriceText != null)
				this._itemPriceText.text = (quantity == 1) ? $"${itemCost}": $"(x{quantity}) ${itemCost}";
			//
			if(shopItem.isLockedCurr == true)
			{
				this._buttonText.text = "locked";
				this._buttonText.color = _cannotBuyTextColor;
			}
			else if(canAfford == false)
			{
				this._buttonText.text = "cannot afford";
				this._buttonText.color = _cannotBuyTextColor;
			}
			else
			{
				this._buttonText.text = "add to cart";
				this._buttonText.color = _canBuyTextColor;
			}

			Image image = this._addButton.gc<Image>();
			image.color = (canAfford) ? _canBuyButtonColor : _cannotBuyButtonColor;
		}
		#endregion
	}
}
