using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libeveapi;

namespace IRCBot.Responders.Impl
{
    public class EveApiResponder : IResponder
    {
        bool IResponder.willRespond(Input input)
        {
            return (input.message.Contains("!isk") || input.message.Contains("!time") ||
                input.message.Contains("!location") || input.message.Contains("!characters") ||
                input.message.Contains("!server") || input.message.Contains("!skill") ||
                input.message.Contains("!api"));
        }

        void IResponder.respond(System.IO.StreamWriter writer, Input message)
        {
            
        }


    }
}
