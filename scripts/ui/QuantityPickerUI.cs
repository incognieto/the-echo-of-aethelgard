using Godot;
using System;

public partial class QuantityPickerUI : Control
{
	[Signal]
	public delegate void QuantitySelectedEventHandler(int quantity);
	
	[Signal]
	public delegate void PickupCancelledEventHandler();

	private Label _titleLabel;
	private Label _quantityLabel;
	private BaseButton _minusButton;
	private BaseButton _plusButton;
	private BaseButton _confirmButton;
	private BaseButton _cancelButton;
	private InventoryUI _inventoryUI;
	
	private int _currentQuantity = 1;
	private int _minQuantity = 1;
	private int _maxQuantity = 10;
	private string _itemName = "";

	public override void _Ready()
	{
		GD.Print("QuantityPickerUI._Ready() called");
		Visible = false;
		
		// Get UI nodes
		var panel = GetNodeOrNull<Panel>("PickerPanel");
		if (panel == null)
		{
			GD.PrintErr("QuantityPickerUI: PickerPanel not found!");
			return;
		}
		
		_titleLabel = panel.GetNodeOrNull<Label>("TitleLabel");
		_quantityLabel = panel.GetNodeOrNull<Label>("QuantityLabel");
		_minusButton = panel.GetNodeOrNull<BaseButton>("MinusButton");
		_plusButton = panel.GetNodeOrNull<BaseButton>("PlusButton");
		_confirmButton = panel.GetNodeOrNull<BaseButton>("ConfirmButton");
		_cancelButton = panel.GetNodeOrNull<BaseButton>("CancelButton");
		
		if (_titleLabel == null || _quantityLabel == null || _minusButton == null || 
		    _plusButton == null || _confirmButton == null || _cancelButton == null)
		{
			GD.PrintErr("QuantityPickerUI: Some UI nodes not found!");
			GD.PrintErr($"  TitleLabel: {_titleLabel != null}");
			GD.PrintErr($"  QuantityLabel: {_quantityLabel != null}");
			GD.PrintErr($"  MinusButton: {_minusButton != null}");
			GD.PrintErr($"  PlusButton: {_plusButton != null}");
			GD.PrintErr($"  ConfirmButton: {_confirmButton != null}");
			GD.PrintErr($"  CancelButton: {_cancelButton != null}");
			return;
		}
		
		// Connect signals
		_minusButton.Pressed += OnMinusPressed;
		_plusButton.Pressed += OnPlusPressed;
		_confirmButton.Pressed += OnConfirmPressed;
		_cancelButton.Pressed += OnCancelPressed;
		
		// Find InventoryUI
		CallDeferred(nameof(FindInventoryUI));
		
		GD.Print("✓ QuantityPickerUI ready!");
	}
	
	private void FindInventoryUI()
	{
		// QuantityPickerUI is child of UI (CanvasLayer)
		var canvasLayer = GetParent() as CanvasLayer;
		if (canvasLayer != null)
		{
			_inventoryUI = canvasLayer.GetNodeOrNull<InventoryUI>("InventoryUI");
			if (_inventoryUI == null)
			{
				GD.PrintErr("QuantityPickerUI: InventoryUI not found!");
			}
		}
	}

	public void ShowPicker(string itemName, int maxQuantity = 10)
	{
		GD.Print($"QuantityPickerUI.ShowPicker called - Item: {itemName}, MaxQty: {maxQuantity}");
		
		_itemName = itemName;
		_maxQuantity = maxQuantity;
		_currentQuantity = 1;
		
		_titleLabel.Text = $"Pick up {itemName}";
		UpdateQuantityDisplay();
		
		Visible = true;
		InventoryUI.IsAnyPanelOpen = true; // Set global flag
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		GD.Print($"QuantityPickerUI visibility set to: {Visible}");
		
		// Hide inventory UI dan crosshair dengan state management
		if (_inventoryUI != null)
		{
			_inventoryUI.HideForPanel();
			_inventoryUI.SetCrosshairVisible(false);
		}
		
		GD.Print($"QuantityPickerUI opened for {itemName}");
	}

	private void UpdateQuantityDisplay()
	{
		_quantityLabel.Text = $"{_currentQuantity}";
		_minusButton.Disabled = (_currentQuantity <= _minQuantity);
		_plusButton.Disabled = (_currentQuantity >= _maxQuantity);
	}

	private void OnMinusPressed()
	{
		if (_currentQuantity > _minQuantity)
		{
			_currentQuantity--;
			UpdateQuantityDisplay();
		}
	}

	private void OnPlusPressed()
	{
		if (_currentQuantity < _maxQuantity)
		{
			_currentQuantity++;
			UpdateQuantityDisplay();
		}
	}

	private void OnConfirmPressed()
	{
		EmitSignal(SignalName.QuantitySelected, _currentQuantity);
		ClosePicker();
	}

	private void OnCancelPressed()
	{
		EmitSignal(SignalName.PickupCancelled);
		ClosePicker();
	}

	private void ClosePicker()
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
		
		GD.Print("QuantityPickerUI closed");
	}

	public override void _Input(InputEvent @event)
	{
		if (Visible && @event.IsActionPressed("ui_cancel"))
		{
			OnCancelPressed();
			GetViewport().SetInputAsHandled();
		}
	}
}
