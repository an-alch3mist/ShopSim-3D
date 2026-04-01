# Shop-Cart Phase — Implementation Guide

> Unity 6000.3+ · TextMeshPro required · matches existing ShopSim architecture

---

## 1. Architecture Overview

```
ScriptableObjects (data)
  SO_ShopCategory       ← one asset per category  (Furniture, Stock, Equipment, Decor)
  SO_PurchasableItem    ← one asset per buyable item
  SO_ShopCatalogue      ← master list (drag items in inspector)

Runtime Services (MonoBehaviours — scene singletons)
  ShopCartService       ← owns cart state, fires GameEvents
  DeliveryService       ← listens to purchase, spawns boxes at delivery point

UI MonoBehaviours (attached to UI prefabs / Canvas children)
  ShopPanel             ← open / close (Key: B), pauses timeScale
  ShopCatalogueUI       ← left panel: category filters + item grid
  ShopCartUI            ← right panel: live cart rows + checkout
  ShopCatalogueItemUI   ← item card (spawned by ShopCatalogueUI)
  ShopCartEntryUI       ← cart row  (spawned by ShopCartUI)

In-World Prefab Component
  DeliveryBox           ← on each box prefab; receives Init(item, units)
```

**Event bus flow:**
```
[ShopCatalogueItemUI] click "+"
  → ShopCartService.AdjustQuantity(item, +1)
      → GameEvents.RaiseCartUpdated(item, newQty)
          → ShopCartUI         : add / refresh row
          → ShopCatalogueItemUI: update badge

[ShopCartUI] click "Confirm"
  → ShopCartService.ConfirmPurchase()
      → GameEvents.RaisePurchaseConfirmed(entries)
          → DeliveryService    : spawn boxes
      → ShopCartService.ClearCart()
          → GameEvents.RaiseCartUpdated(item, 0) × N
              → ShopCartUI     : remove rows
```

---

## 2. Step 1 — Patch GameEvents.cs

Open `GameEvents.cs` and add at the top:
```csharp
using System.Collections.Generic;
```

Then paste the contents of **`GameEvents_ShopAdditions.cs`** (the three events/raisers inside the `#region Shop-Cart` block) **inside the `GameEvents` static class**, after the existing `#region Phase-1.1` block.

---

## 3. Step 2 — Create ScriptableObject Assets

### Categories  
`Assets/Data/ShopCategories/`  
Right-click → Create → ShopSim_SO → SO_shopCategory

Suggested set:

| fileName | categoryId | displayName |
|---|---|---|
| SO_Cat_Stock | stock | Stock |
| SO_Cat_Furniture | furniture | Furniture |
| SO_Cat_Equipment | equipment | Equipment |
| SO_Cat_Decor | decor | Decor |

### Purchasable Items  
`Assets/Data/PurchasableItems/`  
Right-click → Create → ShopSim_SO → SO_purchasableItem

**Milk example:**

| Field | Value |
|---|---|
| itemId | milk |
| displayName | Milk |
| category | SO_Cat_Stock |
| unitPrice | 0.80 |
| unitsPerBox | 100 |
| boxPrefab | *(prefab — see §5)* |
| linkedItemData | SO_ItemData_Milk *(your existing milk SO)* |
| gridSize | (1, 1, 1) |

**Shelf example:**

| Field | Value |
|---|---|
| itemId | shelf_std |
| displayName | Standard Shelf |
| category | SO_Cat_Furniture |
| unitPrice | 45.00 |
| unitsPerBox | 1 |
| boxPrefab | *(box prefab)* |
| itemPrefab | *(shelf prefab — Phase 2)* |
| gridSize | (1, 2, 1) |

### Catalogue  
`Assets/Data/SO_shopCatalogue.asset`  
Right-click → Create → ShopSim_SO → SO_shopCatalogue  
Drag all SO_PurchasableItem assets into the `items` list.

---

## 4. Step 3 — Scene Setup

### Services GameObject  
Create an empty GameObject `_Services` (or add to existing StoreManager).  
Add components:
- `ShopCartService`
- `DeliveryService` → assign `_Tr_deliveryPoint` (Transform near the loading bay)

### Shop Canvas  
Create a **Screen Space – Overlay** Canvas.  
Structure:
```
Canvas
└─ ShopRoot                       ← ShopPanel component
   ├─ LeftPanel  (40% width)      ← ShopCatalogueUI component
   │  ├─ FilterBar                ← _Tr_filterBar
   │  └─ ScrollView/Viewport/Content ← _Tr_itemGrid
   └─ RightPanel (60% width)      ← ShopCartUI component
      ├─ ScrollView/Viewport/Content ← _Tr_rowContainer
      ├─ SummaryBar
      │  ├─ TotalPrice TMP
      │  └─ BoxCount TMP
      ├─ EmptyHint TMP
      ├─ ConfirmButton
      └─ ClearButton
```

Assign on `ShopCatalogueUI`:
- `_catalogue` → SO_shopCatalogue asset
- `_pfCategoryButton` → category button prefab
- `_pfItemCard` → item card prefab

Assign on `ShopPanel`:
- `_go_shopRoot` → ShopRoot GameObject

---

## 5. Step 4 — Prefabs

### Category Button Prefab  
`_pfCategoryButton`
```
Button (Button component)
└─ Image (background, used for active/inactive colour)
└─ Label (TMP_Text)
```

### Item Card Prefab  
`_pfItemCard`
```
GameObject (ShopCatalogueItemUI component)
├─ Icon  (Image)
├─ Name  (TMP_Text)
├─ Price (TMP_Text)
├─ AddButton (Button) → _btn_add
└─ CartBadge (GameObject)
   └─ BadgeText (TMP_Text)
```

### Cart Entry Prefab  
`_pfCartEntry`
```
GameObject (ShopCartEntryUI component)
├─ Icon       (Image)
├─ Name       (TMP_Text)
├─ UnitPrice  (TMP_Text)
├─ QtyGroup
│  ├─ Minus10  (Button)
│  ├─ Minus1   (Button)
│  ├─ QtyInput (TMP_InputField, Content Type: Integer Number)
│  ├─ Plus1    (Button)
│  └─ Plus10   (Button)
├─ TotalPrice (TMP_Text)
└─ RemoveBtn  (Button) — label "×"
```

### Box Prefab (one per item type, or shared)  
```
GameObject (DeliveryBox component, Rigidbody, BoxCollider)
├─ Mesh (cube or custom box mesh)
└─ Label (TMP_Text, optional world-space) → _label
```
Assign this prefab to `SO_PurchasableItem.boxPrefab`.

---

## 6. Packaging Logic Reference

`ShopCartEntry` computes:

```csharp
// 440 milk ordered, unitsPerBox = 100
BoxCount      = Ceil(440 / 100) = 5
UnitsInLastBox = 440 % 100     = 40   (boxes 0-3: 100 each, box 4: 40)

// 3 shelves ordered, unitsPerBox = 1
BoxCount      = 3
UnitsInLastBox = 1   (every box is full)
```

`DeliveryService.RoutineDeliverBoxes` iterates boxes in order and calls
`DeliveryBox.Init(item, unitsInBox)` so each box knows exactly what it carries.

---

## 7. Extending for Future Phases

### Inventory pickup (Phase 2)
When player walks to a DeliveryBox and presses `[E]`:
```csharp
box.OpenBox();   // spawns item.itemPrefab × UnitsInside, destroys box
```
No changes to cart or delivery systems.

### Physical stock box carried by player
Replace `PlayerStockingController` key binding with:
```csharp
// existing IStockable + OverlapSphere logic stays identical
SO_ItemData item = heldBox.ContainedItem.linkedItemData;
TryStockNearest(item);
```

### Grid placement (Phase 2)
`SO_PurchasableItem.gridSize` and `SO_PurchasableItem.itemPrefab` are
already present. The placement system reads `gridSize` for clearance
checks; no changes to the shop-cart scripts.

### Budget / currency system
Subscribe to `GameEvents.OnPurchaseConfirmed` from a `WalletService`:
```csharp
GameEvents.OnPurchaseConfirmed += entries =>
{
    float cost = entries.Sum(e => e.TotalPrice);
    wallet.Deduct(cost);
};
```
`ShopCartService` never touches money — clean separation.

---

## 8. Files Checklist

| File | Type | Notes |
|---|---|---|
| `SO_ShopCategory.cs` | ScriptableObject | category label |
| `SO_PurchasableItem.cs` | ScriptableObject | item sold in shop |
| `SO_ShopCatalogue.cs` | ScriptableObject | master item list |
| `ShopCartEntry.cs` | Plain class | runtime cart row data |
| `ShopCartService.cs` | MonoBehaviour | cart state singleton |
| `GameEvents_ShopAdditions.cs` | Snippet | **paste into GameEvents.cs** |
| `DeliveryBox.cs` | MonoBehaviour | on box prefabs |
| `DeliveryService.cs` | MonoBehaviour | spawns boxes on purchase |
| `ShopPanel.cs` | MonoBehaviour | open/close + timeScale |
| `ShopCatalogueUI.cs` | MonoBehaviour | left panel |
| `ShopCatalogueItemUI.cs` | MonoBehaviour | item card prefab |
| `ShopCartUI.cs` | MonoBehaviour | right panel |
| `ShopCartEntryUI.cs` | MonoBehaviour | cart row prefab |
