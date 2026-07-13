# clogged — Product Backlog

**Last Updated:** 2026-07-13 | **Version:** 1.0
> Derived from [GDD — Core Mechanics](../gdd/01-mechanics.md) and cross-checked against the current state of `prototype_resource_game/src/` (เฟส 1 Prototype เริ่มวันนี้ แต่มีโค้ดพื้นฐานอยู่แล้วจากการทดลองก่อนหน้า)
> **Status legend:** ✅ Implemented in prototype · 🟡 Partial · ⬜ Not started

## Must Have (MVP)

| ID | User Story | Acceptance Criteria | Estimate | Status |
|----|-----------|---------------------|----------|--------|
| [US-CREW-01](./user-stories/US-CREW-01.md) | As a player, I want to hire and manage a crew so that I can send them on missions | จ้างลูกเรือด้วย point ได้, เห็นสถานะ available/busy | M | ✅ |
| [US-MISSION-01](./user-stories/US-MISSION-01.md) | As a player, I want to send crew to gather resources, search relics, or hunt monsters | เลือก crew → เลือก node → ได้ผลลัพธ์ตามประเภทภารกิจ | L | ✅ |
| US-MISSION-02 | As a player, I want to chain multiple missions or send multiple crew together | Multi-node chain ต่อ crew 1 คน, collaborative mission หลาย crew ต่อ 1 target | L | ✅ |
| [US-TIME-01](./user-stories/US-TIME-01.md) | As a player, I want a day/night cycle so planning and danger feel distinct | Day = planning+execution จำกัดเวลา, Night = ป้องกันฐาน | M | ✅ |
| US-BASE-01 | As a player, I want my base to be attacked at night so defense matters | มอนสเตอร์ spawn รอบฐานตอนกลางคืน + ลด base HP | M | ✅ |
| US-CLOG-01 | As a player, I want unused resources to decay so I'm punished for over-hoarding | ทรัพยากรที่เก็บแต่ไม่ใช้หายครึ่งตอนจบวัน | S | ✅ |
| US-WINLOSE-01 | As a player, I want clear win/lose conditions | ชนะ: อยู่ครบ 30 วัน · แพ้: crew ตายหมด หรือ base HP ≤ 0 | S | ✅ |
| US-FOOD-01 | As a player, I want food shortages to have real consequences | ตอนนี้คำนวณ shortage แต่ไม่มีผลลบ (crew ไม่บาดเจ็บ/ตายจากอดอาหาร) | M | 🟡 |
| US-WARN-01 | As a player, I want warnings when resources are about to clog or run out | มี NotificationSystem แต่ยังไม่มี proactive warning ก่อนจบวัน (เฉพาะ log ผลลัพธ์ภารกิจ) | S | 🟡 |
| US-SAVE-01 | As a player, I want to save/load my progress | ยังไม่มีระบบ save/load | M | ⬜ |

## Should Have

| ID | User Story | Acceptance Criteria | Estimate | Status |
|----|-----------|---------------------|----------|--------|
| US-ECON-01 | As a player, I want a currency/upgrade system beyond crew-hire points | ยกระดับเครื่องจักร/ทรัพยากรด้วย currency (plan.md เฟส 2) | L | ⬜ |
| US-PROG-01 | As a player, I want progression — harder challenges unlocked over time | โครงสร้างด่าน/ความยากที่เพิ่มตามวัน ยังไม่มีใน prototype (spawn count คงที่) | L | ⬜ |
| US-EQUIP-01 | As a player, I want to equip crew with weapons/armor/accessories | `Equipment` interface มีอยู่แล้วใน `Crew.ts` แต่ไม่มีระบบมอบ/ใช้งานจริง | M | 🟡 |
| US-UI-02 | As a player, I want readable status for pipes/machines (ตาม Idea-design เดิม) | ต้องตัดสินใจก่อนว่ายังต้องการ machine/pipe visualization หรือไม่ ดู [Design Pivot](../gdd/00-concept.md) | M | ⬜ |

## Nice to Have

| ID | User Story | Acceptance Criteria | Estimate | Status |
|----|-----------|---------------------|----------|--------|
| US-IDLE-01 | As a player, I want passive resource generation while not actively playing | แนวคิด Idle/Clicker จาก Idea-design เดิม — ยังไม่ตัดสินใจว่าจะใช้ | M | ⬜ |
| US-NARR-01 | As a player, I want a setting/theme framing the base and crew | Setting ยังไม่ระบุ (โรงงาน/ฐานอวกาศ/ท่อประปา) | S | ⬜ |
| US-ART-01 | As a player, I want visual polish beyond grayblock shapes | ตาม plan.md เฟส 3 (Content & Polish) | L | ⬜ |

## Linked GDD Features
- Derived from: [Core Mechanics](../gdd/01-mechanics.md), [Concept & Design Pivot](../gdd/00-concept.md)
- Code source of truth for ✅/🟡 status: `prototype_resource_game/src/`
