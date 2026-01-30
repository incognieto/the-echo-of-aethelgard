using Godot;
using System;
using System.Collections.Generic;

public enum MaterialType
{
	None,
	GreenMoss,      // Lumut Hijau (Green Material)
	RedPowder,      // Serbuk Laba-laba (Red Material)
	BlueExtract,    // Ekstrak Buah Magis (Blue Material)
	YellowPotion,   // Green + Red (Secondary)
	MagentaPotion,  // Red + Blue (Secondary)
	CyanPotion,     // Blue + Green (Secondary)
	TealPotion,     // Yellow + Magenta + Cyan (Final)
	BlackMud        // Failed mixture (all primary at once)
}

public partial class MixingUI : Control
{
	[Signal]
	public delegate void MixingCompletedEventHandler(bool success);

	// UI components
	private Label _titleLabel;
	private Label _feedbackLabel;
	private Label _currentMixtureLabel;
	private Label _storageLabel;
	private Label _materialInventoryLabel; // NEW: Show material quantities from inventory
	private Button _greenButton;
	private Button _redButton;
	private Button _blueButton;
	private Button _yellowButton;
	private Button _magentaButton;
	private Button _cyanButton;
	private Button _mixButton;
	private Button _resetButton;
	private Button _closeButton;
	private InventoryUI _inventoryUI;
	
	// Player inventory reference
	private InventorySystem _playerInventory;
	
	// Mixing state
	private List<MaterialType> _currentMixture = new List<MaterialType>();
	private List<MaterialType> _storage = new List<MaterialType>(); // Secondary potions storage
	private bool _isPuzzleSolved = false;

	public override void _Ready()
	{
		Visible = false;
		
		// Get UI nodes
		var panel = GetNode<Panel>("MixingPanel");
		_titleLabel = panel.GetNode<Label>("TitleLabel");
		_feedbackLabel = panel.GetNode<Label>("FeedbackLabel");
		_currentMixtureLabel = panel.GetNode<Label>("CurrentMixtureLabel");
		_storageLabel = panel.GetNode<Label>("StorageLabel");
		_materialInventoryLabel = panel.GetNodeOrNull<Label>("MaterialInventoryLabel");
		
		// Material buttons
		var materialContainer = panel.GetNode<VBoxContainer>("MaterialContainer");
		_greenButton = materialContainer.GetNode<Button>("GreenButton");
		_redButton = materialContainer.GetNode<Button>("RedButton");
		_blueButton = materialContainer.GetNode<Button>("BlueButton");
		
		var secondaryContainer = panel.GetNode<VBoxContainer>("SecondaryContainer");
		_yellowButton = secondaryContainer.GetNode<Button>("YellowButton");
		_magentaButton = secondaryContainer.GetNode<Button>("MagentaButton");
		_cyanButton = secondaryContainer.GetNode<Button>("CyanButton");
		
		// Action buttons
		_mixButton = panel.GetNode<Button>("MixButton");
		_resetButton = panel.GetNode<Button>("ResetButton");
		_closeButton = panel.GetNode<Button>("CloseButton");
		
		// Connect signals
		_greenButton.Pressed += () => AddMaterial(MaterialType.GreenMoss);
		_redButton.Pressed += () => AddMaterial(MaterialType.RedPowder);
		_blueButton.Pressed += () => AddMaterial(MaterialType.BlueExtract);
		_yellowButton.Pressed += () => AddMaterial(MaterialType.YellowPotion);
		_magentaButton.Pressed += () => AddMaterial(MaterialType.MagentaPotion);
		_cyanButton.Pressed += () => AddMaterial(MaterialType.CyanPotion);
		_mixButton.Pressed += OnMixPressed;
		_resetButton.Pressed += OnResetPressed;
		_closeButton.Pressed += OnClosePressed;
		
		// Find InventoryUI
		CallDeferred(nameof(FindInventoryUI));
		
		UpdateDisplay();
	}
	
	private void FindInventoryUI()
	{
		// MixingUI is child of UI (CanvasLayer)
		var canvasLayer = GetParent() as CanvasLayer;
		if (canvasLayer != null)
		{
			_inventoryUI = canvasLayer.GetNodeOrNull<InventoryUI>("InventoryUI");
			if (_inventoryUI == null)
			{
				GD.PrintErr("MixingUI: InventoryUI not found!");
			}
		}
	}

	public void ShowMixingUI(InventorySystem playerInventory)
	{
		_playerInventory = playerInventory;
		_currentMixture.Clear();
		_storage.Clear();
		_isPuzzleSolved = false;
		
		Visible = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		// Hide inventory UI and crosshair dengan state management
		if (_inventoryUI != null)
		{
			_inventoryUI.HideForPanel();
			_inventoryUI.SetCrosshairVisible(false);
		}
		
		_feedbackLabel.Text = "Synthesize the Teal Potion through tri-color synthesis.";
		_feedbackLabel.Modulate = Colors.White;
		
		UpdateDisplay();
		UpdateButtonAvailability();
		
		GD.Print("MixingUI opened - Tri-Color Synthesis");
	}

	private void AddMaterial(MaterialType material)
	{
		// Check if adding from inventory
		if (IsPrimaryMaterial(material))
		{
			// Check if player has this material in inventory
			string itemId = GetItemIdForMaterial(material);
			if (_playerInventory != null && !_playerInventory.HasItem(itemId))
			{
				_feedbackLabel.Text = $"You don't have {GetMaterialName(material)} in your inventory!";
				_feedbackLabel.Modulate = Colors.Red;
				return;
			}
		}
		else if (IsSecondaryPotion(material))
		{
			// Check if we have this secondary potion in inventory
			string itemId = GetItemIdForPotion(material);
			if (_playerInventory != null && !_playerInventory.HasItem(itemId))
			{
				_feedbackLabel.Text = $"You don't have {GetMaterialName(material)} in your inventory!";
				_feedbackLabel.Modulate = Colors.Red;
				return;
			}
		}
		
		_currentMixture.Add(material);
		_feedbackLabel.Text = $"Added {GetMaterialName(material)} to the cauldron.";
		_feedbackLabel.Modulate = Colors.LightGreen;
		UpdateDisplay();
		UpdateButtonAvailability();
		
		GD.Print($"Added {material} to mixture. Current count: {_currentMixture.Count}");
	}
	
	private void UpdateDisplay()
	{
		// Update current mixture display
		string mixtureText = "Cauldron: ";
		if (_currentMixture.Count > 0)
		{
			mixtureText += string.Join(" + ", _currentMixture.ConvertAll(m => GetMaterialName(m)));
		}
		else
		{
			mixtureText += "Empty";
		}
		_currentMixtureLabel.Text = mixtureText;
		
		// Update storage display
		string storageText = "Storage: ";
		if (_storage.Count > 0)
		{
			var counts = new Dictionary<MaterialType, int>();
			foreach (var item in _storage)
			{
				if (!counts.ContainsKey(item))
					counts[item] = 0;
				counts[item]++;
			}
			
			var parts = new List<string>();
			foreach (var kvp in counts)
			{
				parts.Add($"{GetMaterialName(kvp.Key)} x{kvp.Value}");
			}
			storageText += string.Join(", ", parts);
		}
		else
		{
			storageText += "Empty";
		}
		_storageLabel.Text = storageText;
		
		// Update material inventory display
		if (_materialInventoryLabel != null && _playerInventory != null)
		{
			int greenCount = GetMaterialCountInInventory("green_moss");
			int redCount = GetMaterialCountInInventory("red_powder");
			int blueCount = GetMaterialCountInInventory("blue_extract");
			
			_materialInventoryLabel.Text = $"Materials: Green x{greenCount} | Red x{redCount} | Blue x{blueCount}";
		}
	}
	
	private int GetMaterialCountInInventory(string itemId)
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
	
	private void UpdateButtonAvailability()
	{
		// Update primary material buttons based on inventory
		if (_playerInventory != null)
		{
			_greenButton.Disabled = !_playerInventory.HasItem("green_moss");
			_redButton.Disabled = !_playerInventory.HasItem("red_powder");
			_blueButton.Disabled = !_playerInventory.HasItem("blue_extract");
		}
		
		// Update secondary potion buttons based on inventory (not storage)
		if (_playerInventory != null)
		{
			_yellowButton.Disabled = !_playerInventory.HasItem("yellow_potion");
			_magentaButton.Disabled = !_playerInventory.HasItem("magenta_potion");
			_cyanButton.Disabled = !_playerInventory.HasItem("cyan_potion");
		}
	}

	private void OnMixPressed()
	{
		if (_currentMixture.Count == 0)
		{
			_feedbackLabel.Text = "Add materials to the cauldron first!";
			_feedbackLabel.Modulate = Colors.Yellow;
			return;
		}
		
		MaterialType result = ValidateMixture();
		
		if (result == MaterialType.BlackMud)
		{
			// Failed - tried to mix all primaries at once
			_feedbackLabel.Text = "💀 FAILURE! The mixture turned into useless Black Mud! You can't mix all primaries at once!";
			_feedbackLabel.Modulate = Colors.DarkRed;
			
			// Consume materials from inventory
			ConsumeMaterials();
			
			_currentMixture.Clear();
			UpdateDisplay();
			UpdateButtonAvailability();
			GD.Print("Mixture failed - Black Mud created");
		}
		else if (result == MaterialType.TealPotion)
		{
			// Success!
			_feedbackLabel.Text = "✓ SUCCESS! You created the Teal Potion! The perfect rust dissolver!";
			_feedbackLabel.Modulate = Colors.Cyan;
			_isPuzzleSolved = true;
			
			// Consume secondary potions from inventory
			ConsumeSecondaryPotions();
			
			// Add Teal Potion to inventory
			if (_playerInventory != null)
			{
				var tealData = new ItemData("teal_potion", "Teal Potion", 1);
				_playerInventory.AddItem(tealData, 1);
			}
			
			_currentMixture.Clear();
			UpdateDisplay();
			DisableButtons();
			
			// Auto close after delay
			GetTree().CreateTimer(2.5).Timeout += () => {
				EmitSignal(SignalName.MixingCompleted, true);
				CloseMixingUI();
			};
		}
		else if (IsSecondaryPotion(result))
		{
			// Valid secondary potion created - add to player inventory
			string potionItemId = GetItemIdForPotion(result);
			if (_playerInventory != null)
			{
				var potionData = new ItemData(potionItemId, GetMaterialName(result), 10);
				bool added = _playerInventory.AddItem(potionData, 1);
				if (added)
				{
					GD.Print($"✓ Added {result} to player inventory");
					_feedbackLabel.Text = $"✓ Created {GetMaterialName(result)}! Added to your inventory.";
					_feedbackLabel.Modulate = Colors.LightGreen;
				}
				else
				{
					_feedbackLabel.Text = "Inventory full! Cannot store potion.";
					_feedbackLabel.Modulate = Colors.Red;
				}
			}
			
			// Consume materials from inventory
			ConsumeMaterials();
			
			_currentMixture.Clear();
			UpdateDisplay();
			UpdateButtonAvailability();
			GD.Print($"Created secondary potion: {result}");
		}
		else
		{
			// Invalid mixture
			_feedbackLabel.Text = "Invalid mixture! Check the recipe book.";
			_feedbackLabel.Modulate = Colors.Orange;
		}
	}
	
	private MaterialType ValidateMixture()
	{
		if (_currentMixture.Count == 2)
		{
			// Check combinations regardless of order using Contains
			bool hasGreen = _currentMixture.Contains(MaterialType.GreenMoss);
			bool hasRed = _currentMixture.Contains(MaterialType.RedPowder);
			bool hasBlue = _currentMixture.Contains(MaterialType.BlueExtract);
			
			// Check for secondary potions (2 primary materials)
			if (hasGreen && hasRed)
			{
				GD.Print("Recipe matched: Green + Red = Yellow Potion");
				return MaterialType.YellowPotion; // Green + Red = Yellow
			}
			else if (hasRed && hasBlue)
			{
				GD.Print("Recipe matched: Red + Blue = Magenta Potion");
				return MaterialType.MagentaPotion; // Red + Blue = Magenta
			}
			else if (hasBlue && hasGreen)
			{
				GD.Print("Recipe matched: Blue + Green = Cyan Potion");
				return MaterialType.CyanPotion; // Blue + Green = Cyan
			}
		}
		else if (_currentMixture.Count == 3)
		{
			// Check for all primaries at once (FAILURE)
			if (_currentMixture.Contains(MaterialType.GreenMoss) && 
			    _currentMixture.Contains(MaterialType.RedPowder) && 
			    _currentMixture.Contains(MaterialType.BlueExtract))
			{
				return MaterialType.BlackMud; // FAILED
			}
			
			// Check for final synthesis (3 secondary potions)
			if (_currentMixture.Contains(MaterialType.YellowPotion) && 
			    _currentMixture.Contains(MaterialType.MagentaPotion) && 
			    _currentMixture.Contains(MaterialType.CyanPotion))
			{
				return MaterialType.TealPotion; // SUCCESS!
			}
		}
		
		return MaterialType.None; // Invalid
	}
	
	private void ConsumeMaterials()
	{
		// Remove primary materials from player inventory
		foreach (var material in _currentMixture)
		{
			if (IsPrimaryMaterial(material))
			{
				string itemId = GetItemIdForMaterial(material);
				if (_playerInventory != null)
				{
					// Find the item slot
					int slotIndex = FindItemSlot(itemId);
					if (slotIndex != -1)
					{
						_playerInventory.RemoveItem(slotIndex, 1);
						GD.Print($"Consumed {itemId} from inventory slot {slotIndex}");
					}
				}
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
	
	private bool IsPrimaryMaterial(MaterialType material)
	{
		return material == MaterialType.GreenMoss || 
		       material == MaterialType.RedPowder || 
		       material == MaterialType.BlueExtract;
	}
	
	private bool IsSecondaryPotion(MaterialType material)
	{
		return material == MaterialType.YellowPotion || 
		       material == MaterialType.MagentaPotion || 
		       material == MaterialType.CyanPotion;
	}
	
	private string GetItemIdForMaterial(MaterialType material)
	{
		switch (material)
		{
			case MaterialType.GreenMoss:
				return "green_moss";
			case MaterialType.RedPowder:
				return "red_powder";
			case MaterialType.BlueExtract:
				return "blue_extract";
			default:
				return "";
		}
	}
	
	private string GetItemIdForPotion(MaterialType potion)
	{
		switch (potion)
		{
			case MaterialType.YellowPotion:
				return "yellow_potion";
			case MaterialType.MagentaPotion:
				return "magenta_potion";
			case MaterialType.CyanPotion:
				return "cyan_potion";
			case MaterialType.TealPotion:
				return "teal_potion";
			default:
				return "";
		}
	}
	
	private void ConsumeSecondaryPotions()
	{
		// Remove yellow, magenta, cyan potions from inventory
		foreach (var material in _currentMixture)
		{
			if (IsSecondaryPotion(material))
			{
				string itemId = GetItemIdForPotion(material);
				int slotIndex = FindItemSlot(itemId);
				if (slotIndex != -1 && _playerInventory != null)
				{
					_playerInventory.RemoveItem(slotIndex, 1);
					GD.Print($"Consumed {itemId} from inventory slot {slotIndex}");
				}
			}
		}
	}
	
	private string GetMaterialName(MaterialType material)
	{
		switch (material)
		{
			case MaterialType.GreenMoss:
				return "Green Moss";
			case MaterialType.RedPowder:
				return "Red Powder";
			case MaterialType.BlueExtract:
				return "Blue Extract";
			case MaterialType.YellowPotion:
				return "Yellow Potion";
			case MaterialType.MagentaPotion:
				return "Magenta Potion";
			case MaterialType.CyanPotion:
				return "Cyan Potion";
			case MaterialType.TealPotion:
				return "Teal Potion";
			case MaterialType.BlackMud:
				return "Black Mud";
			default:
				return "Unknown";
		}
	}

	private void OnResetPressed()
	{
		// Just clear cauldron - items stay in inventory
		_currentMixture.Clear();
		_feedbackLabel.Text = "Cauldron cleared. Start fresh!";
		_feedbackLabel.Modulate = Colors.White;
		UpdateDisplay();
		UpdateButtonAvailability();
		GD.Print("Mixture reset");
	}
	
	private void DisableButtons()
	{
		_greenButton.Disabled = true;
		_redButton.Disabled = true;
		_blueButton.Disabled = true;
		_yellowButton.Disabled = true;
		_magentaButton.Disabled = true;
		_cyanButton.Disabled = true;
		_mixButton.Disabled = true;
		_resetButton.Disabled = true;
	}
	
	private void EnableButtons()
	{
		// Will be re-enabled based on availability when UI opens again
		_mixButton.Disabled = false;
		_resetButton.Disabled = false;
	}

	private void OnClosePressed()
	{
		EmitSignal(SignalName.MixingCompleted, false);
		CloseMixingUI();
	}

	private void CloseMixingUI()
	{
		Visible = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		
		// Restore inventory UI dan crosshair ke state sebelumnya
		if (_inventoryUI != null)
		{
			_inventoryUI.RestoreAfterPanel();
			_inventoryUI.SetCrosshairVisible(true);
		}
		
		// Re-enable buttons for next time
		EnableButtons();
		
		GD.Print("MixingUI closed");
	}

	public override void _Input(InputEvent @event)
	{
		if (Visible && @event.IsActionPressed("ui_cancel"))
		{
			OnClosePressed();
			GetViewport().SetInputAsHandled();
		}
	}
}
