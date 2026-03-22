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
	joinQueue,
	waitInQueue,
	leaveStore,
	walkOut,
	done,
	// << phase-0

	// +phase-1 >>
	selectItem,
	navigateToShelf,
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
	public void Tick()
	{
		if (this.isInProgressNav == true)
			return;

		int phaseDev = 0;
		if (phaseDev == 0)
		{
				 if (this.currState == CustomerState.walkIn) ExecStateWalkIn();
			else if (this.currState == CustomerState.joinQueue) ExecStateJoinQueue();
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
			else if (this.currState == CustomerState.navigateToShelf) ExecStateNavigateToShelf();
			else if (this.currState == CustomerState.takeItem) ExecStateTakeItem();
			else if (this.currState == CustomerState.joinQueue) ExecStateJoinQueue();
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
			TransitionTo(CustomerState.joinQueue);
		});
	}
	#region Phase-1 ExecState 
	// runs every tick until list resolved
	void ExecStateSelectItem()
	{
		if(owner.shoppingList.Count == 0)
		{
			TransitionTo(CustomerState.joinQueue);
			return;
		}

		SO_ItemData desiredItemData = owner.shoppingList[0];
		ShelfPOI poi = POIRegistry.Ins.GetFirstShelfWithItemAndAvaiableNPCSlot(itemData: desiredItemData);

		if(poi == null)
		{
			// either out of stock, or that itemData shelfTier poi is occupied by other NPC
			owner.shoppingList.RemoveAt(0);
			return;
		}
		owner.currentTargetItem = desiredItemData;
		owner.currentShelfPOI = poi;
		TransitionTo(CustomerState.navigateToShelf);
	}
	void ExecStateNavigateToShelf()
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
		// transition made inside routine
		StartCoroutine(RoutineTakeItem());
	}
	IEnumerator RoutineTakeItem()
	{
		yield return null;
		yield return new WaitForSeconds(1f); // grab pause

		owner.currentShelfPOI.TryTakeItem(owner.currentTargetItem);
		owner.currentShelfPOI.ReleaseSlot(owner);
		// item taken complete

		owner.shoppingList.RemoveAt(0);

		owner.currentTargetItem = null;
		owner.currentShelfPOI = null;
		TransitionTo(CustomerState.selectItem);
	}
	#endregion
	void ExecStateJoinQueue()
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
