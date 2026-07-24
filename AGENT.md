# AGENT.md

Guidance for AI coding agents working in this repo.

## Project

**clogged** — a Resource Management + Action Survival game (Phaser 3 + TypeScript + Vite). Players manage a base and a limited crew: dispatch crew to gather resources, search relics, and hunt monsters by day; defend the base by night. Core theme: resource *flow* must not clog — over-stockpiled resources spoil at day's end, shortages starve the crew.

การพัฒนาเกมแบ่งเป็น **2 ระยะ**:
1. **Web Prototype** — วิเคราะห์กลไกเกมและทดสอบ gameplay loop ด้วย Phaser/TS/Vite ให้ได้ prototype ที่ใช้งานได้
2. **Unity Development** — พัฒนากันจริงจังด้วย Unity สำหรับเผยแพร่เกม

ระยะปัจจุบันอยู่ที่ **Phase 1 — Prototype / Vertical Slice** (2026-07-13 → 2026-08-09, see `docs/agile/sprint-backlogs/sprint-01.md`). The original idea (factory/conveyor pipeline) was pivoted away from — see `docs/gdd/00-concept.md#2-️-design-pivot--บันทึกไว้เพื่อความชัดเจน` before assuming the archived draft at `docs/wiki/archive/idea-design-draft.md` is current.

Docs are largely in Thai. Start at [docs/index.md](docs/index.md) for the full doc index.

## Layout

```
clogged/
├── prototype_resource_game/   ← the actual game code (Phaser 3 + TS + Vite)
├── docs/                      ← GDD, software design, agile docs (see docs/index.md)
├── .agents/                   ← agent rules/skills/workflows used in this repo
├── Packages/, ProjectSettings/← present but not the active engine (see note below)
```

Note: `Packages/` and `ProjectSettings/` look like Unity project scaffolding but the game is built in Phaser/TS under `prototype_resource_game/`. Don't assume Unity workflows apply.

## Working in `prototype_resource_game/`

```bash
cd prototype_resource_game
npm install
npm run dev      # start dev server (also logs via log.js)
npm run build    # production build
```

No test suite exists yet (see Testing below) and no lint/typecheck npm script is defined — `tsc` config lives in `tsconfig.json` if you need to typecheck manually.

### Source structure (`src/`)

Layering principle — respect it when adding code:

- `data/` — static data (Constants, CrewData, ResourceData templates)
- `entities/` — stateful game objects that compute their own values; **don't** reference scenes (`Base.ts`, `Crew.ts`, `Monster.ts`, `ResourceNode.ts`)
- `systems/` — cross-entity orchestration logic; **don't** draw anything (`TimeSystem`, `CrewManager`, `ResourceManager`, `MissionSystem`, `MapGenerator`)
- `scenes/` — Phaser scene lifecycle; `GameScene` is the orchestrator that wires everything together and handles input
- `ui/` — Phaser display objects that read system state to render; **don't** put business logic here
- `utils/` — helpers

`config.ts` holds all game-balance numbers (`GAME_CONFIG`) plus resource icon/type constants — check there before hardcoding balance numbers elsewhere.

Full architecture write-up (including a sequence diagram of one in-game day): [docs/software/01-system-design.md](docs/software/01-system-design.md). Class diagram: [docs/software/02-class-diagram.md](docs/software/02-class-diagram.md).

### Known technical debt (per system-design doc — be aware before extending these areas)

- `MissionSystem` duplicates ~4 near-identical solo/collaborative handlers (`executeGathering`/`executeRelicSearch`/`executeMonsterHunt` × solo/collaborative). A good refactor target, but don't refactor it opportunistically mid-feature-work — it's flagged for a deliberate Phase 2 pass.
- `GameScene` is 1000+ lines and does input handling + mission queue orchestration + rendering. Same caution: known, tracked, not yet worth an unscoped drive-by fix.
- Food shortage is computed in `ResourceManager.consumeFood` but has no gameplay consequence yet — this is an intentional open design question, not a bug (see `docs/gdd/01-mechanics.md`).

## Testing

No automated tests exist yet (Phase 1, pre-formal-testing). Manual checklist before merging changes to `systems/` or `entities/` (from `docs/wiki/guidelines/system-test-guideline.md`):

- [ ] Play a full day end-to-end (planning → execution → night → new day) without crashing
- [ ] Win condition (30 days) and lose conditions (crew all dead / base HP ≤ 0) work correctly
- [ ] No negative resources or `NaN` shows up in the UI

Record results under `docs/agile/reports/` (`qa/`, `polish/`, or `bugs/`) and link them from `docs/agile/05-report-backlog.md`.

## Docs workflow

This repo tracks design and process in `docs/` alongside code — treat it as living documentation, not a one-time dump:

- Game design: `docs/gdd/`
- Software/architecture design: `docs/software/`
- Agile process (backlog, sprints, team, reports): `docs/agile/`
- Knowledge hub / guidelines / archive: `docs/wiki/`

If a change affects game mechanics, architecture, or sprint scope, update the relevant doc in the same change rather than leaving docs stale.
