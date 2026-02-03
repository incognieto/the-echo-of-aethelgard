using Godot;

namespace Teoa.UI.MainMenu;

/// <summary>
/// Handles the "prison gate closes" effect on the main menu.
/// Attach this to the PrisonBars Sprite2D.
/// </summary>
public partial class PrisonBars : Sprite2D
{
	[Export] public float DropDistance { get; set; } = 650f;
	[Export] public float DropDuration { get; set; } = 0.7f;
	[Export] public Tween.TransitionType Transition { get; set; } = Tween.TransitionType.Cubic;
	[Export] public Tween.EaseType Ease { get; set; } = Tween.EaseType.Out;

	private Vector2 _startPos;
	private bool _initialized;
	private bool _isPlaying;

	public override void _Ready()
	{
		_startPos = Position;
		_initialized = true;
	}

	/// <summary>
	/// Plays the bars dropping down. Returns the Tween so callers can await finished.
	/// Method name is referenced by SceneChangeButton (default: PlayCloseAndWait).
	/// </summary>
	public Tween PlayCloseAndWait()
	{
		if (!_initialized)
		{
			_startPos = Position;
			_initialized = true;
		}

		if (_isPlaying)
			return null;

		_isPlaying = true;

		// Ensure we start from the top position each time.
		Position = _startPos;

		var tween = CreateTween();
		tween.SetTrans(Transition);
		tween.SetEase(Ease);
		tween.TweenProperty(this, "position", _startPos + new Vector2(0, DropDistance), DropDuration);
		tween.Finished += () => _isPlaying = false;

		return tween;
	}
}
