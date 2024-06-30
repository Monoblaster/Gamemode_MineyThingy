datablock ItemData(Inventory_InventoryUIItem)
{
	uiname = "Inventory";
};


$c = -1;
$UI = InventoryUI();
$UI.add("Base").set($c++,"Inventory_InventoryUIItem");
