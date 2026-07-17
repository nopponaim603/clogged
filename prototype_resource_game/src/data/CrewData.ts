// src/data/CrewData.ts
export interface CrewTemplate {
    id: number;
    name: string;
    maxHp: number;
    baseSpeed: number;      // 0-100
    baseGathering: number;  // 0-100
    baseSearching: number;  // 0-100
    baseHunting: number;    // 0-100
    perks: string[];
    cost: number;
    description: string;
}

export const CREW_TEMPLATES: CrewTemplate[] = [
    {
        id: 1,
        name: 'Sarah',
        maxHp: 100,
        baseSpeed: 52,
        baseGathering: 64,
        baseSearching: 42,
        baseHunting: 88,
        perks: ['fast_hands'],
        cost: 25,
        description: 'Fast gatherer with decent hunting skills'
    },
    {
        id: 2,
        name: 'John',
        maxHp: 120,
        baseSpeed: 35,
        baseGathering: 30,
        baseSearching: 92,
        baseHunting: 55,
        perks: ['night_vision'],
        cost: 30,
        description: 'Expert searcher, finds relics easily'
    },
    {
        id: 3,
        name: 'Emma',
        maxHp: 90,
        baseSpeed: 75,
        baseGathering: 48,
        baseSearching: 40,
        baseHunting: 95,
        perks: ['gunslinger'],
        cost: 35,
        description: 'Skilled hunter, deadly with guns'
    },
    {
        id: 4,
        name: 'Mike',
        maxHp: 110,
        baseSpeed: 45,
        baseGathering: 98,
        baseSearching: 25,
        baseHunting: 35,
        perks: ['blacksmith'],
        cost: 28,
        description: 'Master blacksmith, gathers metal efficiently'
    },
    {
        id: 5,
        name: 'Lisa',
        maxHp: 85,
        baseSpeed: 95,
        baseGathering: 38,
        baseSearching: 82,
        baseHunting: 30,
        perks: ['quick_reflex', 'scout'],
        cost: 32,
        description: 'Fast and agile, great for scouting'
    },
    {
        id: 6,
        name: 'Tom',
        maxHp: 140,
        baseSpeed: 28,
        baseGathering: 35,
        baseSearching: 25,
        baseHunting: 98,
        perks: ['tough', 'strong'],
        cost: 34,
        description: 'Tanky hunter with high damage'
    },
    {
        id: 7,
        name: 'Anna',
        maxHp: 95,
        baseSpeed: 68,
        baseGathering: 72,
        baseSearching: 55,
        baseHunting: 60,
        perks: ['lucky', 'medic'],
        cost: 38,
        description: 'Balanced all-rounder with healing'
    },
    {
        id: 8,
        name: 'Leo',
        maxHp: 105,
        baseSpeed: 88,
        baseGathering: 45,
        baseSearching: 78,
        baseHunting: 45,
        perks: ['sprinter', 'night_vision'],
        cost: 36,
        description: 'Fast searcher with good speed'
    },
];