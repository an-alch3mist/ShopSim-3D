using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/*

	States:
	-------
	0. walkIn
		|
	1. joinQ
		|
	2. waitInQ
		|
	3. leaveStore
		|
	4. walkOut

*/

public enum CustomerState
{
	walkIn,
	joinQ,
	waitInQ,
	leaveStore,
	walkOut,
	done,
}

public class CustomerFSM : MonoBehaviour
{
	CustomerState _state = CustomerState.walkIn;
	bool _init = false;
	bool _waitNav = false;
	float _timer = 0f;
	float _waitDuration = 10f;
}
