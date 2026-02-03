using Godot;
using System;

public partial class BoxBlur1 : Sprite2D
{
	public override void _Process(double delta)
	{
		Position += (GetGlobalMousePosition() * 4 * (float)delta) - Position;
	}
}
