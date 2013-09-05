using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libeveapi;
using Ircbot.Database;

namespace IRCBot.Pollers.Impl
{
    class SkillQueuePoller : Poller
    {
        private TimeSpan frequency = new TimeSpan(1, 0, 0);//1 Hour
        private static readonly string pollerName = "Eve Skill Queue Poller";
        public override void action(IrcConnection connection, string channel)
        {
            IList<Character> characters = IrcBot.mySession.CreateCriteria<Character>().List<Character>();
            foreach (Character character in characters)
            {
                //connection.privmsg(channel, "DEBUG: Polling " + character.characterName + "'s skill queue for time remaining");
                //Get their skillQueue, and determine if time remaining is less than 24 hours.
                SkillQueue skillQueue = null;
                try
                {
                    skillQueue = EveApi.GetSkillQueue(character.api.apiUserId, character.apiCharacterId, character.api.apiKeyId);
                }
                catch (FormatException)
                {
                    continue;
                }
                if (skillQueue == null)
                    continue;
                DateTime tomorrow = DateTime.Now + new TimeSpan(24,0,0);//24 hours

                if (skillQueue.SkillList.Length > 0)
                {
                    if (skillQueue.SkillList.Last().TrainingEndTimeLocal < tomorrow) // If the skill finishes before tomorrow.
                    {
                        connection.privmsg(channel, "Character " + character.characterName + " has less than 24 hours remaining in his skill queue");
                    }
                }
            }
        }

        public override TimeSpan getFrequency()
        {
            return frequency;
        }

        public override string getName()
        {
            return pollerName;
        }
    }
}
