using Godot;
using System;
using System.Collections.Generic;

public partial class AutoDoor : StaticBody3D
{
	[Export] public float InteractionDistance = 3.0f;
	[Export] public float DestroyDuration = 2.0f;
	[Export] public int ParticleCount = 200;
	
	private Player _player;
	private bool _playerNearby = false;
	private bool _isDestroyed = false;
	private bool _isDestroying = false;
	private float _destroyProgress = 0.0f;
	private Label3D _promptLabel;
	private MeshInstance3D _meshInstance;
	private CollisionShape3D _collisionShape;
	private List<ParticleData> _particles = new List<ParticleData>();
	private Node3D _particleContainer;
	
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
		_promptLabel.Text = "[E] Use Teal Potion on Door";
		_promptLabel.Position = new Vector3(0, 2.5f, 0);
		_promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_promptLabel.FontSize = 24;
		_promptLabel.Modulate = new Color(0, 1, 1, 0);
		_promptLabel.OutlineSize = 10;
		_promptLabel.OutlineModulate = Colors.Black;
		AddChild(_promptLabel);
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
			bool canInteract = _playerNearby && HasTealPotion();
			var targetAlpha = canInteract ? 1.0f : 0.0f;
			var currentAlpha = _promptLabel.Modulate.A;
			var newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, (float)delta * 5.0f);
			_promptLabel.Modulate = new Color(0, 1, 1, newAlpha);
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
		if (_playerNearby && !_isDestroyed && !_isDestroying && @event.IsActionPressed("interact"))
		{
			if (HasTealPotion())
			{
				UseTealPotion();
				GetViewport().SetInputAsHandled();
			}
		}
	}
	
	private bool HasTealPotion()
	{
		if (_player == null || _player._inventory == null)
		{
			return false;
		}
		
		return _player._inventory.HasItem("teal_potion");
	}
	
	private void UseTealPotion()
	{
		if (_player == null || _player._inventory == null)
		{
			return;
		}
		
		GD.Print("💧 Using Teal Potion on door...");
		
		// Remove teal potion from inventory
		var items = _player._inventory.GetAllItems();
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i] != null && items[i].Data.ItemId == "teal_potion")
			{
				_player._inventory.RemoveItem(i, 1);
				GD.Print("✓ Teal Potion consumed!");
				break;
			}
		}
		
		// Start destruction
		StartDestruction();
	}
	
	private void StartDestruction()
	{
		_isDestroying = true;
		_destroyProgress = 0.0f;
		
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
