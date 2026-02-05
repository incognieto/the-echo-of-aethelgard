using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Global manager for button sound effects
/// Automatically adds hover and click sounds to all buttons in the scene
/// </summary>
public partial class ButtonSoundManager : Node
{
	public static ButtonSoundManager Instance { get; private set; }
	
	private AudioStreamPlayer _hoverPlayer;
	private AudioStreamPlayer _clickPlayer;
	
	private AudioStream _hoverSound;
	private AudioStream _clickSound;
	
	private HashSet<BaseButton> _registeredButtons = new HashSet<BaseButton>();

	public override void _Ready()
	{
		Instance = this;
		
		// Ensure ButtonSoundManager continues to process even when game is paused
		ProcessMode = ProcessModeEnum.Always;
		
		// Ensure SFX bus exists (SettingsManager should create it, but double-check)
		int sfxBusIndex = AudioServer.GetBusIndex("SFX");
		if (sfxBusIndex == -1)
		{
			AudioServer.AddBus();
			sfxBusIndex = AudioServer.BusCount - 1;
			AudioServer.SetBusName(sfxBusIndex, "SFX");
			AudioServer.SetBusSend(sfxBusIndex, "Master");
			GD.Print("ButtonSoundManager: Created SFX audio bus");
		}
		
		// Load sound effects
		_hoverSound = GD.Load<AudioStream>("res://assets/audio/sfx/button_hover.wav");
		_clickSound = GD.Load<AudioStream>("res://assets/audio/sfx/button_click.wav");
		
		if (_hoverSound == null)
		{
			GD.PushError("ButtonSoundManager: Failed to load button_hover.wav");
		}
		
		if (_clickSound == null)
		{
			GD.PushError("ButtonSoundManager: Failed to load button_click.wav");
		}
		
		// Create audio players for SFX
		_hoverPlayer = new AudioStreamPlayer();
		_hoverPlayer.Bus = "SFX";
		_hoverPlayer.Stream = _hoverSound;
		_hoverPlayer.ProcessMode = ProcessModeEnum.Always;
		AddChild(_hoverPlayer);
		
		_clickPlayer = new AudioStreamPlayer();
		_clickPlayer.Bus = "SFX";
		_clickPlayer.Stream = _clickSound;
		_clickPlayer.ProcessMode = ProcessModeEnum.Always;
		AddChild(_clickPlayer);
		
		// Connect to scene tree changes to auto-register new buttons
		GetTree().NodeAdded += OnNodeAdded;
		GetTree().NodeRemoved += OnNodeRemoved;
		
		// Wait a bit then register all buttons in the scene
		// Use a timer to give Main Menu time to fully load
		var timer = GetTree().CreateTimer(0.1);
		timer.Timeout += RegisterAllButtonsInScene;
	}
	
	private void OnNodeAdded(Node node)
	{
		// Auto-register buttons as they are added to the tree
		if (node is BaseButton button)
		{
			// Use CallDeferred to ensure button is fully ready
			CallDeferred(nameof(RegisterButton), button);
		}
	}
	
	private void OnNodeRemoved(Node node)
	{
		// Auto-unregister buttons when removed
		if (node is BaseButton button)
		{
			UnregisterButton(button);
		}
	}
	
	/// <summary>
	/// Register all buttons in the current scene tree
	/// </summary>
	public void RegisterAllButtonsInScene()
	{
		var root = GetTree().Root;
		RegisterButtonsRecursive(root);
		GD.Print($"ButtonSoundManager: Registered {_registeredButtons.Count} buttons");
	}
	
	private void RegisterButtonsRecursive(Node node)
	{
		if (node is BaseButton button)
		{
			RegisterButton(button);
		}
		
		foreach (Node child in node.GetChildren())
		{
			RegisterButtonsRecursive(child);
		}
	}
	
	/// <summary>
	/// Register a single button to have sound effects
	/// </summary>
	public void RegisterButton(BaseButton button)
	{
		if (button == null || _registeredButtons.Contains(button))
			return;
		
		button.MouseEntered += () => OnButtonHover(button);
		button.Pressed += () => OnButtonClick(button);
		
		_registeredButtons.Add(button);
	}
	
	/// <summary>
	/// Unregister a button (useful when button is deleted)
	/// </summary>
	public void UnregisterButton(BaseButton button)
	{
		if (button == null || !_registeredButtons.Contains(button))
			return;
		
		_registeredButtons.Remove(button);
	}
	
	private void OnButtonHover(BaseButton button)
	{
		// Only play hover sound if button is not disabled
		if (button.Disabled)
			return;
		
		if (_hoverPlayer != null && _hoverSound != null)
		{
			_hoverPlayer.Play();
		}
	}
	
	private void OnButtonClick(BaseButton button)
	{
		if (_clickPlayer != null && _clickSound != null)
		{
			_clickPlayer.Play();
		}
	}
	
	/// <summary>
	/// Manually play hover sound
	/// </summary>
	public void PlayHoverSound()
	{
		_hoverPlayer?.Play();
	}
	
	/// <summary>
	/// Manually play click sound
	/// </summary>
	public void PlayClickSound()
	{
		_clickPlayer?.Play();
	}
	
	public override void _ExitTree()
	{
		if (GetTree() != null)
		{
			GetTree().NodeAdded -= OnNodeAdded;
			GetTree().NodeRemoved -= OnNodeRemoved;
		}
		_registeredButtons.Clear();
		Instance = null;
	}
}
