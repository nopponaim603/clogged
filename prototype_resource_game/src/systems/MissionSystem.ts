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

    executeMission(crew: Crew, target: ResourceNode): MissionResult {
        // ✅ คำนวณระยะทางจริง
        const distance = this.getDistance(crew.position, target.position);
        const travelTime = crew.calculateTravelTime(distance);
        
        // ✅ คำนวณเวลาปฏิบัติการจาก HP / Proficiency
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

    // ✅ Gathering - ใช้ระบบ HP
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
            
            // ✅ คำนวณ damage ที่ทำได้ในช่วงเวลาที่เหลือ
            const proficiency = crew.getEffectiveGathering();
            const damageDone = Math.floor(remainingTime * proficiency);
            const remainingHp = target.hp - damageDone;
            
            if (damageDone > 0) {
                // ✅ ได้ทรัพยากรตามสัดส่วน damage ที่ทำได้
                const gatheredAmount = Math.floor((damageDone / target.maxHp) * target.amount);
                const actualGathered = Math.min(gatheredAmount, target.amount);
                
                if (actualGathered > 0) {
                    this.resourceManager.addResource(target.type as any, actualGathered);
                    return {
                        success: true,
                        message: `⚠️ ${crew.name} gathered ${actualGathered} ${target.type} (time ran out! Did ${Math.floor(damageDone)} damage)`,
                        resources: { [target.type]: actualGathered }
                    };
                }
            }
            return {
                success: false,
                message: `❌ ${crew.name} couldn't gather anything (time ran out before gathering)!`
            };
        }

        // ✅ เวลาพอ → ได้ทรัพยากรเต็ม
        const amount = target.amount;
        this.resourceManager.addResource(target.type as any, amount);
        return {
            success: true,
            message: `✅ ${crew.name} gathered ${amount} ${target.type}! (${Math.floor(totalTime)} units, ${target.rankName} rank)`,
            resources: { [target.type]: amount }
        };
    }

    // ✅ Relic Search - ใช้ระบบ HP
    private executeRelicSearch(crew: Crew, target: ResourceNode, travelTime: number, actionTime: number): MissionResult {
        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;

        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crew.name} had no time to travel to the relic site!`
                };
            }
            
            // ✅ คำนวณ damage ที่ทำได้
            const proficiency = crew.getEffectiveSearching();
            const damageDone = Math.floor(remainingTime * proficiency);
            const remainingHp = target.hp - damageDone;
            
            if (damageDone > 0 && remainingHp <= 0) {
                // ✅ ค้นพบ relic (แม้เวลาจะเหลือน้อย)
                const successRate = 0.3 + (damageDone / target.maxHp) * 0.5;
                if (Math.random() < successRate) {
                    this.resourceManager.addResource('food', 10);
                    this.resourceManager.addResource('wood', 15);
                    return {
                        success: true,
                        message: `🎉 ${crew.name} found a ${target.rankName} Relic! (Time was running out!)`,
                        relic: `🌟 ${target.rankName} Relic`,
                        resources: { food: 10, wood: 15 }
                    };
                }
            }
            
            return {
                success: false,
                message: `⏰ ${crew.name} ran out of time searching for relic!`,
            };
        }

        // ✅ เวลาพอ
        const successRate = 0.3 + crew.getEffectiveSearching() / 200;
        if (Math.random() < successRate) {
            this.resourceManager.addResource('food', 10);
            this.resourceManager.addResource('wood', 15);
            return {
                success: true,
                message: `🎉 ${crew.name} found a ${target.rankName} Relic! (${Math.floor(totalTime)} units)`,
                relic: `🌟 ${target.rankName} Relic`,
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

    // ✅ Monster Hunt - ใช้ระบบ HP
    private executeMonsterHunt(crew: Crew, target: ResourceNode, travelTime: number, actionTime: number): MissionResult {
        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;

        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crew.name} had no time to travel to ${target.monsterName}!`
                };
            }
            
            // ✅ คำนวณ damage ที่ทำได้
            const proficiency = crew.getEffectiveHunting();
            const damageDone = Math.floor(remainingTime * proficiency);
            const remainingHp = target.hp - damageDone;
            
            if (damageDone > 0 && remainingHp <= 0) {
                // ✅ ล่าสำเร็จ (แม้เวลาจะเหลือน้อย)
                const parts = ['Fangs', 'Hides', 'Claws'];
                const part = parts[Math.floor(Math.random() * parts.length)];
                this.resourceManager.addResource('food', 10);
                this.resourceManager.addMonsterPart(
                    part.toLowerCase() as 'fangs' | 'hides' | 'claws',
                    1
                );
                return {
                    success: true,
                    message: `🎉 ${crew.name} defeated ${target.monsterName}! Got ${part}! (Time was running out!)`,
                    monsterPart: part,
                    resources: { food: 10 }
                };
            } else {
                // ✅ ได้รับความเสียหาย (แต่ไม่ตาย เพราะมีเวลาสู้บ้าง)
                const damage = Math.floor(5 + Math.random() * 10);
                const dead = crew.takeDamage(damage);
                return {
                    success: false,
                    message: dead 
                        ? `💀 ${crew.name} died fighting ${target.monsterName}!`
                        : `💢 ${crew.name} failed to hunt ${target.monsterName} (took ${damage} damage, time ran out)!`
                };
            }
        }

        // ✅ เวลาพอ
        const successRate = 0.2 + crew.getEffectiveHunting() / 300 - (target.difficulty || 1) * 0.05;
        if (Math.random() < successRate) {
            const parts = ['Fangs', 'Hides', 'Claws'];
            const part = parts[Math.floor(Math.random() * parts.length)];
            const amount = 1 + Math.floor(Math.random() * 3);
            this.resourceManager.addResource('food', 20);
            this.resourceManager.addMonsterPart(
                part.toLowerCase() as 'fangs' | 'hides' | 'claws',
                amount
            );
            return {
                success: true,
                message: `🎉 ${crew.name} defeated ${target.monsterName}! Got ${part}x${amount}! (${Math.floor(totalTime)} units, ${target.rankName} rank)`,
                monsterPart: part,
                resources: { food: 20 }
            };
        } else {
            const damage = 5 + Math.floor(Math.random() * 15);
            const dead = crew.takeDamage(damage);
            return {
                success: false,
                message: dead 
                    ? `💀 ${crew.name} died fighting ${target.monsterName}!`
                    : `💢 ${crew.name} failed to hunt ${target.monsterName} (took ${damage} damage)!`
            };
        }
    }

    // ✅ Collaborative Gathering - ใช้ระบบ HP
    private executeCollaborativeGathering(
        crews: Crew[], 
        target: ResourceNode, 
        travelTime: number, 
        actionTime: number
    ): MissionResult {
        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;
        const crewNames = crews.map(c => c.name).join(' + ');
        
        // ✅ คำนวณความสามารถรวม
        const totalProficiency = crews.reduce((sum, c) => sum + c.getEffectiveGathering(), 0);

        // ✅ ถ้าเวลาไม่พอ → ทำงานให้เท่าที่เหลือ
        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crewNames} had no time to travel to ${target.type}!`
                };
            }
            
            // ✅ คำนวณ damage รวมที่ทำได้ในช่วงเวลาที่เหลือ
            const totalDamage = Math.floor(remainingTime * totalProficiency);
            const damagePerCrew = Math.floor(totalDamage / crews.length);
            
            if (totalDamage > 0 && target.hp > 0) {
                // ✅ ได้ทรัพยากรตามสัดส่วน damage ที่ทำได้
                const gatheredAmount = Math.floor((totalDamage / target.maxHp) * target.amount);
                const actualGathered = Math.min(gatheredAmount, target.amount);
                
                if (actualGathered > 0) {
                    this.resourceManager.addResource(target.type as any, actualGathered);
                    return {
                        success: true,
                        message: `⚠️ ${crewNames} gathered ${actualGathered} ${target.type} (time ran out! Did ${Math.floor(totalDamage)} damage, ${crews.length} crews)`,
                        resources: { [target.type]: actualGathered }
                    };
                }
            }
            return {
                success: false,
                message: `❌ ${crewNames} couldn't gather anything (time ran out before gathering)!`
            };
        }

        // ✅ เวลาพอ → ได้ทรัพยากรเต็ม (พร้อมโบนัสจากจำนวนคน)
        const bonusMultiplier = 1 + (crews.length - 1) * 0.2; // +20% ต่อคนที่เพิ่ม
        const amount = Math.floor(target.amount * bonusMultiplier);
        
        this.resourceManager.addResource(target.type as any, amount);
        return {
            success: true,
            message: `✅ ${crewNames} gathered ${amount} ${target.type}! (${Math.floor(totalTime)} units, ${target.rankName} rank, ${crews.length} crews)`,
            resources: { [target.type]: amount }
        };
    }

    // ✅ Collaborative Relic Search - ใช้ระบบ HP
    private executeCollaborativeRelicSearch(
        crews: Crew[], 
        target: ResourceNode, 
        travelTime: number, 
        actionTime: number
    ): MissionResult {
        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;
        const crewNames = crews.map(c => c.name).join(' + ');
        
        // ✅ คำนวณความสามารถรวม
        const totalProficiency = crews.reduce((sum, c) => sum + c.getEffectiveSearching(), 0);
        const avgProficiency = totalProficiency / crews.length;

        // ✅ ถ้าเวลาไม่พอ → ทำงานให้เท่าที่เหลือ
        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crewNames} had no time to travel to the relic site!`
                };
            }
            
            // ✅ คำนวณ damage รวมที่ทำได้
            const totalDamage = Math.floor(remainingTime * totalProficiency);
            
            if (totalDamage > 0 && target.hp > 0) {
                // ✅ ค้นพบ relic ถ้าทำ damage ได้เกินครึ่งของ HP
                const damageRatio = Math.min(1, totalDamage / target.maxHp);
                
                if (damageRatio > 0.5) {
                    const successRate = 0.3 + damageRatio * 0.4 + (crews.length - 1) * 0.05;
                    if (Math.random() < successRate) {
                        const bonusFood = 5 + crews.length * 3;
                        const bonusWood = 8 + crews.length * 3;
                        this.resourceManager.addResource('food', bonusFood);
                        this.resourceManager.addResource('wood', bonusWood);
                        return {
                            success: true,
                            message: `🎉 ${crewNames} found a ${target.rankName} Relic! (Time was running out! ${crews.length} crews)`,
                            relic: `🌟 ${target.rankName} Relic`,
                            resources: { food: bonusFood, wood: bonusWood }
                        };
                    }
                }
                
                // ✅ ไม่เจอแต่ได้ทรัพยากรบ้าง
                const woodFound = Math.floor(5 * damageRatio * (1 + (crews.length - 1) * 0.2));
                if (woodFound > 0) {
                    this.resourceManager.addResource('wood', woodFound);
                    return {
                        success: false,
                        message: `⏰ ${crewNames} ran out of time... found some wood though! (${woodFound} wood)`,
                        resources: { wood: woodFound }
                    };
                }
            }
            
            return {
                success: false,
                message: `⏰ ${crewNames} reached the relic site but had no time to search!`
            };
        }

        // ✅ เวลาพอ
        const successRate = 0.3 + avgProficiency / 200 + (crews.length - 1) * 0.08;
        if (Math.random() < successRate) {
            const bonusFood = 10 + crews.length * 5;
            const bonusWood = 15 + crews.length * 5;
            this.resourceManager.addResource('food', bonusFood);
            this.resourceManager.addResource('wood', bonusWood);
            return {
                success: true,
                message: `🎉 ${crewNames} found a ${target.rankName} Relic! (${Math.floor(totalTime)} units, ${crews.length} crews)`,
                relic: `🌟 ${target.rankName} Relic`,
                resources: { food: bonusFood, wood: bonusWood }
            };
        } else {
            const bonusFood = 5 + crews.length * 3;
            const bonusStone = 5 + crews.length * 3;
            this.resourceManager.addResource('food', bonusFood);
            this.resourceManager.addResource('stone', bonusStone);
            return {
                success: true,
                message: `🔍 ${crewNames} didn't find relic but found resources! (${Math.floor(totalTime)} units)`,
                resources: { food: bonusFood, stone: bonusStone }
            };
        }
    }

    // ✅ Collaborative Monster Hunt - ใช้ระบบ HP
    private executeCollaborativeMonsterHunt(
        crews: Crew[], 
        target: ResourceNode, 
        travelTime: number, 
        actionTime: number
    ): MissionResult {
        const totalTime = travelTime * 2 + actionTime;
        const dayTimeLimit = GAME_CONFIG.DAY_TIME_LIMIT;
        const crewNames = crews.map(c => c.name).join(' + ');
        
        // ✅ คำนวณความสามารถรวม
        const totalProficiency = crews.reduce((sum, c) => sum + c.getEffectiveHunting(), 0);
        const avgProficiency = totalProficiency / crews.length;
        const avgDefense = crews.reduce((sum, c) => {
            let defense = 0;
            if (c.equipment.armor) defense += c.equipment.armor.defenseBonus || 0;
            return sum + defense;
        }, 0) / crews.length;

        // ✅ ถ้าเวลาไม่พอ → ทำงานให้เท่าที่เหลือ
        if (totalTime > dayTimeLimit) {
            const remainingTime = dayTimeLimit - travelTime * 2;
            if (remainingTime <= 0) {
                return {
                    success: false,
                    message: `❌ ${crewNames} had no time to travel to ${target.monsterName}!`
                };
            }
            
            // ✅ คำนวณ damage รวมที่ทำได้
            const totalDamage = Math.floor(remainingTime * totalProficiency);
            const damageRatio = Math.min(1, totalDamage / target.maxHp);
            
            if (totalDamage > 0 && target.hp > 0) {
                // ✅ ถ้าทำ damage เกิน 60% ของ HP → ล่าสำเร็จ
                if (damageRatio > 0.6) {
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
                        message: `🎉 ${crewNames} defeated ${target.monsterName}! Got ${part}x${amount}! (Time was running out! ${crews.length} crews)`,
                        monsterPart: part,
                        resources: { food: 10 + crews.length * 5 }
                    };
                } else {
                    // ✅ สู้ไม่สำเร็จ แต่ได้รับความเสียหายน้อยลง (因为有队友帮忙)
                    const damagePerCrew = Math.floor((3 + Math.random() * 8) / crews.length);
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
        const successRate = 0.2 + avgProficiency / 300 + (crews.length - 1) * 0.1 - (target.difficulty || 1) * 0.05;
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
                message: `🎉 ${crewNames} defeated ${target.monsterName}! Got ${part}x${amount}! (${Math.floor(totalTime)} units, ${target.rankName} rank, ${crews.length} crews)`,
                monsterPart: part,
                resources: { food: 20 + crews.length * 10 }
            };
        } else {
            // ✅ ล่าไม่สำเร็จ แต่ได้รับความเสียหายน้อยลง (因为有队友帮忙)
            const damagePerCrew = Math.floor((5 + Math.random() * 15) / crews.length);
            let deadCount = 0;
            for (const crew of crews) {
                const dead = crew.takeDamage(damagePerCrew);
                if (dead) deadCount++;
            }
            return {
                success: false,
                message: deadCount > 0
                    ? `💀 ${deadCount} crew(s) died fighting ${target.monsterName}!`
                    : `💢 ${crewNames} failed to hunt ${target.monsterName} (took ${damagePerCrew} damage each)!`
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

    // ✅ การคำนวณเวลาเดินทาง (ใช้ speed)
    private getTravelTime(crew: Crew, distance: number): number {
        const speed = crew.getEffectiveSpeed();
        // speed 0-100 → ใช้ 100 เป็น speed สูงสุด
        const speedFactor = Math.max(0.1, speed / 100);
        const baseTime = 500; // base time
        return baseTime / speedFactor;
    }

    // ✅ การคำนวณเวลาปฏิบัติการ (ใช้ stat เฉพาะ)
    private getActionTime(crew: Crew, target: ResourceNode): number {
        let stat = 0;
        let baseTime = 3000;
        
        if (target.isRelic) {
            stat = crew.getEffectiveSearching();
            baseTime = 4000;
        } else if (target.isMonster) {
            stat = crew.getEffectiveHunting();
            baseTime = 5000;
        } else {
            stat = crew.getEffectiveGathering();
            baseTime = 3000;
        }
        
        const statFactor = Math.max(0.1, stat / 100);
        return baseTime / statFactor;
    }
}