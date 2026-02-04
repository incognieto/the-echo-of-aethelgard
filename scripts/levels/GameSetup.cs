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
		
		// Initialize cursor manager
		var cursorManager = new CursorManager();
		AddChild(cursorManager);
		
		// Start with fade-in effect
		StartFadeIn();
		
		// Spawn ancient book di inventory untuk level 2, 3, 4, dan 5
		// Level 1: ancient book adalah pickable item di dunia, bukan auto-spawn
		if (CurrentLevel >= 2 && CurrentLevel <= 5)
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
		
		// Set konten berbeda per level
	string levelTitle = "";
	string levelImage = "";
	string rightContent = "";
	
	switch (CurrentLevel)
	{
		case 1:
			levelTitle = "The First Vision: Ironfang's Sentence";
			levelImage = "res://assets/sprites/ancient_books/ancient_content_lvl-1.png";
			rightContent = "I saw you, Traveller. You stood before the gate, hands trembling as you turned the wheels. One wrong click, and the ceiling wept iron needles. To live, you must mirror the Glimpse. Look into the void, remember the order of the stars: The King, the Eye, the Star, the Sword, the Moon, the Eye, the Sun, and the King again. Do not let the sequence fade, or the spoiler of your death shall become your truth.";
			break;
		case 2:
			levelTitle = "The Chamber of Weighted Sins";
			levelImage = "res://assets/sprites/ancient_books/ancient_content_lvl-2.png";
			rightContent = "I watched the previous captain fall. He stepped on the center (III) and the floor vanished. The stones only hold those who follow the Path of the Pentagram. Start from the End (V), leap to the Beginning (I), then follow the rhythm: Back to Two, Forward to Three, and Finish at Four. Step lightly, for the stones remember the weight of those who stumble.";
			break;
		case 3:
			levelTitle = "The Synthesis of the Teal Soul";
			levelImage = "res://assets/sprites/ancient_books/ancient_content_lvl-3.png";
			rightContent = "Pure elements are violent. Do not force the Moss, the Powder, and the Fruit into one vessel, or the lab shall be your tomb. To create the Teal Dissolver, one must first birth the Three Children of Color. Let the Sun (Yellow) rise from Moss and Dust. Let the Heart (Magenta) beat from Dust and Fruit. Let the Sea (Cyan) flow from Fruit and Moss. Only when these three unite, will the iron gate melt before you.";
			break;
		case 4:
			levelTitle = "The Warden's Forbidden Records: Volume IV";
			levelImage = "res://assets/sprites/ancient_books/ancient_content_lvl-4.png";
			rightContent = "From the towering walls of the ancient [b]Castle[/b], a brave [b]Knight[/b] prepared for his greatest quest. The realm's beloved [b]Princess[/b] had been captured by a fearsome [b]Dragon[/b], casting a shadow of doom over the land. With his trusted [b]Sword[/b] at his side and a sturdy [b]Shield[/b] for protection, the knight mounted his swift [b]Horse[/b] and rode forth. His journey led him through treacherous paths marked by the [b]Skulls[/b] of fallen warriors who had failed before them. Driven by courage, he faced the beast, and from the ashes of the fierce battle, hope for the kingdom rose again like an immortal [b]Phoenix[/b].";
			break;
		case 5:
			levelTitle = "The Toll of Freedom";
			levelImage = "res://assets/sprites/ancient_books/ancient_content_lvl-5.png";
			rightContent = "The gate of the sewers does not open for the weak, nor for the heavy-hearted. It demands a Perfect Burden. Many have piled stones until the chains snapped, buried under their own greed. The spoiler is simple: The lock will only turn when the scales feel the weight of Three Chosen Sins. Find the stones of The Giant (40), The Guard (20), and The Youth (15). No more, no less. Seventy-five is the price of the world outside.";
			break;
		default:
			levelTitle = "Empty Page";
			levelImage = "";
			rightContent = "(Empty page)";
			break;
	}
	
	// Set book behavior dengan konten per level
	ancientBookData.UsableBehavior = new BookUsable("Ancient Book", levelTitle, levelImage, rightContent);
		// Add to inventory
		bool added = inventorySystem.AddItem(ancientBookData, 1);
		if (added)
		{
			GD.Print($"✓ GameSetup: Ancient Book successfully added to inventory for Level {CurrentLevel}");
			GD.Print($"   Title: \"{levelTitle}\", Image: \"{levelImage}\"");
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
