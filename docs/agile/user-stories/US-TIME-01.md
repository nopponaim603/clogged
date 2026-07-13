# User Story: US-TIME-01 - Day/Night Cycle

**Status:** ✅ Done (in prototype)
**Epic:** [Product Backlog](../01-product-backlog.md)
**Owner:** TBD

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** ให้เกมมีรอบกลางวัน (วางแผน/ปฏิบัติภารกิจ) และกลางคืน (ป้องกันฐาน) ที่ชัดเจน
**เพื่อให้** ความเสี่ยงและจังหวะการเล่นรู้สึกแตกต่างกันในแต่ละช่วง

---

## ✅ Acceptance Criteria
1. [x] กลางวันจำกัดเวลาการวางแผน+ปฏิบัติภารกิจ (`DAY_TIME_LIMIT`)
2. [x] กลางคืนสลับเป็นโหมดป้องกันฐาน ไม่สามารถสั่งภารกิจใหม่ได้
3. [x] วนครบ 30 วัน (`MAX_DAYS`) แล้วจบเกมด้วยชัยชนะ

---

## 🛠 Technical Tasks (Git Log Updates)
- [x] `systems/TimeSystem.ts` — planning/executing/night phase state machine
- [x] `scenes/GameScene.ts` — wiring onDayEnd/onNightEnd callbacks

---

## 🔗 Related Files
- Backlog: [Product Backlog](../01-product-backlog.md)
- GDD: [Mechanics — Time/Day-Night System](../../gdd/01-mechanics.md#time--day-night-system)
