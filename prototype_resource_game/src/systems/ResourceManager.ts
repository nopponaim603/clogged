// src/systems/ResourceManager.ts
import { GAME_CONFIG } from '../config';

interface ResourceInventory {
    wood: number;
    stone: number;
    iron: number;
    food: number;
    water: number;
    circuit: number;
    aluminum: number;
    monsterParts: {
        fangs: number;
        hides: number;
        claws: number;
    };
}

export class ResourceManager {
    public resources: ResourceInventory;
    public dayResources: Partial<ResourceInventory>;
    public baseHP: number;
    public maxBaseHP: number;

    constructor() {
        this.resources = {
            wood: 0,
            stone: 0,
            iron: 0,
            food: GAME_CONFIG.STARTING_FOOD,
            water: 0,
            circuit: 0,
            aluminum: 0,
            monsterParts: {
                fangs: 0,
                hides: 0,
                claws: 0
            }
        };
        this.dayResources = {};
        this.baseHP = GAME_CONFIG.BASE_HP;
        this.maxBaseHP = GAME_CONFIG.BASE_HP;
    }

    addResource(type: string, amount: number): void {
        if (type === 'monsterParts' || type === 'monsterpart') return;
        
        const key = type as keyof Omit<ResourceInventory, 'monsterParts'>;
        if (key in this.resources) {
            this.resources[key] += amount;
            this.dayResources[key] = (this.dayResources[key] || 0) + amount;
        }
    }

    addMonsterPart(type: 'fangs' | 'hides' | 'claws', amount: number): void {
        this.resources.monsterParts[type] += amount;
    }

    getResource(type: string): number {
        const key = type as keyof Omit<ResourceInventory, 'monsterParts'>;
        if (key in this.resources) {
            return this.resources[key] || 0;
        }
        return 0;
    }

    loseDayResources(): Record<string, number> {
        const lost: Record<string, number> = {};
        for (const [type, amount] of Object.entries(this.dayResources)) {
            if (typeof amount === 'number' && amount > 0) {
                const loss = Math.floor(amount / 2);
                const key = type as keyof Omit<ResourceInventory, 'monsterParts'>;
                if (key in this.resources) {
                    this.resources[key] -= loss;
                    lost[type] = loss;
                }
            }
        }
        this.dayResources = {};
        return lost;
    }

    consumeFood(crewCount: number): { success: boolean; needed: number; shortage: number } {
        const needed = crewCount * 2;
        if (this.resources.food >= needed) {
            this.resources.food -= needed;
            return { success: true, needed, shortage: 0 };
        } else {
            const shortage = needed - this.resources.food;
            this.resources.food = 0;
            return { success: false, needed, shortage };
        }
    }

    takeBaseDamage(damage: number): boolean {
        this.baseHP -= damage;
        return this.baseHP <= 0;
    }

    resetDay(): void {
        this.dayResources = {};
    }
}