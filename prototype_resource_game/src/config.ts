// src/config.ts
export const GAME_CONFIG = {
    // ============================================================
    // 🖥️ DISPLAY
    // ============================================================
    GAME_NAME: 'CLOGGED',
    GAME_TAGLINE: 'Survive until the end of the world!',
    WIDTH: 1280,
    HEIGHT: 720,
    
    // ============================================================
    // 🎨 COLORS
    // ============================================================
    COLORS: {
        BG_DARK: 0x1a2332,
        BG_MEDIUM: 0x243040,
        BG_LIGHT: 0x2f3d4f,
        BG_OVERLAY: 0x0a0f18,
        
        PATH_INACTIVE: 0x5a6a7a,
        PATH_ACTIVE: 0x00e5cc,
        
        NODE_START: 0x00ff9d,
        NODE_FINISH: 0xff4757,
        NODE_JUNCTION: 0x95a5a6,
        NODE_RESOURCE: 0x00e5cc,
        NODE_RELIC: 0xa29bfe,
        NODE_MONSTER: 0xff6b6b,
        NODE_SHOP: 0xffd93d,
        NODE_ENCOUNTER: 0xff9f43,
        
        TEXT_PRIMARY: 0xffffff,
        TEXT_SECONDARY: 0xb0c4de,
        TEXT_MUTED: 0x8a9aab,
        
        SUCCESS: 0x00e5cc,
        WARNING: 0xffd93d,
        DANGER: 0xff4757,
        INFO: 0x4a9eff,
    },
    
    // ============================================================
    // ⏰ TIME
    // ============================================================
    REAL_SECONDS_PER_GAME_HOUR: 60,
    TIME_SCALES: [1, 2, 4],
    DEFAULT_TIME_SCALE: 1,
    DAY_START_HOUR: 6,
    NIGHT_START_HOUR: 18,
    
    // ============================================================
    // 📅 DAYS
    // ============================================================
    MAX_DAYS: 15,
    ESCALATION_START_DAY: 15,
    ESCALATION_MULTIPLIER: 1.5,
    
    // ============================================================
    // 🚂 BASE
    // ============================================================
    BASE_SPEED_KM_PER_HOUR: 100,
    BASE_RAID_SPEED: 0.5,
    BASE_ENGINE_SPEED_BONUS_PER_LEVEL: 10,
    BASE_ENGINE_LOW_THRESHOLD: 20,
    
    // ============================================================
    // 🚗 VEHICLE
    // ============================================================
    STARTING_VEHICLES: 3,
    VEHICLE_CAPACITY: 5,
    VEHICLE_SPEED_KM_PER_HOUR: 200,
    VEHICLE_FUEL_PER_HOUR: 5,
    VEHICLE_UPGRADE_COST_WOOD: 50,
    VEHICLE_UPGRADE_COST_IRON: 30,
    VEHICLE_UPGRADE_COST_CREDITS: 200,
    VEHICLE_MAX_UPGRADE_LEVEL: 3,
    VEHICLE_SPEED_BONUS_PER_LEVEL: 15,
    VEHICLE_FUEL_EFFICIENCY_PER_LEVEL: 0.1,
    
    // ============================================================
    // 👥 CREW
    // ============================================================
    STARTING_CREW: 6,
    BASE_CREW_SLOTS: 6,
    MAX_CREW_SLOTS: 12,
    CREW_SLOT_UPGRADE_COST: { level2: 200, level3: 400, level4: 800, level5: 1500, level6: 2500 },
    
    CREW_HP_MIN: 8000,
    CREW_HP_TYPICAL_MIN: 10000,
    CREW_HP_TYPICAL_MAX: 20000,
    CREW_HP_RARE_MIN: 20000,
    CREW_HP_RARE_MAX: 25000,
    
    CREW_STAT_TYPICAL_MIN: 50,
    CREW_STAT_TYPICAL_MAX: 100,
    CREW_STAT_LOW_MAX: 49,
    CREW_STAT_HIGH_MIN: 101,
    CREW_STAT_HIGH_MAX: 200,
    
    CREW_DEF_TYPICAL_MIN: 50,
    CREW_DEF_TYPICAL_MAX: 100,
    CREW_DEF_LOW_MAX: 49,
    CREW_DEF_HIGH_MIN: 101,
    CREW_DEF_HIGH_MAX: 200,
    
    CREW_DODGE_TYPICAL_MAX: 30,
    CREW_DODGE_MEDIUM_MAX: 60,
    CREW_DODGE_HIGH_MAX: 100,
    
    GEAR_GATHERING_MIN: 5,
    GEAR_GATHERING_MAX: 10,
    GEAR_GATHERING_RARE_MIN: 11,
    GEAR_GATHERING_RARE_MAX: 20,
    GEAR_EXPLORING_MIN: 5,
    GEAR_EXPLORING_MAX: 10,
    GEAR_EXPLORING_RARE_MIN: 11,
    GEAR_EXPLORING_RARE_MAX: 20,
    GEAR_HUNTING_MIN: 50,
    GEAR_HUNTING_MAX: 100,
    GEAR_HUNTING_RARE_MIN: 101,
    GEAR_HUNTING_RARE_MAX: 200,
    GEAR_DEF_TYPICAL_MIN: 50,
    GEAR_DEF_TYPICAL_MAX: 100,
    GEAR_DEF_LOW_MAX: 49,
    GEAR_DEF_HIGH_MIN: 101,
    GEAR_DEF_HIGH_MAX: 200,
    
    FOOD_CONSUMPTION_PER_CREW_PER_DAY: 2,
    STARTING_FOOD: 100,
    CREW_DEATH_FROM_STARVATION_HOURS: 24,
    
    // ============================================================
    // 💰 ECONOMY
    // ============================================================
    STARTING_CREDITS: 500,
    STARTING_CREW_POINTS: 150,
    CREW_COST_MIN: 15,
    CREW_COST_MAX: 40,
    
    RAID_REWARD_FULL: 1.0,
    RAID_REWARD_PARTIAL: 0.5,
    RAID_REWARD_FAILED: 0.5,
    RAID_RESOURCE_LOSS_PERCENT: 0.5,
    
    // ============================================================
    // 🎬 RECRUITMENT
    // ============================================================
    RECRUIT_POOL_MIN: 10,
    RECRUIT_POOL_MAX: 15,
    RECRUIT_MIN_HIRE: 6,
    
    MERCENARY_SHOP: {
        crewCountMin: 2,
        crewCountMax: 4,
        crewCostMin: 30,
        crewCostMax: 80,
        statBonus: 20,
    },
    
    // ============================================================
    // ⛽ FUEL
    // ============================================================
    FUEL_CAPACITY: 500,
    FUEL_CAPACITY_UPGRADE_PER_LEVEL: 100,
    FUEL_MAX_UPGRADE_LEVEL: 5,
    FUEL_CONSUMPTION_PER_HOUR: 50,
    FUEL_CONSUMPTION_PER_100KM: 50,
    FUEL_GRACE_PERIOD_HOURS: 6,
    FUEL_WARNING_DISTANCE: 1000,
    FUEL_WARNING_TIME: 6,
    
    // ============================================================
    // 🔧 ENGINE
    // ============================================================
    ENGINE_MAX_CONDITION: 100,
    ENGINE_DECAY_NO_WORKER: [5, 4, 3, 2, 1],
    ENGINE_DECAY_WITH_WORKER: [1, 0.8, 0.6, 0.4, 0.2],
    ENGINE_MAX_LEVEL: 5,
    MECHANIST_REDUCTION: { tier1: 0.1, tier2: 0.3, tier3: 0.5 },
    ENGINE_REPAIR_HOURS_PER_PERCENT: 1,
    ENGINE_REPAIR_COST_IRON_PER_PERCENT: 1,
    ENGINE_RAID_DAMAGE_MIN: 5,
    ENGINE_RAID_DAMAGE_MAX: 15,
    
    // ============================================================
    // 🗺️ MAP
    // ============================================================
    MAP_SIZE: 40000,
    MAP_CELL_SIZE: 100,
    NODE_MIN_DISTANCE: 2000,
    NODE_COUNT_MIN: 50,
    NODE_COUNT_MAX: 200,
    PATH_DISTANCE_MIN_HOURS: 4,
    PATH_DISTANCE_MAX_HOURS: 15,
    NODE_DISTRIBUTION: {
        resource: 60,
        monster: 25,
        relic: 15,
    },
    
    // ============================================================
    // 👁️ FOG
    // ============================================================
    FOG_VISIBILITY_RADIUS: 800,
    FOG_FORGET_ENABLED: true,
    
    // ============================================================
    // 🔀 JUNCTION
    // ============================================================
    JUNCTION_AUTO_SELECT_SECONDS: 5,
    
    // ============================================================
    // 📊 RESOURCE
    // ============================================================
    RESOURCE_TYPE_VALUE: {
        wood: 100, stone: 150, iron_ore: 250,
        aluminum_ore: 300, copper_ore: 350, rubber: 200, fuel: 400,
    },
    RESOURCE_TYPE_RANGE: {
        wood: { min: 25, max: 30 },
        stone: { min: 20, max: 25 },
        iron_ore: { min: 15, max: 20 },
        aluminum_ore: { min: 12, max: 18 },
        copper_ore: { min: 10, max: 15 },
        rubber: { min: 20, max: 25 },
        fuel: { min: 30, max: 50 },
    },
    RESOURCE_NODE_RARITY: {
        common: 1.0, uncommon: 1.5, rare: 2.0, epic: 4.0, legendary: 8.0,
    },
    
    EFFICIENCY_MAX_PER_CREW: 350,
    PROFICIENCY_YIELD_MULTIPLIER: 0.01,
    PROFICIENCY_COMBAT_MULTIPLIER: 0.01,
    PROFICIENCY_FACILITY_MULTIPLIER: 0.005,
    
    // ============================================================
    // 🏛️ RELIC
    // ============================================================
    RELIC_RARITY_VALUE: {
        common: 1000, uncommon: 2000, rare: 5000, epic: 10000, legendary: 20000,
    },
    RELIC_SIZE_MULTIPLIER: { small: 1.0, medium: 2.0, large: 4.0, huge: 8.0 },
    RELIC_THREAT_BASE_MIN: 0,
    RELIC_THREAT_BASE_MAX: 10,
    RELIC_THREAT_EFFICIENCY_DIVISOR: 350,
    RELIC_THREAT_EFFICIENCY_REDUCTION: 0.5,
    RELIC_CASUALTY_THRESHOLD: 5.0,
    RELIC_CASUALTY_MIN_PERCENT: 1,
    RELIC_CASUALTY_MAX_PERCENT: 50,
    
    // ============================================================
    // 👹 MONSTER
    // ============================================================
    MONSTER_TYPES: {
        A: { rank: 1, hp: 1500, atk: 10 },
        B: { rank: 1, hp: 2000, atk: 15 },
        C: { rank: 2, hp: 5000, atk: 40 },
        D: { rank: 2, hp: 7500, atk: 60 },
        E: { rank: 3, hp: 15000, atk: 120 },
        F: { rank: 3, hp: 25000, atk: 200 },
        G: { rank: 4, hp: 50000, atk: 350 },
        H: { rank: 5, hp: 100000, atk: 500 },
    },
    MONSTER_AMOUNT_BY_RANK: {
        1: { min: 20, max: 35 },
        2: { min: 15, max: 20 },
        3: { min: 8, max: 14 },
        4: { min: 4, max: 7 },
        5: { min: 1, max: 3 },
    },
    
    // ============================================================
    // ⚔️ COMBAT
    // ============================================================
    COMBAT_TICK_INTERVAL_MINUTES: 1,
    COMBAT_DODGE_BASE_CHANCE: 0.1,
    COMBAT_CRIT_CHANCE: 0.1,
    
    // ============================================================
    // 🏠 BASE INTERIOR
    // ============================================================
    BASE_GRID_WIDTH: 20,
    BASE_GRID_HEIGHT: 12,
    BASE_TOTAL_FLOORS: 4,
    
    // ============================================================
    // 🏭 FACILITY
    // ============================================================
    FACILITY_TYPES: {
        engine: {
            size: { w: 6, h: 3 },
            floors: 1,
            maxLevel: 5,
            buildCost: { wood: 200, iron_bar: 100, credits: 300 },
            upgradeCost: {
                level2: { wood: 100, iron_bar: 50, credits: 200 },
                level3: { wood: 200, iron_bar: 100, credits: 400 },
                level4: { wood: 400, iron_bar: 200, credits: 800 },
                level5: { wood: 800, iron_bar: 400, credits: 1500 },
            },
        },
        kitchen: {
            size: { w: 3, h: 3 },
            floors: 1,
            maxLevel: 5,
            buildCost: { wood: 100, stone: 50, credits: 150 },
            upgradeCost: {
                level2: { wood: 50, stone: 25, credits: 100 },
                level3: { wood: 100, stone: 50, credits: 200 },
                level4: { wood: 200, stone: 100, credits: 400 },
                level5: { wood: 400, stone: 200, credits: 800 },
            },
            production: { baseTimePerFood: 3, timeReductionPerLevel: 0.5 },
        },
        factory: {
            size: { w: 6, h: 3 },
            floors: 1,
            maxLevel: 5,
            buildCost: { wood: 300, iron_bar: 200, credits: 500 },
            upgradeCost: {
                level2: { wood: 150, iron_bar: 100, credits: 300 },
                level3: { wood: 300, iron_bar: 200, credits: 600 },
                level4: { wood: 600, iron_bar: 400, credits: 1200 },
                level5: { wood: 1200, iron_bar: 800, credits: 2500 },
            },
        },
        workshop: {
            size: { w: 3, h: 3 },
            floors: 1,
            maxLevel: 5,
            buildCost: { wood: 150, iron_bar: 100, credits: 250 },
            upgradeCost: {
                level2: { wood: 75, iron_bar: 50, credits: 150 },
                level3: { wood: 150, iron_bar: 100, credits: 300 },
                level4: { wood: 300, iron_bar: 200, credits: 600 },
                level5: { wood: 600, iron_bar: 400, credits: 1200 },
            },
        },
        turret: {
            size: { w: 3, h: 3 },
            floors: 1,
            maxLevel: 3,
            buildCost: { wood: 100, iron_bar: 100, credits: 200 },
            upgradeCost: {
                level2: { wood: 50, iron_bar: 50, credits: 100 },
                level3: { wood: 100, iron_bar: 100, credits: 200 },
            },
        },
        storage: {
            size: { w: 3, h: 3 },
            floors: 1,
            maxLevel: 5,
            buildCost: { wood: 100, stone: 100, credits: 100 },
            upgradeCost: {
                level2: { wood: 50, stone: 50, credits: 50 },
                level3: { wood: 100, stone: 100, credits: 100 },
                level4: { wood: 200, stone: 200, credits: 200 },
                level5: { wood: 400, stone: 400, credits: 400 },
            },
        },
    },
    
    // ============================================================
    // 🗼 TURRET
    // ============================================================
    TURRET_TYPES: {
        gun: { damage: 15, range: 160, fireRate: 0.4, elevationRate: 90, buildCost: { wood: 25, iron_bar: 10 } },
        cannon: { damage: 35, range: 220, fireRate: 0.9, elevationRate: 30, buildCost: { wood: 30, iron_bar: 20, stone: 15 } },
        laser: { damage: 20, range: 190, fireRate: 0.6, elevationRate: 180, buildCost: { wood: 20, circuit: 15, aluminum_sheet: 10 } },
        shield: { damage: 0, range: 100, fireRate: 0, elevationRate: 0, buildCost: { stone: 25, iron_bar: 15, wood: 10 }, shieldHP: 500 },
    },
    
    // ============================================================
    // 🎁 RAID
    // ============================================================
    RAID_MIN_PER_NIGHT: 1,
    RAID_MAX_PER_NIGHT: 3,
    RAID_CHANCE_2: 0.2,
    RAID_CHANCE_3: 0.05,
    RAID_DURATION_MIN: 3,
    RAID_DURATION_MAX: 5,
    RAID_TRIGGER_DISTANCE: 0,
    RAID_ENEMY_COUNT_BASE: 10,
    RAID_ENEMY_COUNT_PER_DAY: 3,
    RAID_BOSS_DAY_INTERVAL: 5,
    
    // ============================================================
    // 💱 TRADE
    // ============================================================
    TRADE_PRICE_MULTIPLIER: 1.0,
    SHOP_PRICE_MULTIPLIER: 1.5,
    
    // ============================================================
    // 🏆 PRESTIGE
    // ============================================================
    PRESTIGE_POINTS_PER_DAY: 10,
    PRESTIGE_BONUS_PER_HOUR_EARLY: 1,
};

export const RESOURCE_ICONS = {
    wood: '🪵', stone: '🪨', iron_ore: '⛏️',
    aluminum_ore: '🔩', copper_ore: '🟤', rubber: '⚫',
    fuel: '⛽', food: '🍖', water: '💧',
    plank: '🪚', brick: '🧱', iron_bar: '🔨',
    copper_bar: '🟠', aluminum_sheet: '📄', rubber_sheet: '⬛',
    gear: '⚙️', circuit: '⚡', credits: '💰',
};

export const RESOURCE_TYPES = [
    'wood', 'stone', 'iron_ore', 'aluminum_ore', 'copper_ore', 'rubber', 'fuel',
    'food', 'water',
    'plank', 'brick', 'iron_bar', 'copper_bar', 'aluminum_sheet', 'rubber_sheet', 'gear', 'circuit',
    'credits',
] as const;

export type ResourceType = typeof RESOURCE_TYPES[number];

export const PERKS = [
    'mechanist_i', 'mechanist_ii', 'mechanist_iii',
    'cook_i', 'cook_ii', 'cook_iii',
    'engineer_i', 'engineer_ii', 'engineer_iii',
    'blacksmith_i', 'blacksmith_ii', 'blacksmith_iii',
    'gunslinger_i', 'gunslinger_ii', 'gunslinger_iii',
    'scout_i', 'scout_ii', 'scout_iii',
    'soldier_i', 'soldier_ii', 'soldier_iii',
] as const;

export type PerkType = typeof PERKS[number];

export const NODE_TYPES = [
    'start', 'junction', 'resource', 'relic', 'monster', 'shop', 'encounter', 'finish',
] as const;

export type NodeType = typeof NODE_TYPES[number];

export const GAME_OVER_REASONS = {
    ENGINE_FAILURE: 'engine_failure',
    FUEL_DEPLETED: 'fuel_depleted',
    ALL_CREW_DEAD: 'all_crew_dead',
    REACHED_FINISH: 'reached_finish',
} as const;