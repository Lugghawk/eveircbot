using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Responders.Impl
{
    class SimpleResponder : Responder
    {

        private string response;

        public SimpleResponder(string trigger, string response, string description)
        {
            this.response = response;
            responseTriggers.Add(trigger, description);
        }
        public override List<String> respond(Input input)
        {
            List<String> returnStrings = new List<String>();
            returnStrings.Add(response);
            return returnStrings;
        }

        public override string name
        {
            get { return name; }
        }

    }
}
