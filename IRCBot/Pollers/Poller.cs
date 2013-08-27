using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Pollers
{
    abstract class Poller
    {

        private DateTime lastRun = DateTime.Now;
        /// <summary>
        ///Implement this and it will be called to perform the poller's main action
        /// </summary>
        public abstract void action();

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

        public void run()
        {
            if (lastRun + getFrequency() >= DateTime.Now)
            {
                try
                {
                    action();
                    lastRun = DateTime.Now;
                }
                catch
                {
                    //Dunno
                }
            }
        }

    }
}
