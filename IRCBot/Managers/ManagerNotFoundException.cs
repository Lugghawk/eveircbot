using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Managers
{
    class ManagerNotFoundException : Exception
    {
        public string managerName { get; set; }

        public ManagerNotFoundException(string managerName)
        {
            this.managerName = managerName;
        }

        public override string Message
        {
            get
            {
                return String.Format("Manager [{0}] Not Found. Did you add it to the appcontext?",managerName);
            }
        }
    }
}
