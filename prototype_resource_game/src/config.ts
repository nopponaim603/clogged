// src/config.ts
export const GAME_CONFIG = {
    WIDTH: 1280,
    HEIGHT: 720,
    DAY_DURATION: 30,
    MAX_DAYS: 30,
    BASE_HP: 100,
    CREW_POINTS: 100,
    STARTING_FOOD: 100,
    
    TRAVEL_SPEED: 50,
    BASE_ACTION_TIME: 7000,
    NIGHT_DURATION: 5000,
    MONSTER_SPAWN_INTERVAL: 1500,
    
    DAY_TIME_LIMIT: 12000,
    TIME_UNIT_PER_SECOND: 100,
    
    // ✅ ค่า Base Travel (ใช้ระยะทางจริง)
    BASE_TRAVEL_TIME: 200,
    TRAVEL_DISTANCE_FACTOR: 2.0,
    
    // ✅ ระบบ HP ของโหนด (Gathering)
    GATHER_HP_PER_UNIT: [500, 1000, 2000, 4000], // 4 ขั้น
    GATHER_RANKS: ['Common', 'Uncommon', 'Rare', 'Epic'],
    
    // ✅ ระบบ HP ของโหนด (Relic)
    RELIC_BASE_HP_RANGE: { min: 5000, max: 20000 },
    RELIC_RARITY_MULTIPLY: [1.0, 2.0, 4.0, 8.0],
    RELIC_RANKS: ['Common', 'Uncommon', 'Rare', 'Epic'],
    
    // ✅ ระบบ HP ของโหนด (Monster)
    MONSTER_BASE_HP_RANGE: { min: 5000, max: 20000 },
    MONSTER_RARITY_MULTIPLY: [1.0, 2.0, 4.0, 8.0],
    MONSTER_RANKS: ['Common', 'Uncommon', 'Rare', 'Epic']
};

export const RESOURCE_ICONS = {
    wood: '🪵',
    stone: '🪨',
    iron: '⛏️',
    food: '🍖',
    water: '💧',
    circuit: '⚡',
    aluminum: '🔩',
};

export const RESOURCE_TYPES = ['wood', 'stone', 'iron', 'food', 'water', 'circuit', 'aluminum'] as const;
export type ResourceType = typeof RESOURCE_TYPES[number];