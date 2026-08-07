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
    private isEndingDay: boolean = false;
    public worldTime: number = 0;
    public speed: number = 1;
    private updateInterval: number = 1000;
    
    // ✅ เก็บ Scene ไว้ใช้ใน setSpeed
    private currentScene: Phaser.Scene | null = null;

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
        this.speed = GAME_CONFIG.DEFAULT_SPEED || 1;
        this.currentScene = null;
    }

    startDay(scene: Phaser.Scene): void {
        this.isPlanningPhase = true;
        this.isNightPhase = false;
        this.worldTime = 0;
        this.dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;
        this.isEndingDay = false;
        this.currentScene = scene;
    }

    startExecution(scene: Phaser.Scene): void {
        this.isExecuting = true;
        this.isPlanningPhase = false;
        this.worldTime = 0;
        this.isEndingDay = false;
        this.currentScene = scene;
        
        if (this.executionTimer) {
            this.executionTimer.remove();
        }
        
        // ✅ ใช้ interval ตาม speed
        const interval = this.updateInterval / this.speed;
        this.executionTimer = scene.time.addEvent({
            delay: interval,
            callback: this.updateTime,
            callbackScope: this,
            loop: true
        });
        
        this.simulateStep();
    }

    // ✅ เปลี่ยน Speed
    setSpeed(newSpeed: number): void {
        this.speed = Math.max(1, newSpeed);
        // ✅ ถ้ากำลัง execute อยู่ ให้ restart timer
        if (this.isExecuting && this.executionTimer && this.currentScene) {
            this.executionTimer.remove();
            const interval = this.updateInterval / this.speed;
            this.executionTimer = this.currentScene.time.addEvent({
                delay: interval,
                callback: this.updateTime,
                callbackScope: this,
                loop: true
            });
        }
    }

    // ✅ Skip - ให้ไปจบวันทันที
    skipDay(): void {
        if (this.isExecuting || this.isPlanningPhase) {
            this.worldTime = this.dayTimeLimit;
            this.simulateStep();
            this.endDay();
        }
    }

    updateTime(): void {
        if (this.isEndingDay || this.isNightPhase) return;
        
        // ✅ เพิ่มเวลาตาม speed
        this.worldTime += GAME_CONFIG.TIME_UNIT_PER_SECOND * this.speed;
        
        if (this.worldTime >= this.dayTimeLimit) {
            this.worldTime = this.dayTimeLimit;
            this.simulateStep();
            this.endDay();
            return;
        }
        
        this.simulateStep();
    }

    private simulateStep(): void {
        if (this.isEndingDay) return;
        if (this.onSimulateStep) {
            this.onSimulateStep();
        }
    }

    getRemainingTime(): number {
        return Math.max(0, this.dayTimeLimit - this.worldTime);
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
        this.currentScene = scene;
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
        return `${Math.floor(this.dayTimeLimit - this.worldTime)}`;
    }

    getElapsedTime(): string {
        return `${Math.floor(this.worldTime)}`;
    }

    getFormattedTime(): string {
        const remaining = this.dayTimeLimit - this.worldTime;
        return `${Math.floor(remaining)}`;
    }
}