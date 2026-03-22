using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/*
## phase-0 Customer state machine.
	State flow:
	-----------
	0. walkIn
		|
	1. joinQueue
		|
	2. waitInQueue
		|
	3. leaveStore
		|
	4. walkOut

*/

/*
## phase-1 Customer state machine.

State flow:
-----------
  walkIn
    └─► selectItem ◄───────────────────────────────┐
            ├─ item found ─► navigateToShelf       │
            │                    └─► takeItem ─────┘
            └─ list empty ─► joinQueue
                                 └─► waitInQueue
                                         └─► leaveStore
                                                 └─► walkOut → Destroy

hasDoneInit  — reset on each TransitionTo; entry code runs once per state.
isInProgressNav — true while NavMeshMover callback pending; Tick() no-ops.
*/

public enum CustomerState
{
	// phase-0 >>
	walkIn,
	bookAndJoinQueue,
	waitInQueue,
	leaveStore,
	walkOut,
	done,
	// << phase-0

	// +phase-1 >>
	selectItem,
	bookAndNavigateToShelf,
	takeItem,
	// << +phase-1
}

public class CustomerFSM : MonoBehaviour
{
	bool isStateCalledOnce = false;
	bool isInProgressNav = false;
	float waitInQueueTimer = 0f;
	float waitInQueueDuration = 10f;

	CustomerAgent owner;
	public CustomerState currState { get; private set; }
	public void Init(CustomerAgent agent)
	{
		this.owner = agent;
		this.currState = CustomerState.walkIn;
	}
	static int phaseDev = 1;
	public void Tick()
	{
		if (this.isInProgressNav == true)
			return;

		if (phaseDev == 0)
		{
				 if (this.currState == CustomerState.walkIn) ExecStateWalkIn();
			else if (this.currState == CustomerState.bookAndJoinQueue) ExecStateBookAndJoinQueue();
			else if (this.currState == CustomerState.waitInQueue) ExecStateWaitInQueue();
			else if (this.currState == CustomerState.leaveStore) ExecStateLeaveStore();
			else if (this.currState == CustomerState.walkOut) ExecStateWalkOut();
			else if (this.currState == CustomerState.done)
			{
				// done
				Debug.Log($"{owner.customerId} life cycle complete".colorTag("lime"));
			}
		}
		else if(phaseDev == 1)
		{
				 if (this.currState == CustomerState.walkIn) ExecStateWalkIn();
			else if (this.currState == CustomerState.selectItem) ExecStateSelectItem();
			else if (this.currState == CustomerState.bookAndNavigateToShelf) ExecStateBookAndNavigateToShelf();
			else if (this.currState == CustomerState.takeItem) ExecStateTakeItem();
			else if (this.currState == CustomerState.bookAndJoinQueue) ExecStateBookAndJoinQueue();
			else if (this.currState == CustomerState.waitInQueue) ExecStateWaitInQueue();
			else if (this.currState == CustomerState.leaveStore) ExecStateLeaveStore();
			else if (this.currState == CustomerState.walkOut) ExecStateWalkOut();
			else if (this.currState == CustomerState.done)
			{
				// done
				Debug.Log($"{owner.customerId} life cycle complete".colorTag("lime"));
			}
		}
	}

	#region Exec State
	void ExecStateWalkIn()
	{
		#region call this state just once
		if (this.isStateCalledOnce == true)
			return;
		isStateCalledOnce = true;
		#endregion
		isInProgressNav = true;
		owner.Mover.MoveTo(owner.TrEntrancePoint.position, onArrived: () =>
		{
			isInProgressNav = false;
			GameEvents.RaiseCustomerEntered(agent: owner);
			if(phaseDev == 0)
				TransitionTo(CustomerState.bookAndJoinQueue);
			else
				TransitionTo(CustomerState.selectItem);
		});
	}
	#region Phase-1 ExecState 
	// runs every tick until list resolved
	void ExecStateSelectItem()
	{
		if(owner.shoppingList.Count == 0)
		{
			TransitionTo(CustomerState.bookAndJoinQueue);
			return;
		}

		// LOG.AddLog(owner.shoppingList.ToTable(name: $"LIST<> ITEM shopping list on enter select item state, {owner.customerId}"));
		SO_ItemData desiredItemData = owner.shoppingList[0];
		ShelfPOI poi = POIRegistry.Ins.GetFirstShelfWithItemAndAvaiableNPCSlot(itemData: desiredItemData);

		if(poi == null)
		{
			// either out of stock, or that itemData shelfTier poi is occupied by other NPC
			owner.shoppingList.RemoveAt(0);
			return;
		}
		owner.currentTargetItem = desiredItemData;
		// Debug.Log($"desired item: {desiredItemData.id}".colorTag("cyan"));
		owner.currentShelfPOI = poi;
		// Debug.Log($"currentShelfPOI: {poi.POIId}".colorTag("cyan"));
		TransitionTo(CustomerState.bookAndNavigateToShelf);
	}
	void ExecStateBookAndNavigateToShelf()
	{
		#region call this state just once
		if (isStateCalledOnce == true)
			return;
		isStateCalledOnce = true;
		#endregion
		Transform trSlot = owner.currentShelfPOI.BookSlot(owner);

		isInProgressNav = true;
		owner.Mover.MoveTo(trSlot.position.xz(), onArrived: () =>
		{
			isInProgressNav = false;
			TransitionTo(CustomerState.takeItem);
		});
	}
	void ExecStateTakeItem()
	{
		#region call this state just once
		if (isStateCalledOnce == true)
			return;
		isStateCalledOnce = true;
		#endregion
		// transition made inside routine
		StartCoroutine(RoutineTakeItem());
	}
	IEnumerator RoutineTakeItem()
	{
		yield return null;
		yield return new WaitForSeconds(1f); // grab pause
		Debug.Log(C.method(this, "magenta"));
		owner.currentShelfPOI.TryTakeItem(owner.currentTargetItem);
		owner.currentShelfPOI.ReleaseSlot(owner);
		// item taken complete

		owner.shoppingList.RemoveAt(0);
		LOG.AddLog(owner.shoppingList.ToTable(name: $"LIST<> ITEM shopping list after item taken, {owner.customerId}"));

		owner.currentTargetItem = null;
		owner.currentShelfPOI = null;
		TransitionTo(CustomerState.selectItem);
	}
	#endregion
	void ExecStateBookAndJoinQueue()
	{
		#region call this state just once
		if (this.isStateCalledOnce == true)
			return;
		isStateCalledOnce = true;
		#endregion
		//
		QueuePOI poi = POIRegistry.Ins.GetFirstAvailableQueueWithSlots();
		// retry
		if (poi == null) // queue is full, retry next tick
		{
			isStateCalledOnce = false;
			return;
		}
		var slot = poi.BookSlot(owner);

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
	void ExecStateWaitInQueue()
	{
		waitInQueueTimer += Time.deltaTime;
		if (waitInQueueTimer >= waitInQueueDuration)
		{
			/*
			if (owner.currentQueuePOI == null)
				Debug.Log(owner.gameObject.name + " null queue".colorTag("red"));
			*/
			owner.currentQueuePOI.ReleaseSlot(owner);
			owner.currentQueuePOI = null;
			owner.TrCurrentQueueSlot = null;
			TransitionTo(CustomerState.leaveStore);
		}
	}
	void ExecStateLeaveStore()
	{
		#region call this state just once
		if (this.isStateCalledOnce == true) return; isStateCalledOnce = true;
		#endregion
		//
		isInProgressNav = true;
		owner.Mover.MoveTo(owner.TrExitPoint.position, onArrived: () =>
		{
			isInProgressNav = false;
			GameEvents.RaiseCustomerLeft(agent: owner);
			TransitionTo(CustomerState.walkOut);
		});
	}
	void ExecStateWalkOut()
	{
		#region call this state just once
		if (this.isStateCalledOnce == true)
			return;
		isStateCalledOnce = true;
		#endregion
		//
		isInProgressNav = true;
		owner.Mover.MoveTo(owner.TrDespawnPoint.position, onArrived: () =>
		{
			isInProgressNav = false;
			TransitionTo(CustomerState.done);
			GameObject.Destroy(owner.gameObject, t: 0.2f);
		});
	}
	#endregion

	// ── Transition ────────────────────────────────────────────
	#region TransitionTo
	/// Changes state and resets the _init flag.
	private void TransitionTo(CustomerState next)
	{
		if (currState == next)
			return;
		Debug.Log($"[FSM] {gameObject.name}: {currState} → {next}");
		isStateCalledOnce = false;
		currState = next;
	} 
	#endregion
}
