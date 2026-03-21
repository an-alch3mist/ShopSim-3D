using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using SPACE_UTIL;

public interface IPOI
{
	string POIId { get; }
	//
	bool HasAnyAvailableSlot();
	Transform BookSlot(CustomerAgent agent);
	void ReleaseSlot(CustomerAgent agent);
}
