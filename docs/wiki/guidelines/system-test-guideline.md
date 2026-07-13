# System Test Guideline — clogged

**Last Updated:** 2026-07-13
> ร่างเริ่มต้น — ทีมยังไม่ได้ทดสอบระบบอย่างเป็นทางการ (เฟส 1 เพิ่งเริ่ม) ปรับปรุงเอกสารนี้เมื่อเริ่มมี test report จริงใน [Report Backlog](../../agile/05-report-backlog.md)

## หลักการ
- **อ่าน guideline ที่เกี่ยวข้องก่อนเริ่มงาน dev หรือ test ทุกครั้ง**
- ทดสอบ core loop ใหม่ทุกครั้งที่แก้ `systems/` หรือ `entities/` ใน `prototype_resource_game/src/`
- บันทึกผลลง `docs/agile/reports/` แยกตามหมวด (`qa/`, `polish/`, `bugs/`) แล้วอัปเดตลิงก์ใน [Report Backlog](../../agile/05-report-backlog.md)

## Checklist ขั้นต่ำก่อน merge (เฟส 1)
- [ ] เล่นจบ 1 วันเต็ม (planning → execution → night → new day) โดยไม่ crash
- [ ] Win condition (30 วัน) และ Lose conditions (crew ตายหมด / base HP ≤ 0) ทำงานถูกต้อง
- [ ] ไม่มี resource ติดลบ หรือค่า NaN ปรากฏใน UI

## Related Documents
- [Report Backlog](../../agile/05-report-backlog.md)
- [Sprint 01](../../agile/sprint-backlogs/sprint-01.md)
