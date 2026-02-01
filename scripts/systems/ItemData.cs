using Godot;
using System;

// Interface untuk item yang bisa digunakan
public interface IUsableItem
{
	void Use(Player player);
	string GetUseText(); // Text untuk UI (misal "Read", "Drink", "Equip")
}

// Class untuk menyimpan data item
public partial class ItemData : Resource
{
	[Export] public string ItemName { get; set; } = "Item";
	[Export] public string Description { get; set; } = "An item";
	[Export] public int MaxStackSize { get; set; } = 64;
	[Export] public Texture2D Icon { get; set; }
	[Export] public bool IsUsable { get; set; } = false;
	[Export] public bool IsKeyItem { get; set; } = false; // Key items cannot be dropped
	
	// Untuk identifikasi unik item type
	public string ItemId { get; set; } = "";
	
	// Reference ke usable behavior (set saat runtime)
	public IUsableItem UsableBehavior { get; set; }

	public ItemData()
	{
	}

	public ItemData(string id, string name, int maxStack = 64, bool usable = false, bool keyItem = false)
	{
		ItemId = id;
		ItemName = name;
		MaxStackSize = maxStack;
		IsUsable = usable;
		IsKeyItem = keyItem;
	}
}

// Class untuk item instance dalam inventory (dengan quantity)
public class InventoryItem
{
	public ItemData Data { get; set; }
	public int Quantity { get; set; }

	public InventoryItem(ItemData data, int quantity = 1)
	{
		Data = data;
		Quantity = quantity;
	}

	public bool CanStack(ItemData otherData)
	{
		return Data.ItemId == otherData.ItemId;
	}

	public bool IsFull()
	{
		return Quantity >= Data.MaxStackSize;
	}

	public int GetAvailableSpace()
	{
		return Data.MaxStackSize - Quantity;
	}
}
