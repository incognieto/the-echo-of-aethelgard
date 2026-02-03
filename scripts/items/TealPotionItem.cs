using Godot;
using System;

// Teal Potion behavior - can be used to destroy AutoDoor
public class TealPotionUsable : IUsableItem
{
	public void Use(Player player)
	{
		// When used, it will be handled by AutoDoor's proximity check
		// This is just to mark it as usable item
		GD.Print("💧 Teal Potion ready to use - approach the rusty door and press F");
	}

	public string GetUseText()
	{
		return "Use on Door";
	}
}

// PickableItem untuk Teal Potion (jika diperlukan di scene)
public partial class TealPotionItem : PickableItem
{
	public override void _Ready()
	{
		base._Ready();
		
		// Create ItemData dengan usable flag
		var potionData = new ItemData(ItemId, ItemName, 16, true, false); // Max stack 16, usable = true
		potionData.Description = "Mystical teal potion - Can dissolve magical barriers";
		
		// Set potion behavior
		potionData.UsableBehavior = new TealPotionUsable();
		
		// Override the default item data
		_itemData = potionData;
		
		GD.Print($"TealPotionItem ready: {ItemName}");
	}
}
