# AI Gateway Setup & Configuration

โฟลเดอร์นี้ประกอบด้วยการตั้งค่าและสคริปต์สำหรับรัน AI Gateway/Proxy ภายในเครื่องด้วย Docker หรือ Node.js เพื่ออำนวยความสะดวกในการพัฒนาโปรเจกต์เกม **clogged** ร่วมกับ AI Coding Agents (เช่น Claude Code, Cursor, Cline) โดยมีทางเลือกให้เลือกใช้งาน 2 ตัวหลัก:

---

## 🚀 เครื่องมือที่มีให้เลือกใช้งาน (Gateway Options)

### 1. OmniRoute (แนะนำเป็นหลัก)
**OmniRoute** เป็น AI Gateway แบบ Open-Source ที่มีความยืดหยุ่นสูง:
* **รองรับ AI Provider กว่า 250 ราย** รวมถึง Free-tier Providers กว่า 90 ราย (ไม่ต้องผูกบัตร)
* **Auto-fallback Routing:** สลับโมเดลหรือผู้ให้บริการสำรองโดยอัตโนมัติถึง 4 ระดับ (Subscription → API → Cheap → Free) เมื่อเกิดข้อผิดพลาดหรือติด Rate Limit
* **Token Compression:** ย่อขนาดของโค้ด/Logs/Git Diff ด้วย compression engine 10 แบบ (เช่น RTK, Caveman) ช่วยประหยัด Token ได้ 15–95%
* **ทางเลือกในการติดตั้ง:** รันผ่าน Docker Container หรือติดตั้งโดยตรงผ่าน Node.js / npm 
* **รายละเอียดเพิ่มเติม:** ดูได้ที่ [omniroute/README.md](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/omniroute/README.md)

### 2. 9Router (ทางเลือกเพิ่มเติม)
**9Router** เป็น AI Gateway/Proxy แบบเน้นประสิทธิภาพ:
* **Rust Token Killer (RTK):** เทคโนโลยีบีบอัด Token 20–40%
* **Fallback Routing:** สลับ API Key และโมเดลอัตโนมัติ
* **การติดตั้ง:** รันผ่าน Docker Container เท่านั้น
* **รายละเอียดเพิ่มเติม:** ดูได้ที่ [9router/README.md](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/9router/README.md)

*⚠️ **หมายเหตุ:** ทั้งสองตัวใช้พอร์ต `20128` เป็นค่าเริ่มต้นเหมือนกัน จึงไม่สามารถเปิดใช้งานพร้อมกันสองตัวในเวลาเดียวกันได้ ให้เลือกเปิดตัวใดตัวหนึ่งเท่านั้น*

---

## 📦 การติดตั้งและการใช้งานด่วน (Quick Start Helper)

เรามีสคริปต์อัตโนมัติสำหรับตรวจสอบ ติดตั้ง และตั้งค่า ทั้งในแบบ **Docker** และ **Node.js**:

1. ดับเบิ้ลคลิกไฟล์ [setup-docker.cmd](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/setup-docker.cmd) ในโฟลเดอร์นี้ หรือเปิด Terminal แล้วรัน:
   ```powershell
   .\ai-gateway\setup-docker.cmd
   ```
2. สคริปต์จะแสดงเมนูทางเลือกหลัก:
   * **[1] ใช้งานผ่าน Docker Container:**
     * ตรวจสอบว่ามี Docker ติดตั้งอยู่หรือไม่ (หากไม่มี จะมีตัวเลือกให้ติดตั้งอัตโนมัติผ่าน Windows `winget` หรือแนะลิงก์ดาวน์โหลด)
     * ตรวจสอบว่า Docker กำลังรันอยู่หรือไม่ (หากไม่รัน จะพยายามเปิดโปรแกรม Docker Desktop ให้)
     * ให้คุณเลือกเปิดใช้งาน **OmniRoute** หรือ **9Router**
   * **[2] ใช้งานผ่าน Node.js / npm (OmniRoute เท่านั้น):**
     * ตรวจสอบว่ามี Node.js และ npm ติดตั้งอยู่หรือไม่ (หากไม่มี จะมีตัวเลือกให้ติดตั้งผ่าน `winget` อัตโนมัติ)
     * ติดตั้ง OmniRoute globally (`npm install -g omniroute`) อัตโนมัติหากยังไม่มีในระบบ
     * แสดงเมนูย่อยเพื่อเลือก: เริ่มการทำงานของเซิร์ฟเวอร์ (`omniroute`), เข้าหน้าตั้งค่า Wizard (`omniroute setup`), หรือรันตัวตรวจสอบระบบ (`omniroute doctor`)

---

## 📂 โครงสร้างไฟล์ในโฟลเดอร์นี้

* [README.md](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/README.md) - เอกสารแนะนำภาพรวมนี้
* [setup-docker.cmd](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/setup-docker.cmd) - สคริปต์เรียกใช้งานด่วนสำหรับ Windows
* [setup-docker.ps1](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/setup-docker.ps1) - สคริปต์ logic หลักบน PowerShell
* [omniroute/](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/omniroute) - โฟลเดอร์ตั้งค่าของ OmniRoute (docker-compose, สคริปต์ย่อย)
* [9router/](file:///c:/Users/noppon/sources/01_DG/clogged/ai-gateway/9router) - โฟลเดอร์ตั้งค่าของ 9Router (docker-compose, สคริปต์ย่อย)

---

## 🔒 ความปลอดภัยของข้อมูล (Security Note)
ข้อมูลคีย์และการตั้งค่าที่ถูกสร้างขึ้นในโฟลเดอร์ `omniroute/data` และ `9router/data` จะถูกเก็บไว้ในเครื่องของคุณเท่านั้น และมีการระบุไว้ในไฟล์ [.gitignore](file:///c:/Users/noppon/sources/01_DG/clogged/.gitignore) เรียบร้อยแล้ว เพื่อไม่ให้เผลอทำการ Commit ข้อมูลสำคัญขึ้นสู่ GitHub
