using Godot;
using System;

public partial class MainMenuController : Control
{
	private TextureButton _playButton;
	private TextureButton _levelSelectButton;
	private TextureButton _settingsButton;
	private TextureButton _creditsButton;
	private TextureButton _exitButton;

	public override void _Ready()
	{
		// Get references to buttons from the MainMenu scene
		_playButton = GetNode<TextureButton>("Panel/BtnPlay");
		_levelSelectButton = GetNode<TextureButton>("Panel/BtnLevelSelect");
		_settingsButton = GetNode<TextureButton>("Panel/BtnSettings");
		_creditsButton = GetNode<TextureButton>("Panel/BtnCredits");
		_exitButton = GetNode<TextureButton>("Panel/BtnExit");

		// Connect button signals
		_playButton.Pressed += OnPlayPressed;
		_levelSelectButton.Pressed += OnLevelSelectPressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_creditsButton.Pressed += OnCreditsPressed;
		_exitButton.Pressed += OnExitPressed;
	}

	private void OnPlayPressed()
	{
		// Navigate to prologue level
		GD.Print("New Game clicked - going to prologue");
		GetTree().ChangeSceneToFile("res://scenes/levels/level_0_prologue/Prologue.tscn");
	}

	private void OnLevelSelectPressed()
	{
		// Navigate to level select scene
		GD.Print("Level Select clicked");
		GetTree().ChangeSceneToFile("res://scenes/ui/LevelSelect.tscn");
	}

	private void OnSettingsPressed()
	{
		// Navigate to settings scene
		GD.Print("Settings clicked");
		GetTree().ChangeSceneToFile("res://scenes/ui/Settings.tscn");
	}

	private void OnCreditsPressed()
	{
		// Navigate to credits scene
		GD.Print("Credits clicked");
		GetTree().ChangeSceneToFile("res://scenes/ui/Credits.tscn");
	}

	private void OnExitPressed()
	{
		// Quit the game
		GD.Print("Exit clicked");
		GetTree().Quit();
	}
}
