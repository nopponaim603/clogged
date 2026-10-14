# Survival Base — Art Direction & Visual Style

**Version:** 1.0 (Prototype) | **Last Updated:** 2026-07-30

## Visual Style

### Genre
**Minimalist / Geometric** — Uses Phaser primitive shapes (Arc, Graphics) with emoji overlays.
No sprite art. Clean, readable, functional aesthetic.

### Color Palette

| Role | Color | Hex | Usage |
|------|-------|-----|-------|
| **Primary BG** | Dark Navy | `#1a1a2e` | Game background, panel fills |
| **Secondary BG** | Dark Gray | `#2d3436` | Panel borders, button backgrounds |
| **Accent** | Teal | `#00b894` | Primary actions, selected state |
| **Danger** | Red | `#ff6b6b` | Base HP, dead crew, errors |
| **Warning** | Amber | `#f9ca24` | Time warnings, queue highlights |
| **Success** | Mint | `#4ecdc4` | Completed actions, status text |
| **Purple** | Violet | `#6c5ce7` | Relic nodes, relic search UI |
| **White** | Text | `#ffffff` | Primary text |
| **Muted** | Gray | `#b2bec3` | Secondary text |
| **Dark Muted** | Dark Gray | `#636e72` | Tertiary text, disabled state |

### Typography
- **Font family:** `monospace` (all text in game)
- **Sizes:** 10px–56px range
- **Headlines:** 48–56px, bold color
- **Body:** 14–20px, muted colors
- **Labels:** 10–12px, very muted

---

## Visual Elements

### Game Map
- **Background:** Solid `#1a1a2e` (no terrain texture)
- **Grid:** Decorative 60×50px grid lines at 30% opacity (`#4a4a5a`)
- **Base:** Green arc (radius 30, `#00b894`) with `🏠 BASE` label
- **Resource nodes:** Colored arcs (radius 18, 70% fill) with icon labels

### Node Colors

| Node Type | Color | Hex | Icon |
|-----------|-------|-----|------|
| Resource | Yellow | `#f9ca24` | 🪵🪨⛏️🍖💧 |
| Relic | Purple | `#6c5ce7` | 🏛️ |
| Monster | Red | `#e74c3c` | 👹 |

### Crew
- **Icon:** `🧑‍🤝‍🧑` (24px)
- **Idle:** Clickable, teal color (`#4ecdc4`)
- **Busy:** Red text (`#ff6b6b`)
- **Selected:** Yellow highlight arc (radius 24)
- **Dead:** Removed from game

### Missions
- **Travel:** `🚶` sprite, green path line (`#4ecdc4`)
- **Action:** `⛏️` / `🔍` / `⚔️` sprite
- **Complete:** `✅` sprite, green path line
- **Arrow:** Custom arrow graphic on path lines

### Notifications
- **Background:** Dark panel with border
- **Colors by type:**
  - Success: Mint (`#00b894`) + ✅
  - Error: Red (`#ff6b6b`) + ❌
  - Warning: Amber (`#f9ca24`) + ⚠️
  - Info: Teal (`#4ecdc4`) + ℹ️
- **Animation:** Slide up + fade in (300ms)
- **Duration:** 3000ms default
- **Max displayed:** 5 notifications

---

## UI Layout

### Screen Resolution
- **Game:** 1280×720 (standard HD)
- **Scaling:** FIT mode, centered (Phaser.Scale.FIT)
- **Padding:** Dark borders on non-game areas

### HUD Positions

```
┌──────────────────────────────────────────────────────────┐
│  Day: 1/30  ⏱️ Elapsed: 0/12000  🏠 HP: 100  ⏳ Left: 12000 │  ← Top bar
│                                                          │
│                                                          │
│  ┌─────────┐     Resource 1    Relic    Monster     ┌────┐│
│  │         │       Resource 2                      │Re-  ││
│  │         │       Resource 3                      │source││
│  │         │       Resource 4                      │  │  ││
│  │         │                                        │P-  ││
│  │         │                                        │an ││
│  │         │                                        │el ││
│  │  Crew    │                                        │  │  ││
│  │  Panel   │                                        │  │  ││
│  └─────────┘                                        └────┘│
│                                                          │
│  🧑‍🤝‍🧑 Sarah  🧑‍🤝‍🧑 John  🧑‍🤝‍🧑 Emma                     │  ← Crew panel
│  ────────────────────────────────────────────────────── │
│  🎯 Click a crew → Click nodes → ADD TO QUEUE │ [Reset] │  ← Planning panel
└──────────────────────────────────────────────────────────┘
```

### Panel Sizes

| Panel | X, Y | Width, Height |
|-------|------|---------------|
| Resource Panel | 1090, 60 | 180×230 |
| Crew Panel | 30, 520 | 1240×130 |
| Planning Panel | 30, 680 | 1240×55 |
| Mission Display | Center overlay | Dynamic |
| Notification | Top center | Dynamic |

---

## Art Direction for Future (Not Yet Implemented)

### Target Visual Direction
When transitioning from prototype to polished game:

1. **Pixel art sprites** for crew, monsters, base (16×16 or 32×32)
2. **Tile-based terrain** for wasteland (sand, rocks, ruins)
3. **Day/Night color shift** for visual distinction
4. **Particle effects** for gathering, combat, building
5. **Animated UI** with transitions between phases
6. **Character portraits** for crew selection

### Current State: PROTOTYPE
All visual elements are **placeholder primitives** (arcs, text, emojis).
No art assets loaded (only a `logo.png` reference in Preloader).

---

## UI Interactions

### Hover Effects
- Buttons: color shift (`#f9ca24` → `#ffffff`)
- Start button: scale 1.0 → 1.05
- Crew text: color shift to amber on hover
- All clickable text: cursor changes to pointer

### Feedback
- Click: immediate visual update
- Mission confirm: toast notification
- Time warnings: red text overlay
- Damage: camera shake + floating damage number
- Completion: green text + success icon

---

## Accessibility Notes

### Current Limitations
- All text uses monospace (no font loading)
- No colorblind mode
- No text scaling options
- Small text (10–12px) for stats

### Future Considerations
- Add high-contrast mode
- Larger text options
- Icon-based labels for colorblind players
- Hover tooltips for crew stats