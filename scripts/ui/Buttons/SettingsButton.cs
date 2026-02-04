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
		// Navigate to settings scene
		GetTree().ChangeSceneToFile("res://scenes/ui/Settings.tscn");
	}
}
