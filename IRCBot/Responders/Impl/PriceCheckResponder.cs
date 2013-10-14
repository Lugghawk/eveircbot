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
        public PriceCheckResponder()
        {
            responseTriggers.Add("!price", "<item> - Returns the average sell price of the item as per Eve Central's API");
        }

        public override List<String> respond(Input input)
        {
            List<String> returnStrings = new List<String>();
            string [] message = input.message.Split(new char[] {' '}, 2);
            if (message.Length < 2) {
                returnStrings.Add("More than you can afford");
                return returnStrings;
            }
            string itemName = message[1];
            if (itemName.Length < 3)
            {
                returnStrings.Add("Please supply at least three characters");
                return returnStrings;
            }
            
            
            List<InvType> types = (List<InvType>)IrcBot.mySession.CreateCriteria<InvType>().Add(Restrictions.InsensitiveLike("typeName", itemName+"%")).List<InvType>();
            var names = from type in types select type.typeName.ToLower();
            if (types.Count == 1 || names.Contains(itemName.ToLower()))
            {
                foreach (InvType type in types) {
                    if (type.typeName.ToLower() == itemName.ToLower()) {
                        returnStrings.Add("Price of " + type.typeName + " is " + string.Format("{0:n}", getMarketPrice(type.typeID)) + " on average.");
                        return returnStrings;
                    }
                }

            }
            else if (types == null || types.Count == 0)
            {
                returnStrings.Add("Can't find that item. Check spelling");
                return returnStrings;
            }
            else if (types.Count > 1 && types.Count <=5)
            {
                string[] itemString  = new string[types.Count];
                for (int i=0; i < types.Count; i++){
                    //If there is more than 1 result, concatenate them into a list and return to give an example.                 
                    itemString[i] = types.ElementAt(i).typeName;
                }
                returnStrings.Add("Found multiple Results: " + string.Join(", ",itemString) +".");
                return returnStrings;
            } else if (types.Count > 5) {
                returnStrings.Add(String.Format("I found {0} matches for that, mind making it more specific?", types.Count));
                return returnStrings;
            }
            return returnStrings;
            
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
