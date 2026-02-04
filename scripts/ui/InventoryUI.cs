using Godot;
using System;

/// <summary>
/// InventoryUI dengan support [Tool] mode untuk visual editing di Godot Editor.
/// 
/// CARA MENGATUR LAYOUT UI:
/// 1. Buka Main.tscn di Godot Editor
/// 2. Pilih node UI/InventoryUI
/// 3. Lihat Inspector panel di kanan
/// 4. Ubah nilai-nilai Export variable:
///    - HotbarPosition: Geser X untuk kiri/kanan, Y untuk atas/bawah
///    - SlotSize: Ubah ukuran slot (misal: 100x100 untuk slot lebih besar)
///    - InventoryPanelSize: Ubah ukuran panel inventory keseluruhan
///    - SlotSpacing: Ubah jarak antar slot
/// 5. Run game untuk melihat perubahan (perubahan terlihat saat runtime)
/// 
/// Contoh:
/// - Hotbar lebih ke kanan: HotbarPosition.X = -100 (dari -180)
/// - Hotbar lebih tinggi: HotbarPosition.Y = -150 (dari -100)
/// - Slot lebih besar: SlotSize = (100, 100)
/// </summary>
public partial class InventoryUI : Control
{
	// Global state untuk tracking panel yang terbuka
	public static bool IsAnyPanelOpen { get; set; } = false;
	
	// Export untuk konfigurasi GUI di Godot Editor
	[Export] public int InventoryColumns = 4; // 4x4 grid
	[Export] public int InventoryRows = 4;
	[Export] public int HotbarSlots = 4; // 1x4 hotbar
	[Export] public Vector2 SlotSize = new Vector2(80, 80);
	[Export] public int SlotSpacing = 5;
	[Export] public Vector2 InventoryPanelSize = new Vector2(450, 450);
	[Export] public Vector2 HotbarPanelSize = new Vector2(360, 90);
	[Export] public Vector2 HotbarPosition = new Vector2(-180, -100); // Position dari center bottom
	
	private InventorySystem _inventory;
	private GridContainer _inventoryGrid; // Legacy - deprecated
	private Control _inventorySlotsContainer; // New: Container for 16 individual panels
	private GridContainer _hotbarGrid;
	private Panel _hotbarContainer;
	private Label _selectedItemLabel;
	private bool _isVisible = false;
	private Control _crosshairContainer;
	private FontFile _customFont; // Custom font for all labels
	private Label _ancientBookNotification; // Notification for ancient book
	private Button _backButton; // Back button
	
	// State management untuk panel lain
	private bool _wasVisibleBeforePanel = false;
	
	// Drag and drop state
	private bool _isDragging = false;
	private int _draggedSlotIndex = -1;
	private Panel _draggedFromSlot = null;
	private Control _dragPreview = null;
	private Vector2 _dragStartPosition = Vector2.Zero;

	public override void _Ready()
	{
		// Load custom font
		_customFont = GD.Load<FontFile>("res://assets/fonts/BLKCHCRY.TTF");
		
		// Setup UI Container
		SetAnchorsPreset(LayoutPreset.FullRect);
		
		// Calculate total slots once
		int totalSlots = InventoryColumns * InventoryRows;
		
		// Try to get nodes from scene first (node-based UI)
		var panel = GetNodeOrNull<Panel>("InventoryPanel");
		Label title = null;
		
		if (panel != null)
		{
			// Node-based UI - nodes sudah ada di .tscn
			GD.Print("InventoryUI: Using node-based UI from .tscn");
			
			title = panel.GetNodeOrNull<Label>("TitleLabel");
			_inventorySlotsContainer = panel.GetNodeOrNull<Control>("InventorySlots");
			_selectedItemLabel = panel.GetNodeOrNull<Label>("SelectedItemLabel");
			
			// Update dynamic values
			if (title != null)
			{
				title.Text = $"Inventory ({totalSlots} Slots)";
			}
			
			// Hide inventory panel initially (hotbar stays visible)
			panel.Visible = false;
			
			// Update panel size using offsets (panel already has CENTER anchor preset)
			float halfWidth = InventoryPanelSize.X / 2;
			float halfHeight = InventoryPanelSize.Y / 2;
			panel.OffsetLeft = -halfWidth;
			panel.OffsetTop = -halfHeight;
			panel.OffsetRight = halfWidth;
			panel.OffsetBottom = halfHeight;
			
			// Update instructions
			var instructions = panel.GetNodeOrNull<Label>("InstructionsLabel");
			if (instructions != null)
			{
				string hotbarKeys = HotbarSlots == 4 ? "1-4" : "1-6";
				instructions.Text = $"E: Pickup | Q: Drop 1 | Ctrl+Q: Drop All | F: Use Item | Tab/I: Toggle Inventory | {hotbarKeys}: Select Hotbar";
			}
			
			// Create Back button
			CreateBackButton(panel);
		}
		else
		{
			// Fallback: Script-generated UI (untuk backward compatibility)
			GD.Print("InventoryUI: Generating UI via script (fallback mode)");
			
			panel = new Panel();
			panel.SetAnchorsPreset(LayoutPreset.Center);
			panel.CustomMinimumSize = InventoryPanelSize;
			panel.Position = new Vector2(-InventoryPanelSize.X / 2, -InventoryPanelSize.Y / 2);
			AddChild(panel);
			
			// Title
			title = new Label();
			title.Text = $"Inventory ({totalSlots} Slots)";
			title.HorizontalAlignment = HorizontalAlignment.Center;
			if (_customFont != null) title.AddThemeFontOverride("font", _customFont);
			title.AddThemeFontSizeOverride("font_size", 24);
			title.Position = new Vector2(10, 10);
			title.Size = new Vector2(InventoryPanelSize.X - 20, 40);
			panel.AddChild(title);
			
			// Main inventory slots container (for PNG assets)
			_inventorySlotsContainer = new Control();
			_inventorySlotsContainer.CustomMinimumSize = new Vector2(360, 360);
			_inventorySlotsContainer.Position = new Vector2(45, 50);
			_inventorySlotsContainer.Size = new Vector2(360, 360);
			panel.AddChild(_inventorySlotsContainer);
			
			// Selected item info
			_selectedItemLabel = new Label();
			_selectedItemLabel.Text = "";
			_selectedItemLabel.HorizontalAlignment = HorizontalAlignment.Center;
			if (_customFont != null) _selectedItemLabel.AddThemeFontOverride("font", _customFont);
			_selectedItemLabel.Position = new Vector2(10, 415);
			_selectedItemLabel.Size = new Vector2(InventoryPanelSize.X - 20, 15);
			panel.AddChild(_selectedItemLabel);
			
			// Instructions
			var instructions = new Label();
			string hotbarKeys = HotbarSlots == 4 ? "1-4" : "1-6";
			instructions.Text = $"E: Pickup | Q: Drop 1 | Ctrl+Q: Drop All | F: Use Item | Tab/I: Toggle Inventory | {hotbarKeys}: Select Hotbar";
			instructions.HorizontalAlignment = HorizontalAlignment.Center;
			if (_customFont != null) instructions.AddThemeFontOverride("font", _customFont);
			instructions.AddThemeFontSizeOverride("font_size", 11);
			instructions.Position = new Vector2(10, 430);
			instructions.Size = new Vector2(InventoryPanelSize.X - 20, 10);
			panel.AddChild(instructions);
			
			// Create Back button
			CreateBackButton(panel);
		}
		
		// Setup inventory slots - use existing Panel nodes or create new ones
		if (_inventorySlotsContainer != null)
		{
			for (int i = 0; i < totalSlots; i++)
			{
				Panel slot = _inventorySlotsContainer.GetNodeOrNull<Panel>($"Slot{i}");
				
				if (slot != null)
				{
					// Use existing Panel from .tscn
					SetupInventorySlot(slot, i, false);
				}
				else
				{
					// Fallback: Create new Panel (script-generated mode)
					slot = CreateInventorySlot(i, false);
					_inventorySlotsContainer.AddChild(slot);
				}
			}
		}
		
		// Hotbar (always visible)
		CreateHotbar();
		
		// Add crosshair (always visible)
		//CreateCrosshair();
	
	// Create ancient book notification (top-left)
	CreateAncientBookNotification();
}

private void CreateCrosshair()
{
	// Crosshair container di tengah layar
	_crosshairContainer = new Control();
	_crosshairContainer.Name = "Crosshair";
	_crosshairContainer.SetAnchorsPreset(LayoutPreset.Center);
	_crosshairContainer.MouseFilter = MouseFilterEnum.Ignore;
	GetParent().CallDeferred("add_child", _crosshairContainer);
	
	// Horizontal line
	var hLine = new ColorRect();
	hLine.Color = new Color(1, 1, 1, 0.8f);
	hLine.Size = new Vector2(20, 2);
	hLine.Position = new Vector2(-10, -1);
	hLine.MouseFilter = MouseFilterEnum.Ignore;
	_crosshairContainer.AddChild(hLine);
	
	// Vertical line
	var vLine = new ColorRect();
	vLine.Color = new Color(1, 1, 1, 0.8f);
	vLine.Size = new Vector2(2, 20);
	vLine.Position = new Vector2(-1, -10);
	vLine.MouseFilter = MouseFilterEnum.Ignore;
	_crosshairContainer.AddChild(vLine);
	
	// Center dot
	var dot = new ColorRect();
	dot.Color = new Color(1, 1, 1, 0.9f);
	dot.Size = new Vector2(4, 4);
	dot.Position = new Vector2(-2, -2);
	dot.MouseFilter = MouseFilterEnum.Ignore;
	_crosshairContainer.AddChild(dot);
}

private void CreateAncientBookNotification()
{
	_ancientBookNotification = new Label();
	_ancientBookNotification.Name = "AncientBookNotification";
	_ancientBookNotification.Text = "Press the F key to activate the ancient book.";
	
	// ===== LAYOUT CONFIGURATION =====
	// Position: Top-left corner of screen
	_ancientBookNotification.Position = new Vector2(20, 20);
	
	// Font size: Adjust this to make text bigger/smaller
	_ancientBookNotification.AddThemeFontSizeOverride("font_size", 24); // Changed from 16 to 24
	
	// Color: Yellow/gold notification
	_ancientBookNotification.AddThemeColorOverride("font_color", new Color(1.0f, 0.9f, 0.3f, 1.0f));
	
	// Font: GoudyMediaeval-Regular
	var goudyFont = GD.Load<FontFile>("res://assets/fonts/GoudyMediaeval-Regular.ttf");
	if (goudyFont != null)
	{
		_ancientBookNotification.AddThemeFontOverride("font", goudyFont);
	}
	else
	{
		// Fallback to custom font if GoudyMediaeval not found
		if (_customFont != null) _ancientBookNotification.AddThemeFontOverride("font", _customFont);
	}
	// ================================
	
	_ancientBookNotification.Visible = false; // Hidden by default
	AddChild(_ancientBookNotification);
}

private void CreateBackButton(Panel panel)
{
	_backButton = new Button();
	_backButton.Text = "Back";
	_backButton.CustomMinimumSize = new Vector2(100, 35);
	_backButton.Position = new Vector2(panel.Size.X - 110, 10); // Top-right corner
	_backButton.Pressed += () => {
		if (_isVisible)
		{
			Toggle(); // Close inventory
		}
	};
	
	panel.AddChild(_backButton);
}

public void SetCrosshairVisible(bool visible)
{
	if (_crosshairContainer != null)
	{
		_crosshairContainer.Visible = visible;
	}
}

public void SetHotbarVisible(bool visible)
{
	if (_hotbarContainer != null)
	{
		_hotbarContainer.Visible = visible;
	}
}

private void CreateHotbar()
{
		// Try to get hotbar nodes from scene first
		_hotbarContainer = GetNodeOrNull<Panel>("HotbarPanel");
		
		if (_hotbarContainer != null)
		{
			// Scene-based hotbar - nodes already exist
			GD.Print("InventoryUI: Using scene-based hotbar from .tscn");
			
			var hotbarSlotsContainer = _hotbarContainer.GetNodeOrNull<Control>("HotbarSlots");
			if (hotbarSlotsContainer != null)
			{
				// Setup individual hotbar slot panels
				for (int i = 0; i < HotbarSlots; i++)
				{
					Panel slot = hotbarSlotsContainer.GetNodeOrNull<Panel>($"HotbarSlot{i}");
					if (slot != null)
					{
						SetupHotbarSlot(slot, i);
					}
				}
				return;
			}
		}
		
		// Fallback: Script-generated hotbar
		GD.Print("InventoryUI: Generating hotbar via script (fallback mode)");
		
		_hotbarContainer = new Panel();
		_hotbarContainer.SetAnchorsPreset(LayoutPreset.CenterBottom);
		_hotbarContainer.CustomMinimumSize = HotbarPanelSize;
		_hotbarContainer.Position = HotbarPosition;
		GetParent().CallDeferred("add_child", _hotbarContainer);
		
		_hotbarGrid = new GridContainer();
		_hotbarGrid.Columns = HotbarSlots;
		_hotbarGrid.SetAnchorsPreset(LayoutPreset.FullRect);
		_hotbarGrid.OffsetLeft = 10;
		_hotbarGrid.OffsetTop = 10;
		_hotbarGrid.OffsetRight = -10;
		_hotbarGrid.OffsetBottom = -10;
		_hotbarGrid.AddThemeConstantOverride("h_separation", SlotSpacing);
		_hotbarContainer.AddChild(_hotbarGrid);
		
		for (int i = 0; i < HotbarSlots; i++)
		{
			var slot = CreateHotbarSlot(i);
			_hotbarGrid.AddChild(slot);
		}
	}

	private Panel CreateInventorySlot(int index, bool isHotbar)
	{
		var slot = new Panel();
		slot.CustomMinimumSize = SlotSize;
		slot.Name = $"Slot{index}";
		SetupInventorySlot(slot, index, isHotbar);
		return slot;
	}

	private void SetupInventorySlot(Panel slot, int index, bool isHotbar)
	{
		slot.MouseFilter = MouseFilterEnum.Pass; // Enable mouse input for drag-drop
		
		// Store index in metadata for drag-drop
		slot.SetMeta("SlotIndex", index);
		slot.SetMeta("IsHotbar", isHotbar);
		
		// Setup drag-drop events
		SetupSlotDragDrop(slot);
		
		// Get or create ItemLabel
		Label label = slot.GetNodeOrNull<Label>("ItemLabel");
		if (label == null)
		{
			label = new Label();
			label.Name = "ItemLabel";
			label.HorizontalAlignment = HorizontalAlignment.Center;
			label.VerticalAlignment = VerticalAlignment.Center;
			label.SetAnchorsPreset(LayoutPreset.FullRect);
			if (_customFont != null) label.AddThemeFontOverride("font", _customFont);
			
			// ===== ITEM NAME FONT SIZE =====
			// Adjust this to make item names bigger/smaller in inventory slots
			label.AddThemeFontSizeOverride("font_size", 16); // Changed from 12 to 16
			// ================================
			
			label.AutowrapMode = TextServer.AutowrapMode.Word;
			label.MouseFilter = MouseFilterEnum.Ignore; // Allow panel to receive mouse events
			slot.AddChild(label);
		}
		else
		{
			// Configure existing label
			label.HorizontalAlignment = HorizontalAlignment.Center;
			label.VerticalAlignment = VerticalAlignment.Center;
			label.SetAnchorsPreset(LayoutPreset.FullRect);
			if (_customFont != null) label.AddThemeFontOverride("font", _customFont);
			label.AddThemeFontSizeOverride("font_size", 16); // Changed from 12 to 16
			label.AutowrapMode = TextServer.AutowrapMode.Word;
			label.MouseFilter = MouseFilterEnum.Ignore;
		}
		
		label.Text = "";
	}

	private void SetupHotbarSlot(Panel slot, int index)
	{
		slot.MouseFilter = MouseFilterEnum.Pass;
		
		// Store index in metadata
		slot.SetMeta("SlotIndex", index);
		slot.SetMeta("IsHotbar", true);
		
		// Setup drag-drop events
		SetupSlotDragDrop(slot);
		
		// Get or create ItemLabel
		Label itemLabel = slot.GetNodeOrNull<Label>("ItemLabel");
		if (itemLabel == null)
		{
			itemLabel = new Label();
			itemLabel.Name = "ItemLabel";
			itemLabel.HorizontalAlignment = HorizontalAlignment.Center;
			itemLabel.VerticalAlignment = VerticalAlignment.Center;
			itemLabel.SetAnchorsPreset(LayoutPreset.Center);
			if (_customFont != null) itemLabel.AddThemeFontOverride("font", _customFont);
			
			// ===== HOTBAR ITEM NAME FONT SIZE =====
			// Adjust this to make item names bigger/smaller in hotbar
			itemLabel.AddThemeFontSizeOverride("font_size", 16); // Changed from 12 to 16
			// =======================================
			
			itemLabel.AutowrapMode = TextServer.AutowrapMode.Word;
			itemLabel.MouseFilter = MouseFilterEnum.Ignore;
			slot.AddChild(itemLabel);
		}
		else
		{
			itemLabel.HorizontalAlignment = HorizontalAlignment.Center;
			itemLabel.VerticalAlignment = VerticalAlignment.Center;
			itemLabel.SetAnchorsPreset(LayoutPreset.Center);
			if (_customFont != null) itemLabel.AddThemeFontOverride("font", _customFont);
			itemLabel.AddThemeFontSizeOverride("font_size", 16); // Changed from 12 to 16
			itemLabel.AutowrapMode = TextServer.AutowrapMode.Word;
			itemLabel.MouseFilter = MouseFilterEnum.Ignore;
		}
		
		itemLabel.Text = "";
	}

	private Panel CreateHotbarSlot(int index)
	{
		var slot = new Panel();
		slot.CustomMinimumSize = SlotSize;
		slot.Name = $"HotbarSlot{index}";
		slot.MouseFilter = MouseFilterEnum.Pass; // Enable mouse input for drag-drop
		
		// Store index in metadata
		slot.SetMeta("SlotIndex", index);
		slot.SetMeta("IsHotbar", true);
		
		// Setup drag-drop events
		SetupSlotDragDrop(slot);
		
		// Slot number
		var numberLabel = new Label();
		numberLabel.Text = (index + 1).ToString();
		numberLabel.Position = new Vector2(5, 5);
		if (_customFont != null) numberLabel.AddThemeFontOverride("font", _customFont);
		numberLabel.AddThemeFontSizeOverride("font_size", 12);
		numberLabel.MouseFilter = MouseFilterEnum.Ignore;
		slot.AddChild(numberLabel);
		
		// Item label
		var itemLabel = new Label();
		itemLabel.Name = "ItemLabel";
		itemLabel.Text = "";
		itemLabel.HorizontalAlignment = HorizontalAlignment.Center;
		itemLabel.VerticalAlignment = VerticalAlignment.Center;
		itemLabel.SetAnchorsPreset(LayoutPreset.Center);
		if (_customFont != null) itemLabel.AddThemeFontOverride("font", _customFont);
		itemLabel.AddThemeFontSizeOverride("font_size", 12);
		itemLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		itemLabel.MouseFilter = MouseFilterEnum.Ignore;
		slot.AddChild(itemLabel);
		
		return slot;
	}
	
	public void SetInventory(InventorySystem inventory)
	{
		_inventory = inventory;
		_inventory.InventoryChanged += UpdateDisplay;
		_inventory.HotbarSlotChanged += OnHotbarSlotChanged;
		GD.Print("InventoryUI: Connected to inventory system");
		
		// Delay initial update untuk memastikan hotbar grid sudah ready
		CallDeferred(nameof(DelayedInitialUpdate));
	}
	
	private void DelayedInitialUpdate()
	{
		GD.Print("InventoryUI: Performing delayed initial update...");
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		if (_inventory == null)
		{
			GD.Print("UpdateDisplay: Inventory is NULL!");
			return;
		}
		
		var items = _inventory.GetAllItems();
		if (items == null || items.Count == 0)
		{
			GD.Print("UpdateDisplay: Items list is empty or null!");
			return;
		}
		
		GD.Print($"UpdateDisplay: Refreshing UI with {items.Count} total slots");
		
		// Update main inventory (dynamic slots)
		int totalSlots = InventoryColumns * InventoryRows;
		if (_inventorySlotsContainer != null)
		{
			for (int i = 0; i < totalSlots && i < items.Count; i++)
			{
				Panel slot = _inventorySlotsContainer.GetNodeOrNull<Panel>($"Slot{i}");
				if (slot == null) continue;
				
				var label = slot.GetNodeOrNull<Label>("ItemLabel");
				if (label == null) continue;
				
				if (i < items.Count && items[i] != null)
				{
					string text = items[i].Data.ItemName;
					if (items[i].Quantity > 1)
						text += $"\nx{items[i].Quantity}";
					label.Text = text;
					GD.Print($"  Slot {i}: {items[i].Data.ItemName} x{items[i].Quantity}");
				}
				else
				{
					label.Text = "";
					GD.Print($"  Slot {i}: [Empty]");
				}
			}
		}
		
		// Update hotbar
		UpdateHotbar();
	
	// Update ancient book notification
	UpdateAncientBookNotification();
}

private void UpdateAncientBookNotification()
{
	if (_ancientBookNotification == null || _inventory == null) return;
	
	// Check if player has ancient book in inventory
	bool hasAncientBook = _inventory.HasItem("ancient_book");
	_ancientBookNotification.Visible = hasAncientBook;
}

private void UpdateHotbar()
{
	if (_inventory == null || _hotbarContainer == null)
	{
		GD.Print("UpdateHotbar: Waiting for hotbar to be ready...");
		return;
	}
	
	var items = _inventory.GetAllItems();
	if (items == null || items.Count == 0)
	{
		GD.Print("UpdateHotbar: Items list is empty!");
		return;
	}
	
	GD.Print($"UpdateHotbar: Updating {HotbarSlots} slots from {items.Count} items");
	
	// Try scene-based hotbar first
	var hotbarSlotsContainer = _hotbarContainer.GetNodeOrNull<Control>("HotbarSlots");
	if (hotbarSlotsContainer != null)
	{
		// Scene-based hotbar
		for (int i = 0; i < HotbarSlots && i < items.Count; i++)
		{
			Panel slot = hotbarSlotsContainer.GetNodeOrNull<Panel>($"HotbarSlot{i}");
			if (slot == null) continue;
			
			var label = slot.GetNodeOrNull<Label>("ItemLabel");
			if (label == null) continue;
			
			if (i < items.Count && items[i] != null)
			{
				string text = items[i].Data.ItemName;
				if (items[i].Quantity > 1)
					text += $"\nx{items[i].Quantity}";
				label.Text = text;
			}
			else
			{
				label.Text = "";
			}
			
			// Highlight selected slot
			if (i == _inventory.GetSelectedHotbarSlot())
			{
				slot.AddThemeStyleboxOverride("panel", CreateHighlightStylebox());
			}
			else
			{
				slot.RemoveThemeStyleboxOverride("panel");
			}
		}
	}
	else if (_hotbarGrid != null && _hotbarGrid.GetChildCount() >= HotbarSlots)
	{
		// Fallback: Grid-based hotbar
		for (int i = 0; i < HotbarSlots && i < _hotbarGrid.GetChildCount() && i < items.Count; i++)
		{
			var slot = _hotbarGrid.GetChild(i) as Panel;
			if (slot == null) continue;
			
			var label = slot.GetNode<Label>("ItemLabel");
			
			if (i < items.Count && items[i] != null)
			{
				string text = items[i].Data.ItemName;
				if (items[i].Quantity > 1)
					text += $"\nx{items[i].Quantity}";
				label.Text = text;
			}
			else
			{
				label.Text = "";
			}
			
			// Highlight selected slot
			if (i == _inventory.GetSelectedHotbarSlot())
			{
				slot.AddThemeStyleboxOverride("panel", CreateHighlightStylebox());
			}
			else
			{
				slot.RemoveThemeStyleboxOverride("panel");
			}
	}
	}
}

private StyleBox CreateHighlightStylebox()
{
		var stylebox = new StyleBoxFlat();
		stylebox.BgColor = new Color(1, 1, 0, 0.3f); // Yellow highlight
		stylebox.BorderColor = new Color(1, 1, 0, 1);
		stylebox.BorderWidthLeft = 2;
		stylebox.BorderWidthRight = 2;
		stylebox.BorderWidthTop = 2;
		stylebox.BorderWidthBottom = 2;
		return stylebox;
	}

	private void OnHotbarSlotChanged(int slotIndex)
	{
		UpdateHotbar();
		
		// Update selected item info
		var selectedItem = _inventory.GetSelectedHotbarItem();
		if (selectedItem != null)
		{
			_selectedItemLabel.Text = $"Selected: {selectedItem.Data.ItemName} x{selectedItem.Quantity}";
		}
		else
		{
			_selectedItemLabel.Text = "Selected: Empty";
		}
	}

	public void Toggle()
	{
		// Jangan bisa toggle inventory jika panel lain sedang terbuka
		if (IsAnyPanelOpen && !_isVisible)
		{
			GD.Print("Cannot open inventory - another panel is open");
			return;
		}
		
		_isVisible = !_isVisible;
		
		// Toggle InventoryPanel visibility (hotbar always visible)
		var panel = GetNodeOrNull<Panel>("InventoryPanel");
		if (panel != null)
		{
			panel.Visible = _isVisible;
		}
		
		// Toggle background 4x4 visibility
		var background4x4 = GetNodeOrNull<TextureRect>("Background4x4");
		if (background4x4 != null)
		{
			background4x4.Visible = _isVisible;
		}
		
		if (_isVisible)
		{
			// Inventory opened
			IsAnyPanelOpen = true; // Set global flag
			Input.MouseMode = Input.MouseModeEnum.Visible;
			SetCrosshairVisible(false);
			
			// Disable camera rotation
			var player = GetTree().Root.GetNodeOrNull<CharacterBody3D>("Main/Player");
			if (player != null)
			{
				player.SetMeta("inventory_open", true);
			}
		}
		else
		{
			// Inventory closed
			IsAnyPanelOpen = false; // Clear global flag
			Input.MouseMode = Input.MouseModeEnum.Captured;
			SetCrosshairVisible(true);
			
			// Enable camera rotation
			var player = GetTree().Root.GetNodeOrNull<CharacterBody3D>("Main/Player");
			if (player != null)
			{
				player.SetMeta("inventory_open", false);
			}
		}
	}

	public bool IsInventoryVisible()
	{
		return _isVisible;
	}
	
	// Method untuk panel lain hide/show inventory dengan state management
	public void HideForPanel()
	{
		_wasVisibleBeforePanel = _isVisible;
		if (_isVisible)
		{
			_isVisible = false;
			var panel = GetNodeOrNull<Panel>("InventoryPanel");
			if (panel != null) panel.Visible = false;
		}
	}
	
	public void RestoreAfterPanel()
	{
		if (_wasVisibleBeforePanel)
		{
			_isVisible = true;
			var panel = GetNodeOrNull<Panel>("InventoryPanel");
			if (panel != null) panel.Visible = true;
		}
		_wasVisibleBeforePanel = false;
	}
	
	public override void _Input(InputEvent @event)
	{
		if (_isVisible && @event.IsActionPressed("ui_cancel"))
		{
			Toggle();
			GetViewport().SetInputAsHandled();
		}
		
		// Handle drag cancellation (right click or ESC while dragging)
		if (_isDragging)
		{
			if (@event is InputEventMouseButton mouseButton)
			{
				if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
				{
					// Cancel drag on right click
					GD.Print("Drag cancelled by right click");
					CleanupDragState();
					GetViewport().SetInputAsHandled();
				}
			}
			else if (@event.IsActionPressed("ui_cancel"))
			{
				// Cancel drag on ESC
				GD.Print("Drag cancelled by ESC");
				CleanupDragState();
				GetViewport().SetInputAsHandled();
			}
		}
	}
	
	public override void _Process(double delta)
	{
		// Update drag preview position to follow mouse
		if (_isDragging && _dragPreview != null)
		{
			_dragPreview.GlobalPosition = GetGlobalMousePosition() - _dragPreview.Size / 2;
		}
		
		// Detect mouse release to handle drop
		if (_isDragging && Input.IsMouseButtonPressed(MouseButton.Left) == false)
		{
			// Mouse button released - find which slot we're over and drop there
			Panel targetSlot = FindSlotUnderMouse();
			
			if (targetSlot != null)
			{
				// Valid drop target found
				int targetSlotIndex = (int)targetSlot.GetMeta("SlotIndex");
				
				// Perform the swap
				if (_draggedSlotIndex != targetSlotIndex)
				{
					_inventory.SwapItems(_draggedSlotIndex, targetSlotIndex);
					GD.Print($"✓ Swapped slot {_draggedSlotIndex} with slot {targetSlotIndex}");
				}
				else
				{
					GD.Print("Same slot - no swap needed");
				}
			}
			else
			{
				GD.Print("Drag cancelled - released outside valid slot area");
			}
			
			// Always cleanup drag state after release
			CleanupDragState();
		}
	}
	
	private Panel FindSlotUnderMouse()
	{
		var mousePos = GetGlobalMousePosition();
		
		// Check all inventory slots
		if (_inventorySlotsContainer != null)
		{
			int totalSlots = InventoryColumns * InventoryRows;
			for (int i = 0; i < totalSlots; i++)
			{
				var slot = _inventorySlotsContainer.GetNodeOrNull<Panel>($"Slot{i}");
				if (slot != null && slot.GetGlobalRect().HasPoint(mousePos))
				{
					return slot;
				}
			}
		}
		
		// Check hotbar slots
		if (_hotbarContainer != null)
		{
			var hotbarSlotsContainer = _hotbarContainer.GetNodeOrNull<Control>("HotbarSlots");
			if (hotbarSlotsContainer != null)
			{
				for (int i = 0; i < HotbarSlots; i++)
				{
					var slot = hotbarSlotsContainer.GetNodeOrNull<Panel>($"HotbarSlot{i}");
					if (slot != null && slot.GetGlobalRect().HasPoint(mousePos))
					{
						return slot;
					}
				}
			}
			else if (_hotbarGrid != null)
			{
				// Fallback: script-generated hotbar
				for (int i = 0; i < HotbarSlots && i < _hotbarGrid.GetChildCount(); i++)
				{
					var slot = _hotbarGrid.GetChild(i) as Panel;
					if (slot != null && slot.GetGlobalRect().HasPoint(mousePos))
					{
						return slot;
					}
				}
			}
		}
		
		return null;
	}
	
	// Setup drag-drop event for a slot
	private void SetupSlotDragDrop(Panel slot)
	{
		slot.GuiInput += (InputEvent @event) => OnSlotGuiInput(slot, @event);
	}
	
	private void OnSlotGuiInput(Panel slot, InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
			{
				// Start dragging on left click press
				OnSlotDragStart(slot);
			}
			// Note: Drop is handled in _Process() when mouse button is released
		}
	}
	
	private void OnSlotDragStart(Panel slot)
	{
		if (_inventory == null) return;
		
		int slotIndex = (int)slot.GetMeta("SlotIndex");
		var item = _inventory.GetItem(slotIndex);
		
		// Only drag if slot has an item
		if (item == null)
		{
			GD.Print($"Cannot drag slot {slotIndex} - empty");
			return;
		}
		
		_isDragging = true;
		_draggedSlotIndex = slotIndex;
		_draggedFromSlot = slot;
		_dragStartPosition = slot.GlobalPosition;
		
		// Create visual preview
		CreateDragPreview(item);
		
		GD.Print($"✓ Started dragging from slot {slotIndex}: {item.Data.ItemName} x{item.Quantity}");
	}
	
	private void CreateDragPreview(InventoryItem item)
	{
		// Create a floating preview panel
		_dragPreview = new Panel();
		_dragPreview.CustomMinimumSize = SlotSize;
		_dragPreview.Size = SlotSize;
		_dragPreview.MouseFilter = MouseFilterEnum.Ignore;
		
		// Add semi-transparent background
		var stylebox = new StyleBoxFlat();
		stylebox.BgColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
		stylebox.BorderColor = new Color(1, 1, 0, 1);
		stylebox.BorderWidthLeft = 2;
		stylebox.BorderWidthRight = 2;
		stylebox.BorderWidthTop = 2;
		stylebox.BorderWidthBottom = 2;
		_dragPreview.AddThemeStyleboxOverride("panel", stylebox);
		
		// Add item label
		var label = new Label();
		label.Text = item.Data.ItemName;
		if (item.Quantity > 1)
			label.Text += $"\nx{item.Quantity}";
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.SetAnchorsPreset(LayoutPreset.FullRect);
		if (_customFont != null) label.AddThemeFontOverride("font", _customFont);
		label.AddThemeFontSizeOverride("font_size", 12);
		label.AutowrapMode = TextServer.AutowrapMode.Word;
		_dragPreview.AddChild(label);
		
		// Add to scene (top layer)
		AddChild(_dragPreview);
		_dragPreview.GlobalPosition = GetGlobalMousePosition() - _dragPreview.Size / 2;
	}
	

	private void CleanupDragState()
	{
		_isDragging = false;
		_draggedSlotIndex = -1;
		_draggedFromSlot = null;
		_dragStartPosition = Vector2.Zero;
		
		// Remove drag preview
		if (_dragPreview != null)
		{
			_dragPreview.QueueFree();
			_dragPreview = null;
		}
	}
}
