---
name: unity-patterns
description: >
  Catalog and decision guide for the 23 game-programming design patterns documented in
  docs/wiki/patterns/ (Command, Flyweight, Observer, Prototype, Singleton, State, Double
  Buffer, Game Loop, Update Method, Bytecode, Subclass Sandbox, Type Object, Component,
  Event Queue, Service Locator, Data Locality, Dirty Flag, Object Pool, Spatial Partition,
  Decorator, Factory, Facade, Template). Use whenever the user asks which pattern solves a
  gameplay/architecture problem ("how do I decouple X", "how do I avoid instantiating so
  many bullets", "how do I structure enemy AI states"), asks to implement, refactor, or
  explain one of these patterns, asks how two patterns relate or differ, or asks whether
  Unity already has a built-in equivalent. Ships its own condensed copy of every write-up in
  references/patterns-reference.md, so it works even without the rest of the repo present.
  Points to the runnable C# reference implementations under
  Assets/UnityTechnologies/LevelUpYourCode/_DesignPatterns/ where they exist.
---

# Unity Design Patterns

This repo's `docs/wiki/patterns/` is a 23-pattern knowledge base (mostly from the book
*Game Programming Patterns*, plus a few classic GoF patterns). This skill turns that prose
into a fast lookup: **what problem → which pattern → where's the code.**

For pattern nuance, read **[references/patterns-reference.md](references/patterns-reference.md)**
— it's a self-contained, condensed copy of all 23 write-ups (problem, how to implement, when
it's useful, related patterns) that ships inside this skill folder, so the skill stays usable
even if `docs/` isn't in context. `docs/wiki/patterns/<N>-<name>.md` remains the canonical,
editable source in this repo; if the two ever disagree, trust the wiki and refresh the local
copy from it. The tables below are a further-compressed index into that reference file — read
the reference (or the wiki) before writing an explanation longer than a couple of sentences.

## Source locations

1. **[references/patterns-reference.md](references/patterns-reference.md)** — this skill's
   own copy of every pattern write-up. Use this first; it's always available with the skill.
2. **`docs/wiki/patterns/1-command.md` … `23-template.md`** — the original, canonical
   write-ups in this repo. Linked from [docs/wiki/wiki.md](../../../docs/wiki/wiki.md). Edit
   *these* if the knowledge itself needs to change, then port the change into the reference
   copy above.
3. **`Assets/UnityTechnologies/LevelUpYourCode/_DesignPatterns/`** — runnable Unity scenes
   + C# for a subset of these patterns (from Unity's official "Level up your code" course).
   Prefer pointing here when the user wants working code to study or copy from, since it's
   already wired into scenes/prefabs in this project.

## Pattern index

| # | Pattern | One-liner | Reference code |
|---|---------|-----------|-----------------|
| 1 | [Command](references/patterns-reference.md#1-command) | Wrap an action as an object so callers don't care how it executes | `_DesignPatterns/4_Command/` |
| 2 | [Flyweight](references/patterns-reference.md#2-flyweight) | Share intrinsic (non-instance-specific) data across many objects | `_DesignPatterns/9_Flyweight/` |
| 3 | [Observer](references/patterns-reference.md#3-observer) | Decouple "event happened" from "what runs in response" | `_DesignPatterns/6_Observer/` |
| 4 | [Prototype](references/patterns-reference.md#4-prototype) | Duplicate an existing object instead of building one from scratch | Unity `Object.Instantiate` (built in) |
| 5 | [Singleton](references/patterns-reference.md#5-singleton) | Exactly one instance, globally reachable — use sparingly | `_DesignPatterns/3_Singleton/` |
| 6 | [State](references/patterns-reference.md#6-state) | Swap a state object instead of branching on an enum | `_DesignPatterns/5_State/`, `Assets/UnityTechnologies/LevelUpYourCode/Scripts/StateMachine/` |
| 7 | [Double Buffer](references/patterns-reference.md#7-double-buffer) | Read from a stable buffer while writing the next one, then swap | — (see write-up: cellular automata, cave gen) |
| 8 | [Game Loop](references/patterns-reference.md#8-game-loop) | Decouple simulation speed from CPU speed (fixed vs. variable step) | Unity `Time.fixedDeltaTime` / `Time.deltaTime` (built in) |
| 9 | [Update Method](references/patterns-reference.md#9-update-method) | Each object simulates its own frame via a per-object step call | Unity `MonoBehaviour.Update()` (built in) |
| 10 | [Bytecode](references/patterns-reference.md#10-bytecode) | Small custom instruction language interpreted at runtime (modding, dialogue) | — |
| 11 | [Subclass Sandbox](references/patterns-reference.md#11-subclass-sandbox) | Parent exposes primitive protected methods; children compose behavior from them | — |
| 12 | [Type Object](references/patterns-reference.md#12-type-object) | Vary "type"/behavior via a referenced object instead of subclassing | — |
| 13 | [Component](references/patterns-reference.md#13-component) | Independent, reusable behavior attached to an entity | Unity `GameObject` + `MonoBehaviour` (built in) |
| 14 | [Event Queue](references/patterns-reference.md#14-event-queue) | Queue events and drain a time-budgeted number per frame (a.k.a. Command Queue) | (see repo history — contributed by masoudarvishian) |
| 15 | [Service Locator](references/patterns-reference.md#15-service-locator) | Global, swappable access point for a service, behind a limited interface | Unity `GetComponent()` is one built-in instance of this idea |
| 16 | [Data Locality](references/patterns-reference.md#16-data-locality) | Lay out data contiguously so CPU cache hits go up — last-resort optimization | Unity DOTS |
| 17 | [Dirty Flag](references/patterns-reference.md#17-dirty-flag) | A bool defers a costly recompute until the result is actually needed | `_DesignPatterns/10_DirtyFlag/` |
| 18 | [Object Pool](references/patterns-reference.md#18-object-pool) | Recycle deactivated objects instead of Instantiate/Destroy churn | `_DesignPatterns/2_ObjectPool/`, Unity `UnityEngine.Pool.ObjectPool<T>` |
| 19 | [Spatial Partition](references/patterns-reference.md#19-spatial-partition) | Bucket objects by position (grid/quadtree/octree/BSP/k-d/BVH) for fast neighbor queries | — |
| 20 | [Decorator](references/patterns-reference.md#20-decorator) | Wrap an object to add behavior at runtime without touching its class | — |
| 21 | [Factory](references/patterns-reference.md#21-factory) | Centralize object creation in one place | `_DesignPatterns/1_Factory/` |
| 22 | [Facade](references/patterns-reference.md#22-facade) | One simple interface in front of a large/complex subsystem | — |
| 23 | [Template](references/patterns-reference.md#23-template) | Fixed algorithm skeleton in the parent; children override individual steps | — |

Not in the 23 but present as worked examples in `_DesignPatterns/`, and referenced from the
write-ups above as related architectural patterns: **MVP** (`7_MVP/`, `7_MVP_UIToolkit/`),
**MVVM** (`7_MVVM/`), **Strategy** (`8_Strategy/`) — Strategy is the sibling of State
mentioned in [State](references/patterns-reference.md#6-state) ("give an object a new
behavior without regard to its current state").

## "I need to…" → pattern lookup

Use this to go from a symptom straight to a candidate pattern (or two), then open that
pattern's entry in [references/patterns-reference.md](references/patterns-reference.md) for
the real guidance.

| Symptom / need | Pattern(s) |
|---|---|
| Rebind keys / input actions | Command |
| Replay or undo/redo a sequence of player actions | Command (Event Queue for replay timing) |
| Encapsulate an AI behavior or ability as a swappable unit | Command, Type Object |
| Serialize actions to send over the network | Command |
| Thousands of similar objects eating memory (trees, cubes, units) | Flyweight |
| Objects share a mesh/texture/material already | Flyweight (Unity `sharedMesh`/`sharedMaterial`) |
| Notify multiple unrelated systems when something happens (score, VFX, audio, achievements) | Observer |
| Avoid a hard reference just to raise/subscribe to an event | Observer (+ an event manager if fully decoupled) |
| Too many events firing at once risks a frame hitch | Event Queue (Observer alone will fire everything immediately) |
| Duplicate an existing configured object (a bullet, a unit) | Prototype (Unity `Instantiate`) |
| Need exactly one instance reachable from anywhere (save system, game controller) | Singleton — but see "Singleton caution" below |
| Character/menu/game has distinct modes with different behavior per mode | State |
| Nested if-statements switching on a mode enum | State |
| Give an object a new behavior independent of any current state | Strategy (`8_Strategy/`) — not State |
| Need last frame's data stable while computing this frame's (grid sim, render buffer) | Double Buffer |
| Movement/physics behaves differently on fast vs. slow machines | Game Loop (`Time.deltaTime` / `Time.fixedDeltaTime`) |
| Each object needs its own per-frame tick, run from one central loop | Update Method (Unity `Update()`) |
| Let non-programmers script cutscenes/dialogue/mod content | Bytecode |
| Child classes share behavior built from primitive parent operations | Subclass Sandbox |
| Can't use inheritance because behavior combos don't map to a class tree (e.g. flying mammal vs. flying bird vs. non-flying bird) | Type Object |
| Reusable, self-contained behavior you can drop onto any GameObject | Component (built in — write a normal `MonoBehaviour`) |
| A global utility needs to be reachable but swappable (audio backend, RNG, localization, pathfinding) | Service Locator |
| Everything else is optimized and it's still too slow, cache misses confirmed by profiling | Data Locality |
| Skip an expensive recompute (save, physics sleep, cost-function re-evaluation) until actually needed | Dirty Flag |
| Constantly instantiating/destroying objects hurts performance (bullets, particles) | Object Pool |
| Find nearest object / broad-phase collision / culling far-away objects / pathfinding search space | Spatial Partition |
| Attach optional modifiers/attachments/power-ups to an object at runtime (PUBG-style attachments) | Decorator |
| Centralize and standardize how objects get created (and optionally destroyed) | Factory |
| A subsystem (AI lib, geometry lib, audio, physics) has too many classes to use directly | Facade |
| Several classes/objects follow the same algorithm shape but differ in a few steps (assembling variants, AI decision steps, level generation) | Template |

## Singleton caution

The write-up itself warns against overusing this one — surface this if a user reaches for
Singleton by default:
- Prefer, in order: no manager class at all → a static class (Service Locator) → assign a
  scene reference in the Inspector → an event system (Observer) → dependency injection →
  collapsing multiple would-be Singletons into one ("One Singleton" holding sub-managers).
- If it must inherit `MonoBehaviour`, guard against duplicate instances on scene load and
  never call it from another object's `OnDestroy()` during quit — it may already be gone.
- See [Singleton](references/patterns-reference.md#5-singleton) for the full alternatives
  list.

## Related-pattern map

Condensed from each write-up's "Related patterns" section — use it to redirect when a user's
first pick is close but not quite right:

- **Command** ↔ Subclass Sandbox (many command subclasses → hoist shared logic to parent),
  Memento (alternative way to roll back state).
- **Flyweight** ↔ Type Object (Type Object allows differing data/behavior, not just shared
  identical data).
- **Observer** ↔ Event Queue (defers/batches instead of firing immediately), MVC (Observer is
  how you'd wire it up).
- **Prototype** ↔ Factory (Factory makes new objects; Prototype can live inside a Factory),
  Object Pool (avoid the Instantiate/Destroy cost Prototype implies).
- **Singleton** ↔ Service Locator (a static class is the non-global-object alternative).
- **State** ↔ Type Object (State swaps the referenced object; Type Object keeps it fixed),
  Strategy (new behavior regardless of state), Memento (rollback), Behavior Tree (more
  complex state graphs).
- **Type Object** ↔ State, Subclass Sandbox, Component (Component doesn't need to know about
  other components; Type Object exists only to extend a host object).
- **Component** ↔ Type Object (see above).
- **Event Queue** ↔ Event Bus (no delay), Observer (used to implement it).
- **Service Locator** ↔ Singleton (same global-access caveats apply), Facade (often combined).
- **Object Pool** ↔ Data Locality (pooled objects of the same type sit together in memory),
  Prototype.
- **Decorator** ↔ Subclass Sandbox (alternative to a deep child-class hierarchy).
- **Factory** ↔ Prototype (copy vs. generate), Object Pool (a Factory can recycle instead of
  `new`).
- **Facade** ↔ Service Locator, Singleton (the facade itself is often one), Adapter (Adapter
  wraps an interface you can't change; Facade simplifies one you can).
- **Template** ↔ Subclass Sandbox (children override steps vs. compose primitives).

## Workflow: "which pattern should I use for X?"

1. Scan the "I need to…" table above for the closest symptom match.
2. Open that pattern's entry in
   [references/patterns-reference.md](references/patterns-reference.md) (or the matching
   `docs/wiki/patterns/<N>-<name>.md` if you need the original wording) and read its "When
   useful" bullets — confirm the user's scenario is actually listed or clearly analogous.
3. Check the related-pattern map for a better-fitting neighbor before committing.
4. If a Unity built-in already implements it (Prototype → `Instantiate`, Game Loop →
   `Time.deltaTime`, Update Method → `Update()`, Component → `MonoBehaviour`, Object Pool →
   `UnityEngine.Pool.ObjectPool<T>`), say so — don't hand-roll it unless there's a stated
   reason to.

## Workflow: "implement/refactor this with pattern X"

1. Read that pattern's "How to implement" section in
   [references/patterns-reference.md](references/patterns-reference.md) first — it's tuned to
   this book's terminology and to Unity specifics (e.g. don't reinvent Object Pool's
   linked-list optimization note, or Singleton's `MonoBehaviour` gotchas). Cross-check
   `docs/wiki/patterns/<N>-<name>.md` if something seems to have drifted from the reference
   copy.
2. If `_DesignPatterns/<N>_<Name>/` exists (see the index table), read the `Scripts/Pattern/`
   folder there first for the reusable pattern code, and `Scripts/ExampleUsage/` for how it's
   wired into a scene — reuse or adapt these rather than writing a fresh implementation from
   scratch.
3. Apply this repo's C# conventions from [unity-csharp-conventions](../unity-csharp-conventions/SKILL.md)
   (naming, file layout, `Awake`/`OnEnable`/`Start` responsibilities) while implementing.
4. After editing any `.cs` file, follow the compile-verification step in
   unity-csharp-conventions (`recompile_scripts` + check console) before touching scenes.
5. If the pattern has related alternatives (see the map above), mention the trade-off briefly
   instead of silently picking one.
