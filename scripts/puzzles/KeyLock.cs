using Godot;
using System;

public partial class KeyLock : TextureRect // Balik ke TextureRect biar ada Expand Mode
{
	[Signal] public delegate void RotatedEventHandler(int newIndex);

	private float _visualIndex = 0.0f;
	private int _logicIndex = 0;
	private const float TotalFrames = 6.0f;
	private ShaderMaterial _mat;

	public override void _Ready()
	{
		if (Material != null) Material = (ShaderMaterial)Material.Duplicate();
		_mat = Material as ShaderMaterial;

		// TextureRect butuh ini biar bisa deteksi klik & hover
		MouseFilter = MouseFilterEnum.Stop; 
		
		// Manual Hover Effect
		MouseEntered += () => CursorManager.Instance?.SetCursor(CursorManager.CursorType.Hover);
		MouseExited += () => CursorManager.Instance?.SetCursor(CursorManager.CursorType.Standard);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				ScrollDown();
			}
		}
	}

	private void ScrollDown()
	{
		_visualIndex += 1.0f;
		_logicIndex = (_logicIndex + 1) % (int)TotalFrames;

		Tween tween = CreateTween();
		tween.TweenProperty(_mat, "shader_parameter/target_index", _visualIndex, 0.25f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
			
		EmitSignal(SignalName.Rotated, _logicIndex);
	}
}
