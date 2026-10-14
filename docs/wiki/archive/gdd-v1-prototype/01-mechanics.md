# Survival Base — Core Mechanics

**Version:** 1.0 (Prototype) | **Last Updated:** 2026-07-30

## Core Loop

```
1. Day Phase — Plan: Select crew, build mission chain (click crew → click targets)
2. Day Phase — Execute: All missions run in parallel, crew travel → act → return
3. Night Phase — Defense: Monsters attack base, base HP drops, resources lost
4. New Day — Map regenerates, consume food, repeat
```

Each cycle = 1 day. The game runs for 30 days.

---

## Phase 1: Day Phase — Mission Planning

### Player Actions

| Action | Input | Result |
|--------|-------|--------|
| Select Crew | Click crew text (bottom panel) | Crew highlighted, UI updates to show selection |
| Add Target to Chain | Click resource node on map | Node added to chain, path line drawn from base → target chain |
| Remove Last Target | Click node again (deselect) / Undo btn / Ctrl+Z | Last node removed from chain |
| Reset Chain | Reset btn / R key | All selections cleared |
| Confirm Chain | Confirm btn | Mission added to queue, crew marked as busy |
| Execute All | Execute btn (after queue has missions) | All queued missions begin parallel execution |
| Deselect All | ESC key | Crew + targets deselected |

### Mission Chain Logic

A mission chain is a sequence of targets for a single crew:

```
Base → [Target A] → [Target B] → [Target C] → Base
```

For each target in the chain, the system calculates:
- **Travel time** from previous location (base or previous target)
- **Action time** for the gathering/searching/hunting
- **Total time** = travel_out + action + travel_back

Total chain time = sum of all `(travel × 2 + action)` for each target.

**Constraints:**
- Crew must be idle (not busy)
- Must be planning phase (not executing, not night)
- Chain can be any length (1+)
- Over-time warnings shown but not blocked (crew still executes)

---

## Phase 2: Mission Execution

### Mission Phases

Each mission goes through 3 visual phases:

| Phase | Symbol | Description |
|-------|--------|-------------|
| **Travel Out** | 🚶 | Crew moves from current position to target |
| **Action** | ⛏️/🔍/⚔️ | Crew performs gathering/searching/hunting |
| **Travel Back** | 🚶 | Crew returns to base |
| **Complete** | ✅ | Mission done, resources collected, crew free |

### Mission Types

| Type | Action Symbol | What Happens |
|------|--------------|--------------|
| **Gather** | ⛏️ | Collects resource based on type (wood/stone/iron/food/water/circuit/aluminum) |
| **Relic Search** | 🔍 → ✨ | Success rate based on searching proficiency; yields food + wood; failure yields partial resources |
| **Monster Hunt** | ⚔️ → 💥 | Success rate based on hunting proficiency; yields food + monster parts; failure deals damage to crew |

### Yield Calculations

**Gathering:**
```
amount = floor(target.amount × (0.5 + effectiveGathering × 0.3))
```
- 50% base yield, scaled by crew proficiency
- Time proportional to amount and proficiency

**Relic Search:**
```
successRate = 0.4 + effectiveSearching × 0.3
```
- Success: 10 food + 15 wood
- Failure: 5 food + 5 stone (partial success)
- Partial time (overtime): scaled success rate × 0.2–0.6, partial loot

**Monster Hunt:**
```
successRate = 0.2 + effectiveHunting × 0.3 - difficulty × 0.05
```
- Success: 20 food + 1–3 monster parts (Fangs/Hides/Claws)
- Failure: Crew takes 5–15 damage (or dies at low HP)
- Difficulty ranges 1–3, higher = harder

### Time Calculation

```
travelTime = (BASE_TRAVEL_TIME + distance × 1.8) / effectiveSpeed
actionTime = baseActionTime × difficultyFactor / crewProficiency
totalTime = travelTime × 2 + actionTime
```

Base times (from config):
| Action | Base Time (ms) |
|--------|---------------|
| Travel | 500 |
| Gather | 3000 |
| Search | 4500 |
| Hunt | 5000 |

### Crew Proficiency Modifiers

Each crew has base proficiencies (1.0 = normal). Effective values are modified by:

**Perks (multiplicative):**
| Perk | Effect |
|------|--------|
| Fast Hands | Gathering × 2.0 |
| Night Vision | Searching × 1.5 |
| Gunslinger | Hunting × 2.0 (only with gun equipment) |
| Blacksmith | Iron gather × 3 |
| Quick Reflex | Speed × 1.3 |
| Tough | Max HP × 1.5 |

**Equipment slots:**
| Slot | Stat Bonuses |
|------|-------------|
| Weapon | Hunting bonus + Gathering bonus |
| Armor | Defense bonus |
| Accessory | Searching bonus |

---

## Phase 2.5: Collaborative Missions

When multiple crews target the same ResourceNode, the system groups them and calculates:

```
totalProficiency = sum of all crew proficiencies
avgTravelTime = average of all crew travel times
actionTime = target.getActionTime(totalProficiency)  ← faster!
```

**Gathering bonus:**
```
amount = floor(target.amount × 0.5 × bonusMultiplier × avgProficiency)
bonusMultiplier = 1 + (crewCount - 1) × 0.3
```

**Success rate boost for groups:**
- Relic: +0.05 per additional crew
- Monster: +0.15 per additional crew

Damage is distributed across all participating crews (shared damage).

---

## Phase 3: Night Phase

### Monster Attacks

After each day ends, night begins. Monsters spawn and attack the base:

**Monster count:** `2 + floor(day / 3)`
- Day 1–2: 2 monsters
- Day 3–5: 3 monsters
- Day 6–8: 4 monsters
- Day 9–11: 5 monsters
- ... scales up every 3 days

**Monster properties:**
```
HP = 20 + difficulty × 10
Damage = 5 + difficulty × 3
Speed = 50 + difficulty × 20 (pixel/s, visual only)
```

**Attack mechanics:**
- Monsters spawn at random position around base (distance 80–200px)
- Each monster travels toward base with a random wiggle
- On arrival: deals 5–14 damage to base HP
- Base HP shown in top HUD
- Resources lost to monsters: half of each type gathered today

**Visual feedback:**
- Damage number floats up from base (`-X HP!`)
- Camera shake on hit
- Base color: green (healthy) → red (critical)

### Monster Types

| Name | Difficulty Range |
|------|-----------------|
| Goblin | 1–2 |
| Wolf | 1–3 |
| Giant Spider | 1–3 |
| Shadow Demon | 1–3 |
| Night Stalker | 1–3 |
| Boss | 3 |

---

## Time System

### Day/Night Cycle

| Phase | Duration | Behavior |
|-------|----------|----------|
| Day | 12000 time units (~12 seconds real-time) | Planning + execution window |
| Night | 5 seconds | Monsters attack, then new day starts |
| Total per day | ~17 seconds | 30 days × 17s = ~8.5 minutes full game |

### Time Units

- Time advances at +100 units per second during execution
- All action times are in the same time unit scale
- Day time limit: 12000 units
- Remaining time shown in HUD: `⏳ Left: X units`

### Over-Time Handling

If a mission's total time exceeds the day time limit:
- The mission still executes (not cancelled)
- Partial results: time-ratio determines how much is gathered
  ```
  partialAmount = floor(target.amount × 0.5 × ratio)
  ratio = remainingTime / actionTime
  ```
- Warning shown: "⚠️ Chain needs X units but only Y left"
- Crew may not complete full action → reduced loot or nothing

---

## Resource System

### Resource Types

| Resource | Icon | Color | Base Gather Time | Use |
|----------|------|-------|-----------------|-----|
| Wood | 🪵 | #8B6914 | 3000 | Construction |
| Stone | 🪨 | #808080 | 3500 | Construction |
| Iron | ⛏️ | #A0A0A0 | 4000 | Crafting/Equipment |
| Food | 🍖 | #FF6B35 | 2500 | Crew sustenance |
| Water | 💧 | #4A90D9 | 2000 | Crew sustenance |
| Circuit | ⚡ | #FFD700 | 5000 | Advanced crafting |
| Aluminum | 🔩 | #C0C0C0 | 4500 | Advanced crafting |

### Monster Parts

| Part | Source | Use |
|------|--------|-----|
| Fangs | Monster hunt success | Equipment/material |
| Hides | Monster hunt success | Armor/material |
| Claws | Monster hunt success | Weapon/material |

### Resource Flow

```
Day Phase → Gather/Search/Hunt → Resources stored
                    ↓
New Day → Consume Food (2 per crew) → Remaining stored
                    ↓
Night → Base damaged, half of today's resources lost
```

**Daily food consumption:** `2 × aliveCrewCount`

---

## Crew System

### Crew Statistics

Each crew has these base stats (range: 0.7–1.8):

| Stat | Range | Description |
|------|-------|-------------|
| HP | 80–120 | Health; 0 = dead (permadeath) |
| Speed | 0.8–1.8 | Movement speed multiplier |
| Gathering | 0.6–2.0 | Resource gathering proficiency |
| Searching | 0.6–1.8 | Relic search proficiency |
| Hunting | 0.7–1.5 | Monster hunting proficiency |
| Hire Cost | 25–35 | Crew points cost to hire |

### Starting Crew (3 hired at game start)

| Name | HP | Speed | Gather | Search | Hunt | Perk | Cost |
|------|----|-------|--------|--------|------|------|------|
| Sarah | 100 | 1.2 | 1.5 | 0.8 | 0.9 | Fast Hands | 25 |
| John | 120 | 0.9 | 0.7 | 1.8 | 1.2 | Night Vision | 30 |
| Emma | 90 | 1.5 | 1.2 | 1.0 | 1.5 | Gunslinger | 35 |

**Total starting crew points:** 100
**Starting resources:** 100 food

### Crew States

| State | Description |
|-------|-------------|
| **Idle** | Available for mission assignment |
| **Busy** | On a mission, cannot be selected |
| **Dead** | HP ≤ 0, permanent removal from game |

### Perks (6 total)

| Perk Name | Emoji | Effect | Crew Type |
|-----------|-------|--------|-----------|
| Fast Hands | 🌪️ | Gathering time reduced 50% (proficiency ×2) | Gatherer type |
| Night Vision | 🌙 | Searching efficiency +50% (proficiency ×1.5) | Scout/Searcher type |
| Gunslinger | 🔫 | Hunting ×2 when equipped with gun | Hunter type |
| Blacksmith | ⚒️ | Iron gathering ×3 | Specialist |
| Quick Reflex | ⚡ | Movement speed +30% | Scout type |
| Tough | 💪 | Max HP +50% | Tank type |

---

## Map Generation

### Resource Node Distribution

Each new day, a new map is generated:

| Node Type | Count | Position Rules |
|-----------|-------|---------------|
| Resource nodes (wood/stone/iron/food/water) | 4–7 | Random, ≥150px from base |
| Relic node | 1 | Random, ≥200px from base |
| Monster node | 1 | Random, ≥180px from base |

**Node properties:**
- Amount: 30–150 (random)
- Position: Random within playfield bounds (50–1230px X, 80–640px Y)
- Base position: Random each day (100–1080px X, 80–440px Y)

### Map Layout

```
┌─────────────────────────────────────────────┐
│  Grid overlay (decorative, 60×50px cells)   │
│                                             │
│    ○ Resource     🏛️ Relic    👹 Monster   │
│    (yellow)       (purple)     (red)        │
│                                             │
│              🏠 BASE                        │
│    [crew positions around base]             │
│                                             │
│  Bottom: Crew panel (3 rows)                │
│  Bottom: Planning panel                     │
│  Right: Resource panel                      │
└─────────────────────────────────────────────┘
```

---

## UI Layout

### HUD (Top Bar)

| Element | Position | Content |
|---------|----------|---------|
| Day counter | Top-left | `Day X/30` |
| Elapsed time | Top-left | `⏱️ Elapsed: X / 12000 units` |
| Base HP | Top-left | `🏠 HP: XXX` |
| Remaining time | Top-left | `⏳ Left: X units` |

### Game Panels

| Panel | Position | Size | Content |
|-------|----------|------|---------|
| Resource Panel | Right (1090, 60) | 180×230 | 7 resource types with counts |
| Crew Panel | Bottom-left (30, 520) | 1240×130 | Crew list with pagination |
| Planning Panel | Bottom (30, 680) | 1240×55 | Selection status + buttons |
| Mission Display | Center overlay | Dynamic | Chain preview with times |
| Notification System | Top-center | Dynamic | Status messages |

### Panel Buttons

**Planning Panel:**
| Button | State | Action |
|--------|-------|--------|
| ADD TO QUEUE | Visible when crew selected + targets chained | Confirm chain → add to queue |
| EXECUTE ALL | Visible when queue has missions | Start all queued missions |
| RESET | Visible when chain has nodes | Clear entire selection |
| UNDO | Visible when chain has nodes | Remove last node from chain |

**Crew Panel:**
- Pagination: 3 crews per page
- Clickable crew text (when idle) → selects crew
- Shows status: 🟢 Ready / 🔴 Busy
- Shows chain count: 📋xN
- Shows perk, stats at 11px size

---

## Player Strategy Tips

### Optimal Crew Assignment
- **Sarah** (Fast Hands + high gather) → Send to gather nodes
- **John** (Night Vision + high search) → Send to relic nodes
- **Emma** (Gunslinger + high hunt) → Send to monster nodes

### Chain Strategy
- Longer chains save travel time between nearby targets
- But total time increases — watch the day time limit
- Group multiple crews on high-value targets for collaborative bonus

### Resource Priorities
- **Food** is critical — consume 2/crew/day; run out = game over
- **Wood/Stone** early for base durability
- **Iron/Circuit/Aluminum** late-game for equipment/crafting (prototype incomplete)
- **Monster parts** for equipment unlocks

### Night Defense
- Monster count scales: +1 every 3 days
- Base HP is the only HP pool — lose it = game over
- No active defense (prototype) — only resource management
- Strategy: gather food/wood to sustain through hard nights