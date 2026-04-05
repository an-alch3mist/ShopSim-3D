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
	public class DEBUG_Check : MonoBehaviour
	{
		private void Start()
		{
			Debug.Log(C.method(this));
			this.StopAllCoroutines();
			this.StartCoroutine(STIMULATE());
		}

		IEnumerator STIMULATE()
		{
			// yield return this.routineCheckSomthng();
			yield return routineCheckAnyMenuOpen();
			yield return null;
		}
		//
		IEnumerator routineCheckSomthng()
		{
			Debug.Log($"somthng".colorTag("lime"));
			yield return null;
		}
		IEnumerator routineCheckAnyMenuOpen()
		{
			Debug.Log(C.method(this, "lime"));
			bool isAnyMenuOpened = true;
			GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: isAnyMenuOpened);
			while (true)
			{
				if (INPUT.K.InstantDown(KeyCode.Tab))
				{
					// Debug.Log($"menu open/close toggled {isAnyMenuOpened}".colorTag("lime"));
					isAnyMenuOpened = !isAnyMenuOpened;
					GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: isAnyMenuOpened);
				}
				//
				yield return null;
			}
			yield return null;
		}
	}
}