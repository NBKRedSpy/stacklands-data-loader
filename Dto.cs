using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using YamlDotNet.Serialization;

namespace DataLoaderPlugin.Dto
{

    public class Card
    {
        public string Name;
        public string Id;
        public CardColors Colors;

        public Card() { }

        public Card(string name, string id, CardColors colors)
        {
            Name = name;
            Id = id;
            Colors = colors;
        }

        public Card(string id, CardUtility cardUtility)
        {
            cardUtility.SetData(this, id);
        }

        public Card(CardData cardData, CardUtility cardUtility)
        {
            SetData(cardData, cardUtility);
       }

        private void SetData(CardData cardData, CardUtility cardUtility)
        {
            Id = cardData.Id;
            Name = cardData.Name;
            Colors = cardUtility.GetColors(cardData);
        }
    }

    public class ResourceCount : Card
    {
        public int Count;

        public ResourceCount()
        {

        }

        public ResourceCount(string id, CardUtility cardUtility, int count ) : base(id, cardUtility)
        {
            Count = count;
        }

        public ResourceCount(CardData cardData, CardUtility cardUtility , int count) : base(cardData, cardUtility)
        {
            Count = count;
        }



        public ResourceCount(string name, string id, CardColors colors, int count) 
            : base(name, id, colors)
        {
            Count = count;
        }

    }

    public class AnimalRecipe
    {
        public ResourceCount Inp;
        public ResourceCount Out;
        public double Time;
        public CardColors Colors;

        public AnimalRecipe(ResourceCount inp, ResourceCount @out, double time, CardColors colors)
        {
            this.Inp = inp;
            this.Out = @out;
            this.Time = time;
            Colors = colors;
        }

        public AnimalRecipe()
        {

        }
    }

    public class ItemChance : Card
    {
        public double Chance;

        public ItemChance()
        {

        }

        public ItemChance(string id, CardUtility cardUtility, double chance) : base(id, cardUtility)
        {
            Chance = chance;
        }

        public ItemChance(string name, string id, CardColors colors, double chance) : base(name, id, colors)
        {
            Chance = chance;
        }

    }


    /// <summary>
    /// A card that produces an item on a timer.
    /// Ex: A forest can produce an apple every 60 seconds.
    /// </summary>
    public class TimedProducerCard : ProducerCard
    {
        [YamlDotNet.Serialization.YamlMember(Order = 2)]
        public double Time;

        public TimedProducerCard(string name, string id, double time, List<ItemChance> itemChances,
            CardColors cardColors) :
            base(name, id, itemChances, cardColors)
        {
            Time = time;
        }

        public TimedProducerCard()
        {

        }
    }
    
    /// <summary>
    /// A card which can create other cards where the produced card is selected from
    /// a list of cards.
    /// </summary>
    public class ProducerCard
    {
        public string Name;
        public string Id;
        public List<ItemChance> ItemChances;
        public CardColors Colors;

        public ProducerCard()
        {
        }

        public ProducerCard(string name, string id, List<ItemChance> itemChances, CardColors colors)
        {
            Name = name;
            Id = id;
            ItemChances = itemChances;
            Colors = colors;
        }
    }

    public class Blueprint
    {
        public string Id;
        public string Name;
        public List<ResourceCount> Inp;
        public List<ResourceCount> Out;
        public double Time;
        public CardColors Colors;

        public Blueprint(List<ResourceCount> inp, List<ResourceCount> @out, double time, 
            CardColors colors, string id, string name)
        {
            Inp = inp;
            Out = @out;
            Time = time;
            Colors = colors;
            Id = id;
            Name = name;
        }

        public Blueprint()
        {

        }
    }

    /// <summary>
    /// A card which has a fixed input and produces a card
    /// based on a list of cards and chances.
    /// 
    /// For example, a TreasureChest or a Traveling Card.
    /// </summary>
    public class ChanceContainer  : Card
    {
        public List<ResourceCount> Inp;
        public List<ItemChance> ChanceCards;


        public ChanceContainer() 
        {

        }

        public ChanceContainer(string id, CardUtility cardUtility) : base(id, cardUtility)
        {
            SetBaseData();
        }

        public ChanceContainer(CardData cardData, CardUtility cardUtility) : base(cardData, cardUtility)
        {
            SetBaseData();
        }

        private void SetBaseData()
        {
            Inp = new List<ResourceCount>();
            ChanceCards = new List<ItemChance>();
        }

    }

    //HTML compatible Color strings.
    public class CardColors
    {
        public string HeaderColor;
        public string BodyColor;

        public CardColors()
        {

        }

        public CardColors(string headerColor, string bodyColor)
        {
            HeaderColor = headerColor;
            BodyColor = bodyColor;
        }
    }

}
