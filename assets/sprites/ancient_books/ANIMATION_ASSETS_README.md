# Ancient Books Animation Assets Guide

## Overview
Sistem animasi ancient books menggunakan AnimationPlayer dengan support untuk sprite sequences. Jika asset tidak tersedia, sistem akan otomatis fallback ke animasi tween sederhana.

## Required Assets

### 1. Cover Closed (`cover_closed.png`)
**Path:** `res://assets/sprites/ancient_books/cover_closed.png`
**Size:** 400x500 pixels
**Description:** Gambar buku ancient tertutup (cover depan)
**Requirements:**
- Format: PNG dengan alpha channel
- Tampilan: Buku tua dengan ornamen mystical
- Warna: Coklat gelap, dengan detail emas/kuning
- Style: Medieval/fantasy

**Status:** ❌ Not yet created (using template as fallback)

---

### 2. Cover Opening (`cover_opening.png`)
**Path:** `res://assets/sprites/ancient_books/cover_opening.png`
**Size:** 867x528 pixels (matching ancient_book_template.png)
**Description:** Gambar transisi cover mulai terbuka
**Requirements:**
- Format: PNG dengan alpha channel
- Tampilan: Buku setengah terbuka, terlihat halaman dalam
- Bisa berupa single frame atau composite

**Status:** ❌ Not yet created (animation skips this if not available)

---

### 3. Page Flip Animation Frames
**Path:** `res://assets/sprites/ancient_books/page_flip/`
**Filenames:** `page_flip_01.png` to `page_flip_10.png`
**Size:** Recommended 400x480 pixels each
**Description:** Sequence frames untuk animasi halaman buku yang bergerak
**Requirements:**
- Format: PNG dengan alpha channel
- 10 frames minimum untuk smooth animation
- Tampilan: Halaman kertas tua yang flip dari kiri ke kanan
- Style: Kertas aging dengan sedikit curl di tepi

**Frame breakdown:**
- Frames 1-3: Halaman mulai terangkat dari kiri
- Frames 4-6: Halaman di tengah flip (90 derajat)
- Frames 7-10: Halaman mendarat di sisi kanan

**Status:** ❌ Not yet created (using ColorRect tween as fallback)

---

## Animation Sequence

### Current Implementation (with assets):
1. **Phase 1** (0.5s): `cover_closed.png` fades in + scales up
2. **Phase 2** (0.5s): Cross-fade to `cover_opening.png`
3. **Phase 3** (0.8s): Page flip animation plays (10 frames @ 12 FPS)
4. **Phase 4** (0.4s): `ancient_book_template.png` fades in
5. **Phase 5** (0.8s): Content (title, image, text) fades in

**Total duration:** ~3 seconds

### Fallback (without assets):
- Uses Godot Tween with ColorRect placeholders
- Still provides smooth animation experience
- Duration: ~3.3 seconds

---

## How to Add Assets

### Method 1: Create manually
1. Use image editor (Photoshop, GIMP, Krita, etc.)
2. Create images following specs above
3. Export as PNG with transparency
4. Place in correct folder structure

### Method 2: AI Generation
Use AI tools like:
- Midjourney: `/imagine ancient mystical book cover, leather bound, gold ornaments`
- Stable Diffusion: "medieval grimoire book cover, closed, detailed, fantasy art"
- DALL-E: "ancient spellbook cover, leather texture, mystical symbols"

For page flip sequence:
- Generate single page image
- Use sprite sheet tool to create flip sequence
- Or manually create frames in animation software

### Method 3: Asset Stores
- Unity Asset Store (convert to PNG)
- Itch.io game assets
- OpenGameArt.org
- Kenny.nl (free game assets)

---

## Testing

### Enable/Disable Advanced Animation
In `BookUI.cs`, line ~30:
```csharp
private bool _useAdvancedAnimation = true; // Set false to test fallback
```

### Check Asset Loading
When ancient book opens, console will show:
- `✓ Loaded cover_closed.png` - Asset found
- `⚠ cover_closed.png not found, using template as fallback` - Using fallback
- `✓ Loaded page flip animation frames` - Sprite sequence ready
- `⚠ Page flip frames not found, will use fallback tween animation` - Using ColorRect

---

## Current Status Summary

| Asset | Status | Fallback |
|-------|--------|----------|
| `cover_closed.png` | ❌ Missing | Uses `ancient_book_template.png` |
| `cover_opening.png` | ❌ Missing | Skipped |
| `page_flip/*.png` | ❌ Missing | ColorRect tween animation |
| `ancient_book_template.png` | ✅ Exists | N/A |
| `ancient_content_lvl-*.png` | ✅ Exists | N/A |

---

## Recommended Creation Order

1. **Start with:** `cover_closed.png` (most visible, highest impact)
2. **Then:** Page flip frames (adds polish)
3. **Optional:** `cover_opening.png` (nice to have, but animation works without it)

---

## Animation System Features

✅ **Automatic fallback** - Works without any custom assets
✅ **Hot-reload support** - Add assets while game running
✅ **Modular** - Each asset loads independently
✅ **Performance optimized** - Uses AnimationPlayer for smooth playback
✅ **Scalable** - Easy to add more frames or transitions

---

**Last Updated:** February 4, 2026
**System Version:** Opsi 2 (Advanced AnimationPlayer)
