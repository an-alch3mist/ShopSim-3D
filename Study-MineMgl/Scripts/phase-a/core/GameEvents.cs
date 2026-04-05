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
	// a central event bus for decoupled event communiacation
	public class GameEvents : MonoBehaviour
	{
		// when economy money is altered >>
		public static event Action<float> OnMoneyChanged;
		public static void RaiseMoneyChanged(float amount)
		{
			Debug.Log(C.method(null, "magenta"));
			GameEvents.OnMoneyChanged? // if there are any subscribers
				.Invoke(amount);
		}
		// << when economy money is altered


		// when menu is opened are closed, param true if menu opened >>
		public static event Action<bool> OnMenuStateChanged;
		public static void RaiseMenuStateChanged(bool isAnyMenuOpen)
		{
			Debug.Log(C.method(null, "magenta"));
			GameEvents.OnMenuStateChanged? // any subscribers ?
				.Invoke(isAnyMenuOpen);
		}
		// << when menu is opened are closed, param true if menu opened
	}
}