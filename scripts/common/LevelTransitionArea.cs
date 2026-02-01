using Godot;
using System;

public partial class LevelTransitionArea : Area3D
{
    [Export] public int NextLevel { get; set; } = 2;
    [Export] public Color AreaColor { get; set; } = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    
    private bool _isTransitioning = false;
    private ColorRect _fadeOverlay;
    private float _fadeProgress = 0f;
    private const float FADE_DURATION = 2.0f;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        
        // Create visual indicator for the area
        var mesh = GetNode<MeshInstance3D>("MeshInstance3D");
        if (mesh != null)
        {
            var material = new StandardMaterial3D();
            material.AlbedoColor = AreaColor;
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            mesh.SetSurfaceOverrideMaterial(0, material);
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_isTransitioning) return;
        
        if (body is CharacterBody3D player)
        {
            StartTransition();
        }
    }

    private void StartTransition()
    {
        _isTransitioning = true;
        _fadeProgress = 0f;
        
        // Create fade overlay
        _fadeOverlay = new ColorRect();
        _fadeOverlay.Color = new Color(0, 0, 0, 0);
        _fadeOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        
        // Add to viewport as CanvasLayer for UI overlay
        var canvasLayer = new CanvasLayer();
        canvasLayer.Layer = 100; // High layer to be on top
        GetTree().Root.AddChild(canvasLayer);
        canvasLayer.AddChild(_fadeOverlay);
        
        GD.Print($"Starting transition to level {NextLevel}...");
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
            // Update fade alpha
            var alpha = Mathf.Clamp(_fadeProgress, 0f, 1f);
            _fadeOverlay.Color = new Color(0, 0, 0, alpha);
        }
    }

    private void LoadNextLevel()
    {
        string levelPath = NextLevel switch
        {
            1 => "res://scenes/levels/level_1_cell/Main.tscn",
            2 => "res://scenes/levels/level_2_bridge/Main.tscn",
            3 => "res://scenes/levels/level_3_lab/Main.tscn",
            4 => "res://scenes/levels/level_4_library/Main.tscn",
            5 => "res://scenes/levels/level_5_Sewer/Main.tscn",
            _ => "res://scenes/levels/level_1_cell/Main.tscn"
        };
        
        GD.Print($"Loading level: {levelPath}");
        
        // Remove fade overlay before changing scene
        if (_fadeOverlay != null && _fadeOverlay.GetParent() != null)
        {
            var canvasLayer = _fadeOverlay.GetParent();
            canvasLayer.GetParent()?.RemoveChild(canvasLayer);
            canvasLayer.QueueFree();
            _fadeOverlay = null;
        }
        
        GetTree().ChangeSceneToFile(levelPath);
    }
}
