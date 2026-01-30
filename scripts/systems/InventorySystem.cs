using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class InventorySystem : Node
{
	[Export] public int InventorySize { get; set; } = 16; // 16 slot total (4x4)
	[Export] public int HotbarSize { get; set; } = 4; // 4 slot hotbar (1x4)

	private List<InventoryItem> _items;
	private int _selectedHotbarSlot = 0;

	[Signal]
	public delegate void InventoryChangedEventHandler();
	
	[Signal]
	public delegate void HotbarSlotChangedEventHandler(int slotIndex);

	public override void _Ready()
	{
		_items = new List<InventoryItem>();
		for (int i = 0; i < InventorySize; i++)
		{
			_items.Add(null);
		}
	}

	// Tambah item ke inventory
	public bool AddItem(ItemData itemData, int quantity = 1)
	{
		if (itemData == null) return false;

		int remainingQuantity = quantity;

		// Coba stack ke item yang sudah ada
		for (int i = 0; i < InventorySize && remainingQuantity > 0; i++)
		{
			if (_items[i] != null && _items[i].CanStack(itemData) && !_items[i].IsFull())
			{
				int spaceAvailable = _items[i].GetAvailableSpace();
				int amountToAdd = Mathf.Min(spaceAvailable, remainingQuantity);
				
				_items[i].Quantity += amountToAdd;
				remainingQuantity -= amountToAdd;
			}
		}

		// Jika masih ada sisa, buat stack baru
		while (remainingQuantity > 0)
		{
			int emptySlot = GetFirstEmptySlot();
			if (emptySlot == -1)
			{
				GD.Print("Inventory penuh!");
				return false; // Inventory penuh
			}

			int amountToAdd = Mathf.Min(itemData.MaxStackSize, remainingQuantity);
			_items[emptySlot] = new InventoryItem(itemData, amountToAdd);
			remainingQuantity -= amountToAdd;
		}

		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	// Hapus item dari slot tertentu
	public InventoryItem RemoveItem(int slotIndex, int quantity = 1)
	{
		if (slotIndex < 0 || slotIndex >= InventorySize || _items[slotIndex] == null)
			return null;

		InventoryItem item = _items[slotIndex];
		int amountToRemove = Mathf.Min(quantity, item.Quantity);

		if (amountToRemove >= item.Quantity)
		{
			// Hapus seluruh stack
			_items[slotIndex] = null;
			EmitSignal(SignalName.InventoryChanged);
			return item;
		}
		else
		{
			// Kurangi quantity
			item.Quantity -= amountToRemove;
			EmitSignal(SignalName.InventoryChanged);
			return new InventoryItem(item.Data, amountToRemove);
		}
	}

	// Drop item dari hotbar slot yang aktif
	public InventoryItem DropSelectedItem(int quantity = 1)
	{
		return RemoveItem(_selectedHotbarSlot, quantity);
	}

	// Dapatkan item di slot tertentu
	public InventoryItem GetItem(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= InventorySize)
			return null;
		return _items[slotIndex];
	}
	
	// Alias untuk GetItem (untuk compatibility)
	public InventoryItem GetItemAt(int slotIndex)
	{
		return GetItem(slotIndex);
	}
	
	// Swap items antara dua slot (untuk drag-drop)
	public void SwapItems(int fromIndex, int toIndex)
	{
		if (fromIndex < 0 || fromIndex >= InventorySize || toIndex < 0 || toIndex >= InventorySize)
		{
			GD.Print($"SwapItems: Invalid indices {fromIndex}, {toIndex}");
			return;
		}
		
		var temp = _items[fromIndex];
		_items[fromIndex] = _items[toIndex];
		_items[toIndex] = temp;
		
		GD.Print($"Swapped items: slot {fromIndex} <-> slot {toIndex}");
		EmitSignal(SignalName.InventoryChanged);
	}

	// Dapatkan semua items
	public List<InventoryItem> GetAllItems()
	{
		return _items;
	}

	// Cari slot kosong pertama
	private int GetFirstEmptySlot()
	{
		for (int i = 0; i < InventorySize; i++)
		{
			if (_items[i] == null)
				return i;
		}
		return -1;
	}

	// Cek apakah punya item tertentu
	public bool HasItem(string itemId, int quantity = 1)
	{
		int totalQuantity = 0;
		foreach (var item in _items)
		{
			if (item != null && item.Data.ItemId == itemId)
			{
				totalQuantity += item.Quantity;
				if (totalQuantity >= quantity)
					return true;
			}
		}
		return false;
	}

	// Hotbar management
	public void SelectHotbarSlot(int slotIndex)
	{
		if (slotIndex >= 0 && slotIndex < HotbarSize)
		{
			_selectedHotbarSlot = slotIndex;
			EmitSignal(SignalName.HotbarSlotChanged, slotIndex);
		}
	}

	public int GetSelectedHotbarSlot()
	{
		return _selectedHotbarSlot;
	}

	public InventoryItem GetSelectedHotbarItem()
	{
		return GetItem(_selectedHotbarSlot);
	}

	// Debug: Print inventory
	public void PrintInventory()
	{
		GD.Print("=== INVENTORY ===");
		for (int i = 0; i < InventorySize; i++)
		{
			if (_items[i] != null)
			{
				GD.Print($"Slot {i}: {_items[i].Data.ItemName} x{_items[i].Quantity}");
			}
		}
	}
}
