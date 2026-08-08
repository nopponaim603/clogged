---
name: clearing-house
description: >
  Skill สำหรับตรวจสอบและจัดระเบียบ User Stories ในโครงการ (Agile User Story Archiving & Link Syncing)
  โดยทำหน้าที่ตรวจเช็กสถานะ (Status) ของ User Stories ทั้งหมดใต้ `docs/agile/user-stories/`
  หากฟีเจอร์/งานใดเสร็จสมบูรณ์แล้ว (Done, Implemented, Playtested, Accepted)
  จะดำเนินการย้ายไฟล์ไปเก็บที่โฟลเดอร์ `docs/agile/user-stories/archives/`
  พร้อมทั้งปรับปรุงลิงก์การอ้างอิงในเอกสาร Product Backlog, Sprint Backlogs, Index และ Changelog ให้ถูกต้องโดยอัตโนมัติ
---

# Clearing House Skill (Agile Archiving & Cleanup)

ทักษะสำหรับการเคลียร์และจัดระเบียบ User Stories ที่พัฒนาเสร็จสมบูรณ์แล้วเข้าสู่คลังเอกสารประวัติ (`archives/`) เพื่อรักษาความสะอาดของ Product Backlog และลดความสับสนในการติดตามงาน

---

## 🎯 วัตถุประสงค์หลัก (Core Workflow Overview)

1. **Audit User Story Status**: ตรวจสอบไฟล์ `.md` ทุกไฟล์ใต้ `docs/agile/user-stories/` โดยอ่านฟิลด์ `**Status:**` หรือ `Status:`
2. **Classify Active vs. Completed**:
   - **Active (คงไว้ในโฟลเดอร์หลัก)**: สถานะ `📋 Planned`, `🚧 In Progress`, `🟡 IN PROGRESS`, `🔴 MISSING`
   - **Completed (ย้ายไป Archives)**: สถานะ `🟢 DONE`, `✅ Done`, `✅ Implemented`, `✅ Playtested`, `✅ Completed`, `✅ Accepted`
3. **Execute Archive Relocation**: ย้ายไฟล์ User Story ที่ทำเสร็จแล้วจาก `docs/agile/user-stories/` ไปยัง `docs/agile/user-stories/archives/` (แนะนำให้ใช้ `git mv`)
4. **Synchronize Relative Links**: อัปเดตลิงก์ Markdown ย้อนกลับไปยังไฟล์ที่ถูกย้าย ในเอกสารสำคัญ เช่น:
   - `docs/agile/01-product-backlog.md` (`./user-stories/US-XXX.md` → `./user-stories/archives/US-XXX.md`)
   - `docs/agile/sprint-backlogs/*.md` (`../user-stories/US-XXX.md` → `../user-stories/archives/US-XXX.md`)
   - `docs/index.md` และ `docs/changelog.md`
5. **Generate Clearing Summary**: สรุปรายการไฟล์ที่ถูกย้าย และรายการที่ยังคงอยู่ใน Active Queue

---

## 🛠️ ขั้นตอนการทำงานโดยรายละเอียด (Execution Steps)

### ขั้นตอนที่ 1: ตรวจสอบสถานะ User Stories (Scan & Audit)
สแกนไดเรกทอรี `docs/agile/user-stories/*.md` (ไม่รวมโฟลเดอร์ `archives/`) เพื่ออ่านบรรทัดสถานะ:
- อ่านค่า `Status:` ในส่วน Header ของ User Story
- จำแนกประเภทไฟล์ตามเกณฑ์:
  - **ย้ายไป archives**: สถานะที่มีสัญลักษณ์ `🟢`, `✅` หรือระบุชัดเจนว่า `Done`, `Implemented`, `Playtested`, `Accepted`
  - **เก็บไว้ใน user-stories/**: สถานะที่มีสัญลักษณ์ `📋`, `🚧`, `🟡`, `🔴` หรือระบุว่า `Planned`, `In Progress`

---

### ขั้นตอนที่ 2: ย้ายไฟล์เข้า Archives (File Relocation)
ดำเนินการย้ายไฟล์โดยใช้คำสั่ง `git mv` (หรือ fallback เป็นระบบจัดการไฟล์) เพื่อรักษาประวัติในระบบควบคุมเวอร์ชัน (Git History):

```bash
git mv docs/agile/user-stories/US-PROD-XX.md docs/agile/user-stories/archives/US-PROD-XX.md
```

---

### ขั้นตอนที่ 3: อัปเดตลิงก์อ้างอิงเอกสาร (Link Synchronization)
ทำการค้นหาและแก้ไข path ของลิงก์ในเอกสารระบบเพื่อไม่ให้เกิด Broken Links:

| ไฟล์เอกสาร | Original Path | Target Path |
| :--- | :--- | :--- |
| `docs/agile/01-product-backlog.md` | `./user-stories/US-XXX.md` | `./user-stories/archives/US-XXX.md` |
| `docs/agile/sprint-backlogs/sprint-XX.md` | `../user-stories/US-XXX.md` | `../user-stories/archives/US-XXX.md` |
| `docs/index.md` | `./agile/user-stories/US-XXX.md` | `./agile/user-stories/archives/US-XXX.md` |

---

### ขั้นตอนที่ 4: สรุปผลการย้ายเอกสาร (Summary Reporting)
สร้างรายงานสรุปในรูปแบบ Markdown นำเสนอผู้ใช้:
- **จำนวนไฟล์ที่ย้ายเข้า Archives**: สรุปรายชื่อและสถานะของ User Stories ที่จัดเก็บเรียบร้อย
- **รายการ Active User Stories ที่เหลือ**: แสดง User Stories ที่ยังคงทำงานอยู่ใน Active Queue
- **ผลการอัปเดตลิงก์เอกสาร**: รายงานไฟล์เอกสารที่มีการปรับเปลี่ยน path ลิงก์

---

## 💡 คำสั่งและทริกเกอร์ที่เกี่ยวข้อง (Trigger Keywords)

- `"ใช้ skill clearing-house"`
- `"ตรวจสอบ user stories และย้ายที่เสร็จแล้วไป archives"`
- `"เคลียร์ backlog ย้ายงานที่เสร็จแล้ว"`
- `"archive completed user stories"`
- `"จัดระเบียบ user-stories"`
