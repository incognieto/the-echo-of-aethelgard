using Godot;
using System;

public partial class TorchFlicker : OmniLight3D
{
	[Export] public float MinIntensity = 0.8f; // Energi minimal
	[Export] public float MaxIntensity = 1.5f; // Energi maksimal
	[Export] public float FlickerSpeed = 0.15f; // Kecepatan kedip

	private float _targetIntensity;
	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		_targetIntensity = LightEnergy;
		_rng.Randomize();
	}

	public override void _Process(double delta)
	{
		// Secara acak nentuin intensitas baru
		if (GD.Randf() < FlickerSpeed)
		{
			_targetIntensity = _rng.RandfRange(MinIntensity, MaxIntensity);
		}

		// Lerp biar transisi cahayanya halus, nggak patah-patah
		LightEnergy = Mathf.Lerp(LightEnergy, _targetIntensity, (float)delta * 12.0f);
	}
}
