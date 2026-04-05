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
	/// category + item + cart + (balance + cartTotal + purchase)
	/// </summary>
	public class ShopUI : MonoBehaviour
	{
		[Header("Holders")]
		[SerializeField] Transform _categoryListHolder, _shopItemListHolder, _cartListHolder;

		[Header("Prefabs")]
		[SerializeField] GameObject _pfcategory, _pfShopItem, _pfCartItem;

		[Header("Individual UI")]
		[SerializeField] TextMeshProUGUI _balanceMoneyText, _cartTotalText;
		[SerializeField] Button _purchaseButton;
		[SerializeField] Color _canAfford = Color.limeGreen, _cannotAfford = Color.grey;

		#region private API
		SO_ShopCategory selectedCategory = null;
		List<ShopCategoryButton> CATEGORY_BUTTON = new List<ShopCategoryButton>();
		List<ShopItemButton> ITEM_BUTTON = new List<ShopItemButton>();
		List<ShopCartItemButton> CART_ITEM_BUTTON = new List<ShopCartItemButton>();

		void InitSetUpCategories()
		{
			foreach(var categoryButton in this.CATEGORY_BUTTON)
			{
				categoryButton.OnPressed -= this.OpenCategory;
				GameObject.Destroy(categoryButton.gameObject);
			}
			this.CATEGORY_BUTTON.Clear();
			this.ClearLeaf(_categoryListHolder);

			Singleton<ShopManager>.Ins.GetAllCategories().forEach(category =>
			{
				var categoryButton = GameObject.Instantiate(this._pfcategory, this._categoryListHolder)
												.gc<ShopCategoryButton>();
				this.CATEGORY_BUTTON.Add(categoryButton);

				categoryButton.Init(category);
				categoryButton.OnPressed += OpenCategory;
				if (category.shouldHideIfAllLocked)
					if (this.AreAllItemsLocked(category))
						categoryButton.gameObject.toggle(false);
			});
			// open the first category by default
			if (this.CATEGORY_BUTTON.Count > 0)
				this.OpenCategory(CATEGORY_BUTTON[0].category);
		}
		void RefreshCurrency()
		{
			this._balanceMoneyText.text = Singleton<EconomyManager>.Ins.GetMoney().formatMoney();
			this._purchaseButton.interactable = this.CanAffordCart();

			this._cartTotalText.text = (this.CART_ITEM_BUTTON.Count > 0) ?
											getTotalCartPrice.formatMoney() :
											"$0.00";
		}
		void RefreshOnItemUnlocked(ShopItem item)
		{
			RepopulateItemList();
		}
		void ToggleShop()
		{
			this.gameObject.toggle(flip: true);
		}

		void OpenCategory(SO_ShopCategory category)
		{
			this.selectedCategory = category;
			this.CATEGORY_BUTTON.forEach(categoryButton =>
			{
				categoryButton.SetSelected(categoryButton.category == category);
			});
			this.RepopulateItemList();
		}
		bool AreAllItemsLocked(SO_ShopCategory category)
		{
			foreach (var item in category.SHOP_ITEM)
				if (item.isLockedCurr == false)
					return false;
			return true;
		}

		/// <summary>
		/// destroys and recreates item buttons for the selected category
		/// </summary>
		void RepopulateItemList()
		{
			if (this.selectedCategory == null)
				return;
			this.ClearLeaf(this._shopItemListHolder);
			this.selectedCategory.SHOP_ITEM.forEach(item =>
			{
				GameObject.Instantiate(this._pfShopItem, this._shopItemListHolder)
							.gc<ShopItemButton>()
							.Init(item, this);
			});
		}

		bool CanAffordCart()
		{
			this.getTotalCartPrice = 0;
			this.CART_ITEM_BUTTON.forEach(cartItem =>
			{
				this.getTotalCartPrice += cartItem.shopItem.GetPrice() * cartItem.GetQuantity();
			});
			bool canMoneyAfford = (Singleton<EconomyManager>.Ins.GetMoney() >= getTotalCartPrice);
			return (this.CART_ITEM_BUTTON.Count) > 0 && canMoneyAfford;
		}
		void ClearCart()
		{
			this.ClearLeaf(this._cartListHolder);
			this.CART_ITEM_BUTTON.Clear();
		}
		public void AddToCart(ShopItem item, int quantity = 1)
		{
			var existingCartItem = this.FindCartItem(item);
			if(existingCartItem != null)
			{
				existingCartItem.ChangeQuantity(existingCartItem.GetQuantity() + quantity);
				return;
			}

			var cartItemButton = GameObject.Instantiate(this._pfCartItem, this._cartListHolder)
											.gc<ShopCartItemButton>();
			cartItemButton.Init(this, quantity, item);
			this.CART_ITEM_BUTTON.Add(cartItemButton);
		}
		void RemoveFromCart(ShopCartItemButton cartItemButton)
		{
			this.CART_ITEM_BUTTON.Remove(cartItemButton);
			GameObject.Destroy(cartItemButton.gameObject);
		}
		ShopCartItemButton FindCartItem(ShopItem item)
		{
			foreach (var cartItemButton in this.CART_ITEM_BUTTON)
				if (cartItemButton.shopItem == item)
					return cartItemButton;
			return null;
		}
		public void PurchaseCart()
		{
			if(this.CanAffordCart() == false)
			{
				Debug.Log("cannot afford cart contents.".colorTag("orange"));
				return;
			}

			var toProcess = new List<ShopCartItemButton>(this.CART_ITEM_BUTTON);
			toProcess.forEach(cartItemButton =>
			{
				ShopItem item = cartItemButton.shopItem;
				int qty = cartItemButton.GetQuantity();
				float cost = item.GetPrice() * qty;

				if(this.TrySpawnItem(item.itemDef, qty)) // if there any spawn points
				{
					Singleton<EconomyManager>.Ins.AddMoney(-cost);
					item.AddPurchasedQuantity(qty);
					CART_ITEM_BUTTON.Remove(cartItemButton);
					GameObject.Destroy(cartItemButton.gameObject);
					GameEvents.RaiseItemPurchased();
				}
			});
			this.RefreshCurrency();
		}

		void ClearLeaf(Transform holder)
		{
			for (int i0 = holder.childCount - 1; i0 >= 0; i0 -= 1)
				GameObject.Destroy(holder.GetChild(i0).gameObject);

		}

		/// <summary>
		/// spawn purchased item into the game world.
		/// </summary>
		/// <returns></returns>
		bool TrySpawnItem(SO_ShopItemDefination itemDef, int quantity = 1)
		{
			var spawnPoint = ShopSpawnPoint.GetRandomSpawnPoint();
			if(spawnPoint == null)
			{
				Debug.Log($"no spawn points found".colorTag("red"));
				return false;
			}

			for(int i0 = 0; i0 < quantity; i0 += 1)
			{
				Vector3 offset = UnityEngine.Random.insideUnitSphere * 0.3f;
				offset.y = offset.y.abs();
				GameObject.Instantiate(
					itemDef.pfToSpawn,
					spawnPoint.transform.position + offset, 
					spawnPoint.transform.rotation);
			}
			return true;
		}
		#endregion

		#region public API
		public float getTotalCartPrice { get; private set; }
		#endregion

		#region Unity Life Cycle
		private void OnEnable()
		{
			Debug.Log(C.method(this));
			if(this.selectedCategory == null)
				this.InitSetUpCategories();
			this.RefreshCurrency();
			// subscribe >>
			GameEvents.OnShopItemUnlocked += this.RefreshOnItemUnlocked;
			// << subscribe
			GameEvents.RaiseMenuStateChanged(true);
		}
		private void Start()
		{
			Debug.Log(C.method(this));
			// subscribe >>
			GameEvents.OnToggleShopUI += this.ToggleShop;
			// << subscribe
		}
		private void Update()
		{
			this.RefreshCurrency();
			if (INPUT.K.InstantDown(KeyCode.E) || INPUT.K.InstantDown(KeyCode.Tab))
				this.gameObject.toggle(false);
		}
		private void OnDisable()
		{
			Debug.Log(C.method(this, "orange"));
			// unSubscribe >>
			GameEvents.OnShopItemUnlocked -= this.RefreshOnItemUnlocked;
			// << unSubscribe
			GameEvents.RaiseMenuStateChanged(false);
		}
		private void OnDestroy()
		{
			Debug.Log(C.method(this, "orange"));
			// unSubscribe >>
			GameEvents.OnToggleShopUI -= this.ToggleShop;
			// << unSubscribe
		}
		#endregion
	}
}