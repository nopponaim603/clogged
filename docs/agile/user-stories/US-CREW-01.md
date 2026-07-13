# User Story: US-CREW-01 - Hire & Manage Crew

**Status:** ✅ Done (in prototype)
**Epic:** [Product Backlog](../01-product-backlog.md)
**Owner:** TBD

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** จ้างและบริหารลูกเรือ
**เพื่อให้** ฉันมีกำลังคนส่งออกไปทำภารกิจแต่ละวัน

---

## ✅ Acceptance Criteria
1. [x] จ้างลูกเรือได้ด้วย point ที่มีจำกัด (`CREW_POINTS`)
2. [x] ลูกเรือแต่ละคนมีค่า speed/gathering/searching/hunting/perk แตกต่างกัน
3. [x] เห็นสถานะ available/busy ของลูกเรือแต่ละคนแบบเรียลไทม์
4. [x] ลูกเรือตายถาวรเมื่อ HP ≤ 0

---

## 🛠 Technical Tasks (Git Log Updates)
- [x] `entities/Crew.ts` — stat model + effective-stat calculation
- [x] `systems/CrewManager.ts` — hire/assign/complete mission lifecycle
- [x] `ui/CrewPanel.ts` — แสดงสถานะลูกเรือ

---

## 🔗 Related Files
- Backlog: [Product Backlog](../01-product-backlog.md)
- GDD: [Mechanics — Crew System](../../gdd/01-mechanics.md#crew-system)
- Software: [System Design — CrewManager](../../software/01-system-design.md)
