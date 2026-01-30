using Godot;
using System;

// Chemical mixing puzzle untuk level 3
public partial class MixingPuzzle : StaticBody3D
{
	[Export]
	public float InteractionRange { get; set; } = 3.0f;
	
	[Export]
	public NodePath RustyDoorPath { get; set; }
	
	private Label3D _promptLabel;
	private Area3D _interactionArea;
	private bool _playerNearby = false;
	private bool _isPuzzleSolved = false;
	private Node3D _rustyDoor;
	private MixingUI _mixingUI;
	private Player _player;

	public override void _Ready()
	{
		// Setup interaction area
		CallDeferred(nameof(SetupInteractionArea));
		
		// Find MixingUI
		CallDeferred(nameof(FindMixingUI));
		
		// Find rusty door
		if (RustyDoorPath != null && !RustyDoorPath.IsEmpty)
		{
			_rustyDoor = GetNode<Node3D>(RustyDoorPath);
		}
		
		GD.Print("MixingPuzzle initialized - Nested Recipe System");
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
		_promptLabel.Text = "[E] Mix Chemicals";
		_promptLabel.Modulate = new Color(0, 1, 1); // Cyan untuk lab theme
		_promptLabel.OutlineModulate = Colors.Black;
		_promptLabel.OutlineSize = 12;
		_promptLabel.FontSize = 32;
		_promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_promptLabel.Position = new Vector3(0, 1.5f, 0);
		_promptLabel.Visible = false;
		AddChild(_promptLabel);
		
		GD.Print("✓ MixingPuzzle interaction area setup complete!");
	}

	private void FindMixingUI()
	{
		var canvasLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>("/root/Main/UI");
		if (canvasLayer != null)
		{
			_mixingUI = canvasLayer.GetNodeOrNull<MixingUI>("MixingUI");
			if (_mixingUI != null)
			{
				_mixingUI.MixingCompleted += OnMixingCompleted;
				GD.Print("✓ MixingPuzzle: MixingUI connected!");
			}
			else
			{
				GD.PrintErr("✗ MixingPuzzle: MixingUI not found!");
			}
		}
		else
		{
			GD.PrintErr("✗ MixingPuzzle: CanvasLayer not found!");
		}
	}

	public override void _Process(double delta)
	{
		// Update prompt visibility
		if (_promptLabel != null)
		{
			if (!_isPuzzleSolved && _playerNearby)
			{
				_promptLabel.Visible = true;
				var alpha = Mathf.Abs(Mathf.Sin((float)Time.GetTicksMsec() / 500.0f));
				_promptLabel.Modulate = new Color(0, 1, 1, Mathf.Lerp(0.6f, 1.0f, alpha));
			}
			else if (_isPuzzleSolved && _playerNearby)
			{
				_promptLabel.Text = "Mixing Complete!";
				_promptLabel.Modulate = new Color(0, 1, 0, 1); // Green
				_promptLabel.Visible = true;
			}
			else
			{
				_promptLabel.Visible = false;
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_playerNearby && !_isPuzzleSolved && @event.IsActionPressed("interact"))
		{
			ShowMixingUI();
			GetViewport().SetInputAsHandled();
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body.IsInGroup("player") || body.Name == "Player")
		{
			_playerNearby = true;
			_player = body as Player;
			GD.Print("Player near mixing table");
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body.IsInGroup("player") || body.Name == "Player")
		{
			_playerNearby = false;
			_player = null;
			GD.Print("Player left mixing table");
		}
	}

	private void ShowMixingUI()
	{
		if (_mixingUI != null && _player != null)
		{
			GD.Print("Opening mixing UI...");
			var inventorySystem = _player.GetNode<InventorySystem>("InventorySystem");
			_mixingUI.ShowMixingUI(inventorySystem);
		}
		else
		{
			GD.PrintErr("Cannot show mixing UI - MixingUI or Player is null!");
		}
	}

	private void OnMixingCompleted(bool success)
	{
		if (success)
		{
			CompletePuzzle();
		}
		else
		{
			GD.Print("Mixing puzzle closed without solving or explosion occurred");
		}
		
		// Restore crosshair visibility
		RestoreCrosshairVisibility();
	}
	
	private void RestoreCrosshairVisibility()
	{
		var canvasLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>("/root/Main/UI");
		if (canvasLayer != null)
		{
			var inventoryUI = canvasLayer.GetNodeOrNull<InventoryUI>("InventoryUI");
			if (inventoryUI != null)
			{
				inventoryUI.SetCrosshairVisible(true);
			}
		}
	}

	private void CompletePuzzle()
	{
		_isPuzzleSolved = true;
		GD.Print("Mixing puzzle solved! Creating Softener item...");
		
		// Destroy rusty door if exists
		if (_rustyDoor != null)
		{
			GD.Print("Destroying rusty door...");
			_rustyDoor.QueueFree();
		}
		
		// Give Softener item to player
		if (_player != null)
		{
			var softenerData = new ItemData("softener", "Cairan Pelunak", 1, false);
			softenerData.Description = "Cairan kuat yang dapat melunakkan material keras";
			
			var inventorySystem = _player.GetNode<InventorySystem>("InventorySystem");
			if (inventorySystem != null)
			{
				inventorySystem.AddItem(softenerData, 1);
				GD.Print("✓ Cairan Pelunak added to inventory!");
			}
		}
	}
}
