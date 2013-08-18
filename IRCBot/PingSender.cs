using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCBot
{
    class PingSender
    {
        static string PING = "PING :";
        private String server;
        private Thread pingSender;
        private IrcConnection connection;

        public void setServer(String server)
        {
            this.server = server;
        }

        public PingSender(IrcConnection connection)
        {
            this.connection = connection;
            pingSender = new Thread(new ThreadStart(this.run));
        }

        public void start()
        {
            pingSender.Start();
        }

        public void run() 
        {
            while (true)
            {
                connection.rawWrite(PING + this.server);
                Thread.Sleep(15000);
            }

        }

    }
}
