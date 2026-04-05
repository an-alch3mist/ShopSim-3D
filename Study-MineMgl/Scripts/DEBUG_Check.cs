using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SPACE_UTIL;

namespace SPACE_MineMGL
{
	public class DEBUG_Check : MonoBehaviour
	{
		private void Start()
		{
			Debug.Log(C.method(this));
			this.StopAllCoroutines();
			this.StartCoroutine(STIMULATE());
			// this.StartCoroutine(STIMULATE_1());
		}

		IEnumerator STIMULATE()
		{
			// yield return this.routineCheckSomthng();
			// yield return routineCheckAnyMenuOpen();
			// yield return this.routineCheckVerticalLayoutUI();
	
			yield return null;
		}
		//
		IEnumerator routineCheckSomthng()
		{
			Debug.Log($"somthng".colorTag("lime"));
			yield return null;
		}
		IEnumerator routineCheckAnyMenuOpen()
		{
			Debug.Log(C.method(this, "lime"));
			bool isAnyMenuOpened = true;
			GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: isAnyMenuOpened);
			while (true)
			{
				if (INPUT.K.InstantDown(KeyCode.Tab))
				{
					// Debug.Log($"menu open/close toggled {isAnyMenuOpened}".colorTag("lime"));
					isAnyMenuOpened = !isAnyMenuOpened;
					GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: isAnyMenuOpened);
				}
				//
				yield return null;
			}
			yield return null;
		}
		[SerializeField] GameObject pfItem;
		[SerializeField] GameObject holder;
		IEnumerator routineCheckVerticalLayoutUI()
		{
			while (true)
			{

				if (INPUT.M.InstantDown(0))
				{
					for (int i0 = 0; i0 < 2; i0 += 1)
						GameObject.Instantiate(this.pfItem, this.holder.transform);
					Debug.Log($"added ui elems, now count: {this.holder.transform.childCount}".colorTag("lime"));
				}
				yield return null;
			}
			yield return null;
		}


		IEnumerator STIMULATE_1()
		{
			yield return this.routineCheck_ListOfList();
			yield return this.routineCheck_Dictionary();
			yield return this.routineCheck_DictionaryOfLists();
			yield return this.routineCheck_NestedEverything();
			yield return this.routineCheck_JObjectDynamic();
			yield return null;
		}
		// ─────────────────────────────────────────────────────────────────────
		//  1. List<List<T>>
		// ─────────────────────────────────────────────────────────────────────
		IEnumerator routineCheck_ListOfList()
		{
			H("List<List<int>>  — a 3x3 grid of ints");

			List<List<int>> grid = new List<List<int>>
			{
				new List<int> { 1,  2,  3  },
				new List<int> { 4,  5,  6  },
				new List<int> { 7,  8,  9  }
			};

			string json = JsonConvert.SerializeObject(grid, Formatting.Indented);
			Log("Serialized", json);

			List<List<int>> restored = JsonConvert.DeserializeObject<List<List<int>>>(json);
			foreach (var row in restored)
				Debug.Log("  row → " + string.Join(", ", row));

			// ── List<List<string>> ────────────────────────────────────────────
			H("List<List<string>>  — dialogue trees");

			List<List<string>> dialogue = new List<List<string>>
			{
				new List<string> { "Hello traveller!", "Need something?" },
				new List<string> { "Buy", "Sell", "Leave" },
				new List<string> { "Come back soon!" }
			};

			string dJson = JsonConvert.SerializeObject(dialogue, Formatting.Indented);
			Log("Serialized", dJson);

			List<List<string>> dRestored = JsonConvert.DeserializeObject<List<List<string>>>(dJson);
			foreach (var branch in dRestored)
				Debug.Log("  branch → " + string.Join(" | ", branch));

			yield return null;
		}
		// ─────────────────────────────────────────────────────────────────────
		//  2. Dictionary<K, V>
		// ─────────────────────────────────────────────────────────────────────
		IEnumerator routineCheck_Dictionary()
		{
			H("Dictionary<string, int>  — item prices");

			Dictionary<string, int> prices = new Dictionary<string, int>
			{
				{ "Iron Sword",     150 },
				{ "Health Potion",  30  },
				{ "Magic Staff",    400 },
				{ "Shield",         200 }
			};

			string json = JsonConvert.SerializeObject(prices, Formatting.Indented);
			Log("Serialized", json);

			Dictionary<string, int> restored = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
			foreach (var kv in restored)
				Debug.Log($"  {kv.Key} → {kv.Value} gold");

			// ── Dictionary<string, bool> ─────────────────────────────────────
			H("Dictionary<string, bool>  — feature flags");

			Dictionary<string, bool> flags = new Dictionary<string, bool>
			{
				{ "DarkMode",       true  },
				{ "ShowMinimap",    true  },
				{ "HardcoreMode",   false },
				{ "DevCheats",      false }
			};

			string fJson = JsonConvert.SerializeObject(flags, Formatting.Indented);
			Log("Serialized", fJson);

			Dictionary<string, bool> fRestored = JsonConvert.DeserializeObject<Dictionary<string, bool>>(fJson);
			foreach (var kv in fRestored)
				Debug.Log($"  {kv.Key} → {kv.Value}");

			// ── Dictionary<int, string> ──────────────────────────────────────
			H("Dictionary<int, string>  — level names");

			Dictionary<int, string> levels = new Dictionary<int, string>
			{
				{ 1, "Whispering Woods" },
				{ 2, "Goblin Caverns"   },
				{ 3, "Lava Keep"        }
			};

			string lJson = JsonConvert.SerializeObject(levels, Formatting.Indented);
			Log("Serialized", lJson);

			// NOTE: int keys get serialized as string keys in JSON ("1","2","3")
			// Newtonsoft handles converting them back to int on deserialize
			Dictionary<int, string> lRestored = JsonConvert.DeserializeObject<Dictionary<int, string>>(lJson);
			foreach (var kv in lRestored)
				Debug.Log($"  Level {kv.Key} → {kv.Value}");

			yield return null;
		}
		// ─────────────────────────────────────────────────────────────────────
		//  3. Dictionary<string, List<T>>
		// ─────────────────────────────────────────────────────────────────────
		IEnumerator routineCheck_DictionaryOfLists()
		{
			H("Dictionary<string, List<string>>  — skill tree");

			Dictionary<string, List<string>> skillTree = new Dictionary<string, List<string>>
			{
				{ "Warrior", new List<string> { "Bash",       "Shield Wall", "Berserk"  } },
				{ "Mage",    new List<string> { "Fireball",   "Ice Lance",   "Teleport" } },
				{ "Rogue",   new List<string> { "Backstab",   "Smoke Bomb",  "Vanish"   } }
			};

			string json = JsonConvert.SerializeObject(skillTree, Formatting.Indented);
			Log("Serialized", json);

			Dictionary<string, List<string>> restored =
				JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);

			foreach (var kv in restored)
				Debug.Log($"  {kv.Key} skills → {string.Join(", ", kv.Value)}");

			// ── Dictionary<string, List<int>> ────────────────────────────────
			H("Dictionary<string, List<int>>  — drop rate tables");

			Dictionary<string, List<int>> dropRates = new Dictionary<string, List<int>>
			{
				{ "Goblin",  new List<int> { 10, 5, 1  } },  // common, rare, legendary %
                { "Dragon",  new List<int> { 60, 30, 10 } },
				{ "Slime",   new List<int> { 80, 15, 0  } }
			};

			string dJson = JsonConvert.SerializeObject(dropRates, Formatting.Indented);
			Log("Serialized", dJson);

			Dictionary<string, List<int>> dRestored =
				JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(dJson);

			foreach (var kv in dRestored)
				Debug.Log($"  {kv.Key} drops → common:{kv.Value[0]}% rare:{kv.Value[1]}% legendary:{kv.Value[2]}%");

			yield return null;
		}

		// ─────────────────────────────────────────────────────────────────────
		//  4. Fully Nested Everything
		//     List< Dictionary< string, List<List<CustomClass>> > >
		// ─────────────────────────────────────────────────────────────────────
		[System.Serializable]
		public class Stat
		{
			public string StatName;
			public float Value;
			public string Unit;
		}
		[System.Serializable]
		public class Ability
		{
			public string Name;
			public int ManaCost;
			public List<string> Tags;                 // List<string> inside class
			public List<Stat> ScalingStats;         // List<Stat>   inside class
		}
		[System.Serializable]
		public class Wave
		{
			public int WaveNumber;
			public List<string> EnemyIDs;
			public List<List<int>> SpawnPositions;       // List<List<int>> inside class
		}
		[System.Serializable]
		public class Zone
		{
			public string ZoneName;
			public List<Wave> Waves;                    // List<Wave>
			public Dictionary<string, int> ResourceRewards;          // Dictionary inside class
			public Dictionary<string, List<Ability>> BossAbilityPhases;        // Dict<string, List<Ability>>
		}
		IEnumerator routineCheck_NestedEverything()
		{
			H("Full nesting — List<Zone> with Dictionary + List<List<>> inside");

			List<Zone> zones = new List<Zone>
			{
				new Zone
				{
					ZoneName = "Goblin Caverns",
					Waves = new List<Wave>
					{
						new Wave
						{
							WaveNumber     = 1,
							EnemyIDs       = new List<string> { "goblin_basic", "goblin_archer" },
							SpawnPositions = new List<List<int>>   // List<List<int>>
                            {
								new List<int> { 0, 0 },
								new List<int> { 3, 1 },
								new List<int> { 6, 0 }
							}
						},
						new Wave
						{
							WaveNumber     = 2,
							EnemyIDs       = new List<string> { "goblin_shaman", "goblin_basic", "goblin_basic" },
							SpawnPositions = new List<List<int>>
							{
								new List<int> { 1, 2 },
								new List<int> { 4, 2 }
							}
						}
					},
					ResourceRewards = new Dictionary<string, int>   // Dictionary<string,int>
                    {
						{ "Gold",     250 },
						{ "XP",       400 },
						{ "Crystals", 10  }
					},
					BossAbilityPhases = new Dictionary<string, List<Ability>>  // Dict<string, List<Ability>>
                    {
						{
							"Phase1", new List<Ability>
							{
								new Ability
								{
									Name     = "Rock Throw",
									ManaCost = 0,
									Tags     = new List<string> { "physical", "ranged" },
									ScalingStats = new List<Stat>
									{
										new Stat { StatName = "Damage", Value = 35f, Unit = "hp" }
									}
								}
							}
						},
						{
							"Phase2", new List<Ability>
							{
								new Ability
								{
									Name     = "Berserker Rage",
									ManaCost = 50,
									Tags     = new List<string> { "buff", "self" },
									ScalingStats = new List<Stat>
									{
										new Stat { StatName = "AttackSpeed", Value = 1.8f, Unit = "x"   },
										new Stat { StatName = "Damage",      Value = 60f,  Unit = "hp"  }
									}
								},
								new Ability
								{
									Name     = "Ground Slam",
									ManaCost = 30,
									Tags     = new List<string> { "physical", "aoe", "stun" },
									ScalingStats = new List<Stat>
									{
										new Stat { StatName = "Damage",   Value = 80f,  Unit = "hp" },
										new Stat { StatName = "StunTime", Value = 2.5f, Unit = "s"  }
									}
								}
							}
						}
					}
				},
				new Zone
				{
					ZoneName = "Lava Keep",
					Waves = new List<Wave>
					{
						new Wave
						{
							WaveNumber     = 1,
							EnemyIDs       = new List<string> { "fire_imp", "lava_golem" },
							SpawnPositions = new List<List<int>>
							{
								new List<int> { 2, 5 },
								new List<int> { 8, 5 }
							}
						}
					},
					ResourceRewards = new Dictionary<string, int>
					{
						{ "Gold",     800  },
						{ "XP",       1200 },
						{ "Crystals", 40   }
					},
					BossAbilityPhases = new Dictionary<string, List<Ability>>
					{
						{
							"Phase1", new List<Ability>
							{
								new Ability
								{
									Name     = "Flame Breath",
									ManaCost = 40,
									Tags     = new List<string> { "fire", "aoe", "dot" },
									ScalingStats = new List<Stat>
									{
										new Stat { StatName = "DPS",      Value = 25f, Unit = "hp/s" },
										new Stat { StatName = "Duration", Value = 4f,  Unit = "s"    }
									}
								}
							}
						}
					}
				}
			};

			// ── Serialize ────────────────────────────────────────────────────
			string json = JsonConvert.SerializeObject(zones, Formatting.Indented);
			Log("Serialized", json);

			// ── Deserialize ──────────────────────────────────────────────────
			List<Zone> restored = JsonConvert.DeserializeObject<List<Zone>>(json);

			foreach (var zone in restored)
			{
				Debug.Log($"<color=cyan>═══ Zone: {zone.ZoneName} ═══</color>");

				foreach (var wave in zone.Waves)
				{
					Debug.Log($"  <color=yellow>Wave {wave.WaveNumber}</color> — enemies: {string.Join(", ", wave.EnemyIDs)}");
					foreach (var pos in wave.SpawnPositions)
						Debug.Log($"    spawn → ({pos[0]}, {pos[1]})");
				}

				Debug.Log("  Rewards:");
				foreach (var kv in zone.ResourceRewards)
					Debug.Log($"    {kv.Key}: {kv.Value}");

				Debug.Log("  Boss phases:");
				foreach (var phase in zone.BossAbilityPhases)
				{
					Debug.Log($"    <color=orange>{phase.Key}</color>");
					foreach (var ab in phase.Value)
					{
						Debug.Log($"      Ability: {ab.Name} ({ab.ManaCost} mp) | tags: {string.Join(", ", ab.Tags)}");
						foreach (var stat in ab.ScalingStats)
							Debug.Log($"        └ {stat.StatName}: {stat.Value} {stat.Unit}");
					}
				}
			}

			yield return null;
		}
		// ─────────────────────────────────────────────────────────────────────
		//  5. JObject — Dynamic / schema-less parsing (no class needed)
		// ─────────────────────────────────────────────────────────────────────
		IEnumerator routineCheck_JObjectDynamic()
		{
			H("JObject — parse arbitrary JSON without a class");

			string rawJson = @"
            {
                ""server"": ""EU-West-1"",
                ""online"": true,
                ""playerCount"": 142,
                ""topPlayers"": [""Alice"", ""Bob"", ""Carol""],
                ""config"": {
                    ""maxPlayers"": 200,
                    ""tickRate"": 64,
                    ""allowedModes"": [""PVP"", ""PVE"", ""Coop""]
                }
            }";

			JObject obj = JObject.Parse(rawJson);

			Debug.Log($"  Server      → {obj["server"]}");
			Debug.Log($"  Online      → {obj["online"]}");
			Debug.Log($"  PlayerCount → {obj["playerCount"]}");

			// Reading a nested array
			JArray topPlayers = (JArray)obj["topPlayers"];
			Debug.Log($"  Top players → {string.Join(", ", topPlayers.Select(p => p.ToString()))}");

			// Drilling into nested object
			JObject config = (JObject)obj["config"];
			Debug.Log($"  MaxPlayers  → {config["maxPlayers"]}");
			Debug.Log($"  TickRate    → {config["tickRate"]}");

			JArray modes = (JArray)config["allowedModes"];
			Debug.Log($"  Modes       → {string.Join(", ", modes.Select(m => m.ToString()))}");

			// Modifying and re-serializing
			obj["playerCount"] = 143;
			config["tickRate"] = 128;
			string modified = obj.ToString(Formatting.Indented);
			Log("Modified + re-serialized", modified);

			yield return null;
		}
		// ─────────────────────────────────────────────────────────────────────
		//  Helpers
		// ─────────────────────────────────────────────────────────────────────
		void H(string title)
			=> Debug.Log($"\n<color=lime>──── {title} ────</color>");
		void Log(string label, string content)
		{
			LOG.AddLog(content, "json");
			// => Debug.Log($"<color=white>[{label}]</color>\n{content}");
		}
	}
}