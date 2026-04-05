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
	/// UI for shop category.
	/// </summary>
	public class ShopCategoryButton : MonoBehaviour
	{
		[SerializeField] Image _bgButtonImage;
		[SerializeField] TextMeshProUGUI _tmCategoryName;
		
		[SerializeField] Color _selectedColor = Color.limeGreen, 
							   _normalColor = Color.white;
		

		public SO_ShopCategory category { get; private set; }
		public void Init(SO_ShopCategory category)
		{
			this.category = category;
			this._tmCategoryName.text = category.name;
		}

		// when instant button down >>
		public event Action<SO_ShopCategory> OnPressed;
		public void RaisePressed()
		{
			this.OnPressed? // if there any subscribers
				.Invoke(this.category);
		}
		// << when instant button down

		public void SetSelected(bool isSelected)
		{
			this._bgButtonImage.color = (isSelected) ? this._selectedColor : this._normalColor;
		}
	}
}