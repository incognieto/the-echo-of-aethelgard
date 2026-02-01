using Godot;
using System;
using System.Threading.Tasks;

// Pastikan nama class SAMA dengan nama file (Prologue.cs)
public partial class Prologue : Node3D
{
    [ExportGroup("Cinematic Components")]
    [Export] public AnimationPlayer DirectorAnim; // Mengontrol Kamera & Fade Layar
    [Export] public Camera3D MainCamera;          // Kamera Utama
    [Export] public MeshInstance3D PlayerVisual;  // Kapsul Player
    [Export] public Label DialogueText;           // Subtitle
    [Export] public ColorRect BlackCover;         // Layar Hitam (Fade)
    
    [ExportGroup("Environment")]
    [Export] public WorldEnvironment WorldEnv;    // Untuk efek Blur/Focus

    [ExportGroup("Transition")]
    [Export(PropertyHint.File, "*.tscn")] public string NextScenePath; // Path ke Main.tscn

    public override async void _Ready()
    {
        // 1. SETUP VISUAL AWAL
        if (DialogueText != null) DialogueText.Text = "";
        if (BlackCover != null) BlackCover.Color = new Color(0, 0, 0, 1); // Gelap gulita
        
        // Posisikan Kapsul "Tidur" (Rebah di lantai)
        if (PlayerVisual != null)
            PlayerVisual.RotationDegrees = new Vector3(90, 0, 0);

        GD.Print("🎬 Prologue Started. Action!");

        // 2. MAINKAN ANIMASI KAMERA & AUDIO
        if (DirectorAnim != null)
            DirectorAnim.Play("CinematicSequence");

        // 3. SEQUENCING NARASI (Sinkronisasi Manual)
        
        // Detik 0-2: Hening / Suara Air (Layar masih hitam di animasi)
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        ShowDialogue("Narator: 'Mereka bilang, masa depan adalah misteri...'");

        // Detik 6: Kamera sedang bergerak turun
        await ToSignal(GetTree().CreateTimer(4.0f), "timeout");
        ShowDialogue("Narator: 'Namun di Ironfang, takdirmu sudah dikunci...'");

        // Detik 10: Kamera sampai di posisi bawah
        await ToSignal(GetTree().CreateTimer(4.0f), "timeout");
        ShowDialogue("Narator: 'Kecuali kamu memilih untuk menulis ulang.'");

        // Detik 12: Player Bangun
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        PlayerWakeUp();
    }

    private void PlayerWakeUp()
    {
        GD.Print("🧍 Kapsul Bangun");
        
        // Animasi Kapsul Berdiri (Tweening)
        if (PlayerVisual != null)
        {
            Tween tween = CreateTween();
            // Putar dari 90 (tidur) ke 0 (berdiri) dalam 2 detik
            tween.TweenProperty(PlayerVisual, "rotation_degrees:x", 0.0f, 2.0f)
                .SetTrans(Tween.TransitionType.Bounce) // Efek membal sedikit saat berdiri (opsional)
                .SetEase(Tween.EaseType.Out);
            
            // Setelah berdiri, selesai
            tween.TweenCallback(Callable.From(EndPrologue));
        }
    }

    private void ShowDialogue(string text)
    {
        if (DialogueText != null) DialogueText.Text = text;
    }

    private void EndPrologue()
    {
        // Tunggu sebentar baca dialog terakhir
        GetTree().CreateTimer(3.0f).Timeout += () => 
        {
            GD.Print("🎬 Cutscene Selesai. Pindah Scene.");
            if (!string.IsNullOrEmpty(NextScenePath))
            {
                GetTree().ChangeSceneToFile(NextScenePath);
            }
            else
            {
                GD.PrintErr("⚠️ NextScenePath belum diisi di Inspector!");
            }
        };
    }
}