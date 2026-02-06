using Godot;
using System;

/// <summary>
/// Fail Screen yang muncul ketika waktu habis
/// "You were caught by the guards" + Respawn button
/// Now uses scene-based UI instead of programmatic creation
/// </summary>
public partial class FailScreen : Control
{
	private ColorRect _blackCover;
	private Panel _messagePanel;
	private Label _messageLabel;
	private Button _respawnButton;
	private FontFile _customFont;
	
	[Signal]
	public delegate void RespawnRequestedEventHandler();
	
	public override void _Ready()
	{
		GD.Print("🎬 FailScreen _Ready() called");
		
		// CRITICAL: Set ProcessMode to Always so it can receive input when paused
		ProcessMode = ProcessModeEnum.Always;
		
		// Load custom font
		_customFont = GD.Load<FontFile>("res://assets/fonts/BLKCHCRY.TTF");
		
		// Get nodes from scene
		GetNodesFromScene();
		SetupStyles();
		
		// IMPORTANT: Start hidden
		Visible = false;
		
		// Connect to TimerManager
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.TimeUp += OnTimeUp;
			GD.Print("✅ FailScreen connected to TimerManager.TimeUp");
		}
		else
		{
			GD.PrintErr("❌ TimerManager not found!");
		}
		
		GD.Print("⚠️ FailScreen initialized and hidden");
	}
	
	private void GetNodesFromScene()
	{
		GD.Print("🔍 FailScreen.GetNodesFromScene() - Loading nodes...");
		
		_blackCover = GetNodeOrNull<ColorRect>("BlackCover");
		if (_blackCover == null) GD.PrintErr("❌ BlackCover not found!");
		else GD.Print("✅ BlackCover found");
		
		_messagePanel = GetNodeOrNull<Panel>("MessagePanel");
		if (_messagePanel == null) GD.PrintErr("❌ MessagePanel not found!");
		else GD.Print("✅ MessagePanel found");
		
		_messageLabel = GetNodeOrNull<Label>("MessagePanel/VBoxContainer/MessageLabel");
		if (_messageLabel == null) GD.PrintErr("❌ MessageLabel not found!");
		else GD.Print("✅ MessageLabel found");
		
		_respawnButton = GetNodeOrNull<Button>("MessagePanel/VBoxContainer/RespawnButton");
		if (_respawnButton == null)
		{
			GD.PrintErr("❌ RespawnButton not found!");
		}
		else
		{
			GD.Print("✅ RespawnButton found: " + _respawnButton.Name);
			_respawnButton.Pressed += OnRespawnPressed;
			GD.Print("✅ RespawnButton.Pressed signal connected");
		}
		
		GD.Print("✅ FailScreen nodes loaded from scene");
	}
	
	private void SetupStyles()
	{
		// Apply custom font
		if (_customFont != null)
		{
			if (_messageLabel != null)
			{
				_messageLabel.AddThemeFontOverride("font", _customFont);
			}
			if (_respawnButton != null)
			{
				_respawnButton.AddThemeFontOverride("font", _customFont);
			}
		}
		
		// Panel style
		if (_messagePanel != null)
		{
			var panelStyle = new StyleBoxFlat();
			panelStyle.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
			panelStyle.BorderColor = new Color(0.8f, 0.2f, 0.2f, 1);
			panelStyle.SetBorderWidthAll(3);
			panelStyle.SetCornerRadiusAll(10);
			_messagePanel.AddThemeStyleboxOverride("panel", panelStyle);
		}
		
		// Button styles
		if (_respawnButton != null)
		{
			var buttonNormal = new StyleBoxFlat();
			buttonNormal.BgColor = new Color(0.3f, 0.1f, 0.1f, 1);
			buttonNormal.SetCornerRadiusAll(8);
			_respawnButton.AddThemeStyleboxOverride("normal", buttonNormal);
			
			var buttonHover = new StyleBoxFlat();
			buttonHover.BgColor = new Color(0.5f, 0.2f, 0.2f, 1);
			buttonHover.SetCornerRadiusAll(8);
			_respawnButton.AddThemeStyleboxOverride("hover", buttonHover);
			
			var buttonPressed = new StyleBoxFlat();
			buttonPressed.BgColor = new Color(0.2f, 0.05f, 0.05f, 1);
			buttonPressed.SetCornerRadiusAll(8);
			_respawnButton.AddThemeStyleboxOverride("pressed", buttonPressed);
		}
	}
	
	private void OnTimeUp()
	{
		GD.Print("⏰ FailScreen.OnTimeUp() - Timer expired!");
		
		// Check if this is the last life (nyawa terakhir)
		if (LivesManager.Instance != null && LivesManager.Instance.CurrentLives == 1)
		{
			// Last life - langsung ke Game Over tanpa tampilkan FailScreen
			GD.Print("💀 Last life detected (nyawa = 1) - going directly to Game Over");
			ShowGameOverDirectly();
		}
		else
		{
			// Masih ada nyawa untuk di-respawn - tampilkan fail screen
			GD.Print($"💚 Lives remaining: {LivesManager.Instance?.CurrentLives ?? 0} - showing fail screen");
			FadeIn();
		}
	}
	
	private void FadeIn()
	{
		GD.Print("📺 FailScreen.FadeIn() - Showing fail screen");
		Visible = true;
		
		// Register to PanelManager to block pause menu
		if (PanelManager.Instance != null)
		{
			PanelManager.Instance.RegisterPanel(this);
			GD.Print("✅ FailScreen registered to PanelManager");
		}
		
		// Block inventory from opening
		InventoryUI.IsAnyPanelOpen = true;
		GD.Print("✅ Inventory blocked (IsAnyPanelOpen = true)");
		
		// Show cursor for button interaction
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		// Reset alpha for animation
		if (_blackCover != null)
		{
			_blackCover.Color = new Color(0, 0, 0, 0);
		}
		
		// Fade in animation
		var tween = CreateTween();
		tween.SetPauseMode(Tween.TweenPauseMode.Process); // Continue during pause
		
		// Fade black cover
		tween.TweenProperty(_blackCover, "color:a", 0.85f, 0.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		
		// Scale in the panel
		_messagePanel.Scale = new Vector2(0.8f, 0.8f);
		_messagePanel.Modulate = new Color(1, 1, 1, 0);
		
		tween.Parallel().TweenProperty(_messagePanel, "scale", new Vector2(1, 1), 0.5f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		
		tween.Parallel().TweenProperty(_messagePanel, "modulate:a", 1.0f, 0.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		
		// Pause the game
		GetTree().Paused = true;
	}
	
	private void OnRespawnPressed()
	{
		GD.Print("");
		GD.Print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
		GD.Print("🔘 🔘 🔘  RESPAWN BUTTON CLICKED!  🔘 🔘 🔘");
		GD.Print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
		GD.Print("");
		
		// Play button sound if available
		if (ButtonSoundManager.Instance != null)
		{
			ButtonSoundManager.Instance.PlayClickSound();
			GD.Print("🔊 Button sound played");
		}
		
		// Check if player has lives remaining
		if (LivesManager.Instance != null)
		{
			GD.Print($"💚 Current lives BEFORE losing: {LivesManager.Instance.CurrentLives}");
			GD.Print($"❓ Has lives remaining? {LivesManager.Instance.HasLivesRemaining()}");
			
			if (LivesManager.Instance.HasLivesRemaining())
			{
				// Lose a life
				LivesManager.Instance.LoseLife();
				GD.Print($"💔 Life lost. Remaining lives: {LivesManager.Instance.CurrentLives}");
				
				// Check again after losing life
				if (LivesManager.Instance.HasLivesRemaining())
				{
					// Still has lives, respawn
					GD.Print("✅ Still has lives, calling FadeOutAndRespawn()...");
					FadeOutAndRespawn();
				}
				else
				{
					// No more lives, show game over
					GD.Print("❌ No more lives, calling FadeOutAndShowGameOver()...");
					FadeOutAndShowGameOver();
				}
			}
			else
			{
				// No lives at all, show game over
				GD.Print("❌ No lives remaining, calling FadeOutAndShowGameOver()...");
				FadeOutAndShowGameOver();
			}
		}
		else
		{
			// No lives manager, show game over
			GD.PrintErr("❌ LivesManager not found! Showing game over...");
			FadeOutAndShowGameOver();
		}
	}
	
	private void FadeOutAndRespawn()
	{
		GD.Print("♻️ FadeOutAndRespawn - preparing to respawn");
		
		var tween = CreateTween();
		tween.SetPauseMode(Tween.TweenPauseMode.Process);
		
		tween.TweenProperty(_messagePanel, "modulate:a", 0.0f, 0.3f);
		tween.TweenCallback(Callable.From(() => 
		{
			Visible = false;
			GetTree().Paused = false;
			
			// Unregister from PanelManager
			if (PanelManager.Instance != null)
			{
				PanelManager.Instance.UnregisterPanel(this);
				GD.Print("✅ FailScreen unregistered from PanelManager");
			}
			
			// Re-enable inventory
			InventoryUI.IsAnyPanelOpen = false;
			GD.Print("✅ Inventory re-enabled (IsAnyPanelOpen = false)");
			
			// Hide cursor back to captured mode
			Input.MouseMode = Input.MouseModeEnum.Captured;
			
			// Restart timer from the beginning
			if (TimerManager.Instance != null)
			{
				string levelName = TimerManager.Instance.CurrentLevelName;
				float timeLimit = TimerManager.Instance.LevelTimeLimit;
				TimerManager.Instance.StartTimer(timeLimit, levelName);
				GD.Print($"⏱️ Timer restarted: {timeLimit}s for {levelName}");
			}
			
			// Emit signal for level to handle respawn (reset player position, etc.)
			EmitSignal(SignalName.RespawnRequested);
			
			GD.Print("✅ Respawn complete, signal emitted");
		}));
	}
	
	private void FadeOutAndShowGameOver()
	{
		GD.Print("💀 FadeOutAndShowGameOver - no lives remaining");
		
		var tween = CreateTween();
		tween.SetPauseMode(Tween.TweenPauseMode.Process);
		
		tween.TweenProperty(_messagePanel, "modulate:a", 0.0f, 0.3f);
		tween.TweenCallback(Callable.From(() => 
		{
			Visible = false;
			
			// Show Game Over Screen - cari di parent (UI node)
			var gameOverScreen = GetParent().GetNodeOrNull<GameOverScreen>("GameOverScreen");
			if (gameOverScreen != null)
			{
				gameOverScreen.Show();
				gameOverScreen.FadeIn();
			}
			else
			{
				GD.PrintErr("⚠️ GameOverScreen not found in UI!");
				GetTree().Paused = false;
			}
		}));
	}
	
	private void ShowGameOverDirectly()
	{
		GD.Print("💀 ShowGameOverDirectly - skipping fail screen, going to game over");
		
		// Lose the last life
		if (LivesManager.Instance != null)
		{
			LivesManager.Instance.LoseLife();
			GD.Print($"💔 Last life lost. Lives now: {LivesManager.Instance.CurrentLives}");
		}
		
		// Pause the game
		GetTree().Paused = true;
		
		// Show cursor for UI interaction
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		// Show Game Over Screen directly
		var gameOverScreen = GetParent().GetNodeOrNull<GameOverScreen>("GameOverScreen");
		if (gameOverScreen != null)
		{
			gameOverScreen.Show();
			gameOverScreen.FadeIn();
			GD.Print("✅ Game Over screen shown directly");
		}
		else
		{
			GD.PrintErr("⚠️ GameOverScreen not found in UI!");
			GetTree().Paused = false;
		}
	}
	
	public override void _ExitTree()
	{
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.TimeUp -= OnTimeUp;
		}
	}
}
