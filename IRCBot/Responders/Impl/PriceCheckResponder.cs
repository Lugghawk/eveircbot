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
    class PriceCheckResponder : IResponder
    {
        bool IResponder.willRespond(Input input)
        {
            if (input == null || input.message == null)
            {
                return false;
            }
            return input.message.StartsWith("!price");
        }

        void IResponder.respond(IrcConnection connection, Input input)
        {

            string [] message = input.message.Split(new char[] {' '}, 2);
            if (message.Length < 2) {
                connection.privmsg(input.target, "More than you can afford");
                return;
            }
            string itemName = message[1];
            if (itemName.Length < 3)
            {
                connection.privmsg(input.target, "Please supply at least three characters");
            }
            
            
            List<InvType> types = (List<InvType>)IrcBot.mySession.CreateCriteria<InvType>().Add(Restrictions.InsensitiveLike("typeName", itemName+"%")).List<InvType>();
            var names = from type in types select type.typeName;
            if (types.Count == 1 || names.Contains(itemName))
            {
                foreach (InvType type in types) {
                    if (type.typeName.ToLower() == itemName.ToLower()) {
                        connection.privmsg(input.target, "Price of " + type.typeName + " is " + string.Format("{0:n}", getMarketPrice(type.typeID)) + " on average.");
                        break;
                    }
                }

            }
            else if (types == null || types.Count == 0)
            {
                connection.privmsg(input.target, "Can't find that item. Check spelling");
                return;
            }
            else if (types.Count > 1 && types.Count <=5)
            {
                string[] itemString  = new string[types.Count];
                for (int i=0; i < types.Count; i++){
                    //If there is more than 1 result, concatenate them into a list and return to give an example.                 
                    itemString[i] = types.ElementAt(i).typeName;
                }
                connection.privmsg(input.target,"Found multiple Results: " + string.Join(", ",itemString) +".");
                return;
            } else if (types.Count > 5) {
                connection.privmsg(input.target, String.Format("I found {0} matches for that, mind making it more specific?", types.Count));
            }
            
        }

        public double getMarketPrice(int typeID)
        {
            string url = getEveCentralAPIURL(typeID);
            XmlDocument eveCentralResponse = new XmlDocument();
            eveCentralResponse.Load(url);
            return double.Parse(eveCentralResponse.SelectSingleNode("/evec_api/marketstat/type/all/avg").InnerText);
        }

        private string getEveCentralAPIURL(int typeID)
        {
            string EVE_CENTRAL_API = "http://api.eve-central.com/api/marketstat";
            return EVE_CENTRAL_API + "?typeid="+typeID;
        }

    }

    
}
