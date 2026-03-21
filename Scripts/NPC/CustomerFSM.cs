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
	1. joinQueue
		|
	2. waitInQueue
		|
	3. leaveStore
		|
	4. walkOut

*/

public enum CustomerState
{
	walkIn,
	joinQueue,
	waitInQueue,
	leaveStore,
	walkOut,
	done,
}

public class CustomerFSM : MonoBehaviour
{
	bool hasDoneInit = false;
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

			 if (this.currState == CustomerState.walkIn) ExecStateWalkIn();
		else if (this.currState == CustomerState.joinQueue) ExecStateJoinQueue();
		else if (this.currState == CustomerState.waitInQueue) ExecStateWaitInQueue();
		else if (this.currState == CustomerState.leaveStore) ExecStateLeaveStore();
		else if (this.currState == CustomerState.walkOut) ExecStateWalkOut();
	}

	void ExecStateWalkIn()
	{
		if (this.hasDoneInit == true)
			return;
		hasDoneInit = true;
		//
		Debug.Log($"require entrancePoint assign {Time.time} {owner.gameObject.name}".colorTag("cyan"));
		Debug.Log($"entrancePoint assigned exist {Time.time} {owner.gameObject.name}".colorTag("cyan"));
		isInProgressNav = true;
		owner.Mover.MoveTo(owner.entrancePoint.position, onArrived: () =>
		{
			isInProgressNav = false;
			GameEvents.RaiseCustomerEntered(agent: owner);
			TransitionTo(CustomerState.joinQueue);
		});
	}
	void ExecStateJoinQueue()
	{
		if (this.hasDoneInit == true)
			return;
		hasDoneInit = true;
		//
		QueuePOI poi = POIRegistry.Ins.GetFirstAvailableQueueWithSlot();
		// retry
		if(poi == null) // queue is full, retry next tick
		{
			hasDoneInit = false;
			return;
		}

		var slot = poi.BookSlot(owner);
		/*
		// retry
		if(slot == null) // overlap book from different agent
		{
			hasDoneInit = false;
			return;
		}
		*/
		owner.CurrentQueue = poi;
		owner.TrCurrentQueueSlot = slot;
		GameEvents.RaiseCustomerJoinedQ(owner);

		waitInQueueDuration = C.Random(owner.Profile.minQWaitSec, owner.Profile.maxQWaitSec);
		owner.Mover.MoveTo(slot.position, onArrived: () =>
		{
			isInProgressNav = false;
			waitInQueueTimer = 0f;
			TransitionTo(CustomerState.waitInQueue);
		});
	}
	void ExecStateWaitInQueue()
	{
		waitInQueueTimer += Time.deltaTime;
		if(waitInQueueTimer >= waitInQueueDuration)
		{
			owner.CurrentQueue.ReleaseSlot(owner);
			owner.CurrentQueue = null;
			owner.TrCurrentQueueSlot = null;
		}
	}
	void ExecStateLeaveStore()
	{
		if (this.hasDoneInit == true)
			return;
		hasDoneInit = true;
		//
	}
	void ExecStateWalkOut()
	{
		if (this.hasDoneInit == true)
			return;
		hasDoneInit = true;
		//
	}

	// ── Transition ────────────────────────────────────────────
	#region TransitionTo

	/// Changes state and resets the _init flag.
	private void TransitionTo(CustomerState next)
	{
		if (currState == next) return;
		Debug.Log($"[FSM] {gameObject.name}: {currState} → {next}");
		currState = next;
		hasDoneInit = false;
	} 
	#endregion
}
