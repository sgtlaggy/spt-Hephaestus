/* eslint-disable @typescript-eslint/no-unused-vars */
/* eslint-disable @typescript-eslint/indent */
import { DependencyContainer } from "tsyringe";
import { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import { PreAkiModLoader } from "@spt-aki/loaders/PreAkiModLoader";
import { DatabaseServer } from "@spt-aki/servers/DatabaseServer";
import { IPmcData } from "@spt-aki/models/eft/common/IPmcData";
import { ImageRouter } from "@spt-aki/routers/ImageRouter";
import { ConfigServer } from "@spt-aki/servers/ConfigServer";
import { ConfigTypes } from "@spt-aki/models/enums/ConfigTypes";
import { ITraderAssort, ITraderBase } from "@spt-aki/models/eft/common/tables/ITrader";
import { ITraderConfig, UpdateTime } from "@spt-aki/models/spt/config/ITraderConfig";
import { JsonUtil } from "@spt-aki/utils/JsonUtil";
import { Item } from "@spt-aki/models/eft/common/tables/IItem";
import { ILocaleGlobalBase } from "@spt-aki/models/spt/server/ILocaleBase";
import ImporterUtil from "@spt-aki/utils/ImporterUtil"
import RagfairPriceService from "@spt-aki/services/RagfairPriceService"
import * as baseJson from "../db/base.json";
import { profile } from "console";
import type { StaticRouterModService } from "@spt-aki/services/mod/staticRouter/StaticRouterModService";
import fs from "fs";
import path from "path";



class SampleTrader implements IPreAkiLoadMod, IPostDBLoadMod {
    mod: string
    logger: ILogger

    constructor() {
        this.mod = "Hephaestus";
    }

    // Perform these actions before server fully loads
    public preAkiLoad(container: DependencyContainer): void {
        this.logger = container.resolve<ILogger>("WinstonLogger");
        const preAkiModLoader: PreAkiModLoader = container.resolve<PreAkiModLoader>("PreAkiModLoader");
        const imageRouter: ImageRouter = container.resolve<ImageRouter>("ImageRouter");
        const configServer = container.resolve<ConfigServer>("ConfigServer");
        const traderConfig: ITraderConfig = configServer.getConfig<ITraderConfig>(ConfigTypes.TRADER);
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        this.logger.debug(`[${this.mod}] Loading... `);
        this.registerProfileImage(preAkiModLoader, imageRouter);
        this.setupTraderUpdateTime(container);

        staticRouterModService.registerStaticRouter(
            "HephaestusUpdateLogin",
            [{
                url: "/launcher/profile/login",
                action: (url: string, info: any, sessionId: string, output: string) => {
                    // 
                    const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
                    const tables = databaseServer.getTables();
                    tables.traders[baseJson._id].assort = this.createAssortTable(container, sessionId);
                    return output
                }
            }], "aki"
        );
        staticRouterModService.registerStaticRouter(
            "HephaestusUpdate",
            [{
                url: "/client/game/profile/items/moving",
                action: (url: string, info: any, sessionId: string, output: string) => {
                    // 
                    if (info.data[0].Action != 'Examine') {
                        const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
                        const tables = databaseServer.getTables();
                        tables.traders[baseJson._id].assort = this.createAssortTable(container, sessionId);
                    }
                    return output
                }
            }], "aki"
        );
        this.logger.debug(`[${this.mod}] Loaded`);
    }

    public postDBLoad(container: DependencyContainer): void {
        this.logger.debug(`[${this.mod}] Delayed Loading... `);
        const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");

        const jsonUtil = container.resolve<JsonUtil>("JsonUtil");

        // Keep a reference to the tables
        const tables = databaseServer.getTables();
        // Add the new trader to the trader lists in DatabaseServer

        tables.traders[baseJson._id] = {
            assort: {
                items: [],
                barter_scheme: {},
                loyal_level_items: {}
            },
            base: jsonUtil.deserialize(jsonUtil.serialize(baseJson)) as ITraderBase,
            questassort: undefined
        };
        // For each language, add locale for the new trader
        const locales = Object.values(tables.locales.global) as Record<string, string>[];
        for (const locale of locales) {
            locale[`${baseJson._id} FullName`] = baseJson.name;
            locale[`${baseJson._id} FirstName`] = "Hephaestus";
            locale[`${baseJson._id} Nickname`] = baseJson.nickname;
            locale[`${baseJson._id} Location`] = baseJson.location;
            locale[`${baseJson._id} Description`] = "You share the reseller license of your creations to Hephaestus and in return you get a hefty discount.";
        }
        this.logger.debug(`[${this.mod}] Delayed Loaded`);
    }

    private registerProfileImage(preAkiModLoader: PreAkiModLoader, imageRouter: ImageRouter): void {
        // Reference the mod "res" folder
        const imageFilepath = `./${preAkiModLoader.getModPath(this.mod)}res`;
        // Register a route to point to the profile picture
        imageRouter.addRoute(baseJson.avatar.replace(".jpg", ""), `${imageFilepath}/img.jpg`);
    }

    private setupTraderUpdateTime(container: DependencyContainer): void {
        // Add refresh time in seconds when Config server allows to set configs
        const configServer = container.resolve<ConfigServer>("ConfigServer");
        const traderConfig = configServer.getConfig<ITraderConfig>(ConfigTypes.TRADER);
        const traderRefreshConfig: UpdateTime = { traderId: baseJson._id, seconds: 3600 }
        traderConfig.updateTime.push(traderRefreshConfig);
    }


    private getPresets(container: DependencyContainer, assortTable, currency, profiles) {
        const jsonUtil = container.resolve<JsonUtil>("JsonUtil");
        const ragfairPriceService = container.resolve<RagfairPriceService>("RagfairPriceService");
        let pool = [];
        for (let p in (profiles || [])) {
            for (let wbk in profiles[p].weaponbuilds) {
                let wb = profiles[p].weaponbuilds[wbk];
                let preItems = wb.items;
                let id = preItems[0]._id;
                let tpl = preItems[0]._tpl;
                if (pool.includes(id)) {
                    continue;
                }
                pool.push(id)
                preItems[0] = {
                    "_id": id,
                    "_tpl": tpl,
                    "parentId": "hideout",
                    "slotId": "hideout",
                    "BackgroundColor": "yellow",
                    "upd": {
                        "UnlimitedCount": true,
                        "StackObjectsCount": 2000
                    },
                    "preWeapon": true
                };
                let preItemsObj = jsonUtil.clone(preItems);
                for (let preItemObj of preItemsObj) {
                    assortTable.items.push(preItemObj);
                }
                let config;
                try {
                    config = require(`../config/config.json`);
                } catch (e) {
                }
                let price = (config || {}).cost || 712
                try {
                    price = ragfairPriceService.getDynamicOfferPrice(preItems,currency);
                } catch (error) {
                    
                }
                if(config?.discount){
                    config.discount = parseFloat(config.discount)
                    price = price * (1-((config.discount||100) / 100))
                }
                let offerRequire = [
                    {
                        "count": price,
                        "_tpl": currency
                    }
                ];
                assortTable.barter_scheme[id] = [offerRequire];
                assortTable.loyal_level_items[id] = 1;
            }
        };
        return assortTable;

    }
    private getContainers(container: DependencyContainer, assortTable, currency, profiles) {
        for (let p in (profiles || [])) {

        }
    }

    private createAssortTable(container, sessionId): ITraderAssort {
        const importer = container.resolve("ImporterUtil");
        // Assort table
        let assortTable: ITraderAssort = {
            items: [],
            barter_scheme: {},
            loyal_level_items: {}
        }
        // const currency = "5449016a4bdc2d6f028b456f";//ROUBLES
        let currency = "569668774bdc2da2298b4568"
        let config;
        try {
            config = require(`../config/config.json`);
        } catch (e) {
        }
        let profiles = {};
        if (sessionId) {
            let t = container.resolve("ProfileHelper").getFullProfile(sessionId)
            profiles = { [sessionId]: t };
        } else {
            profiles = importer.loadRecursive('user/profiles/');
        }
        try {
            assortTable = this.getPresets(container, assortTable, (config || {}).currency || currency, profiles);
            // fs.writeFileSync(path.join(__dirname, '..', "/db/test.json"),JSON.stringify(assortTable.items))
        } catch (error) {
            console.error(error);
        }
        return assortTable;
    }
}

module.exports = { mod: new SampleTrader() }