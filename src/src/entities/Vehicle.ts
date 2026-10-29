// src/entities/Vehicle.ts
import { Vehicle as IVehicle, MissionState } from '../state/types';
import { GAME_CONFIG } from '../config';

export class Vehicle implements IVehicle {
    public id: string;
    public name: string;
    public level: number;
    public capacity: number;
    public speedKmPerHour: number;
    public fuelPerHour: number;
    public isDispatched: boolean;
    public assignedCrewIds: number[];
    public currentMission: MissionState | null;
    
    constructor(data: Partial<Vehicle>) {
        this.id = data.id || `vehicle_${Date.now()}`;
        this.name = data.name || 'Vehicle';
        this.level = data.level || 1;
        this.capacity = data.capacity || GAME_CONFIG.VEHICLE_CAPACITY;
        this.speedKmPerHour = data.speedKmPerHour || GAME_CONFIG.VEHICLE_SPEED_KM_PER_HOUR;
        this.fuelPerHour = data.fuelPerHour || GAME_CONFIG.VEHICLE_FUEL_PER_HOUR;
        this.isDispatched = data.isDispatched || false;
        this.assignedCrewIds = data.assignedCrewIds || [];
        this.currentMission = data.currentMission || null;
    }
    
    // ============================================================
    // STATIC FACTORY
    // ============================================================
    static create(id: string, name: string): Vehicle {
        return new Vehicle({
            id,
            name,
            level: 1,
            capacity: GAME_CONFIG.VEHICLE_CAPACITY,
            speedKmPerHour: GAME_CONFIG.VEHICLE_SPEED_KM_PER_HOUR,
            fuelPerHour: GAME_CONFIG.VEHICLE_FUEL_PER_HOUR,
        });
    }
    
    // ============================================================
    // CREW MANAGEMENT
    // ============================================================
    canAddCrew(): boolean {
        return this.assignedCrewIds.length < this.capacity && !this.isDispatched;
    }
    
    addCrew(crewId: number): boolean {
        if (!this.canAddCrew()) return false;
        if (this.assignedCrewIds.includes(crewId)) return false;
        
        this.assignedCrewIds.push(crewId);
        return true;
    }
    
    removeCrew(crewId: number): boolean {
        if (this.isDispatched) return false;
        
        const index = this.assignedCrewIds.indexOf(crewId);
        if (index === -1) return false;
        
        this.assignedCrewIds.splice(index, 1);
        return true;
    }
    
    clearCrew(): void {
        this.assignedCrewIds = [];
    }
    
    getCrewCount(): number {
        return this.assignedCrewIds.length;
    }
    
    // ============================================================
    // FUEL CALCULATION
    // ============================================================
    
    // ✅ คำนวณน้ำมันที่ใช้สำหรับระยะทาง
    calculateFuelCost(distanceHours: number): number {
        // ✅ Level bonus: ลด fuel/hours ตาม level
        const efficiency = 1 - (this.level - 1) * GAME_CONFIG.VEHICLE_FUEL_EFFICIENCY_PER_LEVEL;
        const fuelPerHour = GAME_CONFIG.VEHICLE_FUEL_PER_HOUR * efficiency;
        
        return distanceHours * fuelPerHour;
    }
    
    // ✅ ความเร็ว
    getSpeed(): number {
        const bonus = 1 + (this.level - 1) * (GAME_CONFIG.VEHICLE_SPEED_BONUS_PER_LEVEL / 100);
        return GAME_CONFIG.VEHICLE_SPEED_KM_PER_HOUR * bonus;
    }
    
    // ============================================================
    // UPGRADE
    // ============================================================
    canUpgrade(): boolean {
        return this.level < GAME_CONFIG.VEHICLE_MAX_UPGRADE_LEVEL;
    }
    
    getUpgradeCost(): Record<string, number> {  // ✅ ระบุ type
        const multiplier = this.level;
        return {
            wood: GAME_CONFIG.VEHICLE_UPGRADE_COST_WOOD * multiplier,
            iron_bar: GAME_CONFIG.VEHICLE_UPGRADE_COST_IRON * multiplier,
            credits: GAME_CONFIG.VEHICLE_UPGRADE_COST_CREDITS * multiplier,
        };
    }
    
    upgrade(): void {
        if (!this.canUpgrade()) return;
        this.level++;
    }
    
    // ============================================================
    // MISSION
    // ============================================================
    startMission(mission: MissionState): void {
        this.isDispatched = true;
        this.currentMission = mission;
    }
    
    completeMission(): void {
        this.isDispatched = false;
        this.currentMission = null;
    }
    
    // ============================================================
    // SERIALIZATION
    // ============================================================
    toJSON(): object {
        return {
            id: this.id,
            name: this.name,
            level: this.level,
            capacity: this.capacity,
            speedKmPerHour: this.speedKmPerHour,
            fuelPerHour: this.fuelPerHour,
            isDispatched: this.isDispatched,
            assignedCrewIds: [...this.assignedCrewIds],
            currentMission: this.currentMission,
        };
    }
    
    static fromJSON(data: any): Vehicle {
        return new Vehicle(data);
    }
}