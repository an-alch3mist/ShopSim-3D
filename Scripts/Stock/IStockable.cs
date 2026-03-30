using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ============================================================
//  IStockable.cs
//  Contract for any world object that can receive item stock.
//
//  Implementors (current):
//    ShelfPOI
//
//  Implementors (future):
//    StockCrate  — player carries box, drops near shelf
//    CoolerUnit  — temperature-zone variant
//    DisplayBin  — bulk bin aisle
//
//  PlayerStockingController only ever references this interface,
//  never a concrete type. Adding a new stockable surface = zero
//  changes to the controller.
//
//  Deliberately NOT extending IPOI:
//    IPOI is about NPC slot-booking (transform reservations).
//    IStockable is about item ingestion. Different concerns,
//    different lifecycles — keeping them separate avoids forcing
//    every stockable to also be an NPC-bookable POI.
// ============================================================

public interface IStockable
{
	/// <summary>Human-readable id for logging and events.</summary>
	string stockableId { get; }

	/// <summary>
	/// True if this receiver can currently accept at least one unit of itemData.
	/// Call before TryReceiveStock to give UI feedback without mutating state.
	/// </summary>
	bool CanRecieveStock(SO_ItemData itemData);

	/// <summary>
	/// Attempt to add one unit of itemData.
	/// Returns true if the unit was actually placed.
	/// Implementations are responsible for firing GameEvents.
	/// </summary>
	bool TryReciveStock(SO_ItemData itemData);
}
