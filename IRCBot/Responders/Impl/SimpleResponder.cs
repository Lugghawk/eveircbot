using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Responders.Impl
{
    class SimpleResponder : Responder
    {

        private string trigger;
        private string response;

        public SimpleResponder()
        {
            responseTriggers.Add(trigger, response);
        }
        public override bool willRespond(Input input)
        {
             return input.message.StartsWith(trigger) ? true : false;
        }

        public override List<String> respond(Input input)
        {
            List<String> returnStrings = new List<String>();
            returnStrings.Add(response);
            return returnStrings;
        }
    }
}
