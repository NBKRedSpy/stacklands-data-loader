using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using static Harvestable;

namespace DataLoaderPlugin
{
    [BepInPlugin("bepinex.plugins.stacklands.dataloader", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Stacklands.exe")]
    public class Plugin : BaseUnityPlugin
    {
        static ManualLogSource Log;

        private void Awake()
        {
            Plugin.Log = base.Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }
    

        [HarmonyPatch(typeof(WorldManager), "Play")]
        [HarmonyPostfix] 
        private static void WorldManager__Load(ref WorldManager __instance)
        {
            load_boosters(__instance);
            load_harvestable(__instance);
            load_mobs(__instance);
            blue_prints(__instance);
        }

        private static void load_boosters(WorldManager worldManager)
        {
            Plugin.Log.LogInfo($"Load Boosters");
            var boosterPackPrefabs = worldManager.GameDataLoader.BoosterPackPrefabs;

            string path = @"boosters-recipes.yaml";
            StreamWriter sw = File.CreateText(path);

            foreach(var booster_pref in boosterPackPrefabs)
            {
                Plugin.Log.LogInfo($"{booster_pref.Name}:");
                foreach(var card_chance in calc_booster_chances(booster_pref))
                {
                    sw.Write($"  {card_chance.Key}: {card_chance.Value}\n");
                }
            }

            sw.Close();
        }

        private static void load_harvestable(WorldManager worldManager)
        {
            Plugin.Log.LogInfo($"Load Harvestable");
            var cards = worldManager.GameDataLoader.CardDataPrefabs;
            var harvestable_cards = (from x in cards
                where typeof(Harvestable).IsInstanceOfType(x)
                select (Harvestable)x).ToList<Harvestable>();

            string path = @"harvestable.yaml";
            StreamWriter sw = File.CreateText(path);

            foreach(var card in harvestable_cards)
            {
                sw.Write($"{card.Id}:\n");
                sw.Write($"  cards:\n");
                foreach(var card_chance in get_bag_chances(card.MyCardBag))
                {
                    sw.Write($"    {card_chance.Key}: {card_chance.Value}\n");
                }
                sw.Write($"  time: {card.HarvestTime}\n");
            }

            var combatable_harvestable_cards = (from x in cards
                where typeof(CombatableHarvestable).IsInstanceOfType(x)
                select (CombatableHarvestable)x).ToList<CombatableHarvestable>();
            foreach(var card in combatable_harvestable_cards)
            {
                sw.Write($"{card.Id}:\n");
                sw.Write($"  cards:\n");
                foreach(var card_chance in get_bag_chances(card.MyCardBag))
                {
                    sw.Write($"    {card_chance.Key}: {card_chance.Value}\n");
                }
                sw.Write($"  time: {card.HarvestTime}\n");
            }

            sw.Close();
        }

        private static void load_mobs(WorldManager worldManager)
        {
            Plugin.Log.LogInfo($"Load Mobs");
            var cards = worldManager.GameDataLoader.CardDataPrefabs;
            var mobs_cards = (from x in cards
                where typeof(Mob).IsInstanceOfType(x)
                select (Mob)x).ToList<Mob>();

            string path = @"mobs.yaml";
            StreamWriter sw = File.CreateText(path);

            StreamWriter animals_recipes = File.CreateText(@"animals_recipes.yaml");

            string csv_path = @"mobs.csv";
            StreamWriter csv = File.CreateText(csv_path);
            csv.Write($"Name,drop_count\n");

            foreach(var card in mobs_cards)
            {
                if (typeof(Animal).IsInstanceOfType(card))
                {
                    var animal = (Animal)card;
                    if (animal.CreateCard != "")
                    {
                        animals_recipes.Write($"-\n");
                        animals_recipes.Write($"  inp: {{{card.Id}: 1}}\n");
                        animals_recipes.Write($"  out: {{{animal.CreateCard}: 1}}\n");
                        animals_recipes.Write($"  time: {animal.CreateTime}\n");
                    }
                }
                csv.Write($"{card.Id},");
                csv.Write($"{card.Drops.CardsInPack}\n");
                sw.Write($"{card.Id}:\n");
                foreach(var card_chance in get_bag_chances(card.Drops))
                {
                    sw.Write($"  {card_chance.Key}: {card_chance.Value}\n");
                }
            }
            animals_recipes.Close();
            csv.Close();
            sw.Close();
        }

        private static void blue_prints(WorldManager worldManager)
        {
            Plugin.Log.LogInfo($"Load Blueprints");
            var blueprints = worldManager.GameDataLoader.BlueprintPrefabs;

            string path = @"recipes.yaml";
            StreamWriter sw = File.CreateText(path);

            foreach(var blueprint in blueprints)
            {
                foreach(var subprint in blueprint.Subprints)
                {
                    sw.Write($"-\n");
                    sw.Write("  inp: {");
                    print_cards(sw, subprint.RequiredCards);
                    sw.Write("}\n");

                    if (string.IsNullOrEmpty(subprint.ResultCard))
                    {
                        sw.Write("  out: {");
                        print_cards(sw, subprint.ExtraResultCards);
                        sw.Write("}\n");
                    }
                    else
                    {
                        sw.Write($"  out: {{{subprint.ResultCard}: 1}}\n");
                    }

                    sw.Write($"  time: {subprint.Time}\n");
                }
            }
            sw.Close();
        }

        private static Dictionary<string, float> get_bag_chances(CardBag bag)
        {
            var result = new Dictionary<string, float>();
            if (bag.CardBagType == CardBagType.SetPack)
            {
                var card_name = bag.SetPackCards[bag.SetPackCards.Count - bag.CardsInPack];
                result.Add(card_name, 100.0f);
                return result;
            }
            else
            {
                var chances = new List<CardChance>();
                if (bag.CardBagType == CardBagType.Chances)
                {
                    chances = bag.Chances;
                }
                else if (bag.CardBagType == CardBagType.SetCardBag)
                {
                    if (bag.UseFallbackBag)
                    {
                        chances = CardBag.GetChancesForSetCardBag(WorldManager.instance.GameDataLoader, bag.SetCardBag, new SetCardBag?(bag.FallbackBag));
                    }
                    else
                    {
                        chances = CardBag.GetChancesForSetCardBag(WorldManager.instance.GameDataLoader, bag.SetCardBag, null);
                    }
                }
                var chance_sum = Enumerable.Sum(chances, (CardChance x) => x.Chance);
                foreach(var cardChance in chances)
                {
                    result[cardChance.Id]= (float)cardChance.Chance / (float)chance_sum;
                }
                return result;
            }
        }
        private static Dictionary<string, float> calc_booster_chances(Boosterpack booster)
        {
            var result = new Dictionary<string, float>();
            for(int i = 1; i <= booster.TotalCardsInPack; i++)
            {
                CardBag currentCardBag = get_bag(booster, i);
                var new_chances = get_bag_chances(currentCardBag);
                foreach(var cardChance in new_chances)
                {
                    if(result.ContainsKey(cardChance.Key))
                    {
                        result[cardChance.Key] += ( 1- result[cardChance.Key]) * cardChance.Value;
                    }
                    else
                    {
                        result[cardChance.Key] = cardChance.Value;
                    }
                }
            }
            return result;
        }

        private static CardBag get_bag(Boosterpack booster, int tap_number)
        {
            int tap = tap_number;
            foreach(var bag in booster.CardBags)
            {
                for(int i = 0; i < bag.CardsInPack; i++)
                {
                    tap--;
                    if(tap == 0)
                    {
                        return bag;
                    }
                }
            }
            throw new System.Exception("bag overflow");
        }

    private static void print_cards(StreamWriter stream, string[] cards)
    {
        var inputs = new Dictionary<string, int>();
        foreach(var card in cards)
        {
            if (inputs.ContainsKey(card))
            {
                inputs[card] += 1;
            }
            else
            {
                inputs[card] = 1;
            }
        }
        foreach(var card in inputs)
        {
            stream.Write($"{card.Key}: {card.Value}, ");
        }
    }
    }

}
