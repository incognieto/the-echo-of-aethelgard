using Godot;
using System;

public partial class LevelSelectButton : Button
{
	public override void _Ready()
	{
		Pressed += OnLevelSelectPressed;
	}

	private void OnLevelSelectPressed()
	{
		// Navigate to level select menu
		GetTree().ChangeSceneToFile("res://scenes/ui/LevelSelect.tscn");
	}
}
