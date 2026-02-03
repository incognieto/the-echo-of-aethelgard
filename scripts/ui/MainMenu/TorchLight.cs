using Godot;
using System;

public partial class TorchLight : PointLight2D
{
	
	[Export] public float MinEnergy { get; set; } = 0.9f;
	[Export] public float MaxEnergy { get; set; } = 1.0f;
	[Export] public float FlickerIntervalSeconds { get; set; } = 0.1333f;

	private bool _running;
	private Vector2 _baseScale;

	public override void _Ready()
	{
		_baseScale = Scale;
		_running = true;
		_ = FlickerLoopAsync();
	}

	public override void _ExitTree()
	{

		_running = false;
	}

	private async System.Threading.Tasks.Task FlickerLoopAsync()
	{
		while (_running && IsInsideTree())
		{
			// energy = randf() * 0.1 + 0.9
			float e = (float)GD.RandRange(MinEnergy, MaxEnergy);
			Energy = e;
			Scale = _baseScale * e;

			await ToSignal(GetTree().CreateTimer(FlickerIntervalSeconds), SceneTreeTimer.SignalName.Timeout);
		}
	}
}
