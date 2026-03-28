using System;
using System.Linq;
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

/*
## Phase-1 state flow:

walkIn
└─► selectItem ◄───────────────────────────────────────────┐
		│                                                  │
		├─ item found, shelf free ──► navigateToShelf      │
		│                                └─► takeItem ─────┘
		│
		├─ item has stock, shelf BUSY
		│     └─ _deferCount < list.Count → rotate list, retry next tick
		│     └─ _deferCount >= list.Count → all items blocked, wait + reset
		│
		├─ item out of stock → RemoveAll that item from list, retry
		│
		└─ list empty ──► joinQueue
							  └─► waitInQueue
									  └─► leaveStore
											  └─► walkOut → Destroy
*/
namespace SPACE_ShopSim3D_prev
{

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

	public class CustomerFSM_prev : MonoBehaviour
	{
		bool isStateCalledOnce = false;
		bool isInProgressNav = false;
		float waitInQueueTimer = 0f;
		float waitInQueueDuration = 10f;
		int _deferCount = 0;   // how many items deferred in current selectItem pass

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
			else if (phaseDev == 1)
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
				if (phaseDev == 0)
					TransitionTo(CustomerState.bookAndJoinQueue);
				else
					TransitionTo(CustomerState.selectItem);
			});
		}
		#region Phase-1 ExecState 
		// runs every tick until list resolved
		void ExecStateSelectItem()
		{
			// shopping list completed
			if (owner.shoppingList.Count == 0)
			{
				_deferCount = 0;
				TransitionTo(CustomerState.bookAndJoinQueue);
				return;
			}

			// LOG.AddLog(owner.shoppingList.ToTable(name: $"LIST<> ITEM shopping list on enter select item state, {owner.customerId}"));
			SO_ItemData desiredItemData = owner.shoppingList[0];
			// ── case 1: item genuinely out of stock ──────────────
			bool stockExists = POIRegistry.Ins.AnyShelfHasStockOf(desiredItemData);
			if (!stockExists)
			{
				Debug.Log($"[FSM] {owner.customerId}: {desiredItemData.id} out of stock — removing all copies".colorTag("orange"));
				// owner.shoppingList.RemoveAll(i => i == desired);
				owner.shoppingList = owner.shoppingList.refine(item => (item != desiredItemData)).ToList();
				_deferCount = 0;
				return; // re-evaluate next tick
			}

			// ── case 2: stock exists, find a shelf with free slot ─
			ShelfPOI poi = POIRegistry.Ins.GetFirstShelfWithItemAndAvaiableNPCSlot(itemData: desiredItemData);
			if (poi == null)
			{
				// stock exists but every matching shelf slot is busy
				_deferCount += 1;

				if (_deferCount >= owner.shoppingList.Count)
				{
					// tried every item in list — all shelves blocked, wait a tick then reset
					Debug.Log($"[FSM] {owner.customerId}: all {owner.shoppingList.Count} items blocked — waiting".colorTag("yellow"));
					_deferCount = 0;
					return;
				}

				// rotate: push current item to end of list, try next one next tick
				SO_ItemData deferred = owner.shoppingList[0];
				owner.shoppingList.RemoveAt(0);
				owner.shoppingList.Add(deferred);
				Debug.Log($"[FSM] {owner.customerId}: {desiredItemData.id} shelf busy — deferred (count: {_deferCount})".colorTag("yellow"));
				return;
			}

			// ── case 3: found shelf with stock and free slot ──────
			_deferCount = 0;
			owner.currentTargetItem = desiredItemData;
			owner.currentShelfPOI = poi;
			TransitionTo(CustomerState.bookAndNavigateToShelf);
		}
		void ExecStateBookAndNavigateToShelf()
		{
			#region call this state just once
			if (isStateCalledOnce == true)
				return;
			isStateCalledOnce = true;
			#endregion

			// BookSlot can return null if another NPC grabbed the slot between
			// selectItem and here (race condition between two agents)
			Transform trSlot = owner.currentShelfPOI.BookSlot(owner);
			if (trSlot == null)
			{
				// slot gone — go back to selectItem to find another shelf
				Debug.Log($"[FSM] {owner.customerId}: slot stolen — returning to selectItem".colorTag("orange"));
				isStateCalledOnce = false;
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
			// LOG.AddLog(owner.shoppingList.ToTable(name: $"LIST<> ITEM shopping list after item taken, {owner.customerId}"));

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
}
