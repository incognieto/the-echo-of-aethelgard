// File: scripts/items/BookItem.cs
using Godot;
using System;

// Book behavior untuk item
public class BookUsable : IUsableItem
{
    private string _bookTitle;
    private string _leftPageContent;
    private string _rightPageContent;

    public BookUsable(string title, string leftContent = "", string rightContent = "")
    {
        _bookTitle = title;
        _leftPageContent = leftContent;
        _rightPageContent = rightContent;
    }

    public void Use(Player player)
    {
        player.ShowBook(_bookTitle, _leftPageContent, _rightPageContent);
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
            bookData.UsableBehavior = new BookUsable(BookTitle);
            
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
}