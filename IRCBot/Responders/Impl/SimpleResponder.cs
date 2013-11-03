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
        public override void respond(Input input)
        {
            
            addResponse(response);
            return;
        }

        public override string name
        {
            get { return name; }
        }

    }
}
