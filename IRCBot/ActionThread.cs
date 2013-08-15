using System.Threading;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace IRCBot
{
    class ActionThread
    {

        private Thread queueAction;

        public ActionThread(){
            queueAction = new Thread(new ThreadStart(this.run));
        }

        public void start() {
            queueAction.Start();
        }

        public void run() {

            while (true) {
                ArrayList results = new ArrayList();

                if (IrcBot.inputQueue.Count != 0) {
                    results = IrcBot.InterpretInput(IrcBot.inputQueue.Dequeue());

                    if (results != null) {
                        IrcBot.PerformAction(results);
                    }
                }
                Thread.Sleep(100);
            }
        }

    }
}
