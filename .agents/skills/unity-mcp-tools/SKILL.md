---
name: unity-mcp-tools
description: >
  Catalog of every tool and resource exposed by the mcp-unity MCP server (CoderGamester/mcp-unity,
  set up via mcp-unity/setup.ps1) that bridges this session to the Unity Editor for
  Unity-Projects/. Use whenever a task needs to inspect or modify the Unity Editor state —
  scenes, GameObjects, components, prefabs, materials, packages, tests, play mode, or the
  console — so the right tool/resource is picked on the first try instead of guessed. Also
  covers what to check when a tool call fails (connection/port issues).
---

# MCP Unity Tool Guide

The bridge is two-tier: this session talks to a local Node MCP server (stdio), which talks over
WebSocket (`ws://localhost:8090/McpUnity` by default) to a listener running inside the Unity
Editor. **The Unity Editor must be open with the project loaded and the MCP Unity server window
active (Tools > MCP Unity > Server Window) for any of these to work.** If every call fails,
check that first before assuming a tool-usage mistake.

Tool/resource names are `lower_snake_case` and must be called exactly as listed — there is no
fuzzy matching.

## GameObject tools

| Tool | Use for |
|---|---|
| `select_gameobject` | Select by hierarchy path or instance ID |
| `get_gameobject` | Full detail incl. all components, for one GameObject |
| `update_gameobject` | Set name/tag/layer/active/static; **creates** the GameObject if it doesn't exist |
| `update_component` | Set fields on a component; **adds** the component if missing |
| `duplicate_gameobject` | Clone, with optional rename/reparent |
| `delete_gameobject` | Remove from scene |
| `reparent_gameobject` | Change hierarchy parent |
| `move_gameobject` / `rotate_gameobject` / `scale_gameobject` | Single-axis-of-transform edits |
| `set_transform` | Position + rotation + scale in one call — prefer this over three separate transform calls when setting all of them |

## Scene tools

| Tool | Use for |
|---|---|
| `create_scene` | New `.unity` file at a given path |
| `load_scene` / `unload_scene` | Open/close a scene, optional additive |
| `save_scene` | Save active scene, optional Save-As path — see [unity-scene-workflow](../unity-scene-workflow/SKILL.md) for when it's safe to call |
| `delete_scene` | Deletes the asset **and** removes it from Build Settings (prefer this over manually deleting the `.unity` file) |
| `get_scene_info` | Active scene name/path/dirty-state + all loaded scenes |
| `get_scenes_hierarchy` *(resource: `unity://scenes-hierarchy`)* | Full GameObject tree of the current scene |

## Asset / Prefab / Material tools

| Tool | Use for |
|---|---|
| `add_asset_to_scene` | Instantiate an existing project asset (e.g. a prefab) into the scene |
| `create_prefab` | New prefab, optionally with a MonoBehaviour + serialized field values pre-set |
| `create_material` | New `.mat` with a given shader |
| `assign_material` | Set a material on a GameObject's Renderer |
| `modify_material` | Change existing material properties (color, float, texture) |
| `get_material_info` | Inspect a material's shader + all current property values |

## Script / package / test tools

| Tool | Use for |
|---|---|
| `recompile_scripts` | Force a compile pass — always call after any `.cs` edit before touching the scene, see [unity-csharp-conventions](../unity-csharp-conventions/SKILL.md) |
| `add_package` | Install a Package Manager package |
| `run_tests` | Run Unity Test Runner (EditMode/PlayMode) |
| `get_console_logs` | Paginated console output — use to confirm zero compiler errors, or to debug a runtime issue |
| `send_console_log` | Write a log line into the Unity console (rarely needed) |

## Play mode tools

| Tool | Use for |
|---|---|
| `get_play_mode_status` | Check `isPlaying` / `isPaused` before issuing a play-mode-only action |
| `set_play_mode_status` | `play` (start/unpause) · `pause` (toggle) · `stop` · `step` (advance one frame) |

## Misc

| Tool | Use for |
|---|---|
| `execute_menu_item` | Invoke any Unity menu command by its menu path (e.g. `GameObject/Create Empty`) — use this as a fallback when no dedicated tool covers an action |
| `batch_execute` | Run several of the above tool calls as one batch, with optional rollback on failure — prefer this for any multi-step change (e.g. "create 5 GameObjects and configure each") over one call per step |
| `show_unity_dashboard` | Opens the Unity dashboard MCP App UI in VS Code (needs VS Code ≥1.109) |

## Resources (read-only, `unity://...`)

Use these to look something up without mutating state:

- `unity://menu-items` — valid menu paths for `execute_menu_item`
- `unity://scenes-hierarchy` — same data as `get_scenes_hierarchy`
- `unity://gameobject/{id}` — same data as `get_gameobject`, addressable by ID/path
- `unity://logs` — same data as `get_console_logs`
- `unity://packages` — installed + available Package Manager packages
- `unity://assets` — Asset Database contents (e.g. "find all textures")
- `unity://tests/{testMode}` — available tests for the Test Runner

## When no tool fits

Fall back to `execute_menu_item` for anything reachable via a Unity menu command, or to a direct
file edit (e.g. writing a `.cs` file) followed by `recompile_scripts`. Don't hand-edit `.unity`/
`.prefab` YAML directly — always go through the scene/GameObject tools so `.meta` GUIDs and
scene serialization stay consistent (see [unity-scene-workflow](../unity-scene-workflow/SKILL.md)).

## If every call fails

1. Confirm Unity Editor is open with `Unity-Projects` loaded.
2. Confirm **Tools > MCP Unity > Server Window** shows the server running (default port 8090).
3. Confirm `Unity-Projects/opencode.json` / the active client config still points at
   `mcp-unity/cloned/Server~/build/index.js` (this path is regenerated by `mcp-unity/setup.ps1`
   if it drifts — see [mcp-unity/README.md](file:///c:/Users/noppon/source/01-DG/clogged/mcp-unity/README.md)).
