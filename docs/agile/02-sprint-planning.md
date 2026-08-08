# Sprint Planning & Roadmap — clogged

**Last Updated:** 2026-08-08 | **Version:** 1.1
> Migrated from the original `plan.md` roadmap (4 เดือน, ~18 สัปดาห์, เริ่ม 29 มิ.ย. 2026 → ส่งมอบ ~1 พ.ย. 2026, ทีม 4 คน — ดู [Team Roster](team.md))

## 📅 Sprint Schedule Overview

| Phase | Timeline | Weeks | Focus Area | Milestone | Status |
|:---|:---|:---|:---|:---|:---|
| 0 — Pre-production | 2026-06-29 → 2026-07-12 | W1–W2 | ล็อกดีไซน์ + tech setup | GDD + Tech setup เสร็จ | 🟡 ล่าช้า — คำถามออกแบบหลักยังไม่ปิดอย่างเป็นทางการ แม้จะมี prototype โค้ดล่วงหน้าแล้ว (ดู [Design Pivot](../gdd/00-concept.md)) |
| [sprint-01](./sprint-backlogs/sprint-01.md) — Prototype | 2026-07-13 → 2026-08-09 | W3–W6 | พิสูจน์ว่า core loop สนุก (vertical slice) | เล่นจบ 1 ด่านได้ | 🔵 In Progress |
| 2a — Production Sprint 1 | 2026-08-10 → 2026-09-06 | W7–W9 | สร้างระบบหลักให้ Playable Loop | **M2 — Presentable Playable Loop** | ⬜ Not started |
| 2b — Production Sprint 2 | 2026-09-07 → 2026-09-20 | W10–W11 | Polish + Should Have | Feature-complete (Alpha) | ⬜ Not started |
| 3 — Content & Polish | 2026-09-21 → 2026-10-18 | W12–W16 | เนื้อหา + ขัดเกลา + บาลานซ์ | Content-complete (Beta) | ⬜ Not started |
| 4 — Test & Release | 2026-10-19 → 2026-11-01 | W17–W18 | เทส แก้บั๊ก ปล่อยเกม | Release build (Gold) | ⬜ Not started |

## 🎯 Milestones (Presentation + Presentation Timeline)

| # | Date | Milestone | Deliverable | ความก้าวหน้า |
|:---|:---|:---|:---|:---|
| M1 | 2026-08-09 | Sprint 01 Decision Gate | Core loop vertical slice เสร็จ | ✅ ใกล้เสร็จ |
| **M2** | **~2026-09-01** | **Presentable Playable Loop** | **Playable demo — ทีมเล่นครบ loop ได้, มีระบบหลักครบทุกอย่าง** | **⬜** |
| M3 | 2026-09-20 | Feature-complete (Alpha) | Must Have ครบทั้งหมด + เริ่ม Should Have | ⬜ |
| M4 | 2026-10-18 | Content-complete (Beta) | Should Have ครบ,  balance, polish แล้ว | ⬜ |
| M5 | 2026-11-01 | Release (Gold) | เทสเสร็จ, release build สำเร็จรูป | ⬜ |

## 🎯 M2: Presentable Playable Loop (เดือนกันยายน)

> เป้าหมาย: นำเสนอ demo ที่แสดง core loop ครบถ้วน — วิ่ง Planning → Mission → Night → Summary ได้ทุกขั้นตอน

**Us stories ที่ต้องเสร็จก่อน M2 (Prioritized):**

| Priority | User Story | สถานะปัจจุบัน |
|:---:|:---|:---|
| 🔴 P0 | US-MISSION-02 — Multi-crew collaborative missions | 🏗️ In-Progress |
| 🔴 P0 | US-BASE-02 — Base defense + scaling waves | 🏗️ In-Progress |
| 🟡 P1 | US-FOOD-02 — Starvation debuffs | 🏗️ In-Progress |
| 🟡 P1 | US-MAP-01 — Dynamic difficulty scaling | 🏗️ In-Progress |
| 🟢 P2 | US-TIME-02 — Day progression + state reset | 🏗️ In-Progress |
| 🟢 P2 | US-WARN-01 — Proactive warnings | 🟡 Partial |

**Timeline สำหรับ M2 (W7–W9):**

```
Aug 10–16  │ US-MISSION-02: Multi-crew mission logic + combined proficiency
Aug 17–20  │ US-BASE-02: Night wave scaling + base defense
Aug 21–23  │ US-FOOD-02: Starvation debuff system
Aug 24–27  │ US-MAP-01: Dynamic difficulty + resource distribution
Aug 28–31  │ Polish loop, fix bugs, team playtest (buffer)
Sep  1     │ **M2 — Presentable Playable Loop** ✅
```

**Definition of Done (M2):**
- ทีมนักเล่นสามารถวิ่ง core loop ครบ 30 วัน (playtest)
- ระบบ collaborative mission ทำงานได้ (หลาย crew ไป node เดียวกัน)
- Base defense มี scaling ที่เห็นความยากเพิ่มขึ้นตามวัน
- Food mechanic ส่งผลต่อ crew เมื่ออาหารหมด
- Map มี dynamic difficulty
- ไม่มี bug ที่เกิดลูปแตก / crash

## 🚀 Sprint Details

- **[sprint-01](./sprint-backlogs/sprint-01.md)**: Prototype / Vertical Slice — core loop (crew dispatch, mission, day/night, base defense) ส่วนใหญ่มีอยู่แล้วจากการทดลองก่อนเฟส 0 จบ งานที่เหลือคือปิดช่องว่างตาม [Product Backlog](01-product-backlog.md) (🟡/⬜ items) แล้ว playtest ในทีม
- **[Phase 2a (W7–W9)]**: สร้างระบบหลัก 4 ใช้รี่ให้เสร็จ → Presentable Playable Loop
- **[Phase 2b (W10–W11)]**: Polish, balance, เริ่ม Should Have (US-ECON-01, US-EQUIP-01)
- **[Phase 3 (W12–W16)]**: เนื้อหาเพิ่ม, content polish, gameplay balance
- **[Phase 4 (W17–W18)]**: เทสแก้บั๊ก, release build สำหรับ Gold

## หลักการบริหารแผน
- **เผื่อ buffer:** อย่าอัดงาน 100% ทุกสัปดาห์
- **MVP มาก่อนเสมอ:** ฟีเจอร์เสริมทำเมื่อ MVP เสถียรแล้ว
- **Playtest บ่อย:** ทดลองเล่นทุกสิ้นเฟส ไม่รอจบเกม
- **ระวัง scope creep:** ทุกฟีเจอร์ใหม่ต้องถามว่า "อยู่ใน MVP ไหม" (ดู [Product Backlog](01-product-backlog.md))

## 📊 Gantt Chart

```mermaid
gantt
    title clogged — Project Schedule (Jun – Nov 2026)

    dateFormat  YYYY-MM-DD
    axisFormat  %b %d
    excludes    weekends

    section Pre-production
    Phase 0: Pre-production           :milestone, 2026-06-29, 2026-07-12

    section Sprint 01
    Sprint 01: Prototype              :milestone, 2026-07-13, 2026-08-09

    section Production
    Phase 2a: Production Sprint 1     :2026-08-10, 2026-09-06
    Phase 2b: Production Sprint 2     :2026-09-07, 2026-09-20

    section Content & Polish
    Phase 3: Content & Polish         :milestone, 2026-09-21, 2026-10-18

    section Release
    Phase 4: Test & Release           :milestone, 2026-10-19, 2026-11-01
```

## Related Documents
- [Product Backlog](01-product-backlog.md)
- [Team Roster](team.md)
- [GDD Concept](../gdd/00-concept.md)
- [Sprint Backlogs](sprint-backlogs/)
