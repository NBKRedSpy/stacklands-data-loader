using DataLoaderPlugin.Dto;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace DataLoaderPlugin
{
    public class CardUtility
    {
        public ColorUtility ColorUtility { get; private set; }

        public CardDataLookup CardDataLookup { get; private set; }
        
        public CardUtility(WorldManager worldManager)
        {
            ColorUtility = new ColorUtility(worldManager);
            CardDataLookup = new CardDataLookup(worldManager);  
        }

        /// <summary>
        /// Sets the basic card info.  Returns "{id}" if the card cannot be found.
        /// For example, the any_villager not a real card.
        /// </summary>
        /// <param name="card"></param>
        /// <param name="id"></param>
        public void SetData(Card card, string id)
        {
            CardData cardData = CardDataLookup.GetCardData(id);

            if(cardData == null)
            {
                card.Id = id;
                card.Name = $"{{{id}}}";
            }
            else
            {
                card.Id = cardData.Id;
                card.Name = cardData.Name;

                card.Colors = ColorUtility.GetColors(cardData);
            }
        }

        public string GetCardName(string id)
        {
            return CardDataLookup.GetCardName(id);
        }

        public CardColors GetColors(CardData card)
        {
            return ColorUtility.GetColors(card);
        }

    }
}
