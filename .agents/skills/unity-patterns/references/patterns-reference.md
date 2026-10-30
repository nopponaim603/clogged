# Game Programming Patterns — Full Reference

Self-contained copy of all 23 pattern write-ups from `docs/wiki/patterns/` (condensed for
readability, no information dropped). This file exists so the `unity-patterns` skill works
even without access to the wider repo's `docs/` tree. **`docs/wiki/patterns/*.md` remains the
canonical, editable source** — if this copy and the wiki ever disagree, trust the wiki and
refresh this file from it.

Each entry: the problem it solves, how to implement it, when it's useful (concrete game
examples), and related patterns with the key distinction from each.

---

## 1. Command

**Problem:** You have many commands (play sound, throw cake, ...). Wrapping each in a command
object means the caller doesn't need to know how the command is actually carried out.

**How to implement:** A base `Command` class with an `Execute()` method a child overrides;
each child implements what actually happens when it executes.

**When useful:**
- Rebinding keys.
- Replay systems: store which command ran on each update in a data structure, then re-run
  them in order to replay.
- Undo/redo: like replay, but each command also has an `Undo()` that reverses its effect.
- Encapsulating AI behaviors/actions so they're easier to control and swap.
- Defining the sequence of actions in a cutscene or scripted event.
- Applying abilities, power-ups, or temporary effects.
- Event handling — wrap an event-specific action as a command, run it when the event fires.
- Multiplayer: commands can be serialized and sent over the network to sync actions.

**Related patterns:**
- **Subclass Sandbox** — many command subclasses pile up; hoist shared high-level operations
  into the parent so children stay small.
- **Memento** — an alternative way to return to a previous state.

---

## 2. Flyweight

**Problem:** A single object is light, but *many* of them add up. Make instances lighter by
sharing the data that doesn't vary per-instance.

**How to implement:** Split out the data that's not instance-specific and can be shared
across all instances into its own class; each object holds a reference to one shared
instance of that "storage" class instead of owning a copy.

**When useful:**
- A million cubes on screen (Minecraft-style) — share one texture atlas across all cubes of
  the same type.
- A strategy game where all infantry units share mesh/texture/animation/max-health; each unit
  only tracks its own position and health.
- Already built into Unity: `MeshFilter.sharedMesh`, `Renderer.sharedMaterial` — changing a
  shared mesh/material changes it for every object using it.
- Open-world trees/rocks reused (rotated/scaled) so no one notices the repetition.
- One crash sound reused across a car game, with pitch/settings varied per crash instead of
  loading a new clip each time.

**Related patterns:**
- **Type Object** — the difference is Type Object doesn't require identical data and can
  carry behavior too.

---

## 3. Observer

**Problem:** Constant events in your game — which methods should run after an enemy dies
(update score, play death animation, ...)? Those methods subscribe to the event.

**How to implement:** C# and Unity already implement this: `EventHandler`, `Action`,
`UnityEvent`, or your own delegate-based version.

**When useful:**
- Decoupling classes from each other (avoiding spaghetti code). The thing raising the event
  doesn't care what's subscribed — there might be nothing, which also makes bugs easier to
  isolate.
- To fully decouple you still need a reference to the class that defines the event; an Event
  Manager (global class owning all events) removes even that.
- Static events are another decoupling option.

**Related patterns:**
- **Event Queue** — Observer fires every subscriber immediately; if 10 enemies die at once
  and 5 methods are subscribed, that's 50 calls in one frame, which can hitch. Event Queue
  spreads them out.
- **MVC** — Observer is typically how you'd wire up an MVC implementation.

---

## 4. Prototype

**Problem:** You have a configured object and want duplicates of it.

**How to implement:** Unity already implements this via
[`Object.Instantiate`](https://docs.unity3d.com/ScriptReference/Object.Instantiate.html)
(assumes the source inherits `UnityEngine.Object`). Rolling your own means deciding between a
deep clone (copies structure *and* elements) or a shallow clone (copies structure only) —
Flyweight may inform that choice.

**When useful:**
- A gun fires bullets: keep one bullet prefab, `Instantiate` a fresh copy each shot so the
  original prefab is never consumed.

**Related patterns:**
- **Factory** — Factory generally generates *new* objects, not copies of existing ones
  (with their state/position); Prototype can live inside a Factory to centralize creation.
- **Object Pool** — repeatedly Instantiate+Destroy hurts performance; pool instead.

---

## 5. Singleton

**Problem:** A save-game class must have exactly one instance (multiple instances risk
saving divergent data) and be easy to reach from anywhere.

**How to implement (plain C#):** Make the instance static, expose it through a public static
accessor that lazily creates it, and make the constructor private with no parameters.

**How to implement (MonoBehaviour):** Same idea, but you can't use a private constructor, so
implement your own setup, and guard against more than one instance existing at once (destroy
duplicates created by scene loads).

**When NOT useful / caution:**
- *Game Programming Patterns* (the book) recommends avoiding this pattern in general — global
  objects cause trouble. If used at all, reserve it for manager classes (`GameController`,
  `SaveGame`). Fewer Singletons is better.
- MonoBehaviour Singletons: calling one from another object's `OnDestroy()` during quit can
  fail because the Singleton may already be destroyed.

**Alternatives, in rough preference order:**
- **No class at all** — most Singletons are just helpers; fold the helper code into the class
  that actually needs it.
- **Static class** — this is essentially the Service Locator pattern.
- **Unity's `Find()` / `SendMessage()`** — avoid, they're slow; if unavoidable, call once in
  `Start()` and cache the result.
- **Assign references in the Inspector** — drag object references onto public fields. Gets
  unwieldy if a reference changes and needs re-dragging in many places.
- **A global event system** — this is the Observer pattern; still needs one Singleton for the
  event system, but removes all the others.
- **Dependency Injection** — inject the reference (e.g. via constructor) into whatever needs
  it; DI frameworks can automate this.
- **One Singleton** — collapse every would-be manager into sub-managers of a single Singleton
  (e.g. `GameController.Instance.getSaveGameManager()`).

---

## 6. State

**Problem:** Your game/character can be in a number of states (jump, walk, run, ...) and you
need a clean way to switch between them. Also called a **state machine**; with a finite
number of states, a **finite state machine (FSM)**.

**How to implement:** An enum + switch statement works but grows unwieldy as states are
added. Better: define one object per state and swap which object is "current" as the state
changes.

**When useful:**
- Deeply nested if-statements, e.g. a menu system.
- Unity's own animation engine uses this pattern.
- Turn-based combat systems.
- GTA-style games: one state per major mode (driving, on-foot, flying, ...), with sub-states
  within a state (on-foot → holding nothing / grenade / pistol).
- Enemy AI — e.g. Minecraft creepers: wander when far, approach when closer, explode when
  very close.
- The game itself as states: intro video, main menu, main game, mini-game, ...
- Interactive objects: a door with open/closed/locked/unlocked states and behavior that
  depends on which one it's in.

**Related patterns:**
- **Type Object** — both add a second object to define something on a main object; State
  *swaps* that object, Type Object keeps it fixed. (If the Type Object were swappable, you'd
  have State.)
- **Strategy** — gives an object a new behavior independent of its current state or what
  comes after it.
- **Memento** — like State, but with the ability to roll back to a previous state.
- **Behavior Tree** — better fit once you have many states and want more complex behavior.

---

## 7. Double Buffer

**Problem:** You need to write new data to a buffer while something else is still reading
old data from it, without the two interfering.

**How to implement:** Keep two buffers (e.g. two arrays). Write to one; when the update is
done, swap which one is "current" and which is "previous."

**When useful:**
- Screen rendering: your computer already does this — read buffer #2 while #1 updates, then
  swap. Using only one buffer would show a half-old/half-new mix on screen.
- Motion blur: blend the current buffer with a bit of the previous one.
- Cellular automata on a grid (2D array): computing a cell's new value needs neighboring
  cells' *old* values, so you can't overwrite in place — use two grids, read from #2 while
  writing #1, then swap.
  - Cave generation (via cellular automata).
  - [Water simulation](http://www.jgallant.com/2d-liquid-simulator-with-cellular-automaton-in-unity/) on a grid.
  - [Forest fire simulation](https://www.youtube.com/watch?v=JtGp9eUugFs) — burning material per
    cell, heat ignites neighbors, fire dies out when material runs out.

---

## 8. Game Loop

**Problem:** The game loop is an infinite update loop, but a naive one runs faster on faster
hardware — an object moving at a fixed speed would visibly move faster on a fast machine
unless time is accounted for.

**Two strategies:**
- **Fixed time step** — target a fixed FPS (e.g. 30 → 0.0333s/step); if a loop iteration
  finishes early, pause until the time budget elapses; if it's consistently slower, that's a
  sign to optimize.
- **Variable (fluid) time step** — measure elapsed time since the last frame and pass it into
  the update, so the simulation takes bigger steps on slow hardware and smaller steps on fast
  hardware.

**How to implement:** Unity already implements both:
- Fixed step: `Time.fixedDeltaTime`, used for physics so calculations stay accurate/consistent.
- Variable step: [`Time.deltaTime`](https://docs.unity3d.com/ScriptReference/Time-deltaTime.html)
  — "the completion time in seconds since the last frame."
The game loop also polls input before anything else runs, which is why
`Input.GetKey(KeyCode.A)` works inside `Update()` — the loop already checked and cached that
state before calling `Update()`.

**When useful:**
- A bullet moving at a constant speed: multiply `bulletSpeed` by `Time.deltaTime` each update
  so travel speed is identical regardless of frame rate.

---

## 9. Update Method

**Problem:** Each object needs to process one frame of its own behavior; cramming every
object's logic into the game loop's single update method doesn't scale.

**How to implement:** Unity already implements this via `MonoBehaviour.Update()` — Unity
calls each object's `Update()` in turn from its own main loop.

**Alternative:** Roll your own update list — store every object needing custom updates in a
list, then have one script (e.g. a `GameController`) iterate that list from inside Unity's
`Update()`. Useful for things like pausing: skip iterating the list while paused instead of
disabling each object individually.

---

## 10. Bytecode

**Problem:** Non-programmer collaborators want to contribute behavior, but can't write C#.

**How to implement:** Define a small custom scripting language. Non-programmers write it in
a `.txt` file; you parse the file line by line and translate each instruction to C# via a
switch statement (essentially a mini interpreter).

**When useful:**
- Adding modding support.
- Avoiding hard-coded behavior.
- Cutscenes and dialogue systems — express scripted sequences as bytecode for easier
  authoring and management.
- Achievement conditions/progress tracking — express the unlock conditions/actions as
  bytecode.

---

## 11. Subclass Sandbox

**Problem:** Similar objects need different behavior, built out of shared building blocks.

**How to implement:** Define several `protected` methods (with their implementation) in the
parent class. Each child class calls whichever subset of those methods it needs to produce
its own behavior.

**When useful:**
- Child classes sharing behavior that the parent can provide wholesale — e.g. superpowers
  that combine shared primitive actions (the book's own example).

**Related patterns:**
- **Update Method** — the update method is often itself implemented as a sandbox method.
- **Type Object** — instead of defining every method on the parent, give the child a
  reference to an object that defines them.
- **Template** — here the child *overrides* the parent's methods rather than composing calls
  to them.

---

## 12. Type Object

**Problem:** You want to change an object's type (behavior/data) via a referenced object,
rather than via subclassing — useful when the same "type" doesn't map cleanly onto a single
class hierarchy.

**How to implement:** All Type Objects should share an interface (or common parent) so the
main class can reference any of them uniformly.

**When useful:**
- Class inheritance breaks down: a base `Animal` → `Bird`/`Fish`/`Mammal`, with flying
  behavior on `Bird` — until an ostrich (a bird that can't fly) or a bat (a mammal that
  *can* fly) shows up. Duplicating flying behavior across two branches is a smell; instead,
  define flying/non-flying as a separate Type Object referenced by whichever animal needs it.
- Event systems: represent event types as objects, register/handle them dynamically at
  runtime.
- Game configuration/settings represented as swappable objects.
- Any case needing runtime type switching.

**Related patterns:**
- **State** — same "main object + referenced object" shape; State *swaps* the referenced
  object, Type Object keeps it fixed. (A switchable type *is* State.)
- **Subclass Sandbox** — alternative: define all types on the parent and combine them in the
  child.
- **Component** — Component doesn't have to be coupled to anything else on the host object
  (it can live independently, e.g. colliders/renderers/scripts on a Unity GameObject don't
  need to know about each other); Type Object exists specifically to add behavior to an
  existing class, so it can't stand alone the way a Component can.

---

## 13. Component

**Problem:** As a game grows, you want behavior units that are independent and reusable —
like a USB mouse that plugs into any computer. Some components still need to talk to each
other, so "independent" isn't absolute (a mouse talks to the computer, not the printer).

**How to implement:** Already built into Unity — GameObjects take colliders, mesh renderers,
and your own scripts as attachable components. The job is making your own scripts as
reusable as possible.

**When useful:**
- A custom FPS counter, reusable across projects, independent of everything else, just
  attach it to a GameObject.
- Car physics (drag, rolling resistance) in one script — the calculations are identical
  across cars, even though the component still needs some data (e.g. current speed) from the
  car it's attached to.

**Related patterns:**
- **Type Object** — Component can live independently on a GameObject; Type Object exists only
  to add behavior to something else, so it can't stand alone.

---

## 14. Event Queue

**Problem:** Some events don't need to be handled immediately — deferring/spreading them out
avoids freezing the game when many fire at once. Sometimes called a **Command Queue**.

**How to implement:** Combine the Command pattern with a queue (e.g. C#'s built-in `Queue`).
In `Update()`, dequeue and run commands while measuring elapsed time (e.g.
`System.Diagnostics.Stopwatch`); keep running commands while there's time budget left, then
stop. How much time to allow per update is game-specific — measure and tune.

**When useful:**
- An event triggers an asset load (e.g. play a sound on click) — loading can stall the frame;
  deferring the sound by a few frames avoids the stall.
- 100 enemies dying at once each trying to play a death sound: queue the events, and either
  skip a sound if one's already playing, or merge duplicate event types so only one instance
  plays.
- Strategy game order queues: queue a unit's orders (build wall → collect food → attack) so
  the player doesn't block on one task finishing; also useful for waypoint patrols, and for
  AI to queue up unit commands.
- Dialogue/speech systems: one queue per character; clear all queues if the player presses
  Escape to skip.

**Related patterns:**
- **Event Bus** — like Event Queue but without the delay.
- **Observer** — used to implement the Event Queue itself.

---

## 15. Service Locator

**Problem:** Standardized helper functionality (e.g. random number generation) should be
globally accessible yet stay independent of your game's main code — and easily replaceable
without touching every caller.

**How to implement:** Put each service in its own static class, in its own folder/namespace
so it doesn't blend into main game code. A Service Locator exposes access to a service
provider; the provider should expose only the methods callers actually need, not everything.
Unity's own `GetComponent()` is an existing instance of this idea.

**When useful:**
- Unity already ships several: `Random.Range()`, `Mathf.PI`, `Debug.Log()`.
- Swapping audio implementations depending on platform (console vs. PC) — a book example.
- Dependency injection: request/retrieve required services at runtime instead of hardcoding
  references.
- Abstracting the underlying input device behind an input service.
- Localization: retrieve the right localized-text service without hardcoding which one.
- AI-related services (pathfinding, decision-making) accessed uniformly.

**Related patterns:**
- **Singleton** — both give global access, so the same caveats about global state apply here.
- **Facade** — often combined with Service Locator.

---

## 16. Data Locality

**Problem:** You've exhausted other optimizations and the game is still too slow — this
pattern speeds things up by improving memory access patterns.

**How to implement:** Arrange data structures so the things you're actively processing sit
next to each other in memory, to take advantage of CPU caching. (This is a large topic — see
*Game Programming Patterns* for the full treatment.) Prefer `struct` over `class` where
possible — [structs are more cache-friendly](https://jacksondunstan.com/articles/3860). Unity
implements this at scale via [DOTS](https://unity.com/dots). Tutorials: [Unity Memory Profiler
Part 1](https://thegamedev.guru/unity-memory/profiler-part-1/) and
[Part 2](https://thegamedev.guru/unity-memory/profiler-part-2/).

**When useful:**
- Only as a last resort, per the book — optimizing code that doesn't need it wastes time and
  adds complexity. Confirm cache misses are actually the bottleneck via profiling before
  reaching for this.

---

## 17. Dirty Flag

**Problem:** Something changed, which should trigger a costly operation — but you don't want
to run that operation until the result is actually needed.

**How to implement:** The dirty flag is just a `bool`.

**When useful:**
- Save games: set a dirty flag when game state changes; use it to warn the player about
  unsaved changes on quit.
- Unity editor scripting: `SetDirty()` marks an object (or a whole scene) as needing a save.
- Unity's physics: a sleeping (non-moving) `Rigidbody` is skipped by the physics system via a
  dirty-flag-like mechanism; it only wakes when a force is applied.
- Optimizing a Genetic Algorithm solving the Traveling Salesman Problem: instead of
  recalculating every solution's cost function each generation, track which solutions were
  actually touched by tournament selection with a per-iteration bool, and only recompute
  those.
- Multiplayer network sync: mark changed state as dirty and only transmit dirty data, cutting
  bandwidth.
- AI/environment state: prioritize updates and decisions only for objects/agents flagged
  dirty.

---

## 18. Object Pool

**Problem:** Constantly creating and destroying objects hurts performance. Instead, create a
fixed batch up front, deactivate them, and hand out/reclaim from that pool as needed.

**How to implement:** An `ObjectPool` class holds a prefab and instantiates however many
you expect to need, storing them in a list. To get an object, scan the list for a deactivated
one and return it. If you run out:
- Instantiate more on demand (watch for wasting memory; you can prune the extras later), or
- Repurpose an active object the player won't notice disappearing, or
- Just do nothing — e.g. one missing explosion in a screen full of them won't be noticed.

Linear-scanning a long pool list wastes time — a linked list is a faster alternative for
finding an available object. Unity ships its own version for newer versions:
[`ObjectPool<T>`](https://docs.unity3d.com/ScriptReference/Pool.ObjectPool_1.html).

**When useful:**
- Guns firing many bullets — includes a linked-list optimized version, a simple/slow
  list-scanning version, and Unity's native pool as three implementation options.
- Unity's particle system: capping max particles is effectively this pattern, preventing
  runaway instantiation.

**Related patterns:**
- **Data Locality** — packing same-type objects together in memory (what a pool naturally
  does) helps keep the CPU cache full while iterating them.
- **Prototype** — pooled objects are typically prototypes cloned once up front.

---

## 19. Spatial Partition

**Problem:** With many objects in the world, organize them by position so spatial queries
(e.g. "what's colliding with what") don't require checking every pair.

**Options:**
- **Grid** — divide the world into cells, track which cell each object is in. (Spatial
  hashing removes the fixed-size-grid limitation — see
  [this project](https://github.com/Habrador/Ten-Minute-Physics-Unity).)
- **Trie-based** (not "Tree"):
  - [Quadtree](https://en.wikipedia.org/wiki/Quadtree) (2D) — split a cell into 4 once it has
    too many objects, recursively. [Tutorial](https://www.youtube.com/watch?v=OJxEcs0w_kE).
  - [Octree](https://en.wikipedia.org/wiki/Octree) (3D) — same idea, 8-way split of cubes.
- **Binary search trees** — split into 2 groups repeatedly rather than 4/8:
  - [BSP](https://en.wikipedia.org/wiki/Binary_space_partitioning) — split with a plane,
    recursively.
  - [k-d trees](https://en.wikipedia.org/wiki/K-d_tree) — same idea, specifically for points.
  - [Bounding volume hierarchy](https://en.wikipedia.org/wiki/Bounding_volume_hierarchy) — pick
    a bounding volume that fits everything, then recursively split it in two.

Moving objects require the structure to be updated, and the structure itself costs memory —
measure that it's actually faster than a naive search before committing to one.

**When useful:**
- Finding the closest object to a character — naive search across hundreds of objects (times
  every soldier searching, in an army-vs-army fight) is slow; partition first.
- Speeding up collision detection and raytracing.
- Culling — deactivate/skip updates for objects (trees, distant AI) far from the player.
- Pathfinding — restrict the search to relevant partitions of a navigation grid instead of
  the whole world.

---

## 20. Decorator

**Problem:** Add behavior to an object flexibly and at runtime, without modifying its class
or affecting other instances of the same class.

**How to implement:** Create wrapper ("decorator") classes around the class you want to
extend; each decorator can itself be wrapped by another to stack behaviors. Often simpler
than a deep subclass hierarchy for the same combinations.

**When useful:**
- An order system where products are ordered together but paid for later — chain decorator
  objects instead of a list, so asking the "last" object for the price walks the whole chain
  (the book's Tesla-car-with-modifications example).
- PUBG-style weapon attachments (magazines, sights, silencers) that modify weapon properties.
- Temporary power-ups (speed boost, damage boost, invincibility) implemented as decorators
  wrapping the original object.
- Special effects/animations layered onto UI buttons, panels, icons.
- Dynamic difficulty adjustment — add/remove decorators based on player performance.

**Related patterns:**
- **Subclass Sandbox** — an alternative when the equivalent child-class hierarchy would get
  too large; hoist shared high-level operations to the parent instead.

---

## 21. Factory

**Problem:** Centralize how new objects get created (and optionally destroyed) in one place.

**How to implement:** If you have multiple factories, have them inherit from a common
abstract parent, and have the products they create also share a common abstract parent, so
callers can treat any product uniformly regardless of which concrete factory made it.

**When useful:**
- Combined with **Decorator** to manufacture already-decorated objects procedurally (the
  book's Tesla-car example, built on top of the Decorator example).
- Playing sounds across different output devices.
- Centralizing allocation makes it easier to monitor/measure memory use.
- Tracking all your Singletons in one place.
- Spawning any kind of game object: characters, enemies, items, obstacles, terrain features,
  structures, power-ups, collectibles, buttons, particles, etc.

**Related patterns:**
- **Prototype** — Prototype copies an existing object; Factory generates new ones (though
  some put Prototype *inside* a Factory).
- **Object Pool** — a Factory doesn't have to always construct new objects; combined with
  Object Pool it becomes a recycling plant instead.

---

## 22. Facade

**Problem:** Several related classes (an AI subsystem, an audio subsystem) are individually
complex; you want a simpler entry point without losing the ability to reach the underlying
classes when needed. Named after building facades — you see the exterior, not the internal
structure.

**How to implement:** Create a manager class exposing a single, simplified interface to a
larger collection of related classes.

**When useful:**
- Large standalone code libraries (e.g. an AI/pathfinding library) accumulate many classes; a
  facade exposes just the common operations (e.g. "get a short path"). Example: the author's
  [Computational geometry](https://github.com/Habrador/Computational-geometry) library wraps
  multiple Delaunay-triangulation implementations behind one `_Delaunay` facade class, so
  swapping the underlying algorithm only requires changing the facade. Multiple facades are
  fine — a separate one exists there for intersection algorithms.
- Choosing between `UnityEngine.Random.Range` and `System.Random.Next` — a facade lets you
  swap between them (or add a third RNG library) by changing one place only.
- Simplifying audio (play/pause/stop/manage sources), input (keyboard/mouse/controller behind
  one interface), save/load (hiding serialization details), and physics (forces, collisions)
  subsystems.

**Related patterns:**
- **Service Locator** — a service located this way doesn't have to be many classes, but
  Service Locator can itself use Facade internally.
- **Singleton** — the facade class is often a Singleton, since you typically want a single
  point of access to something like audio or AI.
- **Adapter** — Adapter makes an interface you *can't* modify work with your system; Facade
  simplifies an interface you *can* modify. Similar enough that some books cover both in the
  same chapter.

---

## 23. Template

**Problem:** Several objects follow the same overall algorithm, but implement individual
steps differently.

**How to implement:** Define a template method on the parent class that calls a sequence of
other methods; child classes override the steps that need to differ.

**When useful:**
- Child classes sharing an assembly process where the steps are the same but the parts
  differ (the book's Tesla-car-assembly example).
- Character behavior: a shared skeleton (move, attack, interact with environment) with
  subclasses (warrior, mage, rogue) overriding specific steps for their unique abilities.
- Procedural level generation: a shared overall layout/structure, with subclasses
  customizing terrain, obstacles, and enemy placement.
- AI decision-making: a shared skeleton (evaluate threats → consider objectives → choose
  action), with different agent/enemy types overriding individual decision steps.
- Game states (menu, gameplay, cutscene): a shared skeleton for transitions/common behavior,
  with per-state subclasses overriding state-specific logic.

**Related patterns:**
- **Subclass Sandbox** — here, methods defined on the parent are *composed* by the child
  rather than *overridden*.
