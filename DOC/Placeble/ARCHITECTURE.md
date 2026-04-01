# ShopSim3D — Phase 2+ Full Architecture & Implementation Blueprint
> Target: Unity 6000.3+, AI Navigation 2.0, existing SPACE_UTIL conventions.
> This document is the complete spec. A future session implements from this alone.

---

## Canonical Terminology (lock these in)

| Term | Type | Description |
|---|---|---|
| `LooseItem` | runtime concept | Apple, milk, axe, lamp — stacks, no grid snap |
| `PlaceableStructure` | runtime concept | Shelf, table, belt, chair — grid or socket placed |
| `DeliveryBox` | runtime MonoBehaviour | Spawned box at delivery point, holds contents |
| `CarrySlot` | plain C# class | One of 5 inventory slots the player carries |
| `CarrySlotPayload` | plain C# sealed class hierarchy | What a slot contains (LooseStack or BoxPayload) |
| `PlacementGhost` | MonoBehaviour | Red/green preview object, separate prefab instance |
| `PlacementSocket` | MonoBehaviour | Child Transform on tables/structures that accepts snapping |
| `PlacementGrid` | singleton MonoBehaviour | World-space grid, cell occupancy map |
| `holdSocket` | Vector3 + Quaternion | Per-item offset when LooseItem is held by player |
| `reachRadius` | float (tiles) | Per-PlaceableDef max placement distance from player |
| `packagingUnit` | int | On SO_BoxDefinition — units per physical box |
| `GridRotation` | int (0–3) | 90° steps of PlaceableStructure on grid |

---

## ScriptableObject Hierarchy

```
SO_ItemDefinitionBase  (abstract ScriptableObject)
│   string id
│   string displayName
│   Sprite icon
│   float basePrice
│   GameObject worldDropPrefab      ← physical object in world (dropped on floor)
│   ShopCategory category           ← enum: Furniture / Food / Tools / Decor / Logistics
│
├── SO_LooseItemDef  : SO_ItemDefinitionBase
│       int stackLimit              ← 100 for lamp, 2 for axe, 1 for unique tools
│       Vector3 holdOffset          ← local offset from player hold anchor
│       Quaternion holdRotation     ← per-item hold rotation
│       Vector2Int gridDimensions   ← always (1,1) — loose items don't snap, but defined for completeness
│
└── SO_PlaceableDef  : SO_ItemDefinitionBase
        Vector2Int gridFootprint    ← XZ cell count e.g. (3,2) for a big table
        float worldHeight           ← for vertical clearance check
        float reachRadius           ← player reach in grid tiles (e.g. 5f for belt, 10f for shelf)
        PlacementMode placementMode ← enum: Grid | Socket
        int packPerBox              ← always 1 for structures
        GameObject ghostPrefab      ← translucent preview prefab (has Renderer, no collider)
        List<SO_PlacementRuleDef> rules  ← scriptable placement rules (see below)
        bool requiresNavMeshCarving ← hint: prefab should have NavMeshObstacle


SO_BoxDefinition  (ScriptableObject)
        SO_ItemDefinitionBase contentItemDef
        int packagingUnit           ← units per box (100 for milk, 1 for shelf)
        GameObject boxPrefab        ← physical box world prefab
        string displayLabel         ← "Milk Crate (100)", "Shelf Box"


SO_ShopCatalogEntry  (ScriptableObject)
        SO_ItemDefinitionBase itemDef
        SO_BoxDefinition boxDef
        bool isAvailable
        int minOrderQty             ← minimum purchase quantity


SO_ShopCatalog  (ScriptableObject)
        List<SO_ShopCatalogEntry> allEntries
        ── helper: GetByCategory(ShopCategory) → filtered list


SO_PlacementRuleDef  (abstract ScriptableObject)
        abstract bool Evaluate(Vector3Int cell, Vector2Int footprint, int rotation, PlacementGrid grid)
        ── implementations as concrete SOs (see Placement Rules section)
```

---

## System Map

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  SHOP SYSTEM                                                                │
│                                                                             │
│  ShopUI (MonoBehaviour)                                                     │
│    CatalogBrowser  ──reads──►  SO_ShopCatalog                               │
│    CartModel (plain C#)       Dictionary<SO_ShopCatalogEntry, int>          │
│    CartWidget (MonoBehaviour) renders CartModel, fires OnCheckoutPressed    │
│                                        │                                   │
│                                        ▼                                   │
│                              CheckoutService (MonoBehaviour)                │
│                                Validates funds (Phase 3 stub)               │
│                                Calculates box orders                        │
│                                Fires GameEvents.RaisePurchaseConfirmed()    │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                        GameEvents.OnPurchaseConfirmed
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│  DELIVERY SYSTEM                                                             │
│                                                                             │
│  DeliveryService (MonoBehaviour)                                            │
│    Listens OnPurchaseConfirmed                                              │
│    For each BoxOrder: spawn DeliveryBox prefab at _Tr_deliveryPoint         │
│    with stagger delay                                                       │
│    DeliveryBox (MonoBehaviour) holds SO_BoxDefinition + remainingCount      │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                        Player presses G near DeliveryBox
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│  CARRY / INVENTORY SYSTEM                                                   │
│                                                                             │
│  PlayerCarryController (MonoBehaviour)                                      │
│    CarrySlot[5]  ── each: CarrySlotPayload (LooseStack | BoxPayload)        │
│    activeSlotIndex (0–4)                                                    │
│    Handles: G (grab), F (drop one), Alt+F (drop box), scroll/1-5 (switch)  │
│    Fires GameEvents.RaiseCarrySlotChanged()                                 │
│                                                                             │
│  CarrySlotHUD (MonoBehaviour)                                               │
│    Listens OnCarrySlotChanged → redraws 5 slot widgets                      │
│    Each widget: icon sprite, quantity label, selection highlight            │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                     Player has PlaceableStructure in active slot
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│  PLACEMENT SYSTEM                                                           │
│                                                                             │
│  PlacementController (MonoBehaviour)                                        │
│    Activated when active CarrySlot holds a PlaceableStructure               │
│    Each frame:                                                              │
│      Raycast → snap to PlacementGrid cell or PlacementSocket                │
│      Validate → run SO_PlacementRuleDef list                                │
│      Update PlacementGhost (green / red / hidden)                          │
│    On confirm (LMB or E):                                                   │
│      PlacementGrid.OccupyCells()                                            │
│      Instantiate world prefab                                               │
│      Fires GameEvents.RaiseStructurePlaced()                                │
│      Deduct from CarrySlot                                                  │
│                                                                             │
│  PlacementGrid (MonoBehaviour singleton)                                    │
│    Dictionary<Vector3Int, PlacedObject> DOC_CELLS                          │
│    OccupyCells / FreeCells / IsEmpty / GetFootprintCells                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## CarrySlot Payload Model

```csharp
// Plain C# — no MonoBehaviour, no Unity dependencies.
// Lives in its own file: CarrySlotPayload.cs

public abstract class CarrySlotPayload { }

public sealed class LooseStack : CarrySlotPayload
{
    public SO_LooseItemDef   itemDef;
    public int               quantity;    // 1..stackLimit
}

public sealed class BoxPayload : CarrySlotPayload
{
    public SO_BoxDefinition  boxDef;
    public int               remaining;  // units still inside the box
}

public sealed class PlaceablePayload : CarrySlotPayload
{
    public SO_PlaceableDef   placeableDef;
    // always qty = 1; when placed, slot cleared
}
```

`PlayerCarryController` holds `CarrySlot[] _SLOTS = new CarrySlot[5]`
where each `CarrySlot` wraps a nullable `CarrySlotPayload`.

---

## Shop UI Architecture

### Components
```
ShopUI (root MonoBehaviour)
├── CatalogBrowser (MonoBehaviour)
│     [SerializeField] SO_ShopCatalog _catalog
│     Spawns CatalogItemWidget per entry in selected category
│     On item click → calls CartModel.AddQty(entry, 1)
│
├── CartModel (plain C# — instantiated by ShopUI)
│     Dictionary<SO_ShopCatalogEntry, int> _DOC_quantities
│     public void AddQty(entry, delta)
│     public void SetQty(entry, qty)
│     public void RemoveEntry(entry)
│     public event Action OnCartChanged
│
├── CartWidget (MonoBehaviour)
│     Listens CartModel.OnCartChanged → redraws line items
│     Each line: item name | [-10][-1][input field][+1][+10] | [x]
│     Input field → CartModel.SetQty on submit/change
│
└── CheckoutButton (MonoBehaviour)
      On click → CheckoutService.TryCheckout(cartModel)
```

### BoxOrder calculation (inside CheckoutService)
```
For each (entry, qty) in cartModel:
    int boxCount    = Mathf.CeilToInt(qty / entry.boxDef.packagingUnit)
    int lastBoxQty  = qty % entry.boxDef.packagingUnit
    if lastBoxQty == 0: lastBoxQty = entry.boxDef.packagingUnit
    Emit BoxOrder { boxDef, fullBoxCount = boxCount-1, lastBoxQty }
```

---

## Delivery System

### DeliveryService
```
Listens: GameEvents.OnPurchaseConfirmed(List<BoxOrder>)
Has:     [SerializeField] Transform _Tr_deliveryPoint
         [SerializeField] float _staggerDelay = 0.3f

On event:
  StartCoroutine(RoutineDeliverBoxes(orders))
  → for each full box: Instantiate(boxDef.boxPrefab, deliveryPoint)
                       box.Init(boxDef, packagingUnit)
  → for last box:      Instantiate + Init with lastBoxQty
  → yield WaitForSeconds(_staggerDelay) between each
```

### DeliveryBox (MonoBehaviour on box prefab)
```
Fields:
  SO_BoxDefinition _boxDef
  int _remaining
  bool _isOpen = false

public void Init(SO_BoxDefinition def, int count)

On player G press (proximity):
  → PlayerCarryController.TryPickupBox(this)
  → if slot available: transfer as BoxPayload, Destroy self

On player F press (while holding box in slot):
  → Eject one item as LooseItem worldDropPrefab at player feet
  → _remaining-- (if this were the box in world — actually on CarrySlot.BoxPayload.remaining)

On Alt+F:
  → Drop entire box as world object (re-instantiate boxPrefab with remaining count)
  → Clear CarrySlot
```

---

## Carry / Inventory System

### PlayerCarryController (MonoBehaviour)
```
Fields:
  CarrySlot[5] _SLOTS
  int _activeIndex = 0
  [SerializeField] Transform _Tr_holdAnchor   ← attach to camera child
  [SerializeField] float _grabRadius = 2f
  GameObject _heldVisual                       ← instantiated on slot change

Update:
  ScrollWheel → cycle _activeIndex
  Keys 1-5    → set _activeIndex directly
  G           → TryGrab()
  F           → TryDropOne()
  Alt+F       → TryDropBox()
  on _activeIndex change → RefreshHeldVisual()

TryGrab():
  OverlapSphere(_grabRadius) → find nearest IPickupable
  IPickupable.TryPickup(this) → mutates a CarrySlot, fires RaiseCarrySlotChanged

RefreshHeldVisual():
  Destroy(_heldVisual)
  payload = _SLOTS[_activeIndex].payload
  if LooseStack:   Instantiate(itemDef.worldDropPrefab) at _Tr_holdAnchor
                   offset by itemDef.holdOffset + itemDef.holdRotation
  if BoxPayload:   Instantiate(boxDef.boxPrefab) at _Tr_holdAnchor
  if PlaceablePayload: PlacementController.Activate(placeableDef)
  if null:             PlacementController.Deactivate()
```

### IPickupable (interface)
```csharp
public interface IPickupable
{
    bool CanPickup(PlayerCarryController carrier);
    bool TryPickup(PlayerCarryController carrier);  // mutates carrier's slot
}
```
Both `DroppedLooseItem` (spawned worldDropPrefab) and `DeliveryBox`
implement `IPickupable`. Controller never casts to concrete type.

---

## Placement System

### PlacementMode enum
```csharp
public enum PlacementMode { Grid, Socket }
```

### PlacementController (MonoBehaviour)
```
Activated by PlayerCarryController when active slot = PlaceablePayload.

Each frame (while active):
  1. RAYCAST
     Ray from camera forward
     if no hit within maxRayDist:   ghost.SetVisible(false); return
     Vector3 worldHit = hit.point

  2. SNAP
     if placeableDef.placementMode == Grid:
         Vector3Int cell = PlacementGrid.WorldToCell(worldHit)
         rotatedFootprint = RotateFootprint(placeableDef.gridFootprint, _rotation)
         cells = PlacementGrid.GetFootprintCells(cell, rotatedFootprint)
     if placeableDef.placementMode == Socket:
         find nearest PlacementSocket via OverlapSphere
         snap to socket.transform

  3. REACH CHECK
     if Grid:
         float dist = Vector3Int.Distance(PlacementGrid.WorldToCell(playerPos), cell)
         if dist > placeableDef.reachRadius: → ghost red, return

  4. VALIDATE
     bool valid = true
     foreach SO_PlacementRuleDef rule in placeableDef.rules:
         if !rule.Evaluate(cell, rotatedFootprint, _rotation, PlacementGrid):
             valid = false; break

  5. UPDATE GHOST
     ghost.transform.position = PlacementGrid.CellToWorld(cell) or socket.position
     ghost.SetColor(valid ? green : red)

  6. INPUT
     R key: _rotation = (_rotation + 1) % 4; (Grid mode only)
     LMB / E (on valid): ConfirmPlacement(cell or socket)

ConfirmPlacement():
  PlacementGrid.OccupyCells(cells, newPlacedObject)
  go = Instantiate(placeableDef.worldDropPrefab, worldPos, rotation)
  go.GetComponent<PlacedStructure>().Init(placeableDef)
  GameEvents.RaiseStructurePlaced(placeableDef, cell)
  PlayerCarryController.DeductActiveSlot(1)
```

### PlacementGrid (MonoBehaviour singleton)
```
Dictionary<Vector3Int, PlacedObject> DOC_CELLS

Vector3Int WorldToCell(Vector3 world):
  return new Vector3Int(
    Mathf.FloorToInt(world.x / _cellSize),
    Mathf.FloorToInt(world.y / _cellSize),
    Mathf.FloorToInt(world.z / _cellSize))

Vector3 CellToWorld(Vector3Int cell):
  return new Vector3(
    cell.x * _cellSize + _cellSize * 0.5f,
    cell.y * _cellSize,
    cell.z * _cellSize + _cellSize * 0.5f)

bool IsEmpty(Vector3Int cell): DOC_CELLS.ContainsKey(cell) == false
bool AreAllEmpty(List<Vector3Int> cells): cells.All(IsEmpty)

List<Vector3Int> GetFootprintCells(Vector3Int origin, Vector2Int footprint, int rotation):
  → generate list of cells covered by footprint, rotated around origin

void OccupyCells(List<Vector3Int> cells, PlacedObject obj):
  foreach cell: DOC_CELLS[cell] = obj

void FreeCells(PlacedObject obj):
  remove all entries where value == obj
```

### PlacedStructure (MonoBehaviour on placed prefab)
```
[SerializeField] SO_PlaceableDef _def
List<Vector3Int> _occupiedCells    ← stored at placement time

void Init(SO_PlaceableDef def, List<Vector3Int> cells):
  _def = def; _occupiedCells = cells

OnDestroy():
  PlacementGrid.FreeCells(this)
  GameEvents.RaiseStructureRemoved(_def)
  // NavMeshObstacle auto-removes carving on Destroy — no extra code needed
```

---

## Placement Rules (SO_PlacementRuleDef implementations)

Each is a concrete ScriptableObject, drag-dropped into
`SO_PlaceableDef.rules` in the inspector.

### SO_Rule_CellsMustBeEmpty
```
Evaluate: return grid.AreAllEmpty(GetFootprintCells(cell, footprint, rotation))
```
Used by: all grid-placed structures.

### SO_Rule_RequireNeighbour
```
[SerializeField] SO_PlaceableDef requiredNeighbourType
[SerializeField] bool allowPlacingFirst  ← for first belt in chain

Evaluate:
  if allowPlacingFirst && grid has no belts at all: return true
  neighbourCells = GetAdjacentCells(footprintCells)
  return neighbourCells.Any(c => grid.GetPlaced(c)?.def == requiredNeighbourType)
```
Used by: conveyor belts (must connect to existing belt or belt origin).

### SO_Rule_MustBeOnNavMesh
```
Evaluate:
  worldPos = grid.CellToWorld(cell)
  return NavMesh.SamplePosition(worldPos, out _, 0.5f, NavMesh.AllAreas)
```
Used by: any structure NPCs need to walk around.

### SO_Rule_HeightClearance
```
[SerializeField] float requiredClearance

Evaluate:
  worldPos = grid.CellToWorld(cell) + Vector3.up * 0.1f
  return !Physics.Raycast(worldPos, Vector3.up, requiredClearance, obstacleMask)
```
Used by: tall structures.

---

## NavMesh Integration

No code required beyond prefab setup:

1. Every `SO_PlaceableDef.worldDropPrefab` that blocks NPC movement
   has a `NavMeshObstacle` component (Unity AI Nav 2.0):
   - `shape = Box`
   - `carving = true`
   - `carveOnlyStationary = true`
   - `size` = matches visual bounds

2. When `PlacedStructure` is instantiated, the `NavMeshObstacle`
   automatically carves a hole in the NavMesh. No code call needed.

3. When `PlacedStructure` is destroyed (removed by player), the
   `NavMeshObstacle` is destroyed with it, hole heals automatically.

4. `CustomerFSM.TickStateSelectItem` already calls `MoveTo` on each
   re-evaluation. Any path replanning triggered by carving is handled
   by `NavMeshAgent` internally on the next `SetDestination`.

5. **The only code concern:** after placement, if a `CustomerAgent` is
   currently navigating through the now-carved area, their path may
   become temporarily invalid. `NavMeshMover.StartTrackingRoutine`
   already has an `_arrivalTime` timeout that calls `onArrived`
   as a fallback — the agent recovers. No special case needed.

---

## GameEvents additions (Phase 2+)

```csharp
#region Phase-2 — Delivery & Purchase

// CheckoutService → DeliveryService
public static event Action<List<BoxOrder>> OnPurchaseConfirmed;
public static void RaisePurchaseConfirmed(List<BoxOrder> orders) { ... }

// PlayerCarryController → CarrySlotHUD
public static event Action<int, CarrySlotPayload> OnCarrySlotChanged;
// int = slotIndex, payload = new contents (null if cleared)
public static void RaiseCarrySlotChanged(int index, CarrySlotPayload payload) { ... }

// DeliveryService → any logger / UI
public static event Action<SO_BoxDefinition, int> OnBoxDelivered;
public static void RaiseBoxDelivered(SO_BoxDefinition def, int count) { ... }

#endregion

#region Phase-2 — Placement

// PlacementController → StoreManager, NavMesh systems
public static event Action<SO_PlaceableDef, Vector3Int> OnStructurePlaced;
public static void RaiseStructurePlaced(SO_PlaceableDef def, Vector3Int cell) { ... }

// PlacedStructure.OnDestroy → POIRegistry (unregister), StoreManager
public static event Action<SO_PlaceableDef, Vector3Int> OnStructureRemoved;
public static void RaiseStructureRemoved(SO_PlaceableDef def, Vector3Int cell) { ... }

#endregion
```

---

## Hold Visuals — LooseItem vs PlaceableStructure

| Aspect | LooseItem | PlaceableStructure |
|---|---|---|
| Held position | `_Tr_holdAnchor.position + itemDef.holdOffset` | Ghost prefab at raycast hit, snapped to grid |
| Hold rotation | `itemDef.holdRotation` | Grid-aligned + `_rotation * 90°` |
| Visual prefab | `itemDef.worldDropPrefab` (tinted normally) | `placeableDef.ghostPrefab` (tinted green/red) |
| Physics while held | disabled (kinematic) | none — ghost has no collider |
| F key | drop one unit at player feet | N/A |
| LMB / E key | nothing (or equip action Phase 4) | confirm placement |
| R key | nothing | rotate ghost 90° |

---

## LooseItem Drop & Grab

### DroppedLooseItem (MonoBehaviour on worldDropPrefab)
```
[HideInInspector] SO_LooseItemDef _itemDef
[HideInInspector] int _quantity          ← 1 for single drops; > 1 if stack dropped as box alt

Implements IPickupable:
  CanPickup: check if any carrier slot has room for itemDef
  TryPickup: find best slot (existing partial stack first, then empty), merge, Destroy self

Spawned by PlayerCarryController.TryDropOne():
  pos = player.position + player.forward * 0.6f + Vector3.up * 0.5f
  go = Instantiate(itemDef.worldDropPrefab, pos, Random.rotation)
  go.GetComponent<DroppedLooseItem>().Init(itemDef, qty: 1)
```

---

## Inventory HUD Layout

```
┌────────────────────────────────────────────────────────┐
│                  [game view]                           │
│                                                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │  [1]        [2]        [3]        [4]        [5] │  │
│  │ ┌──────┐  ┌──────┐  ┌──────┐  ┌──────┐  ┌─────┐ │  │
│  │ │sprite│  │sprite│  │      │  │      │  │     │ │  │
│  │ │  55⁹⁹│  │  2 ²│  │      │  │      │  │     │ │  │
│  │ └──────┘  └──────┘  └──────┘  └──────┘  └─────┘ │  │
│  │  ← active slot highlighted (border + scale up)  │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘

superscript = stackLimit indicator (not quantity label — that's the main number)
quantity label top-right of slot
empty slot = greyed out
```

`CarrySlotHUD` is a pure listener — it only reads from `OnCarrySlotChanged` events. It never calls `PlayerCarryController` methods. One-way data flow.

---

## Phase 3+ Hooks (stub now, implement later)

These are `TODO` comments in the relevant classes, not stubs that block
compilation:

1. **Economy:** `CheckoutService.TryCheckout()` currently approves all
   purchases. Insert `EconomyManager.HasFunds(totalCost)` check here.
   `GameEvents.RaiseFundsDeducted(float)` fires on success.

2. **Truck/container loading:** `PlacementGrid` already tracks all
   placed objects by cell. A `ContainerZone` component marks a
   rectangular region. `TruckLoadingService` queries
   `PlacementGrid.GetObjectsInRegion(zone)` — no architecture change
   needed.

3. **Conveyor belt network:** Each `PlacedStructure` with a belt def
   exposes `Vector3Int inputCell` and `Vector3Int outputCell` based
   on rotation. A `BeltNetwork` singleton builds a directed graph from
   `OnStructurePlaced` events. Pathfinding on that graph is separate
   from NavMesh.

4. **Persistent NPC schedules (Phase 4):** `ShelfPOI` already registers
   itself in `POIRegistry` regardless of whether it was scene-placed or
   player-placed. No change needed — runtime-placed shelves are
   immediately visible to all NPC shopping queries.

5. **Tool equip actions (axe mining, etc.):** `PlayerCarryController`
   fires `GameEvents.RaiseActiveItemUsed(itemDef)` on LMB. Mining
   system listens — it knows nothing about inventory. Inventory knows
   nothing about mining.

---

## File / Class Creation Order for Implementation

Implement in this order to avoid forward-reference issues:

```
1.  ShopCategory.cs                  (enum — no deps)
2.  CarrySlotPayload.cs              (plain C# — no deps)
3.  BoxOrder.cs                      (plain C# struct — no deps)
4.  SO_ItemDefinitionBase.cs         (abstract SO)
5.  SO_LooseItemDef.cs               (extends base)
6.  SO_PlaceableDef.cs               (extends base)
7.  SO_BoxDefinition.cs              (refs base)
8.  SO_ShopCatalogEntry.cs           (refs base + box)
9.  SO_ShopCatalog.cs                (refs entry list)
10. SO_PlacementRuleDef.cs           (abstract SO)
11. SO_Rule_CellsMustBeEmpty.cs      (concrete rule)
12. SO_Rule_RequireNeighbour.cs      (concrete rule)
13. SO_Rule_MustBeOnNavMesh.cs       (concrete rule)
14. SO_Rule_HeightClearance.cs       (concrete rule)
15. IPickupable.cs                   (interface)
16. PlacementGrid.cs                 (singleton MB)
17. PlacedStructure.cs               (MB on placed prefab)
18. DroppedLooseItem.cs              (MB, implements IPickupable)
19. DeliveryBox.cs                   (MB, implements IPickupable)
20. PlayerCarryController.cs         (MB — refs IPickupable, PlacementGrid)
21. PlacementGhost.cs                (MB — visual only, no logic)
22. PlacementController.cs           (MB — refs PlacementGrid, PlacementGhost)
23. CartModel.cs                     (plain C#)
24. CheckoutService.cs               (MB — refs CartModel)
25. DeliveryService.cs               (MB — listens OnPurchaseConfirmed)
26. ShopUI.cs + sub-widgets          (MB — renders CartModel)
27. CarrySlotHUD.cs                  (MB — listens OnCarrySlotChanged)
28. GameEvents.cs additions          (static — add new regions)
```

Each file should be implemented top-to-bottom in this order. Files 1–14
have no MonoBehaviour dependencies and can be written and unit-tested
in isolation before any Unity scene work.
