using Godot;
using System;

/// <summary>
/// Global settings manager that loads and applies settings at game startup
/// </summary>
public partial class SettingsManager : Node
{
	public static SettingsManager Instance { get; private set; }
	
	private const string SETTINGS_PATH = "user://settings.cfg";
	
	// Audio bus indices
	private int _masterBusIndex;
	private int _musicBusIndex;
	private int _sfxBusIndex;
	
	// Current settings
	public float MasterVolume { get; private set; } = 100.0f;
	public float MusicVolume { get; private set; } = 100.0f;
	public float SFXVolume { get; private set; } = 100.0f;
	public int ResolutionIndex { get; private set; } = 0;
	
	// Resolution options
	private readonly Vector2I[] _resolutions = new Vector2I[]
	{
		new Vector2I(1920, 1080),
		new Vector2I(1600, 900),
		new Vector2I(1366, 768),
		new Vector2I(1280, 720),
		new Vector2I(1024, 576),
		new Vector2I(800, 600)
	};

	public override void _Ready()
	{
		Instance = this;
		
		// Ensure SettingsManager continues to process even when game is paused
		ProcessMode = ProcessModeEnum.Always;
		
		// Get or create audio bus indices
		_masterBusIndex = AudioServer.GetBusIndex("Master");
		_musicBusIndex = GetOrCreateBus("Music");
		_sfxBusIndex = GetOrCreateBus("SFX");
		
		// Load and apply settings
		LoadSettings();
		ApplySettings();
	}
	
	private int GetOrCreateBus(string busName)
	{
		int busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex == -1)
		{
			// Bus doesn't exist, create it
			AudioServer.AddBus();
			busIndex = AudioServer.BusCount - 1;
			AudioServer.SetBusName(busIndex, busName);
			AudioServer.SetBusSend(busIndex, "Master");
			GD.Print($"SettingsManager: Created audio bus '{busName}' at index {busIndex}");
		}
		return busIndex;
	}
	
	public void LoadSettings()
	{
		var config = new ConfigFile();
		var error = config.Load(SETTINGS_PATH);
		
		if (error != Error.Ok)
		{
			GD.Print("SettingsManager: No settings file found, using defaults");
			return;
		}
		
		// Load audio settings
		MasterVolume = (float)config.GetValue("Audio", "MasterVolume", 100.0);
		MusicVolume = (float)config.GetValue("Audio", "MusicVolume", 100.0);
		SFXVolume = (float)config.GetValue("Audio", "SFXVolume", 100.0);
		
		// Load display settings
		ResolutionIndex = (int)config.GetValue("Display", "Resolution", 0);
		
		GD.Print("SettingsManager: Settings loaded successfully");
	}
	
	public void ApplySettings()
	{
		// Apply audio settings
		float masterVolumeDb = VolumeToDb(MasterVolume);
		float musicVolumeDb = VolumeToDb(MusicVolume);
		float sfxVolumeDb = VolumeToDb(SFXVolume);
		
		AudioServer.SetBusVolumeDb(_masterBusIndex, masterVolumeDb);
		AudioServer.SetBusVolumeDb(_musicBusIndex, musicVolumeDb);
		AudioServer.SetBusVolumeDb(_sfxBusIndex, sfxVolumeDb);
		
		GD.Print($"SettingsManager: Audio applied - Master: {MasterVolume}%, Music: {MusicVolume}%, SFX: {SFXVolume}%");
		
		// Apply resolution
		if (ResolutionIndex >= 0 && ResolutionIndex < _resolutions.Length)
		{
			Window window = GetTree().Root;
			var resolution = _resolutions[ResolutionIndex];
			
			GD.Print($"SettingsManager: Setting resolution to: {resolution}");
			window.Size = resolution;
			
			// Center the window
			CallDeferred(nameof(CenterWindow));
			
			GD.Print($"SettingsManager: Resolution applied. Current window size: {window.Size}");
		}
	}
	
	private void CenterWindow()
	{
		Window window = GetTree().Root;
		var screenSize = DisplayServer.ScreenGetSize();
		var windowSize = window.Size;
		var centerPos = (screenSize - windowSize) / 2;
		window.Position = centerPos;
		GD.Print($"SettingsManager: Window centered at position: {centerPos}");
	}
	
	/// <summary>
	/// Convert volume percentage (0-100) to decibels (-80 to 0)
	/// </summary>
	private float VolumeToDb(float volume)
	{
		if (volume <= 0)
			return -80.0f;
		
		// Convert 0-100 to 0-1, then to decibels
		float normalizedVolume = volume / 100.0f;
		return Mathf.LinearToDb(normalizedVolume);
	}
}
