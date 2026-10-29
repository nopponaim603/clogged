// src/state/types.ts

// ============================================================
// ⏰ TIME
// ============================================================
export type GamePhase = 'day' | 'night';

export interface TimeState {
    gameHour: number;
    gameMinute: number;
    gameDay: number;
    phase: GamePhase;
    timeScale: number;
    isPaused: boolean;
    escalationLevel: number;
}

// ============================================================
// 🗺️ MAP
// ============================================================
export type NodeType =
    | 'start'
    | 'junction'
    | 'resource'
    | 'relic'
    | 'monster'
    | 'shop'
    | 'encounter'
    | 'finish';

// ✅ เพิ่ม MapNode interface (data-only)
export interface MapNode {
    id: string;
    type: NodeType;
    position: { x: number; y: number };
    connections: string[];
    
    resourceType?: string;
    resourceAmount?: { min: number; max: number };
    rarity?: 'common' | 'uncommon' | 'rare' | 'epic' | 'legendary';
    
    monsterType?: string;
    monsterRank?: number;
    monsterAmount?: { min: number; max: number };
    
    relicType?: string;
    relicSize?: 'small' | 'medium' | 'large' | 'huge';
    threat?: number;
    
    shopType?: 'general' | 'mercenary' | 'trade';
    
    isDiscovered: boolean;
    isVisited: boolean;
}

export interface Path {
    from: string;
    to: string;
    distanceHours: number;
    isActive: boolean;
}

export interface FogState {
    revealedNodes: Set<string>;
    visibilityRadius: number;
}

// ============================================================
// 🚂 BASE
// ============================================================
export interface BasePosition {
    x: number;
    y: number;
    currentNodeId: string | null;
    nextNodeId: string | null;
    progressOnPath: number;
    direction: { x: number; y: number };
}

// ============================================================
// ⛽ FUEL & ENGINE
// ============================================================
export interface FuelState {
    current: number;
    max: number;
    isWarning: boolean;
    hoursRemaining: number;
    kmRemaining: number;
}

export interface EngineState {
    condition: number;
    level: number;
    speedMultiplier: number;
    isWorkerAssigned: boolean;
}

// ============================================================
// 👥 CREW
// ============================================================
export type CrewState = 'idle' | 'dispatched' | 'stranded' | 'assigned' | 'dead';

// ✅ เพิ่ม Crew interface (data-only)
export interface Crew {
    id: number;
    name: string;
    hp: number;
    maxHp: number;
    def: number;
    gearDef: number;
    dodge: number;
    
    gathering: number;
    exploring: number;
    hunting: number;
    
    gearGathering: number;
    gearExploring: number;
    gearHunting: number;
    
    perks: string[];
    
    state: CrewState;
    assignedFacilityId: string | null;
    assignedVehicleId: string | null;
}

// ============================================================
// 🚗 VEHICLE
// ============================================================
export interface Vehicle {
    id: string;
    name: string;
    level: number;
    capacity: number;
    speedKmPerHour: number;
    fuelPerHour: number;
    isDispatched: boolean;
    assignedCrewIds: number[];
    currentMission: MissionState | null;
}

// ============================================================
// 🎯 MISSION
// ============================================================
export type MissionPhase = 'travel_out' | 'action' | 'travel_back' | 'complete' | 'failed';

export interface MissionState {
    vehicleId: string;
    nodeId: string;
    phase: MissionPhase;
    startHour: number;
    startDay: number;
    travelOutTime: number;
    actionTime: number;
    travelBackTime: number;
    elapsedHours: number;
    progress: number;
    success: boolean;
    resourcesGained: { [key: string]: number };
    crewLost: number[];
}

// ============================================================
// 🎁 RAID
// ============================================================
export interface Raid {
    id: string;
    nodeId: string;
    triggerDistance: number;
    durationHours: number;
    enemies: number;
    eliteCount: number;
    hasBoss: boolean;
    isTriggered: boolean;
    isCompleted: boolean;
    reward: number;
}

// ============================================================
// 🏆 GAME OVER
// ============================================================
export type GameOverReason =
    | 'engine_failure'
    | 'fuel_depleted'
    | 'all_crew_dead'
    | 'reached_finish';

export interface GameOverState {
    reason: GameOverReason;
    day: number;
    hour: number;
    isWin: boolean;
    prestigeEarned: number;
}