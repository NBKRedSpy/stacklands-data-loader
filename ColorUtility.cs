using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using d = DataLoaderPlugin.Dto;

namespace DataLoaderPlugin
{
    public class ColorUtility
    {
        private static FieldInfo PropBlockFieldInfo = AccessTools.Field(typeof(GameCard), "propBlock");
        private static MethodInfo SetColorMethodInfo = AccessTools.Method(typeof(GameCard), "SetColors");

        public WorldManager WorldManager { get; }

        public ColorUtility(WorldManager worldManager)
        {
            WorldManager = worldManager;
        }

        /// <summary>
        /// Gets the colors of the card if possible.  Otherwise returns null
        /// </summary>
        /// <param name="card"></param>
        /// <returns></returns>
        public d.CardColors GetColors(CardData card)
        {

            if (card == null) return null;

            var gameCard = WorldManager.AllCards.FirstOrDefault(x => x.CardData = card);

            if(gameCard == null)
            {
                return null;
            }
            else
            {
                //Force the game card's colors to be set.
                SetColorMethodInfo.Invoke(gameCard, new object[] { });
                return GetColors(gameCard);
            }
            

            

        }

        private d.CardColors GetColors(GameCard gameCard)
        {
            return new d.CardColors(
                ToHtmlColor(((MaterialPropertyBlock)PropBlockFieldInfo.GetValue(gameCard)).GetColor("_Color")),
                ToHtmlColor(((MaterialPropertyBlock)PropBlockFieldInfo.GetValue(gameCard)).GetColor("_Color2"))
            );

        }


        /// <summary>
        /// Returns a string with RGBA(255,255,255,%) format.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public string ToHtmlColor(Color color)
        {
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
            return $"rgba({(int)(color.r * 255f)}, {(int)(color.g * 255f)}, {(int)(color.b * 255f)}, {color.a})";
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
        }

    }
}
