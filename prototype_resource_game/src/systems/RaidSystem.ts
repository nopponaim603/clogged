// src/systems/RaidSystem.ts
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Raid, MapNode } from '../state/types';
import { RandomGenerator } from '../utils/RandomGenerator';
import { Systems } from '../main';

export class RaidSystem {
    private nextRaidId: number = 1;
    
    // ============================================================
    // INITIALIZE
    // ============================================================
    init(): void {
        this.nextRaidId = 1;
        GameState.setRaids([]);
    }
    
    // ============================================================
    // GENERATE RAIDS FOR NIGHT
    // ============================================================
    generateRaidsForNight(): void {
        const state = GameState;
        const rng = RandomGenerator.getInstance();
        const time = state.getTime();
        
        // ✅ จำนวน raid
        const raidCount = this.getRaidCount();
        
        console.log(`🌙 Generating ${raidCount} raids for Night ${time.gameDay}`);
        
        const raids: Raid[] = [];
        
        for (let i = 0; i < raidCount; i++) {
            const raid = this.createRaid();
            raids.push(raid);
        }
        
        state.setRaids(raids);
    }
    
    private getRaidCount(): number {
        const rng = RandomGenerator.getInstance();
        const roll = rng.random();
        
        if (roll < GAME_CONFIG.RAID_CHANCE_3) {
            return 3;
        } else if (roll < GAME_CONFIG.RAID_CHANCE_3 + GAME_CONFIG.RAID_CHANCE_2) {
            return 2;
        }
        return GAME_CONFIG.RAID_MIN_PER_NIGHT;
    }
    
    private createRaid(): Raid {
        const rng = RandomGenerator.getInstance();
        const time = GameState.getTime();
        const basePos = GameState.getBasePosition();
        
        // ✅ หา node ที่อยู่ข้างหน้า
        const forwardNodes = this.getForwardNodes(5000);  // 5000 px
        
        if (forwardNodes.length === 0) {
            console.warn('⚠️ No forward nodes for raid');
            return this.createFallbackRaid();
        }
        
        const targetNode = rng.pick(forwardNodes);
        
        // ✅ คำนวณจำนวน enemies
        const enemyBase = GAME_CONFIG.RAID_ENEMY_COUNT_BASE;
        const enemyPerDay = GAME_CONFIG.RAID_ENEMY_COUNT_PER_DAY;
        const enemyCount = enemyBase + time.gameDay * enemyPerDay;
        
        // ✅ Elite count
        const eliteCount = Math.floor(enemyCount * 0.2);
        
        // ✅ Boss
        const hasBoss = time.gameDay % GAME_CONFIG.RAID_BOSS_DAY_INTERVAL === 0;
        
        // ✅ Duration
        const duration = rng.randomRange(
            GAME_CONFIG.RAID_DURATION_MIN,
            GAME_CONFIG.RAID_DURATION_MAX
        );
        
        // ✅ Reward
        const reward = this.calculateReward(enemyCount, eliteCount, hasBoss);
        
        const raid: Raid = {
            id: `raid_${this.nextRaidId++}`,
            nodeId: targetNode.id,
            triggerDistance: 0,
            durationHours: duration,
            enemies: enemyCount,
            eliteCount,
            hasBoss,
            isTriggered: false,
            isCompleted: false,
            reward,
        };
        
        console.log(`⚠️ Raid created: ${raid.id} at ${targetNode.id} (${enemyCount} enemies)`);
        
        return raid;
    }
    
    private createFallbackRaid(): Raid {
        return {
            id: `raid_${this.nextRaidId++}`,
            nodeId: '',
            triggerDistance: 1000,
            durationHours: GAME_CONFIG.RAID_DURATION_MIN,
            enemies: 10,
            eliteCount: 2,
            hasBoss: false,
            isTriggered: false,
            isCompleted: false,
            reward: 100,
        };
    }
    
    private getForwardNodes(maxDistance: number): MapNode[] {
        const state = GameState;
        const basePos = state.getBasePosition();
        const nodes = Systems.map.getAllNodes();
        
        return nodes.filter(node => {
            // ✅ ต้องอยู่ข้างหน้า (x > base.x)
            if (node.position.x <= basePos.x) return false;
            
            // ✅ ระยะทางไม่เกิน maxDistance
            const distance = Math.sqrt(
                Math.pow(node.position.x - basePos.x, 2) +
                Math.pow(node.position.y - basePos.y, 2)
            );
            if (distance > maxDistance) return false;
            
            // ✅ ต้องไม่ใช่ start node
            if (node.type === 'start') return false;
            
            return true;
        });
    }
    
    private calculateReward(enemies: number, elite: number, boss: boolean): number {
        let reward = enemies * 10;
        reward += elite * 50;
        if (boss) reward += 500;
        return reward;
    }
    
    // ============================================================
    // CHECK RAID TRIGGER
    // ============================================================
    checkRaidTrigger(nodeId: string): Raid | null {
        const state = GameState;
        const raids = state.getRaids();
        
        const raid = raids.find(r => 
            r.nodeId === nodeId && 
            !r.isTriggered && 
            !r.isCompleted
        );
        
        return raid || null;
    }
    
    // ============================================================
    // COMPLETE RAID
    // ============================================================
    completeRaid(raidId: string, won: boolean, damageTaken: number): void {
        const state = GameState;
        const raids = state.getRaids();
        const raid = raids.find(r => r.id === raidId);
        
        if (!raid) return;
        
        raid.isCompleted = true;
        
        // ✅ Reward
        let credits = 0;
        
        if (won) {
            if (damageTaken === 0) {
                // ✅ ไม่โดนดาเมจ → ได้เต็ม
                credits = raid.reward;
            } else {
                // ✅ โดนดาเมจ → ได้ครึ่ง
                credits = Math.floor(raid.reward * GAME_CONFIG.RAID_REWARD_PARTIAL);
            }
        } else {
            // ✅ กันไม่สำเร็จ → ได้ครึ่ง + เสียทรัพยากร
            credits = Math.floor(raid.reward * GAME_CONFIG.RAID_REWARD_FAILED);
            this.loseResources();
        }
        
        state.addCredits(credits);
        
        console.log(`✅ Raid ${raidId} ${won ? 'WON' : 'FAILED'}! +${credits} credits`);
        
        EventBus.emit(EVENTS.RAID_COMPLETED, {
            raidId,
            won,
            credits,
            damageTaken,
        });
    }
    
    private loseResources(): void {
        const state = GameState;
        const resources = state.getResources();
        
        const lost: { [key: string]: number } = {};
        
        for (const [type, amount] of Object.entries(resources)) {
            if (typeof amount === 'number' && amount > 0) {
                const loss = Math.floor(amount * GAME_CONFIG.RAID_RESOURCE_LOSS_PERCENT);
                if (loss > 0) {
                    state.removeResource(type, loss);
                    lost[type] = loss;
                }
            }
        }
        
        console.log(`💸 Lost resources:`, lost);
    }
    
    // ============================================================
    // QUERY
    // ============================================================
    getActiveRaids(): Raid[] {
        return GameState.getRaids().filter(r => !r.isCompleted);
    }
    
    getUpcomingRaids(): Raid[] {
        return GameState.getRaids().filter(r => 
            !r.isTriggered && !r.isCompleted
        );
    }
    
    hasActiveRaid(): boolean {
        return GameState.getRaids().some(r => r.isTriggered && !r.isCompleted);
    }
    
    // ============================================================
    // CLEAR
    // ============================================================
    clearRaids(): void {
        GameState.setRaids([]);
    }
}