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
	// just one instance in entire scene, duplicates are destroyed
	public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		public static T Ins { get; private set; }

		protected virtual void Awake()
		{
			if (Ins == null)
				Ins = this as T;
			else
			{
				Debug.Log($"{typeof(T)} singleton already exist, distroying duplicate objName: {this.gameObject.name}".colorTag("orange"));
				GameObject.Destroy(this.gameObject);
			}
		}
	}
}