using Godot;
using System;

public partial class PickableItem : StaticBody3D
{
	[Export] public string ItemId = "item_default";
	[Export] public string ItemName = "Item";
	[Export] public int MaxStackSize = 64;
	[Export] public bool ShowPrompt = true;
	
	protected Label3D _promptLabel; // Changed to protected agar bisa diakses child class
	private bool _playerNearby = false;
	protected ItemData _itemData; // Changed to protected agar bisa diakses child class

	public override void _Ready()
	{
		// Buat ItemData dari properties
		_itemData = new ItemData(ItemId, ItemName, MaxStackSize);
		
		// Store original scale
		_itemData.OriginalScale = Scale;
		
		// Setup area detection untuk show prompt
		var area = new Area3D();
		AddChild(area);
		
		var collisionShape = new CollisionShape3D();
		var shape = new SphereShape3D();
		shape.Radius = 2.0f;
		collisionShape.Shape = shape;
		area.AddChild(collisionShape);
		
		area.BodyEntered += OnBodyEntered;
		area.BodyExited += OnBodyExited;
		
		// Create prompt label (optional)
		if (ShowPrompt)
		{
			_promptLabel = new Label3D();
			_promptLabel.Text = "[E] " + ItemName;
			_promptLabel.Position = new Vector3(0, 0.8f, 0);
			_promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
			_promptLabel.FontSize = 32; // Lebih besar untuk isometric
			_promptLabel.Modulate = new Color(1, 1, 0, 0); // Start invisible, yellow color
			_promptLabel.OutlineSize = 12;
			_promptLabel.OutlineModulate = new Color(0, 0, 0, 1);
			AddChild(_promptLabel);
		}
	}

	public override void _Process(double delta)
	{
		// Update prompt visibility
		if (_promptLabel != null)
		{
			var targetAlpha = _playerNearby ? 1.0f : 0.0f;
			var currentAlpha = _promptLabel.Modulate.A;
			var newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, (float)delta * 5.0f);
			_promptLabel.Modulate = new Color(1, 1, 0, newAlpha); // Yellow color
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player)
		{
			_playerNearby = true;
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			_playerNearby = false;
		}
	}

	public void Pickup()
	{
		// Logic ketika item diambil - sekarang hanya hapus dari scene
		// Inventory management dilakukan oleh Player
		QueueFree();
	}
	
	public ItemData GetItemData()
	{
		return _itemData;
	}
}
