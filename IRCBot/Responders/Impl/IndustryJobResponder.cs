using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ircbot.Database;
using Iesi.Collections.Generic;
using libeveapi;
using log4net;

namespace IRCBot.Responders.Impl
{
    class IndustryJobResponder : Responder
    {

        private const String RESPONDER_NAME = "IndustryJobResponder";

        public override string name
        {
            get { return RESPONDER_NAME; }
        }

        private static readonly ILog log = LogManager.GetLogger(typeof(IndustryJobResponder));
        public IndustryJobResponder()
        {
            responseTriggers.Add("!corpjobs", "Gets a number of active corporation industry jobs");
            responseTriggers.Add("!listjobs", "Returns a list of jobs for the corporation, seperated by Type");
        }

        public override void respond(Input input)
        {
            if (input.message.StartsWith("!corpjobs"))  getCorpJobs(input);
            if (input.message.StartsWith("!listcorpjobs")) getJobList(input);
            return;
        }

        private void getJobList(Input input)
        {
            return;
        }


        private void getCorpJobs(Input input)
        {
            User user = null;
            try
            {
                user = IrcBot.nickDict[input.speaker];
            }
            catch (KeyNotFoundException)
            {
                addResponse("I don't have your character listed. Please add an api call");
                return;
            }

            Dictionary<string, List<IndustryJobList.IndustryJobListItem>> corpJobs = getCorpsJobsForUser(user);
            if (corpJobs.Count == 0)
            {
                addResponse("Found no jobs in any known api");
                return;
            }
            foreach (KeyValuePair<string, List<IndustryJobList.IndustryJobListItem>> job in corpJobs)
            {
                addResponse(String.Format("I found {0} active jobs for {1}", job.Value.Count, job.Key));
            }


            return;
        }


        private Dictionary<string, List<IndustryJobList.IndustryJobListItem>> getCorpsJobsForUser(User user)
        {

            Iesi.Collections.Generic.ISet<UserApi> apis = user.apis;


            Dictionary<string, List<IndustryJobList.IndustryJobListItem>> corpJobs = new Dictionary<string, List<IndustryJobList.IndustryJobListItem>>();
            foreach (UserApi api in apis)
            {
                foreach (Character character in api.characters)
                {
                    try
                    {
                        List<IndustryJobList.IndustryJobListItem> industryJobs = new List<IndustryJobList.IndustryJobListItem>();
                        String corp = EveApi.GetCorporationSheet(api.apiUserId, character.apiCharacterId, api.apiKeyId).CorporationName;
                        if (corpJobs.ContainsKey(corp))
                        {
                            continue;
                        }
                        IndustryJobList jobs = EveApi.GetIndustryJobList(IndustryJobListType.Corporation, api.apiUserId, character.apiCharacterId, api.apiKeyId);
                        foreach (IndustryJobList.IndustryJobListItem job in jobs.IndustryJobListItems)
                        {
                            if (job.EndProductionTimeLocal > DateTime.Now)
                            {
                                industryJobs.Add(job);

                            }
                        }

                        corpJobs.Add(corp, industryJobs);

                    }
                    catch (System.Net.WebException webException)
                    {
                        //Probably a 403, but we should refine this a bit.
                        log.Debug("Got a" + webException.GetType().ToString() + " when trying to get " + character.characterName + "'s corp job sheet.");
                    }
                    catch (Exception e)
                    {
                        log.Warn(e);
                    }

                }
            }
            return corpJobs;
        }
    }
}
