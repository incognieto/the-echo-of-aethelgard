// File: scripts/items/BookItem.cs
using Godot;
using System;

// Book behavior untuk item
public class BookUsable : IUsableItem
{
	private string _bookTitle;
	private string _leftPageTitle;
	private string _leftPageImage;
	private string _rightPageContent;

	// Constructor baru untuk ancient book dengan gambar
	public BookUsable(string title, string leftTitle, string leftImage, string rightContent)
	{
		_bookTitle = title;
		_leftPageTitle = leftTitle;
		_leftPageImage = leftImage;
		_rightPageContent = rightContent;
	}

	// Constructor lama untuk kompatibilitas (buku biasa)
	public BookUsable(string title, string leftContent = "", string rightContent = "")
	{
		_bookTitle = title;
		_leftPageTitle = leftContent;
		_leftPageImage = "";
		_rightPageContent = rightContent;
	}

	public void Use(Player player)
	{
		player.ShowBook(_bookTitle, _leftPageTitle, _leftPageImage, _rightPageContent);
	}

	public string GetUseText()
	{
		return "Read";
	}
}

// PickableItem khusus untuk buku
public partial class BookItem : PickableItem
{
	[Export] public string BookTitle = "Mysterious Book";
	[Export(PropertyHint.MultilineText)] public string BookContent = "This book is empty...";
	[Export(PropertyHint.File, "*.png,*.jpg,*.jpeg")] public string PosterImagePath = "";
	[Export] public NodePath PosterContentPath = "PosterContent";

	private bool _isPoster = false;
	private Player _nearbyPlayer = null;
	private Area3D _interactionArea = null;
	private Sprite3D _posterSprite;

	public override void _Ready()
	{
		// Check if this is a poster (attached to wall, not pickable)
		_isPoster = ItemId == "recipe_poster";
		
		if (_isPoster)
		{
			// Poster behavior - setup custom interaction area
			_interactionArea = new Area3D();
			AddChild(_interactionArea);
			
			var collisionShape = new CollisionShape3D();
			var shape = new SphereShape3D();
			shape.Radius = 3.0f; // Larger radius for wall-mounted poster
			collisionShape.Shape = shape;
			_interactionArea.AddChild(collisionShape);
			
			_interactionArea.BodyEntered += OnPosterBodyEntered;
			_interactionArea.BodyExited += OnPosterBodyExited;
			
			// Create prompt label for poster
			_promptLabel = new Label3D();
			_promptLabel.Text = "[E] See Poster";
			_promptLabel.Position = new Vector3(1.2f, 1.2f, 1.8f); // Above poster, matching poster X and Z position
			_promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
			_promptLabel.FontSize = 32;
			_promptLabel.Modulate = new Color(1, 1, 0, 0); // Start invisible
			_promptLabel.OutlineSize = 12;
			_promptLabel.OutlineModulate = new Color(0, 0, 0, 1);
			AddChild(_promptLabel);
			
			// Get Sprite3D reference for poster visual (created in scene, not code)
			if (HasNode(PosterContentPath))
			{
				_posterSprite = GetNode<Sprite3D>(PosterContentPath);
				
				// Load and set texture
				if (!string.IsNullOrEmpty(PosterImagePath))
				{
					var texture = GD.Load<Texture2D>(PosterImagePath);
					if (texture != null)
					{
						_posterSprite.Texture = texture;
						
						// Auto-calculate pixel size to fit 1.6x2 meter QuadMesh (4:5 ratio)
						float quadWidth = 1.6f;
						float quadHeight = 2.0f;
						float texWidth = texture.GetWidth();
						float texHeight = texture.GetHeight();
						
						float scaleX = quadWidth / texWidth;
						float scaleY = quadHeight / texHeight;
						float scale = Mathf.Min(scaleX, scaleY);
						
						_posterSprite.PixelSize = scale;
						
						GD.Print($"Loaded poster texture: {PosterImagePath} | Size: {texWidth}x{texHeight} | PixelSize: {scale}");
					}
					else
					{
						GD.PrintErr($"Failed to load poster texture: {PosterImagePath}");
					}
				}
			}
			else
			{
				GD.PrintErr($"PosterContent Sprite3D not found at path: {PosterContentPath}");
			}
			
			// Create dummy ItemData to prevent null reference (poster is not pickable)
			_itemData = new ItemData(ItemId, ItemName, 1, false, false);
			_itemData.Description = "A poster on the wall - cannot be picked up";
			
			GD.Print($"Poster ready: {BookTitle}");
		}
		else
		{
			// Normal book behavior
			base._Ready();
			
			// Check if this is the ancient book (key item)
			bool isKeyItem = ItemId == "ancient_book";
			
			// Create ItemData dengan usable flag
			var bookData = new ItemData(ItemId, ItemName, 1, true, isKeyItem); // Max stack 1, usable = true, keyItem if ancient book
			bookData.Description = isKeyItem ? "An ancient mystical book - Contains secrets of alchemy" : "A book that can be read";
			
			// Set book behavior
			if (isKeyItem)
			{
				// Get current level for ancient book content
				int currentLevel = GetCurrentLevel();
				var (levelTitle, levelImage, rightContent) = GetAncientBookContent(currentLevel);
				bookData.UsableBehavior = new BookUsable("Ancient Book", levelTitle, levelImage, rightContent);
			}
			else
			{
				// Regular book
				bookData.UsableBehavior = new BookUsable(BookTitle);
			}
			
			// Override the default item data
			_itemData = bookData;
			
			GD.Print($"BookItem ready: {BookTitle} (Key Item: {isKeyItem})");
		}
	}

	private void OnPosterBodyEntered(Node3D body)
	{
		if (body is Player player)
		{
			_nearbyPlayer = player;
		}
	}

	private void OnPosterBodyExited(Node3D body)
	{
		if (body is Player player)
		{
			_nearbyPlayer = null;
		}
	}

	public override void _Process(double delta)
	{
		if (_isPoster)
		{
			// Update prompt visibility for poster
			if (_promptLabel != null)
			{
				var targetAlpha = _nearbyPlayer != null ? 1.0f : 0.0f;
				var currentAlpha = _promptLabel.Modulate.A;
				var newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, (float)delta * 5.0f);
				_promptLabel.Modulate = new Color(1, 1, 0, newAlpha);
			}

			// Check for E key press when player is nearby
			if (_nearbyPlayer != null && Input.IsActionJustPressed("interact"))
			{
				// Poster - show single page with image
				string imagePath = string.IsNullOrEmpty(PosterImagePath) ? "res://assets/sprites/ui/UI_GembokLayout.png" : PosterImagePath;
				_nearbyPlayer.ShowPoster(BookTitle, imagePath);
				GD.Print($"Player reading poster: {BookTitle} with image: {imagePath}");
			}
		}
		else
		{
			base._Process(delta);
		}
	}
	
	private int GetCurrentLevel()
	{
		// Try to find GameSetup node to get current level
		var gameSetup = GetTree().Root.GetNodeOrNull<GameSetup>("Main/GameSetup");
		if (gameSetup != null)
		{
			return gameSetup.CurrentLevel;
		}
		
		// Fallback: try to detect from scene name
		var currentScene = GetTree().CurrentScene;
		if (currentScene != null)
		{
			string sceneName = currentScene.Name.ToString().ToLower();
			if (sceneName.Contains("level_1") || sceneName.Contains("cell")) return 1;
			if (sceneName.Contains("level_2") || sceneName.Contains("bridge")) return 2;
			if (sceneName.Contains("level_3") || sceneName.Contains("lab")) return 3;
			if (sceneName.Contains("level_4") || sceneName.Contains("library")) return 4;
			if (sceneName.Contains("level_5") || sceneName.Contains("sewer")) return 5;
		}
		
		return 1; // Default to level 1
	}
	
	private (string title, string image, string narasi) GetAncientBookContent(int level)
	{
		switch (level)
		{
			case 1:
				return (
					"The First Vision: Ironfang's Sentence",
					"res://assets/sprites/ancient_books/ancient_content_lvl-1.png",
					"I saw you, Traveller. You stood before the gate, hands trembling as you turned the wheels. One wrong click, and the ceiling wept iron needles. To live, you must mirror the Glimpse. Look into the void, remember the order of the stars: The King, the Eye, the Star, the Sword, the Moon, the Eye, the Sun, and the King again. Do not let the sequence fade, or the spoiler of your death shall become your truth."
				);
			case 2:
				return (
					"The Chamber of Weighted Sins",
					"res://assets/sprites/ancient_books/ancient_content_lvl-2.png",
					"I watched the previous captain fall. He stepped on the center (III) and the floor vanished. The stones only hold those who follow the Path of the Pentagram. Start from the End (V), leap to the Beginning (I), then follow the rhythm: Back to Two, Forward to Three, and Finish at Four. Step lightly, for the stones remember the weight of those who stumble."
				);
			case 3:
				return (
					"The Synthesis of the Teal Soul",
					"res://assets/sprites/ancient_books/ancient_content_lvl-3.png",
					"Pure elements are violent. Do not force the Moss, the Powder, and the Fruit into one vessel, or the lab shall be your tomb. To create the Teal Dissolver, one must first birth the Three Children of Color. Let the Sun (Yellow) rise from Moss and Dust. Let the Heart (Magenta) beat from Dust and Fruit. Let the Sea (Cyan) flow from Fruit and Moss. Only when these three unite, will the iron gate melt before you."
				);
			case 4:
				return (
					"The Warden's Forbidden Records: Volume IV",
					"res://assets/sprites/ancient_books/ancient_content_lvl-4.png",
					"From the towering walls of the ancient [b]Castle[/b], a brave [b]Knight[/b] prepared for his greatest quest. The realm's beloved [b]Princess[/b] had been captured by a fearsome [b]Dragon[/b], casting a shadow of doom over the land. With his trusted [b]Sword[/b] at his side and a sturdy [b]Shield[/b] for protection, the knight mounted his swift [b]Horse[/b] and rode forth. His journey led him through treacherous paths marked by the [b]Skulls[/b] of fallen warriors who had failed before them. Driven by courage, he faced the beast, and from the ashes of the fierce battle, hope for the kingdom rose again like an immortal [b]Phoenix[/b]."
				);
			case 5:
				return (
					"The Toll of Freedom",
					"res://assets/sprites/ancient_books/ancient_content_lvl-5.png",
					"The gate of the sewers does not open for the weak, nor for the heavy-hearted. It demands a Perfect Burden. Many have piled stones until the chains snapped, buried under their own greed. The spoiler is simple: The lock will only turn when the scales feel the weight of Three Chosen Sins. Find the stones of The Giant (40), The Guard (20), and The Youth (15). No more, no less. Seventy-five is the price of the world outside."
				);
			default:
				return ("Empty Page", "", "(Empty page)");
		}
	}
}
