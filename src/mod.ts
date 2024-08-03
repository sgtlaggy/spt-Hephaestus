/* eslint-disable @typescript-eslint/no-unused-vars */
/* eslint-disable @typescript-eslint/indent */
import { ProfileHelper } from "@spt/helpers/ProfileHelper";
import { PreSptModLoader } from "@spt/loaders/PreSptModLoader";
import { ITraderAssort, ITraderBase } from "@spt/models/eft/common/tables/ITrader";
import { IWeaponBuild } from "@spt/models/eft/profile/ISptProfile";
import { ConfigTypes } from "@spt/models/enums/ConfigTypes";
import { Traders } from "@spt/models/enums/Traders";
import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import { IPostSptLoadMod } from "@spt/models/external/IPostSptLoadMod";
import { ITraderConfig, UpdateTime } from "@spt/models/spt/config/ITraderConfig";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { ImageRouter } from "@spt/routers/ImageRouter";
import { ConfigServer } from "@spt/servers/ConfigServer";
import { DatabaseServer } from "@spt/servers/DatabaseServer";
import type { DynamicRouterModService } from "@spt/services/mod/dynamicRouter/DynamicRouterModService";
import { RagfairPriceService } from "@spt/services/RagfairPriceService";
import { JsonUtil } from "@spt/utils/JsonUtil";
import { DependencyContainer } from "tsyringe";

import fs from "fs";

import config from "../config/config.json";
import * as baseJson from "../db/base.json";

class Hephaestus implements IPreSptLoadMod, IPostDBLoadMod, IPostSptLoadMod {
    mod: string
    private logger: ILogger;
    constructor() {
        this.mod = "Hephaestus";

    }

    // Perform these actions before server fully loads
    public preSptLoad(container: DependencyContainer): void {
        this.logger = container.resolve<ILogger>("WinstonLogger");
        const PreSptModLoader: PreSptModLoader = container.resolve<PreSptModLoader>("PreSptModLoader");
        const imageRouter: ImageRouter = container.resolve<ImageRouter>("ImageRouter");
        const configServer = container.resolve<ConfigServer>("ConfigServer");
        const traderConfig: ITraderConfig = configServer.getConfig<ITraderConfig>(ConfigTypes.TRADER);

        const dynamicRouterModService = container.resolve<DynamicRouterModService>("DynamicRouterModService");
        this.logger.debug(`[${this.mod}] Loading... `);
        this.registerProfileImage(PreSptModLoader, imageRouter);
        const UpdateTime =
        {
            "_name": baseJson._id,
            "traderId": baseJson._id,
            "seconds":
            {
                "min": 1600,
                "max": 3600
            }
        }
        traderConfig.updateTime.push(UpdateTime);
        const traderRefreshRecord: UpdateTime = {
            traderId: baseJson._id,
            seconds: { min: 1600, max: 3600 }
        };

        traderConfig.updateTime.push(traderRefreshRecord);
        Traders[baseJson._id] = baseJson._id;
        const routeAction = async (_: string, __: any, ___: string, output: string) => {
            try {
                const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
                const tables = databaseServer.getTables();
                tables.traders[baseJson._id].assort = this.createAssortTable(container);
            } catch (error) {
            }
            return output
        };
        dynamicRouterModService.registerDynamicRouter(
            "HephaestusUpdateExplicit",
            [{
                url: `/client/trading/api/getTraderAssort/${baseJson._id}`,
                action: routeAction
            }], "spt"
        );

        this.logger.debug(`[${this.mod}] Loaded`);
    }

    public postDBLoad(container: DependencyContainer): void {
        const logger = container.resolve<ILogger>("WinstonLogger");
        logger.debug(`[${this.mod}] Delayed Loading... `);
        const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        const jsonUtil = container.resolve<JsonUtil>("JsonUtil");
        const tables = databaseServer.getTables();
        const locales = Object.values(tables.locales.global) as Record<string, string>[];

        let base = jsonUtil.deserialize(jsonUtil.serialize(baseJson)) as ITraderBase;
        tables.traders[baseJson._id] = {
            assort: {
                items: [],
                barter_scheme: {},
                loyal_level_items: {},
                nextResupply: 0
            },
            base: base,
            questassort: { started: {}, success: {}, fail: {} }
        };


        for (const locale of locales) {
            locale[`${baseJson._id} FullName`] = baseJson.name;
            locale[`${baseJson._id} FirstName`] = "Hephaestus";
            locale[`${baseJson._id} Nickname`] = baseJson.nickname;
            locale[`${baseJson._id} Location`] = baseJson.location;
            locale[`${baseJson._id} Description`] = "You share the reseller license of your creations to Hephaestus and in return you get a hefty discount.";
        }
        logger.debug(`[${this.mod}] Delayed Loaded`);
    }

    public postSptLoad(container: DependencyContainer): void {
        const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        const tables = databaseServer.getTables();
        tables.traders[baseJson._id].assort = this.createAssortTable(container);
    }

    private registerProfileImage(PreSptModLoader: PreSptModLoader, imageRouter: ImageRouter): void {
        // Reference the mod "res" folder
        const imageFilepath = `./${PreSptModLoader.getModPath(this.mod)}res`;
        // Register a route to point to the profile picture
        imageRouter.addRoute(baseJson.avatar.replace(".jpg", ""), `${imageFilepath}/Hephaestus.jpg`);
    }

    private createAssortTable(container: DependencyContainer): ITraderAssort {
        const ragfairPriceService = container.resolve<RagfairPriceService>("RagfairPriceService");

        let assortTable: ITraderAssort = {
            items: [],
            barter_scheme: {},
            loyal_level_items: {},
            nextResupply: 0
        }

        let playerbuilds = 0;
        let externalBuilds = 0;

        const fileLevelPattern = /^.*\.([1-4])\.json$/;
        const presetLevelPattern = /^.*\.([1-4])$/;
        function getLevel(name: string, pattern: RegExp, defaultLevel: number): number {
            const match = name.match(pattern);
            return match ? parseInt(match[1]) : defaultLevel;
        }

        // player presets
        const profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
        for (const profile of Object.values(profileHelper.getProfiles())) {
            for (const preset of profile.userbuilds.weaponBuilds) {
                this.addPreset(ragfairPriceService, assortTable, preset, getLevel(preset.Name, presetLevelPattern, 1));
                playerbuilds++;
            }
        }

        // file presets
        const PreSptModLoader: PreSptModLoader = container.resolve<PreSptModLoader>("PreSptModLoader");
        const path = PreSptModLoader.getModPath(this.mod);
        let builds = fs.readdirSync(`./${path}/presets/`);
        builds.forEach((build) => {
            const fileLevel = getLevel(build, fileLevelPattern, 4);
            try {
                const fileContent = fs.readFileSync(`./${path}/presets/${build}`, { encoding: "utf8" });
                const presets: IWeaponBuild[] = JSON.parse(fileContent);
                for (const preset of presets) {
                    this.addPreset(ragfairPriceService, assortTable, preset, getLevel(preset.Name, presetLevelPattern, fileLevel));
                    externalBuilds++;
                }
            } catch (error) {
                this.logger.error(`[${this.mod}] Loading ${build} failed, check syntax and file structure.`);
            }
        }, this);
        this.logger.info(`[${this.mod}] Loaded/refreshed ${playerbuilds} player builds and ${externalBuilds} external builds.`);

        return assortTable;
    }

    private addPreset(ragfairPriceService: RagfairPriceService, assortTable: ITraderAssort, preset: IWeaponBuild, loyaltyLevel: number) {
        let preItems = preset.Items;
        let id = preItems[0]._id;
        let tpl = preItems[0]._tpl;

        preItems[0] = {
            "_id": id,
            "_tpl": tpl,
            "parentId": "hideout",
            "slotId": "hideout",
            "upd": {
                "UnlimitedCount": true,
                "StackObjectsCount": 2000
            },
        };
        let preItemsObj = structuredClone(preItems);
        for (let preItemObj of preItemsObj) {
            assortTable.items.push(preItemObj);
        }

        let price = config.cost
        try {
            price = ragfairPriceService.getDynamicOfferPriceForOffer(preItems, config.currency, false);
        } catch (error) {}
        price = price * (1 - (config.discount / 100))
        let offerRequire = [
            {
                "count": price,
                "_tpl": config.currency
            }
        ];
        assortTable.barter_scheme[id] = [offerRequire];
        assortTable.loyal_level_items[id] = loyaltyLevel;
    }
}

module.exports = { mod: new Hephaestus() }
