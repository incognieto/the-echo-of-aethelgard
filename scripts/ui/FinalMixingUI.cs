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
	private Label _escInstructionLabel; // ESC to return instruction
	private BaseButton _yellowButton;
	private BaseButton _magentaButton;
	private BaseButton _cyanButton;
	private BaseButton _mixButton;
	private BaseButton _clearButton;
	private Button _backButton;
	private InventoryUI _inventoryUI;
	private InventorySystem _playerInventory;
	
	private List<string> _selectedPotions = new List<string>();
	
	// Button visual state tracking
	private bool _yellowButtonClicked = false;
	private bool _magentaButtonClicked = false;
	private bool _cyanButtonClicked = false;
	private Color _buttonNormalColor = new Color(1, 1, 1, 1); // White
	private Color _buttonClickedColor = new Color(0.5f, 1, 0.5f, 1); // Light green tint

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
		
		// Create ESC instruction label
		CreateEscInstructionLabel(panel);
		
		// Create Back button
		CreateBackButton(panel);
		
		CallDeferred(nameof(FindInventoryUI));
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
		{
			if (Visible)
			{
				CloseFinalMixingUI();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void CreateEscInstructionLabel(Panel panel)
	{
		// ===== LAYOUT CONFIGURATION =====
		// ESC instruction label positioning and styling
		// Adjust these values to change the appearance and position of the ESC instruction
		_escInstructionLabel = new Label();
		_escInstructionLabel.Text = "(Esc) to return";
		_escInstructionLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_escInstructionLabel.AddThemeColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f, 0.8f)); // White with slight transparency
		_escInstructionLabel.AddThemeFontSizeOverride("font_size", 18);
		_escInstructionLabel.Position = new Vector2(10, 10); // Top-left corner of panel
		// ===== END CONFIGURATION =====
		
		panel.AddChild(_escInstructionLabel);
	}
	
	private void CreateBackButton(Panel panel)
	{
		_backButton = new Button();
		_backButton.Text = "Back";
		_backButton.CustomMinimumSize = new Vector2(100, 35);
		_backButton.Position = new Vector2(panel.Size.X - 110, 10); // Top-right corner
		_backButton.Pressed += CloseFinalMixingUI;
		
		panel.AddChild(_backButton);
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
		
		ResetButtonVisuals(); // Reset button state when opening UI
		UpdateDisplay();
		GD.Print("FinalMixingUI opened");
	}
	
	private void ResetButtonVisuals()
	{
		_yellowButtonClicked = false;
		_magentaButtonClicked = false;
		_cyanButtonClicked = false;
		
		_yellowButton.Modulate = _buttonNormalColor;
		_magentaButton.Modulate = _buttonNormalColor;
		_cyanButton.Modulate = _buttonNormalColor;
	}

	private void TogglePotion(string itemId, string potionName)
	{
		// Determine which button was clicked
		BaseButton clickedButton = null;
		bool wasClicked = false;
		
		if (itemId == "yellow_potion")
		{
			clickedButton = _yellowButton;
			wasClicked = _yellowButtonClicked;
		}
		else if (itemId == "magenta_potion")
		{
			clickedButton = _magentaButton;
			wasClicked = _magentaButtonClicked;
		}
		else if (itemId == "cyan_potion")
		{
			clickedButton = _cyanButton;
			wasClicked = _cyanButtonClicked;
		}
		
		if (_selectedPotions.Contains(itemId))
		{
			_selectedPotions.Remove(itemId);
			_feedbackLabel.Text = $"Removed {potionName} from cauldron.";
			
			// Reset button visual state
			if (clickedButton != null)
			{
				clickedButton.Modulate = _buttonNormalColor;
				if (itemId == "yellow_potion") _yellowButtonClicked = false;
				else if (itemId == "magenta_potion") _magentaButtonClicked = false;
				else if (itemId == "cyan_potion") _cyanButtonClicked = false;
			}
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
			
			// Set button clicked visual state
			if (clickedButton != null)
			{
				clickedButton.Modulate = _buttonClickedColor;
				if (itemId == "yellow_potion") _yellowButtonClicked = true;
				else if (itemId == "magenta_potion") _magentaButtonClicked = true;
				else if (itemId == "cyan_potion") _cyanButtonClicked = true;
			}
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
		
		// Create Teal Potion
		var tealData = new ItemData("teal_potion", "Teal Potion", 1);
		_playerInventory.AddItem(tealData, 1);
		
		_feedbackLabel.Text = "Success! You created the Teal Potion!";
		_feedbackLabel.Modulate = Colors.Cyan;
		
		ResetButtonVisuals(); // Reset button visual state after success
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
		ResetButtonVisuals(); // Reset button visual state
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
}
