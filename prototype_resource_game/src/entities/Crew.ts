// src/entities/Crew.ts
import { GAME_CONFIG } from '../config';

// ✅ Rank System
export enum Rank {
    F = 'F',
    E = 'E',
    D = 'D',
    C = 'C',
    B = 'B',
    A = 'A',
    AA = 'AA',
    AAA = 'AAA',
    S = 'S',
    SS = 'SS',
    SSS = 'SSS',
    EX = 'EX',
}

export interface RankThreshold {
    min: number;
    max: number;
    rank: Rank;
}

export const RANK_THRESHOLDS: RankThreshold[] = [
    { min: 0, max: 14, rank: Rank.F },
    { min: 15, max: 29, rank: Rank.E },
    { min: 30, max: 44, rank: Rank.D },
    { min: 45, max: 59, rank: Rank.C },
    { min: 60, max: 79, rank: Rank.B },
    { min: 80, max: 89, rank: Rank.A },
    { min: 90, max: 94, rank: Rank.AA },
    { min: 95, max: 99, rank: Rank.AAA },
    { min: 100, max: 129, rank: Rank.S },
    { min: 130, max: 144, rank: Rank.SS },
    { min: 145, max: 150, rank: Rank.SSS },
    { min: 151, max: Infinity, rank: Rank.EX },
];

export interface Equipment {
    weapon: {
        name: string;
        type: 'gun' | 'melee' | 'ranged';
        huntingBonus: number;    // flat bonus
        gatheringBonus: number;  // flat bonus
    } | null;
    armor: {
        name: string;
        defenseBonus: number;    // flat bonus
        hpBonus: number;         // flat bonus
    } | null;
    accessory: {
        name: string;
        searchBonus: number;     // flat bonus
        speedBonus: number;      // flat bonus
    } | null;
}

export class Crew {
    public id: number;
    public name: string;
    public isAlive: boolean;
    public isBusy: boolean;
    
    // ✅ Base Stats (0-100) - ค่าของแต่ละคน
    public baseSpeed: number;      // 0-100
    public baseGathering: number;  // 0-100
    public baseSearching: number;  // 0-100
    public baseHunting: number;    // 0-100
    
    // ✅ HP
    public hp: number;
    public maxHp: number;
    
    // ✅ Perks (เป็น %)
    public perks: string[];
    
    // ✅ Equipment (flat bonus)
    public equipment: Equipment;
    
    public hireCost: number;
    public position: { x: number; y: number };
    public sprite: Phaser.GameObjects.Text | null;

    constructor(data: Partial<Crew>) {
        this.id = data.id || Date.now();
        this.name = data.name || 'Crew';
        this.isAlive = true;
        this.isBusy = false;
        
        // ✅ Base Stats (0-100)
        this.baseSpeed = data.baseSpeed ?? 50;
        this.baseGathering = data.baseGathering ?? 50;
        this.baseSearching = data.baseSearching ?? 50;
        this.baseHunting = data.baseHunting ?? 50;
        
        // ✅ HP
        this.maxHp = data.maxHp ?? 100;
        this.hp = this.maxHp;
        
        // ✅ Perks & Equipment
        this.perks = data.perks || [];
        this.equipment = {
            weapon: null,
            armor: null,
            accessory: null,
        };
        
        this.hireCost = data.hireCost || 20;
        this.position = data.position || { x: 0, y: 0 };
        this.sprite = null;
    }

    // ✅ คำนวณ Stat จริง: (Base + Equipment Bonus) * (1 + Perk %)
    getEffectiveSpeed(): number {
        let flatBonus = 0;
        if (this.equipment.accessory) {
            flatBonus += this.equipment.accessory.speedBonus || 0;
        }
        const total = this.baseSpeed + flatBonus;
        return this.applyPerkMultiplier(total, 'speed');
    }

    getEffectiveGathering(): number {
        let flatBonus = 0;
        if (this.equipment.weapon) {
            flatBonus += this.equipment.weapon.gatheringBonus || 0;
        }
        const total = this.baseGathering + flatBonus;
        return this.applyPerkMultiplier(total, 'gathering');
    }

    getEffectiveSearching(): number {
        let flatBonus = 0;
        if (this.equipment.accessory) {
            flatBonus += this.equipment.accessory.searchBonus || 0;
        }
        const total = this.baseSearching + flatBonus;
        return this.applyPerkMultiplier(total, 'searching');
    }

    getEffectiveHunting(): number {
        let flatBonus = 0;
        if (this.equipment.weapon) {
            flatBonus += this.equipment.weapon.huntingBonus || 0;
        }
        if (this.equipment.armor) {
            flatBonus += this.equipment.armor.defenseBonus || 0;
        }
        const total = this.baseHunting + flatBonus;
        return this.applyPerkMultiplier(total, 'hunting');
    }

    // ✅ ใช้ Perk เป็น %
    private applyPerkMultiplier(value: number, statType: 'speed' | 'gathering' | 'searching' | 'hunting'): number {
        let multiplier = 1;
        for (const perk of this.perks) {
            const bonus = this.getPerkBonus(perk, statType);
            multiplier += bonus / 100;
        }
        return Math.round(value * multiplier * 10) / 10;
    }

    // ✅ ดึงค่า Perk Bonus (เป็น %)
    private getPerkBonus(perk: string, statType: string): number {
        const PERK_BONUSES: { [key: string]: { [key: string]: number } } = {
            'sprinter': { speed: 20 },
            'fast_hands': { gathering: 25 },
            'night_vision': { searching: 20 },
            'gunslinger': { hunting: 30 },
            'blacksmith': { gathering: 15 },
            'quick_reflex': { speed: 15 },
            'tough': { hp: 30 },
            'lucky': { gathering: 10, searching: 10 },
            'strong': { hunting: 20 },
            'scout': { speed: 10, searching: 10 },
            'medic': { hp: 20 },
        };
        return PERK_BONUSES[perk]?.[statType] || 0;
    }

    // ✅ ดึง Rank จากค่า Stat
    getRank(value: number): Rank {
        for (const threshold of RANK_THRESHOLDS) {
            if (value >= threshold.min && value <= threshold.max) {
                return threshold.rank;
            }
        }
        return Rank.F;
    }

    // ✅ ดึง Rank Color
    getRankColor(rank: Rank): string {
        const colors: { [key: string]: string } = {
            'F': '#808080',
            'E': '#4a90d9',
            'D': '#50c878',
            'C': '#f9ca24',
            'B': '#ff8c00',
            'A': '#ff6b6b',
            'AA': '#e74c3c',
            'AAA': '#8e44ad',
            'S': '#ff00ff',
            'SS': '#ffd700',
            'SSS': '#ff4500',
            'EX': '#ff0000',
        };
        return colors[rank] || '#ffffff';
    }

    // ✅ ฟังก์ชันสำหรับ UI
    getRankDisplay(value: number): { rank: Rank; color: string } {
        const rank = this.getRank(value);
        return { rank, color: this.getRankColor(rank) };
    }

    hasPerk(perkId: string): boolean {
        return this.perks.includes(perkId);
    }

    // ✅ คำนวณเวลาเดินทางเป็นหน่วยเวลา (ใช้ระยะทางจริง)
    calculateTravelTime(distance: number): number {
        // ✅ ใช้ระยะทางจริง (pixel) คูณด้วย factor
        const baseTime = GAME_CONFIG.BASE_TRAVEL_TIME || 200;
        const distanceFactor = distance * GAME_CONFIG.TRAVEL_DISTANCE_FACTOR;
        const speed = this.getEffectiveSpeed();
        
        // ✅ speed 0-100 → ใช้ 100 เป็น speed สูงสุด
        const speedFactor = Math.max(0.1, speed / 100);
        
        const travelTime = (baseTime + distanceFactor) / speedFactor;
        return travelTime;
    }

    takeDamage(damage: number): boolean {
        this.hp -= damage;
        if (this.hp <= 0) {
            this.hp = 0;
            this.isAlive = false;
            return true;
        }
        return false;
    }

    heal(amount: number): void {
        this.hp = Math.min(this.maxHp, this.hp + amount);
    }
}