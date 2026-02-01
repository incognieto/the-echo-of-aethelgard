using Godot;
using System;

// Mewarisi PickableItem supaya tetap bisa di-pickup player
public partial class WeightItem : PickableItem
{
    [Export] public float WeightValue = 10.0f; // Berat item dalam kg

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
        }
    }
}