using Godot;
using System;

public partial class MapUI : Control
{
	private Label _coordLabel;
	private Player _player;
	
	public override void _Ready()
	{
		_player = GetTree().Root.GetNodeOrNull<Player>("Main/Player");
		_coordLabel = GetNode<Label>("CoordLabel");
		
		GD.Print("✓ MapUI initialized!");
	}
	
	public override void _Process(double delta)
	{
		if (_player != null && _coordLabel != null)
		{
			_coordLabel.Text = $"X: {_player.GlobalPosition.X:F1}  Z: {_player.GlobalPosition.Z:F1}";
		}
	}
}
