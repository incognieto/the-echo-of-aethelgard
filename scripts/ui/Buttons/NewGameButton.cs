using Godot;
using System;

public partial class NewGameButton : Button
{
	public override void _Ready()
	{
		Pressed += OnNewGamePressed;
	}

	private void OnNewGamePressed()
	{
		// Navigate to prologue level
		GetTree().ChangeSceneToFile("res://scenes/levels/level_0_prologue/Level0Prologue.tscn");
	}
}
