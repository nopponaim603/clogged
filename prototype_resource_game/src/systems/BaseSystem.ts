// src/systems/BaseSystem.ts
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Facility, FacilityType } from '../entities/Facility';
import { Crew } from '../entities/Crew';

export class BaseSystem {
    private facilities: Facility[] = [];
    
    // ============================================================
    // INITIALIZE
    // ============================================================
    init(): void {
        this.facilities = [];
        
        // ✅ สร้าง Engine Room เริ่มต้น (Level 1)
        const engine = Facility.create('engine', 1, 7, 3);
        this.facilities.push(engine);
        
        console.log(`🏠 Base initialized with ${this.facilities.length} facilities`);
    }
    
    // ============================================================
    // FACILITY MANAGEMENT
    // ============================================================
    buildFacility(type: FacilityType, floor: number, gridX: number, gridY: number): boolean {
        const state = GameState;
        
        // ✅ ตรวจสอบพื้นที่ว่าง
        if (!this.isSpaceAvailable(type, floor, gridX, gridY)) {
            console.warn(`❌ No space available for ${type}`);
            return false;
        }
        
        // ✅ ตรวจสอบทรัพยากร
        const config = GAME_CONFIG.FACILITY_TYPES[type];
        const cost = config.buildCost;
        
        for (const [resource, amount] of Object.entries(cost)) {
            if (resource === 'credits') {
                if (state.getCredits() < amount) return false;
            } else {
                if (state.getResource(resource) < amount) return false;
            }
        }
        
        // ✅ หักทรัพยากร
        for (const [resource, amount] of Object.entries(cost)) {
            if (resource === 'credits') {
                state.removeCredits(amount);
            } else {
                state.removeResource(resource, amount);
            }
        }
        
        // ✅ สร้าง Facility
        const facility = Facility.create(type, floor, gridX, gridY);
        this.facilities.push(facility);
        
        EventBus.emit('facility:built', facility);
        
        console.log(`✅ Built ${type} at Floor ${floor} (${gridX}, ${gridY})`);
        
        return true;
    }
    
    dismantleFacility(facilityId: string): boolean {
        const index = this.facilities.findIndex(f => f.id === facilityId);
        if (index === -1) return false;
        
        const facility = this.facilities[index];
        
        // ✅ คืนทรัพยากร 50%
        const state = GameState;
        const config = GAME_CONFIG.FACILITY_TYPES[facility.type];
        
        for (const [resource, amount] of Object.entries(config.buildCost)) {
            const refund = Math.floor(amount * 0.5);
            if (resource === 'credits') {
                state.addCredits(refund);
            } else {
                state.addResource(resource, refund);
            }
        }
        
        // ✅ คืน Worker
        facility.workers.forEach(workerId => {
            const crew = state.getCrew().find(c => c.id === workerId);
            if (crew) {
                crew.state = 'idle';
                crew.assignedFacilityId = null;
            }
        });
        
        this.facilities.splice(index, 1);
        
        EventBus.emit('facility:dismantled', facilityId);
        
        return true;
    }
    
    upgradeFacility(facilityId: string): boolean {
        const facility = this.facilities.find(f => f.id === facilityId);
        if (!facility) return false;
        
        if (!facility.canUpgrade()) {
            console.warn('Facility already at max level');
            return false;
        }
        
        // ✅ ตรวจสอบทรัพยากร
        const state = GameState;
        const cost = facility.getUpgradeCost();
        
        for (const [resource, amount] of Object.entries(cost)) {
            if (resource === 'credits') {
                if (state.getCredits() < amount) return false;
            } else {
                if (state.getResource(resource) < amount) return false;
            }
        }
        
        // ✅ หักทรัพยากร
        for (const [resource, amount] of Object.entries(cost)) {
            if (resource === 'credits') {
                state.removeCredits(amount);
            } else {
                state.removeResource(resource, amount);
            }
        }
        
        facility.upgrade();
        
        EventBus.emit('facility:upgraded', facility);
        
        return true;
    }
    
    repairFacility(facilityId: string): boolean {
        const facility = this.facilities.find(f => f.id === facilityId);
        if (!facility) return false;
        if (!facility.isDamaged()) return false;
        
        // ✅ คำนวณวัสดุที่ต้องใช้
        const state = GameState;
        const repairAmount = 100 - facility.condition;
        const ironCost = Math.ceil(repairAmount * 0.5);
        
        if (state.getResource('iron_bar') < ironCost) return false;
        
        state.removeResource('iron_bar', ironCost);
        facility.repair(repairAmount);
        
        EventBus.emit('facility:repaired', facility);
        
        return true;
    }
    
    // ============================================================
    // WORKER ASSIGNMENT
    // ============================================================
    assignWorker(facilityId: string, crewId: number): boolean {
        const facility = this.facilities.find(f => f.id === facilityId);
        if (!facility) return false;
        
        const state = GameState;
        const crew = state.getCrew().find(c => c.id === crewId);
        if (!crew) return false;
        
        // ✅ เช็ค crew state
        if (crew.state !== 'idle') return false;
        
        // ✅ เพิ่ม worker
        if (!facility.addWorker(crewId)) return false;
        
        // ✅ Update crew
        crew.state = 'assigned';
        crew.assignedFacilityId = facilityId;
        
        EventBus.emit('crew:assigned', { crewId, facilityId });
        
        // ✅ Update Engine Worker
        if (facility.type === 'engine') {
            const engine = state.getEngine();
            engine.isWorkerAssigned = true;
        }
        
        return true;
    }
    
    unassignWorker(facilityId: string, crewId: number): boolean {
        const facility = this.facilities.find(f => f.id === facilityId);
        if (!facility) return false;
        
        if (!facility.removeWorker(crewId)) return false;
        
        const state = GameState;
        const crew = state.getCrew().find(c => c.id === crewId);
        if (crew) {
            crew.state = 'idle';
            crew.assignedFacilityId = null;
        }
        
        EventBus.emit('crew:unassigned', { crewId, facilityId });
        
        // ✅ Update Engine Worker
        if (facility.type === 'engine') {
            const engine = state.getEngine();
            engine.isWorkerAssigned = facility.hasWorker();
        }
        
        return true;
    }
    
    // ============================================================
    // SPACE CHECK
    // ============================================================
    private isSpaceAvailable(type: FacilityType, floor: number, gridX: number, gridY: number): boolean {
        const config = GAME_CONFIG.FACILITY_TYPES[type];
        const w = config.size.w;
        const h = config.size.h;
        
        // ✅ ตรวจสอบขอบเขต
        if (gridX < 0 || gridX + w > GAME_CONFIG.BASE_GRID_WIDTH) return false;
        if (gridY < 0 || gridY + h > GAME_CONFIG.BASE_GRID_HEIGHT) return false;
        
        // ✅ ตรวจสอบการทับซ้อน
        for (const facility of this.facilities) {
            if (facility.floor !== floor) continue;
            
            const fw = facility.size.w;
            const fh = facility.size.h;
            
            const overlapX = gridX < facility.gridX + fw && gridX + w > facility.gridX;
            const overlapY = gridY < facility.gridY + fh && gridY + h > facility.gridY;
            
            if (overlapX && overlapY) return false;
        }
        
        return true;
    }
    
    // ============================================================
    // QUERY
    // ============================================================
    getAllFacilities(): Facility[] {
        return this.facilities;
    }
    
    getFacilitiesByType(type: FacilityType): Facility[] {
        return this.facilities.filter(f => f.type === type);
    }
    
    getFacilityById(id: string): Facility | undefined {
        return this.facilities.find(f => f.id === id);
    }
    
    getFacilitiesByFloor(floor: number): Facility[] {
        return this.facilities.filter(f => f.floor === floor);
    }
    
    // ============================================================
    // RESET
    // ============================================================
    reset(): void {
        this.facilities = [];
        this.init();
    }
}