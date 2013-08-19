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
using NHibernate.Criterion;
using IRCBot.Responders;
using IRCBot.Responders.Impl;

namespace IRCBot {
    class IrcConnection {
        static NetworkStream stream;
        static StreamReader reader;
        static StreamWriter writer;
        static TcpClient irc;
        
        public IrcConnection(string server, int port, string nick) {
            irc = new TcpClient(server, port);
            stream = irc.GetStream();
            Thread.Sleep(1000);
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            rawWrite(String.Format("USER {0} 8 * : Eve Api Bot.", nick));
            rawWrite(String.Format("NICK {0}", nick));
        }

        public void rawWrite(string message) {
            lock (typeof(IrcBot)) {
                try {
                    writer.WriteLine(message);
                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                    System.Environment.Exit(1);
                }
                if (!message.StartsWith("PING")) {
                    Console.WriteLine("OUTBOUND :: " + message);
                }
            }
        }

        public void joinChannel(string channel) {
            rawWrite(String.Format("JOIN {0}", channel));
        }

        public void notice(string target, string message) {
            rawWrite(String.Format("NOTICE {0} :{1}", target, message));
        }

        public void privmsg(string target, string message) {
            rawWrite(String.Format("privmsg {0} :{1}", target, message));
        }

        public string ReadLine() {
            string line = reader.ReadLine();
            string[] parts = line.Split(' ');
            if (parts.Length > 1 && !parts[1].Equals("PONG")) {
                IRCBot.Responders.Input parsed = IRCBot.Responders.Input.parse(line);
                string content = line;
                if (parsed != null) {
                    content = parsed.toString();
                }
                Console.WriteLine("INCOMING :: " + content);
            }
            if (line == null) {
                System.Environment.Exit(1);
            }
            return line;
        }
    }
    
    class IrcBot {
        private static string SERVER = ConfigurationManager.AppSettings["irc.server"];
        private static int PORT = Convert.ToInt32(ConfigurationManager.AppSettings["irc.port"]);
        private static string NICK = ConfigurationManager.AppSettings["irc.nick"];
        private static string CHANNEL = ConfigurationManager.AppSettings["irc.channel"];
        public static int MAX_NO_OF_CHARS = 10;

        public static IrcConnection connection = new IrcConnection(SERVER, PORT, NICK);

        //global input queue from IRC
        public static Queue<string> inputQueue = new Queue<string>();

        //global users list, stores users and API keys
        public static List<User> users = new List<User>();

        //Dictionary for comparing nickname to user class
        public static Dictionary<string, User> nickDict = new Dictionary<string, User>();

        public static List<SkillTree.Skill> skillList = null;

        public static ISession mySession;
        public static IResponder responder = new EveApiResponder();

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
            
            skillList = getSkillList();

            foreach (User user in users)
            {
                nickDict.Add(user.userName, user);
            }
            string inputLine;
            try {
                PingSender ping = new PingSender(connection);
                ping.setServer(SERVER);
                ping.start();

                ActionThread actionThread = new ActionThread();
                actionThread.start();

                connection.joinChannel(CHANNEL);

                connection.privmsg(CHANNEL, "Reporting for duty!");
                ArrayList results = new ArrayList();

                while (true)
                {
                    while ((inputLine = connection.ReadLine()) != null)
                    {
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

        private static List<SkillTree.Skill> getSkillList()
        {
            SkillTree skillTree = EveApi.GetSkillTree();
            List<SkillTree.Skill> skills = new List<SkillTree.Skill>();
            
            foreach (SkillTree.Skill skill in skillTree.Skills)
            {
                skills.Add(skill);
            }
            return skills;
            
        }

        public static SkillTree.Skill getSkillById(int skillId)
        {
            SkillTree.Skill skill =
                (from s in skillList
                where s.TypeId == skillId
                select s).Single();
            return skill;
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

            if ((input.IndexOf("!") - 1) < 0) return null;
            nickname = input.Substring(1, input.IndexOf("!") - 1);

            Input messageInput = Input.parse(input);
            
            if (responder.willRespond(messageInput))
            {
                responder.respond(connection, messageInput);
            }
            //If the input is a message and it contains the bot's name
            if (input.Contains(NICK) && input.Contains("PRIVMSG"))
            {
                
                ////Test for API input
                //if (idMatch.Success &&
                //    apiMatch.Success &&
                //    input.Contains(apiKeyword))
                //{

                //    results.Add(1);
                //    results.Add(nickname);
                //    results.Add(apiMatch.Value);
                //    results.Add(idMatch.Value);
                //    return results;

                ////Test for isk value request
                //}
                //else 
                if ((input.Contains("isk") ||
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
                    connection.privmsg(CHANNEL, "np");
                }

            }
            else if (input.Contains("!server") || input.Contains("!tq"))
            {
                results.Add(5);
                return results;
            }
            else if (input.Contains("!characters"))
            {
                results.Add(6);
                results.Add(nickname);
                return results;
            }
            else if (input.Contains("!isk") || input.Contains("!wallet"))
            {
                results.Add(2);
                results.Add(nickname);
                results.Add(input);
                return results;
            }
            else if (input.Contains("!skill") || input.Contains("!train"))
            {
                results.Add(3);
                results.Add(nickname);
                results.Add(input);
                return results;
            }
            else if (input.Contains("!system") || input.Contains("!location"))
            {
                results.Add(7);
                results.Add(nickname);
                return results;
            }
            else if (input.Contains("!time"))
            {
                results.Add(8);
                return results;
            }
            return null;
        }


        public static void PerformAction(ArrayList results)
        {
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
                case 6:
                    WriteCharacterforUser(results);
                    break;
                case 7:
                    getCharacterLocation(results);
                    break;
                case 8:
                    writeEveServerTime();
                    break;
            }
        }

        private static void writeEveServerTime()
        {
            ServerStatus serverStatus = getServerStatus();
            connection.privmsg(CHANNEL,String.Format("The time on Tranquility is currently {0}", serverStatus.CurrentTime));
        }

        private static void getCharacterLocation(ArrayList input)
        {
            String nick = (String)input[1];
            User user = getUserByNick(nick);
            if (user == null) {
                return;
            }
            UserApi api = user.apis.ElementAt(0);
            CharacterInfo charInfo = EveApi.GetCharacterInfo(api.apiUserId, user.defaultChar, api.apiKeyId);
            connection.privmsg(CHANNEL, String.Format("{0} is currently in {1}", charInfo.name, charInfo.location));
            
        }

        private static User getUserByNick(String nickname)
        {
            try {
                return (User) mySession.CreateCriteria<User>().Add(Restrictions.Eq("userName", nickname)).UniqueResult();
            } catch {
                connection.privmsg(CHANNEL, "No details map to " + nickname);
                return null;
            }
        }

        private static void WriteCharacterforUser(ArrayList input)
        {
            String nick = (String)input[1];
            User user = getUserByNick(nick);
            if (user == null) {
                return;
            }

            List<Character> user_chars = (List<Character>)mySession.CreateCriteria<Character>().Add(Restrictions.Eq("user_id", user)).List<Character>();
            connection.privmsg(CHANNEL, "Your characters are as follows:");
            foreach (Character character in user_chars)
            {
                if (user.defaultChar.Equals(character.apiCharacterId))
                {
                    connection.privmsg(CHANNEL, String.Format("{0} - {1} (Default)", character.apiCharacterId, character.characterName));
                }
                else
                {
                    connection.privmsg(CHANNEL, String.Format("{0} - {1}", character.apiCharacterId, character.characterName));
                }
            }
            
        }

        private static ServerStatus getServerStatus()
        {
            return EveApi.GetServerStatus();
        }

        //Outputs the current status of Tranquility
        private static void TranquilityStatus() {
            ServerStatus status = EveApi.GetServerStatus();
            string message;
            if (status.ServerOpen) {
                message = String.Format("Tranquility is online and has {0} players logged in", status.OnlinePlayers);
            } else {
                message = "Tranquility is DOWN";
            }
            connection.privmsg(CHANNEL, message);
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
                connection.privmsg(CHANNEL, String.Format("Your nick doesn't exist, sorry {0}", (string)input[1]));
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
                connection.privmsg(CHANNEL, String.Format("Your nick doesn't exist, sorry {0}", (string)input[1]));
            }
        }

        //Create a new user, add key to nickDict, add User to users
        //Says to user
        public static void CreateNewUserRequest(ArrayList input) {
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
                connection.privmsg(CHANNEL, String.Format("User ({0}) already exists", newUser.userName));
            }

            //Tell the user they've been added and ask for a default character
            connection.privmsg(CHANNEL, String.Format("New User ({0}) Added!", newUser.userName));
            connection.privmsg(CHANNEL, "Please select a default character");

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
                        connection.privmsg(CHANNEL, "You must enter a valid choice");

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
            if (mySession == null) {
                connection.notice(CHANNEL, "I appear to be missing my db, sorry");
                return;
            }
            ITransaction trans = mySession.BeginTransaction();
            mySession.Save(newUser);
            trans.Commit();
            connection.privmsg(CHANNEL, "Done!");
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
                connection.privmsg(CHANNEL, String.Format("{0} is currently training {1} to level {2} which finishes at {3}",
                                 character.Name, getSkillById(skillInTrain.TrainingTypeId).TypeName, skillInTrain.TrainingToLevel, skillInTrain.TrainingEndTime));

                
            } else {
                connection.privmsg(CHANNEL, String.Format("{0} Isn't currently training anything", character.Name));
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
                connection.privmsg(CHANNEL, String.Format("{0} is currently training {1} to level {2} which finishes at {3}",
                                 character.Name, getSkillById(skillInTrain.TrainingTypeId).TypeName, skillInTrain.TrainingEndTime));
            } else {
                connection.privmsg(CHANNEL, String.Format("{0} isn't currently training anything", character.Name));
            }
        }

        //Print the current character balance. Use defaultChar
        //Uses libeveapi
        //Says to channel
        private static void PrintAccountBalance(User user) {
            UserApi api = user.apis.ElementAt(0);
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, user.defaultChar, api.apiKeyId);

            connection.privmsg(CHANNEL, String.Format("{0} has {1} isk", character.Name, character.Balance.ToString("N")));
        }

        //Print the current character balance. Use separate charID.
        //Uses libeveapi
        //Says to channel
        private static void PrintAccountBalance(User user, int characterID) {
            UserApi api = user.apis.ElementAt(0);
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, characterID, api.apiKeyId);

            connection.privmsg(CHANNEL, String.Format("{0} has {1} isk", character.Name, string.Format("{2:n}", character.Balance.ToString("N"))));
        }

        //Print a list of characters from an account
        //Return a list of character ID's from account
        //Uses libeveapi
        //Says to channel
        public static int[] PrintCharacterList(User user) {
            UserApi api = user.apis.ElementAt(0);
            CharacterList eveChar = EveApi.GetAccountCharacters(api.apiUserId, api.apiKeyId);

            int [] charIDList = new int[MAX_NO_OF_CHARS];
            int counter = 1;
            foreach (CharacterList.CharacterListItem character in eveChar.CharacterListItems){
                Character eveCharacter = new Character(character.Name, character.CharacterId);
                user.addCharacter(eveCharacter);
                connection.privmsg(CHANNEL, String.Format("{0} {1}", counter.ToString(), character.Name));
                charIDList[counter - 1] = character.CharacterId;
                counter++;
            }
            return charIDList;
        }


    }
}

