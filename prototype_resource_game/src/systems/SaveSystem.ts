// src/systems/SaveSystem.ts
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Crew } from '../entities/Crew';
import { Vehicle } from '../entities/Vehicle';
import { MapNode } from '../entities/MapNode';
import { Facility } from '../entities/Facility';

const SAVE_KEY = 'survival_base_save';
const SAVE_VERSION = '1.0.0';

export interface SaveData {
    version: string;
    timestamp: number;
    data: any;
}

export class SaveSystem {
    private autoSaveInterval: number = 5 * 60 * 1000;  // 5 นาที
    private autoSaveTimer: Phaser.Time.TimerEvent | null = null;
    private scene: Phaser.Scene | null = null;
    
    // ============================================================
    // INITIALIZE
    // ============================================================
    init(scene: Phaser.Scene): void {
        this.scene = scene;
        this.startAutoSave();
    }
    
    private startAutoSave(): void {
        if (!this.scene) return;
        
        if (this.autoSaveTimer) {
            this.autoSaveTimer.remove();
        }
        
        this.autoSaveTimer = this.scene.time.addEvent({
            delay: this.autoSaveInterval,
            callback: this.autoSave,
            callbackScope: this,
            loop: true,
        });
    }
    
    // ============================================================
    // AUTO SAVE
    // ============================================================
    private autoSave(): void {
        const state = GameState;
        
        if (state.isGameOver()) return;
        
        console.log('💾 Auto-saving...');
        this.save();
    }
    
    // ============================================================
    // SAVE
    // ============================================================
    save(): boolean {
        try {
            const state = GameState;
            const data = state.getData();
            
            // ✅ Serialize entities
            const serialized = {
                runId: data.runId,
                seed: data.seed,
                
                time: { ...data.time },
                basePosition: { ...data.basePosition },
                fuel: { ...data.fuel },
                engine: { ...data.engine },
                
                vehicles: data.vehicles.map(v => v.toJSON()),
                crew: data.crew.map(c => c.toJSON()),
                nodes: data.nodes.map(n => ({
                    id: n.id,
                    type: n.type,
                    position: n.position,
                    connections: n.connections,
                    resourceType: n.resourceType,
                    resourceAmount: n.resourceAmount,
                    rarity: n.rarity,
                    monsterType: n.monsterType,
                    monsterRank: n.monsterRank,
                    monsterAmount: n.monsterAmount,
                    relicType: n.relicType,
                    relicSize: n.relicSize,
                    threat: n.threat,
                    shopType: n.shopType,
                    isDiscovered: n.isDiscovered,
                    isVisited: n.isVisited,
                })),
                paths: data.paths.map(p => ({ ...p })),
                raids: data.raids.map(r => ({ ...r })),
                
                resources: { ...data.resources },
                credits: data.credits,
                crewPoints: data.crewPoints,
                
                fog: {
                    revealedNodes: Array.from(data.fog.revealedNodes),
                    visibilityRadius: data.fog.visibilityRadius,
                },
                
                gameOver: data.gameOver ? { ...data.gameOver } : null,
            };
            
            const saveData: SaveData = {
                version: SAVE_VERSION,
                timestamp: Date.now(),
                data: serialized,
            };
            
            localStorage.setItem(SAVE_KEY, JSON.stringify(saveData));
            
            console.log('✅ Game saved');
            EventBus.emit('save:success');
            
            return true;
        } catch (error) {
            console.error('❌ Save failed:', error);
            EventBus.emit('save:error', error);
            return false;
        }
    }
    
    // ============================================================
    // LOAD
    // ============================================================
    load(): boolean {
        try {
            const raw = localStorage.getItem(SAVE_KEY);
            if (!raw) {
                console.log('ℹ️ No save found');
                return false;
            }
            
            const saveData: SaveData = JSON.parse(raw);
            
            // ✅ Check version
            if (saveData.version !== SAVE_VERSION) {
                console.warn(`⚠️ Save version mismatch: ${saveData.version} vs ${SAVE_VERSION}`);
                // (TODO: migrate)
            }
            
            const data = saveData.data;
            const state = GameState;
            
            // ✅ Restore state
            const stateData = state.getData();
            
            stateData.runId = data.runId;
            stateData.seed = data.seed;
            
            Object.assign(stateData.time, data.time);
            Object.assign(stateData.basePosition, data.basePosition);
            Object.assign(stateData.fuel, data.fuel);
            Object.assign(stateData.engine, data.engine);
            
            // ✅ Restore entities
            stateData.vehicles = data.vehicles.map((v: any) => Vehicle.fromJSON(v));
            stateData.crew = data.crew.map((c: any) => Crew.fromJSON(c));
            stateData.nodes = data.nodes.map((n: any) => new MapNode(n));
            
            Object.assign(stateData.paths, data.paths);
            Object.assign(stateData.raids, data.raids);
            
            Object.assign(stateData.resources, data.resources);
            stateData.credits = data.credits;
            stateData.crewPoints = data.crewPoints;
            
            stateData.fog.revealedNodes = new Set(data.fog.revealedNodes);
            stateData.fog.visibilityRadius = data.fog.visibilityRadius;
            
            stateData.gameOver = data.gameOver;
            
            console.log('✅ Game loaded');
            EventBus.emit('load:success');
            
            return true;
        } catch (error) {
            console.error('❌ Load failed:', error);
            EventBus.emit('load:error', error);
            return false;
        }
    }
    
    // ============================================================
    // DELETE
    // ============================================================
    deleteSave(): void {
        localStorage.removeItem(SAVE_KEY);
        console.log('🗑️ Save deleted');
        EventBus.emit('save:deleted');
    }
    
    // ============================================================
    // CHECK
    // ============================================================
    hasSave(): boolean {
        return localStorage.getItem(SAVE_KEY) !== null;
    }
    
    getSaveInfo(): { day: number; timestamp: number } | null {
        try {
            const raw = localStorage.getItem(SAVE_KEY);
            if (!raw) return null;
            
            const saveData: SaveData = JSON.parse(raw);
            
            return {
                day: saveData.data.time?.gameDay || 1,
                timestamp: saveData.timestamp,
            };
        } catch {
            return null;
        }
    }
    
    // ============================================================
    // DESTROY
    // ============================================================
    destroy(): void {
        if (this.autoSaveTimer) {
            this.autoSaveTimer.remove();
            this.autoSaveTimer = null;
        }
        this.scene = null;
    }
}