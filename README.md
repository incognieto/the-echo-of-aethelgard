# The Echo of Aethelgard: Fate's Spoiler

## Story
Under the tyrannical rule of King Valerius III, the Kingdom of Aethelgard fell into darkness. The law only favored those of noble blood, while commoners who dared to speak out were immediately thrown into Ironfang Oubliette—an underground prison said to be impossible to escape. You are one of them, a prisoner incarcerated without trial simply for refusing to surrender your family's farmland.

However, Ironfang harbors a secret. Behind the damp cell walls lies a relic from the failed Red Revolution 50 years ago: "The Glimpse Grimoire." This is no ordinary spellbook; it connects to the already-written flow of time. Anyone who touches it is forced to see "spoilers" of their own fate exactly 60 seconds before it happens.

In this first edition, your journey begins from the lowest point. With the help of this cursed yet blessed book, you must navigate the deadly prison corridors, pass through forbidden alchemy laboratories, to the warden's secret library. Every step you take has already been "spoiled" by the book as failure or death. Your task is to manipulate the variables around you—symbols, chemical liquids, even object weights—to transform these deadly spoilers into a path to freedom. The ultimate goal? To obtain the Palace Map that will determine whether the fires of revolution will burn again or be extinguished forever in the king's hands.

---

## Tech Stack
| Technology | Version |
|------------|---------|
| Godot Engine | 4.6 (Mono) |
| Language | C# (.NET) |
| Physics Engine | Jolt Physics |
| Rendering | DirectX 12 (Forward+) |

---

## Implemented Features

### Core Gameplay
| Feature | Description |
|---------|-------------|
| Dual Camera System | First-Person & Isometric 3D top-down view (toggle with C) |
| Player Movement | WASD movement with camera-relative direction |
| Item Interaction | Raycast pickup (FPP) & proximity detection (Isometric) |
| Inventory System | 16 slots (4x4 grid + 1x4 hotbar) with drag-drop & auto-stacking |
| Usable Items | Books with readable content, UI with BBCode support |
| Physics System | Drop items with realistic throw physics |

### UI Systems
| Feature | Description |
|---------|-------------|
| Timer & Lives | Countdown timer + 3 lives system with fail/game over screens |
| Crosshair | FPP-only aiming reticle |
| Inventory Panel | Visual drag-drop interface with hotbar |
| Item Prompts | Context-sensitive interaction hints |
| Static Minimap | Bottom-right minimap with group-based color-coded objects |

### Puzzles
| Level | Puzzle Type | Description |
|-------|-------------|-------------|
| Level 1 - Cell | Door Password | 6-digit password entry with control panel |
| Level 3 - Lab | Color Mixing | Tri-color synthesis: Materials → Secondary → Teal Potion |
| Level 4 - Library | Grid Arrangement | 3x3 story book ordering puzzle |

---

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
| 1-4 | Select hotbar slot | Both |
| Mouse Drag | Drag-drop items between slots | Both |
| Space | Jump | First-Person |
| C | Toggle camera mode (FPP ↔ Isometric) | Both |
| ESC | Release/Capture mouse cursor | Both |

---

## System Documentation

### ⏱️ Timer & Lives System

Sistem countdown timer dan nyawa dengan fail state dan game over screen.

**Components:**
- **TimerManager** (Autoload): Countdown timer per level
- **LivesManager** (Autoload): 3 nyawa pemain
- **GameHUD**: Display timer & lives di UI
- **FailScreen**: "YOU WERE CAUGHT BY THE GUARDS" + respawn
- **GameOverScreen**: "GAME OVER" + back to menu

**Files:**
- Systems: `scripts/systems/TimerManager.cs`, `LivesManager.cs`
- UI: `scripts/ui/GameHUD.cs`, `FailScreen.cs`, `GameOverScreen.cs`
- Scene: `scenes/ui/GameHUD.tscn`

**Setup ke Level:**

**Method 1: Via Scene (RECOMMENDED)**
1. Buka level scene
2. Pilih node `UI` (CanvasLayer)
3. **Scene → Instantiate Child Scene**
4. Pilih `scenes/ui/GameHUD.tscn`
5. Save

✅ **GameHUD sudah terinstall di semua level!** (Level 1-5)

**Method 2: Edit Layout via GUI Godot**
1. Buka `scenes/ui/GameHUD.tscn` di Godot Editor
2. Pilih `TimerPanel` atau `LivesPanel` di Scene Tree
3. Di Inspector, ubah:
   - **Position**: offset_left, offset_top untuk geser panel
   - **Size**: offset_right, offset_bottom untuk resize
4. Pilih `TimerLabel` atau `LivesLabel`:
   - Theme Overrides → Font Sizes → font_size
   - Theme Overrides → Colors → font_color
5. Save (Ctrl+S)

**Dokumentasi Layout**: Lihat `GAMEHUD_LAYOUT.md` untuk panduan lengkap

**API Usage:**
```csharp
// Timer
TimerManager.Instance.StartTimer(300f, "Level 1");
TimerManager.Instance.AddTime(30f); // Bonus time
TimerManager.Instance.CompleteLevel();

// Lives
LivesManager.Instance.LoseLife();
LivesManager.Instance.GainLife();
bool hasLives = LivesManager.Instance.HasLivesRemaining();
```

**Customization:**
- Time per level: Edit `LevelTimeLimit` in Inspector
- Max lives: Edit `MaxLives` in `LivesManager.cs` (default: 3)
- UI position: Edit `_timerPosition` and `_livesPosition` in `GameHUD.cs`

---

### 🎒 Inventory System

16-slot inventory (4x4 grid) + 4-slot hotbar dengan drag-drop support.

**Files:**
- Scene: `scenes/ui/InventoryUI.tscn`
- Script: `scripts/ui/InventoryUI.cs`

**Node Structure:**
```
InventoryUI (Control)
└─ InventoryPanel (Panel)
   └─ VBoxContainer
      ├─ TitleLabel
      ├─ InventoryGrid (GridContainer)
      ├─ SelectedItemLabel
      └─ InstructionsLabel
```

**Visual Editing via Godot Editor:**
1. Open `scenes/ui/InventoryUI.tscn`
2. Select root node `InventoryUI`
3. In Inspector, modify Export Variables:
   - Inventory Columns/Rows: Grid size
   - Hotbar Slots: Number of hotbar slots
   - Slot Size: Slot dimensions (default: 80x80)
   - Hotbar Position: Position from center-bottom

**Per-Level Configuration:**
- All levels use same inventory: 16 slots (4x4) + 4 hotbar
- Automatically instantiated in `Main.tscn` under `UI` node

---

### 🗺️ Static Minimap System

Lightweight static minimap yang menampilkan seluruh area level dengan notasi warna berbeda untuk setiap tipe object. Menggunakan sistem **grup Godot** untuk fleksibilitas.

**Files:**
- Scene: `scenes/ui/MinimapRenderer.tscn`
- Script: `scripts/ui/MinimapRenderer.cs`

**Color Codes:**
- 🟢 **Hijau** = Player (`minimap_player`)
- 🟡 **Kuning** = Items (`minimap_item`)
- 🟣 **Ungu** = Interactable Objects/Puzzles (`minimap_interactable`)
- ⚫ **Hitam/Abu** = Obstacles/Walls (`minimap_obstacle`)

**Setup per Level:**

**Method 1: Instantiate via Godot Editor (RECOMMENDED)**
1. Buka scene level (e.g., `level_1_cell/Main.tscn`)
2. Pilih node `UI` (CanvasLayer)
3. **Scene → Instantiate Child Scene**
4. Pilih `scenes/ui/MinimapRenderer.tscn`
5. Save

**Method 2: Add Nodes to Groups**
1. Pilih node yang ingin ditampilkan di minimap (Player, Items, Doors, Walls, etc.)
2. Di Inspector, tab **Node** → **Groups** → ketik nama grup:
   - `minimap_player` untuk Player
   - `minimap_item` untuk items yang bisa dipick
   - `minimap_interactable` untuk puzzle objects, doors, control panels
   - `minimap_obstacle` untuk walls, barriers, obstacles
3. Klik **Add**
4. Ulangi untuk semua objects yang ingin ditampilkan

**Customization via Inspector:**
```
Minimap Settings:
  - Minimap Size: Ukuran minimap (default: 200x200)
  - Margin: Jarak dari edge layar (default: 20)
  
Visual Settings:
  - Background Color: Warna background (default: hitam semi-transparent)
  - Border Color: Warna border (default: putih)
  
Object Colors:
  - Player Color, Item Color, Interactable Color, Obstacle Color
  
Dot Sizes:
  - Player Dot Size, Item Dot Size, dll.
  
Level Bounds:
  - Auto Detect Bounds: true (otomatis detect dari semua objects)
  - Manual Bounds Min/Max: jika Auto Detect = false
```

**Contoh Penambahan Grup via Script (Optional):**
```csharp
// Di _Ready() Player
AddToGroup("minimap_player");

// Di _Ready() PickableItem
AddToGroup("minimap_item");

// Di _Ready() Puzzle/Door
AddToGroup("minimap_interactable");
```

**Tips:**
- **Auto-detect bounds** akan menghitung area level otomatis dari semua object
- Minimap refresh setiap frame untuk tracking player movement
- Semua objects di-render sebagai **titik bulat (circles)**
- Posisi minimap: **bottom-right corner** (bisa diubah di script/Inspector)

---

## File Structure

```
scenes/
├── common/
│   ├── Player.tscn
│   └── DroppedItem.tscn
├── levels/
│   ├── level_1_cell/Main.tscn
│   ├── level_2_bridge/Main.tscn
│   ├── level_3_lab/Main.tscn
│   ├── level_4_library/Main.tscn
│   └── level_5_Sewer/Main.tscn
└── ui/
    ├── GameHUD.tscn           # Timer & Lives display
    ├── InventoryUI.tscn       # Inventory panel
    ├── MinimapRenderer.tscn   # Static minimap (NEW)
    ├── FailScreen.tscn        # Fail screen (auto-created)
    ├── GameOverScreen.tscn    # Game over (auto-created)
    └── [other UI scenes]

scripts/
├── common/               # Player, camera, movement
├── items/               # Item system, pickups
├── levels/              # Level-specific logic
├── puzzles/             # Puzzle mechanics
├── systems/             # Core managers (autoloaded)
│   ├── TimerManager.cs
│   ├── LivesManager.cs
│   ├── InventorySystem.cs
│   ├── MusicManager.cs
│   └── CursorManager.cs
└── ui/                  # UI controllers
    ├── GameHUD.cs
    ├── FailScreen.cs
    ├── GameOverScreen.cs
    ├── InventoryUI.cs
    └── MinimapRenderer.cs   # Minimap renderer (NEW)
```

---

## Troubleshooting

### HUD tidak muncul
- ✅ Verifikasi `TimerManager` dan `LivesManager` ada di autoload (Project → Settings → Autoload)
- ✅ Pastikan `GameHUD.tscn` sudah di-instantiate di node `UI`
- ✅ Check console untuk error messages
- ✅ Verify nodes di GameHUD.tscn: `GameHUD → TimerPanel/LivesPanel → Labels`
- ✅ Pastikan `visible = true` di semua nodes (buka GameHUD.tscn, check Inspector)

### Timer tidak countdown
- ✅ Timer perlu di-start manual. Tambahkan di level script:
  ```csharp
  // Di _Ready() level script
  if (TimerManager.Instance != null)
      TimerManager.Instance.StartTimer(300f, "Level 1");
  ```
- ✅ Atau attach `LevelTimerStarter.cs` ke root node level
- ✅ Check console: "Timer started" message

### Lives tidak update
- ✅ Check console: `LivesManager not found` ?
- ✅ Verify LivesManager di autoload
- ✅ Pastikan script dapat access Instance

### Edit Layout HUD
- **Via GUI Godot**: Buka `scenes/ui/GameHUD.tscn`
- Pilih `TimerPanel` atau `LivesPanel`
- Edit di Inspector: offset_left, offset_top, offset_right, offset_bottom
- Detailed guide: Lihat comments di `GameHUD.cs`

---

## Development Roadmap

### Completed ✅
- Dual camera system
- Inventory with drag-drop
- Timer & lives system with fail states
- Multiple puzzles (door, mixing, grid)
- Item interaction system

### In Progress 🚧
- Additional levels and puzzles
- Save/load system
- Audio/music integration
- Story progression mechanics

### Planned 📋
- Achievement system
- Difficulty modes
- Time bonus pickups
- Extra life collectibles
- Enhanced visual effects
- Localization support

---

## Development Team

**Politeknik Negeri Bandung**
- Farras Ahmad Rasyid
- Satria Permata Sejati
- Nieto Salim Maula
- Umar Faruq Robbany
- Muhammad Ichsan Rahmat Ramadhan

---

## License

© 2026 Politeknik Negeri Bandung. All rights reserved.

---

## Quick Reference

**Autoloaded Singletons:**
- `CursorManager` - Cursor management
- `PanelManager` - Panel state tracking
- `SettingsManager` - Game settings
- `MusicManager` - Background music
- `ButtonSoundManager` - UI sounds
- `TimerManager` - Level timers
- `LivesManager` - Player lives

**Key Scenes:**
- `scenes/ui/GameHUD.tscn` - Timer & Lives UI
- `scenes/ui/InventoryUI.tscn` - Inventory panel
- `scenes/common/Player.tscn` - Player character

**Essential Scripts:**
- `scripts/systems/LevelGameManager.cs` - Level integration
- `scripts/ui/GameHUD.cs` - HUD controller
- `scripts/ui/InventoryUI.cs` - Inventory controller

---

**For detailed API documentation and advanced customization, refer to individual script comments.**
