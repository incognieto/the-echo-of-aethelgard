using Godot;
using System;

/// <summary>
/// Exit area for Level 5 (Sewer) - transitions to Level 2 after winning
/// </summary>
public partial class SewerExitArea : Area3D
{
	[Export] public Color AreaColor { get; set; } = new Color(0.2f, 0.8f, 0.2f, 0.5f);
	
	private bool _isTransitioning = false;
	private ColorRect _fadeOverlay;
	private float _fadeProgress = 0f;
	private const float FADE_DURATION = 2.0f;
	private const string NEXT_LEVEL_PATH = "res://scenes/levels/level_2_bridge/Main.tscn";

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		
		// Create visual indicator for the area (optional)
		var mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
		if (mesh != null)
		{
			var material = new StandardMaterial3D();
			material.AlbedoColor = AreaColor;
			material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
			mesh.SetSurfaceOverrideMaterial(0, material);
		}
		
		GD.Print("✅ Sewer Exit Area ready");
	}

	private void OnBodyEntered(Node3D body)
	{
		if (_isTransitioning) return;
		
		// Check if it's the player (CharacterBody3D or has "Player" in name)
		if (body is CharacterBody3D || body.Name.ToString().Contains("Player"))
		{
			GD.Print("🎉 Player reached exit! Starting transition to Level 2...");
			StartTransition();
		}
	}

	private void StartTransition()
	{
		_isTransitioning = true;
		_fadeProgress = 0f;
		
		// Stop timer if still running
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.StopTimer();
			GD.Print("⏱️ Timer stopped (Level completed)");
		}
		
		// Create fade overlay
		_fadeOverlay = new ColorRect();
		_fadeOverlay.Color = new Color(0, 0, 0, 0);
		_fadeOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		
		// Add to viewport as CanvasLayer for UI overlay
		var canvasLayer = new CanvasLayer();
		canvasLayer.Layer = 100; // High layer to be on top of everything
		GetTree().Root.AddChild(canvasLayer);
		canvasLayer.AddChild(_fadeOverlay);
		
		GD.Print($"🌑 Starting fade to black...");
	}

	public override void _Process(double delta)
	{
		if (!_isTransitioning || _fadeOverlay == null) return;
		
		_fadeProgress += (float)delta / FADE_DURATION;
		
		if (_fadeProgress >= 1.0f)
		{
			// Transition complete, load next level
			LoadNextLevel();
		}
		else
		{
			// Update fade alpha (fade to black)
			var alpha = Mathf.Clamp(_fadeProgress, 0f, 1f);
			_fadeOverlay.Color = new Color(0, 0, 0, alpha);
		}
	}

	private void LoadNextLevel()
	{
		GD.Print($"✅ Loading Level 2: {NEXT_LEVEL_PATH}");
		
		// Clean up fade overlay
		if (_fadeOverlay != null && _fadeOverlay.GetParent() != null)
		{
			var canvasLayer = _fadeOverlay.GetParent();
			canvasLayer.GetParent()?.RemoveChild(canvasLayer);
			canvasLayer.QueueFree();
			_fadeOverlay = null;
		}
		
		// Change scene to Level 2
		GetTree().ChangeSceneToFile(NEXT_LEVEL_PATH);
	}
}
