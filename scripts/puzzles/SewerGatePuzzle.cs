using Godot;
using System;
using System.Collections.Generic;

public partial class SewerGatePuzzle : Node3D
{
    [ExportGroup("Settings")]
    [Export] public float TargetWeight = 75.0f;
    [Export] public Node3D GateObject; // Referensi ke Gerbang Visual
    [Export] public float MaxGateHeight = 4.0f; // Ketinggian maksimal gerbang (saat 75kg)
    [Export] public AnimationPlayer CameraAnimationPlayer; // Referensi ke AnimationPlayer untuk Gate_Cam
    
    [ExportGroup("Feedback")]
    [Export] public Label3D StatusLabel; // Teks angka di atas gerbang
    
    [ExportGroup("Audio")]
    [Export] public AudioStreamPlayer3D GateRisingSound; // Suara gate naik
    [Export] public AudioStreamPlayer3D GateOverloadSound; // Suara gate rusak/crash

    private float _currentWeight = 0.0f;
    private bool _isSolved = false;
    private bool _isBroken = false; // Gate rusak jika overload
    private bool _isDropping = false; // Flag untuk animasi jatuh cepat saat overload
    private Vector3 _gateClosedPos;
    private Vector3 _gateTargetPos;

    public override void _Ready()
    {
        if (GateObject != null)
        {
            _gateClosedPos = GateObject.Position;
            _gateTargetPos = _gateClosedPos;
        }
        
        // Setup audio nodes jika belum ada di scene
        SetupAudio();
        
        UpdateLabel();
    }
    
    private void SetupAudio()
    {
        // Setup Gate Rising Sound
        if (GateRisingSound == null)
        {
            GateRisingSound = new AudioStreamPlayer3D();
            GateRisingSound.Name = "GateRisingSound";
            GateRisingSound.Bus = "SFX";
            GateRisingSound.MaxDistance = 30.0f;
            GateRisingSound.VolumeDb = 2.0f; // Sedikit lebih keras
            AddChild(GateRisingSound);
        }
        
        // Load Gate Open sound
        var gateOpenStream = GD.Load<AudioStream>("res://assets/sfx/Gate Open.mp3");
        if (gateOpenStream != null)
        {
            GateRisingSound.Stream = gateOpenStream;
            GD.Print("✓ Gate Open sound loaded");
        }
        else
        {
            GD.PrintErr("✗ Failed to load Gate Open.mp3");
        }
        
        // Setup Gate Overload/Smash Sound
        if (GateOverloadSound == null)
        {
            GateOverloadSound = new AudioStreamPlayer3D();
            GateOverloadSound.Name = "GateOverloadSound";
            GateOverloadSound.Bus = "SFX";
            GateOverloadSound.MaxDistance = 40.0f;
            GateOverloadSound.VolumeDb = 5.0f; // Lebih keras untuk impact
            AddChild(GateOverloadSound);
        }
        
        // Load Gate Smash sound
        var gateSmashStream = GD.Load<AudioStream>("res://assets/sfx/Gate Smash.mp3");
        if (gateSmashStream != null)
        {
            GateOverloadSound.Stream = gateSmashStream;
            GD.Print("✓ Gate Smash sound loaded");
        }
        else
        {
            GD.PrintErr("✗ Failed to load Gate Smash.mp3");
        }
    }

    // Hubungkan ini ke signal BodyEntered dari Area3D Scale
    public void OnScaleBodyEntered(Node3D body)
    {
        // Jika puzzle sudah rusak, tidak bisa ditambah lagi
        if (_isBroken)
        {
            GD.Print("[PUZZLE] Gate rusak! Tidak bisa menambah berat lagi.");
            return;
        }
        
        GD.Print($"[SENSOR] Mendeteksi: {body.Name}");

        if (body is DroppedItem droppedItem)
        {
            string id = droppedItem.GetItemData().ItemId;
            float weight = GetWeightFromId(id);
            
            GD.Print($"[HITUNG] Item: {id} | Berat: {weight} kg");

            _currentWeight += weight;
            UpdateLabel();
            
            // Play gate rising sound SEBELUM cek puzzle (saat weight bertambah, gate akan naik)
            if (GateRisingSound != null && !GateRisingSound.Playing && _currentWeight <= TargetWeight)
            {
                GateRisingSound.Play();
                GD.Print("🔊 Playing Gate Open sound");
            }
            
            CheckPuzzle();
            
            // Play camera animation when rock is placed
            if (CameraAnimationPlayer != null && CameraAnimationPlayer.HasAnimation("Gate_Cam"))
            {
                CameraAnimationPlayer.Play("Gate_Cam");
                GD.Print("[ANIMATION] Playing Gate_Cam animation");
            }
        }
    }

    // Hubungkan ini ke signal BodyExited
    public void OnScaleBodyExited(Node3D body)
    {
        // Jika puzzle sudah rusak, tidak bisa dikurangi lagi
        if (_isBroken)
        {
            return;
        }
        
        if (body is DroppedItem droppedItem)
        {
            float weight = GetWeightFromId(droppedItem.GetItemData().ItemId);
            _currentWeight -= weight;
            // Cegah negatif (floating point error)
            if (_currentWeight < 0) _currentWeight = 0;
            UpdateLabel();
            CheckPuzzle();
        }
    }

    private void CheckPuzzle()
    {
        // Cek jika TEPAT 75kg
        if (Mathf.IsEqualApprox(_currentWeight, TargetWeight))
        {
            GD.Print("✅ PUZZLE SOLVED: Tepat 75kg! Gate terbuka 33%!");
            _isSolved = true;
            _isDropping = false;
            _gateTargetPos = _gateClosedPos + new Vector3(0, MaxGateHeight * 0.33f, 0);
        }
        // Cek jika MELEBIHI 75kg -> RUSAK!
        else if (_currentWeight > TargetWeight)
        {
            GD.Print("❌ OVERLOAD! Gate rusak dan tertutup!");
            _isSolved = false;
            _isBroken = true;
            _isDropping = true; // Aktifkan animasi jatuh cepat
            _gateTargetPos = _gateClosedPos; // Kembali ke posisi tertutup
            
            // Play overload crash sound (Gate Smash)
            if (GateOverloadSound != null && !GateOverloadSound.Playing)
            {
                GateOverloadSound.Play();
                GD.Print("🔊 Playing Gate Smash sound");
            }
        }
        // Jika kurang dari 75kg, gate naik bertahap berdasarkan persentase
        else
        {
            _isSolved = false;
            _isDropping = false;
            // Hitung persentase (0-100%)
            float percentage = (_currentWeight / TargetWeight) * 100f;
            // Gate naik proporsional dengan berat (maksimal 33%)
            float heightMultiplier = (_currentWeight / TargetWeight) * 0.33f;
            _gateTargetPos = _gateClosedPos + new Vector3(0, MaxGateHeight * heightMultiplier, 0);
            
            GD.Print($"📊 Berat: {_currentWeight}kg ({percentage:F1}%) - Gate naik {heightMultiplier * 100:F1}%");
        }
    }

    public override void _Process(double delta)
    {
        if (GateObject == null) return;

        // Gunakan kecepatan lebih cepat saat overload (jatuh seperti kebanting)
        float lerpSpeed = _isDropping ? 8.0f : 2.0f;
        
        // Animasi gerbang gerak menuju target position
        GateObject.Position = GateObject.Position.Lerp(_gateTargetPos, (float)delta * lerpSpeed);
    }

    private void UpdateLabel()
    {
        if (StatusLabel != null)
        {
            if (_isBroken)
            {
                StatusLabel.Text = "RUSAK!";
                StatusLabel.Modulate = Colors.DarkRed;
            }
            else
            {
                StatusLabel.Text = $"{_currentWeight} / {TargetWeight} kg";
                if (Mathf.IsEqualApprox(_currentWeight, TargetWeight)) 
                    StatusLabel.Modulate = Colors.Green;
                else if (_currentWeight > TargetWeight) 
                    StatusLabel.Modulate = Colors.Red;
                else 
                    StatusLabel.Modulate = Colors.Yellow;
            }
        }
    }

    // Helper sederhana untuk mapping ID ke Berat (Karena DroppedItem.cs belum simpan berat)
    private float GetWeightFromId(string itemId)
    {
        // Pastikan string di sini SAMA dengan Item Id di Inspector
        switch (itemId) 
        {
            case "stone_10": return 10f;
            case "stone_15": return 15f;
            case "stone_20": return 20f; 
            case "stone_30": return 30f;
            case "stone_40": return 40f;
            default: 
                GD.PrintErr($"ID '{itemId}' tidak dikenali! Berat dianggap 0.");
                return 0f; 
        }
    }
}