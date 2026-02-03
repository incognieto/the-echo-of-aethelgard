using Godot;
using System;

// Mewarisi PickableItem supaya tetap bisa di-pickup player
public partial class WeightItem : PickableItem
{
    [Export] public float WeightValue = 10.0f; // Berat item dalam kg
    [Export] public float PickupDuration = 2.0f; // Durasi hold E untuk pickup (seconds)
    
    private bool _isBeingPickedUp = false;
    private float _pickupProgress = 0.0f;
    private Player _currentPlayer = null;
    private Control _circularProgressUI;
    private TextureProgressBar _circularProgress;
    private CanvasLayer _canvasLayer;

    public override void _Ready()
    {
        base._Ready();
        // Update nama item biar pemain tahu beratnya saat di-hover
        // Misal: "Rusty Iron (10kg)"
        ItemName = $"{ItemName} ({WeightValue}kg)";
        
        // Update ItemData internal
        if (_itemData != null)
        {
            _itemData.ItemName = ItemName;
            
            // IMPORTANT: Force OriginalScale to Vector3.One for weight items
            // Ini memastikan semua batu ukurannya sama saat di-drop dari inventory
            _itemData.OriginalScale = Vector3.One;
            GD.Print($"🔧 WeightItem OriginalScale forced to Vector3.One: {ItemName}");
        }
        
        // Setup progress bar 3D
        SetupCircularProgress();
    }
    
    private void SetupCircularProgress()
    {
        // Create CanvasLayer untuk screen-space UI
        _canvasLayer = new CanvasLayer();
        _canvasLayer.Layer = 100; // Di atas semua UI lain
        AddChild(_canvasLayer);
        
        // Create container di center screen
        _circularProgressUI = new Control();
        _circularProgressUI.SetAnchorsPreset(Control.LayoutPreset.Center);
        _circularProgressUI.Visible = false;
        _canvasLayer.AddChild(_circularProgressUI);
        
        // Create circular progress bar
        _circularProgress = new TextureProgressBar();
        _circularProgress.FillMode = (int)TextureProgressBar.FillModeEnum.Clockwise;
        _circularProgress.MinValue = 0;
        _circularProgress.MaxValue = 100;
        _circularProgress.Value = 0;
        _circularProgress.CustomMinimumSize = new Vector2(150, 150);
        _circularProgress.Position = new Vector2(-75, -75); // Center it
        
        // Create circular textures procedurally
        var circleUnder = CreateCircleTexture(75, new Color(0.2f, 0.2f, 0.2f, 0.5f)); // Dark background
        var circleProgress = CreateCircleTexture(75, new Color(0.3f, 0.8f, 0.3f, 0.9f)); // Green progress
        
        _circularProgress.TextureUnder = circleUnder;
        _circularProgress.TextureProgress = circleProgress;
        
        _circularProgressUI.AddChild(_circularProgress);
        
        // Add center label for percentage
        var percentLabel = new Label();
        percentLabel.Name = "PercentLabel";
        percentLabel.HorizontalAlignment = HorizontalAlignment.Center;
        percentLabel.VerticalAlignment = VerticalAlignment.Center;
        percentLabel.Position = new Vector2(0, 50);
        percentLabel.Size = new Vector2(150, 50);
        percentLabel.AddThemeFontSizeOverride("font_size", 24);
        percentLabel.AddThemeColorOverride("font_color", Colors.White);
        percentLabel.Text = "0%";
        _circularProgressUI.AddChild(percentLabel);
    }
    
    private Texture2D CreateCircleTexture(int radius, Color color)
    {
        int size = radius * 2;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius;
                float dy = y - radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (distance <= radius)
                {
                    // Smooth edge
                    float alpha = Mathf.Clamp(radius - distance + 1, 0, 1);
                    var pixelColor = new Color(color.R, color.G, color.B, color.A * alpha);
                    image.SetPixel(x, y, pixelColor);
                }
                else
                {
                    image.SetPixel(x, y, Colors.Transparent);
                }
            }
        }
        
        return ImageTexture.CreateFromImage(image);
    }
    
    private void SetProgressTexture()
    {
        // Not needed anymore - using CanvasLayer instead
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        
        // Handle pickup progress
        if (_isBeingPickedUp && _currentPlayer != null)
        {
            // Cek apakah player masih hold E
            if (Input.IsActionPressed("interact"))
            {
                _pickupProgress += (float)delta;
                float progressPercent = (_pickupProgress / PickupDuration) * 100.0f;
                
                // Update circular progress
                _circularProgress.Value = progressPercent;
                
                // Update percentage label
                var percentLabel = _circularProgressUI.GetNodeOrNull<Label>("PercentLabel");
                if (percentLabel != null)
                {
                    percentLabel.Text = $"{Mathf.FloorToInt(progressPercent)}%";
                }
                
                // Update prompt untuk show hold instruction
                if (_promptLabel != null)
                {
                    _promptLabel.Text = $"[Hold E] Lifting...";
                }
                
                // Jika progress selesai, pickup item
                if (_pickupProgress >= PickupDuration)
                {
                    CompletePickup();
                }
            }
            else
            {
                // Player release E, cancel pickup
                CancelPickup();
            }
        }
    }
    
    public void StartPickup(Player player)
    {
        _isBeingPickedUp = true;
        _currentPlayer = player;
        _pickupProgress = 0.0f;
        _circularProgress.Value = 0;
        _circularProgressUI.Visible = true;
        
        if (_promptLabel != null)
        {
            _promptLabel.Text = "[Hold E] Lifting...";
        }
        
        GD.Print($"Started picking up heavy item: {ItemName}");
    }
    
    private void CompletePickup()
    {
        if (_currentPlayer != null && _currentPlayer._inventory != null)
        {
            bool added = _currentPlayer._inventory.AddItem(_itemData, 1);
            if (added)
            {
                GD.Print($"✓ Successfully picked up heavy item: {ItemName}");
                Pickup(); // This will QueueFree
            }
            else
            {
                GD.Print("✗ Inventory full!");
                CancelPickup();
            }
        }
    }
    
    private void CancelPickup()
    {
        _isBeingPickedUp = false;
        _currentPlayer = null;
        _pickupProgress = 0.0f;
        _circularProgress.Value = 0;
        _circularProgressUI.Visible = false;
        
        if (_promptLabel != null)
        {
            _promptLabel.Text = $"[E] {ItemName}";
        }
        
        GD.Print("Pickup cancelled");
    }
    
    public bool IsBeingPickedUp()
    {
        return _isBeingPickedUp;
    }
}