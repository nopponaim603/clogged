// src/scenes/BaseInteriorScene.ts
import { Scene } from 'phaser';
import { GAME_CONFIG, RESOURCE_ICONS } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Systems } from '../main';
import { Facility } from '../entities/Facility';

export class BaseInteriorScene extends Scene {
    // ✅ Grid 20×12 (กว้าง×สูง) แบ่งเป็น 4 floors ชั้นละ 3 grid
    private readonly GRID_WIDTH = GAME_CONFIG.BASE_GRID_WIDTH;   // 20
    private readonly GRID_HEIGHT = GAME_CONFIG.BASE_GRID_HEIGHT; // 12
    private readonly TOTAL_FLOORS = GAME_CONFIG.BASE_TOTAL_FLOORS; // 4
    private readonly GRID_PER_FLOOR = 3; // 12 / 4 = 3
    
    // ✅ คำนวณ CELL_SIZE ให้พอดีจอ
    private readonly CELL_SIZE = 50;
    private readonly GRID_OFFSET_X = 140;  // (1280 - 20*50)/2 = 140
    private readonly GRID_OFFSET_Y = 100;
    
    // Facility Sprites
    private facilitySprites: Phaser.GameObjects.GameObject[] = [];
    private gridGraphics!: Phaser.GameObjects.Graphics;
    
    constructor() {
        super('BaseInteriorScene');
    }
    
    init(data: any): void {
        // ✅ ไม่ต้องรับ floor แล้ว
    }
    
    create(): void {
        console.log(`🏠 BaseInteriorScene create`);
        
        // ✅ Background
        this.cameras.main.setBackgroundColor(GAME_CONFIG.COLORS.BG_DARK);
        
        // ✅ Header
        this.createHeader();
        
        // ✅ Grid (1 grid ใหญ่ 20×12, มีเส้นแบ่ง 4 floors)
        this.createGrid();
        
        // ✅ Facilities (ทุก floor)
        this.renderAllFacilities();
        
        // ✅ Close Button
        this.createCloseButton();
        
        // ✅ Listen events
        EventBus.on('facility:built', this.onFacilityChanged, this);
        EventBus.on('facility:dismantled', this.onFacilityChanged, this);
        EventBus.on('facility:upgraded', this.onFacilityChanged, this);
        
        // ✅ ESC/TAB to close
        this.input.keyboard?.on('keydown-ESC', () => this.close());
        this.input.keyboard?.on('keydown-TAB', () => this.close());
    }
    
    // ============================================================
    // HEADER
    // ============================================================
    private createHeader(): void {
        const centerX = GAME_CONFIG.WIDTH / 2;
        
        // ✅ Title
        this.add.text(centerX, 35, '🏠 BASE INTERIOR', {
            fontSize: '32px',
            color: '#00e5cc',
            fontFamily: 'monospace',
            fontStyle: 'bold',
            stroke: '#000000',
            strokeThickness: 4,
        }).setOrigin(0.5);
        
        // ✅ Resources (มุมซ้ายบน)
        const state = GameState;
        const resources = ['wood', 'stone', 'iron_bar', 'credits'];
        
        let text = '';
        resources.forEach(type => {
            const amount = type === 'credits' 
                ? state.getCredits() 
                : state.getResource(type);
            const icon = type === 'credits' 
                ? '💰' 
                : RESOURCE_ICONS[type as keyof typeof RESOURCE_ICONS] || '📦';
            text += `${icon} ${Math.floor(amount)}  `;
        });
        
        this.add.text(20, 25, text, {
            fontSize: '14px',
            color: '#b0c4de',
            fontFamily: 'monospace',
        });
    }
    
    // ============================================================
    // GRID (1 grid ใหญ่ 20×12 + เส้นแบ่ง 4 floors)
    // ============================================================
    private createGrid(): void {
        if (this.gridGraphics) {
            this.gridGraphics.destroy();
        }
        
        this.gridGraphics = this.add.graphics();
        
        const gridWidth = this.GRID_WIDTH * this.CELL_SIZE;   // 20*50 = 1000
        const gridHeight = this.GRID_HEIGHT * this.CELL_SIZE; // 12*50 = 600
        
        console.log(`📐 Grid: ${this.GRID_WIDTH}×${this.GRID_HEIGHT}, Cell: ${this.CELL_SIZE}px`);
        
        // ✅ Background
        this.gridGraphics.fillStyle(0x1a2332, 0.6);
        this.gridGraphics.fillRect(
            this.GRID_OFFSET_X,
            this.GRID_OFFSET_Y,
            gridWidth,
            gridHeight
        );
        
        // ✅ Grid Lines (ทุก cell - บางๆ)
        this.gridGraphics.lineStyle(1, 0x4a5568, 0.2);
        
        for (let x = 0; x <= this.GRID_WIDTH; x++) {
            const px = this.GRID_OFFSET_X + x * this.CELL_SIZE;
            this.gridGraphics.beginPath();
            this.gridGraphics.moveTo(px, this.GRID_OFFSET_Y);
            this.gridGraphics.lineTo(px, this.GRID_OFFSET_Y + gridHeight);
            this.gridGraphics.strokePath();
        }
        
        for (let y = 0; y <= this.GRID_HEIGHT; y++) {
            const py = this.GRID_OFFSET_Y + y * this.CELL_SIZE;
            this.gridGraphics.beginPath();
            this.gridGraphics.moveTo(this.GRID_OFFSET_X, py);
            this.gridGraphics.lineTo(this.GRID_OFFSET_X + gridWidth, py);
            this.gridGraphics.strokePath();
        }
        
        // ✅ Floor Divider Lines (ทุก 3 grid = 1 floor)
        // Floor 1 = แถว 0-2 (บนสุด)
        // Floor 2 = แถว 3-5
        // Floor 3 = แถว 6-8
        // Floor 4 = แถว 9-11 (ล่างสุด)
        this.gridGraphics.lineStyle(3, 0xffd93d, 0.8);
        for (let f = 1; f < this.TOTAL_FLOORS; f++) {
            const py = this.GRID_OFFSET_Y + f * this.GRID_PER_FLOOR * this.CELL_SIZE;
            this.gridGraphics.beginPath();
            this.gridGraphics.moveTo(this.GRID_OFFSET_X, py);
            this.gridGraphics.lineTo(this.GRID_OFFSET_X + gridWidth, py);
            this.gridGraphics.strokePath();
        }
        
        // ✅ Border (สีฟ้า)
        this.gridGraphics.lineStyle(3, GAME_CONFIG.COLORS.PATH_ACTIVE);
        this.gridGraphics.strokeRect(
            this.GRID_OFFSET_X,
            this.GRID_OFFSET_Y,
            gridWidth,
            gridHeight
        );
        
        // ✅ Floor Labels (ด้านซ้าย)
        // Floor 1 อยู่บนสุด → label บนสุด
        for (let f = 1; f <= this.TOTAL_FLOORS; f++) {
            const floorIndexFromTop = f - 1;
            const labelY = this.GRID_OFFSET_Y + floorIndexFromTop * this.GRID_PER_FLOOR * this.CELL_SIZE + 15;
            
            this.add.text(this.GRID_OFFSET_X - 70, labelY, `F${f}`, {
                fontSize: '18px',
                color: '#ffd93d',
                fontFamily: 'monospace',
                fontStyle: 'bold',
            });
        }
    }
    
    // ============================================================
    // FACILITIES (ทุก floor)
    // ============================================================
    private renderAllFacilities(): void {
        // ✅ Clear
        this.facilitySprites.forEach(s => s.destroy());
        this.facilitySprites = [];
        
        const facilities = Systems.base.getAllFacilities();
        
        console.log(`🏭 Rendering ${facilities.length} facilities total`);
        
        facilities.forEach(facility => {
            this.renderFacility(facility);
        });
    }
    
    private renderFacility(facility: Facility): void {
        // ✅ Floor 1 = บนสุด (แถว 0-2)
        // ✅ Floor 4 = ล่างสุด (แถว 9-11)
        // floorIndexFromTop: Floor 1 → 0, Floor 2 → 1, Floor 3 → 2, Floor 4 → 3
        const floorIndexFromTop = facility.floor - 1;
        
        // ✅ absoluteGridY = (floorIndex × 3) + gridY (gridY ต้องเป็น 0-2)
        const absoluteGridY = floorIndexFromTop * this.GRID_PER_FLOOR + facility.gridY;
        
        const x = this.GRID_OFFSET_X + facility.gridX * this.CELL_SIZE;
        const y = this.GRID_OFFSET_Y + absoluteGridY * this.CELL_SIZE;
        const w = facility.size.w * this.CELL_SIZE;
        const h = facility.size.h * this.CELL_SIZE;
        
        console.log(`🏭 ${facility.type} Floor ${facility.floor} grid(${facility.gridX},${facility.gridY}) → abs(${x},${y}) size ${w}×${h}`);
        
        // ✅ Background Rect
        const rect = this.add.rectangle(
            x + w / 2,
            y + h / 2,
            w - 4,
            h - 4,
            facility.getColor(),
            0.75
        );
        rect.setStrokeStyle(3, 0xffffff, 0.9);
        rect.setInteractive({ useHandCursor: true });
        
        rect.on('pointerover', () => {
            rect.setFillStyle(facility.getColor(), 1.0);
        });
        rect.on('pointerout', () => {
            rect.setFillStyle(facility.getColor(), 0.75);
        });
        rect.on('pointerdown', () => {
            this.onFacilityClick(facility);
        });
        
        this.facilitySprites.push(rect);
        
        // ✅ Icon
        const icon = this.add.text(x + w / 2, y + h / 2 - 12, facility.getIcon(), {
            fontSize: '40px',
        }).setOrigin(0.5);
        this.facilitySprites.push(icon);
        
        // ✅ Label
        const label = this.add.text(x + w / 2, y + h / 2 + 30, 
            `${facility.type.toUpperCase()} Lv.${facility.level}`, {
            fontSize: '14px',
            color: '#ffffff',
            fontFamily: 'monospace',
            fontStyle: 'bold',
            backgroundColor: '#1a2332',
            padding: { x: 6, y: 3 },
        }).setOrigin(0.5);
        this.facilitySprites.push(label);
    }
    
    // ============================================================
    // FACILITY CLICK
    // ============================================================
    private onFacilityClick(facility: Facility): void {
        console.log(`🖱️ Clicked: ${facility.type} (Lv.${facility.level})`);
        this.showFacilityInfo(facility);
    }
    
    private showFacilityInfo(facility: Facility): void {
        const centerX = GAME_CONFIG.WIDTH / 2;
        const centerY = GAME_CONFIG.HEIGHT / 2;
        
        const popupObjects: Phaser.GameObjects.GameObject[] = [];
        
        // ✅ Overlay (คลิกปิดได้)
        const overlay = this.add.rectangle(centerX, centerY, GAME_CONFIG.WIDTH, GAME_CONFIG.HEIGHT, 0x000000, 0.75);
        overlay.setInteractive();
        overlay.on('pointerdown', () => {
            popupObjects.forEach(o => o.destroy());
        });
        popupObjects.push(overlay);
        
        // ✅ Popup Background
        const popupBg = this.add.rectangle(centerX, centerY, 500, 400, GAME_CONFIG.COLORS.BG_MEDIUM, 0.95);
        popupBg.setStrokeStyle(2, GAME_CONFIG.COLORS.PATH_ACTIVE);
        popupObjects.push(popupBg);
        
        // ✅ Title
        const title = this.add.text(centerX, centerY - 160, `${facility.getIcon()} ${facility.type.toUpperCase()}`, {
            fontSize: '32px',
            color: '#00e5cc',
            fontFamily: 'monospace',
            fontStyle: 'bold',
        }).setOrigin(0.5);
        popupObjects.push(title);
        
        // ✅ Info
        let y = centerY - 100;
        const infoLines = [
            `Level: ${facility.level} / ${facility.getConfig().maxLevel}`,
            `Condition: ${Math.floor(facility.condition)}%`,
            `Workers: ${facility.workers.length}`,
        ];
        
        infoLines.forEach(line => {
            const t = this.add.text(centerX - 200, y, line, {
                fontSize: '18px',
                color: '#ffffff',
                fontFamily: 'monospace',
            });
            popupObjects.push(t);
            y += 30;
        });
        
        // ✅ Upgrade
        if (facility.canUpgrade()) {
            const upgradeBtn = this.add.text(centerX, y + 30, '⬆️ UPGRADE', {
                fontSize: '20px',
                color: '#00b894',
                fontFamily: 'monospace',
                backgroundColor: '#2d3436',
                padding: { x: 15, y: 8 },
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });
            
            upgradeBtn.on('pointerdown', () => {
                Systems.base.upgradeFacility(facility.id);
                popupObjects.forEach(o => o.destroy());
                this.renderAllFacilities();
            });
            popupObjects.push(upgradeBtn);
        }
        
        // ✅ Close
        const closeBtn = this.add.text(centerX, y + 100, '✕ CLOSE', {
            fontSize: '20px',
            color: '#ff4757',
            fontFamily: 'monospace',
            backgroundColor: '#2d3436',
            padding: { x: 15, y: 8 },
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });
        
        closeBtn.on('pointerdown', () => {
            popupObjects.forEach(o => o.destroy());
        });
        popupObjects.push(closeBtn);
    }
    
    // ============================================================
    // CLOSE
    // ============================================================
    private createCloseButton(): void {
        const closeBtn = this.add.text(
            GAME_CONFIG.WIDTH - 60,
            35,
            '✕',
            {
                fontSize: '32px',
                color: '#ff4757',
                fontFamily: 'monospace',
                backgroundColor: '#2d3436',
                padding: { x: 15, y: 10 },
            }
        ).setOrigin(0.5).setInteractive({ useHandCursor: true });
        
        closeBtn.on('pointerover', () => closeBtn.setStyle({ color: '#ffffff' }));
        closeBtn.on('pointerout', () => closeBtn.setStyle({ color: '#ff4757' }));
        closeBtn.on('pointerdown', () => this.close());
    }
    
    private close(): void {
        this.scene.stop();
    }
    
    // ============================================================
    // EVENTS
    // ============================================================
    private onFacilityChanged(): void {
        this.renderAllFacilities();
    }
}