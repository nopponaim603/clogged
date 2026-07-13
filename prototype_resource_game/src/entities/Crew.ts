// src/entities/Crew.ts
import { PERKS } from '../data/Constants';
import { GAME_CONFIG } from '../config';

export interface Equipment {
    weapon: {
        name: string;
        type: string;
        huntingBonus: number;
        gatheringBonus: number;
    } | null;
    armor: {
        name: string;
        defenseBonus: number;
    } | null;
    accessory: {
        name: string;
        searchBonus: number;
    } | null;
}

export class Crew {
    public id: number;
    public name: string;
    public hp: number;
    public maxHp: number;
    public speed: number;
    public gatheringProficiency: number;
    public searchingProficiency: number;
    public huntingProficiency: number;
    public perks: string[];
    public hireCost: number;
    public isBusy: boolean;
    public position: { x: number; y: number };
    public equipment: Equipment;
    public isAlive: boolean;
    public sprite: Phaser.GameObjects.Text | null;

    constructor(data: Partial<Crew>) {
        this.id = data.id || Date.now();
        this.name = data.name || 'Crew';
        this.hp = data.hp || 100;
        this.maxHp = data.hp || 100;
        this.speed = data.speed || 1.0;
        this.gatheringProficiency = data.gatheringProficiency || 1.0;
        this.searchingProficiency = data.searchingProficiency || 1.0;
        this.huntingProficiency = data.huntingProficiency || 1.0;
        this.perks = data.perks || [];
        this.hireCost = data.hireCost || 20;
        this.isBusy = false;
        this.position = data.position || { x: 0, y: 0 };
        this.equipment = {
            weapon: null,
            armor: null,
            accessory: null,
        };
        this.isAlive = true;
        this.sprite = null;
    }

    getEffectiveGathering(): number {
        let base = this.gatheringProficiency;
        if (this.hasPerk('fast_hands')) base *= 2;
        if (this.equipment.weapon) base += this.equipment.weapon.gatheringBonus || 0;
        return base;
    }

    getEffectiveSearching(): number {
        let base = this.searchingProficiency;
        if (this.hasPerk('night_vision')) base *= 1.5;
        if (this.equipment.accessory) base += this.equipment.accessory.searchBonus || 0;
        return base;
    }

    getEffectiveHunting(): number {
        let base = this.huntingProficiency;
        if (this.hasPerk('gunslinger') && this.equipment.weapon?.type === 'gun') {
            base *= 2;
        }
        if (this.equipment.weapon) base += this.equipment.weapon.huntingBonus || 0;
        if (this.equipment.armor) base += this.equipment.armor.defenseBonus || 0;
        return base;
    }

    getEffectiveSpeed(): number {
        let base = this.speed;
        if (this.hasPerk('quick_reflex')) base *= 1.3;
        return base;
    }

    hasPerk(perkId: string): boolean {
        return this.perks.includes(perkId);
    }

    // ✅ คำนวณเวลาเดินทางเป็นหน่วยเวลา
    calculateTravelTime(distance: number): number {
        // ✅ คำนวณเป็นหน่วยเวลา โดยตรง (ไม่คูณ TIME_UNIT_PER_SECOND)
        const baseTime = GAME_CONFIG.BASE_TRAVEL_TIME; // 200
        const distanceFactor = distance * 1.8;
        const speedFactor = this.getEffectiveSpeed();
        
        // คืนค่าเป็นหน่วยเวลา
        const travelTime = (baseTime + distanceFactor) / speedFactor;
        
        return travelTime;
    }

    takeDamage(damage: number): boolean {
        this.hp -= damage;
        if (this.hp <= 0) {
            this.hp = 0;
            this.isAlive = false;
            return true; // dead
        }
        return false;
    }
}