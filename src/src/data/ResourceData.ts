// src/data/ResourceData.ts
export interface ResourceTypeData {
    id: string;
    name: string;
    icon: string;
    color: string;
    baseGatherTime: number;
}

export const RESOURCE_DATA: { [key: string]: ResourceTypeData } = {
    wood: {
        id: 'wood',
        name: 'Wood',
        icon: '🪵',
        color: '#8B6914',
        baseGatherTime: 3000
    },
    stone: {
        id: 'stone',
        name: 'Stone',
        icon: '🪨',
        color: '#808080',
        baseGatherTime: 3500
    },
    iron: {
        id: 'iron',
        name: 'Iron',
        icon: '⛏️',
        color: '#A0A0A0',
        baseGatherTime: 4000
    },
    food: {
        id: 'food',
        name: 'Food',
        icon: '🍖',
        color: '#FF6B35',
        baseGatherTime: 2500
    },
    water: {
        id: 'water',
        name: 'Water',
        icon: '💧',
        color: '#4A90D9',
        baseGatherTime: 2000
    },
    circuit: {
        id: 'circuit',
        name: 'Circuit',
        icon: '⚡',
        color: '#FFD700',
        baseGatherTime: 5000
    },
    aluminum: {
        id: 'aluminum',
        name: 'Aluminum',
        icon: '🔩',
        color: '#C0C0C0',
        baseGatherTime: 4500
    }
};

export const RELIC_DATA = {
    name: 'Ancient Relic',
    icon: '🏛️',
    color: '#6C5CE7',
    baseSearchTime: 6000
};

export const MONSTER_DATA = {
    icon: '👹',
    color: '#E74C3C',
    baseHuntTime: 8000
};