# Documentation Changelog — clogged

## 2026-10-27

- **UI Layout Redesign Spec (Draft v0.2)** — ร่างสเปก UI ใหม่จากแบบร่างกระดาษ 3 โซน:
  - สร้าง [07-unity-v2-ui-layout-redesign.md](./software/07-unity-v2-ui-layout-redesign.md): layout ใหม่ทั้งจอ (CoreItemA/B, DayFrame+EndDay, LogMessagePanel, CharacterHandPanel, DispatchActionPanel, SkillTriggerPanel), interaction model (คลิกซ้าย select/เพิ่มจำนวน, คลิกขวาล้างจำนวน), เงื่อนไขชนะ/แพ้จาก Core Item 2 ชิ้น, contracts L1–L5, ผลกระทบรายไฟล์, phases P0–P5, คำถามเปิด Q1–Q8
  - **v0.13 (P5 implemented — ครบทุก phase):** ลบของเดิมออกจาก `Core.unity` ทั้งหมด — `EndDay button`, `BaseWorkPanel` (+ ลบไฟล์ `.cs`), `CharCard`, `MapPanel/MessageUi`, `MapPanel/Dispatch`, `MapPanel/ScoutIcon` (+ ลบ `ScoutWalkerView.cs` และ property `TripInProgress`) · `DayFrame` เป็นปุ่มจบวันเองผ่าน `EndDayTrigger` (ไม่มี confirm) · เพิ่ม `CoreItemFrameB` + ผูก `StatusBarView.coreItemBIcon` · ลบ missing script ของ `ScoutWalkerView` ออกจาก `ScoutIcon.prefab` · เปลี่ยน `✓` เป็น ASCII เพราะฟอนต์ไม่มี glyph · `GatherDispatchPanel` เลิกใช้ `messageText` (ข้อมูลถ้ำ + คำเตือน ลง log/warning แทน) · ปรับ map margin กันถ้ำไปอยู่ใต้ panel · LogPanel ทึบ 100% — ทดสอบผ่าน 19/19 (P5) + 35/35 (P4) + 17/17 (P3), console 0 error/warning · เพิ่ม §7.10, AC ข้อ 20–22
- **v0.14 (แก้ bug: กด DayFrame ไม่ได้ — §7.11):** ทุก Graphic ใต้ `DayFrame` มี `raycastTarget = false` ทำให้ raycast ทะลุไป `MapPanel` → เปิด `raycastTarget` ที่ `DayBackground`, ปิดที่ `Day`, เพิ่ม `EndDayTrigger.EnsureClickSurface()` กัน bug กลับมา + warning "The run is already over." · พบถ้ำ 1 ใบ rect คาบเกี่ยวกรอบ DAY → ขยาย margin เป็น R400/T300 · เพิ่มการตรวจ clickability ด้วย `EventSystem.RaycastAll` จริง (ไม่ใช่เรียก handler ตรง ๆ) · `TestCleanup` เพิ่ม 3 ข้อ (raycast hit, handler resolve, ไม่มีถ้ำใต้กรอบ) — ยอดรวม P5 = 22/22 · AC ข้อ 23
  - **v0.15 (รวมชุดทดสอบ — §7.12):** ยุบ 4 สคริปต์ (`TestCleanup`, `TestSkillFlow`, `TestDispatchGating`, `TestClickTargets`) เป็น **`AgentScripts/TestUiRedesign.cs`** ไฟล์เดียว **32 ข้อ รันครั้งเดียวจบ** (เข้า play mode รอบเดียวแทน 4 รอบ) · แก้ความสามารถในการรันซ้ำ: `ResetUnits`/`Revive` แยกกันเพราะบาดแผลคงอยู่ข้ามวัน, `FirstSelectable` แทน `Cards[0]`, `ForceSkillPanelRefresh` เพราะ UI รีเฟรชใน `Update` · ลบสคริปต์ one-shot ที่ทำเสร็จแล้วทั้งหมด (กันรันซ้ำไปทับค่าที่แก้) · เพิ่ม `ProjectSettings/Packages/com.unity.pipeline/` ใน `.gitignore`
  - **เขียน [06-unity-core-scene-hierarchy.md](./software/06-unity-core-scene-hierarchy.md) ใหม่เป็น v3.0** — hierarchy จริงหลัง P5 (`MapPanel` เหลือแค่พื้นหลัง, `EndDayTrigger` บน `DayFrame`, 2 ช่อง Core Item, `CharacterHandPanel` / `DispatchActionPanel` / `LogPanel` / `SkillTriggerPanel`), ตาราง Inspector reference ของทุก UI script, ตาราง "สิ่งที่ถูกลบไปแล้ว" กันเอกสารอื่นอ้างของเก่า, `TestDispatch` → `GatherUI`
  - **v0.12 (P3 + P4 implemented):** ต่อ hand + ปุ่มยืนยันตัวเดียว + สกิลทั้งชุดเข้า `Core.unity` — `CharacterHandPanel` (โหมด `Dispatch` / `Skill` / `HealTarget`), `DispatchActionPanel` (ซ่อนเมื่อว่าง, label ตามบริบท, gate ข้าม flow ด้วย `IsSkillActive`), `UnitRosterView` คำนวณ selectability ใหม่ทุกเฟรม, `GatherMapController.ConfirmTrip` resolve ทันที; เพิ่ม `SkillTriggerPanel` (Cook/Craft counter + คลิกขวาล้าง, Heal ไม่มี queue + toggle `MED`/`AP`), `RightClickHandler` และ `SkillFlowController`; ปิด `BaseWorkPanel` เดิมในซีน — ทดสอบ play mode ผ่าน 35/35 (สกิล) + 16/16 (regression การสำรวจ) · เพิ่ม §7.8/§7.9 และปรับ §4.5/§4.6 ให้ตรงที่ลงมือจริง (กฎเลือกการ์ดใช้ `CanBeDispatched` ร่วมกันทั้ง 2 flow)
  - **v0.11 (P3 started):** ปรับ `CharacterHandPanel` / `DispatchActionPanel` / `UnitRosterView` / `UnitIconView` ให้ hand ใช้งานได้จริง, `GatherMapController` เลิกผูก `TripInProgress`, `BaseWorkPanel` เลิก gate ระหว่างทริป
  - **v0.10 (P2 log wiring):** `LogMessagePanel` render จาก `RunLog` ด้วย row pool + ย่อ/กางพร้อมย่อความสูง panel (300→64) · เพิ่ม `RunLogNarrator` แปลง event เป็น log (`OnBaseWorkCompleted`, `OnDayEnded`, `OnGatherResolved`, `OnRunEnded`) · `BaseWorkController` เพิ่ม `OnBaseWorkCompleted` + overload รับ `useMedicine` · `DayManager` เพิ่ม `DayEndReport` / `PreviewFeeding()` / `OnDayEnded` · ทดสอบใน play mode ผ่าน (capacity 16, minimize, narrator) — เพิ่ม §7.7
  - **v0.9 (P1 prefabs):** สร้าง prefab skeleton 4 ตัวใน `Assets/V2/Prefabs/UI/` (`LogPanel`, `CharacterHandPanel`, `DispatchActionPanel`, `SkillTriggerPanel`) ด้วย `AgentScripts/BuildUIPrefabs.cs` — ตรวจด้วยภาพผ่าน Canvas ชั่วคราวแล้ว แก้ 2 จุด (hand กว้าง 800 + การ์ด 190 กันทับ Log, ปุ่มสลับ Heal เปลี่ยนจาก glyph `⇄` เป็นข้อความ `MED`/`AP`), เพิ่ม §3.3 ตัวเลข layout ที่ล็อก และ §7.6 รายละเอียด prefab · ไม่แตะ `Core.unity`
  - **v0.8:** ปิด Q8 — **เกมไม่มีช่วง in-progress**: dispatch resolve ทันที ผลขึ้น log ทันที ตัวละครเป็น Busy (`HasGatheredToday`) แต่ยังทำสกิลได้ถ้ามี AP, **End Day กดได้ทุกเมื่อ** (รีเซ็ตสถานะ + คืน AP); ลบ `TripInProgress`/`ScoutWalkerView`/ฐานบน map ออกจากสเปก, เพิ่ม selectability ตามโหมด (dispatch = `CanBeSentGathering`, skill = `CanBeDispatched`), เพิ่ม contract L7/L8, ปรับ AC เป็น 19 ข้อ
  - **v0.7:** ปิดคำถาม Q1–Q7, Q9 ตามการตัดสินใจ — ResourceStrip คงกลางบน · โหมดสกิลเลือกการ์ด **1 ใบ** · skill panel แสดงตลอด · **Log อยู่มุมซ้ายล่างและห้ามล้ำเข้า MAP AREA** (เก็บ 16 entry, แสดงเท่าที่ช่องใส่ได้) · **Heal ไม่มี queue** (เลือกผู้ทำ → Heal → เลือกผู้ป่วย → ยืนยัน) · **ไม่มี confirm dialog ก่อนจบวัน** · Easy ดรอป 0% — เหลือ Q8 (warning ระหว่างทริป) และบันทึกว่าต้องเพิ่ม overload `BaseWorkController` รับ `useMedicine`
  - **v0.6 (P0 implemented):** ลงมือส่วน data & logic แล้ว — เพิ่ม `CoreItemKind`, `RunLog`/`LogEntry`/`LogKind` (static, capacity 200), `RunResources` (A/B + `HasBothCoreItems` + `OnChanged`), `GatherZoneConfig.coreItemChancePercent` (Easy 0 · Normal 5 · Hard 15), `TryRollCoreItems` (เฉพาะ HardSuccess), `DayManager` ใช้ `HasBothCoreItems`; อัปเดต `StatusBarView`/`GatherDispatchPanel` ให้ compile และแสดง A/B · compile ผ่าน, ทดสอบใน Editor แล้ว (0 error)
  - **v0.5:** ลด Hard Core Item chance จาก 25% → **15%** (ผลจริงต่อทริป ≈ 8.9% เทียบ Normal ≈ 4.4%)
  - **v0.4:** ปรับโมเดล Core Item drop ให้ตรงความหมาย "โอกาสต่อทริป" — ฟิลด์เดียว `coreItemChancePercent` **ทอยเฉพาะ tier `HardSuccess`**, ทอยแยกต่อชิ้นและข้ามชิ้นที่มีแล้ว (มีครบ 2 = ไม่ทอย); ค่าเริ่มต้น Easy 0% · Normal 5% · Hard 25% (จริงต่อทริป ≈ 4.4% vs 14%) ลบ `CoreItemDrop`/`hardSuccessChance`/`regularChance`
  - **v0.3:** Core Item ดรอปผ่าน **drop table ใน `GatherZoneConfig`** (แบบ 2) — ต่อ entry มี `hardSuccessChance` / `regularChance`, ทอยแยกอิสระต่อชิ้น, เฉพาะ `HardSuccess`/`Regular`; ค่าเริ่มต้น Normal 40/15 และ **Hard 75/25** (Hard ดรอปมากกว่า) เพิ่ม contract L6
  - **v0.2:** Core Item ทั้ง 2 ชิ้น (A = escape, B = key) **ดรอปอิสระจากกัน ไม่มีลำดับ** — ตัดกฎ "ต้องได้ key ก่อน" และบันทึกผลกระทบต่อ `GatherDispatchController.HandleCoreItemProgress` / `GatherDispatchResult` (เจ้าของ = กลุ่ม 2 ต้องประสาน)
  - อัปเดต [index.md](./index.md) ส่วน Software Design ให้ลิงก์เอกสาร Unity V2 ครบชุด
  - **สถานะ: รออนุมัติ — ยังไม่มีการแก้ code หรือ scene**
- **ถอด MCP Unity ออกทั้งหมด** (เลิกใช้ MCP bridge, ใช้ `unity` CLI / unity-pipeline แทน):
  - ลบ `mcp-unity/` (README + `setup.ps1` / `setup.cmd` / `setup.sh` + `cloned/` ~375 MB), `com.gamelovers.mcp-unity` ถูกถอดออกจาก `Unity-Projects/Packages/manifest.json` แล้วโดย commit `53a2807`.
  - ลบ `.agents/skills/unity-mcp-tools/`, `Unity-Projects/ProjectSettings/McpUnitySettings.json`, `Unity-Projects/AgentScripts/RestartMcp.cs`, `Unity-Projects/opencode.json`.
  - ลบ `mcp` block ออกจาก `.kilo/kilo.json` และกฎ ignore ของ `mcp-unity/cloned/` ออกจาก `.gitignore`.
  - ลบ `docs/wiki/agentic-ai/unity-mcp-setup.md` และลิงก์อ้างอิงใน [wiki hub](./wiki/wiki.md), [Naplab AI Provider](./wiki/agentic-ai/ai-provider-naplab.md).
  - อัปเดต skill `unity-csharp-conventions` และ `unity-scene-workflow` ให้ใช้คำสั่ง `unity command …` แทนเครื่องมือ MCP เดิม.

## 2026-08-08

- **Task & Progress Tracker Weekly Report (W32)**:
  - Generated [weekly-2026-W32.md](./agile/reports/weekly/weekly-2026-W32.md) under `docs/agile/reports/weekly/` using the `task-tracker` skill.
  - Updated User Stories status: [US-TIME-02](./agile/user-stories/US-TIME-02.md) (🟢 DONE), [US-MISSION-02](./agile/user-stories/US-MISSION-02.md) (🟢 DONE), [US-BASE-02](./agile/user-stories/US-BASE-02.md) (🟢 DONE).
  - Updated [Product Backlog](./agile/01-product-backlog.md), [Sprint 01 Backlog](./agile/sprint-backlogs/sprint-01.md), [Report Backlog](./agile/05-report-backlog.md), [Kanban Board](./agile/kanban.md), and [Index](./index.md).

## 2026-08-07

- **Sprint 01 System & Progress Audit Report**:
  - Created [2026-08-07-sprint-01-progress-report.md](./agile/reports/2026-08-07-sprint-01-progress-report.md) under `docs/agile/report-logs/` documenting updates in Phaser prototype and Unity C# architecture (`Assets/Day` & `Assets/Night`).
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
