// src/systems/CrewManager.ts
import { Crew } from '../entities/Crew';
import { CREW_NAMES, PERKS } from '../data/Constants';
import { CREW_TEMPLATES } from '../data/CrewData';

export class CrewManager {
    public crews: Crew[];
    public availableCrews: Crew[];
    public busyCrews: Crew[];
    private nextId: number;

    constructor() {
        this.crews = [];
        this.availableCrews = [];
        this.busyCrews = [];
        this.nextId = 1;
    }

    // ✅ ใช้ CREW_TEMPLATES แทนการสุ่ม
    generateRandomCrew(hireCost: number): Crew {
        const template = CREW_TEMPLATES[Math.floor(Math.random() * CREW_TEMPLATES.length)];
        
        return new Crew({
            id: this.nextId++,
            name: template.name,
            maxHp: template.maxHp,
            baseSpeed: template.baseSpeed,
            baseGathering: template.baseGathering,
            baseSearching: template.baseSearching,
            baseHunting: template.baseHunting,
            perks: template.perks,
            hireCost: hireCost || template.cost,
            position: { x: 0, y: 0 }
        });
    }

    // ✅ ดึง Crew Templates ทั้งหมด
    getCrewTemplates(): typeof CREW_TEMPLATES {
        return CREW_TEMPLATES;
    }

    hireCrew(crew: Crew, points: number): { success: boolean; remainingPoints: number; message?: string } {
        if (points >= crew.hireCost) {
            this.crews.push(crew);
            this.availableCrews.push(crew);
            return { success: true, remainingPoints: points - crew.hireCost };
        }
        return { success: false, remainingPoints: points, message: 'Not enough points!' };
    }

    assignMission(crewId: number, targetNode: any): { success: boolean; message?: string } {
        const crew = this.crews.find(c => c.id === crewId);
        if (!crew) return { success: false, message: 'Crew not found!' };
        if (!crew.isAlive) return { success: false, message: 'Crew is dead!' };
        if (!this.availableCrews.includes(crew)) return { success: false, message: 'Crew is busy!' };

        crew.isBusy = true;
        this.availableCrews = this.availableCrews.filter(c => c.id !== crewId);
        this.busyCrews.push(crew);

        return { success: true };
    }

    completeMission(crewId: number): void {
        const crew = this.crews.find(c => c.id === crewId);
        if (crew) {
            crew.isBusy = false;
            this.busyCrews = this.busyCrews.filter(c => c.id !== crewId);
            if (crew.isAlive) {
                this.availableCrews.push(crew);
            }
        }
    }

    getAvailableCount(): number {
        return this.availableCrews.length;
    }

    getBusyCount(): number {
        return this.busyCrews.length;
    }

    getAllAlive(): Crew[] {
        return this.crews.filter(c => c.isAlive);
    }

    getCrewById(id: number): Crew | undefined {
        return this.crews.find(c => c.id === id);
    }

    reset(): void {
        this.crews = [];
        this.availableCrews = [];
        this.busyCrews = [];
        this.nextId = 1;
    }
}