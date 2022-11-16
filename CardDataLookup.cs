using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DataLoaderPlugin
{
    public class CardDataLookup
    {
        private Dictionary<string, CardData> CardData;

        /// <summary>
        /// Attempts to get the name of the card given a card id.  
        /// If the cardId cannot be found, returns the cardId.
        /// </summary>
        /// <param name="cardId"></param>
        /// <returns></returns>
        public string GetCardName(string cardId)
        {
            return CardData.TryGetValue(cardId, out var cardData) ? cardData.Name : cardId;
        }

        /// <summary>
        /// Gets the Card data for the card's ID.
        /// Returns null if not found.
        /// </summary>
        /// <param name="cardId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public CardData GetCardData(string cardId)
        {
            return CardData.TryGetValue(cardId, out CardData cardData) ? cardData : null;
        }

        public CardDataLookup(WorldManager worldManager)
        {
            CardData = worldManager.CardDataPrefabs
                .ToDictionary(x => x.Id);
        }
    }
}
