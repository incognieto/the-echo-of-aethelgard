using Godot;
using System;

public partial class BridgePuzzleUI : Control
{
	[Signal]
	public delegate void PuzzleCompletedEventHandler(bool success);

	private const string CORRECT_ANSWER = "29";

	// UI Components - Diambil dari scene, bukan dibuat via kode
	private Panel _calculatorPanel;
	private Label _displayLabel;
	private Label _escInstructionLabel;
	private string _currentInput = "";

	// Buttons - Diambil dari scene
	private Button _button0, _button1, _button2, _button3, _button4;
	private Button _button5, _button6, _button7, _button8, _button9;
	private Button _delButton;
	private Button _enterButton;
	private TextureButton _closeButton;

	private BridgePuzzle _bridgePuzzle;

	public override void _Ready()
	{
		Visible = false;

		// Get nodes from scene (bukan create baru)
		_calculatorPanel = GetNode<Panel>("CalculatorPanel");
		_displayLabel = GetNode<Label>("CalculatorPanel/DisplayContainer/DisplayVBox/DisplayLabel");

		// Get all number buttons
		var buttonGrid = GetNode<Node2D>("CalculatorPanel/ButtonGrid");
		_button1 = buttonGrid.GetNode<Button>("Button1");
		_button2 = buttonGrid.GetNode<Button>("Button2");
		_button3 = buttonGrid.GetNode<Button>("Button3");
		_button4 = buttonGrid.GetNode<Button>("Button4");
		_button5 = buttonGrid.GetNode<Button>("Button5");
		_button6 = buttonGrid.GetNode<Button>("Button6");
		_button7 = buttonGrid.GetNode<Button>("Button7");
		_button8 = buttonGrid.GetNode<Button>("Button8");
		_button9 = buttonGrid.GetNode<Button>("Button9");
		_button0 = buttonGrid.GetNode<Button>("Button0");

		// Get action buttons
		_delButton = buttonGrid.GetNode<Button>("DelButton");
		_enterButton = buttonGrid.GetNode<Button>("EnterButton");
		_closeButton = _calculatorPanel.GetNode<TextureButton>("CloseButton");

		// Connect signals
		_button1.Pressed += () => OnNumberPressed("1");
		_button2.Pressed += () => OnNumberPressed("2");
		_button3.Pressed += () => OnNumberPressed("3");
		_button4.Pressed += () => OnNumberPressed("4");
		_button5.Pressed += () => OnNumberPressed("5");
		_button6.Pressed += () => OnNumberPressed("6");
		_button7.Pressed += () => OnNumberPressed("7");
		_button8.Pressed += () => OnNumberPressed("8");
		_button9.Pressed += () => OnNumberPressed("9");
		_button0.Pressed += () => OnNumberPressed("0");

		_delButton.Pressed += OnDelPressed;
		_enterButton.Pressed += OnEnterPressed;
		_closeButton.Pressed += OnClosePressed;

		// Create ESC instruction label
		_escInstructionLabel = new Label();
		_escInstructionLabel.Text = "(Esc) to return";
		_escInstructionLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_escInstructionLabel.AddThemeColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f, 0.8f));
		_escInstructionLabel.AddThemeFontSizeOverride("font_size", 18);
		_escInstructionLabel.Position = new Vector2(10, 10);
		_calculatorPanel.AddChild(_escInstructionLabel);

		// Setup cursor hover effects
		SetupButtonHoverEffects();

		GD.Print("✓ BridgePuzzleUI ready - loaded from scene!");
	}

	private void SetupButtonHoverEffects()
	{
		BaseButton[] buttons =
		{
		_button0, _button1, _button2, _button3, _button4,
		_button5, _button6, _button7, _button8, _button9,
		_delButton, _enterButton, _closeButton
	};

		foreach (var button in buttons)
		{
			if (button != null)
			{
				button.MouseEntered += () => CursorManager.Instance?.SetCursor(CursorManager.CursorType.Hover);
				button.MouseExited += () => CursorManager.Instance?.SetCursor(CursorManager.CursorType.Standard);
			}
		}
	}

	private void OnNumberPressed(string digit)
	{
		// Limit input to 3 digits
		if (_currentInput.Length < 3)
		{
			_currentInput += digit;
			UpdateDisplay();
			GD.Print($"Input: {_currentInput}");
		}
	}

	private void OnDelPressed()
	{
		if (_currentInput.Length > 0)
		{
			_currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
			UpdateDisplay();
			GD.Print($"Deleted. Current: {_currentInput}");
		}
	}

	private void OnEnterPressed()
	{
		if (_currentInput.Length == 0)
		{
			GD.Print("⚠️ No input entered!");
			return;
		}

		GD.Print($"Checking answer: {_currentInput}");

		bool isCorrect = _currentInput == CORRECT_ANSWER;

		if (isCorrect)
		{
			GD.Print($"✓ CORRECT! Answer: {_currentInput}");
			EmitSignal(SignalName.PuzzleCompleted, true);
			Hide();
			InventoryUI.IsAnyPanelOpen = false; // Clear global flag
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else
		{
			GD.Print($"✗ WRONG! Answer: {_currentInput} (correct: {CORRECT_ANSWER})");

			// Flash red on display
			_displayLabel.AddThemeColorOverride("font_color", Colors.Red);

			// Wait 1 second then emit fail
			GetTree().CreateTimer(1.0).Timeout += () =>
			{
				EmitSignal(SignalName.PuzzleCompleted, false);
				Hide();
				InventoryUI.IsAnyPanelOpen = false; // Clear global flag
				Input.MouseMode = Input.MouseModeEnum.Captured;
				_displayLabel.AddThemeColorOverride("font_color", Colors.Black);
			};
		}
	}

	private void OnClosePressed()
	{
		Hide();
		InventoryUI.IsAnyPanelOpen = false; // Clear global flag
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_currentInput = "";
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		_displayLabel.Text = _currentInput.Length > 0 ? _currentInput : "";
	}

	public new void Show()
	{
		base.Show();
		_currentInput = "";
		UpdateDisplay();
		_displayLabel.AddThemeColorOverride("font_color", Colors.Black);
		InventoryUI.IsAnyPanelOpen = true; // Set global flag
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GD.Print("Calculator UI shown");
	}

	public void SetBridgePuzzle(BridgePuzzle puzzle)
	{
		_bridgePuzzle = puzzle;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
		{
			if (Visible)
			{
				OnClosePressed();
				GetViewport().SetInputAsHandled();
				return;
			}
		}

		if (!Visible) return;

		// Keyboard number input
		if (@event is InputEventKey keyEvent2 && keyEvent2.Pressed && !keyEvent2.Echo)
		{
			if (keyEvent2.Keycode >= Key.Key0 && keyEvent2.Keycode <= Key.Key9)
			{
				OnNumberPressed(((int)(keyEvent2.Keycode - Key.Key0)).ToString());
			}
			else if (keyEvent2.Keycode >= Key.Kp0 && keyEvent2.Keycode <= Key.Kp9)
			{
				OnNumberPressed(((int)(keyEvent2.Keycode - Key.Kp0)).ToString());
			}
			else if (keyEvent2.Keycode == Key.Backspace)
			{
				OnDelPressed();
			}
			else if (keyEvent2.Keycode == Key.Enter || keyEvent2.Keycode == Key.KpEnter)
			{
				OnEnterPressed();
			}
		}
	}
}
