using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using SPACE_UTIL;
using SPACE_DrawSystem;

public class DEBUG_Check : MonoBehaviour
{
	private void Start()
	{
		Debug.Log(C.method(this));
		this.StopAllCoroutines();
		this.StartCoroutine(this.STIMULATE());
	}

	IEnumerator STIMULATE()
	{
		// yield return this.checkUTIL();
		// yield return this.checkForIncr();
		// yield return this.checkBreak();
		// yield return this.checkCustomerAgent();
		// yield return this.checkCustomerAgent_1();
		// yield return this.checkArray();

		yield return null;
	}
	
	[Header("checkUTIL")][SerializeField] List<int> LIST;
	IEnumerator checkUTIL()
	{
		while(true)
		{
			if (INPUT.M.InstantDown(0))
				Debug.Log(LIST.getRandom());
			yield return null;
		}
		yield return null;
	}

	[Header("checkForIncr")]
	[SerializeField] float _speed = 0f;
	IEnumerator checkForIncr()
	{
		for (float t = 0f; t < 100f; t += this._speed * Time.deltaTime)
		{
			this.transform.position = new Vector3(t, 0f, 0f);
			yield return null;
		}

		yield return null;
	}
	[Header("checkYieldBreak")]
	[SerializeField] bool _runYieldBreak = true;
	IEnumerator checkBreak()
	{
		if (this._runYieldBreak == true)
		{
			for (int i0 = 0; i0 < 10; i0 += 1)
			{
				Debug.Log(C.method(this, "cyan", adMssg: $"i0: {i0}"));
				if (i0 > 5)
					yield break;
				yield return null;
			}
			Debug.Log("the loop complete".colorTag("lime"));
		}
		else
		{
			for (int i0 = 0; i0 < 10; i0 += 1)
			{
				Debug.Log(C.method(this, "cyan", adMssg: $"i0: {i0}"));
				if (i0 > 5)
					break;
				yield return null;
			}
			Debug.Log("the loop complete".colorTag("lime"));
		}
	}

	[Header("checkCustomerAgent")]
	[SerializeField] NavMeshMover _navMeshMover;
	[SerializeField] Transform _TrDestination;
	[SerializeField] SO_CustomerProfileData _so_profileData;
	[SerializeField] Button _btn_moveToDestination, _btn_stopAgent;

	IEnumerator checkCustomerAgent()
	{
		Debug.Log(C.method(this, "lime"));
		this._navMeshMover.ApplyProfileData(this._so_profileData);
		this._btn_moveToDestination.onClick.AddListener(() =>
		{
			this._btn_moveToDestination.logClicked();
			this._navMeshMover.MoveTo(this._TrDestination.position, () =>
			{
				Debug.Log("arrived at destination".colorTag("lime"));
			});
		});
		//
		this._btn_stopAgent.onClick.AddListener(() =>
		{
			this._btn_stopAgent.logClicked();
			this._navMeshMover.Stop();
		});

		yield return null;
	}

	[Header("checkCustomerAgent_1")]
	[Tooltip("Assign multiple NavMeshMover agents — all will target the same point.")]
	[SerializeField] List<NavMeshMover> _agents;
	[SerializeField] Button _btn_moveAll, _btn_stopAll, _btn_scatter;

	// Tracks how many agents have reported arrival this round
	int _arrivedCount = 0;
	IEnumerator checkCustomerAgent_1()
	{
		Debug.Log(C.method(this, "lime"));

		// Apply the same profile to every agent
		foreach (var agent in this._agents)
			agent.ApplyProfileData(this._so_profileData);

		// ── Move all → same destination ──────────────────────────
		// This is the RVO proof: all agents share one target point.
		// Expected: they steer around each other and settle nearby,
		// nobody gets pushed or teleported.
		this._btn_moveAll.onClick.AddListener(() =>
		{
			this._btn_moveAll.logClicked();
			this._arrivedCount = 0;

			for (int i = 0; i < this._agents.Count; i++)
			{
				int idx = i; // capture for lambda
				this._agents[idx].MoveTo(this._TrDestination.position, () =>
				{
					this._arrivedCount++;
					Debug.Log(
						$"agent [{idx}] arrived  ({this._arrivedCount}/{this._agents.Count} total)"
						.colorTag("lime"));

					if (this._arrivedCount >= this._agents.Count)
						Debug.Log("all agents arrived".colorTag("cyan"));
				});
			}
		});


		// ── Stop all ─────────────────────────────────────────────
		this._btn_stopAll.onClick.AddListener(() =>
		{
			this._btn_stopAll.logClicked();
			foreach (var agent in this._agents)
				agent.Stop();
		});

		// ── Scatter — sends each agent to a unique offset position ─
		// Use this first so agents start spread out, then hit MoveAll
		// to watch them converge without colliding.
		this._btn_scatter.onClick.AddListener(() =>
		{
			this._btn_scatter.logClicked();
			for (int i = 0; i < this._agents.Count; i++)
			{
				// Fan agents out in a circle around the destination
				float angle = i * (360f / this._agents.Count) * Mathf.Deg2Rad;
				float radius = 4f;
				Vector3 offset = new Vector3(
					Mathf.Cos(angle) * radius,
					0f,
					Mathf.Sin(angle) * radius);

				int idx = i;
				this._agents[idx].MoveTo(
					this._TrDestination.position + offset, () =>
						Debug.Log($"agent [{idx}] scattered".colorTag("yellow")));
			}
		});

		yield return null;
	}

	IEnumerator checkArray()
	{
		Board<GameObject> B_OBJ = new Board<GameObject>((10, 10), null);
		LOG.AddLog(B_OBJ.ToString(detailed: true, elem => elem)); // ->  null error in console
		LOG.AddLog(B_OBJ.ToString(detailed: true, elem => { return (elem == null) ? "None" : elem.name; })); // it works
																											 //

		GameObject[] OBJ = new GameObject[10];
		LOG.AddLog(OBJ.ToTable(name: "LIST<> OBJ", toString: true), "txt"); // -> null error in console

		OBJ.forEach((obj, index) =>
		{
			Line.create(index).setA(Vector3.zero).setB(Vector3.up + Vector3.right * index);
		});

		yield return null;
	}
}
