using Godot;
using System;

// Symbol enum - must match PuzzleUI
public enum PuzzleSymbol
{
	Bunga = 0,    // Flower
	Mata = 1,     // Eye
	Matahari = 2, // Sun
	Petir = 3,    // Lightning
	Bulan = 4,    // Moon
	Bintang = 5   // Star
}

public partial class DoorPuzzle : StaticBody3D
{
	// Correct sequence as int array (0=Bunga, 1=Mata, 2=Matahari, 3=Petir, 4=Bulan, 5=Bintang)
	[Export]
	public int[] CorrectSequenceValues { get; set; } = new int[] { 0, 1, 2, 3, 4, 5 };
	
	private PuzzleSymbol[] CorrectSequence
	{
		get
		{
			var symbols = new PuzzleSymbol[CorrectSequenceValues.Length];
			for (int i = 0; i < CorrectSequenceValues.Length; i++)
			{
				symbols[i] = (PuzzleSymbol)CorrectSequenceValues[i];
			}
			return symbols;
		}
	}
	
	[Export]
	public float InteractionRange { get; set; } = 3.0f;
	
	[Export]
	public Vector3 OpenOffset { get; set; } = new Vector3(0, 5, 0);
	
	[Export]
	public float OpenSpeed { get; set; } = 2.0f;
	
	private Label3D _promptLabel;
	private Area3D _interactionArea;
	private bool _playerNearby = false;
	private bool _isLocked = true;
	private bool _isOpening = false;
	private Vector3 _closedPosition;
	private Vector3 _openPosition;
	private PuzzleUI _puzzleUI;

	public override void _Ready()
	{
		_closedPosition = GlobalPosition;
		_openPosition = _closedPosition + OpenOffset;
		
		// Wait for control panel to be ready (created from scene)
		CallDeferred(nameof(SetupControlPanel));
		
		// Find PuzzleUI
		CallDeferred(nameof(FindPuzzleUI));
		
		GD.Print($"DoorPuzzle initialized at {GlobalPosition}");
		GD.Print($"Correct sequence: {string.Join(", ", CorrectSequence)}");
	}

	private void SetupControlPanel()
	{
		// Get control panel from Main scene (not child of door)
		var controlPanel = GetNode<Node3D>("/root/Main/ControlPanel");
		if (controlPanel == null)
		{
			GD.PrintErr("✗ ControlPanel not found in Main scene!");
			return;
		}
		
		// Create interaction area on control panel
		_interactionArea = new Area3D();
		controlPanel.AddChild(_interactionArea);
		
		var collisionShape = new CollisionShape3D();
		var sphereShape = new SphereShape3D();
		sphereShape.Radius = InteractionRange;
		collisionShape.Shape = sphereShape;
		_interactionArea.AddChild(collisionShape);
		
		_interactionArea.BodyEntered += OnBodyEntered;
		_interactionArea.BodyExited += OnBodyExited;
		
		// Create prompt label above control panel
		_promptLabel = new Label3D();
		_promptLabel.Text = "[E] Unlock Door";
		_promptLabel.Modulate = new Color(1, 1, 0);
		_promptLabel.OutlineModulate = Colors.Black;
		_promptLabel.OutlineSize = 12;
		_promptLabel.FontSize = 32; // Lebih besar untuk isometric
		_promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_promptLabel.Position = new Vector3(0, 0.8f, 0); // Di atas control panel
		_promptLabel.Visible = false;
		controlPanel.AddChild(_promptLabel);
		
		GD.Print("✓ Control panel setup complete!");
	}

	private void FindPuzzleUI()
	{
		var canvasLayer = GetTree().Root.GetNode<CanvasLayer>("/root/Main/UI");
		if (canvasLayer != null)
		{
			_puzzleUI = canvasLayer.GetNodeOrNull<PuzzleUI>("PuzzleUI");
			if (_puzzleUI != null)
			{
				_puzzleUI.PuzzleCompleted += OnPuzzleCompleted;
				GD.Print("✓ DoorPuzzle: PuzzleUI connected!");
			}
			else
			{
				GD.PrintErr("✗ DoorPuzzle: PuzzleUI not found!");
			}
		}
		else
		{
			GD.PrintErr("✗ DoorPuzzle: UI CanvasLayer not found!");
		}
	}

	public override void _Process(double delta)
	{
		// Update prompt visibility
		if (_promptLabel != null)
		{
			if (_isLocked && _playerNearby)
			{
				_promptLabel.Visible = true;
				var alpha = Mathf.Abs(Mathf.Sin((float)Time.GetTicksMsec() / 500.0f));
				_promptLabel.Modulate = new Color(1, 1, 0, Mathf.Lerp(0.6f, 1.0f, alpha));
			}
			else if (!_isLocked && _playerNearby)
			{
				_promptLabel.Text = "Door Unlocked";
				_promptLabel.Modulate = new Color(0, 1, 0, 1);
				_promptLabel.Visible = true;
			}
			else
			{
				_promptLabel.Visible = false;
			}
		}
		
		// Animate door opening
		if (_isOpening)
		{
			GlobalPosition = GlobalPosition.Lerp(_openPosition, OpenSpeed * (float)delta);
			if (GlobalPosition.DistanceTo(_openPosition) < 0.1f)
			{
				GlobalPosition = _openPosition;
				_isOpening = false;
				GD.Print("Door fully opened");
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_playerNearby && _isLocked && @event.IsActionPressed("interact"))
		{
			ShowPuzzle();
			GetViewport().SetInputAsHandled();
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body.IsInGroup("player") || body.Name == "Player")
		{
			_playerNearby = true;
			GD.Print("Player near door");
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body.IsInGroup("player") || body.Name == "Player")
		{
			_playerNearby = false;
			GD.Print("Player left door");
		}
	}

	private void ShowPuzzle()
	{
		if (_puzzleUI != null)
		{
			GD.Print("Opening puzzle UI...");
			_puzzleUI.ShowPuzzle(CorrectSequence);
		}
		else
		{
			GD.PrintErr("Cannot show puzzle - PuzzleUI is null!");
		}
	}

	private void OnPuzzleCompleted(bool success)
	{
		if (success)
		{
			UnlockDoor();
		}
		else
		{
			GD.Print("Puzzle closed without solving");
		}
		
		// Restore crosshair visibility based on current camera mode
		RestoreCrosshairVisibility();
	}
	
	private void RestoreCrosshairVisibility()
	{
		// Find player and restore crosshair based on camera mode
		var player = GetTree().Root.GetNode<Player>("/root/Main/Player");
		if (player != null)
		{
			var canvasLayer = GetTree().Root.GetNode<CanvasLayer>("/root/Main/UI");
			if (canvasLayer != null)
			{
				var inventoryUI = canvasLayer.GetNodeOrNull<InventoryUI>("InventoryUI");
				if (inventoryUI != null)
				{
					// Check camera mode from player - need to make CurrentCameraMode public or use a different approach
					// For now, just always show crosshair when puzzle closes, player will handle it based on mode
					inventoryUI.SetCrosshairVisible(true);
				}
			}
		}
	}

	private void UnlockDoor()
	{
		_isLocked = false;
		_isOpening = true;
		GD.Print("Door unlocked! Opening...");
		
		// Disable collision saat buka
		var collisionShape = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
		if (collisionShape != null)
		{
			collisionShape.Disabled = true;
		}
	}
}
