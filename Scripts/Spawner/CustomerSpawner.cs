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
	[SerializeField] int _spawnCustomerCount = 1;
	IEnumerator RoutineSpawn()
	{
		while(C.Safe(this._spawnCustomerCount, "spawnLoop"))
		{
			this.SpawnOne();
			yield return new WaitForSeconds(2f);
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

		agent.customerId = go.name;
		agent.TrEntrancePoint = _Tr_entrancePoint;
		agent.TrExitPoint = _Tr_exitPoint;
		agent.TrDespawnPoint = _Tr_despawnPoint;
		agent.shoppingList = this.BuildShoppingList();
		// LOG.AddLog the shoppingList.
		LOG.AddLog(agent.shoppingList.ToTable(name: $"LIST<> ITEM shopping list on spawn customer, {agent.customerId}"));
		agent.ApplyProfileData(pickProfile);

		Debug.Log($"[CustomerSpawner] Spawned {go.name} " +
				  $"[{agent.profileData?.id ?? "no profile"}] " +
				  $"wait: {agent.profileData?.minQWaitSec}–{agent.profileData?.maxQWaitSec}s");
	}
	[Header("shopping list")]
	[SerializeField] int _maxItemTypes = 3;  // distinct item types per customer
	[SerializeField] int _maxQtyPerItemType = 3;  // max units of each type wanted
	List<SO_ItemData> BuildShoppingList()
	{
		List<SO_ItemData> available = POIRegistry.Ins.GetAllStockedItemsOnShelves();
		if (available.Count == 0) return new List<SO_ItemData>();

		// Fisher-Yates shuffle the available pool
		for (int i = available.Count - 1; i > 0; i -= 1)
		{
			int j = C.Random(0, i); // C.Random is inclusive on both ends
			(available[i], available[j]) = (available[j], available[i]);
		}

		// pick how many distinct item types this customer wants
		int typeCount = C.Random(1, _maxItemTypes.clamp(1, available.Count));

		List<SO_ItemData> list = new List<SO_ItemData>();
		for (int i = 0; i < typeCount; i++)
		{
			SO_ItemData item = available[i];

			// pick quantity of this type (1 to _maxQtyPerItemType)
			int qty = C.Random(1, _maxQtyPerItemType);

			for (int q = 0; q < qty; q++)
				list.Add(item);
			// result: [Milk, Milk, Apple, Apple, Apple, Bread]
			// FSM pops one entry per purchase — naturally handles multi-quantity
		}

		return list;
	}
}
