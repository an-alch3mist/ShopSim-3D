using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using SPACE_UTIL;

public class CustomerSpawner : MonoBehaviour
{

	[Header("Prefab")]
	public GameObject pfCustomer;
	[Header("Profiles")]
	[SerializeField] List<SO_CustomerProfileData> _PROFILE;

	[Header("Waypoints")]
	[Tooltip("Far outside — NPC appears here.")]
	public Transform _Tr_spawnPoint;
	[Tooltip("Just inside the store, ON NavMesh.")]
	public Transform _Tr_entrancePoint;
	[Tooltip("Door threshold — NPC walks here on leave.")]
	public Transform _Tr_exitPoint;
	[Tooltip("Far outside — NPC destroyed here silently.")]
	public Transform _Tr_despawnPoint;

	private void Start()
	{
		Debug.Log(C.method(this));
		this.StopAllCoroutines();
		this.StartCoroutine(RoutineSpawn());
	}

	IEnumerator RoutineSpawn()
	{
		while(C.Safe(100, "spawnLoop"))
		{
			this.SpawnOne();
			yield return new WaitForSeconds(0.5f);
		}
		yield return null;
	}

	int spawnedCount = 0;
	void SpawnOne()
	{
		Vector3 pos = _Tr_spawnPoint.position;
		Quaternion rot = _Tr_spawnPoint.rotation;

		GameObject go = GameObject.Instantiate(pfCustomer, pos, rot);
		go.name = $"customer-{spawnedCount:D3}";
		spawnedCount += 1;

		CustomerAgent agent = go.GetComponent<CustomerAgent>();

		SO_CustomerProfileData pickProfile = this._PROFILE.getRandom();

		agent.entrancePoint = _Tr_entrancePoint;
		agent.exitPoint = _Tr_exitPoint;
		agent.despawnPoint = _Tr_despawnPoint;
		agent.ApplyProfileData(pickProfile);

		Debug.Log($"[CustomerSpawner] Spawned {go.name} " +
				  $"[{agent.profileData?.id ?? "no profile"}] " +
				  $"wait: {agent.profileData?.minQWaitSec}–{agent.profileData?.maxQWaitSec}s");

	}
}
