using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace IRCBot.Pollers
{
    abstract class Poller
    {
        public IrcConnection connection{get;set;}
        public string channel { get; set; }
        private DateTime lastRun = DateTime.Now - new TimeSpan(24,0,0);//set previous run 1 day in the past so we do it as we start.
        /// <summary>
        ///Implement this and it will be called to perform the poller's main action
        /// </summary>
        public abstract void action(IrcConnection connection, string channel);

        /// <summary>
        /// Gets a timespan to determine how often the poller should run.
        /// </summary>
        /// <returns>A timespan indicating how often to run.</returns>
        public abstract TimeSpan getFrequency();

        /// <summary>
        /// Gets the name of the poller class.
        /// </summary>
        /// <returns>A string used to identify the poller.</returns>
        public abstract string getName();

        public Poller()
        {
            
        }

        public void run()
        {
            while (true)
            {
                if (connection == null || channel == null)
                {
                    Console.WriteLine("Polling failed due to channel or connection not being set. Poller: " + getName());
                }
                DateTime nextRun = lastRun + getFrequency();
                DateTime now = DateTime.Now;
                if (nextRun <= now)
                {
                    //try
                    //{
                        Console.WriteLine("Attempting poll with poller " + getName());
                        action(connection, channel);
                        lastRun = DateTime.Now;
                    //}
                    /*catch
                    {
                        //Dunno
                    }
                    finally
                    {
                        lastRun = DateTime.Now;
                    }*/
                }
                Thread.Sleep(1000);
            }
        }

    }
}
