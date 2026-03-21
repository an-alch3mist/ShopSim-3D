using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

[CreateAssetMenu(fileName = "SO_customerProfile", menuName ="ShopSim_SO/SO_customerProfile")]
public class SO_CustomerProfileData : ScriptableObject
{
	public string id = "name";
	public float walkSpeed = 3f;

	public float minQWaitSec = 10f, maxQWaitSec = 20f;

	[Range(0, 99)]
	public int avoidancePriority = 50;
}
