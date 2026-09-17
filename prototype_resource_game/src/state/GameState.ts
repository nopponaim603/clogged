// src/state/GameState.ts
import { EventBus, EVENTS } from './EventBus';
import { GAME_CONFIG } from '../config';
import {
    TimeState,
    BasePosition,
    FuelState,
    EngineState,
    Path,
    Raid,
    FogState,
    GameOverState,
    GameOverReason,
} from './types';

// ✅ Import classes จาก entities
import { Crew } from '../entities/Crew';
import { Vehicle } from '../entities/Vehicle';
import { MapNode } from '../entities/MapNode';

// ✅ GameStateData ใช้ class types
export interface GameStateData {
    runId: string;
    seed: number;
    
    time: TimeState;
    basePosition: BasePosition;
    fuel: FuelState;
    engine: EngineState;
    
    vehicles: Vehicle[];
    crew: Crew[];
    nodes: MapNode[];
    paths: Path[];
    raids: Raid[];
    
    resources: { [key: string]: number };
    credits: number;
    crewPoints: number;
    
    fog: FogState;
    
    gameOver: GameOverState | null;
}

class GameStateClass {
    private static instance: GameStateClass;
    private data: GameStateData;
    
    private constructor() {
        this.data = this.createInitialState();
    }
    
    static getInstance(): GameStateClass {
        if (!GameStateClass.instance) {
            GameStateClass.instance = new GameStateClass();
        }
        return GameStateClass.instance;
    }
    
    private createInitialState(): GameStateData {
        return {
            runId: `run_${Date.now()}`,
            seed: Date.now(),
            
            time: {
                gameHour: GAME_CONFIG.DAY_START_HOUR,
                gameMinute: 0,
                gameDay: 1,
                phase: 'day',
                timeScale: GAME_CONFIG.DEFAULT_TIME_SCALE,
                isPaused: false,
                escalationLevel: 0,
            },
            
            basePosition: {
                x: 0,
                y: 0,
                currentNodeId: null,
                nextNodeId: null,
                progressOnPath: 0,
                direction: { x: 1, y: 0 },
            },
            
            fuel: {
                current: GAME_CONFIG.FUEL_CAPACITY,
                max: GAME_CONFIG.FUEL_CAPACITY,
                isWarning: false,
                hoursRemaining: 0,
                kmRemaining: 0,
            },
            
            engine: {
                condition: GAME_CONFIG.ENGINE_MAX_CONDITION,
                level: 1,
                speedMultiplier: 1.0,
                isWorkerAssigned: false,
            },
            
            vehicles: [],
            crew: [],
            nodes: [],
            paths: [],
            raids: [],
            
            resources: {
                wood: 100,
                stone: 100,
                iron_ore: 100,
                aluminum_ore: 100,
                copper_ore: 100,
                rubber: 100,
                fuel: 0,
                food: GAME_CONFIG.STARTING_FOOD,
                water: 100,
                plank: 0,
                brick: 0,
                iron_bar: 0,
                copper_bar: 0,
                aluminum_sheet: 0,
                rubber_sheet: 0,
                gear: 0,
                circuit: 0,
            },
            credits: GAME_CONFIG.STARTING_CREDITS,
            crewPoints: GAME_CONFIG.STARTING_CREW_POINTS,
            
            fog: {
                revealedNodes: new Set<string>(),
                visibilityRadius: GAME_CONFIG.FOG_VISIBILITY_RADIUS,
            },
            
            gameOver: null,
        };
    }
    
    // ============================================================
    // GETTERS
    // ============================================================
    getData(): GameStateData {
        return this.data;
    }
    
    getTime(): TimeState {
        return this.data.time;
    }
    
    getBasePosition(): BasePosition {
        return this.data.basePosition;
    }
    
    getFuel(): FuelState {
        return this.data.fuel;
    }
    
    getEngine(): EngineState {
        return this.data.engine;
    }
    
    getVehicles(): Vehicle[] {
        return this.data.vehicles;
    }
    
    getCrew(): Crew[] {
        return this.data.crew;
    }
    
    getNodes(): MapNode[] {
        return this.data.nodes;
    }
    
    getPaths(): Path[] {
        return this.data.paths;
    }
    
    getRaids(): Raid[] {
        return this.data.raids;
    }
    
    getResources(): { [key: string]: number } {
        return this.data.resources;
    }
    
    getCredits(): number {
        return this.data.credits;
    }
    
    getCrewPoints(): number {
        return this.data.crewPoints;
    }
    
    getFog(): FogState {
        return this.data.fog;
    }
    
    isPaused(): boolean {
        return this.data.time.isPaused;
    }
    
    isGameOver(): boolean {
        return this.data.gameOver !== null;
    }
    
    // ============================================================
    // SETTERS
    // ============================================================
    setVehicles(vehicles: Vehicle[]): void {
        this.data.vehicles = vehicles;
    }
    
    setCrew(crew: Crew[]): void {
        this.data.crew = crew;
    }
    
    setNodes(nodes: MapNode[]): void {
        this.data.nodes = nodes;
    }
    
    setPaths(paths: Path[]): void {
        this.data.paths = paths;
    }
    
    setRaids(raids: Raid[]): void {
        this.data.raids = raids;
    }
    
    // ============================================================
    // RESOURCE
    // ============================================================
    addResource(type: string, amount: number): void {
        if (this.data.resources[type] === undefined) {
            this.data.resources[type] = 0;
        }
        this.data.resources[type] += amount;
        EventBus.emit(EVENTS.RESOURCE_CHANGED, { type, amount });
    }
    
    removeResource(type: string, amount: number): boolean {
        if ((this.data.resources[type] || 0) < amount) return false;
        this.data.resources[type] -= amount;
        EventBus.emit(EVENTS.RESOURCE_CHANGED, { type, amount: -amount });
        return true;
    }
    
    getResource(type: string): number {
        return this.data.resources[type] || 0;
    }
    
    addCredits(amount: number): void {
        this.data.credits += amount;
        EventBus.emit(EVENTS.CREDITS_CHANGED, this.data.credits);
    }
    
    removeCredits(amount: number): boolean {
        if (this.data.credits < amount) return false;
        this.data.credits -= amount;
        EventBus.emit(EVENTS.CREDITS_CHANGED, this.data.credits);
        return true;
    }
    
    // ============================================================
    // GAME OVER
    // ============================================================
    triggerGameOver(reason: string, isWin: boolean): void {
        const prestige = this.calculatePrestige(isWin);
        this.data.gameOver = {
            reason: reason as GameOverReason,
            day: this.data.time.gameDay,
            hour: this.data.time.gameHour,
            isWin,
            prestigeEarned: prestige,
        };
        EventBus.emit(isWin ? EVENTS.GAME_WIN : EVENTS.GAME_OVER, this.data.gameOver);
    }
    
    private calculatePrestige(isWin: boolean): number {
        let prestige = this.data.time.gameDay * GAME_CONFIG.PRESTIGE_POINTS_PER_DAY;
        if (isWin) {
            const hoursEarly = (GAME_CONFIG.MAX_DAYS - this.data.time.gameDay) * 24
                + (GAME_CONFIG.NIGHT_START_HOUR - this.data.time.gameHour);
            prestige += hoursEarly * GAME_CONFIG.PRESTIGE_BONUS_PER_HOUR_EARLY;
        }
        return Math.max(0, prestige);
    }
    
    // ============================================================
    // RESET
    // ============================================================
    reset(): void {
        this.data = this.createInitialState();
    }
}

export const GameState = GameStateClass.getInstance();