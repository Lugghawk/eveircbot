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

            string [] message = input.message.Split(' ');
            //string itemName = message[1];
            string itemName = "";
            for (int i = 1; i < message.Length; i++)
            {
                itemName += message[i];
                if (i < message.Length-1)
                {
                    itemName += " ";
                }

            }
            if (itemName.Length < 3)
            {
                connection.privmsg(input.target, "Please supply at least three characters");
            }
            itemName += "%";//For like statement.
            
            
            List<InvType> types = (List<InvType>)IrcBot.mySession.CreateCriteria<InvType>().Add(Restrictions.InsensitiveLike("typeName", itemName)).List<InvType>();
            if (types == null || types.Count == 0)
            {
                connection.privmsg(input.target, "Can't find that item. Check spelling");
                return;
            }
            else if (types.Count > 1 && types.Count <=5)
            {
                string[] itemString  = new string[5];
                for (int i=0; i < types.Count; i++){
                    //If there is more than 1 result, concatenate them into a list and return to give an example.                 
                    itemString[i] = types.ElementAt(i).typeName;
                }
                connection.privmsg(input.target,"Found multiple Results: " + string.Join(", ",itemString) +".");
                return;
            }else if (types.Count == 1)
            {   
                InvType type = types.ElementAt(0);
                connection.privmsg(input.target, "Price of "+ type.typeName + " is " + string.Format("{0:n}",getMarketPrice(type.typeID)) + " on average.");
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
