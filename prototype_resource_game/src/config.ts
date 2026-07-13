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
    
    // ✅ ปรับ Base Time ให้ใหญ่ขึ้น
    BASE_TRAVEL_TIME: 500,      // จาก 200 เป็น 500
    BASE_GATHER_TIME: 3000,     // จาก 1500 เป็น 3000
    BASE_SEARCH_TIME: 4500,     // จาก 2500 เป็น 4500
    BASE_HUNT_TIME: 5000,       // จาก 3000 เป็น 5000
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