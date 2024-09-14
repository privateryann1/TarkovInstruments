import { DependencyContainer } from "tsyringe";

import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { CustomItemService } from "@spt/services/mod/CustomItemService";
import { NewItemFromCloneDetails } from "@spt/models/spt/mod/NewItemDetails";
import { IPostSptLoadMod } from "@spt/models/external/IPostSptLoadMod";
import { DatabaseServer } from "@spt/servers/DatabaseServer";
import { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";

import {
    Preset,
    Item,
    ConfigItem,
    traderIDs,
    currencyIDs,
    allBotTypes,
    inventorySlots
} from "./references/configConsts";

import { ItemMap } from "./references/items";

class Mod implements IPostDBLoadMod, IPostSptLoadMod
{
    private database: IDatabaseTables;
    private configs: ConfigItem;

    public postDBLoad(container: DependencyContainer): void
    {
        this.database = container.resolve<DatabaseServer>("DatabaseServer").getTables();

        const CustomItem = container.resolve<CustomItemService>("CustomItemService");

        const ExampleCloneItem: NewItemFromCloneDetails = {
            itemTplToClone: "65ca457b4aafb5d7fc0dcb5d",
            overrideProperties: {
			    ExaminedByDefault: true,
			    Width: 3,
			    Height: 2,
			    Weight: 0.87,
			    Prefab: {
			    	path: "WeaponGuitar/guitar.bundle",
			    	rcid: ""
			    }
		    },
            parentId: "5447e1d04bdc2dff2f8b4567",
            newId: "66b9de8cg34d905aa32b5f60c",
            fleaPriceRoubles: 50000,
            handbookPriceRoubles: 42500,
            handbookParentId: "5b5f7a0886f77409407a7f96",
            locales: {
			    en: {
			    	name: "Acoustic Guitar",
			    	shortName: "Guitar",
			    	description: "A playable acoustic guitar."
			    }
		    },
            addtoTraders: true,
		    traderId: "RAGMAN",
		    traderItems: [
		    	{
		    		unlimitedCount: true,
		    		stackObjectsCount: 99
		    	}
		    ],
		    barterScheme: [
		    	{
		    		count: 55000,
		    		_tpl: "ROUBLES"
		    	}
		    ],
		    loyallevelitems: 2,
        };

        this.processTraders(ExampleCloneItem, ExampleCloneItem.newId);

        CustomItem.createItemFromClone(ExampleCloneItem);
    }

    private processTraders(
        itemConfig: ConfigItem[string],
        itemId: string
    ): void
    {
        const tables = this.database;
        if (!itemConfig.addtoTraders)
        {
            return;
        }

        const { traderId, traderItems, barterScheme } = itemConfig;

        const traderIdFromMap = traderIDs[traderId];
        const finalTraderId = traderIdFromMap || traderId;
        const trader = tables.traders[finalTraderId];

        if (!trader)
        {
            return;
        }

        for (const item of traderItems)
        {
            const newItem = {
                _id: itemId,
                _tpl: itemId,
                parentId: "hideout",
                slotId: "hideout",
                upd: {
                    UnlimitedCount: item.unlimitedCount,
                    StackObjectsCount: item.stackObjectsCount
                }
            };

            trader.assort.items.push(newItem);
        }

        trader.assort.barter_scheme[itemId] = [];

        for (const scheme of barterScheme)
        {
            const count = scheme.count;
            const tpl = currencyIDs[scheme._tpl] || ItemMap[scheme._tpl];

            if (!tpl)
            {
                throw new Error(
                    `Invalid _tpl value in barterScheme for item: ${itemId}`
                );
            }

            trader.assort.barter_scheme[itemId].push([
                {
                    count: count,
                    _tpl: tpl
                }
            ]);
        }

        trader.assort.loyal_level_items[itemId] = itemConfig.loyallevelitems;
    }
}

export const mod = new Mod();