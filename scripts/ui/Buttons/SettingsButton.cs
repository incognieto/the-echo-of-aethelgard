using Godot;
using System;

public partial class SettingsButton : Button
{
	public override void _Ready()
	{
		Pressed += OnSettingsPressed;
	}

	private void OnSettingsPressed()
	{
		// Set Settings source to MainMenu (default for generic button)
		Settings.CurrentSource = Settings.SettingsSource.MainMenu;
		Settings.ReturnScenePath = "";
		Settings.SetPauseMenuReference(null); // Clear any previous pause menu reference
		
		// Navigate to settings scene
		GetTree().ChangeSceneToFile("res://scenes/ui/Settings.tscn");
	}
}
