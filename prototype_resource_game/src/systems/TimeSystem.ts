// src/systems/TimeSystem.ts
import { GAME_CONFIG } from '../config';

export class TimeSystem {
    public day: number;
    public dayTimeLimit: number;
    public isPlanningPhase: boolean;
    public isExecuting: boolean;
    public isNightPhase: boolean;
    public executionTimer: Phaser.Time.TimerEvent | null;
    public onDayEnd: () => void;
    public onNightEnd: () => void;
    public elapsedTime: number = 0;
    public onSimulateStep: (() => void) | null = null;
    private isEndingDay: boolean = false; // ✅ ป้องกันการเรียกซ้ำ

    constructor() {
        this.day = 1;
        this.dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;
        this.isPlanningPhase = true;
        this.isExecuting = false;
        this.isNightPhase = false;
        this.executionTimer = null;
        this.onDayEnd = () => {};
        this.onNightEnd = () => {};
        this.onSimulateStep = null;
    }

    startDay(scene: Phaser.Scene): void {
        this.isPlanningPhase = true;
        this.isNightPhase = false;
        this.elapsedTime = 0;
        this.dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;
        this.isEndingDay = false;
    }

    startExecution(scene: Phaser.Scene): void {
        this.isExecuting = true;
        this.isPlanningPhase = false;
        this.elapsedTime = 0;
        this.isEndingDay = false;
        
        if (this.executionTimer) {
            this.executionTimer.remove();
        }
        
        // ✅ Simulation: อัพเดททุก 1 วินาที เพิ่ม TIME_UNIT_PER_SECOND หน่วยเวลา
        this.executionTimer = scene.time.addEvent({
            delay: 1000,
            callback: this.updateTime,
            callbackScope: this,
            loop: true
        });
        
        this.simulateStep();
    }

    updateTime(): void {
        // ✅ ถ้ากำลังจะจบวัน หรือจบวันแล้ว ไม่ต้องทำอะไร
        if (this.isEndingDay || this.isNightPhase) return;
        
        // ✅ Simulation: เพิ่ม TIME_UNIT_PER_SECOND หน่วยเวลา
        this.elapsedTime += GAME_CONFIG.TIME_UNIT_PER_SECOND;
        
        // ✅ ถ้าเวลาถึงหรือเกิน limit
        if (this.elapsedTime >= this.dayTimeLimit) {
            this.elapsedTime = this.dayTimeLimit;
            this.simulateStep(); // เรียก simulateStep ครั้งสุดท้าย
            this.endDay();
            return;
        }
        
        this.simulateStep();
    }

    private simulateStep(): void {
        // ✅ ถ้ากำลังจะจบวัน ไม่ต้อง simulate
        if (this.isEndingDay) return;
        
        if (this.onSimulateStep) {
            this.onSimulateStep();
        }
    }

    getRemainingTime(): number {
        return Math.max(0, this.dayTimeLimit - this.elapsedTime);
    }

    endExecution(): void {
        this.isExecuting = false;
        this.isPlanningPhase = true;
        if (this.executionTimer) {
            this.executionTimer.remove();
            this.executionTimer = null;
        }
    }

    endDay(): void {
        // ✅ ป้องกันการเรียกซ้ำ
        if (this.isEndingDay || this.isNightPhase) return;
        
        this.isEndingDay = true;
        this.isPlanningPhase = false;
        this.isNightPhase = true;
        
        if (this.executionTimer) {
            this.executionTimer.remove();
            this.executionTimer = null;
        }
        
        this.onDayEnd();
    }

    startNight(scene: Phaser.Scene): void {
        this.isNightPhase = true;
        // ✅ ตรวจสอบว่า night phase ยังไม่ถูกเรียก
        scene.time.delayedCall(GAME_CONFIG.NIGHT_DURATION, () => {
            this.endNight();
        });
    }

    endNight(): void {
        this.isNightPhase = false;
        this.day++;
        this.isEndingDay = false;
        this.onNightEnd();
    }

    getTimeString(): string {
        return `${Math.floor(this.dayTimeLimit - this.elapsedTime)}`;
    }

    getElapsedTime(): string {
        return `${Math.floor(this.elapsedTime)}`;
    }

    getFormattedTime(): string {
        const remaining = this.dayTimeLimit - this.elapsedTime;
        return `${Math.floor(remaining)}`;
    }
}