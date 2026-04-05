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
	public class ShopTerminal : MonoBehaviour, IInteractable
	{
		[SerializeField] List<SO_Interaction> _INTERACTION = new List<SO_Interaction>();

		public string GetObjectName()
		{
			return "shop terminal";
		}
		public void Interact(SO_Interaction selectedInteraction)
		{
			GameEvents.RaiseToggleShopUI();
		}
		public List<SO_Interaction> GetInteractions()
		{
			return this._INTERACTION;
		}
		public bool ShouldUseInteractionWheel()
		{
			return false;
		}
	}
}