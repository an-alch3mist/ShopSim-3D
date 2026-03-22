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
		this.SHELF = new List<ShelfPOI>();
	}


	// Queue
	List<QueuePOI> QUEUE;
	public void RegisterQ(QueuePOI poi) { QUEUE.Add(poi); }
	public void UnRegisterQ(QueuePOI poi) { QUEUE.Remove(poi); }
	// first unOccupied queue
	public QueuePOI GetFirstAvailableQueueWithSlots()
	{
		return QUEUE.FirstOrDefault(poi => poi.HasSlotForBooking());
	}

	// Shelf
	List<ShelfPOI> SHELF;
	public void RegisterShelf(ShelfPOI poi) { SHELF.Add(poi); }
	public void UnRegisterShelf(ShelfPOI poi) { SHELF.Remove(poi); }
	// first shelf holding item that has stock and a free NPC slot
	public ShelfPOI GetFirstShelfWithItemAndAvaiableNPCSlot(SO_ItemData itemData)
	{
		return SHELF
					.find(shelf => shelf.HasStockOfItem(itemData) && shelf.HasSlotForBooking());
	}
	// all distinct item types currently stocked across all shelves
	public List<SO_ItemData> GetAllStockedItemsOnShelves()
	{
		return SHELF
					.flatMap(poi => poi.GetStockedItems())
					.rmdup()
					.ToList();
	}
	// all shelves that can currently accept atleast one more item
	// used by player stocking UI in Phase 2 to highlight valid shelves
	public List<ShelfPOI> GetAllShelvesAvailableForStockingItem(SO_ItemData itemData)
	{
		return SHELF
					.refine(poi => poi.GetTierForStocking(itemData) != null)
					.ToList();
	}
}
