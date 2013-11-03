using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IRCBot.Managers.Impl;

namespace IRCBot.Responders.Impl
{
    class IrcCommandResponder : Responder
    {

        public IrcCommandResponder()
        {
            responseTriggers.Add("!join", "<channel> - joins a given channel");
            responseTriggers.Add("!part", "<channel> - leaves a given channel");
        }
        public override List<string> respond(Input input)
        {
            if (input.message.StartsWith("!join")) return joinChannel(input);
            if (input.message.StartsWith("!part")) return partChannel(input);

            return new List<string>();
        }

        private List<String> partChannel(Input input)
        {
            List<String> responseStrings = new List<String>();
            IrcConnection irc = ((IrcConnectionManager)getManager(IrcConnectionManager.MANAGER_NAME)).connection;
            try
            {
                String channel = input.message.Split(' ')[1];
                irc.partChannel(channel);
                responseStrings.Add(String.Format("Parted {0}", channel));
            }
            catch (Exception e)
            {
                responseStrings.Add(String.Format("I couldn't part the channel due to a {0}", e.GetType().ToString()));
            }

            return responseStrings;

        }

        private List<String> joinChannel(Input input)
        {
            List<String> responseStrings = new List<String>();
            IrcConnection irc = ((IrcConnectionManager) getManager(IrcConnectionManager.MANAGER_NAME)).connection;
            try
            {
                String channel = input.message.Split(' ')[1];
                irc.joinChannel(channel);
                responseStrings.Add(String.Format("Joined {0}",channel));
            }
            catch (Exception e)
            {
                responseStrings.Add(String.Format("I couldn't join the channel due to a {0}",e.GetType().ToString()));
            }
            return responseStrings;
        }

        public override string name
        {
            get { return "IrcCommandResponder"; }
        }
    }
}
