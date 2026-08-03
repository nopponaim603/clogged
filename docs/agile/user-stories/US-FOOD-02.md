# User Story: US-FOOD-02 - Starvation & Crew Health Decay

**Status:** 🏗️ In-Progress
**Epic:** [Product Backlog](../01-product-backlog.md)
**Owner:** ทีม clogged

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** ให้การขาดแคลนอาหารส่งผลกระทบเป็นสถานะความหิวและลดพลังชีวิตของลูกเรือเป็นขั้นตอนก่อนตาย
**เพื่อให้** การจัดการอาหารมีความลึกซึ้งและไม่จบเกมทันทีโดยไม่มีโอกาสแก้ไขสถานการณ์

---

## ✅ Acceptance Criteria
1. [ ] หากอาหารไม่พอแจกจ่ายในวันใหม่ ลูกเรือจะเข้าสู่สถานะ `Starving` (ติด Debuff ความเร็วเดินทางลดลง 30%, Proficiency ลดลง 50%)
2. [ ] ลูกเรือที่ติดสถานะ `Starving` จะเสีย HP 20% เมื่อขึ้นวันใหม่
3. [ ] หากผู้เล่นหาอาหารมาเติมทัน ลูกเรือจะหายจากสถานะ `Starving`
4. [ ] เกมจะ Game Over (Starvation) ก็ต่อเมื่อลูกเรือทุกคนเสียชีวิตจากการอดอาหาร หรืออาหารหมดติดต่อกันเกิน 3 วัน

---

## 🛠 Technical Tasks (Git Log Updates)
- [ ] `entities/Crew.ts` — เพิ่มสถานะ `isStarving` และคำนวณ Effective Stats หักลบจาก Debuff
- [ ] `systems/ResourceManager.ts` — ปรับฟังก์ชัน `consumeFood()` ให้ส่งคืนค่าเป็นจำนวนส่วนขาด (Shortage) แทนการสั่ง Game Over ทันที
- [ ] `scenes/GameScene.ts` — ปรับแต่งกระบวนการ `startNewDay()` เพื่อปรับปรุงการแจ้งเตือนและการลด HP ของลูกเรือที่อดอาหาร

---

## 🔗 Related Files
- Backlog: [Product Backlog](../01-product-backlog.md)
- GDD: [Concept — Win / Lose Conditions](../../gdd/00-concept.md#win--lose-conditions)
