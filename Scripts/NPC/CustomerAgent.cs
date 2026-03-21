using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

public class CustomerAgent : MonoBehaviour
{
	// ── Assigned by spawner ───────────────────────────────────
	[HideInInspector] public SO_CustomerProfileData Profile;

	[HideInInspector] public Transform entrancePoint;
	[HideInInspector] public Transform exitPoint;
	[HideInInspector] public Transform despawnPoint;
	public void ApplyProfileData(SO_CustomerProfileData profileData)
	{
		this.Mover.ApplyProfileData(profileData);
		this.Profile = profileData;
	}

	// ── Written/read by FSM ───────────────────────────────────
	[HideInInspector] public QueuePOI CurrentQueue;
	[HideInInspector] public Transform TrCurrentQueueSlot;

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
		UnityEditor.Handles.color = new Color(1f, 0.5f, 0f);
		UnityEditor.Handles.DrawDottedLine(
			this.transform.position + Vector3.up,
			TrCurrentQueueSlot.position + Vector3.up, 3f);
	}

	public override string ToString()
	{
		// return base.ToString();
		return $"{Profile.id}: {CurrentQueue}";
	}
}
