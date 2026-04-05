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
	/// singleton with global UI state: menu's that are open, cursor visibility etc
	/// </summary>
	public class UIManager : Singleton<UIManager>
	{
		[SerializeField] ShopUI shopUI;
		[SerializeField] InteractionWheelUI interactionWheelUI;

		[SerializeField] GameObject _hudObj;
		[SerializeField] GameObject _backgroundBlur;

		public bool IsInAnyMenu()
		{
			if (this.shopUI.gameObject.activeSelf == true)
				return true;
			if (interactionWheelUI.gameObject.activeSelf)
				return true;
			return false;
		}
		public bool IsInShopMenu()
		{
			return this.shopUI.gameObject.activeSelf;
		}
		public void SetBgBlur(bool enabled)
		{
			this._backgroundBlur.toggle(enabled);
		}
		public void SetHudElems(bool enabled)
		{
			this._hudObj.toggle(enabled);
		}

		#region Unity Life Cycle
		private void LateUpdate()
		{
			this.SetBgBlur(this.IsInAnyMenu());
		}
		#endregion
	}
}