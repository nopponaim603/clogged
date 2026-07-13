# User Story: US-MISSION-01 - Gather / Search / Hunt Missions

**Status:** ✅ Done (in prototype)
**Epic:** [Product Backlog](../01-product-backlog.md)
**Owner:** TBD

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** ส่งลูกเรือไปเก็บทรัพยากร ค้นหาโบราณวัตถุ หรือล่ามอนสเตอร์
**เพื่อให้** ฉันได้ทรัพยากรและของหายากมาบริหารฐาน

---

## ✅ Acceptance Criteria
1. [x] เลือก crew แล้วเลือก resource node เพื่อสั่งเก็บทรัพยากร
2. [x] เลือก relic node เพื่อค้นหาโบราณวัตถุ (มีโอกาสสำเร็จ/ไม่สำเร็จ)
3. [x] เลือก monster node เพื่อล่ามอนสเตอร์ (เสี่ยง crew บาดเจ็บ/ตาย)
4. [x] ถ้าเวลาที่เหลือในวันไม่พอสำหรับภารกิจเต็มรูปแบบ ระบบให้ผลลัพธ์บางส่วนแทนการล้มเหลวทั้งหมด

---

## 🛠 Technical Tasks (Git Log Updates)
- [x] `entities/ResourceNode.ts` — action-time calculation ตามประเภท node
- [x] `systems/MissionSystem.ts` — executeGathering / executeRelicSearch / executeMonsterHunt
- [x] `ui/MissionDisplay.ts`, `ui/NotificationSystem.ts` — feedback ผลภารกิจ

---

## 🔗 Related Files
- Backlog: [Product Backlog](../01-product-backlog.md)
- GDD: [Mechanics — Mission System](../../gdd/01-mechanics.md#mission-system)
- Software: [System Design — MissionSystem](../../software/01-system-design.md)
