// src/systems/BaseMovementSystem.ts
import * as Phaser from 'phaser';  // ✅ เพิ่ม
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { MapNode } from '../entities/MapNode';
import { Systems } from '../main';

export class BaseMovementSystem {
    private scene: Phaser.Scene | null = null;  // ✅ แก้

    private currentPathFrom: string | null = null;
    private currentPathTo: string | null = null;
    private progressOnPath: number = 0;
    private isMoving: boolean = false;
    private isWaitingAtJunction: boolean = false;
    
    // ============================================================
    // INITIALIZE
    // ============================================================
    init(scene?: Phaser.Scene): void {  // ✅ แก้
        this.scene = scene || null;
        
        const state = GameState;
        const basePos = state.getBasePosition();
        
        const startNode = Systems.map.getStartNode();
        basePos.x = startNode.position.x;
        basePos.y = startNode.position.y;
        basePos.currentNodeId = startNode.id;
        basePos.nextNodeId = null;
        basePos.progressOnPath = 0;
        
        this.currentPathFrom = startNode.id;
        this.currentPathTo = null;
        this.progressOnPath = 0;
        this.isMoving = false;
        this.isWaitingAtJunction = false;
        
        Systems.map.updateFog({ x: basePos.x, y: basePos.y });
        
        EventBus.emit(EVENTS.BASE_MOVED, basePos);
    }

    private getScene(): Phaser.Scene | null {  // ✅ แก้
        return this.scene;
    }
    
    // ============================================================
    // UPDATE (Called every game minute)
    // ============================================================
    update(deltaGameMinutes: number): void {
        const state = GameState;
        const time = state.getTime();
        const engine = state.getEngine();
        
        if (time.isPaused) return;
        if (state.isGameOver()) return;
        
        // ✅ ถ้ารออยู่ที่ Junction → ไม่ทำอะไร
        if (this.isWaitingAtJunction) return;
        
        // ✅ ถ้าไม่มี path → หา path ใหม่
        if (!this.currentPathTo) {
            this.findNextPath();
            return;
        }
        
        // ✅ เคลื่อนที่ไปตาม path
        this.moveAlongPath(deltaGameMinutes);
    }
    
    // ============================================================
    // FIND NEXT PATH
    // ============================================================
    private findNextPath(): void {
        const state = GameState;
        const basePos = state.getBasePosition();
        
        if (!this.currentPathFrom) return;
        
        const currentNode = Systems.map.getNode(this.currentPathFrom);
        if (!currentNode) return;
        
        // ✅ เช็คว่าเป็น Junction หรือไม่
        if (currentNode.type === 'junction') {
            // ✅ รอเลือกทาง
            this.isWaitingAtJunction = true;
            this.showJunctionPopup(currentNode);
            return;
        }
        
        // ✅ หา next node (ถ้ามี connection เดียว)
        const connections = Systems.map.getConnections(currentNode.id);
        
        if (connections.length === 0) {
            console.warn(`⚠️ Node ${currentNode.id} has no connections!`);
            return;
        }
        
        if (connections.length === 1) {
            // ✅ ทางเดียว → ไปเลย
            this.setNextPath(currentNode.id, connections[0].id);
        } else {
            // ✅ หลายทาง → รอเลือก (เป็น junction by default)
            this.isWaitingAtJunction = true;
            this.showJunctionPopup(currentNode);
        }
    }
    
    private showJunctionPopup(junction: MapNode): void {
        // ✅ สร้าง choices
        const connections = Systems.map.getConnections(junction.id);
        
        const choices = connections.map((conn, index) => ({
            nodeId: conn.id,
            direction: 'straight' as const,
            label: `${conn.getIcon()} ${conn.getLabel()}`,
        }));
        
        // ✅ Emit event
        EventBus.emit(EVENTS.UI_JUNCTION_POPUP, {
            junction,
            choices,
        });
    }
    
    // ============================================================
    // SET NEXT PATH
    // ============================================================
    setNextPath(fromId: string, toId: string): void {
        this.currentPathFrom = fromId;
        this.currentPathTo = toId;
        this.progressOnPath = 0;
        this.isMoving = true;
        this.isWaitingAtJunction = false;
        
        const state = GameState;
        const basePos = state.getBasePosition();
        basePos.nextNodeId = toId;
        basePos.progressOnPath = 0;
        
        console.log(`🚂 Base moving: ${fromId} → ${toId}`);
    }
    
    // ============================================================
    // MOVE ALONG PATH
    // ============================================================
    private moveAlongPath(deltaGameMinutes: number): void {
        if (!this.currentPathFrom || !this.currentPathTo) return;
        
        const state = GameState;
        const engine = state.getEngine();
        const basePos = state.getBasePosition();
        
        const fromNode = Systems.map.getNode(this.currentPathFrom);
        const toNode = Systems.map.getNode(this.currentPathTo);
        
        if (!fromNode || !toNode) return;
        
        // ✅ ระยะทาง (ชั่วโมง)
        const distanceHours = Systems.map.getDistanceBetween(
            this.currentPathFrom,
            this.currentPathTo
        );
        
        if (distanceHours <= 0) {
            // ✅ ถึงแล้ว
            this.arriveAtNode(toNode);
            return;
        }
        
        // ✅ คำนวณความเร็ว (ชั่วโมงต่อนาทีเกม)
        const speedKmPerHour = GAME_CONFIG.BASE_SPEED_KM_PER_HOUR * engine.speedMultiplier;
        const kmPerHour = speedKmPerHour;
        const kmPerMinute = kmPerHour / 60;
        
        // ✅ ระยะทางทั้งหมด (km)
        const totalDistanceKm = distanceHours * kmPerHour;
        
        // ✅ ระยะทางที่เคลื่อนที่ได้ (km)
        const distanceMovedKm = kmPerMinute * deltaGameMinutes;
        
        // ✅ Progress
        const progressDelta = distanceMovedKm / totalDistanceKm;
        this.progressOnPath += progressDelta;
        
        // ✅ Clamp
        this.progressOnPath = Math.min(1, this.progressOnPath);
        
        // ✅ อัพเดทตำแหน่ง
        basePos.x = fromNode.position.x + (toNode.position.x - fromNode.position.x) * this.progressOnPath;
        basePos.y = fromNode.position.y + (toNode.position.y - fromNode.position.y) * this.progressOnPath;
        basePos.progressOnPath = this.progressOnPath;
        
        // ✅ อัพเดท fog
        Systems.map.updateFog({ x: basePos.x, y: basePos.y });
        
        // ✅ Emit event
        EventBus.emit(EVENTS.BASE_MOVED, {
            x: basePos.x,
            y: basePos.y,
            progress: this.progressOnPath,
        });
        
        // ✅ ถึงแล้ว?
        if (this.progressOnPath >= 1) {
            this.arriveAtNode(toNode);
        }
    }
    
    // ============================================================
    // ARRIVE AT NODE
    // ============================================================
    private arriveAtNode(node: MapNode): void {
        const state = GameState;
        const basePos = state.getBasePosition();
        
        // ✅ Snap ตำแหน่ง
        basePos.x = node.position.x;
        basePos.y = node.position.y;
        basePos.currentNodeId = node.id;
        basePos.nextNodeId = null;
        basePos.progressOnPath = 0;
        
        // ✅ Reset
        this.currentPathFrom = node.id;
        this.currentPathTo = null;
        this.progressOnPath = 0;
        this.isMoving = false;
        
        // ✅ Mark visited
        node.isVisited = true;
        
        console.log(`📍 Base arrived at: ${node.id} (${node.type})`);
        
        // ✅ Emit event
        EventBus.emit(EVENTS.BASE_REACHED_NODE, node);
        
        // ✅ เช็ค Raid
        this.checkRaidTrigger(node);
        
        // ✅ เช็ค Finish
        if (node.type === 'finish') {
            state.triggerGameOver('reached_finish', true);
            return;
        }
        
        // ✅ หา path ถัดไป (ในรอบถัดไป)
        this.findNextPath();
    }
    
    // ============================================================
    // RAID TRIGGER
    // ============================================================
    private checkRaidTrigger(node: MapNode): void {
        const raid = Systems.raid.checkRaidTrigger(node.id);
        
        if (raid) {
            raid.isTriggered = true;
            
            console.log(`⚠️ RAID triggered at: ${node.id}`);
            
            // ✅ Emit event เท่านั้น
            EventBus.emit(EVENTS.RAID_TRIGGERED, raid);
        }
    }
    
    // ============================================================
    // GETTERS
    // ============================================================
    getCurrentPathFrom(): string | null {
        return this.currentPathFrom;
    }
    
    getCurrentPathTo(): string | null {
        return this.currentPathTo;
    }
    
    getProgress(): number {
        return this.progressOnPath;
    }
    
    isBaseMoving(): boolean {
        return this.isMoving;
    }
    
    isWaiting(): boolean {
        return this.isWaitingAtJunction;
    }
}