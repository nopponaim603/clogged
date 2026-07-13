// src/data/CrewData.ts
export interface CrewTemplate {
    id: number;
    name: string;
    hp: number;
    speed: number;
    gathering: number;
    searching: number;
    hunting: number;
    perks: string[];
    cost: number;
    description: string;
}

export const CREW_TEMPLATES: CrewTemplate[] = [
    {
        id: 1,
        name: 'Sarah',
        hp: 100,
        speed: 1.2,
        gathering: 1.5,
        searching: 0.8,
        hunting: 0.9,
        perks: ['fast_hands'],
        cost: 25,
        description: 'Fast gatherer, good with resources'
    },
    {
        id: 2,
        name: 'John',
        hp: 120,
        speed: 0.9,
        gathering: 0.7,
        searching: 1.8,
        hunting: 1.2,
        perks: ['night_vision'],
        cost: 30,
        description: 'Expert searcher, finds relics easily'
    },
    {
        id: 3,
        name: 'Emma',
        hp: 90,
        speed: 1.5,
        gathering: 1.2,
        searching: 1.0,
        hunting: 1.5,
        perks: ['gunslinger'],
        cost: 35,
        description: 'Skilled hunter, deadly with guns'
    },
    {
        id: 4,
        name: 'Mike',
        hp: 110,
        speed: 1.0,
        gathering: 2.0,
        searching: 0.6,
        hunting: 0.8,
        perks: ['blacksmith'],
        cost: 28,
        description: 'Master blacksmith, gathers metal efficiently'
    },
    {
        id: 5,
        name: 'Lisa',
        hp: 85,
        speed: 1.8,
        gathering: 0.9,
        searching: 1.6,
        hunting: 0.7,
        perks: ['quick_reflex'],
        cost: 32,
        description: 'Fast and agile, great for scouting'
    }
];