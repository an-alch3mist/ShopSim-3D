using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/*
## Phase-1 Customer FSM  —  Enter / Tick split
   ─────────────────────────────────────────────
   Every state has at most two methods:

     Enter{State}  fired once by TransitionTo(), via OnEnterState()
     Tick{State}   fired every frame by Tick(), only when NOT in nav

   States that only need Enter (nav states):
     walkIn · bookAndNavigateToShelf · takeItem · leaveStore · walkOut

   States that only need Tick (evaluation / timer states):
     selectItem · bookAndJoinQueue · waitInQueue

   done → Enter only (log once, never tick again)

   No isStateCalledOnce flag anywhere.

   State flow:
   ──────────
   walkIn
   └─► selectItem ◄─────────────────────────────────────────────┐
           │                                                    │
           ├─ item found, shelf free ──► bookAndNavigateToShelf │
           │                                └─► takeItem ───────┘
           │
           ├─ item has stock, shelf BUSY
           │     └─ _deferCount < list.Count → rotate list, retry next tick
           │     └─ _deferCount >= list.Count → all blocked, wait + reset
           │
           ├─ item out of stock → remove all copies, retry next tick
           │
           └─ list empty ──► bookAndJoinQueue (retry tick until slot found)
                                  └─► waitInQueue
                                          └─► leaveStore
                                                  └─► walkOut → Destroy
*/

public enum CustomerState
{
	walkIn,
	selectItem,
	bookAndNavigateToShelf,
	takeItem,
	bookAndJoinQueue,
	waitInQueue,
	leaveStore,
	walkOut,
	done,
}

public class CustomerFSM : MonoBehaviour
{
	// ── Runtime state ──────────────────────────────────────────
	bool isInProgressNav = false;
	float waitInQueueTimer = 0f;
	float waitInQueueDuration = 10f;
	int _deferCount = 0;   // how many items rotated in current selectItem pass

	public CustomerAgent owner;
	public CustomerState currState { get; private set; }

	// ── Init ───────────────────────────────────────────────────
	public void Init(CustomerAgent agent)
	{
		this.owner = agent;
	}

	private void Start()
	{
		Debug.Log(C.method(this));
		// set directly then fire Enter — avoids the currState == next early-out in TransitionTo
		this.currState = CustomerState.walkIn;
		this.OnEnterState(CustomerState.walkIn);
	}

	// ── Tick ───────────────────────────────────────────────────
	// Only states that need per-frame evaluation live here.
	// Nav states skip entirely because isInProgressNav returns early.
	public void Tick()
	{
		if (this.isInProgressNav)
			return;

		if (currState == CustomerState.selectItem) TickStateSelectItem();
		else if (currState == CustomerState.bookAndJoinQueue) TickStateBookAndJoinQueue();
		else if (currState == CustomerState.waitInQueue) TickStateWaitInQueue();
	}
	// Dispatch table: maps a state to its Enter method
	// States with no Enter (selectItem, bookAndJoinQueue, waitInQueue) are simply absent.
	// called from TransitionTo to fire instatly that frame
	void OnEnterState(CustomerState state)
	{
		if (state == CustomerState.walkIn) EnterStateWalkIn();
		else if (state == CustomerState.bookAndNavigateToShelf) EnterStateBookAndNavigateToShelf();
		else if (state == CustomerState.takeItem) EnterStateTakeItem();
		else if (state == CustomerState.leaveStore) EnterStateLeaveStore();
		else if (state == CustomerState.walkOut) EnterStateWalkOut();
		else if (state == CustomerState.done) EnterStateDone();
	}

	// ── Enter methods ──────────────────────────────────────────
	// Called exactly once per state entry, from OnEnterState().
	#region Enter
	// 0.
	void EnterStateWalkIn()
	{
		isInProgressNav = true;
		LOG.AddLog(new List<CustomerAgent> { owner }.ToTable(name: "owner that is executing its fsm"));
		owner.Mover.MoveTo(owner.TrEntrancePoint.position, onArrived: () =>
		{
			isInProgressNav = false;
			GameEvents.RaiseCustomerEntered(owner);
			TransitionTo(CustomerState.selectItem);
		});
	}
	// selectItem has no Enter — evaluation starts immediately on the first Tick.
	// 1.
	void EnterStateBookAndNavigateToShelf()
	{
		// BookSlot can return null if another NPC grabbed the slot between
		// selectItem and here (race condition between two agents).
		Transform trSlot = owner.currentShelfPOI.BookSlot(owner);
		if (trSlot == null)
		{
			// slot stolen — fall back to selectItem to find another shelf
			Debug.Log($"[FSM] {owner.customerId}: slot stolen — returning to selectItem".colorTag("orange"));
			owner.currentShelfPOI = null;
			owner.currentTargetItem = null;
			TransitionTo(CustomerState.selectItem);
			return;
		}
		isInProgressNav = true;
		owner.Mover.MoveTo(trSlot.position.xz(), onArrived: () =>
		{
			isInProgressNav = false;
			TransitionTo(CustomerState.takeItem);
		});
	}

	// 2.
	void EnterStateTakeItem()
	{
		this.StartCoroutine(RoutineTakeItem());
	}
	#region Coroutines
	IEnumerator RoutineTakeItem()
	{
		yield return null;
		yield return new WaitForSeconds(1f); // grab pause

		Debug.Log(C.method(this, "magenta"));
		owner.currentShelfPOI.TryTakeItem(owner.currentTargetItem);
		owner.currentShelfPOI.ReleaseSlot(owner);

		owner.shoppingList.RemoveAt(0);

		owner.currentTargetItem = null;
		owner.currentShelfPOI = null;
		TransitionTo(CustomerState.selectItem);
	}

	#endregion

	/* TICK >>
	Select Item
	Book And Join Queue
	Wait In Queue
	<< TICK */ 

	// 6.
	void EnterStateLeaveStore()
	{
		isInProgressNav = true;
		owner.Mover.MoveTo(owner.TrExitPoint.position, onArrived: () =>
		{
			isInProgressNav = false;
			GameEvents.RaiseCustomerLeft(owner);
			TransitionTo(CustomerState.walkOut);
		});
	}
	// 7.
	void EnterStateWalkOut()
	{
		isInProgressNav = true;
		owner.Mover.MoveTo(owner.TrDespawnPoint.position, onArrived: () =>
		{
			isInProgressNav = false;
			TransitionTo(CustomerState.done);
			GameObject.Destroy(owner.gameObject, 0.2f);
		});
	}
	// 8.
	void EnterStateDone()
	{
		Debug.Log($"{owner.customerId} life cycle complete".colorTag("lime"));
	}
	#endregion

	// ── Tick methods ───────────────────────────────────────────
	// Called every frame while in this state (and not in nav).
	#region Tick
	// 3.
	// Runs every tick — resolves shopping list one item at a time.
	void TickStateSelectItem()
	{
		// ── list empty: head to queue ─────────────────────────
		if (owner.shoppingList.Count == 0)
		{
			_deferCount = 0;
			TransitionTo(CustomerState.bookAndJoinQueue);
			return;
		}

		SO_ItemData desiredItemData = owner.shoppingList[0];

		// ── case 1: item genuinely out of stock ───────────────
		if (!POIRegistry.Ins.AnyShelfHasStockOf(desiredItemData))
		{
			Debug.Log($"[FSM] {owner.customerId}: {desiredItemData.id} out of stock — removing all copies".colorTag("orange"));
			owner.shoppingList = owner.shoppingList.refine(item => item != desiredItemData).ToList();
			_deferCount = 0;
			return; // re-evaluate next tick
		}

		// ── case 2: stock exists but every matching shelf slot is busy ─
		ShelfPOI poi = POIRegistry.Ins.GetFirstShelfWithItemAndAvaiableNPCSlot(desiredItemData);
		if (poi == null)
		{
			_deferCount += 1;
			if (_deferCount >= owner.shoppingList.Count)
			{
				// lapped the whole list — every item blocked, wait a tick then reset
				Debug.Log($"[FSM] {owner.customerId}: all {owner.shoppingList.Count} items blocked — waiting".colorTag("yellow"));
				_deferCount = 0;
				return;
			}
			// rotate: push current item to end, try the next one next tick
			SO_ItemData deferred = owner.shoppingList[0];
			owner.shoppingList.RemoveAt(0);
			owner.shoppingList.Add(deferred);
			Debug.Log($"[FSM] {owner.customerId}: {desiredItemData.id} shelf busy — deferred (count: {_deferCount})".colorTag("yellow"));
			return;
		}

		// ── case 3: shelf found with stock and a free slot ────
		_deferCount = 0;
		owner.currentTargetItem = desiredItemData;
		owner.currentShelfPOI = poi;
		TransitionTo(CustomerState.bookAndNavigateToShelf);
	}

	// 4.
	// Retries every tick until a queue slot opens up.
	// Once MoveTo fires, isInProgressNav = true and Tick() short-circuits — no double-booking possible.
	void TickStateBookAndJoinQueue()
	{
		QueuePOI poi = POIRegistry.Ins.GetFirstAvailableQueueWithSlots();
		if (poi == null)
			return; // retry next tick

		Transform slot = poi.BookSlot(owner);
		owner.currentQueuePOI = poi;
		owner.TrCurrentQueueSlot = slot;
		isInProgressNav = true;
		owner.Mover.MoveTo(slot.position.xz(), onArrived: () =>
		{
			isInProgressNav = false;
			GameEvents.RaiseCustomerJoinedQ(owner);
			waitInQueueTimer = 0f;
			waitInQueueDuration = C.Random(owner.profileData.minQWaitSec, owner.profileData.maxQWaitSec);
			TransitionTo(CustomerState.waitInQueue);
		});
	}

	// 5.
	void TickStateWaitInQueue()
	{
		waitInQueueTimer += Time.deltaTime;
		if (waitInQueueTimer >= waitInQueueDuration)
		{
			owner.currentQueuePOI.ReleaseSlot(owner);
			owner.currentQueuePOI = null;
			owner.TrCurrentQueueSlot = null;
			TransitionTo(CustomerState.leaveStore);
		}
	}
	#endregion

	// ── Transition ─────────────────────────────────────────────
	#region TransitionTo
	private void TransitionTo(CustomerState next)
	{
		if (currState == next)
			return;
		Debug.Log($"[FSM] {gameObject.name}: {currState} → {next}");
		currState = next;
		OnEnterState(next);  // ← same frame
	}
	#endregion
}