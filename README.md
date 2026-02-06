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
| Mini Map | Real-time top-down map with player, items, and walls |
| Timer & Lives | Countdown timer + 3 lives system with fail/game over screens |
| Crosshair | FPP-only aiming reticle |
| Inventory Panel | Visual drag-drop interface with hotbar |
| Item Prompts | Context-sensitive interaction hints |

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

### 🗺️ Mini Map System

Real-time mini map di top-right corner yang menampilkan:
- **🟢 Player Position**: Titik hijau dengan indikator arah hadap
- **🟡 Pickable Items**: Semua item yang dapat diambil
- **⬜ Static Obstacles**: Dinding, lantai, dan objek statis

**Files:**
- Scene: `scenes/ui/MiniMap.tscn`
- Script: `scripts/ui/MiniMapSystem.cs`

**Setup ke Level:**
1. Buka level scene (e.g., `level_1_cell/Main.tscn`)
2. Pilih node `UI` (CanvasLayer)
3. **Scene → Instantiate Child Scene** atau `Ctrl+Shift+A`
4. Pilih `scenes/ui/MiniMap.tscn`
5. Save (`Ctrl+S`)

**Export Variables:**
- `MapSize`: Ukuran mini map (default: 200x200)
- `MapPosition`: Offset dari top-right (default: -220, 20)
- `PlayerColor`, `ItemColor`, `WallColor`: Warna elemen
- `PlayerDotSize`, `ItemDotSize`: Ukuran titik

---

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

## Setup Guide for New Levels

### Quick Integration Checklist

When creating a new level, add these UI components:

**1. Mini Map**
```
UI (CanvasLayer)
└─ MiniMap [Instance: scenes/ui/MiniMap.tscn]
```

**2. Timer & Lives**
```
UI (CanvasLayer)
└─ GameHUD [Instance: scenes/ui/GameHUD.tscn]
```

**3. Inventory**
```
UI (CanvasLayer)
└─ InventoryUI [Instance: scenes/ui/InventoryUI.tscn]
```

**Steps:**
1. Open level scene (e.g., `level_2_bridge/Main.tscn`)
2. Select `UI` node
3. For each component:
   - Scene → Instantiate Child Scene
   - Select `.tscn` file
   - Save
4. Test in-game to verify all UI elements appear

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
    ├── MiniMap.tscn           # Mini map
    ├── InventoryUI.tscn       # Inventory panel
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
    ├── MiniMapSystem.cs
    └── InventoryUI.cs
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
- Mini map with real-time tracking
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
- `scenes/ui/MiniMap.tscn` - Mini map
- `scenes/ui/InventoryUI.tscn` - Inventory panel
- `scenes/common/Player.tscn` - Player character

**Essential Scripts:**
- `scripts/systems/LevelGameManager.cs` - Level integration
- `scripts/ui/GameHUD.cs` - HUD controller
- `scripts/ui/MiniMapSystem.cs` - Mini map logic
- `scripts/ui/InventoryUI.cs` - Inventory controller

---

**For detailed API documentation and advanced customization, refer to individual script comments.**
