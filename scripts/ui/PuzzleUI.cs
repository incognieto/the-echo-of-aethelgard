using Godot;
using System;
using System.Collections.Generic;

public partial class PuzzleUI : Control
{
	[Signal]
	public delegate void PuzzleCompletedEventHandler(bool success);

	// Symbol enum untuk puzzle
	private enum Symbol
	{
		Bunga = 0,    // Flower
		Mata = 1,     // Eye
		Matahari = 2, // Sun
		Petir = 3,    // Lightning
		Bulan = 4,    // Moon
		Bintang = 5   // Star
	}
	
	private Label _feedbackLabel;
	private Button _closeButton;
	private InventoryUI _inventoryUI;
	
	private List<TextureButton> _symbolButtons = new List<TextureButton>();
	private List<Symbol> _currentSequence = new List<Symbol>();
	private Symbol[] _correctSequence;
	private int _maxLength = 6;
	
	// Symbol textures
	private Dictionary<Symbol, Texture2D> _symbolTextures = new Dictionary<Symbol, Texture2D>();

	public override void _Ready()
	{
		Visible = false;
		
		// Load symbol textures
		LoadSymbolTextures();
		
		// Initialize sequence dengan Bunga semua
		for (int i = 0; i < _maxLength; i++)
		{
			_currentSequence.Add(Symbol.Bunga);
		}
		
		// Get nodes dari scene tree
		_feedbackLabel = GetNode<Label>("PuzzlePanel/FeedbackLabel");
		_closeButton = GetNode<Button>("PuzzlePanel/CloseButton");
		
		// Get symbol buttons
		var symbolContainer = GetNode<Control>("PuzzlePanel/SymbolContainer");
		for (int i = 1; i <= 6; i++)
		{
			var btn = symbolContainer.GetNode<TextureButton>($"Symbol{i}");
			int index = i - 1;
			btn.Pressed += () => OnSymbolButtonPressed(index);
			_symbolButtons.Add(btn);
		}
		
		// Connect close button
		_closeButton.Pressed += OnClosePressed;
		
		// Find InventoryUI
		CallDeferred(nameof(FindInventoryUI));
		
		UpdateDisplay();
	}
	
	private void FindInventoryUI()
	{
		var canvasLayer = GetParent() as CanvasLayer;
		if (canvasLayer != null)
		{
			_inventoryUI = canvasLayer.GetNodeOrNull<InventoryUI>("InventoryUI");
			if (_inventoryUI == null)
			{
				GD.PrintErr("PuzzleUI: InventoryUI not found!");
			}
		}
	}
	
	private void LoadSymbolTextures()
	{
		_symbolTextures[Symbol.Bunga] = GD.Load<Texture2D>("res://assets/sprites/ui/ButtonBunga.png");
		_symbolTextures[Symbol.Mata] = GD.Load<Texture2D>("res://assets/sprites/ui/ButtonMata.png");
		_symbolTextures[Symbol.Matahari] = GD.Load<Texture2D>("res://assets/sprites/ui/ButtonMatahari.png");
		_symbolTextures[Symbol.Petir] = GD.Load<Texture2D>("res://assets/sprites/ui/ButtonPetir.png");
		_symbolTextures[Symbol.Bulan] = GD.Load<Texture2D>("res://assets/sprites/ui/ButtonBulan.png");
		_symbolTextures[Symbol.Bintang] = GD.Load<Texture2D>("res://assets/sprites/ui/ButtonBintang.png");
		
		foreach (var kvp in _symbolTextures)
		{
			if (kvp.Value == null)
			{
				GD.PrintErr($"Failed to load texture for symbol: {kvp.Key}");
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (Visible && @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	public void ShowPuzzle(PuzzleSymbol[] correctSequence)
	{
		// Convert PuzzleSymbol to internal Symbol enum
		_correctSequence = new Symbol[correctSequence.Length];
		for (int i = 0; i < correctSequence.Length; i++)
		{
			_correctSequence[i] = (Symbol)((int)correctSequence[i]);
		}
		
		// Reset semua ke Bunga
		for (int i = 0; i < _maxLength; i++)
		{
			_currentSequence[i] = Symbol.Bunga;
		}
		
		_feedbackLabel.Text = "";
		UpdateDisplay();
		Visible = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		// Hide crosshair dan hotbar
		if (_inventoryUI != null)
		{
			_inventoryUI.SetCrosshairVisible(false);
			_inventoryUI.SetHotbarVisible(false);
		}
		
		GD.Print("Symbol lock puzzle opened");
	}

	private void OnSymbolButtonPressed(int index)
	{
		// Cycle ke symbol berikutnya
		_currentSequence[index] = (Symbol)(((int)_currentSequence[index] + 1) % 6);
		UpdateDisplay();
		
		// Auto-check jika semua symbol sudah diatur
		CheckSequence();
	}
	
	private void CheckSequence()
	{
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
			_feedbackLabel.Text = "✓ LOCK OPENED!";
			_feedbackLabel.AddThemeColorOverride("font_color", new Color(0, 1, 0, 1));
			GD.Print("Puzzle solved!");
			
			// Delay sebelum close
			GetTree().CreateTimer(1.5).Timeout += () =>
			{
				EmitSignal(SignalName.PuzzleCompleted, true);
				Close();
			};
		}
	}

	private void OnClosePressed()
	{
		Close();
	}

	private void Close()
	{
		Visible = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		
		// Restore hotbar dan crosshair sesuai camera mode
		if (_inventoryUI != null)
		{
			_inventoryUI.SetHotbarVisible(true);
			// Crosshair sudah di-handle oleh Player sesuai camera mode, kita perlu cek dari Player
			// Tapi kita tidak punya reference ke Player, jadi biarkan Player yang handle crosshair visibility
		}
		
		EmitSignal(SignalName.PuzzleCompleted, false);
	}

	private void UpdateDisplay()
	{
		for (int i = 0; i < _maxLength; i++)
		{
			var btn = _symbolButtons[i];
			var currentSymbol = _currentSequence[i];
			
			if (_symbolTextures.ContainsKey(currentSymbol) && _symbolTextures[currentSymbol] != null)
			{
				btn.TextureNormal = _symbolTextures[currentSymbol];
			}
		}
	}
}
