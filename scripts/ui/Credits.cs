using Godot;
using System;

public partial class Credits : Control
{
	private ScrollContainer _scrollContainer;
	private TextureButton _backButton;
	private float _scrollSpeed = 100f; // pixels per second
	private bool _isScrolling = true;

	public override void _Ready()
	{
		// Start music for Credits scene
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.InstantRestart();
		}
		
		_scrollContainer = GetNode<ScrollContainer>("ScrollContainer");
		_backButton = GetNode<TextureButton>("BackButton");
		
		_backButton.Pressed += OnBackPressed;
	}

	public override void _Process(double delta)
	{
		if (_isScrolling)
		{
			// Auto-scroll the credits from bottom to top
			double newVScroll = _scrollContainer.GetVScrollBar().Value + delta * _scrollSpeed;
			
			// Stop scrolling when reached the end
			if (newVScroll >= _scrollContainer.GetVScrollBar().MaxValue)
			{
				_isScrolling = false;
			}
			else
			{
				_scrollContainer.GetVScrollBar().Value = newVScroll;
			}
		}
	}

	private void OnBackPressed()
	{
		// Return to main menu
		GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
	}
}
