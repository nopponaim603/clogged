# Sprint 02a: Production Sprint 1 — Presentable Playable Loop

**Goal:** รวมระบบ Unity 2D C# Architecture (Day ↔ Night Bridge) + เติมระบบหลักที่ขาดให้ครบวงรอบ Playable Loop 30 วัน  
**Timeline:** 2026-08-10 → 2026-09-06 (W7–W9)  
**Target Milestone:** [M2 — Presentable Playable Loop (~2026-09-01)](../02-sprint-planning.md#milestones-presentation--presentation-timeline)  

---

## 📅 Timeline Breakdown

```mermaid
gantt
    title Sprint 02a Tasks (W7–W9)
    dateFormat  YYYY-MM-DD
    axisFormat  %b %d
    
    section Unity Bridge
    Unity Scene Transition (Day ↔ Night) :a1, 2026-08-10, 7d
    
    section Core Mechanics
    US-FOOD-02 Starvation Debuffs (-30% Spd, -50% Prof) :a2, 2026-08-17, 4d
    US-MAP-01 Dynamic Map & Difficulty Scaling :a3, 2026-08-21, 4d
    US-WARN-01 Resource & Food Warning UI :a4, 2026-08-24, 4d
    
    section Playtest & M2
    Polish loop, fix bugs & internal playtest :a5, 2026-08-28, 4d
    M2 Presentable Playable Loop Sign-off :milestone, m2, 2026-09-01, 0d
```

---

## 📋 Committed Stories & Tasks

| ID | Story / Task | Owner | Estimate | Status |
|----|--------------|-------|----------|--------|
| **Unity-Bridge** | เชื่อมระบบ `GameManager.cs` ระหว่าง `ShipDay` และ `Night` (`SampleScene.unity`) | อั้น / ปาร์ค | L | ⬜ Not started |
| [US-FOOD-02](../user-stories/US-FOOD-02.md) | Food shortage starvation debuffs (-30% speed, -50% proficiency, HP decay) | ไอซ์ | M | 🟡 In Progress |
| [US-MAP-01](../user-stories/US-MAP-01.md) | Dynamic map generation & distance difficulty scaling ตามวัน | ปาร์ค | M | 🟡 In Progress |
| US-WARN-01 | Proactive warning UI ก่อนจบวันกรณีทรัพยากรล้น/ขาดอาหาร | อั้น | S | 🟡 In Progress |
| **Playtest-M2** | ทดสอบความเสถียรของ Core Loop 30 วันในทีม (ไม่มี crash/ลูปแตก) | ทั้งทีม | M | ⬜ Not started |

---

## 🎯 Definition of Done (M2 Gate)

- ทีมนักเล่นสามารถวิ่ง Core Loop บน Unity Engine ครบ 30 วันโดยไม่มีข้อผิดพลาดร้ายแรง (Crash)
- Multi-crew collaborative mission ทำงานและแสดงผลถูกต้อง
- Base Defense ในฉากกลางคืนมีความยากไต่ระดับตามจำนวนวัน
- อาหารหมดส่งผลกระทบต่อลูกเรือด้วยสถานะ `Starving`
- แผนที่มีการสุ่มความยากกระจายตามระยะทางจากฐาน
- Presentation demo พร้อมส่งมอบแก่อาจารย์/ผู้ประเมิน

---

## 🔗 Related Documents

- [Product Backlog](../01-product-backlog.md)
- [Sprint Planning Overview](../02-sprint-planning.md)
- [Sprint 01 Backlog](./sprint-01.md)
