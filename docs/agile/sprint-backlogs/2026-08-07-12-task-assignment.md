# Sprint Task Assignment — 2026-08-07 → 2026-08-12

**Sprint:** Sprint 01 (Prototype / Vertical Slice)
**Timeline:** 2026-08-07 (Thu) → 2026-08-12 (Tue)
**Team:** 4 คน

---

## 👤 อั้น — Unity Production

| # | Task | Details |
|---|------|---------|
| 1 | เปลี่ยนระบบการทำงานเป็น Queue-based | ปฏิบัติงานตามคิวที่กำหนด ไม่ใช่ event-driven ตรงๆ ทำให้ flow การเล่นเป็นระบบกว่างขึ้น |
| 2 | ทำหน้าสรุปผลตอนเช้า (Morning Summary) | UI แสดงผลลัพธ์การต่อสู้/ทรัพยากร/สถานะ crew หลังจบ Night Phase |
| 3 | ทำหน้า pop-up เลือก Crew (Crew Select) | UI pop-up ให้เลือก crew ก่อนส่งไปทำภารกิจ แสดงสถานะ HP/Proficiency |
| 4 | ทำหน้า navigator เลือก Node | UI map navigation ให้เลือก resource node / monster node / relic node เพื่อจ้าง crew ไป |

**Output:** Unity C# scripts ใน `Unity-Projects/Assets/` + UI prefabs

---

## 👤 ปาร์ค — Unity Production

| # | Task | Details |
|---|------|---------|
| 1 | เพิ่มระบบวางแผน (Planning System) | ระบบวางแผนการจ้าง crew ไป node ต่างๆ ในช่วง Day Phase ก่อนปฏิบัติจริง |
| 2 | เพิ่ม Base Defense Items | สร้างระบบ equipment/purchase สำหรับป้องกันฐาน — เช่น ป้อมปืน, ใช้ทรัพยากรซื้อในPlanning Phase |

**Output:** Unity C# scripts (PlanningManager, BaseDefenseSystem) + item data

---

## 👤 เอก

| # | Task | Details |
|---|------|---------|
| 1 | เตรียมเอกสารสำหรับ Present | สร้าง presentation deck สรุปความก้าวหน้า, core loop, และแผน Phase ถัดไป |
| 2 | Tracking Progression | จัดทำ trackable metrics — วัดความก้าวหน้าของแต่ละคน + overall sprint progress (kanban / burndown) |

**Output:** Presentation files + progress tracking document

---

## 👤 ไอซ์ — Web Prototype (Phaser)

| # | Task | Details |
|---|------|---------|
| 1 | ทำ Travel Scene ให้ครบ | Scene การเดินทางของ crew ไป-กลับ resource node + mission node — ต้องแสดง animation/feedback ไม่ใช่ teleport |
| 2 | ทำ Night Scene ให้ครบ | Scene การต่อสู้ตอนกลางคืน — monster wave, base defense, สุ่มการตั้งป้อมปืนของ player |

**Output:** `prototype_resource_game/src/scenes/` — TravelScene.ts + NightScene.ts (enhanced)

---

## 📊 Sprint Goal (ระยะ 7–12 ส.ค.)

> ปิด MVP core loop ที่ครบวงจรวิ่ง: **Planning → Mission Execution → Night Defense → Morning Summary → New Day**

Decision Gate เดิมกำหนด 2026-08-09 — ระยะนี้เป็น buffer เพื่อแก้ไข/ polish ก่อน final gate

---

## ✅ Definition of Done

- ทุก task ที่ owner รับผิดชอบเสร็จตาม acceptance criteria
- ทีมสามารถวิ่ง core loop ครบ 1 ด่านได้ ไม่ crash ไม่ stuck
- เอกสารและ presentation พร้อมสำหรับนำเสนอ

---

## 📎 Linked Documents

- [Sprint 01 Plan](./sprint-01.md)
- [Product Backlog](../01-product-backlog.md)
- [Progress Report 2026-08-07](../report-logs/2026-08-07-sprint-01-progress-report.md)
