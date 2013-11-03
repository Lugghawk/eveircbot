using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Managers
{
    /// <summary>
    /// This class is the superclass of all Managers. If your module needs some kind of singleton management system to keep track of things. You should make it by extending this class.
    /// </summary>
    public abstract class AbstractManager
    {
        public abstract String name {get;}


        
    }
}
