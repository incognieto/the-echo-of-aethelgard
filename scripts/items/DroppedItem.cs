using Godot;
using System;

// Class untuk item yang di-drop ke dunia
public partial class DroppedItem : RigidBody3D
{
	private ItemData _itemData;
	private int _quantity = 1;
	private Label3D _label;
	private MeshInstance3D _mesh;
	private float _pickupCooldown = 0.5f;
	private float _pickupTimer = 0.0f;
	
	// Khusus untuk item berat seperti batu
	private bool _isHeavyItem = false; // Disable rolling, no auto-pickup
	private Area3D _pickupArea;
	private Vector3 _originalScale = Vector3.One; // Store original scale
	private Area3D _heavyPickupArea; // Larger area untuk heavy item manual pickup
	
	// Hold-to-pickup untuk heavy items
	private bool _isBeingPickedUp = false;
	private float _pickupProgress = 0.0f;
	private float _pickupDuration = 2.0f;
	private Player _currentPlayer = null;
	private bool _playerNearby = false;
	private Label3D _promptLabel;
	private Control _circularProgressUI;
	private TextureProgressBar _circularProgress;
	private CanvasLayer _canvasLayer;

	public override void _Ready()
	{
		// Create mesh for visual
		_mesh = new MeshInstance3D();
		var sphereMesh = new SphereMesh();
		sphereMesh.Radius = 0.25f;
		sphereMesh.Height = 0.5f;
		_mesh.Mesh = sphereMesh;
		AddChild(_mesh);
		
		// Create collision shape
		var collisionShape = new CollisionShape3D();
		var shape = new SphereShape3D();
		shape.Radius = 0.25f;
		collisionShape.Shape = shape;
		AddChild(collisionShape);
		
		// Create label untuk quantity
		_label = new Label3D();
		_label.Position = new Vector3(0, 0.5f, 0);
		_label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_label.FontSize = 20;
		AddChild(_label);
		
		// Physics properties
		GravityScale = 1.0f;
		Mass = 0.5f;
		
		// Auto-pickup area
		var area = new Area3D();
		AddChild(area);
		
		var areaShape = new CollisionShape3D();
		var areaSphere = new SphereShape3D();
		areaSphere.Radius = 1.5f;
		areaShape.Shape = areaSphere;
		area.AddChild(areaShape);
		
		area.BodyEntered += OnBodyEntered;
		area.BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		if (_pickupTimer > 0)
		{
			_pickupTimer -= (float)delta;
		}
		
		// Update prompt label for heavy items
		if (_isHeavyItem && _promptLabel != null)
		{
			if (_playerNearby && !_isBeingPickedUp)
			{
				_promptLabel.Visible = true;
				var alpha = Mathf.Abs(Mathf.Sin((float)Time.GetTicksMsec() / 500.0f));
				_promptLabel.Modulate = new Color(1, 1, 0, Mathf.Lerp(0.6f, 1.0f, alpha));
			}
			else
			{
				_promptLabel.Visible = false;
			}
		}
		
		// Handle pickup progress for heavy items
		if (_isBeingPickedUp && _currentPlayer != null)
		{
			if (Input.IsActionPressed("interact"))
			{
				_pickupProgress += (float)delta;
				float progressPercent = (_pickupProgress / _pickupDuration) * 100.0f;
				
				_circularProgress.Value = progressPercent;
				
				var percentLabel = _circularProgressUI.GetNodeOrNull<Label>("PercentLabel");
				if (percentLabel != null)
				{
					percentLabel.Text = $"{Mathf.FloorToInt(progressPercent)}%";
				}
				
				if (_pickupProgress >= _pickupDuration)
				{
					CompletePickup();
				}
			}
			else
			{
				CancelPickup();
			}
		}
	}

	public void Initialize(ItemData itemData, int quantity = 1, Vector3 scale = default)
	{
		_itemData = itemData;
		_quantity = quantity;
		
		// Cek apakah ini item berat (batu) berdasarkan ItemId
		_isHeavyItem = itemData.ItemId.Contains("stone") || itemData.ItemId.Contains("rock") || itemData.ItemId.Contains("weight");
		
		// Set scale - FORCE normalize untuk heavy items
		if (_isHeavyItem)
		{
			_originalScale = Vector3.One; // Always normalize heavy items
			Scale = Vector3.One;
			GD.Print($"🟢 Heavy item - forcing normalized scale: {itemData.ItemName}");
		}
		else
		{
			// Non-heavy items: use provided scale or default
			if (scale == default)
			{
				_originalScale = Vector3.One;
			}
			else
			{
				_originalScale = scale;
				Scale = scale;
			}
		}
		
		if (_label != null)
		{
			_label.Text = quantity > 1 ? $"{itemData.ItemName} x{quantity}" : itemData.ItemName;
		}
		
		// NO pickup cooldown untuk heavy items agar bisa langsung diambil lagi
		_pickupTimer = _isHeavyItem ? 0.0f : _pickupCooldown;
		
		// Set physics untuk heavy item (no rolling)
		if (_isHeavyItem)
		{
			LockRotation = true; // Disable rotation untuk mencegah gelinding
			LinearDamp = 5.0f; // High damping untuk stop cepat
			AngularDamp = 10.0f; // High angular damping
			Mass = 5.0f; // Lebih berat
			
			// Setup prompt label
			_promptLabel = new Label3D();
			_promptLabel.Text = $"[E] {itemData.ItemName}";
			_promptLabel.Position = new Vector3(0, 0.8f, 0);
			_promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
			_promptLabel.FontSize = 32;
			_promptLabel.Modulate = new Color(1, 1, 0, 0);
			_promptLabel.OutlineSize = 12;
			_promptLabel.OutlineModulate = Colors.Black;
			_promptLabel.Visible = false;
			AddChild(_promptLabel);
			
			// Setup larger detection area untuk heavy items agar lebih mudah di-pickup
			_heavyPickupArea = new Area3D();
			AddChild(_heavyPickupArea);
			
			var heavyShape = new CollisionShape3D();
			var heavySphere = new SphereShape3D();
			heavySphere.Radius = 3.0f; // Lebih besar dari default (1.5f)
			heavyShape.Shape = heavySphere;
			_heavyPickupArea.AddChild(heavyShape);
			
			_heavyPickupArea.BodyEntered += OnHeavyPickupEntered;
			_heavyPickupArea.BodyExited += OnHeavyPickupExited;
			
			// Setup circular progress UI immediately (not deferred)
			SetupCircularProgress();
			
			GD.Print($"✅ Heavy dropped item initialized: {itemData.ItemName} - Scale: {Scale}, IsHeavy: {_isHeavyItem}");
		}
	}

	public void Throw(Vector3 force)
	{
		ApplyCentralImpulse(force);
	}

	private void OnBodyEntered(Node3D body)
	{
		if (_pickupTimer > 0) return; // Cooldown belum habis
		
		// Skip auto-pickup untuk heavy items (harus manual dengan E)
		if (_isHeavyItem)
		{
			if (body is Player)
			{
				_playerNearby = true;
			}
			return;
		}
		
		if (body is Player player)
		{
			var inventory = player.GetInventory();
			if (inventory != null && inventory.AddItem(_itemData, _quantity))
			{
				GD.Print($"Auto-picked up: {_itemData.ItemName} x{_quantity}");
				QueueFree();
			}
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player && _isHeavyItem)
		{
			_playerNearby = false;
		}
	}
	
	private void OnHeavyPickupEntered(Node3D body)
	{
		if (body is Player && _isHeavyItem)
		{
			_playerNearby = true;
			GD.Print($"🎯 Player entered heavy item area: {_itemData.ItemName}");
		}
	}
	
	private void OnHeavyPickupExited(Node3D body)
	{
		if (body is Player && _isHeavyItem)
		{
			_playerNearby = false;
			GD.Print($"🚶 Player left heavy item area: {_itemData.ItemName}");
		}
	}
	
	public ItemData GetItemData()
	{
		return _itemData;
	}

	public int GetQuantity()
	{
		return _quantity;
	}
	
	public Vector3 GetOriginalScale()
	{
		return _originalScale;
	}
	
	public bool IsHeavyItem()
	{
		return _isHeavyItem;
	}
	
	public void StartPickup(Player player)
	{
		if (!_isHeavyItem)
		{
			GD.Print($"⚠️ Cannot start pickup: Not a heavy item");
			return;
		}
		
		// Null check untuk circular progress
		if (_circularProgress == null || _circularProgressUI == null)
		{
			GD.PrintErr("❌ Circular progress UI not initialized! Setting up now...");
			SetupCircularProgress();
			
			// Double check
			if (_circularProgress == null || _circularProgressUI == null)
			{
				GD.PrintErr("❌ Failed to setup circular progress!");
				return;
			}
		}
		
		_isBeingPickedUp = true;
		_currentPlayer = player;
		_pickupProgress = 0.0f;
		_circularProgress.Value = 0;
		_circularProgressUI.Visible = true;
		
		GD.Print($"👍 Started picking up dropped heavy item: {_itemData.ItemName}");
	}
	
	private void CompletePickup()
	{
		if (_currentPlayer != null)
		{
			var inventory = _currentPlayer.GetInventory();
			if (inventory != null && inventory.AddItem(_itemData, _quantity))
			{
				GD.Print($"✓ Successfully picked up dropped heavy item: {_itemData.ItemName}");
				QueueFree();
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
		
		GD.Print("Dropped item pickup cancelled");
	}
	
	private void SetupCircularProgress()
	{
		_canvasLayer = new CanvasLayer();
		_canvasLayer.Layer = 100;
		AddChild(_canvasLayer);
		
		_circularProgressUI = new Control();
		_circularProgressUI.SetAnchorsPreset(Control.LayoutPreset.Center);
		_circularProgressUI.Visible = false;
		_canvasLayer.AddChild(_circularProgressUI);
		
		_circularProgress = new TextureProgressBar();
		_circularProgress.FillMode = (int)TextureProgressBar.FillModeEnum.Clockwise;
		_circularProgress.MinValue = 0;
		_circularProgress.MaxValue = 100;
		_circularProgress.Value = 0;
		_circularProgress.CustomMinimumSize = new Vector2(150, 150);
		_circularProgress.Position = new Vector2(-75, -75);
		
		var circleUnder = CreateCircleTexture(75, new Color(0.2f, 0.2f, 0.2f, 0.5f));
		var circleProgress = CreateCircleTexture(75, new Color(0.3f, 0.8f, 0.3f, 0.9f));
		
		_circularProgress.TextureUnder = circleUnder;
		_circularProgress.TextureProgress = circleProgress;
		_circularProgressUI.AddChild(_circularProgress);
		
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
}
