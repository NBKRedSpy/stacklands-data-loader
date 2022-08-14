using BepInEx;
using HarmonyLib;
using static WorldManager;
using BepInEx.Logging;
using System.Linq;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

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
            load_boosters();
        }

        private static void load_boosters()
        {
            Plugin.Log.LogInfo($"Load Boosters");
            var boosterPackPrefabs = Resources.LoadAll<Boosterpack>("Boosters").ToList<Boosterpack>();
            boosterPackPrefabs.AddRange(Resources.LoadAll<Boosterpack>("Island_Boosters"));

            string path = @"boosters-recipes.yaml";
            StreamWriter sw = File.CreateText(path);

            foreach(var booster_pref in boosterPackPrefabs)
            {
                // var booster = Object.Instantiate<Boosterpack>(booster_pref);
                sw.Write($"{booster_pref.Name}:\n");
                Plugin.Log.LogInfo($"{booster_pref.Name}:");
                foreach(var card_chance in calc_booster_chances(booster_pref))
                {
                    sw.Write($"  {card_chance.Key}: {card_chance.Value}\n");
                }
            }

            sw.Close();
        }

        private static void load_harvestable()
        {
            Plugin.Log.LogInfo($"Load Harvestable");
            var cards = Resources.LoadAll<Boosterpack>("Boosters").ToList<Boosterpack>();
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
                        Plugin.Log.LogInfo($"SetCardBag:{bag.SetCardBag}");
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
                        Plugin.Log.LogInfo($"change: {cardChance.Key}={result[cardChance.Key]}");
                    }
                    else
                    {
                        result[cardChance.Key] = cardChance.Value;
                        Plugin.Log.LogInfo($"add: {cardChance.Key}={cardChance.Value}");
                    }
                }
            }
            Plugin.Log.LogInfo($"result.Count={result.Count}");
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

    }

}
