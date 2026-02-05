# The Echo of Aethelgard: Fate's Spoiler

## Story
Under the tyrannical rule of King Valerius III, the Kingdom of Aethelgard fell into darkness. The law only favored those of noble blood, while commoners who dared to speak out were immediately thrown into Ironfang Oubliette—an underground prison said to be impossible to escape. You are one of them, a prisoner incarcerated without trial simply for refusing to surrender your family's farmland.

However, Ironfang harbors a secret. Behind the damp cell walls lies a relic from the failed Red Revolution 50 years ago: "The Glimpse Grimoire." This is no ordinary spellbook; it connects to the already-written flow of time. Anyone who touches it is forced to see "spoilers" of their own fate exactly 60 seconds before it happens.

In this first edition, your journey begins from the lowest point. With the help of this cursed yet blessed book, you must navigate the deadly prison corridors, pass through forbidden alchemy laboratories, to the warden's secret library. Every step you take has already been "spoiled" by the book as failure or death. Your task is to manipulate the variables around you—symbols, chemical liquids, even object weights—to transform these deadly spoilers into a path to freedom. The ultimate goal? To obtain the Palace Map that will determine whether the fires of revolution will burn again or be extinguished forever in the king's hands.

## Tech Stack
| Technology | Version |
|------------|---------|
| Godot Engine | 4.6 (Mono) |
| Language | C# (.NET) |
| Physics Engine | Jolt Physics |
| Rendering | DirectX 12 (Forward+) |

## Implemented Features
| Feature | Description |
|---------|-------------|
| Dual Camera System | First-Person & Isometric 3D top-down view (toggle with C) |
| Player Movement | WASD movement with camera-relative direction |
| Item Interaction | Raycast pickup (FPP) & proximity detection (Isometric) |
| Inventory System | 16 slots (4x4 grid + 1x4 hotbar) with drag-drop & auto-stacking |
| Usable Items | Books with readable content, UI with BBCode support |
| Door Puzzle (Level 1) | 6-digit password puzzle with control panel interaction |
| Mixing Puzzle (Level 3) | Tri-color synthesis: Materials → Secondary Potions → Teal Potion |
| Library Puzzle (Level 4) | 3x3 grid puzzle - arrange 9 story books in narrative order |
| Physics System | Drop items with realistic throw physics |
| UI Elements | Crosshair (FPP only), inventory panel, hotbar, item prompts |
| Mini Map System | Real-time top-down map showing player (green), items (yellow), and walls (gray) |

## Mini Map System
Real-time mini map di top-right corner yang menampilkan:
- **🟢 Player Position**: Titik hijau dengan indikator arah hadap
- **🟡 Pickable Items**: Semua item yang dapat diambil (PickableItem & DroppedItem)
- **⬜ Static Obstacles**: Dinding, lantai, dan objek statis lainnya

### File Locations:
- **Scene File**: `scenes/ui/MiniMap.tscn` - UI scene untuk mini map
- **Script File**: `scripts/ui/MiniMapSystem.cs` - Logic untuk rendering & detection

### Cara Mengintegrasikan ke Level:
**Metode 1: Via Godot Editor (Recommended)**
1. Buka level scene (e.g., `level_1_cell/Main.tscn`)
2. Pilih node `UI` (CanvasLayer)
3. Klik **Scene → Instantiate Child Scene** atau tekan `Ctrl+Shift+A`
4. Pilih file `scenes/ui/MiniMap.tscn`
5. Mini map akan otomatis muncul di top-right corner
6. **Ctrl+S** untuk save scene

**Metode 2: Manual Addition (Advanced)**
Di file `.tscn`, tambahkan di dalam node `UI`:
```gdscript
[ext_resource type="PackedScene" uid="uid://bqw5x8nj7m8yc" path="res://scenes/ui/MiniMap.tscn" id="X_minimap"]

# Di dalam [node name="UI" type="CanvasLayer"]
[node name="MiniMap" parent="UI" instance=ExtResource("X_minimap")]
```

### Export Variables (Konfigurasi):
| Variable | Description | Default |
|----------|-------------|---------|
| **MapSize** | Ukuran mini map (width x height) | `(200, 200)` |
| **MapPosition** | Offset dari top-right corner | `(-220, 20)` |
| **MapPadding** | Padding untuk auto-scale boundaries | `10.0` |
| **AutoDetectBounds** | Auto-detect level boundaries | `true` |
| **PlayerColor** | Warna titik player | `Green (#00FF00)` |
| **ItemColor** | Warna titik item | `Yellow (#FFFF00)` |
| **WallColor** | Warna obstacle/dinding | `Gray (#808080)` |
| **PlayerDotSize** | Ukuran titik player | `6.0` |
| **ItemDotSize** | Ukuran titik item | `4.0` |

### Tips Kustomisasi:
- **Perbesar map**: Ubah `MapSize = (250, 250)`
- **Geser posisi**: Ubah `MapPosition.Y` untuk vertical adjustment
- **Ubah warna**: Edit `PlayerColor`, `ItemColor`, atau `WallColor`
- **Manual bounds**: Set `AutoDetectBounds = false` lalu atur `ManualBoundsMin/Max`

### Cara Kerja:
1. **Initialization**: Saat `_Ready()`, sistem mendeteksi:
   - Player reference (`/root/Main/Player`)
   - World boundaries (dari semua StaticBody3D)
   - Static obstacles (dinding, objek statis)
   
2. **Real-time Update**: Setiap frame, sistem:
   - Mengambil posisi player terkini
   - Mendeteksi semua item di group `pickable_items`
   - Mendeteksi semua `DroppedItem` di scene
   - Render ke canvas dengan skala otomatis

3. **Coordinate Mapping**: Konversi 3D world position (X, Z) ke 2D map position

### Troubleshooting:
- **Player tidak muncul**: Pastikan node Player ada di `/root/Main/Player`
- **Items tidak muncul**: Pastikan PickableItem ada di group `pickable_items`
- **Bounds terlalu besar/kecil**: Set `AutoDetectBounds = false` dan atur manual
- **Map tidak update**: Check bahwa scene sudah di-instantiate dengan benar

## Visual UI Editor (Level 3)
InventoryUI sekarang menggunakan **scene terpisah (.tscn) dengan node-based UI** untuk kemudahan editing!

### File Locations:
- **Scene File**: `scenes/ui/InventoryUI.tscn` - UI layout dengan nodes
- **Script File**: `scripts/ui/InventoryUI.cs` - Logic & dynamic behavior

### Node Structure di InventoryUI.tscn:
```
InventoryUI (Control) - Root node dengan export variables
└─ InventoryPanel (Panel) - Background panel
   └─ VBoxContainer
      ├─ TitleLabel (Label) - "Inventory (16 Slots)"
      ├─ InventoryGrid (GridContainer) - Container untuk slots
      ├─ SelectedItemLabel (Label) - Info item terpilih
      └─ InstructionsLabel (Label) - Keyboard shortcuts
```

### Cara Edit Layout via Godot Editor:
1. **Double-click** `scenes/ui/InventoryUI.tscn` di FileSystem Godot
2. Pilih root node `InventoryUI`
3. Di **Inspector** panel (kanan), ubah **Export Variables**:
   - **Inventory Columns**: Jumlah kolom grid (Level 1: 3, Level 3: 4)
   - **Inventory Rows**: Jumlah baris grid (Level 1: 2, Level 3: 4)
   - **Hotbar Slots**: Jumlah slot hotbar (Level 1: 6, Level 3: 4)
   - **Slot Size**: Ukuran slot dalam pixel (default: 80x80)
   - **Slot Spacing**: Jarak antar slot (default: 5)
   - **Inventory Panel Size**: Ukuran panel inventory
   - **Hotbar Panel Size**: Ukuran panel hotbar
   - **Hotbar Position**: Posisi hotbar (X, Y dari center-bottom)
4. **Edit Node Properties** (Optional):
   - Pilih node `InventoryPanel` → Ubah warna background
   - Pilih node `TitleLabel` → Ubah font size, warna, alignment
   - Pilih node `InventoryGrid` → Ubah spacing, columns
5. **Ctrl+S** untuk save
6. Run game untuk melihat perubahan

### Per-Level Configuration:
**PENTING**: Level 1 dan Level 3 sekarang gunakan **inventory yang sama** (16 slots, 4 hotbar)

**Level 1** (`level_1_cell/Main.tscn`):
- Node structure: `Main/UI/InventoryUI`
- Inventory: 16 slots (4x4 grid)
- Hotbar: 4 slots (1x4)

**Level 3** (`level_3_lab/Main.tscn`):
- Node structure: `Main/UI/InventoryUI`  
- Inventory: 16 slots (4x4 grid)
- Hotbar: 4 slots (1x4)

**Level 4** (`level_4_library/Main.tscn`):
- Node structure: `Main/UI/InventoryUI`  
- Inventory: 16 slots (4x4 grid)
- Hotbar: 4 slots (1x4)
- Puzzle: 3x3 grid panel for story book arrangement

### Tips Edit Visual:
- **Geser hotbar horizontal**: Ubah `HotbarPosition.X` (-260 = kiri, -100 = kanan)
- **Geser hotbar vertical**: Ubah `HotbarPosition.Y` (-150 = atas, -80 = bawah)
- **Perbesar slot**: `SlotSize = (100, 100)`
- **Grid lebih rapat**: `SlotSpacing = 2`
- **Ubah warna panel**: Pilih `InventoryPanel` → Theme Overrides → Styles
- **Ubah font title**: Pilih `TitleLabel` → Theme Overrides → Font Sizes

## Game Controls
| Key/Input | Action | Mode |
|-----------|--------|------|
| W, A, S, D | Move character | Both |
| Mouse Move | Camera control | Both |
| E | Pickup/Interact with items | Both |
| Q | Drop 1 item from active slot | Both |
| Ctrl + Q | Drop entire stack | Both |
| F | Use item (e.g., read book) | Both |
| I / Tab | Toggle inventory panel | Both |
| 1-6 | Select hotbar slot | Both |
| 1-4 | Select hotbar slot (Level 3: 4 slots) | Level 3 |
| Mouse Drag | Drag-drop items between inventory slots | Level 3 |
| Space | Jump | First-Person |
| C | Toggle camera mode (FPP ↔ Isometric) | Both |
| ESC | Release/Capture mouse cursor | Both |

## Development Team
**Politeknik Negeri Bandung**
- Farras Ahmad Rasyid
- Satria Permata Sejati
- Nieto Salim Maula
- Umar Faruq Robbany
- Muhammad Ichsan Rahmat Ramadhan

---

© 2026 Politeknik Negeri Bandung. All rights reserved.
