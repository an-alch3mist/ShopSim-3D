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
	// All Money Operations Go Through This Class
	public class EconomyManager : Singleton<EconomyManager>
	{
		[SerializeField] private float _money = 400f;

		// public event Action<float> OnMoneyChanged;
		protected override void Awake()
		{
			Debug.Log(C.method(this));
			base.Awake();
			GameEvents.RaiseMoneyChanged(this._money);
		}

		public float getMoney
		{
			get { return this._money; }
		}
		public void AddMoney(float amount)
		{
			this._money += amount;
			GameEvents.RaiseMoneyChanged(this._money);
		}
		public void SetMoney(float amount)
		{
			this._money = amount;
			GameEvents.RaiseMoneyChanged(this._money);
		}
		public bool CanAfford(float amount)
		{
			return this._money >= amount;
		}
	}
}