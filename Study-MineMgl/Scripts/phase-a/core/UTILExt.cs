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
	public static class UTILExt
	{
		/// <summary>
		/// formatted eg: $1,2345.67
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public static string formatMoney(this float amount)
		{
			return string.Format($"${amount:#,##0.00}");
		}
	}
}
