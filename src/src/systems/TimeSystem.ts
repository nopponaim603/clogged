// src/systems/TimeSystem.ts
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { GamePhase } from '../state/types';

export class TimeSystem {
    private scene: Phaser.Scene | null = null;
    private timerEvent: Phaser.Time.TimerEvent | null = null;
    private lastUpdateTime: number = 0;
    private accumulatedMinutes: number = 0;
    
    // ============================================================
    // INITIALIZE
    // ============================================================
    init(scene: Phaser.Scene): void {
        this.scene = scene;
        this.lastUpdateTime = Date.now();
        this.startTimer();
    }

    getScene(): Phaser.Scene | null {
        return this.scene;
    }
    
    private startTimer(): void {
        if (this.timerEvent) {
            this.timerEvent.remove();
        }
        
        this.timerEvent = this.scene!.time.addEvent({
            delay: 100,
            callback: this.update,
            callbackScope: this,
            loop: true
        });
    }
    
    // ============================================================
    // UPDATE (Called every 100ms)
    // ============================================================
    private update(): void {
        const state = GameState;  // ✅ ลบ .getInstance()
        const time = state.getTime();
        
        if (time.isPaused) return;
        if (state.isGameOver()) return;
        
        const now = Date.now();
        const deltaMs = now - this.lastUpdateTime;
        this.lastUpdateTime = now;
        
        const gameMinutesToAdd = (deltaMs / 1000) 
            * (60 / GAME_CONFIG.REAL_SECONDS_PER_GAME_HOUR) 
            * time.timeScale;
        
        this.accumulateGameMinutes(gameMinutesToAdd);
    }
    
    // ============================================================
    // ACCUMULATE
    // ============================================================
    private accumulateGameMinutes(minutes: number): void {
        this.accumulatedMinutes += minutes;
        
        while (this.accumulatedMinutes >= 1) {
            this.accumulatedMinutes -= 1;
            this.tickOneGameMinute();
        }
    }
    
    private tickOneGameMinute(): void {
        const state = GameState;
        const time = state.getTime();
        
        time.gameMinute++;
        
        // ✅ Emit event ทุกนาทีเกม
        EventBus.emit('time:minuteTick', 1);
        
        if (time.gameMinute >= 60) {
            time.gameMinute = 0;
            this.tickOneGameHour();
        }
    }
    
    private tickOneGameHour(): void {
        const state = GameState;
        const time = state.getTime();
        
        time.gameHour++;
        
        this.checkPhaseChange();
        
        if (time.gameHour >= 24) {
            time.gameHour = 0;
            this.tickNewDay();
        }
        
        if (time.gameDay > GAME_CONFIG.ESCALATION_START_DAY) {
            time.escalationLevel++;
        }
        
        EventBus.emit(EVENTS.TIME_HOUR_TICK, {
            hour: time.gameHour,
            day: time.gameDay,
            escalationLevel: time.escalationLevel,
        });
    }
    
    private tickNewDay(): void {
        const state = GameState;
        const time = state.getTime();
        
        time.gameDay++;
        
        EventBus.emit(EVENTS.TIME_NEW_DAY, {
            day: time.gameDay,
        });
    }
    
    // ============================================================
    // PHASE CHANGE
    // ============================================================
    private checkPhaseChange(): void {
        const state = GameState;
        const time = state.getTime();
        
        let newPhase: GamePhase = time.phase;
        
        if (time.gameHour >= GAME_CONFIG.DAY_START_HOUR 
            && time.gameHour < GAME_CONFIG.NIGHT_START_HOUR) {
            newPhase = 'day';
        } else {
            newPhase = 'night';
        }
        
        if (newPhase !== time.phase) {
            time.phase = newPhase;
            
            EventBus.emit(EVENTS.TIME_PHASE_CHANGE, {
                phase: newPhase,
                hour: time.gameHour,
                day: time.gameDay,
            });
        }
    }
    
    // ============================================================
    // TIME SCALE CONTROL
    // ============================================================
    setTimeScale(scale: number): void {
        const state = GameState;
        const time = state.getTime();
        
        if (!GAME_CONFIG.TIME_SCALES.includes(scale)) {
            console.warn(`Invalid time scale: ${scale}`);
            return;
        }
        
        time.timeScale = scale;
        EventBus.emit(EVENTS.TIME_SCALE_CHANGE, scale);
    }
    
    cycleTimeScale(): void {
        const state = GameState;
        const time = state.getTime();
        
        const currentIndex = GAME_CONFIG.TIME_SCALES.indexOf(time.timeScale);
        const nextIndex = (currentIndex + 1) % GAME_CONFIG.TIME_SCALES.length;
        const nextScale = GAME_CONFIG.TIME_SCALES[nextIndex];
        
        this.setTimeScale(nextScale);
    }
    
    // ============================================================
    // PAUSE CONTROL
    // ============================================================
    pause(): void {
        const state = GameState;
        const time = state.getTime();
        
        if (time.isPaused) return;
        
        time.isPaused = true;
        EventBus.emit(EVENTS.TIME_PAUSE);
    }
    
    resume(): void {
        const state = GameState;
        const time = state.getTime();
        
        if (!time.isPaused) return;
        
        time.isPaused = false;
        this.lastUpdateTime = Date.now();
        EventBus.emit(EVENTS.TIME_RESUME);
    }
    
    togglePause(): void {
        const state = GameState;
        const time = state.getTime();
        
        if (time.isPaused) {
            this.resume();
        } else {
            this.pause();
        }
    }
    
    // ============================================================
    // GETTERS
    // ============================================================
    getCurrentHour(): number {
        return GameState.getTime().gameHour;
    }
    
    getCurrentMinute(): number {
        return GameState.getTime().gameMinute;
    }
    
    getCurrentDay(): number {
        return GameState.getTime().gameDay;
    }
    
    getCurrentPhase(): GamePhase {
        return GameState.getTime().phase;
    }
    
    getTimeScale(): number {
        return GameState.getTime().timeScale;
    }
    
    getTimeString(): string {
        const time = GameState.getTime();
        const hh = String(time.gameHour).padStart(2, '0');
        const mm = String(time.gameMinute).padStart(2, '0');
        return `${hh}:${mm}`;
    }
    
    getDayString(): string {
        return `Day ${GameState.getTime().gameDay}`;
    }
    
    isDay(): boolean {
        return this.getCurrentPhase() === 'day';
    }
    
    isNight(): boolean {
        return this.getCurrentPhase() === 'night';
    }
    
    // ============================================================
    // TIME CALCULATIONS
    // ============================================================
    gameHoursToRealMs(hours: number): number {
        return hours * GAME_CONFIG.REAL_SECONDS_PER_GAME_HOUR * 1000;
    }
    
    getHoursUntilNight(): number {
        const time = GameState.getTime();
        if (time.gameHour < GAME_CONFIG.NIGHT_START_HOUR) {
            return GAME_CONFIG.NIGHT_START_HOUR - time.gameHour 
                - time.gameMinute / 60;
        }
        return 0;
    }
    
    getHoursUntilDay(): number {
        const time = GameState.getTime();
        if (time.gameHour >= GAME_CONFIG.NIGHT_START_HOUR) {
            return 24 - time.gameHour - time.gameMinute / 60 
                + GAME_CONFIG.DAY_START_HOUR;
        }
        if (time.gameHour < GAME_CONFIG.DAY_START_HOUR) {
            return GAME_CONFIG.DAY_START_HOUR - time.gameHour 
                - time.gameMinute / 60;
        }
        return 0;
    }
    
    getTotalGameHours(): number {
        const time = GameState.getTime();
        return (time.gameDay - 1) * 24 + time.gameHour + time.gameMinute / 60;
    }
    
    // ============================================================
    // ESCALATION
    // ============================================================
    getEscalationMultiplier(): number {
        const time = GameState.getTime();
        return Math.pow(GAME_CONFIG.ESCALATION_MULTIPLIER, time.escalationLevel);
    }
    
    isEscalationActive(): boolean {
        return this.getCurrentDay() > GAME_CONFIG.ESCALATION_START_DAY;
    }
    
    // ============================================================
    // DESTROY
    // ============================================================
    destroy(): void {
        if (this.timerEvent) {
            this.timerEvent.remove();
            this.timerEvent = null;
        }
        this.scene = null;
    }
}