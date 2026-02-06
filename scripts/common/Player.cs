using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Movement")]
	[Export] public float Speed = 6.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float RotationSpeed = 10.0f; // Kecepatan putar karakter
	
	[ExportGroup("Interaction")]
	[Export] public float MouseSensitivity = 0.002f;
	[Export] public float ThrowForce = 10.0f;
	[Export] public PackedScene DroppedItemScene;

	// Node References
	private Node3D _head;
	private Camera3D _camera;
	private Node3D _visualNode;
	public InventorySystem _inventory; 
	private InventoryUI _inventoryUI;
	private BookUI _bookUI;
	
	// Node References
private AnimationPlayer _animPlayer; // Tambahkan ini
private bool _isCarrying = false;    // Status apakah sedang memegang item berat
	
	// Rotation Logic
	private float _lastTargetAngle = 0f;

	// Camera mode variables
	private enum CameraMode { FirstPerson, Isometric }
	private CameraMode _currentCameraMode = CameraMode.FirstPerson;
	private Vector3 _isometricOffset = new Vector3(0, 10, 5); 
	private float _isometricRotation = 45f; 
	private Area3D _pickupArea; 
	private PickableItem _nearestItem = null; 
	private DroppedItem _nearestDroppedItem = null; 

	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = _head.GetNode<Camera3D>("Camera3D");
		_visualNode = GetNode<Node3D>("Player"); 
		
		_inventory = GetNode<InventorySystem>("InventorySystem");
		_animPlayer = GetNode<AnimationPlayer>("Player/PlayerAnimated/AnimationPlayer");
		
		CallDeferred(nameof(ConnectInventoryUI));
		SetupPickupArea();
		
		Rotation = Vector3.Zero;
		_head.Rotation = Vector3.Zero;
		_camera.Projection = Camera3D.ProjectionType.Perspective;
		_camera.Fov = 50.0f; 

		_currentCameraMode = CameraMode.Isometric;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
	
	private void UpdateAnimations(Vector3 direction)
{
	if (_animPlayer == null) return;

	string animToPlay = "";

	// Logika Prioritas Animasi
	if (direction == Vector3.Zero)
	{
		// Jika diam
		animToPlay = _isCarrying ? "Carrying (2)_3" : "Idle (2)_1";
	}
	else
	{
		// Jika bergerak
		animToPlay = _isCarrying ? "Walking (4)_4" : "Walking (6)_2";
	}

	// Hanya ganti jika animasi yang diminta berbeda dari yang sedang jalan
	if (_animPlayer.CurrentAnimation != animToPlay)
	{
		_animPlayer.Play(animToPlay, 0.2f); // 0.2f untuk transisi halus (crossfade)
	}
}
	
	private void ConnectInventoryUI()
	{
		_inventoryUI = GetNodeOrNull<InventoryUI>("/root/Main/UI/InventoryUI") ?? GetTree().Root.GetNodeOrNull<InventoryUI>("Main/UI/InventoryUI");
		_bookUI = GetNodeOrNull<BookUI>("/root/Main/UI/BookUI") ?? GetTree().Root.GetNodeOrNull<BookUI>("Main/UI/BookUI");
		
		if (_inventoryUI != null && _inventory != null) _inventoryUI.SetInventory(_inventory);
		if (_inventoryUI != null) GD.Print("✓ Player: InventoryUI connected successfully!");
		if (_bookUI != null) GD.Print("✓ Player: BookUI connected successfully!");
	}
	
	private void SetupPickupArea()
	{
		_pickupArea = new Area3D();
		AddChild(_pickupArea);
		var shape = new CollisionShape3D();
		shape.Shape = new SphereShape3D { Radius = 2.0f };
		_pickupArea.AddChild(shape);
		
		_pickupArea.BodyEntered += (body) => {
			if (_currentCameraMode == CameraMode.Isometric) {
				if (body is PickableItem p) _nearestItem = p;
				else if (body is DroppedItem d) _nearestDroppedItem = d;
			}
		};
		_pickupArea.BodyExited += (body) => {
			if (body == _nearestItem) _nearestItem = null;
			if (body == _nearestDroppedItem) _nearestDroppedItem = null;
		};
	}

	public override void _Input(InputEvent @event)
	{
		bool anyPanelOpen = InventoryUI.IsAnyPanelOpen;
		
		if (@event is InputEventMouseMotion motionEvent && !anyPanelOpen)
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured && _currentCameraMode == CameraMode.FirstPerson)
			{
				RotateY(-motionEvent.Relative.X * MouseSensitivity);
				_head.RotateX(-motionEvent.Relative.Y * MouseSensitivity);
				Vector3 headRot = _head.Rotation;
				headRot.X = Mathf.Clamp(headRot.X, -Mathf.Pi / 2, Mathf.Pi / 2);
				_head.Rotation = headRot;
			}
		}
		
		// Jangan block ui_cancel, biarkan PauseMenu yang handle
		
		// Block semua input jika game paused
		if (GetTree().Paused) return;
		
		if (@event.IsActionPressed("interact")) TryPickupItem();
		if (@event.IsActionPressed("drop_item")) DropItem(false);
		if (@event.IsActionPressed("drop_stack")) DropItem(true);
		if (@event.IsActionPressed("toggle_inventory")) ToggleInventory();
		if (@event.IsActionPressed("use_item")) UseSelectedItem();
		if (@event.IsActionPressed("toggle_camera")) ToggleCameraMode();
		
		for (int i = 1; i <= 6; i++)
			if (@event.IsActionPressed($"hotbar_{i}")) _inventory.SelectHotbarSlot(i - 1);
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;
		if (!IsOnFloor()) velocity += GetGravity() * (float)delta;

		// Jika game paused atau ada panel terbuka, stop movement
		if (GetTree().Paused || InventoryUI.IsAnyPanelOpen)
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
			Velocity = velocity;
			MoveAndSlide();
			return;
		}

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		
		Vector3 cameraForward = -_camera.GlobalTransform.Basis.Z;
		Vector3 cameraRight = _camera.GlobalTransform.Basis.X;
		cameraForward.Y = 0; cameraRight.Y = 0;
		cameraForward = cameraForward.Normalized();
		cameraRight = cameraRight.Normalized();

		Vector3 direction = (cameraForward * -inputDir.Y + cameraRight * inputDir.X).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;

			// FIX: Selama ada input, target angle LANGSUNG mengikuti direction (tidak kaku/maksa)
			_lastTargetAngle = Mathf.Atan2(-direction.X, -direction.Z);
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
			// Pas dilepas, _lastTargetAngle TIDAK berubah, jadi rotasi bakal nyelesain ke posisi terakhir
		}

		// Rotasi Visual: Selalu berjalan (Interpolasi)
		if (_visualNode != null)
		{
			float currentAngle = _visualNode.Rotation.Y;
			// LerpAngle memastikan transisi halus pas ganti input maupun pas berhenti
			float newAngle = (float)Mathf.LerpAngle(currentAngle, _lastTargetAngle, delta * RotationSpeed);
			_visualNode.Rotation = new Vector3(0, newAngle, 0);
		}
		
		UpdateAnimations(direction);

		Velocity = velocity;
		MoveAndSlide();
		RenderingServer.GlobalShaderParameterSet("player_pos", GlobalPosition);
	}
	
	private void TryPickupItem()
	{
		if (_currentCameraMode == CameraMode.Isometric)
		{
			// Find closest item from all items in pickup area
			PickableItem closestPickableItem = null;
			DroppedItem closestDroppedItem = null;
			float closestPickableDistance = float.MaxValue;
			float closestDroppedDistance = float.MaxValue;

			// Scan all items in the pickable_items group
			var allItems = GetTree().GetNodesInGroup("pickable_items");
			foreach (var item in allItems)
			{
				if (item is Node3D itemNode)
				{
					float distance = GlobalPosition.DistanceTo(itemNode.GlobalPosition);
					
					// Only consider items within pickup range
					if (distance <= 3.5f)
					{
						if (item is PickableItem pickable && distance < closestPickableDistance)
						{
							closestPickableItem = pickable;
							closestPickableDistance = distance;
						}
						else if (item is DroppedItem dropped && distance < closestDroppedDistance)
						{
							closestDroppedItem = dropped;
							closestDroppedDistance = distance;
						}
					}
				}
			}

			// Prioritize heavy dropped items if they're closer
			if (closestDroppedItem != null && closestDroppedItem.IsHeavyItem() && closestDroppedDistance < closestPickableDistance)
			{
				closestDroppedItem.StartPickup(this);
				_isCarrying = true;
				return;
			}

			// Otherwise pickup closest pickable item
			if (closestPickableItem != null)
			{
				if (closestPickableItem is WeightItem weightItem)
				{
					if (!weightItem.IsBeingPickedUp()) weightItem.StartPickup(this);
					return;
				}
				if (closestPickableItem.GetType().Name == "MaterialItem")
				{
					((MaterialItem)closestPickableItem).PickupWithQuantity(this);
					return;
				}
				if (_inventory.AddItem(closestPickableItem.GetItemData(), 1))
				{
					closestPickableItem.Pickup();
				}
			}
		}
		else // FPP Raycast
		{
			var space = GetWorld3D().DirectSpaceState;
			var from = _camera.GlobalTransform.Origin;
			var to = from - _camera.GlobalTransform.Basis.Z * 3.0f;
			var res = space.IntersectRay(PhysicsRayQueryParameters3D.Create(from, to));
			if (res.Count > 0)
			{
				var col = res["collider"].As<Node>();
				if (col is DroppedItem d && d.IsHeavyItem()) { d.StartPickup(this); return; }
				if (col is PickableItem p)
				{
					if (p is BookItem b && b.ItemId == "recipe_poster") return;
					if (p is WeightItem w) { if (!w.IsBeingPickedUp()) w.StartPickup(this); return; }
					if (p.GetType().Name == "MaterialItem") { ((MaterialItem)p).PickupWithQuantity(this); return; }
					if (_inventory.AddItem(p.GetItemData(), 1)) p.Pickup();
				}
			}
		}
	}
	
	private void DropItem(bool dropAll = false)
	{
		if (DroppedItemScene == null) return;
		InventoryItem sel = _inventory.GetSelectedHotbarItem();
		if (sel == null || sel.Data.IsKeyItem) return;
		int qty = dropAll ? sel.Quantity : 1;
		InventoryItem dropped = _inventory.DropSelectedItem(qty);
		if (dropped != null)
		{
			var inst = DroppedItemScene.Instantiate<DroppedItem>();
			GetTree().Root.AddChild(inst);
			
			// Hitung ketinggian drop berdasarkan ukuran item
			float dropHeight = 3.0f; // Default
			if (dropped.Data.ItemId.Contains("stone"))
			{
				// Untuk batu, semakin besar semakin tinggi drop point-nya
				dropHeight = 3.0f + (dropped.Data.VisualScale * 1.5f);
			}
			
			inst.GlobalPosition = GlobalPosition + _camera.GlobalTransform.Basis.Z * -3.0f + Vector3.Up * dropHeight;
			bool isWeight = dropped.Data.ItemId.Contains("stone") || dropped.Data.ItemId.Contains("weight");
			if (isWeight) {
				_isCarrying = false; // MATIKAN CARRYING
				inst.Initialize(dropped.Data, dropped.Quantity);
			}
			else { inst.Initialize(dropped.Data, dropped.Quantity, dropped.Data.OriginalScale); }
			inst.Throw(-_camera.GlobalTransform.Basis.Z * ThrowForce);
		}
	}
	
	private void ToggleInventory()
	{
		if (_inventoryUI != null)
		{
			_inventoryUI.Toggle();
			Input.MouseMode = _inventoryUI.IsInventoryVisible() ? Input.MouseModeEnum.Visible : (InventoryUI.IsAnyPanelOpen ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured);
		}
	}
	
	public InventorySystem GetInventory() => _inventory;
	
	private void ToggleCameraMode()
	{
		if (_currentCameraMode == CameraMode.FirstPerson) { _currentCameraMode = CameraMode.Isometric; SetIsometricCamera(); }
		else { _currentCameraMode = CameraMode.FirstPerson; SetFirstPersonCamera(); }
		//if (_inventoryUI != null) _inventoryUI.SetCrosshairVisible(_currentCameraMode == CameraMode.FirstPerson);
	}
	
	private void SetFirstPersonCamera() { _camera.Position = Vector3.Zero; _camera.Rotation = Vector3.Zero; Input.MouseMode = Input.MouseModeEnum.Captured; }
	private void SetIsometricCamera() { Input.MouseMode = Input.MouseModeEnum.Visible; }
	
	public override void _Process(double delta)
	{
		if (_currentCameraMode == CameraMode.Isometric)
		{
			_camera.GlobalPosition = GlobalPosition + _isometricOffset;
			_camera.LookAt(GlobalPosition, Vector3.Up);
		}
	}
	
	private void UseSelectedItem()
	{
		if (InventoryUI.IsAnyPanelOpen) return;
		InventoryItem sel = _inventory.GetSelectedHotbarItem();
		if (sel != null && sel.Data.IsUsable) sel.Data.UsableBehavior?.Use(this);
	}
	
	// Method untuk BookItem memanggil UI
	public void ShowBook(string title, string leftTitle, string leftImage, string rightContent)
	{
		if (_bookUI != null)
		{
			_bookUI.ShowBook(title, leftTitle, leftImage, rightContent);
		}
		else
		{
			GD.PrintErr("BookUI is not available!");
		}
	}
	
	// Overload untuk backward compatibility (buku biasa tanpa gambar)
	public void ShowBook(string title, string leftContent, string rightContent)
	{
		if (_bookUI != null)
		{
			_bookUI.ShowBook(title, leftContent, "", rightContent);
		}
		else
		{
			GD.PrintErr("BookUI is not available!");
		}
	}
	
	public void ShowPoster(string title, string imagePath)
	{
		if (_bookUI != null)
		{
			_bookUI.ShowPoster(title, imagePath);
		}
		else
		{
			GD.PrintErr("BookUI is not available!");
		}
	}
}
