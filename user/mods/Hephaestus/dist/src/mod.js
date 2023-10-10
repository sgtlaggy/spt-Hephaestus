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
Object.defineProperty(exports, "__esModule", { value: true });
const ConfigTypes_1 = require("C:/snapshot/project/obj/models/enums/ConfigTypes");
const baseJson = __importStar(require("../db/base.json"));
class SampleTrader {
    constructor() {
        this.mod = "Hephaestus";
    }
    // Perform these actions before server fully loads
    preAkiLoad(container) {
        this.logger = container.resolve("WinstonLogger");
        const preAkiModLoader = container.resolve("PreAkiModLoader");
        const imageRouter = container.resolve("ImageRouter");
        const configServer = container.resolve("ConfigServer");
        const traderConfig = configServer.getConfig(ConfigTypes_1.ConfigTypes.TRADER);
        const staticRouterModService = container.resolve("StaticRouterModService");
        this.logger.debug(`[${this.mod}] Loading... `);
        this.registerProfileImage(preAkiModLoader, imageRouter);
        this.setupTraderUpdateTime(container);
        staticRouterModService.registerStaticRouter("HephaestusUpdateLogin", [{
                url: "/launcher/profile/login",
                action: (url, info, sessionId, output) => {
                    // 
                    const databaseServer = container.resolve("DatabaseServer");
                    const tables = databaseServer.getTables();
                    tables.traders[baseJson._id].assort = this.createAssortTable(container, sessionId);
                    return output;
                }
            }], "aki");
        staticRouterModService.registerStaticRouter("HephaestusUpdate", [{
                url: "/client/game/profile/items/moving",
                action: (url, info, sessionId, output) => {
                    // 
                    if (info.data[0].Action != 'Examine') {
                        const databaseServer = container.resolve("DatabaseServer");
                        const tables = databaseServer.getTables();
                        tables.traders[baseJson._id].assort = this.createAssortTable(container, sessionId);
                    }
                    return output;
                }
            }], "aki");
        this.logger.debug(`[${this.mod}] Loaded`);
    }
    postDBLoad(container) {
        this.logger.debug(`[${this.mod}] Delayed Loading... `);
        const databaseServer = container.resolve("DatabaseServer");
        const jsonUtil = container.resolve("JsonUtil");
        // Keep a reference to the tables
        const tables = databaseServer.getTables();
        // Add the new trader to the trader lists in DatabaseServer
        tables.traders[baseJson._id] = {
            assort: {
                items: [],
                barter_scheme: {},
                loyal_level_items: {}
            },
            base: jsonUtil.deserialize(jsonUtil.serialize(baseJson)),
            questassort: undefined
        };
        // For each language, add locale for the new trader
        const locales = Object.values(tables.locales.global);
        for (const locale of locales) {
            locale[`${baseJson._id} FullName`] = baseJson.name;
            locale[`${baseJson._id} FirstName`] = "Hephaestus";
            locale[`${baseJson._id} Nickname`] = baseJson.nickname;
            locale[`${baseJson._id} Location`] = baseJson.location;
            locale[`${baseJson._id} Description`] = "You share the reseller license of your creations to Hephaestus and in return you get a hefty discount.";
        }
        this.logger.debug(`[${this.mod}] Delayed Loaded`);
    }
    registerProfileImage(preAkiModLoader, imageRouter) {
        // Reference the mod "res" folder
        const imageFilepath = `./${preAkiModLoader.getModPath(this.mod)}res`;
        // Register a route to point to the profile picture
        imageRouter.addRoute(baseJson.avatar.replace(".jpg", ""), `${imageFilepath}/img.jpg`);
    }
    setupTraderUpdateTime(container) {
        // Add refresh time in seconds when Config server allows to set configs
        const configServer = container.resolve("ConfigServer");
        const traderConfig = configServer.getConfig(ConfigTypes_1.ConfigTypes.TRADER);
        const traderRefreshConfig = { traderId: baseJson._id, seconds: 3600 };
        traderConfig.updateTime.push(traderRefreshConfig);
    }
    getPresets(container, assortTable, currency, profiles) {
        const jsonUtil = container.resolve("JsonUtil");
        const ragfairPriceService = container.resolve("RagfairPriceService");
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
                    "preWeapon": true
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
                    price = ragfairPriceService.getDynamicOfferPrice(preItems, currency);
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
        }
        ;
        return assortTable;
    }
    getContainers(container, assortTable, currency, profiles) {
        for (let p in (profiles || [])) {
        }
    }
    createAssortTable(container, sessionId) {
        const importer = container.resolve("ImporterUtil");
        // Assort table
        let assortTable = {
            items: [],
            barter_scheme: {},
            loyal_level_items: {}
        };
        // const currency = "5449016a4bdc2d6f028b456f";//ROUBLES
        let currency = "569668774bdc2da2298b4568";
        let config;
        try {
            config = require(`../config/config.json`);
        }
        catch (e) {
        }
        let profiles = {};
        if (sessionId) {
            let t = container.resolve("ProfileHelper").getFullProfile(sessionId);
            profiles = { [sessionId]: t };
        }
        else {
            profiles = importer.loadRecursive('user/profiles/');
        }
        try {
            assortTable = this.getPresets(container, assortTable, (config || {}).currency || currency, profiles);
            // fs.writeFileSync(path.join(__dirname, '..', "/db/test.json"),JSON.stringify(assortTable.items))
        }
        catch (error) {
            console.error(error);
        }
        return assortTable;
    }
}
module.exports = { mod: new SampleTrader() };
