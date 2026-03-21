# Supermarket Simulator — Phase 1 Complete Implementation
> Unity 6000.3+ · NavMesh AI Navigation 2.0 · No tight coupling · State Machine NPCs  
> v3 — ShelfTier grid stocking · CustomerProfileData SO · Store open/close · Spawn/despawn waypoints

---

## 1. Design Decisions

### Shop open / close behaviour
When the store **closes**, no new customers enter. Customers already inside the store
**finish being served** — they complete their shopping list, stand in queue, pay, then
leave normally. The door only locks once the last inside customer has departed.

Rationale: yanking an NPC mid-FSM requires interrupt handling across every state.
Letting them finish is cheaper, more realistic, and sets up the Phase 2 "last customer
out, lock up" end-of-day ritual naturally.

### Spawn / despawn positions
- `SpawnPoint` — far outside store (off-screen or behind a building corner). NPC
  materialises here.
- `EntrancePoint` — just inside the store entrance, ON the NavMesh. First nav target.
- `ExitPoint` — door threshold (outside, ON or near NavMesh). NPC walks here when
  leaving.
- `DespawnPoint` — far outside store, same side as SpawnPoint or opposite. NPC is
  destroyed here silently, out of camera view.

This gives the illusion customers walk in from somewhere and disappear to somewhere,
not teleport in/out mid-screen.

### Stock model
Two layers:

| Layer | Owner | What it is |
|---|---|---|
| Logical | `ShelfPOI` | Integer count, queried by FSM and registry |
| Physical | `ShelfTier` | Array of slot GameObjects, spawned/destroyed on the shelf surface |

`ShelfPOI` aggregates across tiers. `ShelfTier` computes slot positions procedurally
from the shelf board's world space — no pre-placed Transforms needed, no overlap.

---

## 2. Scene Hierarchy

```
SupermarketScene
├── _Systems
│   ├── POIRegistry           [POIRegistry.cs]
│   ├── StoreManager          [StoreManager.cs]
│   ├── AutoStockService      [AutoStockService.cs]
│   └── CustomerSpawner       [CustomerSpawner.cs]
│
├── NavMesh
│   └── NavMeshSurface        [NavMeshSurface — bake at edit time]
│
├── Level
│   ├── Plane (100×100)
│   │
│   ├── Shelves
│   │   ├── Shelf_Produce     [ShelfPOI: Item_Apple · tiers: lowerShelf, upperShelf]
│   │   │   ├── shelfModel
│   │   │   │   ├── lowerBoard
│   │   │   │   ├── lowerShelf   (y=0.2)   [ShelfTier: capacity=10, spacing=0.22]
│   │   │   │   ├── upperBoard
│   │   │   │   └── upperShelf   (y=1.0)   [ShelfTier: capacity=10, spacing=0.22]
│   │   │   └── InteractionPoint           (empty Transform, ~0.8m in front)
│   │   ├── Shelf_Dairy       [ShelfPOI: Item_Milk]
│   │   │   └── ... (same structure)
│   │   ├── Shelf_Bakery      [ShelfPOI: Item_Bread]
│   │   │   └── ...
│   │   └── Shelf_Drinks      [ShelfPOI: Item_Soda]
│   │       └── ...
│   │
│   └── Queue
│       └── Queue_Main        [QueuePOI — 5 slots]
│           ├── QueueSlot_0   (front of queue — closest to counter)
│           ├── QueueSlot_1
│           ├── QueueSlot_2
│           ├── QueueSlot_3
│           └── QueueSlot_4
│
├── Waypoints
│   ├── SpawnPoint            (far outside store — NPC birth point)
│   ├── EntrancePoint         (just inside store, ON NavMesh)
│   ├── ExitPoint             (door threshold, ON or near NavMesh)
│   └── DespawnPoint          (far outside store — NPC destroy point, out of view)
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
│   │   ├── ItemData.cs
│   │   └── CustomerProfileData.cs
│   ├── Events/
│   │   └── GameEvents.cs
│   ├── POI/
│   │   ├── IPOI.cs
│   │   ├── POIRegistry.cs
│   │   ├── ShelfPOI.cs
│   │   ├── ShelfTier.cs            ← NEW
│   │   └── QueuePOI.cs
│   ├── NPC/
│   │   ├── NavMeshMover.cs
│   │   ├── ShoppingFSM.cs
│   │   └── CustomerAgent.cs
│   ├── Spawner/
│   │   └── CustomerSpawner.cs
│   └── Store/
│       ├── StoreManager.cs
│       └── AutoStockService.cs
│
└── ScriptableObjects/
    ├── Items/
    │   ├── Item_Apple.asset
    │   ├── Item_Milk.asset
    │   ├── Item_Bread.asset
    │   └── Item_Soda.asset
    └── CustomerProfiles/
        ├── Profile_Regular.asset
        ├── Profile_Hurried.asset
        └── Profile_Browser.asset
```

---

## 4. ScriptableObjects

**Items** — Create → Supermarket → Item Data

| Asset       | itemName | basePrice | displayColor         |
|-------------|----------|-----------|----------------------|
| Item_Apple  | Apple    | 1.20      | (0.9, 0.2, 0.2, 1)   |
| Item_Milk   | Milk     | 2.50      | (0.95, 0.95, 1, 1)   |
| Item_Bread  | Bread    | 1.80      | (0.8, 0.65, 0.3, 1)  |
| Item_Soda   | Soda     | 1.50      | (0.2, 0.6, 0.9, 1)   |

**Customer Profiles** — Create → Supermarket → Customer Profile

| Asset            | profileName | walkSpeed | maxItems | queueWaitSec |
|------------------|-------------|-----------|----------|--------------|
| Profile_Regular  | Regular     | 3.5       | 3        | 10.0         |
| Profile_Hurried  | Hurried     | 5.0       | 1        | 6.0          |
| Profile_Browser  | Browser     | 2.0       | 5        | 15.0         |

---

## 5. Scripts

---

### `ItemData.cs`

```csharp
// ============================================================
//  ItemData.cs
//  ScriptableObject — defines a store product.
//  Pure data, no runtime state, no MonoBehaviour.
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Supermarket/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName  = "Item";
    public float  basePrice = 1.0f;

    [Header("Prototype Visuals")]
    [Tooltip("Tints the spawned item cube on the shelf for quick prototype ID.")]
    public Color displayColor = Color.white;
}
```

---

### `CustomerProfileData.cs`

```csharp
// ============================================================
//  CustomerProfileData.cs
//  ScriptableObject — one asset per customer archetype.
//  Assigned to CustomerAgent by spawner at instantiation time.
//  No runtime state. Pure data, same pattern as ItemData.
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "CustomerProfile", menuName = "Supermarket/Customer Profile")]
public class CustomerProfileData : ScriptableObject
{
    [Header("Identity")]
    public string profileName = "Regular";

    [Header("Movement")]
    [Tooltip("Passed to NavMeshMover.ApplyProfile at spawn time.")]
    public float walkSpeed = 3.5f;

    [Header("Shopping Behaviour")]
    [Tooltip("Maximum distinct items this archetype will add to its shopping list.")]
    [Range(1, 8)]
    public int maxItems = 3;

    [Tooltip("Seconds spent waiting in queue before the auto-purchase fires.")]
    public float queueWaitSeconds = 10f;

    [Header("NavMesh Avoidance")]
    [Tooltip("Lower = higher priority. Customers default 50; employees 20.")]
    [Range(0, 99)]
    public int avoidancePriority = 50;
}
```

---

### `GameEvents.cs`

```csharp
// ============================================================
//  GameEvents.cs
//  Static event bus — the ONLY cross-system communication layer.
//  Systems fire events here. Other systems subscribe here.
//  No system ever holds a direct typed reference to another.
// ============================================================
using System;

public static class GameEvents
{
    // ── Store lifecycle ──────────────────────────────────────

    public static event Action OnStoreOpened;
    /// Fires after AutoStockService finishes filling shelves; CustomerSpawner starts on this.
    public static void StoreOpened() => OnStoreOpened?.Invoke();

    public static event Action OnStoreClosed;
    /// Fires when the player (or timer) closes the store; stops new customer spawns.
    public static void StoreClosed() => OnStoreClosed?.Invoke();

    public static event Action OnStoreEmpty;
    /// Fires when the last in-store customer departs after close; used for end-of-day logic.
    public static void StoreEmpty() => OnStoreEmpty?.Invoke();

    // ── Shelf ────────────────────────────────────────────────

    public static event Action<ShelfPOI, ItemData> OnItemTakenFromShelf;
    /// Fires when an NPC removes one physical unit from a shelf tier.
    public static void ItemTakenFromShelf(ShelfPOI shelf, ItemData item)
        => OnItemTakenFromShelf?.Invoke(shelf, item);

    public static event Action<ShelfPOI, ItemData, int> OnShelfRestocked;
    /// Fires when stock is added to a shelf (AutoStockService or player stocking).
    public static void ShelfRestocked(ShelfPOI shelf, ItemData item, int amount)
        => OnShelfRestocked?.Invoke(shelf, item, amount);

    // ── Customers ────────────────────────────────────────────

    public static event Action<CustomerAgent> OnCustomerEntered;
    /// Fires when a customer crosses the entrance waypoint into the store.
    public static void CustomerEntered(CustomerAgent c) => OnCustomerEntered?.Invoke(c);

    public static event Action<CustomerAgent> OnCustomerLeft;
    /// Fires when a customer exits through the door (before Destroy).
    public static void CustomerLeft(CustomerAgent c) => OnCustomerLeft?.Invoke(c);

    public static event Action<CustomerAgent> OnCustomerJoinedQueue;
    /// Fires when a customer books a queue slot and begins walking to it.
    public static void CustomerJoinedQueue(CustomerAgent c) => OnCustomerJoinedQueue?.Invoke(c);

    // ── Purchases ────────────────────────────────────────────

    public static event Action<CustomerAgent, ItemData, float> OnPurchaseCompleted;
    /// Phase 1: fires after queueWaitSeconds. Phase 2: fires on player cashier accept.
    public static void PurchaseCompleted(CustomerAgent c, ItemData item, float price)
        => OnPurchaseCompleted?.Invoke(c, item, price);
}
```

---

### `IPOI.cs`

```csharp
// ============================================================
//  IPOI.cs
//  Contract for any bookable world-space slot.
//  Shelves, queues, counters, beds — all implement this.
// ============================================================
using UnityEngine;

public interface IPOI
{
    string    POIId { get; }

    /// Returns true if at least one slot is unbooked.
    bool      HasAvailableSlot();

    /// Reserves the earliest free slot for this agent; returns its Transform or null if full.
    Transform BookSlot(CustomerAgent agent);

    /// Releases whichever slot this agent was holding.
    void      ReleaseSlot(CustomerAgent agent);
}
```

---

### `POIRegistry.cs`

```csharp
// ============================================================
//  POIRegistry.cs
//  Singleton query hub for all bookable world objects.
//  POIs self-register on OnEnable; FSM queries here, never
//  holds a direct named reference to any shelf or queue.
// ============================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class POIRegistry : MonoBehaviour
{
    public static POIRegistry Instance { get; private set; }

    private readonly List<ShelfPOI> _shelves = new();
    private readonly List<QueuePOI> _queues  = new();

    /// Enforces singleton; destroys duplicate on scene re-load.
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Registration (called by POIs on OnEnable / OnDisable) ──

    /// Adds shelf; called automatically by ShelfPOI.OnEnable.
    public void RegisterShelf(ShelfPOI s)   => _shelves.Add(s);
    /// Removes shelf; called automatically by ShelfPOI.OnDisable.
    public void UnregisterShelf(ShelfPOI s) => _shelves.Remove(s);

    /// Adds queue; called automatically by QueuePOI.OnEnable.
    public void RegisterQueue(QueuePOI q)   => _queues.Add(q);
    /// Removes queue; called automatically by QueuePOI.OnDisable.
    public void UnregisterQueue(QueuePOI q) => _queues.Remove(q);

    // ── Queries ─────────────────────────────────────────────

    /// Returns the first stocked shelf matching this item that also has a free NPC slot.
    public ShelfPOI GetShelfWithItem(ItemData item)
        => _shelves.FirstOrDefault(s =>
               s.ItemData == item && s.HasStock() && s.HasAvailableSlot());

    /// Returns a random shelf that has at least one unit in stock (used for browsing).
    public ShelfPOI GetRandomStockedShelf()
    {
        var stocked = _shelves.Where(s => s.HasStock()).ToList();
        return stocked.Count > 0 ? stocked[Random.Range(0, stocked.Count)] : null;
    }

    /// Returns the first queue with an unbooked slot.
    public QueuePOI GetQueueWithSlot()
        => _queues.FirstOrDefault(q => q.HasAvailableSlot());

    /// Returns distinct ItemData types currently in stock; used by spawner to build lists.
    public List<ItemData> GetAllStockedItems()
        => _shelves.Where(s => s.HasStock()).Select(s => s.ItemData).Distinct().ToList();
}
```

---

### `ShelfTier.cs`

```csharp
// ============================================================
//  ShelfTier.cs
//  Manages physical item visuals on one shelf board.
//  Computes slot positions procedurally — no pre-placed
//  Transforms needed. Slots are evenly spaced along the board's
//  local X axis, centered, guaranteed no overlap.
//
//  Attach to: lowerShelf child, upperShelf child (each board).
//
//  Fill order:  left → right  (index 0 to capacity-1)
//  Take order:  right → left  (rightmost filled slot first)
//               Gives natural "shelf empties from right" look.
// ============================================================
using UnityEngine;

public class ShelfTier : MonoBehaviour
{
    [Header("Grid")]
    [Tooltip("Max items on this board.")]
    public int   capacity    = 10;
    [Tooltip("World-unit gap between item centers. Match to item width + margin.")]
    public float slotSpacing = 0.22f;

    [Header("Item Visual")]
    [Tooltip("Assigned at runtime by ShelfPOI.SetStock; the small prototype cube prefab.")]
    public GameObject itemVisualPrefab;

    private GameObject[] _slots;   // null entry = empty slot
    private int          _occupied;

    // ── Read-only state ─────────────────────────────────────

    public int  Capacity  => capacity;
    /// Number of item visuals currently present on this board.
    public int  Occupied  => _occupied;
    /// True when at least one item is on this board.
    public bool HasItems  => _occupied > 0;
    /// True when all slots are filled.
    public bool IsFull    => _occupied >= capacity;

    // ── Initialisation ───────────────────────────────────────

    /// Destroys all existing visuals then spawns exactly `count` starting from slot 0.
    public void Initialize(int count, GameObject prefab)
    {
        itemVisualPrefab = prefab;

        if (_slots != null)
            foreach (var go in _slots)
                if (go != null) Destroy(go);

        _slots    = new GameObject[capacity];
        _occupied = 0;

        int toSpawn = Mathf.Min(count, capacity);
        for (int i = 0; i < toSpawn; i++)
            SpawnAt(i);

        _occupied = toSpawn;
    }

    // ── Slot position maths ──────────────────────────────────

    /// Returns the world-space center of slot i, centered along the board's local X axis.
    private Vector3 SlotPosition(int i)
    {
        float totalWidth = (capacity - 1) * slotSpacing;
        float localX     = -totalWidth * 0.5f + i * slotSpacing;
        // +0.1f lifts item to sit on top of the shelf surface (half of 0.2-tall cube)
        return transform.TransformPoint(new Vector3(localX, 0.1f, 0f));
    }

    // ── Internal spawn / destroy ─────────────────────────────

    /// Instantiates the item prefab at slot i, parented to this board Transform.
    private void SpawnAt(int i)
    {
        if (itemVisualPrefab == null) return;
        _slots[i] = Instantiate(
            itemVisualPrefab,
            SlotPosition(i),
            transform.rotation,
            transform
        );
        // Tint the visual if the prefab has a MeshRenderer (prototype cubes do)
        var mr = _slots[i].GetComponentInChildren<MeshRenderer>();
        if (mr != null && itemVisualPrefab.TryGetComponent<ItemData>(out _))
        {
            // Note: tinting is optional here; ShelfPOI can push color after Initialize
        }
    }

    // ── Public mutation ──────────────────────────────────────

    /// Destroys the rightmost filled item; returns false if board is empty.
    public bool RemoveOne()
    {
        if (_occupied == 0) return false;
        _occupied--;
        if (_slots[_occupied] != null)
        {
            Destroy(_slots[_occupied]);
            _slots[_occupied] = null;
        }
        return true;
    }

    /// Spawns an item at the next empty slot (left to right); returns false if full.
    public bool AddOne()
    {
        if (IsFull) return false;
        SpawnAt(_occupied);
        _occupied++;
        return true;
    }

    /// Destroys all item visuals and resets occupancy to zero.
    public void Clear()
    {
        if (_slots == null) return;
        foreach (var go in _slots)
            if (go != null) Destroy(go);
        System.Array.Clear(_slots, 0, _slots.Length);
        _occupied = 0;
    }

    // ── Optional: push item tint after Initialize ────────────

    /// Applies displayColor from ItemData to all spawned visuals on this tier.
    public void ApplyItemColor(Color color)
    {
        if (_slots == null) return;
        foreach (var go in _slots)
        {
            if (go == null) continue;
            var mr = go.GetComponentInChildren<MeshRenderer>();
            if (mr != null) mr.material.color = color;
        }
    }

    // ── Gizmos ───────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (capacity <= 0) return;
        for (int i = 0; i < capacity; i++)
        {
            bool filled = _slots != null && _slots.Length > i && _slots[i] != null;
            Gizmos.color = filled ? Color.yellow : new Color(0.3f, 0.9f, 0.9f, 0.4f);
            Gizmos.DrawWireCube(SlotPosition(i), new Vector3(0.18f, 0.18f, 0.18f));
        }
    }
#endif
}
```

---

### `ShelfPOI.cs`

```csharp
// ============================================================
//  ShelfPOI.cs
//  One shelf unit: one item type, N tiers, one NPC slot.
//  Aggregates stock across tiers. Self-registers with POIRegistry.
//
//  Tier fill order:  lower → upper  (fill from bottom first)
//  Item take order:  upper → lower  (take from top first — realistic)
//  Restock order:    lower → upper  (player fills from bottom up)
// ============================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShelfPOI : MonoBehaviour, IPOI
{
    [Header("Identity")]
    [SerializeField] private string _poiId;
    public string POIId => string.IsNullOrEmpty(_poiId) ? gameObject.name : _poiId;

    [Header("Item")]
    public ItemData ItemData;

    [Header("Tiers")]
    [Tooltip("Assign lowerShelf first, upperShelf second. Stock fills lower before upper.")]
    public List<ShelfTier> tiers = new();

    [Header("Item Visual Prefab")]
    [Tooltip("Prototype cube prefab passed to each ShelfTier. Use cubeModel x0.1.")]
    public GameObject itemVisualPrefab;

    [Header("Interaction")]
    [Tooltip("Empty Transform where NPC stands. Place ~0.8m in front of shelf face, ON NavMesh.")]
    public Transform interactionPoint;

    private CustomerAgent _occupant;

    // ── Aggregated stock ─────────────────────────────────────

    /// Total items currently present across all tiers.
    public int  StockCount    => tiers?.Sum(t => t.Occupied) ?? 0;
    /// Total slots across all tiers.
    public int  TotalCapacity => tiers?.Sum(t => t.Capacity) ?? 0;
    /// True if any tier has at least one item.
    public bool HasStock()    => tiers != null && tiers.Any(t => t.HasItems);

    // ── Lifecycle ────────────────────────────────────────────

    /// Self-registers with POIRegistry; waits one frame via OnEnable (guaranteed after Awake).
    private void OnEnable()
    {
        if (POIRegistry.Instance != null) POIRegistry.Instance.RegisterShelf(this);
    }
    /// Unregisters from POIRegistry when disabled or destroyed.
    private void OnDisable()
    {
        if (POIRegistry.Instance != null) POIRegistry.Instance.UnregisterShelf(this);
    }

    // ── IPOI ─────────────────────────────────────────────────

    /// Returns true when no NPC is currently interacting with this shelf.
    public bool HasAvailableSlot() => _occupant == null;

    /// Books this shelf for one agent; returns the interaction Transform or null if busy.
    public Transform BookSlot(CustomerAgent agent)
    {
        if (_occupant != null) return null;
        _occupant = agent;
        return interactionPoint != null ? interactionPoint : transform;
    }

    /// Clears this shelf's occupancy for the given agent.
    public void ReleaseSlot(CustomerAgent agent)
    {
        if (_occupant == agent) _occupant = null;
    }

    // ── Stock management ─────────────────────────────────────

    /// Distributes `total` units across tiers (lower first); spawns item visuals via ShelfTier.
    public void SetStock(int total)
    {
        if (tiers == null || tiers.Count == 0) return;
        int remaining = total;
        foreach (var tier in tiers)
        {
            int forTier = Mathf.Min(remaining, tier.Capacity);
            tier.Initialize(forTier, itemVisualPrefab);
            if (ItemData != null) tier.ApplyItemColor(ItemData.displayColor);
            remaining -= forTier;
            if (remaining <= 0) break;
        }
        GameEvents.ShelfRestocked(this, ItemData, total);
    }

    /// Removes one item from the topmost tier that has stock (upper first = natural look).
    public bool TryTakeItem()
    {
        if (tiers == null) return false;
        for (int i = tiers.Count - 1; i >= 0; i--)
        {
            if (!tiers[i].HasItems) continue;
            tiers[i].RemoveOne();
            GameEvents.ItemTakenFromShelf(this, ItemData);
            return true;
        }
        return false;
    }

    /// Adds `delta` units filling lower tier first, then upper. Fires restock event.
    public void AddStock(int delta)
    {
        if (tiers == null) return;
        int remaining = delta;
        foreach (var tier in tiers)
        {
            while (remaining > 0 && !tier.IsFull)
            {
                tier.AddOne();
                if (ItemData != null)
                {
                    // Re-tint the newly spawned item
                    var mr = tier.transform.GetChild(tier.Occupied - 1)
                                 .GetComponentInChildren<MeshRenderer>();
                    if (mr != null) mr.material.color = ItemData.displayColor;
                }
                remaining--;
            }
            if (remaining <= 0) break;
        }
        if (delta > 0) GameEvents.ShelfRestocked(this, ItemData, delta);
    }

    // ── Gizmos ───────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = HasStock() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.3f, 0.22f);

        if (interactionPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(interactionPoint.position, 0.18f);
            Gizmos.DrawLine(transform.position + Vector3.up, interactionPoint.position);
        }
    }
#endif
}
```

---

### `QueuePOI.cs`

```csharp
// ============================================================
//  QueuePOI.cs
//  Ordered list of Transform slots. Index 0 = front (counter).
//  Customers book the earliest free index; NPCs walk to that slot.
// ============================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QueuePOI : MonoBehaviour, IPOI
{
    [Header("Identity")]
    [SerializeField] private string _poiId;
    public string POIId => string.IsNullOrEmpty(_poiId) ? gameObject.name : _poiId;

    [Header("Slots")]
    [Tooltip("Ordered list — index 0 is front. Add empty child Transforms.")]
    public List<Transform> queueSlots = new();

    private readonly Dictionary<int, CustomerAgent> _occupants = new();

    /// Self-registers with POIRegistry on enable.
    private void OnEnable()
    {
        if (POIRegistry.Instance != null) POIRegistry.Instance.RegisterQueue(this);
    }
    /// Unregisters on disable.
    private void OnDisable()
    {
        if (POIRegistry.Instance != null) POIRegistry.Instance.UnregisterQueue(this);
    }

    // ── IPOI ─────────────────────────────────────────────────

    /// Returns true when booked count is less than total slot count.
    public bool HasAvailableSlot() => _occupants.Count < queueSlots.Count;

    /// Books the lowest free index; returns its Transform or null if queue is full.
    public Transform BookSlot(CustomerAgent agent)
    {
        for (int i = 0; i < queueSlots.Count; i++)
        {
            if (!_occupants.ContainsKey(i) && queueSlots[i] != null)
            {
                _occupants[i] = agent;
                return queueSlots[i];
            }
        }
        return null;
    }

    /// Frees the slot held by this agent.
    public void ReleaseSlot(CustomerAgent agent)
    {
        int key = _occupants.FirstOrDefault(kv => kv.Value == agent).Key;
        _occupants.Remove(key);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < queueSlots.Count; i++)
        {
            if (queueSlots[i] == null) continue;
            Gizmos.color = _occupants.ContainsKey(i) ? Color.red : Color.green;
            Gizmos.DrawWireCube(queueSlots[i].position, Vector3.one * 0.35f);
            if (i > 0 && queueSlots[i - 1] != null)
                Gizmos.DrawLine(queueSlots[i - 1].position, queueSlots[i].position);
            UnityEditor.Handles.Label(
                queueSlots[i].position + Vector3.up * 0.5f,
                $"Q{i}{(_occupants.ContainsKey(i) ? " ✓" : "")}");
        }
    }
#endif
}
```

---

### `NavMeshMover.cs`

```csharp
// ============================================================
//  NavMeshMover.cs
//  Callback-based thin wrapper around NavMeshAgent.
//  FSM calls MoveTo() with a completion Action.
//  FSM never polls IsMoving — it only reacts to callbacks.
// ============================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshMover : MonoBehaviour
{
    [Header("Defaults (overridden by profile at spawn)")]
    public float speed            = 3.5f;
    public float stoppingDistance = 0.25f;
    public float arrivalTimeout   = 20f;

    private NavMeshAgent _agent;
    private Coroutine    _trackCoroutine;

    /// Caches agent reference and applies default motion parameters.
    private void Awake()
    {
        _agent                  = GetComponent<NavMeshAgent>();
        _agent.speed            = speed;
        _agent.stoppingDistance = stoppingDistance;
        _agent.angularSpeed     = 360f;
        _agent.acceleration     = 14f;
    }

    /// True while the agent is actively moving toward a destination.
    public bool IsMoving =>
        _agent.enabled && !_agent.isStopped &&
        !_agent.pathPending && _agent.remainingDistance > _agent.stoppingDistance;

    /// Pushes walkSpeed and avoidancePriority from the customer's profile into the NavMeshAgent.
    public void ApplyProfile(CustomerProfileData profile)
    {
        if (profile == null) return;
        _agent.speed             = profile.walkSpeed;
        _agent.avoidancePriority = profile.avoidancePriority;
    }

    /// Sends the agent toward destination and fires onArrived once within stoppingDistance.
    public void MoveTo(Vector3 destination, Action onArrived = null)
    {
        CancelTracking();
        _agent.isStopped = false;
        _agent.SetDestination(destination);
        if (onArrived != null)
            _trackCoroutine = StartCoroutine(TrackArrival(onArrived));
    }

    /// Halts movement immediately and clears the current path.
    public void Stop()
    {
        CancelTracking();
        if (_agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); }
    }

    /// Enables or disables the NavMeshAgent (used when swapping to NavMeshObstacle).
    public void SetAgentEnabled(bool enabled) => _agent.enabled = enabled;

    /// Stops the arrival-tracking coroutine without affecting agent movement.
    private void CancelTracking()
    {
        if (_trackCoroutine != null) { StopCoroutine(_trackCoroutine); _trackCoroutine = null; }
    }

    /// Polls remainingDistance each frame; fires callback on arrival or after timeout.
    private IEnumerator TrackArrival(Action onArrived)
    {
        float elapsed = 0f;
        yield return null; // let agent compute path first frame

        while (elapsed < arrivalTimeout)
        {
            elapsed += Time.deltaTime;
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                Stop();
                onArrived?.Invoke();
                yield break;
            }
            yield return null;
        }

        Debug.LogWarning($"[NavMeshMover] Timeout — forcing callback for {gameObject.name}");
        Stop();
        onArrived?.Invoke();
    }
}
```

---

### `ShoppingFSM.cs`

```csharp
// ============================================================
//  ShoppingFSM.cs
//  Customer state machine. All NPC decision logic lives here.
//
//  State flow:
//    WalkIn → EnterStore → SelectItem ──(list empty)──► JoinQueue
//                 └──(item found)──► NavigateToShelf              │
//                                         │                  WaitInQueue
//                                       TakeItem                  │
//                                         │ (loop)          LeaveStore
//                                     SelectItem              │
//                                                          WalkOut → Despawn
//
//  _init flag  — reset on every TransitionTo; entry code runs exactly once per state visit.
//  _waitNav    — true while a NavMeshMover callback is pending; Tick() no-ops until cleared.
//
//  Store close contract:
//    When OnStoreClosed fires, FSM sets _acceptingNewCustomers = false on the spawner side.
//    Customers ALREADY in-store continue normally and complete their full journey.
//    The FSM has no knowledge of store open/close state — it just runs to Done.
// ============================================================
using System.Collections;
using UnityEngine;

public enum ShoppingState
{
    WalkIn,
    EnterStore,
    SelectItem,
    NavigateToShelf,
    TakeItem,
    JoinQueue,
    WaitInQueue,
    LeaveStore,
    WalkOut,
    Done
}

public class ShoppingFSM : MonoBehaviour
{
    private ShoppingState _state   = ShoppingState.WalkIn;
    private bool          _init    = false;
    private bool          _waitNav = false;
    private float         _timer   = 0f;

    private CustomerAgent _owner;
    public  ShoppingState  CurrentState => _state;

    /// Stores owner reference; called by CustomerAgent.Awake.
    public void Initialize(CustomerAgent owner) => _owner = owner;

    /// Called every Update by CustomerAgent; no-ops while waiting for nav callback.
    public void Tick()
    {
        if (_waitNav) return;
        switch (_state)
        {
            case ShoppingState.WalkIn:            OnWalkIn();            break;
            case ShoppingState.EnterStore:        OnEnterStore();        break;
            case ShoppingState.SelectItem:        OnSelectItem();        break;
            case ShoppingState.NavigateToShelf:   OnNavigateToShelf();   break;
            case ShoppingState.JoinQueue:         OnJoinQueue();         break;
            case ShoppingState.WaitInQueue:       OnWaitInQueue();       break;
            case ShoppingState.LeaveStore:        OnLeaveStore();        break;
            case ShoppingState.WalkOut:           OnWalkOut();           break;
        }
    }

    // ── State handlers ────────────────────────────────────────

    /// Entry: walks from SpawnPoint to EntrancePoint (the approach from outside).
    private void OnWalkIn()
    {
        if (_init) return; _init = true;
        if (_owner.entrancePoint != null)
        {
            _waitNav = true;
            _owner.Mover.MoveTo(_owner.entrancePoint.position, () =>
            {
                _waitNav = false;
                TransitionTo(ShoppingState.EnterStore);
            });
        }
        else TransitionTo(ShoppingState.EnterStore);
    }

    /// Entry: fires CustomerEntered event; customer is now "inside" the store.
    private void OnEnterStore()
    {
        if (_init) return; _init = true;
        GameEvents.CustomerEntered(_owner);
        TransitionTo(ShoppingState.SelectItem);
    }

    /// Runs each tick: finds a shelf for the first list item; goes to queue if list is empty.
    private void OnSelectItem()
    {
        if (_owner.ShoppingList.Count == 0)
        {
            TransitionTo(ShoppingState.JoinQueue);
            return;
        }

        ItemData desired = _owner.ShoppingList[0];
        ShelfPOI shelf   = POIRegistry.Instance.GetShelfWithItem(desired);

        if (shelf == null)
        {
            // Out of stock — skip this item this visit
            _owner.ShoppingList.RemoveAt(0);
            return; // re-evaluates next tick
        }

        _owner.CurrentTargetItem  = desired;
        _owner.CurrentTargetShelf = shelf;
        TransitionTo(ShoppingState.NavigateToShelf);
    }

    /// Entry: books shelf slot and walks to interaction point; retries if slot is taken.
    private void OnNavigateToShelf()
    {
        if (_init) return; _init = true;

        Transform slot = _owner.CurrentTargetShelf.BookSlot(_owner);
        if (slot == null) { _init = false; return; } // shelf busy, retry next tick

        _waitNav = true;
        _owner.Mover.MoveTo(slot.position, () =>
        {
            _waitNav = false;
            TransitionTo(ShoppingState.TakeItem);
        });
    }

    /// Entry: books a queue slot and walks to it; retries next tick if queue is full.
    private void OnJoinQueue()
    {
        if (_init) return; _init = true;

        QueuePOI queue = POIRegistry.Instance.GetQueueWithSlot();
        if (queue == null) { _init = false; return; }

        Transform slot = queue.BookSlot(_owner);
        if (slot == null) { _init = false; return; }

        _owner.CurrentQueue     = queue;
        _owner.CurrentQueueSlot = slot;
        GameEvents.CustomerJoinedQueue(_owner);

        _waitNav = true;
        _owner.Mover.MoveTo(slot.position, () =>
        {
            _waitNav = false;
            _timer   = 0f;
            TransitionTo(ShoppingState.WaitInQueue);
        });
    }

    /// Counts up to profile.queueWaitSeconds; fires PurchaseCompleted then leaves.
    private void OnWaitInQueue()
    {
        _timer += Time.deltaTime;
        float waitTime = _owner.Profile != null ? _owner.Profile.queueWaitSeconds : 10f;
        if (_timer < waitTime) return;

        _owner.CurrentQueue?.ReleaseSlot(_owner);
        float price = _owner.CurrentTargetItem != null ? _owner.CurrentTargetItem.basePrice : 0f;
        GameEvents.PurchaseCompleted(_owner, _owner.CurrentTargetItem, price);
        TransitionTo(ShoppingState.LeaveStore);
    }

    /// Entry: fires CustomerLeft, then walks to ExitPoint (the door threshold).
    private void OnLeaveStore()
    {
        if (_init) return; _init = true;

        GameEvents.CustomerLeft(_owner);

        if (_owner.exitPoint != null)
        {
            _waitNav = true;
            _owner.Mover.MoveTo(_owner.exitPoint.position, () =>
            {
                _waitNav = false;
                TransitionTo(ShoppingState.WalkOut);
            });
        }
        else TransitionTo(ShoppingState.WalkOut);
    }

    /// Entry: walks from ExitPoint to DespawnPoint then destroys the GameObject.
    private void OnWalkOut()
    {
        if (_init) return; _init = true;

        if (_owner.despawnPoint != null)
        {
            _waitNav = true;
            _owner.Mover.MoveTo(_owner.despawnPoint.position, () =>
            {
                _waitNav = false;
                TransitionTo(ShoppingState.Done);
                Destroy(_owner.gameObject);
            });
        }
        else
        {
            TransitionTo(ShoppingState.Done);
            Destroy(_owner.gameObject);
        }
    }

    // ── TakeItem coroutine ────────────────────────────────────

    /// Pauses for grab delay, removes one item from the shelf, pops from shopping list.
    private IEnumerator TakeItemRoutine()
    {
        yield return new WaitForSeconds(0.75f);
        _owner.CurrentTargetShelf?.TryTakeItem();
        _owner.CurrentTargetShelf?.ReleaseSlot(_owner);
        if (_owner.ShoppingList.Count > 0) _owner.ShoppingList.RemoveAt(0);
        TransitionTo(ShoppingState.SelectItem);
    }

    // ── Transition ────────────────────────────────────────────

    /// Changes state, resets _init, and starts TakeItemRoutine on entry to TakeItem.
    private void TransitionTo(ShoppingState next)
    {
        if (_state == next) return;
#if UNITY_EDITOR
        Debug.Log($"[FSM] {gameObject.name}: {_state} → {next}");
#endif
        _state = next;
        _init  = false;
        if (next == ShoppingState.TakeItem) StartCoroutine(TakeItemRoutine());
    }
}
```

---

### `CustomerAgent.cs`

```csharp
// ============================================================
//  CustomerAgent.cs
//  Pure data carrier + component owner for one customer NPC.
//  Holds all scene refs and runtime FSM state as HideInInspector
//  fields so the spawner and FSM can read/write freely with no
//  Inspector clutter. Update() delegates 100% to ShoppingFSM.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NavMeshMover))]
[RequireComponent(typeof(ShoppingFSM))]
public class CustomerAgent : MonoBehaviour
{
    // ── Profile (assigned by spawner) ────────────────────────
    [HideInInspector] public CustomerProfileData Profile;

    // ── Scene waypoints (assigned by spawner) ────────────────
    [HideInInspector] public Transform entrancePoint;
    [HideInInspector] public Transform exitPoint;
    [HideInInspector] public Transform despawnPoint;  // ← NEW: far outside, out of view

    // ── Shopping data (assigned by spawner, mutated by FSM) ──
    [HideInInspector] public List<ItemData> ShoppingList = new();

    // ── Runtime FSM state (written/read by ShoppingFSM only) ─
    [HideInInspector] public ItemData  CurrentTargetItem;
    [HideInInspector] public ShelfPOI  CurrentTargetShelf;
    [HideInInspector] public QueuePOI  CurrentQueue;
    [HideInInspector] public Transform CurrentQueueSlot;

    // ── Component refs ────────────────────────────────────────
    public NavMeshMover Mover { get; private set; }
    public ShoppingFSM  FSM   { get; private set; }

    /// Resolves component references and initialises the FSM with this agent as owner.
    private void Awake()
    {
        Mover = GetComponent<NavMeshMover>();
        FSM   = GetComponent<ShoppingFSM>();
        FSM.Initialize(this);
    }

    /// Pushes profile data (speed, avoidance priority) into NavMeshMover.
    public void ApplyProfile()
    {
        if (Profile != null) Mover.ApplyProfile(Profile);
    }

    /// Delegates update entirely to ShoppingFSM — agent has zero logic of its own.
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
        if (CurrentQueue != null)
        {
            UnityEditor.Handles.color = new Color(1, 0.4f, 0.1f);
            UnityEditor.Handles.DrawDottedLine(
                transform.position + Vector3.up,
                CurrentQueueSlot != null
                    ? CurrentQueueSlot.position + Vector3.up
                    : CurrentQueue.transform.position + Vector3.up, 3f);
        }
    }
#endif
}
```

---

### `StoreManager.cs`

```csharp
// ============================================================
//  StoreManager.cs
//  Price authority, revenue tracker, and open/close state owner.
//
//  Open / close contract:
//    IsOpen == true  → customers may enter (spawner checks this)
//    IsOpen == false → no new spawns; in-store customers finish naturally
//    OnStoreEmpty    → fires when last customer departs while IsOpen == false
//                      (end-of-day hook; Phase 2: trigger lock-up sequence)
// ============================================================
using UnityEngine;

public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private bool  _isOpen;
    public bool IsOpen => _isOpen;

    [Header("Revenue (read-only debug)")]
    [SerializeField] private float _totalRevenue;
    [SerializeField] private int   _totalSales;
    [SerializeField] private int   _customersInsideCount;

    public float TotalRevenue         => _totalRevenue;
    public int   TotalSales           => _totalSales;
    public int   CustomersInsideCount => _customersInsideCount;

    /// Enforces singleton; second instance is destroyed on scene re-load.
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnPurchaseCompleted += HandlePurchase;
        GameEvents.OnCustomerEntered   += HandleCustomerEntered;
        GameEvents.OnCustomerLeft      += HandleCustomerLeft;
    }

    private void OnDisable()
    {
        GameEvents.OnPurchaseCompleted -= HandlePurchase;
        GameEvents.OnCustomerEntered   -= HandleCustomerEntered;
        GameEvents.OnCustomerLeft      -= HandleCustomerLeft;
    }

    // ── Open / close ─────────────────────────────────────────

    /// Opens the store; fires StoreOpened only if not already open.
    public void OpenStore()
    {
        if (_isOpen) return;
        _isOpen = true;
        Debug.Log("[StoreManager] Store opened.");
        GameEvents.StoreOpened();
    }

    /// Closes the store to new customers. In-store customers finish normally.
    /// StoreEmpty fires automatically when the last customer leaves.
    public void CloseStore()
    {
        if (!_isOpen) return;
        _isOpen = false;
        Debug.Log("[StoreManager] Store closed — waiting for remaining customers to finish.");
        GameEvents.StoreClosed();

        // Edge case: closed with nobody inside (e.g. close before first spawn)
        if (_customersInsideCount <= 0)
        {
            Debug.Log("[StoreManager] Store empty on close.");
            GameEvents.StoreEmpty();
        }
    }

    // ── Event handlers ────────────────────────────────────────

    /// Increments in-store counter when a customer crosses the entrance.
    private void HandleCustomerEntered(CustomerAgent _) => _customersInsideCount++;

    /// Decrements counter; fires StoreEmpty if closed and all customers have left.
    private void HandleCustomerLeft(CustomerAgent _)
    {
        _customersInsideCount = Mathf.Max(0, _customersInsideCount - 1);
        if (!_isOpen && _customersInsideCount <= 0)
        {
            Debug.Log("[StoreManager] Last customer left after close — store empty.");
            GameEvents.StoreEmpty();
        }
    }

    /// Accumulates revenue and logs each completed sale.
    private void HandlePurchase(CustomerAgent customer, ItemData item, float price)
    {
        _totalRevenue += price;
        _totalSales++;
        Debug.Log($"[StoreManager] Sale: {item?.itemName ?? "?"} ${price:F2} | " +
                  $"Total ${_totalRevenue:F2} ({_totalSales} sales)");
    }

    // ── Price authority ───────────────────────────────────────

    /// Returns the current sell price for an item; override here for dynamic pricing.
    public float GetPrice(ItemData item) => item != null ? item.basePrice : 0f;

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(StoreManager))]
    public class Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var sm = (StoreManager)target;
            UnityEditor.EditorGUILayout.Space();
            if (!sm.IsOpen)
            {
                if (GUILayout.Button("Open Store")) sm.OpenStore();
            }
            else
            {
                if (GUILayout.Button("Close Store")) sm.CloseStore();
            }
        }
    }
#endif
}
```

---

### `AutoStockService.cs`

```csharp
// ============================================================
//  AutoStockService.cs
//  Fills shelf stock at game start, then signals StoreManager
//  to open (which in turn signals CustomerSpawner to begin).
//
//  Execution order:
//    Start() → StockAndOpen coroutine
//      → per-shelf SetStock (spawns item visuals via ShelfTier)
//      → yield null (settle frame — ensures all OnEnable registrations done)
//      → StoreManager.Instance.OpenStore()
//        → GameEvents.StoreOpened()
//          → CustomerSpawner begins spawning
//
//  Phase 2: replace stockOverrides list with a StoreLayoutData
//  ScriptableObject so designers configure stock without Play mode.
// ============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoStockService : MonoBehaviour
{
    [System.Serializable]
    public struct ShelfStockEntry
    {
        public ShelfPOI shelf;
        [Min(0)]
        [Tooltip("Total units to place across all tiers on this shelf.")]
        public int stockAmount;
    }

    [Header("Stock overrides")]
    [Tooltip("Leave empty to use each ShelfPOI's default stock (5 per tier = 10 total). " +
             "Add entries to override individual shelves.")]
    public List<ShelfStockEntry> stockOverrides = new();

    [Header("Default stock per shelf (used when no override entry exists)")]
    [Min(0)]
    public int defaultStockPerShelf = 10;

    [Header("Cosmetic stagger")]
    [Tooltip("Delay between stocking each shelf. Gives a visual fill effect at startup.")]
    public float staggerDelay = 0.06f;

    /// Kicks off stocking coroutine on Start — after all Awake/OnEnable calls complete.
    private void Start() => StartCoroutine(StockAndOpen());

    /// Stocks each registered shelf then opens the store via StoreManager.
    private IEnumerator StockAndOpen()
    {
        // Build override lookup
        var overrideMap = new Dictionary<ShelfPOI, int>();
        foreach (var entry in stockOverrides)
            if (entry.shelf != null)
                overrideMap[entry.shelf] = entry.stockAmount;

        // Stock all shelves known to POIRegistry
        // (POIRegistry populates via OnEnable, which runs before Start)
        if (POIRegistry.Instance != null)
        {
            // We need to stock ALL ShelfPOIs, not just those in overrideMap.
            // POIRegistry only exposes query methods, so we use FindObjectsOfType here
            // (acceptable at startup; not called per-frame).
            var allShelves = FindObjectsByType<ShelfPOI>(FindObjectsSortMode.None);
            foreach (var shelf in allShelves)
            {
                int amount = overrideMap.TryGetValue(shelf, out int ov)
                             ? ov
                             : defaultStockPerShelf;
                shelf.SetStock(amount);

                if (staggerDelay > 0f)
                    yield return new WaitForSeconds(staggerDelay);
            }
        }

        // One settle frame so all physical item visuals are in place
        yield return null;

        // Open the store — this triggers CustomerSpawner via GameEvents.OnStoreOpened
        if (StoreManager.Instance != null)
            StoreManager.Instance.OpenStore();
        else
            GameEvents.StoreOpened(); // fallback if StoreManager is absent
    }
}
```

---

### `CustomerSpawner.cs`

```csharp
// ============================================================
//  CustomerSpawner.cs
//  Spawns CustomerAgent prefabs on an interval.
//  Waits for GameEvents.OnStoreOpened before first spawn.
//  Stops spawning on GameEvents.OnStoreClosed.
//  In-store customers are NOT affected by close — they finish.
//
//  Spawn flow per customer:
//    Instantiate at SpawnPoint → assign profile, waypoints, list
//    → agent.ApplyProfile() → FSM begins WalkIn state automatically
// ============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject customerPrefab;

    [Header("Waypoints")]
    [Tooltip("Where the NPC appears — far outside, off-screen.")]
    public Transform spawnPoint;
    [Tooltip("Just inside the store entrance, ON NavMesh.")]
    public Transform entrancePoint;
    [Tooltip("Door threshold — NPC walks here on LeaveStore.")]
    public Transform exitPoint;
    [Tooltip("Far outside — NPC walks here then is destroyed.")]
    public Transform despawnPoint;

    [Header("Profiles")]
    [Tooltip("Pool of CustomerProfileData assets. One is chosen at random per spawn.")]
    public List<CustomerProfileData> profiles = new();

    [Header("Config")]
    [Tooltip("Maximum customers spawned this session.")]
    public int   maxCustomers        = 10;
    [Tooltip("Seconds between each spawn.")]
    public float spawnInterval       = 2.5f;
    [Tooltip("Max items per shopping list (capped by profile.maxItems).")]
    [Range(1, 8)]
    public int   maxItemsPerCustomer = 3;

    private int  _spawned        = 0;
    private bool _isOpen         = false;
    private bool _spawnCoroutineRunning = false;

    // ── Lifecycle ─────────────────────────────────────────────

    private void OnEnable()
    {
        GameEvents.OnStoreOpened += HandleStoreOpened;
        GameEvents.OnStoreClosed += HandleStoreClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnStoreOpened -= HandleStoreOpened;
        GameEvents.OnStoreClosed -= HandleStoreClosed;
    }

    /// Sets open flag and starts spawn loop when store opens.
    private void HandleStoreOpened()
    {
        _isOpen = true;
        if (!_spawnCoroutineRunning)
            StartCoroutine(SpawnLoop());
    }

    /// Sets close flag; running coroutine checks this and stops gracefully.
    private void HandleStoreClosed() => _isOpen = false;

    // ── Spawn loop ────────────────────────────────────────────

    /// Runs while store is open and spawn budget remains; exits cleanly on close.
    private IEnumerator SpawnLoop()
    {
        _spawnCoroutineRunning = true;
        yield return new WaitForSeconds(0.5f); // initial settle

        while (_spawned < maxCustomers && _isOpen)
        {
            SpawnOne();
            _spawned++;
            yield return new WaitForSeconds(spawnInterval);
        }

        _spawnCoroutineRunning = false;
    }

    // ── Single spawn ──────────────────────────────────────────

    /// Instantiates prefab at SpawnPoint, wires all refs, builds list, applies profile.
    private void SpawnOne()
    {
        if (customerPrefab == null)
        {
            Debug.LogError("[CustomerSpawner] No prefab assigned!"); return;
        }

        Vector3    pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject go = Instantiate(customerPrefab, pos, rot);
        go.name = $"Customer_{_spawned:D2}";

        CustomerAgent agent = go.GetComponent<CustomerAgent>();
        if (agent == null)
        {
            Debug.LogError("[CustomerSpawner] Prefab missing CustomerAgent!"); Destroy(go); return;
        }

        agent.Profile       = PickProfile();
        agent.entrancePoint = entrancePoint;
        agent.exitPoint     = exitPoint;
        agent.despawnPoint  = despawnPoint;
        agent.ShoppingList  = BuildList(agent.Profile);
        agent.ApplyProfile();

        Debug.Log($"[CustomerSpawner] Spawned {go.name} [{agent.Profile?.profileName ?? "?"}] " +
                  $"wants: {ListNames(agent.ShoppingList)}");
    }

    // ── Helpers ───────────────────────────────────────────────

    /// Picks a random CustomerProfileData from the pool; returns null if pool is empty.
    private CustomerProfileData PickProfile()
    {
        if (profiles == null || profiles.Count == 0) return null;
        return profiles[Random.Range(0, profiles.Count)];
    }

    /// Fisher-Yates shuffles the stocked item pool then slices up to min(profile.maxItems, maxItemsPerCustomer).
    private List<ItemData> BuildList(CustomerProfileData profile)
    {
        var available = POIRegistry.Instance?.GetAllStockedItems() ?? new List<ItemData>();
        if (available.Count == 0) return new List<ItemData>();

        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }

        int cap   = profile != null ? Mathf.Min(profile.maxItems, maxItemsPerCustomer)
                                    : maxItemsPerCustomer;
        int count = Random.Range(1, Mathf.Min(cap, available.Count) + 1);
        return available.GetRange(0, count);
    }

    /// Formats a shopping list as comma-separated item names for debug logging.
    private string ListNames(List<ItemData> items)
        => string.Join(", ", items.ConvertAll(i => i.itemName));
}
```

---

## 6. Employee Self-Avoidance

Unity NavMesh RVO handles agent separation automatically. No custom steering code is
needed — just configure agents correctly.

| Agent type        | `avoidancePriority` | `radius` | Notes |
|-------------------|---------------------|----------|-------|
| Employee (mobile) | 20                  | 0.40     | Customers yield to them |
| Customer          | 50                  | 0.35     | Yields to employees |
| Employee (idle)   | —                   | —        | Disable NavMeshAgent, enable NavMeshObstacle with carving |

```csharp
// EmployeeAgent.cs — stationary / mobile swap
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NavMeshObstacle))]
public class EmployeeAgent : MonoBehaviour
{
    private NavMeshAgent    _agent;
    private NavMeshObstacle _obstacle;

    private void Awake()
    {
        _agent            = GetComponent<NavMeshAgent>();
        _obstacle         = GetComponent<NavMeshObstacle>();
        _obstacle.carving = true;    // punches a hole so customers path around
        _obstacle.enabled = false;
    }

    /// Call when employee reaches post; carving activates so customers re-path around them.
    public void GoStationary()
    {
        _agent.isStopped  = true;
        _agent.enabled    = false;
        _obstacle.enabled = true;
    }

    /// Call when employee moves again; reverts to nav agent.
    public void GoMobile()
    {
        _obstacle.enabled = false;
        _agent.enabled    = true;
        _agent.isStopped  = false;
    }
}
```

**Why swap rather than run both?** `NavMeshAgent` and `NavMeshObstacle` with carving
cannot be active on the same GameObject simultaneously — Unity logs a conflict and
the carving silently does nothing.

---

## 7. Full State Flow Diagram

```
[CustomerSpawner]
      │  Instantiate at SpawnPoint
      ▼
  WalkIn ──nav──► EntrancePoint
      │
  EnterStore ── fires CustomerEntered
      │
  SelectItem ◄──────────────────────────────┐
      │ list[0] found in POIRegistry         │
      │                        list empty    │
      ├──(found)──► NavigateToShelf          │
      │                  │ BookSlot → walk   │
      │             TakeItem (coroutine)      │
      │                  │ RemoveOne + pop   │
      │                  └──────────────────►┘
      │
      └──(empty)──► JoinQueue ── BookSlot → walk
                        │
                   WaitInQueue (queueWaitSeconds)
                        │ fires PurchaseCompleted
                   LeaveStore ── fires CustomerLeft
                        │ walk to ExitPoint
                   WalkOut ── walk to DespawnPoint
                        │
                      Done ── Destroy
```

---

## 8. Store Open / Close Contract Summary

```
AutoStockService.Start()
    └── StockAndOpen coroutine
          └── shelf.SetStock() × N   (fills ShelfTier grids)
          └── StoreManager.OpenStore()
                └── GameEvents.StoreOpened()
                      └── CustomerSpawner.HandleStoreOpened()
                            └── SpawnLoop() begins

Player / timer calls StoreManager.CloseStore()
    └── GameEvents.StoreClosed()
          └── CustomerSpawner.HandleStoreClosed()
                └── _isOpen = false → SpawnLoop exits next iteration
          └── StoreManager checks: any customers inside?
                ├── yes → wait
                └── no  → GameEvents.StoreEmpty() immediately

Last customer departs after close
    └── GameEvents.CustomerLeft
          └── StoreManager.HandleCustomerLeft()
                └── _customersInsideCount == 0 && !IsOpen
                      └── GameEvents.StoreEmpty()  ← Phase 2 hooks here
```

---

## 9. Setup Checklist

### A. NavMesh
1. Add `NavMesh Surface` to `Level` root. Agent type → Humanoid. Click **Bake**.
2. Verify in Scene view (NavMesh overlay) that every `InteractionPoint` Transform and all
   `QueueSlot_N` Transforms land on the blue surface.

### B. ShelfTier components
1. On each `lowerShelf` and `upperShelf` child: add `ShelfTier`.
2. Set `capacity = 10`, `slotSpacing = 0.22`.
3. In Editor mode press **Play** briefly — cyan gizmo cubes show slot positions.
   Adjust `slotSpacing` until no two cubes overlap and all fit within the board width.

### C. ShelfPOI
1. On the shelf root: add `ShelfPOI`.
2. Drag `lowerShelf` → `upperShelf` into the `tiers` list (order matters).
3. Assign `ItemData` asset.
4. Assign `itemVisualPrefab` (the `cubeModel x0.1` prefab).
5. Create child empty Transform `InteractionPoint`, position ~0.8m in front, ON NavMesh.
   Assign to `interactionPoint`.

### D. QueuePOI
1. `Queue_Main` → `QueuePOI`. Create 5 child Transforms `QueueSlot_0`…`QueueSlot_4`
   spaced ~0.7m apart in a line facing the counter. Assign to `queueSlots` list.

### E. Waypoints
| Name | Position | On NavMesh? |
|---|---|---|
| SpawnPoint | Far outside (e.g. x=-20) | No — agent snaps |
| EntrancePoint | Just inside door | Yes |
| ExitPoint | Door threshold | Yes or very near |
| DespawnPoint | Far outside (opposite side or same as spawn) | No |

### F. Customer Prefab
1. Root → add `CustomerAgent`, `NavMeshMover`, `ShoppingFSM`.
2. Child → `npcModel` (visuals only, no scripts).
3. NavMeshAgent radius `0.35`, avoidancePriority `50`.
4. Save as Prefab asset.

### G. _Systems
1. `POIRegistry` — no config needed.
2. `StoreManager` — starts closed (`_isOpen = false`). Use the Inspector button to
   open/close manually during Play, or let `AutoStockService` call `OpenStore()`.
3. `AutoStockService` — assign override entries if needed, or leave empty for `defaultStockPerShelf = 10`.
4. `CustomerSpawner` — assign prefab, all 4 waypoints, profile list, `maxCustomers = 10`.

---

## 10. Phase 2 Extension Map

| Phase 1 | Phase 2 |
|---|---|
| `AutoStockService.SetStock()` | Player places `StockBox` → `IInteractable` → `ShelfPOI.AddStock()` |
| `StoreManager.OpenStore/Close` | Bound to player action or in-game clock |
| `GameEvents.OnStoreEmpty` | Triggers lock-up animation, day-end summary screen |
| `ShoppingFSM` (MonoBehaviour) | Wrapped as `ScheduledAction_GoShopping : NPCAction` |
| `CustomerSpawner` | Replaced by `NPCScheduleManager` for persistent daily NPCs |
| `DespawnPoint` | Replaced by NPC walking home to their `ScheduledAction_Sleep` bed POI |
| `IPOI` + `POIRegistry` | Beds, dealer corners, vending machines — zero FSM changes needed |
