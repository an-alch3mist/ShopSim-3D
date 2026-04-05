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
	/// SO_ defining single interaction eg: "use", "open", "take"
	/// </summary>
	[CreateAssetMenu(fileName = "new SO_interaction",
					 menuName = "Interactions/SO_Interaction")]
	public class SO_Interaction : ScriptableObject
	{
		public string interationName;
		public string descr;
		public Sprite icon;
	}
}