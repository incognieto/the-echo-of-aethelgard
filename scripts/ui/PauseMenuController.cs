using Godot;
using System;

public partial class PauseMenuController : CanvasLayer
{
	private TextureButton _resumeButton;
	private TextureButton _restartButton;
	private TextureButton _settingsButton;
	private TextureButton _exitButton;
	private bool _isPaused = false;
	private string _currentScenePath = "";

	public override void _Ready()
	{
		// Set process mode agar bisa terima input meskipun game paused
		ProcessMode = ProcessModeEnum.Always;
		
		_resumeButton = GetNode<TextureButton>("PanelContainer/VBoxContainer/ResumeButton");
		_restartButton = GetNode<TextureButton>("PanelContainer/VBoxContainer/RestartButton");
		_settingsButton = GetNode<TextureButton>("PanelContainer/VBoxContainer/SettingsButton");
		_exitButton = GetNode<TextureButton>("PanelContainer/VBoxContainer/ExitButton");

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
			
			// Show cursor dan unlock untuk UI interaction
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else
		{
			GetTree().Paused = false;
			PanelManager.Instance.UnregisterPanel(this);
			
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
		GetTree().Paused = false;
		PanelManager.Instance.UnregisterPanel(this);
		_isPaused = false;
		Visible = false;
		
		// Keep cursor visible untuk Settings scene
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		GetTree().ChangeSceneToFile("res://scenes/ui/Settings.tscn");
	}

	private void OnExitPressed()
	{
		GetTree().Paused = false;
		PanelManager.Instance.UnregisterPanel(this);
		_isPaused = false;
		Visible = false;
		
		// Keep cursor visible untuk Main Menu
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
	}
}
