// src/state/EventBus.ts
import * as Phaser from 'phaser';  // ✅ เปลี่ยนจาก default เป็น * as

class EventBusClass extends Phaser.Events.EventEmitter {
    constructor() {
        super();
    }
}

export const EventBus = new EventBusClass();

// ============================================================
// EVENT NAMES
// ============================================================
export const EVENTS = {
    // Time
    TIME_HOUR_TICK: 'time:hourTick',
    TIME_MINUTE_TICK: 'time:minuteTick',
    TIME_PHASE_CHANGE: 'time:phaseChange',
    TIME_NEW_DAY: 'time:newDay',
    TIME_SCALE_CHANGE: 'time:scaleChange',
    TIME_PAUSE: 'time:pause',
    TIME_RESUME: 'time:resume',
    
    // Base
    BASE_MOVED: 'base:moved',
    BASE_REACHED_JUNCTION: 'base:reachedJunction',
    BASE_REACHED_NODE: 'base:reachedNode',
    BASE_REACHED_RAID: 'base:reachedRaid',
    BASE_PATH_SELECTED: 'base:pathSelected',
    
    // Fuel & Engine
    FUEL_CHANGED: 'fuel:changed',
    FUEL_WARNING: 'fuel:warning',
    FUEL_DEPLETED: 'fuel:depleted',
    ENGINE_CONDITION_CHANGED: 'engine:conditionChanged',
    ENGINE_FAILED: 'engine:failed',
    
    // Map
    MAP_NODE_DISCOVERED: 'map:nodeDiscovered',
    MAP_FOG_UPDATED: 'map:fogUpdated',
    
    // Crew
    CREW_DISPATCHED: 'crew:dispatched',
    CREW_RETURNED: 'crew:returned',
    CREW_STRANDED: 'crew:stranded',
    CREW_DIED: 'crew:died',
    CREW_HIRED: 'crew:hired',
    CREW_FIRED: 'crew:fired',
    CREW_ASSIGNED: 'crew:assigned',
    CREW_UNASSIGNED: 'crew:unassigned',
    
    // Mission
    MISSION_STARTED: 'mission:started',
    MISSION_UPDATED: 'mission:updated',
    MISSION_COMPLETED: 'mission:completed',
    MISSION_FAILED: 'mission:failed',
    
    // Raid
    RAID_WARNING: 'raid:warning',
    RAID_TRIGGERED: 'raid:triggered',
    RAID_COMPLETED: 'raid:completed',
    
    // Resource
    RESOURCE_CHANGED: 'resource:changed',
    CREDITS_CHANGED: 'credits:changed',
    
    // Facility
    FACILITY_BUILT: 'facility:built',
    FACILITY_DISMANTLED: 'facility:dismantled',
    FACILITY_UPGRADED: 'facility:upgraded',
    FACILITY_REPAIRED: 'facility:repaired',
    
    // Shop
    SHOP_PURCHASE: 'shop:purchase',
    SHOP_SELL: 'shop:sell',
    
    // Save
    SAVE_SUCCESS: 'save:success',
    SAVE_ERROR: 'save:error',
    LOAD_SUCCESS: 'load:success',
    LOAD_ERROR: 'load:error',
    SAVE_DELETED: 'save:deleted',
    
    // Prestige
    PRESTIGE_POINTS_GAINED: 'prestige:pointsGained',
    PRESTIGE_UPGRADE_PURCHASED: 'prestige:upgradePurchased',
    
    // UI
    UI_JUNCTION_POPUP: 'ui:junctionPopup',
    UI_NOTIFICATION: 'ui:notification',
    
    // Defense
    DEFENSE_COMPLETE: 'defense:complete',
    
    // Game
    GAME_OVER: 'game:over',
    GAME_WIN: 'game:win',
} as const;