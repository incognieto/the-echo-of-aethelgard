using Godot;
using System;

public partial class BridgePuzzle : Node3D
{
	[Export]
	public float InteractionRange { get; set; } = 3.0f;
	
	[Export]
	public NodePath Bridge1Path { get; set; }
	
	[Export]
	public NodePath Bridge2Path { get; set; }
	
	[Export]
	public Vector3 Bridge2TargetOffset { get; set; } = new Vector3(10, 0, 0); // Offset untuk Bridge2 bergerak
	
	[Export]
	public float BridgeMoveSpeed { get; set; } = 2.0f; // Kecepatan Bridge2 bergerak
	
	[Export]
	public float Bridge1CollapseDelay { get; set; } = 1.0f; // Delay sebelum runtuh
	
	private Label3D _promptLabel;
	private Area3D _interactionArea;
	private bool _playerNearby = false;
	private bool _isPuzzleSolved = false;
	private bool _isPuzzleFailed = false;
	private bool _isAnimating = false;
	private Player _player;
	
	private Node3D _bridge1;
	private Node3D _bridge2;
	private Vector3 _bridge2StartPos;
	private Vector3 _bridge2TargetPos;
	
	private BridgePuzzleUI _puzzleUI;

	public override void _Ready()
	{
		// Get bridge references
		if (Bridge1Path != null && !Bridge1Path.IsEmpty)
		{
			_bridge1 = GetNode<Node3D>(Bridge1Path);
			GD.Print($"✓ Bridge1 found at {_bridge1.GlobalPosition}");
		}
		else
		{
			GD.PrintErr("✗ Bridge1Path not set!");
		}
		
		if (Bridge2Path != null && !Bridge2Path.IsEmpty)
		{
			_bridge2 = GetNode<Node3D>(Bridge2Path);
			_bridge2StartPos = _bridge2.GlobalPosition;
			_bridge2TargetPos = _bridge2StartPos + Bridge2TargetOffset;
			GD.Print($"✓ Bridge2 found at {_bridge2.GlobalPosition}");
			GD.Print($"  Bridge2 will move to {_bridge2TargetPos}");
		}
		else
		{
			GD.PrintErr("✗ Bridge2Path not set!");
		}
		
		// Setup interaction area
		CallDeferred(nameof(SetupInteractionArea));
		
		// Find BridgePuzzleUI
		CallDeferred(nameof(FindPuzzleUI));
		
		GD.Print("BridgePuzzle initialized at " + GlobalPosition);
	}

	private void SetupInteractionArea()
	{
		// Create interaction area
		_interactionArea = new Area3D();
		AddChild(_interactionArea);
		
		var collisionShape = new CollisionShape3D();
		var sphereShape = new SphereShape3D();
		sphereShape.Radius = InteractionRange;
		collisionShape.Shape = sphereShape;
		_interactionArea.AddChild(collisionShape);
		
		_interactionArea.BodyEntered += OnBodyEntered;
		_interactionArea.BodyExited += OnBodyExited;
		
		// Create prompt label
		_promptLabel = new Label3D();
		_promptLabel.Text = "[E] Activate Bridge";
		_promptLabel.Modulate = new Color(1, 1, 0); // Yellow
		_promptLabel.OutlineModulate = Colors.Black;
		_promptLabel.OutlineSize = 12;
		_promptLabel.FontSize = 32;
		_promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_promptLabel.Position = new Vector3(-0.33f, 0.7f, 0); // Above the controller
		_promptLabel.Visible = false;
		AddChild(_promptLabel);
		
		GD.Print("✓ BridgePuzzle interaction area setup complete!");
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player player)
		{
			_player = player;
			_playerNearby = true;
			GD.Print("Player near bridge controller");
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			_player = null;
			_playerNearby = false;
			GD.Print("Player left bridge controller");
		}
	}

	public override void _Process(double delta)
	{
		// Update prompt visibility with pulsing effect
		if (_promptLabel != null)
		{
			if (!_isPuzzleSolved && !_isPuzzleFailed && _playerNearby)
			{
				_promptLabel.Visible = true;
				// Pulsing effect
				var alpha = Mathf.Abs(Mathf.Sin((float)Time.GetTicksMsec() / 500.0f));
				_promptLabel.Modulate = new Color(1, 1, 0, Mathf.Lerp(0.6f, 1.0f, alpha));
			}
			else
			{
				_promptLabel.Visible = false;
			}
		}
		
		// Animate Bridge2 movement when puzzle solved
		if (_isAnimating && _isPuzzleSolved && _bridge2 != null)
		{
			_bridge2.GlobalPosition = _bridge2.GlobalPosition.Lerp(_bridge2TargetPos, (float)delta * BridgeMoveSpeed);
			
			// Stop animation when close enough
			if (_bridge2.GlobalPosition.DistanceTo(_bridge2TargetPos) < 0.1f)
			{
				_bridge2.GlobalPosition = _bridge2TargetPos;
				_isAnimating = false;
				GD.Print("✓ Bridge2 reached target position!");
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Handle E key press when player is nearby
		if (_playerNearby && !_isPuzzleSolved && !_isPuzzleFailed && @event.IsActionPressed("interact"))
		{
			OpenPuzzleUI();
			GetViewport().SetInputAsHandled();
		}
	}

	private void OpenPuzzleUI()
	{
		GD.Print("BridgePuzzle: Opening puzzle UI...");
		
		if (_puzzleUI != null)
		{
			_puzzleUI.Show();
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else
		{
			GD.PrintErr("BridgePuzzleUI not found!");
		}
	}

	// Method ini akan dipanggil dari PuzzleUI ketika puzzle selesai dengan BENAR
	public void OnPuzzleSolved()
	{
		if (_isPuzzleSolved || _isPuzzleFailed) return;
		
		_isPuzzleSolved = true;
		_isAnimating = true;
		GD.Print("✓ Bridge puzzle SOLVED! Bridge2 bergerak mendekati Bridge1...");
		
		// Kita gunakan absolute path karena Penghalang ada di bawah Main
		var penghalang = GetNodeOrNull<StaticBody3D>("/root/Main/Penghalang");
		
		if (penghalang != null)
		{
			penghalang.QueueFree(); // Menghapus node Penghalang secara permanen
			GD.Print("✓ Penghalang removed! Path is now clear.");
		}
		else
		{
			GD.PrintErr("✗ Penghalang not found at /root/Main/Penghalang");
		}
		
		if (_bridge2 != null)
		{
			// Start smooth movement animation
			GD.Print($"Bridge2 moving from {_bridge2.GlobalPosition} to {_bridge2TargetPos}");
		}
		else
		{
			GD.PrintErr("✗ Bridge2 tidak ditemukan!");
		}
	}

	// Method ini akan dipanggil dari PuzzleUI ketika puzzle GAGAL
	public void OnPuzzleFailed()
	{
		if (_isPuzzleSolved || _isPuzzleFailed) return;
		
		_isPuzzleFailed = true;
		GD.Print("✗ Bridge puzzle FAILED! Bridge1 akan runtuh...");
		
		if (_bridge1 != null)
		{
			// Delay sebelum runtuh untuk efek dramatis
			GetTree().CreateTimer(Bridge1CollapseDelay).Timeout += CollapseBridge1;
		}
		else
		{
			GD.PrintErr("✗ Bridge1 tidak ditemukan!");
		}
	}

	private void CollapseBridge1()
	{
		if (_bridge1 == null) return;
		
		GD.Print("💥 Bridge1 RUNTUH!");
		
		// Hapus CollisionShape dari Bridge1 agar player jatuh
		foreach (Node child in _bridge1.GetChildren())
		{
			if (child is CollisionShape3D collisionShape)
			{
				collisionShape.Disabled = true;
				GD.Print("  Bridge1 collision disabled - player akan jatuh!");
			}
		}
		
		// Optional: Tambahkan efek visual runtuh (rotasi/jatuh)
		// Convert Bridge1 ke RigidBody3D atau tambahkan animasi
		var tween = CreateTween();
		tween.SetParallel(true);
		
		// Jatuh ke bawah sambil berputar
		tween.TweenProperty(_bridge1, "global_position:y", _bridge1.GlobalPosition.Y - 50, 3.0);
		tween.TweenProperty(_bridge1, "rotation:x", Mathf.DegToRad(90), 2.0); // Miring ke depan
		tween.TweenProperty(_bridge1, "rotation:z", Mathf.DegToRad(15), 1.5); // Sedikit berputar
		
		// Setelah jatuh, sembunyikan atau hapus
		tween.Chain().TweenCallback(Callable.From(() => 
		{
			if (_bridge1 != null)
			{
				_bridge1.Visible = false;
				GD.Print("  Bridge1 hidden setelah runtuh");
			}
		}));
	}

	private void FindPuzzleUI()
	{
		var canvasLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>("/root/Main/UI");
		if (canvasLayer != null)
		{
			_puzzleUI = canvasLayer.GetNodeOrNull<BridgePuzzleUI>("BridgePuzzleUI");
			if (_puzzleUI != null)
			{
				// Connect ke signal dengan parameter success/fail
				_puzzleUI.PuzzleCompleted += OnPuzzleCompleted;
				_puzzleUI.SetBridgePuzzle(this);
				GD.Print("✓ BridgePuzzle: UI connected!");
			}
			else
			{
				GD.PrintErr("✗ BridgePuzzleUI not found in UI layer!");
			}
		}
	}
	
	private void OnPuzzleCompleted(bool success)
	{
		if (success)
		{
			OnPuzzleSolved();
		}
		else
		{
			OnPuzzleFailed();
		}
	}
}
