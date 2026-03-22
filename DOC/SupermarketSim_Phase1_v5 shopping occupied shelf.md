# Supermarket Simulator — Phase 1 v5
> Shelf-busy defer · Multi-quantity shopping list · Code review fixes

---

## 1. Code Review of Current Implementation

### Issues found

**`QueuePOI.OnDrawGizmosSelected` — variable name inverted**
```csharp
// CURRENT — variable named isOccupied but checks for null (= FREE)
bool isOccupied = (DOC_OCCUPANTS[tr] == null);
Gizmos.color = (isOccupied) ? Color.red : Color.limeGreen;
// result: red = FREE slot, green = OCCUPIED — visually backwards
```
```csharp
// FIXED
bool isFree = (DOC_OCCUPANTS[tr] == null);
Gizmos.color = isFree ? Color.green : Color.red;
UnityEditor.Handles.Label(..., $"Q[{index}] {(isFree ? "" : "✓")}");
```

**`CustomerFSM.ExecStateNavigateToShelf` — `BookSlot` can return null, not guarded**
```csharp
// CURRENT — if BookSlot returns null (race condition), MoveTo gets null.position → exception
Transform trSlot = owner.currentShelfPOI.BookSlot(owner);
owner.Mover.MoveTo(trSlot.position.xz(), ...);

// FIXED — guard the null case
Transform trSlot = owner.currentShelfPOI.BookSlot(owner);
if (trSlot == null) { isStateCalledOnce = false; return; } // retry next tick
owner.Mover.MoveTo(trSlot.position.xz(), ...);
```

**`CustomerFSM.phaseDev` is `static`**
```csharp
// CURRENT — static means ALL customers share one phaseDev value
static int phaseDev = 1;
```
Fine for now as a dev toggle, but worth noting: changing it at runtime
switches every NPC simultaneously. Move to a `const` or `#if` directive
when you're done testing phase-0.

---

## 2. The Shelf-Busy Problem

### What happens now

`ExecStateSelectItem` calls `GetFirstShelfWithItemAndAvaiableNPCSlot`.
That returns `null` in two distinct situations — but the FSM treats them identically:

| Situation | What happens | What should happen |
|---|---|---|
| Shelf has stock, slot is occupied by another NPC | `RemoveAt(0)` — item lost | Defer, try other items, retry |
| Item is genuinely out of stock | `RemoveAt(0)` — correct | Remove all copies of this item |

The fix requires knowing **which** null case we're in.

---

## 3. Solution Design

### Two separate registry queries

```
GetFirstShelfWithItemAndAvailableNPCSlot(item)
  → stock exists AND slot is free  → proceed normally

AnyShelfHasStockOf(item)                          ← NEW
  → stock exists (slot state irrelevant)
  → used to distinguish "busy" from "out of stock"
```

### FSM defer logic

```
selectItem (runs every tick):

  list empty?
    └─► joinQueue

  desired = list[0]

  anyStockExists = AnyShelfHasStockOf(desired)

  if !anyStockExists:
    // truly gone — remove ALL copies of this item from list
    list.RemoveAll(i => i == desired)
    _deferCount = 0
    return  (re-evaluates next tick with next item)

  poi = GetFirstShelfWithItemAndAvailableNPCSlot(desired)

  if poi == null:
    // stock exists but shelf slot is busy
    _deferCount++

    if _deferCount >= list.Count:
      // tried every item, all blocked — wait, reset, retry next tick
      _deferCount = 0
      return

    // rotate: push current item to end, try next one
    list.Add(list[0])
    list.RemoveAt(0)
    return

  // found — reset defer counter, navigate
  _deferCount = 0
  owner.currentTargetItem = desired
  owner.currentShelfPOI = poi
  TransitionTo(navigateToShelf)
```

### Multi-quantity shopping list

`BuildShoppingList` currently produces unique items: `[Milk, Apple, Bread]`.
With multi-quantity it produces: `[Milk, Milk, Apple, Apple, Apple, Bread]`.

The FSM loop already handles this naturally — each pass through `selectItem`
buys one unit and pops one entry. No FSM changes needed.

When stock runs dry mid-purchase (wanted 3 Milk, only 2 left):
- After buying 2nd Milk, `list = [Milk, ...]`
- Next `selectItem`: `anyStockExists` = false → `RemoveAll(Milk)` → remaining
  Milk entries cleared, continues with next item type.

---

## 4. Updated Scripts

---

### `POIRegistry.cs`

```csharp
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

public class POIRegistry : MonoBehaviour
{
    public static POIRegistry Ins { get; private set; }

    private void Awake()
    {
        Debug.Log(C.method(this));
        POIRegistry.Ins = this;
        QUEUE = new List<QueuePOI>();
        SHELF = new List<ShelfPOI>();
    }

    // ── Queue ─────────────────────────────────────────────────
    List<QueuePOI> QUEUE;
    public void RegisterQ(QueuePOI poi)   => QUEUE.Add(poi);
    public void UnRegisterQ(QueuePOI poi) => QUEUE.Remove(poi);

    public QueuePOI GetFirstAvailableQueueWithSlots() =>
        QUEUE.find(poi => poi.HasSlotForBooking());

    // ── Shelf ─────────────────────────────────────────────────
    List<ShelfPOI> SHELF;
    public void RegisterShelf(ShelfPOI poi)   => SHELF.Add(poi);
    public void UnRegisterShelf(ShelfPOI poi) => SHELF.Remove(poi);

    // stock exists AND slot is free — used by FSM to navigate
    public ShelfPOI GetFirstShelfWithItemAndAvaiableNPCSlot(SO_ItemData itemData) =>
        SHELF.find(shelf => shelf.HasStockOfItem(itemData) && shelf.HasSlotForBooking());

    // stock exists regardless of slot state — used to distinguish busy vs out-of-stock
    public bool AnyShelfHasStockOf(SO_ItemData itemData) =>
        SHELF.findIndex(shelf => shelf.HasStockOfItem(itemData)) != -1;

    // all distinct items currently stocked across all shelves
    public List<SO_ItemData> GetAllStockedItemsOnShelves() =>
        SHELF
            .flatMap(poi => poi.GetStockedItems())
            .rmdup()
            .ToList();

    // all shelves that can accept at least one more of this item (Phase 2 stocking UI)
    public List<ShelfPOI> GetAllShelvesAvailableForStockingItem(SO_ItemData itemData) =>
        SHELF
            .refine(poi => poi.GetTierForStocking(itemData) != null)
            .ToList();
}
```

---

### `CustomerFSM.cs`

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

/*
## Phase-1 state flow:

  walkIn
    └─► selectItem ◄──────────────────────────────────────────┐
            │                                                  │
            ├─ item found, shelf free ──► navigateToShelf      │
            │                                └─► takeItem ─────┘
            │
            ├─ item has stock, shelf BUSY
            │     └─ _deferCount < list.Count → rotate list, retry next tick
            │     └─ _deferCount >= list.Count → all items blocked, wait + reset
            │
            ├─ item out of stock → RemoveAll that item from list, retry
            │
            └─ list empty ──► joinQueue
                                  └─► waitInQueue
                                          └─► leaveStore
                                                  └─► walkOut → Destroy
*/
public enum CustomerState
{
    walkIn,
    joinQueue,
    waitInQueue,
    leaveStore,
    walkOut,
    done,
    // phase-1
    selectItem,
    navigateToShelf,
    takeItem,
}

public class CustomerFSM : MonoBehaviour
{
    bool  isStateCalledOnce = false;
    bool  isInProgressNav   = false;
    float waitInQueueTimer    = 0f;
    float waitInQueueDuration = 10f;
    int   _deferCount         = 0;   // how many items deferred in current selectItem pass

    CustomerAgent owner;
    public CustomerState currState { get; private set; }

    public void Init(CustomerAgent agent)
    {
        owner     = agent;
        currState = CustomerState.walkIn;
    }

    public void Tick()
    {
        if (isInProgressNav) return;

             if (currState == CustomerState.walkIn)          ExecStateWalkIn();
        else if (currState == CustomerState.selectItem)      ExecStateSelectItem();
        else if (currState == CustomerState.navigateToShelf) ExecStateNavigateToShelf();
        else if (currState == CustomerState.takeItem)        ExecStateTakeItem();
        else if (currState == CustomerState.joinQueue)       ExecStateJoinQueue();
        else if (currState == CustomerState.waitInQueue)     ExecStateWaitInQueue();
        else if (currState == CustomerState.leaveStore)      ExecStateLeaveStore();
        else if (currState == CustomerState.walkOut)         ExecStateWalkOut();
        else if (currState == CustomerState.done)
            Debug.Log($"{owner.customerId} lifecycle complete".colorTag("lime"));
    }

    #region Exec States

    void ExecStateWalkIn()
    {
        if (isStateCalledOnce) return;
        isStateCalledOnce = true;

        isInProgressNav = true;
        owner.Mover.MoveTo(owner.TrEntrancePoint.position, onArrived: () =>
        {
            isInProgressNav = false;
            GameEvents.RaiseCustomerEntered(owner);
            TransitionTo(CustomerState.selectItem);
        });
    }

    // ── selectItem — runs every tick ──────────────────────────
    void ExecStateSelectItem()
    {
        // list cleared — head to queue
        if (owner.shoppingList.Count == 0)
        {
            _deferCount = 0;
            TransitionTo(CustomerState.joinQueue);
            return;
        }

        SO_ItemData desired = owner.shoppingList[0];

        // ── case 1: item genuinely out of stock ──────────────
        bool stockExists = POIRegistry.Ins.AnyShelfHasStockOf(desired);
        if (!stockExists)
        {
            Debug.Log($"[FSM] {owner.customerId}: {desired.id} out of stock — removing all copies".colorTag("orange"));
            owner.shoppingList.RemoveAll(i => i == desired);
            _deferCount = 0;
            return; // re-evaluate next tick
        }

        // ── case 2: stock exists, find a shelf with free slot ─
        ShelfPOI poi = POIRegistry.Ins.GetFirstShelfWithItemAndAvaiableNPCSlot(desired);

        if (poi == null)
        {
            // stock exists but every matching shelf slot is busy
            _deferCount++;

            if (_deferCount >= owner.shoppingList.Count)
            {
                // tried every item in list — all shelves blocked, wait a tick then reset
                Debug.Log($"[FSM] {owner.customerId}: all {owner.shoppingList.Count} items blocked — waiting".colorTag("yellow"));
                _deferCount = 0;
                return;
            }

            // rotate: push current item to end of list, try next one next tick
            SO_ItemData deferred = owner.shoppingList[0];
            owner.shoppingList.RemoveAt(0);
            owner.shoppingList.Add(deferred);
            Debug.Log($"[FSM] {owner.customerId}: {desired.id} shelf busy — deferred (count: {_deferCount})".colorTag("yellow"));
            return;
        }

        // ── case 3: found shelf with stock and free slot ──────
        _deferCount = 0;
        owner.currentTargetItem = desired;
        owner.currentShelfPOI   = poi;
        TransitionTo(CustomerState.navigateToShelf);
    }

    void ExecStateNavigateToShelf()
    {
        if (isStateCalledOnce) return;
        isStateCalledOnce = true;

        // BookSlot can return null if another NPC grabbed the slot between
        // selectItem and here (race condition between two agents)
        Transform trSlot = owner.currentShelfPOI.BookSlot(owner);
        if (trSlot == null)
        {
            // slot gone — go back to selectItem to find another shelf
            Debug.Log($"[FSM] {owner.customerId}: slot stolen — returning to selectItem".colorTag("orange"));
            isStateCalledOnce = false;
            owner.currentShelfPOI   = null;
            owner.currentTargetItem = null;
            TransitionTo(CustomerState.selectItem);
            return;
        }

        isInProgressNav = true;
        owner.Mover.MoveTo(trSlot.position.xz(), onArrived: () =>
        {
            isInProgressNav = false;
            TransitionTo(CustomerState.takeItem);
        });
    }

    void ExecStateTakeItem()
    {
        if (isStateCalledOnce) return;
        isStateCalledOnce = true;
        StartCoroutine(RoutineTakeItem());
    }

    IEnumerator RoutineTakeItem()
    {
        yield return new WaitForSeconds(1f); // grab pause

        Debug.Log(C.method(this, "magenta"));
        owner.currentShelfPOI.TryTakeItem(owner.currentTargetItem);
        owner.currentShelfPOI.ReleaseSlot(owner);

        // pop exactly one entry — handles multi-quantity naturally
        if (owner.shoppingList.Count > 0)
            owner.shoppingList.RemoveAt(0);

        LOG.AddLog(owner.shoppingList.ToTable(
            name: $"LIST<> after take, {owner.customerId}"));

        owner.currentTargetItem = null;
        owner.currentShelfPOI   = null;
        TransitionTo(CustomerState.selectItem);
    }

    void ExecStateJoinQueue()
    {
        if (isStateCalledOnce) return;
        isStateCalledOnce = true;

        QueuePOI poi = POIRegistry.Ins.GetFirstAvailableQueueWithSlots();
        if (poi == null) { isStateCalledOnce = false; return; } // full — retry

        Transform slot = poi.BookSlot(owner);
        if (slot == null) { isStateCalledOnce = false; return; } // race guard

        owner.currentQueuePOI    = poi;
        owner.TrCurrentQueueSlot = slot;

        waitInQueueDuration = C.Random(owner.profileData.minQWaitSec,
                                       owner.profileData.maxQWaitSec);
        isInProgressNav = true;
        owner.Mover.MoveTo(slot.position.xz(), onArrived: () =>
        {
            isInProgressNav = false;
            GameEvents.RaiseCustomerJoinedQ(owner);
            waitInQueueTimer = 0f;
            TransitionTo(CustomerState.waitInQueue);
        });
    }

    void ExecStateWaitInQueue()
    {
        waitInQueueTimer += Time.deltaTime;
        if (waitInQueueTimer < waitInQueueDuration) return;

        owner.currentQueuePOI.ReleaseSlot(owner);
        owner.currentQueuePOI    = null;
        owner.TrCurrentQueueSlot = null;
        TransitionTo(CustomerState.leaveStore);
    }

    void ExecStateLeaveStore()
    {
        if (isStateCalledOnce) return;
        isStateCalledOnce = true;

        isInProgressNav = true;
        owner.Mover.MoveTo(owner.TrExitPoint.position, onArrived: () =>
        {
            isInProgressNav = false;
            GameEvents.RaiseCustomerLeft(owner);
            TransitionTo(CustomerState.walkOut);
        });
    }

    void ExecStateWalkOut()
    {
        if (isStateCalledOnce) return;
        isStateCalledOnce = true;

        isInProgressNav = true;
        owner.Mover.MoveTo(owner.TrDespawnPoint.position, onArrived: () =>
        {
            isInProgressNav = false;
            TransitionTo(CustomerState.done);
            GameObject.Destroy(owner.gameObject, t: 0.2f);
        });
    }

    #endregion

    void TransitionTo(CustomerState next)
    {
        if (currState == next) return;
        Debug.Log($"[FSM] {owner.customerId}: {currState} → {next}".colorTag("lime"));
        isStateCalledOnce = false;
        currState = next;
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

public class CustomerSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject pfCustomer;

    [Header("Profiles")]
    [SerializeField] List<SO_CustomerProfileData> _PROFILE;

    [Header("Waypoints")]
    public Transform _Tr_spawnPoint;
    public Transform _Tr_entrancePoint;
    public Transform _Tr_exitPoint;
    public Transform _Tr_despawnPoint;

    [Header("Config")]
    [SerializeField] int   _spawnCustomerCount  = 5;
    [SerializeField] float _spawnInterval       = 2f;
    [SerializeField] int   _maxItemTypes        = 3;  // distinct item types per customer
    [SerializeField] int   _maxQtyPerItemType   = 3;  // max units of each type wanted

    private void Start()
    {
        Debug.Log(C.method(this));
        StopAllCoroutines();
        StartCoroutine(RoutineSpawn());
    }

    int spawnedCount = 0;

    IEnumerator RoutineSpawn()
    {
        // wait for AutoStockService to finish populating shelves
        yield return new WaitForSeconds(0.5f);

        while (C.Safe(_spawnCustomerCount, "spawnLoop"))
        {
            SpawnOne();
            yield return new WaitForSeconds(_spawnInterval);
        }
    }

    void SpawnOne()
    {
        GameObject go = GameObject.Instantiate(pfCustomer,
                                               _Tr_spawnPoint.position,
                                               _Tr_spawnPoint.rotation);
        go.name = $"customer-{spawnedCount:D3}";
        spawnedCount += 1;

        CustomerAgent agent = go.GetComponent<CustomerAgent>();
        if (agent == null)
        {
            Debug.LogError("[CustomerSpawner] Missing CustomerAgent!".colorTag("red"));
            Destroy(go); return;
        }

        SO_CustomerProfileData profile = _PROFILE.getRandom();

        agent.customerId        = go.name;
        agent.TrEntrancePoint   = _Tr_entrancePoint;
        agent.TrExitPoint       = _Tr_exitPoint;
        agent.TrDespawnPoint    = _Tr_despawnPoint;
        agent.shoppingList      = BuildShoppingList();
        agent.ApplyProfileData(profile);

        LOG.AddLog(agent.shoppingList.ToTable(
            name: $"LIST<> shopping list on spawn, {agent.customerId}"));

        Debug.Log($"[Spawner] {go.name} [{profile?.id}] " +
                  $"list: {agent.shoppingList.Count} items, " +
                  $"wait: {profile?.minQWaitSec}–{profile?.maxQWaitSec}s");
    }

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
```

---

### `QueuePOI.cs` — gizmo fix only

```csharp
private void OnDrawGizmosSelected()
{
    if (DOC_OCCUPANTS == null) return;

    _TR_QUEUE_SLOTS.forEach((tr, index) =>
    {
        if (tr == null) return;
        bool isFree = (DOC_OCCUPANTS[tr] == null); // null = FREE (fixed name)
        Gizmos.color = isFree ? Color.green : Color.red;
        Gizmos.DrawWireCube(tr.position, Vector3.one * 0.3f);
        UnityEditor.Handles.Label(
            tr.position + Vector3.up * 0.5f,
            $"Q[{index}] {(isFree ? "" : "✓")}");
    });
}
```

---

## 5. Full selectItem Decision Flow

```
ExecStateSelectItem() — runs every Tick() while in selectItem state
         │
         ▼
  shoppingList.Count == 0?
         │
         YES ──► TransitionTo(joinQueue)
         │
         NO
         ▼
  desired = shoppingList[0]
         │
         ▼
  AnyShelfHasStockOf(desired)?
         │
         NO ──► RemoveAll(desired) from list  ← all copies gone
         │      _deferCount = 0
         │      return  (re-evaluate next tick)
         │
         YES
         ▼
  GetFirstShelfWithItemAndAvailableNPCSlot(desired)?
         │
         NULL ──► shelf exists, slot BUSY
         │            _deferCount++
         │            _deferCount >= list.Count?
         │                YES → wait, _deferCount = 0, return
         │                NO  → rotate list (push [0] to end), return
         │
         NOT NULL
         ▼
  _deferCount = 0
  currentTargetItem = desired
  currentShelfPOI   = poi
  TransitionTo(navigateToShelf)
```

---

## 6. Multi-Quantity Behaviour Trace

Shopping list built by spawner: `[Milk, Milk, Apple, Apple, Apple]`

```
tick  state           list                        action
──────────────────────────────────────────────────────────────
  1   selectItem      [Milk, Milk, Apple×3]       finds Milk shelf → navigate
  2   takeItem        [Milk, Milk, Apple×3]        grabs Milk
  3   selectItem      [Milk, Apple, Apple, Apple]  finds Milk shelf → navigate
  4   takeItem        [Milk, Apple×3]              grabs Milk ← 2nd unit taken
  5   selectItem      [Apple, Apple, Apple]        Milk stock now 0
                                                   AnyShelfHasStockOf(Milk)=false?
                                                   YES still has Milk → navigate
                      (or if Milk is now 0)
                      AnyShelfHasStockOf(Milk)=false → RemoveAll(Milk)
  6   selectItem      [Apple, Apple, Apple]        finds Apple shelf → navigate
  7   takeItem        [Apple, Apple, Apple]        grabs Apple (1st)
  8   selectItem      [Apple, Apple]               grabs Apple (2nd)
  9   selectItem      [Apple]                      grabs Apple (3rd)
 10   selectItem      []                           list empty → joinQueue
```

---

## 7. Shelf-Busy Defer Trace

Two customers. Both want Milk. Only one shelf with Milk. Only one NPC slot.

```
Customer_A enters first:
  selectItem: finds Milk shelf free → BookSlot → navigateToShelf

Customer_B enters second:
  selectItem: AnyShelfHasStockOf(Milk) = true ✓
              GetFirstShelfWithItem... = null  (slot taken by A)
              _deferCount = 1
              list = [Milk, Apple] → rotate → [Apple, Milk]
              return

  next tick: desired = Apple
              finds Apple shelf free → navigate → takes Apple
              list = [Milk]

  next tick: desired = Milk
              slot still taken by A?
                YES: _deferCount = 1, list count = 1
                     _deferCount >= list.Count → wait, reset
                NO:  A finished, slot free → navigate → takes Milk
```

---

## 8. What Remains Unchanged

All other scripts (`SO_ItemData`, `SO_CustomerProfileData`, `GameEvents`,
`IPOI`, `ShelfPOI`, `ShelfTier`, `CustomerAgent`, `NavMeshMover`,
`AutoStockService`) are correct as uploaded. No changes needed.
