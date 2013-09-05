using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace IRCBot.Responders
{
    public abstract class Responder
    {
        /// <summary>
        /// This holds a Dictionary representing the triggers for this responder and their descriptions.
        /// Should be filled by the derivative classes.
        /// </summary>
        public Dictionary<string, string> responseTriggers = new Dictionary<string, string>();
        /// <summary>
        /// Implementer will receive the Input object and determine whether it satisfies its criteria for being the responder.
        /// </summary>
        /// <param name="message">An Input object for which to make the determination</param>
        /// <returns>A boolean indicating whether this responder is going to return a response.</returns>
        public virtual bool willRespond(Input input)
        {
            if (input.message != null)
            {
                return responseTriggers.ContainsKey(input.message.Split(new char[] { ' ' }, 2)[0]);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// The method called when this Responder has been selected for use.
        /// </summary>
        /// <param name="connection">The stream which the response will be given on. Use Streamwriter.WriteLine() to send a line to IRC.</param>
        /// <param name="message">The input which to respond against.</param>
        public abstract List<String> respond(Input input);

        public void doResponse(IrcConnection connection, Input input)
        {
            List<String> responses = respond(input);
            foreach (String response in responses)
            {
                connection.replyTo(input, response);
            }
        }

        public List<string> getHelp()
        {
            List<string> helpList = new List<string>();
            foreach (string command in responseTriggers.Keys)
            {
                helpList.Add(command+": "+ responseTriggers[command]);
            }
            return helpList;
        }



       
    }

}
