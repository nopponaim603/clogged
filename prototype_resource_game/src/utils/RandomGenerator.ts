// src/utils/RandomGenerator.ts
import { GAME_CONFIG } from '../config';

export class RandomGenerator {
    private seed: number;
    private static instance: RandomGenerator | null = null;
    
    private constructor(seed: number) {
        this.seed = seed;
    }
    
    static getInstance(seed?: number): RandomGenerator {
        if (!RandomGenerator.instance) {
            RandomGenerator.instance = new RandomGenerator(seed || Date.now());
        }
        return RandomGenerator.instance;
    }
    
    static reset(seed: number): void {
        RandomGenerator.instance = new RandomGenerator(seed);
    }
    
    // ============================================================
    // CORE RANDOM (Mulberry32 Algorithm)
    // ============================================================
    private next(): number {
        let t = this.seed += 0x6D2B79F5;
        t = Math.imul(t ^ t >>> 15, t | 1);
        t ^= t + Math.imul(t ^ t >>> 7, t | 61);
        return ((t ^ t >>> 14) >>> 0) / 4294967296;
    }
    
    // ============================================================
    // BASIC METHODS
    // ============================================================
    random(): number {
        return this.next();
    }
    
    randomRange(min: number, max: number): number {
        return min + this.next() * (max - min);
    }
    
    randomInt(min: number, max: number): number {
        return Math.floor(this.randomRange(min, max + 1));
    }
    
    // ============================================================
    // COLLECTION METHODS
    // ============================================================
    pick<T>(array: T[]): T {
        return array[Math.floor(this.next() * array.length)];
    }
    
    shuffle<T>(array: T[]): T[] {
        const result = [...array];
        for (let i = result.length - 1; i > 0; i--) {
            const j = Math.floor(this.next() * (i + 1));
            [result[i], result[j]] = [result[j], result[i]];
        }
        return result;
    }
    
    weightedPick<T>(items: T[], weights: number[]): T {
        const totalWeight = weights.reduce((a, b) => a + b, 0);
        let random = this.next() * totalWeight;
        
        for (let i = 0; i < items.length; i++) {
            random -= weights[i];
            if (random <= 0) return items[i];
        }
        
        return items[items.length - 1];
    }
    
    // ============================================================
    // GAME-SPECIFIC METHODS
    // ============================================================
    
    // ✅ สุ่มตำแหน่งบนแผนที่
    randomPosition(mapSize: number, padding: number = 100): { x: number; y: number } {
        return {
            x: this.randomRange(padding, mapSize - padding),
            y: this.randomRange(padding, mapSize - padding),
        };
    }
    
    // ✅ สุ่มระยะห่างระหว่าง node (เป็นชั่วโมง)
    randomPathDistance(): number {
        return this.randomRange(
            GAME_CONFIG.PATH_DISTANCE_MIN_HOURS,
            GAME_CONFIG.PATH_DISTANCE_MAX_HOURS
        );
    }
    
    // ✅ สุ่ม Rarity
    randomRarity(): 'common' | 'uncommon' | 'rare' | 'epic' | 'legendary' {
        const rarities: Array<'common' | 'uncommon' | 'rare' | 'epic' | 'legendary'> = 
            ['common', 'uncommon', 'rare', 'epic', 'legendary'];
        const weights = [50, 25, 15, 8, 2];  // % chance
        
        return this.weightedPick(rarities, weights);
    }
    
    // ✅ สุ่ม Resource Type
    randomResourceType(): string {
        const types = ['wood', 'stone', 'iron_ore', 'aluminum_ore', 'copper_ore', 'rubber', 'fuel'];
        const weights = [25, 20, 15, 12, 10, 12, 6];
        
        return this.weightedPick(types, weights);
    }
    
    // ✅ สุ่ม Monster Type
    randomMonsterType(): string {
        const types = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'];
        const weights = [30, 25, 20, 12, 7, 4, 1.5, 0.5];
        
        return this.weightedPick(types, weights);
    }
    
    // ✅ สุ่ม Crew HP ตามช่วง
    randomCrewHp(): number {
        const roll = this.next();
        
        if (roll <= 0.05) {
            // 5% - ต่ำ
            return this.randomRange(GAME_CONFIG.CREW_HP_MIN, GAME_CONFIG.CREW_HP_TYPICAL_MIN);
        } else if (roll <= 0.85) {
            // 80% - ปกติ
            return this.randomRange(GAME_CONFIG.CREW_HP_TYPICAL_MIN, GAME_CONFIG.CREW_HP_TYPICAL_MAX);
        } else {
            // 15% - หายาก
            return this.randomRange(GAME_CONFIG.CREW_HP_RARE_MIN, GAME_CONFIG.CREW_HP_RARE_MAX);
        }
    }
    
    // ✅ สุ่ม Crew Stat (0-200)
    randomCrewStat(): number {
        const roll = this.next();
        
        if (roll <= 0.15) {
            // 15% - ต่ำ (0-49)
            return this.randomInt(0, GAME_CONFIG.CREW_STAT_LOW_MAX);
        } else if (roll <= 0.95) {
            // 80% - ปกติ (50-100)
            return this.randomInt(GAME_CONFIG.CREW_STAT_TYPICAL_MIN, GAME_CONFIG.CREW_STAT_TYPICAL_MAX);
        } else {
            // 5% - สูง (101-200)
            return this.randomInt(GAME_CONFIG.CREW_STAT_HIGH_MIN, GAME_CONFIG.CREW_STAT_HIGH_MAX);
        }
    }
    
    // ✅ สุ่ม Crew DEF
    randomCrewDef(): number {
        const roll = this.next();
        
        if (roll <= 0.15) {
            return this.randomInt(0, GAME_CONFIG.CREW_DEF_LOW_MAX);
        } else if (roll <= 0.95) {
            return this.randomInt(GAME_CONFIG.CREW_DEF_TYPICAL_MIN, GAME_CONFIG.CREW_DEF_TYPICAL_MAX);
        } else {
            return this.randomInt(GAME_CONFIG.CREW_DEF_HIGH_MIN, GAME_CONFIG.CREW_DEF_HIGH_MAX);
        }
    }
    
    // ✅ สุ่ม Crew Dodge (0-100)
    randomCrewDodge(): number {
        const roll = this.next();
        
        if (roll <= 0.90) {
            return this.randomInt(0, GAME_CONFIG.CREW_DODGE_TYPICAL_MAX);
        } else if (roll <= 0.99) {
            return this.randomInt(GAME_CONFIG.CREW_DODGE_TYPICAL_MAX, GAME_CONFIG.CREW_DODGE_MEDIUM_MAX);
        } else {
            return this.randomInt(GAME_CONFIG.CREW_DODGE_MEDIUM_MAX, GAME_CONFIG.CREW_DODGE_HIGH_MAX);
        }
    }
    
    // ✅ สุ่ม Gear Gathering
    randomGearGathering(): number {
        const roll = this.next();
        
        if (roll <= 0.60) {
            return this.randomInt(GAME_CONFIG.GEAR_GATHERING_MIN, GAME_CONFIG.GEAR_GATHERING_MAX);
        } else if (roll <= 0.90) {
            return 0;
        } else {
            return this.randomInt(GAME_CONFIG.GEAR_GATHERING_RARE_MIN, GAME_CONFIG.GEAR_GATHERING_RARE_MAX);
        }
    }
    
    // ✅ สุ่ม Gear Exploring
    randomGearExploring(): number {
        const roll = this.next();
        
        if (roll <= 0.60) {
            return this.randomInt(GAME_CONFIG.GEAR_EXPLORING_MIN, GAME_CONFIG.GEAR_EXPLORING_MAX);
        } else if (roll <= 0.90) {
            return 0;
        } else {
            return this.randomInt(GAME_CONFIG.GEAR_EXPLORING_RARE_MIN, GAME_CONFIG.GEAR_EXPLORING_RARE_MAX);
        }
    }
    
    // ✅ สุ่ม Gear Hunting
    randomGearHunting(): number {
        const roll = this.next();
        
        if (roll <= 0.80) {
            return this.randomInt(GAME_CONFIG.GEAR_HUNTING_MIN, GAME_CONFIG.GEAR_HUNTING_MAX);
        } else {
            return this.randomInt(GAME_CONFIG.GEAR_HUNTING_RARE_MIN, GAME_CONFIG.GEAR_HUNTING_RARE_MAX);
        }
    }
    
    // ✅ สุ่ม Gear DEF
    randomGearDef(): number {
        const roll = this.next();
        
        if (roll <= 0.15) {
            return this.randomInt(0, GAME_CONFIG.GEAR_DEF_LOW_MAX);
        } else if (roll <= 0.95) {
            return this.randomInt(GAME_CONFIG.GEAR_DEF_TYPICAL_MIN, GAME_CONFIG.GEAR_DEF_TYPICAL_MAX);
        } else {
            return this.randomInt(GAME_CONFIG.GEAR_DEF_HIGH_MIN, GAME_CONFIG.GEAR_DEF_HIGH_MAX);
        }
    }
}