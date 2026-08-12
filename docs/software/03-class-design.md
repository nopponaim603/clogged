# Class Design Document — prototype_resource_game

> **Game**: SURVIVAL BASE — 30-day survival resource management game  
> **Engine**: Phaser 3 (TypeScript)  
> **Resolution**: 1280x720  
> **Game Loop**: Day Phase (Planning + Execution) → Night Phase (Defense) → Loop

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Scene Flow](#scene-flow)
3. [Class Directory Map](#class-directory-map)
4. [Entities](#1-entities)
5. [Systems](#2-systems)
6. [UI Components](#3-ui-components)
7. [Scenes](#4-scenes)
8. [Data Layer](#5-data-layer)
9. [Utility Layer](#6-utility-layer)
10. [Class Relationship Diagram](#class-relationship-diagram)

---

## Architecture Overview

The project follows a **layered architecture** typical of game development:

```
┌─────────────────────────────────────────────┐
│                 SCENES                       │  ← State Management (Phaser Scene)
├─────────────────────────────────────────────┤
│  DayScene │ NightScene │ TravelScene │ ...   │
├─────────────────────────────────────────────┤
│         UI COMPONENTS                       │  ← Presentation Layer
│  PlanningPanel │ CrewPanel │ ResourcePanel   │
├─────────────────────────────────────────────┤
│           SYSTEMS                           │  ← Business Logic
│  MissionSystem │ CrewManager │ ResourceManager│
├─────────────────────────────────────────────┤
│            ENTITIES                         │  ← Data Models
│  Crew │ ResourceNode │ Monster │ Base        │
├─────────────────────────────────────────────┤
│        DATA LAYER  │  UTILS                 │  ← Static Data & Helpers
│  Constants │ ResourceData │ CrewData         │
└─────────────────────────────────────────────┘
```

**Dependency Flow**: Scenes → UI → Systems → Entities → Data/Utils (bottom-up)

---

## Scene Flow

```
BootScene → MenuScene → TravelScene → DayScene → NightScene → TravelScene → DayScene → ... → GameOverScene
              ↑                                                      │
              └──────────────────── Restart ─────────────────────────┘
```

| Scene | Responsibility |
|-------|---------------|
| BootScene | Brief loading screen, transitions to MenuScene |
| MenuScene | Title screen with START button |
| TravelScene | Navigation/transition between nights, displays crew & resource summary |
| DayScene | Core gameplay — planning missions, executing them, simulation |
| NightScene | Tower defense mini-game, enemy waves, base defense |
| GameOverScene | End-game screen with stats |

---

## Class Directory Map

```
src/
├── config.ts                          # Game constants
├── main.ts                            # Phaser Game entry point
│
├── entities/                          # Data models
│   ├── Crew.ts                        # Crew member with stats, perks, equipment
│   ├── ResourceNode.ts                # World resource/gathering node
│   ├── Monster.ts                     # Night attack monster
│   └── Base.ts                        # Player base (HP)
│
├── systems/                           # Business logic
│   ├── TimeSystem.ts                  # Day/Night timer, speed control
│   ├── ResourceManager.ts             # Resource inventory management
│   ├── MissionSystem.ts               # Mission execution logic
│   ├── MapGenerator.ts                # World generation (nodes, positions)
│   └── CrewManager.ts                 # Crew lifecycle management
│
├── ui/                               # Presentation
│   ├── UIManager.ts                   # Top-level UI orchestrator
│   ├── SpeedControl.ts               # Speed/skip controls
│   ├── ResultPopup.ts                # Day summary popup
│   ├── ResourcePanel.ts              # Resource display panel
│   ├── PlanningPanel.ts              # Mission planning & queue panel
│   ├── NotificationSystem.ts         # Toast notifications
│   ├── MissionDisplay.ts             # Mission visualization overlays
│   └── CrewPanel.ts                  # Crew status display with pagination
│
├── scenes/                           # Game state scenes
│   ├── BootScene.ts
│   ├── MenuScene.ts
│   ├── TravelScene.ts
│   ├── DayScene.ts                    # Main gameplay scene (largest)
│   ├── NightScene.ts                  # Defense mini-game
│   └── GameOverScene.ts
│
├── data/                             # Static data tables
│   ├── Constants.ts                  # Game constants, perks, colors
│   ├── ResourceData.ts               # Resource type definitions
│   └── CrewData.ts                   # Crew templates
│
└── utils/                            # Helper utilities
    ├── Helpers.ts                    # Math, format, random utilities
    └── RandomGenerator.ts            # Game-specific random generators
```

---

## 1. Entities

### 1.1 Crew

**File**: `src/entities/Crew.ts`  
**Purpose**: Represents a player-controlled crew member with stats, equipment, and perks.

| Field | Type | Description |
|-------|------|-------------|
| `id` | `number` | Unique identifier |
| `name` | `string` | Crew member name |
| `isAlive` | `boolean` | Dead/alive state |
| `isBusy` | `boolean` | Currently assigned to a mission |
| `baseSpeed` | `number (0-100)` | Movement speed stat |
| `baseGathering` | `number (0-100)` | Resource gathering stat |
| `baseSearching` | `number (0-100)` | Relic searching stat |
| `baseHunting` | `number (0-100)` | Monster hunting stat |
| `hp` / `maxHp` | `number` | Health points |
| `perks` | `string[]` | Active perk IDs |
| `equipment` | `Equipment` | Weapon/armor/accessory slots |
| `hireCost` | `number` | Crew points cost |
| `position` | `{x, y}` | World coordinates |

**Key Methods**:
- `getEffectiveSpeed()` — speed with equipment + perk multiplier
- `getEffectiveGathering()` — gathering with equipment + perk multiplier  
- `getEffectiveSearching()` — searching with equipment + perk multiplier
- `getEffectiveHunting()` — hunting with equipment + perk multiplier
- `calculateTravelTime(distance)` — computes travel time based on distance & speed
- `getRankDisplay(value)` — returns Rank enum and color for a stat value
- `takeDamage(damage)` — reduces HP, returns true if killed
- `heal(amount)` — restores HP up to max

**Enums & Types**:
- `Rank` — F, E, D, C, B, A, AA, AAA, S, SS, SSS, EX (12 ranks)
- `RANK_THRESHOLDS` — mapping of stat value ranges to ranks
- `Equipment` — `{weapon, armor, accessory}` slots with type-specific bonuses

---

### 1.2 ResourceNode

**File**: `src/entities/ResourceNode.ts`  
**Purpose**: Represents a world node that crews can interact with — gathering resource, searching for relic, or fighting monster.

| Field | Type | Description |
|-------|------|-------------|
| `id` | `string` | Unique node identifier |
| `type` | `'wood'/'stone'/...` | Resource type or `'relic'`/`'monster'` |
| `position` | `{x, y}` | World coordinates |
| `amount` | `number` | Resource quantity (for gathering) |
| `isRelic` | `boolean` | Whether this is a relic node |
| `isMonster` | `boolean` | Whether this is a monster node |
| `difficulty` | `number` | Monster difficulty tier |
| `hp` / `maxHp` | `number` | Node HP (relics/monsters use HP-based combat) |
| `rank` / `rankName` | `number`/`string` | Rarity tier (0-3, Common→Epic) |
| `isDepleted` | `boolean` | Whether the node has been fully gathered |

**HP Calculation**:
- **Gathering nodes**: `amount * GATHER_HP_PER_UNIT[rank]` — rank determined by amount thresholds
- **Relic nodes**: random HP in [5000, 20000] × rarity multiplier [1, 2, 4, 8]
- **Monster nodes**: random HP in [5000, 20000] × difficulty-based multiplier

**Key Methods**:
- `getActionTime(crewProficiency)` — time needed = HP / proficiency (capped at 0.1 min)
- `takeDamage(damage)` — reduces HP, sets depleted when HP reaches 0
- `getHpPercentage()` — current HP as percentage of max
- `getColor()` — visual color (purple=relic, red=monster, yellow=gather)

---

### 1.3 Monster

**File**: `src/entities/Monster.ts`  
**Purpose**: Simple monster entity for night defense gameplay.

| Field | Type | Description |
|-------|------|-------------|
| `id` | `string` | Unique identifier |
| `name` | `string` | Monster name |
| `x` / `y` | `number` | Position |
| `hp` / `maxHp` | `number` | Health |
| `damage` | `number` | Damage per hit |
| `speed` | `number` | Movement speed |
| `difficulty` | `number` | Difficulty tier |

**Key Methods**: `takeDamage()`, `isDead()`, `getColor()`

> **Note**: `Monster` entity is defined but the active night game uses inline `Monster` interfaces in `NightScene.ts` rather than this class.

---

### 1.4 Base

**File**: `src/entities/Base.ts`  
**Purpose**: Represents the player's base with HP.

| Field | Type | Description |
|-------|------|-------------|
| `x` / `y` | `number` | Position on map |
| `hp` / `maxHp` | `number` | Base health |
| `sprite` | `Arc` | Phaser visual reference |

**Key Methods**: `takeDamage()`, `getHpPercentage()`, `isDestroyed()`, `setPosition()`

---

## 2. Systems

### 2.1 TimeSystem

**File**: `src/systems/TimeSystem.ts`  
**Purpose**: Controls the day/night cycle timer, simulation speed, and execution timing.

| Field | Type | Description |
|-------|------|-------------|
| `day` | `number` | Current day number (1–30) |
| `worldTime` | `number` | Elapsed time within the current day |
| `dayTimeLimit` | `number` | Maximum time per day (12000 units) |
| `isPlanningPhase` | `boolean` | Crews can still be assigned |
| `isExecuting` | `boolean` | Missions are running |
| `isNightPhase` | `boolean` | Night defense phase active |
| `speed` | `number` | Simulation speed multiplier (1, 2, or 4) |
| `onDayEnd` | `() => void` | Callback when day completes |
| `onNightEnd` | `() => void` | Callback when night completes |
| `onSimulateStep` | `() => void` | Tick callback for mission simulation |

**Key Methods**:
- `startDay(scene)` — initializes a new day, sets planning phase
- `startExecution(scene)` — begins mission execution with timer loop
- `setSpeed(newSpeed)` — changes simulation speed, restarts timer if executing
- `skipDay()` — instantly advances to end of day
- `endDay()` → `onDayEnd` — transitions to night phase
- `startNight(scene)` → delayed `endNight` → `onNightEnd` → increments day
- `getRemainingTime()` — time left in current day

---

### 2.2 ResourceManager

**File**: `src/systems/ResourceManager.ts`  
**Purpose**: Central resource inventory for the player.

| Field | Type | Description |
|-------|------|-------------|
| `resources` | `ResourceInventory` | All collected resources |
| `dayResources` | `Partial<ResourceInventory>` | Resources gathered this day (lost 50% overnight) |
| `baseHP` / `maxBaseHP` | `number` | Base health |

**Resources tracked**: wood, stone, iron, food, water, circuit, aluminum, monsterParts (fangs, hides, claws)

**Key Methods**:
- `addResource(type, amount)` — adds to inventory + dayResources
- `addMonsterPart(type, amount)` — adds monster loot
- `getResource(type)` — reads resource count
- `loseDayResources()` — loses 50% of day's gathering (night cost)
- `consumeFood(crewCount)` — deducts food, returns success/shortage info
- `takeBaseDamage(damage)` — reduces base HP

---

### 2.3 MissionSystem

**File**: `src/systems/MissionSystem.ts`  
**Purpose**: Core game logic — determines mission outcomes based on crew stats, time constraints, and target types.

**Dependencies**: Takes `ResourceManager` in constructor.

**Mission Types** (separate execution logic):

| Type | Crew Stat Used | Action |
|------|---------------|--------|
| Gathering | `baseGathering` | Collects specified amount of resource |
| Relic Search | `baseSearching` | Attempts to find relic (success rate formula) |
| Monster Hunt | `baseHunting` | Fights monster (damage-based) |

**Collaborative Missions** (multiple crews):
- `executeCollaborativeMission(crews[], target)` — combines all crew stats for joint action
- Individual sub-methods for each type with synergy bonuses (+20% per additional crew)

**Key Methods**:
- `executeMission(crew, target)` — dispatches to single-type handler
- `executeCollaborativeMission(crews, target, travelTime)` — dispatches to collaborative handler

**Time handling**: If `totalTime > dayTimeLimit`, partial execution — crew gets proportional results based on remaining time.

---

### 2.4 MapGenerator

**File**: `src/systems/MapGenerator.ts`  
**Purpose**: Generates the game world each day — resource nodes, one relic, one monster node.

**Key Methods**:
- `generateResources()` — creates 4-7 random resource nodes + 1 relic + 1 monster
- `createResourceNode(type)` — places a resource node with minimum distance from base
- `createRelicNode()` — places a relic node (farther from base)
- `createMonsterNode()` — places a monster node with random difficulty

**Placement Logic**: Each node avoids spawning within minimum distance of the base.

---

### 2.5 CrewManager

**File**: `src/systems/CrewManager.ts`  
**Purpose**: Manages crew hiring, mission assignment lifecycle, and availability tracking.

| Field | Type | Description |
|-------|------|-------------|
| `crews` | `Crew[]` | All hired crews |
| `availableCrews` | `Crew[]` | Crews not on missions |
| `busyCrews` | `Crew[]` | Crews currently on missions |

**Key Methods**:
- `generateRandomCrew(hireCost)` — creates a crew from CREW_TEMPLATES with random selection
- `hireCrew(crew, points)` — adds crew if enough points, returns remaining points
- `assignMission(crewId, targetNode)` — marks crew as busy, moves to busyCrews
- `completeMission(crewId)` — marks crew as free, returns to availableCrews if alive
- `getAvailableCount()` / `getBusyCount()` — quick status checks

---

## 3. UI Components

### 3.1 UIManager

**File**: `src/ui/UIManager.ts`  
**Purpose**: Top-level UI orchestrator that creates and manages all UI elements on the game world.

**Created UI Elements**: Top bar (day, time, base HP, points), Resource panel, Map area (base + nodes), Bottom panel (crew status), Planning panel (selection + confirm).

**Key Methods**: `setupUI()`, `setupInteractions()`, `updateSelection()`, `updatePlanStatus()`, `showNotification()`, `showDamageEffect()`, `showNightPhase()`

---

### 3.2 SpeedControl

**File**: `src/ui/SpeedControl.ts`  
**Purpose**: 1x/2x/4x speed toggle + skip button, used in Day and Night scenes.

| Field | Type | Description |
|-------|------|-------------|
| `speed` | `number` | Current speed level |
| `container` | `Container` | Phaser container for all speed UI elements |

**Key Methods**: `setSpeed(speed)`, `setVisible(visible)`, `getSpeed()`

---

### 3.3 ResultPopup

**File**: `src/ui/ResultPopup.ts`  
**Purpose**: Day-end summary popup showing all results — resources, monster parts, relics found, crew status, missions completed.

| Field | Type | Description |
|-------|------|-------------|
| `DayResult` | `interface` | Full day result data structure |

**Shows**: Time used, all resource counts, monster parts, relics found, crew HP status, missions completed, base HP lost.

---

### 3.4 ResourcePanel

**File**: `src/ui/ResourcePanel.ts`  
**Purpose**: Displays current resource inventory on the right side of the screen.

**Key Methods**: `update()` — refreshes all resource text displays, `show()`, `hide()`

---

### 3.5 PlanningPanel

**File**: `src/ui/PlanningPanel.ts`  
**Purpose**: Bottom panel for mission planning — shows selection status, chain count, and action buttons (Reset, Undo, Add to Queue, Execute All).

**Key Methods**: `updateSelection()`, `setStatus()`, `showExecuteButton()`, `hideExecuteButton()`

---

### 3.6 NotificationSystem

**File**: `src/ui/NotificationSystem.ts`  
**Purpose**: Toast notification system for game events.

**Key Methods**: `show()`, `showSuccess()`, `showError()`, `showWarning()`, `showInfo()`, `clearAll()`

**Behavior**: Auto-dismiss after duration, max 5 simultaneous notifications, slide-up animation.

---

### 3.7 MissionDisplay

**File**: `src/ui/MissionDisplay.ts`  
**Purpose**: Visualizes active missions — draws lines from crew to target, shows mission info text with travel/action/total times, and time warnings.

**Key Methods**: `showMission(crew, target)`, `showMissionChain(crew, targets[])`, `clear()`, `update()`

---

### 3.8 CrewPanel

**File**: `src/ui/CrewPanel.ts`  
**Purpose**: Displays crew status cards with stats, perks, rank, and chain counts. Supports pagination.

| Field | Type | Description |
|-------|------|-------------|
| `currentPage` | `number` | Current page index |
| `crewsPerPage` | `number` | 3 per page |

**Shows**: Name, HP, status (Busy/Ready), perks, effective stats with rank badges, chain mission count.

---

## 4. Scenes

### 4.1 BootScene
**File**: `src/scenes/BootScene.ts`  
**Purpose**: Brief loading screen (1.5s), shows "Loading..." with animated dots. Transitions to `MenuScene`.

---

### 4.2 MenuScene
**File**: `src/scenes/MenuScene.ts`  
**Purpose**: Title screen with game info and START button. Transitions to `TravelScene` with `day: 1, isFirstTime: true`.

---

### 4.3 TravelScene
**File**: `src/scenes/TravelScene.ts`  
**Purpose**: Inter-day transition scene. Shows crew status, resource summary, and path preview.

**Responsibilities**:
- On first visit: initializes all game systems (`CrewManager`, `ResourceManager`, `MapGenerator`, `TimeSystem`)
- On subsequent visits: passes existing systems from previous scene via data
- Transitions to `DayScene` with all systems and current day number

---

### 4.4 DayScene (Core Gameplay)
**File**: `src/scenes/DayScene.ts`  
**Purpose**: The main gameplay scene — player plans missions, executes them, and completes the day.

**Subsystems Created**:
- `CrewManager` — manages all crew lifecycle
- `ResourceManager` — tracks all resources
- `MapGenerator` — generates world nodes
- `MissionSystem` — calculates mission outcomes
- `TimeSystem` — controls day timer
- `SpeedControl` — speed/acceleration
- `NotificationSystem` — toast messages
- `MissionDisplay` — mission visualization
- `ResultPopup` — day summary
- `ResourcePanel`, `CrewPanel`, `PlanningPanel` — UI

**Game Flow**:
1. `initSystems()` → create all game systems
2. `generateWorld()` → create base sprite + resource nodes
3. `createInitialCrews()` → hire 4 random crews from templates
4. `setupUI()` → create all UI panels
5. `startDay()` → enter planning phase
6. Player selects crew → selects targets → builds chain → confirms
7. Player executes all missions → simulation runs
8. `simulateStep()` processes each crew's mission (travel → action → return)
9. Missions complete → collect results
10. Show `ResultPopup` → transition to `NightScene`

**Mission Queue** (`QueuedMission` interface):
- Tracks phase: `travel_out` → `action` → `travel_back` → `complete`
- Handles chained missions (crew visits multiple nodes sequentially)
- Each mission knows its `chainIndex` for ordering
- `usedTime` tracks progress toward `totalTime`

**Key Methods**:
- `selectCrew(crew)` / `selectTarget(target)` — UI selection
- `confirmChain()` — validates and adds to mission queue
- `executeAllMissions()` — starts simulation
- `processCrewMission()` — per-tick mission progress
- `handleMissionComplete()` — resolves mission outcome, handles chains
- `collectDayResult()` — builds DayResult for popup
- `startNightPhase()` → `NightScene`
- `checkGameOver()` / `showGameOver()` — win/lose conditions

---

### 4.5 NightScene
**File**: `src/scenes/NightScene.ts`  
**Purpose**: Tower defense mini-game where the player defends the base from monster waves.

**Key Systems**:
- **Tower Defense**: 3 tower types (gun/cannon/laser) with different damage, range, cooldown
- **Enemy Waves**: Increasing difficulty — `totalEnemies = 10 + day * 3`
- **Spawning**: 4 spawn points (N/S/E/W), enemies approach base center

**Monsters** (inline interface):
- `type`: normal / fast / tank / boss
- Each type has different HP, speed, damage, reward
- Boss spawns every 3 days at 0 enemy spawn milestone

**Key Methods**:
- `spawnEnemy()` — creates enemy from spawn point with type-based stats
- `updateMonsters()` — moves enemies toward base, damage on contact
- `updateTowers()` — tower auto-fire logic, cooldown management
- `checkNightComplete()` — all enemies processed → show results
- `skipNight()` — instant-completes the night
- `showNightComplete()` — stats popup before returning to `TravelScene`

---

### 4.6 GameOverScene
**File**: `src/scenes/GameOverScene.ts`  
**Purpose**: End-game screen. Shows victory or defeat message, days survived, and restart button.

---

## 5. Data Layer

### 5.1 Constants (`src/data/Constants.ts`)
- `COLORS` — UI color palette (primary, secondary, accent, danger, etc.)
- `CREW_NAMES` — name pool for random crew names
- `PERKS` — list of 11 perk IDs
- `PERK_DESCRIPTIONS` — human-readable perk descriptions with emoji
- `MONSTER_NAMES` — monster name pool
- `RANK_NAMES` / `RANK_COLORS` — rank display mappings

### 5.2 ResourceData (`src/data/ResourceData.ts`)
- `RESOURCE_DATA` — defines 7 resource types with id, name, icon, color, base gather time
- `RELIC_DATA` — relic definition (name, icon, color, base search time)
- `MONSTER_DATA` — monster definition (icon, color, base hunt time)

### 5.3 CrewData (`src/data/CrewData.ts`)
- `CrewTemplate` interface — template definition for crew generation
- `CREW_TEMPLATES` — 8 predefined crew with unique stat profiles, perks, and costs

| Template | Name | Specialty | Perk |
|----------|------|-----------|------|
| 1 | Sarah | Fast gatherer | fast_hands |
| 2 | John | Expert searcher | night_vision |
| 3 | Emma | Deadly hunter | gunslinger |
| 4 | Mike | Metal gatherer | blacksmith |
| 5 | Lisa | Fast scout | quick_reflex + scout |
| 6 | Tom | Tanky hunter | tough + strong |
| 7 | Anna | Balanced medic | lucky + medic |
| 8 | Leo | Fast searcher | sprinter + night_vision |

---

## 6. Utility Layer

### 6.1 Helpers (`src/utils/Helpers.ts`)
Static utility class:
- `randomRange(min, max)` — float range
- `randomInt(min, max)` — integer range
- `clamp(value, min, max)` — value clamping
- `distance(x1,y1,x2,y2)` — Euclidean distance
- `formatTime(seconds)` — MM:SS formatting
- `shuffleArray<T>` — Fisher-Yates shuffle
- `pickRandom<T>` — random element selection
- `getResourceIcon(type)` — emoji mapping
- `getMonsterEmoji(difficulty)` — emoji by difficulty

### 6.2 RandomGenerator (`src/utils/RandomGenerator.ts`)
Game-specific random generators:
- `generateCrewName()` — random crew name
- `generatePerk()` — random perk selection
- `generateMonsterName()` — random monster name
- `generateResourceType()` — random resource type
- `generateResourceAmount()` — random amount (20-100)
- `generateCrewStats()` — random stat array (0.7-1.4 range, ×10)
- `generatePosition(maxX, maxY, padding)` — random world position

---

## Class Relationship Diagram

```
                    ┌──────────────────────────────────────┐
                    │              main.ts                  │
                    │  Phaser.Game (6 scenes registered)    │
                    └──┬──┬──┬──┬──┬──┬────────────────────┘
                       │  │  │  │  │  │
                    Boot Menu Travel Day Night GameOver
                    Scene Scene Scene Scene Scene Scene
                       │  │    │    │    │     │
                       │  │    │    │    └─────┘
                       │  │    │    └──────────────────────────┐
                       │  │    │                              │
                       │  │    └──────────────────────────────┘
                       │  │
            ┌──────────┘  └──────────┐
            │                        │
      ┌─────▼─────┐           ┌──────▼──────┐
      │ DayScene  │           │ NightScene  │
      └─────┬─────┘           └──────┬──────┘
            │                        │
   ┌────────┼────────────────┐       │
   │        │                │       │
   │  ┌─────▼─────┐ ┌───────▼──────┐ │
   │  │CrewManager│ │MissionSystem │◄┘
   │  └─────┬─────┘ └───────┬──────┘
   │        │               │
   │  ┌─────▼─────┐   ┌─────▼────────┐
   │  │ResourceManager│
   │  └─────┬─────┘   └──────────────┘
   │        │
   │  ┌─────▼─────┐
   │  │MapGenerator│
   │  └───────────┘
   │
   │  ┌─ UI Components (use systems via DayScene) ─┐
   │  │ ResourcePanel │ CrewPanel │ PlanningPanel   │
   │  │ SpeedControl  │ ResultPopup                    │
   │  │ Notification  │ MissionDisplay                 │
   │  └──────────────────────────────────────────────┘
   │
   └─┬─ Entities (used by Systems)
     │
     ├── Crew ──────────┐
     ├── ResourceNode ──┼──► MissionSystem.executeMission()
     ├── Monster        │
     └── Base  ─────────┘

   ┌─ Data Layer (read-only) ─┐
   │ Constants │ ResourceData  │ CrewData
   └──────────────────────────┘

   ┌─ Utils ────────────────┐
   │ Helpers │ RandomGenerator
   └────────────────────────┘
```

---

## Key Game Mechanics

### Day Cycle
1. **Planning Phase** — Select crew, click resource nodes to build chains, confirm
2. **Execution Phase** — Missions execute in order with timing simulation
3. **Day End** — Results shown, time resources are lost (50%)

### Night Cycle
1. Enemies spawn from 4 directions
2. Player places defensive towers
3. Towers auto-fire at enemies in range
4. Enemies reaching base deal damage
5. Survive all waves → next day

### Mission Chain System
- Crew can visit multiple nodes in sequence
- Travel time between nodes is calculated individually
- Only the final node triggers return-to-base travel
- Chain continues to next node if crew survives previous

### Combat (Resource Nodes)
- Relics/Monsters use HP-based combat instead of direct collection
- Time pressure: `timeForAction = dayTimeLimit - travelTime * 2`
- If time insufficient, partial damage is dealt, proportional results given
- Crew stats determine effectiveness (gathering/searching/hunting)

---

## Game Constants Summary

| Constant | Value | Description |
|----------|-------|-------------|
| `WIDTH` | 1280 | Game width |
| `HEIGHT` | 720 | Game height |
| `DAY_DURATION` | 30s | Real-time day display |
| `MAX_DAYS` | 30 | Total game days |
| `BASE_HP` | 100 | Starting base health |
| `CREW_POINTS` | 100 | Starting crew points |
| `STARTING_FOOD` | 100 | Starting food |
| `DAY_TIME_LIMIT` | 12000 | Max game-time per day |
| `TIME_UNIT_PER_SECOND` | 100 | Time unit scale |
| `DEFAULT_SPEED` | 1 | Starting simulation speed |
| `SPEED_OPTIONS` | [1, 2, 4] | Speed multipliers |
| `GATHER_HP_PER_UNIT` | [500,1000,2000,4000] | HP per unit by rank |
