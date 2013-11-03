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
    public class EveApiResponder : Responder
    {

        private const String RESPONDER_NAME = "EveApiResponder";

        public override string name
        {
            get { return RESPONDER_NAME; }
        }

        List<User> waitingOnResponse = new List<User>();
        Dictionary<User, int[]> userCharList = new Dictionary<User, int[]>();

        public EveApiResponder()
        {
            responseTriggers.Add("!api", "<keyId> <vCode> - Adds an api key to the bot under your username. Will list out characters and require another response.");
            responseTriggers.Add("!system", "<system-name> - Returns information about a system, including constellation and region. Also returns known kills in the last hour");
            responseTriggers.Add("!changechar", "Lists out the characters under your irc nickname, and lets you pick a new default one.");
        }

        public override bool willRespond(Input input)
        {
            if (input == null || input.message == null)
            {
                return false;
            }
            return input.message.StartsWith("!api") || input.message.StartsWith("!system") || input.message.StartsWith("!changechar")/* || input.message.StartsWith("!isk") || input.message.StartsWith("!time") ||
                input.message.StartsWith("!location") || input.message.StartsWith("!characters") ||
                input.message.StartsWith("!server") || input.message.Equals("!skill") */
                || checkWaitingOnUser(input);
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

        public override List<string> respond(Input input)
        {
            List<string> responseStrings = new List<string>();
            foreach (User user in waitingOnResponse)
            {
                if (user.userName.Equals(input.speaker))
                {
                    Match choiceMatch = Regex.Match(input.message, "^[0-9]$");
                    if (!choiceMatch.Success)
                    {
                        responseStrings.Add("That wasn't a valid character number! Try again!");
                        return responseStrings;
                    }
                    else
                    {
                        int defaultChar = Convert.ToInt32(input.message);
                        int charId = userCharList[user][defaultChar - 1];
                        if (charId == 0)//Doesn't actually exist in that array.
                        {
                            responseStrings.Add("Doesn't look like a valid char number");
                            return responseStrings;
                        }
                        user.defaultChar = charId;
                        if (IrcBot.mySession == null)
                        {
                            responseStrings.Add("I appear to be missing my DB. Sorry! Fix me pleaaase!");
                            return responseStrings;
                        }
                        ITransaction trans = IrcBot.mySession.BeginTransaction();
                        IrcBot.mySession.Save(user);
                        trans.Commit();
                        waitingOnResponse.Remove(user);
                        responseStrings.Add("Done!");
                        return responseStrings;
                    }
                }
            }

            if (input.message.StartsWith("!api"))
            {
                if (IrcBot.nickDict.ContainsKey(input.speaker))
                {
                    //writer.replyTo(input, "That user already exists");
                    //return;
                }

                //Regex for api key
                Regex apiRegex = new Regex("[0-9a-zA-Z]{64}");
                Match apiMatch = apiRegex.Match(input.message);
                //Regex for user ID
                Regex idRegex = new Regex("[0-9]{6,}");
                Match idMatch = idRegex.Match(input.message);

                if (!(idMatch.Success || apiMatch.Success))
                {
                    //Doesn't match api key specifications.
                    responseStrings.Add("Doesn't look like an API to me");
                    return responseStrings;
                }

                int apiUserId = Convert.ToInt32(idMatch.Value);
                string apiKeyId = apiMatch.Value;
                User newUser = null;
                newUser = (User)IrcBot.mySession.CreateCriteria<User>().Add(Restrictions.Eq("userName", input.speaker)).UniqueResult();

                if (newUser == null)
                {
                    newUser = new User(input.speaker);
                    IrcBot.nickDict.Add(newUser.userName, newUser);
                }

                
                UserApi api = new UserApi(apiUserId, apiKeyId);
                newUser.addApi(api);

                //since user doesn't exist, add him to user dict.x



                //Tell the user they've been added and ask for a default character
                responseStrings.Add(String.Format("New User ({0}) Added!", newUser.userName));
                responseStrings.Add("Please select a default character");

                int[] eveCharIDs = new int[IrcBot.MAX_NO_OF_CHARS];

                eveCharIDs = PrintCharacterList(newUser, responseStrings, input);
                //Add this person to the list of people we're waiting on input from.
                waitingOnResponse.Add(newUser);
                userCharList.Add(newUser, eveCharIDs);
            }

            if (input.message.StartsWith("!system"))
            {
                string systemName = null;
                try
                {
                    systemName = input.message.Split(new char[] { ' ' }, 2)[1];
                    //If no arguments provided, this is actually out of bounds.
                }
                catch (IndexOutOfRangeException)
                {
                    
                    responseStrings.Add("I think you forgot something...");
                    return responseStrings;
                }

                //(List<InvType>)IrcBot.mySession.CreateCriteria<InvType>().Add(Restrictions.InsensitiveLike("typeName", itemName+"%")).List<InvType>();
                SolarSystem system = (SolarSystem)IrcBot.mySession.CreateCriteria<SolarSystem>().Add(Restrictions.Eq("solarSystemName", systemName)).UniqueResult();
                if (system == null)
                {
                    responseStrings.Add("Cannot find system: " + systemName);
                    return responseStrings;
                }
                MapKills eveMapKills = EveApi.GetMapKills();
                MapKills.MapKillsItem kills = null;
                foreach (MapKills.MapKillsItem map in eveMapKills.MapSystemKills)
                {
                    if (map.SolarSystemId == system.solarSystemID)
                    {
                        kills = map;
                    }
                }
                responseStrings.Add(string.Format("System: {0}. Constellation: {1}. Region: {2}. Security Status: {3}", system.solarSystemName, system.constellation.constellationName, system.region.regionName, system.security));
                if (kills != null)
                {
                    responseStrings.Add(string.Format("Kills in the last hour: {0} ships, {1} pods", kills.ShipKills, kills.PodKills));
                }
                else
                {
                    responseStrings.Add("No known kills in the last hour");
                }

                return responseStrings;
            }

            if (input.message.StartsWith("!changechar"))
            {
                int[] eveCharIDs = new int[IrcBot.MAX_NO_OF_CHARS];
                
                User newUser = null;
                try
                {
                    newUser = (User)IrcBot.mySession.CreateCriteria<User>().Add(Restrictions.Eq("userName", input.speaker)).UniqueResult();
                }
                catch
                {
                    responseStrings.Add("Unique Result failed. Please contact admin");
                    return responseStrings;
                }
                eveCharIDs = PrintCharacterList(newUser, responseStrings, input);
                if (! userCharList.ContainsKey(newUser))
                {
                    userCharList.Add(newUser, eveCharIDs);
                }
                
                waitingOnResponse.Add(newUser);
                return responseStrings;
            }

            return responseStrings;

        }

        
        //Print a list of characters from an account
        //Return a list of character ID's from account
        //Uses libeveapi
        int[] PrintCharacterList(User user, List<String> charNames, Input input)
        {
            //UserApi api = user.apis.ElementAt(0);
            int[] charIDList = new int[10];
            int counter = 1;
            foreach (UserApi api in user.apis)
            {
                CharacterList eveChar = EveApi.GetAccountCharacters(api.apiUserId, api.apiKeyId);
                foreach (CharacterList.CharacterListItem character in eveChar.CharacterListItems)
                {
                    Character eveCharacter = new Character(character.Name, character.CharacterId);
                    bool foundChar = false;
                    if (user.characters != null)
                    {
                        foreach (Character userCharacter in user.characters)
                        {
                            if (userCharacter.apiCharacterId == eveCharacter.apiCharacterId)
                            {
                                foundChar = true;
                            }
                        }
                    }
                    if (!foundChar)
                    {
                        eveCharacter.api = api;
                        user.addCharacter(eveCharacter);
                    }

                    charNames.Add(String.Format("{0} {1}", counter.ToString(), character.Name));
                    charIDList[counter - 1] = character.CharacterId;
                    counter++;
                }
            }
            return charIDList;
        }


            
        }        
        
        



    }

