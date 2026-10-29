// src/ui/MinimizedBase.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Systems } from '../main';
import { Facility } from '../entities/Facility';

export class MinimizedBase {
    private scene: Scene;
    private container: Phaser.GameObjects.Container;
    private mapGraphics: Phaser.GameObjects.Graphics;
    private facilitySprites: Phaser.GameObjects.Rectangle[] = [];
    
    private readonly BASE_WIDTH = 300;
    private readonly BASE_HEIGHT = 200;
    private readonly BASE_X = 20;
    private readonly BASE_Y: number;
    private readonly CELL_SIZE = 13;
    
    constructor(scene: Scene) {
        this.scene = scene;
        
        this.BASE_Y = GAME_CONFIG.HEIGHT - this.BASE_HEIGHT - 20;
        
        console.log(`🏠 MinimizedBase position: (${this.BASE_X}, ${this.BASE_Y})`);
        
        this.container = scene.add.container(0, 0);
        this.container.setDepth(1000);
        this.container.setScrollFactor(0);
        
        // ✅ ให้ UI camera เห็นเท่านั้น
        scene.cameras.main.ignore(this.container);
        
        // ✅ Background
        const bg = scene.add.graphics();
        bg.fillStyle(GAME_CONFIG.COLORS.BG_MEDIUM, 0.95);
        bg.fillRoundedRect(this.BASE_X, this.BASE_Y, this.BASE_WIDTH, this.BASE_HEIGHT, 8);
        bg.lineStyle(2, GAME_CONFIG.COLORS.PATH_ACTIVE, 0.8);
        bg.strokeRoundedRect(this.BASE_X, this.BASE_Y, this.BASE_WIDTH, this.BASE_HEIGHT, 8);
        this.container.add(bg);
        
        // ✅ Title
        const title = scene.add.text(this.BASE_X + 10, this.BASE_Y + 8, '🏠 BASE (TAB)', {
            fontSize: '14px',
            color: '#b0c4de',
            fontFamily: 'monospace',
            fontStyle: 'bold',
        });
        this.container.add(title);
        
        // ✅ Map Graphics
        this.mapGraphics = scene.add.graphics();
        this.container.add(this.mapGraphics);
        
        // ✅ Clickable
        const clickArea = scene.add.rectangle(
            this.BASE_X + this.BASE_WIDTH / 2,
            this.BASE_Y + this.BASE_HEIGHT / 2,
            this.BASE_WIDTH,
            this.BASE_HEIGHT,
            0xffffff,
            0
        ).setInteractive({ useHandCursor: true });
        
        clickArea.on('pointerdown', () => {
            EventBus.emit('ui:openBaseInterior');
        });
        this.container.add(clickArea);
        
        this.renderBase();
        
        EventBus.on('facility:built', this.renderBase, this);
        EventBus.on('facility:dismantled', this.renderBase, this);
        EventBus.on('facility:upgraded', this.renderBase, this);
    }
    
    private renderBase(): void {
        this.facilitySprites.forEach(s => s.destroy());
        this.facilitySprites = [];
        this.mapGraphics.clear();
        
        const gridWidth = GAME_CONFIG.BASE_GRID_WIDTH * this.CELL_SIZE;
        const gridHeight = GAME_CONFIG.BASE_GRID_HEIGHT * this.CELL_SIZE;
        const offsetX = this.BASE_X + (this.BASE_WIDTH - gridWidth) / 2;
        const offsetY = this.BASE_Y + 30;
        
        // ✅ Grid Lines
        this.mapGraphics.lineStyle(1, 0x4a5568, 0.4);
        for (let x = 0; x <= GAME_CONFIG.BASE_GRID_WIDTH; x++) {
            const px = offsetX + x * this.CELL_SIZE;
            this.mapGraphics.beginPath();
            this.mapGraphics.moveTo(px, offsetY);
            this.mapGraphics.lineTo(px, offsetY + gridHeight);
            this.mapGraphics.strokePath();
        }
        for (let y = 0; y <= GAME_CONFIG.BASE_GRID_HEIGHT; y++) {
            const py = offsetY + y * this.CELL_SIZE;
            this.mapGraphics.beginPath();
            this.mapGraphics.moveTo(offsetX, py);
            this.mapGraphics.lineTo(offsetX + gridWidth, py);
            this.mapGraphics.strokePath();
        }
        
        // ✅ Floor dividers (ทุก 3 grid)
        this.mapGraphics.lineStyle(2, 0xffd93d, 0.5);
        for (let f = 1; f < GAME_CONFIG.BASE_TOTAL_FLOORS; f++) {
            const py = offsetY + f * 3 * this.CELL_SIZE;
            this.mapGraphics.beginPath();
            this.mapGraphics.moveTo(offsetX, py);
            this.mapGraphics.lineTo(offsetX + gridWidth, py);
            this.mapGraphics.strokePath();
        }
        
        // ✅ Facilities
        const facilities = Systems.base.getAllFacilities();
        facilities.forEach(facility => {
            const floorOffsetY = (facility.floor - 1) * 3;
            
            const x = offsetX + facility.gridX * this.CELL_SIZE;
            const y = offsetY + (facility.gridY + floorOffsetY) * this.CELL_SIZE;
            const w = facility.size.w * this.CELL_SIZE;
            const h = facility.size.h * this.CELL_SIZE;
            
            const rect = this.scene.add.rectangle(
                x + w / 2,
                y + h / 2,
                w - 1,
                h - 1,
                facility.getColor(),
                0.8
            );
            this.container.add(rect);
            this.facilitySprites.push(rect);
        });
    }
    
    destroy(): void {
        this.container.destroy();
        EventBus.off('facility:built', this.renderBase, this);
        EventBus.off('facility:dismantled', this.renderBase, this);
        EventBus.off('facility:upgraded', this.renderBase, this);
    }
}