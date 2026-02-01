using Godot;
using System;

// Script helper untuk setup game
// Note: Player now handles InventoryUI connection directly
public partial class GameSetup : Node
{
	[Export] public int CurrentLevel = 1; // Set di editor: 1, 3, atau 4
	
	private ColorRect _fadeOverlay;
	private float _fadeInProgress = 0f;
	private const float FADE_IN_DURATION = 1.5f;
	private bool _isFadingIn = false;
	
	public override void _Ready()
	{
		GD.Print($"GameSetup: Game initialized - Level {CurrentLevel}");
		
		// Start with fade-in effect
		StartFadeIn();
		
		// Spawn ancient book di inventory untuk level 3 dan 4
		if (CurrentLevel == 3 || CurrentLevel == 4)
		{
			CallDeferred(nameof(SpawnAncientBook));
		}
	}
	
	private void StartFadeIn()
	{
		CallDeferred(nameof(CreateFadeOverlay));
	}
	
	private void CreateFadeOverlay()
	{
		_fadeOverlay = new ColorRect();
		_fadeOverlay.Color = new Color(0, 0, 0, 1); // Start fully black
		_fadeOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		
		// Add to viewport
		var canvasLayer = new CanvasLayer();
		canvasLayer.Layer = 100;
		GetTree().Root.AddChild(canvasLayer);
		canvasLayer.AddChild(_fadeOverlay);
		
		_isFadingIn = true;
		_fadeInProgress = 0f;
		
		GD.Print("GameSetup: Starting fade-in effect...");
	}
	
	public override void _Process(double delta)
	{
		if (!_isFadingIn || _fadeOverlay == null) return;
		
		_fadeInProgress += (float)delta / FADE_IN_DURATION;
		
		if (_fadeInProgress >= 1.0f)
		{
			// Fade-in complete, remove overlay
			_isFadingIn = false;
			if (_fadeOverlay != null && _fadeOverlay.GetParent() != null)
			{
				var canvasLayer = _fadeOverlay.GetParent();
				canvasLayer.QueueFree(); // Remove CanvasLayer and overlay
			}
			_fadeOverlay = null;
			GD.Print("GameSetup: Fade-in complete");
		}
		else
		{
			// Update fade alpha (fade from black to transparent)
			var alpha = 1.0f - Mathf.Clamp(_fadeInProgress, 0f, 1f);
			_fadeOverlay.Color = new Color(0, 0, 0, alpha);
		}
	}
	
	private void SpawnAncientBook()
	{
		GD.Print($"GameSetup: SpawnAncientBook called for Level {CurrentLevel}");
		
		// Try multiple ways to find player
		CharacterBody3D player = null;
		
		// Method 1: Direct path
		player = GetTree().Root.GetNodeOrNull<CharacterBody3D>("Main/Player");
		
		// Method 2: Search by type if method 1 fails
		if (player == null)
		{
			GD.Print("GameSetup: Trying to find player by searching scene tree...");
			var players = GetTree().GetNodesInGroup("Player");
			if (players.Count > 0)
			{
				player = players[0] as CharacterBody3D;
				GD.Print("GameSetup: Found player in 'Player' group");
			}
		}
		
		// Method 3: Search all CharacterBody3D nodes
		if (player == null)
		{
			GD.Print("GameSetup: Searching all CharacterBody3D nodes...");
			foreach (Node node in GetTree().Root.GetChildren())
			{
				var foundPlayer = FindPlayerRecursive(node);
				if (foundPlayer != null)
				{
					player = foundPlayer;
					GD.Print($"GameSetup: Found player at path: {player.GetPath()}");
					break;
				}
			}
		}
		
		if (player == null)
		{
			GD.PrintErr("GameSetup: Player not found after all search methods!");
			return;
		}
		
		GD.Print($"GameSetup: Player found at: {player.GetPath()}");
		
		// Get player's inventory system
		var inventorySystem = player.GetNodeOrNull<InventorySystem>("InventorySystem");
		if (inventorySystem == null)
		{
			GD.PrintErr($"GameSetup: InventorySystem not found! Player children: {string.Join(", ", GetChildNames(player))}");
			return;
		}
		
		GD.Print("GameSetup: InventorySystem found, creating ancient book...");
		
		// Create ancient book item data
		var ancientBookData = new ItemData("ancient_book", "Ancient Book", 1, true, true); // usable = true, keyItem = true
		ancientBookData.Description = "An ancient mystical book - Contains secrets of alchemy";
		ancientBookData.UsableBehavior = new BookUsable("Ancient Book");
		
		// Add to inventory
		bool added = inventorySystem.AddItem(ancientBookData, 1);
		if (added)
		{
			GD.Print($"✓ GameSetup: Ancient Book successfully added to inventory for Level {CurrentLevel}");
		}
		else
		{
			GD.PrintErr("GameSetup: Failed to add Ancient Book - inventory might be full");
		}
	}
	
	private CharacterBody3D FindPlayerRecursive(Node node)
	{
		if (node is CharacterBody3D body && node.Name.ToString().ToLower().Contains("player"))
		{
			return body;
		}
		
		foreach (Node child in node.GetChildren())
		{
			var result = FindPlayerRecursive(child);
			if (result != null) return result;
		}
		
		return null;
	}
	
	private string[] GetChildNames(Node node)
	{
		var names = new System.Collections.Generic.List<string>();
		foreach (Node child in node.GetChildren())
		{
			names.Add(child.Name);
		}
		return names.ToArray();
	}
}
