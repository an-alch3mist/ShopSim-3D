using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using SPACE_UTIL;

// ============================================================
//  Ordered queue of Transform slots.
//  Index 0 = front (close to counter side).
//  Self-registers with POIRegistry on enable.
// ============================================================
public class QueuePOI : MonoBehaviour, IPOI
{
	#region public Interface API

	public string POIId
	{
		get
		{
			return this._poiId;
		}
	}
	public Transform BookSlot(CustomerAgent agent)
	{
		Debug.Log(C.method(this, "cyan"));
		// book the lowest free index
		foreach (Transform tr in _TR_QUEUE_SLOTS)
			if (DOC_OCCUPANTS[tr] == null)
			{
				DOC_OCCUPANTS[tr] = agent;
				return tr;
			}
		return null;

	}
	public bool HasAnyAvailableSlot()
	{
		//int sum = 0;
		//this.DOC_OCCUPANTS.forEach(kvp =>
		//{
		//	if (kvp.Value != null)
		//		sum += 1;
		//});
		//return sum < _TR_QUEUE_SLOTS.Count;

		return DOC_OCCUPANTS.findIndex(kvp => kvp.Value == null) != -1;
	}
	public void ReleaseSlot(CustomerAgent agent)
	{
		Debug.Log(C.method(this, "cyan"));
		LOG.AddLog(this.DOC_OCCUPANTS.ToTable(name: "DOC<>", toString: true));
		foreach (var kvp in DOC_OCCUPANTS)
			if (kvp.Value == agent)
			{
				DOC_OCCUPANTS[kvp.Key] = null;
				return;
			}
	} 
	#endregion

	#region private API
	[Header("Id")] [SerializeField] private string _poiId = "Id";
	[Header("Slots")] [SerializeField] List<Transform> _TR_QUEUE_SLOTS;

	Dictionary<Transform, CustomerAgent> DOC_OCCUPANTS;

	private void Awake()
	{
		Debug.Log(C.method(this));
		this.initDOC();
	}
	void initDOC()
	{
		this.DOC_OCCUPANTS = new Dictionary<Transform, CustomerAgent>();
		foreach (Transform tr in this._TR_QUEUE_SLOTS)
			DOC_OCCUPANTS[tr] = null;
	}

	// self register with POIRegistry on enable
	private void OnEnable()
	{
		Debug.Log(C.method(this));
		POIRegistry.Ins.RegisterQ(this);
	}
	// self un register with POIRegistry on disable
	private void OnDisable()
	{
		Debug.Log(C.method(this, "orange"));
		POIRegistry.Ins.UnRegisterQ(this);
	}

	private void OnDrawGizmosSelected()
	{
		/*
		for (int i = 0; i < queueSlots.Count; i++)
		{
			if (queueSlots[i] == null) continue;
			Gizmos.color = _occupants.ContainsKey(i) ? Color.red : Color.green;
			Gizmos.DrawWireCube(queueSlots[i].position, Vector3.one * 0.4f);
			if (i > 0 && queueSlots[i - 1] != null)
				Gizmos.DrawLine(queueSlots[i - 1].position, queueSlots[i].position);
			UnityEditor.Handles.Label(
				queueSlots[i].position + Vector3.up * 0.55f,
				$"Q{i}{(_occupants.ContainsKey(i) ? " ✓" : "")}");
		}
		*/
		if (DOC_OCCUPANTS == null)
			return;

		_TR_QUEUE_SLOTS.forEach((tr, index) =>
		{
			if (tr == null)
				return;
			bool isOccupied = (DOC_OCCUPANTS[tr] == null);
			Gizmos.color = (isOccupied) ? Color.red : Color.limeGreen;
			Gizmos.DrawWireCube(tr.position, Vector3.one * 0.3f);
			UnityEditor.Handles.Label(tr.position + Vector3.up * 0.5f, $"Q[{index}] {(isOccupied ? "": " ✓")}");
	;	});
	}
	#endregion
}
