# clogged — Product Backlog

**Last Updated:** 2026-08-03 | **Version:** 1.1
> Derived from [GDD — Core Mechanics](../gdd/01-mechanics.md) and cross-checked against the current state of `prototype_resource_game/src/` (Game Loop Audit 2026-08-03)
> **Status legend:** ✅ Implemented in prototype · 🏗️ In-Progress / Task Defined · 🟡 Partial · ⬜ Backlog

## Must Have (MVP)

| ID | User Story | Acceptance Criteria | Estimate | Status |
|----|-----------|---------------------|----------|--------|
| [US-CREW-01](./user-stories/archives/US-CREW-01.md) | As a player, I want to hire and manage a crew so that I can send them on missions | จ้างลูกเรือด้วย point ได้, เห็นสถานะ available/busy | M | ✅ |
| [US-MISSION-01](./user-stories/archives/US-MISSION-01.md) | As a player, I want to send crew to gather resources, search relics, or hunt monsters | เลือก crew → เลือก node → ได้ผลลัพธ์ตามประเภทภารกิจ | L | ✅ |
| [US-MISSION-02](./user-stories/US-MISSION-02.md) | As a player, I want collaborative missions for multiple crew on same node | Multi-crew ไปจุดเดียวกัน คำนวณ combined proficiency และลดเวลาร่วมกัน | L | ✅ |
| [US-TIME-01](./user-stories/archives/US-TIME-01.md) | As a player, I want a day/night cycle so planning and danger feel distinct | Day = planning+execution จำกัดเวลา, Night = ป้องกันฐาน | M | ✅ |
| [US-TIME-02](./user-stories/US-TIME-02.md) | As a player, I want correct day progression and clean state reset | แก้ไข bug การนับวันข้าม 2 วัน และ reset state เมื่อเริ่มใหม่ | S | ✅ |
| [US-BASE-02](./user-stories/US-BASE-02.md) | As a player, I want active base defense by crew and scaling night attacks | ลูกเรือในฐานช่วยต่อสู้ และคลื่นมอนสเตอร์ทวีความยากตามวัน | M | ✅ |
| [US-FOOD-02](./user-stories/US-FOOD-02.md) | As a player, I want food shortages to cause starvation debuffs before death | อาหารหมดเกิดสถานะ Starving (ลด speed/stats, ลด HP) ก่อนตาย | M | 🏗️ |
| [US-MAP-01](./user-stories/US-MAP-01.md) | As a player, I want dynamic map generation and difficulty scaling | ความยากและการกระจายตัวทรัพยากรเพิ่มขึ้นตามวัน | M | 🏗️ |
| US-CLOG-01 | As a player, I want unused resources to decay so I'm punished for over-hoarding | ทรัพยากรที่เก็บแต่ไม่ใช้หายครึ่งตอนจบวัน | S | ✅ |
| US-WINLOSE-01 | As a player, I want clear win/lose conditions | ชนะ: อยู่ครบ 30 วัน · แพ้: crew ตายหมด หรือ base HP ≤ 0 | S | ✅ |
| US-WARN-01 | As a player, I want warnings when resources are about to clog or run out | มี NotificationSystem แต่ยังไม่มี proactive warning ก่อนจบวัน | S | 🟡 |
| US-SAVE-01 | As a player, I want to save/load my progress | ยังไม่มีระบบ save/load | M | ⬜ |

## Should Have

| ID | User Story | Acceptance Criteria | Estimate | Status |
|----|-----------|---------------------|----------|--------|
| [US-EQUIP-01](./user-stories/US-EQUIP-01.md) | As a player, I want to equip crew with weapons/armor/accessories | ระบบ Crafting และสวมใส่อุปกรณ์เพิ่มค่าพลังร่วมกับ Perk | M | ⬜ |
| US-ECON-01 | As a player, I want a currency/upgrade system beyond crew-hire points | ยกระดับเครื่องจักร/ทรัพยากรด้วย currency | L | ⬜ |
| US-UI-02 | As a player, I want readable status for pipes/machines | ต้องตัดสินใจก่อนว่ายังต้องการ machine/pipe visualization หรือไม่ | M | ⬜ |

## Nice to Have

| ID | User Story | Acceptance Criteria | Estimate | Status |
|----|-----------|---------------------|----------|--------|
| US-IDLE-01 | As a player, I want passive resource generation while not actively playing | แนวคิด Idle/Clicker จาก Idea-design เดิม | M | ⬜ |
| US-NARR-01 | As a player, I want a setting/theme framing the base and crew | Setting ยังไม่ระบุ (โรงงาน/ฐานอวกาศ/ท่อประปา) | S | ⬜ |
| US-ART-01 | As a player, I want visual polish beyond grayblock shapes | ตาม plan.md เฟส 3 (Content & Polish) | L | ⬜ |

## Linked GDD Features
- Derived from: [Core Mechanics](../gdd/01-mechanics.md), [Concept & Design Pivot](../gdd/00-concept.md)
- Code source of truth for ✅/🏗️/🟡 status: `prototype_resource_game/src/`
