"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const ConfigTypes_1 = require("C:/snapshot/project/obj/models/enums/ConfigTypes");
const Traders_1 = require("C:/snapshot/project/obj/models/enums/Traders");
const baseJson = __importStar(require("../db/base.json"));
const config_json_1 = __importDefault(require("../config/config.json"));
// import { ItemCreateHelper } from "./itemCreateHelper";
const fs_1 = __importDefault(require("fs"));
let questIds = [];
class Hephaestus {
    mod;
    static container;
    logger;
    constructor() {
        this.mod = "Hephaestus";
    }
    // Perform these actions before server fully loads
    preSptLoad(container) {
        this.logger = container.resolve("WinstonLogger");
        const PreSptModLoader = container.resolve("PreSptModLoader");
        const imageRouter = container.resolve("ImageRouter");
        const hashUtil = container.resolve("HashUtil");
        const configServer = container.resolve("ConfigServer");
        const traderConfig = configServer.getConfig(ConfigTypes_1.ConfigTypes.TRADER);
        const staticRouterModService = container.resolve("StaticRouterModService");
        const dynamicRouterModService = container.resolve("DynamicRouterModService");
        this.logger.debug(`[${this.mod}] Loading... `);
        this.registerProfileImage(PreSptModLoader, imageRouter);
        const UpdateTime = {
            "_name": baseJson._id,
            "traderId": baseJson._id,
            "seconds": {
                "min": 1600,
                "max": 3600
            }
        };
        traderConfig.updateTime.push(UpdateTime);
        const traderRefreshRecord = {
            traderId: baseJson._id,
            seconds: { min: 1600, max: 3600 }
        };
        traderConfig.updateTime.push(traderRefreshRecord);
        Traders_1.Traders[baseJson._id] = baseJson._id;
        staticRouterModService.registerStaticRouter("HephaestusUpdateLogin", [{
                url: "/launcher/profile/login",
                action: (url, info, sessionId, output) => {
                    try {
                        const databaseServer = container.resolve("DatabaseServer");
                        const tables = databaseServer.getTables();
                        tables.traders[baseJson._id].assort = { ...this.createAssortTable(container, sessionId, true) };
                    }
                    catch (error) {
                    }
                    return output;
                }
            }], "spt");
        staticRouterModService.registerStaticRouter("HephaestusUpdateWeaponSave", [{
                url: "/client/builds/weapon/save",
                action: (url, info, sessionId, output) => {
                    try {
                        const databaseServer = container.resolve("DatabaseServer");
                        const tables = databaseServer.getTables();
                        tables.traders[baseJson._id].assort = { ...this.createAssortTable(container, sessionId) };
                    }
                    catch (error) {
                    }
                    return output;
                }
            }], "spt");
        staticRouterModService.registerStaticRouter("HephaestusUpdateBuildDelete", [{
                url: "/client/builds/delete",
                action: (url, info, sessionId, output) => {
                    try {
                        const databaseServer = container.resolve("DatabaseServer");
                        const tables = databaseServer.getTables();
                        tables.traders[baseJson._id].assort = { ...this.createAssortTable(container, sessionId) };
                    }
                    catch (error) {
                    }
                    return output;
                }
            }], "spt");
        dynamicRouterModService.registerDynamicRouter("HephaestusUpdateExplicit", [{
                url: "/client/trading/api/getTraderAssort/hephaestus_alxk",
                action: (url, info, sessionId, output) => {
                    try {
                        const databaseServer = container.resolve("DatabaseServer");
                        const tables = databaseServer.getTables();
                        tables.traders[baseJson._id].assort = { ...this.createAssortTable(container, sessionId) };
                    }
                    catch (error) {
                    }
                    return output;
                }
            }], "spt");
        // dynamicRouterModService.registerDynamicRouter(
        //     "HephaestusUpdate",
        //     [{
        //         url: "/client/trading/api/getTraderAssort/hephaestus_alxk",
        //         action: (url: string, info: any, sessionId: string, output: string) => {
        //             this.logger.info(JSON.stringify(info));
        //             try {
        //                 const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        //                 const tables = databaseServer.getTables();
        //                 tables.traders[baseJson._id].assort = this.createAssortTable(container, sessionId);
        //             } catch (error) {
        //                 this.logger.error(error.message);
        //             }
        //             return output
        //         }
        //     }], "spt"
        // );
        container.afterResolution("InventoryController", (_t, result) => {
            result.openRandomLootContainer = (pmcData, body, sessionID) => {
                this.logger.info(JSON.stringify(body));
                // return this.newOpenRandomLoot(container, pmcData, body, sessionID);
            };
        });
        this.logger.debug(`[${this.mod}] Loaded`);
    }
    postDBLoad(container) {
        const logger = container.resolve("WinstonLogger");
        logger.debug(`[${this.mod}] Delayed Loading... `);
        const databaseServer = container.resolve("DatabaseServer");
        const configServer = container.resolve("ConfigServer");
        const imageRouter = container.resolve("ImageRouter");
        const traderConfig = configServer.getConfig(ConfigTypes_1.ConfigTypes.TRADER);
        const jsonUtil = container.resolve("JsonUtil");
        const tables = databaseServer.getTables();
        const locales = Object.values(tables.locales.global);
        let base = jsonUtil.deserialize(jsonUtil.serialize(baseJson));
        base.repair = {
            "availability": true,
            "currency": config_json_1.default.currency || "569668774bdc2da2298b4568",
            "currency_coefficient": 1,
            "excluded_category": [],
            "excluded_id_list": [],
            "price_rate": 0,
            "quality": "0"
        };
        base.loyaltyLevels = [
            {
                "minLevel": 1,
                "minSalesSum": 0,
                "minStanding": 0,
                "buy_price_coef": 50,
                "repair_price_coef": 300,
                "insurance_price_coef": 10,
                "exchange_price_coef": 0,
                "heal_price_coef": 0
            }
        ];
        tables.traders[baseJson._id] = {
            assort: {
                items: [],
                barter_scheme: {},
                loyal_level_items: {}
            },
            base,
            questassort: { started: {}, success: {}, fail: {} }
        };
        // const importer = container.resolve<ImporterUtil>("ImporterUtil");
        // let profiles = importer.loadRecursive('user/profiles/');
        for (const locale of locales) {
            locale[`${baseJson._id} FullName`] = baseJson.name;
            locale[`${baseJson._id} FirstName`] = "Hephaestus";
            locale[`${baseJson._id} Nickname`] = baseJson.nickname;
            locale[`${baseJson._id} Location`] = baseJson.location;
            locale[`${baseJson._id} Description`] = "You share the reseller license of your creations to Hephaestus and in return you get a hefty discount.";
        }
        logger.debug(`[${this.mod}] Delayed Loaded`);
        //EDW KSEKINAEI TO LOADOUT SAVE
        // this.generateQuests(container, null);
    }
    resetQuests(container, sessionId) {
        let profile = container.resolve("ProfileHelper").getFullProfile(sessionId);
        const saveServer = container.resolve("SaveServer");
        let pmc = profile.characters.pmc;
        if (!pmc || !pmc.Quests) {
            // avoid crash on first game start (fresh profile)
            pmc.Quests = [];
        }
        pmc.Quests.filter((q) => (q.status > 0)).forEach((repeatedQuest) => {
            const originalId = questIds.find(qid => qid == repeatedQuest.qid);
            if (originalId) {
                pmc.Quests = pmc.Quests.map((q) => {
                    if (q.qid === originalId) {
                        return { ...repeatedQuest, qid: originalId };
                    }
                    return q;
                });
            }
        });
        pmc.BackendCounters = Object.keys(pmc.BackendCounters).reduce((acc, key) => {
            if (!questIds.some(qid => qid == pmc.BackendCounters[key].qid)) {
                acc[key] = pmc.BackendCounters[key];
            }
            return acc;
        }, {});
        if (pmc.ConditionCounters) {
            pmc.ConditionCounters.Counters =
                pmc.ConditionCounters?.Counters.filter((counter) => {
                    if (questIds.some(qid => qid == counter.qid)) {
                        return false;
                    }
                    return true;
                }) ?? [];
        }
        pmc.Quests = pmc.Quests.filter(q => !questIds.some(qid => qid == q.qid));
        // const questHelper = container.resolve("QuestHelper");
        // let pmcData = container.resolve("ProfileHelper").getPmcProfile(sessionId);
        // questIds.forEach((qid) => {
        //     questHelper.resetQuestState(pmcData, 1, qid)
        // })
        saveServer.saveProfile(sessionId);
    }
    registerProfileImage(PreSptModLoader, imageRouter) {
        // Reference the mod "res" folder
        const imageFilepath = `./${PreSptModLoader.getModPath(this.mod)}res`;
        // Register a route to point to the profile picture
        imageRouter.addRoute(baseJson.avatar.replace(".jpg", ""), `${imageFilepath}/Hephaestus.jpg`);
    }
    getPresets(container, assortTable, currency, profiles, loadExternal = false) {
        const jsonUtil = container.resolve("JsonUtil");
        const ragfairPriceService = container.resolve("RagfairPriceService");
        let pool = [];
        let weaponBuilds = [];
        for (const p in profiles) {
            if (!profiles[p]?.userbuilds)
                continue;
            weaponBuilds = weaponBuilds.concat(profiles[p].userbuilds.weaponBuilds);
        }
        let external = 0;
        if (loadExternal) {
            const PreSptModLoader = container.resolve("PreSptModLoader");
            try {
                const path = PreSptModLoader.getModPath(this.mod);
                let builds = fs_1.default.readdirSync(`./${path}/presets/`);
                let success = 0;
                builds.forEach((build) => {
                    try {
                        let temp = fs_1.default.readFileSync(`./${path}/presets/${build}`);
                        weaponBuilds = weaponBuilds.concat(JSON.parse(temp));
                        success++;
                    }
                    catch (error) {
                        this.logger.error(`loading ${build} failed, check syntax and file structure.`);
                    }
                });
                this.logger.info(`Hephaestus: Loaded/Refreshed ${success} external builds from external sources.`);
            }
            catch (error) {
                this.logger.error(error.message);
            }
        }
        this.logger.info(`Hephaestus: Loaded/Refreshed ${weaponBuilds.length} weapon builds in total.`);
        for (let wb of weaponBuilds) {
            let preItems = wb.Items;
            let id = preItems[0]._id;
            let tpl = preItems[0]._tpl;
            if (pool.includes(id)) {
                continue;
            }
            pool.push(id);
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
                "preWeapon": true,
            };
            let preItemsObj = jsonUtil.clone(preItems);
            for (let preItemObj of preItemsObj) {
                assortTable.items.push(preItemObj);
            }
            let config;
            try {
                config = require(`../config/config.json`);
            }
            catch (e) {
            }
            let price = (config || {}).cost || 712;
            try {
                price = ragfairPriceService.getDynamicOfferPriceForOffer(preItems, currency);
            }
            catch (error) {
            }
            if (config?.discount) {
                config.discount = parseFloat(config.discount);
                price = price * (1 - ((config.discount || 100) / 100));
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
        return assortTable;
    }
    // private UserPresets(container: DependencyContainer, sessionId: string) {
    //     const itemCreate = new ItemCreateHelper();
    //     itemCreate.createItems(container)
    // }
    createAssortTable(container, sessionId, loadExternal) {
        const importer = container.resolve("ImporterUtil");
        // Assort table
        let assortTable = {
            items: [],
            barter_scheme: {},
            loyal_level_items: {}
        };
        // const currency = "5449016a4bdc2d6f028b456f";//ROUBLES
        let profiles = {};
        if (sessionId) {
            let t = container.resolve("ProfileHelper").getFullProfile(sessionId);
            profiles = { [sessionId]: t };
        }
        else {
            profiles = importer.loadRecursive('user/profiles/');
        }
        try {
            assortTable = this.getPresets(container, assortTable, config_json_1.default.currency || "569668774bdc2da2298b4568", profiles, true);
        }
        catch (error) {
            // console.error(error);
        }
        return assortTable;
    }
}
module.exports = { mod: new Hephaestus() };
//# sourceMappingURL=mod.js.map