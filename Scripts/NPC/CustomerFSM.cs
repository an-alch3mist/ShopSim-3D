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
		// Debug.Log($"require entrancePoint assign {Time.time} {owner.gameObject.name}".colorTag("cyan"));
		// Debug.Log($"entrancePoint assigned exist {Time.time} {owner.gameObject.name}".colorTag("cyan"));
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
		/*
		QueuePOI poi = POIRegistry.Ins.GetFirstAvailableQueueWithSlot();
		// retry
		if(poi == null) // queue is full, retry next tick
		{
			hasDoneInit = false;
			return;
		}
		
		var slot = poi.BookSlot(owner);
		
		
		// retry
		if(slot == null) // overlap book from different agent
		{
			hasDoneInit = false;
			return;
		}
		*/
		QueuePOI poi = POIRegistry.Ins.GetFirstAvailableQueueWithSlots();
		// retry
		if (poi == null) // queue is full, retry next tick
		{
			hasDoneInit = false;
			return;
		}
		var slot = poi.BookSlot(owner);
		
		owner.CurrentQueue = poi;
		owner.TrCurrentQueueSlot = slot;
		isInProgressNav = true;

		owner.Mover.MoveTo(slot.position, onArrived: () =>
		{
			isInProgressNav = false;
			GameEvents.RaiseCustomerJoinedQ(owner);
			waitInQueueTimer = 0f;
			waitInQueueDuration = C.Random(owner.Profile.minQWaitSec, owner.Profile.maxQWaitSec);
			TransitionTo(CustomerState.waitInQueue);
		});
	}
	void ExecStateWaitInQueue()
	{
		waitInQueueTimer += Time.deltaTime;
		if(waitInQueueTimer >= waitInQueueDuration)
		{
			if (owner.CurrentQueue == null)
				Debug.Log(owner.gameObject.name + " null queue".colorTag("red"));

			// LOG.H($"{owner.gameObject.name}");
			// LOG.AddLog(owner.CurrentQueue.DOC_OCCUPANTS.ToTable(name: "DOC<>", toString: true));
			//
			owner.CurrentQueue.ReleaseSlot(owner);
			//
			// LOG.AddLog(owner.CurrentQueue.DOC_OCCUPANTS.ToTable(name: "DOC<>", toString: true));
			// LOG.HEnd($"{owner.gameObject.name}");

			owner.CurrentQueue = null;
			owner.TrCurrentQueueSlot = null;

			TransitionTo(CustomerState.leaveStore);
		}
	}
	void ExecStateLeaveStore()
	{
		if (this.hasDoneInit == true)
			return;
		hasDoneInit = true;
		//
		isInProgressNav = true;
		owner.Mover.MoveTo(owner.exitPoint.position, onArrived: () =>
		{
			isInProgressNav = false;
			GameEvents.RaiseCustomerLeft(agent: owner);
			TransitionTo(CustomerState.walkOut);
		});
	}
	void ExecStateWalkOut()
	{
		if (this.hasDoneInit == true)
			return;
		hasDoneInit = true;
		//
		isInProgressNav = true;
		owner.Mover.MoveTo(owner.despawnPoint.position, onArrived: () =>
		{
			isInProgressNav = false;
			GameObject.Destroy(owner.gameObject);
			TransitionTo(CustomerState.done);
		});
	}

	// ── Transition ────────────────────────────────────────────
	#region TransitionTo

	/// Changes state and resets the _init flag.
	private void TransitionTo(CustomerState next)
	{
		if (currState == next)
			return;
		Debug.Log($"[FSM] {gameObject.name}: {currState} → {next}");
		hasDoneInit = false;
		currState = next;
	} 
	#endregion
}
