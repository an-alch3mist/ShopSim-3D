using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using SPACE_UTIL;

// require navMeshAgent
public class NavMeshMover : MonoBehaviour
{
	public float _speed = 3f;
	public float _stoppingDist = 0.3f;
	public float _arrivalTime = 20f;
	[SerializeField] NavMeshAgent _agent = null;

	#region Unity Life Cycle
	private void Awake()
	{
		Debug.Log(C.method(this));
		_agent.speed = this._speed;
		_agent.stoppingDistance = this._stoppingDist;
		_agent.angularSpeed = 360f;
		_agent.acceleration = 14f;
	} 
	#endregion

	public void ApplyProfileData(SO_CustomerProfileData profileData)
	{
		this._agent.speed = profileData.walkSpeed;
		this._agent.avoidancePriority = profileData.avoidancePriority;
	}

	// send agent toward destination
	#region ref
	Coroutine refTrackingRoutine; 
	#endregion
	public void MoveTo(Vector3 destination, Action onArrived = null)
	{
		this.CancelTracking();
		this._agent.isStopped = false;
		this._agent.SetDestination(destination);
		if (onArrived != null)
			this.refTrackingRoutine = this.StartCoroutine(StartTrackingRoutine(onArrived));
	}
	// stop movment and clear the current path
	public void Stop()
	{
		this.CancelTracking();
		if(this._agent.isOnNavMesh)
		{
			this._agent.isStopped = true;
			this._agent.ResetPath();
		}
	}

	#region private API
	IEnumerator StartTrackingRoutine(Action onArrived)
	{
		for(float elapsed = 0f; elapsed < this._arrivalTime; elapsed += Time.deltaTime)
		{
			bool hasArrived = (_agent.pathPending == false) && (_agent.remainingDistance <= _agent.stoppingDistance);
			if (hasArrived)
			{
				this.Stop();
				onArrived?.Invoke();
				yield break; // return
			}
			yield return null;
		}

		this.Stop();
		onArrived?.Invoke();
	}

	void CancelTracking()
	{
		if (this.refTrackingRoutine != null)
		{
			this.StopCoroutine(this.refTrackingRoutine);
			this.refTrackingRoutine = null;
		}
	}
	#endregion
}
