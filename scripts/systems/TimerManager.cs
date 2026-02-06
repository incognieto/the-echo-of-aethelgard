using Godot;
using System;

/// <summary>
/// Singleton manager untuk countdown timer per level
/// Manages level time limits and triggers fail state when time runs out
/// </summary>
public partial class TimerManager : Node
{
	public static TimerManager Instance { get; private set; }
	
	[Export] public float DefaultLevelTime = 300f; // 5 minutes default
	
	private float _currentTime;
	private float _levelTimeLimit;
	private bool _isRunning = false;
	private string _currentLevelName = "";
	
	// Events
	[Signal] public delegate void TimeChangedEventHandler(float remainingTime);
	[Signal] public delegate void TimeUpEventHandler();
	
	public float CurrentTime => _currentTime;
	public float LevelTimeLimit => _levelTimeLimit;
	public bool IsRunning => _isRunning;
	public string CurrentLevelName => _currentLevelName;
	
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
		
		GD.Print("⏱️ TimerManager initialized");
	}
	
	public override void _Process(double delta)
	{
		if (!_isRunning) return;
		
		_currentTime -= (float)delta;
		
		if (_currentTime <= 0)
		{
			_currentTime = 0;
			_isRunning = false;
			EmitSignal(SignalName.TimeChanged, _currentTime); // Emit 0:00
			EmitSignal(SignalName.TimeUp);
			GD.Print("⏰ Time's up!");
		}
		else
		{
			EmitSignal(SignalName.TimeChanged, _currentTime);
		}
	}
	
	/// <summary>
	/// Start timer for a level with custom time limit
	/// </summary>
	public void StartTimer(float timeLimit, string levelName = "")
	{
		_levelTimeLimit = timeLimit;
		_currentTime = timeLimit;
		_currentLevelName = levelName;
		_isRunning = true;
		
		EmitSignal(SignalName.TimeChanged, _currentTime);
		GD.Print($"⏱️ Timer started for {(string.IsNullOrEmpty(levelName) ? "level" : levelName)}: {timeLimit} seconds");
	}
	
	/// <summary>
	/// Start timer with default time
	/// </summary>
	public void StartTimer(string levelName = "")
	{
		StartTimer(DefaultLevelTime, levelName);
	}
	
	/// <summary>
	/// Stop the timer
	/// </summary>
	public void StopTimer()
	{
		_isRunning = false;
		GD.Print("⏱️ Timer stopped");
	}
	
	/// <summary>
	/// Pause the timer
	/// </summary>
	public void PauseTimer()
	{
		_isRunning = false;
		GD.Print("⏸️ Timer paused");
	}
	
	/// <summary>
	/// Resume the timer
	/// </summary>
	public void ResumeTimer()
	{
		if (_currentTime > 0)
		{
			_isRunning = true;
			GD.Print("▶️ Timer resumed");
		}
	}
	
	/// <summary>
	/// Reset timer to level time limit
	/// </summary>
	public void ResetTimer()
	{
		_currentTime = _levelTimeLimit;
		EmitSignal(SignalName.TimeChanged, _currentTime);
		GD.Print($"🔄 Timer reset to {_levelTimeLimit} seconds");
	}
	
	/// <summary>
	/// Add extra time (for bonus if needed)
	/// </summary>
	public void AddTime(float seconds)
	{
		_currentTime += seconds;
		EmitSignal(SignalName.TimeChanged, _currentTime);
		GD.Print($"⏰ +{seconds}s added! New time: {_currentTime}");
	}
	
	/// <summary>
	/// Get formatted time string (MM:SS)
	/// </summary>
	public string GetFormattedTime()
	{
		int minutes = Mathf.FloorToInt(_currentTime / 60);
		int seconds = Mathf.FloorToInt(_currentTime % 60);
		return $"{minutes:00}:{seconds:00}";
	}
}
