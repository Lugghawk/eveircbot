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
        public override void respond(Input input)
        {
            if (input.message.StartsWith("!join")) joinChannel(input);
            if (input.message.StartsWith("!part")) partChannel(input);

            return;
        }

        private void partChannel(Input input)
        {
            List<String> responseStrings = new List<String>();
            IrcConnection irc = ((IrcConnectionManager)getManager(IrcConnectionManager.MANAGER_NAME)).connection;
            try
            {
                String channel = input.message.Split(' ')[1];
                irc.partChannel(channel);
                addResponse(String.Format("Parted {0}", channel));
            }
            catch (Exception e)
            {
                addResponse(String.Format("I couldn't part the channel due to a {0}", e.GetType().ToString()));
            }

            return;

        }

        private void joinChannel(Input input)
        {
            List<String> responseStrings = new List<String>();
            IrcConnection irc = ((IrcConnectionManager) getManager(IrcConnectionManager.MANAGER_NAME)).connection;
            try
            {
                String channel = input.message.Split(' ')[1];
                irc.joinChannel(channel);
                addResponse(String.Format("Joined {0}",channel));
            }
            catch (Exception e)
            {
                addResponse(String.Format("I couldn't join the channel due to a {0}",e.GetType().ToString()));
            }
            return;
        }

        public override string name
        {
            get { return "IrcCommandResponder"; }
        }
    }
}
