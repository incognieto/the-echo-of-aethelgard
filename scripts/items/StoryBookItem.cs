using Godot;
using System;

// Enum untuk simbol buku dalam narasi
public enum BookSymbol
{
	Castle = 0,
	Knight = 1,
	Princess = 2,
	Dragon = 3,
	Sword = 4,
	Shield = 5,
	Horse = 6,
	Skulls = 7,
	Phoenix = 8
}

// Story book behavior - hanya bisa diambil, tidak bisa digunakan
public partial class StoryBookItem : PickableItem
{
	[Export] 
	public int SymbolValue { get; set; } = 0; // 0=Castle, 1=Knight, dst
	
	public BookSymbol Symbol => (BookSymbol)SymbolValue;
	
	// Light untuk efek menyala
	private OmniLight3D _glowLight;
	private MeshInstance3D _mesh;
	private float _time = 0.0f;

	public override void _Ready()
	{
		base._Ready();
		
		// Set item ID based on symbol
		ItemId = $"book_{Symbol.ToString().ToLower()}";
		ItemName = $"{Symbol} Book";
		
		// Create ItemData untuk inventory
		_itemData = new ItemData(ItemId, ItemName, 1, false); // Max stack 1, tidak usable
		_itemData.Description = $"A glowing book with {Symbol} symbol on the cover";
		
		// Add glow effect
		SetupGlowEffect();
		
		GD.Print($"StoryBookItem ready: {Symbol} ({SymbolValue})");
	}
	
	private void SetupGlowEffect()
	{
		// Add omni light untuk efek menyala
		_glowLight = new OmniLight3D();
		_glowLight.LightColor = GetSymbolColor();
		_glowLight.LightEnergy = 1.5f;
		_glowLight.OmniRange = 2.0f;
		_glowLight.Position = new Vector3(0, 0.5f, 0);
		AddChild(_glowLight);
		
		// Cari mesh untuk animasi
		_mesh = GetNodeOrNull<MeshInstance3D>("Mesh");
	}
	
	private Color GetSymbolColor()
	{
		// Warna berbeda untuk setiap simbol
		return Symbol switch
		{
			BookSymbol.Castle => new Color(0.7f, 0.7f, 0.7f), // Gray
			BookSymbol.Knight => new Color(0.8f, 0.8f, 1.0f), // Light blue
			BookSymbol.Princess => new Color(1.0f, 0.7f, 0.9f), // Pink
			BookSymbol.Dragon => new Color(1.0f, 0.3f, 0.2f), // Red
			BookSymbol.Sword => new Color(0.9f, 0.9f, 1.0f), // Silver
			BookSymbol.Shield => new Color(0.7f, 0.5f, 0.3f), // Bronze
			BookSymbol.Horse => new Color(0.6f, 0.4f, 0.2f), // Brown
			BookSymbol.Skulls => new Color(0.9f, 0.9f, 0.8f), // Bone white
			BookSymbol.Phoenix => new Color(1.0f, 0.6f, 0.1f), // Orange
			_ => Colors.White
		};
	}
	
	public override void _Process(double delta)
	{
		base._Process(delta);
		
		if (_glowLight != null)
		{
			// Pulsing glow effect
			_time += (float)delta;
			float intensity = 1.0f + 0.3f * Mathf.Sin(_time * 3.0f);
			_glowLight.LightEnergy = intensity;
		}
		
		// Slow rotation untuk visual menarik
		if (_mesh != null)
		{
			_mesh.RotateY((float)delta * 0.5f);
		}
	}
}
