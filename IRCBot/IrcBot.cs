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
using IRCBot.Pollers;
using Spring.Context;
using Spring.Core;
using Spring.Context.Support;

namespace IRCBot {
    class IrcConnection {
        static NetworkStream stream;
        static StreamReader reader;
        static StreamWriter writer;
        static TcpClient irc;
        string nick;
        
        public IrcConnection(string server, int port, string nick) {
            this.nick = nick;
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

        public void replyTo(Input input, string message) {
            if (input.target.StartsWith(nick)) {
                privmsg(input.speaker, message);
            } else {
                privmsg(input.target, message);
            }
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
        public static Dictionary<string, int> skillIds = new Dictionary<string, int>();

        public static List<SkillTree.Skill> skillList = null;

        public static ISession mySession;
        public static List<IResponder> botResponders = new List<IResponder>();
        public static List<Poller> pollers = new List<Poller>();
        public static PollerManager pollerManager;
        

        static void Main(string[] args) {
            IApplicationContext context = new XmlApplicationContext("IrcBot-applicationContext.xml");
            botResponders = (List<IResponder>)context.GetObject("responderList");
            NHibernate.Cfg.Configuration config = new NHibernate.Cfg.Configuration();
            config.Configure();
            config.AddAssembly(typeof(User).Assembly);
            ISessionFactory sessionFactory = config.BuildSessionFactory();
            //var schema = new SchemaExport(config);
            //schema.Create(true, true);
            mySession = sessionFactory.OpenSession();
            List<User> savedUsers = (List<User>)mySession.CreateCriteria<User>().List<User>();
            users.AddRange(savedUsers);
            
            //Start pollers
            pollers = (List<Poller>)context.GetObject("pollerList");
            //set the channel and connection objects.
            foreach (Poller poller in pollers)
            {
                poller.connection = connection;
                poller.channel = CHANNEL;
            }
            pollerManager = new PollerManager(pollers);



            skillList = getSkillList();
            foreach (SkillTree.Skill skill in skillList) {
                skillIds.Add(skill.TypeName.ToLower(), skill.TypeId);
            }

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
            Input messageInput = Input.parse(input);
            if (messageInput == null) {
                return null;
            }
            foreach (IResponder botResponder in botResponders)
            {
                if (botResponder.willRespond(messageInput))
                {
                    try
                    {
                        botResponder.respond(connection, messageInput);
                    }
                    catch (WebException webex)
                    {
                        connection.replyTo(messageInput, "Got a 403 error trying to reach: "+webex.Response.ResponseUri);
                        return null;
                    }
                    return null;
                }
            }
            string nickname = messageInput.speaker;
            input = messageInput.message;
            if (input.StartsWith("!server") || input.StartsWith("!tq"))
            {
                results.Add(5);
                return results;
            } else if (input.StartsWith("!characters"))
            {
                results.Add(6);
                results.Add(nickname);
                return results;
            } else if (input.StartsWith("!isk") || input.StartsWith("!wallet"))
            {
                results.Add(2);
                results.Add(nickname);
                results.Add(input);
                return results;
            }
            else if (input.StartsWith("!skill"))
            {
                results.Add(3);
                results.Add(nickname);
                results.Add(input);
                return results;
            } else if (input.StartsWith("!system") || input.StartsWith("!location"))
            {
                results.Add(7);
                results.Add(nickname);
                return results;
            } else if (input.StartsWith("!time"))
            {
                results.Add(8);
                return results;
            }
            return null;
        }


        public static void PerformAction(ArrayList results)
        {
            try
            {
                switch ((int)(results[0]))
                {
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
            catch (WebException webex)
            {
                connection.privmsg(CHANNEL, "Got an error trying to access the api:" + webex.ToString());
                return;    
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
            Character eveChar = mySession.CreateCriteria<Character>().Add(Restrictions.Eq("apiCharacterId", user.defaultChar)).UniqueResult<Character>();
            UserApi api = eveChar.api;
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
                        if (character != 0)
                        {
                            PrintSkillTraining(user, character);
                        }
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
                        if (character != 0)
                        {
                            PrintAccountBalance(user, character);
                        }
                    }
                } else {
                    PrintAccountBalance(user);
                }

            } else {
                connection.privmsg(CHANNEL, String.Format("Your nick doesn't exist, sorry {0}", (string)input[1]));
            }
        }

        //Check to see if input contains requested character
        //Return characterID of any that do.
        //Uses libeveapi
        private static int[] CheckCharacterExistence(User user, string input) {
            //Character userChar = mySession.CreateCriteria<Character>().Add(Restrictions.Eq("apiCharacterId", user.defaultChar)).UniqueResult<Character>();
            HashSet<UserApi> apis = new HashSet<UserApi>(user.apis);
            int[] charIDs = new int[MAX_NO_OF_CHARS];
            foreach (UserApi api in apis)
            {
                CharacterList eveChar = EveApi.GetAccountCharacters(api.apiUserId, api.apiKeyId);
                int counter = 0;
                

                foreach (CharacterList.CharacterListItem character in eveChar.CharacterListItems)
                {
                    if (input.ToLower().Contains(character.Name.ToLower()))
                    {
                        charIDs[counter++] = character.CharacterId;
                    }
                }
            }
            return charIDs;
        }

        //Print the current skill in training.
        //Uses libeveapi
        //Says to channel
        private static void PrintSkillTraining(User user) {
            Character eveChar = mySession.CreateCriteria<Character>().Add(Restrictions.Eq("apiCharacterId",user.defaultChar)).UniqueResult<Character>();
            UserApi api = eveChar.api;
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, eveChar.apiCharacterId, api.apiKeyId);
            SkillInTraining skillInTrain = EveApi.GetSkillInTraining(api.apiUserId, eveChar.apiCharacterId, api.apiKeyId);

            if (skillInTrain.SkillCurrentlyInTraining) {
                DateTime dt = DateTime.SpecifyKind(Convert.ToDateTime(skillInTrain.TrainingEndTime), DateTimeKind.Utc);
                TimeSpan timeTillDone = dt - skillInTrain.CurrentTime;
                string timeTillDoneString = getTimeTillDoneString(ref timeTillDone);
                connection.privmsg(CHANNEL, String.Format("{0} is currently training {1} to level {2} which finishes at {3}. ({4})",
                                 character.Name, getSkillById(skillInTrain.TrainingTypeId).TypeName, skillInTrain.TrainingToLevel, dt.ToString(), timeTillDoneString));

                
            } else {
                connection.privmsg(CHANNEL, String.Format("{0} Isn't currently training anything", character.Name));
            }
        }

        private static string getTimeTillDoneString(ref TimeSpan timeTillDone)
        {
            string timeTillDoneString = "";
            if (timeTillDone.Days > 0)
            {
                timeTillDoneString += string.Format("{0} days, ", timeTillDone.Days);
            }
            if (timeTillDone.Days > 0 || timeTillDone.Hours > 0)
            {
                timeTillDoneString += string.Format("{0} hours, ", timeTillDone.Hours);
            }
            //Minutes should pretty much always be there.
            timeTillDoneString += string.Format("{0} minutes.", timeTillDone.Minutes);
            return timeTillDoneString;
        }

        //Print the current skill in training. Use separate charID.
        //Uses libeveapi
        //Says to channel
        private static void PrintSkillTraining(User user, int characterID) {
            Character eveChar = mySession.CreateCriteria<Character>().Add(Restrictions.Eq("apiCharacterId", characterID)).UniqueResult<Character>();
            UserApi api = eveChar.api;
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, characterID, api.apiKeyId);
            SkillInTraining skillInTrain = EveApi.GetSkillInTraining(api.apiUserId, characterID, api.apiKeyId);

            if (skillInTrain.SkillCurrentlyInTraining) {
                DateTime dt = DateTime.SpecifyKind(Convert.ToDateTime(skillInTrain.TrainingEndTime), DateTimeKind.Utc);
                TimeSpan timeTillDone = dt - skillInTrain.CurrentTime;
                string timeTillDoneString = getTimeTillDoneString(ref timeTillDone);
                connection.privmsg(CHANNEL, String.Format("{0} is currently training {1} to level {2} which finishes at {3}. ({4})",
                                 character.Name, getSkillById(skillInTrain.TrainingTypeId).TypeName, skillInTrain.TrainingToLevel, dt.ToString(), timeTillDoneString));
            } else {
                connection.privmsg(CHANNEL, String.Format("{0} isn't currently training anything", character.Name));
            }
        }

        //Print the current character balance. Use defaultChar
        //Uses libeveapi
        //Says to channel
        private static void PrintAccountBalance(User user) {
            Character eveChar = mySession.CreateCriteria<Character>().Add(Restrictions.Eq("apiCharacterId", user.defaultChar)).UniqueResult<Character>();
            UserApi api = eveChar.api;
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, user.defaultChar, api.apiKeyId);

            connection.privmsg(CHANNEL, String.Format("{0} has {1} isk", character.Name, character.Balance.ToString("N")));
        }

        //Print the current character balance. Use separate charID.
        //Uses libeveapi
        //Says to channel
        private static void PrintAccountBalance(User user, int characterID) {
            Character eveChar = mySession.CreateCriteria<Character>().Add(Restrictions.Eq("apiCharacterId", characterID)).UniqueResult<Character>();
            UserApi api = eveChar.api;
            CharacterSheet character = EveApi.GetCharacterSheet(api.apiUserId, characterID, api.apiKeyId);

            connection.privmsg(CHANNEL, String.Format("{0} has {1} isk", character.Name, character.Balance.ToString("N")));
        }




    }
}

