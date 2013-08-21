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
            
            InvType type = (InvType)IrcBot.mySession.CreateCriteria<InvType>().Add(Restrictions.Eq("typeName", itemName)).UniqueResult();
            if (type == null)
            {
                connection.privmsg(input.target, "Can't find that item. Check spelling");
                return;
            }
            connection.privmsg(input.target, "Price of "+ type.typeName + " is " + string.Format("{0:n}",getMarketPrice(type.typeID)) + " on average.");
            
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
