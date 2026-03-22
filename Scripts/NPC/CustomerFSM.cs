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
			 if (this.currState == CustomerState.walkIn) ExecStateWalkIn();
		else if (this.currState == CustomerState.joinQueue) ExecStateJoinQueue();
		else if (this.currState == CustomerState.waitInQueue) ExecStateWaitInQueue();
		else if (this.currState == CustomerState.leaveStore) ExecStateLeaveStore();
		else if (this.currState == CustomerState.walkOut) ExecStateWalkOut();
		else if (this.currState == CustomerState.done)
		{
			// done
			Debug.Log($"{owner.profileData.id} life cycle complete".colorTag("lime"));
		}
	}

	void ExecStateWalkIn()
	{
		#region call this state just once
		if (this.isStateCalledOnce == true)
			return;
		isStateCalledOnce = true; 
		#endregion
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
		
		owner.CurrentQueue = poi;
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
		if(waitInQueueTimer >= waitInQueueDuration)
		{
			if (owner.CurrentQueue == null)
				Debug.Log(owner.gameObject.name + " null queue".colorTag("red"));

			owner.CurrentQueue.ReleaseSlot(owner);
			owner.CurrentQueue = null;
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
		owner.Mover.MoveTo(owner.exitPoint.position, onArrived: () =>
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
		isStateCalledOnce = false;
		currState = next;
	} 
	#endregion
}
