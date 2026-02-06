using Godot;
using System;

/// <summary>
/// Singleton manager untuk sistem nyawa pemain
/// Manages player lives across different levels
/// </summary>
public partial class LivesManager : Node
{
	public static LivesManager Instance { get; private set; }
	
	[Export] public int MaxLives = 3;
	
	private int _currentLives;
	
	// Events
	[Signal] public delegate void LivesChangedEventHandler(int newLives);
	[Signal] public delegate void LivesDepletedEventHandler();
	
	public int CurrentLives 
	{ 
		get => _currentLives;
		private set
		{
			_currentLives = value;
			EmitSignal(SignalName.LivesChanged, _currentLives);
			
			if (_currentLives <= 0)
			{
				EmitSignal(SignalName.LivesDepleted);
			}
		}
	}
	
	public override void _Ready()
	{
		// Singleton pattern
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		
		Instance = this;
		ProcessMode = ProcessModeEnum.Always; // Always process, even when paused
		
		// Initialize lives
		ResetLives();
		
		GD.Print($"💖 LivesManager initialized with {MaxLives} lives");
	}
	
	/// <summary>
	/// Reset lives to maximum (called when starting new game)
	/// </summary>
	public void ResetLives()
	{
		CurrentLives = MaxLives;
		GD.Print($"💖 Lives reset to {MaxLives}");
	}
	
	/// <summary>
	/// Lose one life (called when timer runs out)
	/// </summary>
	public void LoseLife()
	{
		if (CurrentLives > 0)
		{
			CurrentLives--;
			GD.Print($"💔 Life lost! Remaining: {CurrentLives}/{MaxLives}");
		}
	}
	
	/// <summary>
	/// Add one life (for bonus/powerup if needed)
	/// </summary>
	public void GainLife()
	{
		if (CurrentLives < MaxLives)
		{
			CurrentLives++;
			GD.Print($"💚 Life gained! Current: {CurrentLives}/{MaxLives}");
		}
	}
	
	/// <summary>
	/// Check if player has any lives left
	/// </summary>
	public bool HasLivesRemaining()
	{
		return CurrentLives > 0;
	}
}
