using Godot;
using System;

public partial class Settings : Control
{
	private Button _backButton;

	public override void _Ready()
	{
		_backButton = GetNode<Button>("VBoxContainer/BackButton");
		_backButton.Pressed += OnBackPressed;
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
	}
}
