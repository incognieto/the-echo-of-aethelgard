using Godot;
using System;

/// <summary>
/// Helper script untuk auto-start timer dan lives di level
/// Attach script ini ke root node level (Main) untuk auto-start sistem
/// </summary>
public partial class LevelTimerStarter : Node
{
	[Export] public float LevelTimeLimit = 300f; // 5 minutes default
	[Export] public string LevelName = "Level";
	
	public override void _Ready()
	{
		// Delay sedikit untuk memastikan semua systems ready
		CallDeferred(MethodName.StartSystems);
	}
	
	private void StartSystems()
	{
		// Start Timer
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.StartTimer(LevelTimeLimit, LevelName);
			GD.Print($"⏱️ Timer started: {LevelTimeLimit}s for {LevelName}");
		}
		else
		{
			GD.PrintErr("❌ TimerManager not found in autoload!");
		}
		
		// Initialize Lives (should already be initialized, but ensure it's reset)
		if (LivesManager.Instance != null)
		{
			// Don't reset lives between levels - only at game start
			GD.Print($"💖 Lives Manager ready: {LivesManager.Instance.CurrentLives}/{LivesManager.Instance.MaxLives}");
		}
		else
		{
			GD.PrintErr("❌ LivesManager not found in autoload!");
		}
	}
}
