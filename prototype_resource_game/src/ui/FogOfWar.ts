// src/ui/FogOfWar.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Systems } from '../main';

export class FogOfWar {
    private scene: Scene;
    private fogGraphics: Phaser.GameObjects.Graphics;
    private visibilityRing: Phaser.GameObjects.Graphics;
    private visibleNodes: Set<string> = new Set();
    
    // ✅ จำนวน segments ยิ่งมาก ยิ่งกลม
    private readonly SEGMENTS = 96;
    
    constructor(scene: Scene) {
        this.scene = scene;
        
        this.fogGraphics = scene.add.graphics();
        this.fogGraphics.setDepth(100);
        
        this.visibilityRing = scene.add.graphics();
        this.visibilityRing.setDepth(101);
        
        EventBus.on(EVENTS.MAP_FOG_UPDATED, this.update, this);
        EventBus.on(EVENTS.BASE_MOVED, this.update, this);
        
        this.update();
    }
    
    update(): void {
        const state = GameState;
        const basePos = state.getBasePosition();
        const radius = GAME_CONFIG.FOG_VISIBILITY_RADIUS;
        const camera = this.scene.cameras.main;
        
        if (!Systems.map) return;
        
        // ✅ คำนวณ outer radius ให้ใหญ่พอครอบจอ
        const zoom = camera.zoom;
        const viewWidth = camera.width / zoom;
        const viewHeight = camera.height / zoom;
        
        // ✅ ระยะไกลสุดจาก base ไปมุมจอ
        const dx = Math.max(
            Math.abs(basePos.x - camera.scrollX),
            Math.abs(basePos.x - (camera.scrollX + viewWidth))
        );
        const dy = Math.max(
            Math.abs(basePos.y - camera.scrollY),
            Math.abs(basePos.y - (camera.scrollY + viewHeight))
        );
        const outerRadius = Math.sqrt(dx * dx + dy * dy) + 2000;
        
        // ✅ วาด fog
        this.fogGraphics.clear();
        this.fogGraphics.fillStyle(GAME_CONFIG.COLORS.BG_OVERLAY, 0.88);
        
        // ✅ วาด 96 segments (trapezoid) ระหว่าง innerRadius (visibility) กับ outerRadius
        this.drawFogRing(basePos.x, basePos.y, radius, outerRadius);
        
        // ✅ วาด Visibility Ring
        this.visibilityRing.clear();
        this.visibilityRing.lineStyle(3, GAME_CONFIG.COLORS.PATH_ACTIVE, 0.6);
        this.visibilityRing.strokeCircle(basePos.x, basePos.y, radius);
        this.visibilityRing.lineStyle(1, GAME_CONFIG.COLORS.PATH_ACTIVE, 0.3);
        this.visibilityRing.strokeCircle(basePos.x, basePos.y, radius - 20);
        
        // ✅ อัพเดท visible nodes
        const visibleNodes = Systems.map.getVisibleNodes(basePos);
        this.visibleNodes.clear();
        visibleNodes.forEach(node => this.visibleNodes.add(node.id));
    }
    
    // ============================================================
    // ✅ วาด Fog เป็น Ring (trapezoid segments)
    // ============================================================
    private drawFogRing(cx: number, cy: number, innerR: number, outerR: number): void {
        for (let i = 0; i < this.SEGMENTS; i++) {
            const angle1 = (i / this.SEGMENTS) * Math.PI * 2;
            const angle2 = ((i + 1) / this.SEGMENTS) * Math.PI * 2;
            
            // ✅ จุด 4 จุดของ trapezoid นี้
            const innerX1 = cx + Math.cos(angle1) * innerR;
            const innerY1 = cy + Math.sin(angle1) * innerR;
            const innerX2 = cx + Math.cos(angle2) * innerR;
            const innerY2 = cy + Math.sin(angle2) * innerR;
            
            const outerX1 = cx + Math.cos(angle1) * outerR;
            const outerY1 = cy + Math.sin(angle1) * outerR;
            const outerX2 = cx + Math.cos(angle2) * outerR;
            const outerY2 = cy + Math.sin(angle2) * outerR;
            
            // ✅ วาดรูป 4 เหลี่ยม
            this.fogGraphics.beginPath();
            this.fogGraphics.moveTo(innerX1, innerY1);
            this.fogGraphics.lineTo(outerX1, outerY1);
            this.fogGraphics.lineTo(outerX2, outerY2);
            this.fogGraphics.lineTo(innerX2, innerY2);
            this.fogGraphics.closePath();
            this.fogGraphics.fillPath();
        }
    }
    
    isNodeVisible(nodeId: string): boolean {
        return this.visibleNodes.has(nodeId);
    }
    
    isNodeInFog(nodeId: string): boolean {
        return !this.visibleNodes.has(nodeId);
    }
    
    getVisibleNodeIds(): string[] {
        return Array.from(this.visibleNodes);
    }
    
    show(): void {
        this.fogGraphics.setVisible(true);
        this.visibilityRing.setVisible(true);
    }
    
    hide(): void {
        this.fogGraphics.setVisible(false);
        this.visibilityRing.setVisible(false);
    }
    
    destroy(): void {
        this.fogGraphics.destroy();
        this.visibilityRing.destroy();
        
        EventBus.off(EVENTS.MAP_FOG_UPDATED, this.update, this);
        EventBus.off(EVENTS.BASE_MOVED, this.update, this);
    }
}