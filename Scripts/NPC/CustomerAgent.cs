using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

public class CustomerAgent : MonoBehaviour
{
	// ── Assigned by spawner ───────────────────────────────────
	[HideInInspector] public SO_CustomerProfileData profileData;
	[HideInInspector] public string customerId;

	[HideInInspector] public Transform TrEntrancePoint;
	[HideInInspector] public Transform TrExitPoint;
	[HideInInspector] public Transform TrDespawnPoint;
	public void ApplyProfileData(SO_CustomerProfileData profileData)
	{
		this.Mover.ApplyProfileData(profileData);
		this.profileData = profileData;
	}

	// ── Shopping data (assigned by spawner, mutated by FSM) ─
	[HideInInspector] public List<SO_ItemData> shoppingList;

	// ── FSM runtime state (written/read by CustomerFSM only) ─
	[HideInInspector] public QueuePOI currentQueuePOI;
	[HideInInspector] public Transform TrCurrentQueueSlot;

	[HideInInspector] public ShelfPOI currentShelfPOI;
	[HideInInspector] public SO_ItemData currentTargetItem;

	// ── Component refs ────────────────────────────────────────
	[SerializeField] public NavMeshMover Mover;
	[SerializeField] CustomerFSM FSM ;

	private void Awake()
	{
		Debug.Log(C.method(this));
		this.FSM.Init(this);
	}
	private void Update()
	{
		this.FSM.Tick();
	}

	private void OnDrawGizmosSelected()
	{
		if (TrCurrentQueueSlot == null) return;
#if UNITY_EDITOR
		UnityEditor.Handles.color = new Color(1f, 0.5f, 0f);
		UnityEditor.Handles.DrawDottedLine(
			this.transform.position + Vector3.up,
			TrCurrentQueueSlot.position + Vector3.up, 3f);
#endif
	}
}
