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
	[Export] public AudioStreamPlayer ChainSFX;       // SFX rantai ditarik
	[Export] public AudioStreamPlayer AmbienceBGM;    // Ambient dungeon

	public override async void _Ready()
	{
		// 1. SETUP VISUAL AWAL
		if (DialogueText != null) DialogueText.Text = "";
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

		// 3. SEQUENCING NARASI (20 DETIK TOTAL - 5 CAMERA)
		
		// === CAM 1: Layar Hitam (0-2 detik) ===
		await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
		if (ChainSFX != null) ChainSFX.Play();
		
		await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
		ShowDialogue("Narator: \"Masa depan di Ironfang sudah bocor sebelum kamu bangun.\"");
		
		// === CAM 2: Wide Shot Reveal (2-6 detik) ===
		await ToSignal(GetTree().CreateTimer(4.0f), "timeout");
		ShowDialogue("Narator: \"Raja Valerius mengunci tubuhmu... tapi lupa satu hal.\"");
		PlayerWakeUp();

		
		// === CAM 3: Player Wake Up (6-10 detik) ===
		await ToSignal(GetTree().CreateTimer(4.0f), "timeout");
		ShowDialogue("Narator: \"Ksatria revolusi meninggalkan sesuatu untukmu.\"");
		
		// === CAM 4: Grimoire Focus (10-15 detik) ===
		await ToSignal(GetTree().CreateTimer(5.0f), "timeout");
		ShowDialogue("The Grimoire: \"Selamat datang, sang pengubah naskah.\"");
		ApplyScreenShake();
		
		// === CAM 5: Close-Up Spoiler (15-20 detik) ===
		await ToSignal(GetTree().CreateTimer(5.0f), "timeout");
		ShowDialogue("The Grimoire: \"Spoiler: Di balik pintu itu, panah menunggumu.\"");
		
		// === Ending: Fade Out (20-22 detik) ===
		await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
		ShowDialogue("");
		
		await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
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
}
