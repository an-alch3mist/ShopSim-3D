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
	/// interface for any world object the player can interact with
	/// implemented by shop terminals, machines, tools, etc
	/// </summary>
	public interface IInteractable
	{
		bool ShouldUseInteractionWheel();
		List<SO_Interaction> GetInteractions();
		string GetObjectName();
		void Interact(SO_Interaction selectedInteraction);
	}
}
