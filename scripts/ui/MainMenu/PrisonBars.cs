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

	// --- Audio (optional) ---
	// Assign a sound here (or set up a child AudioStreamPlayer and point AudioPlayerPath).
	[Export] public AudioStream StartDropSfx { get; set; }
	[Export(PropertyHint.Range, "0,2,0.01")] public float StartDropSfxVolume { get; set; } = 1.0f;
	[Export(PropertyHint.NodePathValidTypes, "AudioStreamPlayer,AudioStreamPlayer2D")] public NodePath AudioPlayerPath { get; set; }

	private Node _audioPlayer;

	private Vector2 _startPos;
	private bool _initialized;
	private bool _isPlaying;

	public override void _Ready()
	{
		_startPos = Position;
		_initialized = true;

		// Optional: use a provided AudioStreamPlayer, otherwise create one.
		_audioPlayer = ResolveAudioPlayer();
	}

	private Node ResolveAudioPlayer()
	{
		if (AudioPlayerPath != null && !AudioPlayerPath.IsEmpty)
		{
			// Supports AudioStreamPlayer2D too by casting to Node, then as AudioStreamPlayer via inheritance.
			var n = GetNodeOrNull<Node>(AudioPlayerPath);
			if (n is AudioStreamPlayer asp)
				return asp;
			if (n is AudioStreamPlayer2D asp2d)
				return asp2d;

			GD.PushWarning($"{Name}: AudioPlayerPath '{AudioPlayerPath}' is not an AudioStreamPlayer/AudioStreamPlayer2D.");
		}

		// Create a local player so this script works out-of-the-box.
		var created = new AudioStreamPlayer
		{
			Name = "StartDropSfxPlayer",
			Bus = "SFX",
			Autoplay = false,
		};
		AddChild(created);
		return created;
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

		// Fire SFX right when the animation starts.
		TryPlayStartSfx();

		// Ensure we start from the top position each time.
		Position = _startPos;

		var tween = CreateTween();
		tween.SetTrans(Transition);
		tween.SetEase(Ease);
		tween.TweenProperty(this, "position", _startPos + new Vector2(0, DropDistance), DropDuration);
		tween.Finished += () => _isPlaying = false;

		return tween;
	}

	private void TryPlayStartSfx()
	{
		if (_audioPlayer == null)
			_audioPlayer = ResolveAudioPlayer();

		if (_audioPlayer == null)
			return;

		if (_audioPlayer is AudioStreamPlayer asp)
		{
			if (StartDropSfx != null)
				asp.Stream = StartDropSfx;
			if (asp.Stream == null)
				return;
			asp.VolumeDb = Mathf.LinearToDb(Mathf.Max(0.0001f, StartDropSfxVolume));
			asp.Play();
			return;
		}

		if (_audioPlayer is AudioStreamPlayer2D asp2d)
		{
			if (StartDropSfx != null)
				asp2d.Stream = StartDropSfx;
			if (asp2d.Stream == null)
				return;
			asp2d.VolumeDb = Mathf.LinearToDb(Mathf.Max(0.0001f, StartDropSfxVolume));
			asp2d.Play();
		}
	}
}
