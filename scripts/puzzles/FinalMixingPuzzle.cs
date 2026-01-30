using Godot;
using System;

// Kuali besar untuk menggabungkan 3 secondary potion menjadi Teal Potion
public partial class FinalMixingPuzzle : StaticBody3D
{
	[Export] public NodePath RustyDoorPath;
	[Export] public float InteractionDistance = 3.0f;
	
	private StaticBody3D _rustyDoor;
	private FinalMixingUI _finalMixingUI;
	private Player _player;
	private bool _playerNearby = false;
	private Label3D _promptLabel;

	public override void _Ready()
	{
		// Get door reference
		if (RustyDoorPath != null && !RustyDoorPath.IsEmpty)
		{
			_rustyDoor = GetNode<StaticBody3D>(RustyDoorPath);
		}
		
		// Setup interaction area
		SetupInteractionArea();
		
		// Find FinalMixingUI
		CallDeferred(nameof(FindFinalMixingUI));
	}

	private void SetupInteractionArea()
	{
		var area = new Area3D();
		AddChild(area);
		
		var shape = new CollisionShape3D();
		var sphereShape = new SphereShape3D();
		sphereShape.Radius = InteractionDistance;
		shape.Shape = sphereShape;
		area.AddChild(shape);
		
		area.BodyEntered += OnBodyEntered;
		area.BodyExited += OnBodyExited;
		
		// Create prompt label
		_promptLabel = new Label3D();
		_promptLabel.Text = "[E] Final Synthesis - Kuali Besar";
		_promptLabel.Position = new Vector3(0, 2.0f, 0);
		_promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_promptLabel.FontSize = 28;
		_promptLabel.Modulate = new Color(0, 1, 1, 0);
		_promptLabel.OutlineSize = 10;
		_promptLabel.OutlineModulate = Colors.Black;
		AddChild(_promptLabel);
		
		GD.Print("✓ FinalMixingPuzzle interaction area setup complete!");
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player player)
		{
			_player = player;
			_playerNearby = true;
			GD.Print("Player near final mixing cauldron");
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			_player = null;
			_playerNearby = false;
			GD.Print("Player left final mixing cauldron");
		}
	}

	public override void _Process(double delta)
	{
		if (_promptLabel != null)
		{
			var targetAlpha = _playerNearby ? 1.0f : 0.0f;
			var currentAlpha = _promptLabel.Modulate.A;
			var newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, (float)delta * 5.0f);
			_promptLabel.Modulate = new Color(0, 1, 1, newAlpha);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_playerNearby && @event.IsActionPressed("interact"))
		{
			ShowFinalMixingUI();
			GetViewport().SetInputAsHandled();
		}
	}

	private void FindFinalMixingUI()
	{
		var canvasLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>("/root/Main/UI");
		if (canvasLayer != null)
		{
			_finalMixingUI = canvasLayer.GetNodeOrNull<FinalMixingUI>("FinalMixingUI");
			if (_finalMixingUI != null)
			{
				_finalMixingUI.MixingCompleted += OnFinalMixingCompleted;
				GD.Print("✓ FinalMixingPuzzle: FinalMixingUI connected!");
			}
			else
			{
				GD.PrintErr("✗ FinalMixingPuzzle: FinalMixingUI not found!");
			}
		}
		else
		{
			GD.PrintErr("✗ FinalMixingPuzzle: CanvasLayer not found!");
		}
	}

	private void ShowFinalMixingUI()
	{
		if (_finalMixingUI != null && _player != null)
		{
			GD.Print("Opening final mixing UI...");
			var inventorySystem = _player._inventory;
			
			if (inventorySystem != null)
			{
				_finalMixingUI.ShowFinalMixingUI(inventorySystem);
			}
			else
			{
				GD.PrintErr("Cannot show final mixing UI - InventorySystem is null!");
			}
		}
		else
		{
			GD.PrintErr("Cannot show final mixing UI - FinalMixingUI or Player is null!");
		}
	}

	private void OnFinalMixingCompleted(bool success)
	{
		if (success)
		{
			CompletePuzzle();
		}
		else
		{
			GD.Print("Final mixing closed without solving");
		}
		
		RestoreCrosshairVisibility();
	}

	private void CompletePuzzle()
	{
		GD.Print("🎉 FINAL SYNTHESIS COMPLETE! Teal Potion created!");
		
		if (_rustyDoor != null)
		{
			_rustyDoor.QueueFree();
			GD.Print("✓ Rusty door dissolved by Teal Potion!");
		}
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
}
