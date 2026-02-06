using Godot;
using System.Collections.Generic;

namespace Level5Sewer
{
	// Struct to store WeightItem spawn data
	public struct WeightItemSpawnData
	{
		public Vector3 Position;
		public Vector3 Scale;
		public WeightItem OriginalNode; // Keep reference to original node
		public Node TemplateNode; // Duplicate template for respawning if original is deleted
	}

	public partial class Main : Node
	{
		private Node3D _player;
		private Vector3 _initialPlayerPosition;
		
		// Store initial WeightItem data for respawning
		private List<WeightItemSpawnData> _initialWeightItems = new();
		
		public override void _Ready()
		{
			// Get player reference
			_player = GetNodeOrNull<Node3D>("Player");
			if (_player != null)
			{
				_initialPlayerPosition = _player.GlobalPosition;
			}
			
			// Store initial WeightItem data (stones) for respawn
			// Use CallDeferred to ensure all items have finished their _Ready() and added to group
			CallDeferred(MethodName.StoreWeightItems);
			
			// Connect to FailScreen respawn signal (deferred to ensure FailScreen is ready)
			CallDeferred(MethodName.ConnectFailScreen);
			
			if (TimerManager.Instance != null)
			{
<<<<<<< HEAD
				TimerManager.Instance.StartTimer(300f, "Sewer");
=======
				TimerManager.Instance.StartTimer(180f, "Sewer");
>>>>>>> b04d3e712924dfab1f226c58d1662cb56868cfbb
				GD.Print("⏱️ Level 5 timer started");
			}
		}
		
		private void ConnectFailScreen()
		{
			var failScreenNode = GetNodeOrNull<Control>("UI/FailScreen");
			if (failScreenNode != null)
			{
				failScreenNode.Connect("RespawnRequested", new Callable(this, MethodName.OnRespawnRequested));
				GD.Print("✅ Level 5 connected to FailScreen.RespawnRequested");
			}
			else
			{
				GD.PrintErr("❌ FailScreen not found in UI/FailScreen");
			}
		}
		
		private void OnRespawnRequested()
		{
			// Reset player position
			if (_player != null)
			{
				_player.GlobalPosition = _initialPlayerPosition;
				GD.Print("🔄 Player position reset");
			}
			
			// Reset all weight items to their initial state
			ResetWeightItems();
		}
		
		private void StoreWeightItems()
		{
			_initialWeightItems.Clear();
			
			// Find all WeightItem nodes directly in the scene tree
			FindWeightItemsRecursive(this);
			
			GD.Print($"✅ Stored {_initialWeightItems.Count} weight items for respawn");
		}
		
		private void FindWeightItemsRecursive(Node parent)
		{
			foreach (Node child in parent.GetChildren())
			{
				if (child is WeightItem weightItem)
				{
					// Create a duplicate as template (don't add to tree)
					var template = weightItem.Duplicate((int)Node.DuplicateFlags.Signals | (int)Node.DuplicateFlags.Scripts);
					
					var spawnData = new WeightItemSpawnData
					{
						Position = weightItem.GlobalPosition,
						Scale = weightItem.Scale,
						OriginalNode = weightItem, // Keep reference to original
						TemplateNode = template // Store duplicate template (not in tree)
					};
					
					_initialWeightItems.Add(spawnData);
					GD.Print($"📍 Stored WeightItem: {weightItem.Name} at {spawnData.Position}");
				}
				
				// Recursively search in children
				FindWeightItemsRecursive(child);
			}
		}
		
		private void ResetWeightItems()
		{
			// Only remove DroppedItem instances (items dropped from inventory)
			// Keep original WeightItem nodes intact
			var droppedItems = GetTree().GetNodesInGroup("dropped_items");
			int removedCount = 0;
			
			foreach (var node in droppedItems)
			{
				// Only delete DroppedItem, not WeightItem
				if (node is DroppedItem droppedItem && GodotObject.IsInstanceValid(droppedItem))
				{
					droppedItem.QueueFree();
					removedCount++;
				}
			}
			
			GD.Print($"🗑️ Removed {removedCount} DroppedItems");
			
			// Reset original WeightItems to initial positions
			RestoreWeightItems();
		}
		
		private void RestoreWeightItems()
		{
			// Restore original WeightItems to their initial positions
			int restoredCount = 0;
			int respawnedCount = 0;
			
			foreach (var data in _initialWeightItems)
			{
				// Try to restore original node if it still exists
				if (data.OriginalNode != null && GodotObject.IsInstanceValid(data.OriginalNode))
				{
					var weightItem = data.OriginalNode;
					
					// Reset position and scale
					weightItem.GlobalPosition = data.Position;
					weightItem.Scale = data.Scale;
					
					// Make sure it's visible
					weightItem.Visible = true;
					
					restoredCount++;
					GD.Print($"♻️ Restored original: {weightItem.Name} to {data.Position}");
				}
				// If original was deleted (e.g., picked up), duplicate from template
				else if (data.TemplateNode != null && GodotObject.IsInstanceValid(data.TemplateNode))
				{
					var weightItem = data.TemplateNode.Duplicate((int)Node.DuplicateFlags.Signals | (int)Node.DuplicateFlags.Scripts);
					
					// Find parent node (Sewer/Items)
					var itemsParent = GetNodeOrNull("Sewer/Items");
					if (itemsParent != null)
					{
						itemsParent.AddChild(weightItem);
					}
					else
					{
						AddChild(weightItem);
					}
					
					// Set position and scale
					if (weightItem is Node3D node3D)
					{
						node3D.GlobalPosition = data.Position;
						node3D.Scale = data.Scale;
					}
					
					respawnedCount++;
					GD.Print($"♻️ Respawned from template: {weightItem.Name} at {data.Position}");
				}
				else
				{
					GD.PrintErr($"⚠️ Cannot restore WeightItem: Both node and template are invalid");
				}
			}
			
			GD.Print($"✅ Restored {restoredCount} + Respawned {respawnedCount} weight items");
		}
	}
}
