using Godot;
using System;

// Softener item behavior - bisa digunakan untuk melunakkan material
public class SoftenerUsable : IUsableItem
{
	public void Use(Player player)
	{
		// This would be called when player uses the softener
		// For now, just show a message
		GD.Print("Softener used! (Will be implemented for next level mechanics)");
	}

	public string GetUseText()
	{
		return "Use Softener";
	}
}

// PickableItem khusus untuk Cairan Pelunak
public partial class SoftenerItem : PickableItem
{
	public override void _Ready()
	{
		// Set item data
		ItemId = "softener";
		ItemName = "Cairan Pelunak";
		
		base._Ready();
		
		// Create ItemData dengan usable flag
		var softenerData = new ItemData(ItemId, ItemName, 1, true); // Max stack 1, usable = true
		softenerData.Description = "Cairan kuat yang dapat melunakkan material keras. Digunakan untuk level berikutnya.";
		
		// Set softener behavior
		softenerData.UsableBehavior = new SoftenerUsable();
		
		// Override the default item data
		_itemData = softenerData;
		
		GD.Print($"SoftenerItem ready: {ItemName}");
	}
}
