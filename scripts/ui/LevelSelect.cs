using Godot;
using System;

public partial class LevelSelect : Control
{
	private TextureButton _level1Button;
	private TextureButton _level2Button;
	private TextureButton _level3Button;
	private TextureButton _level4Button;
	private TextureButton _level5Button;
	private TextureButton _backButton;

	public override void _Ready()
	{
		// Start music for Level Select scene
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.InstantRestart();
		}
		
		_level1Button = GetNode<TextureButton>("VBoxContainer/Level1Button");
		_level2Button = GetNode<TextureButton>("VBoxContainer/Level2Button");
		_level3Button = GetNode<TextureButton>("VBoxContainer/Level3Button");
		_level4Button = GetNode<TextureButton>("VBoxContainer/Level4Button");
		_level5Button = GetNode<TextureButton>("VBoxContainer/Level5Button");
		_backButton = GetNode<TextureButton>("VBoxContainer/BackButton");

		_level1Button.Pressed += OnLevel1Pressed;
		_level2Button.Pressed += OnLevel2Pressed;
		_level3Button.Pressed += OnLevel3Pressed;
		_level4Button.Pressed += OnLevel4Pressed;
		_level5Button.Pressed += OnLevel5Pressed;
		_backButton.Pressed += OnBackPressed;
	}

	private void OnLevel1Pressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/levels/level_1_cell/Main.tscn");
	}

	private void OnLevel2Pressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/levels/level_2_bridge/Main.tscn");
	}

	private void OnLevel3Pressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/levels/level_3_lab/Main.tscn");
	}

	private void OnLevel4Pressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/levels/level_4_library/Main.tscn");
	}

	private void OnLevel5Pressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/levels/level_5_Sewer/Main.tscn");
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
	}
}
