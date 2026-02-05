using Godot;
using System;
using System.Collections.Generic;

public partial class PuzzleUI : Control
{
	[Signal] public delegate void PuzzleCompletedEventHandler(bool success);

	private enum Symbol
	{
		Bunga = 0, Mata = 1, Matahari = 2, Petir = 3, Bulan = 4, Bintang = 5
	}
	
	private Label _feedbackLabel;
	private TextureButton _closeButton;
	private InventoryUI _inventoryUI;
	
	// Simpan referensi ke roda-roda gembok
	private List<KeyLock> _keyNodes = new List<KeyLock>();
	private List<Symbol> _currentSequence = new List<Symbol>();
	private Symbol[] _correctSequence;
	private int _maxLength = 6;

	public override void _Ready()
	{
		Visible = false;
		
		// Init sequence awal (semua Bunga/0)
		for (int i = 0; i < _maxLength; i++) _currentSequence.Add(Symbol.Bunga);
		
		_feedbackLabel = GetNode<Label>("PuzzlePanel/FeedbackLabel");
		_closeButton = GetNode<TextureButton>("PuzzlePanel/CloseButton");
		var panel = GetNode<Control>("PuzzlePanel");

		// Cari KeyContainer dan Keys1-6
		var keyContainer = GetNode<Control>("PuzzlePanel/KeysContainer");
		for (int i = 1; i <= 6; i++)
		{
			var keyNode = keyContainer.GetNode<KeyLock>($"Keys{i}");
			int index = i - 1;
			
			// SINKRONISASI: Connect signal dari KeyLock ke fungsi di sini
			keyNode.Rotated += (newIdx) => OnKeyRotated(index, newIdx);
			
			_keyNodes.Add(keyNode);
		}
		
		_closeButton.Pressed += OnClosePressed;
		CallDeferred(nameof(FindInventoryUI));
	}

	// Fungsi ini dipanggil tiap kali lu klik roda
	private void OnKeyRotated(int keySlot, int newSymbolIndex)
	{
		// Update urutan saat ini berdasarkan apa yang dikirim roda
		_currentSequence[keySlot] = (Symbol)newSymbolIndex;
		
		// Tiap muter, langsung cek apakah sudah bener
		CheckSequence();
	}

	private void CheckSequence()
	{
		if (_correctSequence == null) return;

		bool correct = true;
		for (int i = 0; i < _maxLength; i++)
		{
			if (_currentSequence[i] != _correctSequence[i])
			{
				correct = false;
				break;
			}
		}
		
		if (correct)
		{
			_feedbackLabel.Text = "Lock Opened!";
			_feedbackLabel.AddThemeColorOverride("font_color", new Color(0, 1, 0, 1));
			
			GetTree().CreateTimer(1.5).Timeout += () => {
				EmitSignal(SignalName.PuzzleCompleted, true);
				Close();
			};
		}
	}

	// --- Sisanya fungsi standar lu ---

	public void ShowPuzzle(PuzzleSymbol[] correctSequence)
	{
		_correctSequence = new Symbol[correctSequence.Length];
		for (int i = 0; i < correctSequence.Length; i++)
			_correctSequence[i] = (Symbol)((int)correctSequence[i]);
		
		_feedbackLabel.Text = "";
		Visible = true;
		InventoryUI.IsAnyPanelOpen = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		if (_inventoryUI != null)
		{
			_inventoryUI.SetCrosshairVisible(false);
			_inventoryUI.SetHotbarVisible(false);
		}
	}

	private void Close()
	{
		Visible = false;
		InventoryUI.IsAnyPanelOpen = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		if (_inventoryUI != null) _inventoryUI.SetHotbarVisible(true);
		EmitSignal(SignalName.PuzzleCompleted, false);
	}

	private void OnClosePressed() => Close();

	private void FindInventoryUI()
	{
		var canvasLayer = GetParent() as CanvasLayer;
		if (canvasLayer != null) _inventoryUI = canvasLayer.GetNodeOrNull<InventoryUI>("InventoryUI");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape && Visible)
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}
}
