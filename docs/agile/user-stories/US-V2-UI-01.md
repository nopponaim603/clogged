# User Story: US-V2-UI-01 - ระบบการแสดง UI หลักของเกม

**Status:** ✅ Done (2026-10-28)
**Epic:** [Product Backlog](../01-product-backlog.md) · [V2 Task Overview](V2-TASK-OVERVIEW.md)
**Owner:** Dev C — _TBD_ (กลุ่ม 3)
**Estimate:** L

> 🔄 **Layout ถูก supersede:** ตำแหน่ง/องค์ประกอบ UI ให้ยึด [UI Layout Redesign Spec](../../software/07-unity-v2-ui-layout-redesign.md) (2026-10-27) — สเปกนี้ยังใช้ได้ในส่วนข้อมูล/พฤติกรรม (UnitCard, Feed, odds, RunEnd)
> ✅ **ผลจริง (2026-10-28):** งาน UI หลักของ story นี้เสร็จผ่าน redesign P0–P5 แล้ว — Canvas/CharCard → CharacterHandPanel, TestDispatch → GatherUI + DispatchActionPanel, MessageUi → LogPanel + WarningView, EndDay button → คลิก DayFrame. ส่วน Prefabs/UI/DispatchPanel.prefab และ GatherResultPanel.prefab ใน task list ถูกแทนด้วย prefab ชุดใหม่ (CharacterHandPanel, DispatchActionPanel, LogPanel, SkillTriggerPanel) — ดู [§7.10](../../software/07-unity-v2-ui-layout-redesign.md)

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** เห็นสถานะของทีม ทรัพยากร วันที่เหลือ และโอกาสสำเร็จก่อนส่งสำรวจ พร้อมปุ่มหลักที่ป้องกันการกดพลาด
**เพื่อให้** วางแผนแต่ละวันได้จากหน้าจอเดียว และรู้ชัดว่า run จบด้วยผลอะไร

**สถานะปัจจุบัน (อัปเดต 2026-10-28):**
- `StatusBarView` แสดงข้อความล้วน poll ทุกเฟรม (Core Item 2 ช่อง A/B + DayFrame ที่คลิกจบวันได้); `UnitRosterView` spawn **การ์ดยูนิต** (195×265) ใน `CharacterHandPanel` ล่างกลาง แทน toggle เดิม (ลบออกจาก scene แล้ว)
- `GatherDispatchPanel` อยู่บน GameObject ชื่อ **`GatherUI`** (เดิม `TestDispatch`) นอก Canvas, อ่านยูนิตที่เลือกจาก `UnitRosterView` (สูงสุด 2), ผลทริปเขียนลง `RunLog` ผ่าน `RunLogNarrator`, ข้อผิดพลาดแสดงผ่าน `WarningView` + log
- ปุ่มยืนยันย้ายไป `Canvas/DispatchActionPanel` (ปุ่มเดียว ใช้ร่วมกับ flow สกิล) และ `EndDay` ถูกยุบเข้า `DayFrame` แล้ว
- `StarvationController` (ให้อาหารลด stack) **ไม่มี UI**
- ปุ่ม End Day **ไม่ล็อก** ระหว่างทริป และไม่ปิดเมื่อ run จบ; `DayManager.OnRunEnded` **ไม่มีผู้ฟัง** → ไม่มีหน้าจอจบเกม

---

## 📂 ไฟล์ที่ต้องรู้ก่อนเริ่ม

### ต้องอ่านให้เข้าใจ (เจ้าของ = กลุ่มนี้)
| ไฟล์ | สิ่งที่ต้องรู้ |
|---|---|
| `Assets/V2/Scripts/UI/StatusBarView.cs` | ข้อความวัน/deadline, Food เทียบ "need tonight" (`CountMouthsTonight`), สถานะ Core Item 3 ระดับ |
| `Assets/V2/Scripts/UI/UnitRosterView.cs`, `UI/UnitIconView.cs` | spawn การ์ดตาม roster, `Bind` / `Refresh`, alpha ตามสถานะ, `Hungry xN` |
| `Assets/V2/Scripts/UI/GatherDispatchPanel.cs` | toggle ≤ 2 คน, `CanDispatch` แสดงเหตุผล, `ConfirmTrip`, จัดรูปผลใน `HandleTripResolved` (trait ใหม่เช็กจาก `traitsBefore`) |
| `Assets/V2/Scripts/Controllers/StarvationController.cs` | `GetBlockReason` (ไม่หิว / ให้แล้ววันนี้ / Food ไม่พอ), `FeedUnit` → `FeedResult` |
| `Assets/V2/Scripts/Controllers/DayManager.cs` | **เจ้าของ** — ลำดับ `EndDay()` (เช็ก Core Item → กินตามลำดับ roster → วัน+1 → เช็กแพ้ → refresh), `CheckImmediateWin`, `Outcome`, `OnDayStarted`, `OnRunEnded` |

### ต้องเรียกใช้ (ไม่ใช่เจ้าของ)
| ไฟล์ | ใช้อะไร |
|---|---|
| `Assets/V2/Scripts/Entities/UnitCharacter.cs` | `state`, AP, `aptitudes` / `nonAptitudes`, `traits`, `starvationStacks`, `CanBeSentGathering`, `HasBeenFedManuallyToday` |
| `Assets/V2/Scripts/System/UnitManager.cs` | `Units` |
| `Assets/V2/Scripts/System/RunResources.cs` | ค่าทรัพยากร, `HasCoreItemUnlock` / `HasCoreItem` — (เพิ่ม C7 `OnChanged`) |
| `Assets/V2/Scripts/Controllers/GatherDispatchController.cs` | `CanDispatch`, `GatherDispatchResult` |
| `Assets/V2/Scripts/System/GatherResolver.cs` | `ComputeSuccessValue(config, units)` (static, ไม่มี side effect) — ใช้คำนวณ odds preview |
| `Assets/V2/Scripts/System/GatherZoneDatabase.cs`, `Entities/GatherZoneData.cs` | `GetConfig(zone)` → AP cost, base SV, loot |
| `Assets/V2/Scripts/Controllers/GatherMapController.cs` | `SelectedNode`, `ConfirmTrip`, `OnTripResolved` (ไม่มี `TripInProgress` แล้ว) |
| `Assets/V2/Scripts/Scene/SceneFlowController.cs` (กลุ่ม 2 สร้าง) | C5 `RestartRun()` |

### Scene / Prefab
| ไฟล์ | ส่วนที่เกี่ยว |
|---|---|
| `Assets/V2/Scenes/Core.unity` | **หลัง redesign (2026-10-28):** `Canvas/StatusBar` (Core Item 2 ช่อง + DayFrame = End Day ผ่าน `EndDayTrigger`), `Canvas/CharacterHandPanel`, `Canvas/DispatchActionPanel`, `Canvas/LogPanel`, `Canvas/SkillTriggerPanel`, `Canvas/WarningText`, root `GatherUI` — ของเดิม (`CharCard`, `EndDay button`, `MapPanel/{Dispatch,MessageUi}`, `TestDispatch`) ถูกลบแล้ว |
| `Assets/V2/Prefabs/ScoutIcon.prefab` | ตอนนี้ใช้เป็นการ์ดยูนิตด้วย → แยกเป็น `UnitCard.prefab` (C6) |
| `Assets/V2/Prefabs/UI/` | สร้างแล้ว: `CharacterHandPanel`, `DispatchActionPanel`, `LogPanel`, `SkillTriggerPanel` · ยังไม่ทำ: UnitCard (C6), GatherResultPanel, RunEndPanel |

### เอกสาร
- [GDD §2 Unit, §3 Day, §4.3–4.5 odds, §6 Starvation, §7 Core Item](../../gdd/01-mechanics.md)
- [Art Direction §2 Color, §4 Layout](../../gdd/03-art-direction.md)
- [System Design — UI Layer, §4.3, §6 Known Gaps](../../software/04-unity-system-design.md)
- [Scene Hierarchy §3.2–3.3](../../software/06-unity-core-scene-hierarchy.md)

---

## 🛠 Technical Tasks

### U0 — Contracts C4, C7 (S) — ทำก่อน
- [x] `DayManager.cs` — `DayEndReport` + `PreviewFeeding()` + `OnDayEnded` (ทำแล้ว 2026-10-27)
- [x] `RunResources.cs` — `event Action OnChanged` + Core Item A/B (ทำแล้ว 2026-10-27)

### U1 — การ์ดยูนิต + ปุ่มให้อาหาร (M)
- [ ] สร้าง `Prefabs/UI/UnitCard.prefab` (C6) ใช้ `UnitIconView` ที่ขยายแล้ว: ชื่อ/portrait, AP เป็นจุด (pips), สถานะ, Hunger stack, trait (ดี/เสีย), aptitude (tooltip), ป้าย "ออกสำรวจแล้ววันนี้"
- [ ] `UnitRosterView` — ใช้ `UnitCard.prefab` แทน `ScoutIcon.prefab`; เปลี่ยนจาก poll ทุกเฟรมเป็น refresh ตาม event (`OnDayStarted`, `OnTripResolved`, C1, C7) ถ้าทำได้
- [ ] ปุ่ม **Feed** บนการ์ด เรียก `StarvationController.FeedUnit`; ปิดปุ่มพร้อม tooltip จาก `GetBlockReason`

### U2 — Panel ส่งสำรวจ + odds preview (M)
- [x] ~~`Prefabs/UI/DispatchPanel.prefab`~~ → แทนด้วย `DispatchActionPanel.prefab` + `CharacterHandPanel.prefab` (การ์ดสร้างจาก `UnitManager.Units` ตอน runtime แล้ว) — 2026-10-27
- [ ] เมื่อเลือกถ้ำ (C2 `OnNodeSelected`) แสดงระดับ, AP ต่อคน, ของหลัก, bonus pool
- [ ] เมื่อเลือกยูนิต แสดง **Success Value และ % ของ 4 tier** (คำนวณด้วย `GatherResolver.ComputeSuccessValue` + กติกาใน [GDD §4.4](../../gdd/01-mechanics.md#44-การทอย-1d100-roll-under-และระดับผล)) พร้อมเตือนเมื่อ SV < 50 ที่ถ้ำ Hard (Fumble 5% = เสี่ยงเสียยูนิต)
- [x] สูงสุด 2 คน, เหตุผลจาก `CanDispatch` แสดงผ่าน `WarningView` + log, **ไม่มี "ระหว่างทริป"** ให้ปิดปุ่มแล้ว
- [x] ลบของเดิมออกจาก scene แล้ว (2026-10-28) — `TestDispatch` ถูกเปลี่ยนชื่อเป็น `GatherUI` และ toggle/`MessageUi`/`Dispatch` เดิมถูกลบ

### U3 — ผลการสำรวจ, End Day, จบเกม (L)
- [x] ~~`GatherResultPanel.prefab`~~ → ใช้ `LogPanel` + `RunLogNarrator` แสดง tier, roll/SV, ของที่ได้, สถานะแต่ละคน, Core Item ที่ได้ (2026-10-27)
- [x] DayFrame = End Day และไม่ทำงานเมื่อ `DayManager.Outcome != InProgress` (ไม่มี `TripInProgress` แล้ว)
- [x] ไม่มี dialog ยืนยัน (ตามสเปก §4.4) — `PreviewFeeding()` ยังมีไว้ให้ HUD เตือนสีแดงเรื่อง Food ไม่พอ
- [ ] `Prefabs/UI/RunEndPanel.prefab` subscribe `OnRunEnded` — Win / หมดเวลา / ทีมหายหมด + สรุป (วันที่จบ, ใครรอด, trait) + ปุ่ม Restart → C5 `SceneFlowController.RestartRun()`
- [ ] `StatusBarView` — ไอคอนทรัพยากร, แถบ Core Item 3 ขั้น, ใช้ C7 แทน poll

---

## ✅ Acceptance Criteria
1. [ ] การ์ดยูนิตแสดง AP, สถานะ, ความหิว, trait ครบ และให้อาหารลด stack ได้จากการ์ด (1 Food, 1 ครั้ง/คน/วัน)
2. [ ] ก่อนกด Dispatch ผู้เล่นเห็น % ของทั้ง 4 tier ซึ่งตรงกับตารางใน [GDD §4.5](../../gdd/01-mechanics.md#45-ตารางโอกาสตัวอย่าง-ทีมยังไม่มี-trait)
3. [ ] เปลี่ยนลำดับ roster แล้ว panel ส่งสำรวจยังเลือกยูนิตถูกคน (ไม่พึ่ง index)
4. [ ] กด End Day ระหว่างทริป หรือหลัง run จบไม่ได้; มี dialog เตือนเมื่ออาหารไม่พอ
5. [ ] จบ run ทุกแบบแสดง `RunEndPanel` และ Restart ได้
6. [ ] ไม่มี UI ใด poll `Update()` เพื่ออ่านค่าที่มี event ให้แล้ว (ยกเว้นที่ระบุเหตุผล)

---

## 🔗 Coordination
- **ให้กลุ่ม 2:** C4 `OnDayEnded(DayEndReport)` · C6 `UnitCard.prefab`
- **ให้กลุ่ม 1:** C7 `RunResources.OnChanged`
- **ใช้จากกลุ่ม 2:** C2 `OnNodeSelected`, C3 ลำดับทริป, C5 `SceneFlowController`
- **ใช้จากกลุ่ม 1:** C1 `OnBaseWorkCompleted` (refresh การ์ด)
- **ห้ามแก้:** `GatherMapController`, `BaseWorkController`, subtree `SkillTriggerPanel`, `MapPanel` — ของเดิม (`BaseWorkPanel`, toggle, `Dispatch`, `MessageUi`) ถูกลบไปแล้วใน P5

## 📝 Docs to Update
- [gdd/01-mechanics.md §6](../../gdd/01-mechanics.md#6-starvation--ความหิว) (ปุ่มให้อาหารมี UI แล้ว), [§10](../../gdd/01-mechanics.md) · [gdd/03-art-direction.md §4](../../gdd/03-art-direction.md)
- [software/04 UI Layer, §4.3, §5, §6](../../software/04-unity-system-design.md) · [software/05](../../software/05-unity-class-diagram.md) · [software/06](../../software/06-unity-core-scene-hierarchy.md) (Canvas, ลบ TestDispatch)

## 🔗 Related Files
- [V2 Task Overview](V2-TASK-OVERVIEW.md)
- [US-V2-FACTORY-01](US-V2-FACTORY-01.md) · [US-V2-SCENE-01](US-V2-SCENE-01.md)
