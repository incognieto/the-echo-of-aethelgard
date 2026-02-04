using Godot;
using System;

public partial class CreditsButton : Button
{
	public override void _Ready()
	{
		Pressed += OnCreditsPressed;
	}

	private void OnCreditsPressed()
	{
		// Navigate to credits scene
		GetTree().ChangeSceneToFile("res://scenes/ui/Credits.tscn");
	}
}
