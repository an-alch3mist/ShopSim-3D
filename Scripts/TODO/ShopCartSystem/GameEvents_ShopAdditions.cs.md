// ============================================================
//  GameEvents_ShopAdditions.cs
//
//  !! DO NOT compile this file as-is !!
//  Copy the regions below into the existing GameEvents.cs
//  inside the static class body.
//
//  Paste location: after the existing #region Phase-1.1 block.
// ============================================================

/*
────────────────────────────────────────────────────────────────
PASTE INTO GameEvents.cs  — add these two usings at the top if
they are not already present:

    using System.Collections.Generic;

Then paste the region below after Phase-1.1:
────────────────────────────────────────────────────────────────
*/

#region Shop-Cart

// ┌──────────────────────────────────────────────────────────────┐
// │  ShopCartService (SetQuantity / Remove / ClearCart)          │
// │    └─ RaiseCartUpdated(item, newQty)                         │
// │         ├──► ShopCartUI       → refresh or remove row        │
// │         └──► ShopCatalogueItemUI → update "in cart" badge    │
// │                                                              │
// │  newQty == 0  means the item was removed from the cart.      │
// └──────────────────────────────────────────────────────────────┘
public static event Action<SO_PurchasableItem, int> OnCartUpdated;
public static void RaiseCartUpdated(SO_PurchasableItem item, int newQty)
{
	GameEvents.OnCartUpdated?.Invoke(item, newQty);
}

// ┌──────────────────────────────────────────────────────────────┐
// │  ShopCartService (ConfirmPurchase)                           │
// │    └─ RaisePurchaseConfirmed(entries)                        │
// │         ├──► DeliveryService  → spawn boxes at delivery pt   │
// │         └──► AnalyticsLogger  → record spend                 │
// │                                                              │
// │  entries is a snapshot — ShopCartService clears _cart        │
// │  immediately after firing, so listeners must not mutate it.  │
// └──────────────────────────────────────────────────────────────┘
public static event Action<List<ShopCartEntry>> OnPurchaseConfirmed;
public static void RaisePurchaseConfirmed(List<ShopCartEntry> entries)
{
	GameEvents.OnPurchaseConfirmed?.Invoke(entries);
}

// ┌──────────────────────────────────────────────────────────────┐
// │  DeliveryService (after all boxes for an item are spawned)   │
// │    └─ RaiseItemDelivered(item, boxCount)                     │
// │         └──► StoreManager / UI → "Your order arrived!" toast │
// └──────────────────────────────────────────────────────────────┘
public static event Action<SO_PurchasableItem, int> OnItemDelivered;
public static void RaiseItemDelivered(SO_PurchasableItem item, int boxCount)
{
	GameEvents.OnItemDelivered?.Invoke(item, boxCount);
}

#endregion
