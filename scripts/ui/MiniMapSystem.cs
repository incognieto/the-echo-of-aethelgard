using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mini Map System - Menampilkan top-down view dengan:
/// - Titik hijau: Player position (dengan indikator arah)
/// - Titik kuning: Pickable items & dropped items
/// - Abu-abu: Dinding/obstacle
/// 
/// Obstacle Detection:
/// - StaticBody3D (traditional walls/floors)
/// - CSG nodes (CSGBox3D, CSGCylinder3D, dll)
/// - Nodes dengan nama mengandung "wall", "floor", atau "door"
/// - Nodes di group "minimap_visible"
/// </summary>
public partial class MiniMapSystem : Control
{
	[ExportGroup("Mini Map Settings")]
	[Export] public Vector2 MapSize { get; set; } = new Vector2(200, 200);
	[Export] public Vector2 MapPosition { get; set; } = new Vector2(-220, 20); // Top-right offset from corner
	[Export] public float MapPadding { get; set; } = 2f; // Padding untuk auto-scale (kurangi untuk isi penuh)
	[Export] public bool AutoDetectBounds { get; set; } = true;
	[Export] public Vector2 ManualBoundsMin { get; set; } = new Vector2(-50, -50);
	[Export] public Vector2 ManualBoundsMax { get; set; } = new Vector2(50, 50);
	
	[ExportGroup("Colors")]
	[Export] public Color BackgroundColor { get; set; } = new Color(0.1f, 0.1f, 0.1f, 0.8f);
	[Export] public Color BorderColor { get; set; } = new Color(0.3f, 0.3f, 0.3f, 1f);
	[Export] public Color PlayerColor { get; set; } = new Color(0f, 1f, 0f, 1f); // Green
	[Export] public Color ItemColor { get; set; } = new Color(1f, 1f, 0f, 1f); // Yellow
	[Export] public Color WallColor { get; set; } = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gray
	[Export] public Color InteractableColor { get; set; } = new Color(0.3f, 0.6f, 1f, 0.9f); // Blue for interactables
	[Export] public Color WallBorderColor { get; set; } = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark gray border
	[Export] public Color InteractableBorderColor { get; set; } = new Color(0.1f, 0.3f, 0.6f, 1f); // Dark blue border
	
	[ExportGroup("Sizes")]
	[Export] public int BorderWidth { get; set; } = 2; // Border lebar panel mini map (pixel)
	[Export] public float PlayerDotSize { get; set; } = 6f;
	[Export] public float ItemDotSize { get; set; } = 4f;
	[Export] public float WallThickness { get; set; } = 2f;
	[Export] public float WallBorderThickness { get; set; } = 1f;
	
	private Player _player;
	private Vector2 _worldMin;
	private Vector2 _worldMax;
	private float _scale;
	private List<Node3D> _staticObstacles = new List<Node3D>();
	private Panel _mapPanel;
	private bool _isInitialized = false;

	public override void _Ready()
	{
		// Setup panel container
		_mapPanel = new Panel();
		AddChild(_mapPanel);
		
		// Set panel properties
		_mapPanel.SetAnchorsPreset(LayoutPreset.TopRight);
		_mapPanel.Position = MapPosition;
		_mapPanel.CustomMinimumSize = MapSize;
		_mapPanel.Size = MapSize;
		
		// Set visual style
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = BackgroundColor;
		styleBox.BorderColor = BorderColor;
		styleBox.SetBorderWidthAll(BorderWidth);
		_mapPanel.AddThemeStyleboxOverride("panel", styleBox);
		
		// Wait for scene to be ready
		CallDeferred(nameof(InitializeMiniMap));
	}

	private void InitializeMiniMap()
	{
		// Find player
		_player = GetTree().Root.GetNodeOrNull<Player>("/root/Main/Player");
		if (_player == null)
		{
			GD.PrintErr("❌ MiniMapSystem: Player not found!");
			return;
		}
		
		// Detect world bounds
		if (AutoDetectBounds)
		{
			DetectWorldBounds();
		}
		else
		{
			_worldMin = ManualBoundsMin;
			_worldMax = ManualBoundsMax;
		}
		
		// Calculate scale
		CalculateScale();
		
		// Detect static obstacles
		DetectStaticObstacles();
		
		_isInitialized = true;
		GD.Print($"✓ MiniMap initialized | Bounds: ({_worldMin}) to ({_worldMax}) | Scale: {_scale}");
	}

	private void DetectWorldBounds()
	{
		var mainNode = GetTree().Root.GetNodeOrNull("/root/Main");
		if (mainNode == null) return;
		
		float minX = float.MaxValue, minZ = float.MaxValue;
		float maxX = float.MinValue, maxZ = float.MinValue;
		bool foundAny = false;

		// Detect bounds recursively (includes CSG nodes)
		DetectBoundsRecursive(mainNode, ref minX, ref minZ, ref maxX, ref maxZ, ref foundAny);

		// Fallback to player position if nothing found
		if (!foundAny && _player != null)
		{
			var pos = _player.GlobalPosition;
			minX = pos.X - 20;
			minZ = pos.Z - 20;
			maxX = pos.X + 20;
			maxZ = pos.Z + 20;
		}

		// Add padding
		_worldMin = new Vector2(minX - MapPadding, minZ - MapPadding);
		_worldMax = new Vector2(maxX + MapPadding, maxZ + MapPadding);
	}

	private void DetectBoundsRecursive(Node node, ref float minX, ref float minZ, ref float maxX, ref float maxZ, ref bool foundAny)
	{
		if (node is Node3D node3D)
		{
			// Detect StaticBody3D, CSG nodes, or nodes in group
			bool isRelevant = node3D is StaticBody3D || 
			                  node3D is CsgShape3D || 
			                  node3D.IsInGroup("minimap_visible") ||
			                  node.Name.ToString().ToLower().Contains("wall");
			
			// Skip player and items
			if (isRelevant && !(node3D is PickableItem) && !node.Name.ToString().Contains("Player"))
			{
				var pos = node3D.GlobalPosition;
				minX = Mathf.Min(minX, pos.X);
				minZ = Mathf.Min(minZ, pos.Z);
				maxX = Mathf.Max(maxX, pos.X);
				maxZ = Mathf.Max(maxZ, pos.Z);
				foundAny = true;
			}
		}

		foreach (var child in node.GetChildren())
		{
			if (child is Node childNode)
			{
				DetectBoundsRecursive(childNode, ref minX, ref minZ, ref maxX, ref maxZ, ref foundAny);
			}
		}
	}

	private void CalculateScale()
	{
		Vector2 worldSize = _worldMax - _worldMin;
		float scaleX = MapSize.X / worldSize.X;
		float scaleZ = MapSize.Y / worldSize.Y;
		_scale = Mathf.Min(scaleX, scaleZ);
	}

	private void DetectStaticObstacles()
	{
		_staticObstacles.Clear();
		var mainNode = GetTree().Root.GetNodeOrNull("/root/Main");
		if (mainNode == null) return;

		// Find all static bodies (walls, doors, etc) recursively
		DetectObstaclesRecursive(mainNode);
		
		GD.Print($"✓ MiniMap detected {_staticObstacles.Count} obstacles");
	}

	private void DetectObstaclesRecursive(Node node)
	{
		// Detect multiple types of obstacles with collision:
		// 1. StaticBody3D (traditional walls/floors) - always has collision
		// 2. CSG nodes with use_collision = true
		// 3. Nodes in "minimap_visible" group with actual collision
		
		if (node is Node3D node3D)
		{
			bool isObstacle = false;
			string nodeName = node.Name.ToString().ToLower();
			
			// Skip ceiling - not relevant for top-down view
			if (nodeName.Contains("ceiling"))
			{
				// Skip to children without adding this obstacle
				foreach (var child in node.GetChildren())
				{
					if (child is Node childNode)
					{
						DetectObstaclesRecursive(childNode);
					}
				}
				return;
			}
			
			// Skip player-related objects immediately
			if (nodeName.Contains("player"))
			{
				foreach (var child in node.GetChildren())
				{
					if (child is Node childNode)
					{
						DetectObstaclesRecursive(childNode);
					}
				}
				return;
			}
			
			// Check if this is an obstacle WITH COLLISION
			if (node3D is StaticBody3D && !(node3D is PickableItem))
			{
				// StaticBody3D always has collision
				isObstacle = true;
			}
			else if (node3D is CsgShape3D csgShape)
			{
				// CSG nodes - ONLY if use_collision is enabled
				if (csgShape.UseCollision)
				{
					isObstacle = true;
				}
			}
			else if (node3D.IsInGroup("minimap_visible"))
			{
				// Nodes explicitly marked for minimap
				isObstacle = true;
			}
			
			// Add obstacle if valid
			if (isObstacle)
			{
				_staticObstacles.Add(node3D);
			}
		}

		foreach (var child in node.GetChildren())
		{
			if (child is Node childNode)
			{
				DetectObstaclesRecursive(childNode);
			}
		}
	}

	public override void _Process(double delta)
	{
		if (!_isInitialized) return;
		
		// Force redraw every frame
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!_isInitialized || _player == null) return;

		// Draw background (already handled by panel)
		
		// Draw static obstacles (walls)
		DrawObstacles();
		
		// Draw items
		DrawItems();
		
		// Draw player (last so it's on top)
		DrawPlayer();
	}

	private void DrawObstacles()
	{
		foreach (var obstacle in _staticObstacles)
		{
			if (!IsInstanceValid(obstacle)) continue;
			
			Vector2 mapPos = WorldToMapPosition(obstacle.GlobalPosition);
			Vector2 size = GetObstacleSize(obstacle);
			
			// Convert world size to map size
			Vector2 mapSize = size * _scale;
			
			// Draw rectangle centered at position
			var rect = new Rect2(mapPos - mapSize / 2, mapSize);
			
			// Check if this is an interactable object
			bool isInteractable = obstacle.IsInGroup("minimap_interactable");
			
			// Choose colors based on type
			Color borderColor = isInteractable ? InteractableBorderColor : WallBorderColor;
			Color fillColor = isInteractable ? InteractableColor : WallColor;
			
			// Draw border first (darker outline)
			if (WallBorderThickness > 0)
			{
				var borderRect = rect.Grow(WallBorderThickness);
				DrawRect(borderRect, borderColor);
			}
			
			// Draw main obstacle
			DrawRect(rect, fillColor);
		}
	}
	
	private Vector2 GetObstacleSize(Node3D obstacle)
	{
		Vector2 size = Vector2.One * 4; // Default small size
		
		// Try to get size from CSG nodes
		if (obstacle is CsgBox3D csgBox)
		{
			// CSGBox3D has Size property (Vector3) - need to apply rotation
			var boxSize = csgBox.Size;
			
			// Transform size by rotation to get actual world-space dimensions
			// Take the absolute values of transformed vectors to get bounding box size
			var basis = csgBox.GlobalTransform.Basis;
			var sizeX = (basis.X * boxSize.X).Abs();
			var sizeY = (basis.Y * boxSize.Y).Abs();
			var sizeZ = (basis.Z * boxSize.Z).Abs();
			
			// Combine to get total bounding box in each axis
			var worldSize = sizeX + sizeY + sizeZ;
			
			// Use X and Z for top-down view
			size = new Vector2(worldSize.X, worldSize.Z);
		}
		else if (obstacle is CsgCylinder3D csgCylinder)
		{
			// Cylinder: use radius * 2 for diameter
			float diameter = csgCylinder.Radius * 2;
			size = new Vector2(diameter, diameter);
		}
		else if (obstacle is CsgSphere3D csgSphere)
		{
			// Sphere: use radius * 2 for diameter
			float diameter = csgSphere.Radius * 2;
			size = new Vector2(diameter, diameter);
		}
		else if (obstacle is StaticBody3D staticBody)
		{
			// Try to find CollisionShape3D child
			foreach (var child in staticBody.GetChildren())
			{
				if (child is CollisionShape3D collisionShape && collisionShape.Shape != null)
				{
					if (collisionShape.Shape is BoxShape3D boxShape)
					{
						var boxSize = boxShape.Size;
						
						// Apply rotation from both CollisionShape3D and StaticBody3D
						var basis = staticBody.GlobalTransform.Basis * collisionShape.Transform.Basis;
						var sizeX = (basis.X * boxSize.X).Abs();
						var sizeY = (basis.Y * boxSize.Y).Abs();
						var sizeZ = (basis.Z * boxSize.Z).Abs();
						var worldSize = sizeX + sizeY + sizeZ;
						
						size = new Vector2(worldSize.X, worldSize.Z);
						break;
					}
					else if (collisionShape.Shape is SphereShape3D sphereShape)
					{
						float diameter = sphereShape.Radius * 2;
						size = new Vector2(diameter, diameter);
						break;
					}
					else if (collisionShape.Shape is CylinderShape3D cylinderShape)
					{
						float diameter = cylinderShape.Radius * 2;
						size = new Vector2(diameter, diameter);
						break;
					}
				}
			}
		}
		
		return size;
	}

	private void DrawItems()
	{
		// Get all pickable items and dropped items
		var pickableItems = GetTree().GetNodesInGroup("pickable_items");
		
		foreach (var item in pickableItems)
		{
			if (item is Node3D node3D && IsInstanceValid(node3D))
			{
				Vector2 mapPos = WorldToMapPosition(node3D.GlobalPosition);
				DrawCircle(mapPos, ItemDotSize, ItemColor);
			}
		}
		
		// Also check for dropped items
		var droppedItems = GetTree().Root.GetNode("/root/Main").GetChildren();
		foreach (var node in droppedItems)
		{
			if (node is DroppedItem droppedItem && IsInstanceValid(droppedItem))
			{
				Vector2 mapPos = WorldToMapPosition(droppedItem.GlobalPosition);
				DrawCircle(mapPos, ItemDotSize, ItemColor);
			}
		}
	}

	private void DrawPlayer()
	{
		if (_player == null || !IsInstanceValid(_player)) return;
		
		Vector2 mapPos = WorldToMapPosition(_player.GlobalPosition);
		
		// Draw player as green circle
		DrawCircle(mapPos, PlayerDotSize, PlayerColor);
		
		// Draw direction indicator (small line)
		Vector3 forward = -_player.GlobalTransform.Basis.Z; // Forward direction
		Vector2 directionOnMap = new Vector2(forward.X, forward.Z).Normalized() * (PlayerDotSize + 4);
		DrawLine(mapPos, mapPos + directionOnMap, PlayerColor, 2f);
	}

	private Vector2 WorldToMapPosition(Vector3 worldPos)
	{
		// Convert 3D world position (X, Z) to 2D map position
		Vector2 worldPos2D = new Vector2(worldPos.X, worldPos.Z);
		
		// Normalize to 0-1 range
		Vector2 normalized = (worldPos2D - _worldMin) / (_worldMax - _worldMin);
		
		// Convert to map coordinates (relative to panel)
		Vector2 mapPos = normalized * MapSize;
		
		// Adjust for panel offset from this Control
		mapPos += _mapPanel.Position;
		
		return mapPos;
	}

	// Public method to refresh bounds (useful when level changes dynamically)
	public void RefreshBounds()
	{
		if (AutoDetectBounds)
		{
			DetectWorldBounds();
			CalculateScale();
			DetectStaticObstacles();
			GD.Print("✓ MiniMap bounds refreshed");
		}
	}

	// Public method to set manual bounds
	public void SetManualBounds(Vector2 min, Vector2 max)
	{
		AutoDetectBounds = false;
		_worldMin = min;
		_worldMax = max;
		CalculateScale();
		GD.Print($"✓ MiniMap manual bounds set: ({min}) to ({max})");
	}
}
