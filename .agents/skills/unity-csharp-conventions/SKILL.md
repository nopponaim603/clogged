---
name: unity-csharp-conventions
description: >
  C# scripting conventions for this repo's Unity project (Unity-Projects/, Unity 6000.5.4f1).
  Use whenever creating or editing a .cs file under Unity-Projects/Assets, deciding where a
  new script or MonoBehaviour should live, naming fields/methods/classes in Unity C#, or
  deciding whether an assembly definition (.asmdef) is warranted. Also use when a script edit
  needs to be verified against Unity's compiler (via the MCP recompile_scripts tool — see
  unity-mcp-tools) before continuing scene work.
---

# Unity C# Conventions

Unity-Projects/ is a **quick spike/prototype** to validate the MCP Unity ↔ Editor bridge
(see [mcp-unity/README.md](file:///c:/Users/noppon/source/01-DG/clogged/mcp-unity/README.md)),
not the shipping game — the real game is `prototype_resource_game/` (Phaser+TS, see
[AGENT.md](file:///c:/Users/noppon/source/01-DG/clogged/AGENT.md)). Per the project owner: this
Unity work will **not** be developed further once the workflow is proven out. Conventions below
are deliberately lightweight — enough to keep scripts readable and compiling cleanly, not an
enterprise style guide. Don't introduce infrastructure (asmdef splits, DI frameworks, custom
editor tooling) the prototype doesn't need yet.

## Current state

- Unity 6000.5.4f1, Built-in render pipeline (no URP/HDRP package in
  `Unity-Projects/Packages/manifest.json`).
- No `.cs` scripts exist yet under `Assets/`. Only `Materials/BlueMat.mat`,
  `Prefabs/Box.prefab`, `Scenes/Sample Scene.unity`.
- No assembly definitions exist. Don't add one unless a real need shows up (see below).

## File & folder layout

- Put scripts under `Assets/Scripts/`, mirroring by feature if it grows past a handful of
  files (`Assets/Scripts/Player/`, `Assets/Scripts/Enemies/`, ...). Don't pre-create empty
  subfolders speculatively — add structure when the second script in a category shows up.
- **File name must exactly match the class name** (`PlayerController.cs` →
  `class PlayerController`). Unity requires this to drag a MonoBehaviour onto a GameObject.
- One public type per file.

## Naming

- **Classes, methods, properties, public fields, enums, enum members, constants:** PascalCase
  (`MoveSpeed`, `TakeDamage()`, `PlayerState`).
- **Private/protected fields, local variables, parameters:** camelCase, no leading underscore
  (`moveSpeed`, `targetPosition`). Pick this and stay consistent — don't mix `_camelCase` and
  `camelCase` across scripts.
- **Inspector-exposed private fields:** `[SerializeField] private Type fieldName;` — prefer this
  over making the field `public` just to expose it in the Inspector.
- Namespaces are optional at this scale (single small `Scripts/` folder); don't add a
  `Clogged.Unity.*` namespace hierarchy for a handful of files.

## MonoBehaviour patterns

- **`Awake()`**: cache own-object references (`GetComponent<T>()`, self setup). Runs before any
  `Start()`, regardless of script order.
- **`OnEnable()` / `OnDisable()`**: subscribe/unsubscribe from events — always pair them so a
  disabled object doesn't leak a subscription.
- **`Start()`**: cross-object wiring (things that depend on other objects' `Awake()` having run).
- **`Update()` / `FixedUpdate()`**: keep cheap. Never call `GetComponent`, `Find`,
  `FindObjectOfType`, or `Instantiate`/`Destroy` every frame — cache the reference once
  (typically in `Awake`) and reuse it.
- Prefer `[SerializeField]` fields over hardcoded magic numbers so values are tunable from the
  Inspector without a recompile.
- Use `Undo.RecordObject()`-friendly patterns only if writing Editor-time tooling; runtime
  gameplay scripts don't need this.

## Assembly definitions (.asmdef)

Not needed right now — a handful of scripts in one folder compiles fine as part of the default
assembly. Only add an `.asmdef` if one of these becomes true:

- Unity Test Framework tests are added (EditMode/PlayMode tests need their own test assembly
  referencing the code under test — see `com.unity.test-framework` in the package lock).
- Compile times become noticeably slow because of unrelated script churn elsewhere in `Assets/`.

If you do add one, keep it to a single `Scripts.asmdef` at `Assets/Scripts/` — don't split
further for a prototype this size.

## After every script edit

A `.cs` file edit (whether made directly or via an MCP Unity tool) is not verified until Unity's
compiler has run. Before doing any further scene/prefab work that depends on the script:

1. Trigger `recompile_scripts` (see [unity-mcp-tools](../unity-mcp-tools/SKILL.md)).
2. Check `get_console_logs` for compiler errors.
3. Fix and repeat before touching the scene — see
   [unity-scene-workflow](../unity-scene-workflow/SKILL.md) for why this ordering matters for
   `save_scene`.
