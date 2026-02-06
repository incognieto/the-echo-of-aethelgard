using Godot;
using System;

/// <summary>
/// HUD untuk menampilkan Timer dan Lives di gameplay
/// Positioned: Timer (kanan atas), Lives (kanan atas bawah timer)
/// 
/// CARA EDIT LAYOUT VIA GODOT EDITOR:
/// 1. Buka scenes/ui/GameHUD.tscn di Godot Editor
/// 2. Pilih node "TimerPanel" atau "LivesPanel"
/// 3. Di Inspector, ubah:
///    - Position: Geser panel ke posisi yang diinginkan
///    - Size: Ubah ukuran panel
///    - Theme Overrides → Styles → Panel: Ubah warna, border, dll
/// 4. Pilih "TimerLabel" atau "HeartX" untuk ubah:
///    - Font Size (Theme Overrides → Font Sizes)
///    - Font Color (Theme Overrides → Colors)
///    - Alignment (Horizontal Alignment)
///    - Texture (untuk heart sprites)
///    - Custom Minimum Size (untuk ukuran heart)
/// 5. Pilih "HeartsContainer" untuk ubah spacing antar heart
/// 6. Ctrl+S untuk save
/// 
/// SEMUA UI SUDAH ADA DI SCENE FILE - TIDAK DIBUAT VIA SCRIPT
/// </summary>
public partial class GameHUD : Control
{
	// UI Elements (auto-fetched from scene)
	private Label _timerLabel;
	private Panel _timerPanel;
	private Panel _livesPanel;
	
	// Heart sprites (already in scene)
	private TextureRect _heart1;
	private TextureRect _heart2;
	private TextureRect _heart3;
	
	public override void _Ready()
	{
		GD.Print("🎮 GameHUD _Ready() called");
		
		// Get all nodes from scene tree
		GetNodesFromScene();
		
		// Connect to managers
		ConnectToManagers();
		
		GD.Print("🎮 GameHUD initialized (all UI from scene file)");
	}
	
	private void GetNodesFromScene()
	{
		GD.Print("🔍 GameHUD.GetNodesFromScene() - Loading nodes...");
		
		// Get panels
		_timerPanel = GetNodeOrNull<Panel>("TimerPanel");
		if (_timerPanel == null) GD.PrintErr("❌ TimerPanel not found!");
		else GD.Print("✅ TimerPanel found");
		
		_livesPanel = GetNodeOrNull<Panel>("LivesPanel");
		if (_livesPanel == null) GD.PrintErr("❌ LivesPanel not found!");
		else GD.Print("✅ LivesPanel found");
		
		// Get timer label
		_timerLabel = GetNodeOrNull<Label>("TimerPanel/TimerVBox/TimerLabel");
		if (_timerLabel == null) GD.PrintErr("❌ TimerLabel not found!");
		else GD.Print("✅ TimerLabel found");
		
		// Get heart sprites
		_heart1 = GetNodeOrNull<TextureRect>("LivesPanel/LivesVBox/HeartsContainer/Heart1");
		if (_heart1 == null) GD.PrintErr("❌ Heart1 not found!");
		else GD.Print("✅ Heart1 found");
		
		_heart2 = GetNodeOrNull<TextureRect>("LivesPanel/LivesVBox/HeartsContainer/Heart2");
		if (_heart2 == null) GD.PrintErr("❌ Heart2 not found!");
		else GD.Print("✅ Heart2 found");
		
		_heart3 = GetNodeOrNull<TextureRect>("LivesPanel/LivesVBox/HeartsContainer/Heart3");
		if (_heart3 == null) GD.PrintErr("❌ Heart3 not found!");
		else GD.Print("✅ Heart3 found");
		
		GD.Print("✅ GameHUD nodes loaded from scene");
	}
	
	private void ConnectToManagers()
	{
		// Connect to TimerManager
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.TimeChanged += OnTimeChanged;
			UpdateTimerDisplay(TimerManager.Instance.CurrentTime);
		}
		else
		{
			GD.PrintErr("⚠️ TimerManager not found! Make sure it's in autoload.");
		}
		
		// Connect to LivesManager
		if (LivesManager.Instance != null)
		{
			LivesManager.Instance.LivesChanged += OnLivesChanged;
			UpdateLivesDisplay(LivesManager.Instance.CurrentLives);
		}
		else
		{
			GD.PrintErr("⚠️ LivesManager not found! Make sure it's in autoload.");
		}
	}
	
	private void OnTimeChanged(float remainingTime)
	{
		UpdateTimerDisplay(remainingTime);
		
		// Warning effect when time is low (< 30 seconds)
		if (remainingTime <= 30 && remainingTime > 0)
		{
			// Flash red
			if (_timerLabel != null)
			{
				_timerLabel.AddThemeColorOverride("font_color", new Color(1, 0, 0, 1));
			}
		}
		else if (remainingTime > 30)
		{
			// Normal yellow
			if (_timerLabel != null)
			{
				_timerLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0.5f, 1));
			}
		}
	}
	
	private void UpdateTimerDisplay(float time)
	{
		if (_timerLabel == null) return;
		
		int minutes = Mathf.FloorToInt(time / 60);
		int seconds = Mathf.FloorToInt(time % 60);
		_timerLabel.Text = $"{minutes:00}:{seconds:00}";
	}
	
	private void OnLivesChanged(int newLives)
	{
		UpdateLivesDisplay(newLives);
	}
	
	private void UpdateLivesDisplay(int lives)
	{
		// Update heart sprites visibility/opacity based on lives
		// Heart 1 (first heart)
		if (_heart1 != null)
		{
			if (lives >= 1)
				_heart1.Modulate = new Color(1, 1, 1, 1); // Full opacity
			else
				_heart1.Modulate = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Dimmed
		}
		
		// Heart 2 (second heart)
		if (_heart2 != null)
		{
			if (lives >= 2)
				_heart2.Modulate = new Color(1, 1, 1, 1); // Full opacity
			else
				_heart2.Modulate = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Dimmed
		}
		
		// Heart 3 (third heart)
		if (_heart3 != null)
		{
			if (lives >= 3)
				_heart3.Modulate = new Color(1, 1, 1, 1); // Full opacity
			else
				_heart3.Modulate = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Dimmed
		}
		
		GD.Print($"♥️ Lives display updated: {lives}/3");
	}
	
	public override void _ExitTree()
	{
		// Disconnect signals
		if (TimerManager.Instance != null)
		{
			TimerManager.Instance.TimeChanged -= OnTimeChanged;
		}
		
		if (LivesManager.Instance != null)
		{
			LivesManager.Instance.LivesChanged -= OnLivesChanged;
		}
	}
}
