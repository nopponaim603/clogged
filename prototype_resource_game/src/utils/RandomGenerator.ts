// src/utils/RandomGenerator.ts
import { Helpers } from './Helpers';

export class RandomGenerator {
    static generateCrewName(): string {
        const names = ['Sarah', 'John', 'Emma', 'Mike', 'Lisa', 'David', 'Anna', 'Tom', 'Katie', 'James', 'Alex', 'Mia'];
        return Helpers.pickRandom(names);
    }

    static generatePerk(): string {
        const perks = ['fast_hands', 'night_vision', 'gunslinger', 'blacksmith', 'quick_reflex', 'tough'];
        return Helpers.pickRandom(perks);
    }

    static generateMonsterName(): string {
        const names = ['Goblin', 'Wolf', 'Giant Spider', 'Shadow Demon', 'Night Stalker', 'Mutant', 'Crawler', 'Wraith'];
        return Helpers.pickRandom(names);
    }

    static generateResourceType(): string {
        const types = ['wood', 'stone', 'iron', 'food', 'water', 'circuit', 'aluminum'];
        return Helpers.pickRandom(types);
    }

    static generateResourceAmount(): number {
        return Helpers.randomInt(20, 100);
    }

    static generateCrewStats(): {
        speed: number;
        gathering: number;
        searching: number;
        hunting: number;
    } {
        return {
            speed: Math.round((0.8 + Math.random() * 0.6) * 10) / 10,
            gathering: Math.round((0.7 + Math.random() * 0.8) * 10) / 10,
            searching: Math.round((0.7 + Math.random() * 0.8) * 10) / 10,
            hunting: Math.round((0.7 + Math.random() * 0.8) * 10) / 10
        };
    }

    static generatePosition(maxX: number, maxY: number, padding: number = 50): { x: number; y: number } {
        return {
            x: padding + Math.random() * (maxX - padding * 2),
            y: padding + Math.random() * (maxY - padding * 2)
        };
    }
}