// src/entities/Facility.ts
import { GAME_CONFIG } from '../config';

export type FacilityType = 'engine' | 'kitchen' | 'factory' | 'workshop' | 'turret' | 'storage';

// ✅ ลบ type ออกจาก interface
export interface FacilityConfig {
    size: { w: number; h: number };
    floors: number;
    maxLevel: number;
    buildCost: Record<string, number>;
    upgradeCost: Record<string, Record<string, number>>;
    production?: {
        baseTimePerFood?: number;
        timeReductionPerLevel?: number;
    };
}

export class Facility {
    public id: string;
    public type: FacilityType;
    public level: number;
    public floor: number;
    public gridX: number;
    public gridY: number;
    public size: { w: number; h: number };
    public condition: number;
    public workers: number[];
    public productionProgress: number;
    public isActive: boolean;
    
    // Visual
    public sprite: Phaser.GameObjects.Rectangle | null = null;
    public label: Phaser.GameObjects.Text | null = null;
    public icon: Phaser.GameObjects.Text | null = null;
    
    constructor(data: Partial<Facility>) {
        this.id = data.id || `facility_${Date.now()}_${Math.random()}`;
        this.type = data.type || 'storage';
        this.level = data.level || 1;
        this.floor = data.floor || 1;
        this.gridX = data.gridX || 0;
        this.gridY = data.gridY || 0;
        
        const config = GAME_CONFIG.FACILITY_TYPES[this.type];
        this.size = { w: config.size.w, h: config.size.h };
        
        this.condition = data.condition ?? 100;
        this.workers = data.workers || [];
        this.productionProgress = data.productionProgress || 0;
        this.isActive = data.isActive ?? true;
    }
    
    // ============================================================
    // STATIC FACTORY
    // ============================================================
    static create(type: FacilityType, floor: number, gridX: number, gridY: number): Facility {
        return new Facility({
            type,
            floor,
            gridX,
            gridY,
            level: 1,
            condition: 100,
        });
    }
    
    // ============================================================
    // CONFIG
    // ============================================================
    
    // ✅ ใช้ type assertion ผ่าน unknown
    getConfig(): FacilityConfig {
        return GAME_CONFIG.FACILITY_TYPES[this.type] as unknown as FacilityConfig;
    }
    
    // ============================================================
    // WORKER MANAGEMENT
    // ============================================================
    addWorker(crewId: number): boolean {
        if (this.workers.includes(crewId)) return false;
        this.workers.push(crewId);
        return true;
    }
    
    removeWorker(crewId: number): boolean {
        const index = this.workers.indexOf(crewId);
        if (index === -1) return false;
        this.workers.splice(index, 1);
        return true;
    }
    
    hasWorker(): boolean {
        return this.workers.length > 0;
    }
    
    getWorkerCount(): number {
        return this.workers.length;
    }
    
    // ============================================================
    // UPGRADE
    // ============================================================
    canUpgrade(): boolean {
        const config = this.getConfig();
        return this.level < config.maxLevel;
    }
    
    getUpgradeCost(): Record<string, number> {
        const config = this.getConfig();
        const nextLevel = this.level + 1;
        const costKey = `level${nextLevel}`;
        return config.upgradeCost[costKey] || {};
    }
    
    upgrade(): void {
        if (!this.canUpgrade()) return;
        this.level++;
    }
    
    // ============================================================
    // CONDITION
    // ============================================================
    takeDamage(amount: number): void {
        this.condition = Math.max(0, this.condition - amount);
        if (this.condition <= 0) {
            this.isActive = false;
        }
    }
    
    repair(amount: number): void {
        this.condition = Math.min(100, this.condition + amount);
        if (this.condition > 0) {
            this.isActive = true;
        }
    }
    
    isDamaged(): boolean {
        return this.condition < 100;
    }
    
    // ============================================================
    // VISUAL
    // ============================================================
    getColor(): number {
        const colors: { [key: string]: number } = {
            engine: 0xe74c3c,
            kitchen: 0xf9ca24,
            factory: 0x6c5ce7,
            workshop: 0x4ecdc4,
            turret: 0xff6b6b,
            storage: 0x95a5a6,
        };
        return colors[this.type] || 0xffffff;
    }
    
    getIcon(): string {
        const icons: { [key: string]: string } = {
            engine: '🚂',
            kitchen: '🍳',
            factory: '🏭',
            workshop: '🔧',
            turret: '🔫',
            storage: '📦',
        };
        return icons[this.type] || '📦';
    }
    
    getLabel(): string {
        return `${this.getIcon()} ${this.type.toUpperCase()} Lv.${this.level}`;
    }
    
    // ============================================================
    // SERIALIZATION
    // ============================================================
    toJSON(): object {
        return {
            id: this.id,
            type: this.type,
            level: this.level,
            floor: this.floor,
            gridX: this.gridX,
            gridY: this.gridY,
            condition: this.condition,
            workers: [...this.workers],
            productionProgress: this.productionProgress,
            isActive: this.isActive,
        };
    }
    
    static fromJSON(data: any): Facility {
        return new Facility(data);
    }
}