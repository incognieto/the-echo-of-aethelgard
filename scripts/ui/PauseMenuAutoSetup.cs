using Godot;
using System;

/// <summary>
/// Standalone PauseMenu scene yang bisa di-instance ke level manapun.
/// Letakkan di root level untuk mendapatkan fungsi pause menu.
/// </summary>
public partial class PauseMenuAutoSetup : Node
{
	public override void _Ready()
	{
		// Check apakah PauseMenu sudah ada di scene
		if (GetParent().FindChild("PauseMenu") == null)
		{
			GD.PrintErr("PauseMenu not found in scene! Please add PauseMenu.tscn as a child of the root node.");
		}
	}
}
