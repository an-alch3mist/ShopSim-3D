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
	/// sunscribes to game event for decoupled updates
	/// </summary>
	public class MoneyDisplay : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI _moneyText;

		#region private API
		void HandleMoneyChanged(float newAmount)
		{
			if (Singleton<EconomyManager>.Ins != null)
				this._moneyText.text = Singleton<EconomyManager>.Ins.GetMoney().formatMoney();
			else
				this._moneyText.text = "No Economy Manager Ins".colorTag("red");
		}
		#endregion

		#region Unity Life Cycle
		private void OnEnable()
		{
			
		}
		private void OnDisable()
		{
			
		}
		#endregion
	}
}