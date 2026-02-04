using Godot;
using System;
using System.Collections.Generic;

public partial class MapDisplay : Control
{
	private Player _player;
	private float _mapRadius = 100f;
	private Color _playerColor = new Color(0, 1, 0, 1);
	private Color _itemColor = new Color(1, 1, 0, 1);
	private Color _npcColor = new Color(1, 0, 1, 1);
	private Color _borderColor = new Color(1, 1, 1, 1);
	private Color _backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
	
	private string _currentLevel = "";
	private SubViewport _mapViewport;
	private Camera3D _mapCamera;
	
	// Level-themed colors
	private Dictionary<string, Color> _levelColors = new Dictionary<string, Color>()
	{
		{"level_0_prologue", new Color(0.3f, 0.2f, 0.2f, 0.9f)},
		{"level_1_cell", new Color(0.2f, 0.2f, 0.3f, 0.9f)},
		{"level_2_bridge", new Color(0.3f, 0.25f, 0.15f, 0.9f)},
		{"level_3_lab", new Color(0.25f, 0.3f, 0.25f, 0.9f)},
		{"level_4_library", new Color(0.2f, 0.18f, 0.15f, 0.9f)},
		{"level_5_Sewer", new Color(0.15f, 0.2f, 0.2f, 0.9f)},
	};
	
	public override void _Ready()
	{
		_player = GetTree().Root.GetNodeOrNull<Player>("Main/Player");
		DetectCurrentLevel();
		
		// Defer viewport creation sampai tree setup selesai
		CallDeferred(MethodName.CreateMapViewport);
		
		GD.Print("✓ MapDisplay initialized!");
	}
	
	private void CreateMapViewport()
	{
		// Create SubViewport untuk render top-down view
		_mapViewport = new SubViewport();
		_mapViewport.Size = new Vector2I(200, 200);
		_mapViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		
		// Create camera untuk top-down view
		_mapCamera = new Camera3D();
		_mapViewport.AddChild(_mapCamera);
		
		// Add viewport to root
		GetTree().Root.AddChild(_mapViewport);
		
		// Setup camera setelah keduanya di tree
		CallDeferred(MethodName.SetupMapCamera);
	}
	
	private void SetupMapCamera()
	{
		if (_mapCamera != null && _mapCamera.IsInsideTree())
		{
			_mapCamera.GlobalPosition = new Vector3(0, 50, 0);
			_mapCamera.LookAt(new Vector3(0, 0, 0), Vector3.Back);
			_mapCamera.Current = true;
			GD.Print("✓ MapDisplay camera setup complete!");
		}
	}
	
	private void DetectCurrentLevel()
	{
		var root = GetTree().Root;
		if (root.HasNode("Main"))
		{
			var main = root.GetNode("Main");
			string sceneName = main.SceneFilePath;
			
			// Extract level name from path
			if (sceneName.Contains("level_"))
			{
				int startIdx = sceneName.LastIndexOf("level_");
				int endIdx = sceneName.IndexOf("/", startIdx);
				if (endIdx == -1) endIdx = sceneName.Length;
				
				_currentLevel = sceneName.Substring(startIdx, endIdx - startIdx);
			}
		}
		
		// Set background color based on level
		if (_levelColors.ContainsKey(_currentLevel))
		{
			_backgroundColor = _levelColors[_currentLevel];
		}
		
		GD.Print($"✓ Detected level: {_currentLevel}");
	}
	
	public override void _Draw()
	{
		if (_player == null) return;
		
		Vector2 mapCenter = new Vector2(110, 110);
		float mapRadius = 100f;
		
		// Define world map bounds - adjust these based on your level
		float worldMinX = -200f;
		float worldMaxX = 200f;
		float worldMinZ = -200f;
		float worldMaxZ = 200f;
		float worldWidth = worldMaxX - worldMinX;
		float worldHeight = worldMaxZ - worldMinZ;
		
		// Draw solid background circle
		DrawCircle(mapCenter, mapRadius, _backgroundColor);
		
		Vector3 playerPos = _player.GlobalPosition;
		
		// Draw rendered level viewport texture if available
		if (_mapViewport != null && _mapCamera != null && _mapCamera.IsInsideTree())
		{
			_mapCamera.GlobalPosition = new Vector3(playerPos.X, 50, playerPos.Z);
			_mapCamera.LookAt(playerPos, Vector3.Back);
			
			Texture2D viewportTexture = _mapViewport.GetTexture();
			if (viewportTexture != null)
			{
				DrawSetTransformMatrix(Transform2D.Identity.Translated(mapCenter - new Vector2(mapRadius, mapRadius)));
				DrawTextureRect(viewportTexture, new Rect2(Vector2.Zero, new Vector2(mapRadius * 2, mapRadius * 2)), false);
				DrawSetTransformMatrix(Transform2D.Identity);
			}
		}
		
		// Draw border circle
		DrawArc(mapCenter, mapRadius, 0, Mathf.Tau, 64, _borderColor, 2);
		
		// Get all objects with ABSOLUTE positioning
		var allObjects = new Dictionary<Node3D, Color>();
		allObjects[_player] = _playerColor;
		
		var root = GetTree().Root;
		if (root.HasNode("Main"))
		{
			var main = root.GetNode("Main");
			CollectAllObjects(main, allObjects);
		}
		
		// Draw object markers using ABSOLUTE world coordinates
		foreach (var obj in allObjects)
		{
			Vector3 objPos = obj.Key.GlobalPosition;
			
			// Convert absolute world coordinates to map coordinates
			// Normalize position within world bounds
			float normalizedX = (objPos.X - worldMinX) / worldWidth;
			float normalizedZ = (objPos.Z - worldMinZ) / worldHeight;
			
			// Only draw if within world bounds
			if (normalizedX >= 0 && normalizedX <= 1 && normalizedZ >= 0 && normalizedZ <= 1)
			{
				// Map to circle coordinates
				float mapX = mapCenter.X + (normalizedX - 0.5f) * mapRadius * 2;
				float mapY = mapCenter.Y + (normalizedZ - 0.5f) * mapRadius * 2;
				
				Vector2 mapPos = new Vector2(mapX, mapY);
				Vector2 relToCenter = mapPos - mapCenter;
				
				// Clamp to circular boundary
				if (relToCenter.Length() > mapRadius)
				{
					relToCenter = relToCenter.Normalized() * mapRadius;
					mapPos = mapCenter + relToCenter;
				}
				
				float pointSize = obj.Key == _player ? 8f : 5f;
				DrawCircle(mapPos, pointSize, obj.Value);
			}
		}
	}
	
	private void CollectAllObjects(Node parent, Dictionary<Node3D, Color> objects)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is Node3D node3d && node3d != _player)
			{
				// Categorize objects
				if (child is PickableItem)
				{
					objects[node3d] = _itemColor;
				}
				else if (child.Name.ToString().Contains("Door") || child.Name.ToString().Contains("Puzzle"))
				{
					objects[node3d] = _npcColor;
				}
			}
			
			// Recurse into children
			CollectAllObjects(child, objects);
		}
	}
	
	public override void _Process(double delta)
	{
		// Update map every frame
		QueueRedraw();
	}
	
	private void DrawArc(Vector2 center, float radius, float fromAngle, float toAngle, int pointCount, Color color, float width = 1f)
	{
		Vector2 prevPoint = center + new Vector2(Mathf.Cos(fromAngle), Mathf.Sin(fromAngle)) * radius;
		
		for (int i = 1; i <= pointCount; i++)
		{
			float angle = Mathf.Lerp(fromAngle, toAngle, (float)i / pointCount);
			Vector2 nextPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
			DrawLine(prevPoint, nextPoint, color, width);
			prevPoint = nextPoint;
		}
	}
}
