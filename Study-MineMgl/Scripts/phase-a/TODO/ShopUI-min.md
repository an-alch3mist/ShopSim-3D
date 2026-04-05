# min required for ShopUI:

- UIManager <!-- for all UI combined manager just communicates with ShopUI, interactionUI etc .... -->
- ShopUI
	- SO_ShopCategory
		- categoryId
		- L\<SO_ShopItemDef\>
		- pfShopCategory (with ShopCategoryUIFields attached to it)
		- icon
	- SO_ShopItemDef
		- itemId
		- defaultPrice
		- defaultIsLocked
		- pfShopItem (with ShopItemUIFields attached to it)
		- pfShopCartItem (with ShopCartItemUIFields attached to it)
		- pfGameObject (for game world)
	- ShopCategoryUIFields, ShopItemUIFields, ShopCartItemUIFields are purely for data with ui refernce such as button, text, image componenets in inspectorField refernce to modify later