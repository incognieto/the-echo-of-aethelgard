using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Static minimap renderer using Godot's group system.
/// Shows entire level with color-coded dots for different object types.
/// </summary>
public partial class MinimapRenderer : Control
{
	[ExportGroup("Minimap Settings")]
	[Export] public Vector2 MinimapSize { get; set; } = new Vector2(200, 200);
	[Export] public float Margin { get; set; } = 20f; // Distance from screen edges
	[Export] public float Padding { get; set; } = 10f; // Internal padding
	
	[ExportGroup("Visual Settings")]
	[Export] public Color BackgroundColor { get; set; } = new Color(0, 0, 0, 0.7f); // Semi-transparent black
	[Export] public Color BorderColor { get; set; } = new Color(1, 1, 1, 0.8f); // White border
	[Export] public float BorderWidth { get; set; } = 2f;
	
	[ExportGroup("Object Colors")]
	[Export] public Color PlayerColor { get; set; } = new Color(0, 1, 0, 1); // Green
	[Export] public Color ItemColor { get; set; } = new Color(1, 1, 0, 1); // Yellow
	[Export] public Color InteractableColor { get; set; } = new Color(0.7f, 0, 1, 1); // Purple
	[Export] public Color ObstacleColor { get; set; } = new Color(0.2f, 0.2f, 0.2f, 1); // Dark gray
	
	[ExportGroup("Dot Sizes")]
	[Export] public float PlayerDotSize { get; set; } = 6f;
	[Export] public float ItemDotSize { get; set; } = 4f;
	[Export] public float InteractableDotSize { get; set; } = 5f;
	[Export] public float ObstacleDotSize { get; set; } = 3f;
	
	[ExportGroup("Obstacle Settings")]
	[Export] public float MinObstacleThickness { get; set; } = 2f; // Minimum pixel thickness for walls
	
	[ExportGroup("Level Bounds")]
	[Export] public bool AutoDetectBounds { get; set; } = true;
	[Export] public Vector2 ManualBoundsMin { get; set; } = new Vector2(-50, -50);
	[Export] public Vector2 ManualBoundsMax { get; set; } = new Vector2(50, 50);
	[Export] public float BoundsPadding { get; set; } = 5f; // Extra padding for bounds
	
	// Group names - users will add nodes to these groups in Godot editor
	private const string GROUP_PLAYER = "minimap_player";
	private const string GROUP_ITEM = "minimap_item";
	private const string GROUP_INTERACTABLE = "minimap_interactable";
	private const string GROUP_OBSTACLE = "minimap_obstacle";
	
	private Vector2 _levelMin;
	private Vector2 _levelMax;
	private Vector2 _mapScale;
	private Rect2 _minimapRect;

	public override void _Ready()
	{
		// Set layout mode and anchors explicitly for bottom-right positioning
		LayoutMode = 1; // Anchors mode
		
		// Set anchors to bottom-right
		AnchorLeft = 1.0f;
		AnchorTop = 1.0f;
		AnchorRight = 1.0f;
		AnchorBottom = 1.0f;
		
		// Set offsets (negative values to pull inward from bottom-right)
		OffsetLeft = -Margin - MinimapSize.X;
		OffsetTop = -Margin - MinimapSize.Y;
		OffsetRight = -Margin;
		OffsetBottom = -Margin;
		
		// Ensure visibility
		Visible = true;
		MouseFilter = MouseFilterEnum.Ignore; // Don't block mouse input
		
		// Calculate level bounds
		CalculateLevelBounds();
		
		// Calculate minimap rect (with padding)
		_minimapRect = new Rect2(Padding, Padding, MinimapSize.X - Padding * 2, MinimapSize.Y - Padding * 2);
		
		GD.Print("========================================");
		GD.Print("✓ Minimap initialized!");
		GD.Print($"  Position: Bottom-Right");
		GD.Print($"  Size: {MinimapSize}");
		GD.Print($"  Anchors: (1, 1) to (1, 1)");
		GD.Print($"  Offsets: ({OffsetLeft}, {OffsetTop}) to ({OffsetRight}, {OffsetBottom})");
		GD.Print($"  Visible: {Visible}");
		GD.Print($"  Level Bounds: ({_levelMin}) to ({_levelMax})");
		GD.Print("========================================");
		GD.Print("Available Groups:");
		GD.Print($"  • {GROUP_PLAYER} → {PlayerColor} (Player)");
		GD.Print($"  • {GROUP_ITEM} → {ItemColor} (Items)");
		GD.Print($"  • {GROUP_INTERACTABLE} → {InteractableColor} (Puzzles/Doors)");
		GD.Print($"  • {GROUP_OBSTACLE} → {ObstacleColor} (Walls/Obstacles)");
		GD.Print("========================================");
	}

	public override void _Process(double delta)
	{
		QueueRedraw(); // Request redraw every frame
	}

	public override void _Draw()
	{
		// Draw background
		DrawRect(new Rect2(Vector2.Zero, MinimapSize), BackgroundColor, true);
		
		// Draw border
		DrawRect(new Rect2(Vector2.Zero, MinimapSize), BorderColor, false, BorderWidth);
		
		// Draw objects by layer (bottom to top)
		// Obstacles are drawn with their actual shapes, not dots
		int obstacleCount = DrawObstacleShapes();
		int interactableCount = DrawGroupObjects(GROUP_INTERACTABLE, InteractableColor, InteractableDotSize);
		int itemCount = DrawGroupObjects(GROUP_ITEM, ItemColor, ItemDotSize);
		int playerCount = DrawGroupObjects(GROUP_PLAYER, PlayerColor, PlayerDotSize);
	}

	private int DrawGroupObjects(string groupName, Color color, float dotSize)
	{
		var nodes = GetTree().GetNodesInGroup(groupName);
		int count = 0;
		
		foreach (var node in nodes)
		{
			if (node is Node3D node3D)
			{
				Vector2 worldPos = new Vector2(node3D.GlobalPosition.X, node3D.GlobalPosition.Z);
				Vector2 minimapPos = WorldToMinimapPosition(worldPos);
				
				// Draw circle (dot)
				DrawCircle(minimapPos, dotSize, color);
				count++;
			}
			else if (node is Node2D node2D)
			{
				// Support for 2D nodes if needed
				Vector2 minimapPos = WorldToMinimapPosition(node2D.GlobalPosition);
				DrawCircle(minimapPos, dotSize, color);
				count++;
			}
		}
		
		return count;
	}
	
	private int DrawObstacleShapes()
	{
		var nodes = GetTree().GetNodesInGroup(GROUP_OBSTACLE);
		int count = 0;
		
		foreach (var node in nodes)
		{
			if (node is MeshInstance3D meshInstance)
			{
				DrawObstacleFromMesh(meshInstance);
				count++;
			}
			else if (node is Node3D node3D)
			{
				// Fallback: draw as dot if not MeshInstance3D
				Vector2 worldPos = new Vector2(node3D.GlobalPosition.X, node3D.GlobalPosition.Z);
				Vector2 minimapPos = WorldToMinimapPosition(worldPos);
				DrawCircle(minimapPos, ObstacleDotSize, ObstacleColor);
				count++;
			}
		}
		
		if (count > 0 && Engine.GetFramesDrawn() % 60 == 0) // Log once per second
		{
			GD.Print($"Minimap: Drawing {count} obstacles");
		}
		
		return count;
	}
	
	private void DrawObstacleFromMesh(MeshInstance3D meshInstance)
	{
		Vector3 worldPos3D = meshInstance.GlobalPosition;
		Vector2 worldPos = new Vector2(worldPos3D.X, worldPos3D.Z);
		Vector2 minimapPos = WorldToMinimapPosition(worldPos);
		
		Mesh mesh = meshInstance.Mesh;
		
		if (mesh == null)
		{
			// No mesh, draw as dot
			DrawCircle(minimapPos, ObstacleDotSize, ObstacleColor);
			return;
		}
		
		// Get scale from transform - this represents the actual size in world space
		Vector3 scale = meshInstance.Scale;
		Transform3D globalTransform = meshInstance.GlobalTransform;
		Vector3 globalScale = globalTransform.Basis.Scale;
		
		// Get rotation
		float rotationY = meshInstance.GlobalRotation.Y;
		
		// Debug: Log first few obstacles once
		if (Engine.GetFramesDrawn() == 60)
		{
			GD.Print($"Obstacle '{meshInstance.Name}': Pos({worldPos3D.X:F2}, {worldPos3D.Z:F2}), Scale({globalScale.X:F2}, {globalScale.Z:F2})");
		}
		
		if (mesh is BoxMesh boxMesh)
		{
			// BoxMesh default size is (1, 1, 1), multiply by scale to get actual size
			Vector3 size = boxMesh.Size;
			float worldWidth = size.X * globalScale.X;
			float worldHeight = size.Z * globalScale.Z; // Z for top-down view
			
			// Convert world size to minimap size
			float mapWidth = WorldSizeToMinimapSize(worldWidth);
			float mapHeight = WorldSizeToMinimapSize(worldHeight);
			
			// Apply minimum thickness to ensure walls are visible
			mapWidth = Mathf.Max(mapWidth, MinObstacleThickness);
			mapHeight = Mathf.Max(mapHeight, MinObstacleThickness);
			
			// Draw rectangle with rotation support
			if (Mathf.Abs(rotationY) < 0.1f || Mathf.Abs(rotationY - Mathf.Pi) < 0.1f)
			{
				// No rotation or 180° - draw axis-aligned rectangle
				Rect2 rect = new Rect2(
					minimapPos.X - mapWidth / 2,
					minimapPos.Y - mapHeight / 2,
					mapWidth,
					mapHeight
				);
				DrawRect(rect, ObstacleColor, true);
			}
			else if (Mathf.Abs(rotationY - Mathf.Pi / 2) < 0.1f || Mathf.Abs(rotationY + Mathf.Pi / 2) < 0.1f)
			{
				// 90° or -90° rotation - swap width and height
				Rect2 rect = new Rect2(
					minimapPos.X - mapHeight / 2,
					minimapPos.Y - mapWidth / 2,
					mapHeight,
					mapWidth
				);
				DrawRect(rect, ObstacleColor, true);
			}
			else
			{
				// Arbitrary rotation - draw rotated rectangle using polygon
				Vector2[] points = new Vector2[4];
				float halfW = mapWidth / 2;
				float halfH = mapHeight / 2;
				
				// Calculate rotated corners
				float cos = Mathf.Cos(rotationY);
				float sin = Mathf.Sin(rotationY);
				
				points[0] = minimapPos + new Vector2(-halfW * cos + halfH * sin, -halfW * sin - halfH * cos);
				points[1] = minimapPos + new Vector2(halfW * cos + halfH * sin, halfW * sin - halfH * cos);
				points[2] = minimapPos + new Vector2(halfW * cos - halfH * sin, halfW * sin + halfH * cos);
				points[3] = minimapPos + new Vector2(-halfW * cos - halfH * sin, -halfW * sin + halfH * cos);
				
				DrawColoredPolygon(points, ObstacleColor);
			}
		}
		else if (mesh is CylinderMesh cylinderMesh)
		{
			// CylinderMesh uses radius
			float radius = cylinderMesh.TopRadius * Mathf.Max(globalScale.X, globalScale.Z);
			float mapRadius = WorldSizeToMinimapSize(radius * 2) / 2;
			DrawCircle(minimapPos, mapRadius, ObstacleColor);
		}
		else if (mesh is SphereMesh sphereMesh)
		{
			// SphereMesh uses radius
			float radius = sphereMesh.Radius * Mathf.Max(globalScale.X, globalScale.Z);
			float mapRadius = WorldSizeToMinimapSize(radius * 2) / 2;
			DrawCircle(minimapPos, mapRadius, ObstacleColor);
		}
		else if (mesh is CapsuleMesh capsuleMesh)
		{
			// CapsuleMesh uses radius (simplified as circle)
			float radius = capsuleMesh.Radius * Mathf.Max(globalScale.X, globalScale.Z);
			float mapRadius = WorldSizeToMinimapSize(radius * 2) / 2;
			DrawCircle(minimapPos, mapRadius, ObstacleColor);
		}
		else
		{
			// Unknown mesh type - fallback to dot
			DrawCircle(minimapPos, ObstacleDotSize, ObstacleColor);
		}
	}
	
	private float WorldSizeToMinimapSize(float worldSize)
	{
		// Calculate scale factor
		float worldWidth = _levelMax.X - _levelMin.X;
		float minimapWidth = _minimapRect.Size.X;
		float scale = minimapWidth / worldWidth;
		
		return worldSize * scale;
	}

	private Vector2 WorldToMinimapPosition(Vector2 worldPos)
	{
		// Normalize world position to 0-1 range
		float normalizedX = Mathf.InverseLerp(_levelMin.X, _levelMax.X, worldPos.X);
		float normalizedY = Mathf.InverseLerp(_levelMin.Y, _levelMax.Y, worldPos.Y);
		
		// Map to minimap rect
		float mapX = _minimapRect.Position.X + normalizedX * _minimapRect.Size.X;
		float mapY = _minimapRect.Position.Y + normalizedY * _minimapRect.Size.Y;
		
		return new Vector2(mapX, mapY);
	}

	private void CalculateLevelBounds()
	{
		if (!AutoDetectBounds)
		{
			_levelMin = ManualBoundsMin;
			_levelMax = ManualBoundsMax;
			return;
		}
		
		// Collect all positions from all minimap groups
		List<Vector2> allPositions = new List<Vector2>();
		
		string[] allGroups = { GROUP_PLAYER, GROUP_ITEM, GROUP_INTERACTABLE, GROUP_OBSTACLE };
		
		foreach (string groupName in allGroups)
		{
			var nodes = GetTree().GetNodesInGroup(groupName);
			
			foreach (var node in nodes)
			{
				if (node is Node3D node3D)
				{
					allPositions.Add(new Vector2(node3D.GlobalPosition.X, node3D.GlobalPosition.Z));
				}
				else if (node is Node2D node2D)
				{
					allPositions.Add(node2D.GlobalPosition);
				}
			}
		}
		
		if (allPositions.Count == 0)
		{
			GD.PrintErr("⚠ Minimap: No objects found in any group. Using manual bounds.");
			_levelMin = ManualBoundsMin;
			_levelMax = ManualBoundsMax;
			return;
		}
		
		// Find min and max bounds
		float minX = allPositions.Min(p => p.X);
		float maxX = allPositions.Max(p => p.X);
		float minY = allPositions.Min(p => p.Y);
		float maxY = allPositions.Max(p => p.Y);
		
		// Add padding
		_levelMin = new Vector2(minX - BoundsPadding, minY - BoundsPadding);
		_levelMax = new Vector2(maxX + BoundsPadding, maxY + BoundsPadding);
		
		// Ensure minimum size (prevent division by zero)
		if (_levelMax.X - _levelMin.X < 1f)
		{
			_levelMin.X -= 5f;
			_levelMax.X += 5f;
		}
		if (_levelMax.Y - _levelMin.Y < 1f)
		{
			_levelMin.Y -= 5f;
			_levelMax.Y += 5f;
		}
	}
	
	/// <summary>
	/// Recalculate level bounds - call this if objects are spawned dynamically
	/// </summary>
	public void RefreshBounds()
	{
		CalculateLevelBounds();
		GD.Print($"Minimap bounds refreshed: ({_levelMin}) to ({_levelMax})");
	}
}
