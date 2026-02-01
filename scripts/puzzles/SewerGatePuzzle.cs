using Godot;
using System;
using System.Collections.Generic;

public partial class SewerGatePuzzle : Node3D
{
    [ExportGroup("Settings")]
    [Export] public float TargetWeight = 75.0f;
    [Export] public Node3D GateObject; // Referensi ke Gerbang Visual
    [Export] public float OpenHeight = 4.0f; // Seberapa tinggi gerbang naik
    
    [ExportGroup("Feedback")]
    [Export] public Label3D StatusLabel; // Teks angka di atas gerbang

    private float _currentWeight = 0.0f;
    private bool _isSolved = false;
    private Vector3 _gateClosedPos;
    private Vector3 _gateOpenPos;

    public override void _Ready()
    {
        if (GateObject != null)
        {
            _gateClosedPos = GateObject.Position;
            _gateOpenPos = _gateClosedPos + new Vector3(0, OpenHeight, 0);
        }
        UpdateLabel();
    }

    // Hubungkan ini ke signal BodyEntered dari Area3D Scale
public void OnScaleBodyEntered(Node3D body)
{
    // Debug: Cek deteksi
    GD.Print($"[SENSOR] Mendeteksi: {body.Name}");

    if (body is DroppedItem droppedItem)
    {
        string id = droppedItem.GetItemData().ItemId;
        
        // 1. Ambil berat dari ID
        float weight = GetWeightFromId(id);
        
        // Debug: Pastikan beratnya bukan 0
        GD.Print($"[HITUNG] Item: {id} | Berat: {weight} kg");

        // 2. TAMBAHKAN BERATNYA (Ini yang tadi hilang)
        _currentWeight += weight;
        
        // 3. Update Teks di Layar
        UpdateLabel();
        
        // 4. Cek apakah puzzle selesai
        CheckPuzzle();
    }
}

    // Hubungkan ini ke signal BodyExited
    public void OnScaleBodyExited(Node3D body)
    {
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
        // Toleransi kecil untuk float
        if (Mathf.IsEqualApprox(_currentWeight, TargetWeight))
        {
            GD.Print("PUZZLE SOLVED: 75kg Reached!");
            _isSolved = true;
        }
        else if (_currentWeight > TargetWeight)
        {
            GD.Print("OVERLOAD! Too heavy!");
            _isSolved = false;
            // Nanti di sini bisa tambah efek gerbang jatuh/hancur
        }
        else
        {
            _isSolved = false;
        }
    }

    public override void _Process(double delta)
    {
        if (GateObject == null) return;

        Vector3 targetPos = _isSolved ? _gateOpenPos : _gateClosedPos;
        
        // Animasi gerbang gerak perlahan (Lerp)
        GateObject.Position = GateObject.Position.Lerp(targetPos, (float)delta * 2.0f);
    }

    private void UpdateLabel()
    {
        if (StatusLabel != null)
        {
            StatusLabel.Text = $"{_currentWeight} / {TargetWeight}";
            if (_currentWeight == TargetWeight) StatusLabel.Modulate = Colors.Green;
            else if (_currentWeight > TargetWeight) StatusLabel.Modulate = Colors.Red;
            else StatusLabel.Modulate = Colors.Yellow;
        }
    }

    // Helper sederhana untuk mapping ID ke Berat (Karena DroppedItem.cs belum simpan berat)
	// Di dalam SewerGatePuzzle.cs
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
			// dst...
			default: 
				GD.PrintErr($"ID '{itemId}' tidak dikenali! Berat dianggap 0.");
				return 0f; 
		}
	}
}