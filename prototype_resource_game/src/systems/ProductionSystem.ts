// src/systems/ProductionSystem.ts
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Facility } from '../entities/Facility';
import { Crew } from '../entities/Crew';
import { Systems } from '../main';

export interface Recipe {
    id: string;
    input: { [key: string]: number };
    output: { [key: string]: number };
    timePerUnit: number;  // game hours
}

export class ProductionSystem {
    // Recipes
    private recipes: Recipe[] = [
        // Factory recipes
        { id: 'plank', input: { wood: 2 }, output: { plank: 1 }, timePerUnit: 1 },
        { id: 'brick', input: { stone: 2 }, output: { brick: 1 }, timePerUnit: 1.5 },
        { id: 'iron_bar', input: { iron_ore: 3 }, output: { iron_bar: 1 }, timePerUnit: 2 },
        { id: 'copper_bar', input: { copper_ore: 3 }, output: { copper_bar: 1 }, timePerUnit: 2 },
        { id: 'aluminum_sheet', input: { aluminum_ore: 4 }, output: { aluminum_sheet: 1 }, timePerUnit: 2.5 },
        { id: 'rubber_sheet', input: { rubber: 2 }, output: { rubber_sheet: 1 }, timePerUnit: 1 },
        { id: 'gear', input: { iron_bar: 2, copper_bar: 1 }, output: { gear: 1 }, timePerUnit: 3 },
        { id: 'circuit', input: { aluminum_sheet: 2, copper_bar: 1 }, output: { circuit: 1 }, timePerUnit: 4 },
    ];
    
    // ============================================================
    // UPDATE (Called every game hour)
    // ============================================================
    onHourTick(): void {
        const state = GameState;
        const time = state.getTime();
        
        if (time.isPaused) return;
        if (state.isGameOver()) return;
        
        // ✅ Process facilities
        const facilities = this.getAllFacilities();
        
        facilities.forEach(facility => {
            if (!facility.isActive) return;
            if (!facility.hasWorker()) return;
            
            switch (facility.type) {
                case 'kitchen':
                    this.processKitchen(facility, 1);
                    break;
                case 'factory':
                    this.processFactory(facility, 1);
                    break;
                case 'workshop':
                    this.processWorkshop(facility, 1);
                    break;
                case 'engine':
                    this.processEngine(facility, 1);
                    break;
                case 'turret':
                    // Turret ไม่มี production
                    break;
            }
        });
    }
    
    // ============================================================
    // GET FACILITIES (from BaseSystem)
    // ============================================================
    private getAllFacilities(): Facility[] {
        // TODO: integrate with BaseSystem
        // ตอนนี้ return empty ก่อน
        return [];
    }
    
    // ============================================================
    // KITCHEN
    // ============================================================
    private processKitchen(facility: Facility, deltaHours: number): void {
        const state = GameState;
        
        // ✅ คำนวณเวลา
        const baseTime = GAME_CONFIG.FACILITY_TYPES.kitchen.production?.baseTimePerFood || 3;
        const timeReduction = (facility.level - 1) * 
            (GAME_CONFIG.FACILITY_TYPES.kitchen.production?.timeReductionPerLevel || 0.5);
        let timePerFood = Math.max(0.5, baseTime - timeReduction);
        
        // ✅ Cook perk
        const workers = this.getWorkers(facility);
        const perkBonus = this.getCookPerkBonus(workers);
        timePerFood *= (1 - perkBonus);
        
        // ✅ Progress
        facility.productionProgress += deltaHours / timePerFood;
        
        // ✅ ผลิต
        while (facility.productionProgress >= 1) {
            facility.productionProgress -= 1;
            
            // ✅ ตรวจสอบ input (ใช้ food โดยตรง)
            // Kitchen ใช้ "ingredients" (สมมติเป็น food)
            if (state.getResource('food') < 1) {
                // ไม่มี input → หยุด
                facility.productionProgress = 0;
                break;
            }
            
            // ✅ หัก input
            state.removeResource('food', 1);
            
            // ✅ เพิ่ม output
            state.addResource('food', 1);  // สมมติ
        }
    }
    
    // ============================================================
    // FACTORY
    // ============================================================
    private processFactory(facility: Facility, deltaHours: number): void {
        const state = GameState;
        
        // ✅ สุ่ม recipe
        const recipe = this.getActiveRecipe(facility);
        if (!recipe) return;
        
        // ✅ ตรวจสอบ input
        for (const [type, amount] of Object.entries(recipe.input)) {
            if (state.getResource(type) < amount) {
                return;  // input ไม่พอ
            }
        }
        
        // ✅ คำนวณเวลา
        let timePerUnit = recipe.timePerUnit;
        
        // ✅ Engineer perk
        const workers = this.getWorkers(facility);
        const perkBonus = this.getEngineerPerkBonus(workers);
        timePerUnit *= (1 - perkBonus);
        
        // ✅ Progress
        facility.productionProgress += deltaHours / timePerUnit;
        
        // ✅ ผลิต
        if (facility.productionProgress >= 1) {
            facility.productionProgress -= 1;
            
            // ✅ หัก input
            for (const [type, amount] of Object.entries(recipe.input)) {
                state.removeResource(type, amount);
            }
            
            // ✅ เพิ่ม output
            for (const [type, amount] of Object.entries(recipe.output)) {
                state.addResource(type, amount);
            }
            
            console.log(`🏭 Factory produced: ${recipe.id}`);
        }
    }
    
    private getActiveRecipe(facility: Facility): Recipe | null {
        // ✅ ใช้ recipe แรกที่ input พอ
        const state = GameState;
        
        for (const recipe of this.recipes) {
            let canCraft = true;
            
            for (const [type, amount] of Object.entries(recipe.input)) {
                if (state.getResource(type) < amount) {
                    canCraft = false;
                    break;
                }
            }
            
            if (canCraft) return recipe;
        }
        
        return null;
    }
    
    // ============================================================
    // WORKSHOP
    // ============================================================
    private processWorkshop(facility: Facility, deltaHours: number): void {
        // TODO: implement crafting
        // ตอนนี้ข้ามไปก่อน
    }
    
    // ============================================================
    // ENGINE ROOM
    // ============================================================
    private processEngine(facility: Facility, deltaHours: number): void {
        // ✅ Engine Room → ลด decay
        // จริงๆ จัดการใน EngineSystem
        
        // ✅ แค่ update WorkerAssigned
        const state = GameState;
        const engine = state.getEngine();
        engine.isWorkerAssigned = facility.hasWorker();
    }
    
    // ============================================================
    // WORKERS
    // ============================================================
    private getWorkers(facility: Facility): Crew[] {
        const state = GameState;
        return facility.workers
            .map(id => state.getCrew().find(c => c.id === id))
            .filter((c): c is Crew => c !== undefined && c.isAlive());
    }
    
    private getCookPerkBonus(workers: Crew[]): number {
        for (const worker of workers) {
            if (worker.hasPerk('cook_iii')) return 0.6;
            if (worker.hasPerk('cook_ii')) return 0.4;
            if (worker.hasPerk('cook_i')) return 0.2;
        }
        return 0;
    }
    
    private getEngineerPerkBonus(workers: Crew[]): number {
        for (const worker of workers) {
            if (worker.hasPerk('engineer_iii')) return 0.6;
            if (worker.hasPerk('engineer_ii')) return 0.4;
            if (worker.hasPerk('engineer_i')) return 0.2;
        }
        return 0;
    }
    
    private getBlacksmithPerkBonus(workers: Crew[]): number {
        for (const worker of workers) {
            if (worker.hasPerk('blacksmith_iii')) return 0.7;
            if (worker.hasPerk('blacksmith_ii')) return 0.5;
            if (worker.hasPerk('blacksmith_i')) return 0.3;
        }
        return 0;
    }
    
    // ============================================================
    // RECIPE QUERY
    // ============================================================
    getAllRecipes(): Recipe[] {
        return this.recipes;
    }
    
    getRecipesForFacility(type: string): Recipe[] {
        // ✅ ทุก recipe ใช้ factory ได้
        return this.recipes;
    }
}