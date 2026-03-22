# Supermarket Simulator — Phase 1 Complete
> Unity 6000.3+ · NavMesh AI Navigation 2.0 · SPACE_UTIL  
> Queue prototype + Shelf system + Shopping FSM

---

## 1. Design Decisions

### Homogeneous tier rule
Each `ShelfTier` independently tracks its own item type.

| Tier state | Accepts any item? | Rule |
|---|---|---|
| `occupied == 0` | Yes — **free** | First item placed locks the tier |
| `occupied >= 1` | No — **locked** | Only same `SO_ItemData` accepted until zero |
| reaches zero again | Yes — **free** | `currentItemData` cleared, tier unlocks |

Upper and lower tiers are fully independent. A shelf can hold Milk on
lower and Bread on upper simultaneously. Neither knows about the other.

### Scene-start population
`AutoStockService` reads a list of `ShelfStockEntry` structs in the
inspector and calls `ShelfPOI.SetStock()` on each entry at `Start()`.
No code changes needed to adjust starting quantities — just edit the
inspector list. Later this is replaced by player stocking.

### Item visuals — slot grid
`ShelfTier` computes slot positions procedurally along the board's
local X axis, centered, evenly spaced. No pre-placed Transforms.
Items are spawned left→right on stock, removed right→left on take.

### NPC item lookup
NPC holds a `List<SO_ItemData>` shopping list. FSM queries
`POIRegistry.GetShelfWithItem(item)` — a linear scan checking
`HasStockOf(item) && HasAvailableSlot()`. NPC never holds a named
shelf reference.

---

## 2. Scene Hierarchy

```
SupermarketScene
├── _Systems
│   ├── POIRegistry          [POIRegistry.cs]
│   ├── AutoStockService     [AutoStockService.cs]
│   └── CustomerSpawner      [CustomerSpawner.cs]
│
├── NavMesh
│   └── NavMeshSurface       [bake at edit time]
│
├── Level
│   ├── Plane (100×100)
│   │
│   └── Shelves
│       ├── Shelf_A          [ShelfPOI.cs]
│       │   ├── shelfModel
│       │   │   ├── lowerBoard
│       │   │   ├── lowerShelf (y=0.2)   [ShelfTier.cs  capacity=10]
│       │   │   ├── upperBoard
│       │   │   └── upperShelf (y=1.0)   [ShelfTier.cs  capacity=10]
│       │   └── InteractionPoint         (empty Transform ~0.8m in front)
│       ├── Shelf_B          [ShelfPOI.cs]
│       │   └── ... same structure
│       ├── Shelf_C
│       └── Shelf_D
│
├── Queue
│   └── Queue_Main           [QueuePOI.cs]
│       ├── QueueSlot_0      (front — closest to counter)
│       ├── QueueSlot_1
│       ├── QueueSlot_2
│       ├── QueueSlot_3
│       └── QueueSlot_4
│
├── Waypoints
│   ├── SpawnPoint
│   ├── EntrancePoint
│   ├── ExitPoint
│   └── DespawnPoint
│
└── Player
    └── playerCam
```

---

## 3. Project Structure

```
Assets/
├── Scripts/
│   ├── Data/
│   │   └── SO_ItemData.cs
│   ├── Events/
│   │   └── GameEvents.cs
│   ├── POI/
│   │   ├── IPOI.cs
│   │   ├── POIRegistry.cs
│   │   ├── ShelfTier.cs
│   │   ├── ShelfPOI.cs
│   │   └── QueuePOI.cs
│   ├── NPC/
│   │   ├── NavMeshMover.cs
│   │   ├── CustomerFSM.cs
│   │   └── CustomerAgent.cs
│   ├── Spawner/
│   │   └── CustomerSpawner.cs
│   └── Store/
│       └── AutoStockService.cs
│
└── ScriptableObjects/
    ├── Items/
    │   ├── SO_Item_Milk.asset
    │   ├── SO_Item_Apple.asset
    │   ├── SO_Item_Bread.asset
    │   └── SO_Item_Soda.asset
    └── CustomerProfiles/
        ├── SO_Profile_Regular.asset
        └── SO_Profile_Hurried.asset
```

---

## 4. ScriptableObjects to Create

**Items** → Create → ShopSim_SO → SO_ItemData

| Asset         | id    | itemName | basePrice | displayColor         |
|---------------|-------|----------|-----------|----------------------|
| SO_Item_Milk  | milk  | Milk     | 2.50      | (0.95, 0.95, 1, 1)   |
| SO_Item_Apple | apple | Apple    | 1.20      | (0.9, 0.2, 0.2, 1)   |
| SO_Item_Bread | bread | Bread    | 1.80      | (0.8, 0.65, 0.3, 1)  |
| SO_Item_Soda  | soda  | Soda     | 1.50      | (0.2, 0.6, 0.9, 1)   |

**Customer Profiles** → Create → ShopSim_SO → SO_CustomerProfileData

| Asset               | id      | walkSpeed | minQWait | maxQWait |
|---------------------|---------|-----------|----------|----------|
| SO_Profile_Regular  | Regular | 3.5       | 10       | 20       |
| SO_Profile_Hurried  | Hurried | 5.0       | 6        | 12       |

---

## 5. AutoStockService Inspector Setup Example

```
ShelfStockEntry list:
  [0]  shelf: Shelf_A   tierIndex: 0 (lower)   item: SO_Item_Milk    amount: 3
  [1]  shelf: Shelf_A   tierIndex: 1 (upper)   item: SO_Item_Milk    amount: 1
  [2]  shelf: Shelf_B   tierIndex: 0 (lower)   item: SO_Item_Apple   amount: 5
  [3]  shelf: Shelf_B   tierIndex: 1 (upper)   item: SO_Item_Bread   amount: 8
  [4]  shelf: Shelf_C   tierIndex: 0 (lower)   item: SO_Item_Soda    amount: 10
```

Result: Shelf_A lower = 3/10 milk · upper = 1/10 milk (independent).
Shelf_B lower = 5/10 apple · upper = 8/10 bread. Different items, one per tier.

---

## 6. Scripts

---

### `SO_ItemData.cs`

```csharp
using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  SO_ItemData.cs
//  ScriptableObject — defines one product type.
//  Pure data. No MonoBehaviour, no runtime state.
//  Referenced by ShelfTier (what is stocked) and CustomerAgent
//  (what is wanted). Neither holds the other's reference.
// ============================================================
[CreateAssetMenu(fileName = "SO_ItemData",
                 menuName  = "ShopSim_SO/SO_ItemData")]
public class SO_ItemData : ScriptableObject
{
    [Header("Identity")]
    public string id       = "item_id";
    public string itemName = "Item";
    public float  basePrice = 1f;

    [Header("Prototype Visual")]
    [Tooltip("Tints the spawned slot cube on the shelf tier.")]
    public Color displayColor = Color.white;

    [Tooltip("Prototype cube prefab spawned per slot. Use cubeModel x0.1.")]
    public GameObject slotPrefab;
}
```

---

### `GameEvents.cs`

```csharp
using System;
using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  GameEvents.cs
//  Static event bus. The ONLY cross-system channel.
//  No system holds a typed reference to another system.
//
//  Naming:
//    event field  → On[Subject][Verb]     subscribed to
//    raise method → Raise[Subject][Verb]  called by firer
//    handler      → Handle[Subject][Verb] on listener class
// ============================================================
public static class GameEvents
{
    // ┌──────────────────────────────────────────────────────────┐
    // │  CustomerFSM (WalkIn state)                              │
    // │    └─ RaiseCustomerEntered(this)                         │
    // │         ├──► StoreManager  → _insideCount++             │
    // │         └──► DebugLogger   → Debug.Log(agent.name)      │
    // └──────────────────────────────────────────────────────────┘
    public static event Action<CustomerAgent> OnCustomerEntered;
    public static void RaiseCustomerEntered(CustomerAgent agent) =>
        OnCustomerEntered?.Invoke(agent);

    // ┌──────────────────────────────────────────────────────────┐
    // │  CustomerFSM (JoinQueue state)                           │
    // │    └─ RaiseCustomerJoinedQ(this)                         │
    // │         ├──► StoreManager  → track queue occupancy       │
    // │         └──► QueueUI       → refresh slot badge          │
    // └──────────────────────────────────────────────────────────┘
    public static event Action<CustomerAgent> OnCustomerJoinedQ;
    public static void RaiseCustomerJoinedQ(CustomerAgent agent) =>
        OnCustomerJoinedQ?.Invoke(agent);

    // ┌──────────────────────────────────────────────────────────┐
    // │  CustomerFSM (LeaveStore state)                          │
    // │    └─ RaiseCustomerLeft(this)                            │
    // │         ├──► StoreManager  → _insideCount--             │
    // │         └──► Analytics     → record visit duration       │
    // └──────────────────────────────────────────────────────────┘
    public static event Action<CustomerAgent> OnCustomerLeft;
    public static void RaiseCustomerLeft(CustomerAgent agent) =>
        OnCustomerLeft?.Invoke(agent);

    // ┌──────────────────────────────────────────────────────────┐
    // │  ShelfPOI / ShelfTier (TryTakeItem)                      │
    // │    └─ RaiseItemTaken(shelf, tier, item)                  │
    // │         ├──► StoreManager  → track units sold            │
    // │         └──► ShelfUI       → refresh stock badge         │
    // └──────────────────────────────────────────────────────────┘
    public static event Action<ShelfPOI, ShelfTier, SO_ItemData> OnItemTaken;
    public static void RaiseItemTaken(ShelfPOI shelf, ShelfTier tier, SO_ItemData item) =>
        OnItemTaken?.Invoke(shelf, tier, item);

    // ┌──────────────────────────────────────────────────────────┐
    // │  ShelfTier (RemoveOne — when tier hits zero)             │
    // │    └─ RaiseTierCleared(shelf, tier)                      │
    // │         └──► ShelfUI  → show "needs restock" badge       │
    // │              (Phase 2: player stocking highlight)        │
    // └──────────────────────────────────────────────────────────┘
    public static event Action<ShelfPOI, ShelfTier> OnTierCleared;
    public static void RaiseTierCleared(ShelfPOI shelf, ShelfTier tier) =>
        OnTierCleared?.Invoke(shelf, tier);

    // ┌──────────────────────────────────────────────────────────┐
    // │  AutoStockService / ShelfPOI (SetStock / AddStock)       │
    // │    └─ RaiseShelfRestocked(shelf, tier, item, amount)     │
    // │         └──► ShelfUI  → refresh count display            │
    // └──────────────────────────────────────────────────────────┘
    public static event Action<ShelfPOI, ShelfTier, SO_ItemData, int> OnShelfRestocked;
    public static void RaiseShelfRestocked(ShelfPOI shelf, ShelfTier tier,
                                           SO_ItemData item, int amount) =>
        OnShelfRestocked?.Invoke(shelf, tier, item, amount);
}
```

---

### `IPOI.cs`

```csharp
using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  IPOI.cs
//  Contract for any bookable world-space slot.
//  Queue counters, shelves, beds — all implement this.
//  CustomerFSM only ever talks to IPOI, never to concrete types.
// ============================================================
public interface IPOI
{
    string POIId { get; }

    // true if at least one slot is unbooked
    bool      HasAnyAvailableSlot();

    // reserve earliest free slot; returns Transform or null if full
    Transform BookSlot(CustomerAgent agent);

    // release the slot this agent was holding
    void      ReleaseSlot(CustomerAgent agent);
}
```

---

### `ShelfTier.cs`

```csharp
using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  ShelfTier.cs
//  Manages physical item visuals on ONE shelf board.
//  Attach to: lowerShelf child, upperShelf child.
//
//  Homogeneous tier rule (enforced here):
//    IsFree  (occupied == 0) → accepts any SO_ItemData via Stock()
//    IsLocked (occupied > 0) → only accepts same CurrentItemData
//    Hits zero               → CurrentItemData cleared, tier free
//
//  Slot layout (procedural, no pre-placed Transforms):
//    Centered along board local X axis.
//    Fill  : left → right  (index 0 → capacity-1)
//    Remove: right → left  (rightmost filled slot first)
// ============================================================
public class ShelfTier : MonoBehaviour
{
    [Header("Grid")]
    [Tooltip("Max items on this board.")]
    public int   capacity    = 10;
    [Tooltip("World-unit gap between item centers. Tune to item width + margin.")]
    public float slotSpacing = 0.22f;

    // ── Read-only state ──────────────────────────────────────
    public SO_ItemData CurrentItemData { get; private set; }
    public int  Occupied => _occupied;
    public int  Capacity => capacity;
    public bool IsFree   => _occupied == 0;
    public bool IsFull   => _occupied >= capacity;
    public bool HasItems => _occupied > 0;

    // ── Homogeneous rule check ───────────────────────────────
    // true when this tier can accept one more unit of item
    public bool CanAccept(SO_ItemData item) =>
        !IsFull && (IsFree || CurrentItemData == item);

    // ── Init (called by ShelfPOI.SetStock / AutoStockService) ─
    // clears existing visuals then spawns `count` units of item
    public void Initialize(int count, SO_ItemData item)
    {
        Clear();
        if (item == null || count <= 0) return;

        CurrentItemData = item;
        int toSpawn = Mathf.Min(count, capacity);
        for (int i = 0; i < toSpawn; i++)
            SpawnAt(i, item);
        _occupied = toSpawn;
    }

    // ── Slot position ────────────────────────────────────────
    // world-space center of slot i, centered on board local X
    private Vector3 SlotPosition(int i)
    {
        float totalWidth = (capacity - 1) * slotSpacing;
        float localX     = -totalWidth * 0.5f + i * slotSpacing;
        // +0.1f lifts item to sit flush on board surface (half of 0.2-tall cube)
        return transform.TransformPoint(new Vector3(localX, 0.1f, 0f));
    }

    // ── Spawn / tint ─────────────────────────────────────────
    private void SpawnAt(int i, SO_ItemData item)
    {
        if (item.slotPrefab == null)
        {
            Debug.LogWarning($"[ShelfTier] {name}: SO_ItemData '{item.id}' has no slotPrefab.");
            return;
        }
        _slots[i] = Instantiate(item.slotPrefab, SlotPosition(i),
                                 transform.rotation, transform);
        TintSlot(_slots[i], item.displayColor);
    }

    private static void TintSlot(GameObject go, Color color)
    {
        var mr = go.GetComponentInChildren<MeshRenderer>();
        if (mr != null) mr.material.color = color;
    }

    // ── Public mutation ──────────────────────────────────────
    // add one unit; caller must call CanAccept first
    public bool AddOne(SO_ItemData item)
    {
        if (!CanAccept(item)) return false;
        if (IsFree)
        {
            CurrentItemData = item;
            _slots = new GameObject[capacity]; // ensure array ready
        }
        SpawnAt(_occupied, item);
        _occupied++;
        return true;
    }

    // remove rightmost item; fires TierCleared if reaching zero
    public bool RemoveOne(ShelfPOI ownerShelf)
    {
        if (_occupied == 0) return false;
        _occupied--;
        if (_slots[_occupied] != null)
        {
            Destroy(_slots[_occupied]);
            _slots[_occupied] = null;
        }
        if (_occupied == 0)
        {
            CurrentItemData = null;
            GameEvents.RaiseTierCleared(ownerShelf, this);
        }
        return true;
    }

    // destroy all visuals, reset state
    public void Clear()
    {
        if (_slots != null)
            foreach (var go in _slots)
                if (go != null) Destroy(go);
        _slots          = new GameObject[capacity];
        _occupied       = 0;
        CurrentItemData = null;
    }

    // ── Private ──────────────────────────────────────────────
    GameObject[] _slots;
    int          _occupied;

    private void Awake()
    {
        Debug.Log(C.method(this));
        _slots = new GameObject[capacity];
    }

    // ── Gizmos ───────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < capacity; i++)
        {
            bool filled = _slots != null && _slots.Length > i && _slots[i] != null;
            Gizmos.color = filled ? Color.yellow : new Color(0.3f, 0.9f, 0.9f, 0.35f);
            Gizmos.DrawWireCube(SlotPosition(i), new Vector3(0.18f, 0.18f, 0.18f));
        }
        string label = CurrentItemData != null
            ? $"{CurrentItemData.itemName} {_occupied}/{capacity}"
            : $"[free] 0/{capacity}";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.45f, label);
    }
#endif
}
```

---

### `ShelfPOI.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  ShelfPOI.cs
//  Aggregates stock across tiers. Owns ONE NPC interaction slot.
//  No single SO_ItemData field — item ownership lives in tiers.
//
//  Tier fill priority (SetStock):   index 0 first (lower → upper)
//  Item take priority (TryTakeItem): highest index first (upper → lower)
// ============================================================
public class ShelfPOI : MonoBehaviour, IPOI
{
    [Header("Identity")]
    [SerializeField] string _poiId = "shelf_id";
    public string POIId => _poiId;

    [Header("Tiers")]
    [Tooltip("Index 0 = lower board, Index 1 = upper board.")]
    [SerializeField] List<ShelfTier> _TIERS;

    [Header("Interaction")]
    [Tooltip("NPC nav target. ~0.8m in front of shelf face, ON NavMesh.")]
    [SerializeField] Transform _TR_interactionPoint;

    CustomerAgent _occupant;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()  => Debug.Log(C.method(this));
    private void OnEnable()  => POIRegistry.Ins.RegisterShelf(this);
    private void OnDisable() => POIRegistry.Ins.UnRegisterShelf(this);

    // ── Aggregated reads ──────────────────────────────────────
    // true if any tier has at least 1 unit
    public bool HasStock() => _TIERS != null && _TIERS.Any(t => t.HasItems);

    // true if any tier holds at least 1 unit of this specific item
    public bool HasStockOf(SO_ItemData item) =>
        _TIERS != null && _TIERS.Any(t => t.HasItems && t.CurrentItemData == item);

    // all distinct items currently stocked across tiers
    public List<SO_ItemData> GetStockedItems() =>
        _TIERS?
            .Where(t => t.HasItems && t.CurrentItemData != null)
            .Select(t => t.CurrentItemData)
            .Distinct()
            .ToList()
        ?? new List<SO_ItemData>();

    // ── Tier selection ────────────────────────────────────────
    // tier that can take one more of this item
    // prefers existing tier already holding item (consolidate),
    // falls back to free tier
    public ShelfTier GetTierForStocking(SO_ItemData item)
    {
        if (_TIERS == null || item == null) return null;
        ShelfTier existing = _TIERS.find(t => t.CurrentItemData == item && !t.IsFull);
        return existing ?? _TIERS.find(t => t.IsFree);
    }

    // topmost tier (highest index) holding this item, for taking
    public ShelfTier GetTierWithItem(SO_ItemData item)
    {
        if (_TIERS == null || item == null) return null;
        for (int i = _TIERS.Count - 1; i >= 0; i--)
            if (_TIERS[i].HasItems && _TIERS[i].CurrentItemData == item)
                return _TIERS[i];
        return null;
    }

    // ── IPOI ──────────────────────────────────────────────────
    public bool HasAnyAvailableSlot() => _occupant == null;

    public Transform BookSlot(CustomerAgent agent)
    {
        Debug.Log(C.method(this, adMssg: agent.gameObject.name));
        if (_occupant != null) return null;
        _occupant = agent;
        return _TR_interactionPoint != null ? _TR_interactionPoint : transform;
    }

    public void ReleaseSlot(CustomerAgent agent)
    {
        Debug.Log(C.method(this, adMssg: agent.gameObject.name));
        if (_occupant == agent) _occupant = null;
    }

    // ── Stock operations ──────────────────────────────────────
    // bulk stock a specific tier by index (used by AutoStockService)
    public void SetStockOnTier(int tierIndex, SO_ItemData item, int amount)
    {
        if (_TIERS == null || tierIndex >= _TIERS.Count || item == null) return;
        _TIERS[tierIndex].Initialize(amount, item);
        GameEvents.RaiseShelfRestocked(this, _TIERS[tierIndex], item, amount);
    }

    // remove one unit of item from topmost holding tier
    public bool TryTakeItem(SO_ItemData item)
    {
        ShelfTier tier = GetTierWithItem(item);
        if (tier == null) return false;
        bool removed = tier.RemoveOne(this);
        if (removed) GameEvents.RaiseItemTaken(this, tier, item);
        return removed;
    }

    // add one unit of item to best available tier (enforces homogeneous rule)
    public bool TryStockItem(SO_ItemData item)
    {
        ShelfTier target = GetTierForStocking(item);
        if (target == null)
        {
            Debug.LogWarning($"[ShelfPOI] {name}: no tier can accept {item?.id}");
            return false;
        }
        bool added = target.AddOne(item);
        if (added) GameEvents.RaiseShelfRestocked(this, target, item, 1);
        return added;
    }

    // ── Gizmos ───────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = HasStock() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.22f);

        if (_TR_interactionPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_TR_interactionPoint.position, 0.18f);
            Gizmos.DrawLine(transform.position + Vector3.up, _TR_interactionPoint.position);
        }

        if (_TIERS != null)
            _TIERS.forEach((tier, i) =>
            {
                if (tier == null) return;
                string label = tier.CurrentItemData != null
                    ? $"[{i}] {tier.CurrentItemData.itemName} {tier.Occupied}/{tier.Capacity}"
                    : $"[{i}] free";
                UnityEditor.Handles.Label(
                    tier.transform.position + Vector3.up * 0.55f, label);
            });
    }
#endif
}
```

---

### `POIRegistry.cs`

```csharp
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  POIRegistry.cs
//  Singleton query hub. POIs self-register on enable.
//  FSM queries here — never holds a direct named reference
//  to any shelf or queue.
// ============================================================
public class POIRegistry : MonoBehaviour
{
    public static POIRegistry Ins { get; private set; }

    private void Awake()
    {
        Debug.Log(C.method(this));
        POIRegistry.Ins = this;
        SHELVES = new List<ShelfPOI>();
        QUEUES  = new List<QueuePOI>();
    }

    // ── Shelves ───────────────────────────────────────────────
    List<ShelfPOI> SHELVES;
    public void RegisterShelf(ShelfPOI s)   => SHELVES.Add(s);
    public void UnRegisterShelf(ShelfPOI s) => SHELVES.Remove(s);

    // first shelf holding item that has stock AND a free NPC slot
    public ShelfPOI GetShelfWithItem(SO_ItemData item) =>
        SHELVES.find(s => s.HasStockOf(item) && s.HasAnyAvailableSlot());

    // all distinct item types currently stocked across all shelves
    public List<SO_ItemData> GetAllStockedItems() =>
        SHELVES
            .SelectMany(s => s.GetStockedItems())
            .Distinct()
            .ToList();

    // all shelves that can currently accept one more unit of item
    // used by player stocking UI in Phase 2 to highlight valid shelves
    public List<ShelfPOI> GetShelvesAccepting(SO_ItemData item) =>
        SHELVES.Where(s => s.GetTierForStocking(item) != null).ToList();

    // ── Queues ────────────────────────────────────────────────
    List<QueuePOI> QUEUES;
    public void RegisterQ(QueuePOI q)   => QUEUES.Add(q);
    public void UnRegisterQ(QueuePOI q) => QUEUES.Remove(q);

    // first queue with at least one unbooked slot
    public QueuePOI GetFirstAvailableQueueWithSlots() =>
        QUEUES.find(q => q.HasAnyAvailableSlot());
}
```

---

### `QueuePOI.cs`

```csharp
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  QueuePOI.cs
//  Ordered queue of Transform slots. Index 0 = front (counter).
//  Self-registers with POIRegistry on enable.
// ============================================================
public class QueuePOI : MonoBehaviour, IPOI
{
    #region Public Interface API

    public string POIId => _poiId;

    public Transform BookSlot(CustomerAgent agent)
    {
        Debug.Log(C.method(this, "cyan", adMssg: agent.gameObject.name));
        foreach (Transform tr in _TR_QUEUE_SLOTS)
            if (DOC_OCCUPANTS[tr] == null)
            {
                DOC_OCCUPANTS[tr] = agent;
                return tr;
            }
        return null;
    }

    public bool HasAnyAvailableSlot() =>
        DOC_OCCUPANTS.findIndex(kvp => kvp.Value == null) != -1;

    public void ReleaseSlot(CustomerAgent agent)
    {
        Debug.Log(C.method(this, "cyan", adMssg: agent.gameObject.name));
        foreach (var kvp in DOC_OCCUPANTS)
            if (kvp.Value == agent)
            {
                DOC_OCCUPANTS[kvp.Key] = null;
                return;
            }
    }

    #endregion

    #region Private API

    [Header("Id")]    [SerializeField] string          _poiId          = "queue_id";
    [Header("Slots")] [SerializeField] List<Transform> _TR_QUEUE_SLOTS;

    Dictionary<Transform, CustomerAgent> DOC_OCCUPANTS;

    private void Awake()
    {
        Debug.Log(C.method(this));
        DOC_OCCUPANTS = new Dictionary<Transform, CustomerAgent>();
        foreach (Transform tr in _TR_QUEUE_SLOTS)
            DOC_OCCUPANTS[tr] = null;
    }

    private void OnEnable()  => POIRegistry.Ins.RegisterQ(this);
    private void OnDisable() => POIRegistry.Ins.UnRegisterQ(this);

    private void OnDrawGizmosSelected()
    {
        if (DOC_OCCUPANTS == null) return;
        _TR_QUEUE_SLOTS.forEach((tr, index) =>
        {
            if (tr == null) return;
            bool isFree = DOC_OCCUPANTS[tr] == null;
            Gizmos.color = isFree ? Color.green : Color.red;
            Gizmos.DrawWireCube(tr.position, Vector3.one * 0.3f);
            UnityEditor.Handles.Label(
                tr.position + Vector3.up * 0.5f,
                $"Q[{index}] {(isFree ? "" : "✓")}");
        });
    }

    #endregion
}
```

---

### `AutoStockService.cs`

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  AutoStockService.cs
//  Populates shelves at scene start from inspector-configured
//  entries. No code changes needed to adjust starting stock —
//  just edit the inspector list.
//
//  Each entry targets a specific shelf + tier index + item + amount.
//  A shelf can appear multiple times with different tiers and items.
//
//  Execution:
//    Start() → for each entry → ShelfPOI.SetStockOnTier(tier, item, amount)
//
//  Phase 2: replace with player stocking via IInteractable on StockBox.
// ============================================================
public class AutoStockService : MonoBehaviour
{
    [Serializable]
    public struct ShelfStockEntry
    {
        [Tooltip("The shelf to stock.")]
        public ShelfPOI shelf;
        [Tooltip("0 = lower tier, 1 = upper tier.")]
        public int tierIndex;
        [Tooltip("Item type to place on this tier.")]
        public SO_ItemData item;
        [Tooltip("Units to stock on this tier (capped at tier capacity).")]
        [Min(0)]
        public int amount;
    }

    [Header("Initial stock — one entry per shelf+tier combination")]
    [SerializeField] List<ShelfStockEntry> _STOCK_ENTRIES;

    [Header("Cosmetic stagger")]
    [Tooltip("Delay between each entry. Gives a visual fill effect on start.")]
    [SerializeField] float _staggerDelay = 0.05f;

    private void Start()
    {
        Debug.Log(C.method(this));
        StartCoroutine(RoutineStock());
    }

    IEnumerator RoutineStock()
    {
        foreach (var entry in _STOCK_ENTRIES)
        {
            if (entry.shelf == null || entry.item == null || entry.amount <= 0)
                continue;

            entry.shelf.SetStockOnTier(entry.tierIndex, entry.item, entry.amount);

            if (_staggerDelay > 0f)
                yield return new WaitForSeconds(_staggerDelay);
        }
        yield return null; // settle frame
        Debug.Log("[AutoStockService] Stocking complete.".colorTag("lime"));
    }
}
```

---

### `NavMeshMover.cs`

```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SPACE_UTIL;

public class NavMeshMover : MonoBehaviour
{
    public void ApplyProfileData(SO_CustomerProfileData profileData)
    {
        _agent.speed             = profileData.walkSpeed;
        _agent.avoidancePriority = profileData.avoidancePriority;
    }

    public void MoveTo(Vector3 destination, Action onArrived = null)
    {
        CancelTracking();
        _agent.isStopped = false;
        _agent.SetDestination(destination);
        if (onArrived != null)
            refTrackingRoutine = StartCoroutine(RoutineTrackArrival(onArrived));
    }

    public void Stop()
    {
        CancelTracking();
        if (_agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); }
    }

    [SerializeField] float         _speed        = 3f;
    [SerializeField] float         _stoppingDist = 0.3f;
    [SerializeField] float         _arrivalTime  = 20f;
    [SerializeField] NavMeshAgent  _agent;

    private void Awake()
    {
        Debug.Log(C.method(this));
        _agent.speed            = _speed;
        _agent.stoppingDistance = _stoppingDist;
        _agent.angularSpeed     = 360f;
        _agent.acceleration     = 14f;
    }

    Coroutine refTrackingRoutine;

    IEnumerator RoutineTrackArrival(Action onArrived)
    {
        yield return null; // let NavMesh compute path first frame
        for (float elapsed = 0f; elapsed < _arrivalTime; elapsed += Time.deltaTime)
        {
            bool arrived = !_agent.pathPending &&
                           _agent.remainingDistance <= _agent.stoppingDistance;
            if (arrived) { Stop(); onArrived?.Invoke(); yield break; }
            yield return null;
        }
        // timeout fallback — fire so FSM never freezes
        Debug.LogWarning($"[NavMeshMover] Timeout: {gameObject.name}");
        Stop();
        onArrived?.Invoke();
    }

    void CancelTracking()
    {
        if (refTrackingRoutine != null)
        {
            StopCoroutine(refTrackingRoutine);
            refTrackingRoutine = null;
        }
    }
}
```

---

### `SO_CustomerProfileData.cs`

```csharp
using UnityEngine;
using SPACE_UTIL;

[CreateAssetMenu(fileName = "SO_customerProfile",
                 menuName  = "ShopSim_SO/SO_customerProfile")]
public class SO_CustomerProfileData : ScriptableObject
{
    public string id            = "name";
    public float  walkSpeed     = 3f;
    public float  minQWaitSec   = 10f;
    public float  maxQWaitSec   = 20f;
    [Range(0, 99)]
    public int    avoidancePriority = 50;
}
```

---

### `CustomerAgent.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  CustomerAgent.cs
//  Pure data carrier + component owner.
//  Update() delegates entirely to CustomerFSM.
// ============================================================
public class CustomerAgent : MonoBehaviour
{
    // ── Assigned by spawner ────────────────────────────────
    [HideInInspector] public SO_CustomerProfileData Profile;
    [HideInInspector] public Transform TrEntrance;
    [HideInInspector] public Transform TrExit;
    [HideInInspector] public Transform TrDespawn;

    // ── Shopping data (assigned by spawner, mutated by FSM) ─
    [HideInInspector] public List<SO_ItemData> ShoppingList = new();

    // ── FSM runtime state (written/read by CustomerFSM only) ─
    [HideInInspector] public SO_ItemData  CurrentTargetItem;
    [HideInInspector] public ShelfPOI     CurrentTargetShelf;
    [HideInInspector] public QueuePOI     CurrentQueue;
    [HideInInspector] public Transform    TrCurrentQueueSlot;

    // ── Component refs ─────────────────────────────────────
    [SerializeField] public NavMeshMover Mover;
    [SerializeField]        CustomerFSM  FSM;

    public void ApplyProfileData(SO_CustomerProfileData profileData)
    {
        Mover.ApplyProfileData(profileData);
        Profile = profileData;
    }

    private void Awake()
    {
        Debug.Log(C.method(this));
        FSM.Init(this);
    }

    private void Update() => FSM.Tick();

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (CurrentTargetShelf != null)
        {
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawDottedLine(
                transform.position + Vector3.up,
                CurrentTargetShelf.transform.position + Vector3.up, 3f);
        }
        if (TrCurrentQueueSlot != null)
        {
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f);
            UnityEditor.Handles.DrawDottedLine(
                transform.position + Vector3.up,
                TrCurrentQueueSlot.position + Vector3.up, 3f);
        }
    }
#endif
}
```

---

### `CustomerFSM.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  CustomerFSM.cs
//  Customer state machine.
//
//  State flow:
//    walkIn
//      └─► selectItem ◄──────────────────────────────┐
//              ├─ item found ─► navigateToShelf       │
//              │                    └─► takeItem ─────┘
//              └─ list empty ─► joinQueue
//                                   └─► waitInQueue
//                                           └─► leaveStore
//                                                   └─► walkOut → Destroy
//
//  hasDoneInit  — reset on each TransitionTo; entry code runs once per state.
//  isInProgressNav — true while NavMeshMover callback pending; Tick() no-ops.
// ============================================================
public enum CustomerState
{
    walkIn,
    selectItem,
    navigateToShelf,
    takeItem,
    joinQueue,
    waitInQueue,
    leaveStore,
    walkOut,
    done,
}

public class CustomerFSM : MonoBehaviour
{
    // ── State ────────────────────────────────────────────────
    bool  hasDoneInit      = false;
    bool  isInProgressNav  = false;
    float waitTimer        = 0f;
    float waitDuration     = 10f;

    CustomerAgent    owner;
    public CustomerState currState { get; private set; }

    // ── Init ─────────────────────────────────────────────────
    public void Init(CustomerAgent agent)
    {
        owner     = agent;
        currState = CustomerState.walkIn;
    }

    // ── Tick ─────────────────────────────────────────────────
    public void Tick()
    {
        if (isInProgressNav) return;

             if (currState == CustomerState.walkIn)          ExecWalkIn();
        else if (currState == CustomerState.selectItem)      ExecSelectItem();
        else if (currState == CustomerState.navigateToShelf) ExecNavigateToShelf();
        else if (currState == CustomerState.joinQueue)       ExecJoinQueue();
        else if (currState == CustomerState.waitInQueue)     ExecWaitInQueue();
        else if (currState == CustomerState.leaveStore)      ExecLeaveStore();
        else if (currState == CustomerState.walkOut)         ExecWalkOut();
    }

    // ── State handlers ────────────────────────────────────────

    void ExecWalkIn()
    {
        if (hasDoneInit) return;
        hasDoneInit    = true;
        isInProgressNav = true;
        owner.Mover.MoveTo(owner.TrEntrance.position, onArrived: () =>
        {
            isInProgressNav = false;
            GameEvents.RaiseCustomerEntered(owner);
            TransitionTo(CustomerState.selectItem);
        });
    }

    // runs every tick until list resolved
    void ExecSelectItem()
    {
        // list empty — head to queue
        if (owner.ShoppingList.Count == 0)
        {
            TransitionTo(CustomerState.joinQueue);
            return;
        }

        SO_ItemData desired = owner.ShoppingList[0];
        ShelfPOI    shelf   = POIRegistry.Ins.GetShelfWithItem(desired);

        if (shelf == null)
        {
            // out of stock — skip this item this visit
            owner.ShoppingList.RemoveAt(0);
            return; // re-evaluates next tick
        }

        owner.CurrentTargetItem  = desired;
        owner.CurrentTargetShelf = shelf;
        TransitionTo(CustomerState.navigateToShelf);
    }

    void ExecNavigateToShelf()
    {
        if (hasDoneInit) return;
        hasDoneInit = true;

        Transform slot = owner.CurrentTargetShelf.BookSlot(owner);
        if (slot == null)
        {
            // shelf slot taken by another NPC — retry next tick
            hasDoneInit = false;
            return;
        }

        isInProgressNav = true;
        owner.Mover.MoveTo(slot.position, onArrived: () =>
        {
            isInProgressNav = false;
            TransitionTo(CustomerState.takeItem);
        });
    }

    void ExecJoinQueue()
    {
        if (hasDoneInit) return;
        hasDoneInit = true;

        QueuePOI queue = POIRegistry.Ins.GetFirstAvailableQueueWithSlots();
        if (queue == null) { hasDoneInit = false; return; } // full — retry

        Transform slot = queue.BookSlot(owner);
        if (slot == null) { hasDoneInit = false; return; }  // race guard

        owner.CurrentQueue     = queue;
        owner.TrCurrentQueueSlot = slot;

        waitDuration = C.Random(owner.Profile.minQWaitSec, owner.Profile.maxQWaitSec);
        GameEvents.RaiseCustomerJoinedQ(owner);

        isInProgressNav = true;
        owner.Mover.MoveTo(slot.position, onArrived: () =>
        {
            isInProgressNav = false;
            waitTimer = 0f;
            TransitionTo(CustomerState.waitInQueue);
        });
    }

    void ExecWaitInQueue()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer < waitDuration) return;

        owner.CurrentQueue?.ReleaseSlot(owner);
        owner.CurrentQueue       = null;
        owner.TrCurrentQueueSlot = null;
        TransitionTo(CustomerState.leaveStore);
    }

    void ExecLeaveStore()
    {
        if (hasDoneInit) return;
        hasDoneInit = true;

        isInProgressNav = true;
        owner.Mover.MoveTo(owner.TrExit.position, onArrived: () =>
        {
            isInProgressNav = false;
            GameEvents.RaiseCustomerLeft(owner);
            TransitionTo(CustomerState.walkOut);
        });
    }

    void ExecWalkOut()
    {
        if (hasDoneInit) return;
        hasDoneInit = true;

        isInProgressNav = true;
        owner.Mover.MoveTo(owner.TrDespawn.position, onArrived: () =>
        {
            isInProgressNav = false;
            TransitionTo(CustomerState.done);
            Destroy(owner.gameObject);
        });
    }

    // ── TakeItem coroutine ────────────────────────────────────
    // started by TransitionTo when entering takeItem state
    IEnumerator RoutineTakeItem()
    {
        yield return new WaitForSeconds(0.75f); // grab pause

        // pass CurrentTargetItem so ShelfPOI finds the correct tier
        owner.CurrentTargetShelf?.TryTakeItem(owner.CurrentTargetItem);
        owner.CurrentTargetShelf?.ReleaseSlot(owner);

        if (owner.ShoppingList.Count > 0)
            owner.ShoppingList.RemoveAt(0);

        owner.CurrentTargetItem  = null;
        owner.CurrentTargetShelf = null;

        TransitionTo(CustomerState.selectItem);
    }

    // ── Transition ────────────────────────────────────────────
    void TransitionTo(CustomerState next)
    {
        if (currState == next) return;
        Debug.Log($"[FSM] {gameObject.name}: {currState} → {next}".colorTag("lime"));
        hasDoneInit = false;
        currState   = next;
        if (next == CustomerState.takeItem)
            StartCoroutine(RoutineTakeItem());
    }
}
```

---

### `CustomerSpawner.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

// ============================================================
//  CustomerSpawner.cs
//  Spawns CustomerAgent prefabs on an interval.
//  Builds a random shopping list from currently stocked items.
// ============================================================
public class CustomerSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] GameObject _pfCustomer;

    [Header("Profiles")]
    [SerializeField] List<SO_CustomerProfileData> _PROFILES;

    [Header("Waypoints")]
    [SerializeField] Transform _Tr_spawn;
    [SerializeField] Transform _Tr_entrance;
    [SerializeField] Transform _Tr_exit;
    [SerializeField] Transform _Tr_despawn;

    [Header("Config")]
    [SerializeField] int   _maxCustomers  = 10;
    [SerializeField] float _spawnInterval = 3f;
    [Range(1, 6)]
    [SerializeField] int   _maxItemsPerCustomer = 3;

    private void Start()
    {
        Debug.Log(C.method(this));
        StopAllCoroutines();
        StartCoroutine(RoutineSpawn());
    }

    int _spawnedCount = 0;

    IEnumerator RoutineSpawn()
    {
        // small delay so AutoStockService can finish populating shelves first
        yield return new WaitForSeconds(0.5f);

        while (C.Safe(100, "spawnLoop") && _spawnedCount < _maxCustomers)
        {
            SpawnOne();
            _spawnedCount++;
            yield return new WaitForSeconds(_spawnInterval);
        }
    }

    void SpawnOne()
    {
        GameObject go = Instantiate(_pfCustomer, _Tr_spawn.position, _Tr_spawn.rotation);
        go.name = $"Customer_{_spawnedCount:D2}";

        CustomerAgent agent = go.GetComponent<CustomerAgent>();
        if (agent == null)
        {
            Debug.LogError("[CustomerSpawner] Missing CustomerAgent!".colorTag("red"));
            Destroy(go); return;
        }

        SO_CustomerProfileData profile = _PROFILES.getRandom();

        agent.TrEntrance   = _Tr_entrance;
        agent.TrExit       = _Tr_exit;
        agent.TrDespawn    = _Tr_despawn;
        agent.ShoppingList = BuildShoppingList(profile);
        agent.ApplyProfileData(profile);

        Debug.Log($"[Spawner] {go.name} [{profile?.id}] " +
                  $"wants: {string.Join(", ", agent.ShoppingList.ConvertAll(i => i.itemName))}");
    }

    List<SO_ItemData> BuildShoppingList(SO_CustomerProfileData profile)
    {
        List<SO_ItemData> available = POIRegistry.Ins.GetAllStockedItems();
        if (available.Count == 0) return new List<SO_ItemData>();

        // Fisher-Yates shuffle
        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }

        int cap   = Mathf.Min(_maxItemsPerCustomer, available.Count);
        int count = UnityEngine.Random.Range(1, cap + 1);
        return available.GetRange(0, count);
    }
}
```

---

## 7. Full State Flow

```
[CustomerSpawner] Instantiate at _Tr_spawn
      │
  walkIn ──nav──► TrEntrance
      │  RaiseCustomerEntered
      │
  selectItem ◄────────────────────────────────────────────────┐
      │                                                        │
      ├─ ShoppingList[0] exists                                │
      │   └─ POIRegistry.GetShelfWithItem(item)                │
      │       ├─ found & slot free ─► navigateToShelf          │
      │       │       └─ BookSlot → nav → arrive               │
      │       │              └─ takeItem (coroutine 0.75s)      │
      │       │                   └─ TryTakeItem(item)          │
      │       │                   └─ ReleaseSlot               │
      │       │                   └─ ShoppingList.RemoveAt(0) ─┘
      │       └─ not found ─► skip item, re-evaluate
      │
      └─ ShoppingList empty
              └─► joinQueue ── BookSlot → nav → arrive
                      │  RaiseCustomerJoinedQ
                      │
                  waitInQueue (random minQWait–maxQWait sec)
                      │  ReleaseSlot
                      │
                  leaveStore ──nav──► TrExit
                      │  RaiseCustomerLeft
                      │
                  walkOut ──nav──► TrDespawn
                      │
                   Destroy
```

---

## 8. Homogeneous Tier Rule — Decision Table

| Tier state | Stock Milk | Stock Apple |
|---|---|---|
| Empty (free) | ✓ locked to Milk | ✓ locked to Apple |
| 3× Milk, not full | ✓ adds to Milk | ✗ rejected |
| 10× Milk (full) | ✗ full | ✗ full |
| 0× (was Milk, now empty) | ✓ relocked to Milk | ✓ relocked to Apple |

`GetTierForStocking(item)` priority:
1. Non-full tier already holding `item` (consolidate same item)
2. Completely empty (free) tier
3. `null` → `TryStockItem` warns and returns false

---

## 9. Setup Checklist

### A. NavMesh
1. `NavMesh Surface` on Level root → Humanoid → **Bake**.
2. Verify all `InteractionPoint` Transforms and `QueueSlot_N` Transforms
   are on the baked blue surface.

### B. ShelfTier
1. Add `ShelfTier` to `lowerShelf` and `upperShelf` children.
2. Set `capacity = 10`, `slotSpacing = 0.22`.
3. Enter Play — cyan gizmo cubes show slot positions.
   Adjust `slotSpacing` until items space evenly on the board.
4. Assign `slotPrefab` on each `SO_ItemData` asset (the `cubeModel x0.1` prefab).

### C. ShelfPOI
1. Add `ShelfPOI` to shelf root. No `SO_ItemData` field — items set at runtime.
2. Drag `lowerShelf` then `upperShelf` into `_TIERS` list (index = tier index).
3. Create child empty Transform `InteractionPoint` ~0.8m in front, ON NavMesh.

### D. AutoStockService
Add one `ShelfStockEntry` per shelf + tier + item combination. Example:
```
Shelf_A  tier 0 (lower)  Milk   3    ← 3/10 milk on lower
Shelf_A  tier 1 (upper)  Milk   1    ← 1/10 milk on upper
Shelf_B  tier 0 (lower)  Apple  5
Shelf_B  tier 1 (upper)  Bread  8    ← different items on same shelf ✓
```

### E. QueuePOI
`Queue_Main` → `QueuePOI`. Five child Transforms `QueueSlot_0`…`QueueSlot_4`
spaced ~0.7m apart. Assign to `_TR_QUEUE_SLOTS`. Slot 0 = front.

### F. Waypoints
| Name | Position | NavMesh? |
|---|---|---|
| SpawnPoint | Far outside | No |
| EntrancePoint | Just inside door | Yes |
| ExitPoint | Door threshold | Yes |
| DespawnPoint | Far outside | No |

### G. Customer Prefab
1. Root: `CustomerAgent`, `NavMeshMover`, `CustomerFSM`.
2. Serialized refs: drag `NavMeshMover` into `Mover`, `CustomerFSM` into `FSM`.
3. Child: `npcModel` (visuals only).
4. NavMeshAgent: radius `0.35`, avoidancePriority `50`.

### H. CustomerSpawner
Assign prefab, 4 waypoints, profiles list, `_maxItemsPerCustomer`.

---

## 10. Phase 2 Extension Map

| Phase 1 | Phase 2 hook |
|---|---|
| `AutoStockService` | Replaced by player `StockBox` → `IInteractable` → `ShelfPOI.TryStockItem(item)` |
| `GameEvents.OnTierCleared` | UI badge "needs restock" on shelf |
| `GameEvents.OnShelfRestocked` | UI stock count refresh |
| `ShelfPOI.GetTierForStocking` | Player stocking highlight via `POIRegistry.GetShelvesAccepting(item)` |
| `CustomerFSM` (MonoBehaviour) | Wrapped as `ScheduledAction_GoShopping : NPCAction` |
| `CustomerSpawner` | Replaced by `NPCScheduleManager` for persistent daily NPCs |
| `TrDespawn` | NPC walks home to `ScheduledAction_Sleep` bed POI |
| `IPOI` + `POIRegistry` | Beds, vending machines, dealer corners — zero FSM changes |
