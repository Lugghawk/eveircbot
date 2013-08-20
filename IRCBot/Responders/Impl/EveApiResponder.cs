using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libeveapi;
using System.Text.RegularExpressions;
using Ircbot.Database;
using NHibernate;
using NHibernate.Criterion;

namespace IRCBot.Responders.Impl
{
    public class EveApiResponder : IResponder
    {
        List<User> waitingOnResponse = new List<User>();
        Dictionary<User, int[]> userCharList = new Dictionary<User, int[]>();
        bool IResponder.willRespond(Input input)
        {
            if (input == null || input.message == null)
            {
                return false;
            }
            return (input.message.StartsWith("!isk") || input.message.StartsWith("!time") ||
                input.message.StartsWith("!location") || input.message.StartsWith("!characters") ||
                input.message.StartsWith("!server") || input.message.StartsWith("!skill") ||
                input.message.StartsWith("!api") ||
                checkWaitingOnUser(input));
        }

        bool checkWaitingOnUser(Input input)
        {
            foreach (User user in waitingOnResponse)
            {
                if (user.userName.Equals(input.speaker))
                {
                    return true;
                }

            }
            return false;
        }

        void IResponder.respond(IrcConnection writer, Input input)
        {
            foreach (User user in waitingOnResponse){
                if (user.userName.Equals(input.speaker)){
                    Match choiceMatch = Regex.Match(input.message, "^[1-3]$");
                    if (!choiceMatch.Success){
                        writer.privmsg(input.target, "That wasn't a valid character number! Try again!");
                        return;
                    }else{
                        int defaultChar = Convert.ToInt32(input.message);
                        user.defaultChar = userCharList[user][defaultChar - 1];
                        if (IrcBot.mySession == null)
                        {
                            writer.privmsg(input.target, "I appear to be missing my DB. Sorry! Fix me pleaaase!");
                            return;
                        }
                        ITransaction trans = IrcBot.mySession.BeginTransaction();
                        IrcBot.mySession.Save(user);
                        trans.Commit();
                        waitingOnResponse.Remove(user);
                        writer.privmsg(input.target, "Done!");
                        break;
                    }
                }
            }

            if (input.message.StartsWith("!api")){
                //Regex for api key
                Regex apiRegex = new Regex("[0-9a-zA-Z]{64}");
                Match apiMatch = apiRegex.Match(input.message);
                //Regex for user ID
                Regex idRegex = new Regex("[0-9]{6,}");
                Match idMatch = idRegex.Match(input.message);
                
                if (!(idMatch.Success || apiMatch.Success))
                {
                    //Doesn't match api key specifications.
                    writer.privmsg(input.target, "Doesn't look like an API to me");
                }

                int apiUserId = Convert.ToInt32(idMatch.Value);
                string apiKeyId = apiMatch.Value;
                User newUser = new User(input.speaker);
                UserApi api = new UserApi(apiUserId,apiKeyId);
                newUser.addApi(api);

                if (!IrcBot.nickDict.ContainsKey(input.speaker)) {
                    //If user doesn't exist, add him to user dict.x
                   IrcBot.nickDict.Add(newUser.userName, newUser);
                } else {
                    writer.privmsg(input.target, String.Format("User ({0}) already exists", newUser.userName));
                }

                //Tell the user they've been added and ask for a default character
                writer.privmsg(input.target, String.Format("New User ({0}) Added!", newUser.userName));
                writer.privmsg(input.target, "Please select a default character");

                int[] eveCharIDs = new int[IrcBot.MAX_NO_OF_CHARS];

                eveCharIDs = IrcBot.PrintCharacterList(newUser);
                //Add this person to the list of people we're waiting on input from.
                waitingOnResponse.Add(newUser);
                userCharList.Add(newUser, eveCharIDs);
            }

            
        }


    }
}
