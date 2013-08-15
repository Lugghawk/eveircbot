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

        public void setServer(String server)
        {
            this.server = server;
        }

        public PingSender()
        {
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
                IrcBot.writer.WriteLine(PING + this.server);
                IrcBot.writer.Flush();
                Thread.Sleep(15000);
            }

        }

    }
}
