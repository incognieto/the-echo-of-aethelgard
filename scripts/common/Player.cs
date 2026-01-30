using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 5.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float MouseSensitivity = 0.002f;
	[Export] public float ThrowForce = 10.0f;
	[Export] public PackedScene DroppedItemScene;

	private Node3D _head;
	private Camera3D _camera;
	public InventorySystem _inventory; // Public agar bisa diakses dari MaterialItem
	private InventoryUI _inventoryUI;
	private BookUI _bookUI;
	
	// Camera mode variables
	private enum CameraMode { FirstPerson, Isometric }
	private CameraMode _currentCameraMode = CameraMode.FirstPerson;
	private Vector3 _isometricOffset = new Vector3(8, 10, 8); // Offset untuk isometric view
	private float _isometricAngle = -45f; // Sudut kamera isometric (derajat)
	private float _isometricRotation = 45f; // Rotasi horizontal kamera (derajat)
	private bool _isRotatingCamera = false; // Flag untuk drag rotation
	private float _isometricRotationSensitivity = 0.15f; // Sensitivity untuk rotasi kamera isometric (bisa di-adjust)
	private Vector2 _lastMousePosition = Vector2.Zero;
	private Area3D _pickupArea; // Area untuk detect items di isometric mode
	private PickableItem _nearestItem = null; // Item terdekat di isometric mode

	public override void _Ready()
	{
		// Dapatkan referensi ke node Head dan Camera
		_head = GetNode<Node3D>("Head");
		_camera = _head.GetNode<Camera3D>("Camera3D");
		
		// Dapatkan referensi ke Inventory System
		_inventory = GetNode<InventorySystem>("InventorySystem");
		
		// Connect inventory UI - gunakan CallDeferred untuk memastikan semua node ready
		CallDeferred(nameof(ConnectInventoryUI));
		
		// Setup pickup area untuk isometric mode
		SetupPickupArea();
		
		// Lock mouse cursor untuk FPS-style control
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
	
	private void ConnectInventoryUI()
	{
		// Try beberapa kemungkinan path
		_inventoryUI = GetNodeOrNull<InventoryUI>("/root/Main/UI/InventoryUI");
		
		if (_inventoryUI == null)
		{
			// Try relative path
			_inventoryUI = GetTree().Root.GetNodeOrNull<InventoryUI>("Main/UI/InventoryUI");
		}
		
		// Get BookUI reference
		_bookUI = GetNodeOrNull<BookUI>("/root/Main/UI/BookUI");
		if (_bookUI == null)
		{
			_bookUI = GetTree().Root.GetNodeOrNull<BookUI>("Main/UI/BookUI");
		}
		
		if (_inventoryUI != null && _inventory != null)
		{
			_inventoryUI.SetInventory(_inventory);
			GD.Print("✓ Player: InventoryUI connected successfully!");
		}
		else
		{
			GD.PrintErr($"✗ Player: Failed to connect UI - UI exists: {_inventoryUI != null}, Inventory exists: {_inventory != null}");
			
			// Debug: Print available nodes
			if (_inventoryUI == null)
			{
				GD.Print("Attempting to find InventoryUI in tree...");
				var main = GetTree().Root.GetNodeOrNull("Main");
				if (main != null)
				{
					GD.Print($"Main node found, children: {main.GetChildCount()}");
					foreach (Node child in main.GetChildren())
					{
						GD.Print($"  - {child.Name} ({child.GetType().Name})");
						if (child is CanvasLayer canvas)
						{
							GD.Print($"    CanvasLayer children: {canvas.GetChildCount()}");
							foreach (Node subchild in canvas.GetChildren())
							{
								GD.Print($"      - {subchild.Name} ({subchild.GetType().Name})");
							}
						}
					}
				}
			}
		}
		
		if (_bookUI != null)
		{
			GD.Print("✓ Player: BookUI connected successfully!");
		}
		else
		{
			GD.PrintErr("✗ Player: BookUI not found!");
		}
	}
	
	private void SetupPickupArea()
	{
		// Create Area3D untuk proximity detection di isometric mode
		_pickupArea = new Area3D();
		AddChild(_pickupArea);
		
		var shape = new CollisionShape3D();
		var sphereShape = new SphereShape3D();
		sphereShape.Radius = 2.0f; // 2 meter radius
		shape.Shape = sphereShape;
		_pickupArea.AddChild(shape);
		
		_pickupArea.BodyEntered += OnPickupAreaEntered;
		_pickupArea.BodyExited += OnPickupAreaExited;
		
		GD.Print("✓ Pickup area created for isometric mode");
	}
	
	private void OnPickupAreaEntered(Node3D body)
	{
		if (body is PickableItem pickable && _currentCameraMode == CameraMode.Isometric)
		{
			_nearestItem = pickable;
			GD.Print($"Item in range: {pickable.GetItemData().ItemName}");
		}
	}
	
	private void OnPickupAreaExited(Node3D body)
	{
		if (body is PickableItem pickable && _nearestItem == pickable)
		{
			_nearestItem = null;
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Check if inventory is open - disable camera rotation if true
		bool inventoryOpen = HasMeta("inventory_open") && (bool)GetMeta("inventory_open");
		
		// Handle mouse movement untuk rotasi kamera (hanya di FPP mode)
		if (@event is InputEventMouseMotion motionEvent)
		{
			// Skip camera rotation if inventory is open
			if (inventoryOpen)
				return;
			
			if (Input.MouseMode == Input.MouseModeEnum.Captured && 
			    _currentCameraMode == CameraMode.FirstPerson)
			{
				// FPP: Rotasi horizontal (Y-axis) pada player body
				RotateY(-motionEvent.Relative.X * MouseSensitivity);
				
				// Rotasi vertical (X-axis) pada head
				_head.RotateX(-motionEvent.Relative.Y * MouseSensitivity);
				
				// Batasi rotasi vertical agar tidak terlalu ke atas/bawah
				Vector3 headRotation = _head.Rotation;
				headRotation.X = Mathf.Clamp(headRotation.X, -Mathf.Pi / 2, Mathf.Pi / 2);
				_head.Rotation = headRotation;
			}
			else if (_currentCameraMode == CameraMode.Isometric)
			{
				// Isometric: Rotate camera dengan mouse movement langsung (no click needed)
				_isometricRotation -= motionEvent.Relative.X * _isometricRotationSensitivity;
			}
		}
		
		// Toggle mouse mode dengan ESC
		if (@event.IsActionPressed("ui_cancel"))
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
				Input.MouseMode = Input.MouseModeEnum.Visible;
			else
				Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		
		// Interaksi dengan objek menggunakan E
		if (@event.IsActionPressed("interact"))
		{
			TryPickupItem();
		}
		
		// Drop item dengan Q
		if (@event.IsActionPressed("drop_item"))
		{
			DropItem(false); // Drop 1 item
		}
		
		// Drop seluruh stack dengan Ctrl+Q
		if (@event.IsActionPressed("drop_stack"))
		{
			DropItem(true); // Drop semua
		}
		
		// Toggle inventory dengan Tab atau I
		if (@event.IsActionPressed("toggle_inventory"))
		{
			ToggleInventory();
		}
		
		// Hotbar selection (1-6)
		for (int i = 1; i <= 6; i++)
		{
			if (@event.IsActionPressed($"hotbar_{i}"))
			{
				_inventory.SelectHotbarSlot(i - 1);
			}
		}
		
		// Use item dengan F
		if (@event.IsActionPressed("use_item"))
		{
			UseSelectedItem();
		}
		
		// Toggle camera mode dengan C
		if (@event.IsActionPressed("toggle_camera"))
		{
			ToggleCameraMode();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add gravity
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get input direction (WASD)
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		
		Vector3 direction;
		if (_currentCameraMode == CameraMode.FirstPerson)
		{
			// FPP: Transform input direction berdasarkan rotasi player
			direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		}
		else
		{
			// Isometric: Transform input berdasarkan arah kamera
			float rad = Mathf.DegToRad(_isometricRotation);
			Vector3 forward = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
			Vector3 right = new Vector3(Mathf.Cos(rad), 0, -Mathf.Sin(rad));
			direction = (forward * inputDir.Y + right * inputDir.X).Normalized();
		}
		
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
	
	private void TryPickupItem()
	{
		if (_currentCameraMode == CameraMode.Isometric)
		{
			// Isometric: Gunakan proximity detection
			if (_nearestItem != null)
			{
				GD.Print($"Nearest item type: {_nearestItem.GetType().Name}");
				
				// Check if it's a MaterialItem (needs quantity picker)
				if (_nearestItem.GetType().Name == "MaterialItem")
				{
					var materialItem = _nearestItem as MaterialItem;
					if (materialItem != null)
					{
						GD.Print($"Trying to pickup material (quantity picker): {materialItem.ItemName}");
						materialItem.PickupWithQuantity(this);
						_nearestItem = null;
						return;
					}
				}
				
				// Regular item - pickup 1
				ItemData itemData = _nearestItem.GetItemData();
				GD.Print($"Trying to pickup regular item (proximity): {itemData.ItemName}");
				
				if (_inventory != null)
				{
					bool added = _inventory.AddItem(itemData, 1);
					if (added)
					{
						_nearestItem.Pickup();
						_nearestItem = null;
						GD.Print($"✓ Successfully picked up: {itemData.ItemName}");
						_inventory.PrintInventory();
					}
					else
					{
						GD.Print("✗ Inventory penuh!");
					}
				}
			}
			else
			{
				GD.Print("No item nearby in isometric mode");
			}
			return;
		}
		
		// FPP: Raycast dari kamera ke depan
		var spaceState = GetWorld3D().DirectSpaceState;
		var cameraTransform = _camera.GlobalTransform;
		var from = cameraTransform.Origin;
		var to = from - cameraTransform.Basis.Z * 3.0f; // 3 meter reach distance
		
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithBodies = true; // Pastikan bisa hit StaticBody3D
		query.CollideWithAreas = true;
		
		var result = spaceState.IntersectRay(query);
		
		if (result.Count > 0)
		{
			var collider = result["collider"].As<Node>();
			
			// Check jika objek adalah PickableItem
			if (collider is PickableItem pickable)
			{
				GD.Print($"FPP Mode - Item type: {pickable.GetType().Name}");
				
				// Check if it's a MaterialItem (needs quantity picker)
				if (pickable.GetType().Name == "MaterialItem")
				{
					var materialItem = pickable as MaterialItem;
					if (materialItem != null)
					{
						GD.Print($"FPP: Trying to pickup material (quantity picker): {materialItem.ItemName}");
						materialItem.PickupWithQuantity(this);
						return;
					}
				}
				
				// Regular item - pickup 1
				ItemData itemData = pickable.GetItemData();
				GD.Print($"FPP: Trying to pickup regular item: {itemData.ItemName}");
				
				if (_inventory != null)
				{
					bool added = _inventory.AddItem(itemData, 1);
					if (added)
					{
						pickable.Pickup();
						GD.Print($"✓ Successfully picked up: {itemData.ItemName}");
						_inventory.PrintInventory(); // Debug print inventory
					}
					else
					{
						GD.Print("✗ Inventory penuh!");
					}
				}
				else
				{
					GD.PrintErr("✗ Inventory system is NULL!");
				}
			}
			else
			{
				GD.Print($"Hit object: {collider.Name}, but it's not a PickableItem");
			}
		}
		else
		{
			GD.Print("No object in range");
		}
	}
	
	private void DropItem(bool dropAll = false)
	{
		if (DroppedItemScene == null)
		{
			GD.PrintErr("DroppedItemScene belum di-set!");
			return;
		}

		InventoryItem selectedItem = _inventory.GetSelectedHotbarItem();
		if (selectedItem == null) return;

		int quantityToDrop = dropAll ? selectedItem.Quantity : 1;
		InventoryItem droppedItem = _inventory.DropSelectedItem(quantityToDrop);
		
		if (droppedItem != null)
		{
			// Spawn dropped item di dunia
			var droppedInstance = DroppedItemScene.Instantiate<DroppedItem>();
			GetTree().Root.AddChild(droppedInstance);
			
			// Posisi di depan player
			Vector3 dropPosition = GlobalPosition + _camera.GlobalTransform.Basis.Z * -1.5f;
			dropPosition.Y = GlobalPosition.Y + 1.0f;
			droppedInstance.GlobalPosition = dropPosition;
			
			// Set item data
			droppedInstance.Initialize(droppedItem.Data, droppedItem.Quantity);
			
			// Throw item
			Vector3 throwDirection = -_camera.GlobalTransform.Basis.Z;
			droppedInstance.Throw(throwDirection * ThrowForce);
			
			GD.Print($"Dropped: {droppedItem.Data.ItemName} x{droppedItem.Quantity}");
		}
	}
	
	private void ToggleInventory()
	{
		if (_inventoryUI != null)
		{
			_inventoryUI.Toggle();
			
			// Toggle mouse mode
			if (_inventoryUI.IsInventoryVisible())
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
			else
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
		}
	}
	
	public InventorySystem GetInventory()
	{
		return _inventory;
	}
	
	private void ToggleCameraMode()
	{
		if (_currentCameraMode == CameraMode.FirstPerson)
		{
			// Switch to Isometric
			_currentCameraMode = CameraMode.Isometric;
			SetIsometricCamera();
			if (_inventoryUI != null) _inventoryUI.SetCrosshairVisible(false);
			GD.Print("Camera Mode: Isometric (Right-click drag to rotate)");
		}
		else
		{
			// Switch to First Person
			_currentCameraMode = CameraMode.FirstPerson;
			SetFirstPersonCamera();
			if (_inventoryUI != null) _inventoryUI.SetCrosshairVisible(true);
			GD.Print("Camera Mode: First Person");
		}
	}
	
	private void SetFirstPersonCamera()
	{
		// Reset camera to head position (FPP)
		_camera.Position = Vector3.Zero;
		_camera.Rotation = Vector3.Zero;
		
		// Enable mouse capture for FPP
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
	
	private void SetIsometricCamera()
	{
		// Calculate offset berdasarkan rotation
		float rad = Mathf.DegToRad(_isometricRotation);
		float distance = 10f;
		_isometricOffset = new Vector3(
			Mathf.Sin(rad) * distance,
			10,
			Mathf.Cos(rad) * distance
		);
		
		// Position camera untuk isometric view
		_camera.Position = _isometricOffset;
		
		// Rotate camera to look down at angle
		_camera.LookAt(Vector3.Zero, Vector3.Up);
		
		// Keep mouse captured untuk rotasi tidak terbatas
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
	
	public override void _Process(double delta)
	{
		// Update camera position jika dalam mode isometric
		if (_currentCameraMode == CameraMode.Isometric)
		{
			// Recalculate offset berdasarkan rotation
			float rad = Mathf.DegToRad(_isometricRotation);
			float distance = 10f;
			_isometricOffset = new Vector3(
				Mathf.Sin(rad) * distance,
				10,
				Mathf.Cos(rad) * distance
			);
			
			// Camera mengikuti player dengan offset
			_camera.GlobalPosition = GlobalPosition + _isometricOffset;
			_camera.LookAt(GlobalPosition, Vector3.Up);
		}
	}
	
	private void UseSelectedItem()
	{
		InventoryItem selectedItem = _inventory.GetSelectedHotbarItem();
		if (selectedItem == null)
		{
			GD.Print("No item selected");
			return;
		}
		
		if (!selectedItem.Data.IsUsable)
		{
			GD.Print($"{selectedItem.Data.ItemName} cannot be used");
			return;
		}
		
		if (selectedItem.Data.UsableBehavior != null)
		{
			GD.Print($"Using: {selectedItem.Data.ItemName}");
			selectedItem.Data.UsableBehavior.Use(this);
		}
		else
		{
			GD.PrintErr($"{selectedItem.Data.ItemName} is usable but has no behavior!");
		}
	}
	
	// Method untuk BookItem memanggil UI
	public void ShowBook(string title, string content)
	{
		if (_bookUI != null)
		{
			_bookUI.ShowBook(title, content);
		}
		else
		{
			GD.PrintErr("BookUI is not available!");
		}
	}
}
