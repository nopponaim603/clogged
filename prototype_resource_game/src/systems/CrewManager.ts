// src/systems/CrewManager.ts
import { Crew } from '../entities/Crew';
import { CREW_NAMES, PERKS } from '../data/Constants';

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

    generateRandomCrew(hireCost: number): Crew {
        const name = CREW_NAMES[Math.floor(Math.random() * CREW_NAMES.length)];
        const perk = PERKS[Math.floor(Math.random() * PERKS.length)];
        const speed = 0.8 + Math.random() * 0.6;
        const proficiency = 0.7 + Math.random() * 0.8;

        return new Crew({
            id: this.nextId++,
            name: name,
            hp: 80 + Math.floor(Math.random() * 60),
            speed: Math.round(speed * 10) / 10,
            gatheringProficiency: Math.round((proficiency + Math.random() * 0.3) * 10) / 10,
            searchingProficiency: Math.round((proficiency + Math.random() * 0.3) * 10) / 10,
            huntingProficiency: Math.round((proficiency + Math.random() * 0.3) * 10) / 10,
            perks: [perk],
            hireCost: hireCost,
            position: { x: 0, y: 0 }
        });
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