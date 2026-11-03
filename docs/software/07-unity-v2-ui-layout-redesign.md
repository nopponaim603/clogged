# clogged (Unity V2) — UI Layout Redesign Spec

**Version:** 0.23 (Draft) | **Last Updated:** 2026-10-28 | **Status:** ✅ P0–P5 เสร็จครบ · แก้ bug DayFrame click (§7.11) · รวมชุดทดสอบเหลือไฟล์เดียว (§7.12) · ไฮไลต์คนที่รักษาได้ (§7.13) · ฟอนต์จาก tileset (§7.14) · เปลี่ยน TMP ทั้ง V2 UI (§7.15) · จัดกลุ่ม `Warning` ให้ตรงกัน (§7.16) · สี Warning = สี log (§7.17) · รอ final review
**ที่มา:** แบบร่างกระดาษ 3 โซน + คำอธิบายผู้พัฒนา (2026-10-27)
> เอกสารนี้แปลงแบบร่างเป็นสเปกที่ลงมือได้ — §7.5–§7.9 คือสถานะที่ลงมือจริงแล้ว
> ประกอบ: [Unity V2 System Design](04-unity-system-design.md) · [Core Scene Hierarchy](06-unity-core-scene-hierarchy.md) · [V2 Task Overview](../agile/user-stories/V2-TASK-OVERVIEW.md) · [GDD Mechanics](../gdd/01-mechanics.md)

---

## 1. ที่มาและปัญหาที่ต้องแก้

| # | ปัญหา | รายละเอียด |
|---|---|---|
| P-1 | **ตำแหน่ง UI สลับกันในบิลด์ที่นำเสนอ** | `WarningText` ไปแสดงข้าง `CharCard` และ `MessageUi` ไปแสดงกลางจอ (ที่ถูก: warning กลางจอ, message ช่องว่างด้านขวาของการ์ด) |
| P-2 | **คำสั่งซ้ำซ้อน 2 ระบบ** | `BaseWorkPanel` (ปุ่มงาน + dropdown + Confirm) ทำงานซ้ำกับ flow การส่งสำรวจ (เลือกการ์ด → ยืนยัน) ผู้เล่นต้องเรียนรู้ 2 ภาษา interaction |
| P-3 | **ไม่มีประวัติการกระทำ** | ผลของทริป/งานในฐาน/การแจ้งเตือน เขียนทับกันใน `MessageUi` บรรทัดเดียว ผู้เล่นย้อนดูไม่ได้ |
| P-4 | **ปุ่มหลักจม** | `Dispatch` / `EndDay` ถูกย้ายไปมุมขวาล่างชั่วคราว ขนาดเล็ก ไม่มีที่ใส่ภาพ |
| **P-5** | **Core Item มีชิ้นเดียวในโค้ด + บังคับลำดับดรอป** | ดีไซน์ต้องการ 2 ชิ้น (A = ใช้ escape, B = key) แต่ `RunResources` มีแค่ flag เดี่ยว `hasCoreItemUnlock` + `hasCoreItem` และต้องได้ key ก่อนจึงจะดรอปชิ้นหลักได้ |

---

## 2. ขอบเขต

**อยู่ในขอบเขต**

- Layout ใหม่ทั้งจอ (Canvas) ตามแบบร่าง B
- WarningText / Log / CoreItemGroup / DayFrame / CharacterCard / Dispatch / SkillTriggerPanel
- Interaction model ใหม่: คลิกซ้าย = select/เพิ่มจำนวน, คลิกขวา = ล้างจำนวน
- เปลี่ยน `BaseWorkPanel` → **Skill Trigger** (Cook / Heal / Craft)
- โมเดล Core Item 2 ชิ้น + เงื่อนไขชนะ/แพ้

**ไม่อยู่ในขอบเขต (คงเดิม)**

- ระบบทอยเต๋า `DiceRollView` (ยกเว้นข้อความที่ส่งเข้า log)
- ตรรกะ `GatherResolver`, `GatherDispatchController`, `GatherZoneDatabase`
- ภาพศิลป์/เสียง (ใช้ placeholder ไปก่อน)
- หน้าจอจบเกม `RunEndPanel` (มีอยู่ใน [US-V2-UI-01](../agile/user-stories/US-V2-UI-01.md) แล้ว — สเปกนี้แค่ผูก event)

---

## 3. Layout ใหม่

### 3.1 แผนผังโซน (อ้างอิง Canvas 1920×1080)

```
┌──────────────────────────────────────────────────────────────────────────┐
│ [CoreItemA][CoreItemB]   [Food][Med][CM][Mat] (กลางบน คงเดิม)  [ DAY 3 / 7 ]│
│        (บนซ้าย)                                 (บนขวา, คลิกที่ Day = จบวัน) │
│                                                                          │
│                    ┌────────────────────────────┐                        │
│                    │   ⚠ Warning (กลางจอ)        │                       │
│                    └────────────────────────────┘                        │
│                                                                          │
│            ┌───────────────────────────────────────┐                     │
│            │        MAP AREA (เดิม)                 │   ┌───────────────┐ │
│            │         แผนที่ + ถ้ำเท่านั้น            │   │ Cook      [2] │ │
│            └───────────────────────────────────────┘   │ Heal [MED]    │ │
│  ┌──────────────┐                                      │ Craft     [1] │ │
│  │ LOG   [–]    │      ┌────┐┌────┐┌────┐┌────┐        │               │ │
│  │ ...          │      │card││card││card││card│        │  ┌─────────┐  │ │
│  │ บรรทัดล่าสุด  │      └────┘└────┘└────┘└────┘        │  │DISPATCH │  │ │
│  └──────────────┘        CharacterCard (มือไพ่)         │  └─────────┘  │ │
│   (ล่างซ้าย — ห้ามสูง    (ล่างกลาง)                      │    (ขวา/ล่างขวา) │
│    ล้ำเข้า MAP AREA)                                                     │
└──────────────────────────────────────────────────────────────────────────┘
```

### 3.3 ตัวเลข layout ที่ล็อกไว้ (อ้างอิง Canvas 1920×1080)

| Panel | anchorMin/Max | pivot | anchoredPosition | size |
|---|---|---|---|---|
| `LogPanel` | (0,0) | (0,0) | (24, 24) | 700 × 300 |
| `CharacterHandPanel` | (0.5, 0) | (0.5, 0) | (**232**, **92**) | **936** × 300 |
| `SkillTriggerPanel` | (1, 0) | (1, 0) | (−24, 244) | 236 × 300 |
| `DispatchActionPanel` | (1, 0) | (1, 0) | (−24, 24) | 300 × 200 |
| `Warning` | (0.5, 0.5) | (1, 1) | (300, 60) | **620 × 120** |
| `Warning/WarningBackdrop` | stretch | (0.5, 0.5) | (0, 0) | inset 0 |
| `Warning/WarningText` | stretch | (0.5, 0.5) | (0, 0) | inset 8 / 6 |

- การ์ดในมือ: **218 × 300**, spacing **12** → 4 ใบ = 908 px พอดีกับพื้นที่ 936 px
- เหตุผลที่ hand เยื้อง +190: พื้นที่ว่างจริงคือระหว่าง Log (จบที่ x = 724) กับ Dispatch (เริ่มที่ x = 1596) → จุดกึ่งกลาง = 1148 = 960 + 188, ปัดเป็น +190 ให้เหลือขอบเท่ากันสองข้าง
- ช่องไฟ: Log→การ์ด 24 px · การ์ด→Dispatch 48 px → ไม่มีอะไรทับกัน
- **MAP AREA:** `GatherMapController` กัน node ไว้ด้วย margin L32 / R**300** / T**110** / B**330** และ maxCardHeight **700** → ไม่มีถ้ำไปอยู่ใต้ Log / มือการ์ด / คอลัมน์สกิล / DayFrame และการ์ดใช้พื้นที่แนวตั้งเต็ม (ค่าล่าสุดดู §8.6)
  - 2026-11-03: เพิ่มหลอดหมอก (`fogGaugeWidth 26` + `fogGaugeGap 34`) ทางซ้ายสุด และลด `maxCardWidth` 480 → **430** → การ์ดแคบลง 50 px และเลื่อนขวา ~75 px (ดู §8.18 ข้อ 6)

### 3.4 ป้ายบนปุ่มสลับโหมด Heal

ใช้ข้อความ **`MED`** (ใช้ยา 1 + AP 1) / **`AP`** (ใช้ 2 AP) แทนไอคอนลูกศร เพราะ TMP atlas เริ่มต้นไม่มี glyph `⇄` (ทดสอบแล้วขึ้นเป็นสี่เหลี่ยมว่าง)

### 3.2 ตารางเทียบกับของเดิม

| โซนใหม่ | ของเดิม | สิ่งที่เปลี่ยน |
|---|---|---|
| บนซ้าย `CoreItemGroup` | `Canvas/StatusBar/CoreItemGroup/CoreItem` | **1 ช่อง → 2 ช่อง** (`CoreItemA`, `CoreItemB`) |
| บนกลาง `ResourceStrip` | `Canvas/StatusBar/TopBar` (Food/Medicine/CookingMaterial/Materials) | **คงตำแหน่งเดิม (กลางบน)** ไม่ย้าย |
| บนขวา `DayFrame` | `Canvas/StatusBar/DayFrame` + `Canvas/EndDay button` | รวมปุ่ม End Day เข้า DayFrame → **คลิกที่ Day = จบวัน** |
| กลางจอ `Warning` | `Canvas/WarningText` + `WarningBackdrop` | **รวมเป็น group เดียว** (`Warning` ถือ CanvasGroup + `WarningView`), คงตำแหน่งกลางจอ, **แสดงเฉพาะ Error** |
| ล่างซ้าย `LogMessagePanel` | `Canvas/MapPanel/MessageUi` | **ของใหม่** — log มี history, minimize ได้ |
| ล่างกลาง `CharacterHandPanel` | `Canvas/CharCard` | ย้ายมาล่างกลาง, ทำเป็นมือไพ่ |
| ขวา `SkillTriggerPanel` | `Canvas/BaseWorkPanel` | **Rework** เป็นปุ่มสกิล + queue |
| ล่างขวา `DispatchActionPanel` | `Canvas/MapPanel/Dispatch`, root `TestDispatch` | ขยายใหญ่ขึ้น, มีที่ใส่รูป, **ซ่อนเมื่อยังไม่ใช้งาน** |
| กลาง-บน `MapPanel` | `Canvas/MapPanel` | คงไว้ — เหลือแค่พื้นหลัง + ถ้ำ (ลบ `MessageUi`/`Dispatch`/`ScoutIcon` ออกแล้ว) |

---

## 4. สเปกองค์ประกอบ

### 4.1 `Warning` — เตือนเฉพาะที่ทำไม่ได้

| หัวข้อ | สเปก |
|---|---|
| ตำแหน่ง | กลางจอ (เหนือ map area) |
| แสดงเมื่อ | **เฉพาะการกระทำที่ทำไม่ได้** — บันทึกเป็น `LogKind.Warning` ทั้งใน log และบนแบนเนอร์ |
| ตัวอย่าง | เลือกการ์ดตัวที่ 3 (เกิน limit), AP ไม่พอ, Cooking Material / Materials ไม่พอ, กด Heal ตอนไม่มีคนบาดเจ็บ, ยืนยันโดยยังไม่เลือกผู้ป่วย |
| ข้อความทั่วไป/ข้อมูล | **ไม่ขึ้น warning** → ลง log เท่านั้น |
| พร้อม log | ทุกครั้งที่ขึ้น warning ต้องมี log entry ชนิดเดียวกันด้วย |
| สี | **อ่านจาก `UiPalette.For(kind)` ตัวเดียวกับ log** — ข้อความบนแบนเนอร์จึงเป็นสีเดียวกับบรรทัดใน log เป๊ะ (เดิมข้อความขาวล้วน + outline แยกชุดสีของตัวเอง) |
| Animation | **คงของเดิม** — fade in 0.3s → hold 2.5s → fade out 0.3s, คิวทีละข้อความ |
| โครงสร้าง | group `Warning` (CanvasGroup + `WarningView`) → `WarningBackdrop` (Image, stretch เต็ม) + `WarningText` (TMP, stretch inset 8/6) — ดู §7.16 |
| API | `WarningView.Show(text, LogKind kind)` — kind ตัวเดียวตัดสินทั้งสีและคู่ log (ไม่มี enum ที่สองแล้ว) — ดู §7.17 |

### 4.2 `LogMessagePanel` — ประวัติการกระทำ (ล่างซ้าย)

| หัวข้อ | สเปก |
|---|---|
| ตำแหน่ง | **มุมล่างซ้ายเท่านั้น** — ขอบบนของ panel **ต้องไม่สูงล้ำเข้าเขต MAP AREA** |
| ความสูง | ล็อกด้วยพื้นที่ว่างที่เหลือใต้ MAP AREA (ไม่ใช่จำนวนบรรทัดคงที่) |
| ลำดับ | **ใหม่สุดอยู่บรรทัดล่างสุด** แล้วดันของเก่าขึ้น (chat style), auto-scroll ลงล่าง |
| ความจุ (memory) | เก็บ **16 entry ล่าสุด** (`RunLog.Capacity` = 16) |
| แสดงผล | **แสดงเท่าที่ช่องใส่ได้จริง** — ถ้าพื้นที่ใส่ได้ 6 บรรทัด ก็แสดง 6 บรรทัดล่าสุด ที่เหลือเลื่อนดูด้วย scroll |
| Minimize | ปุ่มย่อ → เหลือ **บรรทัดล่าสุดบรรทัดเดียว**; กางกลับได้ |
| เนื้อหา | ทุก action ของผู้เล่น + ผลลัพธ์ + warning + เหตุการณ์วันเปลี่ยน |
| รูปแบบบรรทัด | `[วันที่] [ไอคอน] ข้อความ` — สีตามชนิด (ปกติ/สำเร็จ/ล้มเหลว/เตือน) |
| ชนิด entry | `LogKind { Action, Success, Fail, Warning, DayEnd, System }` |

**แหล่งข้อมูลที่ต้องต่อเข้า log**

| แหล่ง | event/จุดเรียก | สถานะปัจจุบัน |
|---|---|---|
| งานในฐาน (Cook/Heal/Craft) | `BaseWorkController.OnBaseWorkCompleted` (contract C1) | ✅ ต่อแล้ว (P2) |
| ทริปสำรวจ | `GatherDispatchController.OnGatherResolved` | ✅ ต่อแล้ว — loot/tier/hurt/missing/Core Item |
| ข้อมูลถ้ำที่เลือก | `GatherMapController.OnSelectionChanged` | ✅ ต่อแล้ว (P5) — AP/unit, success value, loot |
| trait ที่ได้จากทริป | `GatherDispatchPanel` เทียบ traits ก่อน/หลัง | ✅ ต่อแล้ว (P5) |
| Warning | `WarningView.Show*` | ✅ เรียกคู่กับ `RunLog.Add(LogKind.Warning, …)` ทุกจุด |
| จบวัน/กินอาหาร/อดอาหาร | `DayManager.OnDayEnded` (contract C4) | ✅ ต่อแล้ว (P2) |
| Core Item ได้รับ | `RunResources` (C7 `OnChanged` หรือ method ใหม่) | ✅ ผ่าน `GatherDispatchResult.coreItemsObtained` |

### 4.3 `CoreItemGroup` — 2 ช่อง

| ช่อง | ชื่อ field ใหม่ | ความหมาย | ที่มา |
|---|---|---|---|
| ช่องที่ 1 | `CoreItemA` | **ชิ้นที่ใช้ escape** | drop จาก dungeon (อิสระ) |
| ช่องที่ 2 | `CoreItemB` | **key** (ต้องมีครบทั้งคู่จึงใช้ escape ได้) | drop จาก dungeon (อิสระ) |

- **2 สถานะต่อช่อง** (ตัด `Unlocked` ออก เพราะไม่มีการดรอปแบบมีเงื่อนไขแล้ว): `NotObtained` / `Obtained`
- ไฮไลต์เมื่อได้ครบทั้ง 2 (แจ้งเตือนภาวะชนะ)
- **Win = มีครบ A + B**
- **ไม่มีลำดับการดรอป** — A และ B สุ่มแยกกัน อันไหนก่อนก็ได้ (B ไม่ใช่เงื่อนไขให้ A ดรอป)
- **เลย์เอาต์ (รอบ 3):** `CoreItemGroup` เป็นคอลัมน์ **2×1 (ตั้ง)** ขนาด 118×240 วางซ้อนกันแนวตั้ง (ช่อง A y = 0, ช่อง B y = −122) **ไม่มีข้อความ `CoreItemStatus` แล้ว** และ `HudTopBar.RelocateCoreItemGroup` จัดขอบขวาให้ตรงกับ `DayFrame` แล้ววางใต้ลงมาระหว่าง DayFrame กับแถบสกิล

### 4.4 `DayFrame` — รวม End Day

| หัวข้อ | สเปก |
|---|---|
| เนื้อหา | `DAY n / deadline` + จำนวนวันเหลือ (เหมือน `StatusBarView.RefreshDay`) |
| อินพุต | **คลิกที่ frame = `DayManager.EndDay()`** (ไม่มีปุ่ม End Day แยก) — implement ด้วย `EndDayTrigger` (`IPointerClickHandler`) บน `DayFrame` |
| **พื้นผิวที่กดได้** | **ต้องมี Graphic ที่ `raycastTarget = true` อยู่ใต้ `DayFrame`** — ตัวที่ใช้คือ `DayBackground` (ปิด `Day`/border ไม่ให้แย่งคลิก) · `EndDayTrigger.EnsureClickSurface()` เติม transparent hit area ให้เองถ้าไม่มี และเตือนใน console |
| **ห้ามมีถ้ำอยู่ใต้ frame** | `GatherMapController` margin ต้องกัน rect ของ node ออกจากกรอบ — ปัจจุบัน `marginRight = 300`, `marginTop = 110` |
| สถานะปิด | **run จบแล้วเท่านั้น** (ไม่มีสถานะ "ระหว่างทริป" เพราะทริป resolve ทันที) — `EndDayTrigger` เช็ค `DayManager.Outcome != InProgress` แล้วขึ้น warning "The run is already over." + log |
| คำเตือนก่อนจบวัน | **ไม่มี confirm dialog** — คลิกเดียวจบวันทันที (ผลการกิน/AWOL ลง log หลังจบวัน) |

### 4.5 `CharacterHandPanel` — การ์ดตัวละครล่างกลาง

| หัวข้อ | สเปก |
|---|---|
| รูปแบบ | "มือไพ่" เรียงแนวนอนกลางจอ |
| ข้อมูลบนการ์ด | ชื่อ + พอร์เทรต, **AP (pips/ตัวเลข)**, สถานะ, ความหิว, ป้าย "ออกสำรวจแล้ววันนี้" |
| อินพุต | **คลิกซ้าย = เลือก** (ไฮไลต์กรอบ), คลิกซ้ายซ้ำ = ยกเลิก |
| ใช้ร่วม | ทั้ง flow **dispatch** และ **skill trigger** (ภาษา interaction เดียวกัน) |
| จำนวนที่เลือกได้ | dispatch = **2** · skill = **1 ใบ** |
| **เงื่อนไขเลือกได้ขึ้นกับโหมด** | **Dispatch / Skill ใช้กฎเดียวกัน: `CanBeDispatched` (`state == Idle`)** — ต่างกันแค่จำนวนที่เลือกได้ · **unit ที่ "busy" (ออกสำรวจแล้ววันนี้) จึงยังเลือกทำสกิลได้ถ้ามี AP เหลือ** · **HealTarget:** เฉพาะ `UnitState.Hurt` (โหมดที่ 3 เกิดหลังกด Heal) — เหตุผลที่เปลี่ยนจากเดิมดู §7.8 |
| แสดงสถานะ Busy | การ์ดที่ `HasGatheredToday == true` ติดป้าย "ออกสำรวจแล้ววันนี้" (สีต่างจากปกติ) แม้ยังมี AP |
| ปุ่มให้อาหาร | คงไว้ตาม [US-V2-UI-01](../agile/user-stories/US-V2-UI-01.md) U1 (ไม่กระทบสเปกนี้) |
| ของเดิมที่ใช้ต่อ | `UnitRosterView` / `UnitIconView` (การ์ด 195×265 — ปรับขนาดเป็นไพ่) |

### 4.6 `DispatchActionPanel` — ปุ่มยืนยัน (ล่างขวา)

| หัวข้อ | สเปก |
|---|---|
| ขนาด | ใหญ่ขึ้น มีช่องใส่ภาพ/ไอคอน |
| **ซ่อนเมื่อยังไม่ใช้งาน** | ซ่อนเมื่อไม่มีการ์ดถูกเลือกเลย (queue เดี่ยว ๆ ไม่มีอยู่จริง เพราะ counter ผูกกับการ์ดที่เลือก) |
| แสดงเมื่อ | มีการ์ดที่เลือกอย่างน้อย 1 ใบ — โหมดสำรวจ = `DISPATCH`, โหมดสกิล = `COOK` / `CRAFT` / `HEAL` |
| Label | เปลี่ยนตามบริบท: `Dispatch` (โหมดสำรวจ) / `Cook` / `Heal` / `Craft` (โหมดสกิล) |
| ปิดการใช้งาน | เงื่อนไขไม่ครบเท่านั้น (Heal ยังไม่เลือกผู้ป่วย, ไม่มีถ้ำที่เลือก, AP ไม่พอ) — **ไม่มี `TripInProgress` แล้ว** |
| ข้อความ/ไอคอน | ใช้ `ButtonVisual` เดิมสลับข้อความ ↔ sprite |

### 4.7 `SkillTriggerPanel` — แทน `BaseWorkPanel` (คอลัมน์ขวา)

| ปุ่ม | พฤติกรรม |
|---|---|
| **Cook** | คลิกซ้าย = queue +1 รอบ (1 AP); คลิกซ้ำ = เพิ่มจำนวน · คลิกขวา = ยกเลิกทุกอย่าง (gesture เดียวทั้งจอ, §5) |
| **Craft** | เหมือน Cook ทุกข้อ |
| **Heal** | **ไม่มี queue** — คลิกซ้ายครั้งเดียว = เลือกสกิล Heal แล้วรอเลือกผู้ป่วย · **disabled ทั้งปุ่มถ้าไม่มีคนบาดเจ็บเลย** |

| หัวข้อ | สเปก |
|---|---|
| ตำแหน่ง | คอลัมน์ขวา **แสดงตลอด** (ไม่ซ่อนตามบริบท) |
| ตัวเลข queue | **อยู่นอกกรอบปุ่ม Cook / Craft ทางซ้าย** (2026-11-03) — เดิมอยู่ในการ์ดปุ่ม อ่านแล้วเหมือนเป็นส่วนของ label · ปุ่ม Heal ไม่มีตัวเลข (ไม่มีการนับรอบ) |
| สล็อตของตัวเลข | **มีแผ่นสีพื้นหลังของตัวเอง** (`CountSlot`, สีเดียวกับหน้าปุ่ม) วางเป็น sibling ก่อน label (2026-11-03) → อ่านเป็น `[ 2 ] [ COOK + ]` ไม่ใช่เลขลอยบนแผนที่ · ต้องเป็น object แยก ไม่ใช่ `Image` บน object เดียวกับ TMP เพราะ graphic บน GameObject เดียววาดตามลำดับ enable แล้วแผ่นจะทับตัวเลข · ซ่อน/โชว์พร้อมตัวเลขใน `SetCount` |
| เครื่องหมาย `+` | **อยู่ในกรอบปุ่ม ขวาสุด หลัง label** (2026-11-03) → อ่านเป็น `[ 2 ] [ COOK + ]` · เป็น label สี accent ไม่ใช่ปุ่มที่สอง (ทั้งปุ่มเพิ่มรอบอยู่แล้ว) จึงไม่มี hit area ซ้อนให้กดพลาด · ถ้า label จะชน `+` โค้ดจะเลื่อน label ซ้ายให้พอดี (วัดจาก `preferredWidth` ไม่ได้เดา) |
| เพดานจำนวน | **จน AP ของตัวละครที่เลือกหมด** (ไม่ใช้ค่าคงที่รายรอบ) + หยุดเมื่อวัตถุดิบหมด |
| วัตถุดิบไม่พอ | **หยุด queue** (ไม่เพิ่ม) + warning + log |
| Heal — 2 โหมด | ปุ่มเล็กข้างปุ่ม Heal สลับ `ยา 1 + AP 1` ↔ `AP 2` (ปุ่มเดียว ไม่มี queue) |
| Heal — ผู้ป่วย | หลังกด Heal **ต้องเลือกผู้ป่วย 1 คน** จาก `CharacterHandPanel` (**เฉพาะตัวที่ `UnitState.Hurt`**) |
| Heal — จำนวน | **1 ครั้งต่อ 1 การยืนยัน** (ไม่มีรอบ/ไม่มีคิว) |
| ไม่มี state "สกิลที่เลือก" | เก็บแค่ **การ์ดที่เลือก** + counter ของ Cook/Craft (Heal ไม่มี counter) |
| ของเดิมที่นำกลับมาใช้ | ตัวเลข/เงื่อนไขทั้งหมดอ่านจาก `BaseWorkController` (`CookApPerRound`, `GetMaxCookRounds`, `GetHealPlan`, `WorkshopApPerRound`, …) — ห้าม hard-code ใน UI |

> **ต้องแก้ `BaseWorkController` เพิ่ม (P4):** ตอนนี้ `GetHealPlan` / `Heal` เลือกยาให้อัตโนมัติถ้ามียาในคลัง — แต่ UI มี toggle ให้ผู้เล่นเลือกโหมด จึงต้องเพิ่ม overload ที่รับ mode ชัดเจน เช่น
> `GetHealPlan(UnitCharacter healer, bool useMedicine, out int apCost)`, `CanHeal(healer, patient, bool useMedicine)`, `Heal(healer, patient, bool useMedicine)` โดยคง signature เดิมไว้เป็น wrapper

---

## 5. Interaction Model (normalize ทั้งเกม)

| อินพุต | บนการ์ดตัวละคร | บนปุ่ม Cook / Craft | บนปุ่ม Heal | บนปุ่ม Dispatch |
|---|---|---|---|---|
| **คลิกซ้าย** | เลือก / ยกเลิกการเลือก | เพิ่มจำนวน +1 | เลือกโหมด Heal (ครั้งเดียว) | ยืนยันการกระทำ |
| **คลิกซ้ายซ้ำ** | สลับ selected | เพิ่มจำนวนต่อ | — | — |
| **คลิกขวา** | ยกเลิกทุกอย่าง | ยกเลิกทุกอย่าง | ยกเลิกทุกอย่าง | ยกเลิกทุกอย่าง |

> **คลิกขวาที่ไหนก็ได้บนจอ** (2026-11-03) = ยกเลิกทุกอย่างที่ค้าง: สกิลที่ arm, ถ้ำที่เลือก, การ์ดที่ถือ — **ไม่มีข้อยกเว้นรายปุ่ม** · ตัวจัดการเดิม (`RightClickHandler` ที่ปุ่มสกิล) ถูกลบออกเพื่อไม่ให้ gesture เดียวมีสองความหมาย: เหลือ `RightClickCancel` ตัวเดียวบน `GameSystem` ที่อ่าน `Mouse.current` และเรียก `SkillFlowController.CancelActiveSkill()` + `GatherMapController.ClearSelection()` + `UnitRosterView.ClearSelection()` · `SkillFlowController.Disarm()` จึงเหลือทางเดียว (เคลียร์การเลือกเสมอ) ไม่มี variant "เก็บการ์ดไว้" ให้ประสานงานผิดพลาด

---

## 6. Flow

### 6.1 โหมด Cook / Craft (มี queue)

```mermaid
flowchart TD
    A["1. คลิกซ้ายการ์ดตัวละคร<br/>(เลือกได้ 1 ใบ)"] --> B["2. คลิกซ้าย Cook หรือ Craft"]
    B -->|"คลิกซ้ายซ้ำ"| B2["+1 รอบ<br/>(จน AP หมด / วัตถุดิบหมด)"]
    B -->|"คลิกขวา"| B3["ล้างจำนวน<br/>(เก็บการ์ดที่เลือกไว้)"]
    B2 --> E["ปุ่ม Dispatch ปรากฏ<br/>label = Cook / Craft"]
    B3 --> E
    E --> F["ยืนยัน → BaseWorkController<br/>→ OnBaseWorkCompleted → Log"]
```

### 6.2 โหมด Heal (ไม่มี queue)

```mermaid
flowchart TD
    A["1. คลิกซ้ายการ์ดตัวละคร<br/>(ผู้รักษา 1 ใบ)"] --> B["2. คลิกซ้าย Heal"]
    B --> C["3. เลือกการ์ดผู้ป่วย 1 ใบ<br/>(เฉพาะ UnitState.Hurt)"]
    C --> D["ปุ่ม Dispatch ปรากฏ<br/>label = Heal"]
    B -.->|"ปุ่มเล็กข้าง Heal"| T["สลับโหมด<br/>ยา 1 + AP 1 ↔ AP 2"]
    D --> F["ยืนยัน → BaseWorkController.Heal<br/>→ OnBaseWorkCompleted → Log"]
```

> ปุ่ม Heal ไม่มีตัวเลข/ไม่มีการนับรอบ — 1 การยืนยัน = 1 ครั้ง

### 6.3 โหมดสำรวจ (ไม่มีเวลาเดินทาง)

```mermaid
flowchart LR
    A["1. คลิกถ้ำบนแผนที่<br/>(ไม่มีฐานบน map แล้ว)"] --> B["2. เลือกการ์ด 1-2 ใบ<br/>(CanBeSentGathering)"]
    B --> C["3. ปุ่ม Dispatch"]
    C --> D["resolve ทันที:<br/>AP ถูกหัก, ทอย, loot, hurt/missing,<br/>Core Item, trait"]
    D --> E["log ขึ้นทันที<br/>(ผลลัพธ์ + สถานะใหม่)"]
    D --> F["ตัวละคร → Busy<br/>(HasGatheredToday = true)"]
    F --> G["ยังทำสกิลได้ถ้ามี AP เหลือ<br/>(CanBeDispatched == Idle)"]
    G --> H["End Day → รีเซ็ตสถานะ + คืน AP เต็ม"]
```

**กติกาที่ล็อกแล้ว**

| ข้อ | กติกา |
|---|---|
| เวลาทริป | **0** — ไม่มีเดินทางไป-กลับ, ไม่มีอนิเมชันกัน UI |
| ลำดับ | คลิกยืนยัน → คำนวณผลทั้งหมด → เขียน log **ในเฟรมเดียวกัน** |
| สถานะหลัง dispatch | `HasGatheredToday = true` (busy) · `state` กลับเป็น `Idle` ถ้ายังมี AP, `Resting` ถ้า AP หมด |
| ส่งซ้ำวันเดียว | ไม่ได้ (`CanBeSentGathering` false) |
| ใช้สกิลต่อ | **ได้** ถ้า `state == Idle` และ AP พอ |
| End Day | กดได้ทุกเมื่อ ไม่ผูกกับทริป — รีเซ็ต `HasGatheredToday`, คืน AP, ปลด `Resting` |
| `TripInProgress` | **ไม่มีแล้ว** — ลบออกจาก `GatherMapController` และ UI ทั้งหมด |

> **ต้องประสานกับกลุ่ม 2 (contract C3):** `GatherMapController.ConfirmTrip` ต้องเรียก `Dispatch()` แล้วยิง `OnTripResolved` ทันที (ไม่ผ่าน `ScoutWalkerView`) · `ScoutIcon` บน `MapPanel` และ `ScoutWalkerView` ไม่มีบทบาทแล้ว (ลบตอน P5) · ต้องอัปเดต C3 ใน [V2 Task Overview](../agile/user-stories/V2-TASK-OVERVIEW.md)

---

## 7. การเปลี่ยนข้อมูลและตรรกะ

### 7.1 `RunResources` — Core Item 2 ชิ้น

| เดิม | ใหม่ |
|---|---|
| `bool hasCoreItemUnlock` (key) | `bool coreItemB` |
| `bool hasCoreItem` (escape item) | `bool coreItemA` |
| `HasCoreItemUnlock`, `HasCoreItem` | `HasCoreItemA`, `HasCoreItemB`, **`HasBothCoreItems`** |
| `UnlockCoreItemMaterial()` | `ObtainCoreItemB()` |
| `ObtainCoreItem()` (เฉพาะ `coreItemB` ได้แล้ว ไม่มีเงื่อนไขลำดับ) | `ObtainCoreItemA()` (เฉพาะ `coreItemA` ยังไม่มี ไม่มีเงื่อนไขลำดับ) |

> **ไม่มีลำดับการดรอป:** ทั้ง A และ B สุ่มดรอปได้อิสระจากกัน อันไหนก่อนก็ได้ ไม่มี precondition ต่อกัน
> การดรอปย้ายไปเป็น **ตารางข้อมูลในโซน** — ดู §7.2
> `GatherDispatchResult` มี field `unlockedCoreItemMaterial` / `obtainedCoreItem` → เปลี่ยนเป็น **`List<CoreItemKind> coreItemsObtained`** (ทริปเดียวอาจได้ทั้ง 2 ชิ้น) เพื่อให้ log เขียนถูกชิ้น

### 7.2 `GatherZoneConfig` — Core Item chance (แบบ 2)

ย้ายจุดดรอปจาก `if/else` ผูกโซนใน `GatherDispatchController.HandleCoreItemProgress()` มาเป็น **ตัวเลขใน config ของโซน** — ปรับใน Inspector ได้ และทำให้ Hard มีโอกาสมากกว่า Normal ตามที่ต้องการ

> **อ้างอิง:** main loot ของแต่ละโซนคือ `CookingMaterial` (Easy) / `Medicine` (Normal) / `Materials` (Hard) + `hardSuccessBonusPool` — **Core Item เป็นของแยกจาก main loot** ไม่ได้อยู่ใน loot pool

```csharp
public enum CoreItemKind { A, B }

public class GatherZoneConfig
{
    // ...ของเดิม...

    [Range(0, 100)]
    [Tooltip("โอกาส (%) ที่จะเจอ Core Item 1 ชิ้น เมื่อทริปจบด้วย HardSuccess")]
    public int coreItemChancePercent;
}
```

#### ค่าเริ่มต้นที่เสนอ (tunable)

| Zone | `coreItemChancePercent` | หมายเหตุ |
|---|---|---|
| Easy | **0%** | โซนง่าย ไม่ให้ของสำคัญ |
| **Normal** | **5%** | ต้องพึ่งดวง |
| **Hard** | **15%** | **สูงกว่า Normal 3 เท่า** แลกกับ SV ต่ำ + Fumble เสียยูนิต |

> ตัวเลขนี้คือ **"โอกาสเมื่อทอยได้ HardSuccess"** ไม่ใช่โอกาสต่อทริปตรง ๆ — เพราะ Core Item ดรอปเฉพาะ HardSuccess เท่านั้น (ดูตารางถัดไป)

#### อัตราจริงต่อทริป (ส่ง 2 คนถนัดโซน, ยังไม่มี trait)

| Zone | SV | P(HardSuccess) | ยังไม่มีทั้ง A,B | มีอยู่แล้ว 1 ชิ้น | ได้อย่างน้อย 1 ชิ้น/ทริป |
|---|---|---|---|---|---|
| Normal | 90 | 30% | 9.75% | 5% | 30% × 9.75% ≈ **2.9%** |
| Hard | 65 | 21% | 27.75% | 15% | 21% × 27.75% ≈ **5.8%** |

- คอลัมน์ "ยังไม่มีทั้ง A,B" = `1 − (1 − p)²` เพราะ **ทอยแยกอิสระต่อชิ้น**
- คอลัมน์สุดท้ายคือตัวเลขที่ผู้เล่นรู้สึกจริง = **Hard ≈ 5.8% > Normal ≈ 2.9%** (ประมาณ 2 เท่า)
- **(2026-11-02)** `P(HardSuccess)` คำนวณจากกฎใหม่ ⌊SV ÷ 3⌋ (เดิม ÷2) — ตัวเลขในตารางอัปเดตตามแล้ว

#### กติกาการทอย (ตกลงแล้ว)

| ข้อ | กติกา |
|---|---|
| **ทอยเมื่อไหร่** | **เฉพาะ tier `HardSuccess`** — `Regular` / `Failure` / `Fumble` **ไม่ดรอป** |
| จำนวนครั้ง | ทอยแยก **ต่อชิ้น** (A 1 ครั้ง, B 1 ครั้ง) ต่อ 1 ทริป — ไม่ทอยซ้ำถ้าทริปเดียวมีหลายยูนิต |
| ยังไม่มีทั้ง A และ B | **ทอยทั้ง A และ B** |
| มี A แล้ว ไม่มี B | **ทอยเฉพาะ B** (ข้าม A) |
| มี B แล้ว ไม่มี A | **ทอยเฉพาะ A** (ข้าม B) |
| มีครบทั้ง A และ B | **ไม่ทอยเลย** |
| จำนวนที่ได้ | 1 ชิ้นต่อชิ้นต่อทริป (ทริปเดียวได้มากสุด 2 ชิ้น = ครบพอดี) |
| หลังทอย | ถ้าครบทั้ง 2 → `DayManager.CheckImmediateWin()` **ครั้งเดียว** (ไม่ใช่ต่อชิ้น) |
| log | เขียน 1 บรรทัดต่อชิ้นที่ได้ เช่น `ได้รับ Core Item A` |

#### จุดแก้ใน `ApplyOutcome`

```text
เดิม  : case HardSuccess: ... HandleCoreItemProgress(zone, result);
ใหม่  : case HardSuccess: ... TryRollCoreItems(config, result);   // ไม่รับ tier แล้ว เพราะRegular ไม่ดรอป
        (ลบ HandleCoreItemProgress ทั้ง method)
```

```csharp
private void TryRollCoreItems(GatherZoneConfig config, GatherDispatchResult result)
{
    foreach (CoreItemKind kind in new[] { CoreItemKind.A, CoreItemKind.B })
    {
        if (RunResources.Instance.HasCoreItem(kind)) continue;          // ข้ามชิ้นที่มีแล้ว

        if (UnityEngine.Random.Range(0, 100) >= config.coreItemChancePercent) continue;

        RunResources.Instance.ObtainCoreItem(kind);
        result.coreItemsObtained.Add(kind);
    }

    if (result.coreItemsObtained.Count > 0) DayManager.Instance?.CheckImmediateWin();
}
```

### 7.3 `DayManager` — เงื่อนไขชนะ/แพ้

| จุด | เดิม | ใหม่ |
|---|---|---|
| `EndDay()` ก่อนกินอาหาร | `HasCoreItem` → Win | `HasBothCoreItems` → Win |
| หลัง `day++` | `day > deadline && !HasCoreItem` → `LoseDeadline` | `day > deadline && !HasBothCoreItems` → `LoseDeadline` |
| `CheckImmediateWin()` | `HasCoreItem && day <= deadline` | `HasBothCoreItems` |
| `RunOutcome` | คงเดิม (`Win` / `LoseDeadline` / `LoseAllUnitsLost`) | คงเดิม |

### 7.4 Log — คลาสที่ใช้จริง

```text
LogKind   { Action, Success, Fail, Warning, DayEnd, System }
LogEntry  { readonly LogKind Kind; readonly string Text; readonly int Day; }
RunLog    static class
          + event Action<LogEntry> OnEntryAdded
          + event Action OnCleared
          + void Add(LogKind, string)       // stamp Day จาก DayManager.CurrentDay
          + void Clear()
          + IReadOnlyList<LogEntry> Entries
          + int Capacity { get; set; } = 16
          + [RuntimeInitializeOnLoadMethod] ResetStatics()   // ล้างทุกครั้งที่เข้า play mode
LogMessagePanel : MonoBehaviour   // (P2) subscribe RunLog.OnEntryAdded, render, minimize/expand
```

**ทำไมเป็น static:** ทุกกลุ่มต้องเขียน log ได้โดยไม่ต้องมี Inspector reference และไม่ต้องเพิ่ม GameObject ลงใน `Core.unity` (ไฟล์ที่กลุ่ม 2 เป็นเจ้าของ) — static ถูกล้างอัตโนมัติเมื่อเข้า play mode ด้วย `RuntimeInitializeOnLoadMethod`

---

## 7.5 สถานะการลงมือ (Implementation Status)

| Phase | สถานะ | หมายเหตุ |
|---|---|---|
| **P0 — Data & logic** | ✅ **เสร็จ (2026-10-27)** | ดูรายการไฟล์ด้านล่าง |
| **P1 — โครง prefab** | ✅ **เสร็จ** (สร้าง prefab 4/4) | ดู §7.6 |
| **P2 — LogPanel + wiring** | ✅ **เสร็จ (2026-10-27)** | ดู §7.7 |
| **P3 — CharacterHand + Dispatch panel** | ✅ **เสร็จ (2026-10-27)** | ดู §7.8 |
| **P4 — SkillTriggerPanel** | ✅ **เสร็จ (2026-10-27)** | ดู §7.9 |
| **P5 — ลบของเดิม + DayFrame** | ✅ **เสร็จ (2026-10-28)** | ดู §7.10 |

### 7.6 P1 — Prefab ที่สร้างแล้ว

สร้างด้วย `AgentScripts/BuildUIPrefabs.cs` (เรียกผ่าน `unity command script.run`, ไม่แตะ `Core.unity`)

| Prefab | Root component | ลูกที่สำคัญ |
|---|---|---|
| `Assets/V2/Prefabs/UI/LogPanel.prefab` | `LogMessagePanel` | `Header` (Title + MinMaxButton), `Expanded` (ScrollRect → Viewport → Content → EntryTemplate), `Collapsed` (บรรทัดเดียว) |
| `Assets/V2/Prefabs/UI/CharacterHandPanel.prefab` | `CharacterHandPanel` | `Cards` (HorizontalLayoutGroup) + `CardPlaceholder` |
| `Assets/V2/Prefabs/UI/DispatchActionPanel.prefab` | `DispatchActionPanel` | `Icon` (placeholder 120×120), `Label` (TMP "DISPATCH"), Button บน root |
| `Assets/V2/Prefabs/UI/SkillTriggerPanel.prefab` | `SkillTriggerPanel` | `CookButton` (+Count), `HealRow` (`HealButton` + `HealModeButton` MED/AP), `CraftRow` (`CraftButton` + `CraftModeButton` MED/BUFF) |

**ตรวจด้วยภาพแล้ว** (render ผ่าน Canvas ชั่วคราว 1920×1080) — พบและแก้ 2 ปัญหา:
1. การ์ด 4 ใบกว้าง 1040 ทับ `LogPanel` → ลด hand เป็น 800 px, การ์ด 190 px, เยื้อง +188
2. ปุ่มสลับโหมดใช้ glyph `⇄` ไม่มีใน TMP atlas → เปลี่ยนเป็นข้อความ `MED` / `AP`

> scene ชั่วคราว `UIPreview.unity` ถูกลบแล้ว · `Core.unity` ไม่ถูกแก้ (`isDirty: false`)

### 7.7 P2 — Log wiring (เสร็จ)

| ไฟล์ | สิ่งที่ทำ |
|---|---|
| `Scripts/UI/LogMessagePanel.cs` | render จาก `RunLog` ด้วย **row pool** (clone template ไม่เกิน `capacity` แล้ว reuse), ใหม่สุดล่าง, auto-scroll, ย่อ/กางพร้อมเปลี่ยนความสูง panel (`expandedHeight` 300 → `collapsedHeight` 64) |
| `Scripts/UI/RunLogNarrator.cs` **(ใหม่)** | แปลง event เกมเป็น log: `OnBaseWorkCompleted`, `OnDayEnded`, `OnGatherResolved`, `OnRunEnded` — เลือก `LogKind` ให้เอง (blocked/อดอาหาร = `Warning`, ผลลัพธ์ = `Success`/`Fail`) |
| `Scripts/Controllers/BaseWorkController.cs` | เพิ่ม `OnBaseWorkCompleted` (ยิงทุก path ผ่าน `Emit()`), เพิ่ม overload `GetHealPlan(healer, useMedicine, out apCost)` / `CanHeal(..., bool? useMedicine)` / `Heal(..., bool? useMedicine)` สำหรับ toggle โหมด Heal |
| `Scripts/Controllers/DayManager.cs` | เพิ่ม `DayEndReport` (dayBefore/dayAfter/fed/starved/lostSignal), `PreviewFeeding()` (dry-run) และ `OnDayEnded` |

**ตรวจสอบแล้ว (play mode, Core scene)**

| การทดสอบ | ผล |
|---|---|
| log 9 entry (ผสมทุก kind) | แสดงครบ, สีถูก, ใหม่สุดล่าง |
| 20 entry (เกิน capacity) | `RunLog` เก็บ 16, panel มี 16 row active + template, scroll ไปล่างสุด (`verticalNormalizedPosition = 0`) |
| กดย่อ | `Expanded=false`, `Collapsed=true`, panel height **300 → 64**, บรรทัดล่าสุดอัปเดตสด |
| `Cook(unit, 2)` | narrator เขียน 1 บรรทัด: `Ann cooked 2x — +2 Food, -2 Cooking Material.` |
| `DayManager.EndDay()` | narrator เขียน `Day 2 — everyone ate (4 fed).` |
| `PreviewFeeding()` | คืน fed/starved/lostSignal ถูกต้องโดยไม่แก้ state |
| Console | 0 error / 0 warning หลังล้าง (ตัด `CS0414` ของ skeleton ด้วย `#pragma warning disable 0414`) |

**ข้อควรรู้:** `RunLog` เป็น static → ระหว่าง play mode การเรียก `RunLog.Clear()` ในเทสต์จะล้างของจริง (ใช้เฉพาะตอน debug)

### 7.8 P3 — CharacterHandPanel + DispatchActionPanel (เสร็จ)

| ไฟล์ | สิ่งที่ทำ |
|---|---|
| `Scripts/UI/CharacterHandPanel.cs` | `SelectionMode { Dispatch, Skill, HealTarget }` · `MaxSelection` ตามโหมด (2 / 1 / 1) · `CanSelect` · `CardSize` / `CardSpacing` · `OnModeChanged` |
| `Scripts/UI/DispatchActionPanel.cs` | ปุ่มยืนยันตัวเดียว: `Show(label)` / `ShowDispatch()` / `SetInteractable()` / `Hide()` / `Clicked` — ซ่อนตัวเองใน `Awake` |
| `Scripts/UI/UnitRosterView.cs` | ต่อกับ hand (`handPanel`), `CanPick` ตามโหมด, single-select replacement, `TrimSelectionToMax()`, `SelectedUnit`, และ **คำนวณ selectability ใหม่ทุกเฟรม** ใน `Update()` |
| `Scripts/UI/UnitIconView.cs` | `Refresh()` ไม่เขียนทับ `selectable` ที่คนอื่นตั้งไว้ + ป้าย `Out today` |
| `Scripts/Controllers/GatherMapController.cs` | `ConfirmTrip` resolve ทันทีและยิง `OnTripResolved` ในเฟรมเดียว · `TripInProgress` = `[Obsolete]` และคืน `false` เสมอ · `PlayScoutFlourish` เป็นของประดับเท่านั้น |
| `Scripts/UI/GatherDispatchPanel.cs` | ใช้ `DispatchActionPanel`, ไม่ล็อกระหว่างทริป, `interactable` มาจาก `CanDispatch()` dry-run + บอกเหตุผลใน `MessageUi` |
| `Scenes/Core.unity` | เพิ่ม `CharacterHandPanel` / `DispatchActionPanel` / `LogPanel`, ปิด `CharCard` และ `MapPanel/Dispatch`, ต่อ `RunLogNarrator` ที่ `GameSystem` |

**การตัดสินใจที่ล็อกเพิ่ม (P3)**

| ข้อ | กติกา | เหตุผล |
|---|---|---|
| กติกาเลือกการ์ด | **`Dispatch` และ `Skill` ใช้กฎเดียวกัน: `unit.CanBeDispatched` (Idle)** — ต่างกันแค่ `MaxSelection` | ถ้าใช้ `CanBeSentGathering` การ์ดของคนที่ออกสำรวจแล้ววันนี้จะกดไม่ได้ ทำให้ "เลือกตัวละคร → กดสกิล" ของคนที่ busy ทำไม่ได้เลย (สเปก §4.5 เดิมขัดกับ §6.3) |
| ความถูกต้องของการกระทำ | ตัดสินที่ **ปุ่มยืนยัน** ไม่ใช่ที่การ์ด: `CanDispatch()` / `CanCook()` / `CanHeal()` เป็นคนบอก แล้วปุ่มจะเทา + `MessageUi`/`WarningText` บอกเหตุผล | ผู้เล่นเลือก "ใคร" ก่อน แล้วระบบบอกว่าทำอะไรได้ — ไม่ต้องเดาโหมด |
| ปุ่มยืนยันโผล่เมื่อไร | **เมื่อมีการ์ดถูกเลือกอย่างน้อย 1 ใบ** (ไม่มีถ้ำ → ปุ่มเทา + "Pick a cave on the map first.") | ตรงกับ §4.6 "ซ่อนเมื่อยังไม่ใช้งาน" และไม่เหลือปุ่มตายกลางจอ |
| `HealTarget` | โหมดที่ 3 ใช้เฉพาะตอนกด Heal แล้ว — การ์ดที่เลือกได้เหลือแค่ `UnitState.Hurt` | ผู้รักษาถูกจำไว้ใน controller ทำให้ช่องเลือก 1 ใบยกให้ผู้ป่วยได้ |

### 7.9 P4 — SkillTriggerPanel + SkillFlowController (เสร็จ)

| ไฟล์ | สิ่งที่ทำ |
|---|---|
| `Scripts/UI/SkillTriggerPanel.cs` | `SkillKind { None, Cook, Heal, Craft }` · event `CookClicked/CraftClicked/HealClicked/HealModeToggled/CraftModeToggled` (คลิกขวาไม่อยู่ใน panel แล้ว — ดู §8.18) · `SetCookCount()` / `SetCraftCount()` / `SetArmed()` (ไฮไลต์ปุ่มที่ active) / `SetHealAvailable()` / `SetHealMode()` / `SetCraftMode()` — ปุ่มโหมดของทั้ง Heal และ Craft ผูกมาจากพรีแฟบ (`HealRow` / `CraftRow`) ไม่สร้างตอน `Awake` · แถว `[Count] [Button +]` ประกอบใน `BuildCounters()` |
| ~~`Scripts/UI/RightClickHandler.cs`~~ **(ลบออก 2026-11-03)** | เดิมยิง event เฉพาะคลิกขวาบนปุ่มสกิล — ถูกแทนด้วย `RightClickCancel` ที่ทำงานทั้งจอ (§4.7 / §5 / §8.18) · event `CookCleared` / `CraftCleared` / `HealCancelled` ถูกลบตาม |
| `Scripts/UI/SkillFlowController.cs` **(ใหม่)** | เจ้าของ flow ทั้งหมด: counter ของ Cook/Craft, โหมด Heal, การเลือกผู้ป่วย, `IsSkillActive` + `StateChanged` ให้ gather flow รู้ว่าใครถือปุ่มอยู่ |
| `Scripts/UI/GatherDispatchPanel.cs` | gate ด้วย `IsSkillActive()` ทั้งตอนกดและตอน `RefreshActionPanel()` + กลับมาใช้ปุ่มเองเมื่อ `StateChanged` |

**กติกาที่ลงมือตามสเปก §4.7 / §6.1 / §6.2**

| ข้อ | ผลลัพธ์ |
|---|---|
| Cook / Craft | คลิกซ้าย +1 รอบ (เพดาน = `GetMaxCookRounds` / `GetMaxWorkshopRounds` = AP และวัตถุดิบที่จ่ายได้จริง) · คลิกขวาล้างจำนวน (การ์ดยังเลือกอยู่) · เปลี่ยนสกิลกลางทาง = counter ของอีกอันถูกล้าง |
| เกินเพดาน | ไม่เพิ่ม + `WarningText` + `Debug.LogWarning` ("No AP or cooking material left for another round.") |
| Heal | คลิกซ้าย = arm (ไม่มี counter) · **ต้องเลือกผู้ป่วยก่อนปุ่ม HEAL จึงโผล่** · คลิกขวา = ยกเลิกโหมด Heal |
| Heal disabled | เทาปุ่มทั้งปุ่มเมื่อไม่มีใคร `UnitState.Hurt` |
| Heal 2 โหมด | ปุ่มเล็ก `MED` (ยา 1 + AP 1) ↔ `AP` (AP 2) · ไม่มี medication ในคลัง = บังคับ `AP` และปุ่ม toggle ตาย |
| ตัวเลขบนปุ่ม | แสดงเฉพาะ Cook / Craft (Heal ไม่มี) |

**ตรวจสอบแล้ว (play mode, Core scene)**

| การทดสอบ | ผล |
|---|---|
| เริ่มเกม | ไม่มีสกิลถูก arm, ปุ่มยืนยันซ่อน, hand = `Dispatch` (max 2) |
| เลือกการ์ด → Cook ×2 | arm = Cook, counter 1 → 2, hand หดเหลือ 1 ใบ, ปุ่มขึ้น `COOK` |
| คลิกขวา Cook | counter = 0, arm = None, **การ์ดยังเลือกอยู่** |
| ยืนยัน Cook | AP ถูกหัก, Food เพิ่ม, log มีบรรทัด cook, disarm |
| สลับไป Craft | counter ของ Cook ถูกล้าง, Craft เริ่มที่ 1, ยืนยันแล้ว Materials ลด |
| Heal (มี Hurt 1 คน) | arm = Heal, `healer` ถูกจำไว้, hand เปลี่ยนเป็น `HealTarget`, ปุ่มยืนยันซ่อนจนเลือกผู้ป่วย |
| เลือกผู้ป่วย | เฉพาะคน Hurt กดได้, ปุ่มขึ้น `HEAL`, ยืนยันแล้วหาย Hurt + Medicine ลด 1 |
| Heal โหมด `AP` | ไม่แตะ Medicine, AP ลด 2, ผู้ป่วยหาย |
| ไม่มีใคร Hurt | ปุ่ม Heal เทา |

**ตรวจ regression การสำรวจ**

| การทดสอบ | ผล |
|---|---|
| ไม่เลือกอะไร | ปุ่มซ่อน |
| เลือกถ้ำอย่างเดียว | ปุ่มยังซ่อน |
| เลือกการ์ด 1 ใบ | ปุ่มขึ้น `DISPATCH` |
| arm Cook | label เปลี่ยนเป็น `COOK` (gather flow ไม่แย่ง) |
| คลิกขวาล้าง Cook | label กลับเป็น `DISPATCH` |
| ยืนยันสำรวจ | resolve ทันที: `HasGatheredToday = true`, AP ลด, selection ถูกล้าง, ปุ่มซ่อน, log มีบรรทัดทริป |
| คนที่ busy | ยังกดการ์ดได้ (ทำสกิลต่อได้) |

> **ข้อควรรู้:** `script.run` ของ pipeline compile ทีละไฟล์ → helper ที่อยู่ในไฟล์อื่น (เช่น `ViewLayout`) เรียกจากสคริปต์อื่นไม่ได้<br/>
> **ข้อควรรู้:** `FindAnyObjectByType<T>()` ไม่คืน object ที่ inactive → ต้องระวังตอน `CharCard` (roster เก่า) ยังอยู่ในซีน

### 7.10 P5 — ลบของเดิม + DayFrame = End Day (เสร็จ)

| สิ่งที่ทำ | รายละเอียด |
|---|---|
| **`DayFrame` = ปุ่มจบวัน** | เพิ่ม `Scripts/UI/EndDayTrigger.cs` (`IPointerClickHandler`) บน `StatusBar/DayFrame` — คลิกซ้าย = `DayManager.EndDay()` ไม่มี confirm dialog, ไม่ทำงานซ้ำถ้า run จบแล้ว · คลิกที่ `DayBackground`/ตัวเลข DAY ถูก bubble ขึ้นมาที่ `DayFrame` เอง (พฤติกรรมเดียวกับ EventSystem จริง) |
| **ลบ `EndDay button`** | ออกจากซีน (แทนที่ด้วย DayFrame) |
| **ลบ `BaseWorkPanel`** | ออกจากซีน + ลบไฟล์ `Scripts/UI/BaseWorkPanel.cs` (ทับด้วย `SkillTriggerPanel` + `SkillFlowController`) |
| **ลบ `CharCard`** | roster เก่าที่ปิดไว้ — เหลือ `CharacterHandPanel` ตัวเดียว (กำจัดปัญหา `FindAnyObjectByType<UnitRosterView>` คืนตัวผิด) |
| **ลบ `MapPanel/MessageUi`** | ข้อความย้ายไป log: เลือกถ้ำ → `LogKind.Action` บอก AP/unit, success value, loot · ข้อความที่บล็อก → `WarningText` + `LogKind.Warning` (ลบ field `messageText`/`dispatchButton` ออกจาก `GatherDispatchPanel`) |
| **ลบ `MapPanel/Dispatch`** | ปุ่มเดิม (inactive) — แทนที่ด้วย `DispatchActionPanel` |
| **ลบ `MapPanel/ScoutIcon`** | ของประดับจากยุคที่มีเวลาเดินทาง · **`ScoutIcon.prefab` ยังอยู่** (การ์ดในมือใช้ prefab นี้) · ลบ `ScoutWalkerView.cs`, ถอด field `scoutWalker`/`PlayScoutFlourish` และ property `TripInProgress` ออกจาก `GatherMapController` |
| **Core Item 2 ช่อง** | โคลน `CoreItemFrame` → `CoreItemFrameB` (icon ภายในชื่อ `CoreItemBIcon`) แล้วผูกเข้า `StatusBarView.coreItemBIcon` · เปลี่ยนชื่อ text สรุปเป็น `CoreItemStatus` |
| **Missing script** | ถอด component ที่ script ถูกลบออกจาก `ScoutIcon.prefab` (เดิมเตือน console ทุกครั้งที่ instantiate) |
| **Glyph ที่ฟอนต์ไม่มี** | `✓` (U+2713) ไม่มีใน LiberationSans SDF → เปลี่ยน `CoreItemStatus` เป็น ASCII `Core A +` / `Core B -` |
| **สี idle ของปุ่มสกิล** | `SkillTriggerPanel` ดึง `normalColor` จาก Cook แล้วใช้กับทั้ง Heal และ Craft เพื่อให้คอลัมน์ขวาเป็นชุดเดียวกัน |
| **Map area** | ปรับ `marginLeft/Right/Top/Bottom` ของ `GatherMapController` (150/360/150/420) เพื่อไม่ให้ถ้ำไปอยู่ใต้ LogPanel / มือการ์ด / คอลัมน์สกิล / DayFrame (เดิมมีถ้ำที่มองไม่เห็นและกดไม่ได้) |
| **LogPanel ทึบ** | `LogPanel` background alpha 0.94 → **1** (เดิมถ้ำที่อยู่ข้างหลังทะลุขึ้นมาเห็นผ่านหัว panel) แก้ทั้งในซีนและ prefab |

**ตรวจสอบแล้ว (play mode, Core scene)**

| ชุดทดสอบ | ผล |
|---|---|
| `AgentScripts/TestUiRedesign.cs` — รวมทุก phase ไว้ในไฟล์เดียว (32 ข้อ) | **32/32 ผ่าน** (รันซ้ำ 2 รอบได้ผลเท่ากัน) — โครงสร้าง, clickability, skill flow, dispatch, end day |
| Console (play mode) | **0 error / 0 warning** ที่ไม่ใช่ของ Unity Account API |

> **หมายเหตุ:** `script.run` ที่เรียก `EditorSceneManager.SaveScene` ใช้ได้เฉพาะตอน **ไม่** อยู่ใน play mode · `unity` CLI จะ timeout ทันทีหลัง domain reload — ต้อง `unity status --until-ready` ก่อนเรียกซ้ำ

### 7.11 P5 ตามด้วยการแก้ bug "กด DayFrame ไม่ได้" (2026-10-28)

**อาการ:** คลิกที่กรอบ DAY แล้วไม่มีอะไรเกิดขึ้น

**สาเหตุ (2 ชั้น):**

| # | ปัญหา | หลักฐาน |
|---|---|---|
| 1 | **ทุก Graphic ใต้ `DayFrame` มี `raycastTarget = false`** (`DayBackground`, `Day`, border ทั้ง 4) → raycast ทะลุลงไปโดน `MapPanel` ที่อยู่ข้างหลัง แล้วไม่มี handler ให้เรียก | raycast ที่กลาง frame ได้ผลลัพธ์เดียวคือ `MapPanel`, `GetEventHandler<IPointerClickHandler>` คืน `NONE` |
| 2 | **ถ้ำ 1 ใบอยู่ใต้ frame** (Normal ที่ x≈1535, y≈904 — rect 160×30 คาบเกี่ยวกับขอบซ้ายของ frame) → ถ้าเปิด raycast เฉย ๆ ถ้ำใบนั้นจะเสียพื้นที่คลิก | `overlap = True` จาก `DiagnoseNodeOverlap` |

**วิธีแก้:**

| ไฟล์/ที่ | สิ่งที่ทำ |
|---|---|
| `Scenes/Core.unity` → `StatusBar/DayFrame/DayBackground` | `raycastTarget = true` (พื้นผิวคลิกคือพื้นหลังของกรอบ) |
| `StatusBar/DayFrame/Day` (TMP) | `raycastTarget = false` (ไม่แย่งคลิกจากพื้นหลัง) |
| `Scripts/UI/EndDayTrigger.cs` | เพิ่ม `EnsureClickSurface()` — ถ้าไม่มี raycast target ใน subtree จะเพิ่ม transparent `Image` ให้เอง + `Debug.LogWarning` (กัน bug นี้กลับมาแบบเงียบ ๆ) และเพิ่ม warning + log "The run is already over." เมื่อกดหลัง run จบ |
| `GatherMapController` | `marginRight` 360 → **400**, `marginTop` 150 → **300** → rect ของถ้ำไม่ทับกรอบ DAY อีก |
| `AgentScripts/TestClickTargets.cs` (ภายหลังถูกรวมเข้า `TestUiRedesign.cs` แล้ว) | audit ทุก interactive element บน Canvas ด้วย **raycast จริง** (ไม่ใช่เรียก handler ตรง ๆ) |

**ตรวจสอบแล้ว (play mode)**

| การทดสอบ | ผล |
|---|---|
| `TestUiRedesign` §clickability (16 elements) | **ALL PASS** — DayFrame, การ์ด 4 ใบ, ปุ่ม Cook/Heal/HealMode/Craft, MinMax, ถ้ำ 6 ใบ |
| `TestUiRedesign` §end day | raycast ที่กลาง frame → hit `DayFrame` → handler = `EndDayTrigger` → คลิกแล้ว day เดินหน้า + log |
| Console | 0 error / 0 warning |

> **บทเรียน:** การทดสอบที่เรียก `button.onClick.Invoke()` หรือ `ExecuteEvents.ExecuteHierarchy()` ตรง ๆ จะ **ไม่** จับ bug นี้ เพราะข้าม raycast ไปเลย — ต้อง `EventSystem.RaycastAll` ก่อนเสมอถ้าอยากรู้ว่าผู้เล่นกดได้จริงหรือไม่

### 7.12 รวมชุดทดสอบเหลือไฟล์เดียว (2026-10-28)

เดิมต้องเข้า play mode 4 รอบ (`TestCleanup`, `TestSkillFlow`, `TestDispatchGating`, `TestClickTargets`) ซึ่งกินเวลาและทรัพยากรเพราะการสั่ง play Unity ช้า — รวมเป็น **`AgentScripts/TestUiRedesign.cs`** ไฟล์เดียว รันครั้งเดียวจบ **35 ข้อ** (เดิม 32 ข้อ; เพิ่ม 3 ข้อใน §7.13)

| ส่วน | ครอบอะไร | จำนวนข้อ |
|---|---|---|
| `structure` | ของเก่าถูกลบครบ, 2 ช่อง Core Item ไม่ทับกัน, `coreItemBIcon` ผูกแล้ว + เปลี่ยนสีเมื่อได้ B, `DayFrame` มี `EndDayTrigger` | 5 |
| `clickability` | ทุก interactive element (16 ชิ้น) ผ่าน `EventSystem.RaycastAll` จริง + ไม่มีถ้ำใต้กรอบ DAY + raycast ที่ DayFrame resolve ถึง `EndDayTrigger` | 3 |
| `skill flow` | Heal เทาเมื่อไม่มีคนเจ็บ, Cook queue 1→2, คลิกขวาล้าง (การ์ดยังเลือก), ยืนยันแล้วหัก AP/ได้อาหาร, สลับไป Craft ล้าง counter, Heal MED, Heal AP, **ไฮไลต์ผู้ป่วย 3 ข้อ (§7.13)** | 17 |
| `dispatch` | ปุ่มซ่อน/โผล่, สกิลแย่งปุ่มแล้วคืน, ทริป resolve ทันที, คนที่ busy ยังกดได้ | 8 |
| `end day` | คลิก DayFrame → day เดินหน้า + log | 2 |

**หลักการที่ทำให้รันซ้ำได้ใน play session เดียว**

| ปัญหา | วิธีจัดการ |
|---|---|
| บาดแผล/Lost Signal คงอยู่ข้ามวันโดยดีไซน์ → รอบสองเริ่มมาแบบเจ็บ | `ResetUnits()` ตั้ง `state = Idle` + `RefreshForNewDay()` ตรง ๆ (แยกจาก `Revive()` ที่ไม่ล้าง `HasGatheredToday`) |
| ใช้ AP หมดแล้วยูนิตเป็น `Resting` → การ์ดกดไม่ได้ | `Revive(unit)` ก่อนใช้ยูนิตซ้ำ |
| `Cards[0]` ไม่การันตีว่ากดได้ | `FirstSelectable(roster)` |
| UI รีเฟรชใน `Update()` แต่เทสต์รันในเฟรมเดียว | `ForceSkillPanelRefresh()` เรียก `RefreshPanel` ผ่าน reflection ก่อนอ่านสถานะปุ่ม |

**ลบสคริปต์ one-shot ที่ทำเสร็จแล้ว** (กันการรันซ้ำไปทับค่าที่แก้ไว้): `CleanupLegacyUI`, `PolishLayout`, `PolishMapBounds`, `FixMissingScripts`, `FixDayFrameClick`, `ViewLayout`, `TestCleanup`, `TestClickTargets`, `TestDispatchGating`, `TestSkillFlow` — เหลือ `BuildUIPrefabs` (สร้าง prefab ซ้ำได้), `WireSkillPanel` (ต่อ ref ในซีน), `DemoState` (จัดสถานะถ่ายภาพ) และ `TestUiRedesign`

> **หมายเหตุ:** `ProjectSettings/Packages/com.unity.pipeline/` (config ของ pipeline package ที่เก็บ port/watchdog) ถูกเพิ่มใน `.gitignore` เพราะเป็นค่าของแต่ละเครื่อง ไม่ควร commit

**ไฟล์ที่แก้ใน P0 (compile ผ่าน, รัน play mode ไม่มี error)**

| ไฟล์ | สิ่งที่ทำ |
|---|---|
| `Scripts/CoreItemKind.cs` **(ใหม่)** | enum `CoreItemKind { A, B }` |
| `Scripts/System/RunLog.cs` **(ใหม่)** | `LogKind` / `LogEntry` / `RunLog` (static, capacity 16) |
| `Scripts/System/RunResources.cs` | `coreItemA` / `coreItemB` + `HasCoreItemA/B` / `HasBothCoreItems` / `HasCoreItem(kind)` / `ObtainCoreItem(kind)` + `event OnChanged` |
| `Scripts/Entities/GatherZoneData.cs` | `coreItemChancePercent` (0–100) |
| `Scripts/System/GatherZoneDatabase.cs` | ค่าเริ่มต้น Easy 0 · Normal 5 · Hard 15 |
| `Scripts/Controllers/GatherDispatchController.cs` | ลบ `HandleCoreItemProgress` → `TryRollCoreItems` (เฉพาะ HardSuccess); `coreItemsObtained : List<CoreItemKind>` |
| `Scripts/Controllers/DayManager.cs` | ชนะ/แพ้จาก `HasBothCoreItems` (3 จุด) |
| `Scripts/UI/StatusBarView.cs` | ช่อง A/B (2 ช่อง) + สีตามสถานะ — *โครงชั่วคราวก่อนทำ P1* |
| `Scripts/UI/GatherDispatchPanel.cs` | แสดง Core Item ที่ได้จาก `coreItemsObtained` |

**ตรวจสอบแล้วใน Editor (Unity 6000.5.4f1)**

| การทดสอบ | ผล |
|---|---|
| `script.recompile` | `completed`, `failed: false`, 0 error |
| `GatherZoneDatabase` ค่าจริง | `Easy=0 Normal=5 Hard=15` |
| `ObtainCoreItem(A)` → `HasBothCoreItems` | `false` (ถูกต้อง) |
| `ObtainCoreItem(B)` → `HasBothCoreItems` | `true` (ถูกต้อง) |
| `RunLog.Add` → `Entries.Count` | `1`, stamp day ตรงกับ `DayManager` |
| Console ระหว่าง play mode | ไม่มี error (มีแค่ warning ของ Unity Account API) |

### 7.13 ไฮไลต์คนที่รักษาได้ (2026-10-28)

**ปัญหา:** ตอนกด Heal แล้วต้องเลือกคนไข้ การ์ดของคนที่เจ็บ (ซึ่งเป็นคนเดียวที่เลือกได้) กลับถูก `CanvasGroup.alpha` ลดลงเหลือ 0.55 เพราะ `Refresh()` ลดความสว่างให้ทุกคนที่ `state != Idle` — คนที่ต้องเลือกจึงดู "กดไม่ได้" มากกว่าคนอื่น

**ดีไซน์:** เพิ่มสถานะที่ 3 นอกจาก *selectable* และ *selected* คือ **targetable** = การ์ดที่ถูกต้องของ action ที่กำลังเล็งอยู่

| สถานะ | กรอบ | ความหนา | alpha | หมายเหตุ |
|---|---|---|---|---|
| ปกติ | สี `accent` ของตัวละคร | 4 | 0.6 (0.2 ถ้ากดไม่ได้) | |
| เลือกแล้ว | `accent` | 7 | 1.0 | + `selectedScale` 1.06 |
| **targetable** | **มินต์ `(0.55, 1, 0.75)`** | **7** | **1.0 · เต้น 0.55↔1.0** | สีไม่อยู่ในชุด `accent` ของทั้ง 4 ตัว |

- `UnitIconView.SetTargetable(bool)` เป็นทางเดียวที่ตั้งค่า; `ApplyFrame()` จัดการความหนา, `Update()` เฉพาะการ์ดที่ targetable เท่านั้นที่คำนวณพัลส์ (sin ที่ `targetablePulseSpeed` 3.2) — การ์ดปกติไม่มีค่าใช้จ่าย
- `SetSelectionEnabled(false)` ล้าง flag ทิ้ง เพื่อไม่ให้กรอบเต้นค้างตอนปิดการเลือกชั่วคราว
- `UnitRosterView` เป็นคนตัดสิน: `IsHealTargeting()` (`HandPanel.Mode == HealTarget`) && `CanPick(unit)` — เรียกทั้งใน `Update()` และ `RefreshAvailability()`
- `AlphaForState()` แยกออกมาจาก `Refresh()` เพื่อให้ `targetable` override alpha ได้ในที่เดียว

**ผลลัพธ์:** กด Heal → คนเจ็บกรอบมินต์หนา เต้นอยู่ที่ alpha เต็ม ส่วนคนที่รักษาไม่ได้จาง 0.5 → ตาเห็นทันทีว่าเลือกใครได้

**ยืนยันแล้ว:** `DemoTargets` จัดสถานะ (Boon เจ็บ, Ann เป็นหมอ) แล้วรายงาน `mode=HealTarget targetable=1/4` — ตรงกับภาพหน้าจอ (การ์ด Boon กรอบมินต์, อีก 3 ใบจาง)

### 7.14 ฟอนต์ pixel จาก tileset (2026-10-28)

`Assets/Day/Sprites/tilemap_packed.png` (288×176) เป็นชีตไทล์กริด **16×16 ไม่มี gutter, 18 คอลัมน์ × 11 แถว** ตรวจแล้วพบว่ามีตัวอักษรอยู่ 5 แถว:

| แถว (y) | คอลัมน์ 0–12 | สไตล์ |
|---|---|---|
| 6 (96–111) | `A`–`M` | มีเงา lavender ด้านล่าง |
| 7 (112–127) | `N`–`Z` | มีเงา lavender |
| 8 (128–143) | `%` `+` `-` `0`–`9` | มีเงา lavender |
| 9 (144–159) | `A`–`M` | แบน ไม่มีเงา |
| 10 (160–175) | `N`–`Z` | แบน ไม่มีเงา |

เลือกใช้ **แถว 6–8** เพราะเป็นชุดเดียวที่ครบทั้งตัวอักษรและตัวเลข (ชุดแบนไม่มีตัวเลขเลย)

**ข้อค้นพบสำคัญ:** ตัวอักษรแต่ละไทล์วาดเป็น *ตัวขาวบนแผ่นสีเข้ม + เงา lavender* แต่ TMP tint glyph ด้วย **alpha อย่างเดียว** ดังนั้นถ้าคัดลอกทั้งไทล์จะได้ "บล็อกทึบ" ไม่ใช่ตัวอักษร — ต้องสกัดเฉพาะพิกเซลขาว (`r,g,b > 0.85`) เป็น mask ขาวบนโปร่งใส

**ผลลัพธ์** (`Tools > V2 > Rebuild Pixel Font` → `Assets/V2/Editor/PixelFontBuilder.cs`)

| ไฟล์ | คืออะไร |
|---|---|
| `Assets/V2/Fonts/PixelTilesetAtlas.png` | 128×80, 39 glyph ในกริด 8 คอลัมน์, Point filter, ไม่บีบอัด |
| `Assets/V2/Fonts/PixelTileset.mat` | material shader `TextMeshPro/Bitmap` |
| `Assets/V2/Fonts/PixelTileset.asset` | `TMP_FontAsset` — 66 glyph / 66 character (ก่อนเติมวรรคตอนใน §7.15) |
| `Assets/V2/Fonts/Source/tilemap_packed.png` | สำเนาต้นฉบับ ให้ pipeline ไม่ต้องพึ่ง `Assets/Day` |

| ค่า | ผล (หลัง §7.15) |
|---|---|
| ตัวอักษรที่มี | `A`–`Z` + `a`–`z` (พับไปตัวพิมพ์ใหญ่) + `0`–`9` + `% + -` + วรรคตอนวาดเพิ่ม = **88 glyph / 88 character** |
| ขนาดตัวอักษรจริง | 6×8 px (`I` 2×8, `M`/`W` 8×8, `Q` 6×9) |
| em / cap height | `pointSize` 11 / cap 8 → cap/em = 0.727 (เท่า LiberationSans) |
| ระยะห่าง | `bearingX` = 0 (ตัด padding ของไทล์), advance = ink + 1, space = 6 |
| Fallback | LiberationSans SDF → เครื่องหมายที่ยังไม่มีไม่กลายเป็นกล่องเปล่า |

**วิธีใช้:** ตั้ง `fontSize` เป็น **22** (หรือ 11/33 = ตัวคูณของ `pointSize`) เพื่อให้ 1 texel = 2 px พอดี; ที่ขนาดอื่นภาพยังคม (Point filter) แต่ความหนาเส้นจะไม่สม่ำเสมอ
**ข้อจำกัด:** ไม่มีตัวพิมพ์เล็กจริง (พับเป็นใหญ่), ไม่มีสระไทย — ฟอนต์นี้เหมาะกับตัวเลข/ป้าย/HUD อังกฤษ ไม่ใช่บทสนทนา

**ยืนยันแล้ว:** รัน builder ซ้ำได้ผลเท่าเดิม (`glyphs=66`, `brightPixels=2295`), preview render ที่ `Temp/pixel-font-preview.png` อ่านออกครบ A–Z / a–z / 0–9 / `%+-` / "Day 7", compile 0 error

### 7.15 เปลี่ยน TMP ทั้งหมดของ V2 UI มาใช้ฟอนต์นี้ (2026-10-28)

งานนี้แก้ 4 อย่างที่ §7.14 ยังไม่พอ:

**1. วรรคตอนที่ไทล์ไม่มี — วาดเพิ่มแบบ procedural**
ไทล์มีแค่ `A–Z`, `0–9`, `% + -` แต่ UI พิมพ์ `: ( ) / [ ] , . — – …` ตลอด ถ้าปล่อยให้ fallback ไป LiberationSans ข้อความบรรทัดเดียวจะมี 2 ฟอนต์ปนกัน จึงวาดเพิ่ม 22 ตัวในสไตล์เดียวกัน (เส้นหนา 2px, วางบน baseline เดียวกัน, กริด 6×8) เป็นตาราง `SyntheticGlyphs` ใน builder — เพิ่ม/แก้ glyph ได้ด้วยการแก้ string 8 บรรทัด

**2. Metrics ให้ตรงกับ LiberationSans → layout ไม่ขยับ**
`pointSize` เดิม 16 ทำให้ cap/em = 0.5 (ตัวเล็กกว่าที่ layout ถูกออกแบบไว้ 30%) เปลี่ยนเป็น **11** → cap 8 / em 11 = **0.727** เท่ากับ LiberationSans ทำให้ `fontSize` เดิม (17–26) ให้ขนาดตัวอักษรใกล้เดิมที่สุด

| | เดิม (LiberationSans) | ใหม่ (PixelTileset) |
|---|---|---|
| cap height ที่ fontSize 22 | ~15.6 px | **16 px** |
| line height ที่ fontSize 22 | ~26.4 px | **26 px** |

**3. Advance เดิมติด padding ของไทล์**
ตัวอักษรในไทล์ถูก inset ไว้ 5px (เพราะมีแผ่นเข้ม) ค่า `bearingX` จึงกลายเป็น 5 และ advance = 13 units ทำให้ตัวอักษรห่างกันเกินจริง แก้โดยตัด inset ออก: `bearingX = max(0, inkX - 5)`, `advance = bearingX + inkW + gap` และปรับ `gap` 2 → **1** ให้ความกว้างต่อตัวใกล้ LiberationSans (A–Z 6–8 units + 1)

**4. ตัวเลขต่ำกว่าตัวอักษร 1px ในไทล์ต้นฉบับ**
ไทล์วางตัวเลขที่ y4–11 แต่ตัวอักษร y5–12 → `bearingY` ต่างกัน 1px ถ้าปล่อยไว้ HUD ที่มีทั้งตัวเลขและตัวอักษรจะเหลื่อม จึง snap เฉพาะ glyph ที่สูงเต็ม cap (`inkH == capHeight`) ให้อยู่บน cap line เสมอ ส่วนเครื่องหมายสั้น (`. , - –`) คงตำแหน่งของตัวเองไว้

**การเปลี่ยน component ทั้งหมด** — `Tools > V2 > Apply Pixel Font To V2 UI` (`Assets/V2/Editor/ApplyPixelFont.cs`)

| ขั้น | ทำอะไร |
|---|---|
| 1 | ตั้ง `TMP_Settings.m_defaultFontAsset` = PixelTileset — ครอบ TMP ที่สร้างจากโค้ด (เช่น `ZoneLabel` ใน `MapNodeView` ที่มาจาก `TmpOutlineUtil.CreateLabel` และไม่เคย assign font) |
| 2 | ไล่ prefab ทุกไฟล์ใต้ `Assets/V2/Prefabs` ผ่าน `LoadPrefabContents` → แก้ `m_fontAsset` + `m_sharedMaterial` → `SaveAsPrefabAsset` |
| 3 | เปิด `Core.unity`, แก้ TMP ทุกตัว, save |
| 4 | รายงานจำนวนที่เปลี่ยน (รันซ้ำได้, ตัวที่ตรงแล้วข้าม) |

**กับดักที่เจอ:** อ่าน `text.font` บน reference ที่ตายแล้วจะโยน `MissingReferenceException` และตัวที่ reference ว่างจะถูก TMP เติมจาก default ตอน `Awake` (จึงเห็นเป็น LiberationSans ทั้งที่ prefab ถูกแก้แล้ว) จึงเปลี่ยนไปเขียน/อ่านผ่าน `SerializedObject.FindProperty("m_fontAsset")` ซึ่งทน reference เสียได้ และเป็นเหตุผลที่ต้องตั้ง default font ด้วย ไม่ใช่แค่แก้ prefab

**Layout ที่ต้องขยับ 1 จุด:** ฟอนต์นี้เป็น uppercase-only จึงกว้างกว่า LiberationSans ต่อความสูงตัวอักษรเดียวกัน — `Food: 16 (need 4 tonight)` ต้องการ 302px แต่ label กว้าง 240px จึงตัดขึ้นบรรทัดใหม่ทับแถวบน แก้โดยขยาย label `Food` +80px และเลื่อน `Medicine` / `CookingMaterial` / `Materials` ตาม +80px (แถวเดิมห่างกัน 264px จึงยังไม่ชนกรอบ DAY)

**ตรวจแล้ว:** `TestUiRedesign` เพิ่ม 2 ข้อ → **ALL PASS 37 ข้อ** ในรอบเดียว — "all 34 labels use the pixel font" (นับรวม `ZoneLabel` 6 ตัวและ nametext/statustext 8 ตัวที่สร้างตอนรัน) และ "every printed character has a glyph" (สแกนข้อความจริงหลัง strip rich-text tag) · console 0 error 0 warning

**ยังไม่ทำ (ตั้งใจ):** `Assets/Day` และ `Assets/Night` scenes (`ShipDay`, `Night`, `NPCtest`, `Setup`) ยังใช้ LiberationSans ของเดิม — เป็น prototype คนละชุดกับ V2 และยังอยู่ใน Build Settings ถ้าต้องการให้เปลี่ยนด้วยสั่งได้ (`TMP_Settings` default เปลี่ยนแล้ว จึงมีผลกับ TMP ที่สร้างใหม่ในซีนเหล่านั้น แต่ component เดิมยังชี้ LiberationSans อยู่)

### 7.16 รวมกลุ่ม `Warning` ให้ parent/child ตรงกัน (2026-10-28)

เดิม `WarningBackdrop` กับ `WarningText` เป็น object **แยกกันบน Canvas** (ไม่ใช่พ่อลูก) แต่ละตัวมี `CanvasGroup` ของตัวเอง และ `WarningView` อยู่บน `WarningText` โดยอ้าง `backdropGroup` ของอีกตัว

หลังรวมเป็น group `Warning` (parent) ที่มีลูก 2 ตัว ปัญหาที่โผล่คือ **ลูกทั้งคู่ถูก stretch เต็ม parent แต่ยังถือค่า pivot/anchoredPosition เดิมจากตอนเป็น object อิสระ**

| object | anchor | pivot | anchoredPosition | sizeDelta | ผล |
|---|---|---|---|---|---|
| `WarningBackdrop` | (0,0)-(1,1) | **(0,0)** | **(300,60)** | (−100, 0) | มุมล่างซ้ายไปโผล่ที่ (300,60) ของ parent |
| `WarningText` | (0,0)-(1,1) | (0.5,0.5) | (250,60) | (−100, 0) | กึ่งกลางไปโผล่ที่ (250,60) |

→ backdrop กับ text ห่างกันเกินขนาดของ parent เอง

**หลักการ:** ลูกที่ stretch หมายถึง "เต็ม parent" — จะจริงก็ต่อเมื่อ `pivot = (0.5,0.5)`, `anchoredPosition = (0,0)`, และ `sizeDelta` คือ **inset** (ติดลบเมื่อต้องการขอบ) ไม่ใช่ offset ที่ตกค้าง

**ที่แก้**

| อย่าง | เดิม | ใหม่ |
|---|---|---|
| CanvasGroup | 2 ตัว (คนละลูก) + 1 ที่ root | **1 ตัวที่ `Warning`** — ลูกจึงจางตามพร้อมกัน |
| `WarningView` | อยู่บน `WarningText` | ย้ายไป `Warning` (root) — `label` → `Warning/WarningText`, `backdropGroup` → ว่าง |
| ลูกทั้งคู่ | pivot/offset ตกค้าง | `pivot .5`, offset `0`, backdrop `sizeDelta (0,0)`, text `sizeDelta (−16,−12)` |
| TMP margin | `(2, −3.86, 0.64, −4.05)` | `0` (ระยะขอบมาจาก inset ของ rect แทน) |
| ขนาด group | 200 × 60 | **620 × 120** (ขนาดเดิมของแบนเนอร์ — ที่ 200×60 ข้อความเต็มประโยคขึ้น 4 บรรทัด) |

**กับดักที่เจอ:** ลบ `CanvasGroup` ออกจาก object ที่ยังถือ `WarningView` อยู่ไม่ได้ เพราะ `[RequireComponent(typeof(CanvasGroup))]` จะเติมกลับให้ → ต้องย้าย `WarningView` ไป root **ก่อน** แล้วจึงลบ CanvasGroup ของลูก · การย้าย component ทำให้ serialized reference ของ `GatherDispatchPanel.warningView` / `SkillFlowController.warningView` ชี้ object ที่ถูกทำลาย สคริปต์จึงไล่ repoint ให้ (2 จุด)

**ผล:** `backdrop` world (640,480)-(1260,600) · `text` world (648,486)-(1252,594) → **inset L=8 R=8 B=6 T=6, centred=True** และ fade ด้วย alpha เดียว (ไม่เป็นกำลังสอง)

**ยืนยันแล้ว:** `TestUiRedesign` เพิ่ม 2 ข้อ → **ALL PASS 39 ข้อ** — "warning banner fades through one CanvasGroup on the group root" และ "warning text sits centred inside the backdrop" · console 0 error

> **บทเรียนเรื่อง process:** การแก้ `.unity` จากภายนอกขณะ editor เปิดซีนนั้นอยู่ ทำให้ Unity ขึ้น dialog "The open scene(s) have been modified externally" และบล็อก main thread (pipeline ทุกคำสั่ง timeout) — งานซีน/prefab ต้องทำผ่าน editor API (`script.run`) เท่านั้น

### 7.17 สีของ Warning อ่านจากตารางเดียวกับ log (2026-10-28)

**ปัญหา:** ข้อความเดียวกันเป็นสองสี — `[D1] Pick a cave on the map first.` ใน log เป็น**เหลืองอำพัน** แต่บนแบนเนอร์เป็น**ขาว** (และ outline แดง) เพราะสองฝั่งมีชุดสีของตัวเอง:

| | log (`LogMessagePanel`) | banner (`WarningView`) |
|---|---|---|
| แหล่งสี | 6 field ที่ serialize ไว้ | 3 field outline + `textColor` ขาว |
| enum | `LogKind` (6 ค่า) | `WarningKind` (3 ค่า) |
| ใช้สีกับ | `row.color` = ตัวข้อความ | `label.color` = ขาว · outline = สี |
| ค่าที่ผูกกับ "warning" | `(1, 0.72, 0.25)` อำพัน | `errorOutline (1, 0.25, 0.22)` แดง |

เรียกด้วย `WarningKind.Error` แต่ลง log ด้วย `LogKind.Warning` → สีไม่มีทางตรงกันได้

**อีกปัญหาที่เจอพร้อมกัน:** ฟอนต์ pixel ใช้ shader `TextMeshPro/Bitmap` ซึ่ง **ไม่มี property outline เลย** (`_MainTex _FaceTex _FaceColor _VertexOffsetX …`) ดังนั้น `TmpOutlineUtil.SetOutlineColor(...)` ที่ `WarningView` เรียกมาตลอดจึงเป็น no-op — ที่เห็นเป็นตัวขาวเฉย ๆ ไม่ใช่ "ขาว + outline แดง"

**ที่แก้**

| อย่าง | เดิม | ใหม่ |
|---|---|---|
| ตารางสี | 2 ชุด (6 + 4 สี) | **`UiPalette`** (`Scripts/UI/UiPalette.cs`) — 6 สีคงที่ + `For(LogKind)` |
| enum | `LogKind` + `WarningKind` | **`LogKind` ตัวเดียว** — ลบ `WarningKind` ทิ้ง |
| API | `Show(text, WarningKind)` + `ShowWarning/ShowError/ShowInfo` | `Show(text, LogKind kind = LogKind.Warning)` |
| สีข้อความ | ขาว + outline (no-op) | **`label.color = UiPalette.For(kind)`** |
| outline | สร้าง material instance ทุก Awake | ลบทิ้ง (ใช้ไม่ได้กับ bitmap shader) |
| `LogMessagePanel` | 6 field ใน Inspector | อ่าน `UiPalette.For(entry.Kind)` |
| call site | `Show(msg, WarningKind.Error)` + `RunLog.Add(LogKind.Warning, msg)` | `Show(msg, LogKind.Warning)` + `RunLog.Add(LogKind.Warning, msg)` — ค่าเดียวกัน |

**`SkillFlowController.Warn()`** เดิมขึ้นแบนเนอร์แต่ **ไม่ลง log** (แค่ `Debug.LogWarning`) ซึ่งขัดกับ AC ข้อ 1 — แก้ให้ลง `RunLog.Add(LogKind.Warning, …)` ด้วย ทำให้คู่ banner↔log ครบทุกจุด

**ผล:** วัดจากภาพหน้าจอ — แบนเนอร์ `#FFB840` และบรรทัด log ของข้อความเดียวกัน `#FFB840` (3028 vs 1511 พิกเซลที่ค่าตรงกัน)

**ยืนยันแล้ว:** `TestUiRedesign` เพิ่ม 3 ข้อ → **ALL PASS 42 ข้อ** — "banner colour matches the log row" สำหรับ `Warning` / `Fail` / `Success` โดยเทียบสีที่ **render จริง** ของทั้งสองฝั่ง (ไม่ใช่เทียบค่าคงที่) · console 0 error

> หมายเหตุ: `Show()` เรียก `StartCoroutine` ซึ่งรัน body ถึง `yield` แรกแบบ synchronous → ข้อความและสีถูกเพนต์ทันทีใน `Show()` เทสต์จึงเทียบสีได้ในเฟรมเดียว

### 7.18 ตัดสไปรต์ไอคอน AP และ CoreItem ออกจาก tileset เดิม (2026-10-28)

**ที่มา:** ต้องการไอคอนสำหรับ (ก) แถบ AP บนการ์ดตัวละคร แทนข้อความ `AP 3/3` และ (ข) ไอคอน Core Item ใน HUD

**แหล่งที่มา — สองไฟล์เดิม ไม่มีการย้าย/คัดลอกโฟลเดอร์**

| ไฟล์ | ขนาด | ใช้ทำอะไร |
|---|---|---|
| `Assets/Day/Sprites/tilemap_packed.png` | 288×176 | 3 สไปรต์ AP (อ่านชื่อ + rect จาก `.meta` ที่ slice ไว้แล้ว) |
| `Assets/Day/Sprites/tilemap_packed 1.png` | 288×208 | ไอคอน CoreItem (หา rect จากพิกัดพิกเซล) |

**สไปรต์ AP** — มาจาก slice ที่มีชื่ออยู่แล้วใน `.meta` จึงตัดตาม `rect` ตรง ๆ (Unity เก็บ `y` จากล่างขึ้นบน → `yTop = 176 − y − h`)

| ไฟล์ | slice | rect ใน `.meta` | y จากบน | สถานะ |
|---|---|---|---|---|
| `ap_45.png` | `tilemap_packed_45` | x 275, y 34, 10×12 | 130 | **มี AP** (แกนในสีน้ำเงิน) |
| `ap_30.png` | `tilemap_packed_30` | x 275, y 50, 10×12 | 114 | **กำลังจะถูกใช้** (สีส้ม) — พรีวิวต้นทุนของ action ถัดไป |
| `ap_15.png` | `tilemap_packed_15` | x 275, y 66, 10×12 | 98 | **ใช้ไปแล้ว** (แกนในมืด) |

**ไอคอน CoreItem** — ตัดด้วย bounding box ของ alpha ภายในหน้าต่างที่ปลอดจากไทล์ข้างเคียง (ไม่ใช่ตัดตามกริด 16 px เพราะชีตนี้เป็น packed sheet ที่ไอคอนล้ำเซลล์)

| ไฟล์ | พิกัด (จากบน) | ขนาด | รูป |
|---|---|---|---|
| `coreitem_crystal.png` | 258, 98 | 12×12 | คริสตัลม่วง + ประกายขาว (แบบมีแฉก) |
| `coreitem_crystal_b.png` | 258, 162 | 12×12 | คริสตัลเดียวกันแต่ขอบมนกว่า (ต่างกัน 36/144 px) |
| `coreitem_key_plain.png` | 0, 179 | 16×10 | กุญแจหัวเงิน |
| `coreitem_key_lit.png` | 48, 179 | 16×10 | กุญแจหัวส้ม |
| `coreitem_panel_orange.png` | 0, 194 | 16×14 | แผงส้ม + หมุดกลาง |
| `coreitem_panel_orange_bar.png` | 16, 192 | 16×16 | แผงส้ม + แถบดำ |
| `coreitem_panel_yellow.png` | 32, 194 | 16×14 | แผงเหลือง + หมุดสองข้าง |
| `coreitem_panel_yellow_bar.png` | 48, 192 | 16×16 | แผงเหลือง + แถบดำ |

**Import settings** (`Assets/V2/Sprites/Icons/`) — ตั้งด้วย `TextureImporter` ผ่าน `AgentScripts/ImportIconSprites.cs`: `Sprite (Single)` · `Point` · `FullRect` · `Uncompressed` · `alphaIsTransparency` · no mipmap · PPU 100 · Clamp · ไม่สร้าง physics shape

**ยังไม่ต่อเข้ากับ UI ตอนนั้น** — ต่อเข้าแล้วใน §7.19 (`ap_45/30/15` → pip บนการ์ด) · CoreItem 8 ไอคอนยังรอจับคู่ชื่อ Core Item A/B

**ยืนยันแล้ว:** `script.run` คืน `ok` ครบ 11 ไฟล์ พร้อมขนาด rect จริง (10×12, 12×12, 16×10, 16×14, 16×16) และ `.meta` แสดง `filterMode: 0` / `spriteMode: 1`

### 7.19 AP บนการ์ดเปลี่ยนจากตัวเลขเป็น pip (2026-10-28)

**โจทย์:** `statusText` เดิมพิมพ์ `AP 3/3` เป็นข้อความ ต้องการแทนด้วยไอคอนจาก tileset เพื่อให้เห็นต้นทุนของ action ก่อนกดยืนยัน

**รูปแบบ 3 สถานะ** (สไปรต์จาก §7.18) — วาดจากซ้ายไปขวา, AP ถูกใช้จากซ้าย

| ไอคอน | เงื่อนไข | ความหมาย |
|---|---|---|
| `ap_45` | `i < current − spending` | ยังเหลือและไม่ถูกใช้ใน action นี้ |
| `ap_30` | `current − spending ≤ i < current` | action ที่กำลังจะยืนยันจะกิน pip นี้ |
| `ap_15` | `i ≥ current` | ใช้ไปแล้ว |

**ที่แก้**

| ไฟล์ | สิ่งที่เพิ่ม |
|---|---|
| `Scripts/UI/UnitIconView.cs` | field `apPipAvailable/Pending/Spent` + metric (`18×22`, spacing 4, row 22) · `BuildApPips()` สร้าง row `ApPips` ใต้ชื่อ · `RefreshApPips()` สร้าง pip เท่า `maxActionPoints` แล้วเพนต์ sprite ตามสถานะ · `SetApCostPreview(int)` + `ApCostPreview` |
| `Scripts/UI/UnitRosterView.cs` | `CurrentPendingApCost()` — อ่านจาก `SkillFlowController.PendingApCost` ถ้ามีสกิล armed, ไม่งั้นอ่าน cave ที่เลือกบน map · เรียก `SetApCostPreview()` ทุกเฟรมก่อน `Refresh()` |
| `Scripts/UI/SkillFlowController.cs` | `PendingApCost` = base cost × จำนวนรอบ (cook/craft) หรือค่า heal ตามโหมดยา · 0 เมื่อไม่ได้ armed |
| `Scripts/Controllers/BaseWorkController.cs` | `CookBaseApCost` / `WorkshopBaseApCost` / `HealBaseApCost(bool)` — ต้นทุน **ก่อน** บวก starvation เพราะต้องใช้เพนต์การ์ดหลายใบ |
| `Scripts/UI/UnitIconView.cs` (layout) | `statusText` ย้ายลงมา `apPipRowHeight + 2` และลดความสูง 36 → 32 — บรรทัด `AP x/y` ถูกแทนที่ จึงไม่ต้องขยายการ์ด |
| `Assets/V2/Prefabs/ScoutIcon.prefab` | ผูกสไปรต์ 3 ตัว + metric ผ่าน `AgentScripts/WireApPips.cs` |

**ทำไมพรีวิวไม่ใช่การ push จากฝั่ง panel:** สองเจ้าของ (gather กับ skill) เขียนค่าเดียวกันไม่ได้ — ถ้า skill disarm แล้วเขียน 0 จะลบค่าของ cave ที่เลือกไว้ · จึงให้ `UnitRosterView` **อ่าน** จากระบบเจ้าของทุกเฟรมแทน (single reader, ไม่มีลำดับการเขียนที่ผิดได้)

**Starvation:** `UnitIconView` เรียก `unit.GetActualApCost(base)` เอง ทำให้หน่วยที่อดอาหารเห็น pip สี `ap_30` มากกว่าคนอื่นจากต้นทุนเดียวกัน

**ยืนยันแล้ว:** `TestUiRedesign` เพิ่ม 3 ข้อเป็น **ALL PASS 46 ข้อ** — "card paints one pip per action point", "pending cost flips the top pips to the about-to-be-spent state", "spent AP turns the pips into the spent state" และขยายข้อ "cook queues a second round" ให้ยืนยัน `flow.PendingApCost == 2` · ภาพหน้าจอ: เลือกถ้ำ Easy (1 AP) → ทุกการ์ดมี pip ขวาสุดเป็น `ap_30` ตามที่ควรเป็น

> หมายเหตุ: `RefreshApPips()` ถูกเรียกจาก `Refresh()` ซึ่ง roster เรียกทุกเฟรมอยู่แล้ว — ไม่มี allocation เพิ่มหลังสร้าง pip ครบ · ถ้าไม่ผูกสไปรต์ การ์ดจะยังพิมพ์ `AP x/y` แบบเดิม (fallback)

**แก้ตามรีวิว (2026-10-28):** `ap_30` โผล่บน**ทุกการ์ด** ทั้งที่การ์ดที่ไม่ได้เลือกไม่ได้กำลังจะทำอะไร → ย้ายเงื่อนไขไปที่ `ApCostPreview => IsSelected ? pendingApCost : 0` และ `RefreshApPips()` อ่านค่านี้ · `SetSelected()` เรียก `RefreshApPips()` เพิ่มเพื่อให้เปลี่ยนทันทีในเฟรมเดียวกับที่คลิก (ไม่ต้องรอ `Update` ของ roster) · ผลคือ **ไม่มีอะไรถูกเลือก → ไม่มี pip ส้มเลย** และเลือกการ์ดไหน การ์ดนั้นเท่านั้นที่พรีวิวต้นทุน

### 7.20 Core Item ใช้สไปรต์กุญแจ/กล่อง/คริสตัล (2026-10-28)

**โจทย์:** เปลี่ยนช่อง Core Item จาก "กรอบเปล่า + tint" เป็นสไปรต์จริง และกำหนดความหมายของแต่ละภาพ

**คู่สี — กล่องเป็นตัวกำหนดกุญแจ**

| กล่อง | กุญแจที่เปิดได้ | คริสตัลตอนชนะ |
|---|---|---|
| ส้ม — `coreitem_panel_orange` / เปิด `coreitem_panel_orange_bar` | **ส้ม** — `coreitem_key_lit` | `coreitem_crystal` (แบบมีแฉก) |
| เหลือง — `coreitem_panel_yellow` / เปิด `coreitem_panel_yellow_bar` | **เทา** — `coreitem_key_plain` | `coreitem_crystal_b` (ขอบมน) |

**ทำไมกุญแจไม่มีทางผิด:** `RunResources` ทอยคู่สี **ครั้งเดียวตอนเริ่ม run** (`CoreItemVariant` = Orange / Yellow, สลับได้ด้วย `randomiseVariant`) แล้วทั้งกล่องและกุญแจอ่านคู่เดียวกัน → กุญแจที่ได้เป็นดอกที่เปิดกล่องได้เสมอ ไม่มีทางที่ RNG จะทำให้ run ชนะไม่ได้

**สถานะที่แสดง**

| ถืออะไร | ช่องกล่อง (A) | ช่องกุญแจ (B) | popup กลางจอ |
|---|---|---|---|
| ยังไม่พบ | เงามืดของกล่อง (`LockedColor`) | เงามืดของกุญแจ | — |
| กล่อง | กล่อง**ปิด**สีตามคู่ | เงามืด | — |
| กล่อง + กุญแจ | กล่องเดียวกันแต่ **ฝาเปิด** (`_bar`) | กุญแจสีจริง | **คริสตัล** + caption |

`HasBothCoreItems` = เงื่อนไขชนะอยู่แล้ว → ใช้ค่านี้เป็นสวิตช์ "เปิดกล่อง" ตรง ๆ ไม่ต้องเพิ่ม state ใหม่

**สิ่งที่แก้**

| ไฟล์ | สิ่งที่เพิ่ม |
|---|---|
| `Scripts/CoreItemKind.cs` | enum `CoreItemVariant { Orange, Yellow }` + คำอธิบายใหม่ของ A (กล่อง) / B (กุญแจ) |
| `Scripts/System/RunResources.cs` | field `variant` + `randomiseVariant` + `Variant` getter + `SetVariant()` (ปิด randomisation ให้ เทสต์/Inspector บังคับได้) · ทอยใน `Awake` |
| `Scripts/UI/StatusBarView.cs` | inner class `CoreItemArt { closedBox, openBox, key, crystal }` + field `orangePair` / `yellowPair` · `ApplyCoreItemIcon(icon, placeholder, art, obtained)` เลือกสไปรต์ตามคู่และ tint `Color.white` เมื่อมี art (ของจริงมีสีของตัวเอง) · ข้อความ HUD เปลี่ยนเป็น `BOX` / `KEY` |
| `Scripts/UI/CrystalRevealView.cs` | popup กลางจอ (สร้าง UI เองในโค้ด) — ดู §7.21 |
| `Scripts/UI/RunLogNarrator.cs` | log เปลี่ยนจาก `Found Core Item A!` เป็น `Found the sealed box!` / `Found the key!` |
| `Core.unity` | ผูกสไปรต์ในตาราง (`AgentScripts/WireCoreItemArt.cs`, `AgentScripts/WireCrystalReveal.cs`), เพิ่ม object `Canvas/CrystalReveal`, และตั้งไอคอนทั้งสองช่องเป็น `96×96` กลางกรอบ (`AgentScripts/SizeCoreItemIcons.cs`) — 96 = 6× ของ 16×14/16×10 และ 8× ของ 12×12 จึงยังเป็นจำนวนเต็มเท่า พิกเซลไม่เพี้ยน |

**ยืนยันแล้ว:** `TestUiRedesign` เพิ่ม 2 ข้อ → **ALL PASS 49 ข้อ** — "Orange pair shows its closed box, then the crystal once the key arrives" และคู่ Yellow ตรวจจาก **สไปรต์ที่ถูกเพนต์จริง** บน `Image` (ไม่ใช่ค่าคงที่) · ภาพหน้าจอ: กล่องส้มอย่างเดียว → กล่อง+กุญแจ = คริสตัล ทั้งคู่สีส้มและคู่สีเหลือง

### 7.21 คริสตัลย้ายออกจาก HUD ไปเป็น popup กลางจอ (2026-10-28)

**รีวิวรอบสอง:** การเอาคริสตัลไปแทนช่องกล่องทำให้ HUD อ่านไม่เป็น inventory อีกต่อไป (ของที่ถืออยู่หายไปแล้วกลายเป็นของอีกอย่าง) → เปลี่ยนเป็น **กล่องเดิมแต่ฝาเปิด** และย้ายคริสตัลไปเป็น popup กลางจอ

**ลำดับที่เกิด:** `HasBothCoreItems` false → true (ทริปที่เก็บชิ้นที่สองสำเร็จ หรือ `CheckImmediateWin`) — `CrystalRevealView` เฝ้า edge นี้ใน `Update()`

**`CrystalRevealView` — สร้าง UI เองทั้งหมดในโค้ด** (ซีนมีแค่ object ว่าง `Canvas/CrystalReveal` 1 ตัว) จึงไม่มีปัญหา prefab/scene ค้าง

| ชิ้น | ค่า |
|---|---|
| root | stretch เต็มจอ + `CanvasGroup` (alpha 0, `blocksRaycasts` false ตอนปิด) |
| `Backdrop` | `Image` สี `(0.04, 0.05, 0.08, 0.82)` เต็มจอ — ทำให้คริสตัลเด่นและกลืนคลิกไม่ให้ทะลุไปโดนบอร์ด |
| `Crystal` | `Image` กลางจอ `192×192` (= 16× ของ 12×12, จำนวนเต็มเท่า) `preserveAspect` |
| `Caption` | `TMP` "THE BOX IS OPEN" สีอำพันใต้คริสตัล (`TmpOutlineUtil.CreateLabel` → ได้ฟอนต์ pixel จาก default) |
| ปิด | คลิกที่ไหนก็ได้ (`IPointerClickHandler`) → fade out ; run ยังชนะอยู่ |

**ทำไมไม่มี Coroutine:** fade ทำใน `Update()` ด้วย `Mathf.MoveTowards(alpha, target, deltaTime / fadeSeconds)` — เล่นได้เหมือนเดิม แต่ทดสอบในเฟรมเดียวและกด pause แล้วยังได้ค่าที่คาดหวัง

**`wasOpen` เริ่มจากค่าจริงตอน `Awake`** — ถ้าโหลดซีนซ้ำตอนชนะแล้ว จะไม่เด้ง popup ซ้ำ

**ยืนยันแล้ว:** `TestUiRedesign` เพิ่ม 3 ข้อ → **ALL PASS 52 ข้อ** — "a crystal reveal exists under the canvas", "Orange pair reveals `coreitem_crystal` once the box is opened", "Yellow pair reveals `coreitem_crystal_b`..." · ตรวจว่า HUD **ไม่** เพนต์คริสตัลแล้ว (ต้องเป็น `_bar` ของคู่ตัวเอง) · ข้อ raycast ข้าม popup ตอนปิด (ไม่งั้นจะนับเป็น interactive element ที่กดไม่ถึง) · ภาพหน้าจอ: คู่ส้มและคู่เหลือง ขึ้น popup ถูกดอก คริสตัลคมระดับพิกเซล (คูณ 16 ลงตัว)

---

## 8. Contracts ที่ต้องเพิ่ม (ต่อยอดจากข้อ 3 ใน Overview)

| # | API | เจ้าของ | ผู้ใช้ | หมายเหตุ |
|---|---|---|---|---|
| **L1** | `RunLog.OnEntryAdded : event Action<LogEntry>` + `Add(LogKind, string)` | กลุ่ม 3 | ทุกกลุ่ม | แหล่ง log กลาง |
| **L2** | `BaseWorkController.OnBaseWorkCompleted : event Action<BaseWorkResult>` | กลุ่ม 1 | กลุ่ม 3 (log), กลุ่ม 2 (SFX) | = C1 เดิม — ✅ **มีแล้ว** (P2) |
| **L3** | `RunResources.OnChanged : event Action` | กลุ่ม 3 | กลุ่ม 1 | = C7 เดิม — ✅ **มีแล้ว** (P0) |
| **L4** | `DayManager.OnDayEnded : event Action<DayEndReport>` + `PreviewFeeding()` | กลุ่ม 3 | กลุ่ม 1/2 | = C4 เดิม — ✅ **มีแล้ว** (P2) |
| **L5** | `RunResources.HasBothCoreItems` / `HasCoreItem(CoreItemKind)` + `ObtainCoreItem(CoreItemKind)` + enum `CoreItemKind { A, B }` | กลุ่ม 3 (แตะ shared) | กลุ่ม 1, กลุ่ม 2 | ✅ **มีแล้ว** (P0) — **ต้องแจ้งก่อนแก้** |
| **L6** | `GatherZoneConfig.coreItemChancePercent` (0–100, ทอยเฉพาะ HardSuccess) + `GatherDispatchResult.coreItemsObtained : List<CoreItemKind>` | กลุ่ม 2 (เจ้าของ `GatherZoneData`/`GatherDispatchController`) | กลุ่ม 3 (log) | ✅ **มีแล้ว** (P0) |
| **L7** | `GatherDispatchController.OnGatherResolved` ยิง **ทันที** หลัง resolve (ไม่ผ่าน walker) + ลบ `GatherMapController.TripInProgress` | กลุ่ม 2 | กลุ่ม 3 (log) | **C3 เดิมล้าสมัย — ต้องแก้** |
| **L8** | `UnitCharacter.HasGatheredToday` (busy) เปิดให้ UI อ่านตรง ๆ | มีอยู่แล้ว | กลุ่ม 3 | ใช้ตัดสิน selectability ตามโหมด |

---

## 9. ผลกระทบรายไฟล์ (Draft)

| ไฟล์ | การเปลี่ยนแปลง | ระดับ |
|---|---|---|
| `Scripts/UI/WarningView.cs` | จำกัดการเด้งกลางจอเฉพาะ Error + ส่งเข้า log | M |
| `Scripts/UI/StatusBarView.cs` | แยก CoreItem 2 ช่อง, ย้าย End Day ไป DayFrame, เลิก poll ทุกเฟรม (ใช้ C7) | M |
| `Scripts/UI/UnitRosterView.cs`, `UnitIconView.cs` | ปรับเป็นมือไพ่ล่างกลาง + รองรับ selection ร่วมกับ skill | M |
| `Scripts/UI/GatherDispatchPanel.cs` | ย้าย/ขยาย panel, ซ่อนเมื่อว่าง, label ตามบริบท | M |
| `Scripts/UI/BaseWorkPanel.cs` | **แทนที่**ด้วย `SkillTriggerPanel` (หรือ refactor ตัวเดิม) | L |
| `Scripts/UI/LogMessagePanel.cs` **(ใหม่)** | log list + minimize | M |
| `Scripts/System/RunLog.cs` **(ใหม่)** | buffer + event | S |
| `Scripts/System/RunResources.cs` | CoreItem A/B (ดรอปอิสระ ไม่มีลำดับ) + `HasBothCoreItems` + `ObtainCoreItem(CoreItemKind)` | S |
| `Scripts/Entities/GatherZoneData.cs` | เพิ่ม enum `CoreItemKind` + field `coreItemChancePercent` ใน `GatherZoneConfig` — **เจ้าของกลุ่ม 2** | S |
| `Scripts/System/GatherZoneDatabase.cs` | ใส่ค่าเริ่มต้น chance: Easy 0% · Normal 5% · Hard 15% — **เจ้าของกลุ่ม 2** | S |
| `Scripts/Controllers/GatherDispatchController.cs` | ลบ `HandleCoreItemProgress` → `TryRollCoreItems(config, result)` (เรียกเฉพาะ `HardSuccess`, ทอยแยกต่อชิ้น, ข้ามชิ้นที่มีแล้ว); `GatherDispatchResult.unlockedCoreItemMaterial/obtainedCoreItem` → `coreItemsObtained` — **เจ้าของกลุ่ม 2** | M |
| `Scripts/Controllers/GatherMapController.cs` | **ลบ `TripInProgress` + walker callbacks** → `ConfirmTrip` เรียก `Dispatch()` แล้วยิง `OnTripResolved` ทันที — **เจ้าของกลุ่ม 2** | M |
| `Scripts/UI/ScoutWalkerView.cs`, `DiceRollView.cs` | ไม่มีบทบาท (ลบตอน P5) — **เจ้าของกลุ่ม 2** | S |
| `Scripts/Controllers/DayManager.cs` | เงื่อนไขชนะ/แพ้จาก A+B | S |
| `Scripts/Controllers/BaseWorkController.cs` | เพิ่ม event C1 (ถ้าต้องการ) | S |
| `Scenes/Core.unity` | ย้าย/ลบ subtree ตาม §3.2 — **ทำหลัง prefab เสร็จ** | L |
| `Prefabs/UI/*` (ใหม่) | `CharacterCard`, `LogPanel`, `DispatchPanel`, `SkillTriggerPanel`, `WarningPanel` | L |

---

## 10. ลำดับงาน (Draft Phases)

| Phase | งาน | ผลลัพธ์ที่ตรวจได้ |
|---|---|---|
| **P0** | Data & logic: `CoreItemA/B` + `coreItemChancePercent` ใน `GatherZoneConfig`, `HasBothCoreItems`, `DayManager` win/lose, `RunLog` | ✅ **เสร็จ** — ดรอปจาก HardSuccess ได้จริง, Hard 15% > Normal 5%, Win เมื่อครบ 2 ชิ้น, แพ้เมื่อเกินวันที่ 7 |
| **P1** | โครง prefab เปล่า: Log / SkillTrigger / Dispatch / CharacterHand วางใน Canvas ใหม่ (ยังไม่ผูก logic) | ภาพ layout ตรงกับแบบร่าง |
| **P2** | `LogMessagePanel` + ต่อ event (C1/C4/L1) + minimize | ผลงาน/ทริป/วันเปลี่ยนขึ้น log ครบ |
| **P3** | `CharacterHandPanel` (ย้ายการ์ดล่างกลาง) + `DispatchActionPanel` (ซ่อนเมื่อว่าง) | dispatch เดิมยังเล่นได้ |
| **P4** | `SkillTriggerPanel` + queue (Cook/Craft) + heal toggle (ไม่มี queue) + right-click clear + `BaseWorkController` overload รับ `useMedicine` | ✅ **เสร็จ** — Cook/Craft/Heal ทำงานครบ, ไม่มี dropdown |
| **P5** | ลบของเดิม: `BaseWorkPanel`, `EndDay button`, `MessageUi`, `CharCard`, `ScoutIcon`, `CoreItem` เดี่ยว + `DayFrame` = End Day | ✅ **เสร็จ** — hierarchy ไม่มี dead object, console 0 warning, 2 ช่อง Core Item |

> **กติกา:** ทุก phase ทำ prefab ก่อน แล้วค่อยลากเข้า `Core.unity` (ตาม Overview §4) · แก้ `Core.unity` ทีละคน

---

## 11. คำถามที่ปิดแล้ว (Decisions)

| # | คำถาม | คำตอบที่ยืนยันแล้ว |
|---|---|---|
| Q1 | `ResourceStrip` อยู่ตรงไหน | **กลางบน เหมือนเดิม** (ไม่ย้าย) |
| Q2 | โหมดสกิลเลือกการ์ดได้กี่ใบ | **1 ใบ** |
| Q3 | `SkillTriggerPanel` แสดงตลอดไหม | **แสดงตลอด** |
| Q4 | พื้นที่ MapPanel / Log | **Log อยู่มุมซ้ายล่างเท่านั้น** และ **ห้ามสูงล้ำเข้าเขต MAP AREA** → MAP คงพื้นที่เดิม |
| Q5 | Heal มี queue ไหม | **ไม่มี queue** — เลือกผู้ทำ → กด Heal → เลือกผู้ป่วย → ยืนยัน (1 ครั้งต่อ 1 การยืนยัน) |
| Q6 | Log capacity | เก็บ **16** entry, **แสดงเท่าที่ช่องใส่ได้จริง** |
| Q7 | End Day ต้องมี confirm dialog ไหม | **ไม่ต้องมี** |
| Q8 | Warning ระหว่าง `TripInProgress` | **ไม่มีคำถามนี้แล้ว** — เกมไม่มีช่วง in-progress: dispatch resolve ทันที และ End Day กดได้ทุกเมื่อ |
| Q9 | Easy zone ดรอป Core Item ไหม | **ไม่มี** (0%) |

### ยังเปิดอยู่

ไม่มี — ทุกข้อปิดแล้ว (2026-10-27)

> **หมายเหตุสำคัญ:** เอกสาร sprint/contract เวอร์ชันก่อน (C3, `TripInProgress`, `ScoutWalkerView`) อ้างระบบ **time-unit** ที่เลิกใช้แล้ว — ปัจจุบันใช้ **AP** และ **ไม่มีเวลาเดินทาง/ฐานบน map** ต้องอัปเดต [V2 Task Overview](../agile/user-stories/V2-TASK-OVERVIEW.md) §3 C3 ด้วย

---

## 12. Acceptance Criteria (Draft)

1. [ ] warning ทุกกรณีที่ "ทำไม่ได้" ขึ้นกลางจอ **และ** มี log entry คู่กัน
2. [ ] ข้อความข้อมูล/ผลลัพธ์ทั่วไป **ไม่** เด้ง warning — ลง log เท่านั้น
3. [ ] log อยู่มุมล่างซ้าย **ไม่ล้ำเข้าเขต MAP AREA**, ใหม่สุดล่าง, ย่อเหลือบรรทัดเดียวได้, แสดงเท่าที่ช่องใส่ได้
4. [ ] CoreItem 2 ช่องแสดงสถานะแยกกัน, **ดรอปได้อิสระจากกัน (ไม่มีลำดับ)** และ **ชนะเมื่อมีครบทั้ง 2**
5. [ ] **ดรอปเฉพาะ HardSuccess**: `Regular` / `Failure` / `Fumble` ไม่ดรอป; ทอยแยกต่อชิ้น (A, B) และข้ามชิ้นที่มีอยู่แล้ว — มีครบทั้งคู่ = ไม่ทอย
6. [ ] **Hard มีโอกาสดรอปมากกว่า Normal** (ค่าเริ่มต้น Normal 5% · Hard 15% ต่อ HardSuccess → จริงต่อทริป ≈ 4.4% vs 8.9%) และปรับได้ใน Inspector
7. [ ] ยังไม่มีครบทั้ง 2 และวันที่เกิน deadline → **แพ้**
8. [ ] คลิกที่ DayFrame = จบวันทันที (ไม่มีปุ่มแยก, **ไม่มี confirm dialog**)
9. [ ] Cook/Craft: เลือกการ์ด 1 ใบ → คลิกเพิ่มจำนวนได้จน AP หมด → คลิกขวาล้างจำนวนโดยการ์ดยังเลือกอยู่
10. [ ] **Dispatch resolve ทันที** ไม่มีช่วงล็อก UI, ผลขึ้น log ในเฟรมเดียวกัน, ไม่มี `TripInProgress`
11. [ ] ตัวละครที่ dispatch แล้วเป็น **Busy** — ส่งซ้ำไม่ได้ แต่ **เลือกทำสกิลได้ถ้ามี AP เหลือ**
12. [ ] **End Day กดได้ทุกเมื่อ** และรีเซ็ต Busy + คืน AP เต็ม
13. [ ] Heal: เลือกผู้ทำ → กด Heal → เลือกผู้ป่วย (Hurt) → ยืนยัน · **ไม่มี queue/ตัวเลขบนปุ่ม** · กดไม่ได้เมื่อไม่มีคนบาดเจ็บ
14. [ ] ปุ่มสลับโหมด Heal (ยา ↔ AP) ทำงานและตัวเลข AP ที่แสดงตรงกับโหมด
15. [ ] ปุ่ม Dispatch ซ่อนเมื่อยังไม่ใช้งาน และ label เปลี่ยนตามสกิล
16. [ ] เลือกการ์ดในโหมดสกิลได้ **1 ใบ** และ panel สกิลแสดงตลอดเวลา
17. [ ] `ResourceStrip` ยังอยู่กลางบน
18. [ ] ไม่มีตัวเลข balance hard-code ใน UI — อ่านจาก Controller ทุกค่า
19. [ ] hierarchy ไม่มี object ของ flow เก่าค้าง (`BaseWorkPanel`, `EndDay button`, `MessageUi`, `ScoutIcon`)
20. [ ] **คลิกที่ `DayFrame` = จบวัน** (ไม่มีปุ่ม End Day แยก, ไม่มี confirm dialog)
21. [ ] ไม่มีถ้ำ (node) อยู่ใต้ panel ใด ๆ — ทุกใบมองเห็นและกดได้
22. [ ] ไม่มี console warning จาก missing script / glyph ที่ฟอนต์ไม่มี
23. [ ] **ทุก interactive element ผ่าน raycast จริง** — `EventSystem.RaycastAll` ที่กลาง element ต้อง hit element นั้น (ไม่ใช่ panel ข้างหลัง) · ตรวจด้วย `AgentScripts/TestUiRedesign.cs`
24. [ ] **ตอนเลือกคนไข้ คนที่รักษาได้ถูกไฮไลต์** — กรอบมินต์หนา alpha เต็มและเต้น ส่วนคนที่รักษาไม่ได้จาง และไฮไลต์หายทันทีที่จบการรักษา
25. [ ] **มีฟอนต์ pixel จาก tileset ที่ใช้ได้จริง** — `Assets/V2/Fonts/PixelTileset.asset` ครอบ A–Z / a–z / 0–9 / `% + -` + วรรคตอนที่ UI ใช้, มี fallback, และสร้างซ้ำได้จากเมนู `Tools > V2 > Rebuild Pixel Font`
26. [ ] **TMP ทุกตัวใน V2 UI ใช้ฟอนต์นี้** — รวม label ที่สร้างจากโค้ด (`TMP_Settings` default ชี้ PixelTileset) และไม่มีอักขระใดต้อง fallback · ตรวจด้วย `TestUiRedesign` 2 ข้อ
27. [ ] **ฟอนต์ใหม่ไม่ทำให้ layout แตก** — ขนาดตัวอักษร (cap height / line height) ตรงกับ LiberationSans เดิม และข้อความในแถบทรัพยากรไม่ตัดขึ้นบรรทัดใหม่
28. [ ] **กลุ่ม `Warning` เป็น parent/child ที่ตรงกัน** — CanvasGroup ตัวเดียวบน group root, backdrop กับ text stretch เต็ม group และข้อความอยู่กึ่งกลาง (inset เท่ากันทั้ง 4 ด้าน)
29. [ ] **สีบนแบนเนอร์ = สีใน log** ของข้อความเดียวกัน — ทั้งคู่อ่านจาก `UiPalette.For(LogKind)` และไม่มี enum/ตารางสีที่สอง
30. [ ] **การ์ดแสดง AP เป็นไอคอน ไม่ใช่ตัวเลข** — 1 pip ต่อ 1 AP: `ap_45` = ยังมี, `ap_30` = action ที่กำลังจะยืนยันจะกิน pip นี้, `ap_15` = ใช้ไปแล้ว · จำนวน pip ตรงกับ `maxActionPoints` และอัปเดตทันทีที่ AP เปลี่ยน
31. [ ] **พรีวิวต้นทุนอ่านจากระบบเจ้าของ** — Cave ที่เลือก (dispatch) หรือสกิลที่ queue ไว้ (cook/craft/heal) เป็นตัวกำหนดจำนวน pip สี `ap_30` และโหลด starvation ต่อคน (base + `starvationStacks`)
32. [ ] **`ap_30` แสดงเฉพาะการ์ดที่เลือก** — ยังไม่เลือกตัวละคร = ไม่มี pip ส้มบนการ์ดใดเลย
33. [ ] **ช่อง Core Item ใช้สไปรต์จริงตามคู่สีของ run** — กล่องส้ม↔กุญแจส้ม, กล่องเหลือง↔กุญแจเทา; ได้ครบทั้งคู่เมื่อไรช่องกล่อง**ยังเป็นกล่อง**แต่เปลี่ยนเป็นฝาเปิด (`_bar`) และกุญแจที่ได้เปิดกล่องได้เสมอ
34. [ ] **คริสตัลเด้งเป็น popup กลางจอ** เมื่อเปิดกล่องได้ — แสดงคริสตัลของคู่สีนั้น ไม่โผล่ใน HUD, ไม่เด้งซ้ำเมื่อโหลดซีนตอนชนะแล้ว, และคลิกที่ไหนก็ปิดได้

> **สถานะ AC (2026-10-28):** ข้อ 1–34 ผ่านการตรวจด้วยสคริปต์ใน play mode — ชุดรวม `AgentScripts/TestUiRedesign.cs` รันครั้งเดียว **ALL PASS 52 ข้อ** (§7.12–§7.13, §7.15–§7.21) · ฟอนต์ตรวจด้วย preview render และภาพหน้าจอ (§7.14–§7.15) · pip และ Core Item ตรวจด้วยภาพหน้าจอ (§7.19–§7.21)

---

## Related Documents
- [Unity V2 System Design](04-unity-system-design.md)
- [Unity V2 Class Diagram](05-unity-class-diagram.md)
- [Core Scene Hierarchy](06-unity-core-scene-hierarchy.md)
- [V2 Task Overview](../agile/user-stories/V2-TASK-OVERVIEW.md)
- [US-V2-UI-01 UI หลัก](../agile/user-stories/US-V2-UI-01.md) · [US-V2-FACTORY-01 โรงงาน](../agile/user-stories/US-V2-FACTORY-01.md)
- [GDD §5 Base Work](../gdd/01-mechanics.md) · [Art Direction §4 Layout](../gdd/03-art-direction.md)

---

## 8. Addendum 2026-10-30 — sprint review (16 ข้อ)

รอบนี้เปลี่ยนจาก "layout redesign" เป็นการลงรายละเอียด UI ตามสเก็ตช์ 4 รูป:

### 8.1 การ์ดตัวละคร (ภาพที่ 2)
```
+---------------------------+
| [state/dab]         [E]   |  ดาบ = พร้อมออกสำรวจ · E/N/H = ความถนัดหลัก (สีตามระดับถ้ำ)
|                     [tal] |  ไอคอน talent ใต้อักษร
|        portrait           |
+-------------+-------------+
| NAME        | [][]  lamps |  AP pip ซ้ายล่าง · buff/debuff 6 ดวงขวาล่าง
| [][][]      | [][]        |
+-------------+-------------+
```
- Buff/Debuff 6 ดวงแบบไฟหน้าปัด: Hurt / Hungry / trait ดี / trait เสีย / Blessed / Buff Potion
- สี: Hurt+trait เสีย = แดง · Hungry = ส้ม · trait ดี + Blessed = ฟ้า · Buff Potion = ม่วง · ยังไม่มี = เทา
- พื้นหลังการ์ด: Hurt = แดง (มาก่อน) → ถ้าหาย Hurt แล้วยังหิว = ส้ม
- Outline = สีความถนัดหลัก (Easy เขียว / Normal เหลือง / Hard แดง)
- อนิเมชันหยิบการ์ด: idle จมลง 70px → เลือกแล้วสไลด์ขึ้น (restingOffset, liftSpeed)

### 8.2 แมป + การ์ดดันเจี้ยน (ภาพที่ 3–4)
- 3 การ์ดเรียงตายตัว Easy | Normal | Hard (ไม่สุ่ม ไม่มีลำดับก่อน–หลัง)
- ลูกศรชี้เฉพาะใบที่เลือก
- การ์ดมี: ชื่อระดับ · pity x/y · sprite ถ้ำ (ใช้ภาพเดียวกันไปก่อน) · โอกาส (base → SV จริงเมื่อเลือกครบ) · AP · ไอคอนของที่ดรอป
  - **pity อยู่ใต้ชื่อ** ไม่ใช่ล่างสุด — การ์ดตัวละครที่ถูกยกขึ้นมาทับขอบล่างของการ์ดดันเจี้ยน ถ้า pity อยู่ล่างจะถูกบัง
  - **SV แสดงเป็นสี** ตาม ramp แดง (0) → ส้ม (40) → เหลือง (70) → เขียว (100) และเปลี่ยนสีทันทีเมื่อ SV เปลี่ยน
  - การ์ดที่เลือก: SV ขยายเป็น 34pt + สเกล 1.18× + แถบพื้นหลังอ่อนกว่าตัวการ์ด (ดู §8.10)
- Easy ไม่มีแถว pity

### 8.3 HUD
- ซ้ายบน: **MENU** และ **HELP** (Help = หน้าเปล่า รอ content)
- Core Item ย้ายจากซ้ายบนไปข้าง DayFrame (คำนวณตอนรันใน HudTopBar.RelocateCoreItemGroup)
- ปุ่มสลับโหมด **Craft** (Medicine / Buff Potion) แบบเดียวกับปุ่มสลับโหมด Heal

### 8.4 สิ่งที่ยังค้าง
- ทอยเต๋า DiceRollView ยังไม่ถูกต่อเข้า flow (ข้อ 15 ถูกเลื่อนไปทำ branch ทดสอบ)
- DayFrame ยังไม่ได้ย่อ (พื้นที่พอ) — ปรับได้ถ้า Core Item ทับ
- Help page ยังไม่มีเนื้อหา
- สไปรต์จริง: ดาบ/ไอคอนบัพ/ถ้ำ ใช้ placeholder ที่สร้างจากโค้ด (แทนได้ผ่าน field ใน Inspector)
### 8.5 Addendum รอบ 2 (2026-10-30)

| ข้อ | สิ่งที่แก้ |
|---|---|
| 1 | Player card **218×300** (สูงเท่า LogPanel) · CharacterHandPanel 936×300 · Dungeon card คำนวณจาก margin ≈**480×450** |
| 2 | อนิเมชันหยิบการ์ด: idle จม **70 px** → เลือกแล้วสไลด์ขึ้น (ความเร็ว 900 px/s) |
| 3 | CoreItemGroup อยู่คอลัมน์เดียวกับ DayFrame ใต้ลงมา (ระหว่าง DayFrame กับแถบสกิล) และย่อ scale ให้กว้างเท่า DayFrame |
| 4 | ปุ่มสลับโหมด Craft ย้ายเข้า CraftRow แบบเดียวกับ HealRow (156×64 + 56×64) |
| 5 | MainMenu ข้อความหาย: ตั้ง defaultFontAsset ให้ TMP Settings ทั้ง 3 ชุด + UiFactory.CreateText เรียก TmpFont.Apply |
| 6 | MENU → **OPTIONS** และเพิ่มปุ่ม **RETURN TO MENU** ในหน้า options |

> Overlay (Options/Help) ใช้ CanvasGroup แทน SetActive — overlay ที่ inactive ตอนสร้างจะไม่ถูก batch เข้า canvas (depth ค้าง -1) ทำให้การ์ดดันเจี้ยนบังทั้งการวาดและการคลิก

### 8.6 Addendum รอบ 3 (2026-10-30)

| ข้อ | สิ่งที่แก้ |
|---|---|
| 1 | **การ์ดเลื่อนทั้งใบ:** `Background` + `Frame0..3` ย้ายไปอยู่ใต้ `Lift` (เดิมอยู่ใต้ root) และ root `iconImage` เหลือแค่พื้นกดใส — พื้นหลัง/ขอบ/ข้อความจึงเลื่อนขึ้นพร้อมกัน · พื้นหลังการ์ดทำเป็นสีทึบ (เดิม `a = 0.90/0.95` ทำให้เห็นการ์ดดันเจี้ยนทะลุ) |
| 2 | `CharacterHandPanel` ยกขึ้น y **20 → 92** เพื่อให้การ์ดที่พัก (จม 70 px) ไม่หลุดขอบล่าง |
| 3 | การ์ดที่ยกขึ้นวาดเหนือการ์ดดันเจี้ยน (มือไพ่อยู่ canvas order สูงกว่าแมป) — เทสต์ยืนยัน draw order |
| 4 | **ดันเจี้ยนกินพื้นที่แนวตั้งเต็ม:** marginTop **300 → 110** · maxCardHeight **470 → 700** (ทั้งค่า default ในสคริปต์และ override ใน `Core.unity`) → การ์ด ≈**480×630** เริ่มราว 75 px จากขอบบน จบชิด LogPanel |
| 5 | CoreItemGroup เปลี่ยนจาก 1×2 (นอน) เป็น **2×1 (ตั้ง)** 118×240 (y = 0 / −122) ตัดข้อความ `CoreItemStatus` ออก และจัดขวาชิด DayFrame |

> `Run In Background` ของ Editor ต้องเปิดก่อนเก็บภาพ (`editor.playmode.set_run_in_background --enable true` — ต้องเรียกหลังเข้า play mode ทุกครั้ง) ไม่งั้น player loop หยุดที่เฟรม 2 และภาพ/อนิเมชันที่ได้จะเป็นภาพค้าง

### 8.7 หน้ากาก OPTIONS ใช้ร่วมกัน (รอบ 4)

หน้า options เดียวกันถูกใช้ทั้งใน `MainMenu` และในเกม — สร้างจาก `OptionsPanelView.Create(parent, primary, primaryAction, secondary?, secondaryAction?)`

| ส่วน | สเปก |
|---|---|
| โครง | `OptionsPanel` (จอทึบ คลุมเต็ม) → `Card` 820×520 → `Heading` "OPTIONS" |
| RESOLUTION | `ResolutionField` 340×48 (มี caret `>` หมุน −90°) + `ResolutionList` 340×204 เลื่อนได้; เลือกแถวแล้ว `Screen.SetResolution` ทันที |
| DISPLAY | `WindowedButton` / `FullscreenButton` — ปุ่มที่เลือกใช้สี `UiFactory.ChoiceSelected` |
| VOLUME | `VolumeSlider` 270×28 + ค่าเป็น % (เขียน `AudioListener.volume`) |
| Footer | ไม่ส่ง `secondary` → ปุ่มเดียว **BACK** กลางล่าง · ส่ง `secondary` → **RETURN TO GAME** (x −158) + **RETURN TO MENU** (x +158) ที่ y −206 |
| ค่าเก็บถาวร | `PlayerPrefs`: `clogged.resolution` / `clogged.fullscreen` / `clogged.volume` (คีย์เดิม ใช้ค่าเก่าได้) |
| ในเกม | `HudTopBar` ห่อด้วย `OptionsOverlay` + `CanvasGroup` (ไม่ใช้ `SetActive` — overlay ที่ inactive จะไม่ถูก batch) และเรียก `CloseResolutionList()` ทุกครั้งที่เปิด |

### 8.8 ชุด asset ที่สแกนไว้ (ยังไม่ import)

| ชุด | จำนวน | ขนาด | ที่ใช้ได้กับเกมนี้ |
|---|---|---|---|
| `kenney_board-game-icons` | 513 PNG | 64 / 128 px | **คุ้มที่สุด** — `d4..d20`, `dice_1..6`, `dice_3D`, `sword`, `shield`, `skull`, `campfire` (Cook), `flask_empty/half/full` (Craft/Heal), `resource_apple` (Food), `resource_wood/lumber/planks/iron` (Materials), `pouch` (inventory), `lock_open/closed` (Core Item), `hourglass` (day), `crown`/`skull` (win/lose), `structure_gate` (ถ้ำ), `tag_1..10` (เลขวัน), `target` (SV), `cards_fan`/`hand_card`/`card_lift` (มือการ์ด) |
| `kenney_game-icons` | 210 PNG (ขาว/ดำ) | 50 / 100 px | **HUD chrome** — `gear` (OPTIONS), `question` (HELP), `exitLeft/exitRight/door` (return to menu), `return` (return to game), `home`, `audioOn/Off`, `musicOn/Off`, `pause`, `save`, `locked/unlocked`, `warning`, `information`, `checkmark`, `cross`, `star`, `trophy` |
| `kenney_cursor-pack` | 182 PNG | 32 / 64 px | ไอคอนเครื่องมือ/มาสค์ — `tool_sword_a/b`, `tool_torch`, `tool_pickaxe`, `tool_wrench`, `lock`/`lock_unlocked`, `mark_question`, `target_round_a/b`, `door_enter/exit`, `cursor_help`, `cursor_menu`, `progress_*` (แถบ AP) |
| `kenney_cursor-pixel-pack` | 220 ไทล์ | 16 px | ไทล์พิกเซลล้วน (tilemap) — ใช้เป็นเมาส์/ลวดลาย pixel หรือ patter พื้นหลัง |

> ทุกชุดเป็น **CC0** (ใช้เชิงพาณิชย์ได้ ไม่ต้องให้เครดิต) · ไฟล์ `.zip` ที่วางไว้ใน `Assets/V2/Sprites/` ยังไม่ถูกแตกและไม่ถูก Unity ใช้ — ควรย้ายออกนอก `Assets` แล้ว import เฉพาะไฟล์ที่เลือก

### 8.9 ป๊อปอัปทอยเต๋า (รอบ 5)

`DiceRollView` สร้าง UI เองทั้งหมด (ไม่มี prefab) และถูกสร้างครั้งเดียวต่อซีนใน `RunBootstrap.Awake` ผ่าน `DiceRollView.EnsureExists()`

| หัวข้อ | สเปก |
|---|---|
| เลเยอร์ | GameObject มี **Canvas ของตัวเอง** (`overrideSorting`, `sortingOrder = 500`) + `GraphicRaycaster` — แผนที่อยู่คนละ canvas จึงต้องยกขึ้นมาเป็น modal จริง |
| การซ่อน/แสดง | **`CanvasGroup`** (alpha / blocksRaycasts / interactable) ไม่ใช้ `SetActive` — วัตถุที่ inactive ตอน canvas batch จะได้ `graphic.depth = -1` และ Graphic Raycaster ข้าม graphic พวกนี้ (ปุ่มกดไม่ได้) |
| Panel | 680×560 กลางจอ + dim ดำ 0.7 คลุมเต็มจอ (คลิก = ข้ามอนิเมชัน) |
| เนื้อหา | ชื่อโซน · SV + เงื่อนไขทอย (SV ≥ 100 ขึ้นข้อความ "always a success unless the dice fumble") · เต๋า percentile 2 ลูก (tens 00–90 / ones 0–9) · แถบผล Hard/Regular/Failure/Fumble + marker · legend บรรทัดเดียว (ตัดช่วงที่ว่างออก) · ผลรวม + สรุปของที่ได้ · ปุ่ม CONTINUE |
| Animation | tumble 1.5s → settle 0.25s → marker 0.8s → เผยผล (ทั้งหมด unscaled time) |
| เปิดผลทันที | `RevealImmediately()` — หยุด coroutine แล้วไปสถานะสุดท้าย (ใช้ในเทสต์ที่รันเฟรมเดียว) |
| เรียกจาก | `GatherDispatchPanel.OnDispatchClicked()` หลัง `ConfirmTrip()` (ผลถูกเก็บไว้ใน `pendingRoll` เพราะ event ยิงภายใน call เดียวกัน) |

> **ยกเลิกเลือกถ้ำ:** `GatherMapController.ConfirmTrip()` เรียก `ClearSelection()` หลัง `Dispatch()` — การ์ดดันเจี้ยนคลายไฮไลต์และ readout กลับไปเป็น base SV ทันทีที่กด DISPATCH

### 8.10 การ์ดดันเจี้ยน — ลำดับชั้น + สี SV + pop (รอบ 6)

| หัวข้อ | สเปก |
|---|---|
| ลำดับแนวตั้ง | `ชื่อ` (46) → `pity` (28, จองเสมอ) → `sprite ถ้ำ` (ยืดเต็มที่เหลือ) → `SV / AP` (44) → `ของที่ดรอป` (56) |
| เหตุผลที่ pity อยู่บน | การ์ดตัวละครที่ถูกเลือกจะยกขึ้นและทับ **ขอบล่าง** ของการ์ดดันเจี้ยน — pity เดิมอยู่ล่างสุดจึงถูกบังก่อนอย่างอื่น |
| สีของ SV | `MapNodeView.ColorForChance(int)`: `0` แดง `(0.91,0.30,0.30)` → `40` ส้ม `(0.95,0.58,0.24)` → `70` เหลือง `(0.93,0.84,0.30)` → `100` เขียว `(0.42,0.85,0.45)`; ค่าเกิน 100 clamp เป็นเขียว |
| อัปเดตสี | `RefreshReadout()` ถูกเรียกทุกครั้งที่ค่าเปลี่ยน (เลือกถ้ำ / เลือกยูนิต / ยกเลิก) จึงเปลี่ยนสีตาม SV ของทีมจริง |
| pop ตอนเลือก | ฟอนต์ SV **26 → 34**, `localScale` **1 → 1.18** (`PopSpeed = 6.5`/วินาที, unscaled), แถบพื้นหลัง `SelectedBackdrop (0.21,0.25,0.35)` alpha `0 → 0.95` |
| Pivot ของ SV | `(0.5, 0.5)` เพื่อให้ pop ขยายออกสองข้างเท่ากัน (pivot เดิมเป็น `(0.5, 1)` จะขยายลงไปทับแถวของที่ดรอป) |
| แถบพื้นหลัง | ต้องเป็น **sibling ที่สร้างก่อน** ข้อความ — Unity UI วาด graphic ของ parent ก่อนลูกเสมอ ถ้าเป็นลูกจะทับตัวเลข |
| `SnapPop()` | กระโดดไปสถานะสุดท้ายทันที ใช้ในเทสต์ที่รันทุกอย่างในเฟรมเดียว (เหมือน `DiceRollView.RevealImmediately()`) |

### 8.11 การ์ดดันเจี้ยน — บีบโซนล่าง + ย่อปุ่ม DISPATCH (รอบ 7)

**ตัวเลขที่วัดได้จริงจาก runtime (canvas 1920×1080)** — สถานะสุดท้ายหลังปรับรอบ 7.1

| องค์ประกอบ | ตำแหน่ง world |
|---|---|
| การ์ดดันเจี้ยน (ทั้งสามใบ) | `y[330..970]`, **สูง 640** (กลับมาเท่าเดิมก่อนบีบ) |
| `Title` / `Pity` / `Cave` / `Chance` / `Drops` | `y[916..962]` / `y[886..914]` / `y[526..880]` / `y[480..520]` / `y[428..474]` |
| การ์ดตัวละคร (ตอนพัก) | root `y[92..392]`, `Lift.y = −70` → เนื้อการ์ดอยู่ `y[22..322]` |
| **การ์ดตัวละคร (ตอนถูกเลือก)** | `y[83..401]` (`Lift.y = 0`, `localScale = 1.06`) |
| ปุ่ม DISPATCH | `x[1666..1896] y[24..176]` (230×152) |
| มือการ์ด | `x[724..1660] y[92..392]` |

**เส้นตาย:** ข้อความของดันเจี้ยนต้องมี `y_bottom ≥ 401` — พื้นหลัง/ภาพถ้ำถูกบังได้ แต่ label ห้ามถูกบัง
ตอนนี้ `Drops` ล่างสุด = **428** → clearance 27 px

| การเปลี่ยนแปลง | เดิม → ใหม่ | ไฟล์ |
|---|---|---|
| `oddsBand` | 44 → **40** | `MapNodeView.cs` |
| `dropBand` | 56 → **46** | `MapNodeView.cs` |
| ช่องว่างระหว่างแถบ | 10 / 8 → **6 / 6** | `MapNodeView.cs` |
| `bottomSafeBand` (แถบตายใต้ `Drops`) | — → **98** (ระยะจากขอบล่างการ์ดถึง `Drops`) | `MapNodeView.cs` |
| `marginBottom` | **คงไว้ 330** (ทดลอง 420 แล้วย้อนกลับ — ทำให้การ์ดเตี้ยลง 90 px โดยไม่จำเป็น) | `GatherMapController.cs` **และ** `Core.unity` |
| ขนาดปุ่ม DISPATCH | 300×200 → **230×152** | `DispatchActionPanel.prefab` **และ** override `m_SizeDelta` ใน `Core.unity` |
| Icon ของปุ่ม | 120×120 @ y=12 → **80×80 @ y=10** | `DispatchActionPanel.prefab` |
| Label ของปุ่ม | 44 สูง @ y=8 → **40 สูง @ y=6** | `DispatchActionPanel.prefab` |
| `LogPanel` → `Content` | `m_ChildControlHeight 0 → 1` | `LogPanel.prefab` |
| `EntryTemplate` → `LayoutElement.m_PreferredHeight` | 24 → **−1** | `LogPanel.prefab` |

**วิธีคิดของ `bottomSafeBand`** — เลขทั้งชุดมาจากการวัด ไม่ใช่การเดา:

```
ขอบล่างการ์ด (world)            = 330
การ์ดตัวละครที่ยกขึ้นไปถึง (world) = 401
ระยะที่ต้องเว้น                  = 401 − 330 = 71
+ clearance 27                  = 98  ← bottomSafeBand
```

ผลข้างเคียงที่ตั้งใจ: พื้นที่ 98 px ล่างสุดมีแต่พื้นหลังการ์ดกับ highlight — **ไม่มีข้อความ** จึงถูกการ์ดตัวละครทับได้ตามที่ต้องการ
และเพราะ `LayoutCard()` วัดจากขอบบน/ล่างของความสูงการ์ด ภาพถ้ำจึงยังได้พื้นที่เท่าเดิม (`caveBand 354`)
ส่วน 90 px ที่ได้คืนมาจากการย้อน `marginBottom` ไปลงในแถบตายทั้งหมด ไม่ไปดันข้อความขึ้น

**กับดักที่เจอรอบนี้**

1. **Prefab ที่แก้แล้วไม่พอ** — ค่าที่ใช้จริงมาจาก **override ฝั่งซีน** (`m_Modifications` → `m_SizeDelta` ของ `fileID 958683726539179671`) แก้ prefab อย่างเดียวขนาดยังเป็น 300×200 ตอนรัน → ต้องแก้ทั้งสองที่
2. **`marginBottom` มี override ในซีน** — ค่า default ในสคริปต์ (`[SerializeField]`) ไม่มีผลกับ instance ที่ถูก serialize ไว้แล้ว
3. **`ChildControlHeight = 1` อย่างเดียวไม่พอสำหรับ log** — ยังมี `LayoutElement.m_PreferredHeight: 24` บน `EntryTemplate` ที่ปีนแถวทุกแถวไว้ที่ 24 px ทำให้บรรทัดที่ wrap 2 บรรทัดถูกตัด/ทับ ต้องตั้งเป็น **−1** (ค่า −1 = "ไม่บังคับ" ใน `LayoutElement`) ให้ layout ใช้ preferred height ของ TMP แทน
4. **หลังแก้ `.prefab`/`.unity` ด้วยมือระหว่างที่ Editor เปิดอยู่ ต้องเรียก `AssetDatabase.Refresh`** ก่อน `scene.open` — ถ้าไม่ refresh, Unity จะยังใช้สำเนาในหน่วยความจำและดูเหมือน "แก้แล้วไม่ติด" (รอบนี้เจออาการนี้กับ `LogPanel.prefab`: เทสต์จับได้ว่าแถวยังสูง 24 px)
5. **Editor ค้างหลัง `script.recompile`** — main thread ไม่ตอบคำสั่งใด ๆ (`editor.status` timeout) แต่ `console.status` ยังตอบเพราะมาจาก cache; ทางออกคือปิด process แล้ว `unity open` ใหม่ (ไม่เสียข้อมูลเพราะแก้ไฟล์บนดิสก์อยู่แล้ว)
6. **เทสต์ที่วัด layout ต้องวัดหลัง `Canvas.ForceUpdateCanvases()`** — ไม่งั้นจะอ่านค่าเก่าและรายงาน PASS หลอก ๆ

**เทสต์ที่ป้องกันปัญหานี้แล้ว** (`TestUiRedesign`, ALL PASS 111)

| เทสต์ | กันอะไร |
|---|---|
| `no dungeon label hides under a lifted card` | ไล่ทุก label × ทุกการ์ดที่ยกขึ้น แล้วเทียบ `y_bottom` กับ `liftTop` |
| `the dungeon card keeps its full height` | การ์ดต้องสูง ≥ 620 (กันการเผลอย่อการ์ดเพื่อหนีปัญหา) |
| `the dispatch button clears the character cards` | `Rect.Overlaps` ระหว่าง `DispatchActionPanel` กับทุกการ์ด |
| `a wrapped log line gets the height it needs` | เทียบ `row.rect.height` กับ `TMP.preferredHeight` |

### 8.12 ไอค่อนจริงบนปุ่มและ state (รอบ 8)

**แผนที่ไอค่อน** — ทุกตัวเป็น PNG ขาวบนโปร่งใส 16×16 (tint ได้ด้วย `Image.color`)

| ใช้ที่ | ไฟล์ | ต้นทาง (CC0) | ที่ผูก |
|---|---|---|---|
| OPTIONS | `ui_gear.png` | Kenney *Game Icons* → `White/1x/gear.png` | `RunBootstrap.optionsIcon` → `HudTopBar` |
| HELP | `ui_question.png` | Kenney *Game Icons* → `White/1x/question.png` | `RunBootstrap.helpIcon` → `HudTopBar` |
| READY state (มุมซ้ายบนการ์ด) | `state_sword.png` | Kenney *Board Game Icons* → `Default (64px)/sword.png` | `ScoutIcon.swordSprite` |
| ปุ่ม DISPATCH (ยืนยันทริป) | `state_sword.png` | ตัวเดียวกับ READY | `DispatchActionPanel.dispatchIcon` |
| ปุ่ม COOK / HEAL / CRAFT | ตามสกิล | `SkillCandidates/cook_a_stove_arch`, `heal_a_cabinet`, `craft_a_workbench` | `SkillFlowController` ส่ง `SkillTriggerPanel.*Icon` เข้า `Show()` |

**ทำไมต้องวาดใหม่เป็น 16×16 ไม่ใช้ไฟล์ต้นฉบับ**
ต้นฉบับเป็น 50×50 / 64×64 ที่มี anti-alias (alpha 51–200 ตามขอบ) เมื่อตั้ง Point filter จะได้ขอบเป็นขั้นบันไดสีเทา
ซึ่งขัดกับชุดไอค่อนเดิมของโปรเจกต์ที่เป็น 1-bit แท้ (`ap_45` 10×12, `heal_a_cabinet` 16×16)
การ trace ลงกริด 16×16 จึงทำให้ไอค่อนใหม่ "หน้าเดียวกัน" กับของเดิม — ขนาดแสดงผลจริงคือ 30 px (ช่อง state) และ 20 px (ปุ่ม top bar)

**สนามที่ต้องคำนวณก่อนใส่ไอค่อนในปุ่ม**

```
ปุ่ม 136 × 38
├─ gutter ซ้าย 6 + ไอค่อน 20        = 26
└─ label กว้าง 136 − 26 − 4 (ขวา)   = 106   ← ต้อง ≥ ความกว้าง "OPTIONS" ที่ 22pt (วัดได้ 103.2)
```

ถ้าลืมข้อนี้ label จะถูกตัดเป็น `OPT ION` (เจอจริงตอนปุ่มยังกว้าง 92) — เทสต์ `... label fits beside its icon`
วัดที่ **font size ที่ออกแบบ** (ปิด auto-size ชั่วคราวก่อนวัด) ไม่ใช่วัดหลัง auto-size หดแล้ว
เพื่อไม่ให้เทสต์ผ่านทั้งที่ปุ่มแคบเกิน

**ไอค่อนบนปุ่ม DISPATCH/CONFIRM**

| สถานะ | label | ไอค่อน |
|---|---|---|
| ยังไม่มีอะไรให้ยืนยัน | — (ซ่อน) | — |
| เลือกถ้ำ + การ์ดแล้ว | `DISPATCH` | `state_sword` |
| COOK / CRAFT มีคิว | `COOK` / `CRAFT` | ไอค่อนสกิลนั้น |
| HEAL มีคนเจ็บเป็นเป้า | `HEAL` | ไอค่อน heal |

ไอค่อนเดิมใน prefab เป็น `m_Color.a = 0.1` + `m_Sprite: {fileID: 0}` จึงมองไม่เห็นตลอด —
รอบนี้ตั้ง alpha `1`, `m_PreserveAspect: 1`, `m_RaycastTarget: 0` และย่อจาก 80×80 เป็น **64×64 ที่ y = 23**
(anchor กลางของพาเนล 230×152 → center_y = 76 + 23 = 99 → กล่อง `y[67..131]`, label อยู่ `y[6..46]` จึงไม่ทับ)

### 8.13 ที่เก็บ third-party art pack (รอบ 9)

```
clogged/
├─ asset-sources/                        ← git-ignored (raw downloads)
│  ├─ kenney_board-game-icons.zip        ← 4 zip ที่ย้ายออกจาก Assets
│  ├─ kenney_cursor-pack.zip
│  ├─ kenney_cursor-pixel-pack.zip
│  ├─ kenney_game-icons.zip
│  ├─ kenney_input-prompts-pixel*.zip
│  └─ _extracted/
│     ├─ board-game-icons-vector/        ← 255 SVG (+ .ai/.swf) ย้ายออกจาก Assets
│     ├─ cursor-pack-vector/             ← 364 SVG
│     └─ game-icons-vector/              ← 2 SVG + .ai + .swf
│
└─ Unity-Projects/Assets/V2/Sprites/
   ├─ Icons/                             ← **track ใน git** (เกมใช้จริง)
   ├─ InputPrompts/
   └─ Packs/                             ← **git-ignored** 1,890 PNG / 4.63 MB
      ├─ board-game-icons/   513 png
      ├─ cursor-pack/        729 png
      ├─ cursor-pixel-pack/  223 png
      └─ game-icons/         425 png
```

**ทำไม Packs ไม่ track:** Unity import PNG ทุกไฟล์เป็น asset (สร้าง `.meta` + เข้า texture database) ถ้า commit ไว้จะได้ diff ~1,890 ไฟล์ต่อการ bump เวอร์ชันแพ็ก และ repo บวมขึ้นโดยไม่ได้อะไร เพราะไอค่อนที่ใช้จริงถูก **trace/copy ลง `Icons/`** แล้ว (16×16, 1-bit) ซึ่งเป็นไฟล์เดียวที่ component อ้างถึง

**asset hierarchy note:** `Packs/` เป็น Texture2D ปกติจะใช้กับ `Image.sprite` ไม่ได้ เพราะโปรเจกต์อยู่โหมด 3D — `Assets/V2/Editor/PacksSpriteImporter.cs` แก้ให้เฉพาะโฟลเดอร์นี้:

| setting | ค่า | เหตุผล |
|---|---|---|
| `textureType` | `Sprite` | ให้ลากใส่ `Image.sprite` ได้ |
| `spriteImportMode` | `Single` | แพ็กเป็นไฟล์เดี่ยว ไม่ใช่ sheet |
| `spritePixelsPerUnit` | `16` | ตรงกับ `Icons/` ที่ใช้อยู่ |
| `filterMode` | `Point` | pixel art ห้าม blur |
| `mipmapEnabled` | `false` | ไม่มีระยะไกลให้ sample |
| `maxTextureSize` | `512` | ต้นฉบับใหญ่สุด 100 px ไม่ต้องเผื่อ 2048 |

**ผลลัพธ์ที่ตรวจแล้ว:** `AssetDatabase.LoadAssetAtPath<Sprite>("…/Packs/game-icons/PNG/White/1x/gear.png")` คืน sprite ปกติ, `FindAssets("t:Texture2D", Packs)` = **1,890**

### 8.14 ดาบจากแพ็ก + ปุ่มไอค่อนล้วน (รอบ 10)

**ดาบ**

| | ก่อน | หลัง |
|---|---|---|
| ไฟล์ | `state_sword.png` (วาดเอง ดาบตั้ง) | `state_sword.png` (**ดาบเฉียงจาก `board-game-icons`**) |
| สี | ขาวล้วน (silhouette) | **ขอบดำ + เนื้อขาว** (2 โทน ตามภาพต้นฉบับ) |
| ขนาด | 16×16 | 16×16 (คงเดิม) |
| GUID | `2d529982be1e4057b63272a9a3f6534d` | **ไม่เปลี่ยน** → prefab/scene ไม่ต้องแก้ |

การเลือก 2 โทนแทน silhouette เพราะทดลองแล้ว: ถ้ารวมขอบกับเนื้อเป็นขาวล้วน รูปร่างจะกลายเป็นก้อนทึบ
อ่านไม่ออกว่าเป็นดาบ (เทียบภาพ 8× แล้ว) — แบบขอบดำ+เนื้อขาวอ่านเป็นดาบได้ทั้งที่ 30 px (ช่อง state) และ 64 px (ปุ่ม DISPATCH)

**ปุ่มมุมซ้ายบน**

| | ก่อน | หลัง |
|---|---|---|
| ขนาด | 136 × 38 (แนวนอน) | **48 × 48 (จัตุรัส)** |
| ข้อความ | `OPTIONS` / `HELP` | **ไม่มี Label เลย** |
| ไอค่อน | 20×20 ชิดซ้าย (gutter 6) | **28×28 anchor กลาง** |
| ตำแหน่ง | `x = 16` และ `16 + 136 + 8` | `x = 16` และ `16 + 48 + 8` |

`BuildButton(..., string label, ...)` ใช้ `label` เป็นตัวตัดสินโหมด:

```
label == null/"" → icon-only : icon anchor (0.5,0.5), pivot (0.5,0.5), pos (0,0), size 28
label != null    → มีข้อความ  : icon anchor (0,0.5), pos (gutter,0), size 20 + Label inset ซ้าย 26 ขวา 4
```

ปุ่ม `CLOSE` ในหน้ากดตัวเลือกยังใช้โหมดมีข้อความ (ไม่มีไอค่อน) ตามเดิม — ไม่กระทบ

**เทสต์ที่ครอบคลุมแล้ว** (`TestUiRedesign`, ALL PASS 123)

| เทสต์ | กันอะไร |
|---|---|
| `the OptionsButton shows the ui_gear sprite` | สไปรต์ถูกผูกจริง (ไม่ใช่ `PlaceholderIcons`) |
| `the OptionsButton has no text label` | ไม่มี Label หลงเหลือ |
| `the OptionsButton frame is square` | ผลต่าง `sizeDelta.x` กับ `sizeDelta.y` น้อยกว่า 0.5 |
| `the OptionsButton icon is centred in its frame` | anchor/pivot กลาง + `anchoredPosition == 0` + icon ไม่ใหญ่กว่ากรอบ |
### 8.7 Addendum รอบ 4 (2026-11-02) — AP บนปุ่มยืนยัน + tooltip ทั่ว HUD

| ข้อ | สิ่งที่เพิ่ม |
|---|---|
| 1 | **ปุ่มยืนยันโชว์ AP ที่ใช้จริง:** `DispatchActionPanel` มีบรรทัดที่สอง `Cost` (`"N AP"`) ใต้ label; `SkillFlowController.PendingActualApCost` คิดรวม starvation surcharge แล้วส่งเข้า `Show(...)` — ค่า 0 = ซ่อนบรรทัด (gather ใช้ `config.apCost`) |
| 2 | **tooltip ใช้ตัวเลขสด:** `HudTooltip` + `HudTooltipTrigger` รับ `Func<HudTooltipContent>` แล้วสร้างเนื้อหาใหม่ทุกครั้งที่ hover — ค่า AP/วัสดุ/สต็อกที่เขียนไว้ในโค้ดจึงไม่มีทางค้างเก่า (เดิม Heal เขียนตายว่า "AP ใช้ 2 AP" ทั้งที่ค่าใหม่คือ 3) |
| 3 | **ครอบทุกส่วนของ HUD:** แถบทรัพยากร (Food / Medicine / Cooking / Materials + Core Item สองช่อง), Day frame (`EndDayTrigger`), ปุ่ม OPTIONS / HELP, LOG header, การ์ดดันเจี้ยน 3 ใบ, การ์ดตัวละครทั้ง 4 ใบ, ปุ่มสกิลทั้ง 4 ปุ่ม และปุ่มยืนยัน |
| 4 | **`HudTooltipTrigger.Attach()`** ใช้กับ element ที่ `raycastTarget = false` ได้ — ถ้าไม่มี Graphic ที่รับ raycast ในตัวมัน จะเปิด `raycastTarget` ของ Graphic เดิม (หรือเติม `Image` โปร่งใส) ให้เอง คลิกจึงยัง bubble ขึ้นไปถึงปุ่มแม่ |
| 5 | **Hard Success = ⌊SV ÷ 3⌋** (เดิม ÷2) — เพิ่ม `GatherResolver.HardSuccessThreshold()` ให้ HUD/test อ่านค่าตัวเดียวกับที่ใช้ทอย |

**เทสต์ที่ครอบคลุมแล้ว** (`TestUiRedesign`, ALL PASS 132)

| เทสต์ | กันอะไร |
|---|---|
| `hard success is the top third of the value (75 -> 25, 55 -> 18)` | สูตร ÷3 ไม่หลุดกลับไป ÷2 |
| `heal tooltip quotes the live AP-only cost (3 AP)` | tooltip อ่านจาก `BaseWorkController` ไม่ใช่ข้อความตายตัว |
| `cook tooltip quotes the live material cost` | วัสดุต่อรอบมาจาก controller |
| `every skill button carries a hover popup` | ปุ่มสกิลทั้ง 4 (รวมปุ่มโหมด) มี `HudTooltipTrigger` |
| `the top readouts carry popups` / `the day frame carries a popup` / `the confirm button carries a popup` | HUD ส่วนบน/วัน/ปุ่มยืนยันผูก tooltip แล้ว |
| `dungeon and unit cards carry popups` | การ์ดทุกใบที่สร้างจากโค้ดผูก tooltip ตอน `Initialize` / `Bind` |
| `confirm button paints '1 AP' for the queued cook` | ตัวเลขบนปุ่มยืนยัน = `PendingActualApCost` จริง |

### 8.8 Addendum รอบ 5 (2026-11-02) — tooltip แยกย่อยรายองค์ประกอบ

| ข้อ | สิ่งที่แก้ |
|---|---|
| 1 | **ขนาด popup:** `HudTooltip` กว้าง **360 px** (เดิม 290), title 21 / body 19 / hint 16 (เดิม 16/15/14), padding 14 |
| 2 | **การ์ดตัวละคร:** ถอด tooltip ก้อนเดียวที่ root ออก แล้วผูกแยกที่ `AptitudeBadge` (+ `AptitudeLetter`), `TalentIcon`, `StateIcon`/`statustest`, `Lamps/Lamp0..5` และ `ApPips/ApPipN` |
| 3 | **การ์ดดันเจี้ยน:** ผูกแยกที่ `Title`, `Chance`, `Pity`, `Drops/Drop0..2` + `DropLabel0..2` — ไม่มี tooltip ที่ root แล้ว |
| 4 | **แถบสถานะ:** คงการผูกแยกต่อทรัพยากร (Food / Medicine / Cooking / Materials) และ Core Item สองช่อง |
| 5 | **OPTIONS / HELP:** เอา tooltip ออกตามคำขอ |
| 6 | **เนื้อหาเป็น live ทุกอัน:** provider อ่าน `unit`, `shownChance`, `shownPity`, `shownConfig`, `RunResources` ตอน hover — ตัวเลขไม่มีทางค้าง |

> การเปิด `raycastTarget` ให้ badge/lamp/drop label ไม่ได้กันการคลิกการ์ด เพราะ click bubble ขึ้นไปถึง handler ที่ root — ยืนยันด้วย `EventSystem.RaycastAll` + `ExecuteEvents.GetEventHandler`

**เทสต์ใหม่ (`TestUiRedesign`, ALL PASS 141)**

| เทสต์ | กันอะไร |
|---|---|
| `OPTIONS and HELP stay tooltip-free` | ไม่มี tooltip หลงเหลือบนสองปุ่มนี้ |
| `no card-level aggregate popup` | ไม่มี tooltip ก้อนเดียวที่ root ของการ์ด |
| `unit-card badges each explain themselves` | aptitude / talent / state มี trigger ของตัวเอง |
| `all six unit lamps explain themselves` | lamp ครบ 6 ดวงมีคำอธิบาย |
| `unit AP pips explain themselves` | pip ทีละเม็ดมีคำอธิบาย |
| `dungeon card readouts each explain themselves` | title / SV / pity / drop มี trigger ของตัวเอง |
| `success-value popup quotes the hard-success threshold` | เนื้อหาอ้างเกณฑ์ ÷3 จริง |
| `the popup is large enough to read` | คอลัมน์กว้าง ≥ 340 px |

### 8.9 Addendum รอบ 6 (2026-11-02) — แท่งทอยเต๋าตามกฎ ÷3

| ข้อ | สิ่งที่แก้ |
|---|---|
| 1 | `DiceRollView.BuildBar()` เลิกใช้ `successValue / 2` แล้วเรียก `GatherResolver.HardSuccessThreshold()` — แท่งสีและ legend จึงตรงกับ `Roll()` เป๊ะ (เดิมค้างที่ 1/2 ตั้งแต่เปลี่ยนกฎใน v0.35) |
| 2 | ตัวอย่าง SV 75: `HARD 1-25 · REGULAR 26-75 · FAILURE 76-99 · FUMBLE 100` (เดิม `HARD 1-37 · REGULAR 38-75`) |
| 3 | เพิ่ม `CurrentSuccessValue` ให้ตรวจย้อนได้ว่าแท่งที่วาดตรงกับผลที่ resolver ใช้ |

**เทสต์ใหม่** (`TestUiRedesign`, ALL PASS 142): `the dice bar paints Hard Success from the top third` — เทียบข้อความ legend กับ `HardSuccessThreshold(CurrentSuccessValue)`

### 8.10 Addendum รอบ 7 (2026-11-02) — ปรับระบบตามสเปก help

ตรวจสเปก "วิธีเล่น / ทรัพยากร / ยูนิต / สถานะ / ดันเจี้ยน / Skill / SV" เทียบกับโค้ดจริง แล้วแก้เฉพาะข้อที่ตกลง:

| ข้อ | สเปก | ก่อนแก้ | หลังแก้ |
|---|---|---|---|
| 1 | Fumble → ทุกคน Hurt + trait เสีย | Hard: สุ่ม 1 คนหายตัว · Easy/Normal: ไม่มีผล | **ทุกคนที่ไป Hurt + trait เสียคนละ 1 ทุกถ้ำ** |
| 2 | สถานะชื่อ Death | `UnitState.Missing` / UI "LOST SIGNAL" / ตัว `X` | `UnitState.Death` / "DEATH" / ตัว `D` / log "died." (ค่า enum ยังเป็น 4 → save เดิมใช้ได้) |
| 3 | Normal→Key, Hard→Treasure | ทอยได้ทั้ง 2 ชิ้นทุกโซน | **คงเดิมตามที่ตกลง** (help ต้องเขียนว่า "โอกาสได้ชิ้นที่ยังขาด" ไม่ผูกกับโซน) |
| 4 | Sword เป็นทรัพยากร | ไม่มี — Sword คือไอคอน READY | **คงเดิม** (help ต้องไม่เขียนว่า Sword เป็นทรัพยากร) |

ผลข้างเคียงที่ตั้งใจ: ไม่มีการทอยใดฆ่ายูนิตแล้ว — `Death` มาจาก starvation stack ที่ 3 เท่านั้น (ตรงกับสเปก "สภาวะของยูนิต")

**เทสต์ใหม่** (`TestUiRedesign`, ALL PASS 146): `a fumble hurts every unit on the trip` · `a fumble hands a bad trait to each unit` · `a fumble grants no loot` · `a fumble never kills`

### 8.11 Addendum รอบ 8 (2026-11-02) — หน้า HOW TO PLAY

**ทำไมเป็น rail + pane ไม่ใช่หน้าเดียว:** เนื้อหาตามสเปกมี 8 กลุ่ม (~45 หัวข้อ) ถ้าใส่เป็นบล็อกเดียวจะยาวเกิน 2 หน้าจอและอ่านไม่ไหว จึงแยกเป็นหมวดทางซ้าย (เห็นทั้งหมดในสายตาเดียว) และเปิดทีละหมวดทางขวา — คนใหม่กด "BASICS" อ่าน 7 บรรทัดจบ ไม่ต้องเลื่อนหาที่ตัวเอง ตำแหน่งหมวดไม่เปลี่ยนทำให้กดซ้ำได้เร็ว

| ส่วน | ค่า |
|---|---|
| การ์ด | 1200 × 720 กลางจอ, พื้น `UiFactory.Panel`, ขอบ `Border` 2px |
| หัวเรื่อง | "HOW TO PLAY" 40px `Accent` ซ้ายบน + ตัวนับ `N / 8` ขวาบน + ปุ่ม `CLOSE` 160×48 |
| Rail | กว้าง 300, เยื้อง 16 ซม./ขวา, `VerticalLayoutGroup` spacing 6, ปุ่มสูง 52 (ที่เลือก = `ChoiceSelected` + ตัวอักษรขาว) |
| Pane | `RectMask2D` + `ScrollRect` (vertical, clamped), inset L 352 / R 52 / B 24 / T 140 |
| Scrollbar | track 8×`PaneHeight` ชิดขวา 26, handle ขาว 35%, `AutoHide` |
| แถว | `RowColor` + แท่งสีซ้าย 5px (`ignoreLayout`), หัวข้อ 22px / เนื้อ 19px, `ContentSizeFitter` ให้สูงตามข้อความ |

**หมวดทั้ง 8:** BASICS · RESOURCES · UNITS · STATES · DUNGEONS · SKILLS · SUCCESS VALUE · THE ROLL
ทุกตัวเลข (AP, วัสดุ, SV ฐาน, deadline, stock, เกณฑ์ Hard Success) อ่านจาก controller ตอน `BuildSections()` — หน้า help จึงไม่มีทางขัดกับของจริง

> Copy เป็นภาษาอังกฤษพิมพ์ใหญ่ทั้งหมด เพราะฟอนต์ PixelTileset มีเฉพาะ A-Z / 0-9 / เครื่องหมายจำนวนจำกัด (เทสต์ "every printed character has a glyph" ครอบคลุมข้อความในหน้านี้ด้วย)

**เทสต์ใหม่** (`TestUiRedesign`, ALL PASS 156): `help starts hidden` · `HELP opens the how-to-play page` · `the help page is a real page, not placeholder text` · `the help page has a section rail` · `the help page has a scrolling pane` · `the help page numbers its sections` · `the skills section quotes the live cook cost` · `the skills section quotes the live AP-only heal cost` · `help rows are laid out in a growing column` · `CLOSE hides the help page`

### 8.12 Addendum รอบ 9 (2026-11-02) — ดาบเป็นทรัพยากรของ dispatch + ไอค่อนในหน้า help

**ดาบเปลี่ยนจาก "ของประดับการ์ด" เป็น "ตั๋วเข้าถ้ำ":** เดิมการ์ดโชว์ดาบตลอดแต่ไม่ได้กันอะไร และการกันทริปที่ 2 มาจาก flag `gatheredToday` — ตอนนี้ยุบเหลือค่าเดียวคือ `currentSwords` (1/วัน, รีเฟรชพร้อม AP) แล้วให้ `HasGatheredToday` เป็นค่าอนุพันธ์ (`currentSwords <= 0`) โค้ดเก่าทุกจุดจึงยังทำงานโดยไม่ต้องแก้ และไม่มีทางที่ flag กับดาบจะขัดกันเอง

| ส่วน | ก่อน | หลัง |
|---|---|---|
| ตัวกันทริปที่ 2 | `gatheredToday` (bool) | `currentSwords <= 0` |
| `CanBeSentGathering` | `Idle && !HasGatheredToday` | `Idle && HasSword` |
| การหักตอน dispatch | `MarkGatheredToday()` | `TrySpendSword()` (−1 ต่อคน) |
| Badge บนการ์ด | โผล่เฉพาะตอนไปแล้ว | โผล่ตลอดตอน Idle, **จาง alpha 0.28** เมื่อใช้แล้ว |
| ปุ่มยืนยัน | `N AP` | `N AP + M SWORD(S)` |
| งานในฐาน | ไม่กินดาบ | ไม่กินดาบ (เหมือนเดิม) |

**ไอค่อนในหน้า help:** `Entry` เพิ่ม `WithIcon(Sprite)` และ `WithGlyph(string)` → `BuildMark()` วาง badge 28×28 หน้าข้อความ; `Rebuild()` เรียกทุกครั้งที่เปิดหน้า เพื่อดึง sprite สดจาก `StatusBarView` / `UnitIconView` / `MapNodeView` / `DispatchActionPanel` (หน้า help สร้างครั้งเดียวตอน `Awake` ไอค่อนจึงต้อง re-resolve ไม่ใช่ผูกตอน build)
- ไอค่อนจริงใช้ **สีเดิมของ art** (ไม่ tint ขาว) เพราะเป็นภาพ pixel art สีสันในตัว
- สถานะที่ไม่มี art (SWORD SPENT) ใช้ glyph ตัวอักษรแทน — เลือก `W`/`0` เพราะฟอนต์ PixelTileset ไม่มี `*`

**ลบแถว "NO SOLO PENALTY":** ผู้ใช้ไม่ต้องการให้บอกกติกานี้กับผู้เล่น (ระบบยังคิดโบนัส duo/mismatch ตามเดิม แต่ไม่ประกาศใน help)

**เทสต์ใหม่:** `a trip spends the unit's sword` · `CheckSwordRules` (ดาบ 1 เล่ม/วัน, ทริปที่ 2 ถูกปฏิเสธ, งานในฐานยังทำได้, รีเฟรชตอนวันใหม่, backend ปฏิเสธเอง) · ดาบบนการ์ดจางลงหลังใช้ · `StateInitial` ตัดพารามิเตอร์ `gatheredToday` ที่ไม่ใช้แล้ว → **ALL PASS 163**

### 8.13 Addendum รอบ 10 (2026-11-02) — หน้า Options แยกช่องเสียง 3 ช่อง + Mixer

**ทำไมต้องเป็น mixer ไม่ใช่ `AudioListener.volume`:** ของเดิมมีสไลเดอร์ `VOLUME` อันเดียวที่หมุน `AudioListener.volume` ทั้งเกม ทำให้ปรับเพลงเบาไม่ได้เลยถ้าไม่อยากเสียเสียงเอฟเฟกต์ ตอนนี้เสียงแยกเป็น 3 กลุ่มใน `GameMixer` (Master → Music / SFX) และหน้า Options มีสไลเดอร์ต่อกลุ่ม

| ส่วน | ก่อน | หลัง |
|---|---|---|
| Mixer | ไม่มี | `Assets/V2/Audio/GameMixer.mixer` — Master / Music / SFX, expose `MasterVolume` / `MusicVolume` / `SfxVolume` |
| สไลเดอร์ | `VOLUME` 1 อัน → `AudioListener.volume` | `MASTER` / `MUSIC` / `SFX` → `AudioDirector.SetVolume` → PlayerPrefs + mixer |
| ชื่อ object | `VolumeSlider` / `VolumeValue` | `MasterSlider` / `MusicSlider` / `SfxSlider` + `*Value` |
| หมายเหตุท้ายหน้า | "AUDIO IS WIRED BUT HAS NO CLIPS YET" | ลบแล้ว (มีคลิปจริง) |

**ตำแหน่งแถว (การ์ด 820×520):** RESOLUTION `y=118` · DISPLAY `y=48` · MASTER `−24` · MUSIC `−84` · SFX `−144` · footer `−206` — ระยะห่างเท่ากันทุกแถวและไม่ชนปุ่มล่าง; dropdown ของ resolution ขยับขึ้นเป็น `y=94` ให้ยังเปิดใต้ช่องพอดี

**linear → dB:** สไลเดอร์เป็น 0–1 แต่ mixer รับ dB → `AudioDirector` แปลงด้วย `20·log10(v)` และ map 0 ไปที่ −80 dB (ไม่ใช่ −∞) เช่น 80% = −1.94 dB, 25% = −12.04 dB

**เทสต์:** `the options page splits audio into master, music and sfx faders` · `each fader has its own percentage readout` · `the music fader reaches the mixer and leaves sfx alone`

### 8.14 Addendum รอบ 11 (2026-11-02) — เสียงคลิก 2 แบบ (chrome / action)

**ทำไมต้องแยก:** ผู้เล่นควรได้ยินความต่างระหว่าง "กำลังเลื่อนดู UI" กับ "กำลังทำอะไรบางอย่างกับรัน" จึงแยกเป็น 2 คลิป: เสียงแหลมสั้นสำหรับ chrome และเสียงทึบหนักกว่าสำหรับปุ่มที่ทำงานจริง

| กลุ่ม | ปุ่ม | คลิป |
|---|---|---|
| Chrome (Plain) | menu, options, help, close, mode toggle, CONTINUE, สไลเดอร์ | `Ui_Click` (`click_iron_01`, active 0.08s, ZCR 0.165) |
| Action (Playable) | cook, heal, craft, การ์ดดันเจี้ยน, การ์ดยูนิต, ปุ่มยืนยัน | `Ui_Click_Playable` (`click_stone_01`, active 0.04s, ZCR 0.064) |

**วิธีผูก (กันลืม):** `UiFactory.CreateButton` ติด `Plain` ให้ทุกปุ่มเป็นค่าเริ่มต้น → ปุ่มที่ทำงานจริงเรียก `UiClickSound.MarkPlayable()` (idempotent: `Attach` ใช้ `GetComponent` ก่อน จึงไม่มี listener ซ้อน) ; ปุ่มที่สร้างมือไม่ผ่าน factory (HudTopBar, DiceRollView CONTINUE) ติดเอง ; การ์ดยูนิต (`IPointerClickHandler`) และสไลเดอร์เรียก `UiClickSound.Play()`

**throttle 0.1s:** `Slider.onValueChanged` ยิงทุกเฟรมระหว่างลาก ถ้าไม่จำกัดจะกลายเป็นเสียงรบกวน — `UiClickSound.Play` ทิ้งคลิกที่ถี่กว่า 0.1s และ `OptionsPanelView.OnFaderChanged` ข้ามตอน `suppressVolumeCallback` (ตอนเปิดหน้าจะไม่รัวเสียง)

**ที่มาของคลิป:** คัดจาก `fantasy-ui-sfx-free` — อีกแพ็ก (`GUI_Sound_Effects_by_Lokif`) ถูกวัดแล้วใช้ไม่ได้: `click.wav` peak 0.08 / RMS 0.002 (เงียบเกือบสนิท), `click_2.wav` peak 0.04 และไฟล์อื่นยาว 1.7s ไม่ใช่เสียงคลิก

> archive ทั้ง 2 ชุดย้ายออกจาก `Assets/` ไป `asset-sources/` แล้ว เพราะวางใน `Assets/` ทำให้ Unity import เป็น asset + สร้าง `.meta` โดยไม่จำเป็น

**เทสต์:** `the library carries both ui click clips` · `the options button carries the plain chrome click` · `cook, heal and craft use the action click` · `the craft mode toggle stays a plain click` · `the confirm button uses the action click` · `every dungeon card uses the action click` · `clicking a chrome button plays the plain clip` · `the click throttle stops a slider drag machine-gunning`

### 8.15 Addendum รอบ 12 (2026-11-02) — เสียงคลิกแยกรายปุ่ม 6 แบบ

**ทำไมแยกถึง 6:** 2 แบบเดิม (chrome / action) ยังหยาบเกินไป — ผู้เล่นควรได้ยิน "ฉันกำลังจะทำอะไร" ตั้งแต่กดปุ่ม ไม่ใช่รอตอนผลออก ตอนนี้แต่ละปุ่มประกาศเสียงของตัวเอง จึงแยกได้ว่า COOK ต่างจาก CRAFT ต่างจาก HEAL โดยไม่ต้องอ่านป้าย

| UiClickKind | ปุ่มที่ใช้ | ไฟล์ | ยาว |
|---|---|---|---|
| `Plain` | menu, options, help, close, mode toggle, CONTINUE, slider | `Click.wav` | 0.20s |
| `Select` | การ์ดยูนิต, การ์ดดันเจี้ยน, ปุ่มยืนยัน | `Select.mp3` | 1.07s |
| `Cook` | ปุ่ม COOK | `Food Ready.wav` | 1.59s |
| `Heal` | ปุ่ม HEAL | `Heal.mp3` | 8.04s |
| `Craft` | ปุ่ม CRAFT | `Craft.mp3` | 1.70s |
| `Popup` | เปิดหน้า OPTIONS / HELP | `UI Popup.wav` | 0.18s |

**จุดที่ต้องระวัง:** ปุ่ม OPTIONS/HELP ยิง **2 เสียง** — คลิกของปุ่มเอง (`Plain`) แล้วตามด้วย `Popup` ตอนหน้าเปิด เทสต์จึงต้อง assert เป็น `count + 2` และ `LastSfx == Popup` ไม่ใช่ `+1`

**การเลือกเสียง (fingerprint):** วัด 10 log-spaced bands ด้วย FFT window แบบปรับตามความยาวคลิป + spectral centroid + duration + active length + ZCR แล้วให้คะแนนถ่วงน้ำหนัก (band cosine 0.55, duration 0.16, centroid 0.13, ZCR 0.08, active 0.08) ผลคือ `Select`→`toggle_on_01` 0.889, `Craft`→`accept_01` 0.899, `Food Ready`→`spell_select_01` 0.831 แต่ `Click`/`UI Popup` ไม่มีตัวสั้นพอ และ `Heal` แพ็กสว่างเกิน (0.441) → ใช้ไฟล์ตัวอย่างทั้ง 6

> บทเรียนจากการวัด: FFT window 2048 ตายตัวทำให้คลิปสั้นกว่า 46ms (เช่น click 0.03s) ได้สเปกตรัมเป็นศูนย์ทั้งหมด → ต้องย่อ window ตามความยาวคลิป ไม่งั้นการจัดอันดับจะเพี้ยนแบบเงียบ ๆ

**Import setting:** `Heal.mp3` เป็น **CompressedInMemory** (8.04s หาง reverb) ส่วนอีก 5 ตัวเป็น DecompressOnLoad + preload + mono

**เทสต์:** `the library carries every ui click clip` · `cook wears the food-ready click` · `heal wears the heal click` · `craft wears the craft click` · `the craft mode toggle stays a plain click` · `the confirm button uses the select click` · `every dungeon card uses the select click` · `tapping a unit card uses the select click` · `opening a page plays the popup clip` · `clicking a chrome button plays the plain clip` · `the click throttle stops a slider drag machine-gunning` → **ALL PASS 202**

### 8.16 Addendum รอบ 13 (2026-11-03) — เสียง Error ตอน warning

**ทำไมเสียง error ถึงอยู่ที่ banner ไม่ใช่ที่ปุ่ม:** warning ทุกทางในเกม (สกิลที่ทำไม่ได้, เลือกผิด, end-day ที่ยังไม่พร้อม) วิ่งผ่าน `WarningView.Show()` จุดเดียว ถ้าใส่เสียงที่ปุ่มแต่ละอันจะต้องแก้หลายที่และลืมง่าย — ใส่ที่ banner คือได้ครบทุกทางในบรรทัดเดียว

**เสียงที่เลือก:** `error_01.wav` 0.76s · centroid 593 Hz · 66% พลังงานใต้ 250 Hz → โทน "ปฏิเสธ" แบบทึบ ไม่แสบหูเวลากดพลาดซ้ำ (ตัวเลือกอื่น: `locked_01` 3725 Hz สว่างเกิน, `negative_2` 237 Hz ทุ้มเกิน, `negative` active 1.20s ยาวเกิน)

**ปัญหาที่เจอตอนทำจริง (สำคัญ):** ปุ่มที่ถูกปฏิเสธเล่นเสียงคลิกของตัวเองในเฟรมเดียวกัน แต่ลำดับ listener คือ game handler ก่อน แล้วค่อย `UiClickSound` → เสียง error ดังก่อนแล้วถูก chime 1.59s ของปุ่มกลบมิด

| ทางแก้ | ผล |
|---|---|
| ย้ายเสียงไปใน `RunQueue` | ❌ banner ที่รอคิวอยู่จะเงียบหลายวินาที และเทสต์เฟรมเดียวจับไม่ได้ |
| ให้ error ดังทีหลัง | ❌ ยังทับกันอยู่ (chime 1.59s + error 0.76s) |
| **`SuppressThisFrameClick()`** | ✅ ปุ่มที่ถูกปฏิเสธดัง **แค่ error** — ตั้ง flag ผูกกับ `Time.frameCount` ไม่ใช่ bool ค้าง เพื่อไม่ให้ warning ที่ไม่ได้มาจากคลิกไปปิดเสียงคลิกครั้งถัดไป |

**throttle 0.2s:** กดปุ่มที่บล็อกค้างจะได้เสียง error ออกมาทุกครั้งที่ `Show()` ถูกเรียก — throttle กันไม่ให้กลายเป็นเสียงรัว

> บทเรียนสำหรับเทสต์: warning ไม่ได้แปลว่า `ErrorCount` จะเพิ่ม ถ้า throttle ยังไม่หมดอายุ — เทสต์ต้องเรียก `ResetErrorThrottle()` ก่อนวัด

**เทสต์:** `the library carries an error sting for rejected clicks` · `a refused click shows a warning and plays the error sting` (ยืนยัน `SfxCount +1` และ `LastSfx == Error` = ไม่มีเสียงคลิกซ้อน) · `the error sting is throttled so a held button does not buzz` → **ALL PASS 205**

### 8.17 Addendum รอบ 14 (2026-11-03) — Win เล่น 2 เพลง: บั๊กอยู่ที่ไฟล์ ไม่ใช่โค้ด

**อาการ:** ได้ยินเพลงชนะ 2 เพลงซ้อนกัน

**การไล่หา (สำคัญ: อย่าดูโค้ดอย่างเดียวก่อน)** — วัดที่ runtime ก่อน:
- `AudioDirector` มีตัวเดียว, `AudioSource` 2 ตัว (Music/SFX)
- เรียก `PlayWin()` ครั้งเดียว → `SfxCount 0 → 1`, `LastSfx = Win 2`
- เดินเส้นทางชนะจริง (`ObtainCoreItem` A + B → `CheckImmediateWin`) → `SfxCount +1` ช็อตเดียว

⇒ **โค้ดถูก** ปัญหาอยู่ที่ข้อมูลเสียง

**ต้นเหตุ:** ไฟล์ mp3 เป็น stereo ที่ **ช่องซ้ายกับขวาบรรจุคนละเพลง**

| ไฟล์ | L rms | R rms | correlation | best xcorr (±400 samples) | สรุป |
|---|---|---|---|---|---|
| `Win_1.mp3` | 0.196 | 0.207 | **0.117** | 0.451 @ lag 9 | คนละเพลง |
| `Win 2.mp3` | 0.075 | 0.073 | **0.626** | 0.640 @ lag 0 | คนละเพลง |

ประกอบกับ `AudioImporter.forceToMono = true` ที่ **บวก L+R** → ผลลัพธ์คือ 2 เพลงพร้อมกัน

> กับดักในการวินิจฉัย: RMS และ spectral centroid ของ L กับ R ใกล้กันเป๊ะ (1959 vs 1923 Hz) ทำให้เดาว่า "เป็น stereo ปกติที่กว้าง" — ต้องวัด **correlation ที่ทุก lag** ถึงจะแยก "delay/phase widener" (corr จะพุ่ง > 0.8 ที่ lag หนึ่ง) ออกจาก "คนละเนื้อหา" (corr ต่ำทุก lag) ได้

**วิธีแก้:** แยกช่องที่ดังกว่า (+RMS) เขียนเป็น mono 16-bit WAV ใหม่ที่ sample rate เดิม (24 kHz)
- `Win_1.wav` 4.49s · `Win_2.wav` 3.91s · `ch = 1` ทั้งคู่
- **Normalize เป็น −1 dBFS:** peak เดิม 0.925 vs 0.415 ต่างกัน 2.2 เท่า → สุ่มแล้วรู้สึกเหมือนเสียงดับ (gain ×0.96 / ×2.14)
- ย้าย mp3 ต้นฉบับไป `asset-sources/win-source/` กันผูกกลับเข้าผลิตภัณฑ์

**กันซ้ำ:** เทสต์ `every win sting is mono, so one file cannot hold two tracks` ตรวจ `clip.channels == 1` ของทุกคลิปใน `win[]` — ถ้าใครเปลี่ยนไปใช้ไฟล์ stereo อีก will fail ทันที

**`PlayWin()` ตัด SFX ที่ค้างก่อน** (`sfxSource.Stop()`) เพราะเสียงชนะมาหลังเสียงเก็บของเสมอ

> **ยังค้าง:** `Select.mp3` มีอาการเดียวกัน (correlation **−0.20**) — ใช้ทุกครั้งที่เลือกการ์ด/กดยืนยัน ยังไม่แก้เพราะต้องให้เจ้าของโปรเจกต์ตัดสินใจว่าจะแยกช่องไหน

### 8.18 Addendum รอบ 15 (2026-11-03) — คิว Cook/Craft, คลิกขวายกเลิกทั้งจอ, hover feedback, หมอกแดง

รอบนี้เป็นงานขัด UI 5 ข้อจาก playtest — ไม่แตะกติกาเกมเลย

| # | สิ่งที่ขอ | สิ่งที่ทำ |
|---|---|---|
| 1 | ปุ่ม `+` ให้เห็นว่ากดเพิ่มจำนวนได้ + ย้ายตัวเลขออกนอกกรอบมาข้างปุ่มด้านซ้าย | `SkillTriggerPanel.BuildCounters()` สร้างแถวใหม่ (**สร้างในโค้ด ไม่แก้ prefab**): `CookRow` = `[Count] [CookButton +]` และแทรก `[Count]` เข้า `CraftRow` เดิม (อยู่หน้าปุ่มสกิล) · `HorizontalLayoutGroup.childAlignment = MiddleRight` → ตัวเลขงอกไปทางซ้าย ปุ่มสกิลจึงยังอยู่คอลัมน์เดิม · `+` เป็น TMP label ในกรอบปุ่ม (ช่องขวา, สี accent) **ไม่ใช่ปุ่มที่สอง** — ทั้งปุ่มเพิ่มรอบอยู่แล้ว และ `MakeRoomForPlus()` วัด `preferredWidth` แล้วเลื่อน label ซ้ายถ้าจะชน (ปุ่ม Craft กว้าง 156 จึงชนง่าย) |
| 2 | คลิกขวาที่ไหนก็ได้ = ยกเลิกสิ่งที่เลือกอยู่ | `RightClickCancel` (component บน `GameSystem`) อ่าน `Mouse.current.rightButton.wasPressedThisFrame` (โปรเจกต์เป็น Input System only — `Input.GetMouseButtonDown` ใช้ไม่ได้) แล้วเรียก `SkillFlowController.CancelActiveSkill()` + `GatherMapController.ClearSelection()` + `UnitRosterView.ClearSelection()` · **ไม่มีข้อยกเว้น**: `RightClickHandler` รายปุ่มถูกลบ, event `CookCleared` / `CraftCleared` / `HealCancelled` และ `ClearQueue()` ใน `SkillFlowController` ก็ถูกลบตาม |
| 3 | บั๊ก: เลือกการ์ด → เลือกถ้ำ → เลือกสกิล → ยืนยัน แล้วขึ้น "Pick a cave on the map first." | ดูหัวข้อ "บั๊กปุ่มยืนยันถูกกด 2 handler" ด้านล่าง |
| 4 | hover feedback + เสียง | `HoverHighlight` (mask สีอุ่น fade เข้า/ออก, ยิง `AudioDirector.PlayHover()`), `HoverFeedbackBootstrap` แปะให้ทุก `Button` ในซีน และ `UiFactory.CreateButton` แปะให้ปุ่มที่สร้างตอนรัน (options/help/result) · การ์ดตัวละคร (`UnitIconView`) และการ์ดดันเจี้ยน (`MapNodeView`) ทำ hover เองด้วยการไล่สีพื้น (mask จะไปอยู่ใต้ chrome ที่สร้างในโค้ด) · `DayFrame` ใช้ `HoverHighlight` โหมด `target` (ไล่ alpha 0.5 → 0.72) |
| 5 | หมอกแดง ปรับความหนาได้ | `RedFogOverlay` (component บน `MapPanel`) สร้าง layer ของตัวเองเป็น sibling ของ `MapPanel` — **เหนือแผนที่/การ์ด ใต้ HUD** · noise แบบ value-fbm ที่ tile ได้จริง (wrap lattice ตาม period) 2 ชั้นไหลคนละทาง + vignette แดงเข้ม · `Density` 0–1 ปรับสด ๆ · ~~สไลเดอร์ `FOG` ในหน้า Options~~ → **ถอดออกในรอบ 17** เพราะความหนาหมอกมาจาก `Risk` ตัวเดียว (ดู §8.19) |
| 6 | การ์ดดันเจี้ยน: แคบลง + เลื่อนขวา + หลอดหมอกซ้ายสุด | `GatherMapController` ตัด `maxCardWidth` 480 → **430** และแบ่ง margin ซ้ายเป็น `fogGaugeWidth (26) + fogGaugeGap (34)` ก่อนคิดความกว้างการ์ด → แถวการ์ดแคบลง 50 px และเลื่อนขวา ~75 px โดยอัตโนมัติ (จัดกลาง band ใหม่) · `FogGaugeView` คือหลอดแนวตั้งซ้ายสุด **สูงเท่าการ์ดเป๊ะ** (ส่งขนาดมาจาก controller), มีร่องตี 25/50/75% และคอลัมน์แดงที่ไล่ตาม `Density` (ตามด้วยความเร็ว `followSpeed`, tooltip บอก % ที่แท้จริง) · หลอดถูก parent ใต้ `RedFogOverlay.OverlayRoot` เพื่อให้อ่านค่าได้ชัดแม้หมอกหนา |

**บั๊กปุ่มยืนยันถูกกด 2 handler (ข้อ 3)**

`DispatchActionPanel.Clicked` มี subscriber สองตัว: `GatherDispatchPanel.OnDispatchClicked` (เดิม gate ด้วย `IsSkillActive()`) และ `SkillFlowController.HandleConfirm` · Unity เรียกตามลำดับที่ subscribe ถ้าฝั่งสกิลมาก่อน มันจะทำงานเสร็จและ `Disarm()` **ภายในคลิกเดียวกัน** → พอถึง handler ของ gather `IsSkillActive()` กลายเป็น false แล้ว, ถ้ำก็ถูก `EnterSkillMode()` ล้างไปตั้งแต่ตอน arm → เข้าเงื่อนไข "ยังไม่ได้เลือกถ้ำ" แล้วขึ้น warning ทั้งที่ผู้เล่นเพิ่งทำถูกทุกขั้น

แก้ที่ต้นเหตุ: `DispatchActionPanel` เก็บ `Owner { None, Dispatch, Skill }` ตอน `ShowDispatch()` / `Show()` และเคลียร์เป็น `None` ใน `Hide()` · `GatherDispatchPanel` เช็ก `Owner == Dispatch` แทนการเดาจาก state ของอีกฝั่ง — ไม่ว่า handler ตัวไหนจะได้คิวก่อน ผลก็ถูกทั้งคู่

**ตรวจสอบแล้ว (play mode, Core scene)**

| ชุดทดสอบ | ผล |
|---|---|
| `AgentScripts/TestPolish.cs` (ใหม่) — counter/+ , right-click, hover, fog, fog gauge, options | **31/31 ผ่าน** |
| `AgentScripts/TestUiRedesign.cs` — regression ทั้งหมด | **ALL PASS (211 checks)** |
| Probe `ProbeSkillMode` (การ์ด → ถ้ำ → Cook → ยืนยัน) | ไม่มี warning ปลอมแล้ว (`warn="Warning"` ค่าเดิม ไม่ใช่ "Pick a cave...") |

> **ข้อควรรู้:** `script run` ที่เรียก `Awake` ของ component ที่เพิ่งแก้จะไม่รันโค้ดใหม่ถ้า recompile แบบ method reload — ต้อง stop/start play mode ก่อน

### 8.19 Addendum รอบ 16–17 (2026-11-03) — Risk ขับหมอกแทนสไลเดอร์, Risk รายถ้ำ, ไอคอน Core Item บนการ์ด, กรอบ GOAL

รอบนี้รวมงานสองก้อน: ก้อนแรกคือระบบ **Risk** ที่เข้ามาแทนสไลเดอร์หมอกและผูกกับ Success Value / Core Item และก้อนที่สองคือการเก็บรายละเอียด UI ที่ตามมา

**Risk (แทนค่า fog ที่เคยปรับมือ)**

| หัวข้อ | สเปก |
|---|---|
| แหล่งเดียว | `RunResources.Risk` ค่าเริ่มต้น `MaxRisk = 80`, floor 0 (`AddRisk` / `SetRisk` clamp) · **หมอก (`RedFogOverlay.Density`), หลอดวัด (`FogGaugeView`) และโทษ SV อ่านจากค่านี้ตัวเดียว** |
| สไลเดอร์ FOG | **ถูกลบทั้งแถว** ออกจาก `OptionsPanelView` (`FogSlider` / `FogValue` / `SaveIntensity` / `LoadIntensity` หายไปหมด) — การ์ด Options ย่อกลับเป็น 820×520 และ footer เลื่อนขึ้นเป็น y −204 · ทางเดียวที่ทำให้หมอกบางลงคือไปเคลียร์ถ้ำ |
| Risk รายถ้ำ | ค่าเดียวคือ Risk ของ run (เริ่ม 80) · แต่ละถ้ำมี **Handicap** เป็นค่าลดหย่อน: **Easy 30 / Normal 20 / Hard 0** → `GatherResolver.RiskPenalty(zone, risk) = max(0, risk − handicap)` แล้ว `ComputeSuccessValue` หักค่านี้ |
| ผลของ Handicap | เป็นค่าลดหย่อนคงที่ ไม่ใช่เพดาน → **Easy หลุดโทษที่ Risk 30, Normal ที่ Risk 20, Hard ต้องถึง 0** · `−Risk + Handicap` ไม่มีทางติดลบ SV จึงไม่ถูกดันเกิน base |
| ผลตอนเริ่มเกม | Risk 80 → Easy `80−50 = 30%`, Normal `55−60 → 0%`, Hard `30−80 → 0%` · Risk 40 → `70 / 35 / 0` · Risk 30 → `80 / 45 / 0` (Easy เริ่มฟรี) · Risk 20 → `80 / 55 / 10` |
| ลด Risk | เคลียร์ถ้ำสำเร็จ: Easy −5, Normal −10, Hard −15 (Failure/Fumble ไม่ลด) |
| Core Item | ผูกกับถ้ำ: Normal Hard Success → **Key**, Hard → **Box**, Easy ไม่มี · **การันตี ไม่ทอย** (ระบบ pity กับ `coreItemChancePercent` ถูกลบ) · **Jackpot**: SV ≥ 80 และทอยได้ 1 → รับชิ้นที่ยังขาด (ได้ทั้งคู่ถ้ายังไม่มีสักชิ้น) |
| Buff potion | `2 AP + 5 Materials` (เดิม 4) |

**UI รอบนี้**

| # | สิ่งที่ขอ | สิ่งที่ทำ |
|---|---|---|
| 1 | เอา FOG ออกจาก Options | ถอดฟิลด์/สไลเดอร์/callback ทั้งหมดใน `OptionsPanelView`, `RedFogOverlay.Danger()` เหลือ `RiskRatio` เพียว ๆ · `TestPolish` §4 เปลี่ยนไปเช็ก `Density` ตรง ๆ และเช็กว่าไม่มี knob อื่นเหลือ |
| 2 | Risk ต่างกันรายถ้ำ (50/60/80) → **แก้เป็น Handicap** | เดิมใช้เพดาน Risk รายถ้ำ (`ZoneRisk` 50/60/80) แต่เปลี่ยนเป็น **ค่าลดหย่อน** ตามคำขอ: `ZoneHandicap` = Easy 30 / Normal 20 / Hard 0, โทษ = `max(0, risk − handicap)` → ถ้ำตื้นหลุดโทษก่อนถ้ำลึก (Easy ที่ Risk 30, Normal ที่ 20, Hard ที่ 0) · การ์ดที่ยังไม่ถูกเลือกก็คิดตามถ้ำของตัวเอง (`GatherMapController.RiskPenalty(zone)`) · Help/tooltip เปลี่ยนคำจาก "RISK n ต่อถ้ำ" เป็น "HANDICAP n" |
| 3 | รูปกุญแจ/กล่องในช่องไอเทมที่ดรอปของการ์ด | `MapNodeView` เพิ่มไอคอน `CorePiece` ที่ **ปลายขวาของแถว Drops** (Normal = กุญแจ, Hard = กล่อง, Easy = ว่าง) · ช่อง loot 3 ช่องแบ่งความกว้างที่เหลือหลังกันที่ 50 px ให้ไอคอนนี้ จึงไม่มีของทับกัน · อาร์ตดึงจาก `StatusBarView.CoreKeySprite` / `CoreBoxSprite` เพื่อให้การ์ดกับการ์ด HUD เป็นภาพเดียวกัน (รองรับทั้งชุดส้ม/เหลืองตาม `RunResources.Variant`) |
| 4 | กรอบช่อง Core Item + คำว่า GOAL แนวตั้ง | `StatusBarView.BuildGoalFrame()` สร้าง `GoalFrame` (plate + ขอบ 4 ด้านสีทอง) ครอบสองช่อง แล้วเขียน `G O A L` ลง rail ซ้าย · **สร้างตอนรันเหมือนชิ้นส่วนอื่น** ไม่แก้ซีน และ **ขยายไปทางซ้ายเท่านั้น** เพราะกลุ่มนี้ติดขอบขวาของจอ (ขวา +6 px ยังอยู่ในจอ) · คลิก/ hover ตัวกรอบได้ tooltip เดียวกับช่อง |
| 5 | เขียนคำว่า RISK ที่หลอด | `FogGaugeView` เพิ่ม `RiskLabel` (TMP บรรทัดละตัวอักษร `R I S K`) วางในร่องซ้ายระหว่างขอบ plate กับหลอด · ขยาย `fogGaugeWidth` 54 → **66** ให้ร่องมีที่ · tooltip ของหลอดอัปเดตเป็น "แต่ละถ้ำจ่ายคนละส่วน" |

**ตรวจสอบแล้ว (play mode, Core scene)**

| ชุดทดสอบ | ผล |
|---|---|
| `AgentScripts/TestPolish.cs` — เพิ่ม §9 goal markers, §4/§5 เขียนใหม่ตาม Risk + Handicap รายถ้ำ, §7 เช็กว่าไม่มี FOG slider | **61/61 ผ่าน** |
| `AgentScripts/TestUiRedesign.cs` — regression ทั้งหมด | **ALL PASS (211 checks)** |

> **ข้อควรรู้:** ตอนถ่าย screenshot ถ้า Editor ไม่ได้ focus ให้ `unity command set_run_in_background --enable true` ก่อน ไม่งั้น player loop หยุด (`playerLoopTicking: false`) แล้ว `game_view` จะคืนเฟรมเก่า

### 8.20 Addendum รอบ 18 (2026-11-03) — ซุ้มหิน + แบดจ์ระดับ, แถบทรัพยากรกลางจอ, การ์ด/สกิลเทาเมื่อยังเลือกไม่ได้, SV ไม่มี `%`

| # | สิ่งที่ขอ | สิ่งที่ทำ |
|---|---|---|
| 1 | เปลี่ยนสไปรท์ดันเจี้ยนกลับไปเป็นซุ้มหินเดิม แล้วแปะไอคอนให้ทั้งสามแบบต่างกัน (Hard = กะโหลก) | `GatherMapController` ถือ `caveArt` + `easyBadge` / `normalBadge` / `hardBadge` แล้วส่งเข้า `MapNodeView.SetArt()` ก่อน `Initialize` · `caveArt` = สไปรท์ซุ้มหิน `tilemap_packed 1_1` (ชิ้นเดียวกับ `Dungeon Prefab` เดิมในซีน) · แบดจ์วางกลางปากถ้ำ (`TierBadge`, 26% ของความสูงภาพ) ย้อมสีตามระดับ: **Easy = structure_house, Normal = structure_tower, Hard = skull** จาก `board-game-icons` · ทั้งสามใช้ภาพเดียวกัน แบดจ์คือสิ่งที่ทำให้แยกออก |
| 2 | แถบทรัพยากรมาอยู่กลางจอ และไอคอนกับข้อความให้ชิดกัน | `StatusBarView.BuildResourceRow()` สร้าง `ResourceRow` (HorizontalLayoutGroup + ContentSizeFitter) แล้วจับคู่ใหม่เป็น `Pair_Food` / `Pair_Cooking` / `Pair_Materials` / `Pair_Medicine` (ไอคอน + ข้อความ, `iconLabelGap = 6`) · เดิมไอคอนกับตัวเลขอยู่ห่างกันเพราะแต่ละช่องเป็น Rect ความกว้างคงที่ 240–320 px ทำให้ช่องว่างโผล่ท้ายข้อความ · ตอนนี้วัดจาก `preferredWidth` ของ TMP แล้วจัดกลางผ่าน layout group |
| 3 | ไอคอนทรัพยากรใหม่ | Food = `resource_apple`, Cooking Material = `resource_wheat`, Materials = `resource_planks`, Medicine = `flip_half` (เม็ดยาจากชุดตัวอย่าง) — ทั้งหมดจาก `board-game-icons/PNG/Double (128px)` · ไอคอนสูง 28 px (`resourceIconSize`) |
| 4 | การ์ดดันเจี้ยนเทา (กดไม่ได้) ถ้าตัวละครที่เลือกไม่มีดาบ | `MapNodeView.SetInteractable(bool)` + `IsLocked` · เมื่อ locked: `Button.interactable = false`, พาเลตต์ทั้งใบเปลี่ยนเป็นเทา (`LockedPanel` / `LockedInk` / `LockedFrame` / `LockedArt`), ปิด hover, และคลิกที่ถูกเรียกตรง ๆ ก็ถูกกันไว้ใน `HandleClick` · `GatherMapController.RefreshInteractable()` ปลดล็อกเมื่อมีคนที่ยังถือดาบ และเรียกซ้ำจาก `RefreshReadouts()` + `Update()` (signature: จำนวนที่เลือก + มีดาบหรือไม่) เพื่อจับกรณี "ดาบถูกใช้ไปแล้ว" ที่ไม่มี event ประกาศ |
| 5 | ปุ่มสกิลเทา (กดไม่ได้) ถ้ายังไม่เลือกตัวละคร | `SkillTriggerPanel.SetLocked/RefreshLock` สมัคร `UnitRosterView.OnSelectionChanged` เอง · `ApplyButtonStates()` รวมสามสถานะไว้ที่เดียว: **locked > armed > idle** · สำคัญ: ปุ่มที่ `interactable = false` จะถูกทับด้วย `disabledColor` (alpha 0.45 ของโปรเจกต์) ทำให้เห็นพื้นแผนที่ทะลุขึ้นมาเป็นสีเขียวมะกอก จึงเซ็ต `disabledColor` เป็น `lockedTint` ทึบตอนล็อก และคืนค่าเดิมเมื่อปลดล็อก · ไอคอนสกิลหรี่เหลือ alpha 0.35 และ `+` เปลี่ยนเป็นสีเทา |
| 6 | ตัวเลข SV บนการ์ดให้เป็นเลขล้วน + อธิบายใหม่ | `RefreshReadout` เปลี่ยนจาก `"{chance}%  --  {ap} AP"` เป็น `"{chance}  --  {ap} AP"` · tooltip การ์ด: "YOU ROLL 1 TO 100 AND HAVE TO ROLL UNDER {n}" · หน้า Help §7/§8 เขียนใหม่ให้ตรงกัน และ `BaseValue`/`ZoneLine` ยังคงเป็นตัวเลขล้วน |

**ตรวจสอบแล้ว (play mode, Core scene)**

| ชุดทดสอบ | ผล |
|---|---|
| `AgentScripts/TestPolish.cs` — เพิ่ม §10 (ซุ้มหิน/แบดจ์/Hard = skull, readout ไม่มี `%`, แถบทรัพยากรอยู่กลาง ±40 px, ไอออน‑ข้อความห่าง ≤16 px) และปรับ §3 ให้วัด hover บนปุ่มที่ปลดล็อก + เช็กว่าปุ่มเทาไม่เรืองแสง | **69/69 ผ่าน** |
| `AgentScripts/TestUiRedesign.cs` — ปรับ `ReadChance` (ไม่มี `%`) และเพิ่มเช็ก "การ์ด/สกิลเทาจนกว่าจะเลือกตัวละคร", "ปลดล็อกเมื่อเลือก", "เทาอีกครั้งเมื่อเลิกเลือก" | **ALL PASS (216 checks)** |

> **หมายเหตุ:** `MapPanel` ยังเป็น Image สีพื้น (`0.876, 1.0, 0.730`) ตามสภาพ placeholder เดิม — ที่ Risk 80 หมอกแดงกลบจนอ่านง่าย แต่ที่ Risk 0 พื้นจะสว่างมาก ถ้าจะให้ตัวหนังสือ HUD อ่านง่ายทุกสถานะ ควรใส่แผ่นรองหลังหรือภาพพื้นแผนที่จริงต่อไป

---

### 8.21 Addendum รอบ 19 (2026-11-03) — ระบบ Tutorial แยกซีน + ป๊อปอัป NEW GAME

| # | สิ่งที่ขอ | สิ่งที่ทำ |
|---|---|---|
| 1 | กด NEW GAME แล้วให้มีป๊อปอัปเด้งตั้งแต่เริ่ม เขียนว่า "Find the crystal and win the game!!" พร้อมปุ่ม view the tutorial | `NewGamePromptView` (โมดัลบนหน้าเมนู) — พาดหัว `FIND THE CRYSTAL AND WIN THE GAME!!`, เนื้อความ 2 บรรทัด, ปุ่ม **START THE RUN** และ **VIEW THE TUTORIAL?**, คลิกฉากหลังเพื่อปิด · `MainMenuController.HandleStart()` เปลี่ยนจากเริ่มเกมตรง ๆ เป็นเปิดป๊อปอัป และป้ายปุ่มหลักเปลี่ยน START → **NEW GAME** |
| 2 | คลิก tutorial แล้วเริ่มเกมในซีน `Tutorial` แยกจากเกมปกติ | `SceneFlow` แยก `BeginRun()` (รีเซ็ต stat + ลบเซฟ + สุ่มเพลง) ออกมาใช้ร่วมกัน แล้วเพิ่ม `TutorialScene = "Tutorial"` + `NewTutorial()` · สร้าง `Assets/V2/Scenes/Tutorial.unity` โดยคัดลอก `Core.unity` แล้วเติมออบเจกต์ `TutorialDirector` และเพิ่มใน EditorBuildSettings |
| 3 | สคริปต์สอนแบบ click to continue 26 จังหวะตามข้อความที่กำหนด | `TutorialDirector` ถือสคริปต์ `List<Step>` 26 รายการ (`text`, `prompt`, `target`, `done`, `enter`, `catchTap`, `focus`) — ทุกก้าวเดินหน้าด้วยการคลิก และบางก้าวมี **gate** ที่ต้องทำสิ่งที่สอนก่อน (เลือก Ann, เลือก Easy, เปิด/ปิดเต๋า, คิว Cook, ยืนยัน Cook, จบวัน) · ลำดับ: เป้าหมายเกม → พื้นที่ดันเจี้ยน → กรอบ GOAL → การ์ดตัวละคร → ความถนัดหลัก/รอง (โฟกัสสองจุด) → เลือก Ann → เลือก Easy → AP → ดาบ/readiness → SV → drop → DISPATCH → ลูกเต๋า → แท่ง SV → CONTINUE → Risk → ทรัพยากร → สกิล → เลือก Ann → Cook → เลขคิว → Cook อีกครั้ง → คิวซ้ำได้ → ยืนยัน Cook → จบวัน → ปุ่ม help |
| 4 | ไฮไลต์พื้นที่เป้าหมายแบบเจาะเป็นรู | `TutorialOverlay` (แคนวาสของตัวเอง `sortingOrder = 600` เหนือ `DiceRollView` 500) วาดแถบมืด 4 แถบ + กรอบเรืองแสง 4 เส้นรอบ "รู" — ไม่ใช้ shader/mask · รองรับหลายเป้าหมายพร้อมกัน (`focusRings` pool) สำหรับก้าวที่ไฮไลต์ความถนัดหลักและรองพร้อมกัน |
| 5 | ทอยเต๋าให้ออก 10 (SV 55 = Hard Success) | `GatherResolver.ForcedRoll` — ถ้าตั้งไว้ `Roll()` จะคืนค่านั้นหนึ่งครั้งแล้วเคลียร์ตัวเองเป็น −1 · `HardSuccessThreshold` ถูก clamp ที่ 100 · ก้าว DISPATCH ตั้ง `ForcedRoll = TutorialDirector.ScriptedRoll` (10) |
| 6 | จบแล้วขึ้นป๊อปอัป RETURN TO GAME กลับหน้าเกมหลัก | `TutorialOverlay.ShowFinale()` การ์ดปิดท้าย "THAT IS EVERYTHING YOU NEED TO SURVIVE OUT HERE. GOOD LUCK!" + ปุ่ม **RETURN TO GAME** → `TutorialDirector.Depart()` → `SceneFlow.NewGame()` (เริ่มรันใหม่ที่ Core) |

**ไฟล์ที่เพิ่ม**

| ไฟล์ | หน้าที่ |
|---|---|
| `Assets/V2/Scripts/UI/TutorialOverlay.cs` | เรนเดอร์โค้ชมาร์ก: แถบมืด + กรอบเรืองแสง + การ์ดข้อความ + ตัวรับคลิกเต็มจอ (`Catcher`, เปิดเฉพาะก้าวที่ต้อง tap) + การ์ดปิดท้าย · `ShowStep / Retarget / HideStep / ShowFinale / Tap / Hole / IsShowing / TapsSeen` |
| `Assets/V2/Scripts/UI/TutorialDirector.cs` | สคริปต์ 26 ก้าว, ตัว resolve เป้าหมาย (`GuideCard` ใช้ Rect ของ `Lift`, `HandRow`/`DungeonRow` ใช้ `Union` รวมหลาย Rect, `GuideStats` คืนสอง Rect), gate ต่าง ๆ, `Advance / Pump / Tap / Update`, ระงับเซฟ |
| `Assets/V2/Scripts/UI/NewGamePromptView.cs` | ป๊อปอัป NEW GAME |
| `Assets/V2/Scenes/Tutorial.unity` | ซีน tutorial (สำเนา Core + `TutorialDirector`) |
| `AgentScripts/TestTutorial.cs` | ชุดทดสอบ 20 เช็กที่เดินสคริปต์จนจบ |

**จุดที่พลาดและวิธีแก้**

| อาการ | สาเหตุ | วิธีแก้ |
|---|---|---|
| คลิกที่เปิดก้าวหนึ่ง ๆ แล้วก้าวนั้นข้ามไปเลย | คลิกเดียวกันถูกอ่านเป็น tap ของก้าวใหม่ในเฟรมเดียว | เก็บ `tapArmedAt` แล้วกัน tap ในช่วง 0.2 วินาทีแรก |
| คลิกเมาส์ไม่ติดเลย | โปรเจกต์ใช้ Input System อย่างเดียว `Input.GetMouseButtonDown` ไม่ทำงาน | เปลี่ยนไปอ่าน `Mouse.current.leftButton/rightButton.wasPressedThisFrame` |
| ชุดทดสอบค้างที่ก้าว CONTINUE ของเต๋า | ทั้งชุดรันในเฟรมเดียว animation ไม่จบ ปุ่มยังไม่โผล่ | เพิ่ม `DiceRollView.RevealImmediately()` ให้ผลออกก่อน แล้วทดสอบสั่ง `onClick` ของปุ่ม CONTINUE ตรง ๆ |
| เซฟถูกเขียนทับระหว่างเล่น tutorial | `DayManager.EndDay()` เซฟเอง | `SaveSystem.Suspended` — `SaveNow()` คืน false ระหว่าง tutorial, `Depart()`/`OnDestroy()` คืนค่า |

**ตรวจสอบแล้ว (play mode)**

| ชุดทดสอบ | ผล |
|---|---|
| `AgentScripts/TestTutorial.cs` (Tutorial scene) | **20/20 ผ่าน** |
| `AgentScripts/TestPolish.cs` (Core scene) | **69/69 ผ่าน** |
| `AgentScripts/TestUiRedesign.cs` (Core scene) | **ALL PASS (216 checks)** |

**ตรวจด้วยตา (game_view):** ป๊อปอัป NEW GAME บนหน้าเมนู, ก้าว 0–4, 6, 9, 10, 12 (เต๋าออก 10 = HARD SUCCESS ที่ SV 55), 13 (แท่ง SV), 14 (CONTINUE), 15 (Risk), 16 (ทรัพยากร), 17 (สกิล), 19 (Cook), 21–22 (เลขคิว 1 → 2 ทางซ้ายของปุ่ม), 23 (ยืนยัน Cook), 24 (จบวัน), การ์ด RETURN TO GAME และการกลับเข้า Core ด้วยรันใหม่ — การ์ดโค้ชไม่ทับคอนโทรลที่ไฮไลต์ในทุกก้าว

> **หมายเหตุ:** `Tutorial.unity` เป็นสำเนาของ `Core.unity` ถ้าแก้ซีนหลักในอนาคตต้องแก้ทั้งสองที่ · ก้าวที่บังคับทอย 10 ตั้งอยู่บนสมมติฐาน Risk 80 (Easy SV 55) ถ้าเริ่มเกมด้วย Risk อื่นหรือแก้ base SV ต้องปรับ `TutorialDirector.ScriptedRoll` หรือข้อความในสคริปต์ตาม

---

### 8.22 Addendum รอบ 20 (2026-11-04) — การ์ดเป้าหมายในซีนเกม, ไฮไลต์ความถนัดแบบเจาะจุด, ข้อความกลางกล่อง, ไอคอนดรอปจาก HUD, โค้ชมาร์กรอทอยเต๋าจบ

| # | สิ่งที่ขอ | สิ่งที่ทำ |
|---|---|---|
| 1 | ป๊อปอัปบอก goal ให้เด้งตอนโหลดซีน Core เสร็จ | **ย้ายจากหน้าเมนูมาเป็นในเกม** — `MainMenuController.HandleStart()` เรียก `SceneFlow.NewGame()` ตรง ๆ (ไม่มี prompt บนเมนูแล้ว) · `SceneFlow.NewGame()` ตั้ง `PendingGoalPrompt = true` และ `RunBootstrap.Awake()` (ฝั่ง fresh run) ยกการ์ดขึ้นมาทับแผนที่ที่โหลดเสร็จแล้ว · `NewGamePromptView` ย้ายไปสร้างบน **แคนวาสของตัวเอง `sortingOrder = 700`** (เหนือ tutorial 600 และ dice 500) จึงไม่แพ้ draw order ให้แคนวาสแผนที่ · API ใหม่ `NewGamePromptView.Show(onStart, onTutorial, onClose)` + `Close()`/`TakeDown()` (ปิดแล้ว `SetActive(false)` ก่อน `Destroy` เพื่อไม่ให้คลิกเดียวกันตกใส่การ์ดอีกรอบ) · `NewTutorial()` ล้างธงทิ้งเพราะ tour สอน goal ด้วยตัวเองอยู่แล้ว |
| 2 | ไฮไลต์ความถนัดให้เป็นแบบเจาะที่ป้ายตัวอักษร | ก้าว "Each agent has their own aptitude" เปลี่ยนเป้าหลักจาก `GuideCard` เป็น `HandRow` (เปิดทั้งมือให้เทียบความถนัดข้ามคนได้) และเหลือ **วงเดียว** รอบ `AptitudeBadge` ของ Ann (`GuideAptitude()`) — ถอด `GuideStats()` ที่เคยรัดสองจุด (ความถนัด + talent) ออก |
| 3 | ข้อความในกล่อง tutorial ให้จัดกึ่งกลาง | `TutorialOverlay` เปลี่ยน `message.alignment` จาก `TopLeft` เป็น `Top` (กึ่งกลางบน) — prompt ยังกึ่งกลางเหมือนเดิม |
| 4 | ไอคอน item ในช่อง possible drop ให้ตรงกับ status bar | `StatusBarView.IconFor(InventoryItemType)` คืนสไปรท์ที่ HUD ใช้จริง (wheat / flip_half / planks) · `MapNodeView.SpriteForItem()` ถาม HUD ก่อนเสมอแล้วค่อยตกไปที่ field เดิม/placeholder · `MapNodeView.Config` + `DropIcons` เปิดให้เทสต์ตรวจได้ · กล่อง/กุญแจ (`CorePiece`) ไม่แตะ |
| 5 | ให้ทอยเต๋าเล่นอนิเมชันจบก่อน แล้วค่อยโชว์ tutorial | `DiceRollView.Settled` (false ตอน `Show`, true ตอน `ApplyFinalState`) · `TutorialDirector.Step.wait` — ก้าวที่ไม่ต้องแสดงจนกว่าเงื่อนไขจะเป็นจริง: `Advance()` จะเรียก `overlay.HideStep()` แล้วปล่อยให้ `Pump()` แสดงการ์ดเองเมื่อ `wait` ผ่าน · ก้าว "Roll a die…" ผูกกับ `DiceSettled` |

**ตรวจสอบแล้ว (play mode)**

| ชุดทดสอบ | ผล |
|---|---|
| `AgentScripts/TestTutorial.cs` — เพิ่มเช็ก "การ์ดถูกพักไว้ระหว่างเต๋าหมุน", "การ์ดโผล่หลังเต๋าหยุด", "ก้าวความถนัดเปิดทั้งมือ (>900px)", "ก้าวความถนัดวงเฉพาะป้าย (4 แท่ง)", §6 การ์ดเป้าหมาย (สร้าง/พาดหัว/ปุ่ม/แคนวาส>600/ปิดได้) | **29/29 ผ่าน** |
| `AgentScripts/TestPolish.cs` — เพิ่มเช็ก "แถว loot ใช้ไอคอนเดียวกับ HUD" (เทียบสไปรท์ทีละช่องกับ `StatusBarView.IconFor`) | **70/70 ผ่าน** |
| `AgentScripts/TestUiRedesign.cs` | **ALL PASS (216 checks)** |

**ตรวจด้วยตา (game_view):** ก้าวความถนัด (วงเดียวรอบป้าย E, ทั้งมือสว่าง, ข้อความกึ่งกลาง), ก้าวเต๋า (การ์ดมาหลังผลออก 10 = HARD SUCCESS), และ flow จริงจากเมนู: NEW GAME → Core โหลดเสร็จ → การ์ด FIND THE CRYSTAL ลอยเหนือแผนที่ → กด VIEW THE TUTORIAL? → เข้า `Tutorial` ที่ก้าว 0 ทันที
