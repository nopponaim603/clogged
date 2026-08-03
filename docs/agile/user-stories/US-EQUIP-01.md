# User Story: US-EQUIP-01 - Equipment Crafting & Perk Synergy

**Status:** ⬜ Backlog
**Epic:** [Product Backlog](../01-product-backlog.md)
**Owner:** ทีม clogged

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** สร้างอุปกรณ์ (Equipment) จากทรัพยากรและชิ้นส่วนมอนสเตอร์เพื่อนำมาสวมใส่ให้ลูกเรือ
**เพื่อให้** เพิ่มค่าพลัง ความเร็ว หรือความถนัดของลูกเรือ เกิดเป็น Synergy ร่วมกับ Perk ประจำตัวของลูกเรือ

---

## ✅ Acceptance Criteria
1. [ ] มีระบบ Crafting อุปกรณ์จากทรัพยากร (Iron, Circuit, Aluminum, Monster Parts)
2. [ ] ผู้เล่นสามารถเลือกสวมใส่/ถอดเปลี่ยนอุปกรณ์ให้ลูกเรือใน Planning Phase ได้
3. [ ] อุปกรณ์เพิ่มค่า Stat (Travel Speed, Gathering, Searching, Hunting) และมี Synergy กับ Perk ของลูกเรือเฉพาะประเภท

---

## 🛠 Technical Tasks (Git Log Updates)
- [ ] `entities/Crew.ts` — ขยายการคำนวณ `getEffectiveStats()` ให้รวมค่าโบนัสจาก Equipment ที่สวมใส่อยู่
- [ ] `ui/PlanningPanel.ts` / `ui/CrewPanel.ts` — เพิ่ม UI สำหรับเลือก Craft และสวมใส่อุปกรณ์ให้ลูกเรือ

---

## 🔗 Related Files
- Backlog: [Product Backlog](../01-product-backlog.md)
- GDD: [Concept — Unique Selling Points](../../gdd/00-concept.md#unique-selling-points-usp)
