using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace IRCBot.Responders
{
    interface IResponder
    {
        /// <summary>
        /// Implementer will receive the Input object and determine whether it satisfies its criteria for being the responder.
        /// </summary>
        /// <param name="message">An Input object for which to make the determination</param>
        /// <returns>A boolean indicating whether this responder is going to return a response.</returns>
        bool willRespond(Input input);

        /// <summary>
        /// The method called when this Responder has been selected for use.
        /// </summary>
        /// <param name="connection">The stream which the response will be given on. Use Streamwriter.WriteLine() to send a line to IRC.</param>
        /// <param name="message">The input which to respond against.</param>
        void respond(IrcConnection connection, Input input);

    }
    /*
    public static class ResponderLoader {

        public static IResponder loadCompiled(string filename, string classname) {
            Assembly file = Assembly.LoadFile(filename);
            foreach (Type type in file.GetTypes()) {
                if (type.GetInterface("IResponder") != null && type.Name.Equals(classname)) {
                    return Activator.CreateInstance(type) as IResponder;
                }
            }
            return null;
        }

        public static IResponder loadSource(string filename) {


            return null;
        }

    }
     */
}
