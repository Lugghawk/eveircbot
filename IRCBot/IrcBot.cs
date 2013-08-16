using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Data;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using libeveapi;
using System.Configuration;
using Ircbot.Database;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace IRCBot {
    class IrcBot {
        private static string SERVER = ConfigurationManager.AppSettings["irc.server"];
        private static int PORT = Convert.ToInt32(ConfigurationManager.AppSettings["irc.port"]);
        private static string NICK = ConfigurationManager.AppSettings["irc.nick"];
        private static string USER = String.Format("USER {0} 8 * : Eve Api Bot.",NICK);
        private static string CHANNEL = ConfigurationManager.AppSettings["irc.channel"];
        private static int MAX_NO_OF_CHARS = 10;

        //global input queue from IRC
        public static Queue<string> inputQueue = new Queue<string>();

        //global users list, stores users and API keys
        public static List<User> users = new List<User>();

        //Dictionary for comparing nickname to user class
        public static Dictionary<string, User> nickDict = new Dictionary<string, User>();

        static NetworkStream stream;
        static StreamReader reader;
        public static StreamWriter writer;
        static TcpClient irc;

        private static ISession mySession;

        static void Main(string[] args) {

            NHibernate.Cfg.Configuration config = new NHibernate.Cfg.Configuration();
            config.Configure();
            config.AddAssembly(typeof(User).Assembly);
            ISessionFactory sessionFactory = config.BuildSessionFactory();
            //var schema = new SchemaExport(config);
            //schema.Create(true, true);
            mySession = sessionFactory.OpenSession();
            List<User> savedUsers = (List<User>)mySession.CreateCriteria<User>().List<User>();
            users.AddRange(savedUsers);
            foreach (User user in users)
            {
                nickDict.Add(user.userName, user);
            }
            string inputLine;
            try {
                irc = new TcpClient(SERVER, PORT);
                stream = irc.GetStream();
                Thread.Sleep(1000);
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);

                writer.AutoFlush = true;

                PingSender ping = new PingSender();
                ping.setServer(SERVER);
                ping.start();

                ActionThread actionThread = new ActionThread();
                actionThread.start();

                writeToIRC(USER);
                //writer.Flush();

                writeToIRC("NICK {0}", NICK);
                //writer.Flush();

                writeToIRC("JOIN {0}", CHANNEL);
                //writer.Flush();

                writeToIRC("privmsg {0} : HELLO I AM HERE", CHANNEL);

                ArrayList results = new ArrayList();

                while (true)
                {
                    while ((inputLine = reader.ReadLine()) != null)
                    {
                        Console.WriteLine(inputLine);
                        inputQueue.Enqueue(inputLine);
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
                Thread.Sleep(5000);

                string[] argv = { };
                Main(argv);
            }
        }


        public static ArrayList InterpretInput(string input){

            ArrayList results = new ArrayList();
            string apiKeyword = "api";
            string nickname;

            //Regex for api key
            Regex apiRegex = new Regex("[0-9a-zA-Z]{64}");
            Match apiMatch = apiRegex.Match(input);

            //Regex for user ID
            Regex idRegex = new Regex("[0-9]{6,}");
            Match idMatch = idRegex.Match(input);

            
            //If the input is a message and it contains the bot's name
            if (input.Contains(NICK) && input.Contains("PRIVMSG"))
            {
                if ((input.IndexOf("!") - 1) < 0) return null;
                nickname = input.Substring(1, input.IndexOf("!") - 1);

                //Test for API input
                if (idMatch.Success &&
                    apiMatch.Success &&
                    input.Contains(apiKeyword))
                {

                    results.Add(1);
                    results.Add(nickname);
                    results.Add(apiMatch.Value);
                    results.Add(idMatch.Value);
                    return results;

                //Test for isk value request
                }
                else if ((input.Contains("isk") ||
                          input.Contains("money") ||
                          input.Contains("isks")) &&
                          input.Contains("how much") ||
                          input.Contains("how many"))
                {
                    results.Add(2);
                    results.Add(nickname);
                    results.Add(input);
                    return results;

                //Test for skill training request
                } else if ((input.Contains("train") || 
                            input.Contains("training")) && 
                            input.Contains("what") &&
                            input.Contains("i")) {
                    results.Add(3);
                    results.Add(nickname);
                    results.Add(input);
                    return results;

                //Test for API status
                } else if (input.Contains("api") &&
                          (input.Contains("status") ||
                          input.Contains("online"))) {
                    results.Add(4); 
                    return results;

                //Test for tranqulity status
                } else if (input.Contains("server") &&
                          (input.Contains("status") ||
                          input.Contains("online"))) {
                    results.Add(5);
                    return results;
                } else if (input.Contains("thanks")) {
                    writeToIRC("PRIVMSG {0} np scrub",CHANNEL);
                }
            }
            return null;
        }


        public static void PerformAction(ArrayList results)
        {
            writer.AutoFlush = true;
            switch ((int)(results[0])) {
                //Case 1: New User
                case 1:
                    CreateNewUserRequest(results);
                    break;

                //Balance request
                case 2:
                    BalanceRequest(results);
                    break;

                //Skill training request
                case 3:
                    SkillTrainRequest(results);
                    break;

                case 4:
                    //Insert API status shit here
                    break;

                case 5:
                    TranquilityStatus();
                    break;
            }
        }

        //Outputs the current status of Tranquility
        private static void TranquilityStatus() {
            ServerStatus status = EveApi.GetServerStatus();

            if (status.ServerOpen) {
                writeToIRC("PRIVMSG {0} : Tranquility is online",CHANNEL);
            } else {
                writeToIRC("PRIVMSG {0} : Tranquility is DOWN sukka",CHANNEL);
            }
        }

        //Outputs the currently training skill of multiple or a single character
        private static void SkillTrainRequest(ArrayList input) {
            //Check if the nickname exists
            if (nickDict.ContainsKey((string)input[1])) {
                User user = (User)nickDict[(string)input[1]];

                int[] charIDList = new int[MAX_NO_OF_CHARS];

                charIDList = CheckCharacterExistence(user, (string)input[2]);

                if (!(charIDList[0] == 0)) {
                    //Print out the character balance for each character specified
                    foreach (int character in charIDList) {
                        PrintSkillTraining(user, character);
                    }
                } else {
                    PrintSkillTraining(user);
                }

            } else {
                writeToIRC("PRIVMSG {0} : Your nick doesn't exist, sorry {1}", CHANNEL , (string)input[1]);
            }
        }

        //Outputs the character balance of multiple or a single character
        private static void BalanceRequest(ArrayList input) {

            //Check if the nickname exists
            if (nickDict.ContainsKey((string)input[1])) {
                User user = (User)nickDict[(string)input[1]];

                int[] charIDList = new int[MAX_NO_OF_CHARS];

                charIDList = CheckCharacterExistence(user, (string)input[2]);

                if (!(charIDList[0] == 0)) {
                    //Print out the character balance for each character specified
                    //If there is more than one character
                    foreach (int character in charIDList) {
                        PrintAccountBalance(user, character);
                    }
                } else {
                    PrintAccountBalance(user);
                }

            } else {
                writeToIRC("PRIVMSG {0} : Your nick doesn't exist, sorry {1}", CHANNEL, (string)input[1]);
            }
        }

        //Create a new user, add key to nickDict, add User to users
        //Says to user
        private static void CreateNewUserRequest(ArrayList input) {
            String apiKeyId = (String)input[2];
            int apiUserId = int.Parse((string)input[3]);
            //Potential new instance of User
            User newUser = new User((string)input[1]);
            UserApi api = new UserApi(apiUserId, apiKeyId);
            newUser.addApi(api);

            //New index reference for searching
            //Test is user already exists
            if (!nickDict.ContainsKey((string)input[1])) {
                //If user doesn't exist, add him to user dict.x
                nickDict.Add(newUser.userName, newUser);
            } else {
                writeToIRC("PRIVMSG {0} : User ({1}) already exists", CHANNEL, newUser.userName);
            }

            //Tell the user they've been added and ask for a default character
            writeToIRC("PRIVMSG {0} : New User ({1}) Added!", CHANNEL, newUser.userName);
            writeToIRC("PRIVMSG {0} : Please select a default character: ", CHANNEL);

            int[] eveCharIDs = new int[MAX_NO_OF_CHARS];

            //Print a list of their characters so they can choose from them
            eveCharIDs = PrintCharacterList(newUser);

            int defaultChar = 0;

            string nextInput = "";
            //Loop will continue until there is non-PONG input.
            //This is multithreaded so its ok to loop forever :)
            while (true) {
                while (true) {
                    while (inputQueue.Count == 0) { }

                    //Look at the queue to see if anything's there
                    nextInput = inputQueue.Peek();

                    //If the response isn't a standard PONG return
                    if (!nextInput.Contains("PONG")) {
                        break;
                        //Otherwise remove the offending PONG
                    } else {
                        IrcBot.inputQueue.Dequeue();
                    }
                }
                //sanitize dis input
                if (nextInput.Contains(newUser.userName)) {
                    Match choiceMatch = Regex.Match(nextInput, " :[1-3]");
                    string choice = null;

                    if (choiceMatch.Value.Length != 0) {
                         choice = choiceMatch.Value.Substring(2, 1);
                    } else {
                        writeToIRC("PRIVMSG {0} : You must enter a valid choice", CHANNEL);

                        //Remove bad input
                        inputQueue.Dequeue();
                    }
                    if (choice != null) {
                        defaultChar = int.Parse(choice);
                        break;
                    }
                    
                }
            }
            //Find character number and assign it to user class
            defaultChar = eveCharIDs[defaultChar - 1];
            newUser.defaultChar = defaultChar;
            ITransaction trans = mySession.BeginTransaction();
            mySession.Save(newUser);
            trans.Commit();
            writeToIRC("PRIVMSG {0} : Done!", CHANNEL);
        }

        //Check to see if input contains requested character
        //Return characterID of any that do.
        //Uses libeveapi
        private static int[] CheckCharacterExistence(User user, string input) {
            UserApi api = user.apis.ElementAt(0);
            CharacterList eveChar = EveApi.GetAccountCharacters(api.apiUserId, api.apiKeyId);
            int counter = 0;
            int[] charIDs = new int[MAX_NO_OF_CHARS];

            foreach (CharacterList.CharacterListItem character in eveChar.CharacterListItems) {
                if (input.Contains(character.Name)) {
                    charIDs[counter++] = character.CharacterId;
                }
            }
            return charIDs;
        }

        //Print the current skill in training.
        //Uses libeveapi
        //Says to channel
        private static void PrintSkillTraining(User user) {
            UserApi api = user.apis.ElementAt(0);
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, user.defaultChar, api.apiKeyId);
            SkillInTraining skillInTrain = EveApi.GetSkillInTraining(api.apiUserId, user.defaultChar, api.apiKeyId);

            if (skillInTrain.SkillCurrentlyInTraining) {
                writeToIRC("PRIVMSG {0} : {1} is currently training {2} to level {3} which finishes at {4}",
                                 CHANNEL, character.Name, skillInTrain.TrainingTypeId,skillInTrain.TrainingToLevel, skillInTrain.TrainingEndTime);

                
            } else {
                writeToIRC("PRIVMSG {0} : {1} Isn't currently training anything",CHANNEL,character.Name);
            }
        }

        //Print the current skill in training. Use separate charID.
        //Uses libeveapi
        //Says to channel
        private static void PrintSkillTraining(User user, int characterID) {
            UserApi api = user.apis.ElementAt(0);
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, characterID, api.apiKeyId);
            SkillInTraining skillInTrain = EveApi.GetSkillInTraining(api.apiUserId, characterID, api.apiKeyId);

            if (skillInTrain.SkillCurrentlyInTraining) {
                //writeToIRC(false, CHANNEL, character.Name, "is currently training", skillInTrain.TrainingTypeId.ToString(), "to level",
                //    skillInTrain.TrainingToLevel.ToString(), "which finishes on", skillInTrain.TrainingEndTime.ToString());

                writeToIRC("PRIVMSG {0} : {1} is currently training {2} to level {3} which finishes at {4}",
                                 CHANNEL, character.Name, skillInTrain.TrainingTypeId, skillInTrain.TrainingEndTime);
            } else {
                writeToIRC("PRIVMSG {0} : {1} Isn't currently training anything", CHANNEL, character.Name);
            }
        }

        //Print the current character balance. Use defaultChar
        //Uses libeveapi
        //Says to channel
        private static void PrintAccountBalance(User user) {
            UserApi api = user.apis.ElementAt(0);
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, user.defaultChar, api.apiKeyId);

            writeToIRC("PRIVMSG {0} : {1} has {2} isk", CHANNEL, character.Name, character.Balance.ToString("N") );
        }

        //Print the current character balance. Use separate charID.
        //Uses libeveapi
        //Says to channel
        private static void PrintAccountBalance(User user, int characterID) {
            UserApi api = user.apis.ElementAt(0);
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, characterID, api.apiKeyId);
            
            writeToIRC("PRIVMSG {0} : {1} has {2} isk", CHANNEL,character.Name,string.Format("{2:n}",character.Balance.ToString("N")));
        }

        //Print a list of characters from an account
        //Return a list of character ID's from account
        //Uses libeveapi
        //Says to channel
        private static int[] PrintCharacterList(User user) {
            UserApi api = user.apis.ElementAt(0);
            CharacterList eveChar = EveApi.GetAccountCharacters(api.apiUserId, api.apiKeyId);

            int [] charIDList = new int[MAX_NO_OF_CHARS];
            int counter = 1;
            foreach (CharacterList.CharacterListItem character in eveChar.CharacterListItems){

                writeToIRC("PRIVMSG {0} : {1} {2}", CHANNEL, counter.ToString(), character.Name);

                charIDList[counter - 1] = character.CharacterId;
                counter++;
            }
            return charIDList;
        }

        public static void writeToIRC(string format, params object[] stringsToWrite) {
            
            string formattedString = String.Format(format, stringsToWrite);

            lock (typeof(IrcBot)) {
                writer.WriteLine(formattedString);
                Console.WriteLine(formattedString);

                writer.Flush();
            }

        }
    }
}

