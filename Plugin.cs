using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using d = DataLoaderPlugin.Dto;
using System;
using UnityEngine;

namespace DataLoaderPlugin
{

    [BepInPlugin("bepinex.plugins.stacklands.dataloader", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Stacklands.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

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

            CardUtility cardUtility = new CardUtility(__instance);

            load_boosters(__instance);
            load_harvestable(__instance, cardUtility);
            load_mobs(__instance, cardUtility);
            blue_prints(__instance, cardUtility);
            travelling_cart(__instance, cardUtility);
            treasure_chest(__instance, cardUtility);
        }

        private static void treasure_chest(WorldManager worldManager, CardUtility cardUtility)
        {
            Plugin.Log.LogInfo($"Simulate Treasure Chest");
            List<CardData> cards = (from x in WorldManager.instance.CardDataPrefabs
                                    where (x.MyCardType == CardType.Resources || x.MyCardType == CardType.Food) && !x.IsIslandCard
                                    select x).ToList<CardData>();
            cards.RemoveAll((CardData x) => x.Id == "goblet");
            float chance = 1 / (float)cards.Count;

            const string TreasureChestId = "treasure_chest";
            const string KeyId = "key";
            d.ChanceContainer container = new d.ChanceContainer(TreasureChestId, cardUtility);                

            container.Inp.Add(new d.ResourceCount(TreasureChestId, cardUtility, 1));
            container.Inp.Add(new d.ResourceCount(KeyId, cardUtility, 1));

            foreach (var card in cards)
            {
                container.ChanceCards.Add(new d.ItemChance(card.Name, card.Id, cardUtility.GetColors(card), chance));
            }
            WriteYaml("treasure-chest-recipes.yaml", container);
        }

        private static void travelling_cart(WorldManager worldManager, CardUtility cardUtility)
        {
            Plugin.Log.LogInfo($"Load Travelling cart");
            var travelling_cart = (TravellingCart)worldManager.GetCardPrefab("travelling_cart");

            //The single card Yaml format
            d.ChanceContainer container = new d.ChanceContainer(travelling_cart, cardUtility);

            container.Inp.Add(new d.ResourceCount(travelling_cart.Name, travelling_cart.Id, cardUtility.GetColors(travelling_cart), 1));
            container.Inp.Add(new d.ResourceCount("Coin", "coin", cardUtility.GetColors(travelling_cart),
                1));

            var new_chances = get_bag_chances(travelling_cart.MyCardBag);
            foreach (var cardChance in new_chances)
            {
                container.ChanceCards.Add(
                    new d.ItemChance(cardChance.Key, cardUtility, cardChance.Value));
            }

            WriteYaml("traveling-cart-recipes.yaml", new[] { container });
        }

        private static void load_boosters(WorldManager worldManager)
        {
            Plugin.Log.LogInfo($"Load Boosters");
            var boosterPackPrefabs = worldManager.GameDataLoader.BoosterPackPrefabs;

            string path = @"boosters-recipes.yaml";
            StreamWriter sw = File.CreateText(path);

            foreach (var booster_pref in boosterPackPrefabs)
            {
                foreach (var card_chance in calc_booster_chances(booster_pref))
                {
                    sw.Write($"  {card_chance.Key}: {card_chance.Value}\n");
                }
            }

            sw.Close();
        }

        private static void load_harvestable(WorldManager worldManager, CardUtility cardUtility)
        {
            Plugin.Log.LogInfo($"Load Harvestable");
            var cards = worldManager.GameDataLoader.CardDataPrefabs;
            var harvestable_cards = (from x in cards
                                     where typeof(Harvestable).IsInstanceOfType(x)
                                     select (Harvestable)x).ToList<Harvestable>();

            List<d.TimedProducerCard> producers = new List<d.TimedProducerCard>();

            foreach (var card in harvestable_cards)
            {

                //Finish Here - Convert this to card data.  Create the base class info to match.
                d.TimedProducerCard producer = new d.TimedProducerCard(card.Id, card.Name, card.HarvestTime,
                    new List<d.ItemChance>(), cardUtility.GetColors(card));

                //Bug:  There are some blank cards.  Research why.
                //  Examples: Cave, Catacombs
                foreach (var card_chance in get_bag_chances(card.MyCardBag)
                    .Where(x => !string.IsNullOrEmpty(x.Key)))
                {
                    producer.ItemChances.Add(new d.ItemChance(card_chance.Key, cardUtility,card_chance.Value));
                }

                producers.Add(producer);
            }


            var combatable_harvestable_cards = (
                from x in cards
                where typeof(CombatableHarvestable).IsInstanceOfType(x)
                select (CombatableHarvestable)x)
                .ToList<CombatableHarvestable>();

            foreach (var card in combatable_harvestable_cards)
            {
                d.TimedProducerCard producer = new d.TimedProducerCard(card.Id, card.Name, card.HarvestTime,
                    new List<d.ItemChance>(), cardUtility.GetColors(card));

                foreach (var card_chance in get_bag_chances(card.MyCardBag))
                {
                    producer.ItemChances.Add(new d.ItemChance(card_chance.Key, cardUtility , card_chance.Value));
                }
            }

            WriteYaml("harvestable.yaml", producers);

        }

        private static void load_mobs(WorldManager worldManager, CardUtility cardUtility)
        {
            Plugin.Log.LogInfo($"Load Mobs");
            var cards = worldManager.GameDataLoader.CardDataPrefabs;
            var mobs_cards = (from x in cards
                              where typeof(Mob).IsInstanceOfType(x)
                              select (Mob)x).ToList<Mob>();


            string csv_path = @"mobs.csv";
            StreamWriter csv = File.CreateText(csv_path);
            csv.Write($"Name,drop_count\n");

            List<d.AnimalRecipe> animalRecipies = new List<Dto.AnimalRecipe>();
            List<d.ProducerCard> mobs = new List<d.ProducerCard>();

                
            foreach (var card in mobs_cards)
            {
                d.CardColors mobCardColors = cardUtility.GetColors(card);

                if (typeof(Animal).IsInstanceOfType(card))
                {
                    var animal = (Animal)card;
                    if (animal.CreateCard != "")
                    {
                        d.AnimalRecipe animalRecipe = new d.AnimalRecipe(
                            new d.ResourceCount(card.Id, card.Name, 
                                mobCardColors, 1),
                            new d.ResourceCount(animal.CreateCard, cardUtility, 1 ),
                            animal.CreateTime,
                            mobCardColors);

                        animalRecipies.Add(animalRecipe);
                    }
                }


                csv.Write($"{card.Id},");
                csv.Write($"{card.Drops.CardsInPack}\n");

                d.ProducerCard mob = new d.ProducerCard(card.Id, card.Name, new List<d.ItemChance>(),
                    mobCardColors);

                foreach (var card_chance in get_bag_chances(card.Drops))
                {
                    mob.ItemChances.Add(new d.ItemChance(card_chance.Key, cardUtility,card_chance.Value));
                }
                mobs.Add(mob);
            }

            csv.Close();

            //--- Write YAML

            WriteYaml("mobs.yaml", mobs);
            WriteYaml(@"animals_recipes.yaml", animalRecipies);

        }

        private static void WriteYaml<T>(string filePath, T item)
        {
            ISerializer seralizer = new SerializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .DisableAliases()
                .Build();

            File.WriteAllText(filePath, seralizer.Serialize(item));
        }

        private static void blue_prints(WorldManager worldManager, CardUtility cardUtility)
        {
            Plugin.Log.LogInfo($"Load Blueprints");
            var blueprints = worldManager.GameDataLoader.BlueprintPrefabs;

            List<d.Blueprint> yamlBlueprints = new List<d.Blueprint>();

            foreach (var blueprint in blueprints)
            {

                foreach (var subprint in blueprint.Subprints)
                {
                    d.Blueprint yamlBlueprint = new d.Blueprint()
                    {
                        Name = blueprint.Name,
                        Id = blueprint.Id,
                    };

                    yamlBlueprint.Inp = GetResources(cardUtility, subprint.RequiredCards);

                    if (string.IsNullOrEmpty(subprint.ResultCard))
                    {
                        
                        yamlBlueprint.Out = GetResources(cardUtility, subprint.ExtraResultCards);
                    }
                    else
                    {
                        yamlBlueprint.Out = GetResources(cardUtility, new[] { subprint.ResultCard });
                    }

                    yamlBlueprint.Time = subprint.Time;
                    yamlBlueprints.Add(yamlBlueprint);
                }
            }

            //--- Write YAML
            WriteYaml("recipes.yaml", yamlBlueprints);

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
                foreach (var cardChance in chances)
                {
                    result[cardChance.Id] = (float)cardChance.Chance / (float)chance_sum;
                }
                return result;
            }
        }
        private static Dictionary<string, float> calc_booster_chances(Boosterpack booster)
        {
            var result = new Dictionary<string, float>();
            for (int i = 1; i <= booster.TotalCardsInPack; i++)
            {
                CardBag currentCardBag = get_bag(booster, i);
                var new_chances = get_bag_chances(currentCardBag);
                foreach (var cardChance in new_chances)
                {
                    if (result.ContainsKey(cardChance.Key))
                    {
                        result[cardChance.Key] += (1 - result[cardChance.Key]) * cardChance.Value;
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
            foreach (var bag in booster.CardBags)
            {
                for (int i = 0; i < bag.CardsInPack; i++)
                {
                    tap--;
                    if (tap == 0)
                    {
                        return bag;
                    }
                }
            }
            throw new System.Exception("bag overflow");
        }

        private static List<d.ResourceCount> GetResources(CardUtility cardUtility,  string [] cards)
        {
            return cards.Distinct().Select(cardId =>
            {
                return new d.ResourceCount(cardId, cardUtility, cards.Count(x => x == cardId));
            }).ToList();
        }

    }
}
