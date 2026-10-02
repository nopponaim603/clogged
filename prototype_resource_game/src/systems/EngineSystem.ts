// src/systems/EngineSystem.ts
import * as Phaser from 'phaser';
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';

export class EngineSystem {
    private repairProgress: number = 0;
    private isRepairingActive: boolean = false;
    
    // ============================================================
    // INITIALIZE
    // ============================================================
    init(): void {
        const state = GameState;
        const engine = state.getEngine();
        
        engine.condition = GAME_CONFIG.ENGINE_MAX_CONDITION;
        engine.level = 1;
        engine.speedMultiplier = 1.0;
        engine.isWorkerAssigned = false;
        
        this.updateSpeedMultiplier();
    }
    
    // ============================================================
    // UPDATE
    // ============================================================
    onHourTick(): void {
        const state = GameState;
        const engine = state.getEngine();
        const time = state.getTime();
        
        if (time.isPaused) return;
        if (state.isGameOver()) return;
        
        if (this.isRepairingActive) {
            this.doRepair();
            return;
        }
        
        this.applyDecay();
        this.checkEngineFailure();
        this.updateSpeedMultiplier();
        
        EventBus.emit(EVENTS.ENGINE_CONDITION_CHANGED, {
            condition: engine.condition,
            level: engine.level,
        });
    }
    
    // ============================================================
    // DECAY
    // ============================================================
    private applyDecay(): void {
        const state = GameState;
        const engine = state.getEngine();
        
        const levelIndex = Math.min(
            engine.level - 1,
            GAME_CONFIG.ENGINE_DECAY_NO_WORKER.length - 1
        );
        
        let decay = engine.isWorkerAssigned
            ? GAME_CONFIG.ENGINE_DECAY_WITH_WORKER[levelIndex]
            : GAME_CONFIG.ENGINE_DECAY_NO_WORKER[levelIndex];
        
        const mechanicBonus = this.getMechanistReduction();
        decay *= (1 - mechanicBonus);
        
        const escalationMultiplier = this.getEscalationMultiplier();
        decay *= escalationMultiplier;
        
        engine.condition = Math.max(0, engine.condition - decay);
    }
    
    private getMechanistReduction(): number {
        const state = GameState;
        const crew = state.getCrew();
        
        const engineWorker = crew.find(c => 
            c.assignedFacilityId?.startsWith('engine_') 
            && c.perks.some(p => p.startsWith('mechanist'))
        );
        
        if (!engineWorker) return 0;
        
        if (engineWorker.perks.includes('mechanist_iii')) {
            return GAME_CONFIG.MECHANIST_REDUCTION.tier3;
        }
        if (engineWorker.perks.includes('mechanist_ii')) {
            return GAME_CONFIG.MECHANIST_REDUCTION.tier2;
        }
        if (engineWorker.perks.includes('mechanist_i')) {
            return GAME_CONFIG.MECHANIST_REDUCTION.tier1;
        }
        
        return 0;
    }
    
    private getEscalationMultiplier(): number {
        const time = GameState.getTime();
        if (time.gameDay > GAME_CONFIG.ESCALATION_START_DAY) {
            return Math.pow(GAME_CONFIG.ESCALATION_MULTIPLIER, time.escalationLevel);
        }
        return 1.0;
    }
    
    // ============================================================
    // ENGINE FAILURE
    // ============================================================
    private checkEngineFailure(): void {
        const state = GameState;
        const engine = state.getEngine();
        
        if (engine.condition <= 0) {
            EventBus.emit(EVENTS.ENGINE_FAILED);
            state.triggerGameOver('engine_failure', false);
        }
    }
    
    // ============================================================
    // SPEED MULTIPLIER
    // ============================================================
    private updateSpeedMultiplier(): void {
        const state = GameState;
        const engine = state.getEngine();
        
        if (engine.condition <= 0) {
            engine.speedMultiplier = 0;
            return;
        }
        
        if (engine.condition < GAME_CONFIG.BASE_ENGINE_LOW_THRESHOLD) {
            engine.speedMultiplier = 0.5;
            return;
        }
        
        let multiplier = 1.0;
        multiplier += (engine.level - 1) * (GAME_CONFIG.BASE_ENGINE_SPEED_BONUS_PER_LEVEL / 100);
        
        const conditionBonus = (engine.condition - 50) / 100;
        multiplier += conditionBonus;
        
        engine.speedMultiplier = Math.max(0.5, Math.min(2.0, multiplier));
    }
    
    getSpeedMultiplier(): number {
        return GameState.getEngine().speedMultiplier;
    }
    
    // ============================================================
    // REPAIR
    // ============================================================
    startRepair(): boolean {
        const state = GameState;
        const engine = state.getEngine();
        
        if (engine.condition >= GAME_CONFIG.ENGINE_MAX_CONDITION) {
            return false;
        }
        
        const ironCost = this.calculateRepairCost();
        if (state.getResource('iron_bar') < ironCost) {
            return false;
        }
        
        this.isRepairingActive = true;
        this.repairProgress = 0;
        
        return true;
    }
    
    private doRepair(): void {
        const state = GameState;
        const engine = state.getEngine();
        
        const repairAmount = 1;
        const mechanicBonus = this.getMechanistReduction();
        const actualRepair = repairAmount * (1 + mechanicBonus);
        
        engine.condition = Math.min(
            GAME_CONFIG.ENGINE_MAX_CONDITION,
            engine.condition + actualRepair
        );
        
        this.repairProgress += actualRepair;
        
        if (engine.condition >= GAME_CONFIG.ENGINE_MAX_CONDITION) {
            this.isRepairingActive = false;
            this.repairProgress = 0;
        }
        
        this.updateSpeedMultiplier();
        
        EventBus.emit(EVENTS.ENGINE_CONDITION_CHANGED, {
            condition: engine.condition,
            level: engine.level,
        });
    }
    
    stopRepair(): void {
        this.isRepairingActive = false;
        this.repairProgress = 0;
    }
    
    private calculateRepairCost(): number {
        const state = GameState;
        const engine = state.getEngine();
        
        const missingCondition = GAME_CONFIG.ENGINE_MAX_CONDITION - engine.condition;
        return Math.ceil(missingCondition * GAME_CONFIG.ENGINE_REPAIR_COST_IRON_PER_PERCENT);
    }
    
    // ============================================================
    // UPGRADE
    // ============================================================
    canUpgrade(): boolean {
        const engine = GameState.getEngine();
        return engine.level < GAME_CONFIG.ENGINE_MAX_LEVEL;
    }
    
    upgrade(): boolean {
        const state = GameState;
        const engine = state.getEngine();
        
        if (!this.canUpgrade()) return false;
        
        const cost = this.getUpgradeCost();
        if (!this.canAffordUpgrade(cost)) return false;
        
        this.spendUpgradeCost(cost);
        engine.level++;
        
        this.updateSpeedMultiplier();
        
        EventBus.emit(EVENTS.ENGINE_CONDITION_CHANGED, {
            condition: engine.condition,
            level: engine.level,
        });
        
        return true;
    }
    
    private getUpgradeCost(): { [key: string]: number } {
        const engine = GameState.getEngine();
        const nextLevel = engine.level + 1;
        
        const costKey = `level${nextLevel}` as keyof typeof GAME_CONFIG.FACILITY_TYPES.engine.upgradeCost;
        return GAME_CONFIG.FACILITY_TYPES.engine.upgradeCost[costKey] || {};
    }
    
    private canAffordUpgrade(cost: { [key: string]: number }): boolean {
        const state = GameState;
        
        for (const [type, amount] of Object.entries(cost)) {
            if (type === 'credits') {
                if (state.getCredits() < amount) return false;
            } else {
                if (state.getResource(type) < amount) return false;
            }
        }
        return true;
    }
    
    private spendUpgradeCost(cost: { [key: string]: number }): void {
        const state = GameState;
        
        for (const [type, amount] of Object.entries(cost)) {
            if (type === 'credits') {
                state.removeCredits(amount);
            } else {
                state.removeResource(type, amount);
            }
        }
    }
    
    // ============================================================
    // RAIDER DAMAGE
    // ============================================================
    takeRaidDamage(): void {
        const state = GameState;
        const engine = state.getEngine();
        
        const damage = Phaser.Math.Between(
            GAME_CONFIG.ENGINE_RAID_DAMAGE_MIN,
            GAME_CONFIG.ENGINE_RAID_DAMAGE_MAX
        );
        
        engine.condition = Math.max(0, engine.condition - damage);
        
        this.updateSpeedMultiplier();
        this.checkEngineFailure();
        
        EventBus.emit(EVENTS.ENGINE_CONDITION_CHANGED, {
            condition: engine.condition,
            level: engine.level,
        });
    }
    
    // ============================================================
    // WORKER ASSIGNMENT
    // ============================================================
    setWorkerAssigned(assigned: boolean): void {
        const engine = GameState.getEngine();
        engine.isWorkerAssigned = assigned;
    }
    
    // ============================================================
    // GETTERS
    // ============================================================
    getCondition(): number {
        return GameState.getEngine().condition;
    }
    
    getLevel(): number {
        return GameState.getEngine().level;
    }
    
    isRepairing(): boolean {
        return this.isRepairingActive;
    }
    
    getRepairProgress(): number {
        return this.repairProgress;
    }
    
    getConditionPercent(): number {
        return (this.getCondition() / GAME_CONFIG.ENGINE_MAX_CONDITION) * 100;
    }
    
    // ============================================================
    // RESET
    // ============================================================
    reset(): void {
        this.repairProgress = 0;
        this.isRepairingActive = false;
        this.init();
    }
}