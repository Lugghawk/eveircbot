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
        /// The type of a message (PRIVMSG, NOTICE, etc).
        /// </summary>
        public string type { get; set; }

        public string toString() {
            return String.Format("Input(speaker='{0}', type='{1}', target='{2}', message=\"{3}\")", 
                speaker, type, target, message);
        }
        /// <summary>
        /// Create an Input object after being given a pure irc message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Input parse(string message)
        {
            /* this is ridiculous syntax, but there's apparently
            no signature that takes a single char */
            string[] parts = message.Split(new char[] {' '}, 4);
            bool serverMessage = !parts[0].Contains('!');
            if (serverMessage) {
                // might want some extension of things here
                return null;
            }
            
            Input ret = new Input();
            ret.speaker = message.Substring(1, message.IndexOf("!") - 1); // :nick!~user@host 
            ret.type = parts[1];
            ret.target = parts[2];
            if (parts.Length > 3 && parts[3].Length > 0) {
                ret.message = parts[3].Substring(1); //strips the leading :
            } else {
                ret.message = ""; // or possibly return null
            }
            /* catch emote-style messages */
            if (ret.message.StartsWith("\x01")) {
                ret.type = "ACTION";
                // strip the special characters (leading "\x01ACTION " and trailing "\x01")
                ret.message = ret.message.Substring(8, ret.message.Length - 9);
            }
            return ret;
        }
        
    }
}
