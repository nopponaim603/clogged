// src/systems/CrewSystem.ts
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Crew } from '../entities/Crew';
import { RandomGenerator } from '../utils/RandomGenerator';
import { Vehicle } from '../entities/Vehicle';

export class CrewSystem {
    private nextId: number = 1;
    private crewNames: string[] = [
        'Sarah', 'John', 'Emma', 'Mike', 'Lisa', 'David', 'Anna', 'Tom',
        'Katie', 'James', 'Alex', 'Mia', 'Noah', 'Olivia', 'Liam', 'Sophia',
        'Ethan', 'Ava', 'Lucas', 'Isabella',
    ];
    
    // ============================================================
    // INITIALIZE
    // ============================================================
    init(): void {
        this.nextId = 1;
        // Crew จะถูกสร้างผ่าน RecruitmentScene
    }
    
    // ============================================================
    // GENERATE POOL (สำหรับ RecruitmentScene)
    // ============================================================
    generateCrewPool(): Crew[] {
        const rng = RandomGenerator.getInstance();
        const count = rng.randomInt(
            GAME_CONFIG.RECRUIT_POOL_MIN,
            GAME_CONFIG.RECRUIT_POOL_MAX
        );
        
        const pool: Crew[] = [];
        const usedNames = new Set<string>();
        
        for (let i = 0; i < count; i++) {
            // ✅ สุ่มชื่อ
            let name: string;
            do {
                name = rng.pick(this.crewNames);
            } while (usedNames.has(name) && usedNames.size < this.crewNames.length);
            usedNames.add(name);
            
            // ✅ สร้าง Crew
            const crew = Crew.create(this.nextId++, name, 0);
            
            // ✅ สุ่ม Perk
            crew.perks = this.generateRandomPerks();
            
            pool.push(crew);
        }
        
        return pool;
    }
    
    // ============================================================
    // PERK GENERATION
    // ============================================================
    private generateRandomPerks(): string[] {
        const rng = RandomGenerator.getInstance();
        const perks: string[] = [];
        
        // ✅ 10% โอกาสได้ perk
        if (rng.random() > 0.9) {
            const perkTiers = ['i', 'ii', 'iii'];
            const perkTypes = ['mechanist', 'cook', 'engineer', 'blacksmith', 'gunslinger', 'scout', 'soldier'];
            
            const type = rng.pick(perkTypes);
            const tier = rng.pick(perkTiers);
            
            perks.push(`${type}_${tier}`);
        }
        
        return perks;
    }
    
    // ============================================================
    // HIRE
    // ============================================================
    hireCrew(crew: Crew): boolean {
        const state = GameState;
        const crewList = state.getCrew();
        
        // ✅ เช็คจำนวน slot
        if (crewList.length >= this.getMaxSlots()) {
            return false;
        }
        
        // ✅ เพิ่มเข้า crew list
        crewList.push(crew);
        state.setCrew(crewList);
        
        EventBus.emit('crew:hired', crew);
        
        return true;
    }
    
    fireCrew(crewId: number): boolean {
        const state = GameState;
        const crewList = state.getCrew();
        const index = crewList.findIndex(c => c.id === crewId);
        
        if (index === -1) return false;
        
        // ✅ ถ้า crew กำลังทำ mission → ห้าม fire
        if (crewList[index].state === 'dispatched') return false;
        
        crewList.splice(index, 1);
        state.setCrew(crewList);
        
        EventBus.emit('crew:fired', crewId);
        
        return true;
    }
    
    // ============================================================
    // SLOTS
    // ============================================================
    getMaxSlots(): number {
        // ✅ คำนวณจาก upgrade
        // (TODO: implement upgrade)
        return GAME_CONFIG.BASE_CREW_SLOTS;
    }
    
    getAvailableSlots(): number {
        const state = GameState;
        return this.getMaxSlots() - state.getCrew().length;
    }
    
    // ============================================================
    // QUERY
    // ============================================================
    getCrewById(id: number): Crew | undefined {
        return GameState.getCrew().find(c => c.id === id);
    }
    
    getAvailableCrew(): Crew[] {
        return GameState.getCrew().filter(c => 
            c.state === 'idle' && c.isAlive()
        );
    }
    
    getAllAlive(): Crew[] {
        return GameState.getCrew().filter(c => c.isAlive());
    }
    
    getCrewInVehicle(vehicleId: string): Crew[] {
        const state = GameState;
        const vehicle = state.getVehicles().find((v: Vehicle) => v.id === vehicleId);  // ✅ type
        
        if (!vehicle) return [];
        
        return vehicle.assignedCrewIds
            .map((id: number) => this.getCrewById(id))  // ✅ type
            .filter((c): c is Crew => c !== undefined);
    }
    
    // ============================================================
    // STATE MANAGEMENT
    // ============================================================
    setCrewState(crewId: number, state: Crew['state']): void {
        const crew = this.getCrewById(crewId);
        if (!crew) return;
        
        crew.state = state;
    }
    
    // ============================================================
    // HEAL
    // ============================================================
    healAll(amount: number): void {
        GameState.getCrew().forEach(crew => {
            if (crew.isAlive()) {
                crew.heal(amount);
            }
        });
    }
    
    // ============================================================
    // UPDATE (Called every game hour)
    // ============================================================
    onHourTick(): void {
        const state = GameState;
        const time = state.getTime();
        
        if (time.isPaused) return;
        if (state.isGameOver()) return;
        
        // ✅ เช็ค Crew Death
        this.checkCrewDeath();
    }
    
    private checkCrewDeath(): void {
        const state = GameState;
        const crew = state.getCrew();
        
        crew.forEach(c => {
            if (!c.isAlive()) {
                c.state = 'dead';
                EventBus.emit(EVENTS.CREW_DIED, c);
            }
        });
        
        // ✅ ถ้า crew ตายหมด → Game Over
        const aliveCrew = crew.filter(c => c.isAlive());
        if (aliveCrew.length === 0 && crew.length > 0) {
            state.triggerGameOver('all_crew_dead', false);
        }
    }
    
    // ============================================================
    // STATS
    // ============================================================
    getTotalCrewCount(): number {
        return GameState.getCrew().length;
    }
    
    getAliveCrewCount(): number {
        return this.getAllAlive().length;
    }
}