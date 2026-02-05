using Godot;
using System;
using System.Collections.Generic;

public partial class SewerGatePuzzle : Node3D
{
    [ExportGroup("Settings")]
    [Export] public float TargetWeight = 75.0f;
    [Export] public Node3D GateObject; // Referensi ke Gerbang Visual
    [Export] public float MaxGateHeight = 4.0f; // Ketinggian maksimal gerbang (saat 75kg)
    
    [ExportGroup("Feedback")]
    [Export] public Label3D StatusLabel; // Teks angka di atas gerbang

    private float _currentWeight = 0.0f;
    private bool _isSolved = false;
    private bool _isBroken = false; // Gate rusak jika overload
    private Vector3 _gateClosedPos;
    private Vector3 _gateTargetPos;

    public override void _Ready()
    {
        if (GateObject != null)
        {
            _gateClosedPos = GateObject.Position;
            _gateTargetPos = _gateClosedPos;
        }
        UpdateLabel();
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
            CheckPuzzle();
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
            _gateTargetPos = _gateClosedPos + new Vector3(0, MaxGateHeight * 0.33f, 0);
        }
        // Cek jika MELEBIHI 75kg -> RUSAK!
        else if (_currentWeight > TargetWeight)
        {
            GD.Print("❌ OVERLOAD! Gate rusak dan tertutup!");
            _isSolved = false;
            _isBroken = true;
            _gateTargetPos = _gateClosedPos; // Kembali ke posisi tertutup
        }
        // Jika kurang dari 75kg, gate naik bertahap berdasarkan persentase
        else
        {
            _isSolved = false;
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

        // Animasi gerbang gerak perlahan menuju target position
        GateObject.Position = GateObject.Position.Lerp(_gateTargetPos, (float)delta * 2.0f);
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