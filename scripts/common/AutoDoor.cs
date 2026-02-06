using Godot;
using System;
using System.Collections.Generic;

public partial class AutoDoor : StaticBody3D
{
	[Export] public float InteractionDistance = 3.0f;
	[Export] public float DestroyDuration = 2.0f;
	[Export] public int ParticleCount = 200;
	[Export] public float NotificationDuration = 2.0f;
	[Export] public NodePath LockedDoorPath;
	[Export] public NodePath UnlockedDoorPath;
	
	private Player _player;
	private bool _playerNearby = false;
	private Label3D _notificationLabel;
	private float _notificationTimer = 0.0f;
	private bool _isDestroyed = false;
	private bool _isDestroying = false;
	private float _destroyProgress = 0.0f;
	private Label3D _promptLabel;
	private MeshInstance3D _meshInstance;
	private CollisionShape3D _collisionShape;
	private List<ParticleData> _particles = new List<ParticleData>();
	private Node3D _particleContainer;
	private GridMap _lockedDoor;
	private GridMap _unlockedDoor;
	
	private class ParticleData
	{
		public MeshInstance3D Mesh;
		public Vector3 Velocity;
		public Vector3 RotationVelocity;
		public float LifeTime;
	}
	
	public override void _Ready()
	{
		_meshInstance = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
		_collisionShape = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
		
		// Ambil referensi node berdasarkan path yang di-export
		_lockedDoor = GetNodeOrNull<GridMap>("/root/Main/Map/PuzzleDoorLocked");
		_unlockedDoor = GetNodeOrNull<GridMap>("/root/Main/Map/PuzzleDoorUnlocked");

		// Pastikan pintu unlocked sembunyi di awal
		if (_unlockedDoor != null) _unlockedDoor.Visible = false;
		
		// Setup interaction area
		SetupInteractionArea();
		
		GD.Print($"AutoDoor initialized - requires Teal Potion to destroy");
	}
	
	private void SetupInteractionArea()
	{
		var area = new Area3D();
		AddChild(area);
		
		var shape = new CollisionShape3D();
		var sphereShape = new SphereShape3D();
		sphereShape.Radius = InteractionDistance;
		shape.Shape = sphereShape;
		area.AddChild(shape);
		
		area.BodyEntered += OnBodyEntered;
		area.BodyExited += OnBodyExited;
		
		// Create prompt label
		_promptLabel = new Label3D();
		_promptLabel.Text = "[F] Use Teal Potion to destroy this door";
		_promptLabel.Position = new Vector3(0, 2.5f, 0);
		_promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_promptLabel.FontSize = 24;
		_promptLabel.Modulate = new Color(0, 1, 1, 0);
		_promptLabel.OutlineSize = 10;
		_promptLabel.OutlineModulate = Colors.Black;
		AddChild(_promptLabel);
		
		// Create notification label (for warnings)
		_notificationLabel = new Label3D();
		_notificationLabel.Text = "";
		_notificationLabel.Position = new Vector3(0, 3.5f, 0);
		_notificationLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_notificationLabel.FontSize = 20;
		_notificationLabel.Modulate = new Color(1, 0.5f, 0, 0); // Orange
		_notificationLabel.OutlineSize = 10;
		_notificationLabel.OutlineModulate = Colors.Black;
		AddChild(_notificationLabel);
	}
	
	private void OnBodyEntered(Node3D body)
	{
		if (body is Player player && !_isDestroyed)
		{
			_player = player;
			_playerNearby = true;
			GD.Print("Player near door");
		}
	}
	
	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			_player = null;
			_playerNearby = false;
			GD.Print("Player left door");
		}
	}
	
	public override void _Process(double delta)
	{
		// Update prompt visibility
		if (_promptLabel != null && !_isDestroyed)
		{
			var targetAlpha = _playerNearby ? 1.0f : 0.0f;
			var currentAlpha = _promptLabel.Modulate.A;
			var newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, (float)delta * 5.0f);
			_promptLabel.Modulate = new Color(0, 1, 1, newAlpha);
		}
		
		// Update notification timer
		if (_notificationTimer > 0.0f)
		{
			_notificationTimer -= (float)delta;
			if (_notificationTimer <= 0.0f && _notificationLabel != null)
			{
				_notificationLabel.Modulate = new Color(1, 0.5f, 0, 0);
			}
		}
		
		// Handle destruction animation
		if (_isDestroying)
		{
			_destroyProgress += (float)delta / DestroyDuration;
			_destroyProgress = Mathf.Clamp(_destroyProgress, 0.0f, 1.0f);
			
			// Update particles
			UpdateParticles(delta);
			
			if (_destroyProgress >= 1.0f && _particles.Count == 0)
			{
				CompleteDestruction();
			}
		}
	}
	
	private void UpdateParticles(double delta)
	{
		var gravity = new Vector3(0, -9.8f, 0);
		
		for (int i = _particles.Count - 1; i >= 0; i--)
		{
			var particle = _particles[i];
			particle.LifeTime += (float)delta;
			
			// Apply gravity
			particle.Velocity += gravity * (float)delta;
			
			// Update position
			particle.Mesh.Position += particle.Velocity * (float)delta;
			
			// Rotate particle
			particle.Mesh.RotateX(particle.RotationVelocity.X * (float)delta);
			particle.Mesh.RotateY(particle.RotationVelocity.Y * (float)delta);
			particle.Mesh.RotateZ(particle.RotationVelocity.Z * (float)delta);
			
			// Fade out over time
			var material = particle.Mesh.GetActiveMaterial(0) as StandardMaterial3D;
			if (material != null)
			{
				float alpha = 1.0f - (particle.LifeTime / DestroyDuration);
				alpha = Mathf.Max(0, alpha);
				var color = material.AlbedoColor;
				color.A = alpha;
				material.AlbedoColor = color;
			}
			
			// Remove if fallen too far or fully faded
			if (particle.Mesh.Position.Y < GlobalPosition.Y - 5.0f || particle.LifeTime > DestroyDuration)
			{
				particle.Mesh.QueueFree();
				_particles.RemoveAt(i);
			}
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		if (_playerNearby && !_isDestroyed && !_isDestroying && @event.IsActionPressed("use_item"))
		{
			if (IsHoldingTealPotion())
			{
				UseTealPotion();
				GetViewport().SetInputAsHandled();
			}
			else
			{
				ShowNotification("Use Teal Potion to destroy this door!");
				GetViewport().SetInputAsHandled();
			}
		}
	}
	
	private bool IsHoldingTealPotion()
	{
		if (_player == null || _player._inventory == null)
		{
			return false;
		}
		
		// Check if selected hotbar item is Teal Potion
		var selectedItem = _player._inventory.GetSelectedHotbarItem();
		if (selectedItem != null && selectedItem.Data.ItemId == "teal_potion")
		{
			return true;
		}
		
		return false;
	}
	
	private void ShowNotification(string message)
	{
		if (_notificationLabel != null)
		{
			_notificationLabel.Text = message;
			_notificationLabel.Modulate = new Color(1, 0.5f, 0, 1); // Orange, fully visible
			_notificationTimer = NotificationDuration;
			GD.Print($"⚠️ AutoDoor: {message}");
		}
	}
	
	private void UseTealPotion()
	{
		if (_player == null || _player._inventory == null)
		{
			return;
		}
		
		GD.Print("💧 Using Teal Potion on door...");
		
		// Remove teal potion from selected hotbar slot
		int selectedSlot = _player._inventory.GetSelectedHotbarSlot();
		_player._inventory.RemoveItem(selectedSlot, 1);
		GD.Print("✓ Teal Potion consumed from hotbar!");
		
		// Start destruction
		StartDestruction();
	}
	
	private void StartDestruction()
	{
		_isDestroying = true;
		_destroyProgress = 0.0f;
		
		// Hapus PuzzleDoorLocked
		if (_lockedDoor != null)
		{
			_lockedDoor.QueueFree();
			GD.Print("Locked Door removed (QueueFree)");
		}

		// Munculkan PuzzleDoorUnlocked
		if (_unlockedDoor != null)
		{
			_unlockedDoor.Visible = true;
			GD.Print("Unlocked Door is now visible");
		}
		
		// Hide prompt
		if (_promptLabel != null)
		{
			_promptLabel.Visible = false;
		}
		
		// Disable collision immediately
		if (_collisionShape != null)
		{
			_collisionShape.Disabled = true;
		}
		
		// Create particle container
		_particleContainer = new Node3D();
		GetParent().AddChild(_particleContainer);
		
		// Generate particles from door
		CreateParticles();
		
		// Hide original mesh
		if (_meshInstance != null)
		{
			_meshInstance.Visible = false;
		}
		
		GD.Print("🔥 Door shattering into particles!");
	}
	
	private void CreateParticles()
	{
		if (_meshInstance == null) return;
		
		var doorMesh = _meshInstance.Mesh as BoxMesh;
		if (doorMesh == null) return;
		
		var doorSize = doorMesh.Size;
		var doorMaterial = _meshInstance.GetActiveMaterial(0);
		var doorPosition = _meshInstance.GlobalPosition;
		var doorRotation = _meshInstance.GlobalRotation;
		
		var random = new Random();
		
		// Create small cube particles
		for (int i = 0; i < ParticleCount; i++)
		{
			// Random position within door bounds
			float x = (float)(random.NextDouble() - 0.5) * doorSize.X;
			float y = (float)(random.NextDouble() - 0.5) * doorSize.Y;
			float z = (float)(random.NextDouble() - 0.5) * doorSize.Z;
			
			var localPos = new Vector3(x, y, z);
			
			// Transform to world space with door rotation
			var rotBasis = Basis.FromEuler(doorRotation);
			var worldPos = doorPosition + rotBasis * localPos;
			
			// Create particle mesh
			var particleMesh = new MeshInstance3D();
			var cubeMesh = new BoxMesh();
			float particleSize = (float)(0.05 + random.NextDouble() * 0.1); // Random size 0.05-0.15
			cubeMesh.Size = new Vector3(particleSize, particleSize, particleSize);
			particleMesh.Mesh = cubeMesh;
			
			// Clone material
			if (doorMaterial != null)
			{
				var particleMaterial = doorMaterial.Duplicate() as StandardMaterial3D;
				if (particleMaterial != null)
				{
					particleMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
				}
				particleMesh.SetSurfaceOverrideMaterial(0, particleMaterial);
			}
			
			// Add to container first, then set position
			_particleContainer.AddChild(particleMesh);
			particleMesh.GlobalPosition = worldPos;
			
			// Random velocity (mainly downward with some spread)
			var velocity = new Vector3(
				(float)(random.NextDouble() - 0.5) * 2.0f, // X spread
				(float)(random.NextDouble() * 0.5f),        // Slight upward initially
				(float)(random.NextDouble() - 0.5) * 2.0f  // Z spread
			);
			
			// Random rotation velocity
			var rotVelocity = new Vector3(
				(float)(random.NextDouble() - 0.5) * 10.0f,
				(float)(random.NextDouble() - 0.5) * 10.0f,
				(float)(random.NextDouble() - 0.5) * 10.0f
			);
			
			_particles.Add(new ParticleData
			{
				Mesh = particleMesh,
				Velocity = velocity,
				RotationVelocity = rotVelocity,
				LifeTime = 0.0f
			});
		}
	}
	
	private void CompleteDestruction()
	{
		_isDestroyed = true;
		_isDestroying = false;
		
		// Clean up particle container
		if (_particleContainer != null)
		{
			_particleContainer.QueueFree();
		}
		
		GD.Print("✓ Door completely disintegrated! Path is now clear!");
	}
}
