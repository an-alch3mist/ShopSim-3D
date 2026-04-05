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
	/// attach as componenet to random spawn point obj in scene.
	/// </summary>
	public class ShopSpawnPoint : MonoBehaviour
	{
		public static ShopSpawnPoint GetRandomSpawnPoint()
		{
			var POINT = GameObject.FindObjectsOfType<ShopSpawnPoint>();
			return POINT.getRandom();
		}
		private void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(transform.position, radius: 0.25f);
		}
	}
}