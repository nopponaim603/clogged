// src/entities/Crew.ts
import { Crew as ICrew, CrewState } from '../state/types';
import { GAME_CONFIG } from '../config';
import { RandomGenerator } from '../utils/RandomGenerator';

export class Crew implements ICrew {
    public id: number;
    public name: string;
    public hp: number;
    public maxHp: number;
    public def: number;
    public gearDef: number;
    public dodge: number;
    
    // Stats (0-200)
    public gathering: number;
    public exploring: number;
    public hunting: number;
    
    // Gear
    public gearGathering: number;
    public gearExploring: number;
    public gearHunting: number;
    
    // Perks
    public perks: string[];
    
    // State
    public state: CrewState;
    public assignedFacilityId: string | null;
    public assignedVehicleId: string | null;
    
    constructor(data: Partial<Crew>) {
        this.id = data.id || 0;
        this.name = data.name || 'Crew';
        this.maxHp = data.maxHp || 10000;
        this.hp = data.hp || this.maxHp;
        this.def = data.def || 50;
        this.gearDef = data.gearDef || 50;
        this.dodge = data.dodge || 15;
        
        this.gathering = data.gathering || 50;
        this.exploring = data.exploring || 50;
        this.hunting = data.hunting || 50;
        
        this.gearGathering = data.gearGathering || 5;
        this.gearExploring = data.gearExploring || 5;
        this.gearHunting = data.gearHunting || 50;
        
        this.perks = data.perks || [];
        
        this.state = data.state || 'idle';
        this.assignedFacilityId = data.assignedFacilityId || null;
        this.assignedVehicleId = data.assignedVehicleId || null;
    }
    
    // ============================================================
    // STATIC FACTORY
    // ============================================================
    static create(id: number, name: string, cost: number): Crew {
        const rng = RandomGenerator.getInstance();
        
        const maxHp = rng.randomCrewHp();
        const def = rng.randomCrewDef();
        const gearDef = rng.randomGearDef();
        const dodge = rng.randomCrewDodge();
        
        const gathering = rng.randomCrewStat();
        const exploring = rng.randomCrewStat();
        const hunting = rng.randomCrewStat();
        
        const gearGathering = rng.randomGearGathering();
        const gearExploring = rng.randomGearExploring();
        const gearHunting = rng.randomGearHunting();
        
        return new Crew({
            id,
            name,
            maxHp,
            hp: maxHp,
            def,
            gearDef,
            dodge,
            gathering,
            exploring,
            hunting,
            gearGathering,
            gearExploring,
            gearHunting,
            perks: [],
        });
    }
    
    // ============================================================
    // EFFICIENCY CALCULATIONS (ตามชีต)
    // ============================================================
    
    // ✅ Gathering Efficiency: (stat + gear) × perk multiplier
    getGatheringEfficiency(): number {
        const base = this.gathering + this.gearGathering;
        const perkMult = this.getPerkMultiplier('gathering');
        return base * perkMult;
    }
    
    // ✅ Exploring Efficiency
    getExploringEfficiency(): number {
        const base = this.exploring + this.gearExploring;
        const perkMult = this.getPerkMultiplier('exploring');
        return base * perkMult;
    }
    
    // ✅ Hunting Efficiency
    getHuntingEfficiency(): number {
        const base = this.hunting + this.gearHunting;
        const perkMult = this.getPerkMultiplier('hunting');
        return base * perkMult;
    }
    
    // ✅ Total DEF
    getTotalDef(): number {
        return this.def + this.gearDef;
    }
    
    // ============================================================
    // PERK MULTIPLIER
    // ============================================================
    private getPerkMultiplier(statType: 'gathering' | 'exploring' | 'hunting'): number {
        let multiplier = 1.0;
        
        // ✅ ถ้ามี perk ที่ตรงกับ statType → ×1.5
        const perkMap: { [key: string]: string[] } = {
            gathering: ['scout_i', 'scout_ii', 'scout_iii'],
            exploring: ['scout_i', 'scout_ii', 'scout_iii'],
            hunting: ['soldier_i', 'soldier_ii', 'soldier_iii'],
        };
        
        const relevantPerks = perkMap[statType] || [];
        if (this.perks.some(p => relevantPerks.includes(p))) {
            multiplier *= 1.5;
        }
        
        return multiplier;
    }
    
    // ============================================================
    // COMBAT
    // ============================================================
    
    // ✅ รับดาเมจ
    takeDamage(amount: number): number {
        // ✅ Dodge check
        if (Math.random() * 100 < this.dodge) {
            return 0;  // หลบได้
        }
        
        // ✅ ลดดาเมจด้วย DEF
        const actualDamage = Math.max(1, amount - this.getTotalDef());
        this.hp = Math.max(0, this.hp - actualDamage);
        
        return actualDamage;
    }
    
    isAlive(): boolean {
        return this.hp > 0;
    }
    
    // ============================================================
    // HEAL
    // ============================================================
    heal(amount: number): void {
        this.hp = Math.min(this.maxHp, this.hp + amount);
    }
    
    // ============================================================
    // PERK MANAGEMENT
    // ============================================================
    hasPerk(perkId: string): boolean {
        return this.perks.includes(perkId);
    }
    
    addPerk(perkId: string): void {
        if (!this.perks.includes(perkId)) {
            this.perks.push(perkId);
        }
    }
    
    // ============================================================
    // FACILITY PERK
    // ============================================================
    
    // ✅ ตรวจสอบ perk ที่เกี่ยวกับ facility
    getFacilityPerkBonus(facilityType: string): number {
        const perkMap: { [key: string]: { [key: string]: number } } = {
            engine: {
                mechanist_i: 0.1,
                mechanist_ii: 0.3,
                mechanist_iii: 0.5,
            },
            kitchen: {
                cook_i: 0.2,
                cook_ii: 0.4,
                cook_iii: 0.6,
            },
            factory: {
                engineer_i: 0.2,
                engineer_ii: 0.4,
                engineer_iii: 0.6,
            },
            workshop: {
                blacksmith_i: 0.3,
                blacksmith_ii: 0.5,
                blacksmith_iii: 0.7,
            },
            turret: {
                gunslinger_i: 0.2,
                gunslinger_ii: 0.4,
                gunslinger_iii: 0.6,
            },
        };
        
        const bonuses = perkMap[facilityType] || {};
        
        // ✅ หา perk สูงสุด
        for (const perk of this.perks) {
            if (bonuses[perk] !== undefined) {
                return bonuses[perk];
            }
        }
        
        return 0;
    }
    
    // ============================================================
    // SERIALIZATION
    // ============================================================
    toJSON(): object {
        return {
            id: this.id,
            name: this.name,
            hp: this.hp,
            maxHp: this.maxHp,
            def: this.def,
            gearDef: this.gearDef,
            dodge: this.dodge,
            gathering: this.gathering,
            exploring: this.exploring,
            hunting: this.hunting,
            gearGathering: this.gearGathering,
            gearExploring: this.gearExploring,
            gearHunting: this.gearHunting,
            perks: [...this.perks],
            state: this.state,
            assignedFacilityId: this.assignedFacilityId,
            assignedVehicleId: this.assignedVehicleId,
        };
    }
    
    static fromJSON(data: any): Crew {
        return new Crew(data);
    }
}