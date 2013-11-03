﻿using System;
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

        public override List<string> respond(Input input)
        {
            if (input.message.StartsWith("!corpjobs")) return getCorpJobs(input);
            if (input.message.StartsWith("!listcorpjobs")) return getJobList(input);
            return new List<String>();
        }

        private List<string> getJobList(Input input)
        {
            List<String> returnStrings = new List<String>();
            return returnStrings;
        }


        private List<string> getCorpJobs(Input input)
        {
            List<String> returnStrings = new List<String>();
            User user = null;
            try
            {
                user = IrcBot.nickDict[input.speaker];
            }
            catch (KeyNotFoundException)
            {
                returnStrings.Add("I don't have your character listed. Please add an api call");
                return returnStrings;
            }

            Dictionary<string, List<IndustryJobList.IndustryJobListItem>> corpJobs = getCorpsJobsForUser(user);
            if (corpJobs.Count == 0)
            {
                returnStrings.Add("Found no jobs in any known api");
                return returnStrings;
            }
            foreach (KeyValuePair<string, List<IndustryJobList.IndustryJobListItem>> job in corpJobs)
            {
                returnStrings.Add(String.Format("I found {0} active jobs for {1}", job.Value.Count, job.Key));
            }


            return returnStrings;
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