using Godot;
using System;

/// <summary>
/// Game Over Screen yang muncul ketika semua nyawa habis
/// "GAME OVER" + Back to Main Menu button
/// Now uses scene-based UI instead of programmatic creation
/// </summary>
public partial class GameOverScreen : Control
{
	private ColorRect _blackCover;
	private Panel _messagePanel;
	private Label _gameOverLabel;
	private Label _subLabel;
	private Button _mainMenuButton;
	private FontFile _customFont;
	
	public override void _Ready()
	{
		GD.Print("💀 GameOverScreen _Ready() called");
		
		// CRITICAL: Set ProcessMode to Always so it can receive input when paused
		ProcessMode = ProcessModeEnum.Always;
		
		// Load custom font
		_customFont = GD.Load<FontFile>("res://assets/fonts/BLKCHCRY.TTF");
		
		// Get nodes from scene
		GetNodesFromScene();
		SetupStyles();
		
		Hide(); // Hidden by default
		
		// Connect to LivesManager
		if (LivesManager.Instance != null)
		{
			LivesManager.Instance.LivesDepleted += OnLivesDepleted;
		}
		
		GD.Print("✅ GameOverScreen initialized and hidden");
	}
	
	private void GetNodesFromScene()
	{
		GD.Print("🔍 GameOverScreen.GetNodesFromScene() - Loading nodes...");
		
		_blackCover = GetNodeOrNull<ColorRect>("BlackCover");
		if (_blackCover == null) GD.PrintErr("❌ BlackCover not found!");
		else GD.Print("✅ BlackCover found");
		
		_messagePanel = GetNodeOrNull<Panel>("MessagePanel");
		if (_messagePanel == null) GD.PrintErr("❌ MessagePanel not found!");
		else GD.Print("✅ MessagePanel found");
		
		_gameOverLabel = GetNodeOrNull<Label>("MessagePanel/VBoxContainer/GameOverLabel");
		if (_gameOverLabel == null) GD.PrintErr("❌ GameOverLabel not found!");
		else GD.Print("✅ GameOverLabel found");
		
		_subLabel = GetNodeOrNull<Label>("MessagePanel/VBoxContainer/SubLabel");
		if (_subLabel == null) GD.PrintErr("❌ SubLabel not found!");
		else GD.Print("✅ SubLabel found");
		
		_mainMenuButton = GetNodeOrNull<Button>("MessagePanel/VBoxContainer/MainMenuButton");
		if (_mainMenuButton == null)
		{
			GD.PrintErr("❌ MainMenuButton not found!");
		}
		else
		{
			GD.Print("✅ MainMenuButton found: " + _mainMenuButton.Name);
			_mainMenuButton.Pressed += OnMainMenuPressed;
			GD.Print("✅ MainMenuButton.Pressed signal connected");
		}
		
		GD.Print("✅ GameOverScreen nodes loaded from scene");
	}
	
	private void SetupStyles()
	{
		// Panel style
		if (_messagePanel != null)
		{
			var panelStyle = new StyleBoxFlat();
			panelStyle.BgColor = new Color(0.05f, 0.05f, 0.05f, 0.98f);
			panelStyle.BorderColor = new Color(0.5f, 0.1f, 0.1f, 1);
			panelStyle.SetBorderWidthAll(4);
			panelStyle.SetCornerRadiusAll(15);
			_messagePanel.AddThemeStyleboxOverride("panel", panelStyle);
		}
		
		// Button styles
		if (_mainMenuButton != null)
		{
			var buttonNormal = new StyleBoxFlat();
			buttonNormal.BgColor = new Color(0.4f, 0.15f, 0.15f, 1);
			buttonNormal.SetCornerRadiusAll(8);
			_mainMenuButton.AddThemeStyleboxOverride("normal", buttonNormal);
			
			var buttonHover = new StyleBoxFlat();
			buttonHover.BgColor = new Color(0.6f, 0.2f, 0.2f, 1);
			buttonHover.SetCornerRadiusAll(8);
			_mainMenuButton.AddThemeStyleboxOverride("hover", buttonHover);
			
			var buttonPressed = new StyleBoxFlat();
			buttonPressed.BgColor = new Color(0.3f, 0.1f, 0.1f, 1);
			buttonPressed.SetCornerRadiusAll(8);
			_mainMenuButton.AddThemeStyleboxOverride("pressed", buttonPressed);
		}
	}
	
	private void OnLivesDepleted()
	{
		// This can be called from LivesManager, but we're handling it from FailScreen
		// So we don't need to do anything here unless called directly
	}
	
	public void FadeIn()
	{
		GD.Print("");
		GD.Print("═══════════════════════════════════════════");
		GD.Print("💀 GAME OVER SCREEN - FadeIn() called");
		GD.Print("═══════════════════════════════════════════");
		GD.Print("");
		
		Show();
		
		// Register to PanelManager to block pause menu
		if (PanelManager.Instance != null)
		{
			PanelManager.Instance.RegisterPanel(this);
			GD.Print("✅ GameOverScreen registered to PanelManager");
		}
		
		// Block inventory from opening
		InventoryUI.IsAnyPanelOpen = true;
		GD.Print("✅ Inventory blocked (IsAnyPanelOpen = true)");
		
		// Show cursor for button interaction
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		// Fade in animation
		var tween = CreateTween();
		tween.SetPauseMode(Tween.TweenPauseMode.Process); // Continue during pause
		
		// Fade black cover
		tween.TweenProperty(_blackCover, "color:a", 0.9f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		
		// Scale and fade in the panel
		_messagePanel.Scale = new Vector2(0.5f, 0.5f);
		_messagePanel.Modulate = new Color(1, 1, 1, 0);
		
		tween.Parallel().TweenProperty(_messagePanel, "scale", new Vector2(1, 1), 0.8f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		
		tween.Parallel().TweenProperty(_messagePanel, "modulate:a", 1.0f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		
		// Ensure game is paused
		GetTree().Paused = true;
		
		GD.Print("💀 Game Over displayed");
	}
	
	private void OnMainMenuPressed()
	{
		GD.Print("");
		GD.Print("═══════════════════════════════════════════════════════");
		GD.Print("🏠 🏠 🏠  BACK TO MAIN MENU BUTTON CLICKED!  🏠 🏠 🏠");
		GD.Print("═══════════════════════════════════════════════════════");
		GD.Print("");
		
		// Play button sound if available
		if (ButtonSoundManager.Instance != null)
		{
			ButtonSoundManager.Instance.PlayClickSound();
			GD.Print("🔊 Button sound played");
		}
		
		GD.Print("⏳ Calling FadeOutAndReturnToMenu()...");
		FadeOutAndReturnToMenu();
	}
	
	private void FadeOutAndReturnToMenu()
	{
		GD.Print("🌑 FadeOutAndReturnToMenu - Starting fade out...");
		
		var tween = CreateTween();
		tween.SetPauseMode(Tween.TweenPauseMode.Process);
		
		tween.TweenProperty(_blackCover, "color:a", 1.0f, 0.5f);
		tween.TweenCallback(Callable.From(() => 
		{
			GD.Print("🔄 Fade complete, unpausing and resetting...");
			
			// Unregister from PanelManager
			if (PanelManager.Instance != null)
			{
				PanelManager.Instance.UnregisterPanel(this);
				GD.Print("✅ GameOverScreen unregistered from PanelManager");
			}
			
			// Re-enable inventory
			InventoryUI.IsAnyPanelOpen = false;
			GD.Print("✅ Inventory re-enabled (IsAnyPanelOpen = false)");
			
			// Unpause the game
			GetTree().Paused = false;
			GD.Print("✅ Game unpaused");
			
			// Reset managers
			if (LivesManager.Instance != null)
			{
				LivesManager.Instance.ResetLives();
				GD.Print("✅ Lives reset to 3");
			}
			
			if (TimerManager.Instance != null)
			{
				TimerManager.Instance.StopTimer();
				GD.Print("✅ Timer stopped");
			}
			
			// Clear Settings references
			Settings.CurrentSource = Settings.SettingsSource.MainMenu;
			Settings.SetPauseMenuReference(null);
			GD.Print("🧹 Settings references cleared (GameOver to MainMenu)");
			
			// Return to main menu
			GD.Print("🏠 Loading MainMenu.tscn...");
			GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
			GD.Print("✅ Scene change requested");
		}));
	}
	
	public override void _ExitTree()
	{
		if (LivesManager.Instance != null)
		{
			LivesManager.Instance.LivesDepleted -= OnLivesDepleted;
		}
	}
}
