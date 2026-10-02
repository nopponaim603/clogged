// src/systems/FuelSystem.ts
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';

export class FuelSystem {
    private lastWarningState: boolean = false;
    
    // ============================================================
    // INITIALIZE
    // ============================================================
    init(): void {
        const state = GameState;  // ✅ ลบ .getInstance()
        const fuel = state.getFuel();
        
        fuel.current = GAME_CONFIG.FUEL_CAPACITY;
        fuel.max = GAME_CONFIG.FUEL_CAPACITY;
        fuel.isWarning = false;
        
        this.updateFuelCalculations();
    }
    
    // ============================================================
    // UPDATE (Called every game hour by TimeSystem)
    // ============================================================
    onHourTick(): void {
        const state = GameState;
        const fuel = state.getFuel();
        const time = state.getTime();
        
        if (time.isPaused) return;
        if (state.isGameOver()) return;
        
        // ✅ Consumption ต่อชั่วโมง (รวม escalation)
        let consumption = GAME_CONFIG.FUEL_CONSUMPTION_PER_HOUR;
        consumption *= this.getEscalationMultiplier();
        
        // ✅ หักน้ำมัน
        if (fuel.current > 0) {
            fuel.current = Math.max(0, fuel.current - consumption);
        }
        
        this.updateFuelCalculations();
        this.checkWarning();
        this.checkDepleted();
        
        EventBus.emit(EVENTS.FUEL_CHANGED, {
            current: fuel.current,
            max: fuel.max,
        });
    }
    
    // ============================================================
    // FUEL CALCULATIONS
    // ============================================================
    private updateFuelCalculations(): void {
        const state = GameState;
        const fuel = state.getFuel();
        
        const escalationMultiplier = this.getEscalationMultiplier();
        
        const consumptionPerHour = GAME_CONFIG.FUEL_CONSUMPTION_PER_HOUR 
            * escalationMultiplier;
        
        const consumptionPer100km = GAME_CONFIG.FUEL_CONSUMPTION_PER_100KM 
            * escalationMultiplier;
        
        if (consumptionPerHour > 0) {
            fuel.hoursRemaining = fuel.current / consumptionPerHour;
        } else {
            fuel.hoursRemaining = Infinity;
        }
        
        if (consumptionPer100km > 0) {
            fuel.kmRemaining = (fuel.current / consumptionPer100km) * 100;
        } else {
            fuel.kmRemaining = Infinity;
        }
    }
    
    private getEscalationMultiplier(): number {
        const time = GameState.getTime();
        if (time.gameDay > GAME_CONFIG.ESCALATION_START_DAY) {
            return Math.pow(GAME_CONFIG.ESCALATION_MULTIPLIER, time.escalationLevel);
        }
        return 1.0;
    }
    
    // ============================================================
    // WARNING
    // ============================================================
    private checkWarning(): void {
        const state = GameState;
        const fuel = state.getFuel();
        
        const isWarning = fuel.hoursRemaining < GAME_CONFIG.FUEL_WARNING_TIME
            || fuel.kmRemaining < GAME_CONFIG.FUEL_WARNING_DISTANCE;
        
        fuel.isWarning = isWarning;
        
        if (isWarning && !this.lastWarningState) {
            EventBus.emit(EVENTS.FUEL_WARNING, {
                hoursRemaining: fuel.hoursRemaining,
                kmRemaining: fuel.kmRemaining,
            });
        }
        
        this.lastWarningState = isWarning;
    }
    
    // ============================================================
    // DEPLETED
    // ============================================================
    private checkDepleted(): void {
        const state = GameState;
        const fuel = state.getFuel();
        
        if (fuel.current <= 0) {
            EventBus.emit(EVENTS.FUEL_DEPLETED);
            state.triggerGameOver('fuel_depleted', false);
        }
    }
    
    // ============================================================
    // ADD / REMOVE FUEL
    // ============================================================
    addFuel(amount: number): void {
        const state = GameState;
        const fuel = state.getFuel();
        
        fuel.current = Math.min(fuel.max, fuel.current + amount);
        
        this.updateFuelCalculations();
        this.checkWarning();
        
        EventBus.emit(EVENTS.FUEL_CHANGED, {
            current: fuel.current,
            max: fuel.max,
        });
    }
    
    removeFuel(amount: number): boolean {
        const state = GameState;
        const fuel = state.getFuel();
        
        if (fuel.current < amount) return false;
        
        fuel.current -= amount;
        
        this.updateFuelCalculations();
        this.checkWarning();
        
        EventBus.emit(EVENTS.FUEL_CHANGED, {
            current: fuel.current,
            max: fuel.max,
        });
        
        return true;
    }
    
    // ============================================================
    // UPGRADE
    // ============================================================
    upgradeCapacity(levels: number = 1): void {
        const state = GameState;
        const fuel = state.getFuel();
        
        const maxLevel = GAME_CONFIG.FUEL_MAX_UPGRADE_LEVEL;
        const currentLevel = Math.floor(
            (fuel.max - GAME_CONFIG.FUEL_CAPACITY) 
            / GAME_CONFIG.FUEL_CAPACITY_UPGRADE_PER_LEVEL
        );
        
        if (currentLevel >= maxLevel) {
            console.warn('Fuel capacity already at max level');
            return;
        }
        
        const actualLevels = Math.min(levels, maxLevel - currentLevel);
        fuel.max += GAME_CONFIG.FUEL_CAPACITY_UPGRADE_PER_LEVEL * actualLevels;
        
        this.updateFuelCalculations();
        
        EventBus.emit(EVENTS.FUEL_CHANGED, {
            current: fuel.current,
            max: fuel.max,
        });
    }
    
    // ============================================================
    // DISPATCH COST
    // ============================================================
    calculateDispatchCost(distanceHours: number): number {
        const state = GameState;
        const vehicles = state.getVehicles();
        
        // ✅ หา vehicle level เฉลี่ย
        const vehicleLevel = vehicles.length > 0 ? vehicles[0].level : 1;
        
        // ✅ Fuel per hour ลดลงตาม level
        const efficiency = 1 - (vehicleLevel - 1) * GAME_CONFIG.VEHICLE_FUEL_EFFICIENCY_PER_LEVEL;
        const fuelPerHour = GAME_CONFIG.VEHICLE_FUEL_PER_HOUR * efficiency;
        
        return distanceHours * fuelPerHour;
    }
    
    canAffordDispatch(distanceHours: number): boolean {
        const state = GameState;
        const fuel = state.getFuel();
        
        const cost = this.calculateDispatchCost(distanceHours);
        return fuel.current >= cost;
    }
    
    // ============================================================
    // GETTERS
    // ============================================================
    getCurrent(): number {
        return GameState.getFuel().current;
    }
    
    getMax(): number {
        return GameState.getFuel().max;
    }
    
    getPercent(): number {
        const fuel = GameState.getFuel();
        return (fuel.current / fuel.max) * 100;
    }
    
    isWarning(): boolean {
        return GameState.getFuel().isWarning;
    }
    
    getHoursRemaining(): number {
        return GameState.getFuel().hoursRemaining;
    }
    
    getKmRemaining(): number {
        return GameState.getFuel().kmRemaining;
    }
    
    // ============================================================
    // RESET
    // ============================================================
    reset(): void {
        this.lastWarningState = false;
        this.init();
    }
}