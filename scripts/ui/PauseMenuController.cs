using Godot;
using System;

public partial class PauseMenuController : CanvasLayer
{
	private Button _resumeButton;
	private Button _restartButton;
	private Button _settingsButton;
	private Button _exitButton;
	private bool _isPaused = false;
	private string _currentScenePath = "";

	private CanvasLayer _settingsOverlayLayer = null;
	
	public override void _Ready()
	{
		// Set process mode agar bisa terima input meskipun game paused
		ProcessMode = ProcessModeEnum.Always;
		
		_resumeButton = GetNode<Button>("PanelContainer/VBoxContainer/ResumeButton");
		_restartButton = GetNode<Button>("PanelContainer/VBoxContainer/RestartButton");
		_settingsButton = GetNode<Button>("PanelContainer/VBoxContainer/SettingsButton");
		_exitButton = GetNode<Button>("PanelContainer/VBoxContainer/ExitButton");

		// Connect button signals
		_resumeButton.Pressed += OnResumePressed;
		_restartButton.Pressed += OnRestartPressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_exitButton.Pressed += OnExitPressed;

		// Initially hide the pause menu
		Visible = false;
		_isPaused = false;
	}

	public override void _Process(double delta)
	{
		// Hanya bisa pause jika di gameplay, bukan di menu
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			// Check apakah sedang di menu scene
			string currentScenePath = GetTree().CurrentScene.SceneFilePath.ToLower();
			if (currentScenePath.Contains("menu") || 
			    currentScenePath.Contains("settings") || 
			    currentScenePath.Contains("credits") ||
			    currentScenePath.Contains("levelselect"))
			{
				// Jangan buka pause menu di scene menu
				return;
			}
			
			// Jika ada panel active dan bukan pause menu, jangan buka pause menu
			if (PanelManager.Instance.HasActivePanels() && 
				PanelManager.Instance.GetActivePanelTop() != this)
			{
				// Panel lain sedang active, close panel itu dulu
				return;
			}

			TogglePause();
		}
	}

	public void TogglePause()
	{
		_isPaused = !_isPaused;
		Visible = _isPaused;

		if (_isPaused)
		{
			// Simpan current scene path untuk restart
			_currentScenePath = GetTree().CurrentScene.SceneFilePath;
			GetTree().Paused = true;
			PanelManager.Instance.RegisterPanel(this);
			
			// PAUSE TIMER
			if (TimerManager.Instance != null)
			{
				TimerManager.Instance.PauseTimer();
				GD.Print("⏸️ Timer paused (PauseMenu opened)");
			}
			
			// Show cursor dan unlock untuk UI interaction
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else
		{
			GetTree().Paused = false;
			PanelManager.Instance.UnregisterPanel(this);
			
			// RESUME TIMER
			if (TimerManager.Instance != null)
			{
				TimerManager.Instance.ResumeTimer();
				GD.Print("▶️ Timer resumed (PauseMenu closed)");
			}
			
			// Hide cursor kembali saat resume game
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public bool IsPaused()
	{
		return _isPaused;
	}

	private void OnResumePressed()
	{
		TogglePause();
	}

	private void OnRestartPressed()
	{
		GetTree().Paused = false;
		PanelManager.Instance.UnregisterPanel(this);
		_isPaused = false;
		Visible = false;
		
		// Stop timer before restart
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.StopTimer();
			GD.Print("⏹️ Timer stopped (Level restarting)");
		}
		
		// Reset lives to 3
		if (LivesManager.Instance != null)
		{
			LivesManager.Instance.ResetLives();
			GD.Print("❤️ Lives reset to 3");
		}
		
		// Capture cursor kembali saat restart
		Input.MouseMode = Input.MouseModeEnum.Captured;
		
		// Reload scene yang tersimpan
		if (!string.IsNullOrEmpty(_currentScenePath))
		{
			GetTree().ChangeSceneToFile(_currentScenePath);
		}
		else
		{
			GetTree().ReloadCurrentScene();
		}
	}

	private void OnSettingsPressed()
	{
		// JANGAN unpause game, JANGAN pindah scene
		// Instantiate Settings sebagai overlay dengan CanvasLayer
		
		// Set Settings source to PauseMenu
		Settings.CurrentSource = Settings.SettingsSource.PauseMenu;
		Settings.SetPauseMenuReference(this);
		GD.Print("⚙️ Opening Settings as overlay from PauseMenu");
		
		// Create CanvasLayer untuk Settings (higher layer than PauseMenu)
		_settingsOverlayLayer = new CanvasLayer();
		_settingsOverlayLayer.Name = "SettingsOverlay";
		_settingsOverlayLayer.Layer = 100; // Higher layer = on top
		_settingsOverlayLayer.ProcessMode = ProcessModeEnum.Always; // Work when paused
		
		// Load and instantiate Settings scene
		var settingsScene = GD.Load<PackedScene>("res://scenes/ui/Settings.tscn");
		var settingsInstance = settingsScene.Instantiate<Settings>();
		
		// Add Settings to CanvasLayer
		_settingsOverlayLayer.AddChild(settingsInstance);
		
		// Add CanvasLayer to root (so it appears on top of everything)
		GetTree().Root.AddChild(_settingsOverlayLayer);
		
		// Hide PauseMenu (but keep it active, game still paused)
		Visible = false;
		GD.Print("✅ Settings overlay added to Layer 100, PauseMenu hidden");
	}
	
	public void ShowAfterSettings()
	{
		// Called by Settings when back button is pressed
		
		// Clean up settings overlay layer
		if (_settingsOverlayLayer != null)
		{
			_settingsOverlayLayer.QueueFree();
			_settingsOverlayLayer = null;
			GD.Print("✅ Settings overlay layer removed");
		}
		
		Visible = true;
		GD.Print("✅ PauseMenu shown again after Settings closed");
	}

	private void OnExitPressed()
	{
		GetTree().Paused = false;
		PanelManager.Instance.UnregisterPanel(this);
		_isPaused = false;
		Visible = false;
		
		// Stop timer when exiting to main menu
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.StopTimer();
			GD.Print("⏹️ Timer stopped (Exiting to Main Menu)");
		}
		
		// Reset lives to 3
		if (LivesManager.Instance != null)
		{
			LivesManager.Instance.ResetLives();
			GD.Print("❤️ Lives reset to 3");
		}
		
		// Clear Settings references
		Settings.CurrentSource = Settings.SettingsSource.MainMenu;
		Settings.SetPauseMenuReference(null);
		GD.Print("🧹 Settings references cleared (returning to MainMenu)");
		
		// Keep cursor visible untuk Main Menu
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
	}
}
