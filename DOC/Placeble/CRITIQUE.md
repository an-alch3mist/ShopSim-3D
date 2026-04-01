# ShopSim3D — Phase 2+ Critique & Improvements
> Review of the proposed approach against Unity 6000.3+ constraints,
> your existing architecture conventions, and long-term extensibility.

---

## 1. Terminology gaps to fix first

Your description mixes mechanical concepts without stable names. Before
writing a single line of code the terms need to be locked down, because
ambiguity at this stage leads to class names you regret later.

| Your phrase | Proposed canonical term | Why |
|---|---|---|
| "apple, milk, axe, lamp" | **LooseItem** | Stacks, drops freely, no grid snap, held close |
| "shelf, table, belt, chair" | **PlaceableStructure** | Grid-snapped, clearance-checked, NavMesh obstacle |
| "the box containing items" | **DeliveryBox** | Runtime object, distinct from shop order |
| "how many per box" | **packagingUnit** | lives on `SO_BoxDefinition` |
| "where player holds item" | **holdSocket** | per-item offset in `SO_LooseItemDef` |
| "green/red ghost" | **PlacementGhost** | separate prefab, not a mode toggle |
| "10x10 reach grid" | **reachRadius** | per-item float in tiles, not always 10 |
| "snap spots for chairs" | **PlacementSocket** | child Transform on table prefab |
| "inventory slot UI" | **CarrySlotWidget** | one of five bottom-center slots |

Locking these down means every future Claude session, PR, or comment
will refer to the same thing.

---

## 2. The "10×10 raycast grid" description needs untangling

You described it as *"a raycast fired in 3D, say the grid player
reachable is 10×10 from where player is"*. There are actually two
distinct checks happening here — they should not be conflated:

(the 10 x10 x 10 3d grip works this way start from closer tile and keep checking next next until you 
hit the end of 10 x 10 x 10 boundry, if you hit one of closer and can be placed show it as green if you hit one of closer and its occupied in direction of forward show as red hit none show nothing)

Also: the reach limit should live on `SO_PlaceableDef` not hardcoded
as 10. A conveyor belt might be placeable at 4 tiles. A truck loading
dock at 15. Don't bake the number into a system.

----

## 3. NavMesh: don't rebake, carve

You didn't specify, but the implied approach of placing a shelf and
having NPCs walk around it requires NavMesh awareness. There are two
approaches:

**Wrong approach:** Call `NavMeshSurface.BuildNavMeshAsync()` every
time something is placed. This is expensive (hundreds of ms on complex
scenes), causes a visible one-frame hitch, and in Unity 6 with AI
Navigation 2.0 is no longer the idiomatic path for dynamic obstacles.

**Right approach:** Every `PlaceableStructure` prefab has a
`NavMeshObstacle` component with `carving = true` and
`carveOnlyStationary = true`. Unity's NavMesh system handles the hole
automatically, no code required, no hitch. The trade-off is that
carving creates a rectangular hole — fine for shelves and tables, fine
for belts. For irregularly shaped obstacles you'd add multiple obstacle
components. This is zero-cost at the architecture level to support.

The implication: `SO_PlaceableDef` does **not** need to store NavMesh
data. The prefab carries the `NavMeshObstacle` component. The
architecture just needs to trust the prefab.

---

## 4. The chair/socket relationship — don't use placement rules for snapping

You described chairs as having "6 particular spots" around a table
where they can be placed. The instinct to encode this as a placement
validation rule (`IPlacementRule`) is architecturally wrong for the
following reason:

Validation rules answer the question *"can I place here?"*. Snap
sockets answer the question *"where exactly should this go?"*. Chairs
don't go in grid cells, they go at named attachment points on a
specific table instance. This is a **socket pattern**, not a grid
pattern.

The cleaner model:
- Table prefab has 6 child `PlacementSocket` components (Transforms),
  each with an `acceptedStructureType` field.
- When the player holds a chair and points near a table, raycast against
  `PlacementSocket` colliders rather than the grid.
- Chair has `SO_PlaceableDef.placementMode = PlacementMode.Socket`
  (rather than `PlacementMode.Grid`).
- The ghost snaps to the nearest valid socket, not to a grid cell.

This also correctly handles the "can't place if neighbour table is
blocking" rule — it just means the socket's `IsOccupied` is true.

Trying to encode chair placement in the grid system produces weird
edge cases when tables are rotated or two tables are adjacent.

---

## 5. The inventory "Alt+F drops the box" mechanic needs a runtime container model

You described: *"press Alt+F → drop the box containing it, say 54
remaining."* This implies a `DeliveryBox` prefab that at runtime knows
its contents and current count. That's not the same object as the
initial delivery drop. It's a **carriable container** with its own
carry behavior, separate from a LooseItem.

This distinction is important early because it affects `SO_BoxDefinition`:
- At delivery time, `SO_BoxDefinition` is a data template (items +
  count + prefab).
- At runtime, the spawned box is an instance with mutable `int
  remainingCount`.
- When `remainingCount == 0` the box destroys itself.
- When the player picks it up via `G`, it occupies one CarrySlot just
  like a LooseItem stack but the slot knows it's carrying a box, not
  individual items.

If you don't model this distinction, you'll end up retrofitting it into
LooseItem and making a mess. The cleanest split: `CarrySlot.Payload` is
a discriminated union of either `LooseStack` (SO_LooseItemDef + qty) or
`BoxPayload` (SO_BoxDefinition + remainingCount). Both are plain
C# classes, no MonoBehaviour.

---

## 6. Shop UI: tabs → cart → purchase is three separate concerns

The way you described the shop UI reads as one big system. It's
actually three cleanly separable concerns:

- **CatalogBrowser** — knows which `SO_ShopCatalogEntry` items exist
  per category. Reads from a `SO_ShopCatalog` ScriptableObject. Fires
  events when user selects items.
- **CartModel** — a plain C# class (not MonoBehaviour), holds a
  `Dictionary<SO_ShopCatalogEntry, int>` of quantities. Exposes
  `AddQty`, `RemoveQty`, `Clear`. No Unity dependencies.
- **CheckoutService** — listens to a "purchase confirmed" event,
  validates funds (Phase 3 economy), calculates box count per entry,
  then fires `GameEvents.RaisePurchaseConfirmed(List<BoxOrder>)`.
  Knows nothing about UI.

The UI renders `CartModel`. `CheckoutService` consumes `CartModel`.
Neither knows about the other. This is the same event-bus pattern
already established in `GameEvents.cs`.

The quantity buttons (`-10 -1 +1 +10`) and the text field are just
two views into `CartModel.quantities[entry]`. They don't own the
number — `CartModel` does.

---

## 7. Packaging unit should be on `SO_BoxDefinition`, not `SO_LooseItemDef`

You said milk comes 100 per box and axe 1 per box. The temptation is
to put `packagingUnit` on `SO_LooseItemDef`. Don't. An item's packaging
is a shop/logistics concern, not an item identity concern. Milk could
hypothetically be sold in boxes of 6 (retail pack) or boxes of 100
(wholesale). The same milk `SO_LooseItemDef` serves both.

Keep `SO_LooseItemDef` pure: it describes what the item *is*. Let
`SO_BoxDefinition` describe how it ships.

---

## 8. Dimensions: use `Vector2Int` not three integers

You mentioned integer dimensions like `3 x 2 x 1`. For grid placement,
the Y dimension (height) is almost never used for footprint checking —
what matters for grid occupancy is the XZ footprint. Height matters for
clearance (is there a ceiling?) but that's a separate check.

Use `Vector2Int gridFootprint` on `SO_PlaceableDef` for all grid logic.
Keep a separate `float worldHeight` if you need to check vertical
clearance. Mixing all three into one `Vector3Int` makes grid math
awkward and clearance checking confusing.

The belt example: a 1×2 belt occupies 2 grid cells but its height is
irrelevant to placement. `gridFootprint = new Vector2Int(1, 2)`.

---

## 9. Conveyor belt orientation — add `GridRotation` early

You mentioned belts come in rotations. Footprint rotation (a 1×2
footprint rotated 90° becomes 2×1) needs to be handled at the data
level, not patched in later. Add `int rotationSteps` (0–3, 90° each)
to the placement ghost system from day one. It's a single int. Not
adding it now means refactoring every grid query later.

---

## 10. The existing codebase is in good shape — protect these invariants

The current code has excellent patterns that the new systems must not
break:

- `GameEvents` is the only cross-system communication channel. The new
  shop, inventory, and placement systems must fire events rather than
  call methods on each other.
- `CustomerFSM`'s `selectItem` tick currently calls
  `POIRegistry.Ins.AnyShelfHasStockOf()`. When shelves are placed by
  the player, they must still `RegisterShelf` in `Start()` — the
  runtime-placed shelf is architecturally identical to the scene-placed
  one. No special case needed.
- `IStockable` is already decoupled from `ShelfPOI`. A player-placed
  shelf works exactly like an AutoStocked shelf. This is correct.
- `NavMeshMover` uses the arrival callback pattern. When a shelf is
  placed and carving updates the NavMesh, any NPC mid-path will
  auto-replan on the next `SetDestination` call — no code change
  required.
