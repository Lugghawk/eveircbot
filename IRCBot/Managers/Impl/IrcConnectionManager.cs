using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Managers.Impl
{
    /// <summary>
    /// This class governs the connection to IRC. 
    /// </summary>
    class IrcConnectionManager : AbstractManager
    {
        public const String MANAGER_NAME = "IrcConnection";

        public IrcConnection connection { get; private set; }

        public IrcConnectionManager()
        {
        }

        public IrcConnectionManager(IrcConnection connection)
        {
            this.connection = connection;
        }

        public override string name
        {
            get { return MANAGER_NAME; }
        }
    }
}
