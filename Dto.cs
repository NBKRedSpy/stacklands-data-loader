using System;
using System.Collections.Generic;
using System.Text;

namespace DataLoaderPlugin.Dto
{

    public class ResourceCount
    {
        public string Name;
        public string Id;
        public int Count { get; set; }

        public ResourceCount(string name, string id, int count)
        {
            Name = name;
            Id = id;
            Count = count;
        }

        public ResourceCount()
        {

        }
    }

    public class AnimalRecipe
    {
        public ResourceCount Inp;
        public ResourceCount Out;
        public double Time;

        public AnimalRecipe(ResourceCount inp, ResourceCount @out, double time)
        {
            this.Inp = inp;
            this.Out = @out;
            this.Time = time;
        }

        public AnimalRecipe()
        {

        }
    }

    public class ItemChance
    {
        public string CardId;
        public string CardName;
        public double Chance;

        public ItemChance(string cardName, string cardId, double chance)
        {
            CardName = cardName;
            CardId = cardId;
            Chance = chance;
        }
        public ItemChance()
        {

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

        public TimedProducerCard(string name, string id, double time, List<ItemChance> itemChances) :
            base(name, id, itemChances)
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

        public ProducerCard()
        {

        }

        public ProducerCard(string name, string id, List<ItemChance> itemChances)
        {
            Name = name;
            Id = id;
            ItemChances = itemChances;
        }
    }

    public class Blueprint
    {
        public List<ResourceCount> Inp;
        public List<ResourceCount> Out;
        public double Time;

        public Blueprint(List<ResourceCount> inp, List<ResourceCount> @out, double time)
        {
            Inp = inp;
            Out = @out;
            Time = time;
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
    public class ChanceContainer
    {
        public string CardName;
        public string CardId;
        public List<ItemChance> ChanceCards;
        public List<ResourceCount> Inp;

        public ChanceContainer()
        {

        }

        public ChanceContainer(string cardName, string cardId,  List<ItemChance> chanceCards, List<ResourceCount> inp)
        {
            CardName = cardName;
            CardId = cardId;
            ChanceCards = chanceCards;
            Inp = inp;
        }
    }

}
