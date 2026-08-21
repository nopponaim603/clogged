# Weekly Progress Report: สัปดาห์ที่ 33–34 (10 ส.ค. 2026 – 21 ส.ค. 2026)

**Project:** clogged
**Sprint:** [Sprint 02a: Production Sprint 1 — Presentable Playable Loop](../../sprint-backlogs/sprint-02a.md) (2026-08-10 → 2026-09-06)
**Date:** 2026-08-21
**Author:** ทีมพัฒนา clogged & Antigravity AI Assistant

---

## 📌 Executive Summary (ภาพรวมประจำสัปดาห์)

ในช่วงสัปดาห์ที่ 33–34 (ช่วงครึ่งแรกของ Sprint 02a) ทีมพัฒนาได้มุ่งเน้นการขยายสถาปัตยกรรมทั้งสองฝั่งอย่างมีนัยสำคัญ:

1. **Unity 2D Engine**: พัฒนาระบบ UI กลางวัน (Day Scene) และระบบเดินเรือนำทาง ([ShipNavigatorUI.cs](file:///c:/Users/noppon/source/04_UNITY/clogged/Unity-Projects/Assets/Day/Scripts/UI/ShipNavigatorUI.cs)) พร้อมสรุปผลรายวัน ([DayEndSummaryUI.cs](file:///c:/Users/noppon/source/04_UNITY/clogged/Unity-Projects/Assets/Day/Scripts/UI/DayEndSummaryUI.cs)) และระบบรับสมัครลูกเรือ ([RecruitEventUI.cs](file:///c:/Users/noppon/source/04_UNITY/clogged/Unity-Projects/Assets/Day/Scripts/UI/RecruitEventUI.cs))
2. **Phaser Web Prototype**: ยกระดับระบบ State Persistence ข้ามฉากระหว่าง `TravelScene.ts` กับ `NightScene.ts` ทำให้ข้อมูลแผนที่และการเดินทางคงอยู่ตลอดลูป พร้อมทั้งปรับสมดุลทรัพยากรเริ่มต้นใน `ResourceManager.ts`
3. **Architecture & Tooling**: จัดทำเอกสาร Software Class Design ฉบับเต็ม ([03-class-design.md](file:///c:/Users/noppon/source/04_UNITY/clogged/docs/software/03-class-design.md)) และติดตั้ง Agentic AI Infrastructure / Unity MCP Server สำหรับสนับสนุนการพัฒนา

---

## 👥 สรุปผลงานรายบุคคล (Individual Contributions & Accomplishments)

### 1. 🧑‍💻 อั้น (ปริทัศน์ เผ่าตัน — `Parrykung / Aunn`)

**บทบาท / โมดูล:** Unity 2D Day Phase & Navigation & UI Architecture

- 🟢 **ระบบนำทางและเลือกเส้นทางเดินเรือ (Ship Navigation System)**:
  - พัฒนา [ShipNavigatorUI.cs](file:///c:/Users/noppon/source/04_UNITY/clogged/Unity-Projects/Assets/Day/Scripts/UI/ShipNavigatorUI.cs) และ [ShipNodeOption.cs](file:///c:/Users/noppon/source/04_UNITY/clogged/Unity-Projects/Assets/Day/Scripts/UI/ShipNodeOption.cs) สำหรับควบคุมหน้าต่างเลือกโหนดแผนที่และการเดินทาง
  - ปรับปรุง [GameManager.cs](file:///c:/Users/noppon/source/04_UNITY/clogged/Unity-Projects/Assets/Day/Scripts/System/GameManager.cs) ให้รองรับการมอบผลประโยชน์และโบนัสทรัพยากรเมื่อผู้เล่นเลือก Bonus Node
- 🟢 **แผง UI สรุปผลสิ้นวันและอีเวนต์ลูกเรือ (Summary & Event Panels)**:
  - พัฒนา [DayEndSummaryUI.cs](file:///c:/Users/noppon/source/04_UNITY/clogged/Unity-Projects/Assets/Day/Scripts/UI/DayEndSummaryUI.cs) แสดงสรุปผลทรัพยากรที่ได้รับและความเสียหายในแต่ละวัน
  - พัฒนา [RecruitEventUI.cs](file:///c:/Users/noppon/source/04_UNITY/clogged/Unity-Projects/Assets/Day/Scripts/UI/RecruitEventUI.cs) สำหรับหน้าต่างโต้ตอบการรับลูกเรือใหม่เข้าสู่ยาน
- 🟢 **ปรับปรุงโครงสร้าง Unity System**:
  - จัดระเบียบ C# Scripts ฝั่ง Day เข้าโฟลเดอร์ `Assets/Day/Scripts/System/`
  - ทำความสะอาดและอัปเดตฉากหลัก [ShipDay.unity](file:///c:/Users/noppon/source/04_UNITY/clogged/Unity-Projects/Assets/Day/Scenes/ShipDay.unity) ให้มีความพร้อมในการเชื่อมต่อ

---

### 2. 🎮 ไอซ์ (อุภัยภัทร ลาภมาก — `SggRKunG`)

**บทบาท / โมดูล:** Phaser Prototype Core Mechanics, State Persistence & Game Balancing

- 🟢 **ระบบ State Management ข้ามฉาก (Travel State Persistence)**:
  - สร้างโครงสร้าง `travelState` เพื่อส่งผ่านข้อมูลตำแหน่งปัจจุบัน, โหนดที่เคยสำรวจแล้ว, และดัชนีวัน ระหว่าง `NightScene.ts` กับ `TravelScene.ts` ทำให้ข้อมูลไม่สูญหายเมื่อเปลี่ยนฉาก
- 🟢 **ปรับปรุงระบบ Travel Scene (`TravelScene.ts`)**:
  - ยกระดับตรรกะ Map Generation ให้รองรับ Day Index และโหนดประเภทต่างๆ
  - เพิ่ม Keyboard Shortcuts และ Interactive Feedback เมื่อผู้เล่นเลือกโหนด
- 🟢 **ปรับปรุง Night Scene และการปรับสมดุลเกม (Game Balancing)**:
  - อัปเดตตรรกะใน `NightScene.ts` ให้บันทึกและส่งผ่านผลลัพธ์กลับสู่เกมลูป
  - ปรับเพิ่มทรัพยากรเริ่มต้นใน `ResourceManager.ts` ให้ผู้เล่นไม่ติดขัดในช่วงต้นเกมและมี Gameplay Experience ที่ดีขึ้น

---

### 3. 🏛️ นพพล (อาจารย์ / Tech Lead — `noppon wongta / noppon nitro camt`)

**บทบาท / โมดูล:** Software Class Architecture, AI Tools & Agile Governance

- 🟢 **จัดทำเอกสาร Software Class Design**:
  - เขียนและจัดทำเอกสารสถาปัตยกรรมระบบ [03-class-design.md](file:///c:/Users/noppon/source/04_UNITY/clogged/docs/software/03-class-design.md) ครอบคลุม Class Diagrams และ Component Hierarchy ทั้งฝั่ง Unity 2D C# และ Phaser TypeScript
- 🟢 **วางระบบ Agentic AI Infrastructure**:
  - คอนฟิก AI Provider (NapLab AI Provider / Naplab-vllm) และเชื่อมต่อ Unity MCP Server ใน `.kilo/kilo.json` และ `.claude/settings.json` เพื่อรองรับการควบคุม Unity Editor ผ่าน Agent
- 🟢 **บริหารจัดการ Agile Artifacts**:
  - วางแผน Sprint Backlogs ([Sprint 02a, 02b, 03](../../sprint-backlogs/sprint-02a.md)) และ Mermaid Gantt Chart มุ่งสู่เป้าหมาย Milestone M2

---

### 4. 🧭 ปาร์ค (วงศกร ทองคำพันธ์) & ⚔️ เอก (เอกราช สถานสถิตย์)

**บทบาท / โมดูล:** Dynamic Map Scaling & Unity Combat System

- 🟡 **สถานะการดำเนินงาน**: อยู่ระหว่างเตรียมงานและพัฒนาตาม Sprint 02a Backlog:
  - **ปาร์ค:** วิจัยและจัดเตรียมโครงสร้าง Dynamic Map Scaling ตามระยะทาง ([US-MAP-01](../../user-stories/US-MAP-01.md))
  - **เอก:** จัดเตรียมกราฟิก Asset และตรวจสอบระบบ Enemy Combat / Spawner ในฝั่ง Unity

---

## ⚙️ Technical Progress Summary (สรุปไฟล์ที่มีการพัฒนา)

### 1. Unity 2D (`Unity-Projects/Assets/`)

- `Assets/Day/Scripts/UI/ShipNavigatorUI.cs` — ระบบ UI เลือกโหนดการเดินเรือ
- `Assets/Day/Scripts/UI/ShipNodeOption.cs` — คอมโพเนนต์ตัวเลือกโหนด
- `Assets/Day/Scripts/UI/DayEndSummaryUI.cs` — หน้าต่างสรุปผลเมื่อจบวัน
- `Assets/Day/Scripts/UI/RecruitEventUI.cs` — หน้าต่างอีเวนต์รับลูกเรือใหม่
- `Assets/Day/Scripts/System/GameManager.cs` — ระบบจัดการเกมและโบนัสจากโหนดนำทาง
- `Assets/Day/Scenes/ShipDay.unity` — ฉากหลักฝั่งกลางวันที่มี UI ครบครัน

### 2. Phaser Prototype (`prototype_resource_game/src/`)

- `src/scenes/TravelScene.ts` — ระบบแผนที่ การเดินทาง และ State Persistence
- `src/scenes/NightScene.ts` — ระบบส่งผ่าน Travel State และจัดการลูปกลางคืน
- `src/scenes/DayScene.ts` — การเชื่อมต่อ State กับ Travel Flow
- `src/systems/ResourceManager.ts` — ปรับสมดุล Resource เริ่มต้น

### 3. Documentation & Tooling (`docs/`, `.kilo/`, `.agents/`)

- `docs/software/03-class-design.md` — เอกสาร Class Design ครบวงจร
- `docs/agile/sprint-backlogs/sprint-02a.md` — แผนงาน Sprint 02a
- `.kilo/kilo.json` — คอนฟิก NapLab Provider & Unity MCP Server

---

## ⚠️ Missing Systems & Blockers (ระบบที่ยังขาดและประเด็นสำคัญ)

1. **Unity Day ↔ Night Bridge**:
   - **สิ่งที่ต้องทำ**: นำ UI ระบบกลางวันของ `ShipDay.unity` เชื่อมต่อเข้ากับ `SampleScene.unity` (Night Combat) ผ่าน Scene Switcher อัตโนมัติ
2. **Food Shortage Debuff ([US-FOOD-02](../../user-stories/US-FOOD-02.md))**:
   - **สิ่งที่ต้องทำ**: เชื่อมต่อบทลงโทษ Starvation ลดค่า Stat ความเร็วและประสิทธิภาพการทำงานของลูกเรือ 30–50%
3. **Dynamic Distance Difficulty Scaling ([US-MAP-01](../../user-stories/US-MAP-01.md))**:
   - **สิ่งที่ต้องทำ**: ปรับให้โหนดที่อยู่ห่างไกลมีความยากและของรางวัลที่ทวีคูณขึ้นตามระยะทาง

---

## 🎯 Next Week Priorities (แผนงานสำคัญสัปดาห์ถัดไป)

1. **เชื่อมต่อ Scene Bridge ใน Unity (Day ↔ Night)**: รวม `ShipDay.unity` และ `Night Scene` เข้าด้วยกันเพื่อเริ่มทดสอบ Playable Loop บน Unity
2. **พัฒนาระบบ Starvation Debuffs ([US-FOOD-02](../../user-stories/US-FOOD-02.md))** บน Phaser / Unity
3. **จัดทำระบบ Proactive Warning UI ([US-WARN-01](../../user-stories/US-WARN-01.md))** แจ้งเตือนทรัพยากรล้น/ขาดก่อนกดจบวัน

---

## 📝 Meeting Notes & Discussion Points (บันทึกการประชุม / ประเด็นเพิ่มเติม)

> 💡 **ใช้สำหรับจดบันทึกระหว่างการประชุมประจำสัปดาห์ / สรุปประเด็นหารือเพิ่มเติม**

### 1. ข้อมูลการประชุม (Meeting Info)

* **วัน-เวลา:** 21 สิงหาคม 2026 (13:00 - 15:00 น.)
* **ผู้เข้าร่วมประชุม:** [ ] อ.นพพล  [ ] อั้น (ปริทัศน์)  [ ] ไอซ์ (อุภัยภัทร)  [ ] ปาร์ค (วงศกร)  [ ] เอก (เอกราช)
* **วัตถุประสงค์หลัก:** ติดตามผลงานครึ่งแรกของ Sprint 02a, ตรวจสอบความคืบหน้าของ Core Loop, วางแผนเชื่อมต่อ Scene ใน Unity และเตรียมตัวสู่ Milestone M2
* **สไลด์นำเสนอประกอบการประชุม:** [Clogged Prototype present - Presentation (Canva)](https://www.canva.com/design/DAHSC0yfx-w/GwRlzDGOZ8BZYP7UyeZClw/edit)

---

### 2. ประเด็นหารือและข้อเสนอแนะในที่ประชุม (Discussion Topics & Feedback)

#### 🔹 ด้าน Game Design & Balancing (การเล่นและสมดุลเกม)

* **ทรัพยากรและการเอาชีวิตรอด:**
  * *(บันทึกความคิดเห็นเรื่องอัตราการใช้ทรัพยากรเริ่มต้น / ความตึงเครียดของระบบอาหาร)*
* **ระบบนำทางและการเลือกเส้นทาง (Ship Navigation):**
  * *(ข้อเสนอแนะเรื่องความหลากหลายของ Node types, Bonus Node, และ Event Encounter)*
* **Night Defense / Combat Mechanics:**
  * *(ประเด็นเรื่องความยากของมอนสเตอร์ และความสามารถของ Turret / Player Units)*

#### 🔹 ด้าน Technical & Architecture (การพัฒนาเชิงเทคนิค)

* **Unity Scene Transition (Day ↔ Night Bridge):**
  * *(แนวทางการผสาน GameManager ฝั่ง Day และ Night)*
* **Data Persistence & Save State:**
  * *(การจัดเก็บข้อมูลลูกเรือและสถิติข้ามวันบน Unity C#)*
* **Asset & Animation Pipeline:**
  * *(สถานะการนำเข้า Spritesheet, VFX, และ UI Assets)*

---

### 3. มติที่ประชุม & Action Items ที่ได้รับมอบหมายเพิ่ม (Decisions & Action Items)

| ลำดับ | รายละเอียดงาน (Task / Action Item)                                                                                                         | ผู้รับผิดชอบ (Owner) | กำหนดส่ง (Due Date) | สถานะ |
| :--------: | ------------------------------------------------------------------------------------------------------------------------------------------------------- | :------------------------------: | :-------------------------: | :--------: |
|     1     | แก้หน้าเลือก Node - มี Bonuse                                                                                                             |             อั็น             |                            | ⬜ Pending |
|     2     | เพิ่มฉากเลือกตัวละคร วาง Unit กำลังทำระบบอื่นต่อ วางป้อม (ป้องกัน, ป้อมปืน, กับดัก) |            ปาร์ค            |                            | ⬜ Pending |
|     3     |                                                                                                                                                         |                                  |                            | ⬜ Pending |

---

### 4. บันทึกเพิ่มเติม / คำถามที่ต้องหาคำตอบ (Open Questions & Extra Notes)

---

## 🔗 Related Documents

- [สไลด์นำเสนอครั้งที่ 1 (Canva)](https://www.canva.com/design/DAHSC0yfx-w/GwRlzDGOZ8BZYP7UyeZClw/edit)
- [Product Backlog](../../01-product-backlog.md)
- [Sprint Planning Overview](../../02-sprint-planning.md)
- [Sprint 02a Backlog](../../sprint-backlogs/sprint-02a.md)
- [Software Class Design](../../../software/03-class-design.md)
- [Report Backlog Hub](../../05-report-backlog.md)
