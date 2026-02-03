using Godot;
using System;

public partial class TorchLight : PointLight2D
{
	[Export] public float MinEnergy { get; set; } = 0.9f;
	[Export] public float MaxEnergy { get; set; } = 1.0f;

	// Seberapa sering "target" baru dipilih (lebih besar = lebih tenang)
	[Export] public float FlickerIntervalSeconds { get; set; } = 0.12f;

	// Kecepatan smoothing menuju target (lebih besar = lebih cepat mengejar target)
	[Export] public float SmoothSpeed { get; set; } = 10.0f;

	// Seberapa kuat flicker mempengaruhi scale. 0 = scale tidak berubah.
	[Export] public float ScaleFlickerStrength { get; set; } = 0.35f;

	private bool _running;
	private Vector2 _baseScale;

	private float _targetEnergy;

	public override void _Ready()
	{
		_baseScale = Scale;
		_running = true;

		_targetEnergy = Energy; // mulai dari energy authored
		_ = PickTargetLoopAsync();
	}

	public override void _ExitTree()
	{
		_running = false;
	}

	public override void _Process(double delta)
	{
		// Smoothly approach target energy (frame-rate independent).
		float d = (float)delta;
		Energy = Mathf.Lerp(Energy, _targetEnergy, 1f - Mathf.Exp(-SmoothSpeed * d));

		// Optional: scale ikut “bernafas” tapi lebih halus & tidak sebesar energy.
		if (ScaleFlickerStrength > 0f)
		{
			// Map energy to a smaller scale variation around 1.0
			float scaleMul = 1f + (Energy - 1f) * ScaleFlickerStrength;
			Scale = _baseScale * scaleMul;
		}
		else
		{
			Scale = _baseScale;
		}
	}

	private async System.Threading.Tasks.Task PickTargetLoopAsync()
	{
		while (_running && IsInsideTree())
		{
			_targetEnergy = (float)GD.RandRange(MinEnergy, MaxEnergy);
			await ToSignal(GetTree().CreateTimer(FlickerIntervalSeconds), SceneTreeTimer.SignalName.Timeout);
		}
	}
}
