using Godot;
using System;
using System.Collections.Generic;

public partial class FinalMixingUI : Control
{
	[Signal]
	public delegate void MixingCompletedEventHandler(bool success);

	private Label _titleLabel;
	private Label _feedbackLabel;
	private Label _potionInventoryLabel;
	private BaseButton _yellowButton;
	private BaseButton _magentaButton;
	private BaseButton _cyanButton;
	private BaseButton _mixButton;
	private BaseButton _clearButton;
	private InventoryUI _inventoryUI;
	private InventorySystem _playerInventory;
	
	private List<string> _selectedPotions = new List<string>();

	public override void _Ready()
	{
		Visible = false;
		
		var panel = GetNode<Panel>("FinalMixingPanel");
		_titleLabel = panel.GetNode<Label>("TitleLabel");
		_feedbackLabel = panel.GetNode<Label>("FeedbackLabel");
		_potionInventoryLabel = panel.GetNode<Label>("PotionInventoryLabel");
		
		_yellowButton = panel.GetNode<BaseButton>("YellowButton");
		_magentaButton = panel.GetNode<BaseButton>("MagentaButton");
		_cyanButton = panel.GetNode<BaseButton>("CyanButton");
		_mixButton = panel.GetNode<BaseButton>("MixButton");
		_clearButton = panel.GetNode<BaseButton>("ClearButton");
		
		_yellowButton.Pressed += () => TogglePotion("yellow_potion", "Yellow Potion");
		_magentaButton.Pressed += () => TogglePotion("magenta_potion", "Magenta Potion");
		_cyanButton.Pressed += () => TogglePotion("cyan_potion", "Cyan Potion");
		_mixButton.Pressed += OnMixPressed;
		_clearButton.Pressed += OnClearPressed;
		
		CallDeferred(nameof(FindInventoryUI));
		
		// Setup cursor hover effects
		SetupButtonHoverEffects();
	}
	
	private void SetupButtonHoverEffects()
	{
		var buttons = new[] { _yellowButton, _magentaButton, _cyanButton, _mixButton, _clearButton };
		foreach (var button in buttons)
		{
			if (button != null)
			{
				button.MouseEntered += () => CursorManager.Instance?.SetCursor(CursorManager.CursorType.Hover);
				button.MouseExited += () => CursorManager.Instance?.SetCursor(CursorManager.CursorType.Standard);
			}
		}
	}

	private void FindInventoryUI()
	{
		var canvasLayer = GetParent() as CanvasLayer;
		if (canvasLayer != null)
		{
			_inventoryUI = canvasLayer.GetNodeOrNull<InventoryUI>("InventoryUI");
		}
	}

	public void ShowFinalMixingUI(InventorySystem playerInventory)
	{
		_playerInventory = playerInventory;
		_selectedPotions.Clear();
		
		Visible = true;
		InventoryUI.IsAnyPanelOpen = true; // Set global flag
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		// Hide inventory UI and crosshair dengan state management
		if (_inventoryUI != null)
		{
			_inventoryUI.HideForPanel();
			_inventoryUI.SetCrosshairVisible(false);
		}
		
		_feedbackLabel.Text = "Combine all 3 secondary potions to create Teal Potion!";
		_feedbackLabel.Modulate = Colors.White;
		
		UpdateDisplay();
		GD.Print("FinalMixingUI opened");
	}

	private void TogglePotion(string itemId, string potionName)
	{
		if (_selectedPotions.Contains(itemId))
		{
			_selectedPotions.Remove(itemId);
			_feedbackLabel.Text = $"Removed {potionName} from cauldron.";
		}
		else
		{
			if (!_playerInventory.HasItem(itemId))
			{
				_feedbackLabel.Text = $"You don't have {potionName}!";
				_feedbackLabel.Modulate = Colors.Red;
				return;
			}
			
			_selectedPotions.Add(itemId);
			_feedbackLabel.Text = $"Added {potionName} to cauldron.";
		}
		
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		if (_playerInventory != null)
		{
			int yellowCount = GetPotionCount("yellow_potion");
			int magentaCount = GetPotionCount("magenta_potion");
			int cyanCount = GetPotionCount("cyan_potion");
			
			_potionInventoryLabel.Text = $"Your Potions:\nYellow x{yellowCount}\nMagenta x{magentaCount}\nCyan x{cyanCount}";
			
			_yellowButton.Disabled = yellowCount == 0;
			_magentaButton.Disabled = magentaCount == 0;
			_cyanButton.Disabled = cyanCount == 0;
		}
		
		_feedbackLabel.Text = $"Selected: {_selectedPotions.Count}/3 potions";
		_feedbackLabel.Modulate = Colors.White;
	}

	private int GetPotionCount(string itemId)
	{
		if (_playerInventory == null) return 0;
		
		int total = 0;
		var items = _playerInventory.GetAllItems();
		foreach (var item in items)
		{
			if (item != null && item.Data.ItemId == itemId)
			{
				total += item.Quantity;
			}
		}
		return total;
	}

	private void OnMixPressed()
	{
		if (_selectedPotions.Count != 3)
		{
			_feedbackLabel.Text = "You need all 3 secondary potions!";
			_feedbackLabel.Modulate = Colors.Orange;
			return;
		}
		
		if (!_selectedPotions.Contains("yellow_potion") || 
		    !_selectedPotions.Contains("magenta_potion") || 
		    !_selectedPotions.Contains("cyan_potion"))
		{
			_feedbackLabel.Text = "You need Yellow, Magenta, and Cyan potions!";
			_feedbackLabel.Modulate = Colors.Red;
			return;
		}
		
		// Consume potions
		ConsumePotions();
		
		// Create Teal Potion sebagai usable item
		var tealData = new ItemData("teal_potion", "Teal Potion", 16, true, false); // Stack 16, usable, not key item
		tealData.Description = "Mystical teal potion - Can dissolve magical barriers";
		tealData.UsableBehavior = new TealPotionUsable(); // Set usable behavior
		_playerInventory.AddItem(tealData, 1);
		
		_feedbackLabel.Text = "Success! You created the Teal Potion!";
		_feedbackLabel.Modulate = Colors.Cyan;
		
		_mixButton.Disabled = true;
		
		GetTree().CreateTimer(2.5).Timeout += () => {
			EmitSignal(SignalName.MixingCompleted, true);
			CloseFinalMixingUI();
		};
	}

	private void ConsumePotions()
	{
		foreach (var itemId in _selectedPotions)
		{
			int slotIndex = FindItemSlot(itemId);
			if (slotIndex != -1)
			{
				_playerInventory.RemoveItem(slotIndex, 1);
			}
		}
	}

	private int FindItemSlot(string itemId)
	{
		var items = _playerInventory.GetAllItems();
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i] != null && items[i].Data.ItemId == itemId)
			{
				return i;
			}
		}
		return -1;
	}

	private void OnClearPressed()
	{
		// Clear cauldron - items stay in inventory
		_selectedPotions.Clear();
		_feedbackLabel.Text = "Cauldron cleared. Start fresh!";
		_feedbackLabel.Modulate = Colors.White;
		UpdateDisplay();
		GD.Print("Big cauldron cleared");
	}

	private void CloseFinalMixingUI()
	{
		Visible = false;
		InventoryUI.IsAnyPanelOpen = false; // Clear global flag
		Input.MouseMode = Input.MouseModeEnum.Captured;
		
		// Restore inventory UI dan crosshair ke state sebelumnya
		if (_inventoryUI != null)
		{
			_inventoryUI.RestoreAfterPanel();
			_inventoryUI.SetCrosshairVisible(true);
		}
		
		_mixButton.Disabled = false;
		GD.Print("FinalMixingUI closed");
	}

	public override void _Input(InputEvent @event)
	{
		if (Visible && @event.IsActionPressed("ui_cancel"))
		{
			EmitSignal(SignalName.MixingCompleted, false);
			CloseFinalMixingUI();
			GetViewport().SetInputAsHandled();
		}
	}
}
