using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;


namespace IRCBot.Responders.Impl
{
    class URLParsingResponder : Responder
    {

        private const String NAME = "URLParsingResponder";

        string urlRegex = "http(s)?://";
        public URLParsingResponder()
        {
            responseTriggers.Add("Any url", "Will parse the url and return a title so the channel can see what they're getting themselves into");
        }

        public override bool willRespond(Input input)
        {
            foreach (string word in input.message.Split(' '))
            {
                Match httpMatch = Regex.Match(word, urlRegex);
                if (httpMatch.Success)
                {
                    return true;
                }

            }
            return false;
            
        }
        public override void respond(Input input)
        {
             foreach (string word in input.message.Split(' '))
            {
                Match httpMatch = Regex.Match(word, urlRegex);
                if (httpMatch.Success)
                {

                    HtmlDocument page = new HtmlWeb().Load(word);
                    
                    //if (page.ParseErrors != null && page.ParseErrors.Count() == 0)
                    //{
                    try
                    {
                        string title = page.DocumentNode.SelectSingleNode("//head/title").InnerText;
                        title = title.Replace("\r\n", "");
                        title = title.Replace("\t", "");
                        title = HttpUtility.HtmlDecode(title);
                        addResponse("Link: " + title);
                        return;
                    }
                    catch (NullReferenceException)
                    {
                        return;
                    }
                    //}
                }
            }
             return;
        }

        public override string name
        {
            get { return NAME; }
        }
    }
}
