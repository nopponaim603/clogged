// src/systems/MissionSystem.ts
import { Crew } from '../entities/Crew';
import { ResourceNode } from '../entities/ResourceNode';
import { ResourceManager } from './ResourceManager';
import { GAME_CONFIG } from '../config';

export interface MissionResult {
    success: boolean;
    message: string;
    resources?: Record<string, number>;
    relic?: string;
    monsterPart?: string;
}

export class MissionSystem {
    private resourceManager: ResourceManager;

    constructor(resourceManager: ResourceManager) {
        this.resourceManager = resourceManager;
    }

    // ✅ Method เดิมสำหรับ 1 คน (ปรับให้ทำงานแม้เวลาไม่พอ)
    executeMission(crew: Crew, target: ResourceNode): MissionResult {
        const distance = this.getDistance(crew.position, target.position);
        const travelTime = crew.calculateTravelTime(distance);
        const actionTime = target.getActionTime(crew.getEffectiveGathering());
        const totalTime = travelTime * 2 + actionTime;

        if (target.isRelic) {
            return this.executeRelicSearch(crew, target, travelTime, actionTime);
        } else if (target.isMonster) {
            return this.executeMonsterHunt(crew, target, travelTime, actionTime);
        } else {
            return this.executeGathering(crew, target, travelTime, actionTime);
        }
    }

    // ✅ Gathering - รองรับเวลาไม่พอ
    private executeGathering(crew: Crew, target: ResourceNode, travelTime: number, actionTime: number): MissionResult {
        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;

        // ✅ ถ้าเวลาไม่พอ → ทำงานให้เท่าที่เหลือ
        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crew.name} had no time to travel to ${target.type}!`
                };
            }
            
            const ratio = remainingTime / actionTime;
            const amount = Math.floor(target.amount * 0.5 * ratio);
            if (amount > 0) {
                this.resourceManager.addResource(target.type as any, amount);
                return {
                    success: true,
                    message: `⚠️ ${crew.name} gathered only ${amount} ${target.type} (time ran out! Used ${Math.floor(totalTime)} units)`,
                    resources: { [target.type]: amount }
                };
            }
            return {
                success: false,
                message: `❌ ${crew.name} couldn't gather anything (time ran out before gathering)!`
            };
        }

        // ✅ เวลาพอ
        const amount = Math.floor(target.amount * (0.5 + crew.getEffectiveGathering() * 0.3));
        this.resourceManager.addResource(target.type as any, amount);
        return {
            success: true,
            message: `✅ ${crew.name} gathered ${amount} ${target.type}! (${Math.floor(totalTime)} units)`,
            resources: { [target.type]: amount }
        };
    }

    // ✅ Relic Search - รองรับเวลาไม่พอ
    private executeRelicSearch(crew: Crew, target: ResourceNode, travelTime: number, actionTime: number): MissionResult {
        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;

        // ✅ ถ้าเวลาไม่พอ → ทำงานให้เท่าที่เหลือ
        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crew.name} had no time to travel to the relic site!`
                };
            }
            
            // ✅ มีเวลาไปถึงแต่ไม่พอค้นหา
            const ratio = remainingTime / actionTime;
            if (ratio > 0.3) {
                // ✅ พอมีเวลาค้นหาบ้าง
                const successRate = 0.2 + crew.getEffectiveSearching() * 0.2 * ratio;
                if (Math.random() < successRate) {
                    this.resourceManager.addResource('food', 5);
                    this.resourceManager.addResource('wood', 8);
                    return {
                        success: true,
                        message: `🎉 ${crew.name} found a small relic! (${Math.floor(totalTime)} units, time was running out!)`,
                        relic: '🌟 Small Relic',
                        resources: { food: 5, wood: 8 }
                    };
                } else {
                    this.resourceManager.addResource('wood', 3);
                    return {
                        success: false,
                        message: `⏰ ${crew.name} ran out of time... found some wood though! (${Math.floor(totalTime)} units)`,
                        resources: { wood: 3 }
                    };
                }
            }
            return {
                success: false,
                message: `⏰ ${crew.name} reached the relic site but had no time to search!`
            };
        }

        // ✅ เวลาพอ
        const successRate = 0.4 + crew.getEffectiveSearching() * 0.3;
        if (Math.random() < successRate) {
            this.resourceManager.addResource('food', 10);
            this.resourceManager.addResource('wood', 15);
            return {
                success: true,
                message: `🎉 ${crew.name} found an Ancient Relic! (${Math.floor(totalTime)} units)`,
                relic: '🌟 Ancient Relic',
                resources: { food: 10, wood: 15 }
            };
        } else {
            this.resourceManager.addResource('food', 5);
            this.resourceManager.addResource('stone', 5);
            return {
                success: true,
                message: `🔍 ${crew.name} didn't find relic but found resources! (${Math.floor(totalTime)} units)`,
                resources: { food: 5, stone: 5 }
            };
        }
    }

    // ✅ Monster Hunt - รองรับเวลาไม่พอ
    private executeMonsterHunt(crew: Crew, target: ResourceNode, travelTime: number, actionTime: number): MissionResult {
        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;

        // ✅ ถ้าเวลาไม่พอ → ทำงานให้เท่าที่เหลือ
        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crew.name} had no time to travel to ${target.monsterName}!`
                };
            }
            
            const ratio = remainingTime / actionTime;
            if (ratio > 0.3) {
                // ✅ พอมีเวลาสู้บ้าง (แต่ไม่เต็มที่)
                const successRate = 0.1 + crew.getEffectiveHunting() * 0.2 * ratio - (target.difficulty || 1) * 0.05;
                if (Math.random() < successRate) {
                    const parts = ['Fangs', 'Hides', 'Claws'];
                    const part = parts[Math.floor(Math.random() * parts.length)];
                    this.resourceManager.addResource('food', 10);
                    this.resourceManager.addMonsterPart(
                        part.toLowerCase() as 'fangs' | 'hides' | 'claws',
                        1
                    );
                    return {
                        success: true,
                        message: `🎉 ${crew.name} defeated ${target.monsterName}! Got ${part}! (${Math.floor(totalTime)} units, time was running out!)`,
                        monsterPart: part,
                        resources: { food: 10 }
                    };
                } else {
                    const damage = 3 + Math.floor(Math.random() * 10);
                    const dead = crew.takeDamage(damage);
                    return {
                        success: false,
                        message: dead 
                            ? `💀 ${crew.name} died fighting ${target.monsterName}! (${Math.floor(totalTime)} units)`
                            : `💢 ${crew.name} failed to hunt ${target.monsterName} (took ${damage} damage, time ran out)!`
                    };
                }
            }
            return {
                success: false,
                message: `⏰ ${crew.name} reached ${target.monsterName} but had no time to fight!`
            };
        }

        // ✅ เวลาพอ
        const successRate = 0.2 + crew.getEffectiveHunting() * 0.3 - (target.difficulty || 1) * 0.05;
        if (Math.random() < successRate) {
            const parts = ['Fangs', 'Hides', 'Claws'];
            const part = parts[Math.floor(Math.random() * parts.length)];
            this.resourceManager.addResource('food', 20);
            this.resourceManager.addMonsterPart(
                part.toLowerCase() as 'fangs' | 'hides' | 'claws',
                1 + Math.floor(Math.random() * 3)
            );
            return {
                success: true,
                message: `🎉 ${crew.name} defeated ${target.monsterName}! Got ${part}! (${Math.floor(totalTime)} units)`,
                monsterPart: part,
                resources: { food: 20 }
            };
        } else {
            const damage = 5 + Math.floor(Math.random() * 15);
            const dead = crew.takeDamage(damage);
            return {
                success: false,
                message: dead 
                    ? `💀 ${crew.name} died fighting ${target.monsterName}! (${Math.floor(totalTime)} units)`
                    : `💢 ${crew.name} failed to hunt ${target.monsterName} (took ${damage} damage)! (${Math.floor(totalTime)} units)`
            };
        }
    }

    // ✅ Collaborative Gathering - รองรับเวลาไม่พอ
    private executeCollaborativeGathering(
        crews: Crew[], 
        target: ResourceNode, 
        travelTime: number, 
        actionTime: number
    ): MissionResult {
        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;
        const totalGathering = crews.reduce((sum, c) => sum + c.getEffectiveGathering(), 0);
        const crewNames = crews.map(c => c.name).join(' + ');

        // ✅ ถ้าเวลาไม่พอ → ทำงานให้เท่าที่เหลือ
        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crewNames} had no time to travel to ${target.type}!`
                };
            }
            
            const ratio = remainingTime / actionTime;
            const baseAmount = target.amount * 0.5;
            const bonusMultiplier = 1 + (crews.length - 1) * 0.3;
            const amount = Math.floor(baseAmount * bonusMultiplier * (totalGathering / crews.length) * ratio);
            
            if (amount > 0) {
                this.resourceManager.addResource(target.type as any, amount);
                return {
                    success: true,
                    message: `⚠️ ${crewNames} gathered only ${amount} ${target.type} (time ran out! ${crews.length} crews)`,
                    resources: { [target.type]: amount }
                };
            }
            return {
                success: false,
                message: `❌ ${crewNames} couldn't gather anything (time ran out before gathering)!`
            };
        }

        // ✅ เวลาพอ
        const baseAmount = target.amount * 0.5;
        const bonusMultiplier = 1 + (crews.length - 1) * 0.3;
        const amount = Math.floor(baseAmount * bonusMultiplier * (totalGathering / crews.length));
        
        this.resourceManager.addResource(target.type as any, amount);
        return {
            success: true,
            message: `✅ ${crewNames} gathered ${amount} ${target.type}! (${Math.floor(totalTime)} units, ${crews.length} crews)`,
            resources: { [target.type]: amount }
        };
    }

    // ✅ Collaborative Relic Search - รองรับเวลาไม่พอ
    private executeCollaborativeRelicSearch(
        crews: Crew[], 
        target: ResourceNode, 
        travelTime: number, 
        actionTime: number
    ): MissionResult {
        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;
        const crewNames = crews.map(c => c.name).join(' + ');
        const totalSearching = crews.reduce((sum, c) => sum + c.getEffectiveSearching(), 0);
        const avgSearching = totalSearching / crews.length;

        // ✅ ถ้าเวลาไม่พอ → ทำงานให้เท่าที่เหลือ
        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crewNames} had no time to travel to the relic site!`
                };
            }
            
            const ratio = remainingTime / actionTime;
            if (ratio > 0.3) {
                const successRate = 0.2 + avgSearching * 0.2 * ratio + (crews.length - 1) * 0.05;
                if (Math.random() < successRate) {
                    this.resourceManager.addResource('food', 5 + crews.length * 3);
                    this.resourceManager.addResource('wood', 8 + crews.length * 3);
                    return {
                        success: true,
                        message: `🎉 ${crewNames} found a small relic! (${Math.floor(totalTime)} units, time was running out!)`,
                        relic: '🌟 Small Relic',
                        resources: { food: 5 + crews.length * 3, wood: 8 + crews.length * 3 }
                    };
                } else {
                    this.resourceManager.addResource('wood', 3 + crews.length);
                    return {
                        success: false,
                        message: `⏰ ${crewNames} ran out of time... found some wood though!`,
                        resources: { wood: 3 + crews.length }
                    };
                }
            }
            return {
                success: false,
                message: `⏰ ${crewNames} reached the relic site but had no time to search!`
            };
        }

        // ✅ เวลาพอ
        const successRate = 0.4 + avgSearching * 0.25 + (crews.length - 1) * 0.1;
        if (Math.random() < successRate) {
            this.resourceManager.addResource('food', 10 + crews.length * 5);
            this.resourceManager.addResource('wood', 15 + crews.length * 5);
            return {
                success: true,
                message: `🎉 ${crewNames} found an Ancient Relic! (${Math.floor(totalTime)} units, ${crews.length} crews)`,
                relic: '🌟 Ancient Relic',
                resources: { food: 10 + crews.length * 5, wood: 15 + crews.length * 5 }
            };
        } else {
            this.resourceManager.addResource('food', 5 + crews.length * 3);
            this.resourceManager.addResource('stone', 5 + crews.length * 3);
            return {
                success: true,
                message: `🔍 ${crewNames} didn't find relic but found resources! (${Math.floor(totalTime)} units)`,
                resources: { food: 5 + crews.length * 3, stone: 5 + crews.length * 3 }
            };
        }
    }

    // ✅ Collaborative Monster Hunt - รองรับเวลาไม่พอ
    private executeCollaborativeMonsterHunt(
        crews: Crew[], 
        target: ResourceNode, 
        travelTime: number, 
        actionTime: number
    ): MissionResult {
        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;
        const crewNames = crews.map(c => c.name).join(' + ');
        const totalHunting = crews.reduce((sum, c) => sum + c.getEffectiveHunting(), 0);
        const avgHunting = totalHunting / crews.length;

        // ✅ ถ้าเวลาไม่พอ → ทำงานให้เท่าที่เหลือ
        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crewNames} had no time to travel to ${target.monsterName}!`
                };
            }
            
            const ratio = remainingTime / actionTime;
            if (ratio > 0.3) {
                const successRate = 0.1 + avgHunting * 0.2 * ratio + (crews.length - 1) * 0.08 - (target.difficulty || 1) * 0.05;
                if (Math.random() < successRate) {
                    const parts = ['Fangs', 'Hides', 'Claws'];
                    const part = parts[Math.floor(Math.random() * parts.length)];
                    const amount = 1 + Math.floor(Math.random() * 2) + (crews.length - 1);
                    this.resourceManager.addResource('food', 10 + crews.length * 5);
                    this.resourceManager.addMonsterPart(
                        part.toLowerCase() as 'fangs' | 'hides' | 'claws',
                        amount
                    );
                    return {
                        success: true,
                        message: `🎉 ${crewNames} defeated ${target.monsterName}! Got ${part}x${amount}! (${Math.floor(totalTime)} units, time was running out!)`,
                        monsterPart: part,
                        resources: { food: 10 + crews.length * 5 }
                    };
                } else {
                    const damagePerCrew = Math.floor((3 + Math.random() * 10) / crews.length);
                    let deadCount = 0;
                    for (const crew of crews) {
                        const dead = crew.takeDamage(damagePerCrew);
                        if (dead) deadCount++;
                    }
                    return {
                        success: false,
                        message: deadCount > 0
                            ? `💀 ${deadCount} crew(s) died fighting ${target.monsterName} (time ran out)!`
                            : `💢 ${crewNames} failed to hunt ${target.monsterName} (took ${damagePerCrew} damage each, time ran out)!`
                    };
                }
            }
            return {
                success: false,
                message: `⏰ ${crewNames} reached ${target.monsterName} but had no time to fight!`
            };
        }

        // ✅ เวลาพอ
        const successRate = 0.2 + avgHunting * 0.25 + (crews.length - 1) * 0.15 - (target.difficulty || 1) * 0.05;
        if (Math.random() < successRate) {
            const parts = ['Fangs', 'Hides', 'Claws'];
            const part = parts[Math.floor(Math.random() * parts.length)];
            const amount = 1 + Math.floor(Math.random() * 3) + (crews.length - 1);
            this.resourceManager.addResource('food', 20 + crews.length * 10);
            this.resourceManager.addMonsterPart(
                part.toLowerCase() as 'fangs' | 'hides' | 'claws',
                amount
            );
            return {
                success: true,
                message: `🎉 ${crewNames} defeated ${target.monsterName}! Got ${part}x${amount}! (${Math.floor(totalTime)} units, ${crews.length} crews)`,
                monsterPart: part,
                resources: { food: 20 + crews.length * 10 }
            };
        } else {
            const damagePerCrew = Math.floor((5 + Math.random() * 15) / crews.length);
            let deadCount = 0;
            for (const crew of crews) {
                const dead = crew.takeDamage(damagePerCrew);
                if (dead) deadCount++;
            }
            return {
                success: false,
                message: deadCount > 0
                    ? `💀 ${deadCount} crew(s) died fighting ${target.monsterName}! (${Math.floor(totalTime)} units)`
                    : `💢 ${crewNames} failed to hunt ${target.monsterName} (took ${damagePerCrew} damage each)! (${Math.floor(totalTime)} units)`
            };
        }
    }

    // ✅ Method สำหรับหลายคนร่วมกัน (ปรับให้ทำงานแม้เวลาไม่พอ)
    executeCollaborativeMission(
        crews: Crew[], 
        target: ResourceNode, 
        baseTravelTime: number
    ): MissionResult {
        // คำนวณความสามารถรวม
        let totalGathering = 0;
        let totalSearching = 0;
        let totalHunting = 0;
        let totalSpeed = 0;

        for (const crew of crews) {
            totalGathering += crew.getEffectiveGathering();
            totalSearching += crew.getEffectiveSearching();
            totalHunting += crew.getEffectiveHunting();
            totalSpeed += crew.getEffectiveSpeed();
        }

        const avgSpeed = totalSpeed / crews.length;
        const travelTime = baseTravelTime / avgSpeed;

        let actionTime = 0;
        let actionType = '';

        if (target.isRelic) {
            actionType = 'searching';
            actionTime = target.getActionTime(totalSearching);
        } else if (target.isMonster) {
            actionType = 'hunting';
            actionTime = target.getActionTime(totalHunting);
        } else {
            actionType = 'gathering';
            actionTime = target.getActionTime(totalGathering);
        }

        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;

        // ✅ ถ้าเวลาไม่พอ → ทำงานให้เท่าที่เวลาเหลือ (ส่งต่อให้ method ข้างในจัดการ)
        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crews.map(c => c.name).join(' + ')} had no time to travel!`
                };
            }
            
            // ✅ ปรับ actionTime ให้เหลือเท่าที่เวลาเหลือ
            const ratio = remainingTime / actionTime;
            const adjustedActionTime = actionTime * ratio;
            
            if (target.isRelic) {
                return this.executeCollaborativeRelicSearch(crews, target, travelTime, adjustedActionTime);
            } else if (target.isMonster) {
                return this.executeCollaborativeMonsterHunt(crews, target, travelTime, adjustedActionTime);
            } else {
                return this.executeCollaborativeGathering(crews, target, travelTime, adjustedActionTime);
            }
        }

        // ✅ เวลาพอ → ดำเนินการตามปกติ
        if (target.isRelic) {
            return this.executeCollaborativeRelicSearch(crews, target, travelTime, actionTime);
        } else if (target.isMonster) {
            return this.executeCollaborativeMonsterHunt(crews, target, travelTime, actionTime);
        } else {
            return this.executeCollaborativeGathering(crews, target, travelTime, actionTime);
        }
    }

    private getDistance(pos1: { x: number; y: number }, pos2: { x: number; y: number }): number {
        return Math.sqrt(Math.pow(pos1.x - pos2.x, 2) + Math.pow(pos1.y - pos2.y, 2));
    }
}