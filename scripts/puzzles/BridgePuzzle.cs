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
	public NodePath AngleCameraPath { get; set; } // Path ke Cams/Angle camera
	
	[Export]
	public Vector3 Bridge2TargetOffset { get; set; } = new Vector3(10, 0, 0); // Offset untuk Bridge2 bergerak
	
	[Export]
	public float BridgeMoveSpeed { get; set; } = 2.0f; // Kecepatan Bridge2 bergerak
	
	[Export]
	public float Bridge1CollapseDelay { get; set; } = 1.0f; // Delay sebelum runtuh
	
	[Export]
	public float CameraZoomDuration { get; set; } = 2.0f; // Durasi zoom kamera
	
	[Export]
	public float CameraZoomAmount { get; set; } = 10.0f; // Amount to zoom in (FOV reduction)
	
	private Label3D _promptLabel;
	private Area3D _interactionArea;
	private bool _playerNearby = false;
	private bool _isPuzzleSolved = false;
	private bool _isPuzzleFailed = false;
	private bool _isAnimating = false;
	private Player _player;
	
	private Node3D _bridge1;
	private Node3D _bridge2;
	private Vector3 _bridge1StartPos; // Store initial Bridge1 position
	private Vector3 _bridge1StartRot; // Store initial Bridge1 rotation
	private Vector3 _bridge2StartPos;
	private Vector3 _bridge2TargetPos;
	
	private BridgePuzzleUI _puzzleUI;
	
	// Camera animation variables
	private Camera3D _angleCamera;
	private Camera3D _playerCamera;
	private ColorRect _fadeRect;
	private bool _isCameraSequencePlaying = false;
	private Tween _bridge1CollapseTween; // Track tween for killing on reset

	public override void _Ready()
	{
		// Get bridge references
		if (Bridge1Path != null && !Bridge1Path.IsEmpty)
		{
			_bridge1 = GetNode<Node3D>(Bridge1Path);
			_bridge1StartPos = _bridge1.GlobalPosition; // Save initial position
			_bridge1StartRot = _bridge1.Rotation; // Save initial rotation
			GD.Print($"✓ Bridge1 found at {_bridge1.GlobalPosition}, rotation: {_bridge1.Rotation}");
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
		
		// Setup camera and fade effect
		CallDeferred(nameof(SetupCameraAnimation));
		
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
		GD.Print("✓ Bridge puzzle SOLVED! Starting camera sequence...");
		
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
		
		// Start camera sequence animation
		PlayCameraSequence(true);
	}

	// Method ini akan dipanggil dari PuzzleUI ketika puzzle GAGAL
	public void OnPuzzleFailed()
	{
		if (_isPuzzleSolved || _isPuzzleFailed) return;
		
		_isPuzzleFailed = true;
		GD.Print("✗ Bridge puzzle FAILED! Starting camera sequence...");
		
		// Handle wrong answer - reduce lives and show fail screen
		OnWrongAnswer();
		
		// Reset puzzle flags untuk bisa coba lagi
		_isPuzzleFailed = false;
		_isAnimating = false;
	}

	private void CollapseBridge1()
	{
		if (_bridge1 == null) return;
		
		GD.Print("💥 Bridge1 RUNTUH!");
		
		// Kill previous tween if exists
		if (_bridge1CollapseTween != null && _bridge1CollapseTween.IsRunning())
		{
			_bridge1CollapseTween.Kill();
		}
		
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
		_bridge1CollapseTween = CreateTween();
		_bridge1CollapseTween.SetParallel(true);
		
		// Jatuh ke bawah sambil berputar
		_bridge1CollapseTween.TweenProperty(_bridge1, "global_position:y", _bridge1.GlobalPosition.Y - 50, 3.0);
		_bridge1CollapseTween.TweenProperty(_bridge1, "rotation:x", Mathf.DegToRad(90), 2.0); // Miring ke depan
		_bridge1CollapseTween.TweenProperty(_bridge1, "rotation:z", Mathf.DegToRad(15), 1.5); // Sedikit berputar
		
		// Setelah jatuh, sembunyikan atau hapus
		_bridge1CollapseTween.Chain().TweenCallback(Callable.From(() => 
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
	
	private void SetupCameraAnimation()
	{
		// Find angle camera
		if (AngleCameraPath != null && !AngleCameraPath.IsEmpty)
		{
			_angleCamera = GetNode<Camera3D>(AngleCameraPath);
			GD.Print($"✓ Angle camera found");
		}
		else
		{
			// Try to auto-find if not set
			_angleCamera = GetTree().Root.GetNodeOrNull<Camera3D>("/root/Main/Cams/Angle");
			if (_angleCamera != null)
			{
				GD.Print("✓ Angle camera auto-detected");
			}
			else
			{
				GD.PrintErr("✗ Angle camera not found!");
			}
		}
		
		// Find player camera
		var player = GetTree().Root.GetNodeOrNull<Node>("/root/Main/Player");
		if (player != null)
		{
			_playerCamera = player.GetNodeOrNull<Camera3D>("Head/Camera3D");
			if (_playerCamera != null)
			{
				GD.Print("✓ Player camera found");
			}
		}
		
		// Create fade rect in UI layer
		var uiLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>("/root/Main/UI");
		if (uiLayer != null)
		{
			_fadeRect = new ColorRect();
			_fadeRect.Color = new Color(0, 0, 0, 0); // Start transparent
			_fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
			_fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			_fadeRect.ZIndex = 100; // High z-index to be on top
			uiLayer.AddChild(_fadeRect);
			GD.Print("✓ Fade rect created");
		}
	}
	
	private async void PlayCameraSequence(bool success)
	{
		if (_isCameraSequencePlaying) return;
		if (_angleCamera == null || _playerCamera == null || _fadeRect == null)
		{
			GD.PrintErr("✗ Camera sequence components not ready!");
			// Still execute bridge logic even if camera fails
			if (success)
				StartBridgeConnection();
			else
				StartBridgeCollapse();
			return;
		}
		
		_isCameraSequencePlaying = true;
		
		// Find and disable player input - use direct node path to ensure we get the player
		var playerNode = GetTree().Root.GetNodeOrNull<Player>("/root/Main/Player");
		if (playerNode != null)
		{
			GetTree().Paused = false; // Make sure not paused
			playerNode.ProcessMode = ProcessModeEnum.Disabled;
			GD.Print("  Player input disabled");
		}
		
		GD.Print($"🎬 Starting camera sequence - Success: {success}");
		
		// 1. Fade IN (0.5 seconds) - Transparent to Black
		var fadeTween = CreateTween();
		fadeTween.TweenProperty(_fadeRect, "color:a", 1.0f, 0.5f);
		await ToSignal(fadeTween, Tween.SignalName.Finished);
		
		// 2. Switch to angle camera
		_playerCamera.Current = false;
		_angleCamera.Current = true;
		float originalFov = _angleCamera.Fov;
		GD.Print("  Switched to angle camera");
		
		// Small delay to ensure camera switch
		await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
		
		// 3. Fade OUT (0.5 seconds) - Black to Transparent
		fadeTween = CreateTween();
		fadeTween.TweenProperty(_fadeRect, "color:a", 0.0f, 0.5f);
		await ToSignal(fadeTween, Tween.SignalName.Finished);
		
		// 4. Zoom in camera while bridge moves (2 seconds)
		var zoomTween = CreateTween();
		zoomTween.TweenProperty(_angleCamera, "fov", originalFov - CameraZoomAmount, CameraZoomDuration);
		
		// Start bridge animation
		if (success)
		{
			StartBridgeConnection();
		}
		else
		{
			StartBridgeCollapse();
		}
		
		// Wait for zoom to finish
		await ToSignal(zoomTween, Tween.SignalName.Finished);
		GD.Print("  Zoom complete");
		
		// Small pause at the end
		await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
		
		// 5. Fade IN again (0.5 seconds) - Transparent to Black
		fadeTween = CreateTween();
		fadeTween.TweenProperty(_fadeRect, "color:a", 1.0f, 0.5f);
		await ToSignal(fadeTween, Tween.SignalName.Finished);
		
		// 6. Switch back to player camera
		_angleCamera.Current = false;
		_playerCamera.Current = true;
		_angleCamera.Fov = originalFov; // Reset FOV
		GD.Print("  Switched back to player camera");
		
		// Small delay
		await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
		
		// 7. Fade OUT final (0.5 seconds) - Black to Transparent
		fadeTween = CreateTween();
		fadeTween.TweenProperty(_fadeRect, "color:a", 0.0f, 0.5f);
		await ToSignal(fadeTween, Tween.SignalName.Finished);
		
		// Re-enable player input - use direct node path to ensure we get the player
		playerNode = GetTree().Root.GetNodeOrNull<Player>("/root/Main/Player");
		if (playerNode != null)
		{
			playerNode.ProcessMode = ProcessModeEnum.Inherit;
			GD.Print("  Player input re-enabled");
		}
		else
		{
			GD.PrintErr("✗ Failed to re-enable player input - Player node not found!");
		}
		
		_isCameraSequencePlaying = false;
		GD.Print("🎬 Camera sequence complete!");
	}
	
	private void StartBridgeConnection()
	{
		// Already set in OnPuzzleSolved
		_isAnimating = true;
		GD.Print("  Bridge2 connection animation started");
	}
	
	private void StartBridgeCollapse()
	{
		// Trigger collapse after delay
		GetTree().CreateTimer(Bridge1CollapseDelay).Timeout += CollapseBridge1;
		GD.Print("  Bridge1 collapse scheduled");
	}

	/// <summary>
	/// Reset puzzle state untuk respawn - allow player untuk mengisi puzzle lagi
	/// </summary>
	public void ResetPuzzle()
	{
		GD.Print("🔄 BridgePuzzle: Resetting puzzle state...");
		
		// Reset puzzle state flags
		_isPuzzleSolved = false;
		_isPuzzleFailed = false;
		_isAnimating = false;
		_isCameraSequencePlaying = false;
		
		// Reset Bridge1 if it was collapsed
		if (_bridge1 != null)
		{
			// CRITICAL: Kill any running tweens on Bridge1 first
			if (_bridge1CollapseTween != null && _bridge1CollapseTween.IsRunning())
			{
				_bridge1CollapseTween.Kill();
				GD.Print("  Killed running Bridge1 collapse tween");
			}
			
			_bridge1.Visible = true;
			_bridge1.Rotation = _bridge1StartRot; // Restore to initial rotation
			_bridge1.GlobalPosition = _bridge1StartPos; // Restore to initial position
			
			// Re-enable collision shapes
			foreach (Node child in _bridge1.GetChildren())
			{
				if (child is CollisionShape3D collisionShape)
				{
					collisionShape.Disabled = false;
				}
			}
			
			GD.Print($"  Bridge1 restored to initial position: {_bridge1StartPos}, rotation: {_bridge1StartRot}");
		}
		
		// Reset Bridge2 to start position
		if (_bridge2 != null)
		{
			_bridge2.GlobalPosition = _bridge2StartPos;
			GD.Print("  Bridge2 reset to start position");
		}
		
		// Hide puzzle UI
		if (_puzzleUI != null)
		{
			_puzzleUI.Hide();
			GD.Print("  Puzzle UI hidden");
		}
		
		GD.Print("✅ BridgePuzzle reset complete!");
	}

	/// <summary>
	/// Handle wrong answer - lose life dan show fail screen atau reset puzzle
	/// </summary>
	public void OnWrongAnswer()
	{
		GD.Print("❌ BridgePuzzle: Wrong answer! Reducing lives...");
		
		// CRITICAL: Hide puzzle UI first before showing fail screen
		if (_puzzleUI != null)
		{
			_puzzleUI.Hide();
			GD.Print("🚫 Puzzle UI hidden before fail screen");
		}
		
		// Use CallDeferred to ensure mouse mode is set AFTER all UI operations complete
		CallDeferred(MethodName.ShowFailScreenForWrongAnswer);
	}

	private void ShowFailScreenForWrongAnswer()
	{
		// Ensure mouse is visible for fail screen interaction
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GD.Print($"👁️ Mouse mode set to: {Input.MouseMode}");
		
		// DON'T reduce lives here - FailScreen will handle it when Respawn button is clicked
		// This prevents double life loss (once here, once in FailScreen.OnRespawnPressed)
		
		if (LivesManager.Instance != null)
		{
			// Check if this will be the last life AFTER losing it
			if (LivesManager.Instance.CurrentLives == 1)
			{
				// This will be last life - show game over after losing life
				GD.Print("💀 This is the last life - will show Game Over after losing it");
				var failScreen = GetTree().Root.GetNodeOrNull<FailScreen>("/root/Main/UI/FailScreen");
				if (failScreen != null)
				{
					// FailScreen will handle LoseLife() and show Game Over
					if (failScreen.GetNode<Label>("MessagePanel/VBoxContainer/MessageLabel") is Label msgLabel)
					{
						msgLabel.Text = "Wrong answer!\nYou lost a life!";
					}
					failScreen.FadeIn();
					GD.Print("🎬 Fail screen shown - will handle life loss");
				}
			}
			else if (LivesManager.Instance.CurrentLives > 1)
			{
				// Still have multiple lives
				GD.Print($"💚 Currently have {LivesManager.Instance.CurrentLives} lives - showing fail screen");
				var failScreen = GetTree().Root.GetNodeOrNull<FailScreen>("/root/Main/UI/FailScreen");
				if (failScreen != null)
				{
					if (failScreen.GetNode<Label>("MessagePanel/VBoxContainer/MessageLabel") is Label msgLabel)
					{
						msgLabel.Text = "Wrong answer!\nYou lost a life!";
					}
					failScreen.FadeIn();
					GD.Print("🎬 Fail screen shown with cursor visible");
				}
			}
		}
		else
		{
			GD.PrintErr("❌ LivesManager not found!");
		}
	}
}
