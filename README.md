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
