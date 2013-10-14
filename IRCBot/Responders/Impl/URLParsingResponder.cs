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
        public override List<string> respond(Input input)
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
                        List<string> returns = new List<string>(1);
                        title = HttpUtility.HtmlDecode(title);
                        returns.Add("Link: " + title);
                        return returns;
                    }
                    catch (NullReferenceException nre)
                    {
                        return new List<string>(1);
                    }
                    //}
                }
            }
             return new List<string>();
        }
    }
}
