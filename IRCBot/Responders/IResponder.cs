using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
