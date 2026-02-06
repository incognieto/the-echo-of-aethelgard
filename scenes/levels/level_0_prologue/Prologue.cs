using Godot;
using System;
using System.Threading.Tasks;

// Pastikan nama class SAMA dengan nama file (Prologue.cs)
public partial class Prologue : Node3D
{
	[ExportGroup("Cinematic Components")]
	[Export] public AnimationPlayer DirectorAnim; // Mengontrol Kamera & Fade Layar
	[Export] public Camera3D MainCamera;          // Kamera Utama
	[Export] public Node3D PlayerVisual;  // Kapsul Player
	[Export] public Label DialogueText;           // Subtitle
	[Export] public ColorRect BlackCover;         // Layar Hitam (Fade)
	
	[ExportGroup("Environment")]
	[Export] public WorldEnvironment WorldEnv;    // Untuk efek Blur/Focus

	[ExportGroup("Transition")]
	[Export(PropertyHint.File, "*.tscn")] public string NextScenePath; // Path ke Main.tscn
	
	[ExportGroup("Audio (Manual Setup)")]
	[Export] public AudioStreamPlayer WaterDripSFX;   // SFX tetesan air
	[Export] public AudioStreamPlayer AmbienceBGM;    // Ambient dungeon (dungeon ambient.mp3)
	[Export] public AudioStreamPlayer DialogueTypeSFX; // SFX typewriter (loop selama typing)
	[Export] public AudioStreamPlayer3D GrimoireMagicSFX; // SFX magic 3D spatial (posisi di grimoire)
	[Export] public AudioStreamPlayer HeartbeatSFX;    // SFX heartbeat untuk tension
	
	[ExportGroup("Scene References")]
	[Export] public Node3D GrimoirePosition; // Reference ke posisi Grimoire untuk spatial audio

	public override async void _Ready()
	{
		// DISABLE background music untuk Prologue - hanya gunakan SFX & ambience
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.StopMusic(); // Stop BGM
		}
		
		// 1. SETUP VISUAL AWAL
		SetupDialogueText();
		
		if (BlackCover != null) 
		{
			BlackCover.Color = new Color(0, 0, 0, 1); // Layar hitam total
			BlackCover.Modulate = new Color(1, 1, 1, 1); // Alpha 1.0 untuk animation fade
		}
		
		// Posisikan Player "Tidur" (Rebah di lantai)
		if (PlayerVisual != null)
			PlayerVisual.RotationDegrees = new Vector3(90, 0, 0);

		GD.Print("🎬 Prologue: The Echo of Aethelgard - Action!");

		// 2. PLAY AUDIO & ANIMATION
		if (AmbienceBGM != null) AmbienceBGM.Play();
		if (WaterDripSFX != null) WaterDripSFX.Play();
		
		// Start animation dari awal (ini yang handle BlackCover fade)
		if (DirectorAnim != null)
			DirectorAnim.Play("CinematicSequence");

		// 3. SEQUENCING NARASI - TIMING SYNC DENGAN ANIMATION DIRECTOR
		// Animation Timeline:
		// - Angle 1: 0-5.53s (Layar hitam + reveal)
		// - Angle 2: 5.65-10.06s (Wide shot player)
		// - Angle 3: 10.06-14.76s (Grimoire focus dengan zoom)
		// - Angle 4: 14.76-19.5s (Close-up grimoire)
		// - Angle 5: 19.5-23.01s (Final shot)
		// - Fade out: 22.6-23.06s
		
		// === ANGLE 1: Layar Hitam + Reveal (0-5.53s) ===
		await ToSignal(GetTree().CreateTimer(2.0f), "timeout"); // 2s
		_ = ShowDialogueTyped("The Grimoire: \"Ironfang's fate was sealed before you awoke.\""); // Start typing (fire and forget)
		
		await ToSignal(GetTree().CreateTimer(3.53f), "timeout"); // Total: 5.53s - ANGLE 2 SWITCH
		
		// === ANGLE 2: Wide Shot Player (5.65-10.06s) ===
		_ = ShowDialogueTyped("The Grimoire: \"King Valerius bound your flesh... but forgot one thing.\""); // LANGSUNG typing
		
		// PlayerWakeUp setelah angle 2 settle (delay 0.6s) supaya tidak terlihat jelas rotasinya
		await ToSignal(GetTree().CreateTimer(0.6f), "timeout"); // Total: 6.13s
		PlayerWakeUp(); // Animasi bangun dimulai saat kamera sudah di angle 2
		
		await ToSignal(GetTree().CreateTimer(3.93f), "timeout"); // Total: 10.06s - ANGLE 3 SWITCH
		
		// === ANGLE 3: Grimoire Focus (10.06-14.76s) ===
		_ = ShowDialogueTyped("The Grimoire: \"The rebel knights left their final gift for you.\""); // LANGSUNG typing
		
		await ToSignal(GetTree().CreateTimer(4.7f), "timeout"); // Total: 14.76s - ANGLE 4 SWITCH
		
		// === ANGLE 4: Close-Up Grimoire (14.76-19.5s) ===
		if (GrimoireMagicSFX != null) GrimoireMagicSFX.Play(); // Magic sound saat grimoire "bangun"
		_ = ShowDialogueTyped("The Grimoire: \"Welcome, weaver of fates.\""); // LANGSUNG typing
		ApplyScreenShake();
		
		await ToSignal(GetTree().CreateTimer(4.74f), "timeout"); // Total: 19.5s - ANGLE 5 SWITCH
		
		// === ANGLE 5: Final Shot - Spoiler (19.5-23.01s) ===
		if (HeartbeatSFX != null) HeartbeatSFX.Play(); // Tension build
		_ = ShowDialogueTyped("The Grimoire: \"Spoiler: Beyond that door... six seals await your answer.\""); // LANGSUNG typing
		
		await ToSignal(GetTree().CreateTimer(3.1f), "timeout"); // Total: 22.6s - FADE OUT
		
		// === Clear & Fade Out (22.6-23.06s) ===
		ShowDialogue(""); // Clear dialogue
		
		await ToSignal(GetTree().CreateTimer(0.5f), "timeout"); // Total: 23.1s
		EndPrologue();
	}

	private void PlayerWakeUp()
	{
		GD.Print("🧍 Player Bangun - Mendekati Grimoire");
		
		// Animasi Kapsul Berdiri (Tweening - lebih smooth)
		if (PlayerVisual != null)
		{
			Tween tween = CreateTween();
			tween.SetParallel(false);
			
			// Fase 1: Gerak bangun perlahan (seperti tersadar)
			tween.TweenProperty(PlayerVisual, "rotation_degrees:x", 45.0f, 1.5f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			
			// Fase 2: Berdiri penuh
			tween.TweenProperty(PlayerVisual, "rotation_degrees:x", 0.0f, 1.5f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
		}
	}

	private async Task ShowDialogueTyped(string text)
	{
		if (DialogueText == null) return;
		
		DialogueText.Text = "";
		GD.Print($"💬 {text}");
		
		// Start typing sound effect (akan loop selama typing)
		if (DialogueTypeSFX != null)
		{
			DialogueTypeSFX.Play();
		}
		
		// Typewriter effect - TYPING SPEED DIPERCEPAT
		foreach (char c in text)
		{
			DialogueText.Text += c;
			
			// Delay per huruf DIPERCEPAT: 30ms = 33 karakter per detik (dari 50ms)
			await ToSignal(GetTree().CreateTimer(0.03f), "timeout");
		}
		
		// Stop typing sound setelah selesai
		if (DialogueTypeSFX != null)
		{
			DialogueTypeSFX.Stop();
		}
	}
	
	private void ShowDialogue(string text)
	{
		if (DialogueText != null)
		{
			DialogueText.Text = text;
			GD.Print($"💬 {text}");
		}
	}
	
	private void ApplyScreenShake()
	{
		// Simple screen shake dengan camera offset
		if (MainCamera == null) return;
		
		Vector3 originalPos = MainCamera.Position;
		Tween tween = CreateTween();
		tween.SetLoops(6); // Shake 6 kali
		
		// Shake ke kanan
		tween.TweenProperty(MainCamera, "position:x", originalPos.X + 0.05f, 0.05f);
		// Shake ke kiri
		tween.TweenProperty(MainCamera, "position:x", originalPos.X - 0.05f, 0.05f);
		// Kembali normal
		tween.TweenProperty(MainCamera, "position", originalPos, 0.1f);
	}

	private void EndPrologue()
	{
		GD.Print("🎬 Cutscene Selesai. Memulai Gameplay...");
		
		// Fade to black
		if (BlackCover != null)
		{
			Tween fade = CreateTween();
			fade.TweenProperty(BlackCover, "modulate:a", 1.0f, 2.0f);
			fade.TweenCallback(Callable.From(() => 
			{
				if (!string.IsNullOrEmpty(NextScenePath))
				{
					GetTree().ChangeSceneToFile(NextScenePath);
				}
				else
				{
					GD.PrintErr("⚠️ NextScenePath belum diisi di Inspector!");
				}
			}));
		}
	}
	
	private void SetupDialogueText()
	{
		if (DialogueText == null) return;
		
		// Clear text
		DialogueText.Text = "";
		
		// Set positioning - CENTER BOTTOM
		DialogueText.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		DialogueText.HorizontalAlignment = HorizontalAlignment.Center;
		DialogueText.VerticalAlignment = VerticalAlignment.Bottom;
		
		// Set margin dari bottom (jarak dari bawah layar)
		DialogueText.OffsetBottom = -30; // 30 pixel dari bawah (lebih dekat ke bawah)
		DialogueText.OffsetTop = -120; // Height area text
		
		// Styling
		DialogueText.AddThemeFontSizeOverride("font_size", 22); // Font sedikit lebih besar dari default (16)
		DialogueText.AddThemeColorOverride("font_color", Colors.White);
		DialogueText.AddThemeColorOverride("font_outline_color", Colors.Black);
		DialogueText.AddThemeConstantOverride("outline_size", 8); // Outline untuk readability
		
		// Autowrap untuk text panjang
		DialogueText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		
		GD.Print("✓ Dialogue text positioned: Center Bottom, Font Size 22");
	}
}
