using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using SPACE_UTIL;

public class POIRegistry : MonoBehaviour
{
	public static POIRegistry Ins { get; private set; }

	private void Awake()
	{
		Debug.Log(C.method(this));
		this.Init();
	}

	public void Init()
	{
		POIRegistry.Ins = this;
		this.QUEUE = new List<QueuePOI>();
	}


	// Queue
	List<QueuePOI> QUEUE;
	public void RegisterQ(QueuePOI poi) { QUEUE.Add(poi); }
	public void UnRegisterQ(QueuePOI poi) { QUEUE.Remove(poi); }
	// return first unOccupied queue
	public QueuePOI GetFirstAvailableQueueWithSlots()
	{
		return QUEUE.FirstOrDefault(poi => poi.HasAnyAvailableSlot());
	}
}
