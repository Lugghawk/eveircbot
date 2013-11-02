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
        private static readonly ILog log = LogManager.GetLogger(typeof(IndustryJobResponder));
        public IndustryJobResponder()
        {
            responseTriggers.Add("!corpjobs", "Gets a list of ongoing corporation industry jobs");
        }

        public override List<string> respond(Input input)
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
            Iesi.Collections.Generic.ISet<UserApi> apis = user.apis;

            
            Dictionary<string, List<IndustryJobList.IndustryJobListItem>> corpJobs = new Dictionary<string, List<IndustryJobList.IndustryJobListItem>>();
            foreach (UserApi api in apis) { 
                foreach (Character character in api.characters){
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
                        log.Debug("Got a" + webException.GetType().ToString() + " when trying to get " + character.characterName + "'s corp job sheet.");
                    }
                    catch (Exception e)
                    {
                        returnStrings.Add(e.GetType().ToString() + " occurred when trying to get " + character.characterName + "'s corp job sheet.");
                        log.Warn(e);
                    }
                    
                }
            }
            if (corpJobs.Count == 0)
            {
                returnStrings.Add("Found no jobs in any known api");
                return returnStrings;
            }
            foreach (KeyValuePair<string,List<IndustryJobList.IndustryJobListItem>> job in corpJobs)
            {
                returnStrings.Add(String.Format("I found {0} active jobs for {1}", job.Value.Count, job.Key));
            }


            return returnStrings;
        }
    }
}
