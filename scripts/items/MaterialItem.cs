using Godot;
using System;

// Special pickable item for materials that need quantity selection
public partial class MaterialItem : PickableItem
{
	[Export] public int AvailableQuantity = 10; // Max quantity available to pick
	
	private QuantityPickerUI _quantityPicker;
	private Player _currentPlayer;
	private bool _isPickerOpen = false;

	public override void _Ready()
	{
		GD.Print($"MaterialItem._Ready() called for {ItemName}");
		base._Ready();
		
		// Find QuantityPickerUI
		CallDeferred(nameof(FindQuantityPicker));
		
		GD.Print($"MaterialItem {ItemName} initialized with AvailableQuantity={AvailableQuantity}");
	}
	
	private void FindQuantityPicker()
	{
		// Look for QuantityPickerUI in the scene tree
		_quantityPicker = GetTree().Root.GetNodeOrNull<QuantityPickerUI>("/root/Main/UI/QuantityPickerUI");
		
		if (_quantityPicker == null)
		{
			GD.PrintErr($"MaterialItem ({ItemName}): QuantityPickerUI not found in scene!");
			
			// Debug: print scene tree
			var main = GetTree().Root.GetNodeOrNull("Main");
			if (main != null)
			{
				GD.Print("Main node found, searching for UI...");
				foreach (Node child in main.GetChildren())
				{
					GD.Print($"  - {child.Name} ({child.GetType().Name})");
					if (child.Name == "UI" && child is CanvasLayer canvas)
					{
						GD.Print("    UI (CanvasLayer) found! Children:");
						foreach (Node uiChild in canvas.GetChildren())
						{
							GD.Print($"      - {uiChild.Name} ({uiChild.GetType().Name})");
						}
					}
				}
			}
		}
		else
		{
			// Connect signals
			_quantityPicker.QuantitySelected += OnQuantitySelected;
			_quantityPicker.PickupCancelled += OnPickupCancelled;
			GD.Print($"MaterialItem: Connected to QuantityPickerUI for {ItemName}");
		}
	}

	public void PickupWithQuantity(Player player)
	{
		if (_isPickerOpen)
		{
			GD.Print("Picker already open, ignoring...");
			return;
		}
		
		_currentPlayer = player;
		
		GD.Print($"PickupWithQuantity called for {ItemName}");
		GD.Print($"QuantityPicker is null: {_quantityPicker == null}");
		
		if (_quantityPicker != null)
		{
			GD.Print($"Showing picker for {ItemName} with max quantity {AvailableQuantity}");
			_quantityPicker.ShowPicker(ItemName, AvailableQuantity);
			_isPickerOpen = true;
		}
		else
		{
			GD.PrintErr("QuantityPicker not available, picking up 1 item");
			base.Pickup();
		}
	}

	private void OnQuantitySelected(int quantity)
	{
		if (_currentPlayer != null && _currentPlayer._inventory != null)
		{
			bool added = _currentPlayer._inventory.AddItem(_itemData, quantity);
			if (added)
			{
				GD.Print($"✓ Picked up {quantity}x {ItemName}");
				_currentPlayer._inventory.PrintInventory();
				
				// DON'T remove item from scene - make it unlimited
				// QueueFree(); // REMOVED - item stays in scene
			}
			else
			{
				GD.Print("✗ Inventory penuh!");
			}
		}
		
		_isPickerOpen = false;
		_currentPlayer = null;
	}

	private void OnPickupCancelled()
	{
		GD.Print("Pickup cancelled");
		_isPickerOpen = false;
		_currentPlayer = null;
	}

	public override void _ExitTree()
	{
		// Disconnect signals when node is removed
		if (_quantityPicker != null)
		{
			_quantityPicker.QuantitySelected -= OnQuantitySelected;
			_quantityPicker.PickupCancelled -= OnPickupCancelled;
		}
		
		base._ExitTree();
	}
}
