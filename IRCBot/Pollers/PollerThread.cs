using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace IRCBot.Pollers
{
    class PollerManager
    {
        List<Thread> pollerThreads = new List<Thread>();
        public PollerManager(List<Poller> pollers)
        {
            Console.WriteLine("Starting PollerManager");
            foreach (Poller poller in pollers)
            {
                pollerThreads.Add(new Thread(new ThreadStart(poller.run)));
            }
            foreach (Thread pollerThread in pollerThreads)
            {
                pollerThread.Start();
            }

        }
    }
}
