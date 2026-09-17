// src/systems/MissionSystem.ts
import { GAME_CONFIG, RESOURCE_ICONS } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Crew } from '../entities/Crew';        // ✅ ใช้ entities
import { Vehicle } from '../entities/Vehicle';  // ✅ ใช้ entities
import { MapNode } from '../entities/MapNode';
import { MissionState, MissionPhase } from '../state/types';
import { RandomGenerator } from '../utils/RandomGenerator';  // ✅ import
import { Helpers } from '../utils/Helpers';  // ✅ import

export interface MissionResult {
    success: boolean;
    resourcesGained: { [key: string]: number };
    crewLost: number[];
    message: string;
}

export class MissionSystem {
    
    // ============================================================
    // START MISSION
    // ============================================================
    startMission(vehicle: Vehicle, targetNode: MapNode): MissionState {
        const state = GameState;
        const time = state.getTime();
        const basePos = state.getBasePosition();
        
        // ✅ คำนวณระยะทาง (ชั่วโมง)
        const distance = Math.sqrt(
            Math.pow(targetNode.position.x - basePos.x, 2) +
            Math.pow(targetNode.position.y - basePos.y, 2)
        );
        const distanceHours = distance / 1000;  // 1000 px = 1 ชม.
        
        // ✅ คำนวณเวลา
        const vehicleSpeed = vehicle.getSpeed();  // km/h
        const baseSpeed = GAME_CONFIG.BASE_SPEED_KM_PER_HOUR;
        const speedRatio = vehicleSpeed / baseSpeed;
        
        const travelOutTime = distanceHours / speedRatio;
        const actionTime = this.calculateActionTime(targetNode, vehicle);
        const travelBackTime = travelOutTime;  // ระยะทางกลับเท่ากัน (ประมาณ)
        
        // ✅ สร้าง MissionState
        const mission: MissionState = {
            vehicleId: vehicle.id,
            nodeId: targetNode.id,
            phase: 'travel_out',
            startHour: time.gameHour,
            startDay: time.gameDay,
            travelOutTime,
            actionTime,
            travelBackTime,
            elapsedHours: 0,
            progress: 0,
            success: false,
            resourcesGained: {},
            crewLost: [],
        };
        
        vehicle.startMission(mission);
        
        EventBus.emit(EVENTS.MISSION_STARTED, {
            vehicleId: vehicle.id,
            nodeId: targetNode.id,
            mission,
        });
        
        return mission;
    }
    
    // ============================================================
    // ACTION TIME CALCULATION
    // ============================================================
    private calculateActionTime(targetNode: MapNode, vehicle: Vehicle): number {
        const rng = RandomGenerator.getInstance();  // ✅ ใช้ import
        const state = GameState;
        const crewInVehicle = state.getCrew().filter(c => 
            vehicle.assignedCrewIds.includes(c.id)
        );
        
        if (crewInVehicle.length === 0) return 1;
        
        switch (targetNode.type) {
            case 'resource':
                return this.calculateGatherTime(targetNode, crewInVehicle);
            case 'relic':
                return this.calculateExploreTime(targetNode, crewInVehicle);
            case 'monster':
                return this.calculateHuntTime(targetNode, crewInVehicle);
            default:
                return 0.5;  // 30 นาที default
        }
    }
    
    // ✅ Gathering Time (ตามชีต)
    private calculateGatherTime(node: MapNode, crew: Crew[]): number {
        const rng = RandomGenerator.getInstance();        
        // ✅ ดึงค่า
        const resourceType = node.resourceType || 'wood';
        const baseValue = GAME_CONFIG.RESOURCE_TYPE_VALUE[resourceType as keyof typeof GAME_CONFIG.RESOURCE_TYPE_VALUE] || 100;
        
        const range = node.resourceAmount || { min: 10, max: 20 };
        const amount = rng.randomInt(range.min, range.max);
        
        const rarityMult = GAME_CONFIG.RESOURCE_NODE_RARITY[node.rarity || 'common'];
        
        // ✅ Node HP = value × amount × rarity
        const nodeHP = baseValue * amount * rarityMult;
        
        // ✅ Total Efficiency (ต่อนาที)
        let totalEfficiency = 0;
        crew.forEach(c => {
            totalEfficiency += c.getGatheringEfficiency();
        });
        
        // ✅ เวลา (นาทีเกม)
        const timeMinutes = nodeHP / Math.max(1, totalEfficiency);
        
        // ✅ แปลงเป็นชั่วโมง
        return timeMinutes / 60;
    }
    
    // ✅ Exploring Time (ตามชีต)
    private calculateExploreTime(node: MapNode, crew: Crew[]): number {
        const rng = RandomGenerator.getInstance();        
        // ✅ Relic Value
        const relicValue = rng.pick([
            GAME_CONFIG.RELIC_RARITY_VALUE.common,
            GAME_CONFIG.RELIC_RARITY_VALUE.uncommon,
            GAME_CONFIG.RELIC_RARITY_VALUE.rare,
        ]);
        
        // ✅ Size Multiplier
        const sizeMult = GAME_CONFIG.RELIC_SIZE_MULTIPLIER[node.relicSize || 'small'];
        
        // ✅ Threat
        const baseThreat = node.threat || rng.randomRange(0, 10);
        
        // ✅ Average Efficiency
        let avgEfficiency = 0;
        crew.forEach(c => {
            avgEfficiency += c.getExploringEfficiency();
        });
        avgEfficiency /= crew.length;
        
        // ✅ Threat after efficiency
        const threatReduction = (avgEfficiency / GAME_CONFIG.EFFICIENCY_MAX_PER_CREW) 
            * GAME_CONFIG.RELIC_THREAT_EFFICIENCY_REDUCTION;
        const threatAfter = baseThreat * (1 - threatReduction);
        
        // ✅ Node HP = value × (threat + threatAfter) × size
        const nodeHP = relicValue * (baseThreat + threatAfter) * sizeMult;
        
        // ✅ Total Efficiency
        let totalEfficiency = 0;
        crew.forEach(c => {
            totalEfficiency += c.getExploringEfficiency();
        });
        
        // ✅ เวลา (นาทีเกม)
        const timeMinutes = nodeHP / Math.max(1, totalEfficiency);
        
        return timeMinutes / 60;
    }
    
    // ✅ Hunting Time (ตามชีต)
    private calculateHuntTime(node: MapNode, crew: Crew[]): number {
        const rng = RandomGenerator.getInstance();        
        // ✅ Monster Data
        const monsterType = node.monsterType || 'A';
        const monsterData = GAME_CONFIG.MONSTER_TYPES[monsterType as keyof typeof GAME_CONFIG.MONSTER_TYPES];
        
        if (!monsterData) return 1;
        
        // ✅ Monster Amount
        const amountRange = GAME_CONFIG.MONSTER_AMOUNT_BY_RANK[monsterData.rank as keyof typeof GAME_CONFIG.MONSTER_AMOUNT_BY_RANK];
        const amount = rng.randomInt(amountRange.min, amountRange.max);
        
        // ✅ Total Monster HP
        const totalHP = monsterData.hp * amount;
        
        // ✅ Total Efficiency
        let totalEfficiency = 0;
        crew.forEach(c => {
            totalEfficiency += c.getHuntingEfficiency();
        });
        
        // ✅ เวลา (นาทีเกม)
        const timeMinutes = totalHP / Math.max(1, totalEfficiency);
        
        return timeMinutes / 60;
    }
    
    // ============================================================
    // UPDATE MISSION
    // ============================================================
    updateMission(mission: MissionState, deltaHours: number): void {
        mission.elapsedHours += deltaHours;
        
        const totalTime = mission.travelOutTime + mission.actionTime + mission.travelBackTime;
        mission.progress = Math.min(1, mission.elapsedHours / totalTime);
        
        // ✅ อัพเดท Phase
        if (mission.elapsedHours < mission.travelOutTime) {
            mission.phase = 'travel_out';
        } else if (mission.elapsedHours < mission.travelOutTime + mission.actionTime) {
            mission.phase = 'action';
        } else if (mission.elapsedHours < totalTime) {
            mission.phase = 'travel_back';
        } else {
            mission.phase = 'complete';
        }
        
        EventBus.emit(EVENTS.MISSION_UPDATED, mission);
    }
    
    // ============================================================
    // COMPLETE MISSION
    // ============================================================
    completeMission(vehicle: Vehicle, targetNode: MapNode): MissionResult {
        const state = GameState;
        const crew = state.getCrew().filter(c => 
            vehicle.assignedCrewIds.includes(c.id)
        );
        
        const result: MissionResult = {
            success: false,
            resourcesGained: {},
            crewLost: [],
            message: '',
        };
        
        switch (targetNode.type) {
            case 'resource':
                result.resourcesGained = this.completeGathering(targetNode, crew);
                result.success = true;
                result.message = `✅ Gathered resources!`;
                break;
            case 'relic':
                const relicResult = this.completeExploring(targetNode, crew);
                result.resourcesGained = relicResult.resources;
                result.crewLost = relicResult.crewLost;
                result.success = relicResult.success;
                result.message = relicResult.message;
                break;
            case 'monster':
                const huntResult = this.completeHunting(targetNode, crew);
                result.resourcesGained = huntResult.resources;
                result.crewLost = huntResult.crewLost;
                result.success = huntResult.success;
                result.message = huntResult.message;
                break;
        }
        
        // ✅ เพิ่มทรัพยากรให้ state
        for (const [type, amount] of Object.entries(result.resourcesGained)) {
            state.addResource(type, amount);
        }
        
        vehicle.completeMission();
        
        // ✅ คืน crew
        crew.forEach(c => {
            c.state = 'idle';
            c.assignedVehicleId = null;
        });
        
        EventBus.emit(EVENTS.MISSION_COMPLETED, result);
        
        return result;
    }
    
    private completeGathering(node: MapNode, crew: Crew[]): { [key: string]: number } {
        const rng = RandomGenerator.getInstance();        const resourceType = node.resourceType || 'wood';
        
        // ✅ คำนวณ yield
        let totalEfficiency = 0;
        crew.forEach(c => totalEfficiency += c.getGatheringEfficiency());
        
        const range = node.resourceAmount || { min: 10, max: 20 };
        const baseAmount = rng.randomInt(range.min, range.max);
        
        const rarityMult = GAME_CONFIG.RESOURCE_NODE_RARITY[node.rarity || 'common'];
        
        // ✅ Yield = base × efficiency multiplier × rarity
        const efficiencyMult = 1 + (totalEfficiency / GAME_CONFIG.EFFICIENCY_MAX_PER_CREW) * 0.5;
        const finalAmount = Math.floor(baseAmount * efficiencyMult * rarityMult);
        
        return { [resourceType]: finalAmount };
    }
    
    private completeExploring(node: MapNode, crew: Crew[]): {
        success: boolean;
        resources: { [key: string]: number };
        crewLost: number[];
        message: string;
    } {
        const rng = RandomGenerator.getInstance();        
        const threat = node.threat || 5;
        const threatReduction = 0.5;
        const threatAfter = threat * (1 - threatReduction);
        
        // ✅ Success Rate
        const successRate = Math.max(0, 100 - threatAfter * 10);
        const success = rng.random() * 100 < successRate;
        
        // ✅ Resources
        const resources: { [key: string]: number } = {};
        if (success) {
            resources['wood'] = rng.randomInt(10, 30);
            resources['stone'] = rng.randomInt(5, 15);
        } else {
            resources['wood'] = rng.randomInt(2, 5);
        }
        
        // ✅ Crew Lost
        const crewLost: number[] = [];
        if (threatAfter > 5) {
            const casualtyChance = (threatAfter - 5) * 10;  // 10% ต่อ threat 1 หน่วย
            crew.forEach(c => {
                if (rng.random() * 100 < casualtyChance) {
                    c.hp = 0;
                    crewLost.push(c.id);
                }
            });
        }
        
        return {
            success,
            resources,
            crewLost,
            message: success 
                ? `🏛️ Found relic resources!` 
                : `⏰ Didn't find relic...`,
        };
    }
    
    private completeHunting(node: MapNode, crew: Crew[]): {
        success: boolean;
        resources: { [key: string]: number };
        crewLost: number[];
        message: string;
    } {
        const rng = RandomGenerator.getInstance();        
        const monsterType = node.monsterType || 'A';
        const monsterData = GAME_CONFIG.MONSTER_TYPES[monsterType as keyof typeof GAME_CONFIG.MONSTER_TYPES];
        
        if (!monsterData) {
            return { success: false, resources: {}, crewLost: [], message: '❌ No monster data' };
        }
        
        const amountRange = GAME_CONFIG.MONSTER_AMOUNT_BY_RANK[monsterData.rank as keyof typeof GAME_CONFIG.MONSTER_AMOUNT_BY_RANK];
        const amount = rng.randomInt(amountRange.min, amountRange.max);
        
        // ✅ Crew Damage
        const crewLost: number[] = [];
        const crewDamage = monsterData.atk * amount;
        
        // ✅ กระจาย damage ให้ crew
        crew.forEach(c => {
            const damage = Math.floor(crewDamage / crew.length);
            c.takeDamage(damage);
            
            if (!c.isAlive()) {
                crewLost.push(c.id);
            }
        });
        
        // ✅ ถ้ายังมี crew รอด → ได้ของ
        const aliveCrew = crew.filter(c => c.isAlive());
        
        if (aliveCrew.length === 0) {
            return {
                success: false,
                resources: {},
                crewLost,
                message: `💀 All crew died fighting monsters!`,
            };
        }
        
        // ✅ Resources
        const resources: { [key: string]: number } = {};
        resources['food'] = rng.randomInt(20, 50);
        resources['hides'] = rng.randomInt(5, 15);
        
        return {
            success: true,
            resources,
            crewLost,
            message: `🎉 Hunted ${amount} monsters! Lost ${crewLost.length} crew.`,
        };
    }
    
    // ============================================================
    // CHECK MISSION
    // ============================================================
    isMissionComplete(mission: MissionState): boolean {
        return mission.phase === 'complete';
    }
    
    checkStranded(mission: MissionState): boolean {
        // ✅ ต้องกลับก่อน 18:00
        const state = GameState;
        const time = state.getTime();
        
        if (time.gameHour < GAME_CONFIG.NIGHT_START_HOUR) {
            return false;
        }
        
        // ✅ ถ้าเป็น night แล้วยังไม่กลับ
        return mission.phase !== 'complete';
    }
}