# Unity MCP Setup Guide

> **MCP (Model Context Protocol)** bridge ระหว่าง AI assistant (Kilo, Claude Code, Cursor, etc.) และ Unity Editor  
> ใช้ [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) v1.3.0  
> Setup date: 2026-07-14

## Architecture

```
AI Client (Kilo/Claude Code/etc.)
    │  MCP (stdio) JSON-RPC
    ▼
Node.js MCP Bridge  ─── WebSocket :8090 ───▶  Unity Editor
{mcp-unity/Server~}                           {MCP Unity package}
```

## ขั้นตอนการติดตั้ง

### 1. ติดตั้ง Unity Package

1. เปิด Unity Editor → โหลดโปรเจกต์ที่ `Unity-Projects/`
2. **Window → Package Manager** → กด **"+"** → **"Add package from git URL..."**
3. ใส่:
   ```
   https://github.com/CoderGamester/mcp-unity.git
   ```
4. รอให้ package ลงเสร็จ (package name: `com.gamelovers.mcp-unity`)
5. Package จะไปอยู่ที่ `Unity-Projects/Library/PackageCache/com.gamelovers.mcp-unity@<hash>/`

### 2. เปิด WebSocket Server ใน Unity

1. **Tools → MCP Unity → Server Window**
2. กด **"Start Server"** (พอร์ตเริ่มต้น `8090`)
3. จะเห็นสถานะ WebSocket กำลังรัน

### 3. ตั้งค่า AI Client

#### สำหรับ Kilo (ใช้ใน repo นี้)

ไฟล์ `kilo.json` ที่ repo root:

```json
{
  "$schema": "https://app.kilo.ai/config.json",
  "mcp": {
    "mcp-unity": {
      "type": "local",
      "enabled": true,
      "command": ["node", "Unity-Projects/Library/PackageCache/com.gamelovers.mcp-unity@<hash>/Server~/build/index.js"]
    }
  }
}
```

#### สำหรับ OpenCode

ไฟล์ `Unity-Projects/opencode.json`:

```json
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "mcp-unity": {
      "type": "local",
      "enabled": true,
      "command": ["node", "Library/PackageCache/com.gamelovers.mcp-unity@<hash>/Server~/build/index.js"]
    }
  }
}
```

#### สำหรับ Claude Code

ไฟล์ `Unity-Projects/.mcp.json`:

```json
{
  "mcpServers": {
    "mcp-unity": {
      "command": "node",
      "args": ["Library/PackageCache/com.gamelovers.mcp-unity@<hash>/Server~/build/index.js"]
    }
  }
}
```

#### สำหรับ Cursor

ไฟล์ `Unity-Projects/.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "mcp-unity": {
      "command": "node",
      "args": ["Library/PackageCache/com.gamelovers.mcp-unity@<hash>/Server~/build/index.js"]
    }
  }
}
```

#### สำหรับ VS Code / GitHub Copilot

ไฟล์ `Unity-Projects/.vscode/mcp.json`:

```json
{
  "mcpServers": {
    "mcp-unity": {
      "command": "node",
      "args": ["Library/PackageCache/com.gamelovers.mcp-unity@<hash>/Server~/build/index.js"]
    }
  }
}
```

> **หมายเหตุ:** `<hash>` คือ commit hash ที่ผูกกับ version ของ package (เช่น `c35f184d7656`)  
> ถ้าอัปเดต package ค่า hash จะเปลี่ยน ต้องกลับมาแก้ path นี้

## การใช้งาน

### เงื่อนไขก่อนใช้งาน

- Unity Editor เปิดอยู่
- WebSocket Server กำลังรัน (Tools → MCP Unity → Server Window → "Start Server")
- AI Client (Kilo) เชื่อมต่อกับ MCP server แล้ว

### MCP Tools ที่มีให้ใช้ (37 tools)

| Tool | Description |
|------|-------------|
| `execute_menu_item` | รัน Unity menu item ใดๆ |
| `update_gameobject` | แก้ไข/สร้าง GameObject |
| `update_component` | เพิ่ม/แก้ไข Component |
| `create_prefab` | สร้าง Prefab จาก MonoBehaviour |
| `create_scene` / `load_scene` / `save_scene` | จัดการ Scene |
| `move_gameobject` / `rotate_gameobject` / `scale_gameobject` / `set_transform` | จัดการ Transform |
| `duplicate_gameobject` / `delete_gameobject` / `reparent_gameobject` | จัดการ Hierarchy |
| `create_material` / `assign_material` / `modify_material` | จัดการ Material |
| `add_package` | ติดตั้ง Package ผ่าน UPM |
| `run_tests` | รัน Unity Test Runner |
| `get_play_mode_status` / `set_play_mode_status` | ควบคุม Play Mode |
| `batch_execute` | รันหลายคำสั่งในครั้งเดียว |
| และอื่นๆ | ดูทั้งหมดได้จาก `tools/list` |

### ตัวอย่างคำสั่ง

- *"สร้าง Cube ที่ origin"*
- *"ย้าย Main Camera ไปที่ (5, 10, -15)"*
- *"เพิ่ม Rigidbody ให้ Cube และตั้งค่า mass=5"*
- *"เปลี่ยนสี Material ของ Cube เป็นสีแดง"*
- *"รัน EditMode tests ทั้งหมด"*
- *"Play mode แล้ว Step ไป 1 frame"*

## Troubleshooting

### WebSocket ไม่เชื่อมต่อ
- เช็คว่า Unity Editor เปิดอยู่
- เช็คว่า **Tools → MCP Unity → Server Window → Start Server** กดแล้ว
- เช็คพอร์ต 8090 ไม่ถูกใช้งานโดยโปรแกรมอื่น

### MCP Server Exit Code 1
- ในโหมด one-shot pipe (Get-Content \| node) server อาจ exit ก่อน WebSocket เชื่อมต่อ
- ใช้ persistent connection (Kilo จัดการให้อัตโนมัติผ่าน kilo.json)
- หรือรอ ~1-2 วินาทีหลังจาก initialize ก่อนส่ง tools/call

### npm Execution Policy Error (Windows)
ถ้าเจอ `npm.ps1 cannot be loaded because running scripts is disabled`:
- ใช้ `npm.cmd` แทน `npm`
- หรือรัน PowerShell แบบ Admin: `Set-ExecutionPolicy RemoteSigned -Scope CurrentUser`

### Package Hash เปลี่ยนหลังอัปเดต
- เวลาอัปเดต `com.gamelovers.mcp-unity` ค่า `<hash>` จะเปลี่ยน
- ต้องแก้ path ในไฟล์ config ทุกตัวที่引用ไว้
- ใช้ Unity's **Configure** button (Tools → MCP Unity → Server Window) จะ regenerate config ให้อัตโนมัติ

## Reference

- [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) (GitHub, MIT License)
- [Model Context Protocol](https://modelcontextprotocol.io/introduction)
