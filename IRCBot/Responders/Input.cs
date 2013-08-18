using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Responders
{
    /// <summary>
    /// This is a class which will represent an input message from IRC that the bot can observe.
    /// </summary>
    class Input
    {
        /// <summary>
        /// The nickname of the person who 
        /// </summary>
        public string speaker { get; set; }
        /// <summary>
        /// The raw text of the message.
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// The target the message was sent to (channel, or bot).
        /// </summary>
        public string target { get; set; }
        /// <summary>
        /// Create an Input object after being given a pure irc message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Input parse(string message)
        {
            string nickname = message.Substring(1, message.IndexOf("!") - 1);
            return new Input();
        }
        
    }
}
