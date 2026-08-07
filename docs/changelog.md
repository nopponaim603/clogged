# Documentation Changelog — clogged

## 2026-08-07

- **Sprint 01 System & Progress Audit Report**:
  - Created [2026-08-07-sprint-01-progress-report.md](./agile/report-logs/2026-08-07-sprint-01-progress-report.md) under `docs/agile/report-logs/` documenting updates in Phaser prototype and Unity C# architecture (`Assets/Day` & `Assets/Night`).
  - Updated [05-report-backlog.md](./agile/05-report-backlog.md) hub to index the new progress report.
  - Updated [kanban.md](./agile/kanban.md) and [index.md](./index.md) to reflect current project status ahead of Decision Gate (2026-08-09).

## 2026-08-03


- **Game Loop Audit & User Story Breakdown** — Audit `prototype_resource_game` against [gdd/00-concept.md](./gdd/00-concept.md):
  - Created [US-TIME-02.md](./agile/user-stories/US-TIME-02.md) — Day Cycle Bug Fix & Clean State Reset
  - Created [US-MISSION-02.md](./agile/user-stories/US-MISSION-02.md) — Collaborative Mission System
  - Created [US-BASE-02.md](./agile/user-stories/US-BASE-02.md) — Night Phase Base Defense & Escalation
  - Created [US-FOOD-02.md](./agile/user-stories/US-FOOD-02.md) — Starvation & Crew Health Decay
  - Created [US-MAP-01.md](./agile/user-stories/US-MAP-01.md) — Dynamic Map Generation & Difficulty Scaling
  - Created [US-EQUIP-01.md](./agile/user-stories/US-EQUIP-01.md) — Equipment Crafting & Perk Synergy
  - Updated [01-product-backlog.md](./agile/01-product-backlog.md) with updated status and links

## 2026-07-30


- **GDD v1.0 Reverse-Engineered from prototype source code** — Documented all mechanics as implemented in `prototype_resource_game/src/`:
  - [00-concept.md](./gdd/00-concept.md) — Full game concept with scene flow, systems architecture, mission chaining
  - [01-mechanics.md](./gdd/01-mechanics.md) — Day/Night cycle, crew stats, perks, resources, map gen, time system
  - [02-narrative.md](./gdd/02-narrative.md) — World setting, monster ecology, character backgrounds
  - [03-art-direction.md](./gdd/03-art-direction.md) — Color palette, UI layout, visual elements, future art direction
  - [04-audio-direction.md](./gdd/04-audio-direction.md) — Audio plan (currently no audio in prototype)
- Updated `index.md` to link to new GDD file structure

## 2026-07-24
- **GDD อัปเกรดเป็น v0.3 (Day Phase Clarified)** — แปลงจาก Google Doc ต้นฉบับ:
  - ปรับ `gdd/00-concept.md` เป็นโครงสร้าง 3 Phase (Ship → Day → Night), Node Map Roguelike, Tower Defense Hybrid
  - ปรับ `gdd/01-mechanics.md` เป็นกลไกฉบับสมบูรณ์ของ v0.3 (Time System, Unit Specialty, ระบบป้องกัน 4 ทิศทาง, Facility, Crafting)
  - เพิ่ม Design Evolution section ใน concept doc เชื่อมโยงระหว่าง Draft เดิม → Prototype → v0.3
- อัปเดต `docs/index.md` header แจ้งเตือนการเปลี่ยนแปลง

## 2026-07-18
- **Refactored MCP Unity server**:
  - Moved `mcp-unity` out of `ai-gateway/` to the workspace root `mcp-unity/` to make it modular and separate from API routing.
  - Added automation setup scripts (`setup.ps1` and `setup.cmd`) to handle cloning, installing, and building of the external `CoderGamester/mcp-unity` server.
  - Updated configuration files (`kilo.json` and `Unity-Projects/opencode.json`) to use a deterministic static path, resolving the issue with variable commit hashes in the Unity PackageCache.
  - Configured git ignore rules to ignore only the cloned external repository folder (`mcp-unity/cloned/`) while tracking project-specific setup and documentation files.
  - Updated wiki documentation (`docs/wiki/unity-mcp-setup.md`) and added it to the central knowledge hub (`docs/wiki/wiki.md`).

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
