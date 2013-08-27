using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Responders.Impl
{
    class SimpleResponder : IResponder
    {

        private string trigger;
        private string response;
        bool IResponder.willRespond(Input input)
        {
             return input.message.StartsWith(trigger) ? true : false;
        }

        void IResponder.respond(IrcConnection connection, Input input)
        {
            connection.replyTo(input, response);
        }
    }
}
