using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Ircbot.Database;
using libeveapi;

namespace IRCBot.Responders.Impl {
    class SkillLearnedResponder : Responder {

        private const String RESPONDER_NAME = "SkillLearnedResponder";

        public override string name
        {
            get { return RESPONDER_NAME; }
        }


        public SkillLearnedResponder()
        {
            responseTriggers.Add("!trained", "<skill-name> - Responds with a level that skill is trained to on your default character");
        }

        public override void respond(Input input) {
            string[] message = input.message.Split(new char[] { ' ' }, 2);
            if (message.Length < 2) {
                addResponse("You are lacking skill, tell me which one you want to check");
                return;
            }
            string skillName = message[1];
            if (!IrcBot.nickDict.ContainsKey(input.speaker)) {
                addResponse("Add an api key for your username first");
                return;
            }
            User user = IrcBot.nickDict[input.speaker];
            UserApi api = user.apis.ElementAt(0);
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, user.defaultChar, api.apiKeyId);
            if (!IrcBot.skillIds.ContainsKey(skillName.ToLower())) {
                addResponse(String.Format("I have no mapping for '{0}'", skillName));
                return;
            }
            int target = IrcBot.skillIds[skillName.ToLower()];
            var ids = from skill in character.SkillItemList select skill.TypeId;
            skillName = IrcBot.getSkillById(target).TypeName;
            int i = 0;
            foreach (int id in ids) {
                if (id == target) {
                    addResponse(String.Format("{0} has trained {1} to level {2}", character.Name, skillName, character.SkillItemList[i].Level));
                    return;
                }
                i += 1;
            }
            addResponse(String.Format("{0} has not trained {1}", character.Name, skillName));
            return;
        }

        
        
    }
}
