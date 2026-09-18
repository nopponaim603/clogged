---
name: unity-re
description: >
  Unity Reverse Engineering & Redesign Skill — ใช้เมื่อต้องการ
  (1) Reverse-engineer ส่วนใดส่วนหนึ่งของ Unity codebase เดิม เช่น
  dialogue systems (Fungus/custom flowcharts), Manager/Singleton scripts,
  ScriptableObjects, scoring logic, scene flow, data persistence หรือ third-party plugins
  แล้วบันทึกผลลงในโฟลเดอร์ reverse-engineering/ ในรูปแบบมาตรฐาน หรือ
  (2) วิเคราะห์ผล RE และเขียน redesign decisions ลงในโฟลเดอร์ redesign/
  Trigger เมื่อ: "ถอด", "extract", "reverse engineer", "RE ส่วนนี้", "document ระบบ",
  "เขียน redesign", "วิเคราะห์จาก RE", "update wiki", "map script dependencies",
  "ถอด flowchart", "document architecture" หรือเมื่อผู้ใช้ต้องการสร้างหรือแก้ไขเอกสาร
  ใน reverse-engineering/ หรือ redesign/ ของโปรเจกต์ Unity ใดก็ตาม
---

# Unity Reverse Engineering & Redesign Skill

Skill นี้ให้ template และขั้นตอนสำหรับถอดความเข้าใจจาก Unity project เดิม แล้วแปลงเป็นเอกสารที่ใช้วางแผน migration หรือ redesign ต่อได้

เอกสารแบ่งเป็น 2 ชั้นเสมอ:

```
[project]/docs/wiki/reverse-engineering/   ← บันทึกว่าระบบเดิม "ทำอะไร"
[project]/docs/wiki/redesign/              ← ตัดสินใจว่าระบบใหม่ "ควรทำอย่างไร"
```

---

## ขั้นตอนที่ 1 — ค้นหา Root Design Intent ก่อนอ่าน Code

**อย่าเริ่ม RE ด้วยการอ่าน code ทันที** — ให้สัมภาษณ์ผู้ใช้ก่อนเพื่อเข้าใจว่าระบบเดิม *ตั้งใจ* ทำอะไร เพราะ code ที่เขียนภายใต้ข้อจำกัดด้านเวลาอาจไม่สะท้อน intent ที่แท้จริง การรู้ intent ล่วงหน้าช่วยให้แยกได้ว่าอะไรคือ "design decision" และอะไรคือ "workaround"

### 1A. คำถาม Root Design Intent (ถามก่อนเสมอ)

ถามผู้ใช้ด้วยคำถามต่อไปนี้ตามความเหมาะสม — ไม่จำเป็นต้องถามทุกข้อ เลือกตามส่วนที่จะ RE:

**ภาพรวมระบบ:**
- โปรเจกต์นี้ทำอะไร? ผู้ใช้คือใคร? เป้าหมายหลักของ gameplay / flow คืออะไร?
- มี platform เป้าหมายอื่นนอกจาก Unity ไหม (Web, Mobile, PC)?
- ระบบนี้สร้างขึ้นในช่วงเวลาและข้อจำกัดอะไร?

**Dialogue / Narrative:**
- บทสนทนาในเกมมี branching จริงๆ หรือเป็นแค่ linear? ผู้เล่น choice มีผลต่อ outcome ไหม?
- ตัวละครมี gender variant หรือ language variant อื่นไหม?
- ข้อความบางส่วนมาจาก localization file หรืออยู่ใน Flowchart โดยตรง?

**Architecture / Managers:**
- Manager แต่ละตัวรับผิดชอบอะไร? มีตัวไหนที่ "ทำมากเกินไป" ในความเห็นของผู้ใช้ไหม?
- ระบบสื่อสารกันยังไง — ผ่าน event, message string, หรือ direct reference?
- มีส่วนไหนที่รู้อยู่แล้วว่าเป็น technical debt หรือ workaround?

**Data / Scoring:**
- ข้อมูลผู้เล่น save/load ยังไง? มี cloud sync ไหม?
- scoring หรือ grade คำนวณจากอะไร? มี edge case ที่รู้อยู่แล้วไหม?

### 1B. ระบุ scope และ path

หลังเข้าใจ intent แล้วให้ยืนยัน:
1. **โฟลเดอร์ wiki อยู่ที่ไหน** — ถ้าไม่ระบุให้ใช้ `docs/wiki/`
2. **ต้องการทำ RE หรือ Redesign** — หรือทั้งสองอย่าง
3. **หัวข้อ / component** ที่ต้องการทำ (ดูตารางด้านล่าง)

### ประเภทเอกสารที่รองรับ

| หัวข้อ | โฟลเดอร์ปลายทาง | แหล่งข้อมูลหลัก |
|-------|----------------|----------------|
| Dialogue system (Fungus/custom) | `reverse-engineering/dialogue/` | `.unity` scene files หรือ Unity MCP |
| Script & Manager map | `reverse-engineering/architecture/` | `Assets/Scripts/` หรือ path เทียบเท่า |
| Scene flow & transitions | `reverse-engineering/architecture/` | SceneManager calls, Build Settings |
| Data structures (save/load) | `reverse-engineering/architecture/` | Save scripts, ScriptableObjects |
| Scoring / grading logic | `reverse-engineering/scoring/` | Score calculation scripts |
| Unity packages & plugins | `reverse-engineering/architecture/` | `Packages/manifest.json`, `Assets/plugins/` |
| Script analysis (รายไฟล์) | `reverse-engineering/scripts/` | `.cs` files ใน `Assets/Scripts/` |
| Migration risk register | `reverse-engineering/migration/` | ใช้ผล RE ที่มีอยู่ |
| Architecture redesign | `redesign/` | RE script map |
| Dialogue redesign | `redesign/` | RE dialogue files |
| Data schema redesign | `redesign/` | RE data structures |
| Scoring redesign | `redesign/` | RE scoring logic |

---

## ขั้นตอนที่ 2 — System Flow Overview (ทำก่อน RE รายละเอียด)

ก่อนลงไปดู script หรือ flowchart ทีละไฟล์ ให้สร้างภาพรวม flow ของระบบทั้งหมดก่อน เพื่อให้รู้ว่า RE ส่วนไหนสำคัญและควรทำก่อน

### 2A. Overview Flow (ระดับโปรเจกต์)

สร้างไฟล์ `reverse-engineering/architecture/re-scene-flow.md`:

```markdown
# Scene Flow — Overview

**Project:** [ชื่อโปรเจกต์]
**Extracted:** [YYYY-MM-DD]

---

## Scene Inventory

| # | Scene Name | File | หน้าที่ |
|---|-----------|------|--------|
| 1 | [ชื่อ] | [ชื่อไฟล์.unity] | [คำอธิบาย] |

---

## Flow Diagram

[Entry] → [Scene A] → [Scene B] → [Scene C] → [End]
               ↓ (condition X)
           [Scene B2]

---

## Transition Map

| จาก Scene | ไปยัง Scene | Trigger / Condition | Script ที่รับผิดชอบ |
|----------|------------|--------------------|--------------------|
| Menu | Gameplay | กด Start | SceneManager.LoadScene() |
```

ไฟล์นี้ตอบ 4 คำถาม: Entry point คืออะไร, Scene ทั้งหมดมีอะไรบ้าง, ลำดับการเดินทางของ player ตั้งแต่ต้นจนจบ, และอะไร trigger การเปลี่ยน scene

### 2B. Per-Scene Deep Dive (ระดับ scene)

หลังมี overview แล้ว RE แต่ละ scene สำคัญลงในไฟล์แยก ชื่อ `re-scene-[ชื่อ].md`:

```markdown
# Scene: [ชื่อ Scene] — Deep Dive

**File:** [ชื่อ .unity]
**Purpose:** [หน้าที่หลัก]
**Entry from:** [scene ก่อนหน้า / conditions]
**Exit to:** [scene ถัดไป / conditions]

---

## Objects & Scripts

| Object | Script(s) | หน้าที่ |
|--------|----------|--------|
| [GameObject name] | [ScriptName.cs] | [อธิบาย] |

---

## Initialization Sequence

1. [สิ่งที่เกิดขึ้นใน Awake]
2. [สิ่งที่เกิดขึ้นใน Start]
3. [event/callback ต่อมา]

---

## State Dependencies

[อะไรที่ scene นี้ต้องการจาก scene ก่อนหน้า เช่น player data, save file, flags]

---

## Known Issues / Observations

[สิ่งที่สังเกตได้จาก RE — coupling ที่แปลก, hardcoded values, TODO comments]
```

---

## ขั้นตอนที่ 3 — Reverse Engineering รายละเอียด

### 3A. Dialogue Extraction

ใช้สำหรับ Fungus Flowcharts หรือ dialogue system อื่นๆ ใน Unity
ทุกไฟล์ใน `dialogue/` ใช้ template เดียวกัน:

```markdown
# [Flowchart / Dialogue System Name] — Dialogue Extraction

**Scene:** [ชื่อ scene]
**Extracted:** [YYYY-MM-DD]  |  **By:** [ชื่อ/เครื่องมือ]  |  **Method:** [MCP / Read .unity / Script]
**Blocks:** [จำนวน]  |  **Total lines:** [จำนวน dialogue lines]
**Variables:** [ชื่อตัวแปรที่ใช้ เช่น Try (Integer), IsComplete (Boolean)]
**Unresolved refs:** [ตัวแปรหรือ string reference ที่ยัง resolve ไม่ได้ หรือ "None"]

---

## Block: [Block Name]
**Trigger:** [OnStart / Message "XXX" / Call from Block "YYY" / ระบุ trigger type]
**Character:** [ชื่อตัวละคร หรือ -]
**Portrait:** [asset name หรือ -]

| # | Speaker | Text | Type | Choice Options | Branch to |
|---|---------|------|------|----------------|-----------|
| 1 | NPC_A | "ข้อความ..." | Say | — | — |
| 2 | — | — | Menu | A: "ตัวเลือก A" → Block_A \| B: "ตัวเลือก B" → Block_B | — |
| 3 | — | Call block: NextBlock | Call | — | NextBlock |

**Score/Event Trigger:** [เช่น PassMission1 / BroadcastMessage "EventName" / None]
```

**กฎการ extract:**
- `Say` / dialogue command → เขียนข้อความเต็มในคอลัมน์ Text
- String interpolation เช่น `{$VarName}` → resolve ถ้าทำได้ ไม่งั้นทิ้งไว้เป็น placeholder และบันทึกใน `_unresolved-refs.md`
- `Call Block` → Type = "Call", Branch to = ชื่อ Block ปลายทาง
- `Menu` / choice command → เขียน options ทุกอันรูปแบบ `Label: "ข้อความ" → BlockName`
- ตัวแปร Flowchart (Integer, Boolean ฯลฯ) → บันทึกที่ header
- Language/gender variant → บันทึกทั้งสองเวอร์ชันในบรรทัดเดียวคั่นด้วย ` / `
- Event ที่ trigger ไปยัง C# (เช่น `BroadcastFungusMessage`) → บันทึกใน **Score/Event Trigger**

สร้าง `dialogue/README.md` เป็น inventory รวมทุก flowchart พร้อมสถานะ
สร้าง `dialogue/_unresolved-refs.md` รวม string references ที่ยัง resolve ไม่ได้ทั้งหมด

### 3B. Architecture Documents

ไฟล์ใน `architecture/` ใช้โครงสร้าง:

```markdown
# [ชื่อหัวข้อ — เช่น Script Dependency Map]

[บทนำสั้นๆ อธิบายว่าเอกสารนี้ครอบคลุมอะไร]

---

## [ส่วนที่ 1 — เช่น Manager Scripts]

| Class | Pattern | Dependencies | Purpose |
|-------|---------|-------------|---------|
| ...   | ...     | ...         | ...     |

## [ส่วนที่ 2 — เช่น Event / Message Map]

[อธิบาย coupling pattern ระหว่าง systems]
```

สิ่งที่ต้องครอบคลุมตามประเภทเอกสาร:
- **script-map**: Manager classes, Singleton pattern, inter-system dependencies, Fungus/event integration points
- **scene-flow**: scene transitions, trigger conditions, loading/unloading logic
- **data-structures**: save schema fields, serialization format, cloud sync points

### 3C. Scoring / Game Logic

```markdown
# [ชื่อระบบ] — Logic Extraction

## Input Parameters
[ตัวแปรที่รับเข้ามา]

## Calculation
[สูตรหรือ pseudo-code]

## Output / Grade Thresholds
[ผลลัพธ์และเงื่อนไขต่างๆ]

## Edge Cases
[กรณีพิเศษที่พบ]
```

### 3D. Script Analysis (รายไฟล์)

ใช้สำหรับวิเคราะห์ C# script แต่ละไฟล์อย่างละเอียด บันทึกลงใน `reverse-engineering/scripts/`

**เมื่อใดควรสร้างเอกสารใน `scripts/`:**
- script มี business logic ซับซ้อน เช่น state machine, event chain, async flow
- script เป็น target ของ migration (เช่น Fungus → Ink, local save → cloud)
- script มี workaround หรือ technical debt ที่ต้องบันทึกก่อนแก้ไข
- ต้องการรายละเอียดมากกว่าที่ตาราง overview ใน `re-script-map.md` ให้ได้

**Naming:** `re-script-[ClassName].md` เช่น `re-script-GameManager.md`

**Template:**

```markdown
# Script: [ClassName].cs — Analysis

**Path:** [Assets/Naplab/Scripts/ClassName.cs]
**Extracted:** [YYYY-MM-DD]
**Pattern:** [Singleton / MonoBehaviour / ScriptableObject / Static / Other]
**Scene(s):** [scene ที่ใช้ script นี้ หรือ "All (DontDestroyOnLoad)"]

---

## Purpose

[อธิบายว่า script นี้มีหน้าที่อะไรในระบบ — 2-3 ประโยค]

---

## Public API

| Member | Type | Parameters | หน้าที่ |
|--------|------|-----------|--------|
| [MethodName] | Method | (param: type) | [อธิบาย] |
| [PropertyName] | Property | — | [อธิบาย] |
| [EventName] | Event / Action | — | [อธิบาย] |

---

## Dependencies

| ขึ้นอยู่กับ | ประเภท | วิธีเชื่อม |
|------------|--------|-----------|
| [ClassName] | Manager/Script | [direct ref / FindObjectOfType / static] |
| [EventName] | Unity Event | [BroadcastMessage / UnityEvent / C# event] |

---

## Key Logic Flow

[Awake] → Initialize X
[Start] → Subscribe to events, call Y.Init()
[Main method / state machine] →
  Case A → path 1
  Case B → path 2

---

## State / Data Fields

| Field | Type | Default | หน้าที่ |
|-------|------|---------|--------|
| [fieldName] | [type] | [default] | [อธิบาย] |

---

## Known Issues / Technical Debt

[workaround, hardcoded value, TODO comment, coupling ที่แปลก — หรือ "None identified"]

---

## Migration Notes

[สิ่งที่ต้องพิจารณาเมื่อ refactor หรือ replace — หรือ "N/A"]
```

**กฎการเขียน:**
- **Public API** — บันทึกเฉพาะ public members ที่ scripts อื่นเรียกใช้จริง ไม่ต้องลง private helper ทุกอัน
- **Dependencies** — ระบุวิธีเชื่อมต่อจริง (direct reference, static access, event) ไม่ใช่แค่ชื่อ class
- **Key Logic Flow** — ใช้ pseudo-code หรือ ASCII diagram แสดง flow หลัก ไม่ต้อง copy code ทั้งหมด
- **Migration Notes** — บันทึกเฉพาะเมื่อ script นี้อยู่ใน migration plan

สร้าง `scripts/README.md` เป็น index รวมทุก script ที่วิเคราะห์แล้ว:

```markdown
# Script Analysis — Index

**Last Updated:** [YYYY-MM-DD]

| Script | Pattern | สถานะ RE | Migration Target | หมายเหตุ |
|--------|---------|---------|-----------------|---------|
| GameManager.cs | Singleton | 🟢 Done | ยังคงใช้ต่อ | — |
| DialogManager.cs | Singleton | 🟡 In Progress | Ink integration | — |
| PartManager.cs | Singleton | ⬜ Pending | — | — |
```

---

---

## ขั้นตอนที่ 4 — อัปเดต Index

หลังสร้างหรือแก้ไขไฟล์ทุกครั้งให้อัปเดต `README.md` ของโฟลเดอร์นั้น:
- เพิ่มแถวในตาราง **สถานะเอกสาร**
- ใช้ emoji: 🟢 Done / 🟡 In Progress / ⬜ Pending
- อัปเดต `Last Updated`

ถ้ายังไม่มี `README.md` ให้สร้างใหม่ด้วยโครงสร้าง:

```markdown
# [RE / Redesign] — Knowledge Base

**Last Updated:** [YYYY-MM-DD]

[คำอธิบายโปรเจกต์และวัตถุประสงค์ของเอกสารชุดนี้]

---

## โครงสร้างเอกสาร
[tree หรือตาราง]

## สถานะเอกสาร

| หมวด | ไฟล์ | สถานะ |
|------|------|-------|
| ... | ... | ⬜ Pending |

## วิธีใช้เอกสารเหล่านี้
[ตารางแนะนำว่าเอกสารแต่ละชิ้นใช้เมื่อไหร่]
```

---

## ขั้นตอนที่ 5 — Redesign Documents

Redesign doc คือ **Decision Log** — วิเคราะห์ว่าจะเปลี่ยนอะไรและทำไม ไม่ใช่แค่ถ่ายทอด RE

### สถานะ Component

| สัญลักษณ์ | ความหมาย |
|----------|---------|
| ✅ **ใช้ได้** | นำมาใช้ตรงๆ — เป็นเนื้อหา/กฎที่ถูกต้องแล้ว |
| 🔄 **Redesign** | เข้าใจ intent แล้ว แต่ implement ใหม่ให้ดีกว่า |
| ❌ **ทิ้ง** | ไม่เหมาะสม ไม่นำมาใช้ |

### Template สำหรับ Redesign Doc

```markdown
# Redesign: [หัวข้อ]

**อ้างอิง RE:** [ลิงก์ไปยังไฟล์ RE ที่เกี่ยวข้อง]
**Last Updated:** [YYYY-MM-DD]

---

## ปัญหาที่พบจากระบบเดิม

### 1. [ชื่อปัญหา]

[อธิบาย pattern หรือ design ที่มีปัญหา — อ้างอิงจาก RE docs]

**ผลกระทบ:**
- [ผลกระทบ 1]
- [ผลกระทบ 2]

---

## แนวทาง Redesign

### หลักการ: [ประโยคสั้นๆ อธิบาย approach]

[อธิบาย pattern ใหม่ที่แนะนำ — ใช้ code snippets ถ้าช่วยให้ชัดขึ้น]

---

## สรุปการตัดสินใจ

| Component | สถานะ | หมายเหตุ |
|-----------|-------|---------|
| [ชื่อ] | ✅/🔄/❌ | [เหตุผลสั้นๆ] |
```

**สิ่งที่ต้องมีในทุก redesign doc:**
1. อ้างอิง RE document ต้นทางเสมอ
2. อธิบาย "ทำไม" ถึง redesign — ไม่ใช่แค่ "ทำอะไร"
3. ถ้า redesign สร้าง interface ใหม่ ให้แสดง code signature ตัวอย่าง
4. ถ้าต้องรองรับหลาย platform ให้แสดงทุกฝั่ง

---

## Naming Conventions

| ประเภท | รูปแบบ | ตัวอย่าง |
|-------|--------|---------|
| RE architecture / overview | `re-[topic].md` | `re-script-map.md` |
| RE scene detail | `re-scene-[name].md` | `re-scene-gameplay.md` |
| RE dialogue | `[scene/part]-[flowchart].md` | `part1-mission1.md` |
| RE helper files | `_[topic].md` (ขึ้นต้นด้วย `_`) | `_unresolved-refs.md` |
| RE scoring | `re-[topic]-logic.md` | `re-scoring-logic.md` |
| RE script analysis | `re-script-[ClassName].md` | `re-script-GameManager.md` |
| Redesign | `rd-[topic].md` | `rd-architecture.md` |

---

## การใช้ Unity MCP

ถ้ามี Unity MCP (เช่น antigravity-unity-mcp) ให้ใช้:
- `execute_menu_item` — inspect scene objects, Flowchart blocks
- `get_hierarchy` หรือเทียบเท่า — ดู scene structure

ถ้าไม่มี MCP ให้อ่านไฟล์โดยตรง:
- `.unity` files — YAML format, ค้นหา component type ที่ต้องการ
- `.cs` files — อ่านผ่าน Read tool หรือ Grep
- `Packages/manifest.json` — รายการ packages ทั้งหมด