using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using Ircbot.Database;
using NHibernate.Criterion;

namespace IRCBot.Responders.Impl
{
    class PriceCheckResponder : Responder
    {

        private const String RESPONDER_NAME = "PriceCheckResponder";

        public override string name
        {
            get { return RESPONDER_NAME; }
        }
            
        public PriceCheckResponder()
        {
            responseTriggers.Add("!price", "<item> - Returns the average sell price of the item as per Eve Central's API");
        }

        public override void respond(Input input)
        {
            getPrice(input);
        }

        private void getPrice(Input input)
        {
            string[] message = input.message.Split(new char[] { ' ' }, 2);
            if (message.Length < 2)
            {
                addResponse("More than you can afford");
                return;
            }
            string itemName = message[1];
            if (itemName.Length < 3)
            {
                addResponse("Please supply at least three characters");
                return;
            }


            List<InvType> types = (List<InvType>)IrcBot.mySession.CreateCriteria<InvType>().Add(Restrictions.InsensitiveLike("typeName", itemName + "%")).List<InvType>();
            var names = from type in types select type.typeName.ToLower();
            if (types.Count == 1 || names.Contains(itemName.ToLower()))
            {
                foreach (InvType type in types)
                {
                    if (type.typeName.ToLower() == itemName.ToLower())
                    {
                        addResponse("Price of " + type.typeName + " is " + string.Format("{0:n}", getMarketPrice(type.typeID)) + " on average.");
                        return;
                    }
                }

            }
            else if (types == null || types.Count == 0)
            {
                addResponse("Can't find that item. Check spelling");
                return;
            }
            else if (types.Count > 1 && types.Count <= 5)
            {
                string[] itemString = new string[types.Count];
                for (int i = 0; i < types.Count; i++)
                {
                    //If there is more than 1 result, concatenate them into a list and return to give an example.                 
                    itemString[i] = types.ElementAt(i).typeName;
                }
                addResponse("Found multiple Results: " + string.Join(", ", itemString) + ".");
                return;
            }
            else if (types.Count > 5)
            {
                addResponse(String.Format("I found {0} matches for that, mind making it more specific?", types.Count));
                return;
            }
            return;
        }

        public double getMarketPrice(int typeID)
        {
            string url = getEveCentralAPIURL(typeID);
            XmlDocument eveCentralResponse = new XmlDocument();
            eveCentralResponse.Load(url);
            return double.Parse(eveCentralResponse.SelectSingleNode("/evec_api/marketstat/type/sell/avg").InnerText);
        }

        private string getEveCentralAPIURL(int typeID)
        {
            string EVE_CENTRAL_API = "http://api.eve-central.com/api/marketstat";
            return EVE_CENTRAL_API + "?typeid="+typeID;
        }

    }

    
}
