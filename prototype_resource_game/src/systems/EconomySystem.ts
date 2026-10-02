// src/systems/EconomySystem.ts
import { GAME_CONFIG, RESOURCE_ICONS } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Crew } from '../entities/Crew';
import { Vehicle } from '../entities/Vehicle';
import { RandomGenerator } from '../utils/RandomGenerator';
import { Systems } from '../main';

export interface ShopItem {
    id: string;
    type: 'resource' | 'crew' | 'vehicle' | 'item';
    name: string;
    icon: string;
    price: number;        // Credits
    stock: number;
    resourceType?: string;
    resourceAmount?: number;
    crewData?: Partial<Crew>;
    vehicleData?: Partial<Vehicle>;
}

export interface Shop {
    id: string;
    type: 'general' | 'mercenary' | 'trade';
    items: ShopItem[];
    discount: number;     // 0-1 (0 = ไม่ลด)
}

export class EconomySystem {
    private shops: Map<string, Shop> = new Map();
    private nextShopId: number = 1;
    
    // ============================================================
    // INITIALIZE
    // ============================================================
    init(): void {
        this.shops.clear();
        this.nextShopId = 1;
    }
    
    // ============================================================
    // SHOP GENERATION
    // ============================================================
    generateShop(nodeId: string, type: 'general' | 'mercenary' | 'trade'): Shop {
        const rng = RandomGenerator.getInstance();
        
        const shop: Shop = {
            id: `shop_${this.nextShopId++}`,
            type,
            items: [],
            discount: rng.random() < 0.2 ? 0.2 : 0,  // 20% ลด 20%
        };
        
        switch (type) {
            case 'general':
                shop.items = this.generateGeneralItems(rng);
                break;
            case 'mercenary':
                shop.items = this.generateMercenaryItems(rng);
                break;
            case 'trade':
                shop.items = this.generateTradeItems(rng);
                break;
        }
        
        this.shops.set(shop.id, shop);
        
        console.log(`🏪 Generated ${type} shop: ${shop.id} (${shop.items.length} items)`);
        
        return shop;
    }
    
    private generateGeneralItems(rng: any): ShopItem[] {
        const items: ShopItem[] = [];
        
        // ✅ Resources
        const resources = [
            { type: 'wood', name: 'Wood', icon: '🪵', basePrice: 10 },
            { type: 'stone', name: 'Stone', icon: '🪨', basePrice: 15 },
            { type: 'iron_ore', name: 'Iron Ore', icon: '⛏️', basePrice: 25 },
            { type: 'food', name: 'Food', icon: '🍖', basePrice: 20 },
            { type: 'water', name: 'Water', icon: '💧', basePrice: 15 },
            { type: 'fuel', name: 'Fuel', icon: '⛽', basePrice: 50 },
        ];
        
        // ✅ สุ่ม 3-5 items
        const count = rng.randomInt(3, 5);
        const usedTypes = new Set<string>();
        
        for (let i = 0; i < count; i++) {
            const res = rng.pick(resources);
            if (usedTypes.has(res.type)) continue;
            usedTypes.add(res.type);
            
            const amount = rng.randomInt(10, 50);
            const price = Math.floor(res.basePrice * amount * (1 - 0));
            
            items.push({
                id: `item_${res.type}_${Date.now()}_${i}`,
                type: 'resource',
                name: `${res.name} x${amount}`,
                icon: res.icon,
                price,
                stock: rng.randomInt(1, 5),
                resourceType: res.type,
                resourceAmount: amount,
            });
        }
        
        return items;
    }
    
    private generateMercenaryItems(rng: any): ShopItem[] {
        const items: ShopItem[] = [];
        const count = rng.randomInt(
            GAME_CONFIG.MERCENARY_SHOP.crewCountMin,
            GAME_CONFIG.MERCENARY_SHOP.crewCountMax
        );
        
        const names = ['Sarah', 'John', 'Emma', 'Mike', 'Lisa', 'David', 'Anna', 'Tom'];
        
        for (let i = 0; i < count; i++) {
            const name = rng.pick(names) + ` ${String.fromCharCode(65 + i)}`;
            const cost = rng.randomInt(
                GAME_CONFIG.MERCENARY_SHOP.crewCostMin,
                GAME_CONFIG.MERCENARY_SHOP.crewCostMax
            );
            
            // ✅ สร้าง Crew
            const crew = Crew.create(-1, name, cost);
            crew.perks = this.generateRandomPerks(rng);
            
            items.push({
                id: `crew_${Date.now()}_${i}`,
                type: 'crew',
                name: `${name} (${crew.getGatheringEfficiency().toFixed(0)} gather)`,
                icon: '🧑‍🤝‍🧑',
                price: cost,
                stock: 1,
                crewData: crew.toJSON() as any,
            });
        }
        
        return items;
    }
    
    private generateTradeItems(rng: any): ShopItem[] {
        const items: ShopItem[] = [];
        
        // ✅ Trade = แลกเปลี่ยน resource
        const trades = [
            { from: 'wood', to: 'plank', rate: 3, price: 30 },
            { from: 'stone', to: 'brick', rate: 3, price: 40 },
            { from: 'iron_ore', to: 'iron_bar', rate: 4, price: 60 },
            { from: 'food', to: 'water', rate: 1, price: 20 },
        ];
        
        const count = rng.randomInt(2, 4);
        
        for (let i = 0; i < count; i++) {
            const trade = rng.pick(trades);
            
            items.push({
                id: `trade_${Date.now()}_${i}`,
                type: 'resource',
                name: `${trade.rate}x ${trade.from} → 1x ${trade.to}`,
                icon: RESOURCE_ICONS[trade.to as keyof typeof RESOURCE_ICONS] || '📦',
                price: trade.price,
                stock: rng.randomInt(3, 10),
                resourceType: trade.to,
                resourceAmount: 1,
            });
        }
        
        return items;
    }
    
    private generateRandomPerks(rng: any): string[] {
        const perks: string[] = [];
        
        if (rng.random() > 0.8) {  // 20% มี perk
            const perkTypes = ['mechanist', 'cook', 'engineer', 'blacksmith', 'gunslinger', 'scout', 'soldier'];
            const tiers = ['i', 'ii', 'iii'];
            
            const type = rng.pick(perkTypes);
            const tier = rng.pick(tiers);
            
            perks.push(`${type}_${tier}`);
        }
        
        return perks;
    }
    
    // ============================================================
    // PURCHASE
    // ============================================================
    purchaseItem(shopId: string, itemId: string): boolean {
        const shop = this.shops.get(shopId);
        if (!shop) return false;
        
        const item = shop.items.find(i => i.id === itemId);
        if (!item) return false;
        if (item.stock <= 0) return false;
        
        const state = GameState;
        
        // ✅ คำนวณราคา (รวม discount)
        const finalPrice = Math.floor(item.price * (1 - shop.discount));
        
        // ✅ เช็ค Credits
        if (state.getCredits() < finalPrice) {
            console.warn('❌ Not enough credits');
            return false;
        }
        
        // ✅ หัก Credits
        state.removeCredits(finalPrice);
        
        // ✅ ให้ของ
        switch (item.type) {
            case 'resource':
                if (item.resourceType && item.resourceAmount) {
                    state.addResource(item.resourceType, item.resourceAmount);
                }
                break;
            
            case 'crew':
                if (item.crewData) {
                    const crew = Crew.fromJSON(item.crewData);
                    crew.id = state.getCrew().length + 1;
                    Systems.crew.hireCrew(crew);
                }
                break;
            
            case 'vehicle':
                Systems.vehicle.purchaseVehicle(finalPrice);
                break;
        }
        
        item.stock--;
        
        console.log(`🛒 Purchased: ${item.name} for ${finalPrice} credits`);
        
        EventBus.emit('shop:purchase', { shopId, itemId, price: finalPrice });
        
        return true;
    }
    
    // ============================================================
    // SELL
    // ============================================================
    sellResource(resourceType: string, amount: number): boolean {
        const state = GameState;
        
        // ✅ เช็ค resource
        if (state.getResource(resourceType) < amount) return false;
        
        // ✅ คำนวณราคา
        const basePrices: { [key: string]: number } = {
            wood: 5,
            stone: 8,
            iron_ore: 15,
            aluminum_ore: 18,
            copper_ore: 20,
            rubber: 12,
            food: 10,
            water: 8,
            fuel: 30,
            plank: 15,
            brick: 20,
            iron_bar: 40,
            copper_bar: 50,
            aluminum_sheet: 55,
            rubber_sheet: 30,
            gear: 100,
            circuit: 150,
        };
        
        const price = (basePrices[resourceType] || 5) * amount;
        
        // ✅ หัก resource
        state.removeResource(resourceType, amount);
        
        // ✅ เพิ่ม Credits
        state.addCredits(price);
        
        console.log(`💰 Sold ${amount}x ${resourceType} for ${price} credits`);
        
        EventBus.emit('shop:sell', { resourceType, amount, price });
        
        return true;
    }
    
    // ============================================================
    // QUERY
    // ============================================================
    getShop(shopId: string): Shop | undefined {
        return this.shops.get(shopId);
    }
    
    getAllShops(): Shop[] {
        return Array.from(this.shops.values());
    }
    
    // ============================================================
    // RESET
    // ============================================================
    reset(): void {
        this.shops.clear();
        this.nextShopId = 1;
    }
}