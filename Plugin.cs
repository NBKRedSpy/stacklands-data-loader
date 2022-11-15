using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using d = DataLoaderPlugin.Dto;
using System;

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


        private static Dictionary<string, string> CardNames;

        /// <summary>
        /// Attempts to get the name of the card given a card id.  
        /// If the cardId cannot be found, returns the cardId.
        /// </summary>
        /// <param name="cardId"></param>
        /// <returns></returns>
        private static string GetCardName(string cardId)
        {
            string cardName;

            return CardNames.TryGetValue(cardId, out cardName) ? cardName : cardId;
        }

        [HarmonyPatch(typeof(WorldManager), "Play")]
        [HarmonyPostfix]
        private static void WorldManager__Load(ref WorldManager __instance)
        {
            CardNames = __instance.CardDataPrefabs
                .Select(x => (x.Id, x.Name))
                .Distinct()
                .ToDictionary(x => x.Id, x => x.Name);

            load_boosters(__instance);
            load_harvestable(__instance);
            load_mobs(__instance);
            blue_prints(__instance);
            travelling_cart(__instance);
            treasure_chest(__instance);
        }

        private static void treasure_chest(WorldManager worldManager)
        {
            Plugin.Log.LogInfo($"Simulate Treasure Chest");
            List<CardData> cards = (from x in WorldManager.instance.CardDataPrefabs
                                    where (x.MyCardType == CardType.Resources || x.MyCardType == CardType.Food) && !x.IsIslandCard
                                    select x).ToList<CardData>();
            cards.RemoveAll((CardData x) => x.Id == "goblet");
            float chance = 1 / (float)cards.Count;

            const string TreasureChestId = "treasure_chest";
            const string KeyId = "key";
            StreamWriter recipes = File.CreateText(@"treasure-chest-recipes.yaml");
            d.ChanceContainer container = new d.ChanceContainer(GetCardName(TreasureChestId), 
                TreasureChestId, new List<d.ItemChance>(), new List<d.ResourceCount>());

            container.Inp.Add(new d.ResourceCount(GetCardName(TreasureChestId), TreasureChestId, 1));
            container.Inp.Add(new d.ResourceCount(GetCardName(KeyId), KeyId, 1));

            foreach (var card in cards)
            {
                recipes.Write("-\n");
                recipes.Write($"  inp: {{{TreasureChestId}: 1, {KeyId}: 1}}\n");
                recipes.Write($"  out: {{{card.Id}: 1}}\n");
                recipes.Write($"  chance: {chance}\n");

                container.ChanceCards.Add(new d.ItemChance(GetCardName(card.Id), card.Id, chance));
            }
            recipes.Close();
            WriteYaml("treasure-chest-recipes-single.yaml", container);
        }

        private static void travelling_cart(WorldManager worldManager)
        {
            Plugin.Log.LogInfo($"Load Travelling cart");
            var travelling_cart = (TravellingCart)worldManager.GetCardPrefab("travelling_cart");


            //The single card Yaml format
            d.ChanceContainer container = new d.ChanceContainer(travelling_cart.Name, travelling_cart.Id,
                new List<d.ItemChance>(), new List<d.ResourceCount>());

            container.Inp.Add(new d.ResourceCount(travelling_cart.Name, travelling_cart.Id, 1));
            container.Inp.Add(new d.ResourceCount("Coin", "coin", 1));

            StreamWriter recipes = File.CreateText(@"travelling-cart-recipes.yaml");

            var new_chances = get_bag_chances(travelling_cart.MyCardBag);
            foreach (var cardChance in new_chances)
            {
                recipes.Write("-\n");
                recipes.Write($"  inp: {{travelling_cart: 1, coin: 5}}\n");
                recipes.Write($"  out: {{{cardChance.Key}: 1}}\n");
                recipes.Write($"  chance: {cardChance.Value}\n");

                container.ChanceCards.Add(
                    new d.ItemChance(GetCardName(cardChance.Key), cardChance.Key, cardChance.Value));
            }

            recipes.Close();
            WriteYaml("traveling-card-recipes-single.yaml", new[] { container });
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

        private static void load_harvestable(WorldManager worldManager)
        {
            Plugin.Log.LogInfo($"Load Harvestable");
            var cards = worldManager.GameDataLoader.CardDataPrefabs;
            var harvestable_cards = (from x in cards
                                     where typeof(Harvestable).IsInstanceOfType(x)
                                     select (Harvestable)x).ToList<Harvestable>();

            List<d.TimedProducerCard> producers = new List<d.TimedProducerCard>();

            foreach (var card in harvestable_cards)
            {

                d.TimedProducerCard producer = new d.TimedProducerCard(card.Id, card.Name, card.HarvestTime,
                    new List<d.ItemChance>());

                //Bug:  There are some blank cards.  Research why.
                //  Examples: Cave, Catacombs
                foreach (var card_chance in get_bag_chances(card.MyCardBag)
                    .Where(x => !string.IsNullOrEmpty(x.Key)))
                {
                    producer.ItemChances.Add(new d.ItemChance(GetCardName(card_chance.Key), card_chance.Key, card_chance.Value));
                }

                producers.Add(producer);
            }


            var combatable_harvestable_cards = (from x in cards
                                                where typeof(CombatableHarvestable).IsInstanceOfType(x)
                                                select (CombatableHarvestable)x).ToList<CombatableHarvestable>();
            foreach (var card in combatable_harvestable_cards)
            {
                d.TimedProducerCard producer = new d.TimedProducerCard(card.Id, card.Name, card.HarvestTime,
                    new List<d.ItemChance>());

                foreach (var card_chance in get_bag_chances(card.MyCardBag))
                {
                    producer.ItemChances.Add(new d.ItemChance(GetCardName(card_chance.Key),
                        card_chance.Key, card_chance.Value));
                }
            }

            WriteYaml("harvestable.yaml", producers);

        }

        private static void load_mobs(WorldManager worldManager)
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
                if (typeof(Animal).IsInstanceOfType(card))
                {
                    var animal = (Animal)card;
                    if (animal.CreateCard != "")
                    {
                        d.AnimalRecipe animalRecipe = new d.AnimalRecipe(
                            new d.ResourceCount(card.Id, card.Name, 1),
                            new d.ResourceCount(GetCardName(animal.CreateCard), animal.CreateCard, 1),
                            animal.CreateTime
                            );

                        animalRecipies.Add(animalRecipe);
                    }
                }


                csv.Write($"{card.Id},");
                csv.Write($"{card.Drops.CardsInPack}\n");

                d.ProducerCard mob = new d.ProducerCard(card.Id, card.Name, new List<d.ItemChance>());
                foreach (var card_chance in get_bag_chances(card.Drops))
                {
                    mob.ItemChances.Add(new d.ItemChance(GetCardName(card_chance.Key), card_chance.Key, card_chance.Value));
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
            ISerializer seralizer = GetSerializer();
            File.WriteAllText(filePath, seralizer.Serialize(item));
        }

        private static ISerializer GetSerializer()
        {
            return new SerializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .Build();
        }

        private static void blue_prints(WorldManager worldManager)
        {
            Plugin.Log.LogInfo($"Load Blueprints");
            var blueprints = worldManager.GameDataLoader.BlueprintPrefabs;

            List<d.Blueprint> yamlBlueprints = new List<d.Blueprint>();

            foreach (var blueprint in blueprints)
            {
                foreach (var subprint in blueprint.Subprints)
                {
                    d.Blueprint yamlBlueprint = new d.Blueprint
                    {
                        Inp = GetResources(subprint.RequiredCards),
                        Time = subprint.Time
                    };

                    if (string.IsNullOrEmpty(subprint.ResultCard))
                    {
                        yamlBlueprint.Out = GetResources(subprint.ExtraResultCards);
                    }
                    else
                    {
                        yamlBlueprint.Out = GetResources(new[] { subprint.ResultCard});
                    }

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

        private static List<d.ResourceCount> GetResources(string[] cards)
        {
            var resources = new List<d.ResourceCount>();
            foreach (var cardId in cards)
            {
                var resource = new d.ResourceCount();
                resource.Id = cardId;
                resource.Name = GetCardName(cardId);
                resource.Count = cards.Count(x => x == cardId);

                resources.Add(resource);
            }


            return resources;

        }

    }
}
