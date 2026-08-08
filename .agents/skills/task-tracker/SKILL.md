---
name: task-tracker
description: >
  Skill สำหรับติดตามความคืบหน้าการพัฒนาซอฟต์แวร์/เกม (Code Progress & Sprint Tracker) 
  โดยเริ่มจากการอ่านและวิเคราะห์ code progress (git commit history, modified components, updated codebase),
  สกัดและถอดบทเรียนออกมาเขียนเป็น User Stories, นำไปจัดรวมลงใน Sprint Logs แยกตามรอบการพัฒนา
  พร้อมระบุสถานะว่าเสร็จสมบูรณ์แล้วหรือยังขาดระบบใดอีก (Done, In Progress, Missing/Pending), 
  และสรุปออกมาเป็นรายงานประจำสัปดาห์ (Weekly Report) สำหรับอัปเดตทีมบริหารหรือทีมพัฒนา
---

# Task & Code Progress Tracker Skill

ทักษะสำหรับการติดตามความคืบหน้าของการพัฒนาโปรเจกต์เชิงเทคนิคแบบอัตโนมัติ โดยเปลี่ยนการปรับปรุงโค้ด (Code Progress) ให้เป็นเอกสารการบริหารจัดการโปรเจกต์ (Agile Artifacts) ที่เป็นระบบและเห็นภาพรวมได้อย่างชัดเจน

---

## 🎯 วัตถุประสงค์หลัก (Core Workflow Overview)

1. **Read & Analyze Code Progress**: อ่านประวัติ git log, diff, หรือตรวจสอบไฟล์ซอร์สโค้ดที่เพิ่งแก้ไข เพื่อวิเคราะห์ว่าทีมได้พัฒนา/แก้ไขระบบใดไปบ้าง
2. **Generate User Stories**: แปลงฟังก์ชันและโค้ดที่ถูกพัฒนาให้เป็น User Stories ในรูปแบบ Agile (`As a... I want... So that...`) พร้อมกำหนด Acceptance Criteria และ Technical References
3. **Build & Update Sprint Logs**: รวบรวม User Stories จัดลง Sprint Log แยกตามรอบสัปดาห์/รอบการพัฒนา (Round/Sprint) พร้อมติดแท็กสถานะ:
   - 🟢 **DONE (เสร็จสมบูรณ์)**
   - 🟡 **IN PROGRESS (กำลังพัฒนา / เสร็จบางส่วน)**
   - 🔴 **MISSING / PENDING (ยังขาดระบบใดอีก / รอดำเนินการ)**
   - 📋 **MISSING SYSTEMS ANALYSIS (สรุประบบที่ยังขาดอยู่อย่างชัดเจน)**
4. **Generate Weekly Summary Report**: สร้างรายงานสรุปผลการดำเนินงานประจำสัปดาห์ สำหรับสรุปความคืบหน้า ผลงานเด่น ปัญหา/ความเสี่ยง และแผนงานในรอบถัดไป

---

## 📁 โครงสร้างเอกสารผลลัพธ์ (Target Output Directory Structure)

เอกสารที่ถูกสร้างหรืออัปเดตจาก Skill นี้จะถูกจัดเก็บภายใต้ไดเรกทอรี `docs/` ของโปรเจกต์:

```text
docs/
└── agile/
    ├── 01-product-backlog.md          ← อัปเดต Product Backlog รวม
    ├── reports/
    │   └── weekly/
    │       ├── weekly-2026-W30.md     ← รายงานสรุปประจำสัปดาห์
    │       └── weekly-YYYY-WXX.md
    ├── sprint-logs/
    │   ├── sprint-01.md               ← Sprint Log แต่ละรอบ
    │   ├── sprint-02.md
    │   └── sprint-XX.md
    └── user-stories/
        ├── US-001-feature-name.md     ← User Stories รายตัว/รายระบบ
        └── US-002-feature-name.md
```

---

## 🛠️ ขั้นตอนการทำงานโดยละเอียด (Detailed Execution Steps)

### ขั้นตอนที่ 1: การตรวจอ่าน Code Progress (Codebase & Git Analysis)

เมื่อได้รับคำสั่งให้ติดตามงาน อัปเดต sprint หรือสรุปความคืบหน้า ให้ดำเนินการดังนี้:

1. **รวบรวม Git Logs & Status**:
   - ใช้คำสั่งตรวจประวัติ git เช่น:
     - `git log --since="7 days ago" --oneline --name-status` (ดูการเปลี่ยนแปลงย้อนหลัง 1 สัปดาห์)
     - `git status` หรือ `git diff HEAD~5` เพื่อตรวจดูส่วนที่มีการปรับแต่ง
   - ตรวจดูคอมมิตเมสเสจและไฟล์ที่มีการเพิ่ม/แก้ไข/ลบ
2. **สำรวจซอร์สโค้ดและโครงสร้างโปรเจกต์**:
   - ตรวจสอบ Component, Manager Class, ScriptableObject, API Endpoint, หรือ UI Components ที่มีปรับปรุง
   - จำแนกประเภทความคืบหน้า:
     - **New Feature**: ระบบหรือฟีเจอร์ใหม่ที่เพิ่งถูกเพิ่ม
     - **Bug Fix**: การแก้ไขบั๊กหรือปัญหาประสิทธิภาพ
     - **Refactoring / Architecture**: การปรับปรุงโครงสร้างโค้ด/การเชื่อมต่อภายใน
     - **UI / Content Update**: การแต่งหน้าจอ การใส่ข้อมูลเกม หรือดีไซน์
3. **ระบุส่วนที่ยังขาด (Missing Systems Identification)**:
   - เปรียบเทียบโค้ดที่เขียนเสร็จแล้วกับความต้องการของระบบ (GDD / Spec / Product Backlog)
   - ค้นหา `TODO`, `FIXME`, Stub functions, Mock data หรือการเชื่อมต่อที่ยังไม่สมบูรณ์

---

### ขั้นตอนที่ 2: การสร้างและเขียน User Stories (User Story Generation)

นำความคืบหน้าจากโค้ดมาแปลงเป็นเอกสาร User Story ในไดเรกทอรี `docs/agile/user-stories/` หรืออัปเดตลงใน Product Backlog:

#### โครงสร้างมาตรฐาน User Story:
```markdown
# [US-XXX] ชื่อ User Story / ฟีเจอร์

**System Group / Module**: [ชื่อระบบ เช่น Dialogue System / Player Movement / Data Persistence]  
**Status**: 🟢 DONE | 🟡 IN PROGRESS | 🔴 MISSING  
**Related Code Files**: `path/to/File1.cs`, `path/to/File2.tsx`

### 1. User Story Statement
- **As a** [ผู้ใช้งาน / ผู้เล่น / แอดมิน]
- **I want** [ฟีเจอร์หรือความสามารถที่พัฒนาขึ้น]
- **So that** [คุณค่าหรือประโยชน์ที่ผู้ใช้ได้รับ]

### 2. Acceptance Criteria (เงื่อนไขความสำเร็จ)
- [x] เงื่อนไขที่ 1 (ผ่านการทดสอบแล้ว)
- [x] เงื่อนไขที่ 2
- [ ] เงื่อนไขที่ 3 (ยังขาดระบบสนับสนุน)

### 3. Implementation Summary (สรุปสิ่งที่พัฒนาแล้วในโค้ด)
- รายละเอียดคลาส / สคริปต์ที่ถูกสร้างหรือแก้ไข
- พฤติกรรม logic การทำงานหลัก
```

---

### ขั้นตอนที่ 3: การรวบรวมและอัปเดต Sprint Log (Sprint Log Management)

สร้างหรืออัปเดตไฟล์ Sprint Log ใน `docs/agile/sprint-logs/sprint-XX.md` โดยแบ่งแยกสถานะของแต่ละรายการให้ชัดเจน:

#### โครงสร้าง Sprint Log:
```markdown
# Sprint Log: Sprint [XX] ([วันที่เริ่ม] - [วันที่สิ้นสุด])

**Goal สปรินท์**: [เป้าหมายหลักประจำรอบ]  
**Overall Completion**: [XX]%

---

## 📊 สรุปสถานะความคืบหน้าตามระบบ (System Status Breakdown)

| ID | User Story / Feature | System / Component | สถานะ | สรุปสิ่งที่ทำเสร็จ | ระบบที่ยังขาด/ต้องทำเพิ่ม |
|---|---|---|---|---|---|
| US-001 | ระบบสนทนาโต้ตอบ | DialogueManager.cs | 🟢 DONE | เล่นไดอะล็อกและเลือกตัวเลือกได้ | - |
| US-002 | ระบบบันทึกคะแนน | ScoreSystem.cs | 🟡 IN PROGRESS | คำนวณคะแนนพื้นฐานสำเร็จ | ยังขาดการส่ง API ไปเซิร์ฟเวอร์ |
| US-003 | ระบบสินค้าและร้านค้า | ShopUI.tsx | 🔴 MISSING | - | ยังไม่มี UI และระบบตัดเงิน |

---

## 🟢 1. รายการที่เสร็จสมบูรณ์ (Done Items)
- **US-001: ระบบสนทนาโต้ตอบ**
  - รายละเอียดสิ่งที่เสร็จ: ...
  - ไฟล์ที่เกี่ยวข้อง: ...

## 🟡 2. รายการที่อยู่ระหว่างพัฒนา / เสร็จบางส่วน (In Progress)
- **US-002: ระบบบันทึกคะแนน**
  - ส่วนที่ทำแล้ว: ...
  - ส่วนที่กำลังทำ: ...

## 🔴 3. รายละเอียดระบบที่ยังขาดอีก (Missing / Pending Systems)
ระบุรายการเชิงเทคนิคของระบบหรือโมดูลที่ยังไม่สมบูรณ์:
1. **[ชื่อระบบที่ขาด]**:
   - **สิ่งที่ขาดไป**: [เช่น ยังไม่มี Save/Load Manager, ยังขาดการรับค่าจาก Input Manager]
   - **ผลกระทบ**: [ผลกระทบต่อระบบอื่น]
   - **Action Item**: [สิ่งที่ต้องทำต่อในสปรินท์ถัดไป]
```

---

### ขั้นตอนที่ 4: การจัดทำรายงานสรุปประจำสัปดาห์ (Weekly Report Generation)

สร้างรายงานสรุปประจำสัปดาห์ใน `docs/agile/reports/weekly/weekly-YYYY-WXX.md` สำหรับสื่อสารกับทีมและผู้บริหาร:

#### โครงสร้าง Weekly Report:
```markdown
# Weekly Progress Report: สัปดาห์ที่ [XX] ([ช่วงวันที่])

## 📌 Executive Summary (ภาพรวมประจำสัปดาห์)
สรุปสั้น 2-3 บรรทัดเกี่ยวกับภาพรวมความคืบหน้าและก้าวสำคัญในสัปดาห์นี้

---

## 🚀 Key Highlights & Accomplishments (ผลงานหลักที่ทำเสร็จในสัปดาห์นี้)
- 🟢 **[ระบบ A]**: [อธิบายสิ่งที่ทำเสร็จและประโยชน์ที่ได้]
- 🟢 **[ระบบ B]**: [อธิบายสิ่งที่ทำเสร็จและประโยชน์ที่ได้]

---

## ⚙️ Code Progress & Technical Updates (รายละเอียดการพัฒนาทางเทคนิค)
- **Files Modified / Created**: [รายการไฟล์สำคัญ]
- **Key Refactoring / Architecture Improvements**: [งานปรับปรุงโครงสร้าง]

---

## ⚠️ Missing Systems & Blockers (ระบบที่ยังขาดและอุปสรรค)
- 🔴 **ระบบที่ยังขาดอยู่**:
  1. [รายละเอียดระบบ 1]
  2. [รายละเอียดระบบ 2]
- ⚡ **Blockers / Technical Debt**: [อุปสรรคหรือประเด็นที่ต้องการการตัดสินใจ]

---

## 🎯 Next Week Priorities (แผนงานสำคัญสัปดาห์ถัดไป)
1. [งานลำดับความสำคัญที่ 1]
2. [งานลำดับความสำคัญที่ 2]
3. [งานลำดับความสำคัญที่ 3]
```

---

## 💡 แนวทางการใช้งาน (Usage Instructions for AI Assistant)

เมื่อผู้ใช้สั่งการด้วยข้อความลักษณะดังนี้:
- *"ช่วยติดตามงานและสรุป progress สัปดาห์นี้"*
- *"อ่าน git log/โค้ดที่ทำไป แล้วอัปเดต sprint log และ user stories ให้หน่อย"*
- *"เช็คดูว่าโค้ดตอนนี้เสร็จถึงไหนแล้ว ยังขาดระบบอะไรอีกบ้าง แล้วทำ weekly report"*

ให้ AI ดำเนินการตามลำดับขั้นตอน (Workflow Pipeline):
1. **สำรวจ Git & Codebase**: รันคำสั่งอ่าน commit log หรืออ่านไฟล์ที่เพิ่งอัปเดต
2. **วิเคราะห์ความคืบหน้าเชิงระบบ**: แยกแยะว่าฟังก์ชันใดทำเสร็จแล้ว (Done) ฟังก์ชันใดเสร็จบางส่วน (In Progress) และฟังก์ชันใดที่ยังไม่มีหรือขาดไป (Missing Systems)
3. **อัปเดต/สร้าง User Stories**: เขียน User Stories ลงโฟลเดอร์ `docs/agile/user-stories/`
4. **อัปเดต/สร้าง Sprint Log**: เขียน Sprint Log ลงโฟลเดอร์ `docs/agile/sprint-logs/`
5. **สร้าง Weekly Report**: เขียนรายงานสรุปประจำสัปดาห์ลงโฟลเดอร์ `docs/agile/reports/weekly/`
6. **อัปเดตไฟล์ดัชนี (`docs/index.md` / `docs/changelog.md`)**: เพื่อให้คลังเอกสารของโครงการมีความสอดคล้องกันตลอดเวลา

---

## 🔗 Referencing Examples

สามารถอ้างอิงเทมเพลตตัวอย่างในไดเรกทอรีนี้:
- [User Story Template](file:///c:/Users/noppon/source/03-NAPLAB/Catxel/.agents/skills/task-tracker/examples/user-story-template.md)
- [Sprint Log Template](file:///c:/Users/noppon/source/03-NAPLAB/Catxel/.agents/skills/task-tracker/examples/sprint-log-template.md)
- [Weekly Report Template](file:///c:/Users/noppon/source/03-NAPLAB/Catxel/.agents/skills/task-tracker/examples/weekly-report-template.md)
