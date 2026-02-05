using Godot;
using System;

/// <summary>
/// Helper script untuk menambahkan MiniMap secara programmatic ke scene.
/// 
/// CARA PAKAI:
/// 1. Attach script ini ke node UI (CanvasLayer) di Main scene
/// 2. Mini map akan otomatis ditambahkan saat scene dimuat
/// 
/// OPTIONAL: Jika ingin disable, set EnableMiniMap = false di Inspector
/// </summary>
public partial class MiniMapHelper : Node
{
	[Export] public bool EnableMiniMap { get; set; } = true;
	[Export] public PackedScene MiniMapScene { get; set; }

	public override void _Ready()
	{
		if (!EnableMiniMap)
		{
			GD.Print("MiniMap disabled via MiniMapHelper");
			return;
		}

		// Load MiniMap scene if not set
		if (MiniMapScene == null)
		{
			MiniMapScene = GD.Load<PackedScene>("res://scenes/ui/MiniMap.tscn");
		}

		if (MiniMapScene == null)
		{
			GD.PrintErr("❌ MiniMapHelper: Failed to load MiniMap.tscn");
			return;
		}

		// Instantiate and add to parent (UI CanvasLayer)
		var miniMapInstance = MiniMapScene.Instantiate();
		GetParent().AddChild(miniMapInstance);

		GD.Print("✓ MiniMap added successfully via MiniMapHelper");
	}
}
