// src/systems/PrestigeSystem.ts
import { GAME_CONFIG } from '../config';
import { EventBus, EVENTS } from '../state/EventBus';

const PRESTIGE_KEY = 'survival_base_prestige';

export interface PrestigeUpgrade {
    id: string;
    name: string;
    description: string;
    icon: string;
    maxLevel: number;
    baseCost: number;
    costMultiplier: number;  // ต้นทุนเพิ่มขึ้นต่อ level
    effect: (level: number) => any;
    getEffectText: (level: number) => string;
}

export interface PrestigeData {
    totalPoints: number;        // แต้มสะสม
    spentPoints: number;        // แต้มที่ใช้ไป
    upgrades: { [key: string]: number };  // level ของแต่ละ upgrade
    totalRuns: number;
    bestDay: number;
    totalWins: number;
    totalLosses: number;
}

export class PrestigeSystem {
    private data: PrestigeData;
    
    private upgrades: PrestigeUpgrade[] = [
        {
            id: 'starting_credits',
            name: 'Starting Credits',
            description: 'เริ่มเกมด้วย Credits เพิ่มขึ้น',
            icon: '💰',
            maxLevel: 5,
            baseCost: 10,
            costMultiplier: 2,
            effect: (level: number) => GAME_CONFIG.STARTING_CREDITS + level * 200,
            getEffectText: (level: number) => `+${level * 200} Credits`,
        },
        {
            id: 'starting_crew_points',
            name: 'Starting Crew Points',
            description: 'เริ่มเกมด้วย Crew Points เพิ่มขึ้น',
            icon: '👥',
            maxLevel: 5,
            baseCost: 15,
            costMultiplier: 2,
            effect: (level: number) => GAME_CONFIG.STARTING_CREW_POINTS + level * 30,
            getEffectText: (level: number) => `+${level * 30} Points`,
        },
        {
            id: 'starting_fuel',
            name: 'Fuel Capacity',
            description: 'ความจุน้ำมันเริ่มต้นเพิ่มขึ้น',
            icon: '⛽',
            maxLevel: 5,
            baseCost: 12,
            costMultiplier: 2,
            effect: (level: number) => GAME_CONFIG.FUEL_CAPACITY + level * 100,
            getEffectText: (level: number) => `+${level * 100} Fuel`,
        },
        {
            id: 'fuel_efficiency',
            name: 'Fuel Efficiency',
            description: 'การใช้น้ำมันลดลง',
            icon: '🔋',
            maxLevel: 5,
            baseCost: 20,
            costMultiplier: 2.5,
            effect: (level: number) => 1 - level * 0.1,
            getEffectText: (level: number) => `-${level * 10}% consumption`,
        },
        {
            id: 'engine_quality',
            name: 'Engine Quality',
            description: 'เครื่องยนต์เสื่อมสภาพช้าลง',
            icon: '🔧',
            maxLevel: 5,
            baseCost: 18,
            costMultiplier: 2.5,
            effect: (level: number) => 1 - level * 0.1,
            getEffectText: (level: number) => `-${level * 10}% decay`,
        },
        {
            id: 'base_speed',
            name: 'Base Speed',
            description: 'ความเร็วฐานเพิ่มขึ้น',
            icon: '🚂',
            maxLevel: 5,
            baseCost: 25,
            costMultiplier: 2.5,
            effect: (level: number) => 1 + level * 0.05,
            getEffectText: (level: number) => `+${level * 5}% speed`,
        },
        {
            id: 'crew_slots',
            name: 'Crew Slots',
            description: 'จำนวนลูกเรือสูงสุดเพิ่มขึ้น',
            icon: '➕',
            maxLevel: 6,
            baseCost: 30,
            costMultiplier: 3,
            effect: (level: number) => GAME_CONFIG.BASE_CREW_SLOTS + level,
            getEffectText: (level: number) => `+${level} slots`,
        },
        {
            id: 'starting_vehicles',
            name: 'Starting Vehicles',
            description: 'ยานพาหนะเริ่มต้นเพิ่มขึ้น',
            icon: '🚗',
            maxLevel: 3,
            baseCost: 50,
            costMultiplier: 3,
            effect: (level: number) => GAME_CONFIG.STARTING_VEHICLES + level,
            getEffectText: (level: number) => `+${level} vehicles`,
        },
        {
            id: 'food_efficiency',
            name: 'Food Efficiency',
            description: 'การบริโภคอาหารลดลง',
            icon: '🍖',
            maxLevel: 5,
            baseCost: 22,
            costMultiplier: 2.5,
            effect: (level: number) => 1 - level * 0.1,
            getEffectText: (level: number) => `-${level * 10}% consumption`,
        },
        {
            id: 'production_speed',
            name: 'Production Speed',
            description: 'การผลิตใน facilities เร็วขึ้น',
            icon: '⚙️',
            maxLevel: 5,
            baseCost: 28,
            costMultiplier: 2.5,
            effect: (level: number) => 1 + level * 0.1,
            getEffectText: (level: number) => `+${level * 10}% speed`,
        },
    ];
    
    constructor() {
        this.data = this.loadData();
    }
    
    // ============================================================
    // LOAD / SAVE
    // ============================================================
    private loadData(): PrestigeData {
        try {
            const raw = localStorage.getItem(PRESTIGE_KEY);
            if (raw) {
                return JSON.parse(raw);
            }
        } catch (error) {
            console.error('❌ Failed to load prestige data:', error);
        }
        
        return {
            totalPoints: 0,
            spentPoints: 0,
            upgrades: {},
            totalRuns: 0,
            bestDay: 0,
            totalWins: 0,
            totalLosses: 0,
        };
    }
    
    private saveData(): void {
        try {
            localStorage.setItem(PRESTIGE_KEY, JSON.stringify(this.data));
        } catch (error) {
            console.error('❌ Failed to save prestige data:', error);
        }
    }
    
    // ============================================================
    // POINTS
    // ============================================================
    addPoints(amount: number): void {
        this.data.totalPoints += amount;
        this.data.spentPoints += amount;
        this.saveData();
        
        console.log(`⭐ Gained ${amount} prestige points! Total: ${this.getAvailablePoints()}`);
        
        EventBus.emit('prestige:pointsGained', amount);
    }
    
    getAvailablePoints(): number {
        return this.data.totalPoints - this.data.spentPoints;
    }
    
    getTotalPoints(): number {
        return this.data.totalPoints;
    }
    
    // ============================================================
    // UPGRADES
    // ============================================================
    getUpgrades(): PrestigeUpgrade[] {
        return this.upgrades;
    }
    
    getUpgradeLevel(upgradeId: string): number {
        return this.data.upgrades[upgradeId] || 0;
    }
    
    getUpgradeCost(upgradeId: string): number {
        const upgrade = this.upgrades.find(u => u.id === upgradeId);
        if (!upgrade) return 0;
        
        const currentLevel = this.getUpgradeLevel(upgradeId);
        return Math.floor(upgrade.baseCost * Math.pow(upgrade.costMultiplier, currentLevel));
    }
    
    canPurchaseUpgrade(upgradeId: string): boolean {
        const upgrade = this.upgrades.find(u => u.id === upgradeId);
        if (!upgrade) return false;
        
        const currentLevel = this.getUpgradeLevel(upgradeId);
        if (currentLevel >= upgrade.maxLevel) return false;
        
        const cost = this.getUpgradeCost(upgradeId);
        return this.getAvailablePoints() >= cost;
    }
    
    purchaseUpgrade(upgradeId: string): boolean {
        if (!this.canPurchaseUpgrade(upgradeId)) return false;
        
        const upgrade = this.upgrades.find(u => u.id === upgradeId);
        if (!upgrade) return false;
        
        const cost = this.getUpgradeCost(upgradeId);
        this.data.spentPoints += cost;
        
        const currentLevel = this.getUpgradeLevel(upgradeId);
        this.data.upgrades[upgradeId] = currentLevel + 1;
        
        this.saveData();
        
        console.log(`⭐ Purchased ${upgrade.name} Lv.${currentLevel + 1} for ${cost} points`);
        
        EventBus.emit('prestige:upgradePurchased', { upgradeId, level: currentLevel + 1 });
        
        return true;
    }
    
    // ============================================================
    // GET EFFECTS
    // ============================================================
    getEffect(upgradeId: string): any {
        const upgrade = this.upgrades.find(u => u.id === upgradeId);
        if (!upgrade) return null;
        
        const level = this.getUpgradeLevel(upgradeId);
        return upgrade.effect(level);
    }
    
    // ============================================================
    // RUN STATS
    // ============================================================
    recordRun(day: number, isWin: boolean): void {
        this.data.totalRuns++;
        
        if (day > this.data.bestDay) {
            this.data.bestDay = day;
        }
        
        if (isWin) {
            this.data.totalWins++;
        } else {
            this.data.totalLosses++;
        }
        
        this.saveData();
    }
    
    getStats(): {
        totalRuns: number;
        bestDay: number;
        totalWins: number;
        totalLosses: number;
    } {
        return {
            totalRuns: this.data.totalRuns,
            bestDay: this.data.bestDay,
            totalWins: this.data.totalWins,
            totalLosses: this.data.totalLosses,
        };
    }
    
    // ============================================================
    // APPLY EFFECTS
    // ============================================================
    applyEffects(): void {
        // ✅ Apply ทั้งหมด (TODO: ให้ GameState ใช้ค่าจาก prestige)
        console.log('⭐ Prestige effects applied');
    }
    
    // ============================================================
    // RESET
    // ============================================================
    resetAll(): void {
        this.data = {
            totalPoints: 0,
            spentPoints: 0,
            upgrades: {},
            totalRuns: 0,
            bestDay: 0,
            totalWins: 0,
            totalLosses: 0,
        };
        this.saveData();
    }
    
    resetUpgrades(): void {
        this.data.spentPoints = 0;
        this.data.upgrades = {};
        this.saveData();
    }
}