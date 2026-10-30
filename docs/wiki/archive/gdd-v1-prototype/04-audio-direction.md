# Survival Base — Audio Direction

**Version:** 1.0 (Prototype) | **Last Updated:** 2026-07-30

## Current State: NO AUDIO

The prototype has **no audio implementation**. Phaser's audio system is available but not used.

---

## Proposed Audio Design

### Music

| Scene | Mood | Tempo | Notes |
|-------|------|-------|-------|
| Menu | Tense, atmospheric | Slow | Ambient wasteland ambience |
| Day Phase (Planning) | Tense but focused | Medium | Driving rhythm for time pressure |
| Day Phase (Execution) | Urgent, action | Fast | Increased intensity |
| Night Phase | Horror, suspense | Slow+ | Dark, ominous, monster threat |
| Game Over (Win) | Triumphant, relief | Medium | Celebration, hope |
| Game Over (Lose) | Tragic, somber | Very slow | Loss, despair |

### Sound Effects

| Action | SFX | Notes |
|--------|-----|-------|
| Click / Select | Short blip | UI feedback |
| Confirm chain | Positive chime | Mission accepted |
| Execute | Whoosh | All missions begin |
| Travel complete | Thud | Crew arrives at target |
| Gather success | Crunch / metallic | Resource collected |
| Relic found | Magical chime | Success sound |
| Monster defeated | Impact + roar | Combat success |
| Monster damage | Crunch / scratch | Crew takes damage |
| Crew death | Silence / low tone | Permanent loss |
| Night starts | Wind / howl | Transition |
| Monster attack | Growl / impact | Base under attack |
| Base destroyed | Explosion / crash | Game over |
| Day ends | Bell / horn | New day announcement |
| Food consumed | Chomp / eat | Daily cycle |

### UI Sounds
- Button hover: soft click
- Button press: confirmation tone
- Error notification: buzz / low warning
- Notification dismiss: fade out

---

## Implementation Notes

### Phaser Audio API
Phaser 3 supports:
- **Web Audio API** (primary, high quality)
- **HTML Audio** (fallback, lower quality)
- **Looping** for background music
- **Spatial audio** for directional sound

### Suggested File Format
- **Music:** OGG (best support, small size)
- **SFX:** OGG or WAV (short sounds, WAV is fine)
- **Max size:** 500KB per SFX, 5MB per music track

### Volume Control
- Master volume slider (future)
- Separate music/SFX volume (future)
- Mute button (future)
- Default: 70% music, 100% SFX

---

## Audio Priorities (If Adding Audio)

| Priority | Audio | Justification |
|----------|-------|---------------|
| **P0** | Night phase SFX | Monster attacks need audio impact |
| **P0** | Mission complete sound | Feedback loop for player satisfaction |
| **P1** | Menu music | Sets mood before game starts |
| **P1** | Click/select sounds | UI feedback |
| **P2** | Day phase music | Ambience during planning |
| **P2** | Combat SFX | Monster hunt feedback |
| **P3** | Ambient wasteland | Background atmosphere |
| **P3** | Victory/defeat music | End-of-game emotions |

---

## Reference Games (Audio Inspiration)

| Game | What to Learn |
|------|---------------|
| **FTL** | Tense ambient music, minimal SFX |
| **They Are Billions** | Horror night theme, base build audio |
| **Loop Hero** | Minimalist, mood-setting audio |
| **Against the Storm** | Time pressure audio cues |