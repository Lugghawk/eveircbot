using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Ircbot.Database;
using libeveapi;

namespace IRCBot.Responders.Impl {
    class SkillLearnedResponder : IResponder {
        bool IResponder.willRespond(Input input) {
            if (input == null || input.message == null) {
                return false;
            }
            return input.message.StartsWith("!trained");
        }

        void IResponder.respond(IrcConnection connection, Input input) {
            string[] message = input.message.Split(new char[] { ' ' }, 2);
            if (message.Length < 2) {
                connection.privmsg(input.target, "You are lacking skill, tell me which one you want to check");
                return;
            }
            string skillName = message[1];
            if (!IrcBot.nickDict.ContainsKey(input.speaker)) {
                connection.privmsg(input.target, "Add an api key for your username first");
                return;
            }
            User user = IrcBot.nickDict[input.speaker];
            UserApi api = user.apis.ElementAt(0);
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, user.defaultChar, api.apiKeyId);
            if (!IrcBot.skillIds.ContainsKey(skillName.ToLower())) {
                connection.privmsg(input.target, String.Format("I have no mapping for '{0}'", skillName));
                return;
            }
            int target = IrcBot.skillIds[skillName.ToLower()];
            var ids = from skill in character.SkillItemList select skill.TypeId;
            skillName = IrcBot.getSkillById(target).TypeName;
            int i = 0;
            foreach (int id in ids) {
                if (id == target) {
                    connection.privmsg(input.target, String.Format("{0} has trained {1} to level {2}", character.Name, skillName, character.SkillItemList[i].Level));
                    return;
                }
                i += 1;
            }
            connection.privmsg(input.target, String.Format("{0} has not trained {1}", character.Name, skillName));
        }
    }
}
