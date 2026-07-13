# Documentation Changelog — clogged

## 2026-07-13
- **Restructured** entire doc suite to the `docs/gdd/`, `docs/software/`, `docs/agile/`, `docs/wiki/` layout.
- **Created** `gdd/00-concept.md`, `gdd/01-mechanics.md` — derived from the original `Idea-design.md` draft, reconciled against the actual `prototype_resource_game/` codebase. Flagged an unresolved **design pivot**: the shipped prototype is a crew-dispatch/day-night survival game, not the factory-pipeline concept in the original draft.
- **Created** `software/01-system-design.md`, `software/02-class-diagram.md` — reverse-engineered from `prototype_resource_game/src/` (entities, systems, scenes, ui).
- **Created** `agile/01-product-backlog.md` with Must/Should/Nice-have items, status-tagged against what's actually implemented.
- **Created** `agile/02-sprint-planning.md` (migrated from `plan.md`) and `agile/sprint-backlogs/sprint-01.md` for the Phase 1 / Prototype sprint (2026-07-13 → 2026-08-09).
- **Migrated** `team.md` → `agile/team.md`; `Meeting Notes/*.md` → `agile/meeting-backlogs/*.md` + new hub `agile/03-meeting-backlogs.md`.
- **Created** stub hubs: `agile/04-retrospectives-backlog.md`, `agile/05-report-backlog.md`, `agile/kanban.md` (no entries yet — sprint 01 just started).
- **Archived** the original `Idea-design.md` verbatim to `wiki/archive/idea-design-draft.md` for historical reference.
- **Created** `wiki/wiki.md` knowledge hub and `wiki/guidelines/system-test-guideline.md` starter guideline.
- **Created** `index.md` as the project's doc inventory and entry point.
- **Flagged for follow-up:** root `README.md` states Unity as the engine; the actual prototype uses Phaser 3 + TypeScript + Vite.
