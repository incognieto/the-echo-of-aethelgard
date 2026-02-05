using Godot;
using System;

public partial class Settings : Control
{
	// Audio controls
	private HSlider _masterVolumeSlider;
	private Label _masterVolumeLabel;
	private HSlider _musicVolumeSlider;
	private Label _musicVolumeLabel;
	private HSlider _sfxVolumeSlider;
	private Label _sfxVolumeLabel;
	
	// Display controls
	private OptionButton _resolutionOption;
	
	// Buttons
	private Button _applyButton;
	private Button _backButton;
	
	// Settings file path
	private const string SETTINGS_PATH = "user://settings.cfg";
	
	// Audio bus indices
	private int _masterBusIndex;
	private int _musicBusIndex;
	private int _sfxBusIndex;
	
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
		// Start music for Settings scene
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.InstantRestart();
		}
		
		// Get audio bus indices
		_masterBusIndex = AudioServer.GetBusIndex("Master");
		_musicBusIndex = GetOrCreateBus("Music");
		_sfxBusIndex = GetOrCreateBus("SFX");
		
		// Get audio control nodes
		_masterVolumeSlider = GetNode<HSlider>("ScrollContainer/VBoxContainer/AudioSettings/MasterVolumeContainer/Slider");
		_masterVolumeLabel = GetNode<Label>("ScrollContainer/VBoxContainer/AudioSettings/MasterVolumeContainer/ValueLabel");
		_musicVolumeSlider = GetNode<HSlider>("ScrollContainer/VBoxContainer/AudioSettings/MusicVolumeContainer/Slider");
		_musicVolumeLabel = GetNode<Label>("ScrollContainer/VBoxContainer/AudioSettings/MusicVolumeContainer/ValueLabel");
		_sfxVolumeSlider = GetNode<HSlider>("ScrollContainer/VBoxContainer/AudioSettings/SFXVolumeContainer/Slider");
		_sfxVolumeLabel = GetNode<Label>("ScrollContainer/VBoxContainer/AudioSettings/SFXVolumeContainer/ValueLabel");
		
		// Get display control nodes
		_resolutionOption = GetNode<OptionButton>("ScrollContainer/VBoxContainer/DisplaySettings/ResolutionContainer/OptionButton");
		
		// Get buttons
		_applyButton = GetNode<Button>("ScrollContainer/VBoxContainer/ButtonContainer/ApplyButton");
		_backButton = GetNode<Button>("ScrollContainer/VBoxContainer/ButtonContainer/BackButton");
		
		// Connect signals
		_masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
		_musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
		_sfxVolumeSlider.ValueChanged += OnSFXVolumeChanged;
		_applyButton.Pressed += OnApplyPressed;
		_backButton.Pressed += OnBackPressed;
		
		// Load settings
		LoadSettings();
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
			GD.Print($"Created audio bus: {busName} at index {busIndex}");
		}
		return busIndex;
	}
	
	private void OnMasterVolumeChanged(double value)
	{
		_masterVolumeLabel.Text = $"{(int)value}%";
	}
	
	private void OnMusicVolumeChanged(double value)
	{
		_musicVolumeLabel.Text = $"{(int)value}%";
	}
	
	private void OnSFXVolumeChanged(double value)
	{
		_sfxVolumeLabel.Text = $"{(int)value}%";
	}
	
	private void OnApplyPressed()
	{
		ApplySettings();
		SaveSettings();
		
		// Show feedback
		GD.Print("Settings applied and saved!");
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
	}
	
	private void ApplySettings()
	{
		// Apply audio settings
		float masterVolume = VolumeToDb((float)_masterVolumeSlider.Value);
		float musicVolume = VolumeToDb((float)_musicVolumeSlider.Value);
		float sfxVolume = VolumeToDb((float)_sfxVolumeSlider.Value);
		
		AudioServer.SetBusVolumeDb(_masterBusIndex, masterVolume);
		AudioServer.SetBusVolumeDb(_musicBusIndex, musicVolume);
		AudioServer.SetBusVolumeDb(_sfxBusIndex, sfxVolume);
		
		GD.Print($"Audio settings applied - Master: {_masterVolumeSlider.Value}%, Music: {_musicVolumeSlider.Value}%, SFX: {_sfxVolumeSlider.Value}%");
		
		// Apply resolution
		int selectedResolution = _resolutionOption.Selected;
		Window window = GetWindow();
		
		var resolution = _resolutions[selectedResolution];
		GD.Print($"Setting resolution to: {resolution}");
		
		window.Size = resolution;
		
		// Center the window
		CallDeferred(nameof(CenterWindow));
		
		GD.Print($"Resolution applied. Current window size: {window.Size}");
	}
	
	private void CenterWindow()
	{
		Window window = GetWindow();
		var screenSize = DisplayServer.ScreenGetSize();
		var windowSize = window.Size;
		var centerPos = (screenSize - windowSize) / 2;
		window.Position = centerPos;
		GD.Print($"Window centered at position: {centerPos}");
	}
	
	private void SaveSettings()
	{
		var config = new ConfigFile();
		
		// Save audio settings
		config.SetValue("Audio", "MasterVolume", _masterVolumeSlider.Value);
		config.SetValue("Audio", "MusicVolume", _musicVolumeSlider.Value);
		config.SetValue("Audio", "SFXVolume", _sfxVolumeSlider.Value);
		
		// Save display settings
		config.SetValue("Display", "Resolution", _resolutionOption.Selected);
		
		var error = config.Save(SETTINGS_PATH);
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to save settings: {error}");
		}
	}
	
	private void LoadSettings()
	{
		var config = new ConfigFile();
		var error = config.Load(SETTINGS_PATH);
		
		if (error != Error.Ok)
		{
			// No settings file, use defaults
			GD.Print("No settings file found, using defaults");
			// Don't apply settings here, just load current window state
			LoadCurrentWindowSettings();
			return;
		}
		
		// Load audio settings
		_masterVolumeSlider.Value = (float)config.GetValue("Audio", "MasterVolume", 100.0);
		_musicVolumeSlider.Value = (float)config.GetValue("Audio", "MusicVolume", 100.0);
		_sfxVolumeSlider.Value = (float)config.GetValue("Audio", "SFXVolume", 100.0);
		
		// Load display settings
		_resolutionOption.Selected = (int)config.GetValue("Display", "Resolution", 0); // Default to 1920x1080
		
		GD.Print("Settings loaded from file");
	}
	
	private void LoadCurrentWindowSettings()
	{
		// Read current window state and update UI
		Window window = GetWindow();
		var currentSize = window.Size;
		
		GD.Print($"Loading current window settings - Size: {window.Size}");
		
		// Try to match current resolution to available options
		for (int i = 0; i < _resolutions.Length; i++)
		{
			if (_resolutions[i] == currentSize)
			{
				_resolutionOption.Selected = i;
				GD.Print($"Matched current resolution to index {i}: {_resolutions[i]}");
				break;
			}
		}
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
