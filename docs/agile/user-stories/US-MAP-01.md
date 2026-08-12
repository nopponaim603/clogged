# User Story: US-MAP-01 - Dynamic Map Generation & Difficulty Scaling

**Status:** 🏗️ In-Progress
**Epic:** [Product Backlog](../01-product-backlog.md)
**Owner:** ทีม clogged

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** ให้การสุ่มสร้างแผนที่ในวันใหม่มีการกระจายระดับความยากและตำแหน่งทรัพยากรตามจำนวนวันที่ผ่านไป
**เพื่อให้** สภาพแวดล้อมในเกมมีพัฒนาการและความท้าทายมากขึ้นเรื่อยๆ ตลอด 30 วัน

---

## ✅ Acceptance Criteria
1. [ ] วันแรกๆ (Day 1–5): ททรัพยากรระดับ Common และสัดส่วนพื้นที่ปลอดภัยอยู่ใกล้ฐาน
2. [ ] วันหลังๆ (Day 15+): ทรัพยากรระดับ Rare/Epic อยู่ห่างจากฐานมากขึ้น และมีมอนสเตอร์ระดับสูงเฝ้าอยู่ตามเส้นทาง
3. [ ] เพิ่มระบบ Persistence สำหรับ Node ที่ถูกทำความเสียหายหรือเก็บเกี่ยวบางส่วน (HP ไม่เต็ม) ให้คงค่าเดิมตลอดวันเดียวกัน

---

## 🛠 Technical Tasks (Git Log Updates)
- [ ] `systems/MapGenerator.ts` — ปรับปรุงสูตร `generateResources()` โดยรับค่า `currentDay` เข้าไปคำนวณ Distance Factor และ Rarity Spawn Table
- [ ] `entities/ResourceNode.ts` — รองรับการบันทึกและรักษาสถานะ HP ของ Node
- [ ] `scenes/MainScene.ts` — ปรับแต่งกระบวนการ `drawMap()` ให้แสดงผลความยากและสถานะความเสียหายของ Node บนแผนที่

---

## 🔗 Related Files
- Backlog: [Product Backlog](../01-product-backlog.md)
- GDD: [Concept — Game Loop](../../gdd/00-concept.md#2-game-loop)
- Mechanics: [Core Mechanics — Map Generation](../../gdd/01-mechanics.md)
