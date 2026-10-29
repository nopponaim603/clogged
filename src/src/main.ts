// src/main.ts
import * as Phaser from 'phaser';
import { BootScene } from './scenes/BootScene';
import { MenuScene } from './scenes/MenuScene';
import { WorldScene } from './scenes/WorldScene';
import { BaseInteriorScene } from './scenes/BaseInteriorScene';
import { DefenseScene } from './scenes/DefenseScene';
import { PauseScene } from './scenes/PauseScene';
import { PrestigeScene } from './scenes/PrestigeScene';
import { GameOverScene } from './scenes/GameOverScene';
import { GAME_CONFIG } from './config';

// Systems
import { TimeSystem } from './systems/TimeSystem';
import { FuelSystem } from './systems/FuelSystem';
import { EngineSystem } from './systems/EngineSystem';
import { WorldMapSystem } from './systems/WorldMapSystem';
import { BaseMovementSystem } from './systems/BaseMovementSystem';
import { CrewSystem } from './systems/CrewSystem';
import { VehicleSystem } from './systems/VehicleSystem';
import { MissionSystem } from './systems/MissionSystem';
import { RaidSystem } from './systems/RaidSystem';
import { BaseSystem } from './systems/BaseSystem';
import { ProductionSystem } from './systems/ProductionSystem';
import { EconomySystem } from './systems/EconomySystem';
import { SaveSystem } from './systems/SaveSystem';
import { PrestigeSystem } from './systems/PrestigeSystem';
import { GameState } from './state/GameState';
import { EventBus, EVENTS } from './state/EventBus';

export const Systems = {
    time: new TimeSystem(),
    fuel: new FuelSystem(),
    engine: new EngineSystem(),
    map: new WorldMapSystem(),
    baseMovement: new BaseMovementSystem(),
    crew: new CrewSystem(),
    vehicle: new VehicleSystem(),
    mission: new MissionSystem(),
    raid: new RaidSystem(),
    base: new BaseSystem(),
    production: new ProductionSystem(),
    economy: new EconomySystem(),
    save: new SaveSystem(),
    prestige: new PrestigeSystem(),
};

function setupEventListeners(): void {
    EventBus.on(EVENTS.TIME_HOUR_TICK, () => {
        Systems.fuel.onHourTick();
        Systems.engine.onHourTick();
        Systems.crew.onHourTick();
        Systems.production.onHourTick();
    });
    
    EventBus.on('time:minuteTick', (deltaMinutes: number) => {
        Systems.baseMovement.update(deltaMinutes);
    });
    
    EventBus.on(EVENTS.BASE_PATH_SELECTED, (data: any) => {
        Systems.baseMovement.setNextPath(data.fromNodeId, data.toNodeId);
    });
    
    EventBus.on(EVENTS.GAME_OVER, (data: any) => {
        Systems.prestige.recordRun(data.day, false);
        Systems.prestige.addPoints(data.prestigeEarned);
    });
    
    EventBus.on(EVENTS.GAME_WIN, (data: any) => {
        Systems.prestige.recordRun(data.day, true);
        Systems.prestige.addPoints(data.prestigeEarned);
    });
}

function initializeSystems(scene: Phaser.Scene): void {
    Systems.time.init(scene);
    Systems.fuel.init();
    Systems.engine.init();
    Systems.save.init(scene);
    setupEventListeners();
}

// ✅ Scale Override: บังคับให้ canvas แสดงผล 1280x720 เสมอ
const config: Phaser.Types.Core.GameConfig = {
    type: Phaser.AUTO,
    width: GAME_CONFIG.WIDTH,
    height: GAME_CONFIG.HEIGHT,
    parent: 'game-container',
    backgroundColor: '#1a2332',
    scene: [
        BootScene,
        MenuScene,
        WorldScene,
        BaseInteriorScene,
        DefenseScene,
        PauseScene,
        PrestigeScene,
        GameOverScene,
    ],
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH,
        width: GAME_CONFIG.WIDTH,
        height: GAME_CONFIG.HEIGHT,
        // ✅ บังคับให้ canvas คงสัดส่วน 16:9
        zoom: Phaser.Scale.MAX_ZOOM,
        expandParent: true,
    },
    physics: {
        default: 'arcade',
        arcade: {
            gravity: { x: 0, y: 0 },
            debug: false,
        },
    },
};

GameState.reset();

const game = new Phaser.Game(config);

export { game, initializeSystems };

export default config;