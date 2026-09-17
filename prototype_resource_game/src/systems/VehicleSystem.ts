// src/systems/VehicleSystem.ts
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Vehicle } from '../entities/Vehicle';

export class VehicleSystem {
    private nextVehicleIndex: number = 1;
    
    // ============================================================
    // INITIALIZE
    // ============================================================
    init(): void {
        this.nextVehicleIndex = 1;
        
        // ✅ สร้าง Vehicle เริ่มต้น
        const vehicles: Vehicle[] = [];
        
        for (let i = 0; i < GAME_CONFIG.STARTING_VEHICLES; i++) {
            const vehicle = Vehicle.create(
                `vehicle_${this.nextVehicleIndex}`,
                `Truck ${String.fromCharCode(65 + i)}`  // A, B, C
            );
            vehicles.push(vehicle);
            this.nextVehicleIndex++;
        }
        
        GameState.setVehicles(vehicles);
        
        console.log(`🚗 Created ${vehicles.length} starting vehicles`);
    }
    
    // ============================================================
    // QUERY
    // ============================================================
    getVehicleById(id: string): Vehicle | undefined {
        return GameState.getVehicles().find(v => v.id === id);
    }
    
    getAvailableVehicles(): Vehicle[] {
        return GameState.getVehicles().filter(v => !v.isDispatched);
    }
    
    getDispatchedVehicles(): Vehicle[] {
        return GameState.getVehicles().filter(v => v.isDispatched);
    }
    
    getAllVehicles(): Vehicle[] {
        return GameState.getVehicles();
    }
    
    // ============================================================
    // ASSIGN CREW
    // ============================================================
    assignCrewToVehicle(crewId: number, vehicleId: string): boolean {
        const vehicle = this.getVehicleById(vehicleId);
        if (!vehicle) return false;
        
        return vehicle.addCrew(crewId);
    }
    
    removeCrewFromVehicle(crewId: number, vehicleId: string): boolean {
        const vehicle = this.getVehicleById(vehicleId);
        if (!vehicle) return false;
        
        return vehicle.removeCrew(crewId);
    }
    
    // ============================================================
    // DISPATCH
    // ============================================================
    canDispatch(vehicleId: string, distanceHours: number): { success: boolean; reason?: string } {
        const state = GameState;
        const vehicle = this.getVehicleById(vehicleId);
        
        if (!vehicle) {
            return { success: false, reason: 'Vehicle not found' };
        }
        
        if (vehicle.isDispatched) {
            return { success: false, reason: 'Vehicle already dispatched' };
        }
        
        if (vehicle.assignedCrewIds.length === 0) {
            return { success: false, reason: 'No crew assigned' };
        }
        
        // ✅ เช็ค Fuel
        const fuelCost = vehicle.calculateFuelCost(distanceHours);
        const fuel = state.getFuel();
        
        if (fuel.current < fuelCost) {
            return { success: false, reason: `Not enough fuel (need ${Math.ceil(fuelCost)})` };
        }
        
        return { success: true };
    }
    
    dispatch(vehicleId: string, distanceHours: number): boolean {
        const state = GameState;
        const vehicle = this.getVehicleById(vehicleId);
        
        if (!vehicle) return false;
        
        // ✅ เช็ค
        const check = this.canDispatch(vehicleId, distanceHours);
        if (!check.success) {
            console.warn(`❌ Cannot dispatch: ${check.reason}`);
            return false;
        }
        
        // ✅ หัก Fuel
        const fuelCost = vehicle.calculateFuelCost(distanceHours);
        state.removeResource('fuel', fuelCost);  // ⚠️ Fuel แยก?
        
        // ⚠️ จริงๆ ต้องใช้ FuelSystem
        
        console.log(`🚗 Vehicle ${vehicle.name} dispatched! Fuel cost: ${fuelCost}`);
        
        EventBus.emit(EVENTS.CREW_DISPATCHED, {
            vehicleId,
            crewIds: [...vehicle.assignedCrewIds],
            distanceHours,
        });
        
        return true;
    }
    
    // ============================================================
    // UPGRADE
    // ============================================================
    upgradeVehicle(vehicleId: string): boolean {
        const state = GameState;
        const vehicle = this.getVehicleById(vehicleId);
        
        if (!vehicle) return false;
        if (!vehicle.canUpgrade()) return false;
        
        const cost: Record<string, number> = vehicle.getUpgradeCost();  // ✅ ระบุ type
        
        for (const [type, amount] of Object.entries(cost)) {
            // ✅ amount เป็น number แล้ว
            if (type === 'credits') {
                if (state.getCredits() < amount) return false;
            } else {
                if (state.getResource(type) < amount) return false;
            }
        }
        
        for (const [type, amount] of Object.entries(cost)) {
            if (type === 'credits') {
                state.removeCredits(amount);
            } else {
                state.removeResource(type, amount);
            }
        }
        
        vehicle.upgrade();
        
        return true;
    }
    
    // ============================================================
    // PURCHASE (ซื้อจาก Shop)
    // ============================================================
    purchaseVehicle(cost: number): boolean {
        const state = GameState;
        
        if (state.getCredits() < cost) return false;
        
        state.removeCredits(cost);
        
        const newVehicle = Vehicle.create(
            `vehicle_${this.nextVehicleIndex}`,
            `Truck ${this.nextVehicleIndex}`
        );
        this.nextVehicleIndex++;
        
        const vehicles = state.getVehicles();
        vehicles.push(newVehicle);
        state.setVehicles(vehicles);
        
        console.log(`🚗 Purchased new vehicle: ${newVehicle.name}`);
        
        return true;
    }
    
    // ============================================================
    // STATS
    // ============================================================
    getTotalVehicleCount(): number {
        return GameState.getVehicles().length;
    }
    
    getAvailableVehicleCount(): number {
        return this.getAvailableVehicles().length;
    }
}