import { NewItemDetails } from "@spt/models/spt/mod/NewItemDetails";

const walletGamble: NewItemDetails = {
    newItem: {
        _id: "a_gambling_wallet",
        _name: "gambling_container_wallet",
        _parent: "62f109593b54472778797866",
        _props: {
            "AnimationVariantsNumber": 0,
            "BackgroundColor": "orange",
            "BlocksArmorVest": false,
            "CanPutIntoDuringTheRaid": true,
            "CanRequireOnRagfair": false,
            "CanSellOnRagfair": false,
            "CantRemoveFromSlotsDuringRaid": [],
            "ConflictingItems": [],
            "Description": "Mystery Wallet",
            "DiscardLimit": -1,
            "DiscardingBlock": false,
            "DropSoundType": "None",
            "ExamineExperience": 100,
            "ExamineTime": 1,
            "ExaminedByDefault": true,
            "ExtraSizeDown": 0,
            "ExtraSizeForceAdd": false,
            "ExtraSizeLeft": 0,
            "ExtraSizeRight": 0,
            "ExtraSizeUp": 0,
            "Grids": [
                {
                    "_id": "6489c03c8bc5233fdc78e789",
                    "_name": "main",
                    "_parent": "6489c03c8bc5233fdc78e788",
                    "_props": {
                        "cellsH": 1,
                        "cellsV": 1,
                        "filters": [
                            {
                                "ExcludedFilter": [
                                    "54009119af1c881c07000029"
                                ],
                                "Filter": []
                            }
                        ],
                        "isSortingTable": false,
                        "maxCount": 99,
                        "maxWeight": 0,
                        "minCount": 1
                    },
                    "_proto": "55d329c24bdc2d892f8b4567"
                }
            ],
            "Height": 1,
            "HideEntrails": true,
            "InsuranceDisabled": false,
            "IsAlwaysAvailableForInsurance": false,
            "IsLockedafterEquip": false,
            "IsSpecialSlotOnly": false,
            "IsUnbuyable": false,
            "IsUndiscardable": false,
            "IsUngivable": false,
            "IsUnremovable": false,
            "IsUnsaleable": false,
            "ItemSound": "container_plastic",
            "LootExperience": 20,
            "MergesWithChildren": false,
            "Name": "Mystery Wallet",
            "NotShownInSlot": false,
            "Prefab": {
                "path": "assets/content/items/barter/item_barter_walletwz/item_barter_walletwz.bundle",
                "rcid": ""
            },
            "QuestItem": false,
            "QuestStashMaxCount": 0,
            "RagFairCommissionModifier": 1,
            "RepairCost": 0,
            "RepairSpeed": 0,
            "SearchSound": "drawer_metal_looting",
            "ShortName": "Mystery Wallet",
            "Slots": [],
            "StackMaxSize": 1,
            "StackObjectsCount": 1,
            "Unlootable": false,
            "UnlootableFromSide": [],
            "UnlootableFromSlot": "FirstPrimaryWeapon",
            "UsePrefab": {
                "path": "",
                "rcid": ""
            },
            "Weight": 2,
            "Width": 1,
            "ReverbVolume": 0
        },
        _proto: "",
        _type: "Item"
             
    },
    fleaPriceRoubles: 110000,
    handbookPriceRoubles: 110000,
    handbookParentId: "5b5f6fa186f77409407a7eb7",
    locales: {
        "en": {
            name: "Mystery Wallet",
            shortName: "Mystery Wallet",
            description: `Wager your Roubles to win more, or lose it all!\n==============================\n0 Roubles - 60.0%\n100k Roubles - ${this.config.wallet_common}%\n300k Roubles - ${this.config.wallet_uncommon}%\n500k Roubles - ${this.config.wallet_rare}%\n2 Million Roubles - ${this.config.wallet_extremely_rare}%`
        }
    } 
}