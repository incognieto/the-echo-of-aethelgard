# Mini Map Integration Guide

Panduan lengkap untuk mengintegrasikan Mini Map System ke semua level di The Echo of Aethelgard.

## 📋 Quick Setup (Recommended)

### Metode 1: Via Godot Editor (Paling Mudah)

#### Untuk Level 1 (Cell):
1. Buka `scenes/levels/level_1_cell/Main.tscn` di Godot Editor
2. Di Scene tree, klik node **UI** (CanvasLayer)
3. Klik menu **Scene → Instantiate Child Scene** atau tekan `Ctrl+Shift+A`
4. Navigasi ke `scenes/ui/MiniMap.tscn`
5. Klik **Open**
6. Mini map akan muncul sebagai child dari UI
7. Tekan `Ctrl+S` untuk save

#### Untuk Level 2 (Bridge):
Ulangi langkah yang sama untuk `scenes/levels/level_2_bridge/Main.tscn`

#### Untuk Level 3 (Lab):
Ulangi langkah yang sama untuk `scenes/levels/level_3_lab/Main.tscn`

#### Untuk Level 4 (Library):
Ulangi langkah yang sama untuk `scenes/levels/level_4_library/Main.tscn`

#### Untuk Level 5 (Sewer):
Ulangi langkah yang sama untuk `scenes/levels/level_5_Sewer/Main.tscn`

---

### Metode 2: Via MiniMapHelper Script (Programmatic)

Jika Anda lebih suka menambahkan mini map secara programmatic:

1. Buka level scene di Godot Editor
2. Pilih node **UI** (CanvasLayer)
3. Di Inspector panel, klik **Attach Script**
4. **SKIP** pembuatan script baru
5. Klik icon folder di Script field
6. Pilih `scripts/ui/MiniMapHelper.cs`
7. Attach script tersebut
8. Save scene (`Ctrl+S`)

**Keuntungan metode ini:**
- Lebih fleksibel - bisa disable/enable via Inspector
- Tidak perlu edit file .tscn secara manual
- Cocok untuk testing

**Cara disable (jika diperlukan):**
- Pilih node UI yang punya script MiniMapHelper
- Di Inspector, set **Enable Mini Map** = `false`

---

## 🎨 Kustomisasi Per-Level

Setelah mini map ditambahkan, Anda bisa customize untuk setiap level:

### Level 1 (Cell) - Small Confined Space
```
MapSize = (180, 180)          # Lebih kecil karena ruangan kecil
MapPadding = 5.0              # Padding lebih kecil
PlayerDotSize = 7.0           # Player lebih terlihat
ItemDotSize = 5.0             # Item lebih terlihat
```

### Level 2 (Bridge) - Linear Path
```
MapSize = (250, 150)          # Lebih lebar (horizontal emphasis)
AutoDetectBounds = true
WallColor = Color(0.6, 0.5, 0.4)  # Brownish untuk jembatan kayu
```

### Level 3 (Lab) - Medium Complex
```
MapSize = (200, 200)          # Standard size
PlayerColor = Color(0, 1, 0.5)    # Cyan-green untuk lab theme
ItemColor = Color(1, 0.8, 0)      # Orange untuk chemical items
```

### Level 4 (Library) - Large Open Space
```
MapSize = (220, 220)          # Sedikit lebih besar
MapPadding = 15.0             # Padding lebih besar
BackgroundColor = Color(0.08, 0.06, 0.04, 0.85)  # Dark brown
```

### Level 5 (Sewer) - Complex Maze
```
MapSize = (240, 240)          # Paling besar untuk maze
PlayerDotSize = 8.0           # Sangat terlihat di maze
WallColor = Color(0.4, 0.5, 0.4)  # Greenish untuk sewer
```

---

## 🔧 Manual Bounds Configuration

Jika auto-detection tidak akurat, set manual bounds:

### Level 1 (Cell) Example:
```csharp
AutoDetectBounds = false
ManualBoundsMin = Vector2(-10, -10)
ManualBoundsMax = Vector2(10, 15)
```

### Level 3 (Lab) Example:
```csharp
AutoDetectBounds = false
ManualBoundsMin = Vector2(-20, -15)
ManualBoundsMax = Vector2(20, 20)
```

**Cara menentukan bounds:**
1. Run level di editor
2. Lihat koordinat X dan Z dari pojok-pojok level
3. Gunakan nilai tersebut untuk Min/Max
4. Tambahkan padding sekitar 5-10 unit

---

## 🐛 Troubleshooting

### Problem: Player tidak muncul di mini map
**Solution:**
- Check bahwa Player node ada di path `/root/Main/Player`
- Check bahwa scene hierarchy benar
- Test dengan menjalankan level dan lihat console output

### Problem: Items tidak muncul
**Solution:**
- Pastikan PickableItem ada di group `pickable_items`
- Check di script PickableItem.cs line ~18: `AddToGroup("pickable_items")`
- DroppedItem akan terdeteksi otomatis

### Problem: Mini map terlalu besar/kecil
**Solution:**
- Adjust `MapSize` variable
- Untuk level kecil: 150-180 pixel
- Untuk level medium: 200-220 pixel
- Untuk level besar: 230-250 pixel

### Problem: Walls/obstacles tidak muncul
**Solution:**
- Mini map mendeteksi semua `StaticBody3D`
- Pastikan dinding menggunakan StaticBody3D, bukan CollisionShape3D biasa
- Atau tambahkan objek ke group `minimap_visible`

### Problem: Posisi mini map menutupi UI lain
**Solution:**
- Adjust `MapPosition` variable
- Default: `(-220, 20)` (top-right dengan offset)
- Move down: Tambah nilai Y (e.g., `(-220, 50)`)
- Move left: Kurangi nilai X (e.g., `(-250, 20)`)

---

## 📊 Performance Tips

Mini map di-design untuk efisien, tapi beberapa tips:

1. **Batasi jumlah obstacles**: Sistem hanya render static obstacles yang terdeteksi saat initialization
2. **Auto-detection hanya sekali**: Boundaries dan obstacles hanya dideteksi saat `_Ready()`
3. **Drawing optimized**: Hanya player dan items yang di-update setiap frame
4. **Manual refresh**: Jika level berubah dinamis, call `RefreshBounds()` dari script lain

---

## 🎯 Testing Checklist

Setelah integrasi, test hal berikut:

### Visual Check:
- [ ] Mini map muncul di top-right corner
- [ ] Background semi-transparent terlihat
- [ ] Border terlihat jelas

### Player Tracking:
- [ ] Titik hijau player muncul
- [ ] Indicator arah hadap player bergerak
- [ ] Posisi player update saat bergerak

### Item Detection:
- [ ] Semua PickableItem terlihat sebagai titik kuning
- [ ] DroppedItem muncul saat di-drop
- [ ] Titik item hilang saat item diambil

### Wall/Obstacle:
- [ ] Dinding terlihat sebagai titik/area abu-abu
- [ ] Layout kasar level terlihat di mini map

### Scale & Bounds:
- [ ] Seluruh level area tercover
- [ ] Tidak ada area kosong berlebih
- [ ] Skala proporsional dengan level

---

## 🚀 Next Steps

Setelah mini map terintegrasi di semua level:

1. **Playtest** setiap level untuk ensure accuracy
2. **Adjust colors** sesuai theme level
3. **Fine-tune sizes** jika diperlukan
4. **Consider additional features**:
   - Zoom in/out functionality
   - Toggle on/off dengan hotkey
   - Mini map rotation following player
   - Different icons untuk different item types
   - Fog of war untuk unexplored areas

---

## 📝 Notes

- Mini map menggunakan **fixed north-up orientation** (north selalu di atas)
- Coordinate mapping: World X → Map X, World Z → Map Y
- Items di group `pickable_items` otomatis terdeteksi
- Static objects (`StaticBody3D`) otomatis di-render sebagai obstacles
