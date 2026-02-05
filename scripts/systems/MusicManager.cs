using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MusicManager : Node
{
	public static MusicManager Instance { get; private set; }
	
	private AudioStreamPlayer _musicPlayer;
	private Tween _fadeTween;
	
	private List<string> _playlist = new List<string>();
	private List<string> _shuffledPlaylist = new List<string>();
	private int _currentTrackIndex = 0;
	
	private float _fadeInDuration = 4.0f;
	private float _fadeOutDuration = 3.0f;
	private float _maxVolume = 0.0f; // dB (0 = full volume, -10 = quieter)
	
	private bool _isPlaying = false;
	private bool _isFadingOut = false;
	
	public override void _Ready()
	{
		Instance = this;
		
		// Ensure MusicManager continues to process even when game is paused
		ProcessMode = ProcessModeEnum.Always;
		
		// Setup audio player
		_musicPlayer = new AudioStreamPlayer();
		_musicPlayer.VolumeDb = -80.0f; // Start muted
		_musicPlayer.ProcessMode = ProcessModeEnum.Always; // Continue playing when paused
		_musicPlayer.Bus = "Music"; // Use the Music audio bus
		AddChild(_musicPlayer);
		
		// Load all music tracks
		LoadMusicTracks();
		
		// Connect finished signal
		_musicPlayer.Finished += OnMusicFinished;
	}
	
	private void LoadMusicTracks()
	{
		// Add all medieval music tracks
		for (int i = 1; i <= 8; i++)
		{
			_playlist.Add($"res://assets/audio/music/Medieval Vol. 2 {i} (Loop).mp3");
		}
		
		// Create shuffled playlist
		ShufflePlaylist();
	}
	
	private void ShufflePlaylist()
	{
		_shuffledPlaylist = new List<string>(_playlist);
		
		// Fisher-Yates shuffle algorithm
		Random rng = new Random();
		int n = _shuffledPlaylist.Count;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			string value = _shuffledPlaylist[k];
			_shuffledPlaylist[k] = _shuffledPlaylist[n];
			_shuffledPlaylist[n] = value;
		}
		
		_currentTrackIndex = 0;
	}
	
	/// <summary>
	/// Start playing music with fade in
	/// </summary>
	public void PlayMusic()
	{
		if (_shuffledPlaylist.Count == 0) return;
		
		// Shuffle playlist every time to ensure randomness
		ShufflePlaylist();
		
		_isFadingOut = false;
		PlayTrackAtIndex(_currentTrackIndex);
	}
	
	/// <summary>
	/// Stop music with fade out
	/// </summary>
	public void StopMusic()
	{
		if (!_isPlaying) return;
		
		FadeOut(() => {
			_musicPlayer.Stop();
			_isPlaying = false;
		});
	}
	
	/// <summary>
	/// Change to a new track immediately (fade out current, fade in new)
	/// </summary>
	public void ChangeTrack()
	{
		if (!_isPlaying)
		{
			PlayMusic();
			return;
		}
		
		FadeOut(() => {
			NextTrack();
			PlayTrackAtIndex(_currentTrackIndex);
		});
	}
	
	/// <summary>
	/// Fade out current music and start a new random playlist
	/// Used when changing scenes
	/// </summary>
	public void FadeOutAndRestart()
	{
		GD.Print("MusicManager: FadeOutAndRestart called");
		
		// Stop any ongoing fade
		_fadeTween?.Kill();
		_isFadingOut = false;
		
		if (_musicPlayer.Playing)
		{
			GD.Print("MusicManager: Music is playing, fading out first");
			FadeOut(() => {
				ShufflePlaylist();
				PlayTrackAtIndex(_currentTrackIndex);
			});
		}
		else
		{
			GD.Print("MusicManager: No music playing, starting fresh");
			ShufflePlaylist();
			PlayTrackAtIndex(_currentTrackIndex);
		}
	}
	
	/// <summary>
	/// Instantly restart music without fade out
	/// Used for quick scene transitions from menus
	/// </summary>
	public void InstantRestart()
	{
		GD.Print("MusicManager: InstantRestart called");
		
		// Stop any ongoing fade
		_fadeTween?.Kill();
		_isFadingOut = false;
		
		// Stop current music immediately
		if (_musicPlayer.Playing)
		{
			_musicPlayer.Stop();
		}
		
		// Start new music with shuffle (PlayTrackAtIndex will handle volume reset and fade in)
		ShufflePlaylist();
		PlayTrackAtIndex(_currentTrackIndex);
	}
	
	private void PlayTrackAtIndex(int index)
	{
		if (index < 0 || index >= _shuffledPlaylist.Count) return;
		
		// Load and play the track
		var stream = GD.Load<AudioStream>(_shuffledPlaylist[index]);
		if (stream != null)
		{
			_musicPlayer.Stream = stream;
			
			// Reset volume to muted for fade in
			_musicPlayer.VolumeDb = -80.0f;
			
			_musicPlayer.Play();
			_isPlaying = true;
			
			// Fade in
			FadeIn();
			
			GD.Print($"Now playing: {_shuffledPlaylist[index]}");
		}
	}
	
	private void NextTrack()
	{
		_currentTrackIndex++;
		
		// If we've reached the end of the shuffled playlist, reshuffle
		if (_currentTrackIndex >= _shuffledPlaylist.Count)
		{
			ShufflePlaylist();
		}
	}
	
	private void OnMusicFinished()
	{
		if (_isFadingOut) return; // Don't auto-play next if we're fading out
		
		// Play next track
		NextTrack();
		PlayTrackAtIndex(_currentTrackIndex);
	}
	
	private void FadeIn()
	{
		// Cancel any existing fade
		_fadeTween?.Kill();
		
		_fadeTween = CreateTween();
		_fadeTween.TweenProperty(_musicPlayer, "volume_db", _maxVolume, _fadeInDuration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.InOut);
	}
	
	private void FadeOut(Action onComplete = null)
	{
		_isFadingOut = true;
		
		// Cancel any existing fade
		_fadeTween?.Kill();
		
		_fadeTween = CreateTween();
		_fadeTween.TweenProperty(_musicPlayer, "volume_db", -80.0f, _fadeOutDuration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.InOut);
		
		_fadeTween.TweenCallback(Callable.From(() => {
			_isFadingOut = false;
			onComplete?.Invoke();
		}));
	}
	
	/// <summary>
	/// Set fade durations
	/// </summary>
	public void SetFadeDurations(float fadeIn, float fadeOut)
	{
		_fadeInDuration = fadeIn;
		_fadeOutDuration = fadeOut;
	}
	
	/// <summary>
	/// Set max volume in dB
	/// </summary>
	public void SetMaxVolume(float volumeDb)
	{
		_maxVolume = volumeDb;
	}
	
	public override void _ExitTree()
	{
		_fadeTween?.Kill();
		Instance = null;
	}
}
