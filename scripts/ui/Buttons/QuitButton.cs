using Godot;
using System;

public partial class QuitButton : Button
{
	public override void _Ready()
	{
		Pressed += OnQuitPressed;
	}

	private void OnQuitPressed()
	{
		// Quit the game
		GetTree().Quit();
	}
}
